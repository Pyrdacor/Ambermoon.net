using Ambermoon.Data;
using Ambermoon.Data.Enumerations;

namespace Ambermoon;

/// <summary>
/// Texts which are only spoken by the screen reader.
/// The arrays are indexed by the game language (English, German, French, Polish, Czech).
/// </summary>
public static class ScreenReaderTexts
{
    static string Get(GameLanguage language, string[] texts) => texts[(int)language];

    static readonly string[][] FullClassNames =
    [
        ["Adventurer", "Warrior", "Paladin", "Thief", "Ranger", "Healer", "Alchemist", "Mystic", "Mage", "Animal"],
        ["Abenteurer", "Krieger", "Paladin", "Dieb", "Ranger", "Heiler", "Alchemist", "Mystiker", "Magier", "Tier"],
        ["Aventurier", "Guerrier", "Paladin", "Voleur", "Ranger", "Guérisseur", "Alchimiste", "Mystique", "Mage", "Animal"],
        ["Śmiałek", "Wojownik", "Paladyn", "Rabuś", "Łowca", "Uzdrowiciel", "Alchemik", "Mistyk", "Mag", "Zwierzę"],
        ["Poutník", "Válečník", "Paladin", "Zloděj", "Stopař", "Léčitel", "Alchymista", "Mystik", "Mág", "Zvíře"]
    ];

    static readonly string[][] Directions =
    [
        ["North", "North-east", "East", "South-east", "South", "South-west", "West", "North-west"],
        ["Norden", "Nordosten", "Osten", "Südosten", "Süden", "Südwesten", "Westen", "Nordwesten"],
        ["Nord", "Nord-est", "Est", "Sud-est", "Sud", "Sud-ouest", "Ouest", "Nord-ouest"],
        ["Północ", "Północny wschód", "Wschód", "Południowy wschód", "Południe", "Południowy zachód", "Zachód", "Północny zachód"],
        ["Sever", "Severovýchod", "Východ", "Jihovýchod", "Jih", "Jihozápad", "Západ", "Severozápad"]
    ];

    // order of DayTime: Night, Dusk (6-8), Day, Dawn (18-20)
    static readonly string[][] DayTimes =
    [
        ["Night", "Morning", "Day", "Evening"],
        ["Nacht", "Morgen", "Tag", "Abend"],
        ["Nuit", "Matin", "Jour", "Soir"],
        ["Noc", "Ranek", "Dzień", "Wieczór"],
        ["Noc", "Ráno", "Den", "Večer"]
    ];

    static readonly string[] ClockFormat = ["{0}:{1:00}", "{0} Uhr {1}", "{0} h {1:00}", "Godzina {0}:{1:00}", "{0}:{1:00}"];
    static readonly string[] LocationFormat = ["X {0}, Y {1}", "X {0}, Y {1}", "X {0}, Y {1}", "X {0}, Y {1}", "X {0}, Y {1}"];
    static readonly string[] MonsterEyeActive = ["Monster eye: You are seen", "Monsterauge: Ihr werdet gesehen", "Œil de monstre : vous êtes vus", "Oko potwora: jesteście widziani", "Oko příšery: jste viděni"];
    static readonly string[] MonsterEyeInactive = ["Monster eye: You are not seen", "Monsterauge: Ihr werdet nicht gesehen", "Œil de monstre : vous n'êtes pas vus", "Oko potwora: nie jesteście widziani", "Oko příšery: nejste viděni"];
    static readonly string[] WindChain = ["Wind chain", "Windkette", "Chaîne des vents", "Łańcuch wiatru", "Větrný řetěz"];
    static readonly string[] PreviousPortrait = ["Previous portrait", "Vorheriges Porträt", "Portrait précédent", "Poprzedni portret", "Předchozí portrét"];
    static readonly string[] NextPortrait = ["Next portrait", "Nächstes Porträt", "Portrait suivant", "Następny portret", "Další portrét"];
    static readonly string[] PortraitFormat = ["Portrait {0} of {1}", "Porträt {0} von {1}", "Portrait {0} sur {1}", "Portret {0} z {1}", "Portrét {0} z {1}"];
    static readonly string[] NameFormat = ["Name: {0}", "Name: {0}", "Nom : {0}", "Imię: {0}", "Jméno: {0}"];
    static readonly string[] Ok = ["OK", "OK", "OK", "OK", "OK"];
    static readonly string[] SavegameFormat = ["Savegame {0}", "Spielstand {0}", "Sauvegarde {0}", "Zapis {0}", "Uložená hra {0}"];
    static readonly string[] Empty = ["Empty", "Leer", "Vide", "Pusty", "Prázdné"];
    static readonly string[] AmountFormat = ["Amount {0}", "Anzahl {0}", "Quantité {0}", "Ilość {0}", "Počet {0}"];
    static readonly string[] Unlimited = ["unlimited", "unbegrenzt", "illimitée", "nieograniczona", "neomezený"];
    static readonly string[] PriceFormat = ["Price {0}", "Preis {0}", "Prix {0}", "Cena {0}", "Cena {0}"];
    static readonly string[] Cancel = ["Cancel", "Abbrechen", "Annuler", "Anuluj", "Zrušit"];
    static readonly string[] QuestLog = ["Quest log", "Questlog", "Journal de quêtes", "Dziennik zadań", "Deník úkolů"];

    public static string GetFullClassName(GameLanguage language, Class @class)
    {
        var names = FullClassNames[(int)language];
        return (int)@class < names.Length ? names[(int)@class] : @class.ToString();
    }

    /// <summary>
    /// The angle is 0 for north and increases clockwise.
    /// </summary>
    public static string GetDirection(GameLanguage language, int angle)
    {
        int index = ((int)System.Math.Round(angle / 45.0) % 8 + 8) % 8;
        return Directions[(int)language][index];
    }

    public static string GetDayTime(GameLanguage language, DayTime dayTime) => DayTimes[(int)language][(int)dayTime];

    public static string GetClock(GameLanguage language, int hour, int minute) => string.Format(Get(language, ClockFormat), hour, minute);

    public static string GetLocation(GameLanguage language, int x, int y) => string.Format(Get(language, LocationFormat), x, y);

    public static string GetMonsterEye(GameLanguage language, bool active) => Get(language, active ? MonsterEyeActive : MonsterEyeInactive);

    public static string GetWindChain(GameLanguage language) => Get(language, WindChain);

    public static string GetPreviousPortrait(GameLanguage language) => Get(language, PreviousPortrait);

    public static string GetNextPortrait(GameLanguage language) => Get(language, NextPortrait);

    public static string GetPortrait(GameLanguage language, int index, int count) => string.Format(Get(language, PortraitFormat), index, count);

    public static string GetName(GameLanguage language, string name) => string.Format(Get(language, NameFormat), name);

    public static string GetOk(GameLanguage language) => Get(language, Ok);

    public static string GetQuestLog(GameLanguage language) => Get(language, QuestLog);

    public static string GetCancel(GameLanguage language) => Get(language, Cancel);

    public static string GetAmount(GameLanguage language, int amount, bool unlimited) =>
        string.Format(Get(language, AmountFormat), unlimited ? Get(language, Unlimited) : amount.ToString());

    public static string GetPrice(GameLanguage language, uint price) => string.Format(Get(language, PriceFormat), price);

    public static string GetSavegame(GameLanguage language, int number, string name) =>
        string.Format(Get(language, SavegameFormat), number) + ", " + (string.IsNullOrWhiteSpace(name) ? Get(language, Empty) : name);
}
