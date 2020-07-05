using System;

namespace Ambermoon
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var gameWindow = new GameWindow();

            gameWindow.Run(1280, 800);
        }
    }
}
