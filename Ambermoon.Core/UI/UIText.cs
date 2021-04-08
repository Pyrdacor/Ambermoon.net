using Ambermoon.Data;
using Ambermoon.Render;
using System;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI
{
    internal class UIText
    {
        readonly IRenderView renderView;
        readonly IText text;
        readonly IRenderText renderText;
        readonly Rect bounds;
        bool allowScrolling;
        int lineOffset = 0;
        readonly int numVisibleLines;
        public bool WithScrolling { get; internal set; }

        public bool Visible
        {
            get => renderText.Visible;
            set => renderText.Visible = value;
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
        }

        public UIText(IRenderView renderView, byte paletteIndex, IText text, Rect bounds, byte displayLayer = 1,
            TextColor textColor = TextColor.BrightGray, bool shadow = true, TextAlign textAlign = TextAlign.Left,
            bool allowScrolling = false)
        {
            this.renderView = renderView;
            this.text = renderView.TextProcessor.WrapText(text, bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            this.bounds = bounds;
            this.allowScrolling = allowScrolling;
            renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text), this.text, textColor, shadow, bounds, textAlign);
            renderText.DisplayLayer = displayLayer;
            renderText.PaletteIndex = paletteIndex;
            renderText.Visible = true;
            numVisibleLines = bounds.Height / Global.GlyphLineHeight;
            WithScrolling = allowScrolling;
        }

        public void Destroy()
        {
            renderText?.Delete();
        }

        void UpdateText(int lineOffset)
        {
            renderText.Text = renderView.TextProcessor.WrapText(
                renderView.TextProcessor.GetLines(text, lineOffset, numVisibleLines), bounds,
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        public void SetText(IText text)
        {
            renderText.Text = text;
        }

        public void SetTextColor(TextColor textColor)
        {
            renderText.TextColor = textColor;
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

        public bool Click(Position position)
        {
            if (allowScrolling)
            {
                if (lineOffset >= text.LineCount - numVisibleLines)
                {
                    allowScrolling = false;
                    Scrolled?.Invoke(true);
                    Clicked?.Invoke(true);
                }
                else
                {
                    lineOffset = Math.Min(lineOffset + numVisibleLines, text.LineCount - numVisibleLines);

                    UpdateText(lineOffset);

                    Scrolled?.Invoke(false);
                    Clicked?.Invoke(false);
                }

                return true;
            }

            Clicked?.Invoke(true);

            return false;
        }

        public void Clip(Rect area)
        {
            renderText.ClipArea = area;
        }
    }
}
