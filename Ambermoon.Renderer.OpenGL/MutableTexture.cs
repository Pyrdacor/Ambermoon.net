/*
 * MutableTexture.cs - OpenGL texture creation
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

using System;

namespace Ambermoon.Renderer.OpenGL;

internal class MutableTexture : Texture
{
    uint bytesPerPixel = 4;
    int width = 0;
    int height = 0;
    byte[] data = null;

    public MutableTexture(State state, int width, int height, uint bytesPerPixel)
        : base(state, width, height)
    {
        this.bytesPerPixel = bytesPerPixel;
        this.width = width;
        this.height = height;
        data = new byte[width * height * bytesPerPixel]; // initialized with zeros so non-occupied areas will be transparent
    }

    public override int Width => width;
    public override int Height => height;

    public void AddSubTexture(Position position, byte[] data, int width, int height)
    {
        for (int y = 0; y < height; ++y)
        {
            Buffer.BlockCopy(data, y * width * (int)bytesPerPixel, this.data, (position.X + (position.Y + y) * Width) * (int)bytesPerPixel, width * (int)bytesPerPixel);
        }
    }

    public void Finish(int numMipMapLevels)
    {
        var pixelFormat = bytesPerPixel switch
        {
            1 => PixelFormat.Alpha,
            4 => PixelFormat.RGBA8,
            _ => throw new ArgumentOutOfRangeException($"Unsupported bytes per pixel value: {bytesPerPixel}")
        };

        Create(pixelFormat, data, numMipMapLevels);

        data = null;
    }

    public void Resize(int width, int height)
    {
        if (data != null && this.width == width && this.height == height)
            return;

        this.width = width;
        this.height = height;
        data = new byte[width * height * bytesPerPixel]; // initialized with zeros so non-occupied areas will be transparent
    }
}
