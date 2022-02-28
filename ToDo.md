## Battle

- Vanishing of wind spells looks odd in some cases
- Initial scale of dissolve victim has bad anchor behavior if in front row.
- While a monster is hurt and moved (wind spells) it shortly changes the frame index back to normal which looks strange
- There are other animations in monster data, maybe one of it is the "get moved by wind spell" animation?
- In rare cases (I guess if two party members are killed with one spell in battle) there is a part of the head below the skull in the portrait area


## 3D maps

- 3D monster get easily stuck on wall edges.
- With graphic filter on and fading, elements in front of the 3D view are semi-transparent.
  For example when entering Grandpa's cellar for the first time you can see through the popup.


## Characters

- Use tile flags of map characters (random animation start, movement blocking)


## UI

- Update transport button after game loading
- Triggering a yes/no button by key takes very long to execute the action
- Save the scroll offset for save/load lists
- Giving items per cheat to a char but have another char's inventory open, shows messed up item amounts.
- When closing the spell book with RMB in battle and there is a battle actor behind the window, the inventory is opened afterwards.
- "Do you accept this character?" message is missing in character creator.
- Question about taking items with you is missing from conversations. At least for non-essential items.
- LevelUp popup while an item is given will draw that item in front of the popup.


## Misc

- Around the Bandit's house you still get poisoned
- Teleport cheat can teleport to non-blocking map areas which are "outside" the map borders.
  For example map 344 (Ferrin's forge) has many areas that are considered outside the house.
- Where is the text "CannotCarryAllGold" used?
- Check all unused DataNameProvider texts.
- When leaving a map through a teleporter and there is an engaging monster, the attack might occur after the map change. This must be avoided.