namespace Ambermoon.Data
{
    public enum World : byte
    {
        Lyramion,
        ForestMoon,
        Morag
    }

    public enum WorldFlag : byte
    {
        None = 0,
        Lyramion = 0x01,
        ForestMoon = 0x02,
        Morag = 0x04,
        All = Lyramion | ForestMoon | Morag
    }
}
