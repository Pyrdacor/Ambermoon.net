# Quest log test scenarios

Scenario scripts for the quest log (see `Ambermoon.net/ScenarioScript.cs` for all commands and
`Quests.md` in the repository root for the quests). They start from the initial savegame and play
the quests: items are taken from chests, NPCs and defeated monsters, conversations are held, keys,
riddles and map events are used. After each step the quest states are checked, at the end the quest
log is rebuilt from the savegame (`reloadquests`, like loading a game) to check that the progress persists.

Besides the normal playthrough (`<Quest>.txt`) there are scripts for shortcuts and edge cases
(`<Quest>_<Case>.txt`), e.g. items found before the quest is known, steps done out of order,
alternative solutions or old savegames.

Keywords and riddle answers are given by dictionary index, so the scripts work in every language.
All scripts work with the original game and Ambermoon Advanced, except `ThievesGuild.txt`
(Aman's conversation differs in Advanced) and the scripts of the Advanced-only quests (`YourHouse.txt`, `Pets.txt`, `SeaCreatures.txt`, `ManyeyesCastle.txt`, `Gatekeepers.txt`,
`GoldenArmour.txt`, `MysticCreatures.txt`, `SandrasBrooch.txt`, `Eyrie.txt`, `BlackMountains.txt`,
`DesertTemple.txt`, `PaladinGuild.txt`, `IsdirsTrials.txt`, `MagicWord.txt`, `WaspsNest.txt`, `SapphireThistles.txt`, `AncientVault.txt`, `GralTemple.txt`, `MysticTower.txt`, `MasterThief.txt`, `DrelbsSearch.txt`), which need
`--version 5`.

Run them from the build output folder, e.g.:

```
Ambermoon.net --version 0 --skip-intro --load 0 --script <repo>/Tests/Quests/Grandfather.txt --exit-after 600
```

`--version 0` is the original game (German), `--version 5` is Ambermoon Advanced.
Each script prints `PASS`/`FAIL` lines and a summary at the end.

Most scripts teleport between the steps of a quest (`goto`). The `*_OnFoot.txt` scripts play a quest completely
like a player: the party walks through the maps (`walkto`, battles on the way are won with the win cheat), flies
on the world map (`cheat fly`, `flyto`, `land`) and solves the puzzles on the way, e.g. the lever puzzle of the
orc cave (`OrcPlague_OnFoot.txt`, original game) or the way through the mystic tower (`MysticTower_OnFoot.txt`,
Advanced, `--version 5`). They take about 10 minutes each.

Cheats are only used for things which are not part of the tested quest, e.g. keys or items from other
quests (`cheat give`), light in dark dungeons, winning or avoiding fights (`cheat win`, `cheat berserk`) or teleporting to a place
(`goto`, `cheat teleport`). Skills, attributes and the time of day can be set for tests which need them
(`setskill`, `setattr`, `hour`). Some scripts use `cheat netsrak` (max stats and all languages) so the lone hero
can carry heavy loot or talk to NPCs of other languages.
