
using System;
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
  public static Info? Select(ActionType type, ZDO zdo, string name, string parameter) => Select(InfoManager.Select(type), zdo, name, parameter);
  private static Info? Select(PrefabInfo infos, ZDO zdo, string name, string parameter)
  {
    var prefab = zdo.m_prefab;
    var pos = zdo.m_position;
    if (!infos.TryGetValue(prefab, out var data)) return null;
    if (data.Count == 0) return null;
    var biome = WorldGenerator.instance.GetBiome(pos);
    var distance = Utils.DistanceXZ(pos, Vector3.zero);
    var day = EnvMan.instance.IsDay();
    var parameters = parameter.Split(' ');
    var linq = data
      .Where(d => CheckParameters(d, parameters))
      .Where(d => (d.Biomes & biome) == biome)
      .Where(d => d.Day || !day)
      .Where(d => d.Night || day)
      .Where(d => distance >= d.MinDistance)
      .Where(d => distance < d.MaxDistance)
      .Where(d => pos.y >= d.MinY)
      .Where(d => pos.y < d.MaxY)
      .Where(d => Helper.HasEveryGlobalKey(d.GlobalKeys))
      .Where(d => !Helper.HasAnyGlobalKey(d.BannedGlobalKeys));
    // Minor optimization to resolve simpler checks first (not measured).
    linq = linq.ToArray();
    var checkEnvironments = linq.Any(d => d.Environments.Count > 0) || linq.Any(d => d.BannedEnvironments.Count > 0);
    var checkEvents = linq.Any(d => d.Events.Count > 0);
    var checkObjects = linq.Any(d => d.Objects.Length > 0);
    var checkBannedObjects = linq.Any(d => d.BannedObjects.Length > 0);
    var checkLocations = linq.Any(d => d.Locations.Count > 0);
    var checkFilters = linq.Any(d => d.Filters.Length > 0);
    var checkBannedFilters = linq.Any(d => d.BannedFilters.Length > 0);
    if (checkEnvironments)
    {
      var environment = GetEnvironment(biome);
      linq = linq
        .Where(d => d.Environments.Count == 0 || d.Environments.Contains(environment))
        .Where(d => !d.BannedEnvironments.Contains(environment)).ToArray();
    }
    if (checkEvents)
    {
      var ev = EWP.GetCurrentEvent(pos);
      linq = linq.Where(d => d.Events.Count == 0 || (ev != null && (d.Events.Contains(ev.m_name) || d.Events.Contains("all")) && d.EventDistance >= Utils.DistanceXZ(pos, ev.m_pos))).ToArray();
    }
    if (checkObjects)
    {
      linq = linq.Where(d =>
      {
        if (d.Objects.Length == 0) return true;
        return HasObjectsNearby(d.ObjectsLimit, d.Objects, zdo, name, parameter);
      }).ToArray();
    }
    if (checkBannedObjects)
    {
      linq = linq.Where(d =>
      {
        if (d.BannedObjects.Length == 0) return true;
        return !HasObjectsNearby(d.BannedObjectsLimit, d.BannedObjects, zdo, name, parameter);
      }).ToArray();
    }
    if (checkLocations)
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
    if (checkFilters)
    {
      linq = linq.Where(d => d.Filters.All(f => f.Valid(zdo))).ToArray();
    }
    if (checkBannedFilters)
    {
      linq = linq.Where(d => d.BannedFilters.All(f => !f.Valid(zdo))).ToArray();
    }
    var valid = linq.ToArray();
    if (valid.Length == 0) return null;
    if (valid.Length == 1 && valid[0].Weight >= 1f) return valid[0];
    var totalWeight = Mathf.Max(1f, valid.Sum(d => d.Weight));
    var random = UnityEngine.Random.Range(0f, totalWeight);
    foreach (var item in valid)
    {
      random -= item.Weight;
      if (random <= 0f) return item;
    }
    return null;
  }
  private static bool CheckParameters(Info info, string[] parameters)
  {
    if (info.Parameters.Length == 0) return true;
    if (info.Parameters.Length > parameters.Length) return false;
    for (int i = 0; i < info.Parameters.Length; i++)
      if (!Helper2.CheckWild(info.Parameters[i], parameters[i])) return false;
    return true;

  }
  private static bool HasAllObjectsNearby(Object[] objects, ZDO zdo, string name, string parameter)
  {
    var pos = zdo.m_position;
    var zdos = ZDOMan.instance.m_objectsByID.Values;
    return objects.All(o => zdos.Any(z => o.IsValid(z, pos, name, parameter) && z != zdo));
  }
  private static bool HasObjectsNearby(Range<int>? limit, Object[] objects, ZDO zdo, string name, string parameter)
  {
    if (limit == null) return HasAllObjectsNearby(objects, zdo, name, parameter);
    var pos = zdo.m_position;
    var counter = 0;
    var useMax = limit.Max > 0;
    foreach (var z in ZDOMan.instance.m_objectsByID.Values)
    {
      var valid = objects.FirstOrDefault(o => o.IsValid(z, pos, name, parameter) && z != zdo);
      if (valid == null) continue;
      counter += valid.Weight;
      if (useMax && limit.Max < counter) return false;
      if (limit.Min <= counter && !useMax) return true;
    }
    return limit.Min <= counter && counter <= limit.Max;
  }

  private static string GetEnvironment(Heightmap.Biome biome)
  {
    var em = EnvMan.instance;
    var availableEnvironments = em.GetAvailableEnvironments(biome);
    if (availableEnvironments == null || availableEnvironments.Count == 0) return "";
    UnityEngine.Random.State state = UnityEngine.Random.state;
    var num = (long)ZNet.instance.GetTimeSeconds() / em.m_environmentDuration;
    UnityEngine.Random.InitState((int)num);
    var env = em.SelectWeightedEnvironment(availableEnvironments);
    UnityEngine.Random.state = state;
    return env.m_name.ToLower();
  }
  public static void CreateObject(Spawn spawn, string name, string parameter, ZDO originalZdo, ZDOData? data)
  {
    var pos = originalZdo.m_position;
    var rot = originalZdo.GetRotation();
    pos += rot * spawn.Pos;
    rot *= spawn.Rot;
    data = ZDOData.Merge(data, ZDOData.Create(spawn.Data));
    CreateObject(spawn.GetPrefab(name, parameter), pos, rot, originalZdo, data);
  }
  public static void CreateObject(ZDO originalZdo, ZDOData? data) => CreateObject(originalZdo.m_prefab, originalZdo.m_position, originalZdo.GetRotation(), originalZdo, data);
  public static void CreateObject(int prefab, Vector3 pos, Quaternion rot, ZDO originalZdo, ZDOData? data)
  {
    if (prefab == 0) return;
    var obj = ZNetScene.instance.GetPrefab(prefab);
    if (!obj || !obj.TryGetComponent<ZNetView>(out var view))
    {
      EWP.LogError($"Can't spawn missing prefab: {prefab}");
      return;
    }
    // Prefab hash must be zero to avoid triggering HandleCreated.
    var zdo = ZDOMan.instance.CreateNewZDO(pos, 0);
    zdo.Persistent = view.m_persistent;
    zdo.Type = view.m_type;
    zdo.Distant = view.m_distant;
    zdo.m_prefab = prefab;
    zdo.m_rotation = rot.eulerAngles;
    // Some client should always be the owner so that creatures are initialized correctly (for example max health from stars).
    // Things work slightly better when the server doesn't have ownership (for example max health from stars).

    // For client spawns, the original owner can be just used.
    var owner = originalZdo.GetOwner();
    if (!ZNetView.m_ghostInit && ZNet.instance.IsDedicated() && owner == ZDOMan.instance.m_sessionID && !ZNetView.m_ghostInit)
    {
      // But if the server spawns, the owner must be handled manually.
      // Unless ghost init, because those are meant to be unloaded.
      var closestClient = ZDOMan.instance.m_peers.OrderBy(p => Utils.DistanceXZ(p.m_peer.m_refPos, pos)).FirstOrDefault(p => p.m_peer.m_uid != owner);
      owner = closestClient?.m_peer.m_uid ?? 0;
    }
    zdo.SetOwnerInternal(owner);
    data?.Write(zdo);
  }

  public static void RunCommands(Info info, ZDO zdo, string name, string parameter)
  {
    if (info.Commands.Length == 0) return;
    var pos = zdo.m_position;
    var rot = zdo.m_rotation;
    var players = FindPlayers(zdo, info);
    var commands = info.Commands.Select(s => Helper2.ReplaceParameters(s, name, parameter)).ToArray();
    if (info.PlayerSearch == PlayerSearch.None)
      CommandManager.Run(commands, pos, rot, players.Length == 0 ? null : players[0]);
    else
      CommandManager.Run(commands, pos, rot, players);
  }
  private static PlayerInfo[] FindPlayers(ZDO zdo, Info info)
  {
    var players = ZNet.instance.GetPeers().Select(p => new PlayerInfo(p)).ToList();
    if (ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
      players.Add(new(Player.m_localPlayer));
    if (info.PlayerSearch == PlayerSearch.None)
      return players.Where(p => p.ZDOID == zdo.m_uid).ToArray();

    var pos = zdo.m_position;
    var rot = zdo.m_rotation;
    players = players.Where(p =>
      Utils.DistanceXZ(p.Pos, pos) <= info.PlayerSearchDistance
      && (info.PlayerSearchHeight == 0f || Math.Abs(p.Pos.y - pos.y) <= info.PlayerSearchHeight
    )).ToList();
    if (info.PlayerSearch == PlayerSearch.All)
      return players.ToArray();
    if (info.PlayerSearch == PlayerSearch.Closest)
    {
      var player = players.OrderBy(p => Utils.DistanceXZ(p.Pos, pos)).FirstOrDefault();
      return player == null ? [] : [player];
    }
    return [];
  }
}
