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

  private static void Load(string yaml)
  {
    InfoManager.Clear();
    if (Helper.IsClient()) return;

    var data = ParseYaml(yaml);
    if (data.Count == 0)
    {
      EWP.LogWarning($"Failed to load any prefab data.");
      return;
    }
    EWP.LogInfo($"Reloading prefab ({data.Count} entries).");
    foreach (var item in data)
    {
      InfoManager.Add(item);
    }
  }

  public static void FromSetting()
  {
    //if (Helper.IsClient()) Load(EWP.valuePrefabData.Value);
  }
  public static void FromFile()
  {
    if (Helper.IsClient()) return;
    if (!File.Exists(FilePath))
    {
      var yaml = DataManager.Serializer().Serialize(new Data[]{new(){
        prefab = "Example",
        type = "Create",
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
      //EWP.valuePrefabData.Value = yaml;
    }
  }
  private static List<Info> ParseYaml(string yaml)
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
    var prefabs = DataManager.ToList(data.prefab);
    return prefabs.Select(prefab =>
    {
      var swaps = ParseSpawns(data.swaps ?? (data.swap == null ? [] : [data.swap]));
      var spawns = ParseSpawns(data.spawns ?? (data.spawn == null ? [] : [data.spawn]));
      var playerSearch = DataManager.ToList(data.playerSearch).ToArray();
      var types = DataManager.ToList(data.type);
      if (types.Count == 0 || !Enum.TryParse(types[0], true, out ActionType type))
      {
        if (data.type == "")
          EWP.LogError($"Missing type for prefab {prefab}.");
        else
          EWP.LogError($"Failed to parse type {data.type}.");
        type = ActionType.Create;
      }
      return new Info()
      {
        Prefab = prefab,
        Type = type,
        Parameter = types.Count > 1 ? types[1] : "",
        Remove = data.remove || swaps.Length > 0,
        Spawns = [.. spawns],
        Swaps = [.. swaps],
        Data = data.data,
        Commands = data.commands ?? (data.command == null ? [] : [data.command]),
        PlayerSearch = playerSearch.Length > 0 && Enum.TryParse(playerSearch[0], true, out PlayerSearch mode) ? mode : PlayerSearch.None,
        PlayerSearchDistance = Parse.Float(playerSearch, 1, 0f),
        PlayerSearchHeight = Parse.Float(playerSearch, 2, 0f),
        Weight = data.weight,
        Day = data.day,
        Night = data.night,
        MinDistance = data.minDistance * WorldInfo.Radius,
        MaxDistance = data.maxDistance * WorldInfo.Radius,
        MinY = data.minY ?? data.minAltitude - WorldInfo.WaterLevel,
        MaxY = data.maxY ?? data.maxAltitude - WorldInfo.WaterLevel,
        Biomes = DataManager.ToBiomes(data.biomes),
        Environments = [.. DataManager.ToList(data.environments).Select(s => s.ToLower())],
        BannedEnvironments = [.. DataManager.ToList(data.bannedEnvironments).Select(s => s.ToLower())],
        GlobalKeys = DataManager.ToList(data.globalKeys),
        BannedGlobalKeys = DataManager.ToList(data.bannedGlobalKeys),
        Events = [.. DataManager.ToList(data.events)],
        EventDistance = data.eventDistance,
        LocationDistance = data.locationDistance,
        Locations = [.. DataManager.ToList(data.locations).Select(s => s.GetStableHashCode())],
        ObjectsLimit = data.objectsLimit == "" ?
          null : int.TryParse(data.objectsLimit, out var limit) ?
            new Range<int>(limit, 0) : Parse.IntRange(data.objectsLimit),
        Objects = ParseObjects(data.objects ?? []),
        BannedObjects = ParseObjects(data.bannedObjects ?? []),
        BannedObjectsLimit = data.bannedObjectsLimit == "" ?
          null : int.TryParse(data.bannedObjectsLimit, out limit) ?
            new Range<int>(limit, 0) : Parse.IntRange(data.bannedObjectsLimit),
        Filters = ParseFilters(data.filters ?? (data.filter == null ? [] : [data.filter])),
        BannedFilters = ParseFilters(data.bannedFilters ?? (data.bannedFilter == null ? [] : [data.bannedFilter])),
      };
    }).ToArray();
  }
  private static Spawn[] ParseSpawns(string[] spawns) => spawns.Select(s => new Spawn(s)).ToArray();

  private static Filter[] ParseFilters(string[] filters) => filters.Select(Filter.Create).Where(s => s != null).ToArray();
  private static Object[] ParseObjects(string[] objects) => objects.Select(s => new Object(s)).ToArray();

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