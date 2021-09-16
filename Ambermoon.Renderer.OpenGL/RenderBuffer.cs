/*
 * RenderBuffer.cs - Renders several buffered objects
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
    public class RenderBuffer : IDisposable
    {
        public bool Opaque { get; } = false;
        bool disposed = false;
        readonly State state;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly VectorBuffer vectorBuffer = null;
        readonly PositionBuffer positionBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;
        readonly WordBuffer baseLineBuffer = null;
        readonly ColorBuffer colorBuffer = null;
        readonly ByteBuffer layerBuffer = null;
        readonly IndexBuffer indexBuffer = null;
        readonly ByteBuffer paletteIndexBuffer = null;
        readonly ByteBuffer textColorIndexBuffer = null;
        readonly PositionBuffer textureEndCoordBuffer = null;
        readonly PositionBuffer textureSizeBuffer = null;
        readonly VectorBuffer billboardCenterBuffer = null;
        readonly ByteBuffer billboardOrientationBuffer = null;
        readonly ByteBuffer alphaBuffer = null;
        readonly FloatBuffer extrudeBuffer = null;
        readonly PositionBuffer centerBuffer = null;
        readonly ByteBuffer radiusBuffer = null;
        static readonly Dictionary<State, ColorShader> colorShaders = new Dictionary<State, ColorShader>();
        static readonly Dictionary<State, TextureShader> textureShaders = new Dictionary<State, TextureShader>();
        static readonly Dictionary<State, OpaqueTextureShader> opaqueTextureShaders = new Dictionary<State, OpaqueTextureShader>();
        static readonly Dictionary<State, Texture3DShader> texture3DShaders = new Dictionary<State, Texture3DShader>();
        static readonly Dictionary<State, Billboard3DShader> billboard3DShaders = new Dictionary<State, Billboard3DShader>();
        static readonly Dictionary<State, TextShader> textShaders = new Dictionary<State, TextShader>();
        static readonly Dictionary<State, FowShader> fowShaders = new Dictionary<State, FowShader>();
        static readonly Dictionary<State, SkyShader> skyShaders = new Dictionary<State, SkyShader>();
        static readonly Dictionary<State, AlphaTextureShader> alphaTextureShaders = new Dictionary<State, AlphaTextureShader>();

        public RenderBuffer(State state, bool is3D, bool supportAnimations, bool layered,
            bool noTexture = false, bool isBillboard = false, bool isText = false, bool opaque = false,
            bool fow = false, bool sky = false, bool special = false)
        {
            this.state = state;
            Opaque = opaque;

            if (is3D)
            {
                if (layered || noTexture)
                    throw new AmbermoonException(ExceptionScope.Render, "3D render buffers can't be masked nor layered and must not lack a texture.");
            }

            if (special)
            {
                if (!alphaTextureShaders.ContainsKey(state))
                    alphaTextureShaders[state] = AlphaTextureShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, alphaTextureShaders[state].ShaderProgram);
            }
            else if (sky)
            {
                if (!skyShaders.ContainsKey(state))
                    skyShaders[state] = SkyShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, skyShaders[state].ShaderProgram);
            }
            else if (fow)
            {
                if (!fowShaders.ContainsKey(state))
                    fowShaders[state] = FowShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, fowShaders[state].ShaderProgram);
            }
            else if (noTexture)
            {
                if (!colorShaders.ContainsKey(state))
                    colorShaders[state] = ColorShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, colorShaders[state].ShaderProgram);
            }
            else if (isText)
            {
                if (!textShaders.ContainsKey(state))
                    textShaders[state] = TextShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, textShaders[state].ShaderProgram);
            }
            else
            {
                if (is3D)
                {
                    if (isBillboard)
                    {
                        if (!billboard3DShaders.ContainsKey(state))
                            billboard3DShaders[state] = Billboard3DShader.Create(state);
                        vertexArrayObject = new VertexArrayObject(state, billboard3DShaders[state].ShaderProgram);
                    }
                    else
                    {
                        if (!texture3DShaders.ContainsKey(state))
                            texture3DShaders[state] = Texture3DShader.Create(state);
                        vertexArrayObject = new VertexArrayObject(state, texture3DShaders[state].ShaderProgram);
                    }
                }
                else
                {
                    if (opaque)
                    {
                        if (!opaqueTextureShaders.ContainsKey(state))
                            opaqueTextureShaders[state] = OpaqueTextureShader.Create(state);
                        vertexArrayObject = new VertexArrayObject(state, opaqueTextureShaders[state].ShaderProgram);
                    }
                    else
                    {
                        if (!textureShaders.ContainsKey(state))
                            textureShaders[state] = TextureShader.Create(state);
                        vertexArrayObject = new VertexArrayObject(state, textureShaders[state].ShaderProgram);
                    }
                }
            }

            if (is3D)
            {
                vectorBuffer = new VectorBuffer(state, false);
                alphaBuffer = new ByteBuffer(state, true);
            }
            else
                positionBuffer = new PositionBuffer(state, false);
            indexBuffer = new IndexBuffer(state);

            if (special)
                alphaBuffer = new ByteBuffer(state, false);

            if (fow)
            {
                baseLineBuffer = new WordBuffer(state, true);
                centerBuffer = new PositionBuffer(state, false);
                radiusBuffer = new ByteBuffer(state, false);

                vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, baseLineBuffer);
                vertexArrayObject.AddBuffer(FowShader.DefaultCenterName, centerBuffer);
                vertexArrayObject.AddBuffer(FowShader.DefaultRadiusName, radiusBuffer);
            }
            else if (noTexture)
            {
                colorBuffer = new ColorBuffer(state, true);
                layerBuffer = new ByteBuffer(state, true);

                vertexArrayObject.AddBuffer(ColorShader.DefaultColorName, colorBuffer);
                vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
            }
            else
            {
                paletteIndexBuffer = new ByteBuffer(state, true);
                textureAtlasOffsetBuffer = new PositionBuffer(state, !supportAnimations);

                if (isText)
                {
                    textColorIndexBuffer = new ByteBuffer(state, true);

                    vertexArrayObject.AddBuffer(TextShader.DefaultTextColorIndexName, textColorIndexBuffer);
                }

                if (!isText && !is3D)
                {
                    colorBuffer = new ColorBuffer(state, true);

                    vertexArrayObject.AddBuffer(TextureShader.DefaultMaskColorIndexName, colorBuffer);
                }

                if (layered || isText)
                {
                    layerBuffer = new ByteBuffer(state, true);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
                }
                else if (!is3D)
                {
                    baseLineBuffer = new WordBuffer(state, false);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, baseLineBuffer);
                }
                else
                {
                    textureSizeBuffer = new PositionBuffer(state, true);
                    textureEndCoordBuffer = new PositionBuffer(state, true);

                    vertexArrayObject.AddBuffer(Texture3DShader.DefaultTexSizeName, textureSizeBuffer);
                    vertexArrayObject.AddBuffer(Texture3DShader.DefaultTexEndCoordName, textureEndCoordBuffer);
                }
            }

            if (isBillboard)
            {
                billboardCenterBuffer = new VectorBuffer(state, false);
                billboardOrientationBuffer = new ByteBuffer(state, true);
                extrudeBuffer = new FloatBuffer(state, true);

                vertexArrayObject.AddBuffer(Billboard3DShader.DefaultBillboardCenterName, billboardCenterBuffer);
                vertexArrayObject.AddBuffer(Billboard3DShader.DefaultBillboardOrientationName, billboardOrientationBuffer);
                vertexArrayObject.AddBuffer(Billboard3DShader.DefaultExtrudeName, extrudeBuffer);
            }

            if (is3D)
            {
                vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, vectorBuffer);
                vertexArrayObject.AddBuffer(Texture3DShader.DefaultAlphaName, alphaBuffer);
            }
            else
                vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, positionBuffer);
            vertexArrayObject.AddBuffer("index", indexBuffer);

            if (special)
                vertexArrayObject.AddBuffer(AlphaTextureShader.DefaultAlphaName, alphaBuffer);

            if (!fow && !noTexture)
            {
                vertexArrayObject.AddBuffer(TextureShader.DefaultPaletteIndexName, paletteIndexBuffer);
                vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
            }
        }

        internal ColorShader ColorShader => colorShaders[state];
        internal TextureShader TextureShader => textureShaders[state];
        internal OpaqueTextureShader OpaqueTextureShader => opaqueTextureShaders[state];
        internal Texture3DShader Texture3DShader => texture3DShaders[state];
        internal Billboard3DShader Billboard3DShader => billboard3DShaders[state];
        internal TextShader TextShader => textShaders[state];
        internal FowShader FowShader => fowShaders[state];
        internal SkyShader SkyShader => skyShaders[state];
        internal AlphaTextureShader AlphaTextureShader => alphaTextureShaders[state];

        public int GetDrawIndex(Render.IFow fow,
            Render.PositionTransformation positionTransformation,
            Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(fow.X, fow.Y);
            var size = new Size(fow.Width, fow.Height);
            var center = new Position(fow.Center);

            if (positionTransformation != null)
            {
                position = positionTransformation(position);
                center = positionTransformation(center);
            }

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            int index = positionBuffer.Add((short)position.X, (short)position.Y);
            positionBuffer.Add((short)(position.X + size.Width), (short)position.Y, index + 1);
            positionBuffer.Add((short)(position.X + size.Width), (short)(position.Y + size.Height), index + 2);
            positionBuffer.Add((short)position.X, (short)(position.Y + size.Height), index + 3);

            indexBuffer.InsertQuad(index / 4);

            var baseLineOffsetSize = new Size(0, fow.BaseLineOffset);

            if (sizeTransformation != null)
                baseLineOffsetSize = sizeTransformation(baseLineOffsetSize);

            ushort baseLine = (ushort)Math.Min(ushort.MaxValue, position.Y + size.Height + baseLineOffsetSize.Height);

            baseLineBuffer.Add(baseLine, index);
            baseLineBuffer.Add(baseLine, index + 1);
            baseLineBuffer.Add(baseLine, index + 2);
            baseLineBuffer.Add(baseLine, index + 3);

            centerBuffer.Add((short)center.X, (short)center.Y, index);
            centerBuffer.Add((short)center.X, (short)center.Y, index + 1);
            centerBuffer.Add((short)center.X, (short)center.Y, index + 2);
            centerBuffer.Add((short)center.X, (short)center.Y, index + 3);

            radiusBuffer.Add(fow.Radius, index);
            radiusBuffer.Add(fow.Radius, index + 1);
            radiusBuffer.Add(fow.Radius, index + 2);
            radiusBuffer.Add(fow.Radius, index + 3);

            return index;
        }

        public int GetDrawIndex(Render.IColoredRect coloredRect,
            Render.PositionTransformation positionTransformation,
            Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(coloredRect.X, coloredRect.Y);
            var size = new Size(coloredRect.Width, coloredRect.Height);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            int index = positionBuffer.Add((short)position.X, (short)position.Y);
            positionBuffer.Add((short)(position.X + size.Width), (short)position.Y, index + 1);
            positionBuffer.Add((short)(position.X + size.Width), (short)(position.Y + size.Height), index + 2);
            positionBuffer.Add((short)position.X, (short)(position.Y + size.Height), index + 3);

            indexBuffer.InsertQuad(index / 4);

            if (layerBuffer != null)
            {
                int layerBufferIndex = layerBuffer.Add(coloredRect.DisplayLayer, index);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 1);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 2);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 3);
            }

            if (colorBuffer != null)
            {
                var color = coloredRect.Color;

                int colorBufferIndex = colorBuffer.Add(color, index);
                colorBuffer.Add(color, colorBufferIndex + 1);
                colorBuffer.Add(color, colorBufferIndex + 2);
                colorBuffer.Add(color, colorBufferIndex + 3);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISprite sprite, Render.PositionTransformation positionTransformation,
            Render.SizeTransformation sizeTransformation, byte? textColorIndex = null)
        {
            var position = new Position(sprite.X, sprite.Y);
            var spriteSize = new Size(sprite.Width, sprite.Height);
            var textureAtlasOffset = new Position(sprite.TextureAtlasOffset);
            var textureSize = new Size(sprite.TextureSize ?? spriteSize);

            if (sprite.ClipArea != null)
            {
                float textureWidthFactor = spriteSize.Width / textureSize.Width;
                float textureHeightFactor = spriteSize.Height / textureSize.Height;
                int oldX = position.X;
                int oldY = position.Y;
                int oldWidth = spriteSize.Width;
                int oldHeight = spriteSize.Height;
                sprite.ClipArea.ClipRect(position, spriteSize);
                textureAtlasOffset.Y += Util.Round((position.Y - oldY) / textureHeightFactor);
                textureSize.Width -= Util.Round((oldWidth - spriteSize.Width) / textureWidthFactor);
                textureSize.Height -= Util.Round((oldHeight - spriteSize.Height) / textureHeightFactor);

                if (sprite.MirrorX)
                {
                    int oldRight = oldX + oldWidth;
                    int newRight = position.X + spriteSize.Width;
                    textureAtlasOffset.X += Util.Round((oldRight - newRight) / textureWidthFactor);
                }
                else
                {
                    textureAtlasOffset.X += Util.Round((position.X - oldX) / textureWidthFactor);
                }
            }

            var size = new Size(spriteSize);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            int index = positionBuffer.Add((short)position.X, (short)position.Y);
            positionBuffer.Add((short)(position.X + size.Width), (short)position.Y, index + 1);
            positionBuffer.Add((short)(position.X + size.Width), (short)(position.Y + size.Height), index + 2);
            positionBuffer.Add((short)position.X, (short)(position.Y + size.Height), index + 3);

            indexBuffer.InsertQuad(index / 4);

            if (paletteIndexBuffer != null)
            {
                int paletteIndexBufferIndex = paletteIndexBuffer.Add(sprite.PaletteIndex, index);
                paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 1);
                paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 2);
                paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 3);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                if (sprite.MirrorX)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + textureSize.Width), (short)textureAtlasOffset.Y, index);
                    textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)textureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)(textureAtlasOffset.Y + textureSize.Height), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + textureSize.Width), (short)(textureAtlasOffset.Y + textureSize.Height), textureAtlasOffsetBufferIndex + 3);
                }
                else
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)textureAtlasOffset.Y, index);
                    textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + textureSize.Width), (short)textureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(textureAtlasOffset.X + textureSize.Width), (short)(textureAtlasOffset.Y + textureSize.Height), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)textureAtlasOffset.X, (short)(textureAtlasOffset.Y + textureSize.Height), textureAtlasOffsetBufferIndex + 3);
                }
            }

            if (baseLineBuffer != null)
            {
                var baseLineOffsetSize = new Size(0, sprite.BaseLineOffset);

                if (sizeTransformation != null)
                    baseLineOffsetSize = sizeTransformation(baseLineOffsetSize);

                ushort baseLine = (ushort)Math.Min(ushort.MaxValue, position.Y + size.Height + baseLineOffsetSize.Height);
                int baseLineBufferIndex = baseLineBuffer.Add(baseLine, index);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 1);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 2);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 3);
            }

            if (layerBuffer != null)
            {
                byte layer = sprite is Render.ILayerSprite layerSprite ? layerSprite.DisplayLayer : (byte)0;
                int layerBufferIndex = layerBuffer.Add(layer, index);
                layerBuffer.Add(layer, layerBufferIndex + 1);
                layerBuffer.Add(layer, layerBufferIndex + 2);
                layerBuffer.Add(layer, layerBufferIndex + 3);
            }

            if (colorBuffer != null)
            {
                byte color = sprite.MaskColor ?? 0;
                int maskColorBufferIndex = colorBuffer.Add(color, index);
                colorBuffer.Add(color, maskColorBufferIndex + 1);
                colorBuffer.Add(color, maskColorBufferIndex + 2);
                colorBuffer.Add(color, maskColorBufferIndex + 3);
            }

            if (textColorIndexBuffer != null)
            {
                if (textColorIndex == null)
                    throw new AmbermoonException(ExceptionScope.Render, "No text color index given but text color index buffer is active.");

                int textColorIndexBufferIndex = textColorIndexBuffer.Add(textColorIndex.Value, index);
                textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 1);
                textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 2);
                textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 3);
            }

            if (alphaBuffer != null)
            {
                byte alpha = sprite is AlphaSprite alphaSprite ? alphaSprite.Alpha : (byte)0xff;
                int alphaBufferIndex = alphaBuffer.Add(alpha, index);
                alphaBuffer.Add(alpha, alphaBufferIndex + 1);
                alphaBuffer.Add(alpha, alphaBufferIndex + 2);
                alphaBuffer.Add(alpha, alphaBufferIndex + 3);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISurface3D surface)
        {
            int index = surface.Type switch
            {
                Ambermoon.Render.SurfaceType.Billboard => vectorBuffer.Add(surface.X - 0.5f * surface.Width, surface.Y, surface.Z),
                Ambermoon.Render.SurfaceType.BillboardFloor => vectorBuffer.Add(surface.X - 0.5f * surface.Width, surface.Y + 0.5f * surface.Height, surface.Z),
                _ => vectorBuffer.Add(surface.X, surface.Y, surface.Z)
            };

            switch (surface.Type)
            {
                case Ambermoon.Render.SurfaceType.Floor:
                    vectorBuffer.Add(surface.X, surface.Y, surface.Z + surface.Height);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z + surface.Height);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);                   
                    break;
                case Ambermoon.Render.SurfaceType.Ceiling:
                    vectorBuffer.Add(surface.X, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);                    
                    break;
                case Ambermoon.Render.SurfaceType.Wall:
                    switch (surface.WallOrientation)
                    {
                        case Ambermoon.Render.WallOrientation.Normal:
                            vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);
                            vectorBuffer.Add(surface.X + surface.Width, surface.Y - surface.Height, surface.Z);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z);
                            break;
                        case Ambermoon.Render.WallOrientation.Rotated90:
                            vectorBuffer.Add(surface.X, surface.Y, surface.Z + surface.Width);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z + surface.Width);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z);
                            break;
                        case Ambermoon.Render.WallOrientation.Rotated180:
                            vectorBuffer.Add(surface.X - surface.Width, surface.Y, surface.Z);
                            vectorBuffer.Add(surface.X - surface.Width, surface.Y - surface.Height, surface.Z);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z);
                            break;
                        case Ambermoon.Render.WallOrientation.Rotated270:
                            vectorBuffer.Add(surface.X, surface.Y, surface.Z - surface.Width);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z - surface.Width);
                            vectorBuffer.Add(surface.X, surface.Y - surface.Height, surface.Z);
                            break;
                    }
                    break;
                case Ambermoon.Render.SurfaceType.Billboard:
                    vectorBuffer.Add(surface.X + 0.5f * surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Add(surface.X + 0.5f * surface.Width, surface.Y - surface.Height, surface.Z);
                    vectorBuffer.Add(surface.X - 0.5f * surface.Width, surface.Y - surface.Height, surface.Z);
                    break;
                case Ambermoon.Render.SurfaceType.BillboardFloor:
                {
                    vectorBuffer.Add(surface.X + 0.5f * surface.Width, surface.Y + 0.5f * surface.Height, surface.Z);
                    vectorBuffer.Add(surface.X + 0.5f * surface.Width, surface.Y - 0.5f * surface.Height, surface.Z);
                    vectorBuffer.Add(surface.X - 0.5f * surface.Width, surface.Y - 0.5f * surface.Height, surface.Z);
                    break;
                }
            }

            indexBuffer.InsertQuad(index / 4);

            if (paletteIndexBuffer != null)
            {
                int paletteIndexBufferIndex = paletteIndexBuffer.Add(surface.PaletteIndex, index);
                paletteIndexBuffer.Add(surface.PaletteIndex, paletteIndexBufferIndex + 1);
                paletteIndexBuffer.Add(surface.PaletteIndex, paletteIndexBufferIndex + 2);
                paletteIndexBuffer.Add(surface.PaletteIndex, paletteIndexBufferIndex + 3);
            }

            if (alphaBuffer != null)
            {
                byte alpha = (byte)(surface.Alpha ? 1 : surface.Type == Ambermoon.Render.SurfaceType.Wall ? 2 : 0);
                int alphaBufferIndex = alphaBuffer.Add(alpha, index);
                alphaBuffer.Add(alpha, alphaBufferIndex + 1);
                alphaBuffer.Add(alpha, alphaBufferIndex + 2);
                alphaBuffer.Add(alpha, alphaBufferIndex + 3);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                if (surface.Type == Ambermoon.Render.SurfaceType.Floor)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), index);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 3);
                }
                else if (surface.Type == Ambermoon.Render.SurfaceType.Ceiling)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y, index);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 3);
                }
                else if (surface.Type == Ambermoon.Render.SurfaceType.Billboard || surface.Type == Ambermoon.Render.SurfaceType.BillboardFloor)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y, index);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 3);
                }
                else if (surface.Type == Ambermoon.Render.SurfaceType.Wall)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y, index);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight), textureAtlasOffsetBufferIndex + 3);
                }
            }

            if (textureEndCoordBuffer != null)
            {
                short endX = (short)(surface.TextureAtlasOffset.X + surface.FrameCount * surface.TextureWidth);
                short endY = (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight);
                int textureEndCoordBufferIndex = textureEndCoordBuffer.Add(endX, endY, index);
                textureEndCoordBuffer.Add(endX, endY, textureEndCoordBufferIndex + 1);
                textureEndCoordBuffer.Add(endX, endY, textureEndCoordBufferIndex + 2);
                textureEndCoordBuffer.Add(endX, endY, textureEndCoordBufferIndex + 3);
            }

            if (textureSizeBuffer != null)
            {
                int textureSizeBufferIndex = textureSizeBuffer.Add((short)surface.TextureWidth, (short)surface.TextureHeight, index);
                textureSizeBuffer.Add((short)surface.TextureWidth, (short)surface.TextureHeight, textureSizeBufferIndex + 1);
                textureSizeBuffer.Add((short)surface.TextureWidth, (short)surface.TextureHeight, textureSizeBufferIndex + 2);
                textureSizeBuffer.Add((short)surface.TextureWidth, (short)surface.TextureHeight, textureSizeBufferIndex + 3);
            }

            if (billboardCenterBuffer != null)
            {
                int billboardCenterBufferIndex = billboardCenterBuffer.Add(surface.X, surface.Y, surface.Z, index);
                billboardCenterBuffer.Add(surface.X, surface.Y, surface.Z, billboardCenterBufferIndex + 1);
                billboardCenterBuffer.Add(surface.X, surface.Y, surface.Z, billboardCenterBufferIndex + 2);
                billboardCenterBuffer.Add(surface.X, surface.Y, surface.Z, billboardCenterBufferIndex + 3);
            }

            if (billboardOrientationBuffer != null)
            {
                byte floor = surface.Type == Ambermoon.Render.SurfaceType.BillboardFloor ? (byte)1 : (byte)0;
                int billboardOrientationBufferIndex = billboardOrientationBuffer.Add(floor, index);
                billboardOrientationBuffer.Add(floor, billboardOrientationBufferIndex + 1);
                billboardOrientationBuffer.Add(floor, billboardOrientationBufferIndex + 2);
                billboardOrientationBuffer.Add(floor, billboardOrientationBufferIndex + 3);
            }

            if (extrudeBuffer != null)
            {
                int extrudeBufferIndex = extrudeBuffer.Add(surface.Extrude, index);
                extrudeBuffer.Add(surface.Extrude, extrudeBufferIndex + 1);
                extrudeBuffer.Add(surface.Extrude, extrudeBufferIndex + 2);
                extrudeBuffer.Add(surface.Extrude, extrudeBufferIndex + 3);
            }

            return index;
        }

        public void UpdatePosition(int index, Render.IRenderNode renderNode, int baseLineOffset,
            Render.PositionTransformation positionTransformation, Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(renderNode.X, renderNode.Y);
            var size = new Size(renderNode.Width, renderNode.Height);

            if (renderNode.ClipArea != null)
                renderNode.ClipArea.ClipRect(position, size);

            if (positionTransformation != null)
                position = positionTransformation(position);

            if (sizeTransformation != null)
                size = sizeTransformation(size);

            positionBuffer.Update(index, (short)position.X, (short)position.Y);
            positionBuffer.Update(index + 1, (short)(position.X + size.Width), (short)position.Y);
            positionBuffer.Update(index + 2, (short)(position.X + size.Width), (short)(position.Y + size.Height));
            positionBuffer.Update(index + 3, (short)position.X, (short)(position.Y + size.Height));

            if (baseLineBuffer != null)
            {
                var baseLineOffsetSize = new Size(0, baseLineOffset);

                if (sizeTransformation != null)
                    baseLineOffsetSize = sizeTransformation(baseLineOffsetSize);

                ushort baseLine = (ushort)Math.Min(ushort.MaxValue, position.Y + size.Height + baseLineOffsetSize.Height);

                baseLineBuffer.Update(index, baseLine);
                baseLineBuffer.Update(index + 1, baseLine);
                baseLineBuffer.Update(index + 2, baseLine);
                baseLineBuffer.Update(index + 3, baseLine);
            }
        }

        public void UpdatePosition(int index, Render.ISurface3D surface)
        {
            if (surface.Type == Ambermoon.Render.SurfaceType.Billboard)
            {
                float x = surface.X - surface.Width * 0.5f;

                vectorBuffer.Update(index, x, surface.Y, surface.Z);
                vectorBuffer.Update(index + 1, x + surface.Width, surface.Y, surface.Z);
                vectorBuffer.Update(index + 2, x + surface.Width, surface.Y - surface.Height, surface.Z);
                vectorBuffer.Update(index + 3, x, surface.Y - surface.Height, surface.Z);

                if (billboardCenterBuffer != null)
                {
                    billboardCenterBuffer.Update(index, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 1, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 2, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 3, surface.X, surface.Y, surface.Z);
                }

                if (billboardOrientationBuffer != null)
                {
                    billboardOrientationBuffer.Update(index, 0);
                    billboardOrientationBuffer.Update(index + 1, 0);
                    billboardOrientationBuffer.Update(index + 2, 0);
                    billboardOrientationBuffer.Update(index + 3, 0);
                }
            }
            else if (surface.Type == Ambermoon.Render.SurfaceType.BillboardFloor)
            {
                float x = surface.X - surface.Width * 0.5f;

                vectorBuffer.Update(index, x, surface.Y + 0.5f * surface.Height, surface.Z);
                vectorBuffer.Update(index + 1, x + surface.Width, surface.Y + 0.5f * surface.Height, surface.Z);
                vectorBuffer.Update(index + 2, x + surface.Width, surface.Y - 0.5f * surface.Height, surface.Z);
                vectorBuffer.Update(index + 3, x, surface.Y - 0.5f * surface.Height, surface.Z);

                if (billboardCenterBuffer != null)
                {
                    billboardCenterBuffer.Update(index, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 1, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 2, surface.X, surface.Y, surface.Z);
                    billboardCenterBuffer.Update(index + 3, surface.X, surface.Y, surface.Z);
                }

                if (billboardOrientationBuffer != null)
                {
                    billboardOrientationBuffer.Update(index, 1);
                    billboardOrientationBuffer.Update(index + 1, 1);
                    billboardOrientationBuffer.Update(index + 2, 1);
                    billboardOrientationBuffer.Update(index + 3, 1);
                }
            }
            else
            {
                vectorBuffer.Update(index, surface.X, surface.Y, surface.Z);

                switch (surface.Type)
                {
                    case Ambermoon.Render.SurfaceType.Floor:
                        vectorBuffer.Update(index + 1, surface.X, surface.Y, surface.Z + surface.Height);
                        vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y, surface.Z + surface.Height);
                        vectorBuffer.Update(index + 3, surface.X + surface.Width, surface.Y, surface.Z);                        
                        break;
                    case Ambermoon.Render.SurfaceType.Ceiling:
                        vectorBuffer.Update(index + 1, surface.X, surface.Y, surface.Z - surface.Height);
                        vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                        vectorBuffer.Update(index + 3, surface.X + surface.Width, surface.Y, surface.Z);
                        break;
                    case Ambermoon.Render.SurfaceType.Wall:
                        switch (surface.WallOrientation)
                        {
                            case Ambermoon.Render.WallOrientation.Normal:
                                vectorBuffer.Update(index + 1, surface.X + surface.Width, surface.Y, surface.Z);
                                vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y - surface.Height, surface.Z);
                                vectorBuffer.Update(index + 3, surface.X, surface.Y - surface.Height, surface.Z);
                                break;
                            case Ambermoon.Render.WallOrientation.Rotated90:
                                vectorBuffer.Update(index + 1, surface.X, surface.Y, surface.Z + surface.Width);
                                vectorBuffer.Update(index + 2, surface.X, surface.Y - surface.Height, surface.Z + surface.Width);
                                vectorBuffer.Update(index + 3, surface.X, surface.Y - surface.Height, surface.Z);
                                break;
                            case Ambermoon.Render.WallOrientation.Rotated180:
                                vectorBuffer.Update(index + 1, surface.X - surface.Width, surface.Y, surface.Z);
                                vectorBuffer.Update(index + 2, surface.X - surface.Width, surface.Y - surface.Height, surface.Z);
                                vectorBuffer.Update(index + 3, surface.X, surface.Y - surface.Height, surface.Z);
                                break;
                            case Ambermoon.Render.WallOrientation.Rotated270:
                                vectorBuffer.Update(index + 1, surface.X, surface.Y, surface.Z - surface.Width);
                                vectorBuffer.Update(index + 2, surface.X, surface.Y - surface.Height, surface.Z - surface.Width);
                                vectorBuffer.Update(index + 3, surface.X, surface.Y - surface.Height, surface.Z);
                                break;
                            default:
                                throw new AmbermoonException(ExceptionScope.Render, "Invalid wall orientation.");
                        }
                        break;
                    default:
                        throw new AmbermoonException(ExceptionScope.Render, "Invalid surface type.");
                }
            }
        }

        public void UpdateMaskColor(int index, byte? maskColor)
        {
            if (colorBuffer != null)
            {
                var color = maskColor ?? 0;
                colorBuffer.Update(index, color);
                colorBuffer.Update(index + 1, color);
                colorBuffer.Update(index + 2, color);
                colorBuffer.Update(index + 3, color);
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISprite sprite)
        {
            if (textureAtlasOffsetBuffer == null)
                return;

            var position = new Position(sprite.X, sprite.Y);
            var spriteSize = new Size(sprite.Width, sprite.Height);
            var textureAtlasOffset = new Position(sprite.TextureAtlasOffset);
            var textureSize = new Size(sprite.TextureSize ?? new Size(sprite.Width, sprite.Height));

            if (sprite.ClipArea != null)
            {
                float textureWidthFactor = spriteSize.Width / textureSize.Width;
                float textureHeightFactor = spriteSize.Height / textureSize.Height;
                int oldX = position.X;
                int oldY = position.Y;
                int oldWidth = spriteSize.Width;
                int oldHeight = spriteSize.Height;
                sprite.ClipArea.ClipRect(position, spriteSize);
                textureAtlasOffset.Y += Util.Round((position.Y - oldY) / textureHeightFactor);
                textureSize.Width -= Util.Round((oldWidth - spriteSize.Width) / textureWidthFactor);
                textureSize.Height -= Util.Round((oldHeight - spriteSize.Height) / textureHeightFactor);

                if (sprite.MirrorX)
                {
                    int oldRight = oldX + oldWidth;
                    int newRight = position.X + spriteSize.Width;
                    textureAtlasOffset.X += Util.Round((oldRight - newRight) / textureWidthFactor);
                }
                else
                {
                    textureAtlasOffset.X += Util.Round((position.X - oldX) / textureWidthFactor);
                }
            }

            if (sprite.MirrorX)
            {
                textureAtlasOffsetBuffer.Update(index, (short)(textureAtlasOffset.X + textureSize.Width), (short)textureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 1, (short)textureAtlasOffset.X, (short)textureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 2, (short)textureAtlasOffset.X, (short)(textureAtlasOffset.Y + textureSize.Height));
                textureAtlasOffsetBuffer.Update(index + 3, (short)(textureAtlasOffset.X + textureSize.Width), (short)(textureAtlasOffset.Y + textureSize.Height));
            }
            else
            {
                textureAtlasOffsetBuffer.Update(index, (short)textureAtlasOffset.X, (short)textureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 1, (short)(textureAtlasOffset.X + textureSize.Width), (short)textureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 2, (short)(textureAtlasOffset.X + textureSize.Width), (short)(textureAtlasOffset.Y + textureSize.Height));
                textureAtlasOffsetBuffer.Update(index + 3, (short)textureAtlasOffset.X, (short)(textureAtlasOffset.Y + textureSize.Height));
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISurface3D surface)
        {
            if (textureAtlasOffsetBuffer == null)
                return;

            if (surface.Type == Ambermoon.Render.SurfaceType.Floor)
            {
                textureAtlasOffsetBuffer.Update(index, (short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
                textureAtlasOffsetBuffer.Update(index + 1, (short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 2, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 3, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
            }
            else if (surface.Type == Ambermoon.Render.SurfaceType.Ceiling)
            {
                textureAtlasOffsetBuffer.Update(index, (short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
                textureAtlasOffsetBuffer.Update(index + 1, (short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 2, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 3, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
            }
            else
            {
                textureAtlasOffsetBuffer.Update(index, (short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 1, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)surface.TextureAtlasOffset.Y);
                textureAtlasOffsetBuffer.Update(index + 2, (short)(surface.TextureAtlasOffset.X + surface.MappedTextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
                textureAtlasOffsetBuffer.Update(index + 3, (short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.MappedTextureHeight));
            }
        }

        public void UpdateColor(int index, Render.Color color)
        {
            if (colorBuffer != null)
            {
                colorBuffer.Update(index, color);
                colorBuffer.Update(index + 1, color);
                colorBuffer.Update(index + 2, color);
                colorBuffer.Update(index + 3, color);
            }
        }

        public void UpdateDisplayLayer(int index, byte displayLayer)
        {
            if (layerBuffer != null)
            {
                layerBuffer.Update(index, displayLayer);
                layerBuffer.Update(index + 1, displayLayer);
                layerBuffer.Update(index + 2, displayLayer);
                layerBuffer.Update(index + 3, displayLayer);
            }
        }

        public void UpdateAlpha(int index, byte alpha)
        {
            if (alphaBuffer != null)
            {
                alphaBuffer.Update(index, alpha);
                alphaBuffer.Update(index + 1, alpha);
                alphaBuffer.Update(index + 2, alpha);
                alphaBuffer.Update(index + 3, alpha);
            }
        }

        public void UpdateExtrude(int index, float extrude)
        {
            if (extrudeBuffer != null)
            {
                extrudeBuffer.Update(index, extrude);
                extrudeBuffer.Update(index + 1, extrude);
                extrudeBuffer.Update(index + 2, extrude);
                extrudeBuffer.Update(index + 3, extrude);
            }
        }

        public void UpdatePaletteIndex(int index, byte paletteIndex)
        {
            if (paletteIndexBuffer != null)
            {
                paletteIndexBuffer.Update(index, paletteIndex);
                paletteIndexBuffer.Update(index + 1, paletteIndex);
                paletteIndexBuffer.Update(index + 2, paletteIndex);
                paletteIndexBuffer.Update(index + 3, paletteIndex);
            }
        }

        public void UpdateTextColorIndex(int index, byte textColorIndex)
        {
            if (textColorIndexBuffer != null)
            {
                textColorIndexBuffer.Update(index, textColorIndex);
                textColorIndexBuffer.Update(index + 1, textColorIndex);
                textColorIndexBuffer.Update(index + 2, textColorIndex);
                textColorIndexBuffer.Update(index + 3, textColorIndex);
            }
        }

        public void UpdateRadius(int index, byte radius)
        {
            if (radiusBuffer != null)
            {
                radiusBuffer.Update(index, radius);
                radiusBuffer.Update(index + 1, radius);
                radiusBuffer.Update(index + 2, radius);
                radiusBuffer.Update(index + 3, radius);
            }
        }

        public void UpdateCenter(int index, Position center,
            Render.PositionTransformation positionTransformation)
        {
            if (centerBuffer != null)
            {
                center = new Position(center);

                if (positionTransformation != null)
                    center = positionTransformation(center);

                centerBuffer.Update(index, (short)center.X, (short)center.Y);
                centerBuffer.Update(index + 1, (short)center.X, (short)center.Y);
                centerBuffer.Update(index + 2, (short)center.X, (short)center.Y);
                centerBuffer.Update(index + 3, (short)center.X, (short)center.Y);
            }
        }

        public void FreeDrawIndex(int index)
        {
            /*int newSize = -1;

            if (index == (positionBuffer.Size - 8) / 8)
            {
                int i = (index - 1) * 4;
                newSize = positionBuffer.Size - 8;

                while (i >= 0 && !positionBuffer.IsPositionValid(i))
                {
                    i -= 4;
                    newSize -= 8;
                }
            }*/

            for (int i = 3; i >= 0; --i)
            {

                if (positionBuffer != null)
                {
                    positionBuffer.Update(index + i, short.MaxValue, short.MaxValue); // ensure it is not visible
                    positionBuffer.Remove(index + i);
                }
                else if (vectorBuffer != null)
                {
                    vectorBuffer.Update(index + i, float.MaxValue, float.MaxValue, float.MaxValue); // ensure it is not visible
                    vectorBuffer.Remove(index + i);
                }

                if (paletteIndexBuffer != null)
                {
                    paletteIndexBuffer.Remove(index + i);
                }

                if (textureAtlasOffsetBuffer != null)
                {
                    textureAtlasOffsetBuffer.Remove(index + i);
                }

                if (baseLineBuffer != null)
                {
                    baseLineBuffer.Remove(index + i);
                }

                if (colorBuffer != null)
                {
                    colorBuffer.Remove(index + i);
                }

                if (layerBuffer != null)
                {
                    layerBuffer.Remove(index + i);
                }

                if (textColorIndexBuffer != null)
                {
                    textColorIndexBuffer.Remove(index + i);
                }

                if (alphaBuffer != null)
                {
                    alphaBuffer.Remove(index + i);
                }

                if (billboardCenterBuffer != null)
                {
                    billboardCenterBuffer.Remove(index + i);
                }

                if (textureSizeBuffer != null)
                {
                    textureSizeBuffer.Remove(index + i);
                }

                if (textureEndCoordBuffer != null)
                {
                    textureEndCoordBuffer.Remove(index + i);
                }

                if (billboardOrientationBuffer != null)
                {
                    billboardOrientationBuffer.Remove(index + i);
                }

                if (extrudeBuffer != null)
                {
                    extrudeBuffer.Remove(index + i);
                }

                if (centerBuffer != null)
                {
                    centerBuffer.Remove(index + i);
                }

                if (radiusBuffer != null)
                {
                    radiusBuffer.Remove(index + i);
                }
            }
            
            // TODO: this code causes problems. commented out for now
            /*if (newSize != -1)
            {
                positionBuffer.ReduceSizeTo(newSize);

                if (textureAtlasOffsetBuffer != null)
                    textureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (maskTextureAtlasOffsetBuffer != null)
                    maskTextureAtlasOffsetBuffer.ReduceSizeTo(newSize);

                if (baseLineBuffer != null)
                    baseLineBuffer.ReduceSizeTo(newSize / 2);

                if (colorBuffer != null)
                    colorBuffer.ReduceSizeTo(newSize * 2);

                if (layerBuffer != null)
                    layerBuffer.ReduceSizeTo(newSize / 2);
            }*/
        }

        public void Render()
        {
            if (disposed)
                return;

            vertexArrayObject.Bind();

            unsafe
            {
                vertexArrayObject.Lock();

                try
                {
                    if (positionBuffer != null)
                        state.Gl.DrawElements(PrimitiveType.Triangles, (uint)(positionBuffer.Size / 4) * 3, DrawElementsType.UnsignedInt, (void*)0);
                    else if (vectorBuffer != null)
                        state.Gl.DrawElements(PrimitiveType.Triangles, (uint)vectorBuffer.Size / 2, DrawElementsType.UnsignedInt, (void*)0);
                    else
                        throw new AmbermoonException(ExceptionScope.Render, "Neither position nor vector buffer exists.");
                }
                catch
                {
                    // ignore for now
                }
                finally
                {
                    vertexArrayObject.Unlock();
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                vertexArrayObject?.Dispose();
                positionBuffer?.Dispose();
                paletteIndexBuffer?.Dispose();
                textColorIndexBuffer?.Dispose();
                textureAtlasOffsetBuffer?.Dispose();
                baseLineBuffer?.Dispose();
                colorBuffer?.Dispose();
                layerBuffer?.Dispose();
                indexBuffer?.Dispose();

                disposed = true;
            }
        }
    }
}
