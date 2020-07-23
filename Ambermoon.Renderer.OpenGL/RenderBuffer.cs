/*
 * RenderBuffer.cs - Renders several buffered objects
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

using Ambermoon.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Ambermoon.Renderer
{
    public class RenderBuffer : IDisposable
    {
        public bool Masked { get; } = false;
        bool disposed = false;
        readonly State state;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly VectorBuffer vectorBuffer = null;
        readonly PositionBuffer positionBuffer = null;
        readonly PositionBuffer textureAtlasOffsetBuffer = null;
        readonly PositionBuffer maskTextureAtlasOffsetBuffer = null; // is null for normal sprites
        readonly BaseLineBuffer baseLineBuffer = null;
        readonly ColorBuffer colorBuffer = null;
        readonly LayerBuffer layerBuffer = null;
        readonly IndexBuffer indexBuffer = null;
        readonly LayerBuffer paletteIndexBuffer = null;
        static readonly Dictionary<State, ColorShader> colorShaders = new Dictionary<State, ColorShader>();
        static readonly Dictionary<State, MaskedTextureShader> maskedTextureShaders = new Dictionary<State, MaskedTextureShader>();
        static readonly Dictionary<State, TextureShader> textureShaders = new Dictionary<State, TextureShader>();
        static readonly Dictionary<State, Texture3DShader> texture3DShaders = new Dictionary<State, Texture3DShader>();

        public RenderBuffer(State state, bool is3D, bool masked, bool supportAnimations, bool layered, bool noTexture = false)
        {
            this.state = state;
            Masked = masked;

            if (is3D)
            {
                if (masked || layered || noTexture)
                    throw new AmbermoonException(ExceptionScope.Render, "3D render buffers can't be masked nor layered and must not lack a texture.");
            }

            if (noTexture)
            {
                if (!colorShaders.ContainsKey(state))
                    colorShaders[state] = ColorShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, colorShaders[state].ShaderProgram);
            }
            else if (masked)
            {
                if (!maskedTextureShaders.ContainsKey(state))
                    maskedTextureShaders[state] = MaskedTextureShader.Create(state);
                vertexArrayObject = new VertexArrayObject(state, maskedTextureShaders[state].ShaderProgram);
            }
            else
            {
                if (is3D)
                {
                    if (!texture3DShaders.ContainsKey(state))
                        texture3DShaders[state] = Texture3DShader.Create(state);
                    vertexArrayObject = new VertexArrayObject(state, texture3DShaders[state].ShaderProgram);
                }
                else
                {
                    if (!textureShaders.ContainsKey(state))
                        textureShaders[state] = TextureShader.Create(state);
                    vertexArrayObject = new VertexArrayObject(state, textureShaders[state].ShaderProgram);
                }
            }

            if (is3D)
                vectorBuffer = new VectorBuffer(state, false);
            else
                positionBuffer = new PositionBuffer(state, false);
            indexBuffer = new IndexBuffer(state);

            if (noTexture)
            {
                colorBuffer = new ColorBuffer(state, true);
                layerBuffer = new LayerBuffer(state, true);

                vertexArrayObject.AddBuffer(ColorShader.DefaultColorName, colorBuffer);
                vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
            }
            else
            {
                paletteIndexBuffer = new LayerBuffer(state, true);
                textureAtlasOffsetBuffer = new PositionBuffer(state, !supportAnimations);

                if (layered)
                {
                    layerBuffer = new LayerBuffer(state, true);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, layerBuffer);
                }
                else if (!is3D)
                {
                    baseLineBuffer = new BaseLineBuffer(state, false);

                    vertexArrayObject.AddBuffer(ColorShader.DefaultLayerName, baseLineBuffer);
                }
            }

            if (masked && !noTexture)
            {
                maskTextureAtlasOffsetBuffer = new PositionBuffer(state, !supportAnimations);

                vertexArrayObject.AddBuffer(MaskedTextureShader.DefaultMaskTexCoordName, maskTextureAtlasOffsetBuffer);
            }

            if (is3D)
                vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, vectorBuffer);
            else
                vertexArrayObject.AddBuffer(ColorShader.DefaultPositionName, positionBuffer);
            vertexArrayObject.AddBuffer("index", indexBuffer);

            if (!noTexture)
            {
                vertexArrayObject.AddBuffer(TextureShader.DefaultPaletteIndexName, paletteIndexBuffer);
                vertexArrayObject.AddBuffer(TextureShader.DefaultTexCoordName, textureAtlasOffsetBuffer);
            }
        }

        internal ColorShader ColorShader => colorShaders[state];
        internal MaskedTextureShader MaskedTextureShader => maskedTextureShaders[state];
        internal TextureShader TextureShader => textureShaders[state];
        internal Texture3DShader Texture3DShader => texture3DShaders[state];

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
                int layerBufferIndex = layerBuffer.Add(coloredRect.DisplayLayer);

                if (layerBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 1);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 2);
                layerBuffer.Add(coloredRect.DisplayLayer, layerBufferIndex + 3);
            }

            if (colorBuffer != null)
            {
                var color = coloredRect.Color;

                int colorBufferIndex = colorBuffer.Add(color);

                if (colorBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                colorBuffer.Add(color, colorBufferIndex + 1);
                colorBuffer.Add(color, colorBufferIndex + 2);
                colorBuffer.Add(color, colorBufferIndex + 3);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISprite sprite, Render.PositionTransformation positionTransformation,
            Render.SizeTransformation sizeTransformation, Position maskSpriteTextureAtlasOffset = null)
        {
            var position = new Position(sprite.X, sprite.Y);
            var size = new Size(sprite.Width, sprite.Height);

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
                int paletteIndexBufferIndex = paletteIndexBuffer.Add((byte)sprite.PaletteIndex);

                if (paletteIndexBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                paletteIndexBuffer.Add((byte)sprite.PaletteIndex, paletteIndexBufferIndex + 1);
                paletteIndexBuffer.Add((byte)sprite.PaletteIndex, paletteIndexBufferIndex + 2);
                paletteIndexBuffer.Add((byte)sprite.PaletteIndex, paletteIndexBufferIndex + 3);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);

                if (textureAtlasOffsetBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                textureAtlasOffsetBuffer.Add((short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 2);
                textureAtlasOffsetBuffer.Add((short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height), textureAtlasOffsetBufferIndex + 3);
            }

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                int maskTextureAtlasOffsetBufferIndex = maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);

                if (maskTextureAtlasOffsetBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y, maskTextureAtlasOffsetBufferIndex + 1);
                maskTextureAtlasOffsetBuffer.Add((short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height), maskTextureAtlasOffsetBufferIndex + 2);
                maskTextureAtlasOffsetBuffer.Add((short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height), maskTextureAtlasOffsetBufferIndex + 3);
            }

            if (baseLineBuffer != null)
            {
                ushort baseLine = (ushort)(position.Y + size.Height + sprite.BaseLineOffset);

                int baseLineBufferIndex = baseLineBuffer.Add(baseLine);

                if (baseLineBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 1);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 2);
                baseLineBuffer.Add(baseLine, baseLineBufferIndex + 3);
            }

            if (layerBuffer != null)
            {
                byte layer = (sprite is Render.ILayerSprite) ? (sprite as Render.ILayerSprite).DisplayLayer : (byte)0;

                int layerBufferIndex = layerBuffer.Add(layer);

                if (layerBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                layerBuffer.Add(layer, layerBufferIndex + 1);
                layerBuffer.Add(layer, layerBufferIndex + 2);
                layerBuffer.Add(layer, layerBufferIndex + 3);
            }

            return index;
        }

        public int GetDrawIndex(Render.ISurface3D surface)
        {
            int index = vectorBuffer.Add(surface.X, surface.Y, surface.Z);

            switch (surface.Type)
            {
                case Ambermoon.Render.SurfaceType.Floor:
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z + surface.Height);
                    vectorBuffer.Add(surface.X, surface.Y, surface.Z + surface.Height);
                    break;
                case Ambermoon.Render.SurfaceType.Ceiling:
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Add(surface.X, surface.Y, surface.Z - surface.Height);
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
                    // TODO
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Add(surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Add(surface.X, surface.Y, surface.Z - surface.Height);
                    break;
            }

            indexBuffer.InsertQuad(index / 4);

            if (paletteIndexBuffer != null)
            {
                int paletteIndexBufferIndex = paletteIndexBuffer.Add((byte)surface.PaletteIndex);

                if (paletteIndexBufferIndex != index)
                    throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                paletteIndexBuffer.Add((byte)surface.PaletteIndex, paletteIndexBufferIndex + 1);
                paletteIndexBuffer.Add((byte)surface.PaletteIndex, paletteIndexBufferIndex + 2);
                paletteIndexBuffer.Add((byte)surface.PaletteIndex, paletteIndexBufferIndex + 3);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                if (surface.Type == Ambermoon.Render.SurfaceType.Ceiling)
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight));

                    if (textureAtlasOffsetBufferIndex != index)
                        throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight), textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 3);
                }
                else
                {
                    int textureAtlasOffsetBufferIndex = textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y);

                    if (textureAtlasOffsetBufferIndex != index)
                        throw new AmbermoonException(ExceptionScope.Render, "Invalid index");

                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)surface.TextureAtlasOffset.Y, textureAtlasOffsetBufferIndex + 1);
                    textureAtlasOffsetBuffer.Add((short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight), textureAtlasOffsetBufferIndex + 2);
                    textureAtlasOffsetBuffer.Add((short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight), textureAtlasOffsetBufferIndex + 3);
                }
            }

            return index;
        }

        public void UpdatePosition(int index, Render.IRenderNode renderNode, int baseLineOffset,
            Render.PositionTransformation positionTransformation, Render.SizeTransformation sizeTransformation)
        {
            var position = new Position(renderNode.X, renderNode.Y);
            var size = new Size(renderNode.Width, renderNode.Height);

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
                ushort baseLine = (ushort)(position.Y + size.Height + baseLineOffset);

                baseLineBuffer.Update(index, baseLine);
                baseLineBuffer.Update(index + 1, baseLine);
                baseLineBuffer.Update(index + 2, baseLine);
                baseLineBuffer.Update(index + 3, baseLine);
            }
        }

        public void UpdatePosition(int index, Render.ISurface3D surface)
        {
            vectorBuffer.Update(index, surface.X, surface.Y, surface.Z);

            switch (surface.Type)
            {
                case Ambermoon.Render.SurfaceType.Floor:
                    vectorBuffer.Update(index + 1, surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y, surface.Z + surface.Height);
                    vectorBuffer.Update(index + 3, surface.X, surface.Y, surface.Z + surface.Height);
                    break;
                case Ambermoon.Render.SurfaceType.Ceiling:
                    vectorBuffer.Update(index + 1, surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Update(index + 3, surface.X, surface.Y, surface.Z - surface.Height);
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
                    }
                    break;
                case Ambermoon.Render.SurfaceType.Billboard:
                    // TODO
                    vectorBuffer.Update(index + 1, surface.X + surface.Width, surface.Y, surface.Z);
                    vectorBuffer.Update(index + 2, surface.X + surface.Width, surface.Y, surface.Z - surface.Height);
                    vectorBuffer.Update(index + 3, surface.X, surface.Y, surface.Z - surface.Height);
                    break;
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISprite sprite, Position maskSpriteTextureAtlasOffset = null)
        {
            if (textureAtlasOffsetBuffer == null)
                return;

            textureAtlasOffsetBuffer.Update(index, (short)sprite.TextureAtlasOffset.X, (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 1, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)sprite.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 2, (short)(sprite.TextureAtlasOffset.X + sprite.Width), (short)(sprite.TextureAtlasOffset.Y + sprite.Height));
            textureAtlasOffsetBuffer.Update(index + 3, (short)sprite.TextureAtlasOffset.X, (short)(sprite.TextureAtlasOffset.Y + sprite.Height));

            if (Masked && maskSpriteTextureAtlasOffset != null)
            {
                maskTextureAtlasOffsetBuffer.Update(index, (short)maskSpriteTextureAtlasOffset.X, (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 1, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)maskSpriteTextureAtlasOffset.Y);
                maskTextureAtlasOffsetBuffer.Update(index + 2, (short)(maskSpriteTextureAtlasOffset.X + sprite.Width), (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
                maskTextureAtlasOffsetBuffer.Update(index + 3, (short)maskSpriteTextureAtlasOffset.X, (short)(maskSpriteTextureAtlasOffset.Y + sprite.Height));
            }
        }

        public void UpdateTextureAtlasOffset(int index, Render.ISurface3D surface)
        {
            if (textureAtlasOffsetBuffer == null)
                return;

            textureAtlasOffsetBuffer.Update(index, (short)surface.TextureAtlasOffset.X, (short)surface.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 1, (short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)surface.TextureAtlasOffset.Y);
            textureAtlasOffsetBuffer.Update(index + 2, (short)(surface.TextureAtlasOffset.X + surface.TextureWidth), (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight));
            textureAtlasOffsetBuffer.Update(index + 3, (short)surface.TextureAtlasOffset.X, (short)(surface.TextureAtlasOffset.Y + surface.TextureHeight));
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

            if (positionBuffer != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    positionBuffer.Update(index + i, short.MaxValue, short.MaxValue); // ensure it is not visible
                    positionBuffer.Remove(index + i);
                }
            }
            else if (vectorBuffer != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    vectorBuffer.Update(index + i, float.MaxValue, float.MaxValue, float.MaxValue); // ensure it is not visible
                    vectorBuffer.Remove(index + i);
                }
            }

            if (paletteIndexBuffer != null)
            {
                paletteIndexBuffer.Remove(index);
                paletteIndexBuffer.Remove(index + 1);
                paletteIndexBuffer.Remove(index + 2);
                paletteIndexBuffer.Remove(index + 3);
            }

            if (textureAtlasOffsetBuffer != null)
            {
                textureAtlasOffsetBuffer.Remove(index);
                textureAtlasOffsetBuffer.Remove(index + 1);
                textureAtlasOffsetBuffer.Remove(index + 2);
                textureAtlasOffsetBuffer.Remove(index + 3);
            }

            if (maskTextureAtlasOffsetBuffer != null)
            {
                maskTextureAtlasOffsetBuffer.Remove(index);
                maskTextureAtlasOffsetBuffer.Remove(index + 1);
                maskTextureAtlasOffsetBuffer.Remove(index + 2);
                maskTextureAtlasOffsetBuffer.Remove(index + 3);
            }

            if (baseLineBuffer != null)
            {
                baseLineBuffer.Remove(index);
                baseLineBuffer.Remove(index + 1);
                baseLineBuffer.Remove(index + 2);
                baseLineBuffer.Remove(index + 3);
            }

            if (colorBuffer != null)
            {
                colorBuffer.Remove(index);
                colorBuffer.Remove(index + 1);
                colorBuffer.Remove(index + 2);
                colorBuffer.Remove(index + 3);
            }

            if (layerBuffer != null)
            {
                layerBuffer.Remove(index);
                layerBuffer.Remove(index + 1);
                layerBuffer.Remove(index + 2);
                layerBuffer.Remove(index + 3);
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
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    vertexArrayObject?.Dispose();
                    positionBuffer?.Dispose();
                    paletteIndexBuffer?.Dispose();
                    textureAtlasOffsetBuffer?.Dispose();
                    maskTextureAtlasOffsetBuffer?.Dispose();
                    baseLineBuffer?.Dispose();
                    colorBuffer?.Dispose();
                    layerBuffer?.Dispose();
                    indexBuffer?.Dispose();

                    disposed = true;
                }
            }
        }
    }
}
