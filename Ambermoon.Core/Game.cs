using Ambermoon.Data;
using Ambermoon.Render;
using System;

namespace Ambermoon
{
    public class Game
    {
        const uint TicksPerSecond = 60; // TODO
        uint currentTicks = 0;
        uint lastMapTicksReset = 0;
        uint lastKeyTicksReset = 0;
        bool ingame = false;
        readonly UI.Layout layout;
        readonly IMapManager mapManager;
        readonly IRenderView renderView;
        Player player;
        bool is3D = false;
        readonly bool[] keys = new bool[Enum.GetValues(typeof(Key)).Length];

        // Rendering
        RenderMap2D renderMap2D = null;
        Player2D player2D = null;
        RenderMap3D renderMap3D = null;
        readonly ICamera3D camera3D = null;

        public Game(IRenderView renderView, IMapManager mapManager)
        {
            this.renderView = renderView;
            this.mapManager = mapManager;
            camera3D = renderView.Camera3D;
            layout = new UI.Layout(renderView);
        }

        public void Update(double deltaTime)
        {
            uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

            if (currentTicks <= uint.MaxValue - add)
                currentTicks += add;
            else
                currentTicks = (uint)(((long)currentTicks + add) % uint.MaxValue);

            if (ingame)
            {
                // TODO ingame rendering

                if (is3D)
                {
                    // TODO
                }
                else // 2D
                {
                    var animationTicks = currentTicks >= lastMapTicksReset ? currentTicks - lastMapTicksReset : (uint)((long)currentTicks + uint.MaxValue - lastMapTicksReset);
                    renderMap2D.UpdateAnimations(animationTicks);
                }
            }

            var keyTicks = currentTicks >= lastKeyTicksReset ? currentTicks - lastKeyTicksReset : (uint)((long)currentTicks + uint.MaxValue - lastKeyTicksReset);

            if (keyTicks >= TicksPerSecond / 8)
            {
                if (keys[(int)Key.Left] && !keys[(int)Key.Right])
                {
                    if (renderMap2D != null)
                        player2D.Move(-1, 0, currentTicks);
                    else if (renderMap3D != null)
                        camera3D.TurnLeft(10.0f); // TODO
                }
                if (keys[(int)Key.Right] && !keys[(int)Key.Left])
                {
                    if (renderMap2D != null)
                        player2D.Move(1, 0, currentTicks);
                    else if (renderMap3D != null)
                        camera3D.TurnRight(10.0f); // TODO
                }
                if (keys[(int)Key.Up] && !keys[(int)Key.Down])
                {
                    if (renderMap2D != null)
                        player2D.Move(0, -1, currentTicks);
                    else if (renderMap3D != null)
                        camera3D.MoveForward(0.2f); // TODO
                }
                if (keys[(int)Key.Down] && !keys[(int)Key.Up])
                {
                    if (renderMap2D != null)
                        player2D.Move(0, 1, currentTicks);
                    else if (renderMap3D != null)
                        camera3D.MoveBackward(0.2f); // TODO
                }

                lastKeyTicksReset = currentTicks;
            }
        }

        internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction)
        {
            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

            if (renderMap2D != null)
                throw new AmbermoonException(ExceptionScope.Application, "Render map 2D should not be present.");

            renderMap2D = new RenderMap2D(map, mapManager, renderView,
                (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                (uint)Util.Limit(0, (int)playerY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y));

            player2D.Visible = true;
            player2D.MoveTo(map, playerX, playerY, currentTicks, true, direction);

            var mapOffset = map.MapOffset;
            player.Position.X = mapOffset.X + (int)playerX - (int)renderMap2D.ScrollX;
            player.Position.Y = mapOffset.Y + (int)playerY - (int)renderMap2D.ScrollY;
            player.Direction = direction;

            renderMap3D = null;

            is3D = false;
            renderView.GetLayer(Layer.Map3D).Visible = false;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = true;
        }

        internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

            if (renderMap3D != null)
                throw new AmbermoonException(ExceptionScope.Application, "Render map 3D should not be present.");

            // TODO: player direction is not neccessarily the one of the previous map
            renderMap3D = new RenderMap3D(map, mapManager, renderView, playerX, playerY, direction);
            renderMap2D = null;
            camera3D.SetPosition(playerX * RenderMap3D.DistancePerTile, playerY * RenderMap3D.DistancePerTile);

            player2D.Visible = false;
            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;

            is3D = true;
            renderView.GetLayer(Layer.Map3D).Visible = true;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = false;
        }

        public void StartNew()
        {
            ingame = true;
            layout.SetLayout(UI.LayoutType.Map);
            player = new Player();
            var map = mapManager.GetMap(258u); // grandfather's house
            renderMap2D = new RenderMap2D(map, mapManager, renderView);
            renderMap3D = null;
            player2D = new Player2D(this, renderView.GetLayer(Layer.Characters), player, renderMap2D,
                renderView.SpriteFactory, renderView.GameData, new Position(2, 2), mapManager);
            player2D.Visible = true;
            player.MovementAbility = PlayerMovementAbility.Walking;
            // TODO
        }

        public void LoadGame()
        {
            // TODO
        }

        public void Continue()
        {
            // TODO: load latest game
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            keys[(int)key] = true;

            if (keys[(int)Key.Left] && !keys[(int)Key.Right])
            {
                if (renderMap2D != null)
                    player2D.Move(-1, 0, currentTicks);
                else if (renderMap3D != null)
                    camera3D.TurnLeft(10.0f); // TODO
            }
            if (keys[(int)Key.Right] && !keys[(int)Key.Left])
            {
                if (renderMap2D != null)
                    player2D.Move(1, 0, currentTicks);
                else if (renderMap3D != null)
                    camera3D.TurnRight(10.0f); // TODO
            }
            if (keys[(int)Key.Up] && !keys[(int)Key.Down])
            {
                if (renderMap2D != null)
                    player2D.Move(0, -1, currentTicks);
                else if (renderMap3D != null)
                    camera3D.MoveForward(0.2f); // TODO
            }
            if (keys[(int)Key.Down] && !keys[(int)Key.Up])
            {
                if (renderMap2D != null)
                    player2D.Move(0, 1, currentTicks);
                else if (renderMap3D != null)
                    camera3D.MoveBackward(0.2f); // TODO
            }

            lastKeyTicksReset = currentTicks;
        }

        public void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            keys[(int)key] = false;
        }

        public void OnKeyChar(char keyChar)
        {

        }

        public void OnMouseDown(MouseButtons buttons)
        {

        }
    }
}
