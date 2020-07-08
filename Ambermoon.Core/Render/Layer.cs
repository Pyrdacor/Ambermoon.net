namespace Ambermoon.Render
{
    public enum Layer
    {
        None = -1,
        Map3D,
        MapBackground1,
        MapBackground2,
        MapBackground3,
        MapBackground4,
        MapBackground5,
        MapBackground6,
        MapBackground7,
        MapBackground8,
        Characters,
        MapForeground1,
        MapForeground2,
        MapForeground3,
        MapForeground4,
        MapForeground5,
        MapForeground6,
        MapForeground7,
        MapForeground8,
        UIBackground,
        BattleMonsterRowFarthest,
        BattleMonsterRowFar,
        BattleMonsterRowCenter,
        BattleMonsterRowNear,
        BattleMonsterRowNearest,
        UIForeground, // including borders
        Items,
        Popup,
        Cursor,

        First2DLayer = MapBackground1,
        Last2DLayer = MapForeground8
    }
}
