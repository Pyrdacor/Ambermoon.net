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

        public static bool CanStandOn(this TravelType travelType) => travelType switch
        {
            TravelType.Raft => true,
            TravelType.Ship => true,
            TravelType.SandShip => true,
            _ => false
        };

        public static bool CanCampOn(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => true,
            TravelType.Horse => true,
            TravelType.Raft => true,
            TravelType.Ship => true,
            TravelType.SandLizard => true,
            TravelType.SandShip => true,
            TravelType.MagicalDisc => true,
            _ => false
        };

        public static bool IsStoppable(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => false,
            TravelType.Swim => false,
            _ => true
        };

        public static bool BlockedByWater(this TravelType travelType) => travelType switch
        {
            TravelType.Horse => true,
            TravelType.SandLizard => true,
            _ => false
        };

        public static bool BlockedByTeleport(this TravelType travelType) => travelType switch
        {
            TravelType.Horse => true,
            TravelType.MagicalDisc => true,
            TravelType.SandLizard => true,
            _ => false
        };

        public static bool IgnoreEvents(this TravelType travelType) => travelType switch
        {
            TravelType.Eagle => true,
            TravelType.WitchBroom => true,
            _ => false
        };

        public static Song TravelSong(this TravelType travelType) => travelType switch
        {
            TravelType.Walk => Song.Default,
            TravelType.Horse => Song.HorseIsNoDisgrace,
            TravelType.Raft => Song.RiversideTravellingBlues,
            TravelType.Ship => Song.Ship,
            TravelType.MagicalDisc => Song.CompactDisc,
            TravelType.Eagle => Song.WholeLottaDove,
            TravelType.Fly => Song.WholeLottaDove,
            TravelType.Swim => Song.Default,
            TravelType.WitchBroom => Song.BurnBabyBurn,
            TravelType.SandLizard => Song.MellowCamelFunk,
            TravelType.SandShip => Song.PsychedelicDuneGroove,
            _ => Song.Default
        };
    }
}
