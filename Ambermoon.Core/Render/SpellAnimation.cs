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
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
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

        void AddAnimation(CombatGraphicIndex graphicIndex, int[] frameIndices, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 200, Action finishAction = null)
        {
            var info = renderView.GraphicProvider.GetCombatGraphicInfo(graphicIndex);
            var sprite = renderView.SpriteFactory.Create(info.GraphicInfo.Width, info.GraphicInfo.Height, true, displayLayer) as ILayerSprite;
            sprite.ClipArea = Global.CombatBackgroundArea;
            sprite.Layer = renderView.GetLayer(Layer.UI);
            sprite.PaletteIndex = 17;
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
            animation.SetStartFrame(textureAtlas.GetOffset(Graphics.CombatGraphicOffset + (uint)graphicIndex),
                new Size(info.GraphicInfo.Width, info.GraphicInfo.Height), startPosition, startScale);
            animation.Play(frameIndices, duration / (uint)frameIndices.Length, game.CurrentBattleTicks, endPosition, endScale);
            animations.Add(animation);
        }

        void AddAnimation(CombatGraphicIndex graphicIndex, int numFrames, Position startPosition, Position endPosition,
            uint duration, float startScale = 1.0f, float endScale = 1.0f, byte displayLayer = 200, Action finishAction = null)
        {
            AddAnimation(graphicIndex, Enumerable.Range(0, numFrames).ToArray(), startPosition, endPosition,
                duration, startScale, endScale, displayLayer, finishAction);
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
                    // TODO
                    return new Position();
                case Spell.LPStealer:
                case Spell.SPStealer:
                case Spell.Firebeam:
                case Spell.Fireball:
                case Spell.Iceball:
                    if (fromMonster)
                    {
                        return layout.GetMonsterCombatCenterPosition(startPosition, battle.GetCharacterAt(startPosition) as Monster);
                    }
                    else
                    {
                        return Layout.GetPlayerSlotCenterPosition(startPosition % 6);
                    }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }
        }

        Position GetTargetPosition(int position)
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
                case Spell.DissolveVictim:
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
                    if (fromMonster) // target is party member
                    {
                        return Layout.GetPlayerSlotCenterPosition(position % 6);
                    }
                    else // target is monster
                    {
                        return layout.GetMonsterCombatCenterPosition(position, battle.GetCharacterAt(position) as Monster);
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
                case Spell.DissolveVictim:
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                case Spell.Firebeam:
                    return; // TODO
                case Spell.Fireball:
                {
                    // This only shows a static fireball in the foreground and makes the screen red.
                    ShowOverlay(Color.FireOverlay);
                    float scale = fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 2.0f;
                    AddAnimation(CombatGraphicIndex.FireBall, 8, GetSourcePosition(), GetSourcePosition(), Game.TicksPerSecond * 3 / 4, scale, scale, 200, () =>
                    {
                        HideOverlay();
                        this.finishAction?.Invoke();
                    });
                    break;
                }
                case Spell.Firestorm:
                {
                    ShowOverlay(Color.FireOverlay);
                    var info = renderView.GraphicProvider.GetCombatGraphicInfo(CombatGraphicIndex.BigFlame);
                    void AddFlameAnimation(float startScale, float endScale, Position startGroundPosition, Position endGroundPosition,
                        int startFrame, Action finishAction)
                    {
                        // Note: The start frame also adds an x-offset
                        int[] frames = (startFrame == 0 ? Enumerable.Range(0, 8) :
                            Enumerable.Concat(Enumerable.Range(startFrame, 8 - startFrame), Enumerable.Range(0, startFrame))).ToArray();
                        int startXOffset = startFrame * Util.Round(startScale * info.GraphicInfo.Width);
                        int endXOffset = startFrame * Util.Round(endScale * info.GraphicInfo.Width);
                        int halfStartHeight = Util.Round(0.5f * startScale * info.GraphicInfo.Height);
                        int halfEndHeight = Util.Round(0.5f * endScale * info.GraphicInfo.Height);
                        AddAnimation(CombatGraphicIndex.BigFlame, frames, new Position(startGroundPosition.X + startXOffset, startGroundPosition.Y - halfStartHeight),
                            new Position(endGroundPosition.X + endXOffset, endGroundPosition.Y - halfEndHeight), Game.TicksPerSecond * 3 / 4,
                            startScale, endScale, 200, finishAction);
                    }
                    float scale = fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 2.0f;
                    var combatArea = Global.CombatBackgroundArea;
                    var leftPosition = fromMonster ? Layout.GetPlayerSlotCenterPosition(0) : Layout.GetMonsterCombatGroundPosition(renderView, targetRow * 6);
                    for (int i = 0; i < 4; ++i)
                    {
                        float baseScale = scale - i * 0.1f;

                        AddFlameAnimation(baseScale, baseScale, new Position(combatArea.Right, leftPosition.Y),
                            leftPosition, i, () =>
                        {
                            AddFlameAnimation(baseScale, 0.0f, leftPosition,
                                new Position(combatArea.Left, leftPosition.Y), 0, i == 3 ? (Action)(() =>
                                {
                                    HideOverlay();
                                    this.finishAction?.Invoke();
                                }) : null);
                        });
                    }
                    break;
                }
                case Spell.Firepillar:
                case Spell.Waterfall:
                    return; // TODO
                case Spell.Iceball:
                {
                    // This only shows a static iceball in the foreground and makes the screen blue.
                    /*ShowOverlay(Color.IceOverlay);
                    float scale = fromMonster ? renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)(startPosition / 6)) : 2.0f;
                    AddAnimation(CombatGraphicIndex.IceBall, 8, GetSourcePosition(), GetSourcePosition(), Game.TicksPerSecond * 3 / 4, scale, scale, 200, () =>
                    {
                        HideOverlay();
                        this.finishAction?.Invoke();
                    });
                    break;*/
                    return; // TODO
                }
                case Spell.Icestorm:
                case Spell.Iceshower:
                    // TODO
                    return;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }
        }

        public void MoveTo(int tile, Action<uint> finishAction)
        {
            this.finishAction = () => finishAction?.Invoke(game.CurrentBattleTicks);

            // Used by fire spells
            void PlayBurn()
            {
                var position = GetTargetPosition(tile);
                AddAnimation(CombatGraphicIndex.SmallFlame, 6, position, position, Game.TicksPerSecond);
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
                case Spell.DissolveVictim:
                case Spell.Mudsling:
                case Spell.Rockfall:
                case Spell.Earthslide:
                case Spell.Earthquake:
                case Spell.Winddevil:
                case Spell.Windhowler:
                case Spell.Thunderbolt:
                case Spell.Whirlwind:
                case Spell.Firebeam:
                    return; // TODO
                case Spell.Fireball:
                {
                    var monsterRow = (MonsterRow)(fromMonster ? startPosition / 6 : tile / 6);
                    float endScale = renderView.GraphicProvider.GetMonsterRowImageScaleFactor(monsterRow);
                    AddAnimation(CombatGraphicIndex.FireBall, 8, GetSourcePosition(), GetTargetPosition(tile),
                        BattleEffects.GetFlyDuration((uint)startPosition, (uint)tile),
                        fromMonster ? endScale : 2.0f, fromMonster ? 2.0f : endScale, 200, PlayBurn);
                    break;
                }
                case Spell.Firestorm:
                case Spell.Firepillar:
                {
                    PlayBurn();
                    break;
                }
                case Spell.Waterfall:
                case Spell.Iceball:
                case Spell.Icestorm:
                case Spell.Iceshower:
                    // TODO
                    return;
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
