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
            BattleAnimation.VerticalAnchor anchorY = BattleAnimation.VerticalAnchor.Center)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var textureSize = new Size(info.GraphicInfo.Width, info.GraphicInfo.Height);
            var size = customBaseSize ?? textureSize;
            var sprite = renderView.SpriteFactory.Create(size.Width, size.Height, true, displayLayer) as ILayerSprite;
            sprite.ClipArea = Global.CombatBackgroundArea;
            sprite.Layer = renderView.GetLayer(Layer.BattleEffects);
            sprite.PaletteIndex = 17;
            sprite.TextureSize = textureSize;
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
            animation.ScaleType = scaleType;
            animation.SetStartFrame(textureAtlas.GetOffset(Graphics.CombatGraphicOffset + (uint)graphicIndex),
                size, startPosition, startScale, false, textureSize, anchorX, anchorY);
            animation.Play(frameIndices, duration / (uint)frameIndices.Length, game.CurrentBattleTicks, endPosition, endScale);
            animations.Add(animation);
            return animation;
        }

        BattleAnimation AddAnimation(CombatGraphicIndex graphicIndex, int numFrames, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 255, Action finishAction = null,
            Size customBaseSize = null, BattleAnimation.AnimationScaleType scaleType = BattleAnimation.AnimationScaleType.Both,
            BattleAnimation.HorizontalAnchor anchorX = BattleAnimation.HorizontalAnchor.Center,
            BattleAnimation.VerticalAnchor anchorY = BattleAnimation.VerticalAnchor.Center)
        {
            return AddAnimation(graphicIndex, Enumerable.Range(0, numFrames).ToArray(), startPosition, endPosition,
                duration, startScale, endScale, displayLayer, finishAction, customBaseSize, scaleType);
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
                case Spell.Flight:
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
                case Spell.GhostWeapon:
                case Spell.Blink:
                case Spell.Flight:
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
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.MonsterKnowledge:
                case Spell.ShowMonsterLP:
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
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                case Spell.Firebeam:
                case Spell.Fireball:
                case Spell.Firestorm:
                case Spell.Firepillar:
                case Spell.Waterfall:
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
                case Spell.DissolveVictim:
                {
                    Position targetPosition;
                    if (fromMonster) // target is party member
                    {
                        targetPosition = Layout.GetPlayerSlotCenterPosition(position % 6); // TODO: is this right?
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
                case Spell.DissolveVictim:
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Winddevil:
                case Spell.Windhowler:
                    // Those spells use only the MoveTo method.
                    this.finishAction?.Invoke();
                    break;
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.MonsterKnowledge:
                case Spell.ShowMonsterLP:
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                case Spell.GhostWeapon:
                case Spell.Blink:
                case Spell.Flight:
                    return; // TODO
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
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                    return; // TODO
                case Spell.Firebeam:
                case Spell.Fireball:
                {
                    // This only makes the screen red for a brief duration.
                    ShowOverlay(Color.FireOverlay);
                    float scale = fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 2.0f;
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
                    float scale = GetScaleYRelativeToCombatArea(info.GraphicInfo.Height, 0.85f) *
                        (fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 1.5f);
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
                    var leftPosition = fromMonster ? Layout.GetPlayerSlotCenterPosition(2) : Layout.GetMonsterCombatGroundPosition(renderView, targetRow * 6 + 2);
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
                    return; // TODO
                case Spell.Iceball:
                {
                    // This only makes the screen blue for a brief duration.
                    ShowOverlay(Color.IceOverlay);
                    float scale = fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 2.0f;
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
                    float scale = GetScaleYRelativeToCombatArea(info.GraphicInfo.Height, 0.75f) *
                        (fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 1.5f);
                    void AddIceAnimation(int width, float startScale, float endScale, Position startGroundPosition, Position endGroundPosition,
                        int index, uint duration, Action finishAction)
                    {
                        int[] frames = new int[] { 0, 0, 1, 1, 0, 0, 1, 1 };
                        int startXOffset = index * width * 5 / 6;
                        int endXOffset = index * width * 5 / 6;
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
                    // TODO
                    return;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
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
                AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 3, 1, 1, 255, () => { });
                game.AddTimedEvent(TimeSpan.FromMilliseconds(150), () =>
                {
                    position.X += 6;
                    position.Y += 6;
                    AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 3, 1, 1, 255, () => { });
                });
                game.AddTimedEvent(TimeSpan.FromMilliseconds(300), () =>
                {
                    position.X -= 12;
                    position.Y -= 4;
                    AddAnimation(combatGraphicIndex, frameCount, position, position - new Position(0, 6), Game.TicksPerSecond / 3);
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

            // Used for all curses
            void PlayCurse(CombatGraphicIndex iconGraphicIndex)
            {
                // Note: The hurt animation comes first so we immediately call the passed finish action
                // which will display the hurt animation.
                finishAction?.Invoke(game.CurrentBattleTicks, true, false); // Play hurt animation but do not finish.
                this.finishAction = () => finishAction?.Invoke(game.CurrentBattleTicks, false, true); // This is called after the animation to finish.

                float scale = fromMonster ? 2.0f : renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(tile / 6));
                var targetPosition = GetTargetPosition(tile) - new Position(0, Util.Round(6 * scale));
                game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                {
                    byte displayLayer = (byte)(fromMonster ? 255 : ((tile / 6) * 60 + 60));
                    void AddCurseAnimation(CombatGraphicIndex graphicIndex, Action finishAction, bool reverse)
                    {
                        AddAnimation(graphicIndex, 1, targetPosition, targetPosition, reverse ? Game.TicksPerSecond / 4 : Game.TicksPerSecond / 2,
                            reverse ? scale : 0.0f, reverse ? 0.5f * scale : scale, displayLayer, finishAction);
                    }
                    AddCurseAnimation(CombatGraphicIndex.RedRing, () => { }, false);
                    AddCurseAnimation(iconGraphicIndex, () =>
                    {
                        AddCurseAnimation(CombatGraphicIndex.RedRing, () => { }, true);
                        AddCurseAnimation(iconGraphicIndex, null, true); // This will trigger the outer finish action
                    }, false);
                });
            }

            // Used for Anti-undead spells
            void PlayHolyLight()
            {
                // Should always be a monster but just in case we check here.
                if (battle.GetCharacterAt(tile) is Monster monster)
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
                        }, () => { });
                        // And fade out beam too
                        AddAnimation(CombatGraphicIndex.HolyBeam, 1, startPosition, endPosition, Game.TicksPerSecond * 3 / 4, 1, 0, displayLayer, null,
                            new Size(beamWidth, beamHeight), BattleAnimation.AnimationScaleType.XOnly);
                    }, new Size(beamWidth, beamHeight));
                }
            }

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
                    return; // TODO
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                    PlayHolyLight();
                    break;
                case Spell.GreatHealing:
                case Spell.MassHealing:
                case Spell.RemoveRigidness:
                case Spell.RemoveLamedness:
                case Spell.WakeUp:
                case Spell.RemoveIrritation:
                case Spell.RestoreStamina:
                case Spell.GhostWeapon:
                case Spell.Blink:
                case Spell.Flight:
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
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.MonsterKnowledge:
                case Spell.ShowMonsterLP:
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                    return; // TODO
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
                    int row = tile / 6;
                    var position = GetTargetPosition(tile) - new Position(0, 6 + row * row);
                    void ShowParticle()
                    {
                        AddAnimation(CombatGraphicIndex.GreenStar, 5, position, position - new Position(0, 4), Game.TicksPerSecond / 5, 1, 1, 255,
                            () => finishAction?.Invoke(game.CurrentBattleTicks, false, true));
                    }                   
                    if (battle.GetCharacterAt(tile) is Monster monster)
                    {
                        // Shrink monster to zero
                        battle.StartMonsterAnimation(monster, animation =>
                        {
                            animation.AnchorY = BattleAnimation.VerticalAnchor.Bottom;
                            animation.PlayWithoutAnimating(Game.TicksPerSecond, game.CurrentBattleTicks, position, 0.0f);
                        }, ShowParticle);
                    }
                    else
                    {
                        ShowParticle();
                    }
                    break;
                }
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                    return; // TODO
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
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition, endPosition,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 252, () => { });
                    scaleFactor = 0.85f;
                    var startOffset = new Position(Util.Round(scaleFactor * startXOffset), 4);
                    var endOffset = new Position(Util.Round(scaleFactor * endXOffset), 4);
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 253, () => { });
                    scaleFactor = 0.7f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 3;
                    endOffset.Y += 3;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 254, () => { });
                    scaleFactor = 0.55f;
                    startOffset.X += Util.Round(scaleFactor * startXOffset);
                    endOffset.X += Util.Round(scaleFactor * endXOffset);
                    startOffset.Y += 2;
                    endOffset.Y += 2;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, startPosition + startOffset, endPosition + endOffset,
                        BattleEffects.GetFlyDuration((uint)this.startPosition, (uint)tile),
                        scaleFactor * (fromMonster ? endScale : startScale),
                        scaleFactor * (fromMonster ? startScale : endScale), 255, () => { HideOverlay(); PlayBurn(); });
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
                    // TODO
                    return;
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

        // TODO
    }
}
