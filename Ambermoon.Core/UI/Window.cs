namespace Ambermoon.UI
{
    public enum Window
    {
        MapView,
        Inventory,
        Stats,
        Door,
        Chest,
        Merchant,
        Event,
        Riddlemouth,
        Conversation,
        Battle,
        BattleLoot,
        BattlePositions,
        Trainer,
        FoodDealer,
        Healer,
        Camp,
        Inn,
        HorseSalesman,
        RaftSalesman,
        ShipSalesman,
        Sage,
        Blacksmith,
        Enchanter,
        Automap
        // TODO ...
    }

    public struct WindowInfo
    {
        public Window Window;
        public object[] WindowParameters; // party member index, chest event, etc
        public bool Closable => Window != Window.Battle && Window != Window.MapView; // TODO: add more (windows without exit button)
    }
}
