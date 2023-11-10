

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

[HarmonyPatch(typeof(ZDOMan))]
public class Manager
{
  private static Info? SelectDestroy(ZDO zdo) => Select(Loading.RemoveDatas, zdo, zdo.GetPrefab(), zdo.GetPosition());
  private static Info? SelectCreate(ZDO zdo, int prefab, Vector3 pos) => Select(Loading.CreateDatas, zdo, prefab, pos);
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
    var random = UnityEngine.Random.Range(0f, totalWeight);
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
    UnityEngine.Random.State state = UnityEngine.Random.state;
    var num = (long)ZNet.instance.GetTimeSeconds() / em.m_environmentDuration;
    UnityEngine.Random.InitState((int)num);
    var env = em.SelectWeightedEnvironment(availableEnvironments);
    UnityEngine.Random.state = state;
    return env.m_name.ToLower();
  }

  // Most data is probably for LoadFields that requires refreshing the object.
  // Technically could apply the data less intrusively, but not necessary at the moment.
  public static ZDO ApplyFromClient(ZDO zdo, int prefab, Vector3 pos) => Apply(zdo, prefab, pos);
  public static ZDO ApplyServer(ZDO zdo, int prefab, Vector3 pos)
  {
    // Triggered on ZNetView awake, so the zdo can be reused.
    // If swapped to nothing, remove object.
    // Update zdo fields, then refreesh the object (if swapped).
    var zdo2 = Apply(zdo, prefab, pos);
    return zdo2;
  }
  private static ZDO Apply(ZDO zdo, int prefab, Vector3 pos)
  {
    var info = SelectCreate(zdo, prefab, pos);
    if (info == null) return zdo;
    if (info.Command != "")
      CommandManager.Run([info.Command], pos, zdo.GetRotation().eulerAngles);
    if (info.Data != "" || info.Swaps.Count > 0)
      return ApplyRefresh(zdo, prefab, pos, info);
    return zdo;
  }
  private static ZDO ApplyRefresh(ZDO zdo, int prefab, Vector3 pos, Info info)
  {
    var linq = info.Swaps.Count == 0 ? [prefab] : info.Swaps.SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value));
    var newPrefabs = linq.Where(p => ZNetScene.instance.GetPrefab(p)).ToArray();
    // Allows swapping to nothing.
    if (newPrefabs.Length == 0)
      return RemoveZDO(zdo);

    ZDOData data = new("", zdo);
    var zdoData = ZDOData.Create(info.Data);
    if (zdoData != null) data.Add(zdoData);

    ZDO zdo2 = null!;
    foreach (var p in newPrefabs)
    {
      // Prefab hash must be zero to avoid infinite recursion.
      zdo2 = ZDOMan.instance.CreateNewZDO(pos, 0);
      zdo2.Persistent = zdo.Persistent;
      zdo2.Type = zdo.Type;
      zdo2.Distant = zdo.Distant;
      zdo2.SetPrefab(p);
      zdo2.SetRotation(zdo.GetRotation());
      // Things work slightly better when the server doesn't have ownership (for example max health from stars).
      zdo2.SetOwner(zdo.GetOwner());
      data.Write(zdo2);
    }

    RemoveZDO(zdo);
    return zdo2;
  }

  private static ZDO RemoveZDO(ZDO zdo)
  {
    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    ZDOMan.instance.DestroyZDO(zdo);
    return zdo;
  }
  private static GameObject Refresh(ZDO zdo)
  {
    var obj = ZNetScene.instance.m_instances[zdo].gameObject;
    if (!obj) return obj;
    UnityEngine.Object.Destroy(obj);
    var newObj = ZNetScene.instance.CreateObject(zdo);
    ZNetScene.instance.m_instances[zdo] = newObj.GetComponent<ZNetView>();
    return newObj;
  }



  [HarmonyPatch(nameof(ZDOMan.CreateNewZDO), typeof(ZDOID), typeof(Vector3), typeof(int)), HarmonyPostfix]
  static ZDO CreateNewZDO(ZDO result, Vector3 position, int prefabHashIn)
  {
    if (prefabHashIn != 0)
      return ApplyServer(result, prefabHashIn, position);
    return result;
  }
  [HarmonyPatch(nameof(ZDOMan.RPC_ZDOData)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> RPC_ZDOData(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZDO), nameof(ZDO.Deserialize))))
      .Advance(2)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 12))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 13))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(LoadData).operand))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Stloc_S, 12))
      .InstructionEnumeration();
  }
  static ZDO LoadData(ZDO zdo, bool flag)
  {
    if (flag && Configuration.ServerOnly && ZNet.instance.IsServer())
      return ApplyFromClient(zdo, zdo.m_prefab, zdo.m_position);
    return zdo;
  }

  // Handling destroy is simple because existing object don't have be edited.
  // Server side only is also enough for this.
  [HarmonyPatch(nameof(ZDOMan.HandleDestroyedZDO)), HarmonyPrefix]
  static void HandleDestroyedZDO(ZDOID uid)
  {
    if (!ZNet.instance.IsServer()) return;
    var destroyedZdo = ZDOMan.instance.GetZDO(uid);
    if (destroyedZdo == null) return;
    var pos = destroyedZdo.GetPosition();
    var rot = destroyedZdo.GetRotation();
    var info = SelectDestroy(destroyedZdo);
    if (info == null) return;
    if (info.Command != "")
      CommandManager.Run([info.Command], pos, rot.eulerAngles);
    if (info.Swaps.Count == 0) return;
    var prefabs = info.Swaps.SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value)).Where(p => ZNetScene.instance.GetPrefab(p)).ToArray();

    var data = ZDOData.Create(info.Data);

    foreach (var p in prefabs)
    {
      // Prefab hash must be zero to avoid infinite recursion.
      var zdo = ZDOMan.instance.CreateNewZDO(pos, 0);
      zdo.Persistent = destroyedZdo.Persistent;
      zdo.Type = destroyedZdo.Type;
      zdo.Distant = destroyedZdo.Distant;
      zdo.SetPrefab(p);
      zdo.SetRotation(rot);
      // Things work slightly better when the server doesn't have ownership (for example max health from stars).
      zdo.SetOwner(destroyedZdo.GetOwner());
      data?.Write(zdo);
    }
  }
}
