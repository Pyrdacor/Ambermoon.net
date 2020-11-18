using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon.Render
{
    internal class SpellAnimation
    {
        readonly Game game;
        readonly IRenderView renderView;
        readonly Spell spell;
        readonly BattleAnimation battleAnimation;
        readonly ITextureAtlas textureAtlas;

        public SpellAnimation(Game game, IRenderView renderView, Spell spell)
        {
            this.game = game;
            this.renderView = renderView;
            this.spell = spell;
            var sprite = renderView.SpriteFactory.Create(16, 16, true, 200) as ILayerSprite;
            sprite.ClipArea = Global.CombatBackgroundArea;
            sprite.Layer = renderView.GetLayer(Layer.UI);
            sprite.PaletteIndex = 17;
            sprite.Visible = false;
            battleAnimation = new BattleAnimation(sprite);
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            Initialize();
        }

        void Initialize()
        {
            CombatGraphicIndex initialGraphicIndex = 0;
            Size frameSize = null;
            float initialScale = 1.0f;
            Position position = null;

            // TODO
            return;

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
                    // TODO
                    break;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} can not be rendered during a fight.");
            }

            battleAnimation.SetStartFrame(textureAtlas.GetOffset(Graphics.CombatGraphicOffset + (uint)initialGraphicIndex), frameSize,
                position, initialScale);
        }

        public void Destroy()
        {
            battleAnimation?.Destroy();
        }

        public void Update(uint ticks)
        {
            battleAnimation?.Update(ticks);
        }

        public void MoveTo(int tile, Action<uint> finishAction)
        {
            // TODO
            finishAction?.Invoke(game.CurrentTicks);
        }

        // TODO
    }
}
