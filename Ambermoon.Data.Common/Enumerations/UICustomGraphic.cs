namespace Ambermoon.Data.Enumerations
{
    /// <summary>
    /// We provide some additional UI elements that are not
    /// part of the data (or it is somewhere inside the
    /// executable data) like scrollbars, free portrait slots
    /// and so on.
    /// </summary>
    public enum UICustomGraphic
    {
        ScrollbarSmallVertical, // chests, merchants, etc
        ScrollbarSmallVerticalHighlighted,
        ScrollbarLargeVertical, // inventory
        ScrollbarLargeVerticalHighlighted,
        ScrollbarBackgroundSmallVertical,
        ScrollbarSmallVerticalDisabled,
        ScrollbarBackgroundLargeVertical,
        ScrollbarLargeVerticalDisabled,
        ItemSlotBackground,
        ItemSlotDisabled, // locked chests
        PortraitBackground, // black to blue gradient
        PortraitBorder, // thin 1-pixel border for top and bottom of portraits
        ButtonDisableOverlay, // 28x13 pixel gray dotted overlay
        MapDisableOverlay, // 320x144 (2D maps use 176x144, 3D maps use only 144x144, battle use 320x95)
        AmbermoonInfoBox, // 128x19
        BiggerInfoBox, // 144x26
        BattleFieldYellowBorder,
        BattleFieldOrangeBorder,
        BattleFieldGreenHighlight
    }
}
