/*
 * Player2D.cs - 2D player implementation
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

namespace Ambermoon.Render
{
    internal class Player2D : Character2D, IRenderPlayer
    {
        readonly Game game;
        readonly Player player;
        readonly IMapManager mapManager;

        public Player2D(Game game, IRenderLayer layer, Player player, RenderMap2D map,
            ISpriteFactory spriteFactory, Position startPosition,
            IMapManager mapManager)
            : base(game, layer, TextureAtlasManager.Instance.GetOrCreate(Layer.Characters),
                  spriteFactory, game.GetPlayerAnimationInfo, map, startPosition,
                  game.GetPlayerPaletteIndex, game.GetPlayerDrawOffset)
        {
            this.game = game;
            this.player = player;
            this.mapManager = mapManager;
        }

        public bool Move(int x, int y, uint ticks, TravelType travelType, out bool eventTriggered,
            bool displayOuchIfBlocked = true, CharacterDirection? prevDirection = null,
            bool updateDirectionIfNotMoving = true) // x,y in tiles
        {
            eventTriggered = false;

            if (player.MovementAbility == PlayerMovementAbility.NoMovement)
                return false;

            bool canMove = true;
            var map = Map.Map;
            int newX = Position.X + x;
            int newY = Position.Y + y;
            Map.Tile tile = null;

            if (!map.IsWorldMap)
            {
                // Don't leave the map.
                if (newX < 0 || newY < 0 || newX >= map.Width || newY >= map.Height)
                    canMove = false;
                else
                    tile = Map[(uint)newX, (uint)newY];
            }
            else
            {
                while (newX < 0)
                    newX += map.Width;
                while (newY < 0)
                    newY += map.Height;

                tile = Map[(uint)newX, (uint)newY];
            }

            if (canMove)
            {
                var tileset = mapManager.GetTilesetForMap(map);
                canMove = tile.AllowMovement(tileset, travelType);

                if (!canMove && travelType == TravelType.Swim && tile.AllowMovement(tileset, TravelType.Walk))
                    canMove = true; // go on land

                if (!travelType.IgnoreEvents())
                {
                    // check if there is a place, teleport, riddlemouth, chest
                    // or door event at the new position
                    var mapEventId = Map[(uint)newX, (uint)newY]?.MapEventId;

                    if (mapEventId > 0 && game.CurrentSavegame.IsEventActive(map.Index, mapEventId.Value - 1))
                    {
                        var trigger = EventTrigger.Move;
                        bool lastEventStatus = false;

                        bool HasSpecialEvent(Event ev, out EventType? eventType)
                        {
                            eventType = null;

                            if (ev.Type == EventType.EnterPlace ||
                                (ev is TeleportEvent teleportEvent && teleportEvent.Transition != TeleportEvent.TransitionType.WindGate) ||
                                ev.Type == EventType.Riddlemouth ||
                                (ev.Type == EventType.Chest && Map.Map.IsWorldMap) ||
                                (ev is DoorEvent doorEvent && game.CurrentSavegame.IsDoorLocked(doorEvent.DoorIndex)))
                            {
                                eventType = ev.Type;
                                return true;
                            }

                            if (ev.Next == null)
                                return false;

                            if (ev is ConditionEvent conditionEvent)
                            {
                                ev = conditionEvent.ExecuteEvent(map, game, ref trigger, (uint)Position.X,
                                    (uint)Position.Y, ref lastEventStatus, out bool aborted, out _);

                                if (aborted || ev == null)
                                    return false;
                            }
                            else
                            {
                                ev = ev.Next;
                            }

                            return HasSpecialEvent(ev, out eventType);
                        }

                        var mapAtNewPosition = Map.GetMapFromTile((uint)newX, (uint)newY);

                        if (HasSpecialEvent(mapAtNewPosition.EventList[(int)mapEventId.Value - 1], out var type))
                        {
                            if ((type != EventType.Teleport || !travelType.BlockedByTeleport()) &&
                                EventExtensions.TriggerEventChain(mapAtNewPosition, game, EventTrigger.Move, (uint)x, (uint)y,
                                    mapAtNewPosition.EventList[(int)mapEventId.Value - 1]))
                            {
                                eventTriggered = true;
                                return false;
                            }
                            else
                                canMove = false;
                        }
                    }
                }

                if (canMove && tile.Type == Data.Map.TileType.Water && travelType.BlockedByWater())
                    canMove = false;
            }

            if (canMove)
            {
                var oldMap = map;
                int scrollX = 0;
                int scrollY = 0;
                prevDirection ??= Direction;
                var newDirection = CharacterDirection.Down;
                var lastPlayerPosition = new Position(player.Position);

                if (x > 0 && (map.IsWorldMap || (newX >= 6 && Map.ScrollX < Map.Map.Width - RenderMap2D.NUM_VISIBLE_TILES_X)))
                    scrollX = 1;
                else if (x < 0 && (map.IsWorldMap || (newX <= map.Width - 7 && Map.ScrollX > 0)))
                    scrollX = -1;

                if (y > 0 && (map.IsWorldMap || (newY >= 5 && Map.ScrollY < Map.Map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y)))
                    scrollY = 1;
                else if (y < 0 && (map.IsWorldMap || (newY <= map.Height - 6 && Map.ScrollY > 0)))
                    scrollY = -1;

                if (y > 0)
                    newDirection = CharacterDirection.Down;
                else if (y < 0)
                    newDirection = CharacterDirection.Up;
                else if (x > 0)
                    newDirection = CharacterDirection.Right;
                else if (x < 0)
                    newDirection = CharacterDirection.Left;

                game.CurrentSavegame.CharacterDirection = player.Direction = newDirection;

                Map.Scroll(scrollX, scrollY);

                if (oldMap == Map.Map)
                {
                    bool frameReset = NumFrames == 1 || newDirection != prevDirection;
                    var prevState = CurrentState;

                    MoveTo(oldMap, (uint)newX, (uint)newY, ticks, frameReset, null);

                    if (travelType == TravelType.Walk)
                    {
                        Map.TriggerEvents(this, EventTrigger.Move, (uint)newX,
                            (uint)newY, mapManager, ticks, game.CurrentSavegame);
                    }

                    if (oldMap == Map.Map) // might have changed by map change events
                    {
                        if (!frameReset && CurrentState == prevState)
                            SetCurrentFrame((CurrentFrame + 1) % NumFrames);

                        player.Position.X = Position.X;
                        player.Position.Y = Position.Y;

                        tile = Map[(uint)player.Position.X, (uint)player.Position.Y];
                        Visible = travelType != TravelType.Walk || tile.Type != Data.Map.TileType.Invisible;

                        game.PlayerMoved(false, lastPlayerPosition);
                    }
                }
                else
                {
                    // adjust player position on map transition
                    var position = Map.GetCenterPosition();

                    MoveTo(Map.Map, (uint)position.X, (uint)position.Y, ticks, false, player.Direction);

                    if (travelType == TravelType.Walk)
                    {
                        Map.TriggerEvents(this, EventTrigger.Move, (uint)position.X,
                            (uint)position.Y, mapManager, ticks, game.CurrentSavegame);
                    }

                    if (Map.Map.Type == MapType.Map2D)
                    {
                        player.Position.X = Position.X;
                        player.Position.Y = Position.Y;

                        // Note: For 3D maps the game/3D map will handle player position updating.

                        tile = Map[(uint)player.Position.X, (uint)player.Position.Y];
                        Visible = travelType != TravelType.Walk || tile.Type != Data.Map.TileType.Invisible;

                        game.PlayerMoved(true, null, true, oldMap);
                    }
                }
            }
            else
            {
                if (displayOuchIfBlocked)
                    game.DisplayOuch();

                if (updateDirectionIfNotMoving)
                {
                    // If not able to move, the direction should be adjusted
                    var newDirection = Direction;

                    if (y > 0)
                        newDirection = CharacterDirection.Down;
                    else if (y < 0)
                        newDirection = CharacterDirection.Up;
                    else if (x > 0)
                        newDirection = CharacterDirection.Right;
                    else if (x < 0)
                        newDirection = CharacterDirection.Left;

                    if (newDirection != Direction)
                    {
                        MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, newDirection);
                        player.Direction = newDirection;
                        game.CurrentSavegame.CharacterDirection = newDirection;
                        UpdateAppearance(game.CurrentTicks);
                    }
                }
            }

            return canMove;
        }

        public void UpdateAppearance(uint ticks)
        {
            MoveTo(Map.Map, (uint)Position.X, (uint)Position.Y, ticks, true, null);
        }

        public override void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (Map.Map != map)
                Visible = true; // reset visibility before changing map

            base.MoveTo(map, x, y, ticks, frameReset, newDirection);

            if (frameReset && map.Type == MapType.Map2D && !map.IsWorldMap)
                SetCurrentFrame(CurrentFrameIndex + 1); // Middle move frame = stand frame
        }

        public override void Update(uint ticks, ITime gameTime, bool allowInstantMovement = false,
            Position lastPlayerPosition = null)
        {
            // do not animate so don't call base.Update here
        }
    }
}
