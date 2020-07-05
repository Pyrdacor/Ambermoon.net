using Ambermoon.Data;
using Ambermoon.Render;
using System;

namespace Ambermoon
{
    public class Game
    {
        const uint _ticksPerSecond = 60; // TODO
        uint _currentTicks = 0;
        bool _ingame = false;
        UI.Layout _layout;
        readonly IMapManager _mapManager;
        readonly IRenderView _renderView;
        Player _player;

        // Rendering
        RenderMap2D renderMap = null;
        Player2D player2D = null;

        public Game(IRenderView renderView, IMapManager mapManager)
        {
            _renderView = renderView;
            _mapManager = mapManager;
            _layout = new UI.Layout(renderView);
        }

        public void Update(double deltaTime)
        {
            uint add = (uint)Util.Round(_ticksPerSecond * (float)deltaTime);

            if (_currentTicks <= uint.MaxValue - add)
                _currentTicks += add;
            else
                _currentTicks = (uint)(((long)_currentTicks + add) % uint.MaxValue);

            if (_ingame)
            {
                // TODO ingame rendering
                renderMap.UpdateAnimations(_currentTicks);
            }
        }

        public void StartNew()
        {
            _ingame = true;
            _layout.SetLayout(UI.LayoutType.Map);
            _player = new Player();
            var map = _mapManager.GetMap(258u); // grandfather's house
            renderMap = new RenderMap2D(map, _mapManager, _renderView);
            player2D = new Player2D(_renderView.GetLayer(Layer.Characters), _player, renderMap,
                _renderView.SpriteFactory, _renderView.GameData, new Position(2, 2), _mapManager);
            player2D.Visible = true;
            _player.MovementAbility = PlayerMovementAbility.Walking;
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
                    player2D.Move(-1, 0, _currentTicks);
                    break;
                case Key.Right:
                    player2D.Move(1, 0, _currentTicks);
                    break;
                case Key.Up:
                    player2D.Move(0, -1, _currentTicks);
                    break;
                case Key.Down:
                    player2D.Move(0, 1, _currentTicks);
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
