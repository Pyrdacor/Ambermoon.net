using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    public enum LayoutType
    {
        None,
        Map2D,
        Inventory,
        Items, // Chest, merchant, battle loot and other places like trainers etc
        Battle,
        Map3D,
        Unknown2,
        Event, // Game over, airship travel, grandfather intro, valdyn sequence
        Conversation,
        Riddlemouth,
        BattlePositions,
        Automap
    }

    public class Layout
    {
        public LayoutType Type { get; private set; }
        readonly IRenderView _renderView;
        readonly ISprite _sprite;
        readonly ITextureAtlas _textureAtlas;
        ISprite _sprite80x80Picture;
        readonly List<ISprite> _additionalSprites = new List<ISprite>();

        public Layout(IRenderView renderView)
        {
            _renderView = renderView;
            _textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UIForeground);
            _sprite = renderView.SpriteFactory.Create(320, 163, 0, 0, false, true);
            _sprite.Layer = renderView.GetLayer(Layer.UIForeground);
            _sprite.X = Global.LayoutX;
            _sprite.Y = Global.LayoutY;
            _sprite.PaletteIndex = 0;

            SetLayout(LayoutType.None);
        }

        public void SetLayout(LayoutType layoutType)
        {
            Type = layoutType;

            if (layoutType == LayoutType.None)
            {
                _sprite.Visible = false;
            }
            else
            {
                _sprite.TextureAtlasOffset = _textureAtlas.GetOffset(Graphics.LayoutOffset + (uint)(layoutType - 1));
                _sprite.Visible = true;
            }
        }

        public void Reset()
        {
            _sprite80x80Picture?.Delete();
            _sprite80x80Picture = null;

            foreach (var sprite in _additionalSprites)
                sprite.Delete();

            _additionalSprites.Clear();
        }

        public void Set80x80Picture(Data.Enumerations.Picture80x80 picture)
        {
            if (picture == Data.Enumerations.Picture80x80.None)
            {
                if (_sprite80x80Picture != null)
                    _sprite80x80Picture.Visible = false;
            }
            else
            {
                var sprite = _sprite80x80Picture ??= _renderView.SpriteFactory.Create(80, 80, 0, 0, false, true);
                sprite.TextureAtlasOffset = _textureAtlas.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = Global.LayoutX + 16;
                sprite.Y = Global.LayoutY + 6;
                sprite.PaletteIndex = 49;
                sprite.Layer = _renderView.GetLayer(Layer.UIForeground);
                sprite.Visible = true;
            }
        }
    }
}
