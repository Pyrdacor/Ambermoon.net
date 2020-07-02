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

        // Rendering
        RenderMap renderMap = null;

        public Game(IRenderView renderView, IMapManager mapManager)
        {
            this.renderView = renderView;
            this.mapManager = mapManager;
        }

        public void Update(double deltaTime)
        {
            uint add = (uint)Util.Round(ticksPerSecond * (float)deltaTime / 1000.0f);

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
            renderMap = new RenderMap(mapManager.GetMap(257u), mapManager, renderView, TextureAtlasManager.Instance.GetOrCreate(Layer.MapBackground));

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
    }
}
