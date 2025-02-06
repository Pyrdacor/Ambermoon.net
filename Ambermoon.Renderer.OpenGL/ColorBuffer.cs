/*
 * ColorBuffer.cs - Buffer for shader color data
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

namespace Ambermoon.Renderer.OpenGL;

using Render;

internal class ColorBuffer(State state, bool staticData) : BufferObject<byte>(state, staticData)
{
    public override int Dimension => 4;

    bool UpdateColorData(byte[] buffer, int index, Color color)
    {
        bool changed = false;

        if (buffer[index + 0] != color.R ||
            buffer[index + 1] != color.G ||
            buffer[index + 2] != color.B ||
            buffer[index + 3] != color.A)
        {
            buffer[index + 0] = color.R;
            buffer[index + 1] = color.G;
            buffer[index + 2] = color.B;
            buffer[index + 3] = color.A;
            changed = true;
        }

        return index == Size || changed;
    }

    public int Add(Color color, int index = -1)
    {
        return Add(UpdateColorData, color, index);
    }

    public void Update(int index, Color color)
    {
        Update(UpdateColorData, index, color);
    }
}
