using System;
using System.Linq;

namespace Ambermoon.Data
{
    public enum MapEventType
    {
        Unknown,
        MapChange, // doors, etc
        TextPopup, // TODO: not sure yet, could be a text from the map_texts file
        Chest, // all kinds of lootable map objects
        Event, // looks like events that start a sequence (like grandfather in bed at the beginning)
        Unknown5,
        Hurt, // the burning fire places in grandfathers house have these
        Unknown7,
        Unknown8,
        Unknown9,
        Unknown10,
        Unknown11,
        Unknown12,
        UseMapObject, // buckets, candles, etc
        Unknown14,
        Unknown15,
        Unknown16,
        Unknown17,
        Unknown18,
        Unknown19,
        Unknown20,
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
        public MapEvent Next { get; set; }
    }

    public class MapChangeEvent : MapEvent
    {
        public uint MapIndex { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }

        public override string ToString()
        {
            return $"Map {MapIndex} / Position {X},{Y}";
        }
    }

    public class ChestMapEvent : MapEvent
    {
        public enum LockType
        {
            Lockpick = 1,
            Key = 2,
            Open = 255
        }

        public LockType Lock { get; set; }
        public uint ChestIndex { get; set; }
        public bool RemoveWhenEmpty { get; set; }
        public uint KeyIndex { get; set; }

        public override string ToString()
        {
            return $"Chest {ChestIndex}, {Lock}, RemovedWhenEmpty={RemoveWhenEmpty}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}";
        }
    }

    public class DebugMapEvent : MapEvent
    {
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return string.Join(" ", Data.Select(d => d.ToString("X2")));
        }
    }
}
