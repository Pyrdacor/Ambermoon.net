using Ambermoon.Render;

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
        readonly ISprite _sprite;
        readonly ITextureAtlas _textureAtlas;

        public Layout(IRenderView renderView)
        {
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
                _sprite.TextureAtlasOffset = _textureAtlas.GetOffset((uint)(layoutType - 1));
                _sprite.Visible = true;
            }
        }
    }
}
