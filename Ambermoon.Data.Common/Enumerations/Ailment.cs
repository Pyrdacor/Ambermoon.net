using System;
using System.ComponentModel;

namespace Ambermoon.Data
{
    [Flags]
    public enum Ailment
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
    public static class AilmentExtensions
    {
        public static Ailment WithoutBattleOnlyAilments(this Ailment ailments)
        {
            return (Ailment)((int)ailments & 0xfff2);
        }

        public static bool CanFight(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanSelect(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Panic) &&
                !ailment.HasFlag(Ailment.Crazy) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanOpenInventory(this Ailment ailment)
        {
            // TODO
            return
                !ailment.HasFlag(Ailment.Crazy) &&
                !ailment.HasFlag(Ailment.Panic) &&
                !ailment.HasFlag(Ailment.Petrified);
        }

        public static bool CanMove(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Lamed) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanFlee(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Lamed) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanAttack(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Panic) &&
                !ailment.HasFlag(Ailment.Lamed) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanParry(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Panic) &&
                !ailment.HasFlag(Ailment.Exhausted) &&
                !ailment.HasFlag(Ailment.Lamed) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }

        public static bool CanCastSpell(this Ailment ailment)
        {
            return
                !ailment.HasFlag(Ailment.Irritated) &&
                !ailment.HasFlag(Ailment.Sleep) &&
                !ailment.HasFlag(Ailment.Panic) &&
                !ailment.HasFlag(Ailment.Drugged) &&
                !ailment.HasFlag(Ailment.Petrified) &&
                !ailment.HasFlag(Ailment.Unused) && // the original code states that this disables casting as well
                !ailment.HasFlag(Ailment.DeadCorpse) &&
                !ailment.HasFlag(Ailment.DeadAshes) &&
                !ailment.HasFlag(Ailment.DeadDust);
        }
    }
}
