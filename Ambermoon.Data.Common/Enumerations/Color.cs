namespace Ambermoon.Data.Enumerations
{
    /// <summary>
    /// UI text colors.
    /// 
    /// Note: These colors are only true for the primary UI palette!
    /// </summary>
    public enum Color : byte
    {
        /*//
                    // green (stat UI headers), disabled text, dark gray, red (move blocked cross)
                    0xaa, 0xaa, 0x44, 0xff, 0x66, 0x55, 0x44, 0xff, 0x88, 0x77, 0x66, 0xff, 0x88, 0x11, 0x22, 0xff,
                    // pale red (wait hour message outdoor), pale yellow (wait hour message indoor),
                    // azure (wait hour message 3D), light gray (item info headers)
                    0xbb, 0x77, 0x55, 0xff, 0xcc, 0xaa, 0x44, 0xff, 0x00, 0xcc, 0xff, 0xff, 0xaa, 0xaa, 0x99, 0xff,
                    // light orange (item details headers), 3 blue tones (buff text animation)
                    0xff, 0x99, 0x00, 0xff, 0x00, 0x33, 0x44, 0xff, 0x22, 0x55, 0x66, 0xff, 0x55, 0x77, 0x88, 0xff,
                    */

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

        /*
        Green,
        Disabled,
        DarkGray,
        DarkRed,
        PaleRed,
        PaleYellow,
        Azure,
        LightGray,
        LightOrange,
        DarkBlue,
        LightDarkBlue,
        BluishGray*/
    }

    public static class TextColorExtensions
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
            Color.BluishGray,
            Color.LightDarkBlue,
            Color.DarkBlue,
            Color.LightDarkBlue,
            Color.BluishGray,
            Color.LightBlue,
            Color.White,
            Color.White
        };

        public static Color[] GetTextAnimationColors(this Game game) => textAnimationColors;
        public static Color[] GetTextBlinkColors(this Game game) => textBlinkColors;
    }
}
