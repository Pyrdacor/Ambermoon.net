using System.Diagnostics;
using System.IO.Compression;
using AmbermoonPatcher;

int numWaitCycles = 10;
Console.WriteLine("*** Ambermoon.net Patcher ***");
Console.WriteLine();
Console.WriteLine("Waiting for Ambermoon.net to close ...");

while (true)
{
    Thread.Sleep(500);

    var processes = Enumerable.Concat(Process.GetProcessesByName("Ambermoon.net"), Process.GetProcessesByName("Ambermoon.net.exe"));

    if (!processes.Any())
        break;

    if (numWaitCycles-- == 0)
    {
        Console.WriteLine("Unable to update Ambermoon.net as it is still running.");
        Console.WriteLine("Please close all instances and run only a single Ambermoon.net instance.");
        Console.ReadLine();
        return;
    }
}

Console.WriteLine();
Console.WriteLine($"Installation path: {args[1]}");
Console.WriteLine();
Console.WriteLine("Extracting downloaded files ...");

try
{
    if (OperatingSystem.IsMacOS() || OperatingSystem.IsWindows())
    {
        using var zipFile = File.OpenRead(args[0]);
        using var zip = new ZipArchive(zipFile);
        var exe = zip.Entries.FirstOrDefault(entry => entry.Name.StartsWith("Ambermoon.net"));

        if (exe == null)
        {
            Console.WriteLine("The downloaded zip does not contain the game exe.");
            Console.WriteLine("Please report that to Pyrdacor (trobt@web.de).");
            Console.ReadLine();
            return;
        }

        exe.ExtractToFile(Path.Combine(args[1], exe.Name), true);
    }
    else
    {
        Tar.ExtractTarGz(args[0], args[1], file => file.StartsWith("Ambermoon.net"));
    }
}
catch (Exception ex)
{
    Console.WriteLine("Unable to extract downloaded files. Please download the update yourself.");
#if DEBUG
    Console.WriteLine(ex.ToString());
#endif
    Console.ReadLine();
    return;
}

Console.WriteLine("Starting the updated game ...");

try
{
    if (OperatingSystem.IsWindows())
        Process.Start(Path.Combine(args[1], "Ambermoon.net.exe"));
    else
        Process.Start(Path.Combine(args[1], "Ambermoon.net"));
}
catch (Exception ex)
{
    Console.WriteLine("Failed to start Ambermoon.net. Please do so yourself.");
#if DEBUG
    Console.WriteLine(ex.ToString());
#endif
    Console.ReadLine();
}