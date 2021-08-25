/*
 * Player3D.cs - 3D player implementation
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

namespace Ambermoon.Render
{
    using Geometry = Geometry.Geometry;

    class Player3D : IRenderPlayer
    {
        readonly Game game = null;
        readonly IMapManager mapManager;
        readonly RenderMap3D map;
        readonly Player player;
        float angle = 0.0f;
        // Original uses 120/512 but it feels bad.
        public const float CollisionRadius = 72.0f * Global.DistancePerBlock / RenderMap3D.BlockSize;
        public const float TriggerEventRadius = 88.0f * Global.DistancePerBlock / RenderMap3D.BlockSize; // TODO: use this

        Position lastPosition;
        public Position Position { get; private set; }
        public ICamera3D Camera { get; }
        public float Angle
        {
            get => angle;
            private set
            {
                angle = value;
                while (angle <= -360.0f)
                    angle += 360.0f;
                while (angle >= 360.0f)
                    angle -= 360.0f;
            }
        }

        public Player3D(Game game, Player player, IMapManager mapManager, ICamera3D camera, RenderMap3D map, int x, int y)
        {
            this.game = game;
            this.player = player;
            this.mapManager = mapManager;
            this.map = map;
            Camera = camera;
        }

        void ResetCameraPosition()
        {
            lastPosition = new Position(Position);
            Geometry.BlockToCameraPosition(map.Map, Position, out float x, out float z);
            Camera.SetPosition(-x, z);
        }

        public void SetY(float y)
        {
            Camera.SetPosition(-Camera.X, Camera.Z, y);
        }

        public void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            if (newDirection == CharacterDirection.Random)
                newDirection = (CharacterDirection)game.RandomInt(0, 3);

            lastPosition = new Position(Position);
            Position = new Position((int)x, (int)y);

            if (this.map.Map != map)
            {
                if (newDirection == null)
                    throw new AmbermoonException(ExceptionScope.Application, "Direction must be given when changing maps.");

                if (map.Type == MapType.Map2D)
                {
                    game.Start2D(map, x, y, newDirection.Value, false);
                }
                else
                {
                    this.map.SetMap(map, x, y, newDirection.Value, game.CurrentPartyMember?.Race ?? Race.Human);
                    var oldMapIndex = map.Index;
                    SetPosition((int)x, (int)y, ticks, false);
                    player.Position.X = Position.X;
                    player.Position.Y = Position.Y;
                    game.PlayerMoved(oldMapIndex != game.Map.Index);

                    if (newDirection != null)
                        Angle = (float)newDirection.Value * 90.0f;
                }
            }
            else
            {
                var oldMapIndex = map.Index;
                SetPosition((int)x, (int)y, ticks, false);
                game.PlayerMoved(oldMapIndex != game.Map.Index);

                if (newDirection != null)
                {
                    Angle = (float)newDirection.Value * 90.0f;
                    Camera.TurnTowards(Angle);
                }
            }

            player.Position.X = Position.X;
            player.Position.Y = Position.Y;
        }

        public void SetPosition(int x, int y, uint ticks, bool triggerEvents)
        {
            Position = new Position(x, y);
            ResetCameraPosition();

            if (triggerEvents)
            {
                map.Map.TriggerEvents(game, EventTrigger.Move, (uint)Position.X, (uint)Position.Y, game.CurrentSavegame);
            }
        }

        bool TestCollision(float x, float z, float lastMapX, float lastMapY)
        {
            Geometry.CameraToWorldPosition(map.Map, x, z, out float mapX, out float mapY);

            // This contains all collision bodies in a 3x3 area around the current position.
            var collisionDetectionInfo = map.GetCollisionDetectionInfoForPlayer(Position);

            return collisionDetectionInfo.TestCollision(lastMapX, lastMapY, mapX, mapY, CollisionRadius, true);
        }

        public List<Position> GetTouchedPositions(float radius)
            => Geometry.CameraToTouchedBlockPositions(map.Map, Camera.X, Camera.Z, radius);

        delegate void PositionProvider(float distance, out float newX, out float newY, bool noX, bool noZ);

        void Move(float distance, uint ticks, PositionProvider positionProvider, Action<float, bool, bool> mover, bool turning = false)
        {
            bool TriggerEvents(List<Position> touchedPositions, float oldX, float oldY, float newX, float newY)
            {
                bool anyEventTriggered = false;
                bool anyEventTouched = false;
                Position currentPosition = touchedPositions[0];

                foreach (var touchedPosition in touchedPositions)
                {
                    bool considerPosition = touchedPosition == currentPosition;
                    bool considerNonExitPosition = considerPosition;

                    if (!considerPosition)
                    {
                        Geometry.BlockToCameraPosition(map.Map, touchedPosition, out float touchX, out float touchY);
                        bool sameXDirection = Math.Sign(newX - oldX) == Math.Sign(touchX - oldX);
                        bool sameYDirection = Math.Sign(newY - oldY) == Math.Sign(touchY - oldY);

                        considerPosition = (sameXDirection && (sameYDirection || Math.Abs(touchY - oldY) < 0.5f * Global.DistancePerBlock)) ||
                            (sameYDirection && Math.Abs(touchX - oldX) < 0.5f * Global.DistancePerBlock);
                    }

                    if (!considerNonExitPosition && considerPosition)
                    {
                        Geometry.BlockToCameraPosition(map.Map, touchedPosition, out float touchX, out float touchY);
                        considerNonExitPosition = Math.Abs(touchY - newY) < 0.275f * Global.DistancePerBlock &&
                            Math.Abs(touchX - newX) < 0.275f * Global.DistancePerBlock;
                    }

                    // Tile change events that put something on the block where the player
                    // stands should never be considered. Otherwise we would get stuck.
                    if (considerPosition)
                    {
                        var block = map.Map.Blocks[(uint)touchedPosition.X, (uint)touchedPosition.Y];

                        if (block.MapEventId != 0 && game.CurrentSavegame.IsEventActive(map.Map.Index, block.MapEventId - 1))
                        {
                            var @event = map.Map.EventList[(int)block.MapEventId - 1];

                            do
                            {
                                if (@event is ChangeTileEvent changeTileEvent)
                                {
                                    if (changeTileEvent.X == Position.X + 1 && changeTileEvent.Y == Position.Y + 1 &&
                                        changeTileEvent.WallIndex != 0)
                                    {
                                        var labdata = mapManager.GetLabdataForMap(map.Map);
                                        var wall = labdata.Walls[(int)changeTileEvent.WallIndex - 1];

                                        if (wall.Flags.HasFlag(Tileset.TileFlags.BlockAllMovement) ||
                                            !wall.Flags.HasFlag(Tileset.TileFlags.AllowMovementWalk))
                                        {
                                            considerPosition = false;
                                            break;
                                        }
                                    }
                                }

                                @event = @event.Next;
                            } while (@event != null);
                        }
                    }

                    if (considerPosition)
                    {
                        bool Filter(Event @event)
                        {
                            if (considerNonExitPosition)
                                return true;

                            if (@event.Type == EventType.Teleport ||
                                @event.Type == EventType.EnterPlace ||
                                @event.Type == EventType.Chest ||
                                @event.Type == EventType.Door ||
                                @event.Type == EventType.Riddlemouth)
                                return true;

                            if (@event.Next == null)
                                return false;

                            return Filter(@event.Next);
                        }

                        bool hasMapEvent = false;
                        var oldMapIndex = map.Map.Index;
                        var oldMapPosition = new Position(Position);
                        anyEventTriggered = anyEventTriggered || map.Map.TriggerEvents(game, EventTrigger.Move,
                            (uint)touchedPosition.X, (uint)touchedPosition.Y, game.CurrentSavegame, out hasMapEvent,
                            Filter);

                        if (!anyEventTouched && hasMapEvent)
                            anyEventTouched = true;

                        if (oldMapIndex != game.Map.Index)
                        {
                            game.PlayerMoved(true);
                            break; // map changed
                        }
                        else if (anyEventTriggered && oldMapPosition != Position)
                        {
                            game.PlayerMoved(false, oldMapPosition);
                            break; // teleported
                        }
                    }
                }

                if (!anyEventTouched)
                    map.Map.ClearLastEvent();

                return anyEventTriggered;
            }

            void Move(bool noX, bool noZ)
            {
                float oldX = Camera.X;
                float oldY = Camera.Z;
                mover(distance, noX, noZ);

                var touchedPositions = Geometry.CameraToTouchedBlockPositions(map.Map, Camera.X, Camera.Z, 0.75f * Global.DistancePerBlock);
                Position = touchedPositions[0];
                bool moved = false;

                if (Position != lastPosition)
                {
                    player.Position.X = Position.X;
                    player.Position.Y = Position.Y;
                    lastPosition = new Position(Position);
                    game.GameTime.MoveTick(map.Map, TravelType.Walk);
                    moved = true;
                }

                if (!TriggerEvents(touchedPositions, oldX, oldY, Camera.X, Camera.Z) && moved)
                    game.PlayerMoved(false, lastPosition);
            }

            bool TestMoveStop(float newX, float newY)
            {
                var touchedPositions = Geometry.CameraToTouchedBlockPositions(map.Map, Camera.X, Camera.Z, 0.75f * Global.DistancePerBlock);

                foreach (var touchedPosition in touchedPositions)
                {
                    if (map.Map.StopMovingTowards(game.CurrentSavegame, touchedPosition.X, touchedPosition.Y))
                    {
                        if (TriggerEvents(touchedPositions, Camera.X, Camera.Z, newX, newY))
                            return true;
                    }
                }

                return false;
            }

            Geometry.CameraToWorldPosition(map.Map, Camera.X, Camera.Z, out float cameraMapX, out float cameraMapY);
            float collisionTestDistance = distance + (turning ? 0.334f : 0.2f) * Global.DistancePerBlock;
            positionProvider(collisionTestDistance, out float newX, out float newY, false, false);

            if (TestCollision(newX, newY, cameraMapX, cameraMapY))
            {
                if (!TestMoveStop(newX, newY))
                {
                    // If collision is detected try to move only in x direction
                    positionProvider(collisionTestDistance, out newX, out newY, false, true);

                    if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in x direction
                    {
                        if (!TestMoveStop(newX, newY))
                            Move(false, true);
                        return;
                    }

                    // If collision is detected in x direction too, try to move only in z direction
                    positionProvider(collisionTestDistance, out newX, out newY, true, false);

                    if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in z direction
                    {
                        if (!TestMoveStop(newX, newY))
                            Move(true, false);
                        return;
                    }

                    // If we are here, we can't move at all
                    game.DisplayOuch();
                }
            }
            else
            {
                // We can move freely
                if (!TestMoveStop(newX, newY))
                    Move(false, false);
            }
        }

        public void MoveForward(float distance, uint ticks, bool turning = false)
        {
            Move(distance, ticks, Camera.GetForwardPosition, Camera.MoveForward, turning);
        }

        public void MoveBackward(float distance, uint ticks, bool turning = false)
        {
            Move(distance, ticks, Camera.GetBackwardPosition, Camera.MoveBackward, turning);
        }

        public void MoveLeft(float distance, uint ticks, bool turning = false)
        {
            Move(distance, ticks, Camera.GetLeftPosition, Camera.MoveLeft, turning);
        }

        public void MoveRight(float distance, uint ticks, bool turning = false)
        {
            Move(distance, ticks, Camera.GetRightPosition, Camera.MoveRight, turning);
        }

        public void TurnLeft(float angle) // in degrees
        {
            Angle -= angle;
            Camera.TurnLeft(angle);
        }

        public void TurnRight(float angle) // in degrees
        {
            Angle += angle;
            Camera.TurnRight(angle);
        }

        public void TurnTowards(float angle) // turn to attacking monster or stand on a spinner (in degrees)
        {
            Angle = angle;
            Camera.TurnTowards(angle);
        }

        public void TurnTowards(FloatPosition position)
        {
            Geometry.CameraToMapPosition(map.Map, Camera.X, Camera.Z, out float mapX, out float mapY);
            var playerPosition = new FloatPosition(mapX - 0.5f * Global.DistancePerBlock, mapY - 0.5f * Global.DistancePerBlock);
            double diffX = position.X - playerPosition.X;
            double diffY = position.Y - playerPosition.Y;
            double max = Math.Max(Math.Abs(diffX), Math.Abs(diffY));

            if (max < 0.0001)
                return;

            diffX /= max;
            diffY /= max;
            double length = Math.Sqrt(diffX * diffX + diffY * diffY);

            if (Math.Abs(length) > 0.0001)
            {
                var x = diffX / length;
                var y = diffY / length;
                var angle = Math.Atan2(y, x);
                TurnTowards(90.0f + (float)(180.0 * angle / Math.PI));
            }
        }

        public void LevitateUp(float distance) // used for climbing up ladders/ropes or use levitation spell (distance is in the range of 0 to 1 where 1 is full room height)
        {
            Camera.LevitateUp(distance);
        }

        public void LevitateDown(float distance) // used for climbing down ladders/ropes (distance is in the range of 0 to 1 where 1 is full room height)
        {
            Camera.LevitateDown(distance);
        }

        public CharacterDirection Direction
        {
            get
            {
                float directionAngle = Angle;

                if (directionAngle > 315.0f)
                    directionAngle -= 360.0f;
                if (directionAngle < -45.0f)
                    directionAngle += 360.0f;

                if (directionAngle < 45.0f)
                    return CharacterDirection.Up;
                if (directionAngle < 135.0f)
                    return CharacterDirection.Right;
                if (directionAngle < 225.0f)
                    return CharacterDirection.Down;
                return CharacterDirection.Left;
            }
        }

        public Direction PreciseDirection
        {
            get
            {
                float directionAngle = Angle;

                if (directionAngle > 337.5f)
                    directionAngle -= 360.0f;
                if (directionAngle < -22.5f)
                    directionAngle += 360.0f;

                if (directionAngle < 22.5f)
                    return Ambermoon.Direction.Up;
                if (directionAngle < 67.5f)
                    return Ambermoon.Direction.UpRight;
                if (directionAngle < 112.5f)
                    return Ambermoon.Direction.Right;
                if (directionAngle < 157.5f)
                    return Ambermoon.Direction.DownRight;
                if (directionAngle < 202.5f)
                    return Ambermoon.Direction.Down;
                if (directionAngle < 247.5f)
                    return Ambermoon.Direction.DownLeft;
                if (directionAngle < 292.5f)
                    return Ambermoon.Direction.Left;
                return Ambermoon.Direction.UpLeft;
            }
        }
    }
}
