using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    class Popup
    {
        const byte BaseDisplayLayer = 20;
        readonly Game game;
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly IColoredRect fill;
        readonly List<IRenderText> texts = new List<IRenderText>();
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        readonly List<ILayerSprite> sprites = new List<ILayerSprite>();

        public Popup(Game game, IRenderView renderView, Position position, int columns, int rows)
        {
            if (columns < 3 || rows < 3)
                throw new AmbermoonException(ExceptionScope.Application, "Popups must at least have 3 columns and 3 rows.");

            this.game = game;
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Popup);

            void AddBorder(PopupFrame frame, int column, int row)
            {
                var sprite = renderView.SpriteFactory.Create(16, 16, false, true, BaseDisplayLayer) as ILayerSprite;
                sprite.Layer = renderView.GetLayer(Layer.Popup);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetPopupFrameGraphicIndex(frame));
                sprite.PaletteIndex = 49;
                sprite.X = position.X + column * 16;
                sprite.Y = position.Y + row * 16;
                sprite.Visible = true;
                borders.Add(sprite);
            }

            // 4 corners
            AddBorder(PopupFrame.FrameUpperLeft, 0, 0);
            AddBorder(PopupFrame.FrameUpperRight, columns - 1, 0);
            AddBorder(PopupFrame.FrameLowerLeft, 0, rows - 1);
            AddBorder(PopupFrame.FrameLowerRight, columns - 1, rows - 1);

            // top and bottom border
            for (int i = 0; i < columns - 2; ++i)
            {
                AddBorder(PopupFrame.FrameTop, i + 1, 0);
                AddBorder(PopupFrame.FrameBottom, i + 1, rows - 1);
            }

            // left and right border
            for (int i = 0; i < rows - 2; ++i)
            {
                AddBorder(PopupFrame.FrameLeft, 0, i + 1);
                AddBorder(PopupFrame.FrameRight, columns - 1, i + 1);
            }

            // fill
            // TODO: use named palette color
            fill = renderView.ColoredRectFactory.Create((columns - 2) * 16, (rows - 2) * 16, game.GetPaletteColor(50, 28), BaseDisplayLayer);
            fill.Layer = renderView.GetLayer(Layer.Popup);
            fill.X = position.X + 16;
            fill.Y = position.Y + 16;
            fill.Visible = true;
        }

        public void Destroy()
        {
            borders.ForEach(border => border?.Delete());
            borders.Clear();

            fill?.Delete();

            texts.ForEach(text => text?.Delete());
            texts.Clear();

            filledAreas.ForEach(filledArea => filledArea?.Delete());
            filledAreas.Clear();

            sprites.ForEach(sprite => sprite?.Delete());
            sprites.Clear();
        }

        public void AddText(Position position, string text, TextColor textColor, bool shadow = true, byte displayLayer = 1)
        {
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text), textColor, shadow);
            renderText.DisplayLayer = (byte)Util.Min(255, BaseDisplayLayer + displayLayer);
            renderText.X = position.X;
            renderText.Y = position.Y;
            renderText.Visible = true;
            texts.Add(renderText);
        }

        public void AddText(Rect bounds, string text, TextColor textColor, TextAlign textAlign = TextAlign.Left,
            bool shadow = true, byte displayLayer = 1)
        {
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text), textColor, shadow, bounds, textAlign);
            renderText.DisplayLayer = (byte)Util.Min(255, BaseDisplayLayer + displayLayer);
            renderText.Visible = true;
            texts.Add(renderText);
        }

        public void FillArea(Rect area, Color color, byte displayLayer = 1)
        {
            var filledArea = renderView.ColoredRectFactory.Create(area.Width, area.Height, color,
                (byte)Util.Min(255, BaseDisplayLayer + displayLayer));
            filledArea.Layer = renderView.GetLayer(Layer.Popup);
            filledArea.X = area.Left;
            filledArea.Y = area.Top;
            filledArea.Visible = true;
            filledAreas.Add(filledArea);
        }

        public void AddSunkenBox(Rect area, byte displayLayer = 1)
        {
            displayLayer = (byte)(BaseDisplayLayer + displayLayer);
            // TODO: use named palette colors
            var darkBorderColor = game.GetPaletteColor(50, 26);
            var brightBorderColor = game.GetPaletteColor(50, 31);
            var fillColor = game.GetPaletteColor(50, 27);

            // upper dark border
            FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer);
            // left dark border
            FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer);
            // fill
            FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer);
            // right bright border
            FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer);
            // lower bright border
            FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer);
        }

        public void AddEventPicture(uint index, byte displayLayer = 1)
        {
            var sprite = renderView.SpriteFactory.Create(320, 92, false, true,
                (byte)Util.Min(255, BaseDisplayLayer + displayLayer)) as ILayerSprite;
            sprite.PaletteIndex = index switch
            {
                0 => 27,
                1 => 32,
                2 => 33,
                3 => 33,
                4 => 33,
                5 => 33,
                6 => 33,
                7 => 33,
                8 => 38,
                _ => throw new AmbermoonException(ExceptionScope.Data, $"Invalid event picture index: {index}. Valid indices are 0 to 8.")
            };
            sprite.Layer = renderView.GetLayer(Layer.Popup);
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.EventPictureOffset + index);
            sprite.X = 0;
            sprite.Y = 38;
            sprite.Visible = true;
            sprites.Add(sprite);
        }
    }
}
