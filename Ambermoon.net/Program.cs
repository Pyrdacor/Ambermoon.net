using System;
using System.Drawing;
using Ambermoon.Renderer.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace Ambermoon
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var gameWindow = new GameWindow();

            gameWindow.Run(640, 480);
        }
    }
}
