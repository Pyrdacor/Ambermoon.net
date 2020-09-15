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
}
