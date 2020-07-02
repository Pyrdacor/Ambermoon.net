/*
 * VertexArrayBuffer.cs - OpenGL VAO handling
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
using System.Collections.Generic;
using System.Threading;

namespace Ambermoon.Renderer
{
    // VAO
    internal class VertexArrayObject : IDisposable
    {
        uint index = 0;
        readonly Dictionary<string, PositionBuffer> positionBuffers = new Dictionary<string, PositionBuffer>(4);
        readonly Dictionary<string, BaseLineBuffer> baseLineBuffers = new Dictionary<string, BaseLineBuffer>(1);
        readonly Dictionary<string, ColorBuffer> colorBuffers = new Dictionary<string, ColorBuffer>(4);
        readonly Dictionary<string, LayerBuffer> layerBuffers = new Dictionary<string, LayerBuffer>(1);
        readonly Dictionary<string, IndexBuffer> indexBuffers = new Dictionary<string, IndexBuffer>(4);
        readonly Dictionary<string, int> bufferLocations = new Dictionary<string, int>();
        bool disposed = false;
        bool buffersAreBound = false;
        ShaderProgram program = null;
        readonly object vaoLock = new object();
        readonly State state = null;

        public static VertexArrayObject ActiveVAO { get; private set; } = null;

        public VertexArrayObject(State state, ShaderProgram program)
        {
            this.program = program;
            this.state = state;

            Create();
        }

        void Create()
        {
            index = state.Gl.GenVertexArray();
        }

        public void Lock()
        {
            Monitor.Enter(vaoLock);
        }

        public void Unlock()
        {
            Monitor.Exit(vaoLock);
        }

        public void AddBuffer(string name, PositionBuffer buffer)
        {
            positionBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, BaseLineBuffer buffer)
        {
            baseLineBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, ColorBuffer buffer)
        {
            colorBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, LayerBuffer buffer)
        {
            layerBuffers.Add(name, buffer);
        }

        public void AddBuffer(string name, IndexBuffer buffer)
        {
            indexBuffers.Add(name, buffer);
        }

        public void BindBuffers()
        {
            if (buffersAreBound)
                return;

            lock (vaoLock)
            {
                program.Use();
                InternalBind(true);

                foreach (var buffer in positionBuffers)
                {
                    bufferLocations[buffer.Key] = (int)program.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in baseLineBuffers)
                {
                    bufferLocations[buffer.Key] = (int)program.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in colorBuffers)
                {
                    bufferLocations[buffer.Key] = (int)program.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in layerBuffers)
                {
                    bufferLocations[buffer.Key] = (int)program.BindInputBuffer(buffer.Key, buffer.Value);
                }

                foreach (var buffer in indexBuffers)
                {
                    buffer.Value.Bind();
                }

                buffersAreBound = true;
            }
        }

        public void UnbindBuffers()
        {
            if (!buffersAreBound)
                return;

            lock (vaoLock)
            {
                program.Use();
                InternalBind(true);

                foreach (var buffer in positionBuffers)
                {
                    program.UnbindInputBuffer((uint)bufferLocations[buffer.Key]);
                    bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in baseLineBuffers)
                {
                    program.UnbindInputBuffer((uint)bufferLocations[buffer.Key]);
                    bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in colorBuffers)
                {
                    program.UnbindInputBuffer((uint)bufferLocations[buffer.Key]);
                    bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in layerBuffers)
                {
                    program.UnbindInputBuffer((uint)bufferLocations[buffer.Key]);
                    bufferLocations[buffer.Key] = -1;
                }

                foreach (var buffer in indexBuffers)
                {
                    buffer.Value.Unbind();
                }

                buffersAreBound = false;
            }
        }

        public void Bind()
        {
            InternalBind(false);
        }

        void InternalBind(bool bindOnly)
        {
            lock (vaoLock)
            {
                if (ActiveVAO != this)
                {
                    state.Gl.BindVertexArray(index);
                    program.Use();
                }

                if (!bindOnly)
                {
                    bool buffersChanged = false;

                    // ensure that all buffers are up to date
                    foreach (var buffer in positionBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in baseLineBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in colorBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in layerBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    foreach (var buffer in indexBuffers)
                    {
                        if (buffer.Value.RecreateUnbound())
                            buffersChanged = true;
                    }

                    if (buffersChanged)
                    {
                        UnbindBuffers();
                        BindBuffers();
                    }
                }

                ActiveVAO = this;
            }
        }

        public static void Bind(VertexArrayObject vao)
        {
            if (vao != null)
                vao.Bind();
            else if (ActiveVAO != null)
                ActiveVAO.Unbind();
        }

        public void Unbind()
        {
            if (ActiveVAO == this)
            {
                state.Gl.BindVertexArray(0);
                ActiveVAO = null;
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
                    if (index != 0)
                    {
                        if (ActiveVAO == this)
                            Unbind();

                        state.Gl.DeleteVertexArray(index);
                        index = 0;
                    }

                    disposed = true;
                }
            }
        }
    }
}