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
        BossDefeated = "The guardian of this floor is defeated!"
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
        BossDefeated = "Der Wächter dieser Ebene ist besiegt!"
    };

    public static DCTexts Get(GameLanguage language) => language == GameLanguage.German ? German : English;
}
