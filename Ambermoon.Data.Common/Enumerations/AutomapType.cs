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
        ChestOpened, // not in legend / no text (I guessed opened chest)
        Pile,
        Person,
        GotoPoint
    }

    public static class AutomapExtensions
    {
        public static AutomapGraphic? ToGraphic(this AutomapType automapType) => automapType switch
        {
            AutomapType.Riddlemouth => AutomapGraphic.Riddlemouth,
            AutomapType.Teleporter => AutomapGraphic.Teleport,
            AutomapType.Spinner => AutomapGraphic.Spinner,
            AutomapType.Trap => AutomapGraphic.Trap,
            AutomapType.Trapdoor => AutomapGraphic.TrapDoor,
            AutomapType.Special => AutomapGraphic.Special,
            AutomapType.Monster => AutomapGraphic.Monster,
            AutomapType.Door => AutomapGraphic.DoorClosed,
            AutomapType.DoorOpen => AutomapGraphic.DoorOpen,
            AutomapType.Merchant => AutomapGraphic.Merchant,
            AutomapType.Tavern => AutomapGraphic.Inn,
            AutomapType.Chest => AutomapGraphic.ChestClosed,
            AutomapType.Exit => AutomapGraphic.Exit,
            AutomapType.ChestOpened => AutomapGraphic.ChestOpen,
            AutomapType.Pile => AutomapGraphic.Pile,
            AutomapType.Person => AutomapGraphic.Person,
            AutomapType.GotoPoint => AutomapGraphic.GotoPoint,
            _ => null
        };
    }
}
