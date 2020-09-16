using Ambermoon.Render;
using System;

namespace Ambermoon.UI
{
    /// <summary>
    /// This will not draw any frame or background. It only
    /// displays the text and blinking cursor and handles
    /// the key inputs.
    /// </summary>
    internal class TextInput
    {
        public enum ClickAction
        {
            Submit,
            Focus,
            Abort,
            LoseFocus
        }

        readonly ClickAction leftClickAction;
        readonly ClickAction rightClickAction;
        readonly IRenderView renderView;
        readonly UIText text;
        readonly IColoredRect blinkingCursor;
        readonly Rect area;
        readonly int inputLength;
        DateTime lastBlinkTime;
        string currentInput = "";

        public event Action<string> InputSubmitted;
        public event Action Aborted;

        public bool ReactToGlobalClicks
        {
            get;
            set;
        } = false;

        public static TextInput FocusedInput { get; private set; } = null;

        public TextInput(IRenderView renderView, Position position, int inputLength, byte displayLayer,
            ClickAction leftClickAction, ClickAction rightClickAction)
        {
            this.leftClickAction = leftClickAction;
            this.rightClickAction = rightClickAction;
            this.renderView = renderView;
            this.inputLength = inputLength;

            // Note: There is always 1 char-slot more as the input length.
            area = new Rect(position.X, position.Y, (inputLength + 1) * Global.GlyphWidth - 2, Global.GlyphLineHeight);
            text = new UIText(renderView, renderView.TextProcessor.CreateText(""), area.CreateModified(0, 0, -(Global.GlyphWidth - 2), 0),
                displayLayer, TextColor.Gray);

            blinkingCursor = renderView.ColoredRectFactory.Create(5, 5, Color.DarkAccent, displayLayer); // TODO: named palette color?
            blinkingCursor.Layer = renderView.GetLayer(Layer.UI);
            blinkingCursor.X = position.X;
            blinkingCursor.Y = position.Y;
            blinkingCursor.Visible = true;
            lastBlinkTime = DateTime.Now;
        }

        void Submit()
        {
            if (string.IsNullOrEmpty(currentInput))
                Aborted?.Invoke();
            else
                InputSubmitted?.Invoke(currentInput);
        }

        public void SetFocus()
        {
            var prevFocusedInput = FocusedInput;
            FocusedInput = this;

            if (prevFocusedInput != null && prevFocusedInput != this)
                prevFocusedInput.blinkingCursor.Visible = false;

            blinkingCursor.Visible = true;
            lastBlinkTime = DateTime.Now;

        }

        void LoseFocus()
        {
            if (FocusedInput == this)
            {
                FocusedInput = null;
                blinkingCursor.Visible = false;
            }

        }

        public void Update()
        {
            if (FocusedInput != this)
                return;

            var elapsed = DateTime.Now - lastBlinkTime;

            if (elapsed.TotalMilliseconds >= 500)
            {
                blinkingCursor.Visible = !blinkingCursor.Visible;
                lastBlinkTime = DateTime.Now;
            }
        }

        public void Destroy()
        {
            text?.Destroy();
            blinkingCursor?.Delete();
        }

        void UpdateText()
        {
            text.SetText(renderView.TextProcessor.CreateText(currentInput));
            blinkingCursor.X = area.X + currentInput.Length * Global.GlyphWidth;
        }

        public bool MouseDown(Position position, MouseButtons mouseButtons)
        {
            if (!ReactToGlobalClicks && !area.Contains(position))
                return false;

            ClickAction? action = mouseButtons switch
            {
                MouseButtons.Left => leftClickAction,
                MouseButtons.Right => rightClickAction,
                _ => null
            };

            if (action == null)
                return false;

            switch (action.Value)
            {
                case ClickAction.Submit:
                    Submit();
                    break;
                case ClickAction.Focus:
                    SetFocus();
                    break;
                case ClickAction.Abort:
                    Aborted?.Invoke();
                    break;
                case ClickAction.LoseFocus:
                    LoseFocus();
                    break;
            }

            return true;
        }

        public bool KeyChar(char ch)
        {
            if (FocusedInput != this)
                return false;

            if (currentInput.Length < inputLength && renderView.TextProcessor.IsValidCharacter(ch))
            {
                currentInput += ch;
                UpdateText();
            }

            return true;
        }

        public bool KeyDown(Key key)
        {
            if (FocusedInput != this)
                return false;

            switch (key)
            {
                case Key.Backspace:
                    if (currentInput.Length != 0)
                    {
                        currentInput = currentInput.Remove(currentInput.Length - 1);
                        UpdateText();
                    }
                    break;
                case Key.Return:
                    Submit();
                    break;
                case Key.Escape:
                    Aborted?.Invoke();
                    break;
            }

            return true;
        }
    }
}
