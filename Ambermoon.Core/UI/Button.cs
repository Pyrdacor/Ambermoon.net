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
        readonly Rect area;
        ButtonType buttonType;
        readonly ILayerSprite frameSprite; // 32x17
        readonly ILayerSprite disableOverlay;
        readonly ILayerSprite iconSprite; // 32x13
        readonly ITextureAtlas textureAtlas;
        bool pressed = false;
        bool released = true;
        DateTime pressedTime = DateTime.MinValue;
        uint lastActionTimeInTicks = 0;

        public Button(IRenderView renderView, Position position)
        {
            area = new Rect(position, new Size(Width, Height));

            frameSprite = renderView.SpriteFactory.Create(Width, Height, false, true, 3) as ILayerSprite;
            disableOverlay = renderView.SpriteFactory.Create(Width - 8, Height - 6, false, true, 4) as ILayerSprite;
            iconSprite = renderView.SpriteFactory.Create(Width, Height - 4, false, true, 2) as ILayerSprite;

            var layer = renderView.GetLayer(Layer.UIBackground);
            frameSprite.Layer = layer;
            disableOverlay.Layer = layer;
            iconSprite.Layer = layer;

            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground);
            frameSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.ButtonFrame));
            disableOverlay.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.ButtonDisableOverlay));
            iconSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetButtonGraphicIndex(ButtonType.Empty));

            frameSprite.PaletteIndex = 50;
            disableOverlay.PaletteIndex = 50;
            iconSprite.PaletteIndex = 49;

            frameSprite.X = position.X;
            frameSprite.Y = position.Y;
            disableOverlay.X = position.X + 4;
            disableOverlay.Y = position.Y + 3;
            iconSprite.X = position.X;
            iconSprite.Y = position.Y + 2;

            frameSprite.Visible = true;
            disableOverlay.Visible = false;
            iconSprite.Visible = true;
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

        public Action Action
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
            get;
            set;
        } = null;

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
            }
        }

        public bool Disabled
        {
            get => disableOverlay.Visible || buttonType == ButtonType.Empty;
            set => disableOverlay.Visible = value;
        }

        public void LeftMouseUp(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled)
                return;

            if (Pressed && area.Contains(position))
            {
                released = true;
                cursorType = ExecuteActions(currentTicks);
            }

            Pressed = false;
        }

        public bool LeftMouseDown(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled)
                return false;

            if (area.Contains(position))
            {
                pressedTime = DateTime.Now;
                Pressed = true;
                released = false;

                if (InstantAction)
                {
                    if (ContinuousActionDelayInTicks == null)
                        released = true;
                    cursorType = ExecuteActions(currentTicks);
                }

                return true;
            }

            return false;
        }

        CursorType? ExecuteActions(uint currentTicks)
        {
            lastActionTimeInTicks = currentTicks;
            var cursorChangeAction = CursorChangeAction; // The action invoke might change this by swapping buttons!
            Action?.Invoke();
            return cursorChangeAction?.Invoke();
        }

        internal CursorType? Press(uint currentTicks)
        {
            if (Disabled)
                return null;

            pressedTime = DateTime.Now;
            Pressed = true;
            released = false;

            if (InstantAction)
            {
                if (ContinuousActionDelayInTicks == null)
                    released = true;
            }
            else
                released = true;

            return ExecuteActions(currentTicks);
        }

        internal void Release()
        {
            released = true;
        }

        public void Update(uint currentTicks)
        {
            if (Pressed && released && (DateTime.Now - pressedTime).TotalMilliseconds >= ButtonReleaseTime)
                Pressed = false;

            if (Pressed && ContinuousActionDelayInTicks != null)
            {
                if (currentTicks - lastActionTimeInTicks >= ContinuousActionDelayInTicks.Value)
                    ExecuteActions(currentTicks);
            }
        }
    }
}
