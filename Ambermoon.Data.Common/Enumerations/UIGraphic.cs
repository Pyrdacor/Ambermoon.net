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
        Eagle, // 32x29 (5-bit)
        DamageSplash, // 32x26 (5-bit)
        Ouch, // 32x23
        StarBlinkAnimation, // 4 frames (16x9, 5-bit)
        PlusBlinkAnimation, // 4 frames (16x10)
        LeftPortraitBorder, // 16x36
        CharacterValueBarFrames, // 16x36
        RightPortraitBorder, // 16x36
        SmallBorder1, // 16x1
        SmallBorder2, // 16x1
        Candle, // light buff icon (16x16)
        Shield, // magic protection buff icon (16x16)
        Sword, // magic attack buff icon (16x16)
        Star, // anti-magic buff icon (16x16)
        Eye, // clairvoyance buff icon (16x16)
        Map, // mystic map buff icon (16x16)
        Windchain, // 32x15
        MonsterEyeInactive, // 32x32
        MonsterEyeActive, // 32x32
        Night, // 32x32
        Dusk, // 32x32
        Day, // 32x32
        Dawn, // 32x32
        ButtonFrame, // 32x17
        ButtonFramePressed, // 32x17
        ButtonDisabledOverlay, // 32x11 (1-bit)
        Compass, // 32x32
        Attack, // 16x9
        Defense, // 16x9
        Skull, // 32x34
        EmptyCharacterSlot, // 32x34
        ItemConsume, // 11 frames with 16x16 pixels
        Talisman, // healer's golden symbol / talisman (32x29, 5-bit)
        Unused, // seems to be unused in original code, 26 bytes
        BrokenItemOverlay // 16x16 (1-bit) is colored with color index 26
    }
}
