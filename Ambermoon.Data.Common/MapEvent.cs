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
        [Flags]
        public enum LockFlags
        {
            // Seen these hex values only:
            // 01, 05, 0A, 0F, 14, 19, 1E, 32, 37, 4B, 55, 63, 64
            // In binary:
            // 0000 0001
            // 0000 0101
            // 0000 1010
            // 0000 1111
            // 0001 0100
            // 0001 1001
            // 0001 1110
            // 0011 0010
            // 0011 0111
            // 0100 1011
            // 0101 0101
            // 0110 0011
            // 0110 0100
            // ---------
            // 0x01 is a locked chest that can be opened with a lockpick.
            // 0x64 could be a locked chest that needs a special key.
            Open = 0,
            Lockpick = 0x01 // these also have a trap attached
        }

        public ushort Unknown { get; set; }
        public LockFlags Lock { get; set; }
        /// <summary>
        /// Note: This is 0-based but the files might by 1-based.
        /// </summary>
        public uint ChestIndex { get; set; }
        public bool RemoveWhenEmpty { get; set; }
        public uint KeyIndex { get; set; }

        public override string ToString()
        {
            return $"Chest {ChestIndex}, Lock=[{Lock}], RemovedWhenEmpty={RemoveWhenEmpty}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Unknown {Unknown:X4}";
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
