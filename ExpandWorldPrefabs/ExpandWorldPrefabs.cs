﻿using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ServerSync;
namespace ExpandWorld;
[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("expand_world_data", BepInDependency.DependencyFlags.HardDependency)]
public class EWP : BaseUnityPlugin
{
  public const string GUID = "expand_world_prefabs";
  public const string NAME = "Expand World Prefabs";
  public const string VERSION = "1.3";
#nullable disable
  public static ManualLogSource Log;
  public static CustomSyncedValue<string> valuePrefabData;
#nullable enable
  public static void LogWarning(string message) => Log.LogWarning(message);
  public static void LogError(string message) => Log.LogError(message);
  public static void LogInfo(string message) => Log.LogInfo(message);
  public static ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    ModRequired = true,
    IsLocked = true
  };
  public static Assembly? ExpandEvents;
  public void Awake()
  {
    Log = Logger;
    new Harmony(GUID).PatchAll();
    valuePrefabData = new CustomSyncedValue<string>(ConfigSync, "prefab_data");
    valuePrefabData.ValueChanged += Prefab.Loading.FromSetting;
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
  public void Start()
  {
    if (Chainloader.PluginInfos.TryGetValue("expand_world_events", out var plugin))
    {
      ExpandEvents = plugin.Instance.GetType().Assembly;
    }
  }

  public static RandomEvent GetCurrentEvent(Vector3 pos)
  {
    if (ExpandEvents == null) return RandEventSystem.instance.GetCurrentRandomEvent();
    var method = ExpandEvents.GetType("ExpandWorld.EWE").GetMethod("GetCurrentRandomEvent", BindingFlags.Public | BindingFlags.Static);
    if (method == null) return RandEventSystem.instance.GetCurrentRandomEvent();
    return (RandomEvent)method.Invoke(null, [pos]);
  }
}