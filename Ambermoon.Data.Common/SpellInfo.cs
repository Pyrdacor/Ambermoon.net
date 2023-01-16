using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

namespace Ambermoon.Data
{
    /// <summary>
    /// This is very similar to <see cref="SpellSchool"/>
    /// but spells like Holy Word or Ghost Weapon count
    /// as Destruction.
    /// </summary>
    [Flags]
    public enum SpellType
    {
        Healing = 0x01,
        Alchemistic = 0x02,
        Mystic = 0x04,
        Destruction = 0x08,
        Unknown1 = 0x10,
        Unknown2 = 0x20,
        Function = 0x40 // lockpicking, call eagle, play elf harp etc
    }

    [Flags]
    public enum SpellApplicationArea
    {
        AnyMap = 0x01,
        Camp = 0x02,
        Battle = 0x04,
        WorldMapOnly = 0x08,
        DungeonOnly = 0x10, // this also includes indoor 3D maps
        All = AnyMap | Camp | Battle,
        BattleOnly = Battle,
        NoBattle = AnyMap | Camp,
        CampAndBattle = Camp | Battle
    }

    public enum SpellTarget
    {
        None = -1,
        SingleFriend,
        FriendRow,
        AllFriends,
        SingleEnemy,
        EnemyRow,
        AllEnemies,
        Item,
        BattleField // Blink
    }

    public enum SpellTargetType
    {
        None,
        SingleBattleField,
        BattleFieldRow,
        HalfBattleField,
        Item
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SpellTargetExtensions
    {
        public static bool TargetsEnemy(this SpellTarget spellTarget) => spellTarget switch
        {
            SpellTarget.SingleEnemy => true,
            SpellTarget.EnemyRow => true,
            SpellTarget.AllEnemies => true,
            _ => false
        };

        public static bool TargetsMultipleEnemies(this SpellTarget spellTarget) =>
            spellTarget == SpellTarget.EnemyRow || spellTarget == SpellTarget.AllEnemies;

        public static SpellTargetType GetTargetType(this SpellTarget spellTarget) => spellTarget switch
        {
            SpellTarget.SingleEnemy => SpellTargetType.SingleBattleField,
            SpellTarget.SingleFriend => SpellTargetType.SingleBattleField,
            SpellTarget.EnemyRow => SpellTargetType.BattleFieldRow,
            SpellTarget.AllEnemies => SpellTargetType.HalfBattleField,
            SpellTarget.AllFriends => SpellTargetType.HalfBattleField,
            SpellTarget.Item => SpellTargetType.Item,
            SpellTarget.BattleField => SpellTargetType.SingleBattleField,
            _ => SpellTargetType.None
        };
    }

    public struct SpellInfo
    {
        public SpellSchool SpellSchool;
        public SpellType SpellType;
        public Spell Spell;
        public uint SP;
        public uint SLP;
        public SpellTarget Target;
        public SpellApplicationArea ApplicationArea;
        public WorldFlag Worlds;
    }

