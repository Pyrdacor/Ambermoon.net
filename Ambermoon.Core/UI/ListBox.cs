using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    internal class ListBox
    {
        readonly IRenderView renderView;
        readonly List<KeyValuePair<string, Action<int, string>>> items;
        readonly List<Rect> itemAreas = new List<Rect>(10);
        readonly List<IRenderText> itemIndices = new List<IRenderText>(10);
        readonly List<IRenderText> itemTexts = new List<IRenderText>(10);
        readonly IColoredRect hoverBox;
        readonly int maxItems;
        int hoveredItem = -1;
        int scrollOffset = 0;
        readonly Position relativeHoverBoxOffset;
        int ScrollRange => items.Count - itemAreas.Count;

        public event Action<int> HoverItem;

        ListBox(IRenderView renderView, Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items,
            Rect area, Position itemBasePosition, int itemHeight, int hoverBoxWidth, Position relativeHoverBoxOffset,
            bool withIndex, int maxItems, char? fallbackChar = null)
        {
            this.renderView = renderView;
            this.items = items;
            this.relativeHoverBoxOffset = relativeHoverBoxOffset;
            this.maxItems = maxItems;

            popup.AddSunkenBox(area);
            hoverBox = popup.FillArea(new Rect(itemBasePosition + relativeHoverBoxOffset, new Size(hoverBoxWidth, itemHeight)),
                game.GetTextColor(TextColor.Gray), 3);
            hoverBox.Visible = false;

            for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
            {
                var color = items[i].Value == null ? TextColor.Disabled : TextColor.Gray;

                if (withIndex)
                {
                    int y = itemBasePosition.Y + i * itemHeight;
                    itemIndices.Add(popup.AddText(new Position(itemBasePosition.X, y), $"{i + 1,2}", color, true, 4));
                    itemTexts.Add(popup.AddText(new Position(itemBasePosition.X + 17, y), items[i].Key, color, true, 4, fallbackChar));
                }
                else
                {
                    itemTexts.Add(popup.AddText(new Position(itemBasePosition.X, itemBasePosition.Y + i * itemHeight),
                        items[i].Key, color, true, 4, fallbackChar));
                }
                itemAreas.Add(new Rect(itemBasePosition.X, itemBasePosition.Y + i * itemHeight, area.Width - 34, itemHeight));
            }
        }

        public static ListBox CreateSavegameListbox(IRenderView renderView, Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(32, 85, 256, 73), new Position(33, 87), 7, 237, new Position(16, -1), true, 10, '?');
        }

        public static ListBox CreateDictionaryListbox(IRenderView renderView, Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(48, 48, 130, 115), new Position(52, 50), 7, 127, new Position(-3, -1), false, 16);
        }

        public static ListBox CreateSpellListbox(IRenderView renderView, Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(48, 56, 162, 115), new Position(52, 58), 7, 159, new Position(-3, -1), false, 16);
        }

        public void Destroy()
        {
            items.Clear();
            itemAreas.Clear();
            itemIndices.ForEach(t => t?.Delete());
            itemIndices.Clear();
            itemTexts.ForEach(t => t?.Delete());
            itemTexts.Clear();
            hoverBox?.Delete();
            hoveredItem = -1;
            scrollOffset = 0;
        }

        void SetTextHovered(IRenderText text, bool hovered, bool enabled)
        {
            text.Shadow = !enabled || !hovered;
            text.TextColor = !enabled ? TextColor.Disabled : hovered ? TextColor.Black : TextColor.Gray;
        }

        void SetHoveredItem(int index)
        {
            if (hoveredItem != -1)
                SetTextHovered(itemTexts[hoveredItem], false, items[scrollOffset + hoveredItem].Value != null);

            if (hoveredItem != index)
                HoverItem?.Invoke(scrollOffset + index);

            hoveredItem = index;

            if (hoveredItem != -1)
            {
                bool enabled = items[scrollOffset + hoveredItem].Value != null;
                SetTextHovered(itemTexts[hoveredItem], true, enabled);
                hoverBox.Y = itemAreas[index].Y + relativeHoverBoxOffset.Y;
                hoverBox.Visible = enabled;
            }
            else
            {
                hoverBox.Visible = false;
            }
        }

        public void Hover(Position position)
        {
            for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
            {
                if (itemAreas[i].Contains(position))
                {
                    SetHoveredItem(i);
                    return;
                }
            }

            SetHoveredItem(-1);
        }

        public bool Click(Position position)
        {
            for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
            {
                if (itemAreas[i].Contains(position))
                {
                    if (items[scrollOffset + i].Value == null)
                        return false;

                    items[scrollOffset + i].Value.Invoke(scrollOffset + i, items[scrollOffset + i].Key);
                    return true;
                }
            }

            return false;
        }

        public void ScrollUp()
        {
            if (scrollOffset > 0)
            {
                --scrollOffset;
                PostScrollUpdate();
            }
        }

        public void ScrollDown()
        {
            if (scrollOffset < ScrollRange)
            {
                ++scrollOffset;
                PostScrollUpdate();
            }
        }

        public void ScrollToBegin()
        {
            if (scrollOffset != 0)
            {
                scrollOffset = 0;
                PostScrollUpdate();
            }
        }

        public void ScrollToEnd()
        {
            if (scrollOffset != ScrollRange)
            {
                scrollOffset = ScrollRange;
                PostScrollUpdate();
            }
        }

        public void ScrollTo(int offset)
        {
            offset = Math.Max(0, Math.Min(offset, ScrollRange));

            if (scrollOffset != offset)
            {
                scrollOffset = offset;
                PostScrollUpdate();
            }
        }

        void PostScrollUpdate()
        {
            bool withIndex = itemIndices.Count != 0;

            for (int i = 0; i < itemAreas.Count; ++i)
            {
                var textColor = items[scrollOffset + i].Value != null ? TextColor.Gray : TextColor.Disabled;

                if (withIndex)
                {
                    itemIndices[i].Text = renderView.TextProcessor.CreateText($"{scrollOffset + i + 1,2}");
                    itemIndices[i].TextColor = textColor;
                }
                itemTexts[i].Text = renderView.TextProcessor.CreateText(items[scrollOffset + i].Key);
                itemTexts[i].TextColor = textColor;
            }
        }
    }
}
