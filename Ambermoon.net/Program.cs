using System;
using System.IO;

namespace Ambermoon
{
    class Program
    {
        const string ConfigurationFileName = "ambermoon.cfg";

        static Configuration LoadConfig()
        {
            var path = Path.Combine(Configuration.ExecutableDirectoryPath, ConfigurationFileName);
            var configuration = Configuration.Load(path);

            if (configuration != null)
                return configuration;

            path = Path.Combine(Configuration.FallbackConfigDirectory, ConfigurationFileName);
            return Configuration.Load(path, new Configuration());
        }

        static void SaveConfig(Configuration configuration)
        {
            var path = Path.Combine(Configuration.ExecutableDirectoryPath, ConfigurationFileName);

            try
            {
                configuration.Save(path);
            }
            catch
            {
                try
                {
                    path = Path.Combine(Configuration.FallbackConfigDirectory, ConfigurationFileName);
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
                PrintException(ex);
            }
            finally
            {
                SaveConfig(configuration);
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                PrintException(ex);
            else
                Console.WriteLine(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");
        }

        static void PrintException(Exception ex)
        {
            string message = ex.Message;

            if (ex.InnerException != null)
            {
                message += Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            Console.WriteLine(message + Environment.NewLine + ex.StackTrace);
        }
    }
}
