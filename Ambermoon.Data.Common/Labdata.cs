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
            public byte Type;
            public byte[] Collision;
            public uint TextureIndex;
            public uint NumAnimationFrames;
            public byte Unknown;
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
            Removable = 0x01, // riddlemouth, spiderweb, etc
            BlockSight = 0x02,
            BlockMovement = 0x80,
            // TODO
        }

        public struct WallData
        {
            public byte[] Unknown1;
            public WallFlags Flags;
            public uint TextureIndex;
            public byte[] Unknown2;
            public OverlayData[] Overlays;

            public override string ToString()
            {
                string content = $"Flags: {Flags.ToString().Replace(", ", "|")}, Texture: {TextureIndex}, Overlays: {(Overlays == null ? 0 : Overlays.Length)}";

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

        public Dictionary<uint, Graphic> WallGraphics { get; } = new Dictionary<uint, Graphic>();
        public Dictionary<uint, Graphic> OverlayGraphics { get; } = new Dictionary<uint, Graphic>();
    }
}
