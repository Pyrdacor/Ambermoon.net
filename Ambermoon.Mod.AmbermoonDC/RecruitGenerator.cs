using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Mod.Assets;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Creates random companions: random race, gender, portrait, name, class,
/// attributes, skills, spells and dialogues. The level depends on the floor.
///
/// The class specific values (hit points per level etc.) are taken from the
/// original party member of that class.
/// </summary>
internal class RecruitGenerator(ModApi api, System.Random random)
{
    readonly ModApi api = api;
    readonly System.Random random = random;

    /// <summary>
    /// Original party member which is used as the template for each class.
    /// </summary>
    static readonly Dictionary<Class, uint> ClassTemplates = new()
    {
        { Class.Adventurer, 1 },
        { Class.Warrior, 8 },
        { Class.Thief, 9 },
        { Class.Mage, 10 },
        { Class.Healer, 11 },
        { Class.Ranger, 12 },
        { Class.Mystic, 13 },
        { Class.Alchemist, 14 },
        { Class.Paladin, 15 },
    };

    static readonly Dictionary<Class, Race[]> ClassRaces = new()
    {
        { Class.Adventurer, [Race.Human, Race.Elf, Race.Dwarf, Race.HalfElf] },
        { Class.Warrior, [Race.Human, Race.Dwarf, Race.HalfElf, Race.Felinic] },
        { Class.Paladin, [Race.Human, Race.Dwarf, Race.HalfElf] },
        { Class.Thief, [Race.Human, Race.Elf, Race.Gnome, Race.HalfElf, Race.Sylphe] },
        { Class.Ranger, [Race.Human, Race.Elf, Race.HalfElf, Race.Felinic] },
        { Class.Healer, [Race.Human, Race.Elf, Race.HalfElf] },
        { Class.Alchemist, [Race.Human, Race.Elf, Race.Gnome, Race.HalfElf] },
        { Class.Mystic, [Race.Human, Race.Elf, Race.Gnome, Race.HalfElf] },
        { Class.Mage, [Race.Human, Race.Elf, Race.Gnome, Race.HalfElf] },
    };

    record RaceInfo(int[] MaxAttributes, int MinAge, int MaxAge, int MaxAgeLimit, Language Language);

    // Maximum values of strength, intelligence, dexterity, speed, stamina, charisma, luck and anti-magic
    // like the original party members of these races have.
    static readonly Dictionary<Race, RaceInfo> Races = new()
    {
        { Race.Human, new([50, 50, 50, 50, 50, 50, 50, 0], 18, 35, 80, Language.Human) },
        { Race.Elf, new([35, 90, 70, 70, 35, 90, 50, 30], 60, 250, 999, Language.Elfish) },
        { Race.Dwarf, new([90, 35, 40, 40, 90, 35, 50, 5], 40, 150, 750, Language.Dwarfish) },
        { Race.Gnome, new([50, 75, 60, 60, 40, 20, 60, 10], 40, 200, 500, Language.Gnomish) },
        { Race.HalfElf, new([40, 70, 60, 60, 40, 70, 50, 15], 25, 80, 250, Language.Elfish) },
        { Race.Sylphe, new([20, 25, 95, 95, 25, 95, 75, 50], 40, 200, 800, Language.Sylphic) },
        { Race.Felinic, new([70, 40, 80, 80, 50, 40, 50, 0], 18, 30, 85, Language.Felinic) },
    };

    /// <summary>
    /// Portraits of the original game by race and gender.
    /// </summary>
    static readonly Dictionary<(Race, Gender), byte[]> Portraits = new()
    {
        { (Race.Human, Gender.Male), [3, 4, 5, 6, 8, 9, 11, 12, 13, 14, 16, 17, 18, 20, 21, 22, 23, 27] },
        { (Race.Human, Gender.Female), [31, 42, 43, 46, 47, 51, 52, 53] },
        { (Race.Elf, Gender.Male), [25, 61, 65, 66, 67] },
        { (Race.Elf, Gender.Female), [73, 76] },
        { (Race.Dwarf, Gender.Male), [56, 59, 60, 90, 91, 92, 93, 94, 96, 98] },
        { (Race.Dwarf, Gender.Female), [99, 100, 101] },
        { (Race.Gnome, Gender.Male), [57, 58] },
        { (Race.HalfElf, Gender.Male), [15, 19] },
        { (Race.Sylphe, Gender.Female), [79, 80] },
        { (Race.Felinic, Gender.Male), [84] },
    };

