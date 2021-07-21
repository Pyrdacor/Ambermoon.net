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

        public ButtonGrid(IRenderView renderView)
        {
            area = new Rect(Global.ButtonGridX, Global.ButtonGridY, 3 * Button.Width, 3 * Button.Height);

            for (int i = 0; i < 9; ++i)
            {
                buttons[i] = new Button
                (
                    renderView,
                    new Position(Global.ButtonGridX + (i % 3) * Button.Width, Global.ButtonGridY + (i / 3) * Button.Height)
                );
            }
        }

        public Action GetButtonAction(int index) => buttons[index].LeftClickAction;

        public bool IsButtonPressed(int index) => buttons[index].Pressed;

        public CursorType? PressButton(int index, uint currentTicks)
        {
            return buttons[index].Disabled ? null : buttons[index].Press(currentTicks);
        }

        public void ReleaseButton(int index, bool immediately = false)
        {
            if (!buttons[index].Disabled)
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

        public void SetButton(int slot, ButtonType buttonType, bool disabled, Action action,
            bool instantAction, Func<CursorType> cursorChangeAction = null,
            uint? continuousActionDelayInTicks = null)
        {
            buttons[slot].ButtonType = buttonType;
            buttons[slot].LeftClickAction = action;
            buttons[slot].InstantAction = instantAction;
            buttons[slot].CursorChangeAction = cursorChangeAction;
            buttons[slot].ContinuousActionDelayInTicks = continuousActionDelayInTicks;
            buttons[slot].Disabled = disabled;
        }

        public void MouseUp(Position position, MouseButtons mouseButtons, out CursorType? newCursorType, uint currentTicks)
        {
            newCursorType = null;

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
    }
}
