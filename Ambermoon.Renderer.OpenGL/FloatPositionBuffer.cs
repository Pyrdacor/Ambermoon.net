/*
 * FloatPositionBuffer.cs - Buffer for shader floating-point position data
 *
 * Copyright (C) 2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    internal class FloatPositionBuffer : BufferObject<float>
    {
        public override int Dimension => 2;

        public FloatPositionBuffer(State state, bool staticData)
            : base(state, staticData)
        {

        }

        bool UpdatePositionData(float[] buffer, int index, Tuple<float, float> position)
        {
            bool changed = false;
            float x = position.Item1;
            float y = position.Item2;

            if (!Util.FloatEqual(buffer[index + 0], x) ||
                !Util.FloatEqual(buffer[index + 1], y))
            {
                buffer[index + 0] = x;
                buffer[index + 1] = y;
                changed = true;
            }

            return changed || index == Size;
        }

        public int Add(float x, float y, int index = -1)
        {
            return Add(UpdatePositionData, Tuple.Create(x, y), index);
        }

        public void Update(int index, float x, float y)
        {
            Update(UpdatePositionData, index, Tuple.Create(x, y));
        }

        public void TransformAll(Func<int, Tuple<float, float>, Tuple<float, float>> updater)
        {
            bool TransformPositionData(float[] buffer, int index, Tuple<float, float> _)
            {
                bool changed = false;
                var position = Tuple.Create(buffer[index + 0], buffer[index + 1]);
                var newPosition = updater(index, position);
                float x = newPosition.Item1;
                float y = newPosition.Item2;

                if (!Util.FloatEqual(buffer[index + 0], x) ||
                    !Util.FloatEqual(buffer[index + 1], y))
                {
                    buffer[index + 0] = x;
                    buffer[index + 1] = y;
                    changed = true;
                }

                return changed || index == Size;
            }

            for (int i = 0; i < Size; ++i)
            {
                Update<Tuple<float, float>>(TransformPositionData, i, null);
            }
        }
    }
}
