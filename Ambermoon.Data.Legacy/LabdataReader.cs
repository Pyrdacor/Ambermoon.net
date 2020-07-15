using System;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class LabdataReader : ILabdataReader
    {
        public void ReadLabdata(Labdata labdata, IDataReader dataReader)
        {
            dataReader.ReadBytes(8); // Unknown
            int numEntries = dataReader.ReadWord();
            dataReader.ReadBytes(6); // Unknown

            Console.WriteLine("LAB DATA PART 1");
            Console.WriteLine("NUM ENTRIES: " + numEntries);

            for (int i = 0; i < numEntries; ++i)
            {
                Console.WriteLine($"=== Entry {i+1} ===");

                Console.WriteLine($"\tHeader: {dataReader.ReadWord():x4}");

                // 66 bytes per entry
                for (int n = 0; n < 8; ++n)
                {
                    var data = string.Join(" ", dataReader.ReadBytes(8).Select(b => b.ToString("x2")));
                    Console.WriteLine($"Block{n} -> {data}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("LAB DATA PART 2");

            while (dataReader.Position <= dataReader.Size - 14)
            {
                var index = dataReader.ReadWord();
                Console.WriteLine($"Index: {index} (hex: {index:x4}");
                Console.WriteLine(string.Join(" ", dataReader.ReadBytes(12).Select(b => b.ToString("x2"))));
            }

            Console.WriteLine("Remaining bytes: " + (dataReader.Size - dataReader.Position));
        }
    }
}
