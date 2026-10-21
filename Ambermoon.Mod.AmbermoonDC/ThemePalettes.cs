using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Finds out which map palettes fit the textures of a theme. All 3D textures use the
/// palette of the map, so a texture only looks right with the palettes it was made for.
/// The original maps tell which palettes these are. Palettes whose first 16 colors
/// (the only ones the 3D textures use) are nearly the same also fit.
/// </summary>
internal static class ThemePalettes
{
    /// <summary>
    /// Maximum difference of a color (sum of the differences of the red, green and blue
    /// value, each 0 to 15) for a palette to be similar to another one.
    /// </summary>
    public const int MaxColorDifference = 5;

    public enum TextureKind
    {
        Wall,
        Overlay,
        Object,
        FloorOrCeiling
    }

    public record struct Texture(TextureKind Kind, uint Index)
    {
        public override readonly string ToString() => $"{Kind} {Index}";
    }

    /// <summary>
    /// Palettes which the original 3D maps use for each texture.
    /// </summary>
    public static Dictionary<Texture, SortedSet<uint>> OriginalUsage(IGameData gameData)
    {
        var usage = new Dictionary<Texture, SortedSet<uint>>();

        foreach (var map in gameData.MapManager.Maps.Where(m => m.Type == MapType.Map3D && m.TilesetOrLabdataIndex < DungeonTheme.FirstLabdataIndex))
        {
            Labdata labdata;

            try
            {
                labdata = gameData.MapManager.GetLabdataForMap(map);
            }
            catch (KeyNotFoundException)
            {
                continue;
            }

            foreach (var texture in Textures(labdata))
            {
                if (!usage.TryGetValue(texture, out var palettes))
                    usage.Add(texture, palettes = []);

                palettes.Add(map.PaletteIndex);
            }
        }

        return usage;
    }

    static IEnumerable<Texture> Textures(Labdata labdata)
    {
        if (labdata.FloorTextureIndex != 0)
            yield return new(TextureKind.FloorOrCeiling, labdata.FloorTextureIndex);

        if (labdata.CeilingTextureIndex != 0)
            yield return new(TextureKind.FloorOrCeiling, labdata.CeilingTextureIndex);

        foreach (var wall in labdata.Walls)
        {
            yield return new(TextureKind.Wall, wall.TextureIndex);

            foreach (var overlay in wall.Overlays ?? [])
                yield return new(TextureKind.Overlay, overlay.TextureIndex);
        }

        foreach (var info in labdata.ObjectInfos)
            yield return new(TextureKind.Object, info.TextureIndex);
    }

    /// <summary>
    /// The first 16 colors (as 4 bit RGB) of all palettes of the game.
    /// </summary>
    public static Dictionary<uint, (int R, int G, int B)[]> TextureColors(ILegacyGameData gameData)
    {
        var palettes = new Dictionary<uint, (int, int, int)[]>();

        foreach (var file in gameData.Files["Palettes.amb"].Files.Where(f => f.Value.Size != 0))
        {
            var reader = file.Value;
            reader.Position = 0;
            var colors = new (int, int, int)[16];

            for (int i = 0; i < 16; i++)
            {
                int color = reader.ReadWord();
                colors[i] = ((color >> 8) & 0xf, (color >> 4) & 0xf, color & 0xf);
            }

            palettes.Add((uint)file.Key, colors);
        }

        return palettes;
    }

    public static bool AreSimilar((int R, int G, int B)[] a, (int R, int G, int B)[] b) =>
        a.Zip(b).All(c => Math.Abs(c.First.R - c.Second.R) + Math.Abs(c.First.G - c.Second.G) + Math.Abs(c.First.B - c.Second.B) <= MaxColorDifference);

