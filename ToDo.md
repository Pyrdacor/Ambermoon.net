## Battle

- Can't skip spell reflect message with space key
- Does spell reflect message use wrong color?
- Winddevil and windhowler end animation looks wrong.
- If monsters are in nearest row and can't attack anyone it should
  - Either move diagonal up in direction of a player (best to attack him immediately)
  - Or otherwise don't move at all
- Initial scale of dissolve victim seems to be too small. Don't know why or if this is caused by anchor.
- While a monster is hurt and moved (wind spells) it shortly changes the frame index back to normal which looks off
- If a near monster in the middle casts magic projectile (maybe similar things too) it isn't shown (tested with energy balls)


## 2D maps

- Teleporting from a 2D map to a 2D world map via cheat with fixed positions (e.g. teleport 110 1 1) scrolls the map badly.


## 3D maps

- Should piles block monsters? Test in grandfather's cellar.
- 3D monster get easily stuck on wall edges. :(
- When moving to go-to points the time progresses based on distance.


## UI

- "Do you accept this character?" message is missing in character creator.
- Add "New game" button to game over screen


## Misc

- Add more savegame slots?
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Copying ADF files next to Ambermoon.net.exe with versions.dat doesn't show the external version.
  It also doesn't work when setting the data path in the cfg to the same directory (ADF files).
- Maybe add emergency savegame that is stored when game crashes.
- Where is the text "CannotCarryAllGold" used?
- Check all unused DataNameProvider texts.