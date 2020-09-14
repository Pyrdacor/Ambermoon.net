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
        Riddlemouth
        // TODO ...
    }

    public struct WindowInfo
    {
        public Window Window;
        public object WindowParameter; // party member index, chest event, etc
    }
}
