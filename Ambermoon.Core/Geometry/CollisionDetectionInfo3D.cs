/*
 * CollisionDetectionInfo3D.cs - Collision detection for 3D maps
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
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Geometry
{
    public interface ICollisionBody
    {
        bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player);
    }

    public class CollisionLine3D : ICollisionBody
    {
        public float X { get; set; }
        public float Z { get; set; }
        public float Length { get; set; }
        public bool Horizontal { get; set; }
        public bool PlayerCanPass { get; set; }

        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            if (player && PlayerCanPass)
                return false;

            if (Horizontal)
            {
                if (z < Z == lastZ < Z && Math.Abs(z - Z) >= bodyRadius)
                    return false;

                float left = x - bodyRadius;
                float right = x + bodyRadius;

                return (left > X && left < X + Length) ||
                    (right > X && right < X + Length);
            }
            else
            {
                if (x < X == lastX < X && Math.Abs(x - X) >= bodyRadius)
                    return false;

                float top = z + bodyRadius;
                float bottom = z - bodyRadius;

                return (top < Z && top > Z - Length) ||
                    (bottom < Z && bottom > Z - Length);
            }
        }
    }

    public class CollisionSphere3D : ICollisionBody
    {
        public float CenterX { get; set; }
        public float CenterZ { get; set; }
        public float Radius { get; set; }
        public bool PlayerCanPass { get; set; }

        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            if (player && PlayerCanPass)
                return false;

            float xDist = Math.Abs(x - CenterX) - bodyRadius;
            float zDist = Math.Abs(z - CenterZ) - bodyRadius;
            float safeDist = Radius;

            if (xDist >= safeDist ||
                zDist >= safeDist)
                return false;

            if (xDist <= 0.0f || zDist <= 0.0f)
                return true;

            return Math.Sqrt(xDist * xDist + zDist * zDist) < safeDist;
        }
    }

    public class CollisionDetectionInfo3D
    {
        public List<ICollisionBody> CollisionBodies { get; } = new List<ICollisionBody>();
        
        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            return CollisionBodies.Any(b => b.TestCollision(lastX, lastZ, x, z, bodyRadius, player));
        }
    }
}
