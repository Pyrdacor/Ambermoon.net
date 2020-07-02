using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ambermoon
{
    public interface IGame
    {
        ISceneRenderer SceneRenderer { get; }

        event Action<Keys> KeyDown;
        event Action<Point> LeftMouseDown;
        event Action<Point> RightMouseDown;
        event Action<Point> MiddleMouseDown;
        event Action<Point> MouseMove;
        event Action<int> Wheel;
    }
}
