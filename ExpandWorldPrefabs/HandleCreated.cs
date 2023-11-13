
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ExpandWorldData;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;


[HarmonyPatch(typeof(ZDOMan))]
public class HandleCreated
{
  [HarmonyPatch(nameof(ZDOMan.CreateNewZDO), typeof(ZDOID), typeof(Vector3), typeof(int)), HarmonyPostfix]
  static void HandleOwnCreated(ZDO __result, Vector3 position, int prefabHashIn)
  {
    if (prefabHashIn != 0 && ZNet.instance.IsServer())
      Handle(__result, prefabHashIn, position);
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
    if (flag && ZNet.instance.IsServer())
      Handle(zdo, zdo.m_prefab, zdo.m_position);
  }

  private static void Handle(ZDO zdo, int prefab, Vector3 pos)
  {
    var info = Manager.SelectCreate(zdo, prefab, pos);
    if (info == null) return;
    if (info.Commands.Length > 0)
      CommandManager.Run(info.Commands, pos, zdo.GetRotation().eulerAngles);
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
    ZDOData originalData = new("", zdo);
    if (customData != null) originalData.Add(customData);

    foreach (var p in info.Swaps)
      Manager.CreateObject(p, pos, zdo.GetRotation(), zdo, originalData);
    if (regenerateOriginal)
      Manager.CreateObject(prefab, pos, zdo.GetRotation(), zdo, originalData);
  }
  private static ZDO RemoveZDO(ZDO zdo)
  {
    HandleDestroyed.Ignored.Add(zdo.m_uid);
    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    ZDOMan.instance.DestroyZDO(zdo);
    return zdo;
  }
  private static GameObject Refresh(ZDO zdo)
  {
    var obj = ZNetScene.instance.m_instances[zdo].gameObject;
    if (!obj) return obj;
    Object.Destroy(obj);
    var newObj = ZNetScene.instance.CreateObject(zdo);
    ZNetScene.instance.m_instances[zdo] = newObj.GetComponent<ZNetView>();
    return newObj;
  }
}
