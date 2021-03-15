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
        Chest, // closed
        Exit,
        ChestOpened, // not in legend / no text
        Pile,
        Person,
        GotoPoint,
        Invalid = 0xffff // this seems to be used by map objects that are characters and the type should be determined by character type
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
