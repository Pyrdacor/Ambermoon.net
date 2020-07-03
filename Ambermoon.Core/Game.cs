using Ambermoon.Data;
using Ambermoon.Render;

namespace Ambermoon
{
    public class Game
    {
        const uint ticksPerSecond = 60; // TODO
        uint currentTicks = 0;
        bool ingame = false;
        readonly IMapManager mapManager;
        readonly IRenderView renderView;
        Player player;

        // Rendering
        RenderMap renderMap = null;
        Player2D player2D = null;

        public Game(IRenderView renderView, IMapManager mapManager)
        {
            this.renderView = renderView;
            this.mapManager = mapManager;
        }

        public void Update(double deltaTime)
        {
            uint add = (uint)Util.Round(ticksPerSecond * (float)deltaTime);

            if (currentTicks <= uint.MaxValue - add)
                currentTicks += add;
            else
                currentTicks = (uint)(((long)currentTicks + add) % uint.MaxValue);

            if (ingame)
            {
                // TODO ingame rendering
                renderMap.UpdateAnimations(currentTicks);
            }
        }

        public void StartNew()
        {
            ingame = true;
            player = new Player();
            var map = mapManager.GetMap(258u); // grandfather's house
            renderMap = new RenderMap(map, mapManager.GetTilesetForMap(map), mapManager,
                renderView, TextureAtlasManager.Instance.GetOrCreate(Layer.MapBackground4));
            player2D = new Player2D(renderView.GetLayer(Layer.Characters), player, renderMap,
                renderView.SpriteFactory, renderView.GameData, new Position(2, 2));
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
