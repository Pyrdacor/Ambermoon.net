using System;

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
}
