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
            public byte[] Unknown1;
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
            None = 0,
            BlockSight = 0x02, // Not sure but beside walls this is also used by non-bocking doors or exits
            Transparency = 0x08,
            BlockMovement = 0x80,
            // TODO
        }

        [Flags]
        public enum ObjectFlags
        {
            None = 0,
            FloorObject = 0x08, // like holes in the ground
            BlockMovement = 0x80,
            // TODO
        }

        public enum AutomapType
        {
            None = 0, // empty / no automap symbol
            Wall = 1,
            Riddlemouth = 2,
            DoorClosed = 9,
            DoorOpen = 10,
            Exit = 14,
            // TODO: fake wall? secret door?
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
