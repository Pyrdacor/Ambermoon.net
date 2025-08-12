/*
 * UIText.cs - Advanced UI text element
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

using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI
{
    public class UIText
    {
        readonly IGameRenderView renderView;
        IText text;
        readonly IRenderText renderText;
        readonly Rect bounds;
        bool allowScrolling;
        bool freeScrolling = false;
        bool isScrolling = false;
        int lineOffset = 0;
        readonly int numVisibleLines;
        readonly Action<TimeSpan, Action> timedEventCreator;
        IReadOnlyList<TextColor> textColorsPerLine;
        TextColor defaultTextColor = TextColor.BrightGray;
        public bool WithScrolling { get; internal set; }
        public bool CanScroll => WithScrolling && this.text.LineCount > numVisibleLines;

		public event Action FreeScrollingStarted;
        public event Action FreeScrollingEnded;

        public bool Visible
        {
            get => renderText.Visible;
            set => renderText.Visible = value;
        }

        public byte PaletteIndex
        {
            get => renderText.PaletteIndex;
            set => renderText.PaletteIndex = value;
        }

        /// <summary>
        /// The boolean gives information if the
        /// text was scrolled to the end. It is
        /// true for non-scrollable texts.
        /// </summary>
        public event Action<bool> Clicked;
        /// <summary>
        /// The boolean gives information if the
        /// text was just scrolled to the end.
        /// </summary>
        public event Action<bool> Scrolled;

        public UIText(IRenderText renderText)
        {
            allowScrolling = false;
            this.renderText = renderText;
            defaultTextColor = renderText.TextColor;
            textColorsPerLine = renderText.GetTextColorPerLine(null);
        }

        public UIText(IGameRenderView renderView, byte paletteIndex, IText text, Rect bounds, byte displayLayer = 1,
            TextColor textColor = TextColor.BrightGray, bool shadow = true, TextAlign textAlign = TextAlign.Left,
            bool allowScrolling = false, Action<TimeSpan, Action> timedEventCreator = null)
        {
            this.renderView = renderView;
            this.text = renderView.TextProcessor.WrapText(text, bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            this.bounds = bounds;
            this.allowScrolling = allowScrolling;
            renderText = renderView.RenderTextFactory.Create(
                (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
                renderView.GetLayer(Layer.Text),
                this.text, textColor, shadow, bounds, textAlign);
            renderText.DisplayLayer = displayLayer;
            renderText.PaletteIndex = paletteIndex;
            renderText.Visible = true;
            defaultTextColor = textColor;
            numVisibleLines = (bounds.Height + 1) / Global.GlyphLineHeight;
            WithScrolling = allowScrolling;
            this.timedEventCreator = timedEventCreator;
            textColorsPerLine = renderText.GetTextColorPerLine(this.text);

            if (allowScrolling && this.text.LineCount > numVisibleLines)
            {
                lineOffset = -numVisibleLines;
                ScrollTo(0);
            }
        }

        public void Destroy()
        {
            renderText?.Delete();
        }

        void UpdateText(int lineOffset)
        {
            if (textColorsPerLine != null && lineOffset >= 0 && lineOffset < textColorsPerLine.Count)
                renderText.TextColor = textColorsPerLine[lineOffset];
            renderText.Text = renderView.TextProcessor.WrapText(
                renderView.TextProcessor.GetLines(text, lineOffset, numVisibleLines), bounds,
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        public void SetText(IText text)
        {
            this.text = renderView.TextProcessor.WrapText(text, bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            renderText.TextColor = defaultTextColor;
            textColorsPerLine = renderText.GetTextColorPerLine(this.text);
            allowScrolling = WithScrolling;
            freeScrolling = false;
            lineOffset = 0;

            if (allowScrolling && this.text.LineCount > numVisibleLines)
            {
                lineOffset = -numVisibleLines;
                ScrollTo(0);
            }
            else
                UpdateText(0);
        }

        public void SetTextColor(TextColor textColor)
        {
            renderText.TextColor = defaultTextColor = textColor;
            textColorsPerLine = renderText.GetTextColorPerLine(text);
        }

        public void SetTextAlign(TextAlign textAlign)
        {
            renderText.TextAlign = textAlign;
        }

        public void SetBounds(Rect bounds)
        {
            renderText.Place(bounds, renderText.TextAlign);
        }

        public void SetPosition(Position position)
        {
            renderText.Place(position.X, position.Y);
        }

        public void MouseMove(int y)
        {
            if (freeScrolling)
            {
                if (y < 0)
                {
                    if (lineOffset > 0)
                        UpdateText(--lineOffset);
                }
                else if (y > 0)
                {
                    if (lineOffset < text.LineCount - numVisibleLines)
                        UpdateText(++lineOffset);
                }
            }
        }

        public void InvokeClickEvent() => Clicked?.Invoke(true);

        public bool Click(Position position)
        {
            if (isScrolling)
                return true;

            if (freeScrolling)
            {
                freeScrolling = false;
                FreeScrollingEnded?.Invoke();
                Scrolled?.Invoke(true);
                Clicked?.Invoke(true);
                return true;
            }

            if (allowScrolling)
            {
                if (lineOffset >= text.LineCount - numVisibleLines)
                {
                    allowScrolling = false;
                    bool wasScrollable = text.LineCount > numVisibleLines;

                    if (wasScrollable)
                    {
                        freeScrolling = true;
                        FreeScrollingStarted?.Invoke();
                    }
                    else
                    {
                        Scrolled?.Invoke(true);
                        Clicked?.Invoke(true);
                    }
                }
                else
                {
                    ScrollTo(Math.Min(lineOffset + numVisibleLines, text.LineCount - numVisibleLines));
                }

                return true;
            }

            Clicked?.Invoke(true);

            return false;
        }

        void ScrollTo(int lineOffset)
        {
            if (this.lineOffset == lineOffset)
            {
                isScrolling = false;
                Scrolled?.Invoke(false);
                Clicked?.Invoke(false);
            }
            else
            {
                isScrolling = true;
                UpdateText(++this.lineOffset);
                timedEventCreator?.Invoke(TimeSpan.FromMilliseconds(50), () => ScrollTo(lineOffset));
            }            
        }

        public void Clip(Rect area)
        {
            renderText.ClipArea = area;
        }

        public void IncreaseClipWidth(int amount)
        {
            renderText.ClipArea = renderText.ClipArea.CreateModified(0, 0, amount, 0);
        }
    }
}