    static readonly string[] MaleNames =
    [
        "ALDRIC", "BORAN", "CEDRIK", "DOREN", "EGMAR", "FALKO", "GORWIN", "HALVAR", "IVOR", "JORIK",
        "KASPAR", "LORAN", "MARVEK", "NORIN", "OSWIN", "PERRIN", "RANDOR", "SOREN", "TOBIN", "ULRIK",
        "VALEN", "WENDAL", "YORICK", "ZAREK", "BRANDIR", "TAVISH"
    ];

    static readonly string[] FemaleNames =
    [
        "ALINA", "BRENNA", "CELIA", "DARIA", "ELWYN", "FENJA", "GWENDA", "HELKA", "ISOLDE", "JOLINE",
        "KIRA", "LENIA", "MIRELLA", "NADIRA", "ORLA", "PERA", "RONJA", "SILKA", "TAMIRA", "UNA",
        "VELIA", "WILMA", "YSOLDE", "ZORA", "LIANDRA", "SEVARA"
    ];

    T Pick<T>(IReadOnlyList<T> list) => list[random.Next(list.Count)];

    /// <summary>
    /// Creates a new random recruit and stores it as the given party member index of the savegame.
    /// Returns the texts of the recruit as they are not stored in the savegame.
    /// </summary>
    public PartyMember Create(Savegame savegame, uint index, uint level)
    {
        var characterManager = api.GameData.CharacterManager;
        var itemManager = api.GameData.ItemManager;
        var @class = Pick(ClassTemplates.Keys.ToList());
        var template = characterManager.GetInitialPartyMember(ClassTemplates[@class])
            ?? throw new InvalidOperationException($"Missing party member template for class {@class}.");
        var recruit = Clone(template, index);

        // Remove everything the template had.
        foreach (var slot in recruit.Equipment.Slots.Keys.ToList())
            CharacterHelper.Unequip(recruit, slot, itemManager);

        foreach (var slot in recruit.Inventory.Slots)
            slot.Clear();

        recruit.TotalWeight = 0;
        recruit.SetGold(0);
        recruit.SetFood(0);
        recruit.Conditions = Condition.None;

        // Race, gender, portrait and name
        var race = Pick(ClassRaces[@class]);
        var genders = Portraits.Keys.Where(k => k.Item1 == race).Select(k => k.Item2).ToList();
        var gender = Pick(genders);
        var raceInfo = Races[race];
        recruit.Race = race;
        recruit.Gender = gender;
        recruit.PortraitIndex = Pick(Portraits[(race, gender)]);
        recruit.Name = Pick(gender == Gender.Female ? FemaleNames : MaleNames);
        recruit.SpokenLanguages = Language.Human | raceInfo.Language;

        // Attributes
        for (int i = 0; i < 8; i++)
        {
            var attribute = recruit.Attributes[(Data.Attribute)i];
            int max = raceInfo.MaxAttributes[i];
            attribute.MaxValue = (uint)max;
            attribute.CurrentValue = (uint)(max * random.Next(40, 86) / 100);
        }

        var age = recruit.Attributes[Data.Attribute.Age];
        age.MaxValue = (uint)raceInfo.MaxAgeLimit;
        age.CurrentValue = (uint)random.Next(raceInfo.MinAge, raceInfo.MaxAge + 1);

        // Skills (the maximum values are class specific and kept from the template)
        foreach (var skill in EnumHelper.GetValues<Skill>())
        {
            var value = recruit.Skills[skill];
            int max = (int)value.MaxValue;
            value.CurrentValue = max == 0 ? 0 : (uint)Math.Min(max, max * random.Next(25, 50) / 100 + (int)level);
        }

        // Start at level 1 and level up to the wanted level.
        recruit.Level = 1;
        recruit.MaxReachedLevel = 1;
        recruit.ExperiencePoints = 0;
        recruit.AttacksPerRound = 1;
        recruit.HitPoints.MaxValue = (uint)(recruit.HitPointsPerLevel + random.Next(0, recruit.HitPointsPerLevel / 2 + 1));
        recruit.SpellPoints.MaxValue = @class.IsMagic() ? (uint)(recruit.SpellPointsPerLevel + random.Next(0, recruit.SpellPointsPerLevel / 2 + 1)) : 0;
        recruit.SpellLearningPoints = recruit.SpellLearningPointsPerLevel;
        recruit.TrainingPoints = recruit.TrainingPointsPerLevel;
        recruit.LearnedHealingSpells = 0;
        recruit.LearnedAlchemisticSpells = 0;
        recruit.LearnedMysticSpells = 0;
        recruit.LearnedDestructionSpells = 0;
        CharacterHelper.RaiseLevel(recruit, level, (min, max) => random.Next(min, max + 1));

        LearnSpells(recruit);

        // A lost companion: the character bit of an unused map is used, so dismissing
        // the recruit won't make anything reappear in the dungeon.
        recruit.CharacterBitIndex = (ushort)(((FloorBuilder.RecruitBitsMap - 1) << 5) | (index & 0x1f));

        CreateDialogues(recruit);

        savegame.PartyMembers[index] = recruit;

        return recruit;
    }

