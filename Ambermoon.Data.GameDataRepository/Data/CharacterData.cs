using Ambermoon.Data.Legacy;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public abstract class CharacterData : IIndexed
    {
        private string _name = string.Empty;
        private uint _level = 1;     

        public uint Index { get; private protected set; }

        [StringLength(15)]
        public string Name
        {
            get => _name;
            set
            {
                if (new AmbermoonEncoding().GetByteCount(value) > 15)
                    throw new ArgumentOutOfRangeException(nameof(Name), "Name length is limited to 15 single-byte characters.");

                _name = value;
            }
        }

        public abstract CharacterType Type { get; }

        public Gender Gender { get; set; }

        public Class Class { get; set; }

        public Race Race { get; set; }

        /// <summary>
        /// Level of the character.
        /// 
        /// Note: While this is capped to 50 for party members
        /// it seems to go beyond at least for some monsters.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint Level
        {
            get => _level;
            set
            {
                if (value == 0 || value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(Level), $"Level is limited to the range 1 to {byte.MaxValue}.");

                _level = value;
            }
        }
    }
}
