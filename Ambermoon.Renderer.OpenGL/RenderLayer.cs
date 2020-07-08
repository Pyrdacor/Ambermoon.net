/*
 * RenderLayer.cs - Render layer implementation
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
using Silk.NET.OpenGL;
using System;

namespace Ambermoon.Renderer
{
    public class RenderLayer : IRenderLayer, IDisposable
    {
        public Layer Layer { get; } = Layer.None;

        public bool Visible
        {
            get;
            set;
        }

        public PositionTransformation PositionTransformation
        {
            get;
            set;
        } = null;

        public SizeTransformation SizeTransformation
        {
            get;
            set;
        } = null;

        readonly State state = null;
        readonly RenderBuffer renderBuffer = null;
        readonly RenderBuffer renderBufferColorRects = null;
        readonly Texture texture = null;
        readonly Texture palette = null;
        bool disposed = false;

        // The back map layers and the front layers plus characters have a range of 0.3f for object y-ordering).
        // The UI background has a range of 0.1f for UI layers.
        // The battle monster rows use range of 0.01f (more right monsters are drawn above their left neighbors).
        // The UI foreground (like controls and borders) has a range of 0.2f for UI layers.
        // Items use basically the same layer (range 0.02f) as they won't overlap (but the dragged item will use 0.97f).
        // Popup and cursor are single objects and has therefore a small range of 0.01f.
        private static readonly float[] LayerBaseZ = new float[]
        {
            0.00f,  // Map3D
            0.01f,  // MapBackground1
            0.01f,  // MapBackground2
            0.01f,  // MapBackground3
            0.01f,  // MapBackground4
            0.01f,  // MapBackground5
            0.01f,  // MapBackground6
            0.01f,  // MapBackground7
            0.01f,  // MapBackground8
            0.31f,  // Characters
            0.31f,  // MapForeground1
            0.31f,  // MapForeground2
            0.31f,  // MapForeground3
            0.31f,  // MapForeground4
            0.31f,  // MapForeground5
            0.31f,  // MapForeground6
            0.31f,  // MapForeground7
            0.31f,  // MapForeground8
            0.61f,  // UIBackground
            0.71f,  // BattleMonsterRowFarthest
            0.72f,  // BattleMonsterRowFar
            0.73f,  // BattleMonsterRowCenter
            0.74f,  // BattleMonsterRowNear
            0.75f,  // BattleMonsterRowNearest
            0.76f,  // UIForeground
            0.96f,  // Items
            0.98f,  // Popup
            0.99f   // Cursor
        };

        public RenderLayer(State state, Layer layer, Texture texture, Texture palette)
        {
            if (layer == Layer.None)
                throw new AmbermoonException(ExceptionScope.Application, "Layer.None should never be used.");

            this.state = state;
            bool masked = false; // TODO: do we need this for some layer?
            bool supportAnimations = layer > Layer.Map3D && layer < Layer.MapBackground1; // TODO
            bool layered = layer > Layer.MapForeground8; // map is not layered, drawing order depends on y-coordinate and not given layer

            renderBuffer = new RenderBuffer(state, Layer == Layer.Map3D, masked, supportAnimations, layered);

            if (Layer == Layer.UIBackground)
                renderBufferColorRects = new RenderBuffer(state, false, supportAnimations, true, true);

            Layer = layer;
            this.texture = texture;
            this.palette = palette;
        }

        public void Render()
        {
            if (!Visible)
                return;

            if (renderBufferColorRects != null)
            {
                var colorShader = renderBufferColorRects.ColorShader;

                colorShader.UpdateMatrices(state);
                colorShader.SetZ(LayerBaseZ[(int)Layer]);

                renderBufferColorRects.Render();
            }

            if (texture != null)
            {
                TextureShader shader = renderBuffer.Masked ? renderBuffer.MaskedTextureShader : renderBuffer.TextureShader;

                shader.UpdateMatrices(state);

                shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                state.Gl.ActiveTexture(GLEnum.Texture0);
                texture.Bind();

                if (palette != null)
                {
                    shader.SetPalette(1);
                    state.Gl.ActiveTexture(GLEnum.Texture1);
                    palette.Bind();
                }

                shader.SetAtlasSize((uint)texture.Width, (uint)texture.Height);
                shader.SetZ(LayerBaseZ[(int)Layer]);
            }

            renderBuffer.Render();
        }

        public int GetDrawIndex(ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            return renderBuffer.GetDrawIndex(sprite, PositionTransformation, SizeTransformation, maskSpriteTextureAtlasOffset);
        }

        public int GetDrawIndex(ISurface3D surface)
        {
            return renderBuffer.GetDrawIndex(surface);
        }

        public void FreeDrawIndex(int index)
        {
            renderBuffer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, ISprite sprite)
        {
            renderBuffer.UpdatePosition(index, sprite, sprite.BaseLineOffset, PositionTransformation, SizeTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            renderBuffer.UpdateTextureAtlasOffset(index, sprite, maskSpriteTextureAtlasOffset);
        }

        public void UpdatePosition(int index, ISurface3D surface)
        {
            renderBuffer.UpdatePosition(index, surface);
        }

        public void UpdateTextureAtlasOffset(int index, ISurface3D surface)
        {
            renderBuffer.UpdateTextureAtlasOffset(index, surface);
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            renderBuffer.UpdateDisplayLayer(index, displayLayer);
        }

        public void UpdatePaletteIndex(int index, byte paletteIndex)
        {
            renderBuffer.UpdatePaletteIndex(index, paletteIndex);
        }

        public int GetColoredRectDrawIndex(ColoredRect coloredRect)
        {
            return renderBufferColorRects.GetDrawIndex(coloredRect, PositionTransformation, SizeTransformation);
        }

        public void FreeColoredRectDrawIndex(int index)
        {
            renderBufferColorRects.FreeDrawIndex(index);
        }

        public void UpdateColoredRectPosition(int index, ColoredRect coloredRect)
        {
            renderBufferColorRects.UpdatePosition(index, coloredRect, 0, PositionTransformation, SizeTransformation);
        }

        public void UpdateColoredRectColor(int index, Render.Color color)
        {
            renderBufferColorRects.UpdateColor(index, color);
        }

        public void UpdateColoredRectDisplayLayer(int index, byte displayLayer)
        {
            renderBufferColorRects.UpdateDisplayLayer(index, displayLayer);
        }

        public void TestNode(IRenderNode node)
        {
            if (!(node is Node))
                throw new AmbermoonException(ExceptionScope.Render, "The given render node is not valid for this renderer.");

            if (node is ColoredRect && renderBufferColorRects == null)
                throw new AmbermoonException(ExceptionScope.Render, "This layer does not support colored rects.");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    renderBuffer?.Dispose();
                    renderBufferColorRects?.Dispose();
                    texture?.Dispose();
                    Visible = false;

                    disposed = true;
                }
            }
        }
    }

    public class RenderLayerFactory : IRenderLayerFactory
    {
        public State State { get; }

        public RenderLayerFactory(State state)
        {
            State = state;
        }

        public IRenderLayer Create(Layer layer, Render.Texture texture, Render.Texture palette)
        {
            if (texture != null && !(texture is Texture))
                throw new AmbermoonException(ExceptionScope.Render, "The given texture is not valid for this renderer.");
            if (palette != null && !(palette is Texture))
                throw new AmbermoonException(ExceptionScope.Render, "The given palette is not valid for this renderer.");

            return layer switch
            {
                Layer.None => throw new AmbermoonException(ExceptionScope.Render, $"Cannot create render layer for layer {Enum.GetName(typeof(Layer), layer)}"),
                _ => new RenderLayer(State, layer, texture as Texture, palette as Texture),
            };
        }
    }
}
