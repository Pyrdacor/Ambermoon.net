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
        PortraitBackground // black to blue gradient
    }
}
