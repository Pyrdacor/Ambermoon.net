using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.Legacy.Serialization
{
	/**
	 * The imploder creates
	 * - 1 first code hunk (startup code)
	 * - n BSS hunks (one for each in the deploded file except for RELOC32 hunks) which allocate empty memory
	 * - then another code hunk (actual decompression logic)
	 * - then a data hunk (compressed data of all destination hunks)
	 * - and finally another BSS hunk (decompression buffer)
	 * 
	 * The hunk sizes of the deploded hunks match the sizes of the BSS allocations.
	 * 
	 * AM2_CPU has 8 hunks:
	 * - Code
	 * - Reloc32
	 * - 2x BSS
	 * - 2x Data (this is what we want!)
	 * - Another Reloc32
	 * - Another BSS
	 * 
	 * The Reloc32 hunks are not reflected in the imploder BSS hunks.
	 * 
	 * If we are only interested in the data (and maybe code) hunks of AM2_CPU
	 * we can just get the file sizes from the first, fourth and fifth BSS hunk
	 * which are the deploded sizes (without the header -> number of dwords).
	 */

    public static class AmigaExecutable
    {
		// Note: Amiga executables store strings in encoding "Amiga Commodore".
		// It is very similar to iso-8859-1 except for 4 modifications which
		// shouldn't matter for language texts. So we just use iso-8859-1.
		public static readonly Encoding Encoding = Encoding.GetEncoding("iso-8859-1");

		public interface IHunk
		{
			HunkType Type { get; }
		}

		public struct Hunk : IHunk
        {
			public HunkType Type { get; internal set; }
			public uint NumEntries;
			public byte[] Data;
        }

		public struct Reloc32Hunk : IHunk
        {
			public HunkType Type { get; internal set; }
			public Dictionary<uint, List<uint>> Entries;
        }

		public enum HunkType
        {
			Code = 0x3E9,
			Data = 0x3EA,
			BSS = 0x3EB,
			RELOC32 = 0x3EC,
			END = 0x3F2
        }

		public static List<IHunk> Read(IDataReader dataReader, bool deplodeIfNecessary = true)
        {
			dataReader.Position = 0;

			static void Throw()
			{
				throw new AmbermoonException(ExceptionScope.Data, "Invalid executable file.");
			}

			if (dataReader.ReadDword() != 0x000003F3)
				Throw();

			if (dataReader.ReadDword() != 0) // number ob library strings (should be 0)
				Throw();

			uint numHunks = dataReader.ReadDword();
			uint firstHunk = dataReader.ReadDword();
			uint lastHunk = dataReader.ReadDword();

			if (lastHunk - firstHunk + 1 != numHunks)
				Throw();

			uint[] hunkSizes = new uint[numHunks];

			for (int i = 0; i < numHunks; ++i)
			{
				var hunkSize = dataReader.ReadDword();
				var hunkMemFlags = hunkSize >> 30;

				if (hunkMemFlags == 3) // skip extended mem flags
					dataReader.Position += 4;

				hunkSizes[i] = hunkSize & 0x3FFFFFFF;
			}

			var hunks = new List<IHunk>((int)numHunks);

			for (int i = 0; i < numHunks; ++i)
			{
				var type = (HunkType)(dataReader.ReadDword() & 0x1fffffff);
				IHunk hunk;

				switch (type)
				{
					case HunkType.Code:
					case HunkType.Data:
					{
						uint numEntries = dataReader.ReadDword();
						hunk = new Hunk
						{
							Type = type,
							NumEntries = numEntries,
							Data = dataReader.ReadBytes((int)numEntries * 4)
						};
						break;
					}
					case HunkType.BSS:
					{
						uint allocSize = dataReader.ReadDword();
						hunk = new Hunk
						{
							Type = type,
							NumEntries = allocSize,
							Data = null
						};
						break;
					}
					case HunkType.RELOC32:
					{
						var entries = new Dictionary<uint, List<uint>>();
						uint numOffsets;

						while ((numOffsets = dataReader.ReadDword()) != 0)
						{
							uint hunkNumber = dataReader.ReadDword();
							entries.Add(hunkNumber, new List<uint>((int)numOffsets));
							var list = entries[hunkNumber];

							for (int o = 0; o < numOffsets; ++o)
								list.Add(dataReader.ReadDword());
						}

						hunk = new Reloc32Hunk
						{
							Type = type,
							Entries = entries
						};

						++numHunks;
						break;
					}
					case HunkType.END:
					{
						hunk = new Hunk
						{
							Type = type,
							NumEntries = 0,
							Data = null
						};
						++numHunks;
						break;
					}
					default:
						throw new AmbermoonException(ExceptionScope.Data, $"Unsupported hunk type: {type}.");
				}

				hunks.Add(hunk);
			}

			if (deplodeIfNecessary)
			{
				bool imploded = false;

				if (hunks.Count != 0)
				{
					imploded = true;
					var firstHunkData = ((Hunk)hunks.First()).Data;

					for (int i = 0; i < ImplodeHunkHeader.Length; ++i)
					{
						if (firstHunkData[i] != ImplodeHunkHeader[i])
						{
							imploded = false;
							break;
						}
					}
				}

				return imploded ? ReadImploded(hunks) : hunks;
			}

			return hunks;
		}

		static readonly byte[] ImplodeHunkHeader = new byte[10]
		{
			0x48, 0xe7, 0xff, 0xff, 0x49, 0xfa, 0x00, 0x5e, 0x3c, 0x3c
		};

		static List<IHunk> ReadImploded(List<IHunk> imploderHunks)
        {
			var deplodedData = Deplode(imploderHunks, out var hunkSizes);
			var hunks = new List<IHunk>();
			var reader = new DataReader(deplodedData);
			int hunkSizeIndex = 0;

			while (true)
			{
				var header = reader.ReadDword();
				var flags = header >> 30;
				var hunkSize = header & 0x3FFFFFFF;

				// Note: The following is just guessing.
				// Code hunks seem to have flags = 0 and always have a RELOC32 followed?
				// BSS and DATA have flags = 2 or 3 (BSS has size 0)
				// 3 is used if no END hunk follows.
				// RELOC32 seems to have flags = 1
				// END hunks are inserted after each hunk expect for flags = 3.

				if (flags == 2 || flags == 3) // BSS or DATA
                {
					if (reader.Position < reader.Size && (reader.PeekDword() & 0x3FFFFFFF) != 0) // a size follows -> no BSS but DATA
					{
						hunkSize = reader.ReadDword() & 0x3FFFFFFF;

						if (hunkSize * 4 != hunkSizes[hunkSizeIndex])
							throw new AmbermoonException(ExceptionScope.Data, "Invalid hunk data size.");

						hunks.Add(new Hunk
						{
							Type = HunkType.Data,
							NumEntries = hunkSize,
							Data = reader.ReadBytes((int)hunkSize * 4)
						});
					}
					else // BSS
                    {
						if (hunkSizeIndex == hunkSizes.Count && reader.Position == reader.Size)
							break;

						hunks.Add(new Hunk
						{
							Type = HunkType.BSS,
							NumEntries = hunkSizes[hunkSizeIndex] / 4
						});
					}

					++hunkSizeIndex;
				}
				else if (flags == 0) // CODE
                {
					if (hunkSize * 4 != hunkSizes[hunkSizeIndex])
						throw new AmbermoonException(ExceptionScope.Data, "Invalid hunk data size.");

					hunks.Add(new Hunk
					{
						Type = HunkType.Code,
						NumEntries = hunkSize,
						Data = reader.ReadBytes((int)hunkSize * 4)
					});

					++hunkSizeIndex;
				}
				else if (flags == 1) // RELOC32
                {
					var entries = new Dictionary<uint, List<uint>>();
					uint numOffsets;

					while ((numOffsets = reader.ReadDword()) != 0)
					{
						uint hunkNumber = reader.ReadDword();
						entries.Add(hunkNumber, new List<uint>((int)numOffsets));
						var list = entries[hunkNumber];

						for (int o = 0; o < numOffsets; ++o)
							list.Add(reader.ReadDword());
					}

					hunks.Add(new Reloc32Hunk
					{
						Type = HunkType.RELOC32,
						Entries = entries
					});
				}

				if (flags != 0 && flags != 3) // add END hunk if necessary
					hunks.Add(new Hunk { Type = HunkType.END });

				if (reader.Position == reader.Size)
					break;
			}

			return hunks;
		}

		public static unsafe byte[] Deplode(IDataReader dataReader, out List<uint> deplodedHunkSizes)
        {
			return Deplode(Read(dataReader, false), out deplodedHunkSizes);
		}

		static unsafe byte[] Deplode(List<IHunk> imploderHunks, out List<uint> deplodedHunkSizes)
        {
			deplodedHunkSizes = null;

			var lastCodeHunk = (Hunk)imploderHunks.Last(h => h.Type == HunkType.Code);
			var dataHunk = (Hunk)imploderHunks.Last(h => h.Type == HunkType.Data);

			// Values are located at offset 0x188 in last code hunk.
			// The bit length (last 12 bytes) can have a special encoding.
			// If smaller than 8 the normal value is stored (e.g. 0x07).
			// But if 8 or more the lenght is encoded by subtracting 8 and setting the most significant bit to 1.
			// Examples:
			// - bitlength = 8 -> stored as 0x80
			// - bitlength = 9 -> stored as 0x81
			// In those cases first a full byte from the input stream is read and then continued with the remaining
			// bits from the bit buffer.
			// Example with bitlength 9 (0x81):
			//  result = (read_byte() << 1) | read_bit_from_bit_buffer();
			var table = new byte[8 * 2 + 12 * 1];
			Buffer.BlockCopy(lastCodeHunk.Data, 0x188, table, 0, table.Length);
			var bssHunks = imploderHunks.Where(h => h.Type == HunkType.BSS).ToList();
			deplodedHunkSizes = bssHunks.Take(bssHunks.Count - 1).Select(h => ((Hunk)h).NumEntries * 4).ToList();
			var data = dataHunk.Data;
			uint firstLiteralLength = ((uint)lastCodeHunk.Data[0x1E6] << 8) | lastCodeHunk.Data[0x1E7];
			byte initialBitBuffer = lastCodeHunk.Data[0x1E8];
			uint dataSize = ((uint)lastCodeHunk.Data[0x08] << 24) | ((uint)lastCodeHunk.Data[0x09] << 16) | ((uint)lastCodeHunk.Data[0x0A] << 8) | lastCodeHunk.Data[0x00B];

			var buffer = new Deploder.IncreaseBuffer(data.Length); // deploded data is at least the size of the imploded data

			fixed (byte* ptr = &data[0])
			{
				if (!Deploder.Deplode(ptr, buffer, table, dataSize, firstLiteralLength, initialBitBuffer))
					throw new AmbermoonException(ExceptionScope.Data, "Invalid imploded data.");
			}

			return buffer.ToArray();
        }
	}
}
