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

namespace Ambermoon.Renderer
{
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
    }
}
