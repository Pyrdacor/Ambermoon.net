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
            lastMoveTicks = game.CurrentTicks;
        }

        public void Place(uint x, uint y, bool waitForManualStart)
        {
            targetTilePosition = null;
            lastTilePosition = new Position((int)x, (int)y);
            RealPosition = lastTilePosition * Global.DistancePerBlock;
            direction = null;
            currentState = State.IdleOnTile;
            lastMoveTicks = game.CurrentTicks;
            NextMoveTimeSlot = waitForManualStart ? uint.MaxValue : (game.GameTime.TimeSlot + 1) % 288;
        }

        public void MoveToTile(uint x, uint y)
        {
            targetTilePosition = new Position((int)x, (int)y);
            direction = lastTilePosition.GetDirectionTo(targetTilePosition);
            lastMoveTicks = game.CurrentTicks;
            movedTicks = 0;

            ticksPerMovement = lastTilePosition.GetMaxDistance(targetTilePosition) * TicksPerMovement;

            if (currentState == State.Idle)
            {
                // partial tile movement
                var diff = targetTilePosition * Global.DistancePerBlock - RealPosition;
                float xDiff = diff.X / Global.DistancePerBlock;
                float yDiff = diff.Y / Global.DistancePerBlock;
                float maxDiff = Math.Max(xDiff, yDiff);
                ticksPerMovement += (uint)Util.Round(maxDiff * TicksPerMovement);
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
            lastMoveTicks = game.CurrentTicks;

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
            case State.MovingTowardsPlayer:
                currentState = RealPosition == targetTilePosition * Global.DistancePerBlock
                    ? State.IdleOnTile : State.Idle;
                break;
            }

            NextMoveTimeSlot = waitForManualStart ? uint.MaxValue : (game.GameTime.TimeSlot + 1) % 288;
            lastMoveTicks = game.CurrentTicks;
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
