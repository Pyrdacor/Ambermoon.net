using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Render
{
    internal class SpellAnimation
    {
        readonly Game game;
        readonly Layout layout;
        readonly Battle battle;
        readonly IRenderView renderView;
        readonly Spell spell;
        readonly List<BattleAnimation> animations = new List<BattleAnimation>();
        IColoredRect colorOverlay;
        readonly ITextureAtlas textureAtlas;
        readonly bool fromMonster;
        readonly int startPosition;
        readonly int targetRow;
        int lastPosition = -1;
        Action finishAction;

        public SpellAnimation(Game game, Layout layout, Battle battle, Spell spell,
            bool fromMonster, int sourcePosition, int targetRow = 0)
        {
            this.game = game;
            this.layout = layout;
            this.battle = battle;
            renderView = layout.RenderView;
            this.spell = spell;
            this.fromMonster = fromMonster;
            startPosition = sourcePosition;
            this.targetRow = targetRow;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleEffects);
        }

        public SpellAnimation(Game game, Layout layout)
        {
            this.game = game;
            this.layout = layout;
            renderView = layout.RenderView;
            fromMonster = false;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleEffects);
        }

        void ShowOverlay(Color color)
        {
            colorOverlay?.Delete();
            colorOverlay = layout.CreateArea(Global.CombatBackgroundArea, color, 100, FilledAreaType.CustomEffect);
        }

        void HideOverlay()
        {
            colorOverlay?.Delete();
            colorOverlay = null;
        }

        void RemoveAnimation(BattleAnimation animation, bool callGlobalFinish = true)
        {
            animation?.Destroy();
            animations.Remove(animation);

            if (animations.Count == 0 && callGlobalFinish)
                finishAction?.Invoke();
        }

        BattleAnimation AddAnimation(CombatGraphicIndex graphicIndex, int[] frameIndices, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 255, Action finishAction = null,
            Size customBaseSize = null, BattleAnimation.AnimationScaleType scaleType = BattleAnimation.AnimationScaleType.Both,
            BattleAnimation.HorizontalAnchor anchorX = BattleAnimation.HorizontalAnchor.Center,
            BattleAnimation.VerticalAnchor anchorY = BattleAnimation.VerticalAnchor.Center,
            bool mirrorX = false, byte palette = 17, byte[] maskColors = null, bool removeWhenFinished = true)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var textureSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height);
            var size = customBaseSize ?? textureSize;
            var sprite = renderView.SpriteFactory.Create(size.Width, size.Height, true, displayLayer) as ILayerSprite;
            sprite.ClipArea = Global.CombatBackgroundArea;
            sprite.Layer = renderView.GetLayer(Layer.BattleEffects);
            sprite.PaletteIndex = palette;
            sprite.MaskColor = maskColors == null ? (byte?)null : maskColors[0];
            sprite.TextureSize = textureSize;
            sprite.Visible = true;
            var animation = new BattleAnimation(sprite);
            void AnimationEnded()
            {
                animation.AnimationFinished -= AnimationEnded;

                if (removeWhenFinished)
                    RemoveAnimation(animation, finishAction == null);

                if (finishAction != null)
                    finishAction?.Invoke();
            }
            animation.AnimationFinished += AnimationEnded;
            animation.ScaleType = scaleType;
            animation.SetStartFrame(textureAtlas.GetOffset(Graphics.CombatGraphicOffset + (uint)graphicIndex),
                size, startPosition, startScale, mirrorX, textureSize, anchorX, anchorY);
            animation.Play(frameIndices, duration / (uint)frameIndices.Length, game.CurrentBattleTicks, endPosition, endScale);
            animations.Add(animation);

            if (maskColors != null)
            {
                void UpdateMask(float progress)
                {
                    sprite.MaskColor = maskColors[Math.Min(maskColors.Length - 1, Util.Round(progress * maskColors.Length))];
                }

                void FinishMasking()
                {
                    animation.AnimationUpdated -= UpdateMask;
                    animation.AnimationFinished -= FinishMasking;
                }

                animation.AnimationUpdated += UpdateMask;
                animation.AnimationFinished += FinishMasking;
            }

            return animation;
        }

        BattleAnimation AddAnimationThatRemains(CombatGraphicIndex graphicIndex, int numFrames, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 255, Action finishAction = null)
        {
            return AddAnimationThatRemains(graphicIndex, Enumerable.Range(0, numFrames).ToArray(), startPosition, endPosition, duration, startScale,
                endScale, displayLayer, finishAction);
        }

        BattleAnimation AddAnimationThatRemains(CombatGraphicIndex graphicIndex, int[] frameIndices, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 255, Action finishAction = null)
        {
            return AddAnimation(graphicIndex, frameIndices, startPosition, endPosition, duration, startScale,
                endScale, displayLayer, finishAction, null, BattleAnimation.AnimationScaleType.Both, BattleAnimation.HorizontalAnchor.Center,
                BattleAnimation.VerticalAnchor.Center, false, 17, null, false);
        }

        BattleAnimation AddAnimation(CombatGraphicIndex graphicIndex, int numFrames, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 255, Action finishAction = null,
            Size customBaseSize = null, BattleAnimation.AnimationScaleType scaleType = BattleAnimation.AnimationScaleType.Both,
            BattleAnimation.HorizontalAnchor anchorX = BattleAnimation.HorizontalAnchor.Center,
            BattleAnimation.VerticalAnchor anchorY = BattleAnimation.VerticalAnchor.Center, bool mirrorX = false, byte palette = 17)
        {
            return AddAnimation(graphicIndex, Enumerable.Range(0, numFrames).ToArray(), startPosition, endPosition,
                duration, startScale, endScale, displayLayer, finishAction, customBaseSize, scaleType, anchorX, anchorY, mirrorX, palette);
        }

        BattleAnimation AddAnimation(CombatGraphicIndex graphicIndex, int numFrames, Position startPosition, Position endPosition,
            uint duration, float startScale, float endScale, byte displayLayer, Action finishAction, byte palette)
        {
            return AddAnimation(graphicIndex, Enumerable.Range(0, numFrames).ToArray(), startPosition, endPosition,
                duration, startScale, endScale, displayLayer, finishAction, null, BattleAnimation.AnimationScaleType.Both,
                BattleAnimation.HorizontalAnchor.Center, BattleAnimation.VerticalAnchor.Center, false, palette);
        }

        BattleAnimation AddMaskedAnimation(CombatGraphicIndex graphicIndex, Position startPosition, Position endPosition,
            uint duration, float startScale, float endScale, byte displayLayer, Action finishAction, byte[] maskColors, byte palette)
        {
            return AddAnimation(graphicIndex, Enumerable.Repeat(0, maskColors.Length).ToArray(), startPosition, endPosition,
                duration, startScale, endScale, displayLayer, finishAction, null, BattleAnimation.AnimationScaleType.Both,
                BattleAnimation.HorizontalAnchor.Center, BattleAnimation.VerticalAnchor.Center, false, palette, maskColors);
        }

        BattleAnimation AddPortraitAnimation(int slot, UICustomGraphic graphicIndex, Size frameSize, int frameCount, Position startOffset,
            Position endOffset, uint duration, Action finishAction = null)
        {
            var area = Global.PartyMemberPortraitAreas[slot];
            var sprite = renderView.SpriteFactory.Create(frameSize.Width, frameSize.Height, true, 200) as ILayerSprite;
            sprite.ClipArea = area;
            sprite.Layer = renderView.GetLayer(Layer.UI);
            sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
            sprite.TextureSize = frameSize;
            sprite.Visible = true;
            var animation = new BattleAnimation(sprite);
            void AnimationEnded()
            {
                animation.AnimationFinished -= AnimationEnded;
                RemoveAnimation(animation, finishAction == null);

                if (finishAction != null)
                    finishAction?.Invoke();
            }
            animation.AnimationFinished += AnimationEnded;
            animation.ScaleType = BattleAnimation.AnimationScaleType.None;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            animation.SetStartFrame(textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(graphicIndex)), frameSize,
                area.Position + startOffset, 1.0f, false, frameSize, BattleAnimation.HorizontalAnchor.Left, BattleAnimation.VerticalAnchor.Top);
            var ticks = battle != null ? game.CurrentBattleTicks : game.CurrentAnimationTicks;
            animation.Play(Enumerable.Range(0, frameCount).ToArray(), duration / (uint)frameCount, ticks, area.Position + endOffset);
            animations.Add(animation);
            return animation;
        }

        float GetScaleXRelativeToCombatArea(int baseWidth, float factor)
        {
            return (Global.CombatBackgroundArea.Width * factor) / baseWidth;
        }

        float GetScaleYRelativeToCombatArea(int baseHeight, float factor)
        {
            return (Global.CombatBackgroundArea.Height * factor) / baseHeight;
        }

        Position GetSourcePosition()
        {
            switch (spell)
            {
                case Spell.HealingHand:
                case Spell.RemoveFear:
                case Spell.RemovePanic:
                case Spell.RemoveShadows:
                case Spell.RemoveBlindness:
                case Spell.RemovePain:
                case Spell.RemoveDisease:
                case Spell.SmallHealing:
                case Spell.RemovePoison:
                case Spell.NeutralizePoison:
                case Spell.MediumHealing:
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                case Spell.GreatHealing:
                case Spell.MassHealing:
                case Spell.RemoveRigidness:
                case Spell.RemoveLamedness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.RestoreStamina:
                case Spell.Blink:
                case Spell.Escape:
                case Spell.MagicalShield:
                case Spell.MagicalWall:
                case Spell.MagicalBarrier:
                case Spell.MagicalWeapon:
                case Spell.MagicalAssault:
                case Spell.MagicalAttack:
                case Spell.AntiMagicWall:
                case Spell.AntiMagicSphere:
                case Spell.Hurry:
                case Spell.MassHurry:
                case Spell.MonsterKnowledge:
                case Spell.ShowMonsterLP:
                case Spell.Lame:
                case Spell.Poison:
                case Spell.Petrify:
                case Spell.CauseDisease:
                case Spell.CauseAging:
                case Spell.Irritate:
                case Spell.CauseMadness:
                case Spell.Sleep:
                case Spell.Fear:
                case Spell.Blind:
                case Spell.Drug:
                case Spell.DissolveVictim:
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                case Spell.Firestorm:
                case Spell.Firepillar:
                case Spell.Waterfall:
                case Spell.Icestorm:
                case Spell.Iceshower:
                case Spell.SpellPointsI:
                case Spell.SpellPointsII:
                case Spell.SpellPointsIII:
                case Spell.SpellPointsIV:
                case Spell.SpellPointsV:
                case Spell.AllHealing:
                case Spell.AddStrength:
                case Spell.AddIntelligence:
                case Spell.AddDexterity:
                case Spell.AddSpeed:
                case Spell.AddStamina:
                case Spell.AddCharisma:
                case Spell.AddLuck:
                case Spell.AddAntiMagic:
                case Spell.Drugs:
                    // Those spells have no source position. They just appear somewhere.
                    // Or they only work with the target position. GetSourcePosition should
                    // never be called for those spells so we throw here.
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} should not use a source position.");
                case Spell.GhostWeapon:
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.Firebeam:
                case Spell.Fireball:
                case Spell.Iceball:
                {
                    Position position;
                    if (fromMonster)
                    {
                        position = layout.GetMonsterCombatCenterPosition(startPosition, battle.GetCharacterAt(startPosition) as Monster);
                    }
                    else
                    {
                        position = Layout.GetPlayerSlotTargetPosition(startPosition % 6);
                    }
                    if (position.Y > Global.CombatBackgroundArea.Bottom - 20)
                        position.Y = Global.CombatBackgroundArea.Bottom - 20;
                    return position;
                }
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                {
                    Position position;
                    if (fromMonster)
                    {
                        position = layout.GetMonsterCombatCenterPosition(startPosition, battle.GetCharacterAt(startPosition) as Monster);
                    }
                    else
                    {
                        position = new Position(startPosition % 6 < 3 ? Global.CombatBackgroundArea.Left + 32 : Global.CombatBackgroundArea.Right - 32,
                            Global.CombatBackgroundArea.Top + 64);
                    }
                    return position;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }
        }

        Position GetTargetPosition(int position)
        {
            switch (spell)
            {
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                    return Layout.GetMonsterCombatGroundPosition(renderView, position);
                case Spell.HealingHand:
                case Spell.RemoveFear:
                case Spell.RemovePanic:
                case Spell.RemoveShadows:
                case Spell.RemoveBlindness:
                case Spell.RemovePain:
                case Spell.RemoveDisease:
                case Spell.SmallHealing:
                case Spell.RemovePoison:
                case Spell.NeutralizePoison:
                case Spell.MediumHealing:
                case Spell.GreatHealing:
                case Spell.MassHealing:
                case Spell.RemoveRigidness:
                case Spell.RemoveLamedness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.RestoreStamina:
                case Spell.MagicalShield:
                case Spell.MagicalWall:
                case Spell.MagicalBarrier:
                case Spell.MagicalWeapon:
                case Spell.MagicalAssault:
                case Spell.MagicalAttack:
                case Spell.AntiMagicWall:
                case Spell.AntiMagicSphere:
                case Spell.Hurry:
                case Spell.MassHurry:
                case Spell.ShowMonsterLP:
                case Spell.Earthquake:
                case Spell.Blink:
                case Spell.Escape:
                case Spell.SpellPointsI:
                case Spell.SpellPointsII:
                case Spell.SpellPointsIII:
                case Spell.SpellPointsIV:
                case Spell.SpellPointsV:
                case Spell.AllHealing:
                case Spell.AddStrength:
                case Spell.AddIntelligence:
                case Spell.AddDexterity:
                case Spell.AddSpeed:
                case Spell.AddStamina:
                case Spell.AddCharisma:
                case Spell.AddLuck:
                case Spell.AddAntiMagic:
                case Spell.Drugs:
                    // Those spells have no target position. They are just visible on portraits or not at all.
                    // GetTargetPosition should never be called for those spells so we throw here.
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} should not use a target position.");
                case Spell.GhostWeapon:
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.MonsterKnowledge:
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                case Spell.Lame:
                case Spell.Poison:
                case Spell.Petrify:
                case Spell.CauseDisease:
                case Spell.CauseAging:
                case Spell.Irritate:
                case Spell.CauseMadness:
                case Spell.Sleep:
                case Spell.Fear:
                case Spell.Blind:
                case Spell.Drug:
                case Spell.Firebeam:
                case Spell.Fireball:
                case Spell.Firestorm:
                case Spell.Firepillar:
                case Spell.Iceball:
                case Spell.Icestorm:
                case Spell.Iceshower:
                {
                    Position targetPosition;
                    if (fromMonster) // target is party member
                    {
                        targetPosition = Layout.GetPlayerSlotCenterPosition(position % 6);
                    }
                    else // target is monster
                    {
                        targetPosition = layout.GetMonsterCombatCenterPosition(position, battle.GetCharacterAt(position) as Monster);
                    }
                    if (targetPosition.Y > Global.CombatBackgroundArea.Bottom - 20)
                        targetPosition.Y = Global.CombatBackgroundArea.Bottom - 20;
                    return targetPosition;
                }
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Whirlwind:
                {
                    int row = position / 6;
                    if (fromMonster && (spell != Spell.Whirlwind || row != 0)) // target is party member
                    {
                        return Layout.GetPlayerSlotCenterPosition(position % 6) + new Position(0, 10 + row * 2);
                    }
                    else // target is monster
                    {
                        int yOffset = Util.Round(32 * renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row)) + 8;
                        if (spell != Spell.Winddevil)
                            yOffset += 6;
                        return Layout.GetMonsterCombatGroundPosition(renderView, position) - new Position(0, yOffset);
                    }
                }
                case Spell.Thunderbolt:
                {
                    if (fromMonster) // target is party member
                    {
                        var targetPosition = Layout.GetPlayerSlotCenterPosition(position % 6);
                        targetPosition.Y = Global.CombatBackgroundArea.Center.Y;
                        return targetPosition;
                    }
                    else // target is monster
                    {
                        int row = position / 6;
                        int yOffset = Util.Round(32 * renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row)) + 16;
                        return Layout.GetMonsterCombatGroundPosition(renderView, position) - new Position(0, yOffset);
                    }
                }
                case Spell.Mudsling:
                case Spell.Rockfall:
                {
                    if (fromMonster) // target is party member
                    {
                        return Layout.GetPlayerSlotCenterPosition(position % 6);
                    }
                    else // target is monster
                    {
                        return layout.GetMonsterCombatTopPosition(position, battle.GetCharacterAt(position) as Monster) - new Position(0, 6);
                    }
                }
                case Spell.Earthslide:
                {
                    if (fromMonster) // target is party member
                    {
                        return new Position(0, Global.CombatBackgroundArea.Bottom + 10);
                    }
                    else // target is monster
                    {
                        return Layout.GetMonsterCombatGroundPosition(renderView, position);
                    }
                }
                case Spell.Waterfall:
                {
                    if (fromMonster) // target is party member
                    {
                        return Layout.GetPlayerSlotTargetPosition(position % 6) + new Position(0, 10);
                    }
                    else // target is monster
                    {
                        return Layout.GetMonsterCombatGroundPosition(renderView, position) - new Position(0, 4);
                    }
                }
                case Spell.DissolveVictim:
                {
                    Position targetPosition;
                    if (fromMonster) // target is party member
                    {
                        targetPosition = Layout.GetPlayerSlotCenterPosition(position % 6); // TODO: is this right? Is this spell working on players in general?
                    }
                    else // target is monster
                    {
                        targetPosition = Layout.GetMonsterCombatGroundPosition(renderView, position);
                    }
                    return targetPosition;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }
        }

        void PlayHealingAnimation(PartyMember partyMember, Action finishAction)
        {
            int slot = game.SlotFromPartyMember(partyMember).Value;
            const int starCount = 25;
            int remainingStars = starCount;

            void FallingStarFinished()
            {
                if (--remainingStars == 0)
                    finishAction?.Invoke();
            }

            // x and y are relative to the portrait area
            void PlayFallingStar(int x, int y)
            {
                AddPortraitAnimation(slot, UICustomGraphic.HealingStarAnimation, new Size(7, 7), 3, new Position(x, y),
                    new Position(x, y + 16), Game.TicksPerSecond / 3, FallingStarFinished);
            }

            for (int i = 0; i < starCount; ++i)
            {
                game.AddTimedEvent(TimeSpan.FromMilliseconds(game.RandomInt(0, 300)), () =>
                {
                    PlayFallingStar(game.RandomInt(0, 46) - 6, game.RandomInt(0, 18) - 6);
                });
            }
        }

        /// <summary>
        /// This is used only outside of battles.
        /// For mass spells use <see cref="Play"/> instead.
        /// </summary>
        /// <param name="partyMember">The target party member.</param>
        /// <param name="finishAction">Action which is performed after the animation has finished.</param>
        public void CastOn(Spell spell, PartyMember partyMember, Action finishAction)
        {
            switch (spell)
            {
                case Spell.HealingHand:
                case Spell.RemoveFear:
                case Spell.RemoveShadows:
                case Spell.RemovePain:
                case Spell.SmallHealing:
                case Spell.RemovePoison:
                case Spell.MediumHealing:
                case Spell.GreatHealing:
                case Spell.RemoveRigidness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.Hurry:
                case Spell.WakeTheDead:
                case Spell.Resurrection:
                case Spell.SpellPointsI:
                case Spell.SpellPointsII:
                case Spell.SpellPointsIII:
                case Spell.SpellPointsIV:
                case Spell.SpellPointsV:
                case Spell.AllHealing:
                case Spell.AddStrength:
                case Spell.AddIntelligence:
                case Spell.AddDexterity:
                case Spell.AddSpeed:
                case Spell.AddStamina:
                case Spell.AddCharisma:
                case Spell.AddLuck:
                case Spell.AddAntiMagic:
                    PlayHealingAnimation(partyMember, finishAction);
                    break;
                default:
                    finishAction?.Invoke();
                    break;
            }
        }

        public void CastHealingOnPartyMembers(Action finishAction, bool reviving)
        {
            var partyMembers = game.PartyMembers.Where(p => p.Alive != reviving).ToList();

            for (int i = 0; i < partyMembers.Count; ++i)
                PlayHealingAnimation(partyMembers[i], i == partyMembers.Count - 1 ? finishAction : null);
        }

        /// <summary>
        /// This starts the spell animation (e.g. color overlays, starting animation).
        /// If a spell has only a per-target effects, this function does nothing.
        /// </summary>
        public void Play(Action finishAction)
        {
            this.finishAction = finishAction;

            switch (spell)
            {
                case Spell.HealingHand:
                case Spell.RemoveFear:
                case Spell.RemovePanic:
                case Spell.RemoveShadows:
                case Spell.RemoveBlindness:
                case Spell.RemovePain:
                case Spell.RemoveDisease:
                case Spell.SmallHealing:
                case Spell.RemovePoison:
                case Spell.NeutralizePoison:
                case Spell.MediumHealing:
                case Spell.GreatHealing:
                case Spell.MassHealing:
                case Spell.RemoveRigidness:
                case Spell.RemoveLamedness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.Hurry:
                case Spell.MassHurry:
                case Spell.CreateFood:
                {
                    if (fromMonster)
                    {
                        // No effect if monster casts those.
                        this.finishAction?.Invoke();
                    }
                    else
                    {
                        // These spells only show some redish falling stars above the portraits.
                        var massSpell = SpellInfos.Entries[spell].Target != SpellTarget.SingleFriend;

                        if (massSpell)
                        {
                            var partyMembers = (battle != null ? battle.PartyMembers : game.PartyMembers).Where(p => p.Alive).ToList();

                            for (int i = 0; i < partyMembers.Count; ++i)
                                PlayHealingAnimation(partyMembers[i], i == partyMembers.Count - 1 ? this.finishAction : null);
                        }
                        else
                        {
                            // For single target spells this is handled in MoveTo.
                            this.finishAction?.Invoke();
                        }
                    }
                    break;
                }
                case Spell.RestoreStamina:
                    // This doesn't seem to have any visual effect.
                    this.finishAction?.Invoke();
                    break;
                case Spell.MagicalShield:
                case Spell.MagicalWall:
                case Spell.MagicalBarrier:
                case Spell.MagicalWeapon:
                case Spell.MagicalAssault:
                case Spell.MagicalAttack:
                case Spell.AntiMagicWall:
                case Spell.AntiMagicSphere:
                    // Buffs have no animation at all.
                    this.finishAction?.Invoke();
                    break;
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                case Spell.DissolveVictim:
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.GhostWeapon:
                case Spell.MonsterKnowledge:
                    // Those spells use only the MoveTo method.
                    this.finishAction?.Invoke();
                    break;
                case Spell.ShowMonsterLP:
                {
                    if (fromMonster)
                    {
                        this.finishAction?.Invoke();
                    }
                    else
                    {
                        int lowerWidth = Global.CombatBackgroundArea.Width;
                        int upperWidth = 140;
                        int upperXOffset = 92;
                        int sourceYOffset = Global.CombatBackgroundArea.Center.Y;
                        int targetYOffset = 92;
                        var frames = new int[] { 4, 3, 2, 1, 0, 1, 2, 3, 4, 3, 2, 1, 0 };
                        void ShootGreenStar(bool last)
                        {
                            var startPosition = RandomPosition();
                            int targetX = upperXOffset + startPosition.X * upperWidth / lowerWidth;
                            float dyFactor = (float)upperWidth / lowerWidth;
                            int yDiff = startPosition.Y - sourceYOffset;
                            float startScale = game.RandomInt(50, 150) / 100.0f;
                            var animation = AddAnimation(CombatGraphicIndex.GreenStar, frames, startPosition, new Position(targetX, targetYOffset + Util.Round(dyFactor * yDiff) - 8),
                                Game.TicksPerSecond * 5 / 2, startScale, 0.0f, 255, last ? (Action)null : () => { });
                            animation.AnimationUpdated += Updated;
                            animation.AnimationFinished += Finished;
                            void Updated(float progress)
                            {
                                animation.SetDisplayLayer((byte)Math.Min(255, 251 - Util.Round(progress * 250)));
                            }
                            void Finished()
                            {
                                animation.AnimationUpdated -= Updated;
                                animation.AnimationFinished -= Finished;
                            }
                        }
                        Position RandomPosition()
                        {
                            int minX =  32;
                            int maxX = Global.CombatBackgroundArea.Right - minX;
                            int diffY = 32;
                            int minY = sourceYOffset - diffY;
                            int maxY = sourceYOffset + diffY;
                            return new Position(game.RandomInt(minX, maxX), game.RandomInt(minY, maxY));
                        }
                        for (int i = 0; i < 20; ++i)
                            ShootGreenStar(i == 19);
                    }
                    break;
                }
                case Spell.Blink:
                case Spell.Escape:
                    // Blink and Flight have no spell animations at all.
                    this.finishAction?.Invoke();
                    break;
                case Spell.Lame:
                case Spell.Poison:
                case Spell.Petrify:
                case Spell.CauseDisease:
                case Spell.CauseAging:
                case Spell.Irritate:
                case Spell.CauseMadness:
                case Spell.Sleep:
                case Spell.Fear:
                case Spell.Blind:
                case Spell.Drug:
                    // Curses will only use the MoveTo method.
                    this.finishAction?.Invoke();
                    break;
                case Spell.SpellPointsI:
                case Spell.SpellPointsII:
                case Spell.SpellPointsIII:
                case Spell.SpellPointsIV:
                case Spell.SpellPointsV:
                case Spell.AllHealing:
                case Spell.AddStrength:
                case Spell.AddIntelligence:
                case Spell.AddDexterity:
                case Spell.AddSpeed:
                case Spell.AddStamina:
                case Spell.AddCharisma:
                case Spell.AddLuck:
                case Spell.AddAntiMagic:
                case Spell.Drugs:
                    // Those spells will only use the MoveTo method.
                    this.finishAction?.Invoke();
                    break;
                case Spell.Earthslide:
                {
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.Landslide);
                    float scale = fromMonster ? 1.333f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)targetRow);
                    float endScale = GetScaleXRelativeToCombatArea(info.GraphicInfo.Width, scale * 0.85f);
                    scale = endScale * 0.4f;
                    int halfSpriteHeight = Util.Round(0.5f * scale * info.GraphicInfo.Height);
                    var targetPosition = GetTargetPosition(targetRow * 6) - new Position(0, halfSpriteHeight);
                    targetPosition.X = Global.CombatBackgroundArea.Center.X;
                    var startPosition = new Position(targetPosition.X, Math.Min(targetPosition.Y - 10, Global.CombatBackgroundArea.Y + halfSpriteHeight));
                    byte displayLayer = (byte)Math.Min(255, targetRow * 60 + 60);
                    PlayMaterialization(startPosition, CombatGraphicIndex.Landslide, scale, displayLayer, () =>
                    {
                        var animation = AddAnimationThatRemains(CombatGraphicIndex.Landslide, 1, startPosition, targetPosition,
                            Game.TicksPerSecond * 4 / 5, scale, endScale, displayLayer);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.ReferenceScale = endScale;
                        animation.SetStartFrame(startPosition, endScale);
                        animation.AnimationFinished += FallingFinished;
                        void FallingFinished()
                        {
                            animation.AnimationFinished -= FallingFinished;
                            game.AddTimedEvent(TimeSpan.FromMilliseconds(250), this.finishAction);
                        }
                    }, null, endScale, animation =>
                    {
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.ReferenceScale = scale;
                    }, Game.TicksPerSecond);
                    break;
                }
                case Spell.Earthquake:
                {
                    var position = Global.CombatBackgroundArea.Center + new Position(0, 5);
                    const float initialScale = 1.85f;
                    const float materializeScale = 3.0f;
                    const float endScale = 4.75f;
                    PlayMaterialization(position, CombatGraphicIndex.Landslide, initialScale, 0, () =>
                    {
                        game.ShakeScreen(TimeSpan.FromMilliseconds(150), 9, 0.035f);
                        BattleAnimation animation = AddAnimationThatRemains(CombatGraphicIndex.Landslide, 1,
                            position, position + new Position(0, 14), Game.TicksPerSecond * 8 / 5,
                            materializeScale, endScale, 0);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.YOnly;
                        animation.SetStartFrame(null, initialScale);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.ReferenceScale = materializeScale;
                        animation.SetStartFrame(null, materializeScale);
                        animation.AnimationFinished += AnimationFinished;
                        animation.AnimationUpdated += AnimationUpdated;
                        void AnimationFinished()
                        {
                            animation.AnimationUpdated -= AnimationUpdated;
                            animation.AnimationFinished -= AnimationFinished;

                            animation.ScaleType = BattleAnimation.AnimationScaleType.None;
                            animation.AnchorY = BattleAnimation.VerticalAnchor.Top;
                            animation.PlayWithoutAnimating(Game.TicksPerSecond / 2, game.CurrentBattleTicks,
                                new Position(position.X, Global.CombatBackgroundArea.Bottom), null);
                            animation.AnimationFinished += () => RemoveAnimation(animation);
                        }
                        void AnimationUpdated(float progress)
                        {
                            int row = Util.Round(progress * 5.0f);
                            animation.SetDisplayLayer((byte)Math.Min(255, row * 60));
                        }
                    }, null, materializeScale, animation =>
                    {
                        animation.ScaleType = BattleAnimation.AnimationScaleType.YOnly;
                        animation.SetStartFrame(null, initialScale);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.ReferenceScale = initialScale;
                    }, Game.TicksPerSecond);
                    break;
                }
                case Spell.Thunderbolt:
                {
                    var rowPosition = GetTargetPosition(targetRow * 6);
                    var rowEndPosition = GetTargetPosition(targetRow * 6 + 5);
                    int numLightnings = 10;
                    void PlayLightning()
                    {
                        int whiteDuration = game.RandomInt(50, 150);
                        layout.AddColorFader(Global.CombatBackgroundArea, Color.White, Color.White, whiteDuration, true);
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(whiteDuration), () =>
                        {
                            var position = rowPosition + new Position(game.RandomInt(0, rowEndPosition.X - rowPosition.X), 0);
                            var endPosition = new Position(Util.Limit(rowPosition.X, position.X + game.RandomInt(-20, 20), rowEndPosition.X), position.Y);
                            var scale = 1.5f * (fromMonster ? 2.0f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)targetRow));
                            byte displayLayer = fromMonster ? (byte)255 : (byte)(targetRow * 60 + 60);
                            AddAnimation(CombatGraphicIndex.Lightning, 1, position, endPosition,
                                (uint)Util.Round((250.0f - whiteDuration) * 0.001f * Game.TicksPerSecond),
                                scale, scale, displayLayer, (--numLightnings == 0) ? (Action)null : PlayLightning, null,
                                BattleAnimation.AnimationScaleType.Both, BattleAnimation.HorizontalAnchor.Center,
                                BattleAnimation.VerticalAnchor.Center, game.RandomInt(0, 1) == 0);
                        });
                    }
                    PlayLightning();
                    break;
                }
                case Spell.Whirlwind:
                {
                    // If cast by player, whirlwind starts at the bottom right monster position.
                    // Then starts at the bottom monster row on the right and proceed upwards starting each row on the right.
                    // If cast by monster, whirlwind starts at the upper left battle field position.
                    // Then starts with bottom row on the right and proceed on the above row on the right.
                    lastPosition = fromMonster ? 0 : 4 * 6 - 1;
                    var monsterRow = fromMonster ? MonsterRow.Farthest : MonsterRow.Near;
                    byte displayLayer = (byte)(fromMonster ? 0 : 180); // Behind row 0 or 3
                    PlayMaterialization(GetTargetPosition(lastPosition), CombatGraphicIndex.Whirlwind,
                        renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow) * 1.75f, displayLayer, this.finishAction);
                    break;
                }
                case Spell.Firebeam:
                case Spell.Fireball:
                {
                    // This only makes the screen red for a brief duration.
                    ShowOverlay(Color.FireOverlay);
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(250), () =>
                    {
                        this.finishAction?.Invoke();
                    });
                    break;
                }
                case Spell.Firestorm:
                {
                    ShowOverlay(Color.FireOverlay);
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.BigFlame);
                    const float scaleReducePerFlame = 0.225f;
                    float scale = GetScaleYRelativeToCombatArea(info.GraphicInfo.Height, 1.15f) *
                        (fromMonster ? 1.2f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)targetRow));
                    void AddFlameAnimation(int width, float startScale, float endScale, Position startGroundPosition, Position endGroundPosition,
                        int startFrame, uint duration, Action finishAction)
                    {
                        // Note: The start frame is also used for the x-offset
                        int[] frames = (startFrame == 0 ? Enumerable.Range(0, 8) :
                            Enumerable.Concat(Enumerable.Range(startFrame, 8 - startFrame), Enumerable.Range(0, startFrame))).ToArray();
                        int startXOffset = startFrame * width * 7 / 12;
                        int endXOffset = startFrame * width * 7 / 12;
                        int startHeight = Util.Round(startScale * info.GraphicInfo.Height);
                        int halfEndHeight = Util.Round(0.5f * endScale * info.GraphicInfo.Height);
                        var startPosition = new Position(startGroundPosition.X + startXOffset, startGroundPosition.Y - startHeight / 2);
                        var endPosition = new Position(endGroundPosition.X + endXOffset, endGroundPosition.Y - halfEndHeight);
                        AddAnimation(CombatGraphicIndex.BigFlame, frames,
                            startPosition, endPosition, duration,
                            1.0f, endScale / startScale, (byte)Math.Min(255, targetRow * 60 + 60), finishAction,
                            new Size(width, startHeight), BattleAnimation.AnimationScaleType.YOnly);
                    }
                    var combatArea = Global.CombatBackgroundArea;
                    var leftPosition = fromMonster ? Layout.GetPlayerSlotTargetPosition(2) : Layout.GetMonsterCombatGroundPosition(renderView, targetRow * 6 + 2);
                    int width = Util.Round(scale * info.GraphicInfo.Width * 0.5f);
                    uint primaryDuration = Game.TicksPerSecond;
                    uint secondaryDuration = Game.TicksPerSecond * 5 / 8;
                    int endX = leftPosition.X - (combatArea.Right - leftPosition.X) * (int)secondaryDuration / (int)primaryDuration;
                    for (int i = 0; i < 4; ++i)
                    {
                        float baseScale = scale * (1.0f - i * scaleReducePerFlame);
                        int frame = i;

                        AddFlameAnimation(width, baseScale, baseScale, new Position(combatArea.Right, leftPosition.Y),
                            leftPosition, frame, primaryDuration, () =>
                        {
                            AddFlameAnimation(width, baseScale, 0.5f * baseScale, leftPosition,
                                new Position(endX, leftPosition.Y), frame, secondaryDuration, frame == 3 ? (Action)(() =>
                                {
                                    HideOverlay();
                                    this.finishAction?.Invoke();
                                }) : null);
                        });
                    }
                    break;
                }
                case Spell.Firepillar:
                {
                    ShowOverlay(Color.FireOverlay);
                    var startPosition = new Position(Global.CombatBackgroundArea.Center) + new Position(0, 10);
                    var endPosition = new Position(Global.CombatBackgroundArea.Center) + new Position(0, 25);
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.BigFlame);
                    AddAnimation(CombatGraphicIndex.BigFlame, Enumerable.Range(0, 16).Select(i => i % 8).ToArray(),
                        startPosition, endPosition, Game.TicksPerSecond * 7 / 2, 4.0f, 10.0f, 255, () =>
                        {
                            HideOverlay();
                            this.finishAction?.Invoke();
                        });
                    // TODO: The flame turns black afterwards
                    break;
                }
                case Spell.Waterfall:
                    // Only uses the MoveTo method.
                    this.finishAction?.Invoke();
                    break;
                case Spell.Iceball:
                {
                    // This only makes the screen blue for a brief duration.
                    ShowOverlay(Color.IceOverlay);
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(250), () =>
                    {
                        this.finishAction?.Invoke();
                    });
                    break;
                }
                case Spell.Icestorm:
                {
                    ShowOverlay(Color.IceOverlay);
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.IceBlock);
                    const float scaleReducePerIceBlock = 0.225f;
                    float scale = GetScaleYRelativeToCombatArea(info.GraphicInfo.Height, 0.9f) *
                        (fromMonster ? 1.5f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)targetRow));
                    void AddIceAnimation(int width, float startScale, float endScale, Position startGroundPosition, Position endGroundPosition,
                        int index, uint duration, Action finishAction)
                    {
                        int[] frames = new int[] { 0, 0, 1, 1, 0, 0, 1, 1 };
                        int startXOffset = index * width * 7 / 8;
                        int endXOffset = index * width * 7 / 8;
                        int startHeight = Util.Round(startScale * info.GraphicInfo.Height);
                        int halfEndHeight = Util.Round(0.5f * endScale * info.GraphicInfo.Height);
                        var startPosition = new Position(startGroundPosition.X + startXOffset, startGroundPosition.Y - startHeight / 2);
                        var endPosition = new Position(endGroundPosition.X + endXOffset, endGroundPosition.Y - halfEndHeight);
                        AddAnimation(CombatGraphicIndex.IceBlock, frames,
                            startPosition, endPosition, duration,
                            1.0f, endScale / startScale, (byte)Math.Min(255, targetRow * 60 + 60), finishAction,
                            new Size(width, startHeight), BattleAnimation.AnimationScaleType.YOnly);
                    }
                    var combatArea = Global.CombatBackgroundArea;
                    var leftPosition = fromMonster ? Layout.GetPlayerSlotCenterPosition(2) : Layout.GetMonsterCombatGroundPosition(renderView, targetRow * 6 + 2);
                    int width = Util.Round(scale * info.GraphicInfo.Width * 0.5f);
                    uint primaryDuration = Game.TicksPerSecond;
                    uint secondaryDuration = Game.TicksPerSecond * 5 / 8;
                    int endX = leftPosition.X - (combatArea.Right - leftPosition.X) * (int)secondaryDuration / (int)primaryDuration;
                    for (int i = 0; i < 4; ++i)
                    {
                        float baseScale = scale * (1.0f - i * scaleReducePerIceBlock);
                        int frame = i;
                        AddIceAnimation(width, baseScale, baseScale, new Position(combatArea.Right, leftPosition.Y),
                            leftPosition, frame, primaryDuration, () =>
                            {
                                AddIceAnimation(width, baseScale, 0.5f * baseScale, leftPosition,
                                    new Position(endX, leftPosition.Y), frame, secondaryDuration, frame == 3 ? (Action)(() =>
                                    {
                                        HideOverlay();
                                        this.finishAction?.Invoke();
                                    }) : null);
                            });
                    }
                    break;
                }
                case Spell.Iceshower:
                {
                    ShowOverlay(Color.IceOverlay);
                    int lowerWidth = Global.CombatBackgroundArea.Width;
                    int upperWidth = 140;
                    int upperXOffset = 92;
                    int sourceYOffset = fromMonster ? 92 : Global.CombatBackgroundArea.Center.Y;
                    int targetYOffset = fromMonster ? Global.CombatBackgroundArea.Center.Y : 92;
                    void ShootIceBall(bool last)
                    {
                        var startPosition = RandomPosition();
                        int targetX = fromMonster ? (startPosition.X - upperXOffset) * lowerWidth / upperWidth
                            : upperXOffset + startPosition.X * upperWidth / lowerWidth;
                        float dyFactor = fromMonster ? (float)lowerWidth / upperWidth : (float)upperWidth / lowerWidth;
                        int yDiff = startPosition.Y - sourceYOffset;
                        float startScale = fromMonster ? 0.0f : 1.5f;
                        float endScale = fromMonster ? 1.5f : 0.0f;
                        var animation = AddAnimation(CombatGraphicIndex.IceBall, 1, startPosition, new Position(targetX, targetYOffset + Util.Round(dyFactor * yDiff)),
                            Game.TicksPerSecond * 2, startScale, endScale, 255, () => { if (last) { HideOverlay(); this.finishAction?.Invoke(); } });
                        animation.AnimationUpdated += Updated;
                        animation.AnimationFinished += Finished;
                        void Updated(float progress)
                        {
                            int displayLayerBase = Util.Round(progress * 250);
                            animation.SetDisplayLayer((byte)Math.Min(255, fromMonster ? displayLayerBase : 251 - displayLayerBase));
                        }
                        void Finished()
                        {
                            animation.AnimationUpdated -= Updated;
                            animation.AnimationFinished -= Finished;
                        }
                    }
                    Position RandomPosition()
                    {
                        int minX = fromMonster ? upperXOffset + 16 : 32;
                        int maxX = Global.CombatBackgroundArea.Right - minX;
                        int diffY = fromMonster ? upperWidth * 32 / lowerWidth : 32;
                        int minY = sourceYOffset - diffY;
                        int maxY = sourceYOffset + diffY;
                        return new Position(game.RandomInt(minX, maxX), game.RandomInt(minY, maxY));
                    }
                    for (int i = 0; i < 8; ++i)
                        ShootIceBall(i == 7);
                    break;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }
        }

        static readonly byte[] materializeColorIndices = Enumerable.Range(1, 6).Select(i => (byte)i).ToArray();

        void PlayMaterialization(Position position, CombatGraphicIndex combatGraphicIndex, float scale,
            byte displayLayer, Action finishAction, Position endPosition = null, float? endScale = null,
            Action<BattleAnimation> setupAnimation = null, uint duration = Game.TicksPerSecond * 3 / 4)
        {
            // Materialize some sprite
            // It used the following color sequence which is encoded in palette 52 at index 1-6:
            // Black -> dark red -> light purple -> dark purple -> dark beige -> light beige.
            // See materializeColorIndices above.
            var animation = AddMaskedAnimation(combatGraphicIndex, position, endPosition ?? position, duration,
                scale, endScale ?? scale, displayLayer, finishAction, materializeColorIndices, 51);
            setupAnimation?.Invoke(animation);
        }

        /// <summary>
        /// Called after the whole spell cast is over.
        /// Some spells like Earthslide needs after-spell animations.
        /// </summary>
        public void PostCast(Action finishedAction)
        {
            if (spell == Spell.Earthslide)
            {
                if (animations.Count != 1)
                    throw new AmbermoonException(ExceptionScope.Application, "Earthslide spell has wrong animation count.");

                animations[0].AnimationFinished += () =>
                {
                    RemoveAnimation(animations[0], false); // Remove when finished
                    finishedAction?.Invoke();
                };
                var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.Landslide);
                float scale = fromMonster ? 2.0f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)targetRow);
                scale = GetScaleXRelativeToCombatArea(info.GraphicInfo.Width, scale * 0.85f);
                scale *= 0.4f;
                animations[0].AnchorY = BattleAnimation.VerticalAnchor.Bottom;
                animations[0].ScaleType = BattleAnimation.AnimationScaleType.YOnly;
                animations[0].ReferenceScale = scale;
                animations[0].SetStartFrame(null, scale);
                animations[0].PlayWithoutAnimating(Game.TicksPerSecond / 2, game.CurrentBattleTicks, null, 0.0f);
            }
            else if (spell == Spell.Whirlwind)
            {
                if (animations.Count != 1)
                    throw new AmbermoonException(ExceptionScope.Application, "Whirlwind spell has wrong animation count.");

                animations[0].AnimationFinished += () =>
                {
                    RemoveAnimation(animations[0], false); // Remove when finished
                    finishedAction?.Invoke();
                };
                var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.Whirlwind);
                var scale = animations[0].Scale;
                animations[0].AnchorY = BattleAnimation.VerticalAnchor.Bottom;
                animations[0].ScaleType = BattleAnimation.AnimationScaleType.Both;
                animations[0].ReferenceScale = scale;
                animations[0].SetStartFrame(null, scale);
                animations[0].PlayWithoutAnimating(Game.TicksPerSecond / 3, game.CurrentBattleTicks, null, 0.0f);
            }
            else
            {
                // Ensure all remaining animations are cleaned up.
                for (int i = animations.Count - 1; i >= 0; --i)
                    RemoveAnimation(animations[i], false);
                finishedAction?.Invoke();
            }
        }

        public delegate void MoveToFinishAction(uint ticks, bool playHurtAnimation, bool finish);

        /// <summary>
        /// Moves a spell to a given target represented by a tile.
        /// 
        /// The finish actions takes 3 arguments:
        /// - The battle ticks when invoking (used for starting the hurt animation)
        /// - A bool which specifies if the hurt animation should be played
        /// - A bool which specifies if the caller should treat the spell movement as finished
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="finishAction"></param>
        public void MoveTo(int tile, MoveToFinishAction finishAction)
        {
            this.finishAction = () => finishAction?.Invoke(game.CurrentBattleTicks, true, true);

            void PlayParticleEffect(CombatGraphicIndex combatGraphicIndex, int frameCount)
            {
                var position = GetTargetPosition(tile) - new Position(2, 0);
                if (position.Y > Global.CombatBackgroundArea.Bottom - 6)
                    position.Y = Global.CombatBackgroundArea.Bottom - 6;
                AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 4, 1, 1, 255, () => { });
                game.AddTimedEvent(TimeSpan.FromMilliseconds(125), () =>
                {
                    position.X += 6;
                    position.Y += 6;
                    AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 4, 1, 1, 255, () => { });
                });
                game.AddTimedEvent(TimeSpan.FromMilliseconds(250), () =>
                {
                    position.X -= 12;
                    position.Y -= 4;
                    AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 4, 1, 1);
                });
            }

            // Used by fire spells
            void PlayBurn()
            {
                PlayParticleEffect(CombatGraphicIndex.SmallFlame, 6);
            }

            // Used for ice spells
            void PlayChill()
            {
                PlayParticleEffect(CombatGraphicIndex.SnowFlake, 5);
            }

            // Used for monster knowledge
            void PlayKnowledge()
            {
                const int count = 8;
                var basePosition = GetTargetPosition(tile) + new Position(0, 18);

                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(i * 50), () =>
                    {
                        var position = basePosition + new Position(game.RandomInt(0, 32) - 16, -2 * index);
                        AddAnimation(CombatGraphicIndex.GreenStar, 5, position, new Position(position.X, position.Y - 38),
                            Game.TicksPerSecond * 7 / 10, 1.0f, 1.2f, 255, index == count - 1 ? (Action)null : () => { });
                    });
                }
            }

            // Used for all curses
            void PlayCurse(CombatGraphicIndex iconGraphicIndex)
            {
                if (!battle.CheckSpell(battle.GetCharacterAt(startPosition), battle.GetCharacterAt(tile), spell, null, false, false, false))
                {
                    this.finishAction?.Invoke();
                }
                else
                {
                    // Note: The hurt animation comes first so we immediately call the passed finish action
                    // which will display the hurt animation.
                    finishAction?.Invoke(game.CurrentBattleTicks, true, false); // Play hurt animation but do not finish.
                    this.finishAction = () => finishAction?.Invoke(game.CurrentBattleTicks, false, true); // This is called after the animation to finish.

                    float scale = fromMonster ? 2.0f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                    var targetPosition = GetTargetPosition(tile) - new Position(0, Util.Round(6 * scale));
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                    {
                        void AddCurseAnimation(CombatGraphicIndex graphicIndex, Action finishAction, bool reverse, byte displayLayer)
                        {
                            AddAnimation(graphicIndex, 1, targetPosition, targetPosition, reverse ? Game.TicksPerSecond / 4 : Game.TicksPerSecond / 2,
                                reverse ? scale : 0.0f, reverse ? 0.5f * scale : scale, displayLayer, finishAction);
                        }
                        byte displayLayerRing = (byte)(fromMonster ? 254 : ((tile / 6) * 60 + 60));
                        byte displayLayer = (byte)(displayLayerRing + 1);
                        AddCurseAnimation(CombatGraphicIndex.RedRing, () => { }, false, displayLayerRing);
                        AddCurseAnimation(iconGraphicIndex, () =>
                        {
                            AddCurseAnimation(CombatGraphicIndex.RedRing, () => { }, true, displayLayerRing);
                            AddCurseAnimation(iconGraphicIndex, null, true, displayLayer); // This will trigger the outer finish action
                        }, false, displayLayer);
                    });
                }
            }

            // Used for Anti-undead spells
            void PlayHolyLight()
            {
                // Should always be a monster but just in case we check here.
                if (battle.GetCharacterAt(tile) is Monster monster &&
                    battle.CheckSpell(battle.GetCharacterAt(startPosition), monster, spell, null, false, false, false))
                {
                    var position = GetTargetPosition(tile) + new Position(0, 4);
                    int beamHeight = 8 + position.Y - Global.CombatBackgroundArea.Top;
                    // Note: Positions are ground-based (anchor at the bottom).
                    var startPosition = new Position(position.X, Global.CombatBackgroundArea.Top - beamHeight / 2);
                    var endPosition = new Position(position.X, position.Y - beamHeight / 2);
                    byte displayLayer = (byte)((tile / 6) * 60); // Show behind the monsters
                    float rowScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                    int beamWidth = Util.Round(rowScale * monster.MappedFrameWidth);
                    AddAnimation(CombatGraphicIndex.HolyBeam, 1, startPosition, endPosition, Game.TicksPerSecond * 3 / 2, 1, 1, displayLayer, () =>
                    {
                        startPosition = new Position(endPosition);
                        int diff = endPosition.Y - Global.CombatBackgroundArea.Top;
                        endPosition.Y = Global.CombatBackgroundArea.Top;
                        var monsterEndPosition = new Position(endPosition.X, Layout.GetMonsterCombatCenterPosition(renderView, tile, monster).Y - diff);

                        // Move monster to heavens
                        battle.StartMonsterAnimation(monster, animation =>
                        {
                            animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                            var hurtFrames = monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt);
                            animation.Play(hurtFrames, (Game.TicksPerSecond * 3 / 4) / (uint)hurtFrames.Length, game.CurrentBattleTicks, monsterEndPosition, 0.0f);
                        }, animation => animation.ScaleType = BattleAnimation.AnimationScaleType.Both);
                        // And fade out beam too
                        AddAnimation(CombatGraphicIndex.HolyBeam, 1, startPosition, endPosition, Game.TicksPerSecond * 3 / 4, 1, 0, displayLayer, null,
                            new Size(beamWidth, beamHeight), BattleAnimation.AnimationScaleType.XOnly);
                    }, new Size(beamWidth, beamHeight));
                }
                else
                {
                    this.finishAction?.Invoke();
                }
            }

            // Used for Winddevil, Windhowler and Whirlwind
            void PlayWhirlwind(int startTile, bool materialize, Action finishAction = null)
            {
                var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.Whirlwind);
                var baseScale = spell switch
                {
                    Spell.Winddevil => 1.25f,
                    _ => 1.75f
                };
                var startScale = baseScale * (fromMonster && startTile >= 18 ? 1.75f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startTile / 6)));
                var endScale = baseScale * (fromMonster ? 1.75f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6)));
                byte displayLayer = (byte)Math.Min(255, (startTile / 6) * 60);
                byte endDisplayLayer = (byte)Math.Min(255, (tile / 6) * 60);
                float GetDiffDurationFactor()
                {
                    int diffRow = Math.Abs(tile / 6 - startTile / 6);
                    if (diffRow != 0)
                        return 0.5f + diffRow * 0.25f;
                    else
                    {
                        int diffColumn = Math.Abs(tile % 6 - startTile % 6);

                        if (diffColumn == 0)
                            return 1.0f;

                        return diffColumn * 0.2f;
                    }
                }
                float durationFactor = spell switch
                {
                    Spell.Winddevil => 2.0f,
                    Spell.Windhowler => 2.5f,
                    _ => GetDiffDurationFactor()
                };
                int framesPerDuration = spell switch
                {
                    Spell.Whirlwind => 12,
                    _ => 8
                };
                var duration = Util.Round(Game.TicksPerSecond * durationFactor);
                var frames = Enumerable.Range(0, Util.Round(durationFactor * framesPerDuration)).Select(i => i % 4).ToArray();
                var startPosition = GetTargetPosition(startTile);
                var endPosition = GetTargetPosition(tile);
                if (materialize)
                    PlayMaterialization(startPosition, CombatGraphicIndex.Whirlwind, startScale, displayLayer, PlayAnimation);
                else
                    PlayAnimation();
                void Finished()
                {
                    if (spell == Spell.Whirlwind)
                        finishAction?.Invoke();
                    else
                    {
                        AddAnimation(CombatGraphicIndex.Whirlwind, 1, endPosition, endPosition, Game.TicksPerSecond / 3, endScale, 0.0f, endDisplayLayer,
                            finishAction, null, BattleAnimation.AnimationScaleType.Both, BattleAnimation.HorizontalAnchor.Center, BattleAnimation.VerticalAnchor.Bottom);
                    }
                }
                void PlayAnimation()
                {
                    var animation = spell == Spell.Whirlwind && startTile == tile
                        ? AddAnimationThatRemains(CombatGraphicIndex.Whirlwind, frames,
                        startPosition, endPosition, (uint)duration, startScale, endScale, displayLayer, Finished)
                        : AddAnimation(CombatGraphicIndex.Whirlwind, frames,
                            startPosition, endPosition, (uint)duration, startScale, endScale, displayLayer, Finished);
                    if (startTile == tile && battle.GetCharacterAt(tile) is Monster monster)
                    {
                        int remainingTicks = duration;
                        const int TimePerScaling = (int)Game.TicksPerSecond / 2;
                        float baseScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                        var basePosition = Layout.GetMonsterCombatCenterPosition(renderView, tile, monster);
                        var hurtFrames = monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt);
                        void PlayScale(float additionalScale)
                        {
                            battle.StartMonsterAnimation(monster, animation =>
                            {
                                remainingTicks -= TimePerScaling;
                                int yOffset = Util.Round((16 * (1.0f + additionalScale) - 16) * baseScale);
                                animation.Play(hurtFrames, TimePerScaling / (uint)hurtFrames.Length, game.CurrentBattleTicks,
                                    basePosition - new Position(0, yOffset), baseScale * (1.0f + additionalScale));
                            }, _ =>
                            {
                                if (remainingTicks > 0)
                                {
                                    PlayScale(0.2f - additionalScale);
                                }
                            });
                        }
                        PlayScale(0.2f);
                    }
                    if (endDisplayLayer != displayLayer)
                    {
                        void UpdateDisplayLayer(float progress)
                        {
                            var newDisplayLayer = (byte)Util.Limit(0, Util.Round(displayLayer + (endDisplayLayer - displayLayer) * progress), 255);
                            animation.SetDisplayLayer(newDisplayLayer);
                        }
                        void AnimationFinished()
                        {
                            animation.AnimationUpdated -= UpdateDisplayLayer;
                            animation.AnimationFinished -= AnimationFinished;
                        }
                        animation.AnimationUpdated += UpdateDisplayLayer;
                        animation.AnimationFinished += AnimationFinished;
                    }
                }
            }

            switch (spell)
            {
                case Spell.HealingHand:
                case Spell.RemoveFear:
                case Spell.RemoveShadows:
                case Spell.RemovePain:
                case Spell.SmallHealing:
                case Spell.RemovePoison:
                case Spell.MediumHealing:
                case Spell.GreatHealing:
                case Spell.RemoveRigidness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.Hurry:
                case Spell.SpellPointsI:
                case Spell.SpellPointsII:
                case Spell.SpellPointsIII:
                case Spell.SpellPointsIV:
                case Spell.SpellPointsV:
                case Spell.AllHealing:
                case Spell.AddStrength:
                case Spell.AddIntelligence:
                case Spell.AddDexterity:
                case Spell.AddSpeed:
                case Spell.AddStamina:
                case Spell.AddCharisma:
                case Spell.AddLuck:
                case Spell.AddAntiMagic:
                case Spell.Drugs:
                    if (fromMonster)
                    {
                        // No visual effect if monster casts it.
                        this.finishAction?.Invoke();
                    }
                    else
                    {
                        CastOn(spell, battle.GetCharacterAt(tile) as PartyMember, this.finishAction);
                    }
                    break;
                case Spell.RemovePanic:
                case Spell.RemoveBlindness:
                case Spell.RemoveDisease:
                case Spell.NeutralizePoison:
                case Spell.MassHealing:
                case Spell.RemoveLamedness:
                    // Mass healing spells are handled in Play.
                    this.finishAction?.Invoke();
                    break;
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                    PlayHolyLight();
                    break;
                case Spell.RestoreStamina:
                    // No visual effect.
                    this.finishAction?.Invoke();
                    break;
                case Spell.GhostWeapon:
                {
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.BigFlame);
                    var monsterRow = (MonsterRow)(fromMonster ? this.startPosition / 6 : tile / 6);
                    float startScale = 3.0f;
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow) * 1.5f;
                    var startPosition = GetSourcePosition();
                    var endPosition = GetTargetPosition(tile);
                    int maxStartOffset = Util.Round(0.75f * startScale * info.GraphicInfo.Width);
                    int maxEndOffset = Util.Round(0.75f * endScale * info.GraphicInfo.Width);
                    int maxOffset = Math.Max(maxStartOffset, maxEndOffset);
                    int xOffset = (startPosition.X - endPosition.X) / 8;
                    xOffset = xOffset < -maxOffset ? -maxOffset :
                        (xOffset > maxOffset ? maxOffset : xOffset);
                    int startXOffset = Util.Round(0.5f * xOffset * startScale);
                    int endXOffset = Util.Round(0.5f * xOffset * endScale);
                    float scaleFactor = 1.0f;
                    AddAnimation(CombatGraphicIndex.BigFlame, 8, startPosition, endPosition,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 252, () => { }, 19);
                    scaleFactor = 0.975f;
                    var startOffset = new Position(Util.Round(scaleFactor * startXOffset), 4);
                    var endOffset = new Position(Util.Round(scaleFactor * endXOffset), 4);
                    AddAnimation(CombatGraphicIndex.BigFlame, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 253, () => { }, 19);
                    scaleFactor = 0.95f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 3;
                    endOffset.Y += 3;
                    AddAnimation(CombatGraphicIndex.BigFlame, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 254, () => { }, 19);
                    scaleFactor = 0.925f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 2;
                    endOffset.Y += 2;
                    AddAnimation(CombatGraphicIndex.BigFlame, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 255, () => { HideOverlay(); PlayBurn(); }, 19);
                    break;
                }
                case Spell.Blink:
                case Spell.Escape:
                    // Blink and Flight have no spell animations at all.
                    this.finishAction?.Invoke();
                    break;
                case Spell.MagicalShield:
                case Spell.MagicalWall:
                case Spell.MagicalBarrier:
                case Spell.MagicalWeapon:
                case Spell.MagicalAssault:
                case Spell.MagicalAttack:
                case Spell.AntiMagicWall:
                case Spell.AntiMagicSphere:
                case Spell.MassHurry:
                case Spell.ShowMonsterLP:
                    // Buffs are handled in Play.
                    this.finishAction?.Invoke();
                    break;
                case Spell.MonsterKnowledge:
                    PlayKnowledge();
                    break;
                case Spell.LPStealer:
                case Spell.SPStealer:
                {
                    // Note: The hurt animation comes first so we immediately call the passed finish action
                    // which will display the hurt animation.
                    finishAction?.Invoke(game.CurrentBattleTicks, true, false); // Play hurt animation but do not finish.
                    this.finishAction = () => finishAction?.Invoke(game.CurrentBattleTicks, false, true); // This is called after the animation to finish.

                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                    {
                        byte displayLayer = (byte)(fromMonster ? 255 : ((tile / 6) * 60 + 60));
                        AddAnimation(spell == Spell.LPStealer ? CombatGraphicIndex.BlueBeam : CombatGraphicIndex.GreenBeam, 1,
                            GetTargetPosition(tile), GetSourcePosition(), BattleEffects.GetFlyDuration((uint)tile, (uint)startPosition),
                            fromMonster ? 2.5f: endScale, fromMonster ? endScale : 2.5f, displayLayer);
                    });
                    break;
                }
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                {
                    // Both spells use the same animation. For magical arrows it is just called multiple times.
                    var monsterRow = (MonsterRow)(fromMonster ? startPosition / 6 : tile / 6);
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
                    var graphic = fromMonster ? CombatGraphicIndex.MagicProjectileMonster : CombatGraphicIndex.MagicProjectileHuman;
                    var sourcePosition = GetSourcePosition();
                    var targetPosition = GetTargetPosition(tile);
                    bool mirrorX = targetPosition.X < sourcePosition.X;
                    int rowDist = Math.Abs(startPosition / 6 - tile / 6);
                    float perspectiveScaleFactor = fromMonster ? 1.0f : Util.Limit(0.5f, Math.Abs(targetPosition.X - sourcePosition.X) / 48.0f - rowDist * 0.1f, 1.0f);

                    void Shoot()
                    {
                        var frames = (fromMonster ? Enumerable.Range(0, 12) : Enumerable.Range(16, 8)).ToArray();
                        AddAnimation(graphic, frames, sourcePosition, targetPosition,
                            (uint)Math.Min(280, Math.Abs(targetPosition.X - sourcePosition.X) + rowDist * 50) * Game.TicksPerSecond / 560,
                            fromMonster ? endScale : 1.0f, fromMonster ? 1.0f : Math.Min(endScale, perspectiveScaleFactor), 255, null, null,
                            fromMonster || perspectiveScaleFactor >= endScale ? BattleAnimation.AnimationScaleType.Both : BattleAnimation.AnimationScaleType.XOnly,
                            BattleAnimation.HorizontalAnchor.Center, BattleAnimation.VerticalAnchor.Center, mirrorX);
                    }

                    if (!fromMonster)
                    {
                        AddAnimation(graphic, fromMonster ? 12 : 17, sourcePosition, sourcePosition,
                            Game.TicksPerSecond * 5 / 6, 1.0f, 1.0f, 255, Shoot, null,
                            BattleAnimation.AnimationScaleType.None, BattleAnimation.HorizontalAnchor.Center, BattleAnimation.VerticalAnchor.Center,
                            mirrorX);
                    }
                    else
                    {
                        Shoot();
                    }
                    break;
                }
                case Spell.Lame:
                    PlayCurse(CombatGraphicIndex.IconParalyze);
                    break;
                case Spell.Poison:
                    PlayCurse(CombatGraphicIndex.IconPoison);
                    break;
                case Spell.Petrify:
                    PlayCurse(CombatGraphicIndex.IconPetrify);
                    break;
                case Spell.CauseDisease:
                    PlayCurse(CombatGraphicIndex.IconDisease);
                    break;
                case Spell.CauseAging:
                    PlayCurse(CombatGraphicIndex.IconAging);
                    break;
                case Spell.Irritate:
                    PlayCurse(CombatGraphicIndex.IconIrritation);
                    break;
                case Spell.CauseMadness:
                    PlayCurse(CombatGraphicIndex.IconMadness);
                    break;
                case Spell.Sleep:
                    PlayCurse(CombatGraphicIndex.IconSleep);
                    break;
                case Spell.Fear:
                    PlayCurse(CombatGraphicIndex.IconPanic);
                    break;
                case Spell.Blind:
                    PlayCurse(CombatGraphicIndex.IconBlind);
                    break;
                case Spell.Drug:
                    PlayCurse(CombatGraphicIndex.IconDrugs);
                    break;
                case Spell.DissolveVictim:
                {
                    var target = battle.GetCharacterAt(tile);
                    if (!battle.CheckSpell(battle.GetCharacterAt(startPosition), target, spell, null, false, false, false))
                    {
                        this.finishAction?.Invoke();
                    }
                    else
                    {
                        int row = tile / 6;
                        var position = GetTargetPosition(tile);
                        void ShowParticle()
                        {
                            AddAnimation(CombatGraphicIndex.GreenStar, 5, position, position - new Position(0, 4), Game.TicksPerSecond / 5, 1, 1, 255,
                                () => finishAction?.Invoke(game.CurrentBattleTicks, false, true));
                        }
                        if (target is Monster monster)
                        {
                            // Shrink monster to zero
                            battle.StartMonsterAnimation(monster, animation =>
                                animation.PlayWithoutAnimating(Game.TicksPerSecond, game.CurrentBattleTicks, position, 0.0f),
                                _ => ShowParticle());
                        }
                        else
                        {
                            ShowParticle();
                        }
                    }
                    break;
                }
                case Spell.Mudsling:
                case Spell.Rockfall:
                {
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.LargeStone);
                    float baseScale = spell == Spell.Mudsling ? 1.0f : 1.5f;
                    var monsterScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                    float rowScale = fromMonster ? 2.0f : monsterScale;
                    float scale = baseScale * rowScale;
                    var spellSpriteHeight = Util.Round(info.GraphicInfo.Height * scale);
                    var headPosition = GetTargetPosition(tile);
                    int fallHeight = Util.Round(16.0f * rowScale);
                    var startPosition = headPosition - new Position(0, fallHeight);
                    byte displayLayer = (byte)Math.Min(255, targetRow * 60 + 60);
                    PlayMaterialization(startPosition, CombatGraphicIndex.LargeStone, scale, displayLayer, () =>
                        {
                            // Play fall animation
                            AddAnimation(CombatGraphicIndex.LargeStone, 1, startPosition, headPosition, Game.TicksPerSecond / 4,
                                scale, scale, displayLayer, () =>
                                {
                                    if (battle.GetCharacterAt(tile) is Monster monster)
                                    {
                                        const float squashFactor = 0.8f;
                                        int squashAmount = Util.Round(monster.MappedFrameHeight * monsterScale * (1.0f - squashFactor));
                                        var duration = Game.TicksPerSecond / 5;
                                        var squashRockPosition = headPosition + new Position(0, squashAmount);
                                        // Squash monster ...
                                        battle.StartMonsterAnimation(monster, animation =>
                                        {
                                            animation.AnchorY = BattleAnimation.VerticalAnchor.Bottom;
                                            animation.ScaleType = BattleAnimation.AnimationScaleType.YOnly;
                                            animation.ReferenceScale = monsterScale;
                                            animation.PlayWithoutAnimating(duration, game.CurrentBattleTicks, null, squashFactor * monsterScale);
                                        }, null);
                                        // ... and let the rock fall a bit more
                                        AddAnimation(CombatGraphicIndex.LargeStone, 1, headPosition, squashRockPosition,
                                            duration, scale, scale, displayLayer, () =>
                                            {
                                                // Un-squash monster ...
                                                battle.StartMonsterAnimation(monster, animation =>
                                                {
                                                    animation.PlayWithoutAnimating(duration, game.CurrentBattleTicks, null, monsterScale);
                                                }, animation =>
                                                {
                                                    animation.AnchorY = BattleAnimation.VerticalAnchor.Center;
                                                    animation.ScaleType = BattleAnimation.AnimationScaleType.Both;
                                                    animation.ReferenceScale = 1.0f;
                                                });
                                                // ... and let the rock rebounce up a bit
                                                AddAnimation(CombatGraphicIndex.LargeStone, 1, squashRockPosition, headPosition,
                                                    duration, scale, scale, displayLayer); // This will invoke the finish action automatically.
                                            }
                                        );
                                    }
                                    else
                                    {
                                        this.finishAction?.Invoke();
                                    }
                                }
                            );
                        }
                    );

                    break;
                }
                case Spell.Earthslide:
                case Spell.Earthquake:
                    // Just hurt each monster.
                    this.finishAction?.Invoke();
                    break;
                case Spell.Winddevil:
                case Spell.Windhowler:
                {
                    PlayWhirlwind(tile, true);
                    break;
                }
                case Spell.Thunderbolt:
                    // Just hurt each monster.
                    this.finishAction?.Invoke();
                    break;
                case Spell.Whirlwind:
                {
                    if (animations.Count != 0)
                    {
                        RemoveAnimation(animations[0], false);
                        animations.Clear();
                    }
                    void Play()
                    {
                        PlayWhirlwind(tile, false, () =>
                        {
                            lastPosition = tile;
                            this.finishAction?.Invoke();
                        });
                    }
                    if (tile == lastPosition) // The very first whirlwind could potentially be already on spot.
                        Play();
                    else // In all other cases move the whirlwind to the right spot first.
                    {
                        PlayWhirlwind(lastPosition, false, Play);
                    }
                    break;
                }
                case Spell.Firebeam:
                {
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.FireBall);
                    var monsterRow = (MonsterRow)(fromMonster ? this.startPosition / 6 : tile / 6);
                    float startScale = 2.0f;
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
                    var startPosition = GetSourcePosition();
                    var endPosition = GetTargetPosition(tile);
                    int maxStartOffset = Util.Round(0.75f * startScale * info.GraphicInfo.Width);
                    int maxEndOffset = Util.Round(0.75f * endScale * info.GraphicInfo.Width);
                    int maxOffset = Math.Max(maxStartOffset, maxEndOffset);
                    int xOffset = (startPosition.X - endPosition.X) / 8;
                    xOffset = xOffset < -maxOffset ? -maxOffset :
                        (xOffset > maxOffset ? maxOffset : xOffset);
                    int startXOffset = Util.Round(0.5f * xOffset * startScale);
                    int endXOffset = Util.Round(0.5f * xOffset * endScale);
                    float scaleFactor = 1.0f;
                    int displayLayer = fromMonster ? 255 : 252;
                    int displayLayerChange = fromMonster ? -1 : 1;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition, endPosition,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), (byte)displayLayer, () => { });
                    displayLayer += displayLayerChange;
                    scaleFactor = 0.85f;
                    var startOffset = new Position(Util.Round(scaleFactor * startXOffset), 4);
                    var endOffset = new Position(Util.Round(scaleFactor * endXOffset), 4);
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), (byte)displayLayer, () => { });
                    displayLayer += displayLayerChange;
                    scaleFactor = 0.7f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 3;
                    endOffset.Y += 3;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), (byte)displayLayer, () => { });
                    displayLayer += displayLayerChange;
                    scaleFactor = 0.55f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 2;
                    endOffset.Y += 2;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), (byte)displayLayer, () => { HideOverlay(); PlayBurn(); });
                    break;
                }
                case Spell.Fireball:
                {
                    var monsterRow = (MonsterRow)(fromMonster ? startPosition / 6 : tile / 6);
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
                    AddAnimation(CombatGraphicIndex.FireBall, 8, GetSourcePosition(), GetTargetPosition(tile),
                        BattleEffects.GetFlyDuration((uint)startPosition, (uint)tile),
                        fromMonster ? endScale : 2.0f, fromMonster ? 2.0f : endScale, 255, () => { HideOverlay(); PlayBurn(); });
                    break;
                }
                case Spell.Firestorm:
                case Spell.Firepillar:
                {
                    PlayBurn();
                    break;
                }
                case Spell.Waterfall:
                {
                    var targetPosition = GetTargetPosition(tile);
                    var startPosition = new Position(targetPosition.X, Global.CombatBackgroundArea.Top);
                    byte displayLayer = (byte)Math.Min(255, (tile / 6) * 60 + 60);
                    PlayMaterialization(startPosition, CombatGraphicIndex.Waterdrop, 1.0f, displayLayer, () =>
                    {
                        var animation = AddAnimation(CombatGraphicIndex.Waterdrop, 1, startPosition, targetPosition,
                            Game.TicksPerSecond / 2, 1.0f, 1.5f, displayLayer, () =>
                            {
                                var animation = AddAnimation(CombatGraphicIndex.Waterdrop, 1, targetPosition, targetPosition,
                                    Game.TicksPerSecond * 3 / 4, 0.25f, 2.5f, displayLayer);
                                animation.SetStartFrame(null, 0.25f);
                                animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                            }, null, BattleAnimation.AnimationScaleType.YOnly);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.SetStartFrame(null, 0.5f);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.YOnly;
                        animation.SetStartFrame(null, 1.0f);
                    }, startPosition, 1.0f, animation =>
                    {
                        animation.ScaleType = BattleAnimation.AnimationScaleType.XOnly;
                        animation.SetStartFrame(null, 0.5f);
                        animation.ScaleType = BattleAnimation.AnimationScaleType.None;
                    });
                    break;
                }
                case Spell.Iceball:
                {
                    var monsterRow = (MonsterRow)(fromMonster ? startPosition / 6 : tile / 6);
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
                    AddAnimation(CombatGraphicIndex.IceBall, 1, GetSourcePosition(), GetTargetPosition(tile),
                        BattleEffects.GetFlyDuration((uint)startPosition, (uint)tile),
                        fromMonster ? endScale : 2.0f, fromMonster ? 2.0f : endScale, 255, () => { HideOverlay(); PlayChill(); });
                    break;
                }
                case Spell.Icestorm:
                case Spell.Iceshower:
                {
                    PlayChill();
                    break;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }

            lastPosition = tile;
        }

        public void Destroy()
        {
            animations.ForEach(a => a?.Destroy());
            animations.Clear();
            HideOverlay();
        }

        public void Update(uint ticks)
        {
            // Note: ToList is important as Update might remove the animation from the collection.
            animations.ToList().ForEach(a => a?.Update(ticks));
        }
    }
}
