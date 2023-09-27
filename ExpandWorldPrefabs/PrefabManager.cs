

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

[HarmonyPatch(typeof(ZDOMan))]
public class Manager
{

  private static Info? Select(int prefab, Vector3 pos)
  {
    if (!Loading.PrefabDatas.TryGetValue(prefab, out var data)) return null;
    if (data.Count == 0) return null;
    var biome = WorldGenerator.instance.GetBiome(pos);
    var environment = GetEnvironment(biome);
    var distance = Utils.DistanceXZ(pos, Vector3.zero);
    var altitude = WorldGenerator.instance.GetHeight(pos.x, pos.z) - WorldInfo.WaterLevel;
    var valid = data
      .Where(d => (d.Biomes & biome) == biome)
      .Where(d => distance >= d.MinDistance)
      .Where(d => distance < d.MaxDistance)
      .Where(d => altitude >= d.MinAltitude)
      .Where(d => altitude < d.MaxAltitude)
      .Where(d => d.Environments.Count == 0 || d.Environments.Contains(environment))
      .Where(d => !d.BannedEnvironments.Contains(environment))
      .Where(d => Helper.HasEveryGlobalKey(d.GlobalKeys))
      .Where(d => !Helper.HasAnyGlobalKey(d.BannedGlobalKeys))
      .ToArray();
    if (valid.Length == 1) return valid[0];
    var totalWeight = valid.Sum(d => d.Weight);
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
  public static ZDO ApplyFromClient(ZDO zdo, int prefab, Vector3 pos)
  {
    var info = Select(prefab, pos);
    if (info == null) return zdo;
    if (info.Data != "" || info.Swap != 0)
      return ApplyRefresh(zdo, prefab, pos, info);
    return zdo;
  }
  public static ZDO ApplyServer(ZDO zdo, int prefab, Vector3 pos)
  {
    var info = Select(prefab, pos);
    if (info == null) return zdo;
    if (info.Swap != 0)
      return ApplyRefresh(zdo, prefab, pos, info);

    if (info.Command != "")
      CommandManager.Run([info.Command], pos, zdo.GetRotation().eulerAngles);
    var zdoData = ZDOData.Create(info.Data);
    if (zdoData == null) return zdo;
    zdoData.Write(zdo);
    zdo.DataRevision += 1;
    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    return zdo;
  }
  private static ZDO ApplyRefresh(ZDO zdo, int prefab, Vector3 pos, Info info)
  {
    if (info.Command != "")
      CommandManager.Run([info.Command], pos, zdo.GetRotation().eulerAngles);
    ZDOData data = new("", zdo);
    var zdoData = ZDOData.Create(info.Data);
    if (zdoData != null)
      data.Add(zdoData);

    var zdo2 = ZDOMan.instance.CreateNewZDO(pos, 0);
    zdo2.Persistent = zdo.Persistent;
    zdo2.Type = zdo.Type;
    zdo2.Distant = zdo.Distant;
    zdo2.SetPrefab(info.Swap == 0 ? prefab : info.Swap);
    zdo2.SetRotation(zdo.GetRotation());
    data.Write(zdo2);

    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    ZDOMan.instance.DestroyZDO(zdo);
    return zdo2;
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
    if (flag)
      return ApplyFromClient(zdo, zdo.m_prefab, zdo.m_position);
    return zdo;
  }

}
