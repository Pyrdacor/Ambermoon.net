using Ambermoon.Data;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon.Mod.Tests;

/// <summary>
/// Headless tests for the mod system and the Ambermoon DC mod.
/// They need the builtin game data (Ambermoon.net/versions.dat).
///
/// Usage: dotnet run --project Ambermoon.Mod.Tests [all|assets|dc|probe] [floors]
/// </summary>
static partial class Tests
{
    static int failures = 0;

    static void Check(bool condition, string message)
    {
        if (!condition)
        {
            failures++;
            Console.WriteLine($"  FAILED: {message}");
        }
    }

    public static int Run(string command, string[] args)
    {
        try
        {
            switch (command)
            {
                case "probe":
                    Probe();
                    return 0;
                case "render":
                    RenderFloor(int.Parse(args[0]), args.Length > 1 ? int.Parse(args[1]) : 12345);
                    return 0;
                case "assets":
                    AssetModTests();
                    break;
                case "dc":
                    DungeonCrawlerTests(args.Length > 0 ? int.Parse(args[0]) : 60);
                    break;
                case "all":
                    ManifestTests();
                    AssetModTests();
                    DungeonCrawlerTests(60);
                    break;
                default:
                    Console.WriteLine($"Unknown command {command}");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected exception: {ex}");
            return 2;
        }

        Console.WriteLine(failures == 0 ? "All tests passed." : $"{failures} check(s) failed.");

        return failures == 0 ? 0 : 1;
    }

    static void Probe()
    {
        var gameData = BaseGameLoader.Load("English");
        var savegame = new SavegameManager(Path.GetTempPath()).LoadInitial(gameData, new SavegameSerializer());
        Console.WriteLine($"Initial map {savegame.CurrentMapIndex} {savegame.CurrentMapX},{savegame.CurrentMapY} party={string.Join(",", savegame.CurrentPartyMemberIndices)}");
        Console.WriteLine($"Chests: {string.Join(",", savegame.Chests.Keys.Take(30))} ... count {savegame.Chests.Count} max {savegame.Chests.Keys.Max()}");

        foreach (var pm in savegame.PartyMembers.OrderBy(p => p.Key))
        {
            var p = pm.Value;
            Console.WriteLine($"P{pm.Key} {p.Name} {p.Race} {p.Class} L{p.Level} lang={p.SpokenLanguages} bit=0x{p.CharacterBitIndex:x4} events={p.Events.Count} list={p.EventList.Count}");

            foreach (var e in p.Events)
                Console.WriteLine($"    {e}");
        }
    }

    static void ManifestTests()
    {
        Console.WriteLine("Manifest tests");

        var manifest = ModManifest.Parse("""{ "Id": "test-mod", "Name": "Test", "Version": "1.2", "Base": { "Game": "Advanced" } }""");
        Check(manifest.ParsedVersion == new ModVersion(1, 2), "Version is parsed");
        Check(!manifest.SupportsBaseGame(false, "1.20", out _), "Advanced mods don't run on the original");
        Check(manifest.SupportsBaseGame(true, "1.33", out _), "Advanced mods run on Advanced");

        bool failed = false;

        try
        {
            ModManifest.Parse("""{ "Id": "bad id!", "Name": "Test" }""");
        }
        catch (InvalidDataException)
        {
            failed = true;
        }

        Check(failed, "Invalid ids are rejected");

        var minVersion = ModManifest.Parse("""{ "Id": "x", "Name": "X", "Base": { "MinVersion": "1.20" } }""");
        Check(!minVersion.SupportsBaseGame(false, "1.19", out _), "Min version is checked");
        Check(minVersion.SupportsBaseGame(false, "1.20", out _), "Min version accepts equal versions");

        var data = new ModSaveData();
        data.Set("a", 42);
        data.SetObject("b", new List<uint> { 1, 2 });
        var loaded = ModSaveData.FromJson(data.ToJson());
        Check(loaded.GetInt("a") == 42 && loaded.GetObject<List<uint>>("b")!.SequenceEqual([1u, 2u]), "Save data round trip");
    }
}
