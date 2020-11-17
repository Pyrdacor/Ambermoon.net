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
        public byte InitialDisplayLayer;
    }

    internal static class BattleEffects
    {
        static List<BattleEffectInfo> Effects(params BattleEffectInfo[] battleEffects) => new List<BattleEffectInfo>(battleEffects);

        public static readonly int[] RowYOffsets = new[] { 82, 88, 100, 124, 130 };

        static Position GetCenterPosition(IRenderView renderView, uint tile, int yOffset = 0)
        {
            uint column = tile % 6;
            uint row = tile / 6;
            var combatBackgroundArea = Global.CombatBackgroundArea;
            int centerX = combatBackgroundArea.Width / 2;
            float sizeMultiplier = GetScaleFromRow(renderView, row);
            int slotWidth = Util.Round(40 * sizeMultiplier);
            int slotHeight = Util.Round(100 * sizeMultiplier); // TODO
            return new Position(centerX - (3 - (int)column) * slotWidth, combatBackgroundArea.Y + RowYOffsets[row] - slotHeight / 2 + Util.Round(yOffset * sizeMultiplier));
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint sourceTile, uint targetTile, CombatGraphicIndex graphicIndex,
            uint duration, float startScale = 1.0f, float scaleChangePerY = 0.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetCenterPosition(renderView, sourceTile);
            var endPosition = GetCenterPosition(renderView, targetTile);
            float endScale = startScale + (endPosition.Y - startPosition.Y) * scaleChangePerY;

            startPosition -= new Position(Util.Round(0.5f * info.GraphicInfo.Width), Util.Round(0.5f * info.GraphicInfo.Height));
            endPosition -= new Position(Util.Round(0.5f * info.GraphicInfo.Width), Util.Round(0.5f * info.GraphicInfo.Height));

            return new BattleEffectInfo
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartScale = startScale,
                EndScale = endScale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = duration,
                InitialDisplayLayer = (byte)sourceTile
            };
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint tile, CombatGraphicIndex graphicIndex,
            uint duration, int yOffset = 0, float scale = 1.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var position = GetCenterPosition(renderView, tile, yOffset);
            scale *= GetScaleFromRow(renderView, tile / 6);

            return new BattleEffectInfo
            {
                StartPosition = position,
                EndPosition = position,
                StartScale = scale,
                EndScale = scale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = duration,
                InitialDisplayLayer = (byte)tile
            };
        }

        static float GetScaleFromRow(IRenderView renderView, uint row)
        {
            var monsterRow = row > (uint)MonsterRow.Near ? MonsterRow.Near : (MonsterRow)row;
            return renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
        }

        static BattleEffectInfo CreateFlyingEffect(IRenderView renderView, uint sourceTile, uint targetTile, CombatGraphicIndex graphicIndex)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetCenterPosition(renderView, sourceTile);
            var endPosition = GetCenterPosition(renderView, targetTile);
            var sourceScale = GetScaleFromRow(renderView, sourceTile / 6);
            var targetScale = GetScaleFromRow(renderView, targetTile / 6);

            return new BattleEffectInfo
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartScale = sourceScale,
                EndScale = targetScale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = GetFlyDuration(sourceTile, targetTile),
                InitialDisplayLayer = (byte)sourceTile
            };
        }

        static uint GetFlyDuration(uint sourceTile, uint targetTile)
        {
            int sourceColumn = (int)sourceTile % 6;
            int sourceRow = (int)sourceTile / 6;
            int targetColumn = (int)targetTile % 6;
            int targetRow = (int)targetTile / 6;

            return (uint)((Math.Abs(targetColumn - sourceColumn) + Math.Abs(targetRow - sourceRow) * 2) * Game.TicksPerSecond / 12);
        }

        public static List<BattleEffectInfo> GetEffectInfo(IRenderView renderView, BattleEffect battleEffect, uint sourceTile, uint targetTile, float scale = 1.0f)
        {
            return battleEffect switch
            {
                BattleEffect.HurtMonster => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.Blood, Game.TicksPerSecond / 2)),
                BattleEffect.HurtPlayer => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.AttackClaw, Game.TicksPerSecond / 2)),
                BattleEffect.MonsterArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenMonster)),
                BattleEffect.MonsterBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedMonster)),
                BattleEffect.PlayerArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenHuman)),
                BattleEffect.PlayerBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedHuman)),
                BattleEffect.SlingstoneAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingstone)),
                BattleEffect.SlingdaggerAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingdagger)),
                BattleEffect.SickleAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.FlyingSickle)),
                BattleEffect.Death => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.DeathAnimation, Game.TicksPerSecond * 2, -10, scale)), // I guess scale is monster dependent
                BattleEffect.BlockSpell => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.SpellBlock, Game.TicksPerSecond)),
                BattleEffect.PlayerAtack => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.AttackSword, Game.TicksPerSecond / 3)),
                _ => null // TODO spells
            };
        }
    }
}
