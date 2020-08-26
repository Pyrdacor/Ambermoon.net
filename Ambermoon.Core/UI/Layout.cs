using Ambermoon.Render;
using System;
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

    public enum FadeEffectType
    {
        FadeIn,
        FadeOut,
        FadeInAndOut
    }

    public class FilledArea
    {
        readonly List<IColoredRect> _filledAreas;
        readonly IColoredRect _area;
        internal bool Destroyed { get; private set; } = false;

        internal FilledArea(List<IColoredRect> filledAreas, IColoredRect area)
        {
            _filledAreas = filledAreas;
            _area = area;
        }

        public Color Color
        {
            get => Destroyed ? null : _area.Color;
            set
            {
                if (!Destroyed)
                    _area.Color = value;
            }
        }

        public bool Visible
        {
            get => !Destroyed && _area.Visible;
            set
            {
                if (!Destroyed)
                    _area.Visible = value;
            }
        }

        public void Destroy()
        {
            if (Destroyed)
                return;

            _area.Delete();
            _filledAreas.Remove(_area);
            Destroyed = true;
        }
    }

    public class FadeEffect : FilledArea
    {
        readonly Color _startColor;
        readonly Color _endColor;
        readonly int _duration;
        readonly DateTime _startTime;
        readonly bool _removeWhenFinished;

        internal FadeEffect(List<IColoredRect> filledAreas, IColoredRect area, Color startColor,
            Color endColor, int durationInMilliseconds, DateTime startTime, bool removeWhenFinished)
            : base(filledAreas, area)
        {
            _startColor = startColor;
            _endColor = endColor;
            _duration = durationInMilliseconds;
            _startTime = startTime;
            _removeWhenFinished = removeWhenFinished;
        }

        public void Update()
        {
            bool Finished()
            {
                if (_removeWhenFinished)
                {
                    Destroy();
                    return true;
                }

                return false;
            }

            float percentage;

            if (_duration == 0)
            {
                percentage = 1.0f;

                if (Finished())
                    return;
            }
            else
            {
                var now = DateTime.Now;

                if (now <= _startTime)
                    percentage = 0.0f;
                else
                {
                    var elapsed = (int)(now - _startTime).TotalMilliseconds;

                    if (elapsed >= _duration && Finished())
                        return;

                    percentage = (float)elapsed / _duration;
                }
            }

            byte CalculateColorComponent(byte start, byte end)
            {
                if (start < end)
                    return (byte)(start + Util.Round((end - start) * percentage));
                else
                    return (byte)(start - Util.Round((start - end) * percentage));
            }

            Color = new Color
            (
                CalculateColorComponent(_startColor.R, _endColor.R),
                CalculateColorComponent(_startColor.G, _endColor.G),
                CalculateColorComponent(_startColor.B, _endColor.B),
                CalculateColorComponent(_startColor.A, _endColor.A)
            );
        }
    }

    public class Layout
    {
        public LayoutType Type { get; private set; }
        readonly IRenderView _renderView;
        readonly ISprite _sprite;
        readonly ITextureAtlas _textureAtlasBackground;
        readonly ITextureAtlas _textureAtlasForeground;
        readonly ISprite[] _portraits = new ISprite[6];
        ISprite _sprite80x80Picture;
        readonly List<ItemGrid> _itemGrids = new List<ItemGrid>();
        readonly List<IColoredRect> _filledAreas = new List<IColoredRect>();
        readonly List<FadeEffect> _fadeEffects = new List<FadeEffect>();
        readonly List<ISprite> _additionalSprites = new List<ISprite>();

        public Layout(IRenderView renderView)
        {
            _renderView = renderView;
            _textureAtlasBackground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground);
            _textureAtlasForeground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIForeground);
            _sprite = renderView.SpriteFactory.Create(320, 163, 0, 0, false, true);
            _sprite.Layer = renderView.GetLayer(Layer.UIBackground);
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
                _sprite.TextureAtlasOffset = _textureAtlasBackground.GetOffset(Graphics.LayoutOffset + (uint)(layoutType - 1));
                _sprite.Visible = true;
            }
        }

        public void Reset()
        {
            _sprite80x80Picture?.Delete();
            _sprite80x80Picture = null;
            _additionalSprites.ForEach(sprite => sprite?.Delete());
            _additionalSprites.Clear();
            _itemGrids.ForEach(grid => grid.Destroy());
            _itemGrids.Clear();
            _filledAreas.ForEach(area => area?.Delete());
            _filledAreas.Clear();
        }

        /// <summary>
        /// Set portait to 0 to remove the portrait.
        /// </summary>
        public void SetPortrait(int slot, uint portrait)
        {
            if (portrait == 0)
            {
                // TODO: in original portrait removing is animated by moving down the
                // gray masked picture infront of the portrait

                _portraits[slot]?.Delete();
                _portraits[slot] = null;
            }
            else
            {
                var sprite = _portraits[slot] ??= _renderView.SpriteFactory.Create(32, 34, 0, 0, false, true);
                sprite.Layer = _renderView.GetLayer(Layer.UIForeground);
                sprite.X = Global.PartyMemberPortraitAreas[slot].Left;
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top;
                sprite.TextureAtlasOffset = _textureAtlasForeground.GetOffset(Graphics.PortraitOffset + portrait - 1);
                sprite.PaletteIndex = 49;
                sprite.Visible = true;
            }
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
                sprite.TextureAtlasOffset = _textureAtlasForeground.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = Global.LayoutX + 16;
                sprite.Y = Global.LayoutY + 6;
                sprite.PaletteIndex = 49;
                sprite.Layer = _renderView.GetLayer(Layer.UIForeground);
                sprite.Visible = true;
            }
        }

        public void AddItemGrid(ItemGrid itemGrid)
        {
            _itemGrids.Add(itemGrid);
        }

        IColoredRect CreateArea(Rect rect, Color color, bool topMost)
        {
            var coloredRect = _renderView.ColoredRectFactory.Create(rect.Size.Width, rect.Size.Height,
                color, (byte)(topMost ? 255 : 0));
            coloredRect.Layer = _renderView.GetLayer(topMost ? Layer.Popup : Layer.UIBackground);
            coloredRect.X = rect.Left;
            coloredRect.Y = rect.Top;
            coloredRect.Visible = true;
            _filledAreas.Add(coloredRect);
            return coloredRect;
        }

        public FilledArea FillArea(Rect rect, Color color, bool topMost)
        {
            return new FilledArea(_filledAreas, CreateArea(rect, color, topMost));
        }

        public void AddColorFader(Rect rect, Color startColor, Color endColor,
            int durationInMilliseconds, bool removeWhenFinished, DateTime? startTime = null)
        {
            _fadeEffects.Add(new FadeEffect(_filledAreas, CreateArea(rect, startColor, true), startColor,
                endColor, durationInMilliseconds, startTime ?? DateTime.Now, removeWhenFinished));
        }

        public void AddFadeEffect(Rect rect, Color color, FadeEffectType fadeEffectType,
            int durationInMilliseconds)
        {
            switch (fadeEffectType)
            {
                case FadeEffectType.FadeIn:
                    AddColorFader(rect, new Color(color, 0), color, durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeOut:
                    AddColorFader(rect, color, new Color(color, 0), durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeInAndOut:
                    var halfDuration = durationInMilliseconds / 2;
                    AddColorFader(rect, new Color(color, 0), color, halfDuration, true);
                    AddColorFader(rect, color, new Color(color, 0), halfDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(halfDuration));
                    break;
            }
        }

        public void Update()
        {
            for (int i = _fadeEffects.Count - 1; i >= 0; --i)
            {
                _fadeEffects[i].Update();

                if (_fadeEffects[i].Destroyed)
                    _fadeEffects.RemoveAt(i);
            }
        }
    }
}
