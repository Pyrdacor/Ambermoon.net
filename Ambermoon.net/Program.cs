﻿using System;
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

            try
            {
                Environment.CurrentDirectory = Configuration.ExecutableDirectoryPath;
            }
            catch
            {
                // ignore
            }

            var configuration = LoadConfig();
            configuration.UpgradeAdditionalSavegameSlots();
            var gameWindow = new GameWindow();

            try
            {
                gameWindow.Run(configuration);
            }
            catch (Exception ex)
            {
                PrintException(ex);
                Environment.Exit(1);
            }
            finally
            {
                SaveConfig(configuration);
                DotnetCleanup();
            }
        }

        static void OutputError(string error)
        {
            Console.WriteLine(error);
            
            try
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.txt"), error);
            }
            catch
            {
                // ignore
            }
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

            if (ex.InnerException != null)
            {
                message += Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            OutputError(message + Environment.NewLine + ex.StackTrace ?? "");
        }

        static void DotnetCleanup()
        {
            // As of netcore 3.1 the self-contained assembly will extract
            // all dependencies to a temp location. This is not cleaned up
            // automatically. So we try to do so on termination.
            // NET6 needs to extract some native dependecies as well.
            // TODO: Maybe later (.NET6 etc) we can remove this.
#pragma warning disable IL3000
            string appFolder = Assembly.GetEntryAssembly().Location;
#pragma warning restore IL3000

            if (string.IsNullOrEmpty(appFolder))
                appFolder = AppContext.BaseDirectory;
            else
                appFolder = Path.GetDirectoryName(appFolder);

            appFolder = Path.TrimEndingDirectorySeparator(appFolder);

            if (OperatingSystem.IsWindows())
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
