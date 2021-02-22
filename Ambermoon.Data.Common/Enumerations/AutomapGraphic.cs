namespace Ambermoon.Data.Enumerations
{
    public enum AutomapGraphic
    {
        // Note: All automap graphics use the second palette from the 3 executable palettes.
        // Note: Fill inner map area with AA7744 (index 6). Lines (like walls) are drawn with 663300 (index 7).
        // Note: All map border images use 3 bit palette format with palette index offset 0.
        // Note: The image data starts at offset 0x100. Before that there seem to be some
        // information data.
        MapUpperLeft, // 32x32
        MapUpperRight, // 32x32
        MapLowerLeft, // 32x32
        MapLowerRight, // 32x32
        MapBorderTop1, // 16x32
        MapBorderTop2, // 16x32
        MapBorderTop3, // 16x32
        MapBorderTop4, // 16x32
        MapBorderRight1, // 32x32
        MapBorderRight2, // 32x32
        MapBorderBottom1, // 16x32
        MapBorderBottom2, // 16x32
        MapBorderBottom3, // 16x32
        MapBorderBottom4, // 16x32
        MapBorderLeft1, // 32x32
        MapBorderLeft2, // 32x32
        // Note: All following automap graphics use the 5 bit palette format and have a size of 16x16 per frame.
        PinLowerHalf,
        PinUpperHalf,
        PinDirectionUp,
        PinDirectionUpRight,
        PinDirectionRight,
        PinDirectionDownRight,
        PinDirectionDown,
        PinDirectionDownLeft,
        PinDirectionLeft,
        PinDirectionUpLeft,
        Riddlemouth, // 4 frames
        Teleport, // 4 frames
        Spinner, // 4 frames
        Trap, // 4 frames (skull)
        TrapDoor, // 4 frames (hole)
        Special, // 4 frames (exclamation mark)
        Monster, // 4 frames (red sphere)
        DoorClosed, // 1 frame
        DoorOpen, // 1 frame
        Merchant, // 1 frame
        Inn, // 1 frame
        ChestClosed, // 1 frame
        Exit, // 1 frame (X)
        ChestOpen, // 1 frame
        Pile, // 1 frame
        Person, // 1 frame (green sphere)
        GotoPoint // 7 frames
    }
}
