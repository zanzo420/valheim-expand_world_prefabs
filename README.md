# Expand World Prefabs

Allows configuring and replacing spawned prefabs.

Install on the server and optionally on the clients (modding [guide](https://youtu.be/L9ljm2eKLrk)).

Install [Expand World Data](https://valheim.thunderstore.io/package/JereKuusela/Expand_World_Data/).

## Features

- Modify or swap spawned creatures.
- Modify or swap built structures.
- Modify or swap other objects.
- Swap destroyed creatures, structures and objects.

Note: When swapping creature spawns, the spawn limit still checks the amount of original creature. This can lead to very high amount of creatures.

## Configuration

The file `expand_world/expand_prefabs.yaml` is created when loading a world.

### expand_prefabs.yaml

All fields here are put on a single line. List values are separated by `,`.

- prefab: List of affected object ids.
- type (default `create`): "create" or "destroy".
  - Rules with "create" are used when objects are created.
  - Rules with "destroy" are used when objects are destroyed.
  - Objects spawned or removed by this mod never trigger rules.
- weight (default: `1`): Chance to be selected if multiple entries match.
  - All weights are summed and the probability is `weight / sum`.
  - If the sum is less than 1, the probability is `weight`, so there is a chance to not select any entry.
- data: Injects data to the object.
  - Name of the data entry (from `expand_data.yaml`) or data code.
  - Injection is done by removing the original object and spawning the injected object.
  - The data is also injected to `swap` and `spawn`.
- swap: Swaps the object with another object.
  - The data is copied from the original object and from the `data` field.
  - Swap is done by removing the original object and spawning the swapped object.
  - If the swapped object is not valid, the original object is still destroyed.
- spawn: Spawns another object.
  - The data is copied from the `data` field.
- remove (default: `false`): If true, the created object is removed.
- command: Console command to run.
- biomes: List of valid biomes.
- day (default: `true`): Valid during the day.
- night (default: `true`): Valid during the night.
- minDistance (default: `0` times world radius): Minimum distance from the world center.
- maxDistance (default: `1000` times world radius): Maximum distance from the world center.
- minAltitude (default: `-10000` meters): Minimum altitude.
- maxAltitude (default: `10000` meters): Maximum altitude.
- environments: List of valid environments.
- bannedEnvironments: List of  invalid environments.
- globalKeys: List of global keys that must be set.
- bannedGlobalKeys: List of  global keys that must not be set.
- objects: List of  object ids. At least one must be nearby.
- objectDistance (default: `100` meters): Search distance for nearby objects.
- locations: List of location ids. At least one must be nearby.
- locationDistance (default: `0` meters): Search distance for nearby locations.
  - If 0, uses the location exterior radius.
- events: List of event ids. At least one must be active nearby.
- eventDistance (default: `100` meters): Search distance for nearby events.
- filter: Data filter for the destroyed object.
  - This might for new objects but they don't usually have any data.
  - Format is `type, key, value`. Support types are int, float and string.
  - For example `int, level, 2-3` would apply to creatures with level 2 or 3.
- bannedFilter: Data filter that must not be true.

### Lists

To set multiple values, following fields can be used instead:

- swaps: Swaps the object with multiple objects.
- spawns: Spawns multiple objects.
- commands: List of console commands to run.
- filters: List of data filters. All must match.
- bannedFilters: List of data filters. None must match.

## Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-expand_world_prefabs)
Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
