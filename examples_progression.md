# Examples for progression

Wiki has list of vanilla global keys: <https://valheim.fandom.com/wiki/Global_Keys>

## Remove night time spawns

On vanilla, global keys can enable extra night time spawn.

`expand_prefab.yaml`: Fulings are removed on Meadows.

```yaml
- prefab: Goblin
  biomes: Meadows
  swap: remove
```

Another option is to replace with another monster.

## Weaken night time spawns

`expand_prefab.yaml`: Fulings are weakened on Meadows.

```yaml
- prefab: Goblin
  biomes: Meadows
  data: weak_fuling
```

`expand_data.yaml`: Halves damage dealt and removes hunt mode.

```yaml
- name: weak_fuling
  ints:
  - HasFields, 1
  - HasFieldsHumanoid, 1
  - huntplayer, 0
  strings:
  - Humanoid.m_name, Fuling Patrol
  floats:
  - RandomSkillFactor, 0.5
```

## Harder early biomes after defeating a boss

This can be used to keep lower level areas challenging and give more loot.

`expand_prefab.yaml`: Greydwarves become stronger after defeating Bonemass.

```yaml
- prefab: Greydwarf
  data: two_star
  globalKeys: defeated_bonemass
```

`expand_data.yaml`: Changes level to 3 (2 stars).

```yaml
- name: two_star
  ints:
  - level, 3
```

## Set global key when defeating a special enemy

This global key can then be used in other entries (for example farming, spawns or bosses).

`expand_prefab.yaml`: 1% chance for a special wolf on tall mountains.

```yaml
- prefab: Wolf
  biomes: Mountains
  minAltitude: 100
  weight: 0.01
  data: special_wolf
# Only spawns if this key is set (perhaps activated by another quest or by admin).
# Remove this line to be always available.
  globalKeys: special_wolf
  command: removekey special_wolf
```

`expand_data.yaml`: 3 stars with boss UI.

```yaml
- name: special_wolf
  ints:
  - HasFields, 1
  - HasFieldsHumanoid, 1
# 3 stars for a lot more loot (not shown on UI).
  - level, 4
  - Humanoid.m_boss, 1
# Hunt is needed to prevent taming.
  - huntplayer, 1
  strings:
  - Humanoid.m_name, Strong Wolf
  - Humanoid.m_defeatSetGlobalKey, defeated_special_wolf
# Other fields also available, check example_bosses.md.
```

## Set global key when building a structure

This probably needs some lore or instructions to make sense.

`expand_prefab.yaml`: Set a key when building a windmill on a tall mountain.

```yaml
- prefab: windmill
  biomes: Mountain
  minAltitude: 100
# Can only be triggered after killing Moder.
  globalKeys: defeated_dragon
# Trigger only once.
  bannedGlobalKeys: windmill_on_mountain
# Server Devcommands is needed on server for broadcast command.
  command: setkey windmill_on_mountain;event wolves $$x $$z;broadcast center "Windmill on a mountain!"
```
