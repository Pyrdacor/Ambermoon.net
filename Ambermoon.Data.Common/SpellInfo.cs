using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Ambermoon.Data
{
    [Flags]
    public enum SpellApplicationArea
    {
        AnyMap = 0x01,
        Camp = 0x02,
        Battle = 0x04,
        Door = 0x08,
        WorldMapOnly = 0x10,
        DungeonOnly = 0x20,
        All = AnyMap | Camp | Battle,
        BattleOnly = Battle,
        NoBattle = AnyMap | Camp,
        CampAndBattle = Camp | Battle
    }

    public enum SpellTarget
    {
        None,
        Self,
        SingleEnemy,
        EnemyRow,
        AllEnemies,
        SingleFriend,
        FriendRow,
        AllFriends,
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
    }

    public struct SpellInfo
    {
        public SpellType SpellType;
        public Spell Spell;
        public uint SP;
        public uint SLP;
        public SpellTarget Target;
        public SpellApplicationArea ApplicationArea;
    }

    // TODO: can we load this from game data?
    public static class SpellInfos
    {
        static readonly Dictionary<Spell, SpellInfo> entries = new Dictionary<Spell, SpellInfo>
        {
            { Spell.HealingHand, new SpellInfo { SP = 3, SLP = 1, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemoveFear, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.RemovePanic, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.RemoveShadows, new SpellInfo { SP = 8, SLP = 3, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemoveBlindness, new SpellInfo { SP = 20, SLP = 8, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemovePain, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemoveDisease, new SpellInfo { SP = 20, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All } },
            { Spell.SmallHealing, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemovePoison, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.NeutralizePoison, new SpellInfo { SP = 25, SLP = 12, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MediumHealing, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.DispellUndead, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.DestroyUndead, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.HolyWord, new SpellInfo { SP = 100, SLP = 20, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.WakeTheDead, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.ChangeAshes, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.ChangeDust, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.GreatHealing, new SpellInfo { SP = 100, SLP = 30, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MassHealing, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All } },
            { Spell.Resurrection, new SpellInfo { SP = 250, SLP = 30, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.RemoveRigidness, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.RemoveLamedness, new SpellInfo { SP = 30, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.All } },
            { Spell.HealAging, new SpellInfo { SP = 50, SLP = 12, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.StopAging, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.StoneToFlesh, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.WakeUp, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.RemoveIrritation, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.RemoveDrugged, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.RemoveMadness, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.RestoreStamina, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } }, // TODO: target right?
            { Spell.ChargeItem, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.Light, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MagicalTorch, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MagicalLantern, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MagicalSun, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.GhostWeapon, new SpellInfo { SP = 10, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.CreateFood, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.RemoveCurses, new SpellInfo { SP = 100, SLP = 20, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.Blink, new SpellInfo { SP = 20, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Jump, new SpellInfo { SP = 50, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.Flight, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.BattleOnly } }, // TODO: battle only makes sense but on docu it is "not in battle"
            { Spell.WordOfMarking, new SpellInfo { SP = 150, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.WordOfReturning, new SpellInfo { SP = 250, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MagicalShield, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalWall, new SpellInfo { SP = 30, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalBarrier, new SpellInfo { SP = 50, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalWeapon, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalAssault, new SpellInfo { SP = 30, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalAttack, new SpellInfo { SP = 50, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.Levitation, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.AntiMagicWall, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AntiMagicSphere, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AlchemisticGlobe, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.Hurry, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.MassHurry, new SpellInfo { SP = 50, SLP = 10, Target = SpellTarget.AllFriends, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.RepairItem, new SpellInfo { SP = 100, SLP = 15, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.DuplicateItem, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp} },
            { Spell.LPStealer, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.SPStealer, new SpellInfo { SP = 25, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.UnusedAlchemistic30, new SpellInfo { SP = uint.MaxValue, SLP = uint.MaxValue } },
            { Spell.MonsterKnowledge, new SpellInfo { SP = 5, SLP = 3, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Identification, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.Item, ApplicationArea = SpellApplicationArea.Camp } },
            { Spell.Knowledge, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.Clairvoyance, new SpellInfo { SP = 30, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.SeeTheTruth, new SpellInfo { SP = 60, SLP = 30, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MapView, new SpellInfo { SP = 50, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.MagicalCompass, new SpellInfo { SP = 5, SLP = 2, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.FindTraps, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.FindMonsters, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap} },
            { Spell.FindPersons, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.FindSecretDoors, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.MysticalMapping, new SpellInfo { SP = 100, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.MysticalMapI, new SpellInfo { SP = 25, SLP = 10, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MysticalMapII, new SpellInfo { SP = 35, SLP = 15, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MysticalMapIII, new SpellInfo { SP = 45, SLP = 20, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.MysticalGlobe, new SpellInfo { SP = 250, SLP = 25, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.ShowMonsterLP, new SpellInfo { SP = 15, SLP = 5, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.BattleOnly } },
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
            { Spell.MagicalProjectile, new SpellInfo { SP = 5, SLP = 5, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.MagicalArrows, new SpellInfo { SP = 15, SLP = 10, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Lame, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Poison, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Petrify, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.CauseDisease, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.CauseAging, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Irritate, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.CauseMadness, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Sleep, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Fear, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Blind, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Drug, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.DissolveVictim, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Mudsling, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Rockfall, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Earthslide, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Earthquake, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Winddevil, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Windhowler, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Thunderbolt, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Whirlwind, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Firebeam, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Fireball, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Firestorm, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Firepillar, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Waterfall, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Iceball, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleEnemy, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Icestorm, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.EnemyRow, ApplicationArea = SpellApplicationArea.BattleOnly } },
            { Spell.Iceshower, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.AllEnemies, ApplicationArea = SpellApplicationArea.BattleOnly } },
            // Special spells
            { Spell.Lockpicking, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.Door } },
            { Spell.CallEagle, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.WorldMapOnly } },
            { Spell.DecreaseAge, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.PlayElfHarp, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.AnyMap } },
            { Spell.SpellPointsI, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.SpellPointsII, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.SpellPointsIII, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.SpellPointsIV, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.SpellPointsV, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AllHealing, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.MagicalMap, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.NoBattle } },
            { Spell.AddStrength, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddIntelligence, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddDexterity, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddSpeed, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddStamina, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddCharisma, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddLuck, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.AddAntiMagic, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } },
            { Spell.Rope, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.None, ApplicationArea = SpellApplicationArea.DungeonOnly } },
            { Spell.Drugs, new SpellInfo { SP = 0, SLP = 0, Target = SpellTarget.SingleFriend, ApplicationArea = SpellApplicationArea.All } }
        };

        static SpellInfos()
        {
            foreach (var spell in entries.ToList())
            {
                entries[spell.Key] = new SpellInfo
                {
                    Spell = spell.Key,
                    SpellType = (SpellType)(((int)spell.Key - 1) / 30),
                    SP = spell.Value.SP,
                    SLP = spell.Value.SLP,
                    Target = spell.Value.Target,
                    ApplicationArea = spell.Value.ApplicationArea
                };
           }
        }

        public static IReadOnlyDictionary<Spell, SpellInfo> Entries => entries;
    }
}
