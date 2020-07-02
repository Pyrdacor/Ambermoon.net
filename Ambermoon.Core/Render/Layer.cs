namespace Ambermoon.Render
{
    public enum Layer
    {
        None = -1,
        MapBackground,
        Player,
        MapForeground,
        UIBackground,
        BattleMonsterRowFarthest,
        BattleMonsterRowFar,
        BattleMonsterRowCenter,
        BattleMonsterRowNear,
        BattleMonsterRowNearest,
        UIForeground, // including borders
        Items,
        Popup,
        Cursor
    }
}
