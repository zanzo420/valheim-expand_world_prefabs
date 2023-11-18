using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ExpandWorldData;
using Service;
using UnityEngine;
using Valheim.UI;

namespace ExpandWorld.Prefab;

public class Data
{
  public string prefab = "";
  [DefaultValue("")]
  public string type = "";
  [DefaultValue(1f)]
  public float weight = 1f;
  [DefaultValue(null)]
  public string? swap = null;
  [DefaultValue(null)]
  public string[]? swaps = null;
  [DefaultValue(null)]
  public string? spawn = null;
  [DefaultValue(null)]
  public string[]? spawns = null;
  [DefaultValue(false)]
  public bool remove = false;
  [DefaultValue("")]
  public string data = "";
  [DefaultValue(null)]
  public string? command = null;
  [DefaultValue(null)]
  public string[]? commands = null;
  [DefaultValue(true)]
  public bool day = true;
  [DefaultValue(true)]
  public bool night = true;
  [DefaultValue("")]
  public string biomes = "";
  [DefaultValue(0f)]
  public float minDistance = 0f;
  [DefaultValue(1000f)]
  public float maxDistance = 1000f;
  [DefaultValue(-10000f)]
  public float minAltitude = -10000f;
  [DefaultValue(10000f)]
  public float maxAltitude = 10000f;
  [DefaultValue("")]
  public string environments = "";
  [DefaultValue("")]
  public string bannedEnvironments = "";
  [DefaultValue("")]
  public string globalKeys = "";
  [DefaultValue("")]
  public string bannedGlobalKeys = "";
  [DefaultValue("")]
  public string events = "";
  [DefaultValue(100f)]
  public float eventDistance = 100f;
  [DefaultValue("")]
  public string objects = "";
  [DefaultValue(100f)]
  public float objectDistance = 100f;
  [DefaultValue("")]
  public string locations = "";
  [DefaultValue(0f)]
  public float locationDistance = 0f;
  [DefaultValue(null)]
  public string? filter = null;
  [DefaultValue(null)]
  public string[]? filters = null;
  [DefaultValue(null)]
  public string? bannedFilter = null;
  [DefaultValue(null)]
  public string[]? bannedFilters = null;
}


public class Info
{
  public int Prefab = 0;
  public string Type = "";
  public float Weight = 1f;
  public Spawn[] Swaps = [];
  public Spawn[] Spawns = [];
  public bool Remove = false;
  public string Data = "";
  public string[] Commands = [];
  public bool Day = true;
  public bool Night = true;
  public float MinDistance = 0f;
  public float MaxDistance = 0f;
  public float MinAltitude = 0f;
  public float MaxAltitude = 0f;
  public Heightmap.Biome Biomes = Heightmap.Biome.None;
  public float EventDistance = 0f;
  public HashSet<string> Events = [];
  public HashSet<string> Environments = [];
  public HashSet<string> BannedEnvironments = [];
  public List<string> GlobalKeys = [];
  public List<string> BannedGlobalKeys = [];
  public HashSet<int> Objects = [];
  public float ObjectDistance = 0f;
  public HashSet<int> Locations = [];
  public float LocationDistance = 0f;
  public Filter[] Filters = [];
  public Filter[] BannedFilters = [];
}
public class Spawn
{
  public int Prefab = 0;
  public Vector3 Pos = Vector3.zero;
  public Quaternion Rot = Quaternion.identity;
  public string Data = "";
  public Spawn(string line)
  {
    var split = DataManager.ToList(line);
    Prefab = split[0].GetStableHashCode();
    Prefab = ZNetScene.instance.GetPrefab(Prefab) ? Prefab : 0;
    if (split.Count > 3)
      Pos = new Vector3(Parse.Float(split[1]), Parse.Float(split[3]), Parse.Float(split[2]));
    if (split.Count > 6)
      Rot = Quaternion.Euler(Parse.Float(split[5]), Parse.Float(split[4]), Parse.Float(split[6]));
    if (split.Count > 7)
      Data = split[7];
  }
}
public abstract class Filter
{
  public int Key;
  public abstract bool Valid(ZDO zdo);

  public static Filter Create(string filter)
  {
    var split = DataManager.ToList(filter);
    if (split.Count < 3)
    {
      EWP.LogError($"Invalid filter: {filter}");
      return null!;
    }
    var type = split[0].ToLowerInvariant();
    var key = split[1].GetStableHashCode();
    var value = split[2];
    if (type == "string") return new StringFilter() { Key = key, Value = value };
    if (type == "bool") return new BoolFilter() { Key = key, Value = value == "true" };
    var range = Range(value);
    if (type == "int") return new IntFilter() { Key = key, MinValue = Parse.Int(range.Min), MaxValue = Parse.Int(range.Max) };
    if (type == "float") return new FloatFilter() { Key = key, MinValue = Parse.Float(range.Min), MaxValue = Parse.Float(range.Max) };
    EWP.LogError($"Invalid filter type: {type}");
    return null!;
  }
  private static Range<string> Range(string arg)
  {
    var range = arg.Split('-').ToList();
    if (range.Count > 1 && range[0] == "")
    {
      range[0] = "-" + range[1];
      range.RemoveAt(1);
    }
    if (range.Count > 2 && range[1] == "")
    {
      range[1] = "-" + range[2];
      range.RemoveAt(2);
    }
    if (range.Count == 1) return new(range[0]);
    else return new(range[0], range[1]);

  }
}
public class IntFilter : Filter
{
  public int MinValue;
  public int MaxValue;
  public override bool Valid(ZDO zdo)
  {
    var value = zdo.GetInt(Key);
    return MinValue <= value && value <= MaxValue;
  }
}
public class BoolFilter : Filter
{
  public bool Value;
  public override bool Valid(ZDO zdo) => zdo.GetBool(Key) == Value;
}
public class FloatFilter : Filter
{
  public float MinValue;
  public float MaxValue;
  public override bool Valid(ZDO zdo)
  {
    var value = zdo.GetFloat(Key);
    return MinValue <= value && value <= MaxValue;
  }
}
public class StringFilter : Filter
{
  public string Value = "";
  public override bool Valid(ZDO zdo) => zdo.GetString(Key) == Value;
}