/*
 * Cursor.cs - Graphical mouse cursor
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

using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    public class Cursor
    {
        readonly IGameRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly ISprite sprite;
        readonly Dictionary<CursorType, Position> cursorHotspots = [];
        CursorType type = CursorType.Sword;
        internal Position Hotspot { get; private set; } = null;
        protected virtual bool Visible
        {
            get => sprite.Visible;
            set => sprite.Visible = value;
        }

        public Cursor(IGameRenderView renderView, IReadOnlyList<Position> cursorHotspots, TextureAtlasManager textureAtlasManager = null)
        {
            this.renderView = renderView;
            textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.Cursor);
            sprite = renderView.SpriteFactory.Create(16, 16, true);
            sprite.PaletteIndex = 0;
            sprite.Layer = renderView.GetLayer(Layer.Cursor);

            for (int i = 0; i < cursorHotspots.Count; ++i)
                this.cursorHotspots.Add((CursorType)i, cursorHotspots[i]);

            UpdateCursor();
        }

        public void Destroy()
        {
            sprite?.Delete();
        }

        public CursorType Type
        {
            get => type;
            set
            {
                if (type == value)
                    return;

                type = value;

                if (Type == CursorType.None)
                {
					Visible = false;
                }
                else
                {
                    UpdateCursor();
					Visible = true;
                }
            }
        }

        void UpdateCursor()
        {
            lock (sprite)
            {
                var hotspot = Hotspot ?? new Position();
                int x = sprite.X + hotspot.X;
                int y = sprite.Y + hotspot.Y;
                Hotspot = cursorHotspots[type];
                sprite.X = x - Hotspot.X;
                sprite.Y = y - Hotspot.Y;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)type);
            }
        }

        public void UpdatePosition(Position screenPosition, Game game)
        {
            var viewPosition = renderView.ScreenToGame(screenPosition);

            if (viewPosition != null)
            {
                lock (sprite)
                {
                    sprite.PaletteIndex = game?.UIPaletteIndex ?? 0;
                    sprite.X = viewPosition.X - Hotspot.X;
                    sprite.Y = viewPosition.Y - Hotspot.Y;
					Visible = Type != CursorType.None;
                }
            }
        }

        public void UpdatePalette(Game game)
        {
            lock (sprite)
            {
                sprite.PaletteIndex = game?.UIPaletteIndex ?? 0;
            }
        }
    }

	public class InvisibleCursor : Cursor
	{
		public InvisibleCursor(IGameRenderView renderView, IReadOnlyList<Position> cursorHotspots, TextureAtlasManager textureAtlasManager = null)
            : base(renderView, cursorHotspots, textureAtlasManager)
		{
		}

		protected override bool Visible { get => false; set => base.Visible = false; }
	}
}
