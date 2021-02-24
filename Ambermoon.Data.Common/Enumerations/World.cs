namespace Ambermoon.Data
{
    public enum World
    {
        Lyramion,
        ForestMoon,
        Morag
    }

    public enum WorldFlag
    {
        None = 0,
        Lyramion = 0x01,
        ForestMoon = 0x02,
        Morag = 0x04,
        All = Lyramion | ForestMoon | Morag
    }
}
