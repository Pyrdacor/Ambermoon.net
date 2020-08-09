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
        public const int TextureWidth = 128;
        public const int TextureHeight = 80;
        readonly ICamera3D camera = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        ITextureAtlas textureAtlas = null;
        ISurface3D floor = null;
        ISurface3D ceiling = null;
        Labdata labdata = null;
        readonly List<ISurface3D> walls = new List<ISurface3D>();
        readonly List<ISurface3D> objects = new List<ISurface3D>();
        static readonly Dictionary<uint, ITextureAtlas> labdataTextures = new Dictionary<uint, ITextureAtlas>(); // contains all textures for a labdata (walls, objects and overlays)
        static Graphic[] labBackgroundGraphics = null;
        public Map Map { get; private set; } = null;

        public RenderMap3D(Map map, IMapManager mapManager, IRenderView renderView, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            camera = renderView.Camera3D;
            this.mapManager = mapManager;
            this.renderView = renderView;

            EnsureLabBackgroundGraphics(renderView.GraphicProvider);

            if (map != null)
                SetMap(map, playerX, playerY, playerDirection);
        }

        public void Destroy()
        {
            // TODO
        }

        public void SetMap(Map map, uint playerX, uint playerY, CharacterDirection playerDirection)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to load a 2D map into a 3D render map.");

            camera.SetPosition(playerX * Global.DistancePerTile, (map.Height - playerY) * Global.DistancePerTile);
            camera.TurnTowards((float)playerDirection * 90.0f);

            if (Map != map)
            {
                CleanUp();

                Map = map;
                labdata = mapManager.GetLabdataForMap(map);
                EnsureLabdataTextureAtlas();
                UpdateSurfaces();
                // TODO: objects
            }
        }

        void CleanUp()
        {
            floor?.Delete();
            ceiling?.Delete();

            floor = null;
            ceiling = null;

            walls.ForEach(wall => wall?.Delete());
            objects.ForEach(obj => obj?.Delete());

            walls.Clear();
            objects.Clear();
        }

        void EnsureLabBackgroundGraphics(IGraphicProvider graphicProvider)
        {
            if (labBackgroundGraphics != null)
                return;

            // Note: Palette index 9 is used for the transparent parts (I don't know why) so
            // we replace this index here with index 0.
            labBackgroundGraphics = graphicProvider.GetGraphics(GraphicType.LabBackground).ToArray();

            for (int i = 0; i < labBackgroundGraphics.Length; ++i)
            {
                var labBackgroundGraphic = labBackgroundGraphics[i];

                for (int b = 0; b < labBackgroundGraphic.Width * labBackgroundGraphic.Height; ++b)
                {
                    if (labBackgroundGraphic.Data[b] == 9)
                        labBackgroundGraphic.Data[b] = 0;
                }
            }
        }

        void EnsureLabdataTextureAtlas()
        {
            if (!labdataTextures.ContainsKey(Map.TilesetOrLabdataIndex))
            {
                var graphics = new Dictionary<uint, Graphic>();

                foreach (var obj in labdata.Objects)
                {
                    foreach (var subObj in obj.SubObjects)
                    {
                        if (!graphics.ContainsKey(subObj.Object.TextureIndex))
                            graphics.Add(subObj.Object.TextureIndex, labdata.ObjectGraphics[labdata.ObjectInfos.IndexOf(subObj.Object)]);
                    }
                }
                for (int i = 0; i < labdata.WallGraphics.Count; ++i)
                    graphics.Add((uint)i + 1000u, labdata.WallGraphics[i]);
                graphics.Add(10000u, labdata.FloorGraphic ?? new Graphic(64, 64, 0)); // TODO
                graphics.Add(10001u, labdata.CeilingGraphic ?? new Graphic(64, 64, 0)); // TODO

                if (Map.Flags.HasFlag(MapFlags.Outdoor))
                    graphics.Add(10002u, labBackgroundGraphics[(int)Map.World]);

                labdataTextures.Add(Map.TilesetOrLabdataIndex, TextureAtlasManager.Instance.CreateFromGraphics(graphics, 1));
            }

            textureAtlas = labdataTextures[Map.TilesetOrLabdataIndex];
            renderView.GetLayer(Layer.Map3D).Texture = textureAtlas.Texture;
            renderView.GetLayer(Layer.Billboards3D).Texture = textureAtlas.Texture;
        }

        Position GetObjectTextureOffset(uint objectIndex)
        {
            return textureAtlas.GetOffset(objectIndex);
        }

        Position GetWallTextureOffset(uint wallIndex)
        {
            return textureAtlas.GetOffset(wallIndex + 1000u);
        }

        Position FloorTextureOffset => textureAtlas.GetOffset(10000u);
        Position CeilingTextureOffset => textureAtlas.GetOffset(10001u);

        void AddObject(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, Labdata.Object obj)
        {
            float baseX = mapX * Global.DistancePerTile;
            float baseY = (-Map.Height + mapY) * Global.DistancePerTile;

            // TODO: animations

            const float blockSize = 512.0f;
            const float mappedWallHeight = Global.WallHeight * blockSize / Global.DistancePerTile;

            foreach (var subObject in obj.SubObjects)
            {
                var objectInfo = subObject.Object;
                var mapObject = surfaceFactory.Create(SurfaceType.Billboard,
                    (objectInfo.MappedTextureWidth / blockSize) * Global.DistancePerTile,
                    (objectInfo.MappedTextureHeight / mappedWallHeight) * Global.WallHeight,
                        objectInfo.TextureWidth, objectInfo.TextureHeight, objectInfo.TextureWidth, objectInfo.TextureHeight, true);
                mapObject.Layer = layer;
                mapObject.PaletteIndex = (byte)Map.PaletteIndex;
                mapObject.X = baseX + (subObject.X / blockSize) * Global.DistancePerTile;
                mapObject.Y = ((subObject.Z + objectInfo.MappedTextureHeight) / mappedWallHeight) * Global.WallHeight; // TODO
                mapObject.Z = baseY + Global.DistancePerTile - (subObject.Y / blockSize) * Global.DistancePerTile;
                mapObject.TextureAtlasOffset = GetObjectTextureOffset(objectInfo.TextureIndex);
                mapObject.Visible = true; // TODO: not all objects should be always visible
                objects.Add(mapObject);
            }
        }

        void AddWall(ISurface3DFactory surfaceFactory, IRenderLayer layer, uint mapX, uint mapY, uint wallIndex)
        {
            var wallTextureOffset = GetWallTextureOffset(wallIndex);
            bool alpha = labdata.Walls[(int)wallIndex].Flags.HasFlag(Labdata.WallFlags.Transparency);

            void AddSurface(WallOrientation wallOrientation, float x, float z)
            {
                var wall = surfaceFactory.Create(SurfaceType.Wall, Global.DistancePerTile, Global.WallHeight,
                    TextureWidth, TextureHeight, TextureWidth, TextureHeight, alpha, wallOrientation);
                wall.Layer = layer;
                wall.PaletteIndex = (byte)Map.PaletteIndex;
                wall.X = x;
                wall.Y = Global.WallHeight;
                wall.Z = z;
                wall.TextureAtlasOffset = wallTextureOffset;
                wall.Visible = true; // TODO: not all walls should be always visible
                walls.Add(wall);
            }

            float baseX = mapX * Global.DistancePerTile;
            float baseY = (-Map.Height + mapY) * Global.DistancePerTile;

            // TODO

            // front face
            //if (mapY < Map.Height - 1 && Map.Blocks[mapX, mapY + 1].WallIndex == 0 && !Map.Blocks[mapX, mapY + 1].MapBorder)
                AddSurface(WallOrientation.Normal, baseX, baseY);

            // left face
            //if (mapX > 0 && Map.Blocks[mapX - 1, mapY].WallIndex == 0 && !Map.Blocks[mapX - 1, mapY].MapBorder)
                AddSurface(WallOrientation.Rotated90, baseX + Global.DistancePerTile, baseY);

            // back face
            //if (mapY > 0 && Map.Blocks[mapX, mapY - 1].WallIndex == 0 && !Map.Blocks[mapX, mapY - 1].MapBorder)
                AddSurface(WallOrientation.Rotated180, baseX + Global.DistancePerTile, baseY + Global.DistancePerTile);

            // right face
            //if (mapX < Map.Width - 1 && Map.Blocks[mapX + 1, mapY].WallIndex == 0 && !Map.Blocks[mapX + 1, mapY].MapBorder)
                AddSurface(WallOrientation.Rotated270, baseX, baseY + Global.DistancePerTile);
        }

        void UpdateSurfaces()
        {
            // Delete all surfaces
            floor?.Delete();
            ceiling?.Delete();
            walls.ForEach(s => s?.Delete());

            var surfaceFactory = renderView.Surface3DFactory;
            var layer = renderView.GetLayer(Layer.Map3D);
            var billboardLayer = renderView.GetLayer(Layer.Billboards3D);

            // Add floor and ceiling
            floor = surfaceFactory.Create(SurfaceType.Floor,
                Map.Width * Global.DistancePerTile, Map.Height * Global.DistancePerTile,
                64, 64, (uint)Map.Width * 64, (uint)Map.Height * 64, false);
            floor.PaletteIndex = (byte)Map.PaletteIndex;
            floor.Layer = layer;
            floor.X = 0.0f;
            floor.Y = 0.0f;
            floor.Z = -Map.Height * Global.DistancePerTile;
            floor.TextureAtlasOffset = FloorTextureOffset;
            floor.Visible = true;
            ceiling = surfaceFactory.Create(SurfaceType.Ceiling,
                Map.Width * Global.DistancePerTile, Map.Height * Global.DistancePerTile,
                64, 64, (uint)Map.Width * 64, (uint)Map.Height * 64, false);
            ceiling.PaletteIndex = (byte)Map.PaletteIndex;
            ceiling.Layer = layer;
            ceiling.X = 0.0f;
            ceiling.Y = Global.WallHeight;
            ceiling.Z = 0.0f;
            ceiling.TextureAtlasOffset = CeilingTextureOffset;
            ceiling.Visible = true;

            // Add walls and objects
            for (uint y = 0; y < Map.Height; ++y)
            {
                for (uint x = 0; x < Map.Width; ++x)
                {
                    var block = Map.Blocks[x, y];

                    if (block.WallIndex != 0)
                        AddWall(surfaceFactory, layer, x, y, block.WallIndex - 1);
                    else if (block.ObjectIndex != 0)
                        AddObject(surfaceFactory, billboardLayer, x, y, labdata.Objects[(int)block.ObjectIndex - 1]);
                }
            }
        }
    }
}
