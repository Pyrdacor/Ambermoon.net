/*
 * Texture.cs - Basic texture interface
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

namespace Ambermoon.Render
{
    public abstract class Texture
    {
        public enum PixelFormat
        {
            RGBA8 = 0,
            BGRA8,
            RGB8,
            BGR8,
            Alpha,
            RGB5A1,
            R5G6B5,
            BGR5A1,
            B5G6R5
        }

        protected static readonly uint[] BytesPerPixel = new uint[9]
        {
            4,
            4,
            3,
            3,
            1,
            2,
            2,
            2,
            2
        };

        public abstract int Width { get; }
        public abstract int Height { get; }

        protected static byte[] Convert16BitColorWithAlpha(byte[] pixelData)
        {
            int numPixels = pixelData.Length / 2; // 16 bits (2 bytes) per pixel
            var buffer = new byte[numPixels * 4]; // new format has 4 components (RGBA) one byte each

            // Note: RGBA can also be BGRA. The comments below are for RGBA.
            // The order is the same in source and destination so the same
            // code can be used. Only the meaning of the bytes is different.

            for (int i = 0; i < numPixels; ++i)
            {
                var b1 = pixelData[i * 2 + 0];
                var b2 = pixelData[i * 2 + 1];

                // Byte1     Byte2
                // RRRRRGGG  GGBBBBBA
                buffer[i * 4 + 0] = (byte)((b1 >> 3) * 8 + 4); // R
                buffer[i * 4 + 1] = (byte)((((b1 & 0x07) << 2) | (b2 >> 6)) * 8 + 4); // G
                buffer[i * 4 + 2] = (byte)(((b2 >> 1) & 0x1f) * 8 + 4); // B
                buffer[i * 4 + 3] = (byte)((b2 & 0x1) * 255); // A
            }

            return buffer;
        }

        protected static byte[] Convert16BitColorWithoutAlpha(byte[] pixelData)
        {
            int numPixels = pixelData.Length / 2; // 16 bits (2 bytes) per pixel
            var buffer = new byte[numPixels * 3]; // new format has 3 components (RGB) one byte each

            // Note: RGB can also be BGR. The comments below are for RGB.
            // The order is the same in source and destination so the same
            // code can be used. Only the meaning of the bytes is different.

            for (int i = 0; i < numPixels; ++i)
            {
                var b1 = pixelData[i * 2 + 0];
                var b2 = pixelData[i * 2 + 1];

                // Byte1     Byte2
                // RRRRRGGG  GGGBBBBB
                buffer[i * 4 + 0] = (byte)((b1 >> 3) * 8 + 4); // R
                buffer[i * 4 + 1] = (byte)((((b1 & 0x07) << 3) | (b2 >> 5)) * 4 + 2); // G
                buffer[i * 4 + 2] = (byte)((b2 & 0x1f) * 8 + 4); // B
            }

            return buffer;
        }

        protected static byte[] ConvertPixelData(byte[] pixelData, ref PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.RGB5A1:
                    format = PixelFormat.RGBA8;
                    return Convert16BitColorWithAlpha(pixelData);
                case PixelFormat.R5G6B5:
                    format = PixelFormat.RGB8;
                    return Convert16BitColorWithoutAlpha(pixelData);
                case PixelFormat.BGR5A1:
                    format = PixelFormat.BGRA8;
                    return Convert16BitColorWithAlpha(pixelData);
                case PixelFormat.B5G6R5:
                    format = PixelFormat.BGR8;
                    return Convert16BitColorWithoutAlpha(pixelData);
                default:
                    return pixelData;
            }
        }
    }

    public interface IMinimapTextureFactory
    {
        Texture GetMinimapTexture();
        void ResizeMinimapTexture(int width, int height);
        void FillMinimapTexture(byte[] colorData);
    }
}
