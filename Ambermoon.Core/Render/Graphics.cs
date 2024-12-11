/*
 * Graphics.cs - Some graphic related values
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;

namespace Ambermoon.Render
{
    public static class Graphics
    {
        // Characters
        public const uint TravelGraphicOffset = 3 * 17;
        public const uint TransportGraphicOffset = TravelGraphicOffset + 12 * 4;
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
        public const uint AutomapOffset = 3200u;
        public const uint RiddlemouthOffset = 3300u;
        public const uint RiddlemouthEyeIndex = RiddlemouthOffset;
        public const uint RiddlemouthMouthIndex = RiddlemouthOffset + 1;

        // We load 3 things into the same layer -> GraphicType.UIElements
        // 1. Our own UI elements like scrollbars, etc (see UICustomGraphic)
        // 2. Game UI elements from the executable (see UIGraphic)
        // 3. Game button graphics from the executable (see ButtonType)
        static readonly uint UIGraphicOffset = UICustomGraphicOffset + (uint)EnumHelper.NameCount<UICustomGraphic>();
        static readonly uint ButtonOffset = UIGraphicOffset + (uint)EnumHelper.NameCount<UIGraphic>();
        static readonly uint PopupFrameOffset = UIGraphicOffset;

        internal static uint GetScrollbarGraphicIndex(ScrollbarType scrollbarType) => UICustomGraphicOffset + (uint)scrollbarType;
        public static uint GetCustomUIGraphicIndex(UICustomGraphic customGraphic) => UICustomGraphicOffset + (uint)customGraphic;
        public static uint GetUIGraphicIndex(UIGraphic graphic) => UIGraphicOffset + (uint)graphic;
        public static uint GetButtonGraphicIndex(ButtonType buttonType) => ButtonOffset + (uint)buttonType;
        internal static uint GetPopupFrameGraphicIndex(PopupFrame frame) => PopupFrameOffset + (uint)frame;
        public static UIGraphic? GetConditionGraphic(Condition condition) => condition switch
        {
            Condition.Irritated => UIGraphic.StatusIrritated,
            Condition.Crazy => UIGraphic.StatusCrazy,
            Condition.Sleep => UIGraphic.StatusSleep,
            Condition.Panic => UIGraphic.StatusPanic,
            Condition.Blind => UIGraphic.StatusBlind,
            Condition.Drugged => UIGraphic.StatusDrugs,
            Condition.Exhausted => UIGraphic.StatusExhausted,
            Condition.Lamed => UIGraphic.StatusLamed,
            Condition.Poisoned => UIGraphic.StatusPoisoned,
            Condition.Petrified => UIGraphic.StatusPetrified,
            Condition.Diseased => UIGraphic.StatusDiseased,
            Condition.Aging => UIGraphic.StatusAging,
            Condition.DeadCorpse => UIGraphic.StatusDead,
            Condition.DeadAshes => UIGraphic.StatusDead,
            Condition.DeadDust => UIGraphic.StatusDead,
            _ => null
        };
        public static uint GetConditionGraphicIndex(Condition condition) => GetUIGraphicIndex(GetConditionGraphic(condition).Value);
        public static uint GetAutomapGraphicIndex(AutomapGraphic automapGraphic) => AutomapOffset + (uint)automapGraphic;
        public static uint GetNPCGraphicIndex(uint npcFileIndex, uint npcIndex, IGraphicProvider graphicProvider) =>
            NPCGraphicOffset + (graphicProvider.NPCGraphicOffsets.TryGetValue((int)npcFileIndex, out var offset) ? (uint)offset : 0) + npcIndex;
	}
}
