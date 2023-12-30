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
  Say,
  Poke
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
  public static readonly PrefabInfo PokeDatas = new();

  public static void Clear()
  {
    CreateDatas.Clear();
    RemoveDatas.Clear();
    RepairDatas.Clear();
    DamageDatas.Clear();
    StateDatas.Clear();
    CommandDatas.Clear();
    SayDatas.Clear();
    PokeDatas.Clear();
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
    ActionType.Poke => PokeDatas,
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
  public static readonly int CreatureHash = "creature".GetStableHashCode();
}

public class PrefabInfo
{
  public readonly Dictionary<int, List<Info>> Prefabs = [];
  public readonly List<Info> Creatures = [];
  public readonly List<Info> All = [];
  public bool Exists => Prefabs.Count > 0 || Creatures.Count > 0 || All.Count > 0;


  public void Clear()
  {
    Prefabs.Clear();
    Creatures.Clear();
    All.Clear();
  }
  public void Add(Info info)
  {
    var prefabs = DataManager.ToList(info.Prefabs);
    HashSet<int> hashes = [];
    foreach (var prefab in prefabs)
    {
      if (prefab == "all")
        All.Add(info);
      else if (prefab.Contains("*"))
        hashes.UnionWith(InfoManager.GetPrefabs(prefab));
      else
        hashes.Add(prefab.GetStableHashCode());
    }
    foreach (var hash in hashes)
    {
      if (hash == InfoManager.CreatureHash)
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
    list = All;
    var ret = Prefabs.TryGetValue(prefab, out var prefabs);
    if (ret)
      list = list.Count == 0 ? prefabs : [.. list, .. prefabs];
    var creatures = GetCreaturePrefabs(prefab);
    if (creatures != null)
      list = list.Count == 0 ? creatures : [.. list, .. creatures];
    return list.Count > 0;
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
