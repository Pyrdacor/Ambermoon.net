# Ambermoon Advanced 1.33 data fixes

The Advanced 1.33 version data of the remake contains some fixes from Ambermoon Advanced episode 4
(see `Package/changelog.txt`, version 1.14). These scripts check them with the scripting interface
(see `Ambermoon.net/ScenarioScript.cs`):

```
Ambermoon.net --version 5 --skip-intro --load 0 --script <repo>/Tests/AdvancedData/AA133Fixes_German.txt --exit-after 150
Ambermoon.net --version 6 --skip-intro --load 0 --script <repo>/Tests/AdvancedData/AA133Fixes_English.txt --exit-after 150
```

The Marla fix (global variable 272) is checked by `Tests/Quests/DesertTemple.txt`.
