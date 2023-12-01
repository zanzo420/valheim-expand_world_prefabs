using System.Collections.Generic;
using System.Linq;

namespace ExpandWorld.Prefab;

public enum ActionType
{
  Create,
  Destroy,
  Repair,
  Damage,
  State,
  Command,
  Say
}
public class InfoManager
{
  public static readonly PrefabInfo CreateDatas = new();
  public static readonly PrefabInfo RemoveDatas = new();
  public static readonly PrefabInfo RepairDatas = new();
  public static readonly PrefabInfo DamageDatas = new();
  public static readonly PrefabInfo TargetDatas = new();
  public static readonly PrefabInfo StateDatas = new();
  public static readonly PrefabInfo CommandDatas = new();
  public static readonly PrefabInfo SayDatas = new();

  public static void Clear()
  {
    CreateDatas.Clear();
    RemoveDatas.Clear();
    RepairDatas.Clear();
    DamageDatas.Clear();
    TargetDatas.Clear();
    StateDatas.Clear();
    CommandDatas.Clear();
    SayDatas.Clear();
  }
  public static void Add(Info info)
  {
    Select(info.Type).Add(info);
    if (info.Type == ActionType.Say)
      Select(ActionType.Command).Add(info);
  }


  public static PrefabInfo Select(ActionType type) => type switch
  {
    ActionType.Destroy => RemoveDatas,
    ActionType.Repair => RepairDatas,
    ActionType.Damage => DamageDatas,
    ActionType.State => StateDatas,
    ActionType.Command => CommandDatas,
    ActionType.Say => SayDatas,
    _ => CreateDatas,
  };

  private static readonly Dictionary<int, bool> IsCreatureCache = [];
  public static bool IsCreature(int prefab)
  {
    if (IsCreatureCache.TryGetValue(prefab, out var result))
      return result;
    return IsCreatureCache[prefab] = ZNetScene.instance.GetPrefab(prefab)?.GetComponent<BaseAI>() != null;

  }
}

public class PrefabInfo
{
  private static readonly int CreatureHash = "creature".GetStableHashCode();
  public readonly Dictionary<int, List<Info>> Prefabs = [];
  public readonly List<Info> WildPrefabs = [];
  public readonly List<Info> Creatures = [];


  public void Clear()
  {
    Prefabs.Clear();
    Creatures.Clear();
    WildPrefabs.Clear();
  }
  public void Add(Info info)
  {
    if (info.Prefab.Contains("*"))
    {
      WildPrefabs.Add(info);
      return;
    }
    var hash = info.Prefab.GetStableHashCode();
    if (hash == CreatureHash)
      Creatures.Add(info);
    else
    {
      if (!Prefabs.TryGetValue(hash, out var list))
        Prefabs[hash] = list = [];
      list.Add(info);
    }
  }

  public bool TryGetValue(int prefab, out List<Info> list)
  {
    var ret = Prefabs.TryGetValue(prefab, out list);
    var wild = GetWildPrefabs(prefab);
    var creatures = GetCreaturePrefabs(prefab);
    if (!ret && wild == null && creatures == null)
      return false;
    if (ret)
    {
      if (wild != null && creatures != null)
        list = [.. list, .. wild, .. creatures];
      else if (wild != null)
        list = [.. list, .. wild];
      else if (creatures != null)
        list = [.. list, .. creatures];
      return true;
    }
    if (creatures != null && wild != null)
      list = [.. wild, .. creatures];
    else if (creatures != null)
      list = creatures;
    else if (wild != null)
      list = wild;
    return true;
  }
  private List<Info>? GetCreaturePrefabs(int prefab)
  {
    if (Creatures.Count == 0)
      return null;
    var isCreature = InfoManager.IsCreature(prefab);
    if (!isCreature)
      return null;
    return Creatures;
  }
  private List<Info>? GetWildPrefabs(int prefab)
  {
    if (WildPrefabs.Count == 0) return null;
    var obj = ZNetScene.instance.GetPrefab(prefab);
    if (!obj) return null;
    var name = obj.name;
    var ret = WildPrefabs.Where(info => Helper2.CheckWild(info.Prefab, name)).ToList();
    return ret.Count == 0 ? null : ret;
  }

}