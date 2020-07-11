/*
 * RenderMap3D.cs - Handles 3D map rendering
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
    internal class RenderMap3D : IRenderMap
    {
        public const int DistancePerTile = 2; // TODO
        public const int WallHeight = 3; // TODO
        readonly ICamera3D camera = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        readonly ITextureAtlas textureAtlas = null;
        ISurface3D floor = null;
        ISurface3D ceiling = null;
        readonly List<ISurface3D> walls = new List<ISurface3D>();
        public Map Map { get; private set; } = null;

        public RenderMap3D(Map map, IMapManager mapManager, IRenderView renderView, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            camera = renderView.Camera3D;
            this.mapManager = mapManager;
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Map3D);

            SetMap(map, playerX, playerY, playerDirection);
        }

        public void SetMap(Map map, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            camera.SetPosition(playerX * DistancePerTile, playerY * DistancePerTile);
            camera.TurnTowards((float)playerDirection * 90.0f);

            if (Map != map)
            {
                Map = map;
                UpdateSurfaces();
            }
        }

        void AddWall(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, uint textureIndex, bool overlay)
        {
            void AddSurface(WallOrientation wallOrientation, float x, float z)
            {
                var wall = surfaceFactory.Create(SurfaceType.Wall, DistancePerTile, WallHeight, wallOrientation);
                wall.Layer = layer;
                wall.PaletteIndex = (byte)Map.PaletteIndex;
                wall.X = x;
                wall.Y = WallHeight;
                wall.Z = -z;
                wall.TextureAtlasOffset = textureAtlas.GetOffset(textureIndex);
                wall.Visible = true; // TODO: not all walls should be always visible
                walls.Add(wall);
            }

            float baseX = mapX * DistancePerTile;
            float baseY = mapY * DistancePerTile;

            // front face
            //if (overlay || (mapY < Map.Height - 1 && Map.Tiles[mapX, mapY + 1].BackTileIndex == 0))
                AddSurface(WallOrientation.Normal, baseX, baseY);

            // left face
            //if (overlay || (mapX > 0 && Map.Tiles[mapX - 1, mapY].BackTileIndex == 0))
                AddSurface(WallOrientation.Rotated90, baseX + DistancePerTile, baseY);

            // back face
            //if (overlay || (mapY > 0 && Map.Tiles[mapX, mapY - 1].BackTileIndex == 0))
                AddSurface(WallOrientation.Rotated180, baseX + DistancePerTile, baseY + DistancePerTile);

            // right face
            //if (overlay || (mapX < Map.Width - 1 && Map.Tiles[mapX + 1, mapY].BackTileIndex == 0))
                AddSurface(WallOrientation.Rotated270, baseX, baseY + DistancePerTile);
        }

        void UpdateSurfaces()
        {
            // Delete all surfaces
            floor?.Delete();
            ceiling?.Delete();
            walls.ForEach(s => s?.Delete());

            var surfaceFactory = renderView.Surface3DFactory;
            var layer = renderView.GetLayer(Layer.Map3D);

            // Add floor and ceiling
            floor = surfaceFactory.Create(SurfaceType.Floor, Map.Width * DistancePerTile, Map.Height * DistancePerTile);
            floor.PaletteIndex = (byte)Map.PaletteIndex;
            floor.Layer = layer;
            floor.X = 0.0f;
            floor.Y = 0.0f;
            floor.Z = -Map.Height * DistancePerTile;
            floor.Visible = true;
            ceiling = surfaceFactory.Create(SurfaceType.Ceiling, Map.Width * DistancePerTile, Map.Height * DistancePerTile);
            ceiling.PaletteIndex = (byte)Map.PaletteIndex;
            ceiling.Layer = layer;
            ceiling.X = 0.0f;
            ceiling.Y = WallHeight;
            ceiling.Z = 0.0f;
            ceiling.Visible = true;

            // Add walls
            for (uint y = 0; y < Map.Height; ++y)
            {
                for (uint x = 0; x < Map.Width; ++x)
                {
                    var tile = Map.Tiles[x, y];

                    if (tile.BackTileIndex != 0 && tile.BackTileIndex != 255)
                        AddWall(surfaceFactory, layer, x, y, tile.BackTileIndex, false);

                    // TODO
                    /*if (tile.FrontTileIndex != 0)
                        AddWall(surfaceFactory, layer, x, y, tile.FrontTileIndex, true);*/
                }
            }

            // TODO: add billboards (monsters, NPCs, flames, etc)
        }
    }
}
