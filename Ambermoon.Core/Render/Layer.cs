namespace Ambermoon
{
    public enum Layer
    {
        None = -1,
        Map3D,
        Billboards3D,
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
        CombatBackground,
        BattleMonsterRowFarthest,
        BattleMonsterRowFar,
        BattleMonsterRowCenter,
        BattleMonsterRowNear,
        BattleMonsterRowNearest,
        UI,
        Items,
        Text,
        Effects,
        Cursor
    }

    public partial class Global
    {
        public const Layer First2DLayer = Layer.MapBackground1;
        public const Layer Last2DLayer = Layer.MapForeground8;
    }
}
