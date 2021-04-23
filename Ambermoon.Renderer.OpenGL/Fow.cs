/*
 * Fow.cs - Fog of war
 *
 * Copyright (C) 2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Ambermoon.Render;

namespace Ambermoon.Renderer.OpenGL
{
    public class Fow : RenderNode, IFow
    {
        protected int drawIndex = -1;
        Position center = new Position();
        byte radius = 0;

        public Fow(int width, int height, Position center, byte radius, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            this.center = center;
            this.radius = radius;
        }

        public Position Center
        {
            get => center;
            set
            {
                if (center?.X == value?.X && center?.Y == value?.Y)
                    return;

                center = value == null ? new Position() : new Position(value);

                UpdateCenter();
            }
        }

        public byte Radius
        {
            get => radius;
            set
            {
                if (radius == value)
                    return;

                radius = value;

                UpdateRadius();
            }
        }

        protected virtual void UpdateCenter()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateFOWCenter(drawIndex, center);
        }

        protected virtual void UpdateRadius()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateFOWRadius(drawIndex, radius);
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetDrawIndex(this);
        }

        protected override void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdatePosition(drawIndex, this);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
        }

        protected override void OnClipAreaChanged(bool onScreen, bool needUpdate)
        {
            if (onScreen && needUpdate)
            {
                UpdatePosition();
            }
        }
    }

    public class FowFactory : IFowFactory
    {
        internal Rect VirtualScreen { get; private set; } = null;

        public FowFactory(Rect virtualScreen)
        {
            VirtualScreen = virtualScreen;
        }

        public IFow Create(int width, int height, Position center, byte radius)
        {
            return new Fow(width, height, center, radius, VirtualScreen);
        }
    }
}
