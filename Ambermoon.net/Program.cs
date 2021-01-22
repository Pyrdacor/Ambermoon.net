using Ambermoon.Data.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ambermoon
{
    class Program
    {
        static void CheckSandbox(Configuration configuration)
        {
            using var reader = new BinaryReader(File.OpenRead(Process.GetCurrentProcess().MainModule.FileName));

            reader.BaseStream.Position = reader.BaseStream.Length - 6;
            var offset = reader.ReadUInt32();
            var magic = reader.ReadBytes(2);

            if (magic[0] == 0xB0 && magic[1] == 0x55 && offset > 0 && offset < reader.BaseStream.Length - 6) // sandbox
            {
                reader.BaseStream.Position = offset;
                var versionCount = reader.ReadUInt32();

                if (versionCount == 0 || versionCount >= int.MaxValue)
                    return;

                while (true)
                {
                    Console.WriteLine("Welcome to the Ambermoon.net sandbox.");
                    Console.WriteLine("This version should ease game testing.");
                    Console.WriteLine();
                    Console.WriteLine("=== VERSIONS ===");
                    List<uint> versionOffset = new List<uint>((int)versionCount);

                    for (int i = 1; i <= versionCount; ++i)
                    {
                        var version = reader.ReadString();
                        var parts = version.Split(',');
                        versionOffset.Add(reader.ReadUInt32());

                        if (parts.Length != 3)
                            return;

                        Console.WriteLine($"[{i}]: {parts[0]} ({parts[1]}) by {parts[2]}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("Enter 0 to load from normal data.");
                    Console.Write("Pick sandbox data version: ");
                    var indexString = Console.ReadLine();

                    if (!int.TryParse(indexString, out int index) || index > versionCount)
                    {
                        Console.Clear();
                        continue;
                    }

                    if (index == 0)
                        return;

                    offset = versionOffset[index - 1];
                    reader.BaseStream.Position = offset;
                    uint size = reader.ReadUInt32();
                    var gameData = new GameData();
                    gameData.LoadFromContainer(reader.ReadBytes((int)size));
                    configuration.GameData = gameData;
                }
            }
        }

        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var configurationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ambermoon", "ambermoon.cfg");
            var configuration = Configuration.Load(configurationPath);
            var gameWindow = new GameWindow();

            try
            {
                CheckSandbox(configuration);
                gameWindow.Run(configuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                // TODO: ignored for now
            }

            configuration.Save(configurationPath);
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
