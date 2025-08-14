/*
 * BufferObject.cs - Base class for integer based data buffers
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
using System.Runtime.InteropServices;

namespace Ambermoon.Renderer.OpenGL;

internal abstract class BufferObject<T> : IDisposable where T : unmanaged, IEquatable<T>
{
    public abstract int Dimension { get; }
    public bool Normalized { get; protected set; } = false;
    public int Size { get; protected set; }
    protected GLEnum BufferTarget { get; set; } = GLEnum.ArrayBuffer;
    public static VertexAttribPointerType Type => typeof(T).Name.ToLower() switch
    {
        "byte" => VertexAttribPointerType.UnsignedByte,
        "int16" => VertexAttribPointerType.Short,
        "uint16" => VertexAttribPointerType.UnsignedShort,
        "uint32" => VertexAttribPointerType.UnsignedInt,
        "single" => VertexAttribPointerType.Float,
        _ => throw new Exception("Invalid buffer data type")
    };
    uint index = 0;
    bool disposed = false;
    T[] buffer = null;
    readonly object bufferLock = new();
    readonly IndexPool indices = new();
    bool changedSinceLastCreation = true;
    readonly GLEnum usageHint = GLEnum.DynamicDraw;
    readonly State state;

    protected delegate bool DataUpdater<U>(T[] buffer, int index, U value);

    internal T[] Buffer
    {
        get
        {
            lock (bufferLock)
            {
                return buffer;
            }
        }
    }

    protected BufferObject(State state, bool staticData)
    {
        this.state = state;
        index = state.Gl.GenBuffer();

        if (staticData)
            usageHint = GLEnum.StaticDraw;
    }

    bool DefaultUpdater(T[] buffer, int index, T value)
    {
        if (!buffer[index].Equals(value))
        {
            buffer[index] = value;
            return true;
        }

        return index == Size;
    }

    public int Add(T value, int index = -1)
    {
        return Add(DefaultUpdater, value, index);
    }

    protected int Add<U>(DataUpdater<U> inserter, U value, int index = -1)
    {
        bool reused;

        if (index == -1)
            index = indices.AssignNextFreeIndex(out reused);
        else
            reused = indices.AssignIndex(index);

        if (buffer == null)
        {
            buffer = new T[128];
            inserter(buffer, 0, value);
            Size += Dimension;
            changedSinceLastCreation = true;
        }
        else
        {
            buffer = EnsureBufferSize(buffer, (index + 1) * Dimension, out bool changed);

            if (!reused)
            {
                Size += Dimension;
                changed = true;
            }

            int bufferIndex = index * Dimension;

            if (inserter(buffer, bufferIndex, value) || changed)
            {
                changedSinceLastCreation = true;
            }
        }

        return index;
    }

    public void Update(int index, T value)
    {
        Update(DefaultUpdater, index, value);
    }

    protected void Update<U>(DataUpdater<U> updater, int index, U value)
    {
        if (buffer == null)
            return; // already disposed

        if (updater(buffer, index * Dimension, value))
            changedSinceLastCreation = true;
    }

    public void Remove(int index)
    {
        indices.UnassignIndex(index);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            state.Gl.BindBuffer(BufferTarget, 0);

            if (index != 0)
            {
                state.Gl.DeleteBuffer(index);

                if (buffer != null)
                {
                    lock (bufferLock)
                    {
                        buffer = null;
                    }
                }

                Size = 0;
                index = 0;
            }

            disposed = true;
        }
    }

    public void Bind()
    {
        if (disposed)
            throw new Exception("Tried to bind a disposed buffer.");

        state.Gl.BindBuffer(BufferTarget, index);

        Recreate(); // ensure that the data is up to date
    }

    public void Unbind()
    {
        if (disposed)
            return;

        state.Gl.BindBuffer(BufferTarget, 0);
    }

    void Recreate() // is only called when the buffer is bound (see Bind())
    {
        if (!changedSinceLastCreation || buffer == null)
            return;

        lock (bufferLock)
        {
            state.Gl.BufferData(BufferTarget, (uint)(Size * Marshal.SizeOf<T>()),
                new ReadOnlySpan<T>(buffer), usageHint);
        }

        changedSinceLastCreation = false;
    }

    internal bool RecreateUnbound()
    {
        if (!changedSinceLastCreation || buffer == null)
            return false;

        if (disposed)
            throw new Exception("Tried to recreate a disposed buffer.");

        state.Gl.BindBuffer(BufferTarget, index);

        lock (bufferLock)
        {
            state.Gl.BufferData(BufferTarget, (uint)(Size * Marshal.SizeOf<T>()),
                new ReadOnlySpan<T>(buffer), usageHint);
        }

        changedSinceLastCreation = false;

        return true;
    }

    protected static T[] EnsureBufferSize(T[] buffer, int size, out bool changed)
    {
        changed = false;

        if (buffer == null)
        {
            changed = true;

            // first we just use a 256B buffer
            return new T[256];
        }
        else if (buffer.Length <= size) // we need to recreate the buffer
        {
            changed = true;

            if (buffer.Length < 0xffff) // double size up to 64K
                Array.Resize(ref buffer, buffer.Length << 1);
            else // increase by 1K after 64K reached
                Array.Resize(ref buffer, buffer.Length + 1024);
        }

        return buffer;
    }
}
