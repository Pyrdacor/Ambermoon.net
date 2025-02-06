/*
 * VectorBuffer.cs - Buffer for shader 3D position data
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

class VectorBuffer(State state, bool staticData) : BufferObject<float>(state, staticData)
{
    public override int Dimension => 3;

    bool UpdateVectorData(float[] buffer, int index, Tuple<float, float, float> vector)
    {
        bool changed = false;
        float x = vector.Item1;
        float y = vector.Item2;
        float z = vector.Item3;

        if (buffer[index + 0] != x ||
            buffer[index + 1] != y ||
            buffer[index + 2] != z)
        {
            buffer[index + 0] = x;
            buffer[index + 1] = y;
            buffer[index + 2] = z;
            changed = true;
        }

        return index == Size || changed;
    }

    public int Add(float x, float y, float z, int index = -1)
    {
        return base.Add(UpdateVectorData, Tuple.Create(x, y, z), index);
    }

    public void Update(int index, float x, float y, float z)
    {
        base.Update(UpdateVectorData, index, Tuple.Create(x, y, z));
    }
}
