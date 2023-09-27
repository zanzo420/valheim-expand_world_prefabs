# Expand World Prefabs

Allows configuring and replacing spawned prefabs.

Install on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

Install [Expand World Data](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Data/).

## Features

- Modify or swap spawned creatures.
- Modify or swap built structures.
- Modify or swap other objects.

Note: When swapping creature spawns, the spawn limit still checks the amount of original creature. This can lead to very high amount of creatures.

## Configuration

The file `expand_world/expand_prefabs.yaml` is created when loading a world.

### expand_prefabs.yaml

- prefab: Id of the affected object.
- weight (default: `1`): Chance to be selected if multiple entries match.
  - All weights are summed and the probability is `weight / sum`.
- swap: Swapped id.
- data: Name of the data entry (from `expand_data.yaml`) or data code.
- command: Console command to run.
- biomes: List of valid biomes.
- minDistance (default: `0`): Minimum distance from the world center.
- maxDistance (default: `1`): Maximum distance from the world center.
- minAltitude (default: `-10000`): Minimum altitude.
- maxAltitude (default: `10000`): Maximum altitude.
- environments: List of valid environments.
- bannedEnvironments: List of invalid environments.
- globalKeys: List of global keys that must be set.
- bannedGlobalKeys: List of global keys that must not be set.

## Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-expand_world_prefabs)
Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
