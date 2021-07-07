/*
 * RenderLayer.cs - Render layer implementation
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

        public Render.Texture Texture
        {
            get;
            set;
        } = null;

        internal RenderBuffer RenderBuffer { get; } = null;

        readonly State state = null;
        readonly RenderBuffer renderBufferColorRects = null;
        readonly Texture palette = null;
        bool disposed = false;

        private static readonly float[] LayerBaseZ = new float[]
        {
            0.00f,  // Map3DBackground
            0.00f,  // Map3D
            0.00f,  // Billboards3D
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
            0.31f,  // FOW
            0.61f,  // CombatBackground
            0.62f,  // BattleMonsterRow
            0.62f,  // BattleEffects
            0.70f,  // UI
            0.70f,  // Items
            0.70f,  // Text
            0.70f,  // IntroGraphics
            0.70f,  // IntroText
            0.97f,  // Effects
            0.98f,  // Cursor
            0.99f,  // DrugEffect
            0.00f,  // Intro
            0.00f   // Outro
        };

        public RenderLayer(State state, Layer layer, Texture texture, Texture palette)
        {
            if (layer == Layer.None)
                throw new AmbermoonException(ExceptionScope.Application, "Layer.None should never be used.");

            this.state = state;
            bool supportAnimations = layer != Layer.CombatBackground && layer != Layer.FOW && layer != Layer.Map3DBackground;
            bool layered = layer == Layer.Map3DBackground || layer > Global.Last2DLayer; // map is not layered, drawing order depends on y-coordinate and not given layer
            bool opaque = layer == Layer.CombatBackground || layer >= Layer.MapBackground1 && layer <= Layer.MapBackground8;

            RenderBuffer = new RenderBuffer(state, layer == Layer.Map3D || layer == Layer.Billboards3D,
                supportAnimations, layered, layer == Layer.DrugEffect, layer == Layer.Billboards3D, layer == Layer.Text,
                opaque, layer == Layer.FOW, layer == Layer.Map3DBackground);

            // UI uses color-filled areas and effects use colored areas for things like black fading map transitions.
            if (layer == Layer.Map3DBackground || layer == Layer.UI || layer == Layer.Effects || layer == Layer.DrugEffect ||
                layer == Layer.Intro || layer == Layer.Outro)
                renderBufferColorRects = new RenderBuffer(state, false, false, true, true);

            Layer = layer;
            Texture = texture;
            this.palette = palette;
        }

        public void Render()
        {
            if (!Visible)
                return;

            if (Layer == Layer.FOW)
            {
                var fowShader = RenderBuffer.FowShader;

                fowShader.UpdateMatrices(state);
                fowShader.SetZ(LayerBaseZ[(int)Layer]);
            }
            else
            {
                if (renderBufferColorRects != null)
                {
                    var colorShader = renderBufferColorRects.ColorShader;

                    colorShader.UpdateMatrices(state);
                    colorShader.SetZ(LayerBaseZ[(int)Layer]);

                    renderBufferColorRects.Render();
                }

                if (Texture != null)
                {
                    if (!(Texture is Texture texture))
                        throw new AmbermoonException(ExceptionScope.Render, "Invalid texture for this renderer.");

                    if (Layer == Layer.Map3D)
                    {
                        Texture3DShader shader = RenderBuffer.Texture3DShader;

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

                        shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                    }
                    else if (Layer == Layer.Billboards3D)
                    {
                        Billboard3DShader shader = RenderBuffer.Billboard3DShader;

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

                        shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                    }
                    else if (Layer == Layer.Text)
                    {
                        TextShader shader = RenderBuffer.TextShader;

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

                        shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                        shader.SetZ(LayerBaseZ[(int)Layer]);
                    }
                    else
                    {
                        bool sky = Layer == Layer.Map3DBackground;
                        TextureShader shader = sky ? RenderBuffer.SkyShader :
                            RenderBuffer.Opaque ? RenderBuffer.OpaqueTextureShader : RenderBuffer.TextureShader;

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

                        shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                        shader.SetZ(LayerBaseZ[(int)Layer]);
                    }
                }
            }

            RenderBuffer?.Render();
        }

        public int GetDrawIndex(ISprite sprite, byte? textColorIndex = null)
        {
            return RenderBuffer.GetDrawIndex(sprite, PositionTransformation,
                SizeTransformation, textColorIndex);
        }

        public int GetDrawIndex(ISurface3D surface)
        {
            return RenderBuffer.GetDrawIndex(surface);
        }

        public int GetDrawIndex(IFow fow)
        {
            return RenderBuffer.GetDrawIndex(fow, PositionTransformation,
                SizeTransformation);
        }

        public void FreeDrawIndex(int index)
        {
            RenderBuffer.FreeDrawIndex(index);
        }

        public void UpdatePosition(int index, ISprite sprite)
        {
            RenderBuffer.UpdatePosition(index, sprite, sprite.BaseLineOffset, PositionTransformation, SizeTransformation);
        }

        public void UpdateTextureAtlasOffset(int index, ISprite sprite)
        {
            RenderBuffer.UpdateTextureAtlasOffset(index, sprite);
        }

        public void UpdateMaskColor(int index, byte? maskColor)
        {
            RenderBuffer.UpdateMaskColor(index, maskColor);
        }

        public void UpdatePosition(int index, ISurface3D surface)
        {
            RenderBuffer.UpdatePosition(index, surface);
        }

        public void UpdateTextureAtlasOffset(int index, ISurface3D surface)
        {
            RenderBuffer.UpdateTextureAtlasOffset(index, surface);
        }

        public void UpdatePosition(int index, IFow fow)
        {
            RenderBuffer.UpdatePosition(index, fow, fow.BaseLineOffset, PositionTransformation, SizeTransformation);
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            RenderBuffer.UpdateDisplayLayer(index, displayLayer);
        }

        public void UpdateExtrude(int index, float extrude)
        {
            RenderBuffer.UpdateExtrude(index, extrude);
        }

        public void UpdatePaletteIndex(int index, byte paletteIndex)
        {
            RenderBuffer.UpdatePaletteIndex(index, paletteIndex);
        }

        public void UpdateTextColorIndex(int index, byte textColorIndex)
        {
            RenderBuffer.UpdateTextColorIndex(index, textColorIndex);
        }

        public void UpdateFOWCenter(int index, Position center)
        {
            RenderBuffer.UpdateCenter(index, center, PositionTransformation);
        }

        public void UpdateFOWRadius(int index, byte radius)
        {
            RenderBuffer.UpdateRadius(index, radius);
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
                    RenderBuffer?.Dispose();
                    renderBufferColorRects?.Dispose();
                    if (Texture is Texture texture)
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
                Layer.None => throw new AmbermoonException(ExceptionScope.Render, $"Cannot create render layer for layer {Enum.GetName(layer)}"),
                _ => new RenderLayer(State, layer, texture as Texture, palette as Texture),
            };
        }
    }
}
