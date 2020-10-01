/*
 * Node.cs - Basic render node implementation
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
using System;

namespace Ambermoon.Renderer
{
    public abstract class RenderNode : IRenderNode
    {
        int x = short.MaxValue;
        int y = short.MaxValue;
        bool visible = false;
        IRenderLayer layer = null;
        bool visibleRequest = false;
        bool deleted = false;
        bool notOnScreen = true;
        protected readonly Rect virtualScreen = null;
        Rect clipArea = null;

        protected RenderNode(int width, int height, Rect virtualScreen)
        {
            Width = width;
            Height = height;
            this.virtualScreen = virtualScreen;
        }

        public bool Visible
        {
            get => visible && !deleted && !notOnScreen;
            set
            {
                if (deleted)
                    return;

                if (layer == null)
                {
                    visibleRequest = value;
                    visible = false;
                    return;
                }

                visibleRequest = false;

                if (visible == value)
                    return;

                visible = value;

                OnVisibilityChanged();
            }
        }

        public IRenderLayer Layer
        {
            get => layer;
            set
            {
                if (value != null && !(value is RenderLayer))
                    throw new InvalidCastException("The given layer is not valid for this renderer.");

                if (layer == value)
                    return;

                if (layer != null && Visible)
                    RemoveFromLayer();

                layer = value;

                if (layer != null && visibleRequest && !deleted)
                {
                    visible = true;
                    visibleRequest = false;
                    CheckOnScreen();
                }

                if (layer == null)
                {
                    visibleRequest = false;
                    visible = false;
                    notOnScreen = true;
                }

                if (layer != null && Visible)
                    AddToLayer();
            }
        }

        public Rect ClipArea
        {
            get => clipArea;
            set
            {
                if (clipArea == value)
                    return;

                clipArea = value;
                bool handled = CheckOnScreen();
                OnClipAreaChanged(!notOnScreen, !handled);
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public virtual void Resize(int width, int height)
        {
            if (Width != width || Height != height)
            {
                Width = width;
                Height = height;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        protected abstract void AddToLayer();

        protected abstract void RemoveFromLayer();

        protected abstract void UpdatePosition();

        protected virtual void OnVisibilityChanged()
        {
            if (Visible)
                AddToLayer();
            else
                RemoveFromLayer();
        }

        protected abstract void OnClipAreaChanged(bool onScreen, bool needUpdate);

        bool CheckOnScreen()
        {
            bool oldNotOnScreen = notOnScreen;
            bool oldVisible = Visible;
            var area = clipArea ?? virtualScreen;

            notOnScreen = !area.IntersectsWith(new Rect(X, Y, Width, Height));

            if (oldNotOnScreen != notOnScreen)
            {
                if (oldVisible != Visible)
                {
                    OnVisibilityChanged();
                    return true; // handled
                }
            }

            return false;
        }

        public void Delete()
        {
            if (!deleted)
            {
                RemoveFromLayer();
                deleted = true;
                visible = false;
                visibleRequest = false;
            }
        }

        public int X
        {
            get => x;
            set
            {
                if (x == value)
                    return;

                x = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public int Y
        {
            get => y;
            set
            {
                if (y == value)
                    return;

                y = value;

                if (!deleted)
                {
                    if (!CheckOnScreen())
                        UpdatePosition();
                }
            }
        }

        public bool InsideClipArea(Rect area)
        {
            if (area == null)
                return true;

            return area.IntersectsWith(X, Y, Width, Height);
        }
    }
}
