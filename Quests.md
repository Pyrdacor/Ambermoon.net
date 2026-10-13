# Ambermoon Quest Log

The quest log (F6 or the book icon at the top left) shows main quests with their sub quests (steps).
A step is *active*, *blocked* (it waits for another step) or *completed*. Inactive steps are hidden.

The quest definitions are in `Ambermoon.Core/UI/Quests.cs`, the texts in `Ambermoon.Core/UI/QuestTexts.cs`
(German, English, French, Polish and Czech) and the custom global variables in
`Ambermoon.Core/UI/QuestCustomVariables.cs`.

## How progress is tracked

The quest log has no state of its own. Everything is derived from the savegame, so it works with existing
savegames and after loading. A step is activated or completed by triggers which check:

- global variables, event bits (disabled map event chains), character bits, tile changes,
  unlocked chests and doors, learned keywords and explored 3D blocks,
- owned items (only while they are owned, so used up items always have a persistent alternative),
- conversation and map events while they are executed.

Completion triggers also complete steps which are not active yet, so steps can be skipped (e.g. an item is found
before somebody asked for it). A main quest is completed when a final step is completed and no step is open
anymore. Skipped steps stay hidden.

### Custom global variables

Some events don't change the savegame (e.g. an NPC only tells something or takes an item without setting a
variable). For those the quest log sets otherwise unused global variables itself, counting down from 8191.

