using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemSlotFlags : byte
    {
        None = 0,
        /// <summary>
        /// Magic item is identified already.
        /// </summary>
        Identified = 0x01,
        /// <summary>
        /// Item is broken.
        /// 
        /// Can not be used nor equipped before repaired.
        /// </summary>
        Broken = 0x02,
        /// <summary>
        /// Only cursed equipment slots use this flag.
        /// 
        /// If true, the item bonus is negative. Affected are:
        /// - Bonus HP
        /// - Bonus SP
        /// - Damage
        /// - Defense
        /// - Bonus Attribute
        /// - Bonus Skill
        /// 
        /// Cursed items can only be removed by using a Remove Curse spell
        /// in which case they are destroyed completely. If a cursed item
        /// is not equipped, the curse has no effect until it is equipped.
        /// </summary>
        Cursed = 0x04,
        /// <summary>
        /// Advanced only. Can not drag the item.
        /// Mainly used for cat items which resemble skills.
        /// </summary>
        Locked = 0x08
    }
}
