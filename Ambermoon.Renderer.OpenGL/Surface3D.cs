/*
 * Surface3D.cs - Textured 3D surface
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

using Ambermoon.Render;

namespace Ambermoon.Renderer.OpenGL
{
    internal class Surface3D : ISurface3D
    {
        float x = float.MaxValue;
        float y = float.MaxValue;
        float z = float.MaxValue;
        bool visible = false;
        protected int drawIndex = -1;
        IRenderLayer layer = null;
        bool visibleRequest = false;
        bool deleted = false;
        bool notOnScreen = true;
        readonly Rect virtualScreen = null;
        Position textureAtlasOffset = null;
        byte paletteIndex = 0;
        public WallOrientation WallOrientation { get; } = WallOrientation.Normal;
        public uint TextureWidth { get; } = 0;
        public uint TextureHeight { get; } = 0;
        public uint MappedTextureWidth { get; } = 0;
        public uint MappedTextureHeight { get; } = 0;

        public Surface3D(SurfaceType type, float width, float height, int textureAtlasX, int textureAtlasY, uint textureWidth, uint textureHeight,
            uint mappedTextureWidth, uint mappedTextureHeight, Rect virtualScreen, WallOrientation wallOrientation)
        {
            Type = type;
            Width = width;
            Height = height;
            this.virtualScreen = virtualScreen;
            textureAtlasOffset = new Position(textureAtlasX, textureAtlasY);
            WallOrientation = wallOrientation;
            TextureWidth = textureWidth;
            TextureHeight = textureHeight;
            MappedTextureWidth = mappedTextureWidth;
            MappedTextureHeight = mappedTextureHeight;
        }

        public bool Visible
        {
            get => visible && !deleted && !notOnScreen;
            set
            {
                if (deleted)
                    return;

                if (layer == null)
                {
                    visibleRequest = value;
                    visible = false;
                    return;
                }

                visibleRequest = false;

                if (visible == value)
                    return;

                visible = value;

                if (Visible)
                    AddToLayer();
                else if (!visible)
                    RemoveFromLayer();
            }
        }

        public IRenderLayer Layer
        {
            get => layer;
            set
            {
                if (value != null && !(value is RenderLayer))
                    throw new AmbermoonException(ExceptionScope.Render, "The given layer is not valid for this renderer.");

                if (layer == value)
                    return;

                if (layer != null && Visible)
                    RemoveFromLayer();

                layer = value;

                if (layer != null && visibleRequest && !deleted)
                {
                    visible = true;
                    visibleRequest = false;
                    CheckOnScreen();
                }

                if (layer == null)
                {
                    visibleRequest = false;
                    visible = false;
                    notOnScreen = true;
                }

                if (layer != null && Visible)
                    AddToLayer();
            }
        }

        public float Width { get; private set; }

        public float Height { get; private set; }

        public byte PaletteIndex
        {
            get => paletteIndex;
            set
            {
                if (paletteIndex == value)
                    return;

                paletteIndex = value;

                UpdatePaletteIndex();
            }
        }

        public virtual Position TextureAtlasOffset
        {
            get => textureAtlasOffset;
            set
            {
                if (textureAtlasOffset == value)
                    return;

                textureAtlasOffset = new Position(value);

                UpdateTextureAtlasOffset();
            }
        }

        public SurfaceType Type { get; }

        public float X
        {
            get => x;
            set
            {
                if (x == value)
                    return;

                x = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public float Y
        {
            get => y;
            set
            {
                if (y == value)
                    return;

                y = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public float Z
        {
            get => z;
            set
            {
                if (z == value)
                    return;

                z = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public void Delete()
        {
            if (!deleted)
            {
                RemoveFromLayer();
                deleted = true;
                visible = false;
                visibleRequest = false;
            }
        }

        bool CheckOnScreen()
        {
            bool oldNotOnScreen = notOnScreen;
            bool oldVisible = Visible;

            // TODO
            notOnScreen = false;// !virtualScreen.IntersectsWith(new Rect(X, Y, Width, Height));

            if (oldNotOnScreen != notOnScreen)
            {
                if (oldVisible != Visible)
                {
                    if (Visible)
                        AddToLayer();
                    else
                        RemoveFromLayer();

                    return true; // handled
                }
            }

            return false;
        }

        protected virtual void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this);
        }

        protected virtual void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected virtual void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }

        protected virtual void UpdateTextureAtlasOffset()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateTextureAtlasOffset(drawIndex, this);
        }

        protected virtual void UpdatePaletteIndex()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePaletteIndex(drawIndex, PaletteIndex);
        }
    }

    public class Surface3DFactory : ISurface3DFactory
    {
        readonly Rect virtualScreen = null;

        public Surface3DFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public ISurface3D Create(SurfaceType type, float width, float height, uint textureWidth, uint textureHeight,
            uint mappedTextureWidth, uint mappedTextureHeight, WallOrientation wallOrientation = WallOrientation.Normal,
            int textureAtlasX = 0, int textureAtlasY = 0)
        {
            return new Surface3D(type, width, height, textureAtlasX, textureAtlasY, textureWidth, textureHeight,
                mappedTextureWidth, mappedTextureHeight, virtualScreen, wallOrientation);
        }
    }
}
