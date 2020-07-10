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
            dataReader.ReadBytes(8); // Unknown

            Console.WriteLine("NUM ENTRIES: " + numEntries);

            for (int i = 0; i < numEntries; ++i)
            {
                Console.WriteLine($"=== Entry {i} ===");

                // 66 bytes per entry
                for (int n = 0; n < 8; ++n)
                {
                    var data = string.Join(" ", dataReader.ReadBytes(8).Select(b => b.ToString("x2")));
                    Console.WriteLine($"Block{n} -> {data}");
                }
                Console.WriteLine($"\tEnd: {dataReader.ReadWord():x4}");
                Console.WriteLine();

                if (i == 10)
                    break; // TODO
            }
        }
    }
}
