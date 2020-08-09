using Ambermoon.Data;

namespace Ambermoon.Render
{
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
            Camera.SetPosition(Position.X * Global.DistancePerTile + 0.5f * Global.DistancePerTile,
                (map.Map.Height - Position.Y) * Global.DistancePerTile - 0.5f * Global.DistancePerTile);
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

        public void MoveForward(float distance, uint ticks)
        {
            // TODO: collision detection
            Camera.MoveForward(distance);

            var position = Camera.Position;
            Position = new Position(position.X, map.Map.Height - position.Y);
            map.Map.TriggerEvents(this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
        }

        public void MoveBackward(float distance, uint ticks)
        {
            // TODO: collision detection
            Camera.MoveBackward(distance);

            var position = Camera.Position;
            Position = new Position(position.X, map.Map.Height - position.Y);
            map.Map.TriggerEvents(this, MapEventTrigger.Move, (uint)Position.X, (uint)Position.Y, mapManager, ticks);
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
