
# Object filtering

## Simple filter

All wolves near a fenring, spawns as a fenring.

```yaml
- prefab: Wolf
  swap: Fenring
# No action to keep the original wolf.
  objects:
  - Fenring, 50
```

## Data filter

Destroying bushes while having a special boar nearby gives a chance to spawn a mushroom.

```yaml
- prefab: Bush01_heath
  type: destroy
  weight: 0.5
  spawn: Mushroom
  objects:
    - Boar, 10, mushroom_boar
```

Data

```yaml
- name: mushroom_boar
  ints:
  - HasFields, 1
  - HasFieldsHumanoid, 1
  strings:
  - Humanoid.m_name, Mushroom Boar
```

## Multiple filters

When stalagmite is destroyed, it spawns a wolf if nearby itemstands have correct items.

Otherwise a lightning is spawned.

```yaml
- prefab: caverock_ice_stalagmite
  type: destroy
# Very high weight to always select it if filters match.
  weight: 1E30
  spawn: Wolf
# Upgrade World required to remove items.
  command: objects_edit itemstandh pos={x},{z} maxDistance=10 data=item,""#
# No object limit so each rule must apply at least once.
  objects:
    - itemstandh, 10, item_hammer
    - itemstandh, 10, item_torch

- prefab: caverock_ice_stalagmite
  command: objects_edit itemstandh pos={x},{z} maxDistance=10 data=item,""
  type: destroy
  spawn: lightningAOE
  objects:
    - itemstandh, 10
```

Data

```yaml
- name: item_torch
  strings:
  - item, Torch
- name: item_hammer
  strings:
  - item, Hammer
```

## Custom objects limit

If 10 wolves are nearby, the next wolf spawns as a fenring.

```yaml
- prefab: Wolf
  swap: Fenring
# Note: If multiple filters match, the first one is used.
  objects:
# Two star wolf counts as 4 wolves.
  - Wolf, 50, two_star, 4
# One star wolf counts as 2 wolves.
  - Wolf, 50, one_star, 2
  - Wolf, 50
  objectsLimit: 10

- prefab: Wolf
# Very high weight to always select it if a fenring is nearby.
  weight: 1E30
# No action to keep the original wolf.
  objects:
  - Fenring, 50
```

Data

```yaml
- name: one_star
  ints:
  - level, 1
- name: two_star
  ints:
  - level, 2
```
