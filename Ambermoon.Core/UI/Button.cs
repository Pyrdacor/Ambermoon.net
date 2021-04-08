using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;

namespace Ambermoon.UI
{
    internal class Button
    {
        public const int ButtonReleaseTime = 250;
        public const int Width = 32;
        public const int Height = 17;
        public Rect Area { get; }
        ButtonType buttonType = ButtonType.Empty;
        readonly ILayerSprite frameSprite; // 32x17
        readonly ILayerSprite disableOverlay;
        readonly ILayerSprite iconSprite; // 32x13
        readonly ITextureAtlas textureAtlas;
        bool pressed = false;
        bool released = true;
        bool rightMouse = false;
        bool disabled = false;
        bool visible = true;
        DateTime pressedTime = DateTime.MinValue;
        uint lastActionTimeInTicks = 0;
        uint? continuousActionDelayInTicks = null;
        uint? initialContinuousActionDelayInTicks = null;

        public Button(IRenderView renderView, Position position,
            TextureAtlasManager textureAtlasManager = null)
        {
            Area = new Rect(position, new Size(Width, Height));
            byte paletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);

            frameSprite = renderView.SpriteFactory.Create(Width, Height, true, 3) as ILayerSprite;
            disableOverlay = renderView.SpriteFactory.Create(Width, Height - 6, true, 5) as ILayerSprite;
            iconSprite = renderView.SpriteFactory.Create(Width, Height - 4, true, 4) as ILayerSprite;

            var layer = renderView.GetLayer(Layer.UI);
            frameSprite.Layer = layer;
            disableOverlay.Layer = layer;
            iconSprite.Layer = layer;

            textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.UI);
            frameSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.ButtonFrame));
            disableOverlay.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.ButtonDisabledOverlay));
            iconSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetButtonGraphicIndex(ButtonType.Empty));

            frameSprite.PaletteIndex = paletteIndex;
            disableOverlay.PaletteIndex = paletteIndex;
            iconSprite.PaletteIndex = paletteIndex;

            frameSprite.X = position.X;
            frameSprite.Y = position.Y;
            disableOverlay.X = position.X;
            disableOverlay.Y = position.Y + 3;
            iconSprite.X = position.X;
            iconSprite.Y = position.Y + 2;

            frameSprite.Visible = true;
            disableOverlay.Visible = false;
            iconSprite.Visible = true;
        }

        public byte DisplayLayer
        {
            get => (byte)(frameSprite.DisplayLayer - 3);
            set
            {
                frameSprite.DisplayLayer = (byte)Math.Min(255, value + 3);
                iconSprite.DisplayLayer = (byte)Math.Min(255, value + 4);
                disableOverlay.DisplayLayer = (byte)Math.Min(255, value + 5);
            }
        }

        public void Destroy()
        {
            frameSprite?.Delete();
            disableOverlay?.Delete();
            iconSprite?.Delete();
        }

        public ButtonType ButtonType
        {
            get => buttonType;
            set
            {
                if (buttonType == value)
                    return;

                buttonType = value;
                Pressed = false;
                released = true;

                iconSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetButtonGraphicIndex(buttonType));
            }
        }

        public Action LeftClickAction
        {
            get;
            set;
        }

        public Action RightClickAction
        {
            get;
            set;
        }

        public Func<CursorType> CursorChangeAction
        {
            get;
            set;
        }

        /// <summary>
        /// If false the action is only triggered when the mouse
        /// button is released inside the button area after it
        /// was pressed in that area.
        /// 
        /// If true the action is immediately triggered when clicked
        /// in the button area.
        /// </summary>
        public bool InstantAction
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Delay between continuous actions while buttons stays
        /// pressed.
        /// 
        /// null means no continuation.
        /// </summary>
        public uint? ContinuousActionDelayInTicks
        {
            get => initialContinuousActionDelayInTicks;
            set
            {
                initialContinuousActionDelayInTicks = value;
                continuousActionDelayInTicks = value;
            }
        }

        /// <summary>
        /// Only used in conjunction with <see cref="ContinuousActionDelayInTicks"/>.
        /// If set to non-zero value each execution will reduce the delay by the given ticks
        /// down to 1 tick at max.
        /// </summary>
        public uint ContinuousActionDelayReductionInTicks
        {
            get;
            set;
        } = 0;

        public bool Pressed
        {
            get => pressed;
            set
            {
                if (pressed == value)
                    return;

                pressed = value;
                frameSprite.TextureAtlasOffset =
                    textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(pressed ? UIGraphic.ButtonFramePressed : UIGraphic.ButtonFrame));
                iconSprite.Y = frameSprite.Y + (pressed ? 4 : 2);

                if (!pressed)
                    continuousActionDelayInTicks = initialContinuousActionDelayInTicks;

            }
        }

        public bool Disabled
        {
            get => disabled || buttonType == ButtonType.Empty || !Visible;
            set
            {
                disabled = value;
                disableOverlay.Visible = Visible && disabled;
            }
        }

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                    return;

                visible = value;

                frameSprite.Visible = visible;
                iconSprite.Visible = visible;
                disableOverlay.Visible = visible && disabled;
            }
        }

        public byte PaletteIndex
        {
            get => frameSprite.PaletteIndex;
            set
            {
                frameSprite.PaletteIndex = value;
                disableOverlay.PaletteIndex = value;
                iconSprite.PaletteIndex = value;
            }
        }

        public void LeftMouseUp(Position position, uint currentTicks)
        {
            CursorType? cursorType = null;
            LeftMouseUp(position, ref cursorType, currentTicks);
        }

        public void LeftMouseUp(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled || rightMouse)
                return;

            if (Pressed && !InstantAction && Area.Contains(position))
            {
                cursorType = ExecuteActions(currentTicks, false);
            }

            released = true;
            Pressed = false;
        }

        public void RightMouseUp(Position position, uint currentTicks)
        {
            if (Disabled || !rightMouse)
                return;

            if (Pressed && !InstantAction && Area.Contains(position))
            {
                ExecuteActions(currentTicks, true);
            }

            rightMouse = false;
            released = true;
            Pressed = false;
        }

        public bool LeftMouseDown(Position position, uint currentTicks)
        {
            CursorType? cursorType = null;
            return LeftMouseDown(position, ref cursorType, currentTicks);
        }

        public bool LeftMouseDown(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled)
                return false;

            if (Area.Contains(position))
            {
                pressedTime = DateTime.Now;
                Pressed = true;
                released = false;
                rightMouse = false;

                if (InstantAction)
                {
                    if (continuousActionDelayInTicks == null)
                        released = true;
                    cursorType = ExecuteActions(currentTicks, false);
                }

                return true;
            }

            return false;
        }

        public bool RightMouseDown(Position position, uint currentTicks)
        {
            if (Disabled)
                return false;

            if (Area.Contains(position))
            {
                pressedTime = DateTime.Now;
                Pressed = true;
                released = false;
                rightMouse = true;

                if (InstantAction)
                {
                    if (continuousActionDelayInTicks == null)
                        released = true;
                    ExecuteActions(currentTicks, true);
                }

                return true;
            }

            return false;
        }

        CursorType? ExecuteActions(uint currentTicks, bool rightMouse)
        {
            lastActionTimeInTicks = currentTicks;
            var cursorChangeAction = CursorChangeAction; // The action invoke might change this by swapping buttons!
            if (rightMouse)
                RightClickAction?.Invoke();
            else
                LeftClickAction?.Invoke();

            if (continuousActionDelayInTicks != null && continuousActionDelayInTicks > 1)
                continuousActionDelayInTicks = (uint)Math.Max(1, (int)continuousActionDelayInTicks.Value - (int)ContinuousActionDelayReductionInTicks);

            return cursorChangeAction?.Invoke();
        }

        internal CursorType? Press(uint currentTicks)
        {
            if (Disabled)
                return null;

            pressedTime = DateTime.Now;
            Pressed = true;
            released = false;
            rightMouse = false;

            if (InstantAction)
            {
                if (continuousActionDelayInTicks == null)
                    released = true;
            }
            else
                released = true;

            return ExecuteActions(currentTicks, false);
        }

        internal void Release()
        {
            released = true;
        }

        public void Update(uint currentTicks)
        {
            if (Pressed && released && (DateTime.Now - pressedTime).TotalMilliseconds >= ButtonReleaseTime)
                Pressed = false;

            if (Pressed && continuousActionDelayInTicks != null)
            {
                if (currentTicks - lastActionTimeInTicks >= continuousActionDelayInTicks.Value)
                    ExecuteActions(currentTicks, rightMouse);
            }
        }
    }
}
