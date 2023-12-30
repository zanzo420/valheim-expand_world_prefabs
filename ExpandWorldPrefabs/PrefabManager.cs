using System.Linq;
using Service;
using UnityEngine;

namespace ExpandWorld.Prefab;

public class Manager
{

  public static void Handle(ActionType type, string parameter, ZDO zdo, ZDO? source = null)
  {
    // Already destroyed before.
    if (ZDOMan.instance.m_deadZDOs.ContainsKey(zdo.m_uid)) return;
    if (!ZNet.instance.IsServer()) return;
    var name = ZNetScene.instance.GetPrefab(zdo.m_prefab)?.name ?? "";
    var info = InfoSelector.Select(type, zdo, name, parameter, source);
    if (info == null) return;
    Commands.Run(info, zdo, name, parameter, source);
    HandleSpawns(info, zdo, name, parameter);
    Poke(info, zdo, name, parameter);
    if (info.Drops)
      SpawnDrops(zdo);
    // Original object was regenerated to apply data.
    if (info.Remove || info.Data != "")
      RemoveZDO(zdo);
  }
  private static void HandleSpawns(Info info, ZDO zdo, string name, string parameter)
  {
    // Original object must be regenerated to apply data.
    var regenerateOriginal = !info.Remove && info.Data != "";
    if (info.Spawns.Length == 0 && info.Swaps.Length == 0 && !regenerateOriginal) return;

    var customData = ZDOData.Create(info.Data);
    foreach (var p in info.Spawns)
      CreateObject(p, name, parameter, zdo, customData);

    if (info.Swaps.Length == 0 && !regenerateOriginal) return;
    var data = ZDOData.Merge(new("", zdo), customData);

    foreach (var p in info.Swaps)
      CreateObject(p, name, parameter, zdo, data);
    if (regenerateOriginal)
      CreateObject(zdo, data);
  }
  private static void RemoveZDO(ZDO zdo)
  {
    ZDOMan.instance.m_deadZDOs[zdo.m_uid] = ZNet.instance.GetTime().Ticks;
    zdo.SetOwner(ZDOMan.instance.m_sessionID);
    ZDOMan.instance.DestroyZDO(zdo);
  }
  public static void CreateObject(Spawn spawn, string name, string parameter, ZDO originalZdo, ZDOData? data)
  {
    var pos = originalZdo.m_position;
    var rot = originalZdo.GetRotation();
    pos += rot * spawn.Pos;
    rot *= spawn.Rot;
    data = ZDOData.Merge(data, ZDOData.Create(spawn.Data));
    DelayedSpawn.Add(spawn.Delay, pos, rot, spawn.GetPrefab(name, parameter), originalZdo.GetOwner(), data);
  }
  public static void CreateObject(ZDO originalZdo, ZDOData? data) => CreateObject(originalZdo.m_prefab, originalZdo.m_position, originalZdo.GetRotation(), originalZdo.GetOwner(), data);
  public static void CreateObject(int prefab, Vector3 pos, Quaternion rot, long owner, ZDOData? data)
  {
    if (prefab == 0) return;
    var obj = ZNetScene.instance.GetPrefab(prefab);
    if (!obj || !obj.TryGetComponent<ZNetView>(out var view))
    {
      EWP.LogError($"Can't spawn missing prefab: {prefab}");
      return;
    }
    // Prefab hash must be zero to avoid triggering HandleCreated.
    var zdo = ZDOMan.instance.CreateNewZDO(pos, 0);
    zdo.Persistent = view.m_persistent;
    zdo.Type = view.m_type;
    zdo.Distant = view.m_distant;
    zdo.m_prefab = prefab;
    zdo.m_rotation = rot.eulerAngles;
    // Some client should always be the owner so that creatures are initialized correctly (for example max health from stars).
    // Things work slightly better when the server doesn't have ownership (for example max health from stars).

    // For client spawns, the original owner can be just used.
    if (!ZNetView.m_ghostInit && ZNet.instance.IsDedicated() && owner == ZDOMan.instance.m_sessionID && !ZNetView.m_ghostInit)
    {
      // But if the server spawns, the owner must be handled manually.
      // Unless ghost init, because those are meant to be unloaded.
      var closestClient = ZDOMan.instance.m_peers.OrderBy(p => Utils.DistanceXZ(p.m_peer.m_refPos, pos)).FirstOrDefault(p => p.m_peer.m_uid != owner);
      owner = closestClient?.m_peer.m_uid ?? 0;
    }
    zdo.SetOwnerInternal(owner);
    data?.Write(zdo);
  }

  public static void SpawnDrops(ZDO zdo)
  {
    var obj = ZNetScene.instance.CreateObject(zdo);
    obj.GetComponent<ZNetView>().m_ghost = true;
    ZNetScene.instance.m_instances.Remove(zdo);
    HandleCreated.Skip = true;
    if (obj.TryGetComponent<DropOnDestroyed>(out var drop))
      drop.OnDestroyed();
    if (obj.TryGetComponent<CharacterDrop>(out var characterDrop))
    {
      characterDrop.m_character = obj.GetComponent<Character>();
      if (characterDrop.m_character)
        characterDrop.OnDeath();
    }
    if (obj.TryGetComponent<Piece>(out var piece))
      piece.DropResources();
    HandleCreated.Skip = false;
    UnityEngine.Object.Destroy(obj);
  }

  public static void Poke(Info info, ZDO zdo, string name, string parameter)
  {
    var zdos = ObjectsFiltering.GetNearby(info.PokeLimit, info.Pokes, zdo, name, parameter);
    foreach (var z in zdos)
      Handle(ActionType.Poke, info.PokeParameter, z);
  }
}
