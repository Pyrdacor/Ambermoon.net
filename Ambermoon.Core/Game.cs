using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon
{
    public class Game
    {
        const uint TicksPerSecond = 60; // TODO
        uint currentTicks = 0;
        uint lastMapTicksReset = 0;
        bool ingame = false;
        UI.Layout layout;
        readonly IMapManager mapManager;
        readonly IRenderView renderView;
        Player player;
        bool is3D = false;

        // Rendering
        RenderMap2D renderMap2D = null;
        Player2D player2D = null;
        RenderMap3D renderMap3D = null;

        public Game(IRenderView renderView, IMapManager mapManager)
        {
            this.renderView = renderView;
            this.mapManager = mapManager;
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
                    renderMap2D.UpdateAnimations(currentTicks >= lastMapTicksReset ? currentTicks - lastMapTicksReset : (uint)((long)currentTicks + uint.MaxValue - lastMapTicksReset));
                }
            }
        }

        internal void Start2D(Map map, uint playerX, uint playerY)
        {
            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

            if (renderMap2D != null)
                throw new AmbermoonException(ExceptionScope.Application, "Render map 2D should not be present.");

            renderMap2D = new RenderMap2D(map, mapManager, renderView,
                (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                (uint)Util.Limit(0, (int)playerY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y));

            player2D.Visible = true;
            var mapOffset = map.MapOffset;
            player.Position.X = mapOffset.X + (int)playerX - (int)renderMap2D.ScrollX;
            player.Position.Y = mapOffset.Y + (int)playerY - (int)renderMap2D.ScrollY;

            renderMap3D = null;

            is3D = false;
            renderView.GetLayer(Layer.Map3D).Visible = false;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = true;
        }

        internal void Start3D(Map map, uint playerX, uint playerY)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

            if (renderMap3D != null)
                throw new AmbermoonException(ExceptionScope.Application, "Render map 3D should not be present.");

            renderMap3D = new RenderMap3D(map, mapManager, renderView, playerX, playerY, player.Direction);

            player2D.Visible = false;
            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;

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
            switch (key)
            {
                case Key.Left:
                    player2D.Move(-1, 0, currentTicks);
                    break;
                case Key.Right:
                    player2D.Move(1, 0, currentTicks);
                    break;
                case Key.Up:
                    player2D.Move(0, -1, currentTicks);
                    break;
                case Key.Down:
                    player2D.Move(0, 1, currentTicks);
                    break;
            }
        }

        public void OnKeyChar(char keyChar)
        {

        }

        public void OnMouseDown(MouseButtons buttons)
        {

        }
    }
}
