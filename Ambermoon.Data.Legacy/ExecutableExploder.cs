using System;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
	/**
	 * The imploder creates
	 * - 1 first code chunk
	 * - n BSS chunks (one for each in the exploded file except for RELOC32 hunks) which allocate empty memory
	 * - then another code chunk
	 * - then a data chunk
	 * - and finally another BSS chunk (I guess for internal stuff)
	 * 
	 * The hunk sizes of the exploded hunks match the sizes of the BSS allocations.
	 * 
	 * AM2_CPU has 8 chunks:
	 * - Code
	 * - Reloc32
	 * - 2x BSS
	 * - 2x Data (this is what we want!)
	 * - Another Reloc32
	 * - Another BSS
	 * 
	 * The Reloc32 hunks are not reflected in the imploder BSS chunks.
	 * 
	 * If we are only interested in the data (and maybe code) hunks of AM2_CPU
	 * we can just get the file sizes from the first, fourth and fifth BSS chunk
	 * which are the exploded sizes (without the header -> number of dwords).
	 */

    internal class ExecutableExploder
    {
		struct Hunk
        {
			public HunkType Type;
			public uint NumEntries;
			public byte[] Data;
        }

		enum HunkType
        {
			Code = 0x3E9,
			Data = 0x3EA,
			BSS = 0x3EB,
			RELOC32 = 0x3EC,
			END = 0x3F2
        }

		static readonly byte[] ExplodeLiteralBase = { 6, 10, 10, 18 };
		static readonly byte[] ExplodeLiteralExtraBits = { 1, 1, 1, 1, 2, 3, 3, 4, 4, 5, 7, 14 };

		public unsafe byte[] Explode(IDataReader dataReader)
        {
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

			Hunk[] hunks = new Hunk[numHunks];
			int j = 0;

			for (int i = 0; i < numHunks; ++i)
			{
				var type = (HunkType)dataReader.ReadDword();

				if (type != HunkType.RELOC32 && type != HunkType.END)
				{
					hunks[j] = new Hunk
					{
						Type = type
					};
				}

				switch (type)
                {
					case HunkType.Code:
					case HunkType.Data:
						{
							uint numEntries = dataReader.ReadDword();
							hunks[j].NumEntries = numEntries;
							hunks[j].Data = dataReader.ReadBytes((int)numEntries * 4);
							Console.WriteLine($"Hunk{i:00}: {type}, Size: {4 + numEntries * 4}, NumEntries: {numEntries}");
							++j;
						}
						break;
					case HunkType.BSS:
						{
							uint allocSize = dataReader.ReadDword();
							hunks[j].NumEntries = allocSize;
							Console.WriteLine($"Hunk{i:00}: {type}, Size: 4, AllocSize: {allocSize * 4}");
							++j;
						}
						break;
					case HunkType.RELOC32:
                        {
							uint size = 4;
							uint totalOffsets = 0;
							uint numOffsets;
							
							while ((numOffsets = dataReader.ReadDword()) != 0)
                            {
								size += 8 + numOffsets * 4;
								totalOffsets += numOffsets;
								dataReader.Position += 4 + (int)numOffsets * 4;
                            }

							Console.WriteLine($"Hunk{i:00}: {type}, Size: {size - 4}, Num offsets: {totalOffsets}");
							++numHunks;
						}
						break;
					case HunkType.END:
						{
							Console.WriteLine($"Hunk{i:00}: {type}");
							++numHunks;
						}
						break;
					default:
						throw new AmbermoonException(ExceptionScope.Data, $"Unsupported hunk type: {type}.");
				}

				if (dataReader.ReadDword() != (uint)HunkType.END)
					dataReader.Position -= 4; // a hunk may end with HUNK_END (0x000003F2)
			}

			var lastCodeHunk = hunks.LastOrDefault(h => h.Type == HunkType.Code);
			var dataHunk = hunks.LastOrDefault(h => h.Type == HunkType.Data);

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
			/*var table = new byte[8 * 2 + 12 * 1]
			{
				0, 64, // 64 (0x0040)
				0, 128, // 128 (0x0080)
				0, 128, // 128 (0x0080)
				1, 0, // 256 (0x0100)
				0, 192, // 192 (0x00C0)
				2, 128, // 640 (0x0280)
				4, 128, // 1152 (0x0480)
				9, 0, // 2304 (0x0900)
				0x06, 0x07, 0x07, 0x80, 0x07, 0x81, 0x82, 0x83, 0x80, 0x83, 0x85, 0x86
			};*/
			var table = new byte[8 * 2 + 12 * 1];
			Buffer.BlockCopy(lastCodeHunk.Data, 0x188, table, 0, table.Length);

			var bssHunks = hunks.Where(h => h.Type == HunkType.BSS).ToList();
			var data = dataHunk.Data;
			uint firstLiteralLength = ((uint)lastCodeHunk.Data[0x1E6] << 8) | lastCodeHunk.Data[0x1E7];
			byte initialBitBuffer = lastCodeHunk.Data[0x1E8];
			// TODO: The total size needs some more research
			/*var totalSize = (uint)bssHunks.Take(bssHunks.Count - 1) // skip last BSS hunk as it is only a temp buffer hunk
				.Select(h => h.NumEntries * 4).Sum(x => x);*/
			var totalSize = ((uint)lastCodeHunk.Data[0x1D] << 16) | ((uint)lastCodeHunk.Data[0x1E] << 8) | lastCodeHunk.Data[0x1F];
			byte[] explodedData = new byte[totalSize];
			Buffer.BlockCopy(data, 0, explodedData, 0, data.Length);

			fixed (byte* ptr = &explodedData[0])
			{
				// TODO: This does not read everything (the size does not fit I guess). But it's good enough for now.
				/*if (!*/
				Explode(ptr, table, (uint)data.Length, (uint)explodedData.Length, firstLiteralLength, initialBitBuffer)/*)*/;
					//throw new AmbermoonException(ExceptionScope.Data, "Invalid imploded data.");
			}

			byte[] reversedData = new byte[explodedData.Length];
			int dataSize = reversedData.Length - 1;

			for (int i = 0; i < explodedData.Length; ++i)
				reversedData[dataSize - i] = explodedData[i];

			return reversedData;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer">A buffer that is large enough to contain all the decompressed data.
		/// On entry, the buffer should contain the entire compressed data at offset 0.
		/// On successful exit, the buffer will contain the decompressed data at offset 0.</param>
		/// <param name="table">Explosion table, consisting of 8 16-bit big-endian "base offset" values and
		/// 12 8-bit "extra bits" values.</param>
		/// <param name="implodedSize">Compressed size in bytes</param>
		/// <param name="explodedSize">Decompressed size in bytes</param>
		/// <returns></returns>
		unsafe bool Explode(byte* buffer, byte[] table, uint implodedSize, uint explodedSize, uint firstLiteralLength, byte initialBitBuffer)
		{
			byte* input = buffer + implodedSize - 3; /* input pointer  */
			byte* output = buffer + explodedSize; /* output pointer */
			byte* match; /* match pointer  */
			byte bitBuffer;
			uint literalLength;
			uint matchLength;
			uint selector, x, y;
			uint[] matchBase = new uint[8];

			uint ReadBits(uint count)
			{
				uint result = 0;

				if ((count & 0x80) != 0)
				{
					result = *(--input);
					count &= 0x7f;
				}

				for (int i = 0; i < count; i++)
				{
					byte bit = (byte)(bitBuffer >> 7);
					bitBuffer <<= 1;

					if (bitBuffer == 0)
					{
						byte temp = bit;
						bitBuffer = *(--input);
						bit = (byte)(bitBuffer >> 7);
						bitBuffer <<= 1;
						if (temp != 0)
							++bitBuffer;
					}

					result <<= 1;
					result |= bit;
				}

				return result;
			}

			/* read the 'base' part of the explosion table into native byte order,
			 * for speed */
			for (x = 0; x < 8; x++)
			{
				matchBase[x] = (uint)((table[x * 2] << 8) | table[x * 2 + 1]);
			}

			literalLength = firstLiteralLength; // word at offset 0x1E6 in the last code hunk
			bitBuffer = initialBitBuffer; // byte at offset 0x1E8 in the last code hunk
			int i;

			while (true)
			{
				/* copy literal run */
				if ((output - buffer) < literalLength)
					return false; /* enough space? */

				for (i = 0; i < literalLength; ++i)
					*--output = *--input;

				/* main exit point - after the literal copy */
				if (output <= buffer)
					break;

				/* static Huffman encoding of the match length and selector: 
				 * 
				 * 0     -> selector = 0, match_len = 1
				 * 10    -> selector = 1, match_len = 2
				 * 110   -> selector = 2, match_len = 3
				 * 1110  -> selector = 3, match_len = 4
				 * 11110 -> selector = 3, match_len = 5 + next three bits (5-12)
				 * 11111 -> selector = 3, match_len = (next input byte)-1 (0-254)
				 * 
				 */
				if (ReadBits(1) != 0)
				{
					if (ReadBits(1) != 0)
					{
						if (ReadBits(1) != 0)
						{
							selector = 3;

							if (ReadBits(1) != 0)
							{
								if (ReadBits(1) != 0) // 11111
								{
									matchLength = *--input;

									if (matchLength == 0)
										return false; /* bad input */

									matchLength--;
								}
								else // 11110
								{
									matchLength = 5 + ReadBits(3);
								}
							}
							else // 1110
							{
								matchLength = 4;
							}
						}
						else // 110
						{
							selector = 2;
							matchLength = 3;
						}
					}
					else // 10
					{
						selector = 1;
						matchLength = 2;
					}
				}
				else // 0
				{
					selector = 0;
					matchLength = 1;
				}

				/* another Huffman tuple, for deciding the base value (y) and number
				 * of extra bits required from the input stream (x) to create the
				 * length of the next literal run. Selector is 0-3, as previously
				 * obtained.
				 *
				 * 0  -> base = 0,                      extra = {1,1,1,1}[selector]
				 * 10 -> base = 2,                      extra = {2,3,3,4}[selector]
				 * 11 -> base = {6,10,10,18}[selector]  extra = {4,5,7,14}[selector]
				 */
				y = 0;
				x = selector;
				if (ReadBits(1) != 0)
				{
					if (ReadBits(1) != 0) // 11
					{
						y = ExplodeLiteralBase[x];
						x += 8;
					}
					else // 10
					{
						y = 2;
						x += 4;
					}
				}
				x = ExplodeLiteralExtraBits[x];

				/* next literal run length: read [x] bits and add [y] */
				literalLength = y + ReadBits(x);

				/* another Huffman tuple, for deciding the match distance: _base and
				 * _extra are from the explosion table, as passed into the explode
				 * function.
				 *
				 * 0  -> base = 1                        extra = _extra[selector + 0]
				 * 10 -> base = 1 + _base[selector + 0]  extra = _extra[selector + 4]
				 * 11 -> base = 1 + _base[selector + 4]  extra = _extra[selector + 8]
				 */
				match = output + 1;
				x = selector;
				if (ReadBits(1) != 0)
				{
					if (ReadBits(1) != 0)
					{
						match += matchBase[selector + 4];
						x += 8;
					}
					else
					{
						match += matchBase[selector];
						x += 4;
					}
				}
				x = table[x + 16];

				/* obtain the value of the next [x] extra bits and
				 * add it to the match offset */
				match += ReadBits(x);


				/* copy match */
				if ((output - buffer) < matchLength)
					return false; /* enough space? */

				for (i = 0; i < matchLength + 1; ++i)
					*--output = *--match;
			}

			/* return true if we used up all input bytes (as we should) */
			return input == buffer;
		}
	}
}
