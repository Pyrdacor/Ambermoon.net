using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    internal class Popup
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
        readonly List<Button> buttons = new List<Button>();
        ListBox listBox = null;

        public Popup(Game game, IRenderView renderView, Position position, int columns, int rows)
        {
            if (columns < 3 || rows < 3)
                throw new AmbermoonException(ExceptionScope.Application, "Popups must at least have 3 columns and 3 rows.");

            this.game = game;
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);

            void AddBorder(PopupFrame frame, int column, int row)
            {
                var sprite = renderView.SpriteFactory.Create(16, 16, false, true, BaseDisplayLayer) as ILayerSprite;
                sprite.Layer = renderView.GetLayer(Layer.UI);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetPopupFrameGraphicIndex(frame));
                sprite.PaletteIndex = 0;
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
            fill.Layer = renderView.GetLayer(Layer.UI);
            fill.X = position.X + 16;
            fill.Y = position.Y + 16;
            fill.Visible = true;
        }

        public bool CloseOnClick { get; set; } = true;
        public bool DisableButtons { get; set; } = false;
        public event Action Closed;

        public void OnClosed()
        {
            Closed?.Invoke();
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

            buttons.ForEach(button => button?.Destroy());
            buttons.Clear();
        }

        public IRenderText AddText(Position position, string text, TextColor textColor, bool shadow = true, byte displayLayer = 1)
        {
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text), textColor, shadow);
            renderText.DisplayLayer = (byte)Util.Min(255, BaseDisplayLayer + displayLayer);
            renderText.X = position.X;
            renderText.Y = position.Y;
            renderText.Visible = true;
            texts.Add(renderText);
            return renderText;
        }

        public IRenderText AddText(Rect bounds, string text, TextColor textColor, TextAlign textAlign = TextAlign.Left,
            bool shadow = true, byte displayLayer = 1)
        {
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text), textColor, shadow, bounds, textAlign);
            renderText.DisplayLayer = (byte)Util.Min(255, BaseDisplayLayer + displayLayer);
            renderText.Visible = true;
            texts.Add(renderText);
            return renderText;
        }

        public IRenderText AddText(IRenderText renderText, byte displayLayer = 1)
        {
            renderText.DisplayLayer = (byte)Util.Min(255, BaseDisplayLayer + displayLayer);
            renderText.Visible = true;
            texts.Add(renderText);
            return renderText;
        }

        public IColoredRect FillArea(Rect area, Color color, byte displayLayer = 1)
        {
            var filledArea = renderView.ColoredRectFactory.Create(area.Width, area.Height, color,
                (byte)Util.Min(255, BaseDisplayLayer + displayLayer));
            filledArea.Layer = renderView.GetLayer(Layer.UI);
            filledArea.X = area.Left;
            filledArea.Y = area.Top;
            filledArea.Visible = true;
            filledAreas.Add(filledArea);
            return filledArea;
        }

        public void AddSunkenBox(Rect area, byte displayLayer = 1)
        {
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

        public Button AddButton(Position position)
        {
            var button = new Button(renderView, position);
            buttons.Add(button);
            return button;
        }

        public bool Click(Position position)
        {
            if (listBox?.Click(position) == true)
                return true;

            // Note: LeftMouseDown may remove buttons or close the popup.
            for (int i = buttons.Count - 1; i >= 0; --i)
            {
                if (i >= buttons.Count)
                    continue;

                if (buttons[i]?.LeftMouseDown(position, game.CurrentTicks) == true)
                    return true;
            }

            return false;
        }

        public void LeftMouseUp(Position position)
        {
            // Note: LeftMouseUp may remove buttons or close the popup.
            for (int i = buttons.Count - 1; i >= 0; --i)
            {
                if (i >= buttons.Count)
                    continue;

                buttons[i]?.LeftMouseUp(position, game.CurrentTicks);
            }
        }

        public void Hover(Position position)
        {
            listBox?.Hover(position);
        }

        public void AddListBox(List<KeyValuePair<string, Action<int, string>>> items)
        {
            if (listBox != null)
                throw new AmbermoonException(ExceptionScope.Application, "Only one list box can be added.");

            listBox = new ListBox(game, this, items);
        }

        public void Update(uint currentTicks)
        {
            foreach (var button in buttons)
                button?.Update(currentTicks);
        }
    }
}
