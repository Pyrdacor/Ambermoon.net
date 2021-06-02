using System;

namespace SonicArranger
{
	public struct Note
	{
		[Flags]
		public enum Flags
        {
			/// <summary>
			/// If set the note's value can't be changed
			/// by a voice's note transpose.
			/// </summary>
			DisableNoteTranspose = 4,
			/// <summary>
			/// If set the note's instrument can't be changed
			/// by a voice's sound transpose.
			/// </summary>
			DisableSoundTranspose = 8
        }

		public byte Value { get; private set; }
		public byte Instrument { get; private set; }
		public Flags NoteFlags { get; private set; }
		public byte Command { get; private set; }
		public byte CommandInfo { get; private set; }

		internal Note(ICustomReader reader) : this()
		{
			Value = reader.ReadByte();
			Instrument = reader.ReadByte();
			Command = reader.ReadByte();
			NoteFlags = (Flags)(Command >> 4);
			Command &= 0xf;
			CommandInfo = reader.ReadByte();
		}
	}
}
