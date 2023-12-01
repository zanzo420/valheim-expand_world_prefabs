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
    var name = ZNetScene.instance.GetPrefab(zdo.m_prefab)?.name ?? "";
    var info = Manager.Select(ActionType.Destroy, zdo, name, "");
    if (info == null) return;

    Manager.RunCommands(info, zdo, name, "");
    CreateObjects(info, zdo, name);
  }
  static void CreateObjects(Info info, ZDO zdo, string name)
  {
    if (info.Spawns.Length == 0 && info.Swaps.Length == 0) return;
    var customData = ZDOData.Create(info.Data);
    foreach (var p in info.Spawns)
      Manager.CreateObject(p, name, "", zdo, customData);

    if (info.Swaps.Length == 0) return;
    var data = ZDOData.Merge(new("", zdo), customData);

    foreach (var p in info.Swaps)
      Manager.CreateObject(p, name, "", zdo, data);
  }
}
