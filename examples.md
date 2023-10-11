# Examples

The simplest is swaps.

## Swaps

Note: When swapping creatures, the spawn limit still checks the amount of original creature. This can lead to very high amount of creatures. Recommended to only swap some of the creatures.

### Swap 33% of wolves to fenrings in high mountains

```yaml
- prefab: Wolf
  biomes: Mountain
  minAltitude: 100
  weight: 2
- prefab: Wolf
  swap: Fenring
  biomes: Mountain
  minAltitude: 100
  weight: 1
```

## Custom data

Data requires adding entries to the `expand_data.yaml` file.

### After killing Bonemass, all Greydwarves spawn with 2 stars

```yaml
- prefab: Greydwarf
  globalKeys: ```
  data: 2star
```

```yaml
- data: 2star
  ints:
  - level: 2
```

## Command

- Use $$x, $$y and $$z in the command to use the object center point.
- Use $$a, in the command to use the object rotation.
- Basic arithmetic is supported. For example `$$x+10` would add 10 meters to the x coordinate.

### Gift from Odin

```yaml
- prefab: odin
  command: spawn_object TreasureChest_meadows refPos=$$x+5,$$z,$$y
```
