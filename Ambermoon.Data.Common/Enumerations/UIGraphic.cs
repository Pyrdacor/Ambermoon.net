namespace Ambermoon.Data.Enumerations
{
    public enum UIGraphic
    {
        DisabledOverlay16x16,
        FrameUpperLeft,
        FrameLeft,
        FrameLowerLeft,
        FrameTop,
        FrameBottom,
        FrameUpperRight,
        FrameRight,
        FrameLowerRight,
        StatusDead,
        StatusAttack,
        StatusDefend,
        StatusUseMagic,
        StatusFlee,
        StatusMove,
        StatusUseItem,
        StatusHandStop,
        StatusHandTake,
        StatusLamed,
        StatusPoisoned,
        StatusPetrified,
        StatusDiseased,
        StatusAging,
        StatusIrritated,
        StatusCrazy,
        StatusSleep,
        StatusPanic,
        StatusBlind,
        StatusOverweight,
        StatusDrugs,
        StatusExhausted,
        StatusRangeAttack,
        Eagle,
        Explosion,
        Ouch,
        StarBlinkAnimation, // 4 frames (16x15)
        PlusBlinkAnimation, // 4 frames (16x10)
        LeftPortraitBorder, // 16x36
        CharacterValueBarFrames, // 16x36
        RightPortraitBorder, // 16x36
        SmallBorder1, // 16x1
        SmallBorder2, // 16x1
        Candle, // light
        Shield, // magic protection buff
        Sword, // magic attack buff
        Star, // anti-magic buff
        Eye, // magic sight
        Map, // mystic map
        Windchain,
        MonsterEyeInactive,
        MonsterEyeActive,
        Night,
        Dusk,
        Day,
        Dawn,
        ButtonFrame,
        ButtonFramePressed,
        ButtonDisabledOverlay, // 32x11 (1-bit)
        Compass,
        Attack, // 16x9
        Defense, // 16x9
        Skull,
        EmptyCharacterSlot,
        ItemConsume, // 11 frames with 16x16 pixels
        Talisman, // healer's golden symbol / talisman
        Unused, // seems to be unused in original code, 26 bytes
        BrokenItemOverlay // 16x16 (1-bit) is colored with color index 26
    }
}
