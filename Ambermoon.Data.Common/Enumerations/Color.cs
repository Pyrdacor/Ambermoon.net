namespace Ambermoon.Data.Enumerations
{
    /// <summary>
    /// UI text colors.
    /// 
    /// Note: These colors are only true for the primary UI palette!
    /// </summary>
    public enum Color : byte
    {
        /// <summary>
        /// RGB: 000
        /// </summary>
        Black,
        /// <summary>
        /// RGB: EDC
        /// </summary>
        Beige,
        /// <summary>
        /// RGB: FFE
        /// </summary>
        White,
        /// <summary>
        /// RGB: BBC
        /// </summary>
        BrightBlue,
        /// <summary>
        /// RGB: 89A
        /// </summary>
        LightBlue,
        /// <summary>
        /// RGB: 578
        /// </summary>
        Blue,
        /// <summary>
        /// RGB: 256
        /// </summary>
        DarkerBlue, 
        /// <summary>
        /// RGB: 034
        /// </summary>
        DarkBlue,
        /// <summary>
        /// RGB: FC9
        /// </summary>
        BrightBrown,
        /// <summary>
        /// RGB: EA7
        /// </summary>
        LightBrown,
        /// <summary>
        /// RGB: C85
        /// </summary>
        Brown,
        /// <summary>
        /// RGB: A63
        /// </summary>
        DarkerBrown,
        /// <summary>
        /// RGB: 842
        /// </summary>
        DarkBrown,
        /// <summary>
        /// RGB: 521
        /// </summary>
        VeryDarkBrown,
        /// <summary>
        /// RGB: B80
        /// </summary>
        DarkYellow,
        /// <summary>
        /// RGB: DA0
        /// </summary>
        Yellow,
        /// <summary>
        /// RGB: FC0
        /// </summary>
        LightYellow,
        /// <summary>
        /// RGB: F90
        /// </summary>
        LightOrange,
        /// <summary>
        /// RGB: C60
        /// </summary>
        Orange,
        /// <summary>
        /// RGB: 812
        /// </summary>
        Pink,
        /// <summary>
        /// RGB: C43
        /// </summary>
        Red,
        /// <summary>
        /// RGB: E63
        /// </summary>
        LightRed,
        /// <summary>
        /// RGB: AA4
        /// Usage: Character stat header texts
        /// </summary>
        LightGreen,
        /// <summary>
        /// RGB: 573
        /// </summary>
        Green,
        /// <summary>
        /// RGB: 254
        /// </summary>
        DarkGreen,
        /// <summary>
        /// RGB: 509
        /// </summary>
        Purple,
        /// <summary>
        /// RGB: 222
        /// </summary>
        VeryDarkGray,
        /// <summary>
        /// RGB: 443
        /// </summary>
        DarkGray,
        /// <summary>
        /// RGB: 665
        /// </summary>
        DarkerGray,
        /// <summary>
        /// RGB: 887
        /// </summary>
        Gray,
        /// <summary>
        /// RGB: AA9
        /// </summary>
        LightGray,
        /// <summary>
        /// RGB: CCB
        /// </summary>
        BrightGray,

        /// <summary>
        /// Active/selected party member
        /// </summary>
        ActivePartyMember = LightYellow,
        /// <summary>
        /// Non-selected party member
        /// </summary>
        PartyMember = Red,
        /// <summary>
        /// Dead or disabled party member
        /// </summary>
        DeadPartyMember = LightBlue,
        /// <summary>
        /// Player battle messages
        /// </summary>
        BattlePlayer = White,
        /// <summary>
        /// Monster battle messages
        /// </summary>
        BattleMonster = LightRed,
        /// <summary>
        /// Dark UI element text
        /// </summary>
        Dark = Black,
        /// <summary>
        /// Bright UI element text
        /// </summary>
        Bright = BrightGray,
        /// <summary>
        /// Disabled UI text
        /// </summary>
        Disabled = DarkerGray,
        /// <summary>
        /// Header color in the monster info
        /// </summary>
        MonsterInfoHeader = Gray,
    }

    public static class TextColors
    {
        static readonly Color[] textAnimationColors = new Color[]
        {
            Color.LightRed,
            Color.LightYellow,
            Color.White,
            Color.LightYellow,
            Color.LightRed,
            Color.Red
        };
        static readonly Color[] textBlinkColors = new Color[]
        {
            Color.White,
            Color.LightBlue,
            Color.Blue,
            Color.DarkerBlue,
            Color.DarkBlue,
            Color.DarkerBlue,
            Color.Blue,
            Color.LightBlue,
            Color.White,
            Color.White
        };

        public static Color[] TextAnimationColors => textAnimationColors;
        public static Color[] TextBlinkColors => textBlinkColors;
    }
}