    // TODO: this is stored in AM2_CPU. Load it from there later.
    public static class SpellInfos
    {
        static readonly Dictionary<Spell, SpellInfo> entries = new Dictionary<Spell, SpellInfo>
        {
            { Spell.HealingHand, new SpellInfo { SP = 3, SLP = 1, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemoveFear, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.RemovePanic, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.RemoveShadows, new SpellInfo { SP = 8, SLP = 3, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemoveBlindness, new SpellInfo { SP = 20, SLP = 8, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemovePain, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemoveDisease, new SpellInfo { SP = 20, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SmallHealing, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemovePoison, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.NeutralizePoison, new SpellInfo { SP = 25, SLP = 12, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MediumHealing, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.DispellUndead, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.DestroyUndead, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.HolyWord, new SpellInfo { SP = 100, SLP = 20, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.WakeTheDead, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.ChangeAshes, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.ChangeDust, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.GreatHealing, new SpellInfo { SP = 100, SLP = 30, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MassHealing, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.Resurrection, new SpellInfo { SP = 250, SLP = 30, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.RemoveRigidness, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.RemoveLamedness, new SpellInfo { SP = 30, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.HealAging, new SpellInfo { SP = 50, SLP = 12, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.StopAging, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.StoneToFlesh, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.WakeUp, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.RemoveIrritation, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.RemoveDrugged, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.RemoveMadness, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.RestoreStamina, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.ChargeItem, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.Light, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MagicalTorch, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MagicalLantern, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MagicalSun, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.GhostWeapon, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.CreateFood, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.RemoveCurses, new SpellInfo { SP = 100, SLP = 20, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.Blink, new SpellInfo { SP = 20, SLP = 5, Target = SpellTarget.BattleField, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Jump, new SpellInfo { SP = 50, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.DungeonOnly, Worlds = WorldFlag.All } },
            { Spell.Escape, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.WordOfMarking, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.Lyramion } },
            { Spell.WordOfReturning, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.Lyramion } },
            { Spell.MagicalShield, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalWall, new SpellInfo { SP = 30, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalBarrier, new SpellInfo { SP = 50, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalWeapon, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalAssault, new SpellInfo { SP = 30, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalAttack, new SpellInfo { SP = 50, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.Levitation, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.DungeonOnly, Worlds = WorldFlag.All } },
            { Spell.AntiMagicWall, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AntiMagicSphere, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AlchemisticGlobe, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.Hurry, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.MassHurry, new SpellInfo { SP = 50, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.RepairItem, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.DuplicateItem, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.LPStealer, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.SPStealer, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.UnusedAlchemistic30, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.MonsterKnowledge, new SpellInfo { SP = 5, SLP = 3, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Identification, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.Knowledge, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.Clairvoyance, new SpellInfo { SP = 30, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.SeeTheTruth, new SpellInfo { SP = 60, SLP = 30, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MapView, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MagicalCompass, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.FindTraps, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.FindMonsters, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.FindPersons, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.FindSecretDoors, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.MysticalMapping, new SpellInfo { SP = 100, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.MysticalMapI, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MysticalMapII, new SpellInfo { SP = 35, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MysticalMapIII, new SpellInfo { SP = 45, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.MysticalGlobe, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.ShowMonsterLP, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.UnusedMystic18, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic19, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic20, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic21, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic22, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic23, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic24, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic25, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic26, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic27, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic28, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic29, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.UnusedMystic30, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.MagicalProjectile, new SpellInfo { SP = 5, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.MagicalArrows, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Lame, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Poison, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Petrify, new SpellInfo { SP = 60, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.CauseDisease, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.CauseAging, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Irritate, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.CauseMadness, new SpellInfo { SP = 30, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Sleep, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Fear, new SpellInfo { SP = 50, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Blind, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Drug, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.DissolveVictim, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Mudsling, new SpellInfo { SP = 8, SLP = 1, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.Morag } },
            { Spell.Rockfall, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.Morag } },
            { Spell.Earthslide, new SpellInfo { SP = 20, SLP = 10, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.Morag } },
            { Spell.Earthquake, new SpellInfo { SP = 30, SLP = 15, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.Morag } },
            { Spell.Winddevil, new SpellInfo { SP = 12, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Windhowler, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Thunderbolt, new SpellInfo { SP = 35, SLP = 15, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Whirlwind, new SpellInfo { SP = 50, SLP = 20, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Firebeam, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Fireball, new SpellInfo { SP = 60, SLP = 15, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Firestorm, new SpellInfo { SP = 80, SLP = 20, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Firepillar, new SpellInfo { SP = 120, SLP = 25, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.All } },
            { Spell.Waterfall, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.ForestMoon } },
            { Spell.Iceball, new SpellInfo { SP = 100, SLP = 20, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.ForestMoon } },
            { Spell.Icestorm, new SpellInfo { SP = 150, SLP = 25, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.ForestMoon } },
            { Spell.Iceshower, new SpellInfo { SP = 200, SLP = 30, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly, Worlds = WorldFlag.Lyramion | WorldFlag.ForestMoon } },
            // Special spells
            { Spell.Lockpicking, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.CallEagle, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.WorldMapOnly, Worlds = WorldFlag.Lyramion } },
            { Spell.DecreaseAge, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle, Worlds = WorldFlag.All } },
            { Spell.PlayElfHarp, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap, Worlds = WorldFlag.All } },
            { Spell.SpellPointsI, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SpellPointsII, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SpellPointsIII, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SpellPointsIV, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SpellPointsV, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AllHealing, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.MagicalMap, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.WorldMapOnly, Worlds = WorldFlag.Lyramion } },
            { Spell.AddStrength, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddIntelligence, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddDexterity, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddSpeed, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddStamina, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddCharisma, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddLuck, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.AddAntiMagic, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.Rope, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.DungeonOnly, Worlds = WorldFlag.All } },
            { Spell.Drugs, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SelfHealing, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All, Worlds = WorldFlag.All } },
            { Spell.SelfReviving, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.All } },
            { Spell.ExpExchange, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp, Worlds = WorldFlag.Lyramion } }
        };

        static readonly Dictionary<Spell, uint> adjustedSLP = new Dictionary<Spell, uint>
        {
            { Spell.RemoveFear, 10 },
            { Spell.RemovePanic, 20 },
            { Spell.RemoveShadows, 10 },
            { Spell.RemoveBlindness, 20 },
            { Spell.RemovePain, 20 },
            { Spell.RemoveDisease, 30 },
            { Spell.SmallHealing, 20 },
            { Spell.RemovePoison, 30 },
            { Spell.NeutralizePoison, 45 },
            { Spell.MediumHealing, 40 },
            { Spell.DestroyUndead, 25 },
            { Spell.HolyWord, 50 },
            { Spell.WakeTheDead, 80 },
            { Spell.ChangeAshes, 50 },
            { Spell.ChangeDust, 50 },
            { Spell.GreatHealing, 60 },
            { Spell.MassHealing, 65 },
            { Spell.Resurrection, 120 },
            { Spell.RemoveRigidness, 25 },
            { Spell.RemoveLamedness, 35 },
            { Spell.HealAging, 15 },
            { Spell.StopAging, 20 },
            { Spell.StoneToFlesh, 55 },
            { Spell.WakeUp, 10 },
            { Spell.RemoveIrritation, 25 },
            { Spell.RemoveDrugged, 15 },
            { Spell.RemoveMadness, 50 },
            { Spell.RestoreStamina, 5 },
            { Spell.ChargeItem, 60 },
            { Spell.Light, 5 },
            { Spell.MagicalTorch, 10 },
            { Spell.MagicalLantern, 20 },
            { Spell.MagicalSun, 35 },
            { Spell.GhostWeapon, 30 },
            { Spell.CreateFood, 30 },
            { Spell.Blink, 15 },
            { Spell.Jump, 50 },
            { Spell.WordOfMarking, 70 },
            { Spell.WordOfReturning, 80 },
            { Spell.MagicalShield, 25 },
            { Spell.MagicalWall, 35 },
            { Spell.MagicalBarrier, 45 },
            { Spell.MagicalWeapon, 25 },
            { Spell.MagicalAssault, 35 },
            { Spell.MagicalAttack, 45 },
            { Spell.Levitation, 65 },
            { Spell.AntiMagicWall, 30 },
            { Spell.AntiMagicSphere, 50 },
            { Spell.AlchemisticGlobe, 120 },
            { Spell.Hurry, 50 },
            { Spell.MassHurry, 80 },
            { Spell.RepairItem, 45 },
            { Spell.DuplicateItem, 75 },
            { Spell.LPStealer, 25 },
            { Spell.SPStealer, 25 },
            { Spell.MonsterKnowledge, 15 },
            { Spell.Identification, 35 },
            { Spell.Knowledge, 5 },
            { Spell.Clairvoyance, 15 },
            { Spell.MapView, 20 },
            { Spell.MagicalCompass, 5 },
            { Spell.FindTraps, 25 },
            { Spell.FindMonsters, 25 },
            { Spell.FindPersons, 25 },
            { Spell.FindSecretDoors, 25 },
            { Spell.MysticalMapping, 70 },
            { Spell.MysticalMapI, 75 },
            { Spell.MysticalMapII, 80 },
            { Spell.MysticalMapIII, 85 },
            { Spell.MysticalGlobe, 100 },
            { Spell.ShowMonsterLP, 15 }
        };

        static readonly Dictionary<Spell, uint> adjustedSP = new Dictionary<Spell, uint>
        {            
            { Spell.Escape, 100 },
            { Spell.AlchemisticGlobe, 200 },
            { Spell.MysticalGlobe, 100 },
            { Spell.Mudsling, 10 },
            { Spell.Earthquake, 25 },
            { Spell.Winddevil, 20 },
            { Spell.Windhowler, 30 },
            { Spell.Thunderbolt, 40 },
            { Spell.Firebeam, 40 },
            { Spell.Firepillar, 100 },
            { Spell.Waterfall, 80 },
            { Spell.Iceball, 120 },
            { Spell.Icestorm, 160 }
        };

        public static uint GetSPCost(Features features, Spell spell, Character caster)
        {
            uint GetBaseSP(Spell spell) => features.HasFlag(Features.AdjustedSPAndSLP) && adjustedSP.TryGetValue(spell, out var sp)
                ? sp : Entries[spell].SP;

            if (caster is PartyMember)
            {
                if (spell >= Spell.Mudsling && spell <= Spell.Earthquake)
                {
                    if (caster.BattleFlags.HasFlag(BattleFlags.EarthSpellDamageBonus))
                        return GetBaseSP((Spell)((int)spell + 12));
                }
                else if (spell >= Spell.Winddevil && spell <= Spell.Whirlwind)
                {
                    if (caster.BattleFlags.HasFlag(BattleFlags.WindSpellDamageBonus))
                        return GetBaseSP((Spell)((int)spell + 8));
                }
                else if (spell >= Spell.Firebeam && spell <= Spell.Firepillar)
                {
                    if (caster.BattleFlags.HasFlag(BattleFlags.FireSpellDamageBonus))
                        return GetBaseSP((Spell)((int)spell + 4));
                }
            }

            return GetBaseSP(spell);
        }

        public static uint GetSLPCost(Features features, Spell spell)
        {
            return features.HasFlag(Features.AdjustedSPAndSLP) && adjustedSLP.TryGetValue(spell, out var slp)
                ? slp : Entries[spell].SLP;
        }

        static SpellInfos()
        {
            foreach (var spell in entries.ToList())
            {
                entries[spell.Key] = new SpellInfo
                {
                    Spell = spell.Key,
                    SpellSchool = (SpellSchool)(((int)spell.Key - 1) / 30),
                    SpellType = GetSpellType(spell.Key),
                    SP = spell.Value.SP,
                    SLP = spell.Value.SLP,
                    Target = spell.Value.Target,
                    ApplicationArea = spell.Value.ApplicationArea,
                    Worlds = spell.Value.Worlds
                };
           }
        }

        static SpellType GetSpellType(Spell spell)
        {
            switch (spell)
            {
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                case Spell.GhostWeapon:
                    return SpellType.Destruction;
                default:
                    return (SpellType)(1 << (((int)spell - 1) / 30));
            }
        }

        public static IReadOnlyDictionary<Spell, SpellInfo> Entries => entries;
    }
}
