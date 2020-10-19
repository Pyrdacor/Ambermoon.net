/*
 * ISurface3D.cs - 3D surface interface
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Ambermoon.Render
{
    public enum SurfaceType
    {
        Floor,
        Ceiling,
        Wall,
        Billboard, // monsters, NPCs, trees, etc
        BillboardFloor // holes, lava, etc
    }

    /// <summary>
    /// Only used for wall surfaces.
    /// Other surfaces will ignore this.
    /// </summary>
    public enum WallOrientation
    {
        /// <summary>
        /// Wall facing front
        /// </summary>
        Normal,
        /// <summary>
        /// Wall facing left
        /// </summary>
        Rotated90,
        /// <summary>
        /// Wall facing back
        /// </summary>
        Rotated180,
        /// <summary>
        /// Wall facing right
        /// </summary>
        Rotated270
    }

    public interface ISurface3D
    {
        SurfaceType Type { get; }
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Width { get; }
        float Height { get; }
        uint TextureWidth { get; }
        uint TextureHeight { get; }
        uint MappedTextureWidth { get; }
        uint MappedTextureHeight { get; }
        bool Visible { get; set; }
        bool Alpha { get; }
        int FrameCount { get; }
        IRenderLayer Layer { get; set; }
        WallOrientation WallOrientation { get; }
        float Extrude { get; set; }

        void Delete();

        Position TextureAtlasOffset
        {
            get;
            set;
        }

        byte PaletteIndex
        {
            get;
            set;
        }
    }

    public interface ISurface3DFactory
    {
        ISurface3D Create(SurfaceType type, float width, float height, uint textureWidth, uint textureHeight,
            uint mappedTextureWidth, uint mappedTextureHeight, bool alpha, int frameCount = 1, float extrude = 0.0f,
            WallOrientation wallOrientation = WallOrientation.Normal, int textureAtlasX = 0, int textureAtlasY = 0);
    }
}
