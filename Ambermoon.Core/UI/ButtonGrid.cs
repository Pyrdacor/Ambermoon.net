/*
 * ButtonGrid.cs - UI 3x3 button grid implementation
 *
 * Copyright (C) 2020-2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;

namespace Ambermoon.UI
{
    /// <summary>
    /// The 3x3 button grid at the lower right.
    /// </summary>
    internal class ButtonGrid
    {
        readonly Button[] buttons = new Button[9];
        public event Action RightMouseClicked;
        bool rightMouseDown = false;
        readonly Rect area;
        bool visible = true;
        public readonly static Rect[] ButtonAreas = new Rect[9];

        static ButtonGrid()
        {
            for (int i = 0; i < 9; ++i)
            {
                ButtonAreas[i] = new(Global.ButtonGridX + (i % 3) * Button.Width,
                    Global.ButtonGridY + (i / 3) * Button.Height, Button.Width, Button.Height);
            }
		}

        public ButtonGrid(IRenderView renderView)
        {
            area = new Rect(Global.ButtonGridArea);

            for (int i = 0; i < 9; ++i)
            {
                buttons[i] = new Button(renderView, new Position(ButtonAreas[i].Position));
            }
        }

        public Action GetButtonAction(int index) => buttons[index].LeftClickAction;

        public bool IsButtonPressed(int index) => buttons[index].Pressed;

        public CursorType? PressButton(int index, uint currentTicks)
        {
            return Disabled || buttons[index].Disabled ? null : buttons[index].Press(currentTicks);
        }

        public void ReleaseButton(int index, bool immediately = false)
        {
            if (!Disabled && !buttons[index].Disabled)
                buttons[index].Release(immediately);
        }

        public void SetButtonAction(int slot, Action action)
        {
            buttons[slot].LeftClickAction = action;
        }

        public void EnableButton(int slot, bool enable)
        {
            buttons[slot].Disabled = !enable;
        }

		public Button GetButton(int index) => buttons[index];

		public void SetButton(int slot, ButtonType buttonType, bool disabled, Action action,
            bool instantAction, string tooltip = null, Func<CursorType> cursorChangeAction = null,
            uint? continuousActionDelayInTicks = null)
        {
            buttons[slot].ButtonType = buttonType;
            buttons[slot].LeftClickAction = action;
            buttons[slot].InstantAction = instantAction;
            buttons[slot].CursorChangeAction = cursorChangeAction;
            buttons[slot].ContinuousActionDelayInTicks = continuousActionDelayInTicks;
            buttons[slot].Disabled = disabled;
            buttons[slot].Tooltip = tooltip;
        }

        public void HideTooltips()
        {
            foreach (var button in buttons)
                button?.HideTooltip();
        }

        public void Hover(Position position)
        {
            foreach (var button in buttons)
            {
                if (button != null && !button.Disabled && button.Visible)
                    button.Hover(position);
            }
        }

        public void MouseUp(Position position, MouseButtons mouseButtons, out CursorType? newCursorType, uint currentTicks)
        {
            newCursorType = null;

            if (Disabled)
                return;

            if (mouseButtons.HasFlag(MouseButtons.Right))
            {
                if (rightMouseDown)
                {
                    rightMouseDown = false;
                    RightMouseClicked?.Invoke();
                }
                return;
            }

            for (int i = 0; i < 9; ++i)
                buttons[i].LeftMouseUp(position, ref newCursorType, currentTicks);
        }

        public bool MouseDown(Position position, MouseButtons mouseButtons,
            out CursorType? newCursorType, uint currentTicks)
        {
            newCursorType = null;

            if (Disabled)
                return false;

            if (mouseButtons == MouseButtons.Left)
            {
                for (int i = 0; i < 9; ++i)
                {
                    if (buttons[i].LeftMouseDown(position, ref newCursorType, currentTicks))
                        return true;
                }
            }
            else if (mouseButtons == MouseButtons.Right)
            {
                if (area.Contains(position))
                    rightMouseDown = true;
            }

            return false;
        }

        public void Update(uint currentTicks)
        {
            foreach (var button in buttons)
                button.Update(currentTicks);
        }

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                    return;

                visible = value;

                foreach (var button in buttons)
                    button.Visible = visible;
            }
        }

        public byte PaletteIndex
        {
            get => buttons[0].PaletteIndex;
            set
            {
                foreach (var button in buttons)
                    button.PaletteIndex = value;
            }
        }

        public bool Disabled
        {
            get;
            set;
        } = false;
    }
}
