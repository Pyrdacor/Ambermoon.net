/*
 * ScreenRenderBuffer.cs - Renders the screen's framebuffer
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

using Silk.NET.OpenGL;
using System;

namespace Ambermoon.Renderer
{
    internal class ScreenRenderBuffer : IDisposable
    {
        bool disposed = false;
        readonly State state;

        readonly VertexArrayObject vertexArrayObject = null;
        readonly PositionBuffer positionBuffer = null;
        readonly IndexBuffer indexBuffer = null;

        public ScreenRenderBuffer(State state, ScreenShader screenShader)
        {
            this.state = state;
            vertexArrayObject = new VertexArrayObject(state, screenShader.ShaderProgram);
            positionBuffer = new PositionBuffer(state, true);
            positionBuffer.Add(0, 0, 0);
            positionBuffer.Add(Global.VirtualScreenWidth, 0, 1);
            positionBuffer.Add(Global.VirtualScreenWidth, Global.VirtualScreenHeight, 2);
            positionBuffer.Add(0, Global.VirtualScreenHeight, 3);
            vertexArrayObject.AddBuffer(ScreenShader.DefaultPositionName, positionBuffer);
            indexBuffer = new IndexBuffer(state);
            indexBuffer.InsertQuad(0);
            vertexArrayObject.AddBuffer("index", indexBuffer);
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
                    state.Gl.DrawElements(PrimitiveType.Triangles, (uint)(positionBuffer.Size / 4) * 3, DrawElementsType.UnsignedInt, (void*)0);
                    vertexArrayObject.Unbind();
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

                disposed = true;
            }
        }
    }
}
