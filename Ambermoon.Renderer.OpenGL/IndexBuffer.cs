/*
 * IndexBuffer.cs - Dynamic buffer for vertex indices
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
using System.Collections.Generic;

namespace Ambermoon.Renderer.OpenGL;

internal class IndexBuffer : BufferObject<uint>
{
    public override int Dimension => 6;

    public IndexBuffer(State state)
        : base(state, true)
    {
        BufferTarget = GLEnum.ElementArrayBuffer;
    }

    bool InsertIndexData(uint[] buffer, int index, uint startIndex)
    {
        buffer[index++] = startIndex + 0;
        buffer[index++] = startIndex + 1;
        buffer[index++] = startIndex + 2;
        buffer[index++] = startIndex + 3;
        buffer[index++] = startIndex + 0;
        buffer[index++] = startIndex + 2;

        return true;
    }

    public void InsertQuad(int quadIndex)
    {
        if (quadIndex >= int.MaxValue / 6)
            throw new OutOfMemoryException("Too many polygons to render.");

        int arrayIndex = quadIndex * 6; // 2 triangles with 3 vertices each
        uint vertexIndex = (uint)(quadIndex * 4); // 4 different vertices form a quad

        while (Size <= arrayIndex + 6)
        {
            base.Add(InsertIndexData, (uint)vertexIndex, quadIndex);
        }
    }

    internal void EnsureCorrectRenderOrder(ByteBuffer layerBuffer)
    {
        if (layerBuffer.Size == 0)
            return;

        // We expect always 4 equal values
        var layerValues = layerBuffer.Buffer;
        var quads = new List<KeyValuePair<byte, int>>(layerBuffer.Size / 4);

        for (int i = 0; i < layerBuffer.Size / 4; i++)
        {
            byte layer = layerValues[i * 4];
            quads.Add(new KeyValuePair<byte, int>(layer, i));
        }

        // Sort quads by layer (lower values first), then by index
        quads.Sort((a, b) =>
        {
            int result = a.Key.CompareTo(b.Key);

            if (result == 0)
                return a.Value.CompareTo(b.Value);

            return result;
        });

        int bufferIndex = 0;

        foreach (var quad in quads)
        {
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 0);
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 1);
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 2);
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 3);
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 0);
            Buffer[bufferIndex++] = (uint)(quad.Value * 4 + 2);
        }
    }
}
