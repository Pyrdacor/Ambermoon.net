/*
 * RenderMap2D.cs - Handles 2D map rendering
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Render
{
    internal class RenderMap2D
    {
        class Transport
        {
            // This is the array index in the savegame transport location array.
            public int Index { get; set; }
            public Position Position { get; set; }
            public ISprite Sprite { get; set; }
            public Position Offset { get; set; }
        }

        public const int TILE_WIDTH = 16;
        public const int TILE_HEIGHT = 16;
        public const int NUM_VISIBLE_TILES_X = 11; // maps will always be at least 11x11 in size
        public const int NUM_VISIBLE_TILES_Y = 9; // maps will always be at least 11x11 in size
        const int NUM_TILES = NUM_VISIBLE_TILES_X * NUM_VISIBLE_TILES_Y;
        readonly Game game;
        public Map Map { get; private set; } = null;
        Map[] adjacentMaps = null;
        Tileset tileset = null;
        readonly IMapManager mapManager = null;
        readonly IRenderView renderView = null;
        ITextureAtlas textureAtlas = null;
        readonly List<IAnimatedSprite> backgroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        readonly List<IAnimatedSprite> foregroundTileSprites = new List<IAnimatedSprite>(NUM_TILES);
        readonly List<Transport> mapTransports = new List<Transport>();
        uint ticksPerFrame = 0;
        bool worldMap = false;
        uint lastFrame = 0;
        Position lastUpdateScroll = null;
        Map lastUpdateMap = null;
        readonly Dictionary<uint, MapCharacter2D> mapCharacters = new Dictionary<uint, MapCharacter2D>();
        readonly RandomAnimationInfo[] randomAnimationFramesBackground = new RandomAnimationInfo[NUM_TILES];
        readonly RandomAnimationInfo[] randomAnimationFramesForeground = new RandomAnimationInfo[NUM_TILES];

        class RandomAnimationInfo
        {
            public int CurrentFrame { get; set; }
            public int FrameCount { get; set; }
            public bool Poison { get; set; }
        }

        public event Action<Map, Map[]> MapChanged;

        public uint ScrollX { get; private set; } = 0;
        public uint ScrollY { get; private set; } = 0;

        public RenderMap2D(Game game, Map map, IMapManager mapManager, IRenderView renderView,
            uint initialScrollX = 0, uint initialScrollY = 0)
        {
            this.game = game;
            this.mapManager = mapManager;
            this.renderView = renderView;

            var spriteFactory = renderView.SpriteFactory;

            for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var backgroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 1);
                    var foregroundSprite = spriteFactory.CreateAnimated(TILE_WIDTH, TILE_HEIGHT, 0, 1);

                    backgroundSprite.Visible = true;
                    backgroundSprite.X = Global.Map2DViewX + column * TILE_WIDTH;
                    backgroundSprite.Y = Global.Map2DViewY + row * TILE_HEIGHT;
                    foregroundSprite.Visible = false;
                    foregroundSprite.X = Global.Map2DViewX + column * TILE_WIDTH;
                    foregroundSprite.Y = Global.Map2DViewY + row * TILE_HEIGHT;

                    backgroundTileSprites.Add(backgroundSprite);
                    foregroundTileSprites.Add(foregroundSprite);
                }
            }

            SetMap(map, initialScrollX, initialScrollY);
        }

        public void Update(uint ticks, ITime gameTime, bool monstersCanMoveImmediately, Position lastPlayerPosition)
        {
            uint frame = ticks / ticksPerFrame;

            void PoisonPlayer(int x, int y)
            {
                var playerPosition = game.PartyPosition - Map.MapOffset;

                if (playerPosition.X == x && playerPosition.Y == y)
                {
                    game.ForeachPartyMember((p, f) =>
                    {
                        if (game.RollDice100() >= p.Attributes[Data.Attribute.Luck].TotalCurrentValue)
                        {
                            game.AddCondition(Condition.Poisoned, p);
                            game.ShowDamageSplash(p, _ => 0, f);
                        }
                        else
                        {
                            f?.Invoke();
                        }
                    }, p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified), () => game.ResetMoveKeys());
                }
            }

            if (frame != lastFrame)
            {
                int index = 0;

                for (int row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
                {
                    for (int column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                    {
                        uint animationFrameBackground = frame;
                        uint animationFrameForeground = frame;
                        var randomAnimationBackground = randomAnimationFramesBackground[column + row * NUM_VISIBLE_TILES_X];
                        var randomAnimationForeground = randomAnimationFramesForeground[column + row * NUM_VISIBLE_TILES_X];

                        if (randomAnimationBackground != null || randomAnimationForeground != null)
                        {
                            bool poison = randomAnimationBackground?.Poison == true || randomAnimationForeground?.Poison == true;
                            bool poisoned = false;

                            if (randomAnimationBackground != null)
                            {
                                int frameCountBackground = randomAnimationBackground.FrameCount;                                

                                if (++randomAnimationBackground.CurrentFrame >= frameCountBackground) // full cycle passed
                                {
                                    randomAnimationFramesBackground[column + row * NUM_VISIBLE_TILES_X] = CreateRandomStart(frameCountBackground, poison);
                                    animationFrameBackground = 0;
                                }
                                else if (randomAnimationBackground.CurrentFrame >= 0)
                                {
                                    animationFrameBackground = (uint)randomAnimationBackground.CurrentFrame;

                                    if (poison && randomAnimationBackground.CurrentFrame == 0 && !game.TravelType.IgnoreEvents())
                                    {
                                        poisoned = true;
                                        PoisonPlayer((int)ScrollX + column, (int)ScrollY + row);
                                    }
                                }
                                else
                                {
                                    animationFrameBackground = 0;
                                }
                            }

                            if (randomAnimationForeground != null)
                            {
                                int frameCountForeground = randomAnimationForeground.FrameCount;

                                if (++randomAnimationForeground.CurrentFrame >= frameCountForeground) // full cycle passed
                                {
                                    randomAnimationFramesForeground[column + row * NUM_VISIBLE_TILES_X] = CreateRandomStart(frameCountForeground, poison);
                                    animationFrameForeground = 0;
                                }
                                else if (randomAnimationForeground.CurrentFrame >= 0)
                                {
                                    animationFrameForeground = (uint)randomAnimationForeground.CurrentFrame;

                                    if (!poisoned && poison && randomAnimationForeground.CurrentFrame == 0 && !game.TravelType.IgnoreEvents())
                                    {
                                        poisoned = true;
                                        PoisonPlayer((int)ScrollX + column, (int)ScrollY + row);
                                    }
                                }
                                else
                                {
                                    animationFrameForeground = 0;
                                }
                            }
                        }
                        else if (animationFrameBackground == 0 || animationFrameForeground == 0)
                        {
                            var tile = this[ScrollX + (uint)column, ScrollY + (uint)row];

                            if (tile.FrontTileIndex != 0 && animationFrameForeground == 0)
                            {
                                var frontTile = tileset.Tiles[(int)tile.FrontTileIndex - 1];

                                if (frontTile.Flags.HasFlag(Tileset.TileFlags.AutoPoison) && !game.TravelType.IgnoreAutoPoison())
                                    PoisonPlayer((int)ScrollX + column, (int)ScrollY + row);
                            }
                            else if (tile.BackTileIndex != 0 && animationFrameBackground == 0)
                            {
                                var backTile = tileset.Tiles[(int)tile.BackTileIndex - 1];

                                if (backTile.Flags.HasFlag(Tileset.TileFlags.AutoPoison) && !game.TravelType.IgnoreAutoPoison())
                                    PoisonPlayer((int)ScrollX + column, (int)ScrollY + row);
                            }
                        }

                        if (backgroundTileSprites[index].NumFrames != 1)
                            backgroundTileSprites[index].CurrentFrame = animationFrameBackground;
                        if (foregroundTileSprites[index].NumFrames != 1)
                            foregroundTileSprites[index].CurrentFrame = animationFrameForeground;
                        ++index;
                    }
                }

                lastFrame = frame;
            }

            foreach (var mapCharacter in mapCharacters)
                mapCharacter.Value.Update(ticks, gameTime, monstersCanMoveImmediately, lastPlayerPosition);
        }

        public void Pause()
        {
            foreach (var character in mapCharacters)
                character.Value.Paused = true;
        }

        public void Resume()
        {
            foreach (var character in mapCharacters)
                character.Value.Paused = false;
        }

        bool TestCharacterInteraction(MapCharacter2D mapCharacter, bool cursor, Position position)
        {
            if (!mapCharacter.Visible || !mapCharacter.Active)
                return false;

            if (position == mapCharacter.Position)
                return true;

            return cursor && mapCharacter.IsRealCharacter && position == new Position(mapCharacter.Position.X, mapCharacter.Position.Y - 1);
        }

        public bool TriggerEvents(IRenderPlayer player, EventTrigger trigger,
            uint x, uint y, IMapManager mapManager, uint ticks, Savegame savegame)
        {
            if (trigger != EventTrigger.Always)
            {
                // First check character interaction
                var position = new Position((int)x, (int)y);
                foreach (var mapCharacter in mapCharacters.ToList())
                {
                    if (TestCharacterInteraction(mapCharacter.Value, trigger != EventTrigger.Move, position) &&
                        mapCharacter.Value.Interact(trigger, this[(uint)mapCharacter.Value.Position.X,
                            (uint)mapCharacter.Value.Position.Y].Type == Map.TileType.Bed))
                        return true;
                }
            }

            if (x >= Map.Width)
            {
                if (y >= Map.Height)
                    return adjacentMaps[2].TriggerEvents(game, trigger, x - (uint)Map.Width,
                        y - (uint)Map.Height, savegame);
                else
                    return adjacentMaps[0].TriggerEvents(game, trigger, x - (uint)Map.Width,
                        y, savegame);
            }
            else if (y >= Map.Height)
            {
                return adjacentMaps[1].TriggerEvents(game, trigger, x, y - (uint)Map.Height,
                    savegame);
            }
            else
            {
                return Map.TriggerEvents(game, trigger, x, y, savegame);
            }
        }

        public Event GetEvent(uint x, uint y, Savegame savegame)
        {
            if (x >= Map.Width)
            {
                if (y >= Map.Height)
                    return adjacentMaps[2].GetEvent(x - (uint)Map.Width, y - (uint)Map.Height, savegame);
                else
                    return adjacentMaps[0].GetEvent(x - (uint)Map.Width, y, savegame);
            }
            else if (y >= Map.Height)
            {
                return adjacentMaps[1].GetEvent(x, y - (uint)Map.Height, savegame);
            }
            else
            {
                return Map.GetEvent(x, y, savegame);
            }
        }

        /// <summary>
        /// Converts a map view position (pixels) to a tile position
        /// (x gives the tile column index and y the tile row index).
        /// 
        /// Note that x and y can exceed map width or height for world
        /// maps as there can be 2x2 world maps displayed at the same
        /// time. The tile position will still be relative to the
        /// first map though.
        /// </summary>
        /// <param name="position">Position inside the map view in pixels</param>
        /// <returns>Null if not a valid map position or the transformed position otherwise</returns>
        public Position PositionToTile(Position position)
        {
            if (position.X < 0 || position.Y < 0 ||
                position.X >= Global.Map2DViewWidth || position.Y >= Global.Map2DViewHeight)
                return null;

            return new Position
            (
                (int)ScrollX + position.X / TILE_WIDTH,
                (int)ScrollY + position.Y / TILE_HEIGHT
            );
        }

        public Position GetCenterPosition()
        {
            return new Position((int)ScrollX + NUM_VISIBLE_TILES_X / 2, (int)ScrollY + NUM_VISIBLE_TILES_Y / 2);
        }

        public Map GetMapFromTile(uint x, uint y)
        {
            if (adjacentMaps == null)
                return Map;

            if (x >= Map.Width)
            {
                if (y >= Map.Height)
                    return adjacentMaps[2];
                else
                    return adjacentMaps[0];
            }
            else if (y >= Map.Height)
            {
                return adjacentMaps[1];
            }
            else
            {
                return Map;
            }
        }

        public Map.Tile this[Position position] => this[(uint)position.X, (uint)position.Y];

        public Map.Tile this[uint x, uint y]
        {
            get
            {
                if (x >= Map.Width)
                {
                    if (y >= Map.Height)
                        return adjacentMaps[2].Tiles[x - Map.Width, y - Map.Height];
                    else
                        return adjacentMaps[0].Tiles[x - Map.Width, y];
                }
                else if (y >= Map.Height)
                {
                    return adjacentMaps[1].Tiles[x, y - Map.Height];
                }
                else
                {
                    return Map.Tiles[x, y];
                }
            }
        }

        public void Destroy()
        {
            foreach (var tile in backgroundTileSprites)
                tile.Visible = false;
            foreach (var tile in foregroundTileSprites)
                tile.Visible = false;
            ClearTransports();
            ClearCharacters();
        }

        void ClearCharacters()
        {
            mapCharacters.Values.ToList().ForEach(character => character.Destroy());
            mapCharacters.Clear();
        }

        public void ClearTransports()
        {
            mapTransports.ForEach(transport => transport.Sprite?.Delete());
            mapTransports.Clear();
        }

        void RepositionTransports(Map lastMap)
        {
            if (lastMap == null || !lastMap.UseTravelTypes || !Map.UseTravelTypes)
                return;

            var offset = Map.MapOffset - lastMap.MapOffset;

            if (Math.Abs(offset.X) >= 2 * Map.Width || Math.Abs(offset.Y) >= 2 * Map.Height)
            {
                ClearTransports();
                return;
            }

            foreach (var transport in mapTransports.ToList()) // ToList is needed as we might modify the collection
            {
                var lastMapIndex = lastMap.Index;

                if (transport.Position.X >= lastMap.Width)
                {
                    if (transport.Position.Y >= lastMap.Height)
                        lastMapIndex = lastMap.DownRightMapIndex.Value;
                    else
                        lastMapIndex = lastMap.RightMapIndex.Value;
                }
                else if (transport.Position.Y >= lastMap.Height)
                    lastMapIndex = lastMap.RightMapIndex.Value;

                if (lastMapIndex != Map.Index &&
                    lastMapIndex != Map.RightMapIndex &&
                    lastMapIndex != Map.DownMapIndex &&
                    lastMapIndex != Map.DownRightMapIndex)
                {
                    transport.Sprite.Delete();
                    mapTransports.Remove(transport);
                }
                else
                {
                    transport.Position.X += offset.X * TILE_WIDTH;
                    transport.Position.Y += offset.Y * TILE_HEIGHT;
                }
            }
        }

        internal void UpdateTransports()
        {
            foreach (var transport in mapTransports)
            {
                transport.Sprite.X = Global.Map2DViewX + (int)(transport.Position.X - ScrollX) * TILE_WIDTH + transport.Offset.X;
                transport.Sprite.Y = Global.Map2DViewY + (int)(transport.Position.Y - ScrollY) * TILE_HEIGHT + transport.Offset.Y;
            }
        }

        public void RemoveTransport(int index)
        {
            var transport = mapTransports.FirstOrDefault(t => t.Index == index);

            if (transport != null)
            {
                transport.Sprite.Delete();
                mapTransports.Remove(transport);
            }
        }

        public void PlaceTransport(uint mapIndex, uint x, uint y, TravelType travelType, int index)
        {
            if (mapTransports.Any(t => t.Index == index))
                return;

            var position = new Position((int)x, (int)y);

            if (mapIndex != Map.Index)
            {
                if (mapIndex != adjacentMaps[0].Index &&
                    mapIndex != adjacentMaps[1].Index &&
                    mapIndex != adjacentMaps[2].Index)
                    return;

                if (mapIndex == adjacentMaps[0].Index || mapIndex == adjacentMaps[2].Index)
                    position.X += Map.Width;
                if (mapIndex == adjacentMaps[1].Index || mapIndex == adjacentMaps[2].Index)
                    position.Y += Map.Height;
            }

            var info = renderView.GameData.StationaryImageInfos[travelType];
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Characters);
            var sprite = renderView.SpriteFactory.Create(info.Width, info.Height, false);
            var offset = new Position((TILE_WIDTH - info.Width) / 2 - 2, (TILE_HEIGHT - info.Height) / 2 - 2);
            sprite.Layer = renderView.GetLayer(Layer.Characters);
            sprite.ClipArea = Game.Map2DViewArea;
            sprite.BaseLineOffset = TILE_HEIGHT / 2;
            sprite.PaletteIndex = (byte)game.GetPlayerPaletteIndex();
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.TransportGraphicOffset + travelType.AsStationaryImageIndex());
            sprite.X = Global.Map2DViewX + (position.X - (int)ScrollX) * TILE_WIDTH + offset.X;
            sprite.Y = Global.Map2DViewY + (position.Y - (int)ScrollY) * TILE_HEIGHT + offset.Y;
            sprite.Visible = true;
            mapTransports.Add(new Transport { Index = index, Position = position, Sprite = sprite, Offset = offset });
        }

        internal void UpdateTile(uint x, uint y)
        {
            if (x < ScrollX || y < ScrollY || x >= ScrollX + NUM_VISIBLE_TILES_X || y >= ScrollY + NUM_VISIBLE_TILES_Y)
                return; // not visible

            int spriteIndex = (int)(x - ScrollX + (y - ScrollY) * NUM_VISIBLE_TILES_X);
            var tile = this[x, y];

            if (tile.BackTileIndex == 0)
            {
                backgroundTileSprites[spriteIndex].Visible = false;
            }
            else
            {
                var backTile = tileset.Tiles[(int)tile.BackTileIndex - 1];
                var backGraphicIndex = backTile.GraphicIndex;
                backgroundTileSprites[spriteIndex].TextureAtlasOffset = textureAtlas.GetOffset(backGraphicIndex - 1);
                backgroundTileSprites[spriteIndex].NumFrames = (uint)backTile.NumAnimationFrames;
                backgroundTileSprites[spriteIndex].CurrentFrame = 0;
                backgroundTileSprites[spriteIndex].Alternate = backTile.Flags.HasFlag(Tileset.TileFlags.WaveAnimation);
                backgroundTileSprites[spriteIndex].Visible = true;
            }

            if (tile.FrontTileIndex == 0)
            {
                foregroundTileSprites[spriteIndex].Visible = false;
            }
            else
            {
                var frontTile = tileset.Tiles[(int)tile.FrontTileIndex - 1];
                var frontGraphicIndex = frontTile.GraphicIndex;
                foregroundTileSprites[spriteIndex].TextureAtlasOffset = textureAtlas.GetOffset(frontGraphicIndex - 1);
                foregroundTileSprites[spriteIndex].NumFrames = (uint)frontTile.NumAnimationFrames;
                foregroundTileSprites[spriteIndex].CurrentFrame = 0;
                foregroundTileSprites[spriteIndex].Alternate = frontTile.Flags.HasFlag(Tileset.TileFlags.WaveAnimation);
                foregroundTileSprites[spriteIndex].Visible = true;
                foregroundTileSprites[spriteIndex].BaseLineOffset = frontTile.BringToFront ? TILE_HEIGHT + 2 : frontTile.Background ? -1 : 0;
            }
        }

        void UpdateTiles()
        {
            var backLayer = renderView.GetLayer((Layer)((uint)Layer.MapBackground1 + tileset.Index - 1));
            var frontLayer = renderView.GetLayer((Layer)((uint)Layer.MapForeground1 + tileset.Index - 1));
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate((Layer)((uint)Layer.MapBackground1 + tileset.Index - 1));
            int index = 0;
            bool mapChange = Map != lastUpdateMap;
            if (mapChange)
                lastUpdateScroll = null;
            lastUpdateMap = Map;
            var randomAnimationFramesBgBackup = mapChange ? null : new RandomAnimationInfo[NUM_TILES];
            var randomAnimationFramesFgBackup = mapChange ? null : new RandomAnimationInfo[NUM_TILES];
            int scrolledX = lastUpdateScroll == null ? 0 : (int)ScrollX - lastUpdateScroll.X;
            int scrolledY = lastUpdateScroll == null ? 0 : (int)ScrollY - lastUpdateScroll.Y;
            lastUpdateScroll = new Position((int)ScrollX, (int)ScrollY);

            if (!mapChange)
            {
                Array.Copy(randomAnimationFramesBackground, randomAnimationFramesBgBackup, randomAnimationFramesBgBackup.Length);
                Array.Copy(randomAnimationFramesForeground, randomAnimationFramesFgBackup, randomAnimationFramesFgBackup.Length);
            }

            for (uint row = 0; row < NUM_VISIBLE_TILES_Y; ++row)
            {
                for (uint column = 0; column < NUM_VISIBLE_TILES_X; ++column)
                {
                    var tile = this[ScrollX + column, ScrollY + row];

                    backgroundTileSprites[index].Layer = backLayer;
                    backgroundTileSprites[index].TextureAtlasWidth = textureAtlas.Texture.Width;
                    backgroundTileSprites[index].PaletteIndex = (byte)(Map.PaletteIndex - 1);
                    foregroundTileSprites[index].Layer = frontLayer;
                    foregroundTileSprites[index].TextureAtlasWidth = textureAtlas.Texture.Width;
                    foregroundTileSprites[index].PaletteIndex = (byte)(Map.PaletteIndex - 1);
                    randomAnimationFramesBackground[index] = null;
                    randomAnimationFramesForeground[index] = null;

                    if (tile.BackTileIndex == 0)
                    {
                        backgroundTileSprites[index].Visible = false;
                    }
                    else
                    {
                        var backTile = tileset.Tiles[(int)tile.BackTileIndex - 1];
                        var backGraphicIndex = backTile.GraphicIndex;
                        backgroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(backGraphicIndex - 1);
                        backgroundTileSprites[index].NumFrames = (uint)backTile.NumAnimationFrames;
                        backgroundTileSprites[index].CurrentFrame = 0;
                        backgroundTileSprites[index].Alternate = backTile.Flags.HasFlag(Tileset.TileFlags.WaveAnimation);
                        backgroundTileSprites[index].Visible = true;
                        backgroundTileSprites[index].BaseLineOffset = 0;

                        if (backTile.Flags.HasFlag(Tileset.TileFlags.RandomAnimationStart))
                        {
                            if (mapChange || column + scrolledX < 0 || column + scrolledX >= NUM_VISIBLE_TILES_X
                                || row + scrolledY < 0 || row + scrolledY >= NUM_VISIBLE_TILES_Y)
                                randomAnimationFramesBackground[index] = CreateRandomStart(backTile);
                            else
                                randomAnimationFramesBackground[index] = randomAnimationFramesBgBackup[column + scrolledX + (row + scrolledY) * NUM_VISIBLE_TILES_X];
                        }
                    }

                    if (tile.FrontTileIndex == 0)
                    {
                        foregroundTileSprites[index].Visible = false;
                    }
                    else
                    {
                        var frontTile = tileset.Tiles[(int)tile.FrontTileIndex - 1];
                        var frontGraphicIndex = frontTile.GraphicIndex;
                        foregroundTileSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(frontGraphicIndex - 1);
                        foregroundTileSprites[index].NumFrames = (uint)frontTile.NumAnimationFrames;
                        foregroundTileSprites[index].CurrentFrame = 0;
                        foregroundTileSprites[index].Visible = true;
                        foregroundTileSprites[index].Alternate = frontTile.Flags.HasFlag(Tileset.TileFlags.WaveAnimation);
                        foregroundTileSprites[index].BaseLineOffset = frontTile.BringToFront ? (Map.UseTravelTypes ? TILE_HEIGHT : 2 * TILE_HEIGHT) + 2 : frontTile.Background ? -1 : 0;

                        if (frontTile.Flags.HasFlag(Tileset.TileFlags.RandomAnimationStart))
                        {
                            if (mapChange || column + scrolledX < 0 || column + scrolledX >= NUM_VISIBLE_TILES_X
                                || row + scrolledY < 0 || row + scrolledY >= NUM_VISIBLE_TILES_Y)
                                randomAnimationFramesForeground[index] = CreateRandomStart(frontTile);
                            else
                                randomAnimationFramesForeground[index] = randomAnimationFramesFgBackup[column + scrolledX + (row + scrolledY) * NUM_VISIBLE_TILES_X];
                        }
                    }

                    ++index;
                }
            }

            Update(0, game.GameTime, false, null);
        }

        public bool IsMapVisible(uint index)
        {
            if (Map == null) // current map might be a 3D map, then Map is null here
                return false;

            if (Map.Index == index)
                return true;

            if (!Map.IsWorldMap)
                return false;

            return
                index == Map.RightMapIndex ||
                index == Map.DownMapIndex ||
                index == Map.DownRightMapIndex;
        }

        public bool IsMapVisible(uint index, ref uint localMapX, ref uint localMapY)
        {
            if (Map == null) // current map might be a 3D map, then Map is null here
                return false;

            if (Map.Index == index)
                return true;

            if (!Map.IsWorldMap)
                return false;

            if (index == Map.RightMapIndex ||
                index == Map.DownRightMapIndex)
                localMapX += (uint)Map.Width;

            if (index == Map.DownMapIndex ||
                index == Map.DownRightMapIndex)
                localMapY += (uint)Map.Height;

            return
                index == Map.RightMapIndex ||
                index == Map.DownMapIndex ||
                index == Map.DownRightMapIndex;
        }

        public void LimitScrollOffset(ref uint x, ref uint y, Map map = null)
        {
            map ??= Map;

            if (map.IsWorldMap)
            {
                x = (uint)Util.Limit(0, (long)x - NUM_VISIBLE_TILES_X / 2, map.Width - 1);
                y = (uint)Util.Limit(0, (long)y - NUM_VISIBLE_TILES_Y / 2, map.Height - 1);
            }
            else
            {
                x = (uint)Util.Limit(0, (long)x - NUM_VISIBLE_TILES_X / 2, map.Width - NUM_VISIBLE_TILES_X);
                y = (uint)Util.Limit(0, (long)y - NUM_VISIBLE_TILES_Y / 2, map.Height - NUM_VISIBLE_TILES_Y);
            }
        }

        public void LimitScrollOffset(Position position, out Map newMap)
        {
            newMap = Map;

            if (Map.IsWorldMap)
            {
                if (position.X < 0)
                {
                    newMap = game.MapManager.GetMap(Map.LeftMapIndex.Value);
                    position.X += Map.Width;
                }
                if (position.Y < 0)
                {
                    newMap = game.MapManager.GetMap(newMap.UpMapIndex.Value);
                    position.Y += newMap.Height;
                }

                position.X = Util.Limit(0, position.X - NUM_VISIBLE_TILES_X / 2, Map.Width - 1);
                position.Y = Util.Limit(0, position.Y - NUM_VISIBLE_TILES_Y / 2, Map.Height - 1);
            }
            else
            {
                position.X = Util.Limit(0, position.X - NUM_VISIBLE_TILES_X / 2, Map.Width - NUM_VISIBLE_TILES_X);
                position.Y = Util.Limit(0, position.Y - NUM_VISIBLE_TILES_Y / 2, Map.Height - NUM_VISIBLE_TILES_Y);
            }
        }

        public void SetMap(Map map, uint initialScrollX = 0, uint initialScrollY = 0)
        {
            if (Map == map)
                return;

            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to load a 3D map into a 2D render map.");

            var lastMap = Map;
            map.Reset();
            Map = map;
            tileset = mapManager.GetTilesetForMap(map);
            ticksPerFrame = map.TicksPerAnimationFrame;

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

            if (map.UseTravelTypes)
                RepositionTransports(lastMap);

            ClearCharacters();

            ScrollTo(initialScrollX, initialScrollY, true); // also updates tiles etc

            AddCharacters(map);

            if (map.IsWorldMap)
                InvokeMapChangedHandler(lastMap, map, adjacentMaps[0], adjacentMaps[1], adjacentMaps[2]);
            else
                InvokeMapChangedHandler(lastMap, map);
        }

        RandomAnimationInfo CreateRandomStart(int frameCount, bool poison)
        {
            return new RandomAnimationInfo
            {
                CurrentFrame = -game.RandomInt(0, frameCount * 4),
                FrameCount = frameCount,
                Poison = poison
            };
        }

        RandomAnimationInfo CreateRandomStart(Tileset.Tile tile)
        {
            return CreateRandomStart(tile.NumAnimationFrames, tile.Flags.HasFlag(Tileset.TileFlags.AutoPoison));
        }

        internal void InvokeMapChange()
        {
            if (Map.IsWorldMap)
            {
                InvokeMapChangedHandler(null, Map,
                    mapManager.GetMap(Map.RightMapIndex.Value),
                    mapManager.GetMap(Map.DownMapIndex.Value),
                    mapManager.GetMap(Map.DownRightMapIndex.Value));
            }
            else
                InvokeMapChangedHandler(null, Map);
        }

        internal void AddCharacters(Map map)
        {
            for (uint characterIndex = 0; characterIndex < map.CharacterReferences.Length; ++characterIndex)
            {
                var characterReference = map.CharacterReferences[characterIndex];

                if (characterReference == null)
                    break;

                var mapCharacter = MapCharacter2D.Create(game, renderView, mapManager, this, characterIndex, characterReference);
                mapCharacter.Active = !game.CurrentSavegame.GetCharacterBit(map.Index, characterIndex);
                mapCharacters.Add(characterIndex, mapCharacter);
            }
        }

        public void CheckIfMonsterSeesPlayer(MapCharacter2D monster, bool visible)
        {
            if (Map.IsWorldMap)
            {
                game.MonsterSeesPlayer = false;
                return;
            }

            monster.CheckedIfSeesPlayer = true;
            monster.SeesPlayer = false;

            if (!game.MonsterSeesPlayer && visible)
            {
                monster.SeesPlayer = MonsterSeesPlayer(monster.Position, null, null);

                if (monster.SeesPlayer)
                    game.MonsterSeesPlayer = true;
            }
            else if (game.MonsterSeesPlayer)
            {
                CheckIfMonstersSeePlayer();
            }
        }

        public void CheckIfMonstersSeePlayer(uint? playerX = null, uint? playerY = null)
        {
            game.MonsterSeesPlayer = false;

            if (!Map.IsWorldMap)
            {
                bool check = true;

                foreach (var monster in mapCharacters.Where(c => c.Value.Active && c.Value.IsMonster))
                {
                    if (check)
                    {
                        monster.Value.CheckedIfSeesPlayer = true;
                        monster.Value.SeesPlayer = false;

                        if (MonsterSeesPlayer(monster.Value.Position, playerX, playerY))
                        {
                            monster.Value.SeesPlayer = true;
                            game.MonsterSeesPlayer = true;
                            check = false;
                        }
                    }
                    else
                    {
                        monster.Value.CheckedIfSeesPlayer = false;
                    }
                }
            }
        }

        public bool MonsterSeesPlayer(Position monsterPosition, uint? playerX = null, uint? playerY = null)
        {
            var position = new Position((int)(playerX ?? (uint)game.RenderPlayer.Position.X), (int)(playerY ?? (uint)game.RenderPlayer.Position.Y));
            return !Geometry.Raycast2D.TestRay(Map, position.X, position.Y, monsterPosition.X, monsterPosition.Y, tile => tile.BlocksSight(tileset));
        }

        public void UpdateCharacterVisibility(uint characterIndex)
        {
            if (Map.CharacterReferences[characterIndex] == null)
                throw new AmbermoonException(ExceptionScope.Application, "Null map character");

            mapCharacters[characterIndex].Active = !game.CurrentSavegame.GetCharacterBit(Map.Index, characterIndex);

            CheckIfMonstersSeePlayer();
        }

        void InvokeMapChangedHandler(Map lastMap, params Map[] maps)
        {
            MapChanged?.Invoke(lastMap, maps);
        }

        public bool Scroll(int x, int y)
        {
            int newScrollX = (int)ScrollX + x;
            int newScrollY = (int)ScrollY + y;

            if (worldMap)
            {
                if (newScrollX < 0 || newScrollY < 0 || newScrollX >= Map.Width || newScrollY >= Map.Height)
                {
                    Map newMap;

                    if (newScrollX < 0)
                        newMap = mapManager.GetMap(Map.LeftMapIndex.Value);
                    else if (newScrollX >= Map.Width)
                        newMap = mapManager.GetMap(Map.RightMapIndex.Value);
                    else
                        newMap = Map;

                    if (newScrollY < 0)
                        newMap = mapManager.GetMap(newMap.UpMapIndex.Value);
                    else if (newScrollY >= Map.Height)
                        newMap = mapManager.GetMap(newMap.DownMapIndex.Value);

                    uint newMapScrollX = newScrollX < 0 ? (uint)(Map.Width + newScrollX) : (uint)(newScrollX % Map.Width);
                    uint newMapScrollY = newScrollY < 0 ? (uint)(Map.Height + newScrollY) : (uint)(newScrollY % Map.Height);

                    SetMap(newMap, newMapScrollX, newMapScrollY);
                    game.MonsterSeesPlayer = false;

                    return true;
                }
            }
            else
            {
                if (newScrollX < 0 || newScrollY < 0 || newScrollX > Map.Width - NUM_VISIBLE_TILES_X || newScrollY > Map.Height - NUM_VISIBLE_TILES_Y)
                    return false;
            }

            ScrollTo((uint)newScrollX, (uint)newScrollY, false);

            return true;
        }

        public void ScrollToPlayer(uint x, uint y) => ScrollToPlayer(new Position((int)x, (int)y));

        public void ScrollToPlayer(Position playerPosition)
        {
            playerPosition ??= game.PartyPosition;

            uint x = (uint)playerPosition.X;
            uint y = (uint)playerPosition.Y;
            LimitScrollOffset(ref x, ref y);
            ScrollTo(x, y);
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
                if (ScrollX > Map.Width - NUM_VISIBLE_TILES_X)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll x position is outside the map bounds.");
                if (ScrollY > Map.Height - NUM_VISIBLE_TILES_Y)
                    throw new AmbermoonException(ExceptionScope.Render, "Map scroll y position is outside the map bounds.");
            }

            UpdateTiles();
            UpdateTransports();
        }

        public uint GetCombatBackgroundIndex(Map map, uint x, uint y)
        {
            var tile = map.Tiles[x, y];
            var tilesetTile = tile.BackTileIndex == 0
                ? tileset.Tiles[tile.FrontTileIndex - 1]
                : tileset.Tiles[tile.BackTileIndex - 1];
            return tilesetTile.CombatBackgroundIndex;
        }

        public void StopMonstersForOneTimeSlot()
        {
            foreach (var monster in mapCharacters.Where(c => c.Value?.Active == true && c.Value?.IsMonster == true))
                monster.Value.StopMonsterForOneTimeSlot();
        }
    }
}
