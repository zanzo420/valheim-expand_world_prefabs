
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
using Service;

namespace ExpandWorld.Prefab;

// Handling destroy is simple because single player and dedicated server work the same.
// The existing object also doesn't need to be edited so only have to support new objects and commands.
// This can always be done on the server.
[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO))]
public class HandleDestroyed
{
  static void Prefix(ZDOID uid)
  {
    // Already destroyed before.
    if (ZDOMan.instance.m_deadZDOs.ContainsKey(uid)) return;
    if (!ZNet.instance.IsServer()) return;
    var zdo = ZDOMan.instance.GetZDO(uid);
    if (zdo == null) return;
    var info = Manager.SelectDestroy(zdo);
    if (info == null) return;

    Manager.RunCommands(info, zdo.m_position, zdo.m_rotation);
    CreateObjects(zdo, info);
  }
  static void CreateObjects(ZDO zdo, Info info)
  {
    if (info.Spawns.Length == 0 && info.Swaps.Length == 0) return;
    var customData = ZDOData.Create(info.Data);
    foreach (var p in info.Spawns)
      Manager.CreateObject(p, zdo, customData);

    if (info.Swaps.Length == 0) return;
    var data = ZDOData.Merge(new("", zdo), customData);

    foreach (var p in info.Swaps)
      Manager.CreateObject(p, zdo, data);
  }
}
