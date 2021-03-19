/*
 * IndexPool.cs - Pool of indices which handles index reusing
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

using System.Collections.Generic;

namespace Ambermoon.Renderer
{
    internal class IndexPool
    {
        readonly List<int> releasedIndices = new List<int>();
        int firstFree = 0;

        public int AssignNextFreeIndex(out bool reused)
        {
            if (releasedIndices.Count != 0)
            {
                reused = true;

                int index = releasedIndices[^1];

                // Remove last has O(1) while remove first has O(n)!
                releasedIndices.RemoveAt(releasedIndices.Count - 1);

                return index;
            }

            reused = false;

            if (firstFree == int.MaxValue)
            {
                throw new AmbermoonException(ExceptionScope.Render, "No free index available.");
            }

            return firstFree++;
        }

        public void UnassignIndex(int index)
        {
            releasedIndices.Add(index);
        }

        public bool AssignIndex(int index)
        {
            // The logic should prefer the last index so that this is much faster.
            if (releasedIndices.Count != 0)
            {
                if (releasedIndices[^1] == index)
                {
                    releasedIndices.RemoveAt(releasedIndices.Count - 1);
                    return true;
                }
                else if (releasedIndices.Contains(index))
                {
                    releasedIndices.Remove(index);
                    return true;
                }
            }

            if (index == firstFree)
                ++firstFree;

            return false;
        }
    }
}
