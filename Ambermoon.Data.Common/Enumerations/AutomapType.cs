namespace Ambermoon.Data.Enumerations
{
    public enum AutomapType
    {
        None, // not in legend / no text
        Wall, // not in legend / no text
        Riddlemouth,
        Teleporter,
        Spinner,
        Trap,
        Trapdoor,
        Special,
        Monster,
        Door, // closed
        DoorOpen,  // not in legend / no text
        Merchant,
        Tavern,
        Chest, // Treasure
        Exit,
        Unknown, // not in legend / no text (maybe fake walls / secret doors?)
        Pile,
        Person,
        GotoPoint
    }
}
