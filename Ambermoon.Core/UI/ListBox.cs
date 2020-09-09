using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    internal class ListBox
    {
        readonly List<KeyValuePair<string, Action<int, string>>> items;
        readonly List<Rect> itemAreas = new List<Rect>(10);
        readonly List<IRenderText> itemTexts = new List<IRenderText>(10);
        readonly IColoredRect hoverBox;
        int hoveredItem = -1;

        public ListBox(Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items)
        {
            this.items = items;

            popup.AddSunkenBox(new Rect(32, 85, 256, 73));
            hoverBox = popup.FillArea(new Rect(49, 86, 237, 7), game.GetTextColor(TextColor.Gray), 3);
            hoverBox.Visible = false;

            for (int i = 0; i < Util.Min(10, items.Count); ++i)
            {
                popup.AddText(new Position(33, 87 + i * 7), $"{i + 1,2}", TextColor.Gray, true, 4);
                itemTexts.Add(popup.AddText(new Position(50, 87 + i * 7), items[i].Key, TextColor.Gray, true, 4));
                itemAreas.Add(new Rect(33, 87 + i * 7, 222, 7));
            }
        }

        void SetTextHovered(IRenderText text, bool hovered)
        {
            text.Shadow = !hovered;
            text.TextColor = hovered ? TextColor.Black : TextColor.Gray;
        }

        void SetHoveredItem(int index)
        {
            if (hoveredItem != -1)
                SetTextHovered(itemTexts[hoveredItem], false);

            hoveredItem = index;

            if (hoveredItem != -1)
            {
                SetTextHovered(itemTexts[hoveredItem], true);
                hoverBox.Y = 86 + index * 7;
                hoverBox.Visible = true;
            }
            else
            {
                hoverBox.Visible = false;
            }
        }

        public void Hover(Position position)
        {
            for (int i = 0; i < Util.Min(10, items.Count); ++i)
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
            for (int i = 0; i < Util.Min(10, items.Count); ++i)
            {
                if (itemAreas[i].Contains(position))
                {
                    items[i].Value?.Invoke(i, items[i].Key);
                    return true;
                }
            }

            return false;
        }
    }
}
