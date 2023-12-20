using System.Collections.Generic;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

public class DelayedSpawn(float delay, Vector3 pos, Quaternion rot, int prefab, long originalOwner, ZDOData? data)
{
  private static readonly List<DelayedSpawn> Spawns = [];
  public static void Add(float delay, Vector3 pos, Quaternion rot, int prefab, long originalOwner, ZDOData? data)
  {
    if (delay <= 0f)
    {
      Manager.CreateObject(prefab, pos, rot, originalOwner, data);
      return;
    }
    Spawns.Add(new(delay, pos, rot, prefab, originalOwner, data));
  }
  public static void Execute(float dt)
  {
    // Two loops to preserve order.
    foreach (var spawn in Spawns)
    {
      spawn.Delay -= dt;
      if (spawn.Delay > -0.001) continue;
      spawn.Execute();
    }
    for (var i = Spawns.Count - 1; i >= 0; i--)
    {
      if (Spawns[i].Delay > -0.001) continue;
      Spawns.RemoveAt(i);
    }
  }
  private readonly Vector3 Pos = pos;
  private readonly Quaternion Rot = rot;
  private readonly int Prefab = prefab;
  private readonly long OriginalOwner = originalOwner;
  private readonly ZDOData? Data = data;
  public float Delay = delay;

  public void Execute()
  {
    Manager.CreateObject(Prefab, Pos, Rot, OriginalOwner, Data);
  }
}