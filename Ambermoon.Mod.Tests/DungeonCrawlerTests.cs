using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Mod.AmbermoonDC;
using Ambermoon.Mod.Packaging;

namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    static string FindDCModFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Ambermoon.net.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Solution folder not found.");

        var modFolder = Directory.GetDirectories(Path.Combine(directory.FullName, "Ambermoon.Mod.AmbermoonDC", "bin"), "AmbermoonDC", SearchOption.AllDirectories)
            .Where(d => File.Exists(Path.Combine(d, ModManifest.FileName)))
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return modFolder ?? throw new DirectoryNotFoundException("Build the AmbermoonDC mod first.");
    }

    static void DungeonCrawlerTests(int floors)
    {
        Console.WriteLine("Ambermoon DC: loading the mod through the mod runtime");

        {
            using var package = ModPackage.Open(FindDCModFolder());
            var runtime = ModRuntime.Load(package, BaseGameLoader.Load("English"), "1.20");
            Check(runtime.Mod?.GetType().Name == nameof(AmbermoonDCMod), "Mod class is created");
            Check(runtime.Mod?.GetType().Assembly != typeof(AmbermoonDCMod).Assembly, "Mod is loaded into its own load context");
        }

        foreach (var language in new[] { "English", "German" })
        {
            Console.WriteLine($"Ambermoon DC: generating {floors} floors ({language})");

            var gameData = BaseGameLoader.Load(language);
            var savegameManager = new SavegameManager(Path.GetTempPath());
            var savegame = savegameManager.LoadInitial(gameData, new SavegameSerializer());
            var manifest = ModManifest.Parse(File.ReadAllText(Path.Combine(FindDCModFolder(), ModManifest.FileName)));
            var api = new ModApi(manifest, new FolderModSource(FindDCModFolder()), gameData);
            var mod = new AmbermoonDCMod { Api = api };
            mod.OnInitialize();
            mod.StartNewRun(savegame, 12345);

            Check(mod.Themes.Count >= 5, "Themes are loaded");
            Check(savegame.CurrentMapIndex == 601, "Floor 1 uses map 601");
            Check(savegame.GetPartyMember(0)!.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex != 0, "Hero has a weapon");
            Check(savegame.GetPartyMember(0)!.Inventory.Slots.Any(s => s.ItemIndex == DCContent.Torch), "Hero has torches");

            var party = savegame.CurrentPartyMemberIndices;
            DungeonTheme? previousTheme = null;

            for (int floor = 1; floor <= floors; floor++)
            {
                if (floor > 1)
                    mod.EnterFloor(savegame, floor);

                var layout = mod.CurrentLayout!;
                var map = gameData.MapManager.GetMap(FloorBuilderTestAccess.MapIndex(floor))!;
                var labdata = gameData.MapManager.GetLabdataForMap(map);
                var theme = mod.CurrentTheme!;
                Check(map.TilesetOrLabdataIndex == theme.LabdataIndex && labdata == theme.Labdata, $"Floor {floor}: uses the labdata of the theme");
                Check(map.PaletteIndex == theme.PaletteForFloor(mod.State!.Seed, floor) && theme.Definition.Palettes.Contains(map.PaletteIndex),
                    $"Floor {floor}: uses a palette of the theme");

                if (floor % 5 == 1)
                {
                    Check(theme != previousTheme, $"Floor {floor}: a new theme is chosen");

                    if (floor % 5 == 1)
                        Console.WriteLine($"  Floor {floor}: theme {theme.Definition.Id}");
                }
                else
                {
                    Check(theme == previousTheme, $"Floor {floor}: the theme stays the same for 5 floors");
                }

                previousTheme = theme;
                ValidateFloor(floor, layout, map, labdata, gameData, savegame, mod.State!);
                ValidateThemeFloor(floor, layout, map, theme, gameData);

                // Simulate recruiting: the recruit joins the party (if there is room).
                if (mod.State!.Recruit != 0)
                {
                    int slot = Array.IndexOf(party, 0u);

                    if (slot != -1)
                        party[slot] = mod.State.Recruit;
                }

                // Determinism: the same seed and floor result in the same layout.
                var again = FloorGenerator.Generate(mod.State.Seed, floor);
                Check(SameLayout(layout, again), $"Floor {floor}: layout is deterministic");

                // Simulate defeating the boss.
                if (layout.IsBossFloor)
                    mod.OnBattleEnded(FloorBuilderTestAccess.BossGroupIndex, true, false);
            }

            Check(party.Count(p => p != 0) > 1, "Recruits were offered");

            // Milestone quests (updated by the mod code)
            var quests = api.Quests;
            Check(quests.HasQuests, "Ambermoon DC has quests");
            Check(new[] { "floor10", "floor25", "floor50" }.All(s => quests.GetState(s) == UI.QuestState.Completed), "Depth milestones are completed");
            Check(quests.GetState("floor100") == UI.QuestState.Active && quests.GetAmount("floor100") == floors, "The next depth milestone shows the progress");
            Check(new[] { "boss1", "bossgroup", "boss5", "boss10" }.All(s => quests.GetState(s) == UI.QuestState.Completed), "Boss milestones are completed");
            Check(quests.GetState("recruit") == UI.QuestState.Active, "Companion quest waits for a companion");
            var entries = quests.GetEntries(language == "German" ? GameLanguage.German : GameLanguage.English);
            Check(entries.Count == 3 && entries[0].SubEntries[3].Text.EndsWith($"({floors}/100)"), "Quest log entries show the progress");
            Console.WriteLine($"  Quest log: {string.Join(" | ", entries.Select(e => e.Text + ": " + string.Join(", ", e.SubEntries.Where(s => s.State != UI.QuestState.Inactive).Select(s => s.Text))))}");
            var loaded = SavegameRoundTrip(gameData, savegame);

            if (loaded != null)
            {
                // Recruit texts are not part of the savegame. The mod restores them.
                var data = new ModSaveData();
                mod.OnSaving(data);
                var loadedMod = new AmbermoonDCMod { Api = api };
                loadedMod.OnInitialize();
                loadedMod.OnLoading(ModSaveData.FromJson(data.ToJson()));
                loadedMod.OnGameStarting(loaded, false);

                foreach (var index in loaded.CurrentPartyMemberIndices.Where(i => i > 1))
                {
                    var recruit = loaded.PartyMembers[index];
                    Check(recruit.Texts?.Count == 5, $"Texts of recruit {index} are restored");
                    Check(recruit.EventList.Any(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.LeaveParty),
                        $"Events of recruit {index} are stored");
                }

                Check(loadedMod.State!.Floor == mod.State!.Floor && loadedMod.CurrentLayout != null, "Floor is restored");
            }
        }
    }

    static class FloorBuilderTestAccess
    {
        public static uint MapIndex(int floor) => FloorBuilder.MapIndexForFloor(floor);
        public const uint BossGroupIndex = FloorBuilder.FirstMonsterGroupIndex;
    }

    static bool SameLayout(FloorLayout a, FloorLayout b)
    {
        if (a.Width != b.Width || a.Height != b.Height || a.Start != b.Start || a.Stairs != b.Stairs)
            return false;

        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
                if (a.Cells[x, y] != b.Cells[x, y])
                    return false;

        return a.Monsters.SequenceEqual(b.Monsters) && a.Chests.SequenceEqual(b.Chests);
    }

    static void ValidateFloor(int floor, FloorLayout layout, Map map, Labdata labdata, IGameData gameData, Savegame savegame, DCState state)
    {
        string prefix = $"Floor {floor}:";

        Check(map.EventList.Count <= 64, $"{prefix} at most 64 map events");
        Check(map.EventAutomapTypes.Count == map.EventList.Count, $"{prefix} automap types match the event list");
        Check(map.Events.All(e => map.EventList.Contains(e) || map.Events.Any(o => o.Next == e)), $"{prefix} all events are reachable");
        Check(map.Events.Select((e, i) => e.Index == i + 1).All(x => x), $"{prefix} event indices are set");

        // The map must be clonable (the game clones maps) which requires all raw data.
        try
        {
            map.Clone();
        }
        catch (Exception ex)
        {
            Check(false, $"{prefix} map clone failed: {ex.Message}");
        }

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var block = map.Blocks[x, y];
                Check(block.WallIndex <= labdata.Walls.Count, $"{prefix} valid wall at {x},{y}");
                Check(block.ObjectIndex <= labdata.Objects.Count, $"{prefix} valid object at {x},{y}");
                Check(block.MapEventId <= map.EventList.Count, $"{prefix} valid event at {x},{y}");
                Check(block.WallIndex == 0 || block.ObjectIndex == 0, $"{prefix} no wall and object at {x},{y}");
            }
        }

        // The border must be closed.
        for (int i = 0; i < map.Width; i++)
        {
            Check(map.Blocks[i, 0].WallIndex != 0 && map.Blocks[i, map.Height - 1].WallIndex != 0, $"{prefix} closed border");
            Check(map.Blocks[0, i].WallIndex != 0 && map.Blocks[map.Width - 1, i].WallIndex != 0, $"{prefix} closed border");
        }

        // Reachability (chests and obstacles block movement)
        var blocked = new HashSet<Pos>(layout.Chests.Select(c => c.Position).Concat(layout.Obstacles.Keys));
        var distances = layout.Distances(layout.Start, blocked);
        Check(distances[layout.Stairs.X, layout.Stairs.Y] > 0, $"{prefix} stairs are reachable");

        foreach (var chest in layout.Chests)
        {
            bool reachable = Pos.Directions.Any(d =>
            {
                var next = chest.Position + d;
                return layout.InBounds(next) && (distances[next.X, next.Y] >= 0 ||
                    (chest.Kind == ChestKind.Secret && layout[next] is CellType.Floor or CellType.SecretWall));
            });
            Check(reachable, $"{prefix} chest at {chest.Position} is reachable");
            Check(savegame.Chests.ContainsKey((uint)layout.Chests.IndexOf(chest) + 1), $"{prefix} chest content exists");
        }

        foreach (var monster in layout.Monsters)
            Check(distances[monster.Position.X, monster.Position.Y] >= 6, $"{prefix} monsters are not next to the start");

        // Characters
        var characters = map.CharacterReferences.Where(c => c != null).ToList();
        Check(characters.Count <= 32, $"{prefix} at most 32 characters");
        Check(map.CharacterReferences.TakeWhile(c => c != null).Count() == characters.Count, $"{prefix} characters have no gaps");

        foreach (var character in characters)
        {
            Check(character!.GraphicIndex >= 1 && character.GraphicIndex <= labdata.Objects.Count, $"{prefix} character graphic is valid");

            if (character.Type == CharacterType.Monster)
            {
                var group = gameData.CharacterManager.GetMonsterGroup(character.Index);
                Check(group != null && group.Monsters.Cast<Monster?>().Any(m => m != null), $"{prefix} monster group {character.Index} exists");
                Check(group == null || group.Monsters.Cast<Monster?>().All(m => m == null || gameData.CharacterManager.GetMonster(m.Index) != null),
                    $"{prefix} monsters have known indices (for the battle graphics)");
            }
            else if (character.Type == CharacterType.PartyMember)
            {
                Check(savegame.PartyMembers.ContainsKey(character.Index), $"{prefix} recruit exists in the savegame");
                var recruit = savegame.PartyMembers[character.Index];
                Check(recruit.SpokenLanguages.HasFlag(Language.Human), $"{prefix} recruit speaks the common language");
                Check(recruit.EventList.Any(e => e is ConversationEvent c && c.Interaction == ConversationEvent.InteractionType.JoinParty),
                    $"{prefix} recruit can join");
                Check(recruit.Level >= Math.Min(50, floor * 3 / 4) && recruit.HitPoints.CurrentValue > 0, $"{prefix} recruit has a fitting level and is alive");
                Check(character.Index >= 2 && character.Index <= 15, $"{prefix} recruit uses a party member slot");
                Check(recruit.Texts?.Count == 5 && recruit.Texts.All(t => !string.IsNullOrWhiteSpace(t)), $"{prefix} recruit has dialogues");
                Check(!string.IsNullOrWhiteSpace(recruit.Name) && recruit.PortraitIndex > 0, $"{prefix} recruit has a name and portrait");
                Check(!recruit.Class.IsMagic() || recruit.LearnedHealingSpells + recruit.LearnedAlchemisticSpells + recruit.LearnedMysticSpells + recruit.LearnedDestructionSpells != 0,
                    $"{prefix} magic recruits know spells");
                Console.WriteLine($"  Floor {floor}: recruit {recruit.Name}, {recruit.Gender} {recruit.Race} {recruit.Class} L{recruit.Level}, portrait {recruit.PortraitIndex}: {recruit.Texts![1]}");
            }
        }

        Check(layout.IsBossFloor == (layout.Boss != null), $"{prefix} bosses on every 5th floor");

        if (layout.IsBossFloor)
        {
            var boss = map.CharacterReferences[0]!;
            Check(boss.Type == CharacterType.Monster && boss.CharacterFlags == Map.CharacterReference.Flags.None, $"{prefix} the boss does not move");
            var bossGroup = gameData.CharacterManager.GetMonsterGroup(boss.Index)!;
            int count = bossGroup.Monsters.Cast<Monster?>().Count(m => m != null);
            Check(layout.IsBossWithMinionsFloor ? count > 1 : count == 1, $"{prefix} boss has {(layout.IsBossWithMinionsFloor ? "minions" : "no minions")}");
            Check(state.BossItem != 0, $"{prefix} the boss carries loot");
            Check(bossGroup.Monsters.Cast<Monster?>().Any(m => m?.Inventory.Slots.Any(s => s.ItemIndex == state.BossItem) == true),
                $"{prefix} the boss loot is in the inventory");
            Check(layout.Chests.Count(c => c.Kind == ChestKind.Treasure) > 0, $"{prefix} boss floors have treasure chests");
        }

        if (layout.IsRecruitFloor)
            Check(layout.Recruit != null, $"{prefix} a recruit is placed");

        // No curses on the first floors
        if (floor <= DCContent.LastFloorWithoutCurses)
        {
            foreach (var character in characters.Where(c => c!.Type == CharacterType.Monster))
            {
                var group = gameData.CharacterManager.GetMonsterGroup(character!.Index)!;
                Check(group.Monsters.Cast<Monster?>().All(m => m == null || (m.LearnedDestructionSpells & DCContent.CurseSpellBits) == 0),
                    $"{prefix} monsters can't cast curses");
            }
        }

        // Good equipment every 2 or 3 floors
        var gearChests = layout.Chests.Where(c => c.Kind == ChestKind.Gear).ToList();
        Check(gearChests.Count == (layout.IsGearFloor ? 1 : 0), $"{prefix} {(layout.IsGearFloor ? "one gear chest" : "no gear chest")}");

        foreach (var gearChest in gearChests)
        {
            var chest = savegame.Chests[(uint)layout.Chests.IndexOf(gearChest) + 1];
            var gear = chest.Slots.Cast<ItemSlot>().Where(s => !s.Empty && s.ItemIndex != 0)
                .Select(s => gameData.ItemManager.GetItem(s.ItemIndex)).FirstOrDefault(i => i.EquipmentSlot != EquipmentSlot.None);
            Check(gear != null, $"{prefix} the gear chest contains equipment");

            if (floor == 2)
            {
                Check(gear?.EquipmentSlot == EquipmentSlot.RightHand, $"{prefix} the first gear is a weapon");
                Console.WriteLine($"  Floor {floor}: gear {gear?.Name} (damage {gear?.Damage}, price {gear?.Price})");
            }
        }

        Check(layout.Chests.Any(c => c.Kind == ChestKind.Secret), $"{prefix} has a secret room", warnOnly: true);
    }

    static void ValidateThemeFloor(int floor, FloorLayout layout, Map map, DungeonTheme theme, IGameData gameData)
    {
        string prefix = $"Floor {floor}:";
        var blocked = new HashSet<Pos>(layout.Chests.Select(c => c.Position).Concat(layout.Obstacles.Keys));
        var distances = layout.Distances(layout.Start, blocked);

        Check(map.Blocks[layout.Stairs.X, layout.Stairs.Y].ObjectIndex == theme.ExitObject, $"{prefix} the exit object of the theme is used");
        Check(map.MusicIndex == (layout.IsBossFloor ? theme.Definition.BossMusic : theme.MusicForFloor(floor)), $"{prefix} the music of the theme is used");

        foreach (var obstacle in layout.Obstacles.Keys)
        {
            Check(layout[obstacle] == CellType.Floor && map.Blocks[obstacle.X, obstacle.Y].MapEventId == 0, $"{prefix} obstacle at {obstacle} is on a free cell");
            Check(theme.ObstacleObjects.Count == 0 || theme.ObstacleObjects.Contains(map.Blocks[obstacle.X, obstacle.Y].ObjectIndex), $"{prefix} obstacle of the theme is used");
        }

        if (theme.TeleporterObject != 0)
        {
            foreach (var teleporter in layout.Teleporters)
            {
                var block = map.Blocks[teleporter.Position.X, teleporter.Position.Y];
                Check(block.ObjectIndex == theme.TeleporterObject && block.MapEventId != 0, $"{prefix} teleporter at {teleporter.Position}");
                var teleport = block.MapEventId == 0 ? null : map.EventList[(int)block.MapEventId - 1] as TeleportEvent;
                Check(teleport != null && teleport.MapIndex == map.Index && teleport.X == teleporter.Target.X + 1 && teleport.Y == teleporter.Target.Y + 1,
                    $"{prefix} teleporter at {teleporter.Position} leads to its target");
                Check(distances[teleporter.Target.X, teleporter.Target.Y] > 0 && !layout.Teleporters.Any(t => t.Position == teleporter.Target),
                    $"{prefix} teleporter target {teleporter.Target} is reachable and no teleporter");
            }
        }

        var themeMonsterIndices = theme.Monsters.Select(m => m.Index).ToHashSet();

        foreach (var character in map.CharacterReferences.Where(c => c?.Type == CharacterType.Monster))
        {
            var group = gameData.CharacterManager.GetMonsterGroup(character!.Index)!;
            var monsters = group.Monsters.Cast<Monster?>().Where(m => m != null).Cast<Monster>().ToList();

            if (theme.Definition.Monsters.Count != 0)
            {
                Check(monsters.All(m => themeMonsterIndices.Contains(m.Index)), $"{prefix} only monsters of the theme");
                Check(monsters.All(m => m.Level >= floor - 1 && m.HitPoints.CurrentValue > 0 && m.BaseAttackDamage > 0),
                    $"{prefix} monster stats depend on the floor");
                Check(monsters.Count <= 6, $"{prefix} at most 6 monsters in a group");
            }
        }

        if (layout.IsBossFloor && (layout.IsBossWithMinionsFloor ? theme.Definition.BossGroup : (object?)theme.Definition.Boss) != null)
        {
            var bossGroup = gameData.CharacterManager.GetMonsterGroup(map.CharacterReferences[0]!.Index)!;
            var boss = bossGroup.Monsters.Cast<Monster?>().Where(m => m != null).MaxBy(m => m!.HitPoints.CurrentValue)!;
            string bossId = layout.IsBossWithMinionsFloor ? theme.Definition.BossGroup!.Boss : theme.Definition.Boss!.Monster;
            Check(boss.Index == theme.GetMonster(bossId).Index && boss.BattleFlags.HasFlag(BattleFlags.Boss), $"{prefix} the boss of the theme is used");

            if (floor == 5 || floor == 50)
                Console.WriteLine($"  Floor {floor}: boss {boss.Name} L{boss.Level} HP {boss.HitPoints.CurrentValue} ATK {boss.BaseAttackDamage} DEF {boss.BaseDefense} XP {boss.DefeatExperience}");
        }
    }

    static void ThemeTests()
    {
        Console.WriteLine("Ambermoon DC: theme definitions");

        static bool Fails(string json)
        {
            try
            {
                ThemeDefinition.Parse(json, "test");
                return false;
            }
            catch (Exception ex) when (ex is InvalidDataException or Newtonsoft.Json.JsonException)
            {
                return true;
            }
        }

        const string Objects = "\"Chest\": { \"Labdata\": 2, \"Object\": 25 }, \"Exit\": { \"Labdata\": 2, \"Object\": 20 }, \"Entrance\": { \"Texture\": 136, \"Width\": 32, \"Height\": 88 }";
        string valid = "{ \"Music\": [ 18 ], \"Walls\": [ { \"Texture\": 116 } ], " + Objects + " }";
        Check(!Fails(valid), "Minimal theme is valid");
        Check(ThemeDefinition.Parse(valid, "test").Id == "test", "Theme id defaults to the file name");
        Check(Fails("{ \"Music\": [ 18 ], \"Walls\": [], " + Objects + " }"), "Theme without walls is invalid");
        Check(Fails("{ \"Music\": [ 18 ], \"Walls\": [ { \"Texture\": 116 } ], " + Objects + ", \"Unknown\": 1 }"), "Unknown properties are invalid");
        Check(Fails("{ \"Music\": [ 18 ], \"Walls\": [ { \"Texture\": 116 } ], " + Objects + ", \"Boss\": { \"Monster\": \"x\" } }"), "Unknown boss is invalid");
        Check(Fails("{ \"Music\": [ 18 ], \"Walls\": [ { \"Texture\": 116 } ], " + Objects +
            ", \"Monsters\": [ { \"Id\": \"a\", \"Template\": 1, \"Name\": { \"en\": \"A\" }, \"Sprite\": { \"Labdata\": 2, \"Object\": 13 }, \"Spells\": [ \"Nope\" ] } ] }"),
            "Unknown spells are invalid");

        // Theme selection is deterministic and changes every 5 floors.
        for (int seed = 1; seed < 50; seed++)
        {
            for (int floor = 1; floor <= 60; floor++)
            {
                int theme = DungeonTheme.ThemeIndexForFloor(seed, floor, 4);
                Check(theme == DungeonTheme.ThemeIndexForFloor(seed, floor, 4), "Theme selection is deterministic");
                Check(theme is >= 0 and < 4, "Theme index is valid");

                if (floor % 5 != 1)
                    Check(theme == DungeonTheme.ThemeIndexForFloor(seed, floor - 1, 4), "Theme stays for 5 floors");
                else if (floor > 1)
                    Check(theme != DungeonTheme.ThemeIndexForFloor(seed, floor - 1, 4), "Theme changes after 5 floors");
            }
        }

        Check(Enumerable.Range(1, 200).Select(seed => DungeonTheme.ThemeIndexForFloor(seed, 1, 4)).Distinct().Count() == 4, "All themes are used");
    }

    static void Check(bool condition, string message, bool warnOnly)
    {
        if (!condition && warnOnly)
            Console.WriteLine($"  Note: not fulfilled: {message}");
        else
            Check(condition, message);
    }

    static Savegame? SavegameRoundTrip(GameData gameData, Savegame savegame)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ambermoon-dc-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var savegameManager = new SavegameManager(directory);
            var serializer = new SavegameSerializer();
            savegame.Automaps[savegame.CurrentMapIndex] = new Automap { ExplorationBits = new byte[64] };
            savegameManager.Save(gameData, serializer, 11, "DC test", savegame);
            var loaded = savegameManager.Load(gameData, serializer, 11, 30);

            Check(loaded != null, "Savegame can be loaded");

            if (loaded != null)
            {
                Check(loaded.CurrentMapIndex == savegame.CurrentMapIndex, "Map index is stored");
                Check(loaded.CurrentPartyMemberIndices.SequenceEqual(savegame.CurrentPartyMemberIndices), "Party is stored");
                Check(loaded.Chests[1].Gold == savegame.Chests[1].Gold, "Chest content is stored");
                Check(loaded.Automaps.ContainsKey(savegame.CurrentMapIndex), "Automap of the dungeon is stored");

                foreach (var index in savegame.CurrentPartyMemberIndices.Where(i => i != 0))
                    Check(loaded.PartyMembers[index].Level == savegame.PartyMembers[index].Level, $"Level of party member {index} is stored");
            }

            return loaded;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
