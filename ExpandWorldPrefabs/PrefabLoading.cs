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

  public static readonly Dictionary<int, List<Info>> PrefabDatas = [];

  public static void Initialize()
  {
    Load();
  }
  public static void Load()
  {
    PrefabDatas.Clear();
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

    var data = FromFile();
    if (data.Count == 0)
    {
      EWD.Log.LogWarning($"Failed to load any prefab data.");
      return;
    }
    EWD.Log.LogInfo($"Reloading prefab ({data.Count} entries).");
    foreach (var item in data)
    {
      if (!PrefabDatas.TryGetValue(item.Prefab, out var list))
        PrefabDatas[item.Prefab] = list = [];
      list.Add(item);
    }
  }
  ///<summary>Loads all yaml files returning the deserialized vegetation entries.</summary>
  private static List<Info> FromFile()
  {
    try
    {
      var yaml = DataManager.Read(Pattern);
      return DataManager.Deserialize<Data>(yaml, FileName).Select(FromData).ToList();
    }
    catch (Exception e)
    {
      EWD.Log.LogError(e.Message);
      EWD.Log.LogError(e.StackTrace);
    }
    return [];
  }

  public static Info FromData(Data data)
  {
    return new()
    {
      Prefab = data.prefab.GetStableHashCode(),
      Swap = data.swap == "" ? 0 : data.swap.GetStableHashCode(),
      Data = data.data,
      Command = data.command,
      Weight = data.weight,
      MinDistance = data.minDistance * WorldInfo.Radius,
      MaxDistance = data.maxDistance * WorldInfo.Radius,
      MinAltitude = data.minAltitude,
      MaxAltitude = data.maxAltitude,
      Biomes = DataManager.ToBiomes(data.biomes),
      Environments = [.. DataManager.ToList(data.environments).Select(s => s.ToLower())],
      BannedEnvironments = [.. DataManager.ToList(data.bannedEnvironments).Select(s => s.ToLower())],
      GlobalKeys = DataManager.ToList(data.globalKeys),
      BannedGlobalKeys = DataManager.ToList(data.bannedGlobalKeys),
    };
  }

  public static void SetupWatcher()
  {
    DataManager.SetupWatcher(Pattern, Load);
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start)), HarmonyPriority(Priority.VeryLow)]
public class InitializeContent
{
  static void Postfix()
  {
    if (Helper.IsServer())
      Loading.Initialize();
  }
}