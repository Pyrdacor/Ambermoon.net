## Battle

- Can't skip spell reflect message with space key
- Does spell reflect message use wrong color?
- Winddevil etc enlarges/changes size of the monster sprite.
- Winddevil and windhowler end animation looks wrong.
- Row target selection should only show 1 highlighted row on hover (not blinking all rows).
- When clicking on a row which is not possible, the spell should not be locked in.
- If monsters are in nearest row and can't attack anyone it should
  - Either move diagonal up in direction of a player (best to attack him immediately)
  - Or otherwise don't move at all
- Initial scale of dissolve victim seems to be too small. Don't know why or if this is caused by anchor.
- If background has darker color (night battles) the monsters should be darkened too.
- While a monster is hurt and moved (wind spells) it shortly changes the frame index back to normal which looks off
- If a near monster in the middle casts magic projectile (maybe similar things too) it isn't shown (tested with energy balls)
- Energyballs did not disappear after won battle (map 361)
- Locking in a "target all" spell and then pick the target of a single-target spell with same char, will still show the selection of all fields.


## 2D maps

- Scroll offset of the map is sometimes wrong. Especially after teleporting (with and without cheat).
- A horse etc can be drawn in front of tree tops etc.
- Sometimes the indoor player is in front of arcs he should be covered by (map 290 -> Nalven's magic school).
- Ouch bubble on top of map slightly shimmers through the UI border


## 3D maps

- Should piles block monsters? Test in grandfather's cellar.
- 3D monster get easily stuck on wall edges. :(


## UI

- When repairing item etc the item tooltip should not show up after magic animation.
- "Do you accept this character?" message is missing in character creator.


## Misc

- Add more savegame slots?
- First time resolution is wrong. Also sometimes after switching from fullscreen to window.
- Load time is long. Either improve performance or if not possible
  only load the intro and load data while intro/logo is shown (maybe with progress bar).
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Copying ADF files next to Ambermoon.net.exe with versions.dat doesn't show the external version.
  It also doesn't work when setting the data path in the cfg to the same directory (ADF files).
- Maybe add emergency savegame that is stored when game crashes.
- Check if NPCs have a time range where they are visible (Aman for example)