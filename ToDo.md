## Battle

- Initial scale of dissolve victim has bad anchor behavior if in front row.
- While a monster is hurt and moved (wind spells) it shortly changes the frame index back to normal which looks off


## 3D maps

- 3D monster get easily stuck on wall edges. :(
- With graphic filter one and fading, elements in front of the 3D view are semi-transparent.
  For example when entering Grandpa's cellar for the first time you can see through the popup.


## UI

- When editing a savegame name (in save dialog list) and aborting, the text stays (fixed?).
- "Do you accept this character?" message is missing in character creator.
- "Really load savegame" popup is much to high. This is due to a change to YesNo popup sizing.
  But without the change, the german crash savegame load question doesn't fit.


## Misc

- Add more savegame slots?
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Where is the text "CannotCarryAllGold" used?
- Check all unused DataNameProvider texts.