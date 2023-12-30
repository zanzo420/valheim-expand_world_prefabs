using System;
using System.Collections.Generic;
using System.Linq;
using ExpandWorldData;

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
  public static readonly PrefabInfo StateDatas = new();
  public static readonly PrefabInfo CommandDatas = new();
  public static readonly PrefabInfo SayDatas = new();

  public static void Clear()
  {
    CreateDatas.Clear();
    RemoveDatas.Clear();
    RepairDatas.Clear();
    DamageDatas.Clear();
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
  public static void Patch()
  {
    EWP.Harmony.UnpatchSelf();
    EWP.Harmony.PatchAll();
    if (CreateDatas.Exists)
      HandleCreated.Patch(EWP.Harmony);
    if (RemoveDatas.Exists)
      HandleDestroyed.Patch(EWP.Harmony);
    if (RepairDatas.Exists || DamageDatas.Exists || StateDatas.Exists || CommandDatas.Exists || SayDatas.Exists)
      HandleRPC.Patch(EWP.Harmony);
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

  private static Dictionary<string, int> PrefabCache = [];
  public static IEnumerable<int> GetPrefabs(string prefab)
  {
    var p = prefab.ToLowerInvariant();
    if (PrefabCache.Count == 0)
      PrefabCache = ZNetScene.instance.m_namedPrefabs.ToDictionary(pair => pair.Value.name, pair => pair.Key);
    if (p == "*")
      return PrefabCache.Values;
    if (p[0] == '*' && p[p.Length - 1] == '*')
    {
      p = p.Substring(1, p.Length - 2);
      return PrefabCache.Where(pair => pair.Key.ToLowerInvariant().Contains(p)).Select(pair => pair.Value);
    }
    if (p[0] == '*')
    {
      p = p.Substring(1);
      return PrefabCache.Where(pair => pair.Key.EndsWith(p, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Value);
    }
    if (p[p.Length - 1] == '*')
    {
      p = p.Substring(0, p.Length - 1);
      return PrefabCache.Where(pair => pair.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Value);
    }
    return [];
  }
}

public class PrefabInfo
{
  private static readonly int CreatureHash = "creature".GetStableHashCode();
  public readonly Dictionary<int, List<Info>> Prefabs = [];
  public readonly List<Info> Creatures = [];
  public bool Exists => Prefabs.Count > 0 || Creatures.Count > 0;


  public void Clear()
  {
    Prefabs.Clear();
    Creatures.Clear();
  }
  public void Add(Info info)
  {
    var prefabs = DataManager.ToList(info.Prefabs);
    HashSet<int> hashes = [];
    foreach (var prefab in prefabs)
    {
      if (prefab.Contains("*"))
        hashes.UnionWith(InfoManager.GetPrefabs(prefab));
      else
        hashes.Add(prefab.GetStableHashCode());
    }
    foreach (var hash in hashes)
    {
      if (hash == CreatureHash)
        Creatures.Add(info);
      else
      {
        if (!Prefabs.TryGetValue(hash, out var list))
          Prefabs[hash] = list = [];
        list.Add(info);
      }
    }
  }

  public bool TryGetValue(int prefab, out List<Info> list)
  {
    var ret = Prefabs.TryGetValue(prefab, out list);
    var creatures = GetCreaturePrefabs(prefab);
    if (!ret && creatures == null)
      return false;
    if (ret)
    {
      if (creatures != null)
        list = [.. list, .. creatures];
    }
    else if (creatures != null)
      list = creatures;
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
}
