namespace Ambermoon.Data
{
    public enum MapEventType
    {
        Unknown,
        MapChange, // doors, etc
        Unknown2,
        Unknown3,
        Unknown4,
        Unknown5,
        Unknown6,
        Unknown7,
        Unknown8,
        Unknown9,
        Unknown10,
        Unknown11,
        Unknown12,
        Unknown13,
        Unknown14,
        Unknown15,
        // TODO ...
        // Maybe: Message popup, activatable by hand/eye/mouth cursor, etc
    }

    public enum MapEventTrigger
    {
        Move,
        Hand,
        Eye,
        Mouth
    }

    public class MapEvent
    {
        public MapEventType Type { get; set; }
    }

    public class MapChangeEvent : MapEvent
    {
        public uint MapIndex { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }
    }
}
