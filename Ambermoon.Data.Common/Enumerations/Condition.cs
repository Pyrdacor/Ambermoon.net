using System;
using System.ComponentModel;

namespace Ambermoon.Data
{
    [Flags]
    public enum Condition : ushort
    {
        None = 0,
        Irritated = 0x0001,
        Crazy = 0x0002,
        Sleep = 0x0004,
        Panic = 0x0008,
        Blind = 0x0010,
        Drugged = 0x0020,
        Exhausted = 0x0040,
        Unused = 0x0080,
        Lamed = 0x0100,
        Poisoned = 0x0200,
        Petrified = 0x0400,
        Diseased = 0x0800,
        Aging = 0x1000,
        DeadCorpse = 0x2000,
        DeadAshes = 0x4000,
        DeadDust = 0x8000
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ConditionExtensions
    {
        public static Condition WithoutBattleOnlyConditions(this Condition conditions)
        {
            return (Condition)((int)conditions & 0xfff2);
        }

        public static bool CanFight(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanSelect(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Crazy) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanTalk(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Crazy) && // TODO: correct?
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanOpenInventory(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Crazy) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Petrified);
        }

        public static bool CanUseItem(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Crazy) &&
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Drugged) &&
                !conditions.HasFlag(Condition.Lamed) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanMove(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Lamed) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanBlink(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanFlee(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Lamed) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanAttack(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Lamed) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanParry(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Exhausted) &&
                !conditions.HasFlag(Condition.Lamed) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }

        public static bool CanCastSpell(this Condition conditions)
        {
            return
                !conditions.HasFlag(Condition.Irritated) &&
                !conditions.HasFlag(Condition.Sleep) &&
                !conditions.HasFlag(Condition.Panic) &&
                !conditions.HasFlag(Condition.Drugged) &&
                !conditions.HasFlag(Condition.Petrified) &&
                !conditions.HasFlag(Condition.Unused) && // the original code states that this disables casting as well
                !conditions.HasFlag(Condition.DeadCorpse) &&
                !conditions.HasFlag(Condition.DeadAshes) &&
                !conditions.HasFlag(Condition.DeadDust);
        }
    }
}
