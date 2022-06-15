using Ambermoon.AdditionalData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ambermoon.ConcatFiles
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Too few arguments for ConcatFiles");
                return;
            }

            if (args.Length % 2 != 1)
            {
                Console.WriteLine("Invalid number of arguments for ConcatFiles");
                return;
            }

            string outfile = args[^1];

            if (!Path.IsPathRooted(outfile))
                outfile = Path.GetFullPath(outfile);

            List<DataEntry> entries = new();

            for (int i = 0; i < args.Length - 1; i += 2)
            {
                string name = args[i];
                string file = Path.GetFullPath(args[i + 1]);
                Console.WriteLine($"Appending file {file} with name {name} ...");
                entries.Add(new DataEntry(name, File.ReadAllBytes(file)));
            }

            Loader.Create(outfile, entries);

            Console.WriteLine($"Added {(args.Length - 1) / 2} files into {outfile}");
        }
    }
}
