using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.GameDataRepository
{
    using Enumerations;
    using System.Runtime.CompilerServices;

    public sealed partial record GameDataInfo : INotifyPropertyChanged
    {

        #region Fields

        private bool _advanced;
        private string _version;
        private DateTime _releaseDate;
        private GameDataLanguage _language;

        #endregion


        #region Properties

        public bool Advanced
        {
            get => _advanced;
            set => SetField(ref _advanced, value);
        }

        public string Version
        {
            get => _version;
            set => SetField(ref _version, value);
        }

        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set => SetField(ref _releaseDate, value);
        }

        public GameDataLanguage Language
        {
            get => _language;
            set => SetField(ref _language, value);
        }

        #endregion


        #region Constructors

        internal GameDataInfo(string versionString, string dateAndLanguageString)
        {
            var versionRegex = VersionRegex();
            var versionMatch = versionRegex.Match(versionString);
            _advanced = versionMatch.Groups.Count >= 1 && versionMatch.Groups[1].Value.ToLower().Contains("adv");
            _version = versionMatch.Groups.Count < 2 ? "Unknown version" : versionMatch.Groups[2].Value;
            var dateLanguageRegex = DateLanguageRegex();
            var dateLanguageMatch = dateLanguageRegex.Match(dateAndLanguageString);
            string? releaseDataString = dateLanguageMatch.Groups.Count < 1 ? null : dateLanguageMatch.Groups[1].Value;
            if (releaseDataString is not null)
            {
                // Convert 01.01.99 or 01-01-99 to 01/01/99
                releaseDataString = releaseDataString.Replace('.', '/').Replace('-', '/');
                try
                {
                    string format = releaseDataString.Length == 8 ? "dd/MM/yy" : "dd/MM/yyyy";
                    _releaseDate = DateTime.ParseExact(releaseDataString, format, CultureInfo.InvariantCulture);
                }
                catch
                {
                    _releaseDate = DateTime.Today;
                }
            }
            else
            {
                _releaseDate = DateTime.Today;
            }
            _language = dateLanguageMatch.Groups.Count < 2 ? GameDataLanguage.Unknown : dateLanguageMatch.Groups[2].Value.ToLower() switch
            {
                // Make lowercase
                "english" => GameDataLanguage.English,
                "german" => GameDataLanguage.German,
                "deutsch" => GameDataLanguage.German,
                "french" => GameDataLanguage.French,
                "français" => GameDataLanguage.French,
                "spanish" => GameDataLanguage.Spanish,
                "italian" => GameDataLanguage.Italian,
                "czech" => GameDataLanguage.Czech,
                "český" => GameDataLanguage.Czech,
                "polish" => GameDataLanguage.Polish,
                "polski" => GameDataLanguage.Polish,
                "russian" => GameDataLanguage.Russian,
                "русский" => GameDataLanguage.Russian,
                "hungarian" => GameDataLanguage.Hungarian,
                "magyar" => GameDataLanguage.Hungarian,
                "dutch" => GameDataLanguage.Dutch,
                "nederlands" => GameDataLanguage.Dutch,
                "swedish" => GameDataLanguage.Swedish,
                "finnish" => GameDataLanguage.Finnish,
                "danish" => GameDataLanguage.Danish,
                "norwegian" => GameDataLanguage.Norwegian,
                "portuguese" => GameDataLanguage.Portuguese,
                "slovak" => GameDataLanguage.Slovak,
                "slovenian" => GameDataLanguage.Slovenian,
                "croatian" => GameDataLanguage.Croatian,
                "serbian" => GameDataLanguage.Serbian,
                "bulgarian" => GameDataLanguage.Bulgarian,
                "romanian" => GameDataLanguage.Romanian,
                "greek" => GameDataLanguage.Greek,
                "turkish" => GameDataLanguage.Turkish,
                _ => GameDataLanguage.Unknown,
            };
        }

        #endregion


        #region Equality

        public override int GetHashCode()
        {
            return HashCode.Combine(Language, Version, Advanced);
        }

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion


        #region Regex

        [GeneratedRegex("([A-Za-z ]+) ([vV][0-9][.][0-9]{1,2})")]
        private static partial Regex VersionRegex();

        [GeneratedRegex("([0-9]{1,2}[-.][0-9]{1,2}[-.][0-9]{2,4})[ ]+[/][ ]+([A-Za-z]+)")]
        private static partial Regex DateLanguageRegex();

        #endregion

    }
}