| Variable | Set when |
|---|---|
| 8191 | Tolimar is asked about tools (keyword "Tools") |
| 8190 | The cook in Baron George's house is talked to |
| 8189 | Shandra's amber is shown or given to grandfather |
| 8188 | The hill giant's head is given to Baron George |
| 8187 | The silver hand or Nagier's letter is given to Baron George |
| 8186 | All four golden horseshoes are given to Tolimar |
| 8185 | Lady Heidi or Baron George is asked about the fairies (chain of office) |
| 8184 | Sandire is talked to (monster eye offer) |
| 8183 | Aman asks for the gardener's brooch (keyword "Thing") |
| 8182 | Clementine tells about the madman (keyword "Madman") |
| 8181 | Sandra is talked to after Sabine was rescued |
| 8180 | The yellow sphere is used at the teleporter machine (map 369) |
| 8179 | Lady Heidi tells the fairy story |
| 8178 | The demon sleep is brewed (treasure with the potion in the witch's cellar) |
| 8177 | The airship on the forest moon flies away with a navstone |

Old savegames which already passed such an event can't know about it. The affected steps then wait for the
next persistent change (e.g. a later step).

## Original quests

All original quests work in the original game and in Ambermoon Advanced. Differences in Advanced are noted.
Map positions are 1-based, as shown in the game.

### Grandfather's Quest (Großvaters Queste)

1. **Talk to your grandfather**: active from the start, done when he tells about the wine (keyword "Wine").
2. **Go to the wine cellar**: the riddlemouth in the cellar (map 259) is solved (tile change 259 10,17).
3. **Find grandfather's equipment**: grandfather's chest in the old cave (chest 23) is opened.
   Blocked while the cave-in isn't removed.
4. **Inform grandfather about the cave-in**: shown after the cave-in text (event 260:13), done when he
   gives the keyword "Tools".
5. **Find Tolimar, the farrier**: done when Tolimar tells about his horseshoes or got them.
6. **Remove the cave-in**: blocked until the horseshoes are returned, done with the pick-axe (variable 11).
   A pick-axe from elsewhere completes it as well.
7. **Return to grandfather**, then **Visit grandfather's grave**: grandfather died, Father Anthony waits in his
   house (character 266:1) and the grave in Spannenberg is visited (event 263:9).

Tests: `Grandfather.txt`, `GrandfatherShortcuts.txt`, `GrandfatherPickaxe.txt`

### Golden Horseshoes (Goldene Hufeisen)

1. **Find the golden horseshoes** (x/4): started by Tolimar (8191). Owned horseshoes count, giving them to
   Tolimar (8186) keeps the step completed.
2. **Return them to Tolimar**: 8186.

Advanced: Tolimar also sets variable 255 and sells tools afterwards.
Tests: `Grandfather.txt`, `GrandfatherShortcuts.txt`

### Swamp Fever (Sumpffieber)

1. **Talk to Father Anthony**: Wat tells about his sick daughter (keyword "Antidote"). Done when Anthony is
   asked about the antidote (variable 26) or the antidote was made anyway.
2. **Obtain an empty bottle / a swamp lilly / water of life**: after Anthony's request. Done while owned or once
   the antidote was made (owning it or variable 34).
3. **Return to Father Anthony**: when both the lilly and the water of life are owned.
4. **Heal Sally**: the antidote is used on Sally (variable 34).
5. **Talk to Sally** (36) and **Talk to Wat** (35) for the rewards.

Tests: `SwampFever.txt`, `SwampFever_ItemsFirst.txt`, `SwampFever_SkipWat.txt`

### Alkem's Ring (Alkems Ring)

1. **Enter the old crypt**: Alkem gives the crypt key, the crypt door (door 15) is opened.
2. **Find the ring** inside the crypt: owned or returned (81).
3. **Return the ring to Alkem**: variable 81.

Tests: `AlkemsRing.txt`, `AlkemsRing_AskTwiceShowRing.txt`

### Sylphs (Sylphen)

1. **Talk to Lady Heidi about fairies**: started by the cook (8190). Done when Heidi tells the story (8179)
   or the quest went on without her.
2. **Find the item in the tree**: the weird stone, owned or the sylph cave door (door 10) opened with it.
3. **Find the fairies**: Sariel teaches Sylphic (variable 78).
4. **Rescue Selena**: variable 79.

Tests: `Sylphs.txt`, `Sylphs_SkipCook.txt`, `Sylphs_WithoutHeidi.txt`

### Orcish Onslaught (Die Orkplage)

1. **Defeat the orc leader**: Baron George tells about his problems (keyword "Problems"). Done with the hill
   giant's head (owned or given to the baron, 8188).
2. **Bring the proof to Baron George**: 8188.

Tests: `OrcPlague.txt`, `OrcPlague_LeaderFirst.txt`

### The Thieves' Menace (Die Diebesplage)

1. **Find the bandit leader Silverhand**: Baron George tells about his problems. Done with the silver hand
   (Nagier killed) or Nagier's letter (peace, variable 60), or when given to the baron (8187).
2. **Bring the proof to Baron George**: 8187.

Tests: `ThiefPlague.txt` (peace), `ThiefPlague_KillNagier.txt`

### Wine Trophies (Weinpokale)

1. **Find the stolen wine goblets** (x/2): Canth tells about them (keyword "Wine goblets"). Owned goblets plus
   the ones already returned count.
2. **Return them to Canth and Norlael** (x/2): each takes one goblet (variables 37 and 38).

Tests: `WineTrophies.txt`, `WineTrophies_GobletsFirst.txt`

### Chain of Office (Amtskette)

1. **Find the stolen chain of office**: Lady Heidi or Baron George tell about the fairies (8185). Done when the
   chain is found (sylph cave) or returned (31).
2. **Return it to Baron George**: variable 31.

Tests: `ChainOfOffice.txt`, `ChainOfOffice_FoundFirst.txt`

### Monster's Eye (Monsterauge)

1. **Buy the monster eye from Sandire**: Sandire offers it (8184), done when 5000 gold are given (221).

Tests: `Monstereye.txt`, `Monstereye_BuyAtOnce.txt`, `Monstereye_NotEnoughGold.txt`

### The Graveyard (Der Friedhofsgärtner)

1. **Find the gardener of the graveyard**: Aman asks for the brooch (8183). Done when the gardener (zombie
   master) appears at the hole in the hedge (event 263:24).
2. **Get the gardener's brooch**: owned or given to Aman (12).
3. **Bring the brooch to Aman**: variable 12 (he tells the password of the thieves' guild).

Tests: `Graveyard.txt`, `Graveyard_BroochFirst.txt`

### The Thieves' Guild (Die Diebesgilde)

1. **Enter the thieves' guild**: after the password (12), the riddlemouth in the wine cellar of the inn opens
   the way (event 265:4).
2. **Pass the test of thieves**: the reward chest at the end of the test (chest 36) is opened.

Advanced: Aman has a large extension (a master thief task with own variables). The riddlemouth doesn't open
without the password (in the original game it opens after a punishment).
Tests: `ThievesGuild.txt` (original game only), `ThievesGuild_NoPassword.txt`

### The Madman (Der Verrückte)

1. **Visit the madman in the cellar**: Clementine tells about him (8182), done when he is watched in his cell
   (event 267:4).
2. **Find the second part of the amulet**: after finding a part (B O S in the cellar chest, T A R from
   Thornahuun). Done with both parts, the united amulet or the healed madman (29).
3. **Have the amulet united**: Theresa in Burnville (or 29).
4. **Give the amulet to the madman**: variable 29, he becomes Tar the dark.
5. **Kill the madman**: only shown (completed) if he is killed. The amulet steps are hidden then.

Tests: `Madman.txt`, `Madman_KillWithCellKey.txt`, `Madman_KilledThenBothParts.txt`, `Madman_AmuletBeforeClementine.txt`

### Sandra's Daughter (Sandras Tochter)

1. **Look for Sabine in Burnville**: Sandra asks for her daughter (variable 28). Done with Sabine's diary
   (event 286:7) or in Luminor's torture chamber (event 293:2).
2. **Rescue Sabine from Luminor**: the cells are opened with Luminor's key (variable 27).
3. **Return to Sandra**: Sandra is talked to after the rescue (8181).

Advanced: Sandra has more conversation options (animal items), the quest is the same.
Tests: `SandrasDaughter.txt`, `SandrasDaughter_TortureChamberFirst.txt`, `SandrasDaughter_RescueBeforeSandra.txt`

### The Tower of Black Magic (Turm der Schwarzmagier)

1. **Open the tower with the sphere**: Nelvin tells about the sphere of opening (keyword) or joins.
   Done when the sphere is used in the gate house (event 292:3).
2. **Free the people of Burnville**: after entering the torture chamber (293:2), done when the cells are opened (32).
3. **Weaken Luminor with icewater** (x/4): after learning about the flame of life, finding icewater or pouring
   it (variables 99-102). Optional, completed when Luminor is defeated.
4. **Defeat Luminor**: after the torture chamber, done with Luminor's key (owned or the cells opened, 27).

Advanced: the torture chamber rescue also shows Morgat in Nalven's magic school.
Tests: `TowerOfBlackMagic.txt`, `TowerOfBlackMagic_NoIcewater.txt`, `TowerOfBlackMagic_PartialIcewaterReload.txt`,
`TowerOfBlackMagic_LuminorFirst.txt`

### Valdyn's Return (Valdyns Rückkehr)

1. **Find a way to bring Valdyn home**: Valdyn came through the light gate in Luminor's tower (event 296:9).
   Done with the yellow sphere from S'Krel (variable 218).
2. **Use the yellow sphere at the teleporter**: the teleporter room is entered with the sphere and Valdyn
   (event 369:11) or the sphere is used at the machine (8180).

Nothing is shown before Valdyn was met. Valdyn's actual return happens in the outro.
Advanced: the machine also accepts another item (variable 355).
Tests: `ValdynsReturn.txt`, `ValdynsReturn_MachineSphere.txt`, `ValdynsReturn_SphereBeforeValdyn.txt`

### Ambermoon Picture (Ambermoon Gemälde)

1. **Deliver the picture to Baron Karsten**: Dieter Rottermund gives the picture (variable 97). Done when the
   picture hangs in the baron's hall (tile change 427 11,14). Variable 184 isn't used as a lever in the antique
   area toggles it.

Advanced: Karsten's experience reward is smaller.
Tests: `AmbermoonPicture.txt`, `AmbermoonPicture_Lever.txt`

### The Brotherhood of Tarbos (Die Bruderschaft von Tarbos)

1. **Take the Amberstar to the temple**: the Amberstar is found in castle Godsbane, done when it opens the door
   to the coffin hall (door 31).
2. **Defeat Thornahuun**: after meeting him in the coffin hall (event 433:3), done when he is defeated (he is
   hidden again, noticed with the next event or item).

Advanced: Gryban has additional texts and keywords (paladin guild), the quest is the same.
Tests: `BrotherhoodOfTarbos.txt`, `BrotherhoodOfTarbos_ThornahuunFirst.txt`, `BrotherhoodOfTarbos_FleeFromThornahuun.txt`

### Lyramion's Faith (Lyramions Schicksal)

The main story:

1. **Talk to Shandra in Newlake**: grandfather tells about the wine (same dialogue). Done when Shandra's amber
   is used at her coffin (variable 193).
2. **Deliver Shandra's amber to grandfather**: after finding the amber. Done when it was shown to him (8189),
   used at the coffin or grandfather died.
3. **Use Shandra's amber**: the book key is taken from the coffin (event 428:7).
4. **Enter the secret room in the library** (door 29), **Find the recipe**, **Brew the demon sleep** (8178).
5. **Enter the temple of brotherhood**: blocked until the demon sleep is brewed. Done when the guard demon is
   gone (event 431:6, demon sleep or fight). If he is killed, the open library steps are hidden.
6. **Explore the temple**: the hangar key from S'Orel's chest (owned, chest 111 or hangar door 30).
7. **Explore the hangar**: Kire's note. **Find the navstone**: the green navstone (owned or its chest 116).
8. **Fly to the forest moon** (event 343:16), **Meet the dwarf leader Kire** (51),
   **Find a way to leave the forest moon** (blue or yellow navstone, or flown away with it, 8177).
9. **Fly to Morag**: arriving in the prison of S'angrila (event 439:2) or S'riel freed (222).
10. **Talk to S'riel**: in his cell (variable 222, he teaches Morag and brings the party to his palace).
11. **Search S'riel's palace**: chest 117 (or the crystal string 354 / S'orel's palace key 388 owned).
12. **Collect the crystal strings** (x/7): owned strings, done with seven or the strung harp / the opened
    crystal wall.
13. **Have the harp strung**: Matthias in Illien sells the elven harp and strings it (crystal harp 159 owned or
    the wall opened).
14. **Shatter the crystal wall** in the entry hall of the temple with the harp (event 431:3). The party walks
    around the wall on the first temple visit, so it is not part of step 5.
15. **Destroy the Moranian machine** (event 453:4). Final step, the outro follows.

Advanced: the library has an additional treasure, on the forest moon Ketnar offers another way (variable 320).
Tests: `LyramionsFaith.txt` and `LyramionsFaith_*.txt` (`LyramionsFaith_Morag.txt` for steps 9-15)

### Kire's Letter (Kires Brief)

1. **Find Dorina**: Kire gives a letter for his wife (keyword, variable 52). Done when Dorina is talked to (61)
   or the letter is given (45).
2. **Show the letter to Ketnar**: optional, Ketnar tells where Dorina lives (66). Completed with step 1.
3. **Give the letter to Dorina** (variable 45, she brings the party back to Kire).
4. **Return to Kire**: the reward (variable 55).

Tests: `KiresLetter.txt`, `KiresLetter_SkipKetnar.txt`

### Ferrin's Daughter (Ferrins Tochter)

1. **Ask Ferrin about his mourning**: Ferrin is talked to (86), done with the keyword and the cellar key (87).
2. **Search the storage cellar**: the cellar door (door 13) is unlocked or the daughter found.
3. **Find Ferrin's daughter**: in the small cave behind the tornak caves (variable 94).
4. **Return to Ferrin**: his wife gives the reward (95).

If the daughter is found before Ferrin was asked, the first step stays hidden.
Tests: `FerrinsDaughter.txt`, `FerrinsDaughter_FoundFirst.txt`

## Ambermoon Advanced quests

No Advanced-only quests are in the quest log yet.

## Tests

The scenario scripts in `Tests/Quests` play the quests with real items (chests, NPCs and monsters) and check
the quest states after each step and after rebuilding the quest log from the savegame. See
`Tests/Quests/README.md` for how to run them.
