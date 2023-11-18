
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

[HarmonyPatch(typeof(ZDOMan))]
public class Manager
{
  public static Info? SelectDestroy(ZDO zdo) => Select(Loading.RemoveDatas, zdo, zdo.GetPrefab(), zdo.GetPosition());
  public static Info? SelectCreate(ZDO zdo, int prefab, Vector3 pos) => Select(Loading.CreateDatas, zdo, prefab, pos);
  private static Info? Select(Dictionary<int, List<Info>> infos, ZDO zdo, int prefab, Vector3 pos)
  {
    if (!infos.TryGetValue(prefab, out var data)) return null;
    if (data.Count == 0) return null;
    var biome = WorldGenerator.instance.GetBiome(pos);
    var distance = Utils.DistanceXZ(pos, Vector3.zero);
    var altitude = WorldGenerator.instance.GetHeight(pos.x, pos.z) - WorldInfo.WaterLevel;
    var day = EnvMan.instance.IsDay();
    var linq = data
      .Where(d => (d.Biomes & biome) == biome)
      .Where(d => d.Day || !day)
      .Where(d => d.Night || day)
      .Where(d => distance >= d.MinDistance)
      .Where(d => distance < d.MaxDistance)
      .Where(d => altitude >= d.MinAltitude)
      .Where(d => altitude < d.MaxAltitude)
      .Where(d => Helper.HasEveryGlobalKey(d.GlobalKeys))
      .Where(d => !Helper.HasAnyGlobalKey(d.BannedGlobalKeys));
    // Minor optimization to resolve simpler checks first (not measured).
    linq = linq.ToArray();
    if (linq.Any(d => d.Environments.Count > 0) || linq.Any(d => d.BannedEnvironments.Count > 0))
    {
      var environment = GetEnvironment(biome);
      linq = linq
        .Where(d => d.Environments.Count == 0 || d.Environments.Contains(environment))
        .Where(d => !d.BannedEnvironments.Contains(environment)).ToArray();
    }
    if (linq.Any(d => d.Events.Count > 0))
    {
      var events = EWP.GetCurrentEvent(pos);
      linq.Where(d => d.Events.Count == 0 || d.Events.Contains(events.m_name) && d.EventDistance >= Utils.DistanceXZ(pos, events.m_pos)).ToArray();
    }
    if (linq.Any(d => d.Objects.Count > 0))
    {
      linq = linq.Where(d =>
      {
        if (d.Objects.Count == 0) return true;
        return ZDOMan.instance.m_objectsByID.Values.Any(zdo => d.Objects.Contains(zdo.m_prefab) && Utils.DistanceXZ(zdo.m_position, pos) <= d.ObjectDistance);
      }).ToArray();
    }
    if (linq.Any(d => d.Locations.Count > 0))
    {
      var zone = ZoneSystem.instance.GetZone(pos);
      linq = linq.Where(d =>
      {
        if (d.Locations.Count == 0) return true;
        // +1 because the location can be at zone edge, so any distance can be on the next zone.
        var di = (int)(d.LocationDistance / 64f) + 1;
        var dj = (int)(d.LocationDistance / 64f) + 1;
        var minI = zone.x - di;
        var maxI = zone.x + di;
        var minJ = zone.y - dj;
        var maxJ = zone.y + dj;

        for (int i = minI; i <= maxI; i++)
        {
          for (int j = minJ; j <= maxJ; j++)
          {
            var key = new Vector2i(i, j);
            if (!ZoneSystem.instance.m_locationInstances.TryGetValue(key, out var loc)) continue;
            if (!d.Locations.Contains(loc.m_location.m_hash)) continue;
            var dist = d.LocationDistance == 0 ? loc.m_location.m_exteriorRadius : d.LocationDistance;
            if (Utils.DistanceXZ(loc.m_position, pos) > dist) continue;
            return true;
          }
        }
        return false;
      }).ToArray();
    }
    if (linq.Any(d => d.Filters.Length > 0))
    {
      linq = linq.Where(d => d.Filters.All(f => f.Valid(zdo))).ToArray();
    }
    if (linq.Any(d => d.BannedFilters.Length > 0))
    {
      linq = linq.Where(d => d.BannedFilters.All(f => !f.Valid(zdo))).ToArray();
    }
    var valid = linq.ToArray();
    if (valid.Length == 0) return null;
    if (valid.Length == 1 && valid[0].Weight >= 1f) return valid[0];
    var totalWeight = Mathf.Max(1f, valid.Sum(d => d.Weight));
    var random = Random.Range(0f, totalWeight);
    foreach (var item in valid)
    {
      random -= item.Weight;
      if (random <= 0f) return item;
    }
    return null;
  }
  private static string GetEnvironment(Heightmap.Biome biome)
  {
    var em = EnvMan.instance;
    var availableEnvironments = em.GetAvailableEnvironments(biome);
    if (availableEnvironments == null || availableEnvironments.Count == 0) return "";
    Random.State state = Random.state;
    var num = (long)ZNet.instance.GetTimeSeconds() / em.m_environmentDuration;
    Random.InitState((int)num);
    var env = em.SelectWeightedEnvironment(availableEnvironments);
    Random.state = state;
    return env.m_name.ToLower();
  }
  public static void CreateObject(Spawn spawn, Vector3 pos, Quaternion rot, ZDO originalZdo, ZDOData? data)
  {
    pos += rot * spawn.Pos;
    rot *= spawn.Rot;
    data = ZDOData.Merge(data, ZDOData.Create(spawn.Data));
    CreateObject(spawn.Prefab, pos, rot, originalZdo, data);
  }
  public static void CreateObject(int prefab, Vector3 pos, Quaternion rot, ZDO originalZdo, ZDOData? data)
  {
    if (prefab == 0) return;
    // Prefab hash must be zero to avoid triggering HandleCreated.
    var zdo = ZDOMan.instance.CreateNewZDO(pos, 0);
    zdo.Persistent = originalZdo.Persistent;
    zdo.Type = originalZdo.Type;
    zdo.Distant = originalZdo.Distant;
    zdo.m_prefab = prefab;
    zdo.m_rotation = rot.eulerAngles;
    // Some client should always be the owner so that creatures are initialized correctly (for example max health from stars).
    // Things work slightly better when the server doesn't have ownership (for example max health from stars).

    // For client spawns, the original owner can be just used.
    var owner = originalZdo.GetOwner();
    if (ZNet.instance.IsDedicated() && owner == ZDOMan.instance.m_sessionID)
    {
      // But if the server spawns, the owner must be handled manually.
      var closestClient = ZDOMan.instance.m_peers.OrderBy(p => Utils.DistanceXZ(p.m_peer.m_refPos, pos)).FirstOrDefault(p => p.m_peer.m_uid != owner);
      owner = closestClient?.m_peer.m_uid ?? 0;
    }
    zdo.SetOwnerInternal(owner);
    data?.Write(zdo);
  }

  public static void CreateObject(Spawn spawn, ZDO originalZdo, ZDOData? data) => CreateObject(spawn, originalZdo.m_position, originalZdo.GetRotation(), originalZdo, data);
}
