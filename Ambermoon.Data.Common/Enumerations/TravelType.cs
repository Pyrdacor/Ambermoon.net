using System.ComponentModel;

namespace Ambermoon.Data.Enumerations
{
    /// <summary>
    /// The index here matches the travel gfx file index (0-based).
    /// Each travel gfx has 4 frames for directions Up, Right, Down and Left in that order.
    /// </summary>
    public enum TravelType
    {
        Walk,
        Horse,
        Raft,
        Ship,
        MagicalDisc,
        Eagle,
        Fly, // never seen it but looks like flying with a cape like superman :D
        Swim,
        WitchBroom,
        SandLizard,
        SandShip
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TravelTypeExtensions
    {
        public static bool UsesMapObject(this TravelType travelType) => travelType switch
        {
            TravelType.Horse => true,
            TravelType.Raft => true,
            TravelType.Ship => true,
            TravelType.SandLizard => true,
            TravelType.SandShip => true,
            _ => false
        };

        public static bool IsStoppable(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => false,
            TravelType.Swim => false,
            TravelType.Fly => false,
            _ => true
        };
    }
}
