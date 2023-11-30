
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;


[HarmonyPatch(typeof(ZDOMan))]
public class HandleCreated
{
  private static readonly List<ZDOID> CreatedZDOs = [];
  private static readonly List<ZDOID> GhostZDOs = [];
  public static void Execute()
  {
    foreach (var uid in CreatedZDOs)
    {
      var zdo = ZDOMan.instance.GetZDO(uid);
      if (zdo == null) continue;
      Handle(ActionType.Create, "", zdo);
    }
    ZNetView.m_ghostInit = true;
    foreach (var uid in GhostZDOs)
    {
      var zdo = ZDOMan.instance.GetZDO(uid);
      if (zdo == null) continue;
      Handle(ActionType.Create, "", zdo);
    }
    ZNetView.m_ghostInit = false;
    CreatedZDOs.Clear();
    GhostZDOs.Clear();
  }
  [HarmonyPatch(nameof(ZDOMan.CreateNewZDO), typeof(Vector3), typeof(int)), HarmonyPostfix]
  static void HandleOwnCreated(ZDO __result, int prefabHash)
  {
    if (prefabHash == 0) return;
    if (ZNetView.m_ghostInit)
      GhostZDOs.Add(__result.m_uid);
    else
      CreatedZDOs.Add(__result.m_uid);
  }
  [HarmonyPatch(nameof(ZDOMan.RPC_ZDOData)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> RPC_ZDOData(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZDO), nameof(ZDO.Deserialize))))
      .Advance(2)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 12))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 13))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(HandleClientCreated).operand))
      .InstructionEnumeration();
  }
  static void HandleClientCreated(ZDO zdo, bool flag)
  {
    if (flag)
      CreatedZDOs.Add(zdo.m_uid);
  }

  public static void Handle(ActionType type, string parameter, ZDO zdo)
  {
    var prefab = zdo.m_prefab;
    var pos = zdo.m_position;
    // Already destroyed before.
    if (ZDOMan.instance.m_deadZDOs.ContainsKey(zdo.m_uid)) return;
    if (!ZNet.instance.IsServer()) return;
    var info = Manager.Select(type, zdo, parameter);
    if (info == null) return;
    Manager.RunCommands(info, pos, zdo.m_rotation, parameter);
    HandleSpawns(zdo, prefab, pos, info);
    // Original object was regenerated to apply data.
    if (info.Remove || info.Data != "")
      RemoveZDO(zdo);
  }
  private static void HandleSpawns(ZDO zdo, int prefab, Vector3 pos, Info info)
  {
    // Original object must be regenerated to apply data.
    var regenerateOriginal = !info.Remove && info.Data != "";
    if (info.Spawns.Length == 0 && info.Swaps.Length == 0 && !regenerateOriginal) return;

    var customData = ZDOData.Create(info.Data);
    foreach (var p in info.Spawns)
      Manager.CreateObject(p, pos, zdo.GetRotation(), zdo, customData);

    if (info.Swaps.Length == 0 && !regenerateOriginal) return;
    var data = ZDOData.Merge(new("", zdo), customData);

    foreach (var p in info.Swaps)
      Manager.CreateObject(p, pos, zdo.GetRotation(), zdo, data);
    if (regenerateOriginal)
      Manager.CreateObject(prefab, pos, zdo.GetRotation(), zdo, data);
  }
  private static void RemoveZDO(ZDO zdo)
  {
    ZDOMan.instance.m_deadZDOs[zdo.m_uid] = ZNet.instance.GetTime().Ticks;
    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    ZDOMan.instance.DestroyZDO(zdo);
  }
}
