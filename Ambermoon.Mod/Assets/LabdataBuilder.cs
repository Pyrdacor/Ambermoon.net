using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using static Ambermoon.Data.Tileset;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// Overlay of a wall texture.
/// </summary>
public record WallOverlay(uint Texture, uint X, uint Y, uint Width, uint Height, bool Blend = false);

/// <summary>
/// Builds new 3D labdata from the global 3D textures of the base game
/// (Floors.amb, 2/3Wall3D.amb, 2/3Overlay3D.amb and 2/3Object3D.amb).
///
/// All 3D textures use the map palette, so walls and objects of different
/// original labdata can be combined freely.
/// </summary>
public class LabdataBuilder
{
    readonly ModApi api;
    readonly GraphicReader graphicReader = new();
    readonly TileFlags combatBackgroundFlags;

    public Labdata Labdata { get; }

    public LabdataBuilder(ModApi api, uint combatBackground, uint wallHeight = 300)
    {
        this.api = api;
        combatBackgroundFlags = (TileFlags)((combatBackground & 0xf) << 28);
        Labdata = new Labdata
        {
            WallHeight = wallHeight,
            CombatBackground = combatBackground & 0xf,
            Flags = (ushort)(combatBackground & 0xf)
        };
    }

    ILegacyGameData GameData => api.GameData as ILegacyGameData
        ?? throw new NotSupportedException("3D textures can only be loaded from the legacy game data.");

    /// <summary>
    /// Returns the texture with the given index from the first file which provides it.
    /// The original game splits the textures over the files "2..." and "3...".
    /// </summary>
    IDataReader GetTexture(string name, uint index)
    {
        foreach (var prefix in new[] { "2", "3" })
        {
            if (GameData.Files.TryGetValue(prefix + name, out var container) &&
                container.Files.TryGetValue((int)index, out var file) && file.Size != 0)
                return file;
        }

        throw new KeyNotFoundException($"There is no texture {index} in {name}.");
    }

    Graphic ReadGraphic(IDataReader file, int width, int height, bool alpha, bool texture = true)
    {
        var graphic = new Graphic
        {
            Width = width,
            Height = height,
            IndexedGraphic = true
        };

        file.Position = 0;
        graphicReader.ReadGraphic(graphic, file, new GraphicInfo
        {
            Width = width,
            Height = height,
            GraphicFormat = texture ? GraphicFormat.Texture4Bit : GraphicFormat.Palette4Bit,
            PaletteOffset = 0,
            Alpha = alpha
        });

        return graphic;
    }

    /// <summary>
    /// Sets the floor texture (index into Floors.amb, 0 = only use the color).
    /// </summary>
    public void SetFloor(byte textureIndex, byte colorIndex)
    {
        Labdata.FloorTextureIndex = textureIndex;
        Labdata.FloorColorIndex = colorIndex;
        Labdata.FloorGraphic = textureIndex == 0 ? null : ReadGraphic(GameData.Files["Floors.amb"].Files[textureIndex], 64, 64, false, false);
    }

    /// <summary>
    /// Sets the ceiling texture (index into Floors.amb, 0 = only use the color).
    /// </summary>
    public void SetCeiling(byte textureIndex, byte colorIndex)
    {
        Labdata.CeilingTextureIndex = textureIndex;
        Labdata.CeilingColorIndex = colorIndex;
        Labdata.CeilingGraphic = textureIndex == 0 ? null : ReadGraphic(GameData.Files["Floors.amb"].Files[textureIndex], 64, 64, false, false);
    }

    /// <summary>
    /// Adds a wall and returns its 1-based index (the value of Map.Block.WallIndex).
    /// </summary>
    /// <param name="texture">Index into Wall3D.amb (128x80).</param>
    /// <param name="flags">For example BlockAllMovement | BlockSight for a normal wall
    /// or BlockSight | AllowMovementWalk for a wall the player can walk through.</param>
    /// <param name="colorIndex">Color which is used on the automap.</param>
    public uint AddWall(uint texture, TileFlags flags, AutomapType automapType, byte colorIndex, IEnumerable<WallOverlay>? overlays = null)
    {
        var overlayList = overlays?.ToList() ?? [];
        var wall = new Labdata.WallData
        {
            Flags = flags | combatBackgroundFlags,
            TextureIndex = texture,
            AutomapType = automapType,
            ColorIndex = colorIndex,
            Overlays = overlayList.Count == 0 ? null! : [.. overlayList.Select(o => new Labdata.OverlayData
            {
                Blend = o.Blend,
                TextureIndex = o.Texture,
                PositionX = o.X,
                PositionY = o.Y,
                TextureWidth = o.Width,
                TextureHeight = o.Height
            })]
        };
        var graphic = ReadGraphic(GetTexture("Wall3D.amb", texture), 128, 80, flags.HasFlag(TileFlags.Transparency));

        foreach (var overlay in overlayList)
        {
            graphic.AddOverlay(overlay.X, overlay.Y, ReadGraphic(GetTexture("Overlay3D.amb", overlay.Texture),
                (int)overlay.Width, (int)overlay.Height, true), overlay.Blend);
        }

        Labdata.Walls.Add(wall);
        Labdata.WallGraphics.Add(graphic);

        return (uint)Labdata.Walls.Count;
    }

