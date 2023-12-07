using System;
using System.Collections.Generic;
using System.ComponentModel;
using ExpandWorldData;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

public class Data
{
  public string prefab = "";
  public string type = "";
  [DefaultValue(null)]
  public string[]? types = null;
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
  [DefaultValue("")]
  public string playerSearch = "";
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
  [DefaultValue(null)]
  public float? minY = null;
  [DefaultValue(null)]
  public float? maxY = null;
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
  [DefaultValue(null)]
  public float? eventDistance = null;
  [DefaultValue("")]
  public string[]? objects = null;
  [DefaultValue("")]
  public string objectsLimit = "";
  [DefaultValue("")]
  public string[]? bannedObjects = null;
  [DefaultValue("")]
  public string bannedObjectsLimit = "";
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
  public string Prefabs = "";
  public ActionType Type = ActionType.Create;
  public string[] Parameters = [];
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
  public float MinY = 0f;
  public float MaxY = 0f;
  public Heightmap.Biome Biomes = Heightmap.Biome.None;
  public float EventDistance = 0f;
  public HashSet<string> Events = [];
  public HashSet<string> Environments = [];
  public HashSet<string> BannedEnvironments = [];
  public List<string> GlobalKeys = [];
  public List<string> BannedGlobalKeys = [];
  public Range<int>? ObjectsLimit = null;
  public Object[] Objects = [];
  public Range<int>? BannedObjectsLimit = null;
  public Object[] BannedObjects = [];
  public HashSet<int> Locations = [];
  public float LocationDistance = 0f;
  public Filter[] Filters = [];
  public Filter[] BannedFilters = [];
  public PlayerSearch PlayerSearch = PlayerSearch.None;
  public float PlayerSearchDistance = 0f;
  public float PlayerSearchHeight = 0f;
}
public class Spawn
{
  public int Prefab = 0;
  public string WildPrefab = "";
  public Vector3 Pos = Vector3.zero;
  public Quaternion Rot = Quaternion.identity;
  public string Data = "";
  public Spawn(string line)
  {
    var split = DataManager.ToList(line);
    if (split[0].Contains("<") && split[0].Contains(">"))
      WildPrefab = split[0];
    else
    {
      Prefab = split[0].GetStableHashCode();
      Prefab = ZNetScene.instance.GetPrefab(Prefab) ? Prefab : 0;
    }
    if (split.Count > 3)
    {
      if (Parse.TryFloat(split[1], out var x))
        Pos = new Vector3(x, Parse.Float(split[3]), Parse.Float(split[2]));
      else
        Data = split[1];
    }
    if (split.Count > 6)
    {
      if (Parse.TryFloat(split[4], out var x))
        Rot = Quaternion.Euler(Parse.Float(split[5]), x, Parse.Float(split[6]));
      else
        Data = split[4];
    }
    if (split.Count > 7)
      Data = split[7];
  }
  public int GetPrefab(string name, string parameter)
  {
    if (Prefab != 0) return Prefab;
    var prefabName = Helper2.ReplaceParameters(WildPrefab, name, parameter);
    var prefab = prefabName.GetStableHashCode();
    return ZNetScene.instance.GetPrefab(prefab) ? prefab : 0;
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
    if (type == "hash") return new IntFilter() { Key = key, MinValue = value.GetStableHashCode(), MaxValue = value.GetStableHashCode() };
    if (type == "string") return new StringFilter() { Key = key, Value = value };
    if (type == "bool") return new BoolFilter() { Key = key, Value = value == "true" };
    var range = Parse.StringRange(value);
    if (type == "int") return new IntFilter() { Key = key, MinValue = Parse.Int(range.Min), MaxValue = Parse.Int(range.Max) };
    if (type == "float") return new FloatFilter() { Key = key, MinValue = Parse.Float(range.Min), MaxValue = Parse.Float(range.Max) };
    EWP.LogError($"Invalid filter type: {type}");
    return null!;
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
public enum PlayerSearch
{
  None,
  All,
  Closest
}
public class Object
{
  public int Prefab = 0;
  public string WildPrefab = "";
  public float MinDistance = 0f;
  public float MaxDistance = 100f;
  public int Data = 0;
  public int Weight = 1;
  public Object(string line)
  {
    var split = DataManager.ToList(line);
    if (split[0].Contains("<") && split[0].Contains(">"))
      WildPrefab = split[0];
    else
    {
      Prefab = split[0].ToLowerInvariant() == "all" ? 0 : split[0].GetStableHashCode();
      if (Prefab != 0 && !ZNetScene.instance.GetPrefab(Prefab))
      {
        EWP.LogError($"Invalid object filter prefab: {split[0]}");
        Prefab = 0;
      }
    }
    if (split.Count > 1)
    {
      var range = Parse.FloatRange(split[1]);
      MinDistance = range.Min == range.Max ? 0f : range.Min;
      MaxDistance = range.Max;
    }
    if (split.Count > 2)
    {
      Data = split[2].GetStableHashCode();
      if (!ZDOData.Cache.ContainsKey(Data))
      {
        EWP.LogError($"Invalid object filter data: {split[2]}");
        Data = 0;
      }
    }
    if (split.Count > 3)
    {
      Weight = Parse.Int(split[3]);
    }
  }
  public bool IsValid(ZDO zdo, Vector3 pos, string name, string parameter)
  {
    if (Prefab != 0 && zdo.GetPrefab() != Prefab) return false;
    if (WildPrefab != "")
    {
      var prefabName = Helper2.ReplaceParameters(WildPrefab, name, parameter);
      var hash = prefabName.GetStableHashCode();
      if (zdo.GetPrefab() != hash) return false;
    }
    if (MinDistance > 0f && Utils.DistanceXZ(pos, zdo.GetPosition()) < MinDistance) return false;
    if (Utils.DistanceXZ(pos, zdo.GetPosition()) > MaxDistance) return false;
    if (Data == 0) return true;
    return ZDOData.Cache.TryGetValue(Data, out var d) && d.Match(zdo);
  }
}

public class InfoType
{
  public readonly ActionType Type;
  public readonly string[] Parameters;
  public InfoType(string prefab, string line)
  {
    var types = DataManager.ToList(line);
    if (types.Count == 0 || !Enum.TryParse(types[0], true, out Type))
    {
      if (line == "")
        EWP.LogWarning($"Missing type for prefab {prefab}.");
      else
        EWP.LogError($"Failed to parse type {prefab}.");
      Type = ActionType.Create;
    }
    Parameters = types.Count > 1 ? types[1].Split(' ') : [];
  }
}