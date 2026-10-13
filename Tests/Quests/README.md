# Quest log test scenarios

Scenario scripts for the quest log (see `Ambermoon.net/ScenarioScript.cs` for all commands).
They start from the initial savegame and work with the original game and Ambermoon Advanced
in every language, as keywords are given by dictionary index.

Run them from the build output folder, e.g.:

```
Ambermoon.net --version 0 --skip-intro --load 0 --script <repo>/Tests/Quests/Grandfather.txt --exit-after 400
```

`--version 0` is the original game (German), `--version 5` is Ambermoon Advanced.
Each script prints `PASS`/`FAIL` lines and a summary at the end.

Steps which need a riddle answer, a door interaction or gold in a conversation are
simulated by setting the savegame state (`setvar`, `cheat give`) followed by `reloadquests`,
which rebuilds the quest log from the savegame like loading a game does.
