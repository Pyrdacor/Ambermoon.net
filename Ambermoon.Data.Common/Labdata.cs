using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class Labdata
    {
        public struct ObjectPosition
        {
            public float X;
            public float Y;
            public float Z;
            public ObjectInfo Object;
        }

        public struct ObjectInfo
        {
            public uint CollisionRadius; // in x/z direction 
            public byte Unknown1;
            public uint ExtrudeOffset; // Move this amount to viewer (or up if floor object)
            public ObjectFlags Flags;
            public uint TextureIndex;
            public uint NumAnimationFrames;
            public byte Unknown2;
            public uint TextureWidth;
            public uint TextureHeight;
            public uint MappedTextureWidth;
            public uint MappedTextureHeight;
        }

        public struct Object
        {
            public ushort Header; // TODO: decode this
            public List<ObjectPosition> SubObjects;
        }

        public struct OverlayData
        {
            public uint NumAnimationFrames;
            public uint TextureIndex;
            public uint PositionX;
            public uint PositionY;
            public uint TextureWidth;
            public uint TextureHeight;
        }

        [Flags]
        public enum WallFlags
        {
            // Note: Only the mentioned 3 bits/flags are used in Ambermoon.
            None = 0,
            BlockSight = 0x02, // Not sure but beside walls this is also used by non-bocking doors or exits
            Transparency = 0x08,
            BlockMovement = 0x80,
        }

        [Flags]
        public enum ObjectFlags
        {
            None = 0,
            FloorObject = 0x08, // like holes in the ground
            BlockMovement = 0x80,
            // TODO
        }

        public struct WallData
        {
            public byte[] Unknown1;
            public WallFlags Flags;
            public uint TextureIndex;
            public AutomapType AutomapType;
            public byte Unknown2;
            public OverlayData[] Overlays;

            public override string ToString()
            {
                string content = $"Flags: {Flags.ToString().Replace(", ", "|")}(0x{(uint)Flags:x2}), Texture: {TextureIndex}, AutomapType: {AutomapType}, Overlays: {(Overlays == null ? 0 : Overlays.Length)}";

                if (Overlays != null && Overlays.Length != 0)
                {
                    for (int o = 0; o < Overlays.Length; ++o)
                    {
                        var overlay = Overlays[o];
                        content += $"\n\t\tOverlay{o + 1} -> Texture: {overlay.TextureIndex} ({overlay.TextureWidth}x{overlay.TextureHeight}), Position: {overlay.PositionX}:{overlay.PositionY}, Frames {overlay.NumAnimationFrames}";
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
