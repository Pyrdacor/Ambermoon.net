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
| 8176 | Pelanis tells about Sansri (keywords "Curse" and "Concerns") |
| 8175 | Morgat makes the golden armour (Advanced) |
| 8174 | Jeff, a gatekeeper of Cavetown, is talked to (Advanced) |
| 8173 | Luke, the deputy gatekeeper of Cavetown, is talked to (Advanced) |
| 8172 | Brogat brews his blue potion from five sapphire thistles (Advanced) |

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

### The Beast (Die Bestie)

1. **Ask Norel about his apprentice Kay**: Norel in his hut on Darlogna asks for Kay (keyword "Kay"). Done when
   he told about the beast and gave the key of his weapons cupboard (variable 169).
2. **Kill the beast in the cave in the north**: after Norel's story or when the beast attacks in its cave
   (event 415:4). Done when the dead beast turns into Kay (event 415:5) or with Kay's amulet (348).
3. **Bring Kay's amulet to Norel**: Norel gives the xenobil wood for the construction staff (variable 168).

Tests: `TheBeast.txt`, `TheBeast_KillFirst.txt`

### The Witch Master (Der Hexenmeister)

1. **Ask Bralum about the gnomes' problem**: Bralum in the labyrinth of the gnomes mentions a problem (keyword
   "Problem"). Done when he told about Nera (keyword "Nera").
2. **Find Nera's lair below the witch's house**: the riddlemouth in the cellar of the witch's house is answered
   with "Nera" (event 412:2).
3. **Defeat Nera, the witch master**: Nera's ring (342) is owned or was shown to Bralum.
4. **Show Nera's ring to Bralum**: the reward (variable 129).

If Nera is defeated before Bralum told about him, the quest starts with the ring and the skipped steps stay hidden.
Tests: `WitchMaster.txt`, `WitchMaster_NeraFirst.txt`

### Sansri's Curse (Sansris Fluch)

1. **Ask Pelanis about his concerns**: Pelanis mentions the curse when the party leaves (keyword "Curse"). Done
   when he told about Sansri and the petrified eagles (8176).
2. **Defeat Sansri in her temple** (event 423:5).
3. **Give Sansri's blood to Pelanis**: he lifts the curse and gives the key of the treasure chamber and the eagle
   flute (variable 180).

Advanced: Pelanis has additional keywords (mystical beings), the quest is the same.
Tests: `SansrisCurse.txt`, `SansrisCurse_SansriFirst.txt`

### The Windgates (Die Windtore)

1. **Enter the wind shrine**: the windgate book or the key to the shrine is found (library of Newlake). Done when
   the shrine door is opened (door 23).
2. **Create the construction staff**: the xenobil wood (The Beast) and a gem on the left altar. Done with the
   staff (321) or a repaired windgate, because the shrine resets variable 127 when the staff is taken.
3. **Create the windchain**: twelve windpearls and the pearl chain on the right altar (windchain 134 owned).
4. **Repair the destroyed windgates** (x/11): variables 115-118 and 120-126. The windgate to map 234 (119) is
   intact from the start.

Tests: `Windgates.txt`

### A Trip to Gadlon (Ausflug nach Gadlon)

An easter egg leading into the world of *Dragonflight*.

1. **Activate the teleporter with the sphere**: the red carved sphere is found at the skeleton in the teleporter
   room (map 369). Done when the machine takes it (variable 210) or the party met Aderin in Gadlon (170).
2. **Find Rinakles, the head of the school**: Rinakles is talked to (variable 176).
3. **Ask Rinakles for the way home**: he sets his teleporter to Lyramion (variable 177).

Advanced: the teleporter resets variable 210 when it is used, so Aderin is checked as well.
Tests: `TripToGadlon.txt`

### Donner's Old Mine (Dönners alte Mine)

1. **Ask Bore about Donner**: Bore, the dwarf left behind in Gemstone, drops the staff of opening (variable 114).
2. **Open the entrance on the island** in front of the mine with the staff (event 230:3).
3. **Find Donner's key** in his old labyrinth (346, chest 88) or open his door.
4. **Open Donner's door** (door 25).

Tests: `DonnersMine.txt`

## Ambermoon Advanced quests

These quests only exist in Ambermoon Advanced. They are only created for Advanced savegames of the given episode or
later (the episode is detected from the savegame, see `GetAdvancedEpisode`).

### Your House (Dein Haus) — from episode 1

