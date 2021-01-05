/*
 * PositionBuffer.cs - Buffer for shader position data
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

namespace Ambermoon.Renderer
{
    internal class PositionBuffer : BufferObject<short>
    {
        public override int Dimension => 2;

        public PositionBuffer(State state, bool staticData)
            : base(state, staticData)
        {

        }

        bool UpdatePositionData(short[] buffer, int index, Tuple<short, short> position)
        {
            bool changed = false;
            short x = position.Item1;
            short y = position.Item2;

            if (buffer[index + 0] != x ||
                buffer[index + 1] != y)
            {
                buffer[index + 0] = x;
                buffer[index + 1] = y;
                changed = true;
            }

            return index == Size || changed;
        }

        public int Add(short x, short y, int index = -1)
        {
            return base.Add(UpdatePositionData, Tuple.Create(x, y), index);
        }

        public void Update(int index, short x, short y)
        {
            base.Update(UpdatePositionData, index, Tuple.Create(x, y));
        }
    }
}
