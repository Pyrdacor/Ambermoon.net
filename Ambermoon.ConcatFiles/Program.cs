using System;
using System.IO;

namespace Ambermoon.ConcatFiles
{
    class Program
    {
        static void AppendAllBytes(string path, byte[] bytes)
        {
            using var stream = new FileStream(path, FileMode.Append);
            stream.Write(bytes, 0, bytes.Length);
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Too few arguments for ConcatFiles");
                return;
            }

            string outfile = args[^1];

            for (int i = 0; i < args.Length - 1; ++i)
            {
                string file = Path.GetFullPath(args[i]);
                Console.WriteLine($"Appending file {file} ...");
                AppendAllBytes(outfile, File.ReadAllBytes(file));
            }

            Console.WriteLine($"Added {args.Length - 1} into {outfile}");
        }
    }
}
