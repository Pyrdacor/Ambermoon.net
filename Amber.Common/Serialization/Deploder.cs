namespace Amber.Serialization
{
    public static class Deploder
    {
		internal class BufferIterator
		{
			readonly IncreaseBuffer buffer;
			int offset;

			public BufferIterator(IncreaseBuffer buffer, int offset)
			{
				this.buffer = buffer;
				this.offset = offset;
			}

			public static BufferIterator operator +(BufferIterator iter, int amount)
			{
				return new BufferIterator(iter.buffer, iter.offset + amount);
			}

			public static BufferIterator operator -(BufferIterator iter, int amount)
			{
				return new BufferIterator(iter.buffer, iter.offset - amount);
			}

			public static implicit operator byte(BufferIterator iter) => iter.buffer.GetByte(iter.offset);

			public void Assign(byte value) => buffer.SetByte(offset, value);

			public void AssignAndIncrease(byte value) => buffer.SetByte(offset++, value);
			public byte GetAndIncrease() => buffer.GetByte(offset++);
		}

		internal class IncreaseBuffer
		{
			readonly List<byte> data;

			public IncreaseBuffer(int size)
			{
				data = new List<byte>(size);

				if (size != 0)
					data.AddRange(Enumerable.Repeat((byte)0, size));
			}

			public BufferIterator this[int index] => new BufferIterator(this, index);
			public byte GetByte(int index) => data[index];
			public void SetByte(int index, byte value)
			{
				if (index == data.Count)
					data.Add(value);
				else if (index > data.Count)
					throw new IndexOutOfRangeException("Index was out of range.");
				else
					data[index] = value;
			}
			public int Size => data.Count;
			public byte[] ToArray() => data.ToArray();
		}

		static readonly byte[] DeplodeLiteralBase = { 6, 10, 10, 18 };
		static readonly byte[] DeplodeLiteralExtraBits = { 1, 1, 1, 1, 2, 3, 3, 4, 4, 5, 7, 14 };

		public static unsafe byte[] DeplodeFimp(DataReader dataReader)
        {
			if (dataReader.PeekDword() != 0x494d5021) // "IMP!"
				throw new AmberException(ExceptionScope.Data, "No valid IMP data");

			int position = dataReader.Position;
			dataReader.Position += 4; // skip header
			int explodedSize = (int)(dataReader.ReadDword() & int.MaxValue);
			int implodedSize = (int)(dataReader.ReadDword() & int.MaxValue);
			var destBuffer = new IncreaseBuffer(explodedSize);
			dataReader.Position = dataReader.Position - 12 + implodedSize;
			byte[] initialData = dataReader.ReadBytes(12);
			var firstLiteralLength = dataReader.ReadDword();
			bool evenData = (dataReader.ReadByte() & 0x80) != 0; // bit 0x80 means even data
			byte initialBitBuffer = dataReader.ReadByte();
			byte[] table = dataReader.ReadBytes(8 * 2 + 12 * 1);
			int footerSize = 12 + 4 + 2 + 8 * 2 + 12 * 1 + 4; // the last 4 bytes are a checksum but we don't care about it

			if (!evenData)
			{
				++footerSize;
				--implodedSize;
			}

			byte[] preparedData = new byte[implodedSize];
			for (int i = 0; i < 3; ++i)
				Buffer.BlockCopy(initialData, i * 4, preparedData, (2 - i) * 4, 4);
			dataReader.Position = position + 12;
			Buffer.BlockCopy(dataReader.ReadBytes(implodedSize - 12), 0, preparedData, 12, implodedSize - 12);
			dataReader.Position += footerSize;

			fixed (byte* ptr = preparedData)
			{
				if (!Deplode(ptr, destBuffer, table, (uint)implodedSize, firstLiteralLength, initialBitBuffer))
					throw new AmberException(ExceptionScope.Data, "Error exploding data");
			}

			if (destBuffer.Size != explodedSize)
				throw new AmberException(ExceptionScope.Data, "Exploded size does not match the value in the header");

			return destBuffer.ToArray();
		}

		public static unsafe byte[] DeplodeFimp(byte[] sourceBuffer, int offset)
		{
			return DeplodeFimp(new DataReader(sourceBuffer, offset));
		}

		internal static unsafe bool Deplode(byte* sourceBuffer, IncreaseBuffer destBuffer, byte[] table,
			uint implodedSize, uint firstLiteralLength, byte initialBitBuffer)
		{
			byte* input = sourceBuffer + implodedSize; /* input pointer  */
			BufferIterator output = destBuffer[0]; /* output pointer */
			BufferIterator match; /* match pointer  */
			byte bitBuffer;
			uint literalLength;
			uint matchLength;
			uint selector, x, y;
			int[] matchBase = new int[8];

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
				matchBase[x] = ((table[x * 2] << 8) | table[x * 2 + 1]);
			}

			literalLength = firstLiteralLength; // word at offset 0x1E6 in the last code hunk
			bitBuffer = initialBitBuffer; // byte at offset 0x1E8 in the last code hunk
			int i;

			while (true)
			{
				/* copy literal run */
				for (i = 0; i < literalLength; ++i)
					output.AssignAndIncrease(*--input);

				/* main exit point - after the literal copy */
				if (input <= sourceBuffer)
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
						y = DeplodeLiteralBase[x];
						x += 8;
					}
					else // 10
					{
						y = 2;
						x += 4;
					}
				}
				x = DeplodeLiteralExtraBits[x];

				/* next literal run length: read [x] bits and add [y] */
				literalLength = y + ReadBits(x);

				/* another Huffman tuple, for deciding the match distance: _base and
				 * _extra are from the explosion table, as passed into the deplode
				 * function.
				 *
				 * 0  -> base = 1                        extra = _extra[selector + 0]
				 * 10 -> base = 1 + _base[selector + 0]  extra = _extra[selector + 4]
				 * 11 -> base = 1 + _base[selector + 4]  extra = _extra[selector + 8]
				 */
				match = output - 1;
				x = selector;
				if (ReadBits(1) != 0)
				{
					if (ReadBits(1) != 0)
					{
						match -= matchBase[selector + 4];
						x += 8;
					}
					else
					{
						match -= matchBase[selector];
						x += 4;
					}
				}
				x = table[x + 16];

				/* obtain the value of the next [x] extra bits and
				 * add it to the match offset */
				match -= (int)ReadBits(x);


				/* copy match */
				for (i = 0; i < matchLength + 1; ++i)
					output.AssignAndIncrease(match.GetAndIncrease());
			}


			/* return true if we used up all input bytes (as we should) */
			return input == sourceBuffer || (implodedSize % 2 == 1 && sourceBuffer - input == 1);
		}
	}
}
