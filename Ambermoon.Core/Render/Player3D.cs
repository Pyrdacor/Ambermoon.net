using Ambermoon.Data;
using Ambermoon.Geometry;
using System;

namespace Ambermoon.Render
{
    using Geometry = Ambermoon.Geometry.Geometry;

    class Player3D : IRenderPlayer
    {
        readonly Game game = null;
        readonly IMapManager mapManager;
        readonly RenderMap3D map;
        int lastX = 0;
        int lastY = 0;

        public Position Position { get; private set; }
        public ICamera3D Camera { get; }

        public Player3D(Game game, IMapManager mapManager, ICamera3D camera, RenderMap3D map, int x, int y)
        {
            this.game = game;
            this.mapManager = mapManager;
            this.map = map;
            Camera = camera;
        }

        void ResetCameraPosition()
        {
            Geometry.BlockToCameraPosition(map.Map, Position, out float x, out float z);
            Camera.SetPosition(x, z);
        }

        public void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection)
        {
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
                    SetPosition((int)x, (int)y, ticks);
                }
            }
            else
            {
                SetPosition((int)x, (int)y, ticks);
            }
        }

        /// <summary>
        /// This will reset the view angle to up
        /// </summary>
        public void SetPosition(int x, int y, uint ticks)
        {
            Position = new Position(x, y);
            ResetCameraPosition();
            map.Map.TriggerEvents(this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
            lastX = x;
            lastY = y;
        }

        bool TestCollision(float x, float z, float lastMapX, float lastMapY)
        {
            Geometry.CameraToWorldPosition(map.Map, x, z, out float mapX, out float mapY);

            // This contains all collision bodies in a 3x3 area around the current position.
            var collisionDetectionInfo = map.GetCollisionDetectionInfo(Position);

            return collisionDetectionInfo.TestCollision(lastMapX, lastMapY, mapX, mapY, 0.125f * Global.DistancePerTile);
        }

        public void MoveForward(float distance, uint ticks)
        {
            void Move(bool noX, bool noZ)
            {
                Camera.MoveForward(distance, noX, noZ);
                Position = Geometry.CameraToBlockPosition(map.Map, Camera.X, Camera.Z);
                map.Map.TriggerEvents(this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
            }

            Geometry.CameraToWorldPosition(map.Map, Camera.X, Camera.Z, out float cameraMapX, out float cameraMapY);
            Camera.GetForwardPosition(distance * 2f, out float newX, out float newY, false, false);

            if (TestCollision(newX, newY, cameraMapX, cameraMapY))
            {
                // If collision is detected try to move only in x direction
                Camera.GetForwardPosition(distance * 2f, out newX, out newY, false, true);

                if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in x direction
                {
                    Move(false, true);
                    return;
                }

                // If collision is detected in x direction too, try to move only in z direction
                Camera.GetForwardPosition(distance * 2f, out newX, out newY, true, false);

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

        public void MoveBackward(float distance, uint ticks)
        {
            void Move(bool noX, bool noZ)
            {
                Camera.MoveBackward(distance, noX, noZ);
                Position = Geometry.CameraToBlockPosition(map.Map, Camera.X, Camera.Z);
                map.Map.TriggerEvents(this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
            }

            Geometry.CameraToWorldPosition(map.Map, Camera.X, Camera.Z, out float cameraMapX, out float cameraMapY);
            Camera.GetBackwardPosition(distance * 2f, out float newX, out float newY, false, false);

            if (TestCollision(newX, newY, cameraMapX, cameraMapY))
            {
                // If collision is detected try to move only in x direction
                Camera.GetBackwardPosition(distance * 2f, out newX, out newY, false, true);

                if (!TestCollision(newX, newY, cameraMapX, cameraMapY)) // we can move in x direction
                {
                    Move(false, true);
                    return;
                }

                // If collision is detected in x direction too, try to move only in z direction
                Camera.GetBackwardPosition(distance * 2f, out newX, out newY, true, false);

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

        public void TurnLeft(float angle) // in degrees
        {
            Camera.TurnLeft(angle);
        }

        public void TurnRight(float angle) // in degrees
        {
            Camera.TurnRight(angle);
        }

        public void TurnTowards(float angle) // turn to attacking monster or stand on a spinner (in degrees)
        {
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
    }
}
