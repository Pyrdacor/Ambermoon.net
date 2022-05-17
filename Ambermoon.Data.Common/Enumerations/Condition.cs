using System;
using System.ComponentModel;

namespace Ambermoon.Data
{
    [Flags]
    public enum Condition
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
        public static Condition WithoutBattleOnlyAilments(this Condition ailments)
        {
            return (Condition)((int)ailments & 0xfff2);
        }

        public static bool CanFight(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanSelect(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Panic) &&
                !ailment.HasFlag(Condition.Crazy) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanTalk(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Crazy) && // TODO: correct?
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanOpenInventory(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Crazy) &&
                !ailment.HasFlag(Condition.Panic) &&
                !ailment.HasFlag(Condition.Petrified);
        }

        public static bool CanMove(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Lamed) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanBlink(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanFlee(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Lamed) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanAttack(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Panic) &&
                !ailment.HasFlag(Condition.Lamed) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanParry(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Panic) &&
                !ailment.HasFlag(Condition.Exhausted) &&
                !ailment.HasFlag(Condition.Lamed) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }

        public static bool CanCastSpell(this Condition ailment)
        {
            return
                !ailment.HasFlag(Condition.Irritated) &&
                !ailment.HasFlag(Condition.Sleep) &&
                !ailment.HasFlag(Condition.Panic) &&
                !ailment.HasFlag(Condition.Drugged) &&
                !ailment.HasFlag(Condition.Petrified) &&
                !ailment.HasFlag(Condition.Unused) && // the original code states that this disables casting as well
                !ailment.HasFlag(Condition.DeadCorpse) &&
                !ailment.HasFlag(Condition.DeadAshes) &&
                !ailment.HasFlag(Condition.DeadDust);
        }
    }
}
