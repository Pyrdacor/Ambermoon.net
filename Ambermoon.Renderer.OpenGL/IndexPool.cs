/*
 * IndexPool.cs - Pool of indices which handles index reusing
 *
 * Copyright (C) 2020-2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System.Collections.Generic;

namespace Ambermoon.Renderer.OpenGL;

internal class IndexPool
{
    readonly LinkedList<int> releasedIndices = [];
    readonly Dictionary<int, LinkedListNode<int>> releasedIndexNodesByIndex = [];
    int firstFree = 0;
    readonly object releasedIndicesLock = new();

    public int AssignNextFreeIndex(out bool reused)
    {
        lock (releasedIndicesLock)
        {
            if (releasedIndices.Count != 0)
            {
                reused = true;

                int index = releasedIndices.Last.Value;

                releasedIndices.RemoveLast();
                releasedIndexNodesByIndex.Remove(index);

                return index;
            }

            reused = false;

            if (firstFree == int.MaxValue)
            {
                throw new AmbermoonException(ExceptionScope.Render, "No free index available.");
            }

            return firstFree++;
        }
    }

    public void UnassignIndex(int index)
    {
        lock (releasedIndicesLock)
        {
            releasedIndexNodesByIndex.Add(index, releasedIndices.AddLast(index));
        }
    }

    public bool AssignIndex(int index)
    {
        lock (releasedIndicesLock)
        {
            if (releasedIndexNodesByIndex.TryGetValue(index, out var indexNode))
            {
                releasedIndices.Remove(indexNode);
                releasedIndexNodesByIndex.Remove(index);
                return true;
            }

            if (index == firstFree)
                ++firstFree;

            return false;
        }
    }
}