1. **Show the testament to Theodorus**: grandfather's testament (206, chest 2 in his cupboard) or Theodorus, the new
   carpenter of Spannenberg (keyword "Building projects"). Done when he saw it (variable 224).
2. **Have the house refurbished**: 2500 gold (variable 225).
3. **Ask Gustav about Theodorus**: his former apprentice in the inn of Snakesign (keyword "Apprentice", variable 226).
4. **Have a windgate built**: after the refurbishment and Gustav's story, 8000 gold (variable 227).
5. **Have a storage cave dug**: 10000 gold (variable 228).
6. **Explore the storage cave**: a giant spider attacks in the lower level (event 460:19).
7. **Defeat the giant spider**: it is Theodorus himself. Done when it is hidden again after it was shown or with
   Gustav's ring (407) from the remains next to it.

Tests: `YourHouse.txt`

### Pets (Haustiere) — from episode 1

1. **Feed the hungry tomcat**: Kasimir in Spannenberg (language of animals, variable 53). Done when he got food
   (character 273:4 shown) or joined the party.
2. **Adopt a cat from Ferdinand**: the gardener of Spannenberg (keyword "Cat"), 1000 gold, needs the refurbished
   house (variable 230).
3. **Make friends with the cat**: the cat toy in the house (variable 232).
4. **Adopt a dog from Ferdinand** (variable 231).
5. **Make friends with the dog**: the dog toy (variable 233).
6. **Return to Ferdinand**: his finest silver (variable 234).

Tests: `Pets.txt`

### The Sea Creatures (Die Kreaturen der See) — from episode 1

1. **Ask captain Torle about the creatures**: Michael (keyword "Map", then "Creatures of the sea") sends the party to
   Torle, who gives his logbook (variable 238).
2. **Find the creatures from Torle's logbook** (x/5): variables 239, 240, 242, 243 and 244 at sea.
3. **Visit the mermaid**: she takes the party into her grotto (event 163:1) and opens the vortex.
4. **Sail into the eye of the vortex** (variable 241).

Tests: `SeaCreatures.txt`

### Manyeyes' Castle (Vielauges Schloss) — from episode 1

1. **Open Tristan's office**: Tristan in the inn of Cavetown gives the office key (variable 245), door 39.
2. **Open the door to the castle**: the switch in the office (variable 246).
3. **Defeat the guardian of the upper floor** (variable 247).
4. **Banish Manyeyes' curse**: the monsters of the cellar are defeated, then the wall is touched (variable 249).

Tests: `ManyeyesCastle.txt`

### The Gatekeepers of Cavetown (Die Pförtner von Cavetown) — from episode 1

1. **Bring Jeff a blind brandy**: after talking to Jeff (8174) or buying the brandy (409). Done with his key (250).
2. **Find Luke's treasure map**: after talking to Luke (8173), dug up in the eye of the vortex (410).
3. **Give the treasure map to Luke**: he drops the second key (variable 251).
4. **Open the passage for ships**: the pressure plate behind both doors of the gatekeepers' house (event 376:11).

Tests: `Gatekeepers.txt`

### The Golden Armour (Die goldene Rüstung) — from episode 2

1. **Collect golden scales** (x/7): after meeting Morgat in Nalven's magic school (variable 285, he appears after
   the prisoners were freed) or finding a scale. The golden lizards south-west of Burnville drop them.
2. **Bring the scales and a chain mail to Morgat**: he makes the golden armour (8175, nothing is stored by the game).

Tests: `GoldenArmour.txt`

### The Mystic Creatures (Die mystischen Wesen) — from episode 2

The largest Advanced quest line. The creatures' quests below provide the essences.

1. **Find the wine cellar of Crystal**: the book "Towns of Lyramion" (466, library of Newlake) hints at a sign west
   of the sighing desert. Done when the entrance is dug up with a shovel (variable 260).
2. **Find the book of mystic creatures**: in the old paladin guild behind the wine cellar (variable 268).
3. **Collect the mystic creatures' essences** (x/6): the essences of Isdir, Pyrdacor, Marla, Lugthir, Fargon and
   Gunbar (438-443) owned or transferred into Kasimir by Sandra (variables 262-267).

Tests: `MysticCreatures.txt`

### Sandra's Brooch (Sandras Brosche) — from episode 2

1. **Return the brooch to Sandra**: the brooch (434) lies in the wine cellar of Crystal (variable 54).
2. **Transfer the essences into Kasimir** (x/6): Sandra transfers each essence while Kasimir is in the party
   (variables 262-267). She returns the empowered brooch afterwards.

