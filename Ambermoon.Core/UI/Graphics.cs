using Ambermoon.Data.Enumerations;

namespace Ambermoon.UI
{
    internal static class Graphics
    {
        // Background layer
        public const uint LayoutOffset = 0u;
        public const uint UIElementOffset = 20u;
        // Foreground layer
        public const uint PortraitOffset = 0u;
        public const uint Pics80x80Offset = 120u;
        // Popup layer
        public const uint EventPictureOffset = 20u;

        // We load 3 things into the same layer -> GraphicType.UIElements
        // 1. Our own UI elements like scrollbars, etc (see UICustomGraphic)
        // 2. Game UI elements from the executable (see UIGraphic)
        // 3. Game button graphics from the executable (see ButtonType)
        static readonly uint UIGraphicOffset = UIElementOffset + (uint)Enum.NameCount<UICustomGraphic>();
        static readonly uint ButtonOffset = UIGraphicOffset + (uint)Enum.NameCount<UIGraphic>();

        public static uint GetScrollbarGraphicIndex(ScrollbarType scrollbarType) => UIElementOffset + (uint)scrollbarType;
        public static uint GetCustomUIGraphicIndex(UICustomGraphic customGraphic) => UIElementOffset + (uint)customGraphic;
        public static uint GetUIGraphicIndex(UIGraphic graphic) => UIGraphicOffset + (uint)graphic;
        public static uint GetButtonGraphicIndex(ButtonType buttonType) => ButtonOffset + (uint)buttonType;
        public static uint GetPopupFrameGraphicIndex(PopupFrame frame) => (uint)frame;
    }
}
