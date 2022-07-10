/*
 * Character3D.cs - 3D character implementation
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

using System;

namespace Ambermoon.Geometry
{
    internal class Character3D
    {
        const uint TimePerMovement = 2000; // in ms
        const uint TicksPerMovement = TimePerMovement * Game.TicksPerSecond / 1000;
        const float BlockDivider = 1.0f / Global.DistancePerBlock;

        enum State
        {
            IdleOnTile,
            Idle,
            MovingToTile,
            MovingTowardsPlayer,
            BlockedToPlayer
        }

        readonly Game game;
        Position lastTilePosition;
        Position targetTilePosition;
        public FloatPosition RealPosition { get; private set; }
        Direction? direction = null;
        State currentState = State.IdleOnTile;
        uint lastMoveTicks = 0;
        uint movedTicks = 0;
        uint ticksPerMovement = TicksPerMovement;
        public uint NextMoveTimeSlot { get; private set; } = uint.MaxValue;
        public Position Position => RealPosition.Round(BlockDivider);
        public bool Moving => currentState == State.MovingToTile || currentState == State.MovingTowardsPlayer;

        public event Action RandomMovementRequested;
        public event Func<FloatPosition, bool> MoveRequested;

        public bool Paused
        {
            get;
            set;
        } = false;

        public Character3D(Game game)
        {
            this.game = game;
            ResetMovementTimer();
            lastMoveTicks = game.CurrentMapTicks;
        }

        public void Place(uint x, uint y, bool waitForManualStart)
        {
            targetTilePosition = null;
            lastTilePosition = new Position((int)x, (int)y);
            RealPosition = lastTilePosition * Global.DistancePerBlock;
            direction = null;
            currentState = State.IdleOnTile;
            lastMoveTicks = game.CurrentMapTicks;
            NextMoveTimeSlot = waitForManualStart ? uint.MaxValue : (game.GameTime.TimeSlot + 1) % 288;
        }

        public void MoveToTile(uint x, uint y, Position lastPosition = null)
        {
            if (lastPosition != null) // Fixed route
            {
                if (targetTilePosition == null || targetTilePosition.X != x || targetTilePosition.Y != y)
                {
                    // New position and last position is given (fixed route).
                    // In this case we ensure that the character is synced with
                    // its path and place him at the last position.
                    Place((uint)lastPosition.X, (uint)lastPosition.Y, false);
                    lastTilePosition = new Position(lastPosition);
                    InitMovement();
                    currentState = direction == null ? State.IdleOnTile : State.MovingToTile;
                }
                return;
            }

            void InitMovement()
            {
                targetTilePosition = new Position((int)x, (int)y);
                direction = lastTilePosition.GetDirectionTo(targetTilePosition);
                lastMoveTicks = game.CurrentMapTicks;
                movedTicks = 0;
                ticksPerMovement = lastTilePosition.GetMaxDistance(targetTilePosition) * TicksPerMovement;
            }

            if (currentState == State.Idle && targetTilePosition != null)
            {
                // partial tile movement
                var diff = targetTilePosition * Global.DistancePerBlock - RealPosition;
                float xDiff = diff.X / Global.DistancePerBlock;
                float yDiff = diff.Y / Global.DistancePerBlock;
                float maxDiff = Math.Max(xDiff, yDiff);
                ticksPerMovement += (uint)Util.Round(maxDiff * TicksPerMovement);
            }
            else
            {
                InitMovement();
            }

            currentState = State.MovingToTile;
        }

        public void MoveTowardsPlayer(FloatPosition playerPosition)
        {
            var diff = playerPosition - RealPosition;
            diff.Normalize();
            var nextPosition = RealPosition + diff * (0.25f * Global.DistancePerBlock);

            if (!MoveRequested(nextPosition))
            {
                currentState = State.BlockedToPlayer;
                return;
            }

            targetTilePosition = new Position(Util.Round(playerPosition.X / Global.DistancePerBlock), Util.Round(playerPosition.Y / Global.DistancePerBlock));
            direction = lastTilePosition.GetDirectionTo(targetTilePosition);
            currentState = State.MovingTowardsPlayer;
            NextMoveTimeSlot = uint.MaxValue;
            lastMoveTicks = game.CurrentMapTicks;

            var distance = Math.Min(0.25f, RealPosition.GetMaxDistance(playerPosition) / Global.DistancePerBlock);

            if (distance < 0.01f * Global.DistancePerBlock)
            {
                currentState = RealPosition == targetTilePosition * Global.DistancePerBlock
                    ? State.IdleOnTile : State.Idle;
                return;
            }

            ticksPerMovement = (uint)Util.Round(distance * TicksPerMovement);
        }

        void LostPlayer()
        {
            Stop(false);
        }

        public void Stop(bool waitForManualStart)
        {
            switch (currentState)
            {
            case State.MovingToTile:
                currentState = RealPosition == targetTilePosition * Global.DistancePerBlock
                    ? State.IdleOnTile : State.Idle;
                break;
            case State.MovingTowardsPlayer:
                currentState = RealPosition == targetTilePosition * Global.DistancePerBlock
                    ? State.IdleOnTile : State.Idle;
                targetTilePosition = null;
                break;
            }

            NextMoveTimeSlot = waitForManualStart ? uint.MaxValue : (game.GameTime.TimeSlot + 1) % 288;
            lastMoveTicks = game.CurrentMapTicks;
            movedTicks = 0;
        }

        public void ResetMovementTimer()
        {
            NextMoveTimeSlot = (game.GameTime.TimeSlot + 1) % 288;
        }

        public void Update(uint ticks, FloatPosition playerPosition, bool moveRandom, bool canSeePlayer,
            bool onlyMoveWhenSeePlayer, bool monster)
        {
            if (Paused)
                return;

            switch (currentState)
            {
            case State.IdleOnTile:
                if (canSeePlayer)
                {
                    if (monster)
                    {
                        movedTicks = 0;
                        MoveTowardsPlayer(playerPosition);
                    }
                    else if (moveRandom && game.GameTime.TimeSlot >= NextMoveTimeSlot)
                    {
                        ResetMovementTimer();
                        RandomMovementRequested?.Invoke();
                    }
                }
                else if (!onlyMoveWhenSeePlayer && moveRandom && game.GameTime.TimeSlot >= NextMoveTimeSlot)
                {
                    ResetMovementTimer();
                    RandomMovementRequested?.Invoke();
                }
                break;
            case State.Idle:
                if (canSeePlayer)
                {
                    if (monster)
                    {
                        movedTicks = 0;
                        MoveTowardsPlayer(playerPosition);
                    }
                    else if (moveRandom && game.GameTime.TimeSlot >= NextMoveTimeSlot)
                    {
                        ResetMovementTimer();
                        MoveToTile((uint)targetTilePosition.X, (uint)targetTilePosition.Y);
                    }
                }
                else if (!onlyMoveWhenSeePlayer && moveRandom && game.GameTime.TimeSlot >= NextMoveTimeSlot)
                {
                    ResetMovementTimer();

                    if (targetTilePosition == null)
                        RandomMovementRequested?.Invoke();
                    else
                        MoveToTile((uint)targetTilePosition.X, (uint)targetTilePosition.Y);
                }
                break;
            case State.MovingToTile:
                {
                    if (monster && canSeePlayer)
                    {
                        movedTicks = 0;
                        lastTilePosition = Position;
                        MoveTowardsPlayer(playerPosition);
                    }
                    else
                    {
                        if (ticks > lastMoveTicks)
                        {
                            uint moveTicks = Math.Min(ticks - lastMoveTicks, ticksPerMovement - movedTicks);
                            lastMoveTicks = ticks;
                            var diff = targetTilePosition - lastTilePosition;
                            diff.Normalize();
                            float stepSize = moveTicks * Global.DistancePerBlock / TicksPerMovement;

                            RealPosition.X += diff.X * stepSize;
                            RealPosition.Y += diff.Y * stepSize;
                            movedTicks += moveTicks;

                            if (movedTicks == ticksPerMovement) // finished movement
                            {
                                currentState = State.IdleOnTile;
                                lastTilePosition = Position;
                            }
                        }
                    }

                    break;
                }
            case State.MovingTowardsPlayer:
                {
                    if (!canSeePlayer)
                    {
                        LostPlayer();
                        return;
                    }

                    uint moveTicks = Math.Min(ticks - lastMoveTicks, ticksPerMovement - movedTicks);
                    lastMoveTicks = ticks;
                    var diff = playerPosition - RealPosition;
                    diff.Normalize();
                    float stepSize = moveTicks * Global.DistancePerBlock / TicksPerMovement;

                    RealPosition.X += diff.X * stepSize;
                    RealPosition.Y += diff.Y * stepSize;
                    movedTicks += moveTicks;

                    if (movedTicks == ticksPerMovement) // finished movement
                    {
                        currentState = State.IdleOnTile;
                        lastTilePosition = Position;
                        movedTicks = 0;
                        MoveTowardsPlayer(playerPosition);
                    }

                    break;
                }
            case State.BlockedToPlayer:
                {
                    if (!canSeePlayer)
                    {
                        LostPlayer();
                        return;
                    }

                    MoveTowardsPlayer(playerPosition);

                    break;
                }
            }
        }
    }
}
