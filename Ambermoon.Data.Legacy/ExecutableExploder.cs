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
        }

		enum HunkType
        {
			Code = 0x3E9,
			Data = 0x3EA,
			BSS = 0x3EB,
			RELOC32 = 0x3EC,
			END = 0x3F2
        }

		static byte[] explode_literal_base = { 6, 10, 10, 18 };
		static byte[] explode_literal_extra_bits = { 1, 1, 1, 1, 2, 3, 3, 4, 4, 5, 7, 14 };

		unsafe byte* src;
		unsafe byte* cmpr_data;
		uint write_pos, src_size, src_end /*a4*/, token_run_len /*d2*/;
		int cmpr_pos /*a3*/;

		byte token;

		ushort[] run_base_off_tbl = new ushort[8];
		byte[] run_extra_bits_tbl = new byte[12];

		unsafe static void copy_bytes(byte* dst, byte* src, int count)
		{
			for (int i = 0; i < count; i++)
			{
				dst[i] = src[i];
			}
		}

		unsafe static ushort read_word(byte* buf)
		{
			return (ushort)((buf[0] << 8) | buf[1]);
		}

		unsafe static uint read_dword(byte* buf)
		{
			return (uint)((read_word(&buf[0]) << 16) | read_word(&buf[2]));
		}

		unsafe static void write_word(byte* dst, ushort value)
		{
			dst[0] = (byte)((value >> 8) & 0xFF);
			dst[1] = (byte)((value >> 0) & 0xFF);
		}

		unsafe static void write_dword(byte* dst, uint value)
		{
			write_word(&dst[0], (ushort)((value >> 16) & 0xFFFF));
			write_word(&dst[2], (ushort)((value >> 0) & 0xFFFF));
		}

		public unsafe byte[] Explode(IDataReader dataReader)
        {
			// TODO: REMOVE
			dataReader = new DataReader(System.IO.File.ReadAllBytes(@"C:\Projects\ambermoon.net\FileSpecs\Extract\decoded\AM2_CPU"));

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

			byte[] data = null;
			uint[] hunkSizes = new uint[numHunks];

			for (int i = 0; i < numHunks; ++i)
            {
				var hunkSize = dataReader.ReadDword();
				var hunkMemFlags = hunkSize >> 29;

				if (hunkMemFlags == 3) // skip extended mem flags
					dataReader.Position += 4;

				hunkSizes[i] = hunkSize & 0x3FFFFFFF;

			}

			Hunk[] hunks = new Hunk[numHunks];

			for (int i = 0; i < numHunks; ++i)
			{
				var hunkHeader = dataReader.ReadDword();
				var hunkMemFlags = hunkHeader >> 29;

				if (hunkMemFlags == 3) // skip extended mem flags
					dataReader.Position += 4;

				hunks[i] = new Hunk
				{
					Type = (HunkType)(hunkHeader & 0x3FFFFFFF)
				};

				switch (hunks[i].Type)
                {
					case HunkType.Code:
					case HunkType.Data:
						{
							uint numEntries = dataReader.ReadDword();
							hunks[i].NumEntries = numEntries;
							Console.WriteLine($"Hunk{i:00}: {hunks[i].Type}, Size: {4 + numEntries * 4}, NumEntries: {numEntries}");

							if (hunks[i].Type == HunkType.Code)
								System.IO.File.WriteAllBytes($@"C:\Projects\ambermoon.net\FileSpecs\Extract\decoded\cruncher{i}.code", dataReader.ReadBytes((int)numEntries * 4));
							else
							{
								data = dataReader.ReadBytes((int)numEntries * 4);
								System.IO.File.WriteAllBytes(@"C:\Projects\ambermoon.net\FileSpecs\Extract\decoded\crunch.dat", data);
							}
						}
						break;
					case HunkType.BSS:
						{
							uint allocSize = dataReader.ReadDword();
							hunks[i].NumEntries = allocSize;
							Console.WriteLine($"Hunk{i:00}: {hunks[i].Type}, Size: 4, AllocSize: {allocSize * 4}");
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

							Console.WriteLine($"Hunk{i:00}: {hunks[i].Type}, Size: {size - 4}, Num offsets: {totalOffsets}");
							++numHunks;
						}
						break;
				}

				if (dataReader.ReadDword() != (uint)HunkType.END)
					dataReader.Position -= 4; // a hunk may end with HUNK_END (0x000003F2)
			}

			uint totalSize = (uint)hunks.Where(h => h.Type == HunkType.BSS).Take(5).Select(h => h.NumEntries * 4).Sum(x => x);

			byte[] explodedData = new byte[totalSize];
			Buffer.BlockCopy(data, 0, explodedData, 0, data.Length);

			var table = new byte[8 * 2 + 12] // TODO
			{
				0, 0, 0, 0, 0, 32, 0, 32, 0, 64, 0, 64, 0, 128, 0, 128,
				6, 7, 7, 10, 6, 7, 7, 10, 7, 8, 9, 10
			};

			fixed (byte* ptr = &explodedData[0])
			{
				Explode(ptr, table, (uint)data.Length, totalSize);
            }

			System.IO.File.WriteAllBytes(@"C:\Projects\ambermoon.net\FileSpecs\Extract\decoded\exploded.dat", explodedData);

			throw new Exception("FOO");

			/*fixed (byte* ptr = &data[0])
            {
				uint size = exploded_size(ptr);
				byte[] result = new byte[size];
				Buffer.BlockCopy(data, 0, result, 0, data.Length);
				if (explode(ptr) != size)
					throw new AmbermoonException(ExceptionScope.Data, "Invalid imploded file.");
				return result;
            }*/
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
		unsafe bool Explode(byte* buffer, byte[] table, uint implodedSize, uint explodedSize)
		{
			byte* input = buffer + implodedSize - 3; /* input pointer  */
			byte* output = buffer + explodedSize;   /* output pointer */
			byte* match;                      /* match pointer  */
			byte bitBuffer;
			uint literalLength;
			uint matchLength;
			uint selector, x, y;
			uint[] matchBase = new uint[8];
			int nbit = 0;

			uint ReadBits(byte count)
			{
				uint result = 0;

				for (int i = 0; i < count; i++)
				{
					byte bit = (byte)(bitBuffer >> 7);
					bitBuffer <<= 1;
					/*++nbit;

					if (nbit == 8)
                    {
						nbit = 0;
						bitBuffer = *--input;
					}*/

					if (bitBuffer == 0)
					{
						byte temp = bit;
						bitBuffer = *(--input);
						bit = (byte)(bitBuffer >> 7);
						bitBuffer <<= 1;
						if (temp != 0)
							++bitBuffer;
						//bit = temp;
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

			/* get initial bit buffer contents, and first literal length */
			/*if ((implodedSize & 1) != 0)
			{
				bitBuffer = input[4];
				literalLength = (uint)((input[0] << 24) | (input[1] << 16) | (input[2] << 8) | input[3]);
			}
			else
			{
				bitBuffer = input[0];
				literalLength = (uint)((input[1] << 24) | (input[2] << 16) | (input[3] << 8) | input[4]);
			}*/

			literalLength = 0x3c;
			bitBuffer = 0xa2;// 0xa0;
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
						y = explode_literal_base[x];
						x += 8;
					}
					else // 10
					{
						y = 2;
						x += 4;
					}
				}
				x = explode_literal_extra_bits[x];

				/* next literal run length: read [x] bits and add [y] */
				literalLength = 0;
				for (i = 0; i < x; ++i)
				{
					literalLength <<= 1;
					if (ReadBits(1) != 0)
						literalLength++;
				}
				literalLength += y;

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
				y = 0;
				for (i = 0; i < x; ++i)
				{
					y <<= 1;
					if (ReadBits(1) != 0)
						y++;
				}
				match += y;

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
