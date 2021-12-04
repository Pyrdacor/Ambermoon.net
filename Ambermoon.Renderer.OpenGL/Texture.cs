/*
 * Texture.cs - OpenGL texture handling
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

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using System;
using System.IO;

namespace Ambermoon.Renderer
{
    public class Texture : Render.Texture, IDisposable
    {
        public static Texture ActiveTexture { get; private set; } = null;

        public virtual uint Index { get; private set; } = 0u;
        public override int Width { get; } = 0;
        public override int Height { get; } = 0;
        readonly State state = null;
        bool disposed = false;

        protected Texture(State state, int width, int height)
        {
            this.state = state;
            Index = state.Gl.GenTexture();
            Width = width;
            Height = height;
        }

        public Texture(State state, int width, int height, PixelFormat format, Stream pixelDataStream, int numMipMapLevels = 0)
        {
            int size = width * height * (int)BytesPerPixel[(int)format];

            if ((pixelDataStream.Length - pixelDataStream.Position) < size)
                throw new Exception("Pixel data stream does not contain enough data.");

            if (!pixelDataStream.CanRead)
                throw new Exception("Pixel data stream does not support reading.");

            byte[] pixelData = new byte[size];

            pixelDataStream.Read(pixelData, 0, size);

            this.state = state;
            Index = state.Gl.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);
        }

        public Texture(State state, int width, int height, PixelFormat format, byte[] pixelData, int numMipMapLevels = 0)
        {
            if (width * height * BytesPerPixel[(int)format] != pixelData.Length)
                throw new Exception("Invalid texture data size.");

            this.state = state;
            Index = state.Gl.GenTexture();
            Width = width;
            Height = height;

            Create(format, pixelData, numMipMapLevels);
        }

        static GLEnum ToOpenGLPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGBA8:
                    return GLEnum.Rgba;
                case PixelFormat.RGB8:
                    return GLEnum.Rgb;
                case PixelFormat.Alpha:
                    // Note: for the supported image format GL_RED means one channel data, GL_ALPHA is only used for texture storage on the gpu, so we don't use it
                    // We always use RGBA8 as texture storage on the gpu
                    return GLEnum.Red;
                default:
                    throw new Exception("Invalid pixel format.");
            }
        }

        protected void Create(PixelFormat format, byte[] pixelData, int numMipMapLevels)
        {
            if (format >= PixelFormat.RGB5A1)
            {
                pixelData = ConvertPixelData(pixelData, ref format);
            }

            Bind();

            var minMode = (numMipMapLevels > 0) ? TextureMinFilter.NearestMipmapNearest : TextureMinFilter.Nearest;

            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minMode);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            state.Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            unsafe
            {
                fixed (byte* ptr = &pixelData[0])
                {
                    state.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)Width, (uint)Height, 0, ToOpenGLPixelFormat(format), GLEnum.UnsignedByte, ptr);
                }
            }

            if (numMipMapLevels > 0)
                state.Gl.GenerateMipmap(GLEnum.Texture2D);
        }

        public virtual void Bind()
        {
            if (disposed)
                throw new Exception("Tried to bind a disposed texture.");

            if (ActiveTexture == this)
                return;

            state.Gl.BindTexture(TextureTarget.Texture2D, Index);
            ActiveTexture = this;
        }

        public void Unbind()
        {
            if (ActiveTexture == this)
            {
                state.Gl.BindTexture(TextureTarget.Texture2D, 0);
                ActiveTexture = null;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (ActiveTexture == this)
                    Unbind();

                if (Index != 0)
                {
                    state.Gl.DeleteTexture(Index);
                    Index = 0;
                }

                disposed = true;
            }
        }
    }
}
