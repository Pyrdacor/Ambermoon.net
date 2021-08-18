/*
 * TextInput.cs - Text input control
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
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;

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
        bool visible = true;

        public event Action<string> InputSubmitted;
        public event Action Aborted;
        public event Action<string> InputChanged;

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
        public bool AllowEmpty
        {
            get;
            set;
        } = false;

        /// <summary>
        /// If true losing focus will also submit the current input.
        /// </summary>
        public bool AutoSubmit
        {
            get;
            set;
        } = false;

        public void SetText(string text)
        {
            currentInput = text;
            currentText = text;
            UpdateText();
        }

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                text.Visible = value;

                if (!value)
                    blinkingCursor.Visible = false;
            }
        }

        public uint Value => Text.Length == 0 ? 0 : uint.Parse(Text);

        public bool ClearOnNewInput
        {
            get;
            set;
        } = false;

        public bool Empty => string.IsNullOrEmpty(currentInput);

        public bool Whitespace => string.IsNullOrWhiteSpace(currentInput);

        public static TextInput FocusedInput { get; private set; } = null;

        public TextInput(Game game, IRenderView renderView, Position position, int inputLength, byte displayLayer,
            ClickAction leftClickAction, ClickAction rightClickAction, TextAlign textAlign)
        {
            this.leftClickAction = leftClickAction;
            this.rightClickAction = rightClickAction;
            this.renderView = renderView;
            this.inputLength = inputLength;
            this.textAlign = textAlign;

            // Note: There is always 1 char-slot more as the input length.
            area = new Rect(position.X, position.Y, (inputLength + 1) * Global.GlyphWidth - 2, Global.GlyphLineHeight);
            text = new UIText(renderView, game?.UIPaletteIndex ?? (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1),
                renderView.TextProcessor.CreateText(""), area, displayLayer, TextColor.BrightGray, true, textAlign);

            blinkingCursor = renderView.ColoredRectFactory.Create(5, 5, game?.GetUIColor(28) ?? new Color(0x66, 0x66, 0x55), displayLayer);
            blinkingCursor.Layer = renderView.GetLayer(Layer.UI);
            blinkingCursor.X = position.X;
            blinkingCursor.Y = position.Y;
            blinkingCursor.Visible = false;
            lastBlinkTime = DateTime.Now;
        }

        public void MoveTo(Position position)
        {
            area.Position = new Position(position);
            text.SetPosition(position);
            blinkingCursor.X = position.X;
            blinkingCursor.Y = position.Y;
        }

        public void Submit()
        {
            if ((!AllowEmpty || DigitsOnly) && string.IsNullOrEmpty(currentInput))
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
            if (!Visible)
                return;

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

                if (AutoSubmit)
                    Submit();
            }

        }

        public void Update()
        {
            if (FocusedInput != this)
                return;

            var elapsed = DateTime.Now - lastBlinkTime;

            if (elapsed.TotalMilliseconds >= 350)
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
            string currentText = FocusedInput == this ? currentInput : Text;
            text.SetText(renderView.TextProcessor.CreateText(currentText));
            blinkingCursor.X = area.X + currentInput.Length * Global.GlyphWidth;
            InputChanged?.Invoke(currentText);
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
