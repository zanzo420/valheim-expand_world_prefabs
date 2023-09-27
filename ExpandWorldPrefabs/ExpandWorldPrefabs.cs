using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
namespace ExpandWorld;
[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("expand_world_data", BepInDependency.DependencyFlags.HardDependency)]
public class EWP : BaseUnityPlugin
{
  public const string GUID = "expand_world_prefabs";
  public const string NAME = "Expand World Prefabs";
  public const string VERSION = "1.0";
#nullable disable
  public static ManualLogSource Log;
#nullable enable
  public static void LogWarning(string message) => Log.LogWarning(message);
  public static void LogError(string message) => Log.LogError(message);
  public static void LogInfo(string message) => Log.LogInfo(message);
  public void Awake()
  {
    Log = Logger;
    new Harmony(GUID).PatchAll();
    try
    {
      if (ExpandWorldData.Configuration.DataReload)
      {
        Prefab.Loading.SetupWatcher();
      }
    }
    catch (Exception e)
    {
      Log.LogError(e);
    }
  }
}
