using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    public class Cursor
    {
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly ISprite sprite;
        readonly Dictionary<CursorType, Position> cursorHotspots = new Dictionary<CursorType, Position>();
        CursorType type = CursorType.Sword;
        Position hotspot = null;

        public Cursor(IRenderView renderView, IReadOnlyList<Position> cursorHotspots)
        {
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Cursor);
            sprite = renderView.SpriteFactory.Create(16, 16, 0, 0, false, true);
            sprite.PaletteIndex = 0;
            sprite.Layer = renderView.GetLayer(Layer.Cursor);

            for (int i = 0; i < cursorHotspots.Count; ++i)
                this.cursorHotspots.Add((CursorType)i, cursorHotspots[i]);

            UpdateCursor();
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
                    sprite.Visible = false;
                }
                else
                {
                    UpdateCursor();
                    sprite.Visible = true;
                }
            }
        }

        void UpdateCursor()
        {
            lock (sprite)
            {
                var hotspot = this.hotspot ?? new Position();
                int x = sprite.X + hotspot.X;
                int y = sprite.Y + hotspot.Y;
                this.hotspot = cursorHotspots[type];
                sprite.X = x - this.hotspot.X;
                sprite.Y = y - this.hotspot.Y;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset((uint)type);
            }
        }

        public void UpdatePosition(Position screenPosition)
        {
            var viewPosition = renderView.ScreenToGame(screenPosition);

            if (viewPosition != null)
            {
                lock (sprite)
                {
                    sprite.X = viewPosition.X - hotspot.X;
                    sprite.Y = viewPosition.Y - hotspot.Y;
                    sprite.Visible = Type != CursorType.None;
                }
            }
        }
    }
}
