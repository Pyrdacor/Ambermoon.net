using Ambermoon.Data;
using Ambermoon.Mod.AmbermoonDC;
using Ambermoon.Mod.Packaging;

namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    static List<(ThemeDefinition Theme, ThemePalettes.Analysis Analysis)> AnalyzeThemePalettes()
    {
        var gameData = BaseGameLoader.Load("English");
        var usage = ThemePalettes.OriginalUsage(gameData);
        var colors = ThemePalettes.TextureColors(gameData);
        Labdata GetLabdata(uint index) => gameData.MapManager.GetLabdataForMap(new Map { Type = MapType.Map3D, TilesetOrLabdataIndex = index });
        using var source = new FolderModSource(FindDCModFolder());

        return [.. source.ListFiles("themes").Select(file =>
        {
            var theme = ThemeDefinition.Parse(source.ReadAllText(file), Path.GetFileNameWithoutExtension(file));
            return (theme, ThemePalettes.Analyze(theme, GetLabdata, usage, colors));
        })];
    }

    /// <summary>
    /// Every palette of a theme must fit all its textures.
    /// </summary>
    static void ThemePaletteTests()
    {
        Console.WriteLine("Ambermoon DC: theme palettes");

        foreach (var (theme, analysis) in AnalyzeThemePalettes())
        {
            foreach (var palette in theme.Palettes)
                Check(analysis.SimilarPalettes.Contains(palette), $"Theme {theme.Id}: palette {palette} fits all textures");
        }
    }

    /// <summary>
    /// Prints which palettes fit the textures of each theme.
    /// </summary>
    static void PrintThemePalettes(bool details)
    {
        Console.WriteLine($"Original: the original game uses all textures with this palette.");
        Console.WriteLine($"Similar: for each texture, the first 16 colors differ by at most {ThemePalettes.MaxColorDifference} from a palette the original game uses it with.");

        foreach (var (theme, analysis) in AnalyzeThemePalettes())
        {
            Console.WriteLine($"{theme.Id}: original {string.Join(", ", analysis.OriginalPalettes)}, similar {string.Join(", ", analysis.SimilarPalettes)}" +
                $" (theme uses {string.Join(", ", theme.Palettes)})");

            if (details)
            {
                foreach (var texture in analysis.Textures)
                    Console.WriteLine($"  {texture.Texture,-22} {string.Join(",", texture.Palettes),-24} {texture.Usage}");
            }
        }
    }
}
