using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public static class MonsterGraphics
    {
        public static readonly Dictionary<MonsterGraphicIndex, MonsterGraphicInfo> Info = new Dictionary<MonsterGraphicIndex, MonsterGraphicInfo>
        {
            { MonsterGraphicIndex.Gargoyle, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 67,
                    FirstIdleAnimationFrame = 13, // TODO
                    IdleAnimationFrameCount = 1, // TODO
                    FirstAttackFrame = 1,
                    AttackFrameCount = 6,
                    FirstCastFrame = 10,
                    CastFrameCount = 3,
                    FirstMoveFrame = 7,
                    MoveFrameCount = 3,
                    HurtFrame = 14
                }
            },
            { MonsterGraphicIndex.Undead, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 67,
                    FirstIdleAnimationFrame = 1,
                    IdleAnimationFrameCount = 2,
                    FirstAttackFrame = 1,
                    AttackFrameCount = 2,
                    FirstCastFrame = 3,
                    CastFrameCount = 2,
                    FirstMoveFrame = 1,
                    MoveFrameCount = 2,
                    HurtFrame = 5
                }
            },
            { MonsterGraphicIndex.Demon, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 80,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Orc, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 78,
                    // TODO ...
                }
            },

            { MonsterGraphicIndex.Lizard, new MonsterGraphicInfo
                {
                    Width = 80,
                    Height = 84,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Giant, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 64,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Knight, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 65,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.MoragDragon, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 84,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Golem, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 80,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Hyrda, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 96,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Magician, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 68,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Minotaur, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 71,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Nera, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 84,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.MagicGuard, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 64,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.FireDragon, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 84,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Spider, new MonsterGraphicInfo
                {
                    Width = 80,
                    Height = 54,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Bandit, new MonsterGraphicInfo
                {
                    Width = 48,
                    Height = 65,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Beast, new MonsterGraphicInfo
                {
                    Width = 80,
                    Height = 78,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.EnergySphere, new MonsterGraphicInfo
                {
                    Width = 48,
                    Height = 52,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.MoranianMagician, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 68,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.AntiqueGuard, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 80,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.CurseWesp, new MonsterGraphicInfo
                {
                    Width = 64,
                    Height = 48,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Tornak, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 88,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.Gizzek, new MonsterGraphicInfo
                {
                    Width = 96,
                    Height = 75,
                    // TODO ...
                }
            },
            { MonsterGraphicIndex.MoragMachine, new MonsterGraphicInfo
                {
                    Width = 80,
                    Height = 66,
                    // TODO ...
                }
            },
        };
    }
}
