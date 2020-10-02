using System;

namespace Ambermoon.UI
{
    public enum Window
    {
        MapView,
        Inventory,
        Stats,
        Chest,
        Merchant,
        Event,
        Riddlemouth,
        Conversation
        // TODO ...
    }

    public struct WindowInfo
    {
        public Window Window;
        public object[] WindowParameters; // party member index, chest event, etc
    }
}
