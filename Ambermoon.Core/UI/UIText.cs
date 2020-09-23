using Ambermoon.Data;
using Ambermoon.Render;
using System;

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
        public bool WithScrolling { get; private set; }

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

        public UIText(IRenderView renderView, IText text, Rect bounds, byte displayLayer = 1,
            TextColor textColor = TextColor.Gray, bool shadow = true, TextAlign textAlign = TextAlign.Left, bool allowScrolling = false)
        {
            this.renderView = renderView;
            this.text = renderView.TextProcessor.WrapText(text, bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            this.bounds = bounds;
            this.allowScrolling = allowScrolling;
            renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text), this.text, textColor, shadow, bounds, textAlign);
            renderText.DisplayLayer = displayLayer;
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

        public void SetBounds(Rect bounds)
        {
            if (renderText.Width != bounds.Width || renderText.Height != bounds.Height)
                renderText.Resize(bounds.Width, bounds.Height);

            renderText.X = bounds.X;
            renderText.Y = bounds.Y;
        }

        public void SetPosition(Position position)
        {
            renderText.X = position.X;
            renderText.Y = position.Y;
        }

        public bool Click(Position position)
        {
            if (allowScrolling)
            {
                lineOffset = Math.Min(lineOffset + numVisibleLines, text.LineCount - numVisibleLines);

                UpdateText(lineOffset);

                if (lineOffset == text.LineCount - numVisibleLines)
                {
                    allowScrolling = false;
                    Scrolled?.Invoke(true);
                }
                else
                    Scrolled?.Invoke(false);

                Clicked?.Invoke(false);

                return true;
            }

            Clicked?.Invoke(true);

            return false;
        }
    }
}
