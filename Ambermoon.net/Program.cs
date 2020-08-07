using System;
using System.IO;

namespace Ambermoon
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var configurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ambermoon", "ambermoon.cfg");
            var configuration = Configuration.Load(configurationPath);
            var gameWindow = new GameWindow();

            gameWindow.Run(configuration);

            configuration.Save(configurationPath);
        }
    }
}
