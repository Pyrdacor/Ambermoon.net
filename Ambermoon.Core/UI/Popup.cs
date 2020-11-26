using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.UI
{
    internal class Popup
    {
        const byte BaseDisplayLayer = 20;
        readonly byte displayLayer;
        readonly Game game;
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly IColoredRect fill;
        readonly List<UIText> texts = new List<UIText>();
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        readonly List<ILayerSprite> sprites = new List<ILayerSprite>();
        readonly List<Button> buttons = new List<Button>();
        readonly List<TextInput> inputs = new List<TextInput>();
        ListBox listBox = null;
        Scrollbar scrollbar = null;
        public Rect ContentArea { get; }

        public Popup(Game game, IRenderView renderView, Position position, int columns, int rows, bool transparent,
            byte displayLayerOffset = 0)
        {
            if (columns < 3 || rows < 3)
                throw new AmbermoonException(ExceptionScope.Application, "Popups must at least have 3 columns and 3 rows.");

            displayLayer = (byte)Math.Min(255, BaseDisplayLayer + displayLayerOffset);
            this.game = game;
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);

            void AddBorder(PopupFrame frame, int column, int row)
            {
                var sprite = renderView.SpriteFactory.Create(16, 16, true, displayLayer) as ILayerSprite;
                sprite.Layer = renderView.GetLayer(Layer.UI);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetPopupFrameGraphicIndex(frame));
                sprite.PaletteIndex = 0;
                sprite.X = position.X + column * 16;
                sprite.Y = position.Y + row * 16;
                sprite.Visible = true;
                borders.Add(sprite);
            }

            if (!transparent)
            {
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
                fill = renderView.ColoredRectFactory.Create((columns - 2) * 16, (rows - 2) * 16,
                    game.GetPaletteColor(50, 28), displayLayer);
                fill.Layer = renderView.GetLayer(Layer.UI);
                fill.X = position.X + 16;
                fill.Y = position.Y + 16;
                fill.Visible = true;

                ContentArea = new Rect(fill.X, fill.Y, fill.Width, fill.Height);
            }
        }

        public bool CloseOnClick { get; set; } = true;
        public bool DisableButtons { get; set; } = false;
        public bool CanAbort { get; set; } = true;
        public bool ClickCursor => CloseOnClick || texts.Any(text => text.WithScrolling);
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

            texts.ForEach(text => text?.Destroy());
            texts.Clear();

            filledAreas.ForEach(filledArea => filledArea?.Delete());
            filledAreas.Clear();

            sprites.ForEach(sprite => sprite?.Delete());
            sprites.Clear();

            buttons.ForEach(button => button?.Destroy());
            buttons.Clear();

            inputs.ForEach(input => input?.Destroy());
            inputs.Clear();

            listBox?.Destroy();
            listBox = null;

            scrollbar?.Destroy();
            scrollbar = null;
        }

        public IRenderText AddText(Position position, string text, TextColor textColor, bool shadow = true,
            byte displayLayer = 1, char? fallbackChar = null)
        {
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text, fallbackChar), textColor, shadow);
            renderText.DisplayLayer = (byte)Util.Min(255, this.displayLayer + displayLayer);
            renderText.X = position.X;
            renderText.Y = position.Y;
            renderText.Visible = true;
            texts.Add(new UIText(renderText));
            return renderText;
        }

        public UIText AddText(Rect bounds, string text, TextColor textColor, TextAlign textAlign = TextAlign.Left,
            bool shadow = true, byte displayLayer = 1, bool scrolling = false)
        {
            return AddText(bounds, renderView.TextProcessor.CreateText(text), textColor, textAlign,
                shadow, (byte)Util.Min(255, this.displayLayer + displayLayer), scrolling);
        }

        public UIText AddText(Rect bounds, IText text, TextColor textColor, TextAlign textAlign = TextAlign.Left,
            bool shadow = true, byte displayLayer = 1, bool scrolling = false)
        {
            var uiText = new UIText(renderView, text, bounds, (byte)Util.Min(255, this.displayLayer + displayLayer),
                textColor, shadow, textAlign, scrolling);
            texts.Add(uiText);
            if (scrolling)
            {
                CloseOnClick = false;
                uiText.Scrolled += scrolledToEnd => CloseOnClick = scrolledToEnd;
            }
            return uiText;
        }

        public UIText AddText(IRenderText renderText, byte displayLayer = 1)
        {
            renderText.DisplayLayer = (byte)Util.Min(255, this.displayLayer + displayLayer);
            renderText.Visible = true;
            var uiText = new UIText(renderText);
            texts.Add(uiText);
            return uiText;
        }

        public IColoredRect FillArea(Rect area, Color color, byte displayLayer = 1)
        {
            var filledArea = renderView.ColoredRectFactory.Create(area.Width, area.Height, color,
                (byte)Util.Min(255, this.displayLayer + displayLayer));
            filledArea.Layer = renderView.GetLayer(Layer.UI);
            filledArea.X = area.Left;
            filledArea.Y = area.Top;
            filledArea.Visible = true;
            filledAreas.Add(filledArea);
            return filledArea;
        }

        public void AddImage(Rect area, uint imageIndex, Layer layer, byte displayLayer = 1)
        {
            var sprite = renderView.SpriteFactory.Create(area.Width, area.Height, true,
                (byte)Util.Min(255, this.displayLayer + displayLayer)) as ILayerSprite;
            sprite.Layer = renderView.GetLayer(layer);
            sprite.PaletteIndex = 49;
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(layer).GetOffset(imageIndex);
            sprite.X = area.X;
            sprite.Y = area.Y;
            sprite.Visible = true;
            sprites.Add(sprite);
        }

        public void AddItemImage(Rect area, uint imageIndex, byte displayLayer = 1)
        {
            AddImage(area, imageIndex, Layer.Items, displayLayer);
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
            // TODO: use named palette colors
            var brightBorderColor = game.GetPaletteColor(50, 31);
            var darkBorderColor = game.GetPaletteColor(50, 26);            

            FillArea(new Rect(position.X, position.Y, Button.Width + 1, Button.Height + 1), brightBorderColor, 1);
            FillArea(new Rect(position.X - 1, position.Y - 1, Button.Width + 1, Button.Height + 1), darkBorderColor, 2);            
            var button = new Button(renderView, position);
            button.DisplayLayer = (byte)Util.Min(255, displayLayer + 3);
            buttons.Add(button);
            return button;
        }

        void ScrollTo(int offset)
        {
            scrollbar.SetScrollPosition(offset);
        }

        public bool KeyChar(char ch)
        {
            // Note: Key may remove inputs or close the popup.
            for (int i = inputs.Count - 1; i >= 0; --i)
            {
                if (i >= inputs.Count)
                    continue;

                if (inputs[i].KeyChar(ch))
                    return true;
            }

            return false;
        }

        public bool KeyDown(Key key)
        {
            // Note: Key may remove inputs or close the popup.
            for (int i = inputs.Count - 1; i >= 0; --i)
            {
                if (i >= inputs.Count)
                    continue;

                if (inputs[i].KeyDown(key))
                    return true;
            }

            if (scrollbar != null && !scrollbar.Disabled)
            {
                int scrollOffset = scrollbar.ScrollOffset;

                switch (key)
                {
                    case Key.Up:
                        if (scrollOffset > 0)
                            ScrollTo(scrollOffset - 1);
                        return true;
                    case Key.Down:
                        if (scrollOffset < scrollbar.ScrollRange)
                            ScrollTo(scrollOffset + 1);
                        return true;
                    case Key.PageUp:
                        if (scrollOffset > 0)
                            ScrollTo(Math.Max(0, scrollOffset - 5));
                        return true;
                    case Key.PageDown:
                        if (scrollOffset < scrollbar.ScrollRange)
                            ScrollTo(Math.Min(scrollbar.ScrollRange, scrollOffset + 5));
                        return true;
                    case Key.Home:
                        ScrollTo(0);
                        return true;
                    case Key.End:
                        ScrollTo(scrollbar.ScrollRange);
                        return true;
                }
            }

            return false;
        }

        public bool Drag(Position position)
        {
            if (scrollbar == null || scrollbar.Disabled)
                return false;

            return scrollbar.Drag(position);
        }

        public bool Click(Position position, MouseButtons mouseButtons)
        {
            if (mouseButtons == MouseButtons.Left && TextInput.FocusedInput == null)
            {
                if (scrollbar != null && !scrollbar.Disabled && scrollbar.LeftClick(position))
                    return true;

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

                // Note: Click may remove texts or close the popup.
                for (int i = texts.Count - 1; i >= 0; --i)
                {
                    if (i >= texts.Count)
                        continue;

                    if (texts[i].Click(position))
                        return true;
                }
            }

            // Note: Click may remove inputs or close the popup.
            for (int i = inputs.Count - 1; i >= 0; --i)
            {
                if (i >= inputs.Count)
                    continue;

                if (inputs[i].MouseDown(position, mouseButtons))
                    return true;
            }

            return false;
        }

        public void LeftMouseUp(Position position)
        {
            if (scrollbar != null && !scrollbar.Disabled)
            {
                scrollbar.LeftMouseUp();

                if (game.CursorType == CursorType.None)
                    game.CursorType = CursorType.Sword;
            }

            if (TextInput.FocusedInput != null)
                return;

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

        public void AddSavegameListBox(List<KeyValuePair<string, Action<int, string>>> items)
        {
            if (listBox != null)
                throw new AmbermoonException(ExceptionScope.Application, "Only one list box can be added.");

            listBox = ListBox.CreateSavegameListbox(renderView, game, this, items);
        }

        public void AddDictionaryListBox(List<KeyValuePair<string, Action<int, string>>> items)
        {
            if (listBox != null)
                throw new AmbermoonException(ExceptionScope.Application, "Only one list box can be added.");

            listBox = ListBox.CreateDictionaryListbox(renderView, game, this, items);
        }

        public ListBox AddSpellListBox(List<KeyValuePair<string, Action<int, string>>> items)
        {
            if (listBox != null)
                throw new AmbermoonException(ExceptionScope.Application, "Only one list box can be added.");

            return listBox = ListBox.CreateSpellListbox(renderView, game, this, items);
        }

        public Scrollbar AddScrollbar(Layout layout, int scrollRange, int displayLayer = 1, int yOffset = 0)
        {
            return scrollbar = new Scrollbar(layout, ScrollbarType.LargeVertical, new Rect(ContentArea.Right - 9, ContentArea.Top + yOffset, 6, 112),
                6, 56, scrollRange, (byte)Util.Min(255, this.displayLayer + displayLayer));
        }

        public bool HasTextInput() => inputs.Count != 0;

        public TextInput AddTextInput(Position position, int inputLength, TextAlign textAlign,
            TextInput.ClickAction leftClickAction, TextInput.ClickAction rightClickAction)
        {
            AddSunkenBox(new Rect(position, new Size((inputLength + 1) * Global.GlyphWidth + 3, 10)), 1);
            var input = new TextInput(renderView, position + new Position(2, 2), inputLength, (byte)Math.Min(255, displayLayer + 2),
                leftClickAction, rightClickAction, textAlign);
            inputs.Add(input);
            return input;
        }

        public void Update(uint currentTicks)
        {
            foreach (var button in buttons)
                button?.Update(currentTicks);

            foreach (var input in inputs)
                input.Update();
        }
    }
}
