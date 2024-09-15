namespace Ambermoon.Data.Enumerations
{
    public enum Option : ushort
    {
        Music = 0x01,
        FastBattleMode = 0x02,
        TextJustification = 0x04,
        FloorTexture3D = 0x08,
        CeilingTexture3D = 0x10,
        // TODO: For later possible adjusted ending (Advanced only)
        ValdynTalkedToSheera = 0x4000,
        FoundYellowSphere = 0x8000
    }
}