    static PartyMember Clone(PartyMember template, uint index)
    {
        var writer = new DataWriter();
        new PartyMemberWriter().WritePartyMember(template, writer);
        var clone = PartyMember.Load(index, new PartyMemberReader(), new DataReader(writer.ToArray()), null);
        clone.Index = index;
        clone.Events.Clear();
        clone.EventList.Clear();

        return clone;
    }

    void LearnSpells(PartyMember recruit)
    {
        if (!recruit.Class.IsMagic())
            return;

        var schools = new List<SpellSchool>();

        foreach (var school in new[] { SpellSchool.Healing, SpellSchool.Alchemistic, SpellSchool.Mystic, SpellSchool.Destruction })
        {
            if (recruit.SpellMastery.HasFlag((SpellTypeMastery)(1 << (int)school)))
                schools.Add(school);
        }

        // Cheap spells are learned first. Higher levels allow more and better spells.
        uint maxCost = 5 + recruit.Level * 3u;
        var candidates = SpellInfos.Entries
            .Where(s => schools.Contains(s.Value.SpellSchool) && s.Value.SLP > 0 && s.Value.SLP <= maxCost && (int)s.Key <= 4 * 30)
            .Select(s => s.Key)
            .OrderBy(_ => random.Next())
            .Take(2 + recruit.Level / 2);

        foreach (var spell in candidates)
            recruit.AddSpell(spell);
    }

    void CreateDialogues(PartyMember recruit)
    {
        var texts = DCTexts.Get(api.Language);
        var names = api.GameData.DataNameProvider;
        var classLines = texts.ClassLines[recruit.Class];
        var talk = random.Next(3) == 0 ? Pick(texts.GenericLines) : Pick(classLines);

        recruit.Texts =
        [
            string.Format(texts.RecruitLook, recruit.Name, names.GetRaceName(recruit.Race), names.GetClassName(recruit.Class)),
            string.Format(texts.RecruitGreeting, recruit.Name) + " " + talk,
            Pick(texts.RecruitFarewells),
            Pick(texts.RecruitJoins),
            Pick(texts.RecruitLeaves)
        ];
        recruit.LookAtCharTextIndex = 0;

        Event Conversation(ConversationEvent.InteractionType interaction)
            => EventFactory.Create<ConversationEvent>(e => e.Interaction = interaction);
        Event Text(uint textIndex) => EventFactory.Create<PrintTextEvent>(e => e.NPCTextIndex = textIndex);
        Event Interact() => EventFactory.Create<InteractEvent>();

        void AddEntry(params Event[] chain)
        {
            for (int i = 0; i < chain.Length; i++)
            {
                chain[i].Index = (uint)recruit.Events.Count + 1;
                chain[i].Next = i < chain.Length - 1 ? chain[i + 1] : null;
                recruit.Events.Add(chain[i]);
            }

            recruit.EventList.Add(chain[0]);
        }

        AddEntry(Conversation(ConversationEvent.InteractionType.Talk), Text(1));
        AddEntry(Conversation(ConversationEvent.InteractionType.Leave), Text(2));
        AddEntry(Conversation(ConversationEvent.InteractionType.JoinParty), Text(3), Interact());
        AddEntry(Conversation(ConversationEvent.InteractionType.LeaveParty), Text(4), Interact());
    }
}
