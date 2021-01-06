using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;
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
        PlayerAtack // sword swing
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
        public bool MirrorX;
    }

    internal static class BattleEffects
    {
        static List<BattleEffectInfo> Effects(params BattleEffectInfo[] battleEffects) => new List<BattleEffectInfo>(battleEffects);

        public static readonly int[] RowYOffsets = new[] { 81, 88, 98, 111, 124 };

        static Position GetProjectileTargetPosition(IRenderView renderView, uint tile, Character[] battleField)
        {
            if (battleField[(int)tile] is Monster monster)
            {
                return Layout.GetMonsterCombatCenterPosition(renderView, (int)tile, monster);
            }
            else
            {
                return Layout.GetPlayerSlotTargetPosition((int)tile % 6);
            }
        }

        static Position GetCenterPosition(IRenderView renderView, uint tile, Character[] battleField, int yOffset = 0)
        {
            var offset = new Position(0, yOffset);

            if (battleField[(int)tile] is Monster monster)
            {
                return Layout.GetMonsterCombatCenterPosition(renderView, (int)tile, monster) + offset;
            }
            else
            {
                return Layout.GetPlayerSlotTargetPosition((int)tile % 6) + offset;
            }
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint sourceTile, uint targetTile, CombatGraphicIndex graphicIndex,
            Character[] battleField, uint duration, float startScale = 1.0f, float scaleChangePerY = 0.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetCenterPosition(renderView, sourceTile, battleField);
            var endPosition = GetCenterPosition(renderView, targetTile, battleField);
            float endScale = startScale + (endPosition.Y - startPosition.Y) * scaleChangePerY;

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
                InitialDisplayLayer = (byte)Math.Min(255, (sourceTile / 6) * 60 + 60), // display over the given row
                MirrorX = false
            };
        }

        static BattleEffectInfo CreateSimpleEffect(IRenderView renderView, uint tile, CombatGraphicIndex graphicIndex,
            Character[] battleField, uint duration, int yOffset = 0, float scale = 1.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var position = GetCenterPosition(renderView, tile, battleField, yOffset);
            scale *= GetScaleFromRow(renderView, tile, battleField);

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
                InitialDisplayLayer = (byte)Math.Min(255, (tile / 6) * 60 + 60), // display over the given row
                MirrorX = false
            };
        }

        static float GetScaleFromRow(IRenderView renderView, uint tile, Character[] battleField)
        {
            if (battleField[(int)tile]?.Type == CharacterType.PartyMember)
                return 2.0f;

            return renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
        }

        static BattleEffectInfo CreateFlyingEffect(IRenderView renderView, uint sourceTile, uint targetTile,
            CombatGraphicIndex graphicIndex, Character[] battleField, float baseScale = 1.0f)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var startPosition = GetProjectileTargetPosition(renderView, sourceTile, battleField);
            var endPosition = GetProjectileTargetPosition(renderView, targetTile, battleField);
            var sourceScale = GetScaleFromRow(renderView, sourceTile, battleField);
            var targetScale = GetScaleFromRow(renderView, targetTile, battleField);
            int maxY = Global.CombatBackgroundArea.Bottom;

            if (startPosition.Y > maxY)
                startPosition.Y = maxY;
            if (endPosition.Y > maxY)
                endPosition.Y = maxY;

            return new BattleEffectInfo
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                StartScale = sourceScale * baseScale,
                EndScale = targetScale * baseScale,
                StartTextureIndex = Graphics.CombatGraphicOffset + (uint)graphicIndex,
                FrameSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height),
                FrameCount = info.FrameCount,
                Duration = GetFlyDuration(sourceTile, targetTile),
                InitialDisplayLayer = (byte)Math.Min(255, (sourceTile / 6) * 60 + 60), // display over the given row
                MirrorX = battleField[(int)sourceTile] is Monster ? startPosition.X < endPosition.X : startPosition.X > endPosition.X
            };
        }

        public static uint GetFlyDuration(uint sourceTile, uint targetTile)
        {
            int sourceColumn = (int)sourceTile % 6;
            int sourceRow = (int)sourceTile / 6;
            int targetColumn = (int)targetTile % 6;
            int targetRow = (int)targetTile / 6;

            return Math.Max(Game.TicksPerSecond / 5, (uint)((Math.Abs(targetColumn - sourceColumn) + Math.Abs(targetRow - sourceRow) * 2) * Game.TicksPerSecond / 12));
        }

        public static List<BattleEffectInfo> GetEffectInfo(IRenderView renderView, BattleEffect battleEffect, uint sourceTile, uint targetTile,
            Character[] battleField, float scale = 1.0f)
        {
            return battleEffect switch
            {
                BattleEffect.HurtMonster => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.Blood, battleField, Game.TicksPerSecond / 2)),
                BattleEffect.HurtPlayer => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.AttackClaw, battleField, Game.TicksPerSecond / 2)),
                BattleEffect.MonsterArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenMonster, battleField)),
                BattleEffect.MonsterBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedMonster, battleField)),
                BattleEffect.PlayerArrowAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowGreenHuman, battleField)),
                BattleEffect.PlayerBoltAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.ArrowRedHuman, battleField)),
                BattleEffect.SlingstoneAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingstone, battleField)),
                BattleEffect.SlingdaggerAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.Slingdagger, battleField)),
                BattleEffect.SickleAttack => Effects(CreateFlyingEffect(renderView, sourceTile, targetTile, CombatGraphicIndex.FlyingSickle, battleField, 1.5f)),
                BattleEffect.Death => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.DeathAnimation, battleField, Game.TicksPerSecond * 5 / 4, 0, scale)),
                BattleEffect.BlockSpell => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.SpellBlock, battleField, Game.TicksPerSecond)),
                BattleEffect.PlayerAtack => Effects(CreateSimpleEffect(renderView, targetTile, CombatGraphicIndex.AttackSword, battleField, Game.TicksPerSecond / 4, -10)),
                _ => null
            };
        }
    }
}
