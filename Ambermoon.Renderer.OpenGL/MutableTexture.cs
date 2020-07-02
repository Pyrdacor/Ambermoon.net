/*
 * MutableTexture.cs - OpenGL texture creation
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

using System;

namespace Ambermoon.Renderer
{
    internal class MutableTexture : Texture
    {
        int width = 0;
        int height = 0;
        byte[] data = null;

        public MutableTexture(State state, int width, int height)
            : base(state, width, height)
        {
            this.width = width;
            this.height = height;
            data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }

        public override int Width => width;
        public override int Height => height;

        public void AddSubTexture(Position position, byte[] data, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                Buffer.BlockCopy(data, y * width * 4, this.data, (position.X + (position.Y + y) * Width) * 4, width * 4);
            }
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
        {
            int index = y * Width + x;

            data[index * 4 + 0] = r;
            data[index * 4 + 1] = g;
            data[index * 4 + 2] = b;
            data[index * 4 + 3] = a;
        }

        public void SetPixels(byte[] pixelData)
        {
            if (pixelData == null)
                throw new AmbermoonException(ExceptionScope.Data, "Pixel data was null.");

            if (pixelData.Length != data.Length)
                throw new AmbermoonException(ExceptionScope.Data, "Pixel data size does not match texture data size.");

            System.Buffer.BlockCopy(pixelData, 0, data, 0, pixelData.Length);
        }

        public void Finish(int numMipMapLevels)
        {
            Create(PixelFormat.BGRA8, data, numMipMapLevels);

            data = null;
        }

        public void Resize(int width, int height)
        {
            if (data != null && this.width == width && this.height == height)
                return;

            this.width = width;
            this.height = height;
            data = new byte[width * height * 4]; // initialized with zeros so non-occupied areas will be transparent
        }
    }
}
