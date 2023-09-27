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
  [DefaultValue("")]
  public string biomes = "";
  [DefaultValue(0f)]
  public float minDistance = 0f;
  [DefaultValue(1f)]
  public float maxDistance = 1f;
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
}


public class Info
{
  public int Prefab = 0;
  public float Weight = 1f;
  public int Swap = 0;
  public string Data = "";
  public string Command = "";
  public float MinDistance = 0f;
  public float MaxDistance = 0f;
  public float MinAltitude = 0f;
  public float MaxAltitude = 0f;
  public Heightmap.Biome Biomes = Heightmap.Biome.None;
  public HashSet<string> Environments = [];
  public HashSet<string> BannedEnvironments = [];
  public List<string> GlobalKeys = [];
  public List<string> BannedGlobalKeys = [];
}


