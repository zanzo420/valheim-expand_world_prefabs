using System.Reflection;
using HarmonyLib;


namespace ExpandWorld.Prefab;


//[HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.HandleRoutedRPC))]
public class HandleRepaired
{
  static readonly int RepairHash = "WNTHealthChanged".GetStableHashCode();
  static readonly ParameterInfo[] RepairPars = AccessTools.Method(typeof(WearNTear), nameof(WearNTear.RPC_HealthChanged)).GetParameters();
  static void Postfix(ZRoutedRpc.RoutedRPCData data)
  {
    if (data.m_methodHash != RepairHash) return;
    var zdo = ZDOMan.instance.GetZDO(data.m_targetZDO);
    if (zdo == null) return;
    var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
    if (!prefab) return;
    if (!prefab.TryGetComponent(out WearNTear wearNTear)) return;
    data.m_parameters.SetPos(0);
    var pars = ZNetView.Deserialize(data.m_senderPeerID, RepairPars, data.m_parameters);
    if (pars.Length < 2) return;
    var health = (float)pars[1];
    if (health > 1E20) return;
    var type = health == wearNTear.m_health ? ActionType.Repair : ActionType.Damage;
    HandleCreated.Handle(type, zdo);
  }

}