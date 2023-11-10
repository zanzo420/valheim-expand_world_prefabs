using System.Collections.Generic;
using System.ComponentModel;

namespace ExpandWorld.Prefab;

public class Data
{
  public string prefab = "";
  [DefaultValue(1f)]
  public float weight = 1f;
  [DefaultValue("")]
  public string swap = "";
  [DefaultValue("")]
  public string data = "";
  [DefaultValue("")]
  public string command = "";
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
}


public class Info
{
  public int Prefab = 0;
  public float Weight = 1f;
  public Dictionary<int, int> Swaps = [];
  public string Data = "";
  public string Command = "";
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
}


