/*
 * Global.cs - Global UI values
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
using Ambermoon.Render;
using System.Linq;

namespace Ambermoon
{
    public partial class Global
    {
        public const int LayoutX = 0;
        public const int LayoutY = 37;
        public const int Map2DViewX = 16;
        public const int Map2DViewY = 49;
        public const int Map2DViewWidth = RenderMap2D.NUM_VISIBLE_TILES_X * RenderMap2D.TILE_WIDTH;
        public const int Map2DViewHeight = RenderMap2D.NUM_VISIBLE_TILES_Y * RenderMap2D.TILE_HEIGHT;
        public const int Map3DViewX = 32;
        public const int Map3DViewY = 49;
        public const int Map3DViewWidth = 144;
        public const int Map3DViewHeight = 144;
        public const int ButtonGridX = 208;
        public const int ButtonGridY = 143;
        public const int InventoryX = 109;
        public const int InventoryY = 76;
        public const int InventorySlotWidth = 22;
        public const int InventorySlotHeight = 29;
        public const int InventoryWidth = Inventory.VisibleWidth * InventorySlotWidth;
        public const int InventoryHeight = Inventory.VisibleHeight * InventorySlotHeight;
        public static readonly Rect InventoryTrapArea = Rect.CreateFromBoundaries(108, 75, 179, 185);
        public static readonly Rect InventoryAndEquipTrapArea = Rect.CreateFromBoundaries(19, 71, 179, 185);
        /// <summary>
        /// This includes a 1-pixel border around the portrait.
        /// </summary>
        public static readonly Rect[] PartyMemberPortraitAreas = Enumerable.Range(0, 6).Select(index =>
            new Rect(15 + index * 48, 0, 34, 36)).ToArray();
        /// <summary>
        /// This includes a 1-pixel border around the portrait.
        /// This also includes the condition icon and the bars for HP and SP.
        /// </summary>
        public static readonly Rect[] ExtendedPartyMemberPortraitAreas = Enumerable.Range(0, 6).Select(index =>
            new Rect(15 + index * 48, 0, 48, 36)).ToArray();
        public static readonly Rect PartyMemberPortraitArea = new Rect(0, 0, 320, 36);
        public const int GlyphWidth = 6;
        public const int GlyphLineHeight = 7;
        public static readonly Rect CombatBackgroundArea = new Rect(0, 38, 320, 95);
        public const int BattleFieldX = 96;
        public const int BattleFieldY = 134;
        public const int BattleFieldSlotWidth = 16;
        public const int BattleFieldSlotHeight = 13;
        public static readonly Rect BattleFieldArea = new Rect(BattleFieldX, BattleFieldY, 6 * BattleFieldSlotWidth, 5 * BattleFieldSlotHeight);
        public static Rect BattleFieldSlotArea(int column, int row) => new Rect
        (
            BattleFieldX + column * BattleFieldSlotWidth,
            BattleFieldY + row * BattleFieldSlotHeight,
            BattleFieldSlotWidth, BattleFieldSlotHeight
        );
        public static Rect BattleFieldSlotArea(int index) => BattleFieldSlotArea(index % 6, index / 6);
        public static readonly Rect AutomapArea = new Rect(0, 37, 208, 163);
    }
}
