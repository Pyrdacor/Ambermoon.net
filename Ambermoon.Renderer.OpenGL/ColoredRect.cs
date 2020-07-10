/*
 * ColoredRect.cs - Colored rectangle
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

using Ambermoon.Render;

namespace Ambermoon.Renderer
{
    public class ColoredRect : RenderNode, IColoredRect
    {
        protected int drawIndex = -1;
        Color color;
        byte displayLayer = 0;

        public ColoredRect(int width, int height, Color color, byte displayLayer, Rect virtualScreen)
            : base(width, height, virtualScreen)
        {
            this.color = color;
            this.displayLayer = displayLayer;
        }

        public Color Color
        {
            get => color;
            set
            {
                if (color == value) // TODO: add comparison operator to Color!
                    return;

                color = value;

                UpdateColor();
            }
        }

        public byte DisplayLayer
        {
            get => displayLayer;
            set
            {
                if (displayLayer == value)
                    return;

                displayLayer = value;

                UpdateDisplayLayer();
            }
        }

        protected virtual void UpdateDisplayLayer()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateColoredRectDisplayLayer(drawIndex, displayLayer);
        }

        protected override void AddToLayer()
        {
            drawIndex = (Layer as RenderLayer).GetColoredRectDrawIndex(this);
        }

        protected override void RemoveFromLayer()
        {
            if (drawIndex != -1)
            {
                (Layer as RenderLayer).FreeColoredRectDrawIndex(drawIndex);
                drawIndex = -1;
            }
        }

        protected override void UpdatePosition()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateColoredRectPosition(drawIndex, this);
        }

        protected virtual void UpdateColor()
        {
            if (drawIndex != -1) // -1 means not attached to a layer
                (Layer as RenderLayer).UpdateColoredRectColor(drawIndex, color);
        }

        public override void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            base.Resize(width, height);

            UpdatePosition();
        }
    }

    public class ColoredRectFactory : IColoredRectFactory
    {
        readonly Rect virtualScreen = null;

        public ColoredRectFactory(Rect virtualScreen)
        {
            this.virtualScreen = virtualScreen;
        }

        public IColoredRect Create(int width, int height, Render.Color color, byte displayLayer)
        {
            return new ColoredRect(width, height, color, displayLayer, virtualScreen);
        }
    }
}
