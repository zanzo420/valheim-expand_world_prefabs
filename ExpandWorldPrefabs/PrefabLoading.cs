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
    InfoManager.Patch();
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
    var swaps = ParseSpawns(data.swaps ?? (data.swap == null ? [] : [data.swap]), data.delay);
    var spawns = ParseSpawns(data.spawns ?? (data.spawn == null ? [] : [data.spawn]), data.delay);
    var playerSearch = DataManager.ToList(data.playerSearch).ToArray();
    var types = (data.types ?? [data.type]).Select(s => new InfoType(data.prefab, s)).ToArray();
    HashSet<string> events = [.. DataManager.ToList(data.events)];
    var commands = ParseCommands(data.commands ?? (data.command == null ? [] : [data.command]));
    HashSet<string> environments = [.. DataManager.ToList(data.environments).Select(s => s.ToLower())];
    HashSet<string> bannedEnvironments = [.. DataManager.ToList(data.bannedEnvironments).Select(s => s.ToLower())];
    HashSet<int> locations = [.. DataManager.ToList(data.locations).Select(s => s.GetStableHashCode())];
    var objectsLimit = ParseObjectsLimit(data.objectsLimit);
    var objects = ParseObjects(data.objects ?? []);
    var bannedObjects = ParseObjects(data.bannedObjects ?? []);
    var bannedObjectsLimit = ParseObjectsLimit(data.bannedObjectsLimit);
    var filters = ParseFilters(data.filters ?? (data.filter == null ? [] : [data.filter]));
    var bannedFilters = ParseFilters(data.bannedFilters ?? (data.bannedFilter == null ? [] : [data.bannedFilter]));
    var pokes = ParseObjects(data.pokes ?? []);
    return types.Select(t =>
    {
      return new Info()
      {
        Prefabs = data.prefab,
        Type = t.Type,
        Parameters = t.Parameters,
        Remove = t.Type != ActionType.Destroy && (data.remove || swaps.Length > 0),
        Drops = t.Type != ActionType.Destroy && data.drops,
        Spawns = [.. spawns],
        Swaps = [.. swaps],
        Data = t.Type != ActionType.Destroy ? data.data : "",
        Commands = commands,
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
        Environments = environments,
        BannedEnvironments = bannedEnvironments,
        GlobalKeys = DataManager.ToList(data.globalKeys),
        BannedGlobalKeys = DataManager.ToList(data.bannedGlobalKeys),
        Events = events,
        // Distance can be set without events for any event.
        // However if event is set, there must be a distance (the default value).
        // Zero distance means no check at all.
        EventDistance = data.eventDistance ?? (events.Count > 0 ? 100f : 0f),
        LocationDistance = data.locationDistance,
        Locations = locations,
        PokeLimit = data.pokeLimit,
        PokeParameter = data.pokeParameter,
        Pokes = pokes,
        ObjectsLimit = objectsLimit,
        Objects = objects,
        BannedObjects = bannedObjects,
        BannedObjectsLimit = bannedObjectsLimit,
        Filters = filters,
        BannedFilters = bannedFilters,
      };
    }).ToArray();
  }
  private static string[] ParseCommands(string[] commands) => commands.Select(s =>
  {
    if (s.Contains("$$"))
    {
      EWP.LogWarning($"Command \"{s}\" contains $$ which is obsolete. Use {"<>"} instead.");
      return s.Replace("$$x", "<x>").Replace("$$y", "<y>").Replace("$$z", "<z>").Replace("$$a", "<a>").Replace("$$i", "<i>").Replace("$$j", "<j>");
    }
    if (s.Contains("{") && s.Contains("}"))
    {
      EWP.LogWarning($"Command \"{s}\" contains {{}} which is obsolete. Use {"<>"} instead.");
      return s.Replace("{", "<").Replace("}", ">");
    }
    return s;
  }).ToArray();

  private static Spawn[] ParseSpawns(string[] spawns, float delay) => spawns.Select(s => new Spawn(s, delay)).ToArray();

  private static Filter[] ParseFilters(string[] filters) => filters.Select(Filter.Create).Where(s => s != null).ToArray();
  private static Object[] ParseObjects(string[] objects) => objects.Select(s => new Object(s)).ToArray();
  private static Range<int>? ParseObjectsLimit(string str) => str == "" ?
    null : int.TryParse(str, out var limit) ?
      new Range<int>(limit, 0) : Parse.IntRange(str);

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