using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;

namespace Ambermoon.Render
{
    internal static class Graphics
    {
        // Characters
        public const uint TravelGraphicOffset = 3 * 17;
        public const uint TransportGraphicOffset = TravelGraphicOffset + 11 * 4;
        public const uint NPCGraphicOffset = TransportGraphicOffset + 5;

        // UI layer
        public const uint LayoutOffset = 0u;
        public const uint PortraitOffset = 20u;
        public const uint Pics80x80Offset = 150u;
        public const uint EventPictureOffset = 200u;
        public const uint UICustomGraphicOffset = 250u;
        public const uint CombatBackgroundOffset = 2000u;
        public const uint CombatGraphicOffset = 2500u;
        public const uint BattleFieldIconOffset = 3000u;

        // We load 3 things into the same layer -> GraphicType.UIElements
        // 1. Our own UI elements like scrollbars, etc (see UICustomGraphic)
        // 2. Game UI elements from the executable (see UIGraphic)
        // 3. Game button graphics from the executable (see ButtonType)
        static readonly uint UIGraphicOffset = UICustomGraphicOffset + (uint)Enum.NameCount<UICustomGraphic>();
        static readonly uint ButtonOffset = UIGraphicOffset + (uint)Enum.NameCount<UIGraphic>();
        static readonly uint PopupFrameOffset = UIGraphicOffset;

        public static uint GetScrollbarGraphicIndex(ScrollbarType scrollbarType) => UICustomGraphicOffset + (uint)scrollbarType;
        public static uint GetCustomUIGraphicIndex(UICustomGraphic customGraphic) => UICustomGraphicOffset + (uint)customGraphic;
        public static uint GetUIGraphicIndex(UIGraphic graphic) => UIGraphicOffset + (uint)graphic;
        public static uint GetButtonGraphicIndex(ButtonType buttonType) => ButtonOffset + (uint)buttonType;
        public static uint GetPopupFrameGraphicIndex(PopupFrame frame) => PopupFrameOffset + (uint)frame;
        public static UIGraphic GetAilmentGraphic(Ailment ailment) => ailment switch
        {
            Ailment.Irritated => UIGraphic.StatusIrritated,
            Ailment.Crazy => UIGraphic.StatusCrazy,
            Ailment.Sleep => UIGraphic.StatusSleep,
            Ailment.Panic => UIGraphic.StatusPanic,
            Ailment.Blind => UIGraphic.StatusBlind,
            Ailment.Drugged => UIGraphic.StatusDrugs,
            Ailment.Exhausted => UIGraphic.StatusExhausted,
            Ailment.Lamed => UIGraphic.StatusLamed,
            Ailment.Poisoned => UIGraphic.StatusPoisoned,
            Ailment.Petrified => UIGraphic.StatusPetrified,
            Ailment.Diseased => UIGraphic.StatusDiseased,
            Ailment.Aging => UIGraphic.StatusAging,
            Ailment.DeadCorpse => UIGraphic.StatusDead,
            Ailment.DeadAshes => UIGraphic.StatusDead,
            Ailment.DeadDust => UIGraphic.StatusDead,
            _ => throw new AmbermoonException(ExceptionScope.Application, "Ailment has no graphic")
        };
        public static uint GetAilmentGraphicIndex(Ailment ailment) => GetUIGraphicIndex(GetAilmentGraphic(ailment));
    }
}
