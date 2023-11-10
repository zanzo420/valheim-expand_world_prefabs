# Expand World Prefabs

Allows configuring and replacing spawned prefabs.

Install on the server and optionally on the clients (modding [guide](https://youtu.be/L9ljm2eKLrk)).

Install [Expand World Data](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Data/).

## Features

- Modify or swap spawned creatures.
- Modify or swap built structures.
- Modify or swap other objects.

Note: When swapping creature spawns, the spawn limit still checks the amount of original creature. This can lead to very high amount of creatures.

### Client / server

Expand World Data has a setting "Server only" that disables the client side requirement.

Most of the features will still work. Client side installation gives following benefits:

- No network lag. With "Server only", the objects can briefly disappear when the server replaces them.
- Temporary objects like projectiles and area attacks can be replaced.

## Configuration

The file `expand_world/expand_prefabs.yaml` is created when loading a world.

### expand_prefabs.yaml

- prefab: List of affected object ids.
- weight (default: `1`): Chance to be selected if multiple entries match.
  - All weights are summed and the probability is `weight / sum`.
  - If the sum is less than 1, the probability is `weight`, so there is a chance to not select any entry.
- swap: List of swapped ids and their amount.
  - For example `swap: WolfMeat` swaps to a single wolf meat.
  - For example `swap: WolfMeat:2` swaps to two wolf meats.
  - For example `swap: WolfMeat:2, WolfPelt` swaps to two wolf meats and one wolf pelt.
- data: Name of the data entry (from `expand_data.yaml`) or data code.
- command: Console command to run.
- biomes: List of valid biomes.
- day (default: `true`): Valid during the day.
- night (default: `true`): Valid during the night.
- minDistance (default: `0` times world radius): Minimum distance from the world center.
- maxDistance (default: `1000` times world radius): Maximum distance from the world center.
- minAltitude (default: `-10000` meters): Minimum altitude.
- maxAltitude (default: `10000` meters): Maximum altitude.
- environments: List of valid environments.
- bannedEnvironments: List of invalid environments.
- globalKeys: List of global keys that must be set.
- bannedGlobalKeys: List of global keys that must not be set.
- objects: List of object ids. At least one must be nearby,
- objectDistance (default: `100` meters): Search distance for nearby objects.
- locations: List of location ids. At least one must be nearby,
- locationDistance (default: `0` meters): Search distance for nearby locations.
  - If 0, uses the location exterior radius.
- events: List of event ids. At least one must be active nearby.
- eventDistance (default: `100` meters): Search distance for nearby events.

## Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-expand_world_prefabs)
Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
