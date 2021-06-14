## Battle

- Can't skip spell reflect message with space key
- Does spell reflect message use wrong color?
- Firepillar's big flame must appear between 2nd and 3rd row.
- Firepillar flame animation looks wrong.
- Winddevil etc enlarges/changes size of the monster sprite.
- Winddevil and windhowler end animation looks wrong.
- Row target selection should only show 1 highlighted row on hover (not blinking all rows).
- When clicking on a row which is not possible, the spell should not be locked in.
- If monsters are in nearest row and can't attack anyone it should
  - Either move diagonal up in direction of a player (best to attack him immediately)
  - Or otherwise don't move at all
- Initial scale of dissolve victim seems to be too small. Don't know why or if this is caused by anchor.
- If background has darker color (night battles) the monsters should be darkened too.

## Events

- Teleporters in temple of gala did not work once after cheat teleport (map: 277 x: ~9 y: ~9)


## 3D maps

- Should piles block monsters? Test in grandfather's cellar.
- Monster should be able to run through destroyed cobwebs etc.
- Blocks behind walls should not be explored when nearby.
- 3D monster get easily stuck on wall edges. :(
- NPCs should not walk through doors etc. Or should they? They do so in Spannenberg park.
  For sure they should not leave the town. The burnville guard sometimes leaves the town and
  walks outside.
- After climbing to new map (map 355 ~17,4) with a rope, the monster jumps to a new location.
- When climbing up/down the player appears to far away from the hole (maybe move him nearer beforehand).
- Sometimes stars are shown in dungeons when it's dark (no light).


## 2D maps

- 2D monsters should stay for 1 timeslot when they were on top of the player (e.g. flee success) cause
  otherwise they always stay on the player when he moves.
- Town house 2 (couple) inside door is only triggered when already moved outside map.
- In Spannenberg Inn the player is above walls. This is the case cause the position is wrong when entering the map.


## UI

- Memorize spell list scroll offset per party member (temporary per game).
- Cursor keys should scroll automap. Mouse wheel would be cool to.
- Center player when opening automap.
- Scrolling by dragging can't put the scrollbar to the top or bottom. There is a small gap.
- When repairing item etc the item tooltip should not show up after magic animation.
- I guess buff duration bars are a bit wrong (ingame no empty bar is shown, while it is in remake).
- Speaking to simple NPC the popup uses centered text. Is this right?
- Error messages in inventory can't be acknowledge by right click.
- "Do you accept this character?" message is missing in character creator.


## Misc

- Add item breakage (if breack chance != 0 against 1000) on item usage. Effect will take place even when breaking.
- Load time is long. Either improve performance or if not possible
  only load the intro and load data while intro/logo is shown (maybe with progress bar).
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Copying ADF files next to Ambermoon.net.exe with versions.dat doesn't show the external version.
  It also doesn't work when setting the data path in the cfg to the same directory (ADF files).
- Add visual effects of drugs and mouse movement.
- Maybe add emergency savegame that is stored when game crashes.
- Switching fullscreen by key (F11) the resolution is not corrected
