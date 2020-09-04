namespace Ambermoon.Data.Enumerations
{
    /// <summary>
    /// Buttons use Empty or EmptyPressed as background.
    /// Then there is an optional icon starting at Yes.
    /// The icon should be drawn over the background with
    /// alpha test active.
    /// If the button is disabled the DisableOverlay is
    /// drawn on top with alpha test active.
    /// </summary>
    public enum ButtonType
    {
        Empty,
        EmptyPressed,
        DisableOverlay,
        Yes,
        No,
        Eye,
        Hand,
        Mouth,
        Transport,
        Spells,
        Camp,
        Map,
        BattlePositions,
        Options,
        Wait,
        MoveUpLeft,
        MoveUp,
        MoveUpRight,
        MoveLeft,
        MoveRight,
        MoveDownLeft,
        MoveDown,
        MoveDownRight,
        TurnLeft,
        MoveForward,
        TurnRight,
        StrafeLeft,
        StrafeRight,
        RotateLeft,
        MoveBackward,
        RotateRight,
        Quit,
        Exit,
        Opt,
        Save,
        Load,
        ReadMagic,
        Sleep,
        ShowItem, // also look at item
        GiveItem,
        AskToJoin,
        AskToLeave,
        GiveGold,
        GiveFood,
        DistributeGold,
        DistributeFood,
        LootGold, // to single player
        LootFood, // to single player
        Lockpick,
        UseItem,
        FindTrap,
        DisarmTrap,
        Ear, // hear riddle
        Buy,
        Sell
        // TODO ... sage, healer, trainer, etc
    }
}
