## Battle

- Initial scale of dissolve victim has bad anchor behavior if in front row.
- While a monster is hurt and moved (wind spells) it shortly changes the frame index back to normal which looks off


## 2D maps

- Teleporting from a 2D map to a 2D world map via cheat with fixed positions (e.g. teleport 110 1 1) scrolls the map badly.


## 3D maps

- Should piles block monsters? Test in grandfather's cellar.
- 3D monster get easily stuck on wall edges. :(


## UI

- When editing a savegame name (in save dialog list) and aborting, the text stays (fixed?).
- "Do you accept this character?" message is missing in character creator.


## Misc

- Add more savegame slots?
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Maybe add emergency savegame that is stored when game crashes.
- Where is the text "CannotCarryAllGold" used?
- Check all unused DataNameProvider texts.