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
using Ambermoon.Renderer.OpenGL;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using System;
using System.Collections.Generic;

namespace Ambermoon.Renderer
{
    public class RenderLayer : IRenderLayer, IDisposable
    {
        internal static Dictionary<Layer, LayerConfig> DefaultLayerConfigs = new()
        {
            { Layer.Map3DBackground, new ()
            {
                BaseZ = 0.00f,
                EnableBlending = false,
                SupportAnimations = false,
                SupportColoredRects = true,
                Opaque = true
            } },
            { Layer.Map3DCeiling, new ()
            {
                Layered = false,
                BaseZ = 0.00f,
                EnableBlending = false
            } },
            { Layer.Map3D, new ()
            {
                Layered = false,
                BaseZ = 0.00f,
                EnableBlending = false
            } },
            { Layer.Billboards3D, new ()
            {
                Layered = false,
                BaseZ = 0.00f,
                EnableBlending = false
            } },
            { Layer.MapBackground1, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground2, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground3, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground4, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground5, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground6, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground7, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground8, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground9, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.MapBackground10, new ()
            {
                Layered = false,
                BaseZ = 0.01f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.Characters, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground1, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground2, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground3, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground4, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground5, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground6, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground7, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground8, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground9, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.MapForeground10, new ()
            {
                Layered = false,
                BaseZ = 0.31f,
                EnableBlending = false
            } },
            { Layer.FOW, new ()
            {
                // Note: this uses neither colored rects
                // nor textured sprites. Have a look at IFow.
                BaseZ = 0.31f,
                EnableBlending = true,
                SupportAnimations = false,
                SupportTextures = false
            } },
            { Layer.CombatBackground, new ()
            {
                BaseZ = 0.61f,
                EnableBlending = false,
                SupportAnimations = false,
                Opaque = true
            } },
            { Layer.BattleMonsterRow, new ()
            {
                BaseZ = 0.62f,
                EnableBlending = false
            } },
            { Layer.BattleEffects, new ()
            {
                BaseZ = 0.62f,
                EnableBlending = false
            } },
            { Layer.UI, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = false,
                SupportColoredRects = true
            } },
            { Layer.Items, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = false
            } },
            { Layer.Text, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = false
            } },
            { Layer.SmallDigits, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = false
            } },
            { Layer.MainMenuGraphics, new ()
            {
                BaseZ = 0.70f
            } },
            { Layer.MainMenuText, new ()
            {
                BaseZ = 0.70f
            } },
            { Layer.MainMenuEffects, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                SupportColoredRects = true
            } },
            { Layer.IntroGraphics, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                SupportColoredRects = true,
                Use320x256 = true
            } },
            { Layer.IntroText, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                Use320x256 = true
            } },
            { Layer.IntroEffects, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                SupportColoredRects = true,
                SupportTextures = false,
                Use320x256 = true
            } },
            { Layer.OutroGraphics, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = false,
                Opaque = true
            } },
            { Layer.OutroText, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true
            } },
            { Layer.FantasyIntroGraphics, new ()
            {
                BaseZ = 0.70f,
                Use320x256 = true
            } },
            { Layer.FantasyIntroEffects, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                SupportColoredRects = true,
                SupportTextures = false,
                Use320x256 = true
            } },
            { Layer.Misc, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                SupportColoredRects = true
            } },
            { Layer.Images, new ()
            {
                BaseZ = 0.70f,
                EnableBlending = true,
                RenderToVirtualScreen = false
            } },
            { Layer.Effects, new ()
            {
                BaseZ = 0.97f,
                EnableBlending = true,
                SupportColoredRects = true,
                SupportTextures = false
            } },
            { Layer.Cursor, new ()
            {
                BaseZ = 0.98f,
                EnableBlending = false
            } },
            { Layer.DrugEffect, new ()
            {
                BaseZ = 0.99f,
                EnableBlending = true,
                SupportColoredRects = true,
                SupportTextures = false
            } }
        };

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

        public LayerConfig Config { get; private set; }
        public uint TextureFactor => Config.TextureFactor;

        internal RenderBuffer RenderBuffer { get; } = null;

        readonly State state = null;
        readonly RenderBuffer renderBufferColorRects = null;
        readonly Texture palette = null;
        bool disposed = false;

        public RenderLayer(State state, Layer layer, Texture texture, Texture palette)
        {
            if (layer == Layer.None)
                throw new AmbermoonException(ExceptionScope.Application, "Layer.None should never be used.");

            this.state = state;
            Config = DefaultLayerConfigs[layer];
            bool supportAnimations = Config.SupportAnimations;
            bool layered = Config.Layered;
            bool opaque = Config.Opaque;

            RenderBuffer = new RenderBuffer(state, layer == Layer.Map3DCeiling || layer == Layer.Map3D || layer == Layer.Billboards3D,
                supportAnimations, layered, !Config.SupportTextures, layer == Layer.Billboards3D, layer == Layer.Text || layer == Layer.SmallDigits,
                opaque, layer == Layer.FOW, layer == Layer.Map3DBackground,
                layer == Layer.Misc || layer == Layer.OutroText || layer == Layer.IntroText || layer == Layer.IntroGraphics, // textures with alpha
                layer == Layer.Images,
                Config.TextureFactor);

            if (Config.SupportColoredRects)
                renderBufferColorRects = new RenderBuffer(state, false, false, true, true);

            Layer = layer;
            Texture = texture;
            this.palette = palette;
        }

        public void UsePalette(bool use)
        {
            if (Config.UsePalette == use || !Config.SupportTextures || Layer == Layer.Images)
                return;

            Config = Config with { UsePalette = use };
        }

        public void SetTextureFactor(uint factor)
        {
            if (Config.TextureFactor == factor || !Config.SupportTextures || Layer == Layer.Images)
                return;

            Config = Config with { TextureFactor = factor };
            RenderBuffer.SetTextureFactor(factor);
        }

        public void Render()
        {
            if (!Visible)
                return;

            if (Layer == Layer.FOW)
            {
                var fowShader = RenderBuffer.FowShader;

                fowShader.UpdateMatrices(state);
                fowShader.SetZ(Config.BaseZ);
            }
            else
            {
                if (renderBufferColorRects != null)
                {
                    var colorShader = renderBufferColorRects.ColorShader;

                    colorShader.UpdateMatrices(state);
                    colorShader.SetZ(Config.BaseZ);

                    renderBufferColorRects.Render();
                }

                if (Texture != null)
                {
                    if (Texture is not Texture texture)
                        throw new AmbermoonException(ExceptionScope.Render, "Invalid texture for this renderer.");

                    if (Layer == Layer.Map3D || Layer == Layer.Map3DCeiling)
                    {
                        Texture3DShader shader = RenderBuffer.Texture3DShader;

                        shader.UsePalette(Config.UsePalette);
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

                        shader.UsePalette(Config.UsePalette);
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
                    else if (Layer == Layer.Text || Layer == Layer.SmallDigits)
                    {
                        TextShader shader = RenderBuffer.TextShader;

                        shader.UsePalette(Config.UsePalette);
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
                        shader.SetZ(Config.BaseZ);
                    }
                    else if (Layer == Layer.Images)
                    {
                        ImageShader shader = RenderBuffer.ImageShader;

                        shader.UpdateMatrices(state);

                        shader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
                        state.Gl.ActiveTexture(GLEnum.Texture0);
                        texture.Bind();

                        shader.SetAtlasSize((uint)Texture.Width, (uint)Texture.Height);
                        shader.SetZ(Config.BaseZ);
                    }
                    else
                    {
                        bool special = Layer == Layer.Misc || Layer == Layer.OutroText || Layer == Layer.IntroText || Layer == Layer.IntroGraphics;
                        bool sky = Layer == Layer.Map3DBackground;
                        TextureShader shader = special ? RenderBuffer.AlphaTextureShader : sky ? RenderBuffer.SkyShader :
                            RenderBuffer.Opaque ? RenderBuffer.OpaqueTextureShader : RenderBuffer.TextureShader;

                        shader.UsePalette(Config.UsePalette);
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
                        shader.SetZ(Config.BaseZ);
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

        public void UpdateAlpha(int index, byte alpha)
        {
            RenderBuffer.UpdateAlpha(index, alpha);
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
            if (!disposed)
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
