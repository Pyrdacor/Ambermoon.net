using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// All texts of the mod. German and English are provided,
/// other languages fall back to English.
/// </summary>
internal class DCTexts
{
    public required string FloorName { get; init; } // {0} = floor
    public required string DescendQuestion { get; init; } // {0} = next floor
    public required string WayBackBlocked { get; init; }
    public required string SecretFound { get; init; }
    public required string Welcome { get; init; }
    public required string FloorEntered { get; init; } // {0} = floor
    public required string BossFloor { get; init; }
    public required string BossWithMinionsFloor { get; init; }
    public required string RecruitFloor { get; init; }
    public required string BossDefeated { get; init; }
    public required string RecruitLook { get; init; } // {0} = name, {1} = race, {2} = class
    public required string RecruitGreeting { get; init; } // {0} = name
    public required Dictionary<Class, string[]> ClassLines { get; init; }
    public required string[] GenericLines { get; init; }
    public required string[] RecruitFarewells { get; init; }
    public required string[] RecruitJoins { get; init; }
    public required string[] RecruitLeaves { get; init; }

    static readonly DCTexts English = new()
    {
        FloorName = "FLOOR {0}",
        DescendQuestion = "A hole leads down into the darkness. Do you want to descend to floor {0}?",
        WayBackBlocked = "The way back up is blocked. There is only one way: down.",
        SecretFound = "You found a secret passage!",
        Welcome = "You wake up in a dark cave. Your memories are blurred. Somewhere below, they say, lies the way out. " +
            "Find the holes which lead down, search for hidden passages and gather allies on your way.",
        FloorEntered = "You reached floor {0}.",
        BossFloor = "A terrible presence guards the way down on this floor.",
        BossWithMinionsFloor = "You hear the marching of many feet. A mighty foe and its servants guard the way down.",
        RecruitFloor = "You hear someone wander around on this floor. Maybe a fellow adventurer?",
        BossDefeated = "The guardian of this floor is defeated!",
        RecruitLook = "YOU SEE {0}, {1} ({2}).",
        RecruitGreeting = "HELLO! I AM {0}.",
        ClassLines = new()
        {
            { Class.Adventurer, ["I AM LOOKING FOR ADVENTURE.", "I LOST MY COMPANIONS A FEW FLOORS ABOVE."] },
            { Class.Warrior, ["LET'S KILL SOME MONSTERS!", "MY SWORD IS SHARP AND MY ARM IS STRONG.", "I HAVE BEEN FIGHTING MY WAY DOWN FOR DAYS."] },
            { Class.Paladin, ["I SWORE TO CLEANSE THESE DEPTHS OF EVIL.", "MAY THE GODS GUIDE MY BLADE."] },
            { Class.Thief, ["I AM A TREASURE HUNTER AND SEARCH FOR RARE ITEMS.", "NO LOCK DOWN HERE CAN STOP ME.", "THEY SAY THERE IS GOLD DEEPER DOWN."] },
            { Class.Ranger, ["I TRACK THE BEASTS OF THESE CAVES.", "MY ARROWS NEVER MISS."] },
            { Class.Healer, ["I TEND TO THE WOUNDED ADVENTURERS DOWN HERE.", "YOU LOOK HURT. MAYBE I CAN HELP."] },
            { Class.Alchemist, ["I COLLECT RARE INGREDIENTS IN THESE CAVES.", "MY POTIONS ARE BETTER THAN ANY SWORD."] },
            { Class.Mystic, ["THE DARKNESS HIDES MANY SECRETS. I CAN SEE THEM.", "I STUDY THE ANCIENT RUNES DOWN HERE."] },
            { Class.Mage, ["I PRACTICE MY DESTRUCTION SPELLS IN THESE CATACOMBS.", "ONE FIREBALL AND THE MONSTERS ARE ASHES."] },
        },
        GenericLines = ["IT IS DANGEROUS TO WANDER ALONE DOWN HERE.", "HAVE YOU SEEN THE WAY DOWN?", "I DON'T REMEMBER HOW I GOT HERE."],
        RecruitFarewells = ["FAREWELL!", "TAKE CARE DOWN THERE.", "SEE YOU AROUND."],
        RecruitJoins = ["TOGETHER WE ARE STRONGER. LET'S GO!", "FINALLY SOME COMPANY!", "LEAD THE WAY!"],
        RecruitLeaves = ["VERY WELL. GOOD LUCK DOWN THERE.", "I WILL FIND MY OWN WAY.", "A PITY. FAREWELL!"]
    };

