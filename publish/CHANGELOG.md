- v1.4
  - Adds a new field `objectsLimit` to set how the field `objects` is used.
  - Improves single player support.
  - Reworks the field `objects` to allow putting data filter.
  - Removes the field `objectDistance` as obsolete.
  - Fixes lag if vegetation was changed in a server.
  - Fixes swapping incorrectly taking some properties from the original object, instead of the new object.

- v1.3
  - Adds a new field `type` to select if the entry affects spawned or destroyed objects.
  - Adds a new field `spawn` to spawn a new object without removing the original object.
  - Adds a new field `remove` to remove the original object.
  - Adds new fields `bannedFilter` and `filter` to filter destroyed objects.
  - Adds new fields  `bannedFilters`, `commands`, `filters`, `spawns` and `swaps` to allow multiple values.
  - Adds support of coordinates and data to `spawn` and `swap`.
  - Changes the default max distance to 1000x of the world radius.

- v1.2
  - Improves object swapping.

- v1.1
  - Adds support for multiple ids in the prefab field.
  - Adds fields events and eventDistance.
  - Adds fields objects and objectDistance.
  - Adds fields locations and locationDistance.
  - Adds fields day and night.
  - Changes weight calculation to allow nothing if total weight is less than 1.
  - Fixes command not working without data or swap.

- v1.0
  - Initial release.
