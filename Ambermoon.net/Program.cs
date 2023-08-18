using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ambermoon
{
    class Program
    {
        const string ConfigurationFileName = Configuration.ConfigurationFileName;

        static Configuration LoadConfig()
        {
            var path = Path.Combine(Configuration.BundleDirectory, ConfigurationFileName);
            var configuration = Configuration.Load(path);

            if (configuration != null)
                return configuration;

            path = Path.Combine(Configuration.FallbackConfigDirectory, ConfigurationFileName);
            return Configuration.Load(path, new Configuration { FirstStart = true });
        }

        static void SaveConfig(Configuration configuration)
        {
            var path = Path.Combine(Configuration.BundleDirectory, ConfigurationFileName);

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

            try
            {
                Environment.CurrentDirectory = Configuration.ReadonlyBundleDirectory;
            }
            catch
            {
                // ignore
            }

            Configuration.FixMacOSPaths();
            var configuration = LoadConfig();
            configuration.UpgradeAdditionalSavegameSlots();
            configuration.SaveRequested += () => SaveConfig(configuration);
            var gameWindow = new GameWindow();
            int exitCode = 0;

            try
            {
                gameWindow.Run(configuration);
            }
            catch (Exception ex)
            {
                PrintException(ex);
                exitCode = 1;
            }
            finally
            {
                SaveConfig(configuration);
                DotnetCleanup();
                Environment.Exit(exitCode);
            }
        }

        static void OutputError(string error)
        {
            Console.WriteLine(error);
            
            try
            {
                File.WriteAllText(Path.Combine(Configuration.BundleDirectory, "error.txt"), error);
            }
            catch
            {
                // ignore
            }

            Console.WriteLine("Press return to exit");
            Console.ReadLine();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                PrintException(ex);
            else
                OutputError(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");

            Environment.Exit(1);
        }

        static void PrintException(Exception ex)
        {
            string message = ex.Message;
            string stackTrace = ex.StackTrace;
            ex = ex.InnerException;

            while (ex != null)
            {
                message += Environment.NewLine + ex.Message;
                ex = ex.InnerException;
            }

            OutputError(message + Environment.NewLine + stackTrace ?? "");
        }

        static void DotnetCleanup()
        {
            // As of netcore 3.1 the self-contained assembly will extract
            // all dependencies to a temp location. This is not cleaned up
            // automatically. So we try to do so on termination.
            // NET6 needs to extract some native dependecies as well.
            // TODO: Maybe later (.NET6 etc) we can remove this.
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
#pragma warning disable IL3000
            string appFolder = Assembly.GetEntryAssembly().Location;
#pragma warning restore IL3000

            if (string.IsNullOrEmpty(appFolder))
                appFolder = AppContext.BaseDirectory;
            else
                appFolder = Path.GetDirectoryName(appFolder);

            appFolder = Path.TrimEndingDirectorySeparator(appFolder);

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