Tests: `SandrasBrooch.txt`

### The Eyrie (Der Adlerhorst) — from episode 2

1. **Get the eagle key from Pelanis**: after the book of mystic creatures (268) and Sansri's curse (180), keyword
   "Lugthir" or the book (variable 304).
2. **Open the eyrie** (door 48).
3. **Take Lugthir's essence** (event 468:2).

Tests: `Eyrie.txt`

### The Black Mountains (Die Schwarzberge) — from episode 2

1. **Ask Baron Karsten about Pyrdacor** (variable 301).
2. **Get the key crystal**: after telling him about the paladin guild or showing the book (variable 302).
3. **Have Lebab charge the crystal**: the fire crystal (426) is owned or used.
4. **Open the black mountains** with the fire crystal (event 118:3).
5. **Defeat Pyrdacor**: for the third time, inside the mountains (variable 270).

Tests: `BlackMountains.txt`

### The Desert Temple (Der Wüstentempel) — from episode 2

The sighing desert swallows the party now and then once the book of mystic creatures is read. Advanced checks
variable 272 ("Marla defeated") there, but no event sets it, so the desert keeps swallowing the party.

1. **Solve the puzzle of the face**: the eyes closed and the mouth open (levers in maps 469 and 474, variable 286).
2. **Answer the three riddlemouths** (sand, magma or lava, swamp or marsh; events 474:2-4).
3. **Defeat Marla** (event 474:32).

Tests: `DesertTemple.txt`

### The Paladin Guild of Crystal (Die Paladingilde von Crystal) — from episode 2

1. **Break through the cracked wall** in the wine cellar of Crystal with the giant mallet (event 470:31).
2. **Light the four candles** with flint and iron (variable 259).
3. **Summon the paladin master** with ten paladin emblems, Gryban has to be in the party (event 472:4).
4. **Defeat the paladin master**: he is hidden again after he was summoned.
5. **Open the exit of the guild** (variable 261).
6. **Take Gunbar's essence** from his statue (variable 400).
7. **Find Fargon's essence**: owned, its chest (181) unlocked or transferred into Kasimir (266).

Tests: `PaladinGuild.txt`

### Isdir's Trials (Isdirs Prüfungen) — from episode 2

Needs the sea creatures (eye of the vortex) and Jeff's blind brandy (Gatekeepers of Cavetown).

1. **Ask Jeff about Isdir**: after the book of mystic creatures (268), Jeff remembers the northern beach (305).
2. **Meet Isdir at the northern beach at night**: the way to Isdir's cave appears (event 370:14).
3. **Bring golden water to the crystal**: Lebab makes it from a golden scale, water of life and a fire thistle
   (touching the crystal 291, done 287).
4. **Bring a restless soul to the crystal**: the ghost at the graveyard of Spannenberg at night (292, 288).
5. **Bring a poisonous snake to the crystal**: dug up on an island (map 249, 293, 289).
6. **Bring the blue crystal to the ice crystal**: the beast at night and digging on map 117 (294, 290).
7. **Receive Isdir's essence** (variable 298).

Tests: `IsdirsTrials.txt`

### The Magic Word (Das Zauberwort) — from episode 2

1. **Tell Milzor the magic word**: Necros, the talking chair in the witch's house, tells the old magic word "Talis"
   when Kasimir is in the party. Milzor tries it and raises the luck of the party (variable 307).

Tests: `MagicWord.txt`

### The Wasps' Nest (Das Wespennest) — from episode 3

1. **Talk to Ketnar in Dor Grestin**: Ketnar asks for it when the airship is entered with a navstone (variable 320).
   Done when he told about the cave which can only be reached flying (321).
2. **Ask Feldrar about wasps**: Feldrar is in the tavern "The nest" in the morning only (wasp riding tack, 350).
3. **Defeat the wasp queen** in the sinking room below the wasps' nest (character 379:1).
4. **Report to Ketnar**: he hears about Hudyn's fate (variable 395).

Tests: `WaspsNest.txt`

### Dorina's Wedding Ring (Dorinas Ehering) — from episode 3

1. **Find Dorina's wedding ring**: after Ketnar sent the party to her (396), Dorina tells about the ring (397).
   It lies in a lake (522).
2. **Return the ring to Dorina**: the alchemist crown (variable 398).

Tests: `WaspsNest.txt`