    /// <summary>
    /// All textures of a theme with a description where they are used.
    /// </summary>
    public static IEnumerable<(string Usage, Texture Texture)> Textures(ThemeDefinition theme, Func<uint, Labdata> getLabdata)
    {
        if (theme.FloorTexture != 0)
            yield return ("floor", new(TextureKind.FloorOrCeiling, theme.FloorTexture));

        if (theme.CeilingTexture != 0)
            yield return ("ceiling", new(TextureKind.FloorOrCeiling, theme.CeilingTexture));

        foreach (var wall in theme.Walls)
        {
            yield return ("wall", new(TextureKind.Wall, wall.Texture));

            foreach (var overlay in wall.Overlays)
                yield return ("wall overlay", new(TextureKind.Overlay, overlay.Texture));
        }

        var door = theme.Door ?? DungeonTheme.DefaultDoor;
        yield return ("door", new(TextureKind.Wall, door.Closed));

        if (door.Open != 0)
            yield return ("open door", new(TextureKind.Wall, door.Open));

        IEnumerable<(string, Texture)> ObjectTextures(string usage, ThemeObject? obj)
        {
            if (obj == null)
                yield break;

            if (!obj.IsCopy)
            {
                yield return (usage, new(TextureKind.Object, obj.Texture));
                yield break;
            }

            var source = getLabdata(obj.Labdata);

            foreach (var subObject in source.Objects[(int)obj.Object - 1].SubObjects)
                yield return ($"{usage} (labdata {obj.Labdata} object {obj.Object})", new(TextureKind.Object, subObject.Object.TextureIndex));
        }

        var objects = new List<(string, ThemeObject?)>
        {
            ("chest", theme.Chest),
            ("exit", theme.Exit),
            ("entrance", theme.Entrance),
            ("teleporter", theme.Teleporter),
            ("recruit", theme.RecruitSprite ?? DungeonTheme.DefaultRecruitSprite)
        };
        objects.AddRange(theme.Decorations.Select(d => ("decoration", (ThemeObject?)d)));
        objects.AddRange(theme.Obstacles.Select(o => ("obstacle", (ThemeObject?)o)));
        objects.AddRange(theme.Monsters.Select(m => ($"monster {m.Id}", (ThemeObject?)m.Sprite)));

        if (DungeonTheme.NeedsGlobalSprites(theme))
        {
            objects.AddRange(DCContent.Sprites.Select(s => ($"global sprite {s.Key}",
                (ThemeObject?)new ThemeObject { Labdata = s.Value.Labdata, Object = s.Value.Object })));
        }

        foreach (var (usage, obj) in objects)
        {
            foreach (var texture in ObjectTextures(usage, obj))
                yield return texture;
        }
    }

    public record Analysis(SortedSet<uint> OriginalPalettes, SortedSet<uint> SimilarPalettes,
        List<(string Usage, Texture Texture, SortedSet<uint> Palettes)> Textures);

    /// <summary>
    /// Palettes which fit all textures of the theme:
    /// - OriginalPalettes: the original game uses every texture of the theme with these palettes.
    /// - SimilarPalettes: for every texture, the palette is similar to one the original game uses it with
    ///   (includes the original palettes).
    /// Also returns the palettes of each texture.
    /// </summary>
    public static Analysis Analyze(ThemeDefinition theme, Func<uint, Labdata> getLabdata,
        Dictionary<Texture, SortedSet<uint>> originalUsage, Dictionary<uint, (int R, int G, int B)[]> textureColors)
    {
        var original = new SortedSet<uint>(textureColors.Keys);
        var similar = new SortedSet<uint>(textureColors.Keys);
        var textures = new List<(string, Texture, SortedSet<uint>)>();

        foreach (var (usage, texture) in Textures(theme, getLabdata).DistinctBy(t => t.Texture))
        {
            var used = originalUsage.TryGetValue(texture, out var p) ? p : [];
            textures.Add((usage, texture, used));
            original.IntersectWith(used);
            similar.RemoveWhere(palette => !used.Any(u => textureColors.TryGetValue(u, out var colors) && AreSimilar(textureColors[palette], colors)));
        }

        return new(original, similar, textures);
    }
}
