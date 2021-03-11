using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using static Ambermoon.Data.Tileset;

namespace Ambermoon.Data
{
    public class Labdata
    {
        public struct ObjectPosition
        {
            public short X;
            public short Y;
            public short Z;
            public ObjectInfo Object;
        }

        public struct ObjectInfo
        {
            public TileFlags Flags;
            public uint TextureIndex;
            public uint NumAnimationFrames;
            public byte Unknown2; // TODO: decode this (is this even used in Ambermoon?)
            public uint TextureWidth;
            public uint TextureHeight;
            public uint MappedTextureWidth;
            public uint MappedTextureHeight;
        }

        public struct Object
        {
            public AutomapType AutomapType;
            public List<ObjectPosition> SubObjects;
        }

        public struct OverlayData
        {
            public bool Blend;
            public uint TextureIndex;
            public uint PositionX;
            public uint PositionY;
            public uint TextureWidth;
            public uint TextureHeight;
        }

        public struct WallData
        {
            public TileFlags Flags;
            public uint TextureIndex;
            public AutomapType AutomapType;
            public byte Unknown2;
            public OverlayData[] Overlays;

            public override string ToString()
            {
                string content = $"Flags: {Flags.ToString().Replace(", ", "|")}(0x{(uint)Flags:x8}), Texture: {TextureIndex}, AutomapType: {AutomapType}, Overlays: {(Overlays == null ? 0 : Overlays.Length)}";

                if (Overlays != null && Overlays.Length != 0)
                {
                    for (int o = 0; o < Overlays.Length; ++o)
                    {
                        var overlay = Overlays[o];
                        content += $"\n\t\tOverlay{o + 1} -> Texture: {overlay.TextureIndex} ({overlay.TextureWidth}x{overlay.TextureHeight}), Position: {overlay.PositionX}:{overlay.PositionY}, Blend {overlay.Blend}";
                    }
                }

                return content;
            }
        }

        /// <summary>
        /// The floor dimension (tile width/height) seems to be considered as 250.
        /// So if this value is 250 as well, the wall's height is exactly a tile
        /// width and therefore each map block is a cube. If the value would be
        /// 500, a wall would be twice as height as a tile width, etc.
        /// </summary>
        public uint WallHeight { get; set; }
        /// <summary>
        /// There are 16 combat background sets.
        /// See <see cref="CombatBackgrounds"/>.
        /// </summary>
        public uint CombatBackground { get; set; }
        public byte Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }
        public List<Object> Objects { get; } = new List<Object>();
        public List<ObjectInfo> ObjectInfos { get; } = new List<ObjectInfo>();
        public List<WallData> Walls { get; } = new List<WallData>();

        public List<Graphic> ObjectGraphics { get; } = new List<Graphic>();
        /// <summary>
        /// They include optional overlays.
        /// </summary>
        public List<Graphic> WallGraphics { get; } = new List<Graphic>();
        public Graphic FloorGraphic { get; set; } = null;
        public Graphic CeilingGraphic { get; set; } = null;

        private Labdata()
        {

        }

        public static Labdata Load(ILabdataReader labdataReader, IDataReader dataReader, IGameData gameData)
        {
            var labdata = new Labdata();

            labdataReader.ReadLabdata(labdata, dataReader, gameData);

            return labdata;
        }
    }
}