    static readonly DCTexts German = new()
    {
        FloorName = "EBENE {0}",
        DescendQuestion = "Ein Loch führt hinab in die Dunkelheit. Wollt ihr zu Ebene {0} hinabsteigen?",
        WayBackBlocked = "Der Weg zurück nach oben ist versperrt. Es gibt nur einen Weg: nach unten.",
        SecretFound = "Ihr habt einen geheimen Durchgang gefunden!",
        Welcome = "Ihr erwacht in einer dunklen Höhle. Eure Erinnerungen sind verschwommen. Irgendwo tief unten soll der Ausweg liegen. " +
            "Findet die Löcher, die hinab führen, sucht nach geheimen Gängen und sammelt Verbündete auf eurem Weg.",
        FloorEntered = "Ihr habt Ebene {0} erreicht.",
        BossFloor = "Eine schreckliche Präsenz bewacht auf dieser Ebene den Weg nach unten.",
        BossWithMinionsFloor = "Ihr hört das Marschieren vieler Füße. Ein mächtiger Gegner und seine Diener bewachen den Weg nach unten.",
        RecruitFloor = "Ihr hört jemanden auf dieser Ebene umherstreifen. Vielleicht ein anderer Abenteurer?",
        BossDefeated = "Der Wächter dieser Ebene ist besiegt!",
        RecruitLook = "IHR SEHT {0}, {1} ({2}).",
        RecruitGreeting = "HALLO! ICH BIN {0}.",
        ClassLines = new()
        {
            { Class.Adventurer, ["ICH SUCHE DAS ABENTEUER.", "ICH HABE MEINE GEFÄHRTEN EIN PAAR EBENEN WEITER OBEN VERLOREN."] },
            { Class.Warrior, ["LASST UNS EIN PAAR MONSTER ERSCHLAGEN!", "MEIN SCHWERT IST SCHARF UND MEIN ARM IST STARK.", "ICH KÄMPFE MICH SEIT TAGEN NACH UNTEN."] },
            { Class.Paladin, ["ICH HABE GESCHWOREN, DIESE TIEFEN VOM BÖSEN ZU BEFREIEN.", "MÖGEN DIE GÖTTER MEINE KLINGE FÜHREN."] },
            { Class.Thief, ["ICH BIN SCHATZJÄGER UND SUCHE NACH SELTENEN GEGENSTÄNDEN.", "KEIN SCHLOSS HIER UNTEN KANN MICH AUFHALTEN.", "MAN SAGT, WEITER UNTEN GIBT ES GOLD."] },
            { Class.Ranger, ["ICH VERFOLGE DIE BESTIEN DIESER HÖHLEN.", "MEINE PFEILE VERFEHLEN NIE IHR ZIEL."] },
            { Class.Healer, ["ICH VERSORGE DIE VERWUNDETEN ABENTEURER HIER UNTEN.", "IHR SEHT VERLETZT AUS. VIELLEICHT KANN ICH HELFEN."] },
            { Class.Alchemist, ["ICH SAMMLE SELTENE ZUTATEN IN DIESEN HÖHLEN.", "MEINE TRÄNKE SIND BESSER ALS JEDES SCHWERT."] },
            { Class.Mystic, ["DIE DUNKELHEIT VERBIRGT VIELE GEHEIMNISSE. ICH KANN SIE SEHEN.", "ICH STUDIERE DIE ALTEN RUNEN HIER UNTEN."] },
            { Class.Mage, ["ICH ÜBE MEINE ZERSTÖRUNGSZAUBER IN DIESEN KATAKOMBEN.", "EIN FEUERBALL UND DIE MONSTER SIND ASCHE."] },
        },
        GenericLines = ["ES IST GEFÄHRLICH, HIER UNTEN ALLEIN UMHERZUWANDERN.", "HABT IHR DEN WEG NACH UNTEN GESEHEN?", "ICH WEISS NICHT MEHR, WIE ICH HIERHER GEKOMMEN BIN."],
        RecruitFarewells = ["LEBT WOHL!", "PASST AUF EUCH AUF.", "BIS BALD."],
        RecruitJoins = ["ZUSAMMEN SIND WIR STÄRKER. GEHEN WIR!", "ENDLICH GESELLSCHAFT!", "GEHT VORAN!"],
        RecruitLeaves = ["NUN GUT. VIEL GLÜCK DA UNTEN.", "ICH FINDE MEINEN EIGENEN WEG.", "SCHADE. LEBT WOHL!"]
    };

    public static DCTexts Get(GameLanguage language) => language == GameLanguage.German ? German : English;
}
