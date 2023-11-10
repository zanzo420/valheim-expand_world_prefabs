using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpandWorldData;
using HarmonyLib;
namespace ExpandWorld.Prefab;

public class Loading
{
  private static readonly string FileName = "expand_prefabs.yaml";
  private static readonly string FilePath = Path.Combine(EWD.YamlDirectory, FileName);
  private static readonly string Pattern = "expand_prefabs*.yaml";

  public static readonly Dictionary<int, List<Info>> CreateDatas = [];
  public static readonly Dictionary<int, List<Info>> RemoveDatas = [];

  private static void Load(string yaml)
  {
    CreateDatas.Clear();
    RemoveDatas.Clear();
    if (Helper.IsClient()) return;


    var data = Parse(yaml);
    if (data.Count == 0)
    {
      EWP.LogWarning($"Failed to load any prefab data.");
      return;
    }
    EWP.LogInfo($"Reloading prefab ({data.Count} entries).");
    foreach (var item in data)
    {
      if (item.Type == "destroy")
      {
        if (!RemoveDatas.TryGetValue(item.Prefab, out var list))
          RemoveDatas[item.Prefab] = list = [];
        list.Add(item);
        continue;
      }
      else
      {
        if (!CreateDatas.TryGetValue(item.Prefab, out var list))
          CreateDatas[item.Prefab] = list = [];
        list.Add(item);
      }
    }
  }

  public static void FromSetting()
  {
    if (Helper.IsClient()) Load(EWP.valuePrefabData.Value);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    if (!File.Exists(FilePath))
    {
      var yaml = DataManager.Serializer().Serialize(new Data[]{new(){
        prefab = "Example",
        swap = "Surtling",
        biomes = "Meadows",
      }});
      File.WriteAllText(FilePath, yaml);
      // Watcher triggers reload.
      return;
    }
    else
    {
      var yaml = DataManager.Read(Pattern);
      Load(yaml);
      EWP.valuePrefabData.Value = yaml;
    }
  }
  private static List<Info> Parse(string yaml)
  {
    try
    {
      return DataManager.Deserialize<Data>(yaml, FileName).SelectMany(FromData).ToList();
    }
    catch (Exception e)
    {
      EWP.LogError(e.Message);
      EWP.LogError(e.StackTrace);
    }
    return [];
  }
  private static Info[] FromData(Data data)
  {
    var prefabs = DataManager.ToList(data.prefab).Select(s => s.GetStableHashCode());
    return prefabs.Select(s =>
      new Info()
      {
        Prefab = s,
        Type = data.type,
        Swaps = ParseSwaps(data.swap),
        Data = data.data,
        Command = data.command,
        Weight = data.weight,
        Day = data.day,
        Night = data.night,
        MinDistance = data.minDistance * WorldInfo.Radius,
        MaxDistance = data.maxDistance * WorldInfo.Radius,
        MinAltitude = data.minAltitude,
        MaxAltitude = data.maxAltitude,
        Biomes = DataManager.ToBiomes(data.biomes),
        Environments = [.. DataManager.ToList(data.environments).Select(s => s.ToLower())],
        BannedEnvironments = [.. DataManager.ToList(data.bannedEnvironments).Select(s => s.ToLower())],
        GlobalKeys = DataManager.ToList(data.globalKeys),
        BannedGlobalKeys = DataManager.ToList(data.bannedGlobalKeys),
        Events = [.. DataManager.ToList(data.events)],
        EventDistance = data.eventDistance,
        LocationDistance = data.locationDistance,
        Locations = [.. DataManager.ToList(data.locations).Select(s => s.GetStableHashCode())],
        ObjectDistance = data.objectDistance,
        Objects = [.. DataManager.ToList(data.objects).Select(s => s.GetStableHashCode())],
        Filters = data.filters == null ? [] : ParseFilters(data.filters),
        BannedFilters = data.bannedFilters == null ? [] : ParseFilters(data.bannedFilters),
      }).ToArray();
  }
  private static Dictionary<int, int> ParseSwaps(string swap) =>
    DataManager.ToList(swap).Select(s => s.Split(':')).ToDictionary(
      s => s[0].GetStableHashCode(),
      s => ExpandWorldData.Parse.Int(s, 1, 1)
    );

  private static Filter[] ParseFilters(string[] filters) => filters.Select(s => Filter.Create(s)).Where(s => s != null).ToArray();

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, FromFile);
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeContent
{
  static void Postfix()
  {
    if (Helper.IsServer())
      Loading.FromFile();
  }
}