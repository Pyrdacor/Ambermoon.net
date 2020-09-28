using Ambermoon.Render;
using System;
using System.Linq;

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
            FocusOrSubmit,
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
        readonly TextAlign textAlign;
        DateTime lastBlinkTime;
        string currentInput = "";
        string currentText = "";

        public event Action<string> InputSubmitted;
        public event Action Aborted;

        public bool ReactToGlobalClicks
        {
            get;
            set;
        } = false;

        public bool DigitsOnly
        {
            get;
            set;
        } = false;

        public uint? MaxIntegerValue
        {
            get;
            set;
        } = null;

        public string Text
        {
            get => currentText;
            set
            {
                if (currentText == value)
                    return;

                currentText = value;
                UpdateText();
            }
        }

        public uint Value => Text.Length == 0 ? 0 : uint.Parse(Text);

        public bool ClearOnNewInput
        {
            get;
            set;
        } = false;

        public static TextInput FocusedInput { get; private set; } = null;

        public TextInput(IRenderView renderView, Position position, int inputLength, byte displayLayer,
            ClickAction leftClickAction, ClickAction rightClickAction, TextAlign textAlign)
        {
            this.leftClickAction = leftClickAction;
            this.rightClickAction = rightClickAction;
            this.renderView = renderView;
            this.inputLength = inputLength;
            this.textAlign = textAlign;

            // Note: There is always 1 char-slot more as the input length.
            area = new Rect(position.X, position.Y, (inputLength + 1) * Global.GlyphWidth - 2, Global.GlyphLineHeight);
            text = new UIText(renderView, renderView.TextProcessor.CreateText(""), area,
                displayLayer, TextColor.Gray, true, textAlign);

            blinkingCursor = renderView.ColoredRectFactory.Create(5, 5, Color.DarkAccent, displayLayer); // TODO: named palette color?
            blinkingCursor.Layer = renderView.GetLayer(Layer.UI);
            blinkingCursor.X = position.X;
            blinkingCursor.Y = position.Y;
            blinkingCursor.Visible = false;
            lastBlinkTime = DateTime.Now;
        }

        void Submit()
        {
            if (string.IsNullOrEmpty(currentInput))
            {
                Abort();
            }
            else
            {
                if (DigitsOnly)
                {
                    var digits = currentInput.TakeWhile(ch => ch >= '0' && ch <= '9').ToArray();

                    if (digits.Length == 0)
                    {
                        Text = "0";
                    }
                    else
                    {
                        var digitString = new string(digits).TrimStart('0');

                        if (digitString.Length == 0)
                        {
                            Text = "0";
                        }
                        else
                        {
                            uint value = uint.Parse(digitString);

                            if (MaxIntegerValue != null && value > MaxIntegerValue)
                                value = MaxIntegerValue.Value;

                            Text = value.ToString();
                        }
                    }
                }
                else
                {
                    Text = currentInput;
                }

                currentInput = Text;
                LoseFocus();
                UpdateText();
                InputSubmitted?.Invoke(Text);
            }
        }

        public void SetFocus()
        {
            var prevFocusedInput = FocusedInput;
            FocusedInput = this;

            if (prevFocusedInput != null && prevFocusedInput != this)
                prevFocusedInput.blinkingCursor.Visible = false;

            blinkingCursor.Visible = true;
            text.SetTextAlign(TextAlign.Left); // always left if writing
            lastBlinkTime = DateTime.Now;

            if (ClearOnNewInput)
            {
                currentInput = "";
                UpdateText();
            }
        }

        public void LoseFocus()
        {
            if (FocusedInput == this)
            {
                FocusedInput = null;
                blinkingCursor.Visible = false;
                text.SetTextAlign(textAlign);
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
            LoseFocus();
            text?.Destroy();
            blinkingCursor?.Delete();
        }

        void UpdateText()
        {
            text.SetText(renderView.TextProcessor.CreateText(FocusedInput == this ? currentInput : Text));
            blinkingCursor.X = area.X + currentInput.Length * Global.GlyphWidth;
        }

        public bool MouseDown(Position position, MouseButtons mouseButtons)
        {
            if (!ReactToGlobalClicks && !area.Contains(position))
                return false;

            bool clickedInArea = area.Contains(position);

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
                    if (clickedInArea)
                        SetFocus();
                    break;
                case ClickAction.FocusOrSubmit:
                    if (FocusedInput == this)
                        Submit();
                    else if (clickedInArea)
                        SetFocus();
                    break;
                case ClickAction.Abort:
                    Abort();
                    break;
                case ClickAction.LoseFocus:
                    Text = currentInput;
                    LoseFocus();
                    UpdateText();
                    break;
            }

            return true;
        }

        public void Abort()
        {
            currentInput = Text;
            LoseFocus();
            UpdateText();
            Aborted?.Invoke();
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
                    Abort();
                    break;
            }

            return true;
        }
    }
}