    /// <summary>
    /// Copies an object of an original labdata (with all its sub objects) and
    /// returns the 1-based index of the new object (the value of Map.Block.ObjectIndex).
    /// </summary>
    /// <param name="flagsOverride">If given, the flags of all sub objects are replaced (except for the animation flags).</param>
    public uint CopyObject(uint labdataIndex, uint objectIndex, TileFlags? flagsOverride = null)
    {
        var source = api.GetLabdata(labdataIndex);

        if (objectIndex < 1 || objectIndex > source.Objects.Count)
            throw new KeyNotFoundException($"Labdata {labdataIndex} has no object {objectIndex}.");

        var sourceObject = source.Objects[(int)objectIndex - 1];
        var subObjects = sourceObject.SubObjects.Select(subObject =>
        {
            var info = subObject.Object;
            var graphic = source.ObjectGraphics[source.ObjectInfos.IndexOf(info)];
            info.Flags = AdjustFlags(info.Flags, flagsOverride);
            return (subObject, info, graphic);
        });

        return AddObject(sourceObject.AutomapType, subObjects.Select(s => new Labdata.ObjectPosition
        {
            X = s.subObject.X,
            Y = s.subObject.Y,
            Z = s.subObject.Z,
            Object = s.info
        }).ToList(), subObjects.Select(s => s.graphic).ToList());
    }

    /// <summary>
    /// Adds an object which consists of a single texture in the center of the block and
    /// returns its 1-based index (the value of Map.Block.ObjectIndex).
    /// </summary>
    /// <param name="texture">Index into Object3D.amb.</param>
    /// <param name="width">Texture width (multiple of 16).</param>
    /// <param name="height">Texture height.</param>
    /// <param name="mappedWidth">Size of the object in the 3D world (a block is 512 wide). Default is 3 times the width.</param>
    /// <param name="mappedHeight">Size of the object in the 3D world. Default is 3 times the height.</param>
    /// <param name="frames">Number of animation frames.</param>
    /// <param name="z">Height above the floor.</param>
    public uint AddObject(uint texture, uint width, uint height, TileFlags flags, AutomapType automapType = AutomapType.None,
        uint? mappedWidth = null, uint? mappedHeight = null, uint frames = 1, short z = 0)
    {
        var info = new Labdata.ObjectInfo
        {
            Flags = flags | combatBackgroundFlags,
            TextureIndex = texture,
            NumAnimationFrames = Math.Max(1, frames),
            TextureWidth = width,
            TextureHeight = height,
            MappedTextureWidth = mappedWidth ?? width * 3,
            MappedTextureHeight = mappedHeight ?? height * 3
        };
        var file = GetTexture("Object3D.amb", texture);
        Graphic graphic;

        if (info.NumAnimationFrames == 1)
        {
            graphic = ReadGraphic(file, (int)width, (int)height, true);
        }
        else
        {
            // All frames are stored one after another and are put side by side.
            graphic = new Graphic((int)(info.NumAnimationFrames * width), (int)height, 0);
            file.Position = 0;

            for (uint i = 0; i < info.NumAnimationFrames; i++)
            {
                var frame = new Graphic { Width = (int)width, Height = (int)height, IndexedGraphic = true };
                graphicReader.ReadGraphic(frame, file, new GraphicInfo
                {
                    Width = (int)width,
                    Height = (int)height,
                    GraphicFormat = GraphicFormat.Texture4Bit,
                    PaletteOffset = 0,
                    Alpha = true
                });
                graphic.AddOverlay(i * width, 0u, frame, false);
            }
        }

        return AddObject(automapType, [new Labdata.ObjectPosition { X = 255, Y = 255, Z = z, Object = info }], [graphic]);
    }

    TileFlags AdjustFlags(TileFlags flags, TileFlags? flagsOverride)
    {
        const TileFlags KeptFlags = TileFlags.WaveAnimation | TileFlags.RandomAnimationStart | TileFlags.Floor | TileFlags.No3DAnimation;

        if (flagsOverride != null)
            flags = (flags & KeptFlags) | flagsOverride.Value;

        // Replace the combat background of the source labdata.
        return (flags & (TileFlags)0x0fffffff) | combatBackgroundFlags;
    }

    uint AddObject(AutomapType automapType, List<Labdata.ObjectPosition> subObjects, List<Graphic> graphics)
    {
        for (int i = 0; i < subObjects.Count; i++)
        {
            // The renderer finds the graphic by the index of the object info,
            // so equal infos must share the same graphic.
            if (!Labdata.ObjectInfos.Contains(subObjects[i].Object))
            {
                Labdata.ObjectInfos.Add(subObjects[i].Object);
                Labdata.ObjectGraphics.Add(graphics[i]);
            }
        }

        Labdata.Objects.Add(new Labdata.Object
        {
            AutomapType = automapType,
            SubObjects = subObjects
        });

        return (uint)Labdata.Objects.Count;
    }
}
