using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Util;

    public abstract class CharacterData : IIndexed, IMutableIndex, IEquatable<CharacterData>, INotifyPropertyChanged, ICloneable
    {

        #region Fields

        private string _name = string.Empty;
        private uint _level = 1;
        private Gender _gender;
        private Class _class;
        private Race _race;

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
                SetField(ref _name, value);
            }
        }

        public abstract CharacterType Type { get; }

        public Gender Gender
        {
            get => _gender;
            set => SetField(ref _gender, value);
        }

        public Class Class
        {
            get => _class;
            set => SetField(ref _class, value);
        }

        public Race Race
        {
            get => _race;
            set => SetField(ref _race, value);
        }

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
                SetField(ref _level, value);
            }
        }

        #endregion


        #region Constructors

        private protected CharacterData()
        {

        }

        #endregion


        #region Equality

        public bool Equals(CharacterData? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   Type == other.Type &&
                   _name == other._name &&
                   _level == other._level &&                   
                   _gender == other._gender &&
                   _class == other._class &&
                   _race == other._race;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
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


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
