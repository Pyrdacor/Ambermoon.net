using System;
using System.IO;

namespace Ambermoon
{
    class Program
    {
        const string ConfigurationFileName = "ambermoon.cfg";

        static Configuration LoadConfig()
        {
            var path = Path.Combine(Configuration.ExecutablePath, ConfigurationFileName);
            var configuration = Configuration.Load(path);

            if (configuration != null)
                return configuration;

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ambermoon", ConfigurationFileName);
            return Configuration.Load(path, new Configuration());
        }

        static void SaveConfig(Configuration configuration)
        {
            var path = Path.Combine(Configuration.ExecutablePath, ConfigurationFileName);

            try
            {
                configuration.Save(path);
            }
            catch
            {
                try
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Ambermoon", ConfigurationFileName);
                    configuration.Save(path);
                }
                catch
                {
                    Console.WriteLine("Unable to save configuration.");
                }
            }
        }

        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var configuration = LoadConfig();
            var gameWindow = new GameWindow();

            try
            {
                gameWindow.Run(configuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                // TODO: ignored for now
            }
            finally
            {
                SaveConfig(configuration);
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            else
                Console.WriteLine(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");
        }
    }
}
