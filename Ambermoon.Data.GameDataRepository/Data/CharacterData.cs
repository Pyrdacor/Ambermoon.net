using Ambermoon.Data.Legacy;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public abstract class CharacterData : IIndexed
    {
        private string _name = string.Empty;
        private uint _level = 1;
        private uint _age = 1;
        private uint _maxAge = 1;        

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

        [Range(1, 50)]
        public uint Level
        {
            get => _level;
            set
            {
                if (value == 0 || value > 50)
                    throw new ArgumentOutOfRangeException(nameof(Level), "Level is limited to the range 1 to 50.");

                _level = value;
            }
        }

        [Range(1, ushort.MaxValue)]
        public uint Age
        {
            get => _age;
            set
            {
                if (value == 0 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(Age), $"Age is limited to the range 1 to {ushort.MaxValue}.");

                _age = value;
            }
        }

        [Range(1, ushort.MaxValue)]
        public uint MaxAge
        {
            get => _maxAge;
            set
            {
                if (value == 0 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(MaxAge), $"Max age is limited to the range 1 to {ushort.MaxValue}.");

                _maxAge = value;
            }
        }
    }
}
