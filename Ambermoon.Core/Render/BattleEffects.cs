using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    internal enum BattleEffect
    {
        HurtMonster, // blood
        HurtPlayer, // claw
        MonsterArrowAttack, // green arrow
        MonsterBoltAttack, // red arrow
        PlayerArrowAttack, // green arrow
        PlayerBoltAttack, // red arrow
        SlingstoneAttack,
        SlingdaggerAttack,
        SickleAttack,
        Death, // looks like a fire pillar to be honest
        BlockSpell, // blueish ring
        PlayerAtack, // sword swing
        // TODO: spells
    }

    internal struct BattleEffectInfo
    {
        public Position StartPosition;
        public Position EndPosition;
        public float StartScale;
        public float EndScale;
        public uint StartTextureIndex;
        public Size FrameSize;
        public uint FrameCount;
        public uint Duration;
    }

    internal static class BattleEffects
    {
        static List<BattleEffectInfo> Effects(params BattleEffectInfo[] battleEffects) => new List<BattleEffectInfo>(battleEffects);

        static readonly int[] RowYOffsets = new[] { 82, 88, 100, 124 };

        static Position GetCenterPosition(IRenderView renderView, uint tile)
        {
            uint column = tile % 6;
            uint row = tile / 6;
            var combatBackgroundArea = Global.CombatBackgroundArea;
            int centerX = combatBackgroundArea.Width / 2;
            float sizeMultiplier = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);
            int slotWidth = Util.Round(40 * sizeMultiplier);
            int slotHeight = Util.Round(64 * sizeMultiplier); // TODO
            return new Position(centerX - (3 - (int)column) * slotWidth, combatBackgroundArea.Y + RowYOffsets[row] - slotHeight / 2);
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint sourceTile, uint targetTile, CombatGraphicIndex graphicIndex,
            uint duration, float startScale = 1.0f, float scaleChangePerY = 0.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetCenterPosition(renderView, sourceTile);
            var endPosition = GetCenterPosition(renderView, targetTile);
            float endScale = startScale + (endPosition.Y - startPosition.Y) * scaleChangePerY;

            startPosition -= new Position(Util.Round(0.5f * startScale * info.GraphicInfo.Width), Util.Round(0.5f * startScale * info.GraphicInfo.Height));
            endPosition -= new Position(Util.Round(0.5f * endScale * info.GraphicInfo.Width), Util.Round(0.5f * endScale * info.GraphicInfo.Height));

            return new BattleEffectInfo
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartScale = startScale,
                EndScale = endScale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = duration
            };
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint tile, CombatGraphicIndex graphicIndex,
            uint duration, float scale = 1.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var position = GetCenterPosition(renderView, tile);

            return new BattleEffectInfo
            {
                StartPosition = position,
                EndPosition = position,
                StartScale = scale,
                EndScale = scale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = duration
            };
        }

        static BattleEffectInfo CreateFlyingEffect(IRenderView renderView, uint sourceTile, uint targetTile, CombatGraphicIndex graphicIndex)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetCenterPosition(renderView, sourceTile);
            var endPosition = GetCenterPosition(renderView, targetTile);
            var sourceScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(sourceTile / 6));
            var targetScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(targetTile / 6));

            return new BattleEffectInfo
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartScale = sourceScale,
                EndScale = targetScale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = GetFlyDuration(sourceTile, targetTile)
            };
        }

        static uint GetFlyDuration(uint sourceTile, uint targetTile)
        {
            uint sourceColumn = sourceTile % 6;
            uint sourceRow = sourceTile / 6;
            uint targetColumn = targetTile % 6;
            uint targetRow = targetTile / 6;

            return (uint)((Math.Abs(targetColumn - sourceColumn) + Math.Abs(targetRow - sourceRow) * 2) * Game.TicksPerSecond / 4);
        }

        public static List<BattleEffectInfo> GetEffectInfo(IRenderView renderView, BattleEffect battleEffect, uint sourceTile, uint targetTile)
        {
            return battleEffect switch
            {
                BattleEffect.HurtMonster => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.Blood, Game.TicksPerSecond)),
                BattleEffect.HurtPlayer => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.AttackClaw, Game.TicksPerSecond)),
                BattleEffect.MonsterArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenMonster)),
                BattleEffect.MonsterBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedMonster)),
                BattleEffect.PlayerArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenHuman)),
                BattleEffect.PlayerBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedHuman)),
                BattleEffect.SlingstoneAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingstone)),
                BattleEffect.SlingdaggerAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingdagger)),
                BattleEffect.SickleAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.FlyingSickle)),
                BattleEffect.Death => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.DeathAnimation, Game.TicksPerSecond)),
                BattleEffect.BlockSpell => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.SpellBlock, Game.TicksPerSecond)),
                BattleEffect.PlayerAtack => Effects(CreateSimpleEffect(renderView, sourceTile, CombatGraphicIndex.AttackSword, Game.TicksPerSecond / 4)),
                _ => null // TODO spells
            };
        }
    }
}
