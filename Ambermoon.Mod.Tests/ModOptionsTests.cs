namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    static void ModOptionsTests()
    {
        Console.WriteLine("Mod option defaults");

        static bool Fails(string options)
        {
            try
            {
                ModManifest.Parse($$"""{ "Id": "test", "Name": "Test", "Options": {{options}} }""");
                return false;
            }
            catch (InvalidDataException)
            {
                return true;
            }
        }

        Check(!Fails("""{ "AutoBattleRounds": 5, "ShowFloor": true, "Movement3D": "WASDQE" }"""), "Valid options are accepted");
        Check(Fails("""{ "Music": false }"""), "Options like music can't be set by mods");
        Check(Fails("""{ "Width": 640 }"""), "The resolution can't be set by mods");
        Check(Fails("""{ "ShowFloor": 3 }"""), "Option values must have the right type");
        Check(Fails("""{ "Movement3D": "Fly" }"""), "Enum values must exist");

        var file = Path.Combine(Path.GetTempPath(), "ambermoon-mod-options-" + Guid.NewGuid().ToString("N"), "test.json");

        try
        {
            var manifest = ModManifest.Parse("""{ "Id": "test", "Name": "Test", "Options": { "AutoBattleRounds": 5, "ShowFog": true } }""");
            var options = ModOptions.Load(manifest, file);
            Check(options.TryGet<int>("AutoBattleRounds", out var rounds) && rounds == 5, "Mod defaults are used");
            Check(!options.TryGet<bool>("ShowFloor", out _), "Other options come from the normal configuration");
            Check(File.Exists(file), "Mod defaults are stored in the mod config");

            // The player changes an option: the mod default is removed for good.
            options.Remove("AutoBattleRounds");
            Check(!options.TryGet<int>("AutoBattleRounds", out _), "Changed options are removed from the mod config");
            options = ModOptions.Load(manifest, file);
            Check(!options.TryGet<int>("AutoBattleRounds", out _), "Changed options stay removed");
            Check(options.TryGet<bool>("ShowFog", out var fog) && fog, "Unchanged options stay");

            // A newer mod version adds a default.
            var newerManifest = ModManifest.Parse("""{ "Id": "test", "Name": "Test", "Options": { "AutoBattleRounds": 10, "ShowFog": true, "ShowFloor": true } }""");
            options = ModOptions.Load(newerManifest, file);
            Check(options.TryGet<bool>("ShowFloor", out var floor) && floor, "New defaults of a mod update are added");
            Check(!options.TryGet<int>("AutoBattleRounds", out _), "Defaults the player changed are not added again");
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(file)!, true);
        }
    }
}
