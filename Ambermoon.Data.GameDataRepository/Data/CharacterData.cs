using Ambermoon.Data.Legacy;
using System.ComponentModel.DataAnnotations;
using Ambermoon.Data.GameDataRepository.Util;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public abstract class CharacterData : IIndexed, IMutableIndex, IEquatable<CharacterData>
    {

        #region Fields

        private string _name = string.Empty;
        private uint _level = 1;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [StringLength(15)]
        public string Name
        {
            get => _name;
            set
            {
                ValueChecker.Check(value, 15);
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
                ValueChecker.Check(value, 1, byte.MaxValue);
                _level = value;
            }
        }

        #endregion


        #region Equality

        public bool Equals(CharacterData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _name == other._name &&
                   _level == other._level &&
                   Index == other.Index &&
                   Type == other.Type &&
                   Gender == other.Gender &&
                   Class == other.Class &&
                   Race == other.Race;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CharacterData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(CharacterData? left, CharacterData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CharacterData? left, CharacterData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public abstract object Clone();


        #endregion

    }
}
