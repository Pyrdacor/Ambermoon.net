using Ambermoon.Data;
using System;

namespace Ambermoon.Render
{
    using Geometry = Geometry.Geometry;

    class Player3D : IRenderPlayer
    {
        readonly Game game = null;
        readonly IMapManager mapManager;
        readonly RenderMap3D map;
        readonly Player player;

        Position lastPosition;
        public Position Position { get; private set; }
        public ICamera3D Camera { get; }
        public float angle = 0.0f;

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
            angle = 0.0f;
            lastPosition = new Position(Position);
            Geometry.BlockToCameraPosition(map.Map, Position, out float x, out float z);
            Camera.SetPosition(x, z);
        }

        public void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
            lastPosition = new Position(Position);
            Position = new Position((int)x, (int)y);

            if (this.map.Map != map)
            {
                if (newDirection == null)
                    throw new AmbermoonException(ExceptionScope.Application, "Direction must be given when changing maps.");

                if (map.Type == MapType.Map2D)
                {
                    game.Start2D(map, x, y, newDirection.Value);
                }
                else
                {
                    this.map.SetMap(map, x, y, newDirection.Value);
                    var oldMapIndex = map.Index;
                    SetPosition((int)x, (int)y, ticks);
                    game.PlayerMoved(oldMapIndex != game.Map.Index);

                    if (newDirection != null)
                        angle = (float)newDirection.Value * 90.0f;
                }
            }
            else
            {
                var oldMapIndex = map.Index;
                SetPosition((int)x, (int)y, ticks);
                game.PlayerMoved(oldMapIndex != game.Map.Index);

                if (newDirection != null)
                {
                    angle = (float)newDirection.Value * 90.0f;
                    Camera.TurnTowards(angle);
                }
            }

            player.Position.X = Position.X;
            player.Position.Y = Position.Y;
        }

        /// <summary>
        /// This will reset the view angle to up
        /// </summary>
        public void SetPosition(int x, int y, uint ticks)
        {
            Position = new Position(x, y);
            ResetCameraPosition();
            map.Map.TriggerEvents(game, this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
        }

        bool TestCollision(float x, float z, float lastMapX, float lastMapY)
        {
            Geometry.CameraToWorldPosition(map.Map, x, z, out float mapX, out float mapY);

            // This contains all collision bodies in a 3x3 area around the current position.
            var collisionDetectionInfo = map.GetCollisionDetectionInfo(Position);

            return collisionDetectionInfo.TestCollision(lastMapX, lastMapY, mapX, mapY, 0.15f * Global.DistancePerTile);
        }

        delegate void PositionProvider(float distance, out float newX, out float newY, bool noX, bool noZ);

        void Move(float distance, uint ticks, PositionProvider positionProvider, Action<float, bool, bool> mover)
        {
            void Move(bool noX, bool noZ)
            {
                mover(distance, noX, noZ);
                Position = Geometry.CameraToBlockPosition(map.Map, Camera.X, Camera.Z);

                if (Position != lastPosition)
                {
                    player.Position.X = Position.X;
                    player.Position.Y = Position.Y;
                    lastPosition = new Position(Position);
                    var oldMapIndex = map.Map.Index;
                    map.Map.TriggerEvents(game, this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
                    game.PlayerMoved(oldMapIndex != game.Map.Index);
                }
            }

            Geometry.CameraToWorldPosition(map.Map, Camera.X, Camera.Z, out float cameraMapX, out float cameraMapY);
            positionProvider(distance * 2f, out float newX, out float newY, false, false);

            if (TestCollision(newX, newY, cameraMapX, cameraMapY))
            {
                // If collision is detected try to move only in x direction
                positionProvider(distance * 2f, out newX, out newY, false, true);

                if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in x direction
                {
                    Move(false, true);
                    return;
                }

                // If collision is detected in x direction too, try to move only in z direction
                positionProvider(distance * 2f, out newX, out newY, true, false);

                if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in z direction
                {
                    Move(true, false);
                    return;
                }

                // If we are here, we can't move at all
                // TODO: Display OUCH
            }
            else
            {
                // We can move freely
                Move(false, false);
            }
        }

        public void MoveForward(float distance, uint ticks)
        {
            Move(distance, ticks, Camera.GetForwardPosition, Camera.MoveForward);
        }

        public void MoveBackward(float distance, uint ticks)
        {
            Move(distance, ticks, Camera.GetBackwardPosition, Camera.MoveBackward);
        }

        public void MoveLeft(float distance, uint ticks)
        {
            Move(distance, ticks, Camera.GetLeftPosition, Camera.MoveLeft);
        }

        public void MoveRight(float distance, uint ticks)
        {
            Move(distance, ticks, Camera.GetRightPosition, Camera.MoveRight);
        }

        public void TurnLeft(float angle) // in degrees
        {
            this.angle -= angle;
            Camera.TurnLeft(angle);
        }

        public void TurnRight(float angle) // in degrees
        {
            this.angle += angle;
            Camera.TurnRight(angle);
        }

        public void TurnTowards(float angle) // turn to attacking monster or stand on a spinner (in degrees)
        {
            this.angle = angle;
            Camera.TurnTowards(angle);
        }

        public void LevitateUp(float distance) // used for climbing up ladders/ropes or use levitation spell (distance is in the range of 0 to 1 where 1 is full room height)
        {
            // TODO
            Camera.LevitateUp(distance);
        }

        public void LevitateDown(float distance) // used for climbing down ladders/ropes (distance is in the range of 0 to 1 where 1 is full room height)
        {
            // TODO
            Camera.LevitateDown(distance);
        }

        public CharacterDirection Direction
        {
            get
            {
                while (angle > 315.0f)
                    angle -= 360.0f;
                while (angle < -45.0f)
                    angle += 360.0f;

                if (angle < 45.0f)
                    return CharacterDirection.Up;
                if (angle < 135.0f)
                    return CharacterDirection.Right;
                if (angle < 225.0f)
                    return CharacterDirection.Down;
                return CharacterDirection.Left;
            }
        }
    }
}
