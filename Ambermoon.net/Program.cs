using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

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
            return Configuration.Load(path, new Configuration { FirstStart = true });
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
                DotnetCleanup();
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

        static void DotnetCleanup()
        {
            // As of netcore 3.1 the self-contained assembly will extract
            // all dependencies to a temp location. This is not cleaned up
            // automatically. So we try to do so on termination.
            // TODO: Maybe later (.NET6 etc) we can remove this.
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            string mainExecutable = Assembly.GetEntryAssembly().Location;
            string appFolder = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            if (isWindows)
            {
                CleanUpTempFolder(Path.TrimEndingDirectorySeparator(Path.GetTempPath()));
            }
            else
            {
                CleanUpTempFolder("/var/tmp");
                CleanUpTempFolder("~");
            }

            void TryDeleteFile(string path)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // ignore
                }
            }

            void TryDeleteDirectory(string path, bool onlyClearContent = false)
            {
                try
                {
                    var files = Directory.GetFiles(path);

                    foreach (var file in files)
                        TryDeleteFile(file);

                    var subDirectories = Directory.GetDirectories(path);

                    foreach (var subDirectory in subDirectories)
                        TryDeleteDirectory(subDirectory);

                    if (!onlyClearContent)
                        Directory.Delete(path);
                }
                catch
                {
                    // ignore
                }
            }

            void CleanUpTempFolder(string tempRootFolder)
            {
                string escapedTempDirectory = Path.DirectorySeparatorChar == '/' ? tempRootFolder : tempRootFolder.Replace("\\", "\\\\");
                string guidPattern = @"[^/\\]+";
                string escapedDirSeparator = Path.DirectorySeparatorChar == '\\' ? "\\\\" : Path.DirectorySeparatorChar.ToString();
                var tempRegex = new Regex($"^{escapedTempDirectory}{escapedDirSeparator}.net{escapedDirSeparator}Ambermoon.net{escapedDirSeparator}{guidPattern}$");

                if (tempRegex.IsMatch(appFolder))
                {
                    TryDeleteDirectory(appFolder, true);

                    foreach (var directory in Directory.GetDirectories(Path.GetDirectoryName(appFolder)))
                    {
                        if (appFolder != directory && tempRegex.IsMatch(directory))
                            TryDeleteDirectory(directory);
                    }
                }
            }
        }
    }
}
