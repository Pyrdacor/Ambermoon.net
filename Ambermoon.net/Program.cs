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

            try
            {
                gameWindow.Run(configuration);
            }
            catch
            {
                // TODO: ignored for now
            }

            configuration.Save(configurationPath);
        }
    }
}
