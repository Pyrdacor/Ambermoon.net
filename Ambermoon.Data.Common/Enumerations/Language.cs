using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum Language
    {
        None = 0,
        Human = 0x01,
        Elfish = 0x02,
        Dwarfish = 0x04,
        Gnomish = 0x08,
        Sylphic = 0x10,
        Felinic = 0x20,
        Morag = 0x40,
        Animal = 0x80
    }
}
