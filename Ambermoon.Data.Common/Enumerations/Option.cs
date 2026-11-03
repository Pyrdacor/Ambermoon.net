namespace Ambermoon.Data.Enumerations
{
    public enum Option : ushort
    {
        Music = 0x01,
        FastBattleMode = 0x02,
        TextJustification = 0x04,
        FloorTexture3D = 0x08,
        CeilingTexture3D = 0x10,
        // Ambermoon.net only (not used by the original game data): items which were obtained once
        ObtainedMapOfLyramion = 0x20,
        ObtainedRuneAlphabet = 0x40,
        // TODO: For later possible adjusted ending (Advanced only)
        ValdynTalkedToSheera = 0x4000,
        FoundYellowSphere = 0x8000
    }
}
