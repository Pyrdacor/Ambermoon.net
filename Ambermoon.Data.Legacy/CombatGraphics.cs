using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public static class CombatGraphics
    {
        public static readonly Dictionary<CombatGraphicIndex, CombatGraphicInfo> Info = new Dictionary<CombatGraphicIndex, CombatGraphicInfo>
        {
            { CombatGraphicIndex.FireBall, new CombatGraphicInfo(8, 16, 16) },
            { CombatGraphicIndex.BigFlame, new CombatGraphicInfo(8, 16, 32) },
            { CombatGraphicIndex.SmallFlame, new CombatGraphicInfo(6, 16, 16) },
            { CombatGraphicIndex.MagicProjectileHuman, new CombatGraphicInfo(24, 32, 32) },
            { CombatGraphicIndex.MagicProjectileMonster, new CombatGraphicInfo(12, 32, 32) },
            { CombatGraphicIndex.Whirlwind, new CombatGraphicInfo(8, 32, 32) },
            { CombatGraphicIndex.Blood, new CombatGraphicInfo(4, 32, 32) },
            { CombatGraphicIndex.SnowFlake, new CombatGraphicInfo(5, 16, 16) },
            { CombatGraphicIndex.GreenStar, new CombatGraphicInfo(5, 16, 16) },
            { CombatGraphicIndex.Lightning, new CombatGraphicInfo(2, 32, 32) },
            { CombatGraphicIndex.HolyBeam, new CombatGraphicInfo(1, 16, 1) },
            { CombatGraphicIndex.ArrowRedHuman, new CombatGraphicInfo(1, 16, 19) },
            { CombatGraphicIndex.ArrowRedMonster, new CombatGraphicInfo(1, 16, 19) },
            { CombatGraphicIndex.ArrowGreenHuman, new CombatGraphicInfo(1, 16, 20) },
            { CombatGraphicIndex.ArrowGreenMonster, new CombatGraphicInfo(1, 16, 20) },
            { CombatGraphicIndex.Slingstone, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.Slingdagger, new CombatGraphicInfo(1, 16, 15) },
            { CombatGraphicIndex.IceBall, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.LargeStone, new CombatGraphicInfo(1, 32, 16) },
            { CombatGraphicIndex.Landslide, new CombatGraphicInfo(1, 64, 32) },
            { CombatGraphicIndex.Waterdrop, new CombatGraphicInfo(1, 64, 32) },
            { CombatGraphicIndex.BlueBeam, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.GreenBeam, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.RedRing, new CombatGraphicInfo(1, 32, 32) },
            { CombatGraphicIndex.IconParalyze, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconPoison, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconPetrify, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconDisease, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconAging, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconIrritation, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconMadness, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconSleep, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconPanic, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconBlind, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.IconDrugs, new CombatGraphicInfo(1, 16, 16) },
            { CombatGraphicIndex.DeathAnimation, new CombatGraphicInfo(14, 48, 59) },
            { CombatGraphicIndex.AttackClaw, new CombatGraphicInfo(4, 32, 32) },
            { CombatGraphicIndex.IceBlock, new CombatGraphicInfo(2, 16, 32) },
            { CombatGraphicIndex.AttackSword, new CombatGraphicInfo(3, 16, 43) },
            { CombatGraphicIndex.SpellBlock, new CombatGraphicInfo(4, 32, 32) },
            { CombatGraphicIndex.FlyingSickle, new CombatGraphicInfo(4, 16, 13) },
            { CombatGraphicIndex.UISwordAndMace, new CombatGraphicInfo(1, 32, 36, 1, true) },
            { CombatGraphicIndex.BattleFieldIcons, new CombatGraphicInfo(35, 16, 14, 50) }
        };
    }
}
