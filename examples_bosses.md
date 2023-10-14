# Examples for bosses

This mod can be used to make bosses more difficult.

## Two stars on all bosses

Unfortunately not visible because bosses have different UI.

```yaml
- prefab: Eikthyr,gd_king,Bonemass,Dragon,GoblinKing,SeekerQueen
  data: two_star
```

`expand_data.yaml`: Changes level to 3 (2 stars).

```yaml
- name: two_star
  ints:
  - level, 3
```

## Stronger bosses

10% chance for a much stronger variant:

```yaml
- prefab: Bonemass
  data: ultra_bonemass
  weight: 0.1
```

`expand_data.yaml`: Changes multiple stats as an example.

```yaml
- name: ultra_bonemass
  ints:
  - HasFields, 1
  - HasFieldsHumanoid, 1
# Could use level here too.
  strings:
  - Humanoid.m_name, Ultra Bonemass
# Raid event constantly spawns enemies.
  - Humanoid.m_bossEvent, army_bonemass
  floats:
  - Humanoid.m_runSpeed, 50
# 50% more damage.
  - RandomSkillFactor, 1.5
# Slightly different health prevents the star based health (at least until boss is damaged and healed back to full).
  - max_health, 10000
  - health, 10000.1
```

## Bonemass: Summons different enemies

50% chance to summon a different enemy when near Bonemass:

```yaml
- prefab: Blob
  weight: 0.5
  swap: BlobElite
  objects: Bonemass
  objectDistance: 50
- prefab: Skeleton
  weight: 0.5
  swap: Draugr
  objects: Bonemass
  objectDistance: 50
```

50% chance for two stars when near Bonemass:

```yaml
- prefab: Blob, Skeleton
  weight: 0.5
  data: two_star
  objects: Bonemass
  objectDistance: 50
```

## Moder: Ice blast can spawn hatchlings

In the ground:

```yaml
- prefab: IceBlocker
  weight: 0.5
  swap: Hatchling
```

In the air:

```yaml
- prefab: dragon_ice_projectile
  weight: 0.5
# World Edit commands is needed on server for spawn_object command.
  command: spawn_object Hatchling pos=$$x,$$z,$$y
```
