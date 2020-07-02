/*
 * RenderMap.cs - Handles map rendering
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Generic;

namespace Ambermoon.Render
{
    internal class RenderMap
    {
        public const int TILE_WIDTH = 32;
        public const int TILE_HEIGHT = 32;
        public const int NUM_VISIBLE_TILES_X = 11; // maps will always be at least 11x11 in size
        public const int NUM_VISIBLE_TILES_Y = 9; // maps will always be at least 11x11 in size
        const int NUM_TILES = NUM_VISIBLE_TILES_X * NUM_VISIBLE_TILES_Y;
        Map map = null;
        Map[] adjacentMaps = null;
        readonly IMapManager mapManager = null;
        readonly ITextureAtlas textureAtlas = null;
        readonly List<IAnimatedSprite> backgroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        readonly List<IAnimatedSprite> foregroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        uint ticksPerFrame = 0;
        bool worldMap = false;
        uint lastFrame = 0;

        public uint ScrollX { get; private set; } = 0;
        public uint ScrollY { get; private set; } = 0;

        public RenderMap(Map map, IMapManager mapManager, IRenderView renderView,
            ISpriteFactory spriteFactory, ITextureAtlas textureAtlas, uint ticksPerFrame,
            uint initialScrollX = 0, uint initialScrollY = 0)
        {
            this.mapManager = mapManager;
            this.textureAtlas = textureAtlas;

            var backgroundLayer = renderView.GetLayer(Layer.MapBackground);
            var foregroundLayer = renderView.GetLayer(Layer.MapForeground);

            for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var backgroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 0, textureAtlas.Texture.Width);
                    var foregroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 0, textureAtlas.Texture.Width);

                    backgroundSprite.Layer = backgroundLayer;
                    backgroundSprite.Visible = false;
                    backgroundSprite.X = column * TILE_WIDTH;
                    backgroundSprite.Y = row * TILE_HEIGHT;
                    foregroundSprite.Layer = foregroundLayer;
                    foregroundSprite.Visible = false;
                    foregroundSprite.X = column * TILE_WIDTH;
                    foregroundSprite.Y = row * TILE_HEIGHT;

                    backgroundTileSprites.Add(backgroundSprite);
                    foregroundTileSprites.Add(foregroundSprite);
                }
            }

            SetMap(map, ticksPerFrame, initialScrollX, initialScrollY);
        }

        public void UpdateAnimations(uint ticks)
        {
            uint frame = ticks / ticksPerFrame;

            if (frame != lastFrame)
            {
                int index = 0;

                for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
                {
                    for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                    {
                        if (backgroundTileSprites[index].NumFrames != 1)
                            backgroundTileSprites[index].CurrentFrame = frame;
                        if (foregroundTileSprites[index].NumFrames != 1)
                            foregroundTileSprites[index].CurrentFrame = frame;
                        ++index;
                    }
                }

                lastFrame = frame;
            }
        }

        void UpdateTiles()
        {
            int index = 0;

            for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var tile = map.Tiles[ScrollX + column, ScrollY + row];

                    backgroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(tile.BackGraphicIndex);

                    if (tile.FrontGraphicIndex == 0)
                    {
                        foregroundTileSprites[index].Visible = false;
                    }
                    else
                    {
                        foregroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(tile.FrontGraphicIndex);
                        foregroundTileSprites[index].Visible = true;
                    }
                }
            }

            UpdateAnimations(0);
        }

        public void SetMap(Map map, uint ticksPerFrame, uint initialScrollX = 0, uint initialScrollY = 0)
        {
            if (this.map == map)
                return;

            this.map = map;
            this.ticksPerFrame = ticksPerFrame;

            if (map.IsWorldMap)
            {
                worldMap = true;
                adjacentMaps = new Map[3]
                {
                    mapManager.GetMap(map.RightMapIndex.Value),
                    mapManager.GetMap(map.DownMapIndex.Value),
                    mapManager.GetMap(map.DownRightMapIndex.Value)
                };
            }
            else
            {
                worldMap = false;
                adjacentMaps = null;
            }

            ScrollTo(initialScrollX, initialScrollY, true); // also updates tiles etc
        }

        public void ScrollTo(uint x, uint y, bool forceUpdate = false)
        {
            if (!forceUpdate && ScrollX == x && ScrollY == y)
                return;

            ScrollX = x;
            ScrollY = y;

            if (!worldMap)
            {
                // check scroll offset for non-world maps
                if (ScrollX > map.Width - NUM_VISIBLE_TILES_X)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll x position is outside the map bounds.");
                if (ScrollY > map.Height - NUM_VISIBLE_TILES_Y)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll y position is outside the map bounds.");
            }

            UpdateTiles();
        }
    }
}