### Sapphire Thistles (Saphir-Disteln) — from episode 3

1. **Pick sapphire thistles** (x/5): Brogat in the tavern "The nest" tells about them (352). They grow randomly
   on the river island east of Dor Grestin.
2. **Bring five sapphire thistles to Brogat**: he brews his blue potion (8172, nothing is stored by the game).

Tests: `SapphireThistles.txt`

### The Ancient Vault (Das antike Gewölbe) — from episode 3

Below the wasps' nest.

1. **Place the ancient book on the pedestal**: after the wasp queen or with the ancient book (362, antique area).
   Done when the book lies on the pedestal (variable 328).
2. **Pass the trials of the vault** (x/3): intelligence, dexterity and strength end at green lights (344-346).
3. **Receive the reward of the guardian**: the green carved sphere (488, variable 347).

Tests: `AncientVault.txt`

### The Unknown World (Die unbekannte Welt) — from episode 3

1. **Activate the teleporter with the sphere**: the green sphere in the machine of the teleporter room (variable
   355, reset when the teleporter is used, so meeting Sheera counts as well).
2. **Meet Sheera**, the feline explorer in the krondir cave (variable 356).
3. **Collect ten triis** (x/10): the crystals of the krondirs (493, charged 494).
4. **Charge the triis**: Sheera charges one (358), the krondirs' liquid the others, the portal works (360).
5. **Return to Lyramion** through the exit (variable 354).

Tests: `AncientVault.txt`

### The Gral Temple (Der Graltempel) — from episode 3

1. **Ask Melvin about his family**: Melvin in Snakesign (keyword "Family"). Done with the legend (variable 310).
2. **Say GRAL in front of the mountains at night** (map 183, event 183:1).
3. **Open the inner door of the temple** with the gral key (door 52).
4. **Place the heavy stone on the altar** (event 475:29).
5. **Defeat Diokhar** (event 475:36).
6. **Find the remains of Melvin's ancestor**: the emerald necklace (483) in the old sewers.
7. **Bring the emerald necklace to Melvin** (variable 322).

Tests: `GralTemple.txt`

### The Mystic Tower (Der Mystikerturm) — from episode 3

Targor (party member 13) brings his notes (507). The places need Targor in the party and able to act.

1. **Visit the pond west of Illien** (event 85:1).
2. **Visit the lonely tree** (event 102:1).
3. **Visit the mountain pass**: the third symbol makes the mystic tower rise (event 134:1).
4. **Enter the mystic tower**: the stone head lets Targor in (event 478:2).
5. **Pass the test of high mysticism**: the friends die, Targor learns the mystic imitation at the glass orbs and
   defeats the demon (variable 371 or event 478:18). Variable 374 (spell learned) is reset if the test is aborted.

Tests: `MysticTower.txt`

### The Highest Thieves' Test (Die höchste Diebesprüfung) — from episode 3

Starts after the thieves' guild (the riddlemouth shows Aman in the weapons chamber of Gemstone).

1. **Help Aman against the knights**: Aman asks for help (variable 376), done when the knights are gone (375).
2. **Meet Aman in the thieves' guild**: he gives the silver lockpick in the cellar (variable 377).
3. **Solve the riddle of the barrels**: after the trap, lock and dexterity doors the old items are put into the right
   barrels (variable 391, the stealth cloak is unlocked). The barrel variables 381-390 are reset on failure.
4. **Show the stealth cloak to Aman**: he gives his key (variable 380).
5. **Pass the test of stealth**: the final switch behind the torches shows Aman in the small room (character 265:20).
6. **Talk to Aman**: he reveals himself as the head of the guild (variable 378).

Tests: `MasterThief.txt`

### Drelb's Search (Drelbs Suche) — from episode 3

1. **Defeat the monsters in the gnome mine**: after meeting Drelb (variable 362), the four monsters in the labyrinth
   of the gnomes (characters 409:2-5).
2. **Ask Drelb about his search**: he hides six things (variable 369).
3. **Find Drelb's hiding places** (x/6): variables 364-368 (searching 20 to 100) and his certificate (496, searching
   120) in the last one.
4. **Give the certificate to Drelb**: his brooch (variable 363).

Tests: `DrelbsSearch.txt`

## Tests

The scenario scripts in `Tests/Quests` play the quests with real items (chests, NPCs and monsters) and check
the quest states after each step and after rebuilding the quest log from the savegame. See
`Tests/Quests/README.md` for how to run them.
