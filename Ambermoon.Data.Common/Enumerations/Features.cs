using System;

namespace Ambermoon.Data.Enumerations
{
    [Flags]
    public enum Features
    {
        None = 0x00,
        Elements = 0x01,
        AdjustedSpellDamage = 0x02,
        AmbermoonAdvanced = Elements | AdjustedSpellDamage
    }
}
