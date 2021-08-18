/*
 * ListBox.cs - List box (savegames, spells, known words, etc)
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Ambermoon.Render;
using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI
{
    internal class ListBox
    {
        readonly Game game;
        readonly IRenderView renderView;
        readonly List<KeyValuePair<string, Action<int, string>>> items;
        readonly List<Rect> itemAreas = new List<Rect>(10);
        readonly List<IRenderText> itemIndices = new List<IRenderText>(10);
        readonly List<IRenderText> itemTexts = new List<IRenderText>(10);
        readonly IColoredRect hoverBox;
        readonly TextInput editInput;
        readonly int maxItems;
        readonly Func<string, TextColor> colorProvider;
        int hoveredItem = -1;
        int scrollOffset = 0;
        int editingItem = -1;
        readonly bool canEdit = false;
        readonly Position relativeHoverBoxOffset;
        int ScrollRange => items.Count - itemAreas.Count;
        public bool Editing => editingItem != -1;

        public event Action<int> HoverItem;

        ListBox(IRenderView renderView, Game game, Popup popup, List<KeyValuePair<string, Action<int, string>>> items,
            Rect area, Position itemBasePosition, int itemHeight, int hoverBoxWidth, Position relativeHoverBoxOffset,
            bool withIndex, int maxItems, char? fallbackChar = null, bool canEdit = false,
            Func<string, TextColor> colorProvider = null)
        {
            this.game = game;
            this.renderView = renderView;
            this.items = items;
            this.relativeHoverBoxOffset = relativeHoverBoxOffset;
            this.maxItems = maxItems;
            this.canEdit = canEdit;
            this.colorProvider = colorProvider;

            popup.AddSunkenBox(area);
            hoverBox = popup.FillArea(new Rect(itemBasePosition + relativeHoverBoxOffset, new Size(hoverBoxWidth, itemHeight)),
                game.GetTextColor(TextColor.Bright), 3);
            hoverBox.Visible = false;

            for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
            {
                var color = items[i].Value == null ? TextColor.Disabled
                    : colorProvider?.Invoke(items[i].Key) ?? TextColor.Bright;

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
                itemAreas.Add(new Rect(itemBasePosition.X, itemBasePosition.Y + i * itemHeight, area.Right - itemBasePosition.X - 1, itemHeight));
            }

            if (canEdit && itemTexts.Count != 0)
            {
                editInput = new TextInput(game, renderView, new Position(), (itemTexts[0].Width / Global.GlyphWidth) - 1,
                    (byte)(popup.DisplayLayer + 6), TextInput.ClickAction.Submit, TextInput.ClickAction.Abort, TextAlign.Left)
                {
                    ClearOnNewInput = false,
                    DigitsOnly = false,
                    ReactToGlobalClicks = true
                };
                editInput.InputSubmitted += _ => CommitEdit();
            }
        }

        public static ListBox CreateOptionsListbox(IRenderView renderView, Game game, Popup popup,
            List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(64, 85, 191, 38), new Position(67, 87), 7, 189, new Position(-2, -1), false, 5);
        }

        public static ListBox CreateSavegameListbox(IRenderView renderView, Game game, Popup popup,
            List<KeyValuePair<string, Action<int, string>>> items, bool canEdit)
        {
            return new ListBox(renderView, game, popup, items, new Rect(32, 85, 256, 73),
                new Position(33, 87), 7, 237, new Position(16, -1), true, 10, '?', canEdit);
        }

        public static ListBox CreateDictionaryListbox(IRenderView renderView, Game game, Popup popup,
            List<KeyValuePair<string, Action<int, string>>> items, Func<string, TextColor> colorProvider)
        {
            return new ListBox(renderView, game, popup, items, new Rect(48, 48, 130, 115),
                new Position(52, 50), 7, 127, new Position(-3, -1), false, 16, null, false, colorProvider);
        }

        public static ListBox CreateSpellListbox(IRenderView renderView, Game game, Popup popup,
            List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(48, 56, 162, 115),
                new Position(52, 58), 7, 159, new Position(-3, -1), false, 16);
        }

        public static ListBox CreateSongListbox(IRenderView renderView, Game game, Popup popup,
            List<KeyValuePair<string, Action<int, string>>> items)
        {
            return new ListBox(renderView, game, popup, items, new Rect(32, 50, 192, 115),
                new Position(36, 52), 7, 189, new Position(-3, -1), false, 16);
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

        public void SetItemText(int index, string text, char? fallbackChar = null)
        {
            itemTexts[index - scrollOffset].Text = renderView.TextProcessor.CreateText(text, fallbackChar);
            items[index] = KeyValuePair.Create(text, items[index].Value);
            itemTexts[index - scrollOffset].Visible = !string.IsNullOrWhiteSpace(text);
            PostScrollUpdate();
        }

        void SetTextHovered(IRenderText text, bool hovered, bool enabled, string textEntry)
        {
            text.Shadow = !enabled || !hovered;
            text.TextColor = !enabled ? TextColor.Disabled : hovered ? TextColor.Dark
                : colorProvider?.Invoke(textEntry) ?? TextColor.Bright;
        }

        void SetHoveredItem(int index)
        {
            if (hoveredItem != -1)
            {
                int realIndex = scrollOffset + hoveredItem;
                SetTextHovered(itemTexts[hoveredItem], false, items[realIndex].Value != null, items[realIndex].Key);
            }

            if (hoveredItem != index)
                HoverItem?.Invoke(scrollOffset + index);

            hoveredItem = index;

            if (hoveredItem != -1)
            {
                int realIndex = scrollOffset + hoveredItem;
                bool enabled = items[realIndex].Value != null;
                SetTextHovered(itemTexts[hoveredItem], true, enabled, items[realIndex].Key);
                hoverBox.Y = itemAreas[index].Y + relativeHoverBoxOffset.Y;
                hoverBox.Visible = enabled;

                if (hoverBox.Visible)
                    hoverBox.Color = game.GetTextColor(colorProvider?.Invoke(items[realIndex].Key) ?? TextColor.Bright);
            }
            else
            {
                hoverBox.Visible = false;
            }
        }

        public void Hover(Position position)
        {
            if (!Editing)
            {
                for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
                {
                    if (itemAreas[i].Contains(position))
                    {
                        SetHoveredItem(i);
                        return;
                    }
                }
            }

            SetHoveredItem(-1);
        }

        public void CommitEdit()
        {
            if (!Editing)
                return;

            int itemIndex = editingItem;
            editingItem = -1;
            if (editInput == TextInput.FocusedInput)
                editInput.Submit();
            AbortEdit(itemIndex);
            itemTexts[itemIndex - scrollOffset].Text = renderView.TextProcessor.CreateText(editInput.Text);
            items[itemIndex] = KeyValuePair.Create(editInput.Text, items[itemIndex].Value);
            items[itemIndex].Value?.Invoke(itemIndex, items[itemIndex].Key);
        }

        public void AbortEdit(int? index = null)
        {
            itemTexts[index ?? editingItem].Visible = true;
            editingItem = -1;
            editInput.LoseFocus();
            editInput.Visible = false;
        }

        void StartEdit(int row, int itemIndex)
        {
            editingItem = itemIndex;
            SetHoveredItem(-1);
            itemTexts[itemIndex].Visible = false;
            editInput.MoveTo(new Position(itemTexts[row].X, itemTexts[row].Y));
            editInput.Visible = true;
            editInput.SetText(items[itemIndex].Key);
            editInput.SetFocus();
        }

        public void Update(uint ticks)
        {
            editInput?.Update();
        }

        public bool KeyChar(char ch)
        {
            return editInput?.KeyChar(ch) ?? false;
        }

        public bool KeyDown(Key key)
        {
            return editInput?.KeyDown(key) ?? false;
        }

        public bool Click(Position position)
        {
            if (Editing)
            {
                CommitEdit();
                return true;
            }

            for (int i = 0; i < Util.Min(maxItems, items.Count); ++i)
            {
                if (itemAreas[i].Contains(position))
                {
                    if (canEdit)
                    {
                        if (!Editing)
                            StartEdit(i, scrollOffset + i);
                        return true;
                    }

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
                var textColor = items[scrollOffset + i].Value == null ? TextColor.Disabled
                    : colorProvider?.Invoke(items[scrollOffset + i].Key) ?? TextColor.Bright;

                if (withIndex)
                {
                    itemIndices[i].Text = renderView.TextProcessor.CreateText($"{scrollOffset + i + 1,2}");
                    itemIndices[i].TextColor = textColor;
                }
                itemTexts[i].Text = renderView.TextProcessor.CreateText(items[scrollOffset + i].Key);
                itemTexts[i].TextColor = textColor;
            }
        }

        public void SetItemAction(int index, Action<int, string> action)
        {
            items[index] = KeyValuePair.Create(items[index].Key, action);
            PostScrollUpdate();
        }
    }
}
