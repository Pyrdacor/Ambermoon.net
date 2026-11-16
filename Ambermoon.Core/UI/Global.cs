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
using Ambermoon.UI;
using System;
using System.Linq;

namespace Ambermoon
{
    public partial class Global
    {
        public const int LayoutX = 0;
        public const int LayoutY = 37;
        public const int DefaultMap2DViewX = 16;
        public const int DefaultMap2DViewY = 49;
        // The 2D map view is larger in the immersive mode (see SetMap2DView).
        public static int Map2DViewX { get; private set; } = DefaultMap2DViewX;
        public static int Map2DViewY { get; private set; } = DefaultMap2DViewY;
        public static int Map2DViewWidth => RenderMap2D.NUM_VISIBLE_TILES_X * RenderMap2D.TILE_WIDTH;
        public static int Map2DViewHeight => RenderMap2D.NUM_VISIBLE_TILES_Y * RenderMap2D.TILE_HEIGHT;
        // Tiles of the immersive 2D map view. These are odd numbers so the player is in the center.
        // They fit into the screen, so the map borders are fully visible (with black borders around).
        // Around them an additional ring of tiles is drawn which is partially visible at the screen
        // edges, so the black borders only appear at the map borders (see Map2DViewMargin).
        public const int ImmersiveMap2DTilesX = 19;
        public const int ImmersiveMap2DTilesY = 11;
        // The world map has no borders, so it just shows more tiles which are cut at the screen edges.
        public const int ImmersiveWorldMap2DTilesX = 21;
        public const int ImmersiveWorldMap2DTilesY = 13;
        /// <summary>
        /// Number of additionally drawn tiles at each side of the 2D map view.
        /// They are only drawn if they are inside the map.
        /// </summary>
        public static int Map2DViewMargin { get; private set; } = 0;
        /// <summary>
        /// The area where 2D map tiles and characters are drawn (the view plus the margin).
        /// </summary>
        public static Rect Map2DRenderArea => new
        (
            Map2DViewX - Map2DViewMargin * RenderMap2D.TILE_WIDTH,
            Map2DViewY - Map2DViewMargin * RenderMap2D.TILE_HEIGHT,
            Map2DViewWidth + 2 * Map2DViewMargin * RenderMap2D.TILE_WIDTH,
            Map2DViewHeight + 2 * Map2DViewMargin * RenderMap2D.TILE_HEIGHT
        );

        /// <summary>
        /// Sets the size and position of the 2D map view. In the immersive mode
        /// it covers the whole screen. Maps which are smaller than the view are
        /// centered with a black border around them.
        /// </summary>
        internal static void SetMap2DView(bool immersive, Map? map)
        {
            if (!immersive)
            {
                RenderMap2D.SetVisibleTiles(RenderMap2D.DEFAULT_VISIBLE_TILES_X, RenderMap2D.DEFAULT_VISIBLE_TILES_Y);
                Map2DViewX = DefaultMap2DViewX;
                Map2DViewY = DefaultMap2DViewY;
                Map2DViewMargin = 0;
                return;
            }

            int tilesX = ImmersiveWorldMap2DTilesX;
            int tilesY = ImmersiveWorldMap2DTilesY;
            Map2DViewMargin = 0;

            if (map != null && !map.IsWorldMap)
            {
                tilesX = Math.Min(ImmersiveMap2DTilesX, map.Width);
                tilesY = Math.Min(ImmersiveMap2DTilesY, map.Height);
                Map2DViewMargin = 1;
            }

            RenderMap2D.SetVisibleTiles(tilesX, tilesY);
            Map2DViewX = (VirtualScreenWidth - tilesX * RenderMap2D.TILE_WIDTH) / 2;
            Map2DViewY = (VirtualScreenHeight - tilesY * RenderMap2D.TILE_HEIGHT) / 2;
        }
        public const int DefaultMap3DViewX = 32;
        public const int DefaultMap3DViewY = 49;
        public const int DefaultMap3DViewWidth = 144;
        public const int DefaultMap3DViewHeight = 144;
        // The 3D map view covers the whole screen in the immersive mode (see SetMap3DView).
        public static int Map3DViewX { get; private set; } = DefaultMap3DViewX;
        public static int Map3DViewY { get; private set; } = DefaultMap3DViewY;
        public static int Map3DViewWidth { get; private set; } = DefaultMap3DViewWidth;
        public static int Map3DViewHeight { get; private set; } = DefaultMap3DViewHeight;

        /// <summary>
        /// Sets the size and position of the 3D map view. In the immersive mode it
        /// covers the whole screen, so more of the map is visible at the sides.
        /// </summary>
        internal static void SetMap3DView(bool immersive)
        {
            if (immersive)
            {
                Map3DViewX = 0;
                Map3DViewY = 0;
                Map3DViewWidth = VirtualScreenWidth;
                Map3DViewHeight = VirtualScreenHeight;
            }
            else
            {
                Map3DViewX = DefaultMap3DViewX;
                Map3DViewY = DefaultMap3DViewY;
                Map3DViewWidth = DefaultMap3DViewWidth;
                Map3DViewHeight = DefaultMap3DViewHeight;
            }
        }
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
        public static readonly Rect PartyMemberPortraitArea = new(0, 0, 320, 36);
        public const int GlyphWidth = 6;
        public const int GlyphLineHeight = 7;
        public static readonly Rect CombatBackgroundArea = new(0, 38, 320, 95);
        public const int BattleFieldX = 96;
        public const int BattleFieldY = 134;
        public const int BattleFieldSlotWidth = 16;
        public const int BattleFieldSlotHeight = 13;
        public static readonly Rect BattleFieldArea = new(BattleFieldX, BattleFieldY, 6 * BattleFieldSlotWidth, 5 * BattleFieldSlotHeight);
        public static Rect BattleFieldSlotArea(int column, int row) => new
        (
            BattleFieldX + column * BattleFieldSlotWidth,
            BattleFieldY + row * BattleFieldSlotHeight,
            BattleFieldSlotWidth, BattleFieldSlotHeight
        );
        public static Rect BattleFieldSlotArea(int index) => BattleFieldSlotArea(index % 6, index / 6);
        public static readonly Rect AutomapArea = new(0, 37, 208, 163);
        public static readonly Rect UpperRightArea = new(208, 49, 96, 80);
        public static readonly Rect ButtonGridArea = new(ButtonGridX, ButtonGridY, 3 * Button.Width, 3 * Button.Height);
		public static readonly Rect MobileMovementIndicator = new(208 + 16, 49 + 8, 64, 64);


		internal static Rect GetTextRect(int glyphHeight, Rect rect)
        {
            if (glyphHeight == GlyphLineHeight)
                return rect;

            return rect.CreateModified(0, GlyphLineHeight - glyphHeight, 0, glyphHeight - GlyphLineHeight);
        }

        public static Rect GetTextRect(IGameRenderView renderView, Rect rect) => GetTextRect(renderView.FontProvider.GetFont().GlyphHeight, rect);
    }
}
