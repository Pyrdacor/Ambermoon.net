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

            var labdata = gameData.MapManager.GetLabdataForMap(gameData.MapManager.GetMap(savegame.CurrentMapIndex)!);
            Check(savegame.CurrentMapIndex == 601, "Floor 1 uses map 601");
            Check(savegame.GetPartyMember(0)!.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex != 0, "Hero has a weapon");
            Check(savegame.GetPartyMember(0)!.Inventory.Slots.Any(s => s.ItemIndex == DCContent.Torch), "Hero has torches");

            var party = savegame.CurrentPartyMemberIndices;

            for (int floor = 1; floor <= floors; floor++)
            {
                if (floor > 1)
                    mod.EnterFloor(savegame, floor);

                var layout = mod.CurrentLayout!;
                var map = gameData.MapManager.GetMap(FloorBuilderTestAccess.MapIndex(floor))!;
                ValidateFloor(floor, layout, map, labdata, gameData, savegame, mod.State!);

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
            }

            Check(party.Count(p => p != 0) > 1, "Recruits were offered");
            SavegameRoundTrip(gameData, savegame);
        }
    }

    static class FloorBuilderTestAccess
    {
        public static uint MapIndex(int floor) => FloorBuilder.MapIndexForFloor(floor);
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

        // Reachability (chests block movement)
        var blocked = new HashSet<Pos>(layout.Chests.Select(c => c.Position));
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
                Check(recruit.Level >= 1 && recruit.HitPoints.CurrentValue > 0, $"{prefix} recruit is alive");
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

        Check(layout.Chests.Any(c => c.Kind == ChestKind.Secret), $"{prefix} has a secret room", warnOnly: true);
    }

    static void Check(bool condition, string message, bool warnOnly)
    {
        if (!condition && warnOnly)
            Console.WriteLine($"  Note: not fulfilled: {message}");
        else
            Check(condition, message);
    }

    static void SavegameRoundTrip(GameData gameData, Savegame savegame)
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
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
