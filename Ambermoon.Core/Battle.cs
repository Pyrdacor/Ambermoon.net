/*
 * Battle.cs - Battle logic and visuals
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Attribute = Ambermoon.Data.Attribute;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon
{
    internal static class CharacterBattleExtensions
    {
        public static bool HasLongRangedWeapon(this Character character, IItemManager itemManager)
        {
            var itemIndex = character.Equipment?.Slots[EquipmentSlot.RightHand]?.ItemIndex;

            if (itemIndex == null || itemIndex == 0)
                return false;

            var weapon = itemManager.GetItem(itemIndex.Value);
            return weapon.Type == ItemType.LongRangeWeapon;
        }

        public static bool HasLongRangedAttack(this Character character, IItemManager itemManager, out bool hasAmmo)
        {
            hasAmmo = false;

            var itemIndex = character.Equipment?.Slots[EquipmentSlot.RightHand]?.ItemIndex;

            if (itemIndex == null || itemIndex == 0)
                return false;

            var weapon = itemManager.GetItem(itemIndex.Value);
            bool hasLongRangedWeapon = weapon.Type == ItemType.LongRangeWeapon;

            if (hasLongRangedWeapon)
            {
                if (weapon.UsedAmmunitionType == AmmunitionType.None)
                {
                    hasAmmo = true;
                    return true;
                }

                var ammoSlot = character.Equipment.Slots[EquipmentSlot.LeftHand];
                hasAmmo = ammoSlot?.ItemIndex != null && ammoSlot.ItemIndex != 0 && ammoSlot.Amount > 0 &&
                    itemManager.GetItem(ammoSlot.ItemIndex).AmmunitionType == weapon.UsedAmmunitionType;

                // I guess for monsters it's fine if the monster has the ammo in inventory
                if (!hasAmmo && character is Monster)
                {
                    hasAmmo = character.Inventory.Slots.Any(slot =>
                    {
                        if (slot?.ItemIndex == null || slot.ItemIndex == 0 || slot.Amount == 0)
                            return false;

                        var item = itemManager.GetItem(slot.ItemIndex);

                        if (item?.Type != ItemType.Ammunition || item.AmmunitionType != weapon.UsedAmmunitionType)
                            return false;

                        return true;
                    });
                }

                return true;
            }

            return false;
        }
    }

    internal class Battle
    {
        internal enum BattleActionType
        {
            None,
            /// <summary>
            /// Parameter: New position index (0-29)
            /// 
            /// Plays the move animation for monsters and the moves
            /// the monster or party member.
            /// </summary>
            Move,
            /// <summary>
            /// No parameter
            /// 
            /// This is an immediate action for the party and is therefore
            /// processed outside of battle rounds. If the monster group decides
            /// to move forward this is done as the last action in a battle round.
            /// </summary>
            MoveGroupForward,
            /// <summary>
            /// - Lowest 5 bits: Tile index (0-29) to attack
            /// - Next 11 bits: Weapon item index (can be 0 -> attacking without weapon)
            /// - Next 11 bits: Optional ammunition item index
            /// 
            /// This plays a monster or attack animation and prints text about
            /// how much damage the attacker dealt or if he missed etc.
            /// 
            /// After this an additional <see cref="Hurt"/> action will follow
            /// which plays the hurt animation and removed the hitpoints from the enemy.
            /// </summary>
            Attack,
            /// <summary>
            /// No parameter
            /// 
            /// This is not used as a real action and it is only available for party members.
            /// Each player who picks this action will get a chance equal to his Parry
            /// skill to block physical attacks. This is only checked if the attack did
            /// not miss or failed before.
            /// </summary>
            Parry,
            /// <summary>
            /// Parameter:
            /// - Lowest 5 bits: Tile index (0-29) or row (0-4) to cast spell on
            /// - Next 11 bits: Item index (when spell came from an item, otherwise 0)
            /// - Upper 16 bits: Spell index
            /// 
            /// This plays the spell animation and also calculates and applies
            /// spell effects like damage. So this also plays hurt effects on monsters.
            /// </summary>
            CastSpell,
            /// <summary>
            /// No parameter
            /// 
            /// Plays the flee animation for monsters and removes the monster or
            /// party member from the battle.
            /// </summary>
            Flee,
            /// <summary>
            /// No parameter
            /// 
            /// This just prints text about what the actor is doing.
            /// The text depends on the following enqueued action.
            /// </summary>
            DisplayActionText,
            /// <summary>
            /// - Lowest 5 bits: Tile index (0-29) which should be hurt
            /// - Rest: Damage amount
            /// 
            /// This is playing hurt animations like blood on monsters
            /// or claw on player. It also removes the hitpoints and
            /// displays this as an effect on players.
            /// This is used after attacks only, spells will automatically
            /// play the hurt animations as well.
            /// It is added for every  attack action but might be
            /// skipped if attack misses etc.
            /// </summary>
            Hurt,
            // The following actions will be optional actions
            // that might occur by chance.
            WeaponBreak,
            ArmorBreak,
            DefenderWeaponBreak,
            DefenderShieldBreak,
            LastAmmo,
            DropWeapon
        }

        /*
         * Possible action chains:
         * 
         *  - Attack
         *      1. DisplayActionText
         *      2. Attack
         *      3. Hurt
         *  - Cast spell
         *      1. DisplayActionText
         *      2. CastSpell
         *  - Move
         *      1. DisplayActionText
         *      2. Move
         *  - Flee
         *      1. DisplayActionText
         *      2. Flee
         *  - Parry
         *      This isn't executed in battle but a parrying
         *      character has a chance to parry an attack.
         *  - MoveGroupForward
         *      1. DisplayActionText
         *      2. MoveGroupForward
         */

        internal class PlayerBattleAction
        {
            public BattleActionType BattleAction;
            public uint Parameter;
        }

        internal class BattleAction
        {
            public Character Character;
            public BattleActionType Action = BattleActionType.None;
            public uint ActionParameter;
            public bool Skip = false; // Used for hurt actions if attacks miss, etc.
        }

        readonly Game game;
        readonly Layout layout;
        readonly PartyMember[] partyMembers;
        readonly Queue<BattleAction> roundBattleActions = new();
        readonly Character[] battleField = new Character[6 * 5];
        readonly List<PartyMember> parryingPlayers = new(Game.MaxPartyMembers);
        readonly List<Character> fledCharacters = new();
        readonly List<PartyMember> hurriedPlayers = new();
        readonly Dictionary<int, int> monsterSizeDisplayLayerMapping = new();
        uint? animationStartTicks = null;
        Monster currentlyAnimatedMonster = null;
        BattleAnimation currentBattleAnimation = null;
        bool startAnimationRunning = false;
        bool idleAnimationRunning = false;
        int finishedStartAnimationCount = 0;
        uint nextIdleAnimationTicks = 0;
        Queue<Monster> startAnimationMonsters = new();
        List<BattleAnimation> effectAnimations = null;
        SpellAnimation currentSpellAnimation = null;
        readonly List<KeyValuePair<uint, ItemSlotFlags>> brokenItems = new();
        readonly List<Monster> droppedWeaponMonsters = new();
        readonly uint[] totalPlayerDamage = new uint[Game.MaxPartyMembers];
        readonly uint[] numSuccessfulPlayerHits = new uint[Game.MaxPartyMembers];
        readonly uint[] averagePlayerDamage = new uint[Game.MaxPartyMembers];
        readonly List<uint> monsterMorale = new();
        readonly List<uint> totalMonsterDamage = new();
        readonly List<uint> numSuccessfulMonsterHits = new();
        readonly List<uint> averageMonsterDamage = new();
        uint relativeDamageEfficiency = 0;
        bool showMonsterLP = false;
        bool showElements = false;
        bool foreseeMagic = false;
        bool foreseeAttack = false;
        readonly List<Monster> weakenedMonsters = new();
		readonly List<Character> protectedCharacters = new();
		bool anyMonsterWantedToFlee = false;
        internal bool NeedsClickForNextAction { get; set; }
        internal int Speed { get; set; }
        public bool ReadyForNextAction { get; private set; } = false;
        public bool WaitForClick { get; set; } = false;
        public bool SkipNextBattleFieldClick { get; private set; } = false;

        public event Action StartAnimationFinished;
        public event Action RoundFinished;
        public event Action<Character> CharacterDied;
        public event Action<Game.BattleEndInfo> BattleEnded;
        public event Action<BattleAction> ActionCompleted;
        public event Action<PartyMember> PlayerWeaponBroke;
        public event Action<PartyMember> PlayerLastAmmoUsed;
        public event Action<PartyMember> PlayerLostTarget;
        event Action AnimationFinished;
        readonly List<Monster> initialMonsters = new List<Monster>();
        readonly Dictionary<int, IRenderText> battleFieldDamageTexts = new Dictionary<int, IRenderText>();
        public IEnumerable<Monster> Monsters => battleField.Where(c => c?.Type == CharacterType.Monster).Cast<Monster>();
        public IEnumerable<PartyMember> PartyMembers => battleField.Where(c => c?.Type == CharacterType.PartyMember).Cast<PartyMember>();
        public IEnumerable<Character> Characters => battleField.Where(c => c != null);
        public Character GetCharacterAt(int index) => battleField[index];
        public Character GetCharacterAt(int column, int row) => GetCharacterAt(column + row * 6);
        public int GetSlotFromCharacter(Character character) => battleField.ToList().IndexOf(character);
        public bool HasPartyMemberFled(PartyMember partyMember) => fledCharacters.Contains(partyMember);
        public bool IsBattleFieldEmpty(int slot) => battleField[slot] == null;
        public bool RoundActive { get; private set; } = false;
        public bool CanPartyMoveForward => !battleField.Skip(12).Take(6).Any(c => c != null) && // middle row empty
            !battleField.Skip(18).Take(6).Any(c => c?.Type == CharacterType.Monster); // and no monster in front row
        public bool CanMonstersMoveForward => !battleField.Skip(18).Take(6).Any(c => c != null); // 4th row empty
        public bool HasStartAnimation { get; } = false;
        public bool StartAnimationPlaying => startAnimationRunning;

        public Battle(Game game, Layout layout, PartyMember[] partyMembers, MonsterGroup monsterGroup,
            Dictionary<int, BattleAnimation> monsterBattleAnimations, bool needsClickForNextAction)
        {
            this.game = game;
            this.layout = layout;
            this.partyMembers = partyMembers;
            NeedsClickForNextAction = needsClickForNextAction;

            // place characters
            for (int i = 0; i < partyMembers.Length; ++i)
            {
                if (partyMembers[i] != null && partyMembers[i].Alive)
                {
                    battleField[18 + game.CurrentSavegame.BattlePositions[i]] = partyMembers[i];
                }
            }
            List<int> monsterSizes = new List<int>(24);
            for (int y = 0; y < 3; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    var monster = monsterGroup.Monsters[x, y];

                    if (monster != null)
                    {
                        int index = x + y * 6;
                        battleField[index] = monster;
                        monsterBattleAnimations[index].AnimationFinished += () => MonsterAnimationFinished(monster);
                        initialMonsters.Add(monster);
                        totalMonsterDamage.Add(0);
                        numSuccessfulMonsterHits.Add(0);
                        averageMonsterDamage.Add(0);
                        monsterMorale.Add(monster.Morale);
                        monsterSizes.Add((int)monster.MappedFrameWidth);
                    }
                }
            }
            monsterSizes.Sort();
            // Each row has a display layer range of 60 (row 0: 1-60, row 1: 61-120, row 2: 121-180, row 3: 181-240).
            // Depending on monster size an offset of 0, 6, 12, 18, 24, 30, 36, 42, 48 or 54 is possible (up to 10 monster sizes).
            // Smaller (thinner) monsters get higher values to appear in front of larger monsters.
            // Each column then increases the value by 0 to 5.
            int currentMonsterSizeIndex = 0;
            foreach (var monsterSize in monsterSizes.Distinct())
            {
                monsterSizeDisplayLayerMapping.Add(monsterSize, (9 - currentMonsterSizeIndex) * 6);

                if (currentMonsterSizeIndex < 9)
                    ++currentMonsterSizeIndex;
            }

            effectAnimations = new List<BattleAnimation>();

            var startAnimationMonsters = Monsters.Where(m => m.Animations[(int)MonsterAnimationType.Start].UsedAmount != 0);

            if (startAnimationMonsters.Any())
            {
                HasStartAnimation = true;
                animationStartTicks = 0;
                startAnimationRunning = true;
                currentlyAnimatedMonster = startAnimationMonsters.First();
                foreach (var monster in startAnimationMonsters.Skip(1))
                {
                    this.startAnimationMonsters.Enqueue(monster);
                    layout.UpdateMonsterCombatSprite(monster, MonsterAnimationType.Start, 0, 0);
                }
                layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Start, 0, 0);
            }
            else
            {
                game.EndSequence();
                SetupNextIdleAnimation(0);
            }
        }

        public void SetMonsterAnimations(Dictionary<int, BattleAnimation> monsterBattleAnimations)
        {
            foreach (var monsterBattleAnimation in monsterBattleAnimations)
            {
                var monster = GetCharacterAt(monsterBattleAnimation.Key) as Monster;
                monsterBattleAnimation.Value.AnimationFinished += () => MonsterAnimationFinished(monster);
            }
        }

        void MonsterAnimationFinished(Monster monster)
        {
            if (!startAnimationRunning)
            {
                animationStartTicks = null;
                idleAnimationRunning = false;
                currentlyAnimatedMonster = null;
                layout.ResetMonsterCombatSprite(monster);
            }
        }

        void SetupNextIdleAnimation(uint battleTicks)
        {
            // TODO: adjust to work like original
            // TODO: in original idle animations can also occur in active battle round while no other animation is played
            nextIdleAnimationTicks = battleTicks + (uint)game.RandomInt(1, 16) * Game.TicksPerSecond / 4;
        }

        public void InitImitatingPlayers()
        {
			foreach (var partyMember in PartyMembers.Where(partyMember => partyMember.Alive && !fledCharacters.Contains(partyMember)))
			{
				if (imitationBackupData.TryGetValue(partyMember.Index, out var backup))
				{
					game.ReplacePartyMemberBattleFieldSprite(partyMember, backup.MonsterGraphicIndex);
				}
			}
        }

        public void Update(uint battleTicks, uint normalizedBattleTicks)
        {
            if (RoundActive && roundBattleActions.Count != 0 && (currentlyAnimatedMonster == null || idleAnimationRunning))
            {
                var currentAction = roundBattleActions.Peek();

                if (currentAction.Character is Monster currentMonster)
                {
                    var animationType = currentAction.Action.ToAnimationType();

                    if (animationType != null)
                    {
                        if (idleAnimationRunning) // idle animation still running
                        {
                            idleAnimationRunning = false;
                            layout.ResetMonsterCombatSprite(currentlyAnimatedMonster);
                            animationStartTicks = battleTicks;
                        }
                        else if (animationStartTicks == null)
                        {
                            animationStartTicks = battleTicks;
                        }

                        var animationTicks = battleTicks - animationStartTicks.Value;
                        currentlyAnimatedMonster = currentMonster;
                        layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, animationType.Value, animationTicks, battleTicks);
                    }
                }
            }

            if (!RoundActive && startAnimationRunning && finishedStartAnimationCount > 0)
                WaitForClick = true;

            if (startAnimationRunning)
            {
                var animationTicks = normalizedBattleTicks - animationStartTicks.Value;

                if (layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Start, animationTicks, normalizedBattleTicks)?.Finished != false)
                {
                    // start animation finished
                    ++finishedStartAnimationCount;

                    if (startAnimationMonsters.Any())
                    {
                        // still more start animations to play
                        animationStartTicks = normalizedBattleTicks;
                        if (currentlyAnimatedMonster != null)
                            layout.ResetMonsterCombatSprite(currentlyAnimatedMonster);
                        currentlyAnimatedMonster = startAnimationMonsters.Dequeue();
                        layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Start, 0, normalizedBattleTicks);
                    }
                    else
                    {
                        WaitForClick = false;
                        game.EndSequence();
                        animationStartTicks = null;
                        startAnimationRunning = false;
                        if (currentlyAnimatedMonster != null)
                            layout.ResetMonsterCombatSprite(currentlyAnimatedMonster);
                        SetupNextIdleAnimation(normalizedBattleTicks);
                        StartAnimationFinished?.Invoke();
                    }
                }
            }
            else if (idleAnimationRunning)
            {
                var animationTicks = normalizedBattleTicks - animationStartTicks.Value;

                // Note: Idle animations use the move animation.
                if (layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Move, animationTicks, normalizedBattleTicks)?.Finished != false)
                {
                    animationStartTicks = null;
                    idleAnimationRunning = false;
                    if (currentlyAnimatedMonster != null)
                        layout.ResetMonsterCombatSprite(currentlyAnimatedMonster);
                    SetupNextIdleAnimation(normalizedBattleTicks);
                }
            }
            else if (!RoundActive)
            {
                if (normalizedBattleTicks >= nextIdleAnimationTicks)
                {
                    var monsters = Monsters.Where(m => m.Conditions.CanMove()).ToList();

                    if (monsters.Count != 0)
                    {
                        int index = game.RandomInt(0, monsters.Count - 1);
                        animationStartTicks = normalizedBattleTicks;
                        idleAnimationRunning = true;
                        currentlyAnimatedMonster = monsters[index];
                        layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Move, 0, normalizedBattleTicks);
                    }
                }
            }

            if (ReadyForNextAction && (!NeedsClickForNextAction || !WaitForClick))
                NextAction(battleTicks);

            if (currentBattleAnimation != null)
            {
                if (!currentBattleAnimation.Update(battleTicks))
                {
                    currentBattleAnimation = null;
                    AnimationFinished?.Invoke();
                }
                else if (currentlyAnimatedMonster != null)
                {
                    SetMonsterDisplayLayer(currentBattleAnimation, currentlyAnimatedMonster);
                }
            }

            foreach (var effectAnimation in effectAnimations.ToList())
            {
                if (effectAnimation != null && !effectAnimation.Finished)
                {
                    effectAnimation.Update(battleTicks);
                }
            }

            if (currentSpellAnimation != null)
                currentSpellAnimation.Update(battleTicks);
        }

        public byte GetMonsterDisplayLayer(Monster monster, int? position = null)
        {
            position ??= GetCharacterPosition(monster);
            // Each row has a display layer range of 60 (row 0: 1-60, row 1: 61-120, row 2: 121-180, row 3: 181-240).
            // Depending on monster size an offset of 0, 6, 12, 18, 24, 30, 36, 42, 48 or 54 is possible (up to 10 monster sizes).
            // Each column then increases the value by 0 to 5.
            int column = position.Value % 6;
            int row = position.Value / 6;
            return (byte)(1 + row * 60 + monsterSizeDisplayLayerMapping[(int)monster.MappedFrameWidth] + column);
        }

        public void SetMonsterDisplayLayer(BattleAnimation animation, Monster monster, int? position = null)
        {
            byte displayLayer = GetMonsterDisplayLayer(monster, position);
            animation.SetDisplayLayer(displayLayer);
        }

        static string GetLPString(uint lp) => lp > 999 ? "***" : lp.ToString();

        static string GetMonsterLPString(Monster monster) =>
            $"{GetLPString(monster.HitPoints.CurrentValue)}/{GetLPString(monster.HitPoints.TotalMaxValue)}^{monster.Name}";

		/// <summary>
		/// Called while updating the battle. Each call will
		/// perform the next action which can be a movement,
		/// attack, spell cast, flight or group forward move.
		/// 
		/// <see cref="StartRound"/> will automatically call
		/// this method.
		/// 
		/// Each action may trigger some text messages,
		/// animations or other changes.
		/// </summary>
		public void NextAction(uint battleTicks)
        {
            ReadyForNextAction = false;

            if (roundBattleActions.Count == 0)
            {
                // Check for monster advance at end of round
                if (!anyMonsterWantedToFlee && CanMonstersMoveForward && PartyMembers.Count(p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified) && !fledCharacters.Contains(p)) > 1)
                {
                    var firstMonster = Monsters.FirstOrDefault(c => c.Alive && !fledCharacters.Contains(c)); // take any existing one to avoid NullRef exception
                    if (firstMonster != null)
                    {
                        roundBattleActions.Enqueue(new BattleAction
                        {
                            Character = firstMonster,
                            Action = BattleActionType.DisplayActionText,
                            ActionParameter = 0
                        });
                        roundBattleActions.Enqueue(new BattleAction
                        {
                            Character = firstMonster,
                            Action = BattleActionType.MoveGroupForward,
                            ActionParameter = 0
                        });
                        anyMonsterWantedToFlee = true; // set this so the monsters can't advance again this round
                        NextAction(battleTicks); // call this again but now roundBattleActions.Count is not 0, so it will execute the advance actions
                        return;
                    }
                }

                anyMonsterWantedToFlee = false;
                RoundActive = false;
                if (showMonsterLP)
                {
                    foreach (var monster in Monsters)
                    {
                        string tooltip = GetMonsterLPString(monster);

                        if (showElements)
                            tooltip += $"^({game.DataNameProvider.GetElementName(monster.Element)})";
						
                        layout.GetMonsterBattleFieldTooltip(monster).Text = tooltip;
                    }
                }
                else if (showElements)
				{
					foreach (var monster in Monsters)
					{
						layout.GetMonsterBattleFieldTooltip(monster).Text = monster.Name + $"^({game.DataNameProvider.GetElementName(monster.Element)})";
					}
				}

				foreseeMagic = false;
				foreseeAttack = false;
                protectedCharacters.Clear();

				RoundFinished?.Invoke();

                return;
            }

            var action = roundBattleActions.Dequeue();

            if (action.Skip)
            {
                NextAction(battleTicks);
                return;
            }

            RunBattleAction(action, battleTicks);
        }

        public void ResetClick()
        {
            SkipNextBattleFieldClick = false;
        }

        public void Click(uint battleTicks)
        {
            if (HasStartAnimation && startAnimationRunning && currentlyAnimatedMonster != null && finishedStartAnimationCount > 0)
            {
                // You can abort start animations after the first character by clicking
                startAnimationMonsters.Clear(); // clear all other start animation monsters
                foreach (var monster in Monsters.Where(m => m.Animations[(int)MonsterAnimationType.Start].UsedAmount != 0))
                {
                    if (monster != currentlyAnimatedMonster)
                        layout.GetMonsterBattleAnimation(monster).Reset();
                }
                layout.GetMonsterBattleAnimation(currentlyAnimatedMonster).Reset(); // this finishes the animation
                return;
            }

            SkipNextBattleFieldClick = false;

            if (!WaitForClick)
                return;

            WaitForClick = false;
            SkipNextBattleFieldClick = true;

            if (RoundActive)
            {
                if (ReadyForNextAction && NeedsClickForNextAction)
                {
                    NextAction(battleTicks);
                }
            }
            else
            {
                layout.SetBattleMessage(null);
                game.InputEnable = true;
            }
        }

        void PoisonDamageMonster(Monster monster, Action followAction)
        {
            var animation = layout.GetMonsterBattleAnimation(monster);
            uint damage = (uint)game.RandomInt(1, 5);

            void EndHurt()
            {
                monster.Damage(damage);

                if (!monster.Alive)
                {
                    HandleCharacterDeath(null, monster, followAction);
                }
                else
                {
                    followAction?.Invoke();
                }
            }

            bool hurtAnimFinished = false;
            bool hurtSplashFinished = false;

            void HurtAnimationFinished()
            {
                animation.AnimationFinished -= HurtAnimationFinished;
                currentBattleAnimation = null;
                currentlyAnimatedMonster = null;
                hurtAnimFinished = true;

                if (hurtSplashFinished)
                    EndHurt();
            }

            void HurtSplashFinished()
            {
                hurtSplashFinished = true;

                if (hurtAnimFinished)
                    EndHurt();
            }
            
            animation.AnimationFinished += HurtAnimationFinished;
            var frames = monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt);
            animation.Play(frames, (Game.TicksPerSecond / 2) / (uint)frames.Length, game.CurrentBattleTicks);
            currentBattleAnimation = animation;
            currentlyAnimatedMonster = monster;
            int tile = GetSlotFromCharacter(monster);

            PlayBattleEffectAnimation(BattleEffect.HurtMonster, (uint)tile, game.CurrentBattleTicks, HurtSplashFinished);
            ShowBattleFieldDamage(tile, damage);
            if (monster.Conditions.HasFlag(Condition.Sleep))
                game.RemoveCondition(Condition.Sleep, monster);
        }

        /// <summary>
        /// Starts a new battle round.
        /// </summary>
        /// <param name="playerBattleActions">Battle actions for party members 1-6.</param>
        /// <param name="battleTicks">Battle ticks when starting the round.</param>
        internal void StartRound(PlayerBattleAction[] playerBattleActions, uint battleTicks)
        {
            game.ProcessPoisonDamage(1, _ =>
            {
                // Remove all died party members from the battle field
                foreach (var partyMember in partyMembers.Where(p => p != null && !p.Alive))
                    RemoveCharacterFromBattleField(partyMember);

                var poisonedMonsters = Monsters.Where(m => m.Alive && m.Conditions.HasFlag(Condition.Poisoned)).ToList();

                if (poisonedMonsters.Count == 0)
                    Start();
                else
                {
                    void HandleMonster(int index)
                    {
                        var next = index == poisonedMonsters.Count - 1
                            ? (Action)Start
                            : () => HandleMonster(index + 1);
                        var monster = poisonedMonsters[index];
                        PoisonDamageMonster(monster, next);
                    }

                    HandleMonster(0);
                }

                void Start()
                {
                    // Recalculate the RDE value each round
                    var partyDamage = Util.Limit(1, (uint)averagePlayerDamage.Where((d, i) => partyMembers[i] != null && battleField.Contains(partyMembers[i])).Sum(x => x), 0x7fff);
                    var monsterDamage = Util.Limit(1, (uint)averageMonsterDamage.Where((d, i) => battleField.Contains(initialMonsters[i])).Sum(x => x), 0x7fff);
                    relativeDamageEfficiency = Math.Min(partyDamage * 50 / monsterDamage, 100);

                    var roundActors = battleField
                        .Where(f => f != null)
                        .OrderByDescending(c => c.Attributes[Attribute.Speed].TotalCurrentValue)
                        .ThenBy(c => c.Type)
                        .ToList();
                    parryingPlayers.Clear();

                    foreach (var droppedWeaponMonster in droppedWeaponMonsters)
                    {
                        if (roundActors.Contains(droppedWeaponMonster))
                        {
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = droppedWeaponMonster,
                                Action = BattleActionType.DropWeapon,
                                ActionParameter = 0
                            });
                        }
                    }

                    droppedWeaponMonsters.Clear();
                    anyMonsterWantedToFlee = false;

                    var forbiddenMoveSpots = playerBattleActions.Where(a => a != null && a.BattleAction == BattleActionType.Move)
                        .Select(a => (int)GetTargetTileOrRowFromParameter(a.Parameter)).ToList();
                    var forbiddenMonsterMoveSpots = new List<int>();

                    foreach (var roundActor in roundActors)
                    {
                        if (roundActor is Monster monster)
                        {
                            AddMonsterActions(monster, forbiddenMonsterMoveSpots);
                        }
                        else
                        {
                            var partyMember = roundActor as PartyMember;
                            int playerIndex = partyMembers.ToList().IndexOf(partyMember);
                            var playerAction = playerBattleActions[playerIndex];

                            if (partyMember.Conditions.HasFlag(Condition.Panic))
                            {
                                PickPanicAction(partyMember, playerAction, forbiddenMoveSpots);
                            }
                            else if (partyMember.Conditions.HasFlag(Condition.Crazy))
                            {
                                PickMadAction(partyMember, playerAction, forbiddenMoveSpots);
                            }

                            if (playerAction.BattleAction == BattleActionType.None)
                                continue;
                            if (playerAction.BattleAction == BattleActionType.Parry)
                            {
                                parryingPlayers.Add(partyMember);
                                continue;
                            }

                            // Note: We add twice as much attack actions but the second half with
                            // Skip=true. They will be used if the player has the Hurry buff.
                            int numActions = playerAction.BattleAction == BattleActionType.Attack
                                ? partyMember.AttacksPerRound * 2 : 1;

                            for (int i = 0; i < numActions; ++i)
                            {
                                bool skip = i >= partyMember.AttacksPerRound;
                                roundBattleActions.Enqueue(new BattleAction
                                {
                                    Character = partyMember,
                                    Action = BattleActionType.DisplayActionText,
                                    ActionParameter = 0,
                                    Skip = skip
                                });
                                roundBattleActions.Enqueue(new BattleAction
                                {
                                    Character = partyMember,
                                    Action = playerAction.BattleAction,
                                    ActionParameter = playerAction.Parameter,
                                    Skip = skip
                                });
                                if (playerAction.BattleAction == BattleActionType.Attack)
                                {
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.WeaponBreak,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.ArmorBreak,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.DefenderWeaponBreak,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.DefenderShieldBreak,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.LastAmmo,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                    roundBattleActions.Enqueue(new BattleAction
                                    {
                                        Character = partyMember,
                                        Action = BattleActionType.Hurt,
                                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter)),
                                        Skip = skip
                                    });
                                }
                            }
                        }
                    }

                    RoundActive = true;
                    NextAction(battleTicks);
                }
            });
        }

        void AddMonsterActions(Monster monster, List<int> forbiddenMonsterMoveSpots)
        {
            bool wantsToFlee = MonsterWantsToFlee(monster);

            if (wantsToFlee)
                anyMonsterWantedToFlee = true;

            BattleActionType action;
            uint actionParameter;
            bool canCast = true;

            while (true)
            {
                action = PickMonsterAction(monster, wantsToFlee, forbiddenMonsterMoveSpots, canCast);

                if (action == BattleActionType.None) // do nothing
                    return;

                actionParameter = PickActionParameter(action, monster, wantsToFlee, forbiddenMonsterMoveSpots);

                if (action == BattleActionType.CastSpell && actionParameter == 0)
                {
                    canCast = false;
                    continue;
                }

                break;
            }

            int numActions = action == BattleActionType.Attack
                ? monster.AttacksPerRound : 1;

            for (int i = 0; i < numActions; ++i)
            {
                roundBattleActions.Enqueue(new BattleAction
                {
                    Character = monster,
                    Action = BattleActionType.DisplayActionText,
                    ActionParameter = 0
                });
                roundBattleActions.Enqueue(new BattleAction
                {
                    Character = monster,
                    Action = action,
                    ActionParameter = actionParameter
                });
                if (action == BattleActionType.Attack)
                {
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.WeaponBreak,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.ArmorBreak,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.DefenderWeaponBreak,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.DefenderShieldBreak,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.LastAmmo,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.Hurt,
                        ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(actionParameter))
                    });
                }
            }
        }

        void KillMonster(PartyMember attacker, Character target, int targetPosition)
        {
            CharacterDied?.Invoke(target);
            if (attacker == null) // death from poison etc
            {
                foreach (var partyMember in PartyMembers.Where(p => p.Alive))
                {
                    if (roundBattleActions.Any(a => a.Character == partyMember &&
                        a.Action == BattleActionType.Attack &&
                        GetTargetTileOrRowFromParameter(a.ActionParameter) == targetPosition))
                        PlayerLostTarget?.Invoke(partyMember);
                }
            }
            else
                PlayerLostTarget?.Invoke(attacker);
            if (Monsters.Count() == 0)
            {
                EndBattleCleanup();
                BattleEnded?.Invoke(new Game.BattleEndInfo
                {
                    MonstersDefeated = true,
                    KilledMonsters = initialMonsters.Where(m => !fledCharacters.Contains(m)).ToList(),
                    FledPartyMembers = fledCharacters.Where(c => c?.Type == CharacterType.PartyMember).Cast<PartyMember>().ToList(),
                    TotalExperience = initialMonsters.Sum(m => m.DefeatExperience),
                    BrokenItems = brokenItems
                });
            }
        }

        internal void EndBattleCleanup()
        {
            foreach (var battleFieldDamageText in battleFieldDamageTexts)
                battleFieldDamageText.Value?.Delete();
            battleFieldDamageTexts.Clear();
            currentSpellAnimation?.Destroy();
            currentSpellAnimation = null;
            RestoreImitatingPartyMembers();
        }

        /// <summary>
        /// Note: This is only use to end a battle externally (e.g. through a cheat code).
        /// </summary>
        /// <param name="flee"></param>
        internal void EndBattle(bool flee)
        {
            if (flee)
            {
                fledCharacters.AddRange(battleField.Where(c => c?.Type == CharacterType.PartyMember));
                EndBattleCleanup();
                BattleEnded?.Invoke(new Game.BattleEndInfo
                {
                    MonstersDefeated = false
                });
            }
            else
            {
                EndBattleCleanup();
                BattleEnded?.Invoke(new Game.BattleEndInfo
                {
                    MonstersDefeated = true,
                    KilledMonsters = initialMonsters.Where(m => !fledCharacters.Contains(m)).ToList(),
                    FledPartyMembers = fledCharacters.Where(c => c?.Type == CharacterType.PartyMember).Cast<PartyMember>().ToList(),
                    TotalExperience = initialMonsters.Sum(m => m.DefeatExperience),
                    BrokenItems = brokenItems
                });
            }
        }

        void KillPlayer(Character target)
        {
            CharacterDied?.Invoke(target);

            if (!partyMembers.Any(p => p != null && p.Alive && p.Conditions.CanFight() && !fledCharacters.Contains(p)))
            {
                EndBattleCleanup();
                BattleEnded?.Invoke(new Game.BattleEndInfo
                {
                    MonstersDefeated = false
                });
            }
            else
            {
                var targetPartyMember = target as PartyMember;
                layout.SetCharacter(game.SlotFromPartyMember(targetPartyMember).Value, targetPartyMember);
            }
        }

        void TrackPlayerHit(PartyMember partyMember, uint damage)
        {
            int index = partyMembers.ToList().IndexOf(partyMember);
            totalPlayerDamage[index] += damage;
            ++numSuccessfulPlayerHits[index];
            averagePlayerDamage[index] = totalPlayerDamage[index] / numSuccessfulPlayerHits[index];
        }

        void TrackMonsterHit(Monster monster, uint damage)
        {
            int index = initialMonsters.IndexOf(monster);
            totalMonsterDamage[index] += damage;
            ++numSuccessfulMonsterHits[index];
            averageMonsterDamage[index] = totalMonsterDamage[index] / numSuccessfulMonsterHits[index];
        }

        void Proceed(Action action, bool important = false)
            => game.AddTimedEvent(TimeSpan.FromMilliseconds(NeedsClickForNextAction ? 200 : (important ? 2750 : 850) + (100 - Speed) * 5), action);

        void RunBattleAction(BattleAction battleAction, uint battleTicks)
        {
            game.CursorType = CursorType.Sword;

            void ActionFinished(bool needClickAfterwards = true)
            {
                ActionCompleted?.Invoke(battleAction);
                if (NeedsClickForNextAction && needClickAfterwards)
                    WaitForClick = true;
                ReadyForNextAction = true;
            }

            void DefenderEquipBreak(AttackActionFlags flag, EquipmentSlot equipmentSlot)
            {
                GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out _, out var flags);
                var target = GetCharacterAt((int)tile);
                var nextAction = roundBattleActions.Peek();
                nextAction.ActionParameter = battleAction.ActionParameter;
                if (flags.HasFlag(flag))
                {
                    var textColor = target.Type == CharacterType.PartyMember ? TextColor.BattlePlayer : TextColor.BattleMonster;
                    var equipSlot = target.Equipment.Slots[equipmentSlot];
                    var itemIndex = equipSlot.ItemIndex;
                    var item = game.ItemManager.GetItem(itemIndex);
                    if (item.NumberOfHands == 2)
                        target.Equipment.Slots[EquipmentSlot.LeftHand]?.Clear();
                    game.EquipmentRemoved(target, itemIndex, 1, equipSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
                    brokenItems.Add(KeyValuePair.Create(itemIndex, equipSlot.Flags));
                    equipSlot.Clear();
                    layout.SetBattleMessage(target.Name + string.Format(game.DataNameProvider.BattleMessageWasBroken, item.Name), textColor);
                    Proceed(() => ActionFinished(true), true);
                }
                else
                {
                    ActionFinished(false);
                }
            }

            void SkipAllFollowingAttacks(Character character = null)
            {
                character ??= battleAction.Character;
                bool foundNextDisplayAction = false;
                foreach (var action in roundBattleActions.Where(a => a.Character == character))
                {
                    if (!foundNextDisplayAction)
                    {
                        if (action.Action == BattleActionType.DisplayActionText)
                        {
                            foundNextDisplayAction = true;
                            action.Skip = true;
                        }
                    }
                    else
                    {
                        action.Skip = true;
                    }
                }
            }

            bool CheckAmmo(Item weapon, uint ammoIndex)
            {
                // Check for last ammunition consumption
                if (weapon?.Type == ItemType.LongRangeWeapon)
                {
                    void LastAmmoUsed()
                    {
                        var followAction = roundBattleActions.Peek();
                        followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.LastAmmo);
                        SkipAllFollowingAttacks();
                        if (battleAction.Character is PartyMember partyMember)
                            PlayerLastAmmoUsed?.Invoke(partyMember);
                    }
                    var attacker = battleAction.Character;
                    if (weapon.UsedAmmunitionType != AmmunitionType.None)
                    {
                        var slot = attacker.Inventory.Slots.FirstOrDefault(slot => slot.ItemIndex == ammoIndex && slot.Amount > 0);

                        if (slot != null)
                        {
                            slot.Remove(1);

                            if (attacker is PartyMember partyMember)
                                game.InventoryItemRemoved(ammoIndex, 1, partyMember);

                            if (slot.Amount == 0)
                            {
                                // Do we have more in inventory?
                                if (!attacker.Inventory.Slots.Any(slot => slot.ItemIndex == ammoIndex && slot.Amount > 0))
                                {
                                    var ammoSlot = attacker.Equipment.Slots[EquipmentSlot.LeftHand];

                                    if (ammoSlot.ItemIndex != ammoIndex || ammoSlot.Amount <= 0)
                                    {
                                        // Monsters might only have the ammo in inventory
                                        LastAmmoUsed();
                                        return false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var ammoSlot = attacker.Equipment.Slots[EquipmentSlot.LeftHand];

                            if (ammoSlot.ItemIndex != ammoIndex || ammoSlot.Amount <= 0) // This should not happen!
                                throw new AmbermoonException(ExceptionScope.Application, "Character used long ranged weapon without needed ammo.");

                            ammoSlot.Remove(1);
                            game.EquipmentRemoved(attacker, ammoIndex, 1, false);

                            if (ammoSlot.Amount == 0)
                            {
                                LastAmmoUsed();
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            // If hurried and attacking, enable twice as much attack actions.
            if (battleAction.Character is PartyMember player &&
                hurriedPlayers.Contains(player))
            {
                if (roundBattleActions.Any(a => a.Action == BattleActionType.Attack && a.Character == player))
                {
                    hurriedPlayers.Remove(player);

                    foreach (var action in roundBattleActions.Where(a => a.Character == player))
                        action.Skip = false;
                }
            }

            switch (battleAction.Action)
            {
                case BattleActionType.DropWeapon:
                {
                    // Note: This only displays the message. The PickMonsterAction method will drop/switch ranged weapons automatically.
                    layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageHasDroppedWeapon, TextColor.BrightGray);
                    Proceed(() => ActionFinished());
                    return;
                }
                case BattleActionType.DisplayActionText:
                {
                    var next = roundBattleActions.Peek();
                    string text;

                    switch (next.Action)
                    {
                        case BattleActionType.Move:
                        {
                            uint currentRow = (uint)GetCharacterPosition(next.Character) / 6;
                            uint newRow = next.ActionParameter / 6;
                            bool retreat = battleAction.Character.Type == CharacterType.Monster && newRow < currentRow;
                            text = next.Character.Name + (retreat ? game.DataNameProvider.BattleMessageRetreats : game.DataNameProvider.BattleMessageMoves);
                            break;
                        }
                        case BattleActionType.Flee:
                            text = next.Character.Name + game.DataNameProvider.BattleMessageFlees;
                            break;
                        case BattleActionType.Attack:
                        {
                            GetAttackInformation(next.ActionParameter, out uint targetTile, out uint weaponIndex, out uint ammoIndex);
                            var weapon = weaponIndex == 0 ? null : game.ItemManager.GetItem(weaponIndex);
                            var target = battleField[targetTile];

                            if (target == null)
                            {
                                text = next.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget;
                                roundBattleActions.Dequeue(); // Remove the attack action
                                if (CheckAmmo(weapon, ammoIndex))
                                {
                                    foreach (var action in roundBattleActions.Where(a => a.Character == next.Character))
                                        action.Skip = true;
                                }
                                if (next.Character is PartyMember partyMember)
                                    PlayerLostTarget?.Invoke(partyMember);
                            }
                            else if (weapon == null)
                                text = next.Character.Name + string.Format(game.DataNameProvider.BattleMessageAttacks, target.Name);
                            else
                                text = next.Character.Name + string.Format(game.DataNameProvider.BattleMessageAttacksWith, target.Name, weapon.Name);
                            break;
                        }
                        case BattleActionType.CastSpell:
                        {
                            GetCastSpellInformation(next.ActionParameter, out _, out Spell spell, out var itemSlotIndex, out bool equippedItem);
                            string spellName = game.DataNameProvider.GetSpellName(spell);

                            if (itemSlotIndex != null)
                            {
                                var itemSlot = equippedItem
                                    ? battleAction.Character.Equipment.Slots[(EquipmentSlot)(itemSlotIndex.Value + 1)]
                                    : battleAction.Character.Inventory.Slots[itemSlotIndex.Value];
                                var item = game.ItemManager.GetItem(itemSlot.ItemIndex);
                                text = next.Character.Name + string.Format(game.DataNameProvider.BattleMessageCastsSpellFrom, spellName, item.Name);
                            }
                            else
                                text = next.Character.Name + string.Format(game.DataNameProvider.BattleMessageCastsSpell, spellName);
                            break;
                        }
                        case BattleActionType.MoveGroupForward:
                            text = next.Character.Type == CharacterType.Monster
                                ? game.DataNameProvider.BattleMessageMonstersAdvance
                                : game.DataNameProvider.BattleMessagePartyAdvances;
                            break;
                        default:
                            text = null;
                            break;
                    }
                    layout.SetBattleMessage(text, next.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                    Proceed(() => ActionFinished());
                    return;
                }
                case BattleActionType.Move:
                {
                    layout.SetBattleMessage(null);

                    void EndMove()
                    {
                        MoveCharacterTo(battleAction.ActionParameter, battleAction.Character);
                        ActionCompleted?.Invoke(battleAction);
                        ReadyForNextAction = true;
                    }

                    bool moveFailed = false;
                    if (!battleAction.Character.CanMove())
                    {
                        // TODO: is this right or is the action just skipped?
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageCannotMove,
                            battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                        moveFailed = true;
                    }
                    else if (battleField[battleAction.ActionParameter & 0x1f] != null)
                    {
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageWayWasBlocked,
                            battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                        moveFailed = true;
                    }

                    if (moveFailed)
                    {
                        Proceed(() => ActionFinished());
                        return;
                    }

                    int currentPosition = GetCharacterPosition(battleAction.Character);

                    HideBattleFieldDamage(currentPosition);

                    if (battleAction.Character is Monster monster)
                    {
                        int currentColumn = currentPosition % 6;
                        int currentRow = currentPosition / 6;
                        uint newPosition = GetTargetTileOrRowFromParameter(battleAction.ActionParameter);
                        int newColumn = (int)newPosition % 6;
                        int newRow = (int)newPosition / 6;
                        bool retreat = newRow < currentRow;
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void MoveAnimationFinished()
                        {
                            animation.AnimationFinished -= MoveAnimationFinished;
                            SetMonsterDisplayLayer(animation, monster, (int)newPosition);
                            EndMove();
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                        }

                        var newDisplayPosition = layout.GetMonsterCombatCenterPosition((int)battleAction.ActionParameter % 6, (int)newRow, monster);
                        animation.AnimationFinished += MoveAnimationFinished;
                        var frames = monster.GetAnimationFrameIndices(MonsterAnimationType.Move);
                        animation.Play(frames, (uint)Math.Max(Math.Abs(newRow - currentRow), Math.Abs(newColumn - currentColumn)) * Game.TicksPerSecond / (2 * (uint)frames.Length),
                            battleTicks, newDisplayPosition, layout.RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)newRow));
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;
                    }
                    else
                    {
                        Proceed(EndMove);
                    }
                    return;
                }
                case BattleActionType.MoveGroupForward:
                    // No parameter
                    layout.SetBattleMessage(null);
                    Proceed(() =>
                    {
                        foreach (var player in PartyMembers.Where(p => p.Alive && !fledCharacters.Contains(p)))
                        {
                            int currentPosition = GetCharacterPosition(player);

                            HideBattleFieldDamage(currentPosition);

                            MoveCharacterTo((uint)(currentPosition - 6), player);
                        }

                        ActionCompleted?.Invoke(battleAction);
                        ReadyForNextAction = true;
                    });
                    return;
                case BattleActionType.Attack:
                {
                    GetAttackInformation(battleAction.ActionParameter, out uint targetTile, out uint weaponIndex, out uint ammoIndex);
                    var target = GetCharacterAt((int)targetTile);
                    if (target == null)
                    {
                        ActionFinished(false);
                        return;
                    }
                    var attackResult = ProcessAttack(battleAction.Character, (int)targetTile, out int damage, out bool abort);
                    var textColor = battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer;
                    if (abort)
                    {
                        foreach (var action in roundBattleActions.Where(a => a.Character == battleAction.Character))
                            action.Skip = true;
                    }
                    if (attackResult == AttackResult.Missed && battleAction.Character is PartyMember attackingPlayer)
                        PlayerLostTarget?.Invoke(attackingPlayer);
                    var followAction = roundBattleActions.Peek();
                    followAction.ActionParameter = UpdateHurtParameter(followAction.ActionParameter, (uint)damage, attackResult);
                    Item weapon = weaponIndex == 0 ? null : game.ItemManager.GetItem(weaponIndex);
                    Item ammo = ammoIndex == 0 ? null : game.ItemManager.GetItem(ammoIndex);
                    if (attackResult == AttackResult.Petrified)
                    {
                        if (target.Type == CharacterType.Monster)
                        {
                            layout.SetBattleMessage(game.DataNameProvider.BattleMessageCannotDamagePetrifiedMonsters, textColor);
                            Proceed(() => ActionFinished(true), true);
                        }
                        else
                        {
                            layout.SetBattleMessage(null);
                            ActionFinished(true);
                        }
                        return;
                    }
                    if (damage != 0)
                    {
                        uint trackDamage = attackResult == AttackResult.CriticalHit ? target.HitPoints.TotalMaxValue : (uint)damage;
                        // Update damage statistics
                        if (battleAction.Character is PartyMember partyMember) // Memorize last damage for players
                            TrackPlayerHit(partyMember, trackDamage);
                        else if (battleAction.Character is Monster attackingMonster) // Memorize monster damage stats
                            TrackMonsterHit(attackingMonster, trackDamage);
                    }
                    CheckAmmo(weapon, ammoIndex);
                    // Check weapon or armor breakage
                    if (attackResult != AttackResult.Missed && attackResult != AttackResult.Failed)
                    {
                        int RollDice1000() => game.RandomInt(0, 999);

                        if (weapon != null && weapon.CanBreak && RollDice1000() < weapon.BreakChance)
                        {
                            followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakWeapon);
                            SkipAllFollowingAttacks();
                        }

                        if (attackResult == AttackResult.Blocked)
                        {
                            // When parried the defenders weapon and shield can break instead of the armor
                            var enemyWeaponIndex = target.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex;

                            if (enemyWeaponIndex != 0)
                            {
                                var enemyWeapon = game.ItemManager.GetItem(enemyWeaponIndex);

                                if (enemyWeapon.CanBreak && RollDice1000() < enemyWeapon.BreakChance)
                                {
                                    followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakDefenderWeapon);

                                    if (target is PartyMember)
                                    {
                                        // If the weapon of a party member breaks through parrying
                                        // he should no longer be able to attack.
                                        SkipAllFollowingAttacks(target);
                                    }
                                }
                            }

                            var enemyShieldIndex = target.Equipment.Slots[EquipmentSlot.LeftHand].ItemIndex;

                            if (enemyShieldIndex != 0 && enemyShieldIndex != enemyWeaponIndex)
                            {
                                var enemyShield = game.ItemManager.GetItem(enemyShieldIndex);

                                if (enemyShield.Type == ItemType.Shield && enemyShield.CanBreak && RollDice1000() < enemyShield.BreakChance)
                                    followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakDefenderShield);
                            }
                        }
                        else
                        {
                            var enemyArmorIndex = target.Equipment.Slots[EquipmentSlot.Body].ItemIndex;

                            if (enemyArmorIndex != 0)
                            {
                                var enemyArmor = game.ItemManager.GetItem(enemyArmorIndex);

                                if (enemyArmor.CanBreak && RollDice1000() < enemyArmor.BreakChance)
                                    followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakArmor);
                            }
                        }
                    }
                    void ShowAttackMessage()
                    {
                        switch (attackResult)
                        {
                            case AttackResult.Failed:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageAttackFailed, textColor);
                                break;
                            case AttackResult.NoDamage:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageAttackDidNoDamage, textColor);
                                break;
                            case AttackResult.Missed:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget, textColor);
                                break;
                            case AttackResult.Blocked:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageAttackWasParried, textColor);
                                break;
                            case AttackResult.Protected:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageCannotPenetrateMagicalAura, textColor);
                                break;
                            case AttackResult.CriticalHit:
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMadeCriticalHit, textColor);
                                break;
                            case AttackResult.Damage:
                                layout.SetBattleMessage(battleAction.Character.Name + string.Format(game.DataNameProvider.BattleMessageDidPointsOfDamage, damage), textColor);
                                break;
                        }
                        Proceed(() => ActionFinished(true));
                    }
                    if (battleAction.Character is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void AttackAnimationFinished()
                        {
                            animation.AnimationFinished -= AttackAnimationFinished;
                            if (weapon == null || weapon.Type != ItemType.LongRangeWeapon) // in this case the ammunition effect calls it
                                ShowAttackMessage();
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                        }

                        if (weapon?.Type == ItemType.LongRangeWeapon)
                        {
                            PlayBattleEffectAnimation(weapon.UsedAmmunitionType switch
                            {
                                AmmunitionType.None => BattleEffect.SickleAttack,
                                AmmunitionType.Slingstone => BattleEffect.SlingstoneAttack,
                                AmmunitionType.Arrow => BattleEffect.MonsterArrowAttack,
                                AmmunitionType.Bolt => BattleEffect.MonsterBoltAttack,
                                _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid ammunition type for monster.")
                            }, (uint)GetCharacterPosition(battleAction.Character), targetTile, battleTicks, ShowAttackMessage);
                        }

                        animation.AnimationFinished += AttackAnimationFinished;
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.CloseRangedAttack), Game.TicksPerSecond / 6,
                            battleTicks);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;
                    }
                    else
                    {
                        if (weapon?.Type == ItemType.LongRangeWeapon)
                        {
                            PlayBattleEffectAnimation(weapon.UsedAmmunitionType switch
                            {
                                AmmunitionType.None => BattleEffect.SickleAttack,
                                AmmunitionType.Slingstone => BattleEffect.SlingstoneAttack,
                                AmmunitionType.Arrow => BattleEffect.PlayerArrowAttack,
                                AmmunitionType.Bolt => BattleEffect.PlayerBoltAttack,
                                AmmunitionType.Slingdagger => BattleEffect.SlingdaggerAttack,
                                _ => throw new AmbermoonException(ExceptionScope.Application, "Invalid ammunition type for player.")
                            }, (uint)GetCharacterPosition(battleAction.Character), targetTile, battleTicks, ShowAttackMessage);
                        }
                        else
                        {
                            PlayBattleEffectAnimation(BattleEffect.PlayerAttack, targetTile, battleTicks, ShowAttackMessage);
                        }
                    }
                    return;
                }
                case BattleActionType.CastSpell:
                {
                    layout.SetBattleMessage(null);
                    GetCastSpellInformation(battleAction.ActionParameter, out uint targetRowOrTile, out Spell spell,
                        out var itemSlotIndex, out bool equippedItem);

                    // Note: Support spells like healing can also miss. In this case no message is displayed but the SP is spent.

                    var spellInfo = SpellInfos.Entries[spell];

                    if (battleAction.Character is PartyMember partyMember)
                        game.CurrentCaster = partyMember;

                    if (itemSlotIndex == null)
                    {
                        battleAction.Character.SpellPoints.CurrentValue = (uint)Math.Max(0, (int)battleAction.Character.SpellPoints.CurrentValue - (int)SpellInfos.GetSPCost(game.Features, spell, battleAction.Character));

                        if (battleAction.Character is PartyMember castingPartyMember)
                            layout.FillCharacterBars(castingPartyMember);

                        if (!CheckSpellCast(battleAction.Character, spellInfo))
                        {
                            EndCast(true);
                            return;
                        }
                    }

                    void EndCast(bool needClickAfterwards = false)
                    {
                        if (itemSlotIndex != null)
                        {
                            // Note: It will always be a party member as monsters can't use item spells.
                            var itemSlot = equippedItem
                                ? battleAction.Character.Equipment.Slots[(EquipmentSlot)(itemSlotIndex.Value + 1)]
                                : battleAction.Character.Inventory.Slots[itemSlotIndex.Value];
                            layout.ReduceItemCharge(itemSlot, false, equippedItem, battleAction.Character);
                        }

                        game.CurrentSpellTarget = null;

                        if (currentSpellAnimation != null)
                        {
                            currentSpellAnimation.PostCast(() =>
                            {
                                currentSpellAnimation?.Destroy();
                                currentSpellAnimation = null;
                                Proceed(() => ActionFinished(needClickAfterwards));
                            });
                        }
                        else
                        {
                            Proceed(() => ActionFinished(needClickAfterwards));
                        }
                    }

                    switch (spellInfo.Target)
                    {
                        case SpellTarget.SingleEnemy:
                            if (GetCharacterAt((int)targetRowOrTile) == null)
                            {
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                    battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                                Proceed(() => EndCast());
                                return;
                            }
                            break;
                        case SpellTarget.SingleFriend:
                            if (GetCharacterAt((int)targetRowOrTile) == null)
                            {
                                EndCast();
                                return;
                            }
                            break;
                        // Note: For row spells the initial animation is cast and in "spell move to" the miss message is displayed.
                    }

                    currentSpellAnimation = new SpellAnimation(game, layout, this, spell,
                        battleAction.Character.Type == CharacterType.Monster, GetCharacterPosition(battleAction.Character), (int)targetRowOrTile);

                    void CastSpellOn(Character target, Action finishAction)
                    {
                        if (target == null)
                        {
                            finishAction?.Invoke();
                            return;
                        }

                        if (target is PartyMember targetPlayer &&
                            (spell == Spell.Hurry || spell == Spell.MassHurry) &&
                            !hurriedPlayers.Contains(targetPlayer))
                            hurriedPlayers.Add(targetPlayer);

                        game.CurrentSpellTarget = target;
                        int position = GetCharacterPosition(target);
                        bool failed = false;
                        bool spellBlocked = false;
                        bool earlyCheckSpell = spell == Spell.DispellUndead || spell == Spell.DestroyUndead ||
                            spell == Spell.HolyWord || spell == Spell.DissolveVictim;
                        if (earlyCheckSpell)
                        {
                            // Check spell deflection first so the spell animation won't
                            // shrink/remove the monster sprite.
                            if (!CheckSpell(battleAction.Character, target, spell, _ =>
                                finishAction?.Invoke(), true))
                                return;
                        }
                        // Note: Some spells like Fireball or Whirlwind move to the target.
                        currentSpellAnimation.MoveTo(position, (ticks, playHurt, finish) =>
                        {
                            if (finish && spellBlocked)
                            {
                                ShowSpellFailMessage(battleAction.Character, spellInfo, target.Name + game.DataNameProvider.BattleMessageDeflectedSpell, NeedsClickForNextAction ? finishAction : null);
                                PlayBattleEffectAnimation(BattleEffect.BlockSpell, (uint)GetSlotFromCharacter(target), game.CurrentBattleTicks, NeedsClickForNextAction ? null : finishAction);
                                return;
                            }
                            else if (playHurt || finish)
                            {
                                if (failed)
                                {
                                    if (finish)
                                        finishAction?.Invoke();
                                    return;
                                }
                                else if (!earlyCheckSpell && !CheckSpell(battleAction.Character, target, spell, blocked =>
                                {
                                    if (finish)
                                        finishAction?.Invoke();
                                    else if (blocked)
                                        spellBlocked = true;
                                }, finish))
                                {
                                    failed = true;
                                    // Note: The finishAction is called automatically if CheckSpell returns false.
                                    // But it might be called a bit later (e.g. after block animation) so we won't
                                    // invoke it here ourself.
                                    return;
                                }
                            }

                            if (playHurt && target is Monster monster) // This is only for the hurt monster animation
                            {
                                var animation = layout.GetMonsterBattleAnimation(monster);

                                void HurtAnimationFinished()
                                {
                                    animation.AnimationFinished -= HurtAnimationFinished;
                                    currentBattleAnimation = null;
                                    currentlyAnimatedMonster = null;
                                }

                                void EffectApplied()
                                {
                                    // We have to wait until the monster hurt animation finishes.
                                    // Otherwise the animation reset might not happen.
                                    if (currentBattleAnimation == null && currentlyAnimatedMonster == null)
                                        finishAction?.Invoke();
                                    else
                                        game.AddTimedEvent(TimeSpan.FromMilliseconds(25), EffectApplied);
                                }

                                animation.AnimationFinished += HurtAnimationFinished;
                                animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt), Game.TicksPerSecond / 5,
                                    game.CurrentBattleTicks);
                                currentBattleAnimation = animation;
                                currentlyAnimatedMonster = monster;
                                if (finish)
                                {
                                    ApplySpellEffect(battleAction.Character, target, spell, game.CurrentBattleTicks, EffectApplied);
                                }
                            }
                            else if (finish)
                            {
                                ApplySpellEffect(battleAction.Character, target, spell, game.CurrentBattleTicks, finishAction);
                            }
                        });
                    }

                    void CastSpellOnRow(CharacterType characterType, int row, Action finishAction)
                    {
                        var targets = Enumerable.Range(0, 6).Select(column => battleField[5 - column + row * 6])
                            .Where(c => c?.Type == characterType).ToList();

                        if (targets.Count == 0)
                        {
                            finishAction?.Invoke();
                            return;
                        }

                        void Cast(int index)
                        {
                            CastSpellOn(targets[index], () =>
                            {
                                if (index == targets.Count - 1)
                                    finishAction?.Invoke();
                                else
                                    Cast(index + 1);
                            });
                        }

                        Cast(0);
                    }

                    void CastSpellOnAll(CharacterType characterType, Action finishAction)
                    {
                        int minRow = characterType == CharacterType.Monster ? 0 : 3;
                        int maxRow = characterType == CharacterType.Monster ? 3 : 4;

                        void Cast(int row)
                        {
                            CastSpellOnRow(characterType, row, () =>
                            {
                                if (row == minRow)
                                    finishAction?.Invoke();
                                else
                                    Cast(row - 1);
                            });
                        }

                        Cast(maxRow);
                    }

                    if (battleAction.Character is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void CastAnimationFinished()
                        {
                            animation.AnimationFinished -= CastAnimationFinished;
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                            StartCasting();
                        }

                        animation.AnimationFinished += CastAnimationFinished;
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Cast), Game.TicksPerSecond / 6,
                            battleTicks);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;
                    }
                    else
                    {
                        StartCasting();
                    }

                    void StartCasting()
                    {
                        currentSpellAnimation.Play(() =>
                        {
                            switch (spellInfo.Target)
                            {
                                case SpellTarget.None:
                                    if (spell == Spell.SelfHealing || spell == Spell.SelfReviving)
                                        CastSpellOn(battleAction.Character, () => EndCast());
                                    else
                                        ApplySpellEffect(battleAction.Character, null, spell, game.CurrentBattleTicks, () => EndCast());
                                    break;
                                case SpellTarget.SingleEnemy:
                                case SpellTarget.SingleFriend:
                                    CastSpellOn(GetCharacterAt((int)targetRowOrTile), () => EndCast());
                                    break;
                                case SpellTarget.AllEnemies:
                                    CastSpellOnAll(battleAction.Character.Type == CharacterType.Monster
                                        ? CharacterType.PartyMember : CharacterType.Monster, () => EndCast());
                                    break;
                                case SpellTarget.AllFriends:
                                    CastSpellOnAll(battleAction.Character.Type, () => EndCast());
                                    break;
                                case SpellTarget.EnemyRow:
                                {
                                    var enemyType = battleAction.Character.Type == CharacterType.Monster ?
                                        CharacterType.PartyMember : CharacterType.Monster;
                                    if (!Enumerable.Range((int)targetRowOrTile * 6, 6).Any(p => GetCharacterAt(p)?.Type == enemyType))
                                    {
                                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                            battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                                        Proceed(() => EndCast());
                                        return;
                                    }
                                    CastSpellOnRow(enemyType, (int)targetRowOrTile, () => EndCast());
                                    break;
                                }
                                case SpellTarget.FriendRow:
                                {
                                    if (!Enumerable.Range((int)targetRowOrTile * 6, 6).Any(p => GetCharacterAt(p)?.Type == battleAction.Character.Type))
                                    {
                                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                            battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                                        Proceed(() => EndCast());
                                        return;
                                    }
                                    CastSpellOnRow(battleAction.Character.Type, (int)targetRowOrTile, () => EndCast());
                                    break;
                                }
                                case SpellTarget.BattleField:
                                {
                                    var character = GetCharacterAt((int)GetBlinkCharacterPosition(battleAction.ActionParameter));
                                    if (character == null)
                                    {
                                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                            battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                                        Proceed(() => EndCast());
                                        return;
                                    }
                                    ApplySpellEffect(battleAction.Character, character, spell, game.CurrentBattleTicks, () => EndCast(),
                                        targetRowOrTile);
                                    break;
                                }
                            }
                        });
                    }
                    return;
                }
                case BattleActionType.Flee:
                {
                    layout.SetBattleMessage(null);
                    void EndFlee()
                    {
                        fledCharacters.Add(battleAction.Character);
                        RemoveCharacterFromBattleField(battleAction.Character);
                        ActionCompleted?.Invoke(battleAction);

                        if (battleAction.Character.Type == CharacterType.Monster &&
                            Monsters.Count() == 0)
                        {
                            EndBattleCleanup();
                            BattleEnded?.Invoke(new Game.BattleEndInfo
                            {
                                MonstersDefeated = true,
                                KilledMonsters = initialMonsters.Where(m => !fledCharacters.Contains(m)).ToList(),
                                FledPartyMembers = fledCharacters.Where(c => c?.Type == CharacterType.PartyMember).Cast<PartyMember>().ToList(),
                                TotalExperience = initialMonsters.Sum(m => m.DefeatExperience),
                                BrokenItems = brokenItems
                            });
                            return;
                        }
                        else if (battleAction.Character.Type == CharacterType.PartyMember &&
                            !battleField.Any(c => c?.Type == CharacterType.PartyMember))
                        {
                            EndBattleCleanup();
                            BattleEnded?.Invoke(new Game.BattleEndInfo
                            {
                                MonstersDefeated = false
                            });
                            return;
                        }
                        ReadyForNextAction = true;
                    }
                    if (battleAction.Character is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void MoveAnimationFinished()
                        {
                            animation.AnimationFinished -= MoveAnimationFinished;
                            EndFlee();
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                        }

                        animation.AnimationFinished += MoveAnimationFinished;
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Move), Game.TicksPerSecond / 20,
                            battleTicks, new Position(160, 105), 0.0f);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;
                    }
                    else
                    {
                        EndFlee();
                    }
                    return;
                }
                case BattleActionType.WeaponBreak:
                {
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out _, out var flags);
                    var nextAction = roundBattleActions.Peek();
                    nextAction.ActionParameter = battleAction.ActionParameter;
                    if (flags.HasFlag(AttackActionFlags.BreakWeapon))
                    {
                        var textColor = battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer;
                        var weaponSlot = battleAction.Character.Equipment.Slots[EquipmentSlot.RightHand];
                        var itemIndex = weaponSlot.ItemIndex;
                        var weapon = game.ItemManager.GetItem(itemIndex);
                        if (weapon.NumberOfHands == 2)
                        {
                            // Remove the cross from left hand slot
                            battleAction.Character.Equipment.Slots[EquipmentSlot.LeftHand]?.Clear();
                        }
                        game.EquipmentRemoved(battleAction.Character, itemIndex, 1, weaponSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
                        brokenItems.Add(KeyValuePair.Create(itemIndex, weaponSlot.Flags));
                        weaponSlot.Clear();
                        layout.SetBattleMessage(battleAction.Character.Name + string.Format(game.DataNameProvider.BattleMessageWasBroken, weapon.Name), textColor);
                        if (battleAction.Character is PartyMember partyMember)
                        {
                            PlayerWeaponBroke?.Invoke(partyMember);
                        }
                        else if (battleAction.Character is Monster monster)
                        {
                            if (weapon.Type == ItemType.LongRangeWeapon)
                            {
                                // Switch to melee weapon if available
                                bool IsMeleeWeapon(uint itemIndex) => game.ItemManager.GetItem(itemIndex).Type == ItemType.CloseRangeWeapon;
                                var meleeWeaponSlot = battleAction.Character.Inventory.Slots.FirstOrDefault(s => !s.Empty && IsMeleeWeapon(s.ItemIndex));
                                if (meleeWeaponSlot != null)
                                {
                                    weaponSlot.Exchange(meleeWeaponSlot);
                                    game.EquipmentAdded(weaponSlot.ItemIndex, 1, monster);
                                }
                            }

                            if (monster.BaseAttackDamage + monster.BonusAttackDamage == 0)
                                monsterMorale[initialMonsters.IndexOf(monster)] /= 2;
                        }
                        Proceed(() => ActionFinished(true), true);
                    }
                    else
                    {
                        ActionFinished(false);
                    }
                    return;
                }
                case BattleActionType.ArmorBreak:
                {
                    DefenderEquipBreak(AttackActionFlags.BreakArmor, EquipmentSlot.Body);
                    return;
                }
                case BattleActionType.DefenderWeaponBreak:
                {
                    DefenderEquipBreak(AttackActionFlags.BreakDefenderWeapon, EquipmentSlot.RightHand);
                    return;
                }
                case BattleActionType.DefenderShieldBreak:
                {
                    DefenderEquipBreak(AttackActionFlags.BreakDefenderShield, EquipmentSlot.LeftHand);
                    return;
                }
                case BattleActionType.LastAmmo:
                {
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out _, out var flags);
                    var nextAction = roundBattleActions.Peek();
                    nextAction.ActionParameter = battleAction.ActionParameter;
                    if (flags.HasFlag(AttackActionFlags.LastAmmo))
                    {
                        var textColor = battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer;
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageUsedLastAmmunition, textColor);
                        if (battleAction.Character is Monster monster)
                            droppedWeaponMonsters.Add(monster);
                        Proceed(() => ActionFinished(true), true);
                    }
                    else
                    {
                        ActionFinished(false);
                    }
                    return;
                }
                case BattleActionType.Hurt:
                {
                    layout.SetBattleMessage(null);
                    var textColor = battleAction.Character.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer;
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out var attackResult, out var flags);
                    var target = GetCharacterAt((int)tile);
                    if (target == null)
                    {
                        ActionFinished(false);
                        return;
                    }
                    if(attackResult != AttackResult.Damage && attackResult != AttackResult.CriticalHit)
                    {
                        ActionFinished(false);
                        return;
                    }
                    void EndHurt()
                    {
                        if (!target.Alive)
                        {
                            HandleCharacterDeath(battleAction.Character, target, () => ActionFinished(false));
                        }
                        else
                        {
                            if (target is PartyMember partyMember)
                                layout.FillCharacterBars(partyMember);
                            ActionFinished(false);
                        }
                    }

                    if (target is PartyMember partyMember)
                    {
                        PlayBattleEffectAnimation(BattleEffect.HurtPlayer, tile, battleTicks, EndHurt);
                        game.ShowPlayerDamage(game.SlotFromPartyMember(partyMember).Value, Math.Min(damage, partyMember.HitPoints.CurrentValue));
                    }
                    else if (target is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void HurtAnimationFinished()
                        {
                            animation.AnimationFinished -= HurtAnimationFinished;
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                            EndHurt();
                        }

                        animation.AnimationFinished += HurtAnimationFinished;
                        var frames = monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt);
                        animation.Play(frames, (Game.TicksPerSecond / 2) / (uint)frames.Length, battleTicks);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;

                        PlayBattleEffectAnimation(BattleEffect.HurtMonster, tile, battleTicks, null);

                        if (game.Godmode)
                            damage = target.HitPoints.CurrentValue;
                    }
                    ShowBattleFieldDamage((int)tile, damage);
                    if (target.Conditions.HasFlag(Condition.Sleep))
                        game.RemoveCondition(Condition.Sleep, target);
                    if (target is Monster)
                        target.Damage(damage);
                    else if (!game.Godmode)
                        target.Damage(damage, deathCondition => game.KillPartyMember(target as PartyMember, deathCondition));
                    return;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid battle action.");
            }

            throw new AmbermoonException(ExceptionScope.Application, "Not processed battle action.");
        }

        internal void HideAllBattleFieldDamage()
        {
            foreach (var battleFieldDamageText in battleFieldDamageTexts)
                battleFieldDamageText.Value?.Delete();

            battleFieldDamageTexts.Clear();
        }

        void HideBattleFieldDamage(int tile)
        {
            if (battleFieldDamageTexts.ContainsKey(tile))
            {
                battleFieldDamageTexts[tile]?.Delete();
                battleFieldDamageTexts.Remove(tile);
            }
        }

        void ShowBattleFieldDamage(int tile, uint damage)
        {
            var layer = layout.RenderView.GetLayer(Layer.SmallDigits);
            // Note: Don't use *** as the digit font has no such character.
            var text = layout.RenderView.TextProcessor.CreateText(damage >= 999 ? "999" : $"{damage:000}");
            var area = Global.BattleFieldSlotArea(tile).CreateModified(-5, 9, 12, 0);
            var damageText = layout.RenderView.RenderTextFactory.CreateDigits(
                game.UIPaletteIndex, layer, text, TextColor.Red, false, area, TextAlign.Center);
            var colors = TextColors.TextAnimationColors;
            int colorCycle = 0;
            int colorIndex = -1;
            const int numColorCycles = 3;

            if (battleFieldDamageTexts.ContainsKey(tile))
            {
                battleFieldDamageTexts[tile].Delete();
                battleFieldDamageTexts[tile] = damageText;
            }
            else
            {
                battleFieldDamageTexts.Add(tile, damageText);
            }

            game.AddTimedEvent(TimeSpan.FromMilliseconds(150), ChangeColor);

            void ChangeColor()
            {
                if (!battleFieldDamageTexts.ContainsKey(tile))
                    return; // Might be removed by moving/dying or battle end

                ++colorIndex;

                if (colorIndex == colors.Length)
                {
                    colorIndex = 0;

                    if (++colorCycle == numColorCycles)
                    {
                        battleFieldDamageTexts[tile].Delete();
                        battleFieldDamageTexts.Remove(tile);
                        return;
                    }
                }

                battleFieldDamageTexts[tile].TextColor = colors[colorIndex];
                game.AddTimedEvent(TimeSpan.FromMilliseconds(150), ChangeColor);
            }

            damageText.DisplayLayer = 255;
            damageText.Visible = true;
        }

        bool CheckSpellCast(Character caster, SpellInfo spellInfo)
        {
            long chance = caster.Skills[Skill.UseMagic].TotalCurrentValue;

            if (caster is Monster && foreseeMagic)
                chance -= 25;

			if (game.RollDice100() >= chance)
            {
                layout.SetBattleMessage(caster.Name + game.DataNameProvider.SpellFailed,
                    caster.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer);
                return false;
            }

            return true;
        }

        void ShowSpellFailMessage(Character caster, SpellInfo spellInfo, string message, Action finishAction)
        {
            var color = caster.Type == CharacterType.Monster ? TextColor.BattleMonster : TextColor.BattlePlayer;
            var delay = TimeSpan.FromMilliseconds(1100 - Speed * 4);

            if (NeedsClickForNextAction && !spellInfo.Target.TargetsMultipleEnemies())
            {
                game.SetBattleMessageWithClick(message, color, finishAction, delay);
            }
            else
            {
                game.AddTimedEvent(delay, () =>
                {
                    layout.SetBattleMessage(message, color);
                    Proceed(() =>
                    {
                        layout.SetBattleMessage(null);
                        finishAction?.Invoke();
                    });
                });
            }
        }

        internal bool CheckSpell(Character caster, Character target, Spell spell, Action<bool> failAction, bool playBlocked,
            bool showMessage = true, bool checkDeflection = true)
        {
            var spellInfo = SpellInfos.Entries[spell];
            void Fail() => failAction?.Invoke(false);
            void ShowFailMessage(string message, Action finishAction)
            {
                if (showMessage)
                    ShowSpellFailMessage(caster, spellInfo, message, finishAction);
                else
                    finishAction?.Invoke();
            }

            if (target.Type != caster.Type)
            {
                bool godmode = game.Godmode && caster is PartyMember;

                if (checkDeflection && !godmode)
                {
					uint antiMagicBuffValue = game.CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.AntiMagic);

                    if (protectedCharacters.Contains(target) || game.RollDice100() < (int)(target.Attributes[Attribute.AntiMagic].TotalCurrentValue + antiMagicBuffValue))
                    {
                        Action blockedAction = () => failAction?.Invoke(true);
                        // Blocked
                        if (playBlocked)
                        {
                            ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageDeflectedSpell, NeedsClickForNextAction ? blockedAction : null);
                            PlayBattleEffectAnimation(BattleEffect.BlockSpell, (uint)GetSlotFromCharacter(target), game.CurrentBattleTicks, NeedsClickForNextAction ? null : blockedAction);
                        }
                        else
                        {
                            blockedAction();
                        }
                        return false;
                    }
                }

                if (!godmode && (target.SpellTypeImmunity & (SpellTypeImmunity)spellInfo.SpellType) != 0)
                {
                    ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageImmuneToSpellType, Fail);
                    return false;
                }

                if (!godmode && target.IsImmuneToSpell(spell, out bool silent, game.Features.HasFlag(Features.Elements)))
                {
                    if (silent)
                        Fail();
                    else
                        ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageImmuneToSpell, Fail);
                    return false;
                }
            }

            if (target.Conditions.HasFlag(Condition.Petrified) && spell.FailsAgainstPetrifiedEnemy())
            {
                // Note: In original there is no message in this case but I think
                //       it's better to show the reason.
                ShowFailMessage(game.DataNameProvider.BattleMessageCannotDamagePetrifiedMonsters, Fail);
                return false;
            }

            return true;
        }

        internal void RemoveCondition(Condition condition, Character target)
        {
            // Healing spells or potions.
            // Sleep can be removed by attacking as well.
            target.Conditions &= ~condition;

            if (target is PartyMember partyMember)
            {
                game.UpdateBattleStatus(partyMember);
                layout.UpdateCharacterNameColors(game.CurrentSavegame.ActivePartyMemberSlot);
            }

            void SkipActions()
            {
                foreach (var action in roundBattleActions.Where(a => a.Character == target))
                    action.Skip = true;
            }

            // Avoid fleeing or acting crazy if the condition is removed
            if (condition == Condition.Panic || condition == Condition.Crazy)
                SkipActions();
        }

        void AddCondition(Condition condition, Character target)
        {
            if (target is PartyMember && game.Godmode)
                return;

            target.Conditions |= condition;

            if (target is PartyMember partyMember)
            {
                if (!condition.CanParry() && parryingPlayers.Contains(partyMember))
                    parryingPlayers.Remove(partyMember);

                game.ShowPlayerDamage(game.SlotFromPartyMember(partyMember).Value, 0);
                game.UpdateBattleStatus(partyMember);
                layout.UpdateCharacterNameColors(game.CurrentSavegame.ActivePartyMemberSlot);
            }

            void SkipActions()
            {
                foreach (var action in roundBattleActions.Where(a => a.Character == target))
                    action.Skip = true;
            }

            if (!condition.CanSelect()) // disabled
            {
                // Still allow fleeing under fear
                if (condition.HasFlag(Condition.Panic) && condition.CanFlee() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Flee))
                    return;
                // Still allow moving and attacking when crazy
                if (condition.HasFlag(Condition.Crazy) &&
                    (condition.CanMove() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Move)) ||
                    (condition.CanAttack() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Attack)))
                    return;

                SkipActions();
            }
            else
            {
                if (!condition.CanAttack() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Attack))
                {
                    SkipActions();
                }
                else if (!condition.CanCastSpell() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.CastSpell))
                {
                    SkipActions();
                }
                else if (!condition.CanMove() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Move))
                {
                    SkipActions();
                }
                else if (!condition.CanFlee() && roundBattleActions.Any(a => a.Character == target && a.Action == BattleActionType.Flee))
                {
                    SkipActions();
                }
            }
        }

        static float GetMonsterDeathScale(Monster monster)
        {
            // 59 is the normal frame height
            return Math.Max(monster.MappedFrameWidth, monster.MappedFrameHeight) / 59.0f;
        }

        void HandleCharacterDeath(Character attacker, Character target, Action finishAction)
        {
            // Remove all actions that are performed by the dead target
            // or by the attacker. Note that following targets of a multi-target
            // spell won't be skipped as the spell cast action is already running.
            foreach (var action in roundBattleActions.Where(a => a.Character == target || a.Character == attacker))
                action.Skip = true;

            if (target is Monster)
            {
                var battleFieldCopy = (Character[])battleField.Clone();
                int slot = GetSlotFromCharacter(target);
                if (currentBattleAnimation != null && target == currentlyAnimatedMonster)
                {
                    currentBattleAnimation?.Destroy();
                    currentBattleAnimation = null;
                    currentlyAnimatedMonster = null;
                }
                else
                {
                    layout.GetMonsterBattleAnimation(target as Monster)?.Destroy();
                }
                PlayBattleEffectAnimation(BattleEffect.Death, (uint)slot, game.CurrentBattleTicks, () =>
                {
                    RemoveCharacterFromBattleField(target);
                    finishAction?.Invoke();
                    KillMonster(attacker as PartyMember, target, slot);
                }, GetMonsterDeathScale(target as Monster), battleFieldCopy);
            }
            else
            {
                RemoveCharacterFromBattleField(target);
                finishAction?.Invoke();
                KillPlayer(target);
            }
        }


        private class ImitationBackupData
        {
			readonly byte attacksPerRound;
            readonly CharacterElement element;
            readonly uint currentHitPoints;
            readonly uint maxHitPoints;
            readonly uint currentSpellPoints;
            readonly uint maxSpellPoints;
			readonly short baseAttack;
			readonly short baseDefense;
			readonly short magicAttack;
            readonly short magicDefense;
            readonly uint[] currentAttributes = new uint[8];
            readonly uint[] maxAttributes = new uint[8];
            readonly uint[] currentSkills = new uint[4];
            readonly uint[] maxSkills = new uint[4];
            readonly uint learnedSpells;
            static readonly Skill[] SkillIndexMapping = new Skill[4] { Skill.Attack, Skill.Parry, Skill.CriticalHit, Skill.UseMagic };

            public MonsterGraphicIndex MonsterGraphicIndex { get; }

			public ImitationBackupData(PartyMember partyMember, MonsterGraphicIndex monsterGraphicIndex)
            {
                MonsterGraphicIndex = monsterGraphicIndex;
				attacksPerRound = partyMember.AttacksPerRound;
                element = partyMember.Element;
                currentHitPoints = partyMember.HitPoints.CurrentValue;
                maxHitPoints = partyMember.HitPoints.MaxValue;
                currentSpellPoints = partyMember.SpellPoints.CurrentValue;
                maxSpellPoints = partyMember.SpellPoints.MaxValue;
                baseAttack = partyMember.BaseAttackDamage;
                baseDefense = partyMember.BaseDefense;
                magicAttack = partyMember.MagicAttack;
                magicDefense = partyMember.MagicDefense;
                for (int i = 0; i < 8; ++i)
                {
                    var attribute = partyMember.Attributes[(Attribute)i];
                    maxAttributes[i] = attribute.MaxValue;
                    currentAttributes[i] = attribute.CurrentValue;
                }
                for (int i = 0; i < 4; ++i)
                {
                    var skill = partyMember.Skills[SkillIndexMapping[i]];
                    maxSkills[i] = skill.MaxValue;
                    currentSkills[i] = skill.CurrentValue;
                }
                learnedSpells = partyMember.LearnedMysticSpells;
            }

            public void ApplyToPartyMember(PartyMember partyMember)
            {
                partyMember.AttacksPerRound = attacksPerRound;
                partyMember.Element = element;
                partyMember.HitPoints.MaxValue = maxHitPoints;
				if (partyMember.HitPoints.CurrentValue > maxHitPoints)
					partyMember.HitPoints.CurrentValue = maxHitPoints;
				partyMember.SpellPoints.MaxValue = maxSpellPoints;
                if (partyMember.SpellPoints.MaxValue == 0)
                    partyMember.SpellPoints.CurrentValue = currentSpellPoints;
                else if (partyMember.SpellPoints.CurrentValue > maxSpellPoints)
                    partyMember.SpellPoints.CurrentValue = maxSpellPoints;
				for (int i = 0; i < 8; ++i)
                {
                    var attribute = partyMember.Attributes[(Attribute)i];
                    attribute.MaxValue = maxAttributes[i];
                    attribute.CurrentValue = currentAttributes[i];
                }
                for (int i = 0; i < 4; ++i)
                {
                    var skill = partyMember.Skills[SkillIndexMapping[i]];
                    skill.MaxValue = maxSkills[i];
                    skill.CurrentValue = currentSkills[i];
                }
                partyMember.LearnedHealingSpells = 0;
                partyMember.LearnedAlchemisticSpells = 0;
                partyMember.LearnedMysticSpells = learnedSpells;
                partyMember.LearnedDestructionSpells = 0;
                partyMember.SpellMastery = partyMember.Class == Class.Mystic ? SpellTypeMastery.Mystic | SpellTypeMastery.Mastered : SpellTypeMastery.Mystic;
                partyMember.BaseDefense = baseDefense;
                partyMember.BonusAttackDamage = baseDefense;
                partyMember.MagicAttack = magicAttack;
                partyMember.MagicDefense = magicDefense;
                partyMember.InventoryInaccessible = false;
            }
        }

        readonly Dictionary<uint, ImitationBackupData> imitationBackupData = new Dictionary<uint, ImitationBackupData>();

        void ImitateMonster(PartyMember caster, Monster monster, Action finishAction)
        {
            if ((caster.Class != Class.Mystic && caster.Class != Class.Ranger) || caster.Index > 13)
            {
                // This is for safety. For example it should be avoided that Kasimir or a thief can use
                // the spell from a scroll as the Amiga implementation only allows specific characters
                // which are Chris, Targor and Valdyn. In addition there should be no scroll for this
                // spell or it should not be usable by animals and thieves.
                finishAction?.Invoke();
                return;
            }    

            imitationBackupData.Add(caster.Index, new ImitationBackupData(caster, monster.CombatGraphicIndex));

            game.ReplacePartyMemberBattleFieldSprite(caster, monster.CombatGraphicIndex);

            caster.AttacksPerRound = monster.AttacksPerRound;
            caster.Element = monster.Element;
            caster.HitPoints.MaxValue = monster.HitPoints.MaxValue;
            caster.HitPoints.CurrentValue = monster.HitPoints.CurrentValue;
            caster.SpellPoints.MaxValue = monster.SpellPoints.MaxValue;
            caster.SpellPoints.CurrentValue = monster.SpellPoints.CurrentValue;
            caster.MagicAttack = monster.MagicAttack;
            caster.MagicDefense = monster.MagicDefense;
            foreach (var attribute in EnumHelper.GetValues<Attribute>().Take(8))
            {
                caster.Attributes[attribute].MaxValue = monster.Attributes[attribute].MaxValue;
                caster.Attributes[attribute].CurrentValue = monster.Attributes[attribute].CurrentValue;
            }
            caster.Skills[Skill.Attack].MaxValue = monster.Skills[Skill.Attack].MaxValue;
            caster.Skills[Skill.Attack].CurrentValue = monster.Skills[Skill.Attack].CurrentValue;
            caster.Skills[Skill.Parry].MaxValue = monster.Skills[Skill.Parry].MaxValue;
            caster.Skills[Skill.Parry].CurrentValue = monster.Skills[Skill.Parry].CurrentValue;
            caster.Skills[Skill.CriticalHit].MaxValue = monster.Skills[Skill.CriticalHit].MaxValue;
            caster.Skills[Skill.CriticalHit].CurrentValue = monster.Skills[Skill.CriticalHit].CurrentValue;
            caster.Skills[Skill.UseMagic].MaxValue = monster.Skills[Skill.UseMagic].MaxValue;
            caster.Skills[Skill.UseMagic].CurrentValue = monster.Skills[Skill.UseMagic].CurrentValue;
            caster.LearnedHealingSpells = monster.LearnedHealingSpells;
            caster.LearnedAlchemisticSpells = monster.LearnedAlchemisticSpells;
            caster.LearnedMysticSpells = monster.LearnedMysticSpells;
            caster.LearnedDestructionSpells = monster.LearnedDestructionSpells;
            caster.SpellMastery = monster.SpellMastery;
            caster.BaseDefense = monster.BaseDefense;
            caster.BaseAttackDamage = monster.BaseAttackDamage;
            caster.InventoryInaccessible = true;

            finishAction?.Invoke();
        }

        void RestoreImitatingPartyMembers()
        {
            foreach (var partyMember in game.PartyMembers)
            {
                if (imitationBackupData.TryGetValue(partyMember.Index, out var backup))
                {
                    backup.ApplyToPartyMember(partyMember);
                }
            }

            imitationBackupData.Clear();
        }

        void ShowMonsterInfo(Monster monster, Action finishAction)
        {
            game.StartSequence();
            var area = new Rect(64, 38, 12 * 16, 10 * 16);
            var popup = layout.OpenPopup(area.Position, 12, 10, true, true, 225);
            area = area.CreateShrinked(16);
            int panelWidth = 12 * Global.GlyphWidth;
            // Attributes
            popup.AddText(new Rect(area.Position, new Size(panelWidth, Global.GlyphLineHeight)),
                game.DataNameProvider.AttributesHeaderString, TextColor.MonsterInfoHeader, TextAlign.Center);
            var position = new Position(area.Position.X, area.Position.Y + Global.GlyphLineHeight + 1);
            foreach (var attribute in EnumHelper.GetValues<Attribute>())
            {
                if (attribute == Attribute.Age)
                    break;

                var attributeValues = monster.Attributes[attribute];
                popup.AddText(position,
                    $"{game.DataNameProvider.GetAttributeShortName(attribute)}  {((attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}")}",
                    TextColor.BrightGray);
                position.Y += Global.GlyphLineHeight;
            }
            // Skills
            position = area.Position + new Position(panelWidth + Global.GlyphWidth, 0);
            popup.AddText(new Rect(position, new Size(panelWidth, Global.GlyphLineHeight)),
                game.DataNameProvider.SkillsHeaderString, TextColor.MonsterInfoHeader, TextAlign.Center);
            position.Y += Global.GlyphLineHeight + 1;
            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                var skillValues = monster.Skills[skill];
                popup.AddText(position,
                    $"{game.DataNameProvider.GetSkillShortName(skill)}  {((skillValues.TotalCurrentValue > 99 ? "**" : $"{skillValues.TotalCurrentValue:00}") + $"%/{skillValues.MaxValue:00}%")}",
                    TextColor.BrightGray);
                position.Y += Global.GlyphLineHeight - 1;
            }
            // Data
            position.X = area.X;
            position.Y += 3;
            popup.AddText(new Rect(position, new Size(area.Width, Global.GlyphLineHeight)),
                game.DataNameProvider.DataHeaderString, TextColor.MonsterInfoHeader, TextAlign.Center);
            position.Y += Global.GlyphLineHeight + 1;
            popup.AddText(position,
                string.Format(game.DataNameProvider.CharacterInfoHitPointsString, monster.HitPoints.CurrentValue > 999 ? "***" : monster.HitPoints.CurrentValue.ToString(),
                monster.HitPoints.TotalMaxValue > 999 ? "***" : monster.HitPoints.TotalMaxValue.ToString()) + " " +
                string.Format(game.DataNameProvider.CharacterInfoSpellPointsString, monster.SpellPoints.CurrentValue > 999 ? "***" : monster.SpellPoints.CurrentValue.ToString(),
                monster.SpellPoints.TotalMaxValue > 999 ? "***" : monster.SpellPoints.TotalMaxValue.ToString()),
                TextColor.BrightGray);
            position.Y += Global.GlyphLineHeight;
            popup.AddText(position,
                string.Format(game.DataNameProvider.CharacterInfoGoldAndFoodString.Replace(" ", "      "), monster.Gold, monster.Food),
                TextColor.BrightGray);
            position.Y += Global.GlyphLineHeight;
            popup.AddImage(new Rect(position.X, position.Y, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), Layer.UI, 1, game.UIPaletteIndex);
            int damage = monster.BaseAttackDamage + monster.BonusAttackDamage;
            popup.AddText(position + new Position(6, 2),
                string.Format(game.DataNameProvider.CharacterInfoDamageString.Replace(' ', damage < 0 ? '-' : '+'), Math.Abs(damage)),
                TextColor.BrightGray);
            position.X = area.X + panelWidth + Global.GlyphWidth;
            popup.AddImage(new Rect(position.X, position.Y, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), Layer.UI, 1, game.UIPaletteIndex);
            int defense = monster.BaseDefense + monster.BonusDefense;
            popup.AddText(position + new Position(7, 2),
                string.Format(game.DataNameProvider.CharacterInfoDefenseString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)),
                TextColor.BrightGray);
            position.X = area.X;
            position.Y += Global.GlyphLineHeight + 4;
            popup.AddText(position, $"{game.DataNameProvider.CharacterInfoAPRString.TrimEnd()}{monster.AttacksPerRound}", TextColor.BrightGray);
            // Icon and level
            --position.X;
            position.Y += Global.GlyphLineHeight;
            popup.AddSunkenBox(new Rect(position, new Size(18, 18)));
            popup.AddImage(new Rect(position.X + 1, position.Y + 2, 16, 14), Graphics.BattleFieldIconOffset + (uint)Class.Monster + (uint)monster.CombatGraphicIndex - 1,
                Layer.UI, 2, game.PrimaryUIPaletteIndex);
            if (game.Features.HasFlag(Features.Elements))
            {
                popup.AddText(position + new Position(21, 2), $"{monster.Name} {monster.Level}", TextColor.BrightGray);
                popup.AddText(position + new Position(21, 10), $"{game.DataNameProvider.ElementLabel} {game.DataNameProvider.GetElementName(monster.Element)}", TextColor.BrightGray);
            }
            else
            {
                popup.AddText(position + new Position(21, 5), $"{monster.Name} {monster.Level}", TextColor.BrightGray);
            }
            // Closing
            game.TrapMouse(area);
            popup.Closed += () =>
            {
                game.CursorType = CursorType.Sword;
                game.UntrapMouse();
                finishAction?.Invoke();
            };
            game.EndSequence();
        }

        static readonly KeyValuePair<uint, uint>[] DestructionSpellDamageValues = new KeyValuePair<uint, uint>[16]
        {
            // Mudsling
            KeyValuePair.Create(4u, 8u),
            // Rockfall
            KeyValuePair.Create(10u, 25u),
            // Earthslide
            KeyValuePair.Create(8u, 16u),
            // Earthquake
            KeyValuePair.Create(8u, 22u),
            // Winddevil
            KeyValuePair.Create(8u, 16u),
            // Windhowler
            KeyValuePair.Create(16u, 48u),
            // Thunderbolt
            KeyValuePair.Create(20u, 32u),
            // Whirlwind
            KeyValuePair.Create(20u, 35u),
            // Firebeam
            KeyValuePair.Create(20u, 30u),
            // Fireball
            KeyValuePair.Create(40u, 85u),
            // Firestorm
            KeyValuePair.Create(35u, 65u),
            // Firepillar
            KeyValuePair.Create(40u, 70u),
            // Waterfall
            KeyValuePair.Create(32u, 60u),
            // Iceball
            KeyValuePair.Create(90u, 180u),
            // Icestorm
            KeyValuePair.Create(64u, 128u),
            // Iceshower
            KeyValuePair.Create(128u, 256u)
        };

        static readonly KeyValuePair<uint, uint>[] AdjustedDestructionSpellDamageValues = new KeyValuePair<uint, uint>[16]
        {
            // Mudsling
            KeyValuePair.Create(5u, 10u),
            // Rockfall
            KeyValuePair.Create(15u, 25u),
            // Earthslide
            KeyValuePair.Create(10u, 22u),
            // Earthquake
            KeyValuePair.Create(8u, 20u),
            // Winddevil
            KeyValuePair.Create(20u, 35u),
            // Windhowler
            KeyValuePair.Create(30u, 50u),
            // Thunderbolt
            KeyValuePair.Create(25u, 45u),
            // Whirlwind
            KeyValuePair.Create(20u, 40u),
            // Firebeam
            KeyValuePair.Create(35u, 55u),
            // Fireball
            KeyValuePair.Create(70u, 105u),
            // Firestorm
            KeyValuePair.Create(65u, 95u),
            // Firepillar
            KeyValuePair.Create(55u, 85u),
            // Waterfall
            KeyValuePair.Create(60u, 75u),
            // Iceball
            KeyValuePair.Create(110u, 150u),
            // Icestorm
            KeyValuePair.Create(100u, 135u),
            // Iceshower
            KeyValuePair.Create(90u, 120u)
        };

        static int UnsignedToSigned(uint word)
        {
            ushort w = (ushort)(word & 0xffff);

            if ((w & 0x8000) == 0)
                return w;

            return w - 0x10000;
        }

        void AdjustDamage(ref uint minDamage, ref uint maxDamage, CharacterValue spellBonus)
        {
            int baseBonus = UnsignedToSigned(spellBonus.CurrentValue);
            int maxBonus = UnsignedToSigned(spellBonus.MaxValue);
            int percBonus = UnsignedToSigned(spellBonus.StoredValue);

            if (percBonus <= -100)
            {
                minDamage = 1;
                maxDamage = 1;
                return;
            }

            minDamage = (uint)Math.Max(1, minDamage + baseBonus);
            maxDamage = (uint)Math.Max(minDamage, maxDamage + baseBonus + maxBonus);

            if (percBonus != 0)
            {
                percBonus += 100;
                minDamage = (uint)Math.Max(1, (minDamage * percBonus) / 100);
                maxDamage = (uint)Math.Max(minDamage, (maxDamage * percBonus) / 100);
            }
        }

        /// <summary>
        /// The boolean argument of the finish action means: NeedsClickAfterwards
        /// </summary>
        void ApplySpellEffect(Character caster, Character target, Spell spell, uint ticks, Action finishAction, uint? targetField = null)
        {
            switch (spell)
            {
                case Spell.GhostWeapon:
                case Spell.GhostInferno:
                {
                    bool ignoreDamageBonus = caster is PartyMember p && p.Index < 16;
                    var damage = (uint)Math.Max(1, caster.BaseAttackDamage + caster.BonusAttackDamage);
                    var bonusDamage = caster.Attributes[Attribute.BonusSpellDamage];
                    uint minDamage = damage;
                    uint maxDamage = damage;
                    if (!ignoreDamageBonus)
                        AdjustDamage(ref minDamage, ref maxDamage, bonusDamage);
                    DealDamage(minDamage, maxDamage - minDamage);
                    return;
                }
                case Spell.Blink:
                    game.SetBattleMessageWithClick(target.Name + game.DataNameProvider.BattleMessageHasBlinked, TextColor.BattlePlayer,
                        () => { MoveCharacterTo(targetField.Value, target); finishAction?.Invoke(); });
                    return;
                case Spell.Escape:
                    // Note: In Ambermoon it was marked as it only can be used outside of battles
                    // but the spell code was only available in battle. This is a bug in the original.
                    EndBattleCleanup();
                    BattleEnded?.Invoke(new Game.BattleEndInfo
                    {
                        MonstersDefeated = false
                    });
                    return;
                case Spell.DissolveVictim:
                case Spell.DispellUndead:
                case Spell.DestroyUndead:
                case Spell.HolyWord:
                {
                    int slot = GetCharacterPosition(target);
                    RemoveCharacterFromBattleField(target);
                    if (caster is PartyMember partyMember)
                    {
                        // Monsters dissolved by those spells are
                        // handled like fled monsters so that their
                        // belongings won't remain after battle.
                        fledCharacters.Add(target);
                        KillMonster(partyMember, target, slot);
                    }
                    else
                    {
                        KillPlayer(target);
                    }
                    break;
                }
                case Spell.Lame:
                    AddCondition(Condition.Lamed, target);
                    break;
                case Spell.Poison:
                    AddCondition(Condition.Poisoned, target);
                    break;
                case Spell.Petrify:
                    AddCondition(Condition.Petrified, target);
                    break;
                case Spell.CauseDisease:
                    AddCondition(Condition.Diseased, target);
                    break;
                case Spell.CauseAging:
                    AddCondition(Condition.Aging, target);
                    break;
                case Spell.Irritate:
                    AddCondition(Condition.Irritated, target);
                    break;
                case Spell.CauseMadness:
                    AddCondition(Condition.Crazy, target);
                    break;
                case Spell.Sleep:
                    AddCondition(Condition.Sleep, target);
                    break;
                case Spell.Fear:
                    AddCondition(Condition.Panic, target);
                    break;
                case Spell.Blind:
                    AddCondition(Condition.Blind, target);
                    break;
                case Spell.Drug:
                    AddCondition(Condition.Drugged, target);
                    break;
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
                    var damageValues = game.Features.HasFlag(Features.AdjustedSpellDamage)
                        ? AdjustedDestructionSpellDamageValues
                        : DestructionSpellDamageValues;
                    int index = spell - Spell.Mudsling;
                    if (index < 4)
                    {
                        if (caster.BattleFlags.HasFlag(BattleFlags.EarthSpellDamageBonus))
                            index += 12;
                    }
                    else if (index < 8)
                    {
                        if (caster.BattleFlags.HasFlag(BattleFlags.WindSpellDamageBonus))
                            index += 8;
                    }
                    else if (index < 12)
                    {
                        if (caster.BattleFlags.HasFlag(BattleFlags.FireSpellDamageBonus))
                            index += 4;
                    }
                    bool ignoreDamageBonus = caster is PartyMember p && p.Index < 16;
                    var damageValue = damageValues[index];
                    var bonusDamage = caster.Attributes[Attribute.BonusSpellDamage];
                    uint minDamage = damageValue.Key;
                    uint maxDamage = damageValue.Value;
                    if (!ignoreDamageBonus)
                        AdjustDamage(ref minDamage, ref maxDamage, bonusDamage);
                    DealDamage(minDamage, maxDamage - minDamage);
                    return;
                }
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                {
                    // Those deal half the caster level as damage.
                    // Monsters deal full level as damage instead.
                    bool ignoreDamageBonus = caster is PartyMember p && p.Index < 16;
                    uint damage = Math.Max(1u, caster is Monster ? (uint)caster.Level : (uint)caster.Level / 2);
                    var bonusDamage = caster.Attributes[Attribute.BonusSpellDamage];
                    uint minDamage = damage;
                    uint maxDamage = damage;
                    if (!ignoreDamageBonus)
                        AdjustDamage(ref minDamage, ref maxDamage, bonusDamage);
                    DealDamage(minDamage, maxDamage - minDamage);
                    return;
                }
                case Spell.LPStealer:
                {
                    uint stealAmount = Math.Min(caster.Level, target.HitPoints.CurrentValue);
                    DealDamage(stealAmount, 0, false);
                    caster.HitPoints.CurrentValue += Math.Min(stealAmount, caster.HitPoints.TotalMaxValue - caster.HitPoints.CurrentValue);
                    if (caster is PartyMember castingMember)
                        layout.FillCharacterBars(castingMember);
                    return;
                }
                case Spell.SPStealer:
                {
                    // Note: In original when you steal more SP as the monster would need for an already
                    // locked-in spell, it still casts that spell.
                    uint stealAmount = Math.Min(caster.Level, target.SpellPoints.CurrentValue);
                    target.SpellPoints.CurrentValue = target.SpellPoints.CurrentValue - stealAmount;
                    caster.SpellPoints.CurrentValue += Math.Min(stealAmount, caster.SpellPoints.TotalMaxValue - caster.SpellPoints.CurrentValue);
                    if (target is PartyMember targetMember)
                        layout.FillCharacterBars(targetMember);
                    else if (caster is PartyMember castingMember)
                        layout.FillCharacterBars(castingMember);
                    break;
                }
                case Spell.MysticImitation:
                {
                    if (target is Monster monster)
                    {
                        ImitateMonster(caster as PartyMember, monster, finishAction);
                        return;
                    }
                    break;
                }
                case Spell.MonsterKnowledge:
                {
                    if (target is Monster monster)
                    {
                        ShowMonsterInfo(monster, finishAction);
                        return;
                    }
                    break;
                }
                case Spell.ShowMonsterLP:
                {
                    if (!showMonsterLP)
                    {
                        foreach (var monster in Monsters)
                        {
                            string tooltip = GetMonsterLPString(monster);

                            if (showElements)
                                tooltip += $"^({game.DataNameProvider.GetElementName(monster.Element)})";

							layout.GetMonsterBattleFieldTooltip(monster).Text = tooltip;
						}
                        showMonsterLP = true;
                    }
                    break;
                }
				case Spell.ShowElements:
				{
					if (!showElements)
					{
						foreach (var monster in Monsters)
						{
							layout.GetMonsterBattleFieldTooltip(monster).Text += $"^({game.DataNameProvider.GetElementName(monster.Element)})";
						}
						showElements = true;
					}
					break;
				}
                case Spell.ForeseeMagic:
                    foreseeMagic = true;
                    break;
				case Spell.ForeseeAttack:
					foreseeAttack = true;
					break;
				case Spell.RecognizeWeakPoint:
				case Spell.SeeWeaknesses:
				case Spell.KnowledgeOfTheWeakness:
                {
                    if (target is Monster monster)
                    {
                        if (!weakenedMonsters.Contains(monster))
                            weakenedMonsters.Add(monster);
                    }
                    break;
                }
				case Spell.MysticDecay:
				{
					// Base Damage = Caster Level
					// Dmg Increase % = (1 + (MaxTargetHP - CurrTargetHP) * 10 / MaxTargetHP) ^ 2
					// MinDmg = 5
                    // The max damage at level 50 would be 100 against a target with 10% LP or below.
                    // But Level and INT bonus is applied as well which can also increase the damage
                    // by 50% or more.
                    //
                    // The damage is random between half the value and full the value while the min
                    // value can still not be below 5.
					//
					// Caster Level 25
					// Target has 10%, 25%, 50%, 75%, 100% LP
					// 10% -> 200% * 25 = 50
					// 25% -> 164% * 25 = 41
					// 50% -> 136% * 25 = 34
					// 75% -> 109% * 25 = 27
					// 100% -> 101% * 25 = 25
					bool ignoreDamageBonus = caster is PartyMember p && p.Index < 16;
					long bonus = 1 + Math.Max(0, ((long)target.HitPoints.MaxValue - target.HitPoints.CurrentValue) * 10 / target.HitPoints.MaxValue);
					bonus *= bonus;
                    uint damage = Math.Max(5u, ((uint)caster.Level * 3 / 2) * (100 + (uint)bonus) / 100);
					var bonusDamage = caster.Attributes[Attribute.BonusSpellDamage];
					uint minDamage = Math.Max(5, damage / 2);
					uint maxDamage = damage;
					if (!ignoreDamageBonus)
						AdjustDamage(ref minDamage, ref maxDamage, bonusDamage);
					DealDamage(minDamage, maxDamage - minDamage);
					return;
				}
                case Spell.ProtectionSphere:
                {
                    protectedCharacters.Add(caster);
                    break;
                }
				default:
                    game.ApplySpellEffect(spell, caster, target, finishAction, false);
                    return;
            }

            finishAction?.Invoke();

            void DealDamage(uint baseDamage, uint variableDamage, bool useSpellBonusDamage = true)
            {
				if (!game.Features.HasFlag(Features.SpellDamageBonus))
                    useSpellBonusDamage = false;

                void EndHurt()
                {
                    if (!target.Alive)
                    {
                        HandleCharacterDeath(caster, target, finishAction);
                    }
                    else
                    {
                        if (target is PartyMember partyMember)
                            layout.FillCharacterBars(partyMember);
                        finishAction?.Invoke();
                    }
                }
                uint damage = CalculateSpellDamage(caster, target, baseDamage, variableDamage);
                uint trackDamage = spell == Spell.DissolveVictim ? target.HitPoints.TotalMaxValue : damage;
                if (caster is Monster monster)
                    TrackMonsterHit(monster, trackDamage);
                else if (caster is PartyMember partyMember)
                    TrackPlayerHit(partyMember, trackDamage);
                if (useSpellBonusDamage)
                {
                    int factor = caster.Level + ((int)caster.Attributes[Attribute.Intelligence].TotalCurrentValue - 50) / 5;

                    if (target is Monster)
                    {
                        int GetWorstBonus(int elementRowIndex)
                        {
                            int worstBonus = 255;
                            var element = target.Element;

                            for (int i = 0; i < 8; ++i)
                            {
                                if (((uint)element & (1 << i)) != 0) // has this element
                                {
                                    int bonus = SpellBonusTable[elementRowIndex + i];

                                    if (bonus < worstBonus)
                                        worstBonus = bonus;
                                }
                            }

                            return worstBonus == 255 ? 100 : worstBonus;
                        }

                        factor += GetWorstBonus(GetElementIndex(spell) * 8);
                    }
                    else
                    {
                        factor += 100;
                    }

                    if (factor <= 0)
                        damage = 1;
                    else
                    {
                        int damageReduction = UnsignedToSigned((uint)target.Attributes[Attribute.BonusSpellDamage].BonusValue);
                        damage = (damage * (uint)factor) / 100;                        
                        damage = (uint)Math.Max(1, damage - (damage * damageReduction) / 100);
                    }
                }
                if (target is Monster monsterTarget && weakenedMonsters.Contains(monsterTarget))
                {
                    uint bonus = monsterTarget.BattleFlags.HasFlag(BattleFlags.Boss) ? 15u : 30u;
                    damage = Math.Min((100 + bonus) * damage / 100, monsterTarget.HitPoints.CurrentValue);
                }
                uint position = (uint)GetSlotFromCharacter(target);
                PlayBattleEffectAnimation(target.Type == CharacterType.Monster ? BattleEffect.HurtMonster : BattleEffect.HurtPlayer,
                    position, ticks, () =>
                    {
                        if (target is PartyMember partyMember)
                        {
                            game.ShowPlayerDamage(game.SlotFromPartyMember(partyMember).Value, damage);
                        }
                        if (target.Conditions.HasFlag(Condition.Sleep))
                            RemoveCondition(Condition.Sleep, target);
                        if (game.Godmode && target is Monster monster)
                            damage = target.HitPoints.CurrentValue;
                        if (target is Monster)
                            target.Damage(damage);
                        else if (!game.Godmode)
                            target.Damage(damage, deathCondition => game.KillPartyMember(target as PartyMember, deathCondition));
                        EndHurt();
                    }
                );
                ShowBattleFieldDamage(GetSlotFromCharacter(target), damage);
            }
        }

        static int GetElementIndex(Spell spell)
        {
            if (spell >= Spell.Mudsling && spell <= Spell.Earthquake)
                return 4;
            if (spell >= Spell.Winddevil && spell <= Spell.Whirlwind)
                return 5;
            if (spell >= Spell.Firebeam && spell <= Spell.Firepillar)
                return 6;
            if (spell >= Spell.Waterfall && spell <= Spell.Iceshower)
                return 7;
            return 1; // all other relevant spells are spirit spells
        }

        static readonly int[] SpellBonusTable = new int[]
        {
              0, 100, 100, 100, 100, 100, 100, 100, // Mental spells
            125,   0, 150, 150, 100, 100, 100, 100, // Spirit spells
            100, 100,   0, 100, 100, 100, 100, 100, // Physical spells
            100, 100, 100,   0, 100, 100, 100, 100, // Undead spells
             75, 100, 100,  75,   0,  50, 100, 150, // Earth spells
             75, 100, 100,  75, 150,   0,  50, 150, // Wind spells
             75,  75, 100, 125, 100, 150,   0,  50, // Fire spells
             75, 100, 100,  75,  50, 100, 150,   0  // Water spells
        };

        public void StartMonsterAnimation(Monster monster, Action<BattleAnimation> setupAction, Action<BattleAnimation> finishAction)
        {
            if (setupAction == null)
                return;

            var animation = layout.GetMonsterBattleAnimation(monster);

            void AnimationFinished()
            {
                animation.AnimationFinished -= AnimationFinished;
                currentBattleAnimation = null;
                currentlyAnimatedMonster = null;
                finishAction?.Invoke(animation);
            }

            animation.AnimationFinished += AnimationFinished;
            setupAction(animation);
            currentBattleAnimation = animation;
            currentlyAnimatedMonster = monster;
        }

        void RemoveCharacterFromBattleField(Character character)
        {
            int position = GetCharacterPosition(character);

            if (position < 0) // already removed
                return;

            HideBattleFieldDamage(position);

            if (currentBattleAnimation != null && character == currentlyAnimatedMonster)
            {
                currentBattleAnimation?.Destroy();
                currentBattleAnimation = null;
                currentlyAnimatedMonster = null;
            }

            battleField[position] = null;
            roundBattleActions.Where(b => b.Character == character).ToList().ForEach(b => b.Skip = true);
            game.RemoveBattleActor(character);
        }

        public void MoveCharacterTo(uint tile, Character character)
        {
            MoveCharacterTo(tile % 6, tile / 6, character);
        }

        void MoveCharacterTo(uint column, uint row, Character character)
        {
            battleField[GetCharacterPosition(character)] = null;
            battleField[column + row * 6] = character;
            game.MoveBattleActorTo(column, row, character);
        }

        int GetCharacterPosition(Character character) => battleField.ToList().IndexOf(character);

        void PickPanicAction(PartyMember partyMember, PlayerBattleAction playerBattleAction, List<int> forbiddenMoveSpots)
        {
            var position = GetCharacterPosition(partyMember);

            if (position >= 24 && partyMember.CanFlee())
            {
                playerBattleAction.BattleAction = BattleActionType.Flee;
            }
            else if (position < 24 && partyMember.CanMove() && MoveSpotAvailable(position, partyMember, true, forbiddenMoveSpots))
            {
                playerBattleAction.BattleAction = BattleActionType.Move;
                int playerColumn = position % 6;
                int playerRow = position / 6;
                var possibleSpots = new List<int>(2);
                int newSpot = -1;
                int moveRange = GetMoveRange(partyMember);

                for (int column = Math.Max(0, playerColumn - moveRange); column <= Math.Min(5, playerColumn + moveRange); ++column)
                {
                    int newPosition = column + (playerRow + 1) * 6;

                    if (battleField[newPosition] == null && !forbiddenMoveSpots.Contains(newPosition))
                    {
                        if (column == playerColumn)
                        {
                            newSpot = newPosition;
                            break;
                        }
                        else
                        {
                            possibleSpots.Add(newPosition);
                        }
                    }
                }

                if (newSpot == -1)
                {
                    if (possibleSpots.Count == 0)
                    {
                        // Should not happen but if so, just do nothing.
                        playerBattleAction.BattleAction = BattleActionType.None;
                        return;
                    }

                    newSpot = possibleSpots[game.RandomInt(0, possibleSpots.Count - 1)];
                }

                playerBattleAction.Parameter = CreateMoveParameter((uint)newSpot);
            }
            else
            {
                playerBattleAction.BattleAction = BattleActionType.None;
            }
        }

        void PickMadAction(PartyMember partyMember, PlayerBattleAction playerBattleAction, List<int> forbiddenMoveSpots)
        {
            // Mad players can only attack and move.
            var position = GetCharacterPosition(partyMember);

            bool TryAttack()
            {
                if (partyMember.Conditions.CanAttack() &&
                    !partyMember.HasLongRangedWeapon(game.ItemManager) &&
                    AttackSpotAvailable(position, partyMember, true))
                {
                    playerBattleAction.BattleAction = BattleActionType.Attack;
                    playerBattleAction.Parameter = CreateAttackParameter(GetRandomAttackSpot(position, partyMember), partyMember, game.ItemManager);
                    return true;
                }

                return false;
            }

            bool TryMove()
            {
                if (partyMember.CanMove() && MoveSpotAvailable(position, partyMember, false, forbiddenMoveSpots))
                {
                    uint moveSpot = GetRandomMoveSpot(position, partyMember, forbiddenMoveSpots);
                    if (moveSpot == uint.MaxValue) // still no spot found?
                        return false;
                    playerBattleAction.BattleAction = BattleActionType.Move;
                    playerBattleAction.Parameter = CreateMoveParameter(moveSpot);
                    forbiddenMoveSpots.Add((int)moveSpot);
                    return true;
                }

                return false;
            }

            bool done;

            // Try attack first?
            if (game.RandomInt(0, 0xffff) < 40000)
            {
                done = TryAttack();

                if (!done)
                    done = TryMove();
            }
            // Otherwise try move first
            else
            {
                done = TryMove();

                if (!done)
                    done = TryAttack();
            }

            if (!done)
            {
                playerBattleAction.BattleAction = BattleActionType.None;
                playerBattleAction.Parameter = 0;
            }

            layout.UpdateCharacterStatus(game.SlotFromPartyMember(partyMember).Value,
                playerBattleAction.BattleAction.ToStatusGraphic(playerBattleAction.Parameter, game.ItemManager));
        }

        BattleActionType PickMonsterAction(Monster monster, bool wantsToFlee, List<int> forbiddenMonsterMoveSpots, bool canCast)
        {
            var position = GetCharacterPosition(monster);
            List<BattleActionType> possibleActions = new List<BattleActionType>();

            if (position < 6 && wantsToFlee && monster.CanFlee())
            {
                return BattleActionType.Flee;
            }
            if (wantsToFlee && monster.CanMove())
            {
                // In this case always retreat if possible
                if (MoveSpotAvailable(position, monster, wantsToFlee, forbiddenMonsterMoveSpots))
                    return BattleActionType.Move;
            }
            if (monster.Conditions.HasFlag(Condition.Crazy))
            {
                return AttackOrMove(true);
            }
            bool canAttackRanged = true;
            bool canAttackMelee = true;

            while (true)
            {
                int rand = game.RandomInt(0, 15);

                if (rand < 8)
                {
                    if (canCast && monster.HasAnySpell() && monster.Conditions.CanCastSpell() && CanCastAnySpell(monster))
                        return BattleActionType.CastSpell;

                    canCast = false;
                }
                else if (rand < 14)
                {
                    if (canAttackRanged)
                    {
                        if (!monster.HasLongRangedAttack(game.ItemManager, out bool hasAmmo))
                        {
                            canAttackRanged = false;
                            continue;
                        }
                        else if (!hasAmmo)
                        {
                            bool IsMeleeWeapon(uint itemIndex) => game.ItemManager.GetItem(itemIndex).Type == ItemType.CloseRangeWeapon;
                            var weaponSlot = monster.Equipment.Slots[EquipmentSlot.RightHand];
                            game.EquipmentRemoved(monster, weaponSlot.ItemIndex, 1, weaponSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
                            var meleeWeaponSlot = monster.Inventory.Slots.FirstOrDefault(s => !s.Empty && IsMeleeWeapon(s.ItemIndex));
                            if (meleeWeaponSlot != null)
                            {
                                // Switch weapons
                                weaponSlot.Exchange(meleeWeaponSlot);
                                game.EquipmentAdded(weaponSlot.ItemIndex, 1, monster);
                            }
                            else
                            {
                                // Just drop the ranged weapon
                                var emptyInventorySlot = monster.Inventory.Slots.FirstOrDefault(s => s.Empty);
                                if (emptyInventorySlot != null)
                                    emptyInventorySlot.Replace(weaponSlot);
                                weaponSlot.Clear();

                                if (monster.BaseAttackDamage + monster.BonusAttackDamage == 0)
                                    monsterMorale[initialMonsters.IndexOf(monster)] /= 2;
                            }
                        }
                        return AttackOrMove();
                    }
                }
                else
                {
                    if (canAttackMelee)
                    {
                        if (monster.HasLongRangedWeapon(game.ItemManager))
                        {
                            canAttackMelee = false;
                            continue;
                        }
                        return AttackOrMove();
                    }
                }
            }
            BattleActionType AttackOrMove(bool mad = false)
            {
                if (mad)
                {
                    bool TryAttack() =>
                        monster.Conditions.CanAttack() &&
                        !monster.HasLongRangedWeapon(game.ItemManager) &&
                        AttackSpotAvailable(position, monster, false);
                    bool TryMove() =>
                        monster.CanMove() && MoveSpotAvailable(position, monster, wantsToFlee, forbiddenMonsterMoveSpots);
                    if (game.RandomInt(0, 0xffff) < 40000)
                    {
                        if (TryAttack())
                            return BattleActionType.Attack;
                        if (TryMove())
                            return BattleActionType.Move;
                    }
                    else
                    {
                        if (TryMove())
                            return BattleActionType.Move;
                        if (TryAttack())
                            return BattleActionType.Attack;
                    }
                    return BattleActionType.None;
                }
                else
                {
                    if (monster.Conditions.CanAttack() && AttackSpotAvailable(position, monster, false))
                        return BattleActionType.Attack;
                    else if (monster.CanMove() && MoveSpotAvailable(position, monster, wantsToFlee, forbiddenMonsterMoveSpots))
                        return BattleActionType.Move;
                    else
                        return BattleActionType.None;
                }
            }
        }

        List<Spell> GetAvailableSpells(Character caster, Func<Spell, bool> checker)
        {
            var sp = caster.SpellPoints.CurrentValue;

            if (sp == 0)
                return new List<Spell>();

            return caster.LearnedSpells.Where(spell =>
            {
                var spellInfo = SpellInfos.Entries[spell];
                return sp >= SpellInfos.GetSPCost(game.Features, spell, caster) &&
                    spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle) &&
                    checker(spell);

            }).ToList();
        }

        List<Spell> GetAvailableMonsterSpells(Monster monster)
        {
            return GetAvailableSpells(monster, spell =>
            {
                return spell.IsCastableByMonster() &&
                       SpellInfos.Entries[spell].Target.TargetsEnemy();
            });
        }

        bool CanCastAnySpell(Monster monster)
        {
            return GetAvailableMonsterSpells(monster).Count != 0;
        }

        int GetMoveRange(Character character)
        {
            return Util.Limit(1, (int)character.Attributes[Attribute.Speed].TotalCurrentValue / 40, 3);
        }

        bool MoveSpotAvailable(int characterPosition, Character character, bool wantsToFlee, List<int> forbiddenMoveSpots = null)
        {
            int moveRange = GetMoveRange(character);

            if (!GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY,
                moveRange, RangeType.Move, wantsToFlee))
                return false;
            int currentRow = characterPosition / 6;

            for (int y = minY; y <= maxY; ++y)
            {
                if (character.Type == CharacterType.Monster)
                {
                    if ((!wantsToFlee && y < currentRow) ||
                        (wantsToFlee && y >= currentRow))
                        continue;
                }
                else
                {
                    if (wantsToFlee && y <= currentRow)
                        continue;
                }

                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6] == null && (forbiddenMoveSpots == null || !forbiddenMoveSpots.Contains(x + y * 6)))
                    {
                        if (y == currentRow) // we only allow moving left/right in rare cases
                        {
                            // Note: This can only happen if the monster doesn't want to flee
                            if (character.Type != CharacterType.Monster || IsPlayerNearby(x + y * 6)) // only move left/right to reach a player
                                return true;
                            else if (currentRow == 3) // monster in second lowest row
                            {
                                int currentX = characterPosition % 6;
                                int xDir = x - currentX; // >0 = right, <0 = left
                                int leftEnemyCount = 0;
                                int rightEnemyCount = 0;

                                for (int py = 3; py <= 4; py++)
                                {
                                    for (int px = 0; px < 6; px++)
                                    {
                                        if (battleField[px + py * 6]?.Type == CharacterType.PartyMember)
                                        {
                                            if (px < currentX)
                                                leftEnemyCount++;
                                            else if (px > currentX)
                                                rightEnemyCount++;
                                        }
                                    }
                                }

                                if (xDir < 0 && leftEnemyCount >= rightEnemyCount)
                                    return true;
                                else if (xDir > 0 && rightEnemyCount >= leftEnemyCount)
                                    return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool AttackSpotAvailable(int characterPosition, Character character, bool mad)
        {
            int range = character.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? 6 : 1;

            if (!GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, range, RangeType.Enemy))
                return false;

            var targetCheck = mad
                ? (Func<Character, bool>)(c => c != null && c != character)
                : (Func<Character, bool>)(c => c != null && c.Type != character.Type);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;
                    if (targetCheck(battleField[position]))
                        return true;
                }
            }

            return false;
        }

        enum RangeType
        {
            Move,
            Enemy,
            Friend
        }

        bool GetRangeMinMaxValues(int characterPosition, Character character, out int minX, out int maxX,
            out int minY, out int maxY, int range, RangeType rangeType, bool wantsToFlee = false)
        {
            int characterX = characterPosition % 6;
            int characterY = characterPosition / 6;
            minX = Math.Max(0, characterX - range);
            maxX = Math.Min(5, characterX + range);

            if (character.Type == CharacterType.Monster)
            {
                if (rangeType == RangeType.Enemy)
                {
                    minY = Math.Max(3, characterY - range);
                    maxY = Math.Min(4, characterY + range);
                }
                else
                {
                    minY = Math.Max(0, characterY - range);
                    maxY = Math.Min(3, characterY + range);
                }

                if (wantsToFlee)
                {
                    if (characterY == 0) // We are in perfect flee position, so don't move
                        return false;

                    // Don't move down or to the side when trying to flee
                    maxY = characterY - 1;
                }
                else
                {
                    // TODO: Allow up movement if other monsters block path to players
                    //       and we need to move around them.
                    // Don't move up (away from players)
                    minY = characterY;
                }
            }
            else if (wantsToFlee) // Fleeing party member (Fear)
            {
                minY = maxY = 4; // always move to last row when fleeing
            }
            else // Mad party member
            {
                if (rangeType == RangeType.Enemy)
                {
                    minY = Math.Max(0, characterY - range);
                    maxY = Math.Min(3, characterY + range);
                }
                else
                {
                    minY = Math.Max(3, characterY - range);
                    maxY = Math.Min(4, characterY + range);
                }
            }

            return true;
        }

        uint GetRandomMoveSpot(int characterPosition, Character character, List<int> forbiddenPositions)
        {
            GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, 1, RangeType.Move);
            var possiblePositions = new List<int>();
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;

                    if (battleField[position] == null && !forbiddenPositions.Contains(position))
                        possiblePositions.Add(position);
                }
            }
            if (possiblePositions.Count == 0)
                return uint.MaxValue;
            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        uint GetRandomAttackSpot(int characterPosition, Character character)
        {
            // Note: This is only used for mad players, so the target can be of any kind.
            GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, 1, RangeType.Enemy);
            var possiblePositions = new List<int>();
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;

                    if (battleField[position] != null)
                        possiblePositions.Add(position);
                }
            }
            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        uint GetBestMoveSpot(int characterPosition, Monster monster, bool wantsToFlee, List<int> forbiddenMonsterMoveSpots)
        {
            int moveRange = monster.Attributes[Attribute.Speed].TotalCurrentValue >= 80 ? 2 : 1;
            GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY,
                moveRange, RangeType.Move, wantsToFlee);
            int currentColumn = characterPosition % 6;
            int currentRow = characterPosition / 6;
            var possiblePositions = new List<int>();

            if (wantsToFlee)
            {
                for (int row = minY; row < currentRow; ++row)
                {
                    if (battleField[currentColumn + row * 6] == null)
                        return (uint)(currentColumn + row * 6);
                }
            }

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;

                    if (battleField[position] == null && !forbiddenMonsterMoveSpots.Contains(position))
                    {
                        if (y == currentRow) // we only allow moving left/right in rare cases
                        {
                            // Note: This can only happen if the monster doesn't want to flee
                            if (IsPlayerNearby(position)) // only move left/right to reach a player
                                possiblePositions.Add(position);
                            else if (currentRow == 3) // monster in second lowest row
                            {
                                int currentX = characterPosition % 6;
                                int xDir = x - currentX; // >0 = right, <0 = left
                                int leftEnemyCount = 0;
                                int rightEnemyCount = 0;

                                for (int py = 3; py <= 4; py++)
                                {
                                    for (int px = 0; px < 6; px++)
                                    {
                                        if (battleField[px + py * 6]?.Type == CharacterType.PartyMember)
                                        {
                                            if (px < currentX)
                                                leftEnemyCount++;
                                            else if (px > currentX)
                                                rightEnemyCount++;
                                        }
                                    }
                                }

                                if (xDir < 0 && leftEnemyCount >= rightEnemyCount)
                                    possiblePositions.Add(position);
                                else if (xDir > 0 && rightEnemyCount >= leftEnemyCount)
                                    possiblePositions.Add(position);
                            }
                        }
                        else
                        {
                            possiblePositions.Add(position);
                        }
                    }
                }
            }

            if (!wantsToFlee)
            {
                // Prefer moving to positions where a player is nearby.
                // Also prefer moving straight down.
                var nearPlayerPositions = possiblePositions.Where(p => IsPlayerNearby(p)).ToList();

                if (nearPlayerPositions.Count != 0)
                {
                    // Prefer spots with the most reachable players
                    if (nearPlayerPositions.Count > 1)
                    {
                        var nearPlayerPositionsWithAmount = nearPlayerPositions.Select(p => new { p, n = NearbyPlayerAmount(p) }).ToList();
                        nearPlayerPositionsWithAmount.Sort((a, b) => b.n.CompareTo(a.n));
                        int maxAmount = nearPlayerPositionsWithAmount[0].n;

                        if (maxAmount > nearPlayerPositionsWithAmount[^1].n)
                        {
                            nearPlayerPositions = nearPlayerPositionsWithAmount.TakeWhile(p => p.n == maxAmount).Select(p => p.p).ToList();
                        }

                        // Prefer spots in the center
                        if (nearPlayerPositions.Any(p => p % 6 == 2 || p % 6 == 3))
                            nearPlayerPositions = nearPlayerPositions.Where(p => p % 6 == 2 || p % 6 == 3).ToList();
                        else if (nearPlayerPositions.Any(p => p % 6 == 1 || p % 6 == 4))
                            nearPlayerPositions = nearPlayerPositions.Where(p => p % 6 == 1 || p % 6 == 4).ToList();
                    }

                    return (uint)nearPlayerPositions[game.RandomInt(0, nearPlayerPositions.Count - 1)];
                }

                // If down and left/right is no player, move to the other direction instead.
                if (battleField[currentColumn + 18]?.Type != CharacterType.PartyMember &&
                    battleField[currentColumn + 24]?.Type != CharacterType.PartyMember)
                {
                    var specificPositions = new List<int>(possiblePositions);

                    // No target down below.
                    if (currentColumn == 0)
                    {
                        foreach (var exclude in possiblePositions.Where(p => p % 6 == 0))
                            specificPositions.Remove(exclude);
                    }
                    else if (currentColumn == 5)
                    {
                        if (currentColumn == 0)
                        {
                            foreach (var exclude in possiblePositions.Where(p => p % 6 == 5))
                                specificPositions.Remove(exclude);
                        }
                    }
                    else
                    {
                        bool[] hasTargetsInColumn = new bool[6];

                        for (int i = 0; i < 6; ++i)
                        {
                            hasTargetsInColumn[i] = battleField[18 + i]?.Type == CharacterType.PartyMember ||
                                battleField[24 + i]?.Type == CharacterType.PartyMember;
                        }

                        if (hasTargetsInColumn[currentColumn - 1] && !hasTargetsInColumn[currentColumn + 1])
                        {
                            foreach (var exclude in possiblePositions.Where(p => currentColumn < p % 6).ToList())
                                specificPositions.Remove(exclude);
                        }
                        else if (hasTargetsInColumn[currentColumn + 1] && !hasTargetsInColumn[currentColumn - 1])
                        {
                            foreach (var exclude in possiblePositions.Where(p => currentColumn > p % 6).ToList())
                                specificPositions.Remove(exclude);
                        }
                        else
                        {
                            bool hasLeftTargets = hasTargetsInColumn[currentColumn - 1] ||
                                (currentColumn > 1 && hasTargetsInColumn[currentColumn - 2]) ||
                                (currentColumn > 2 && hasTargetsInColumn[currentColumn - 3]) ||
                                (currentColumn > 3 && hasTargetsInColumn[currentColumn - 4]);
                            bool hasRightTargets = hasTargetsInColumn[currentColumn + 1] ||
                                (currentColumn < 4 && hasTargetsInColumn[currentColumn + 2]) ||
                                (currentColumn < 3 && hasTargetsInColumn[currentColumn + 3]) ||
                                (currentColumn < 2 && hasTargetsInColumn[currentColumn + 4]);

                            if (hasLeftTargets && !hasRightTargets)
                            {
                                foreach (var exclude in possiblePositions.Where(p => currentColumn <= p % 6).ToList())
                                    specificPositions.Remove(exclude);
                            }
                            else if (hasRightTargets && !hasLeftTargets)
                            {
                                foreach (var exclude in possiblePositions.Where(p => currentColumn >= p % 6).ToList())
                                    specificPositions.Remove(exclude);
                            }
                        }

                        if (specificPositions.Count != 0)
                            return (uint)specificPositions[game.RandomInt(0, specificPositions.Count - 1)];
                    }
                }
                // Prefer moving straight down if not on left or right column
                else if (currentColumn != 0 && currentColumn != 5)
                {
                    for (int row = maxY; row > currentRow; --row)
                    {
                        if (battleField[currentColumn + row * 6] == null)
                        {
                            return (uint)(currentColumn + row * 6);
                        }
                    }
                }

                // Prefer move positions that are not on left or right side because positions in the middle
                // will generally be better for reaching players sooner.
                var positionsWithoutOutsideColumns = possiblePositions.Where(p => p % 6 != 0 && p % 6 != 5).ToList();

                if (positionsWithoutOutsideColumns.Count != 0)
                    return (uint)positionsWithoutOutsideColumns[game.RandomInt(0, positionsWithoutOutsideColumns.Count - 1)];
            }
            else
            {
                // Prefer moving straight back when fleeing
                for (int row = minY; row <= maxX; ++row)
                {
                    if (battleField[currentColumn + row * 6] == null)
                    {
                        return (uint)(currentColumn + row * 6);
                    }
                }
            }

            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        bool IsPlayerNearby(int position)
        {
            int minX = Math.Max(0, position % 6 - 1);
            int maxX = Math.Min(5, position % 6 + 1);
            int minY = Math.Max(0, position / 6 - 1);
            int maxY = Math.Min(4, position / 6 + 1);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6]?.Type == CharacterType.PartyMember)
                        return true;
                }
            }

            return false;
        }

        int NearbyPlayerAmount(int position)
        {
            int minX = Math.Max(0, position % 6 - 1);
            int maxX = Math.Min(5, position % 6 + 1);
            int minY = Math.Max(0, position / 6 - 1);
            int maxY = Math.Min(4, position / 6 + 1);
            int amount = 0;

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6]?.Type == CharacterType.PartyMember)
                        ++amount;
                }
            }

            return amount;
        }

        bool MonsterWantsToFlee(Monster monster)
        {
            if (monster.BattleFlags.HasFlag(BattleFlags.Boss))
                return false;

            if (monster.Conditions.HasFlag(Condition.Panic))
                return true;

            if (monster.Conditions.HasFlag(Condition.Crazy))
                return false;

            int lowLPEffect = (int)((monster.HitPoints.TotalMaxValue - monster.HitPoints.CurrentValue) * 75 / monster.HitPoints.TotalMaxValue);
            int rdeEffect = ((int)relativeDamageEfficiency - 50) / 4;
            int monsterAllyEffect = 0;

            if (initialMonsters.Count > 1)
                monsterAllyEffect = (Monsters.Count() - 1) * 40 / (initialMonsters.Count - 1) - 25;

            int fear = Util.Limit(0, lowLPEffect + rdeEffect - monsterAllyEffect, 100);
            int morale = (int)monsterMorale[initialMonsters.IndexOf(monster)];

            if (fear > morale)
            {
                int fleeChance = Math.Min(fear - (int)morale, 100);
                return game.RollDice100() < fleeChance;
            }

            return false;
        }

        uint GetBestAttackSpot(int characterPosition, Monster monster)
        {
            int range = monster.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? 6 : 1;
            GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, range, RangeType.Enemy);
            var possiblePositions = new Dictionary<int, uint>();
            bool mad = monster.Conditions.HasFlag(Condition.Crazy);
            var targetCheck = mad
                ? (Func<Character, bool>)(c => c != null && c != monster)
                : (Func<Character, bool>)(c => c != null && c.Type == CharacterType.PartyMember);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;
                    if (targetCheck(battleField[position]))
                        possiblePositions.Add(position, mad ? 0 : averagePlayerDamage[partyMembers.ToList().IndexOf(battleField[position] as PartyMember)]);
                }
            }

            if (possiblePositions.Count == 1)
                return (uint)possiblePositions.First().Key;

            if (!mad)
            {
                var maxDamage = possiblePositions.Max(p => p.Value);
                var maxDamagePositions = possiblePositions.Where(p => p.Value == maxDamage).Select(p => p.Key).ToList();

                if (maxDamagePositions.Count == 1)
                    return (uint)maxDamagePositions[0];

                return (uint)maxDamagePositions[game.RandomInt(0, maxDamagePositions.Count - 1)];
            }

            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        uint GetBestSpellSpotOrRow(Monster monster, Spell spell)
        {
            var spellInfo = SpellInfos.Entries[spell];

            if (spellInfo.Target == SpellTarget.EnemyRow)
            {
                // TODO: maybe pick the row with most players for some clever monsters?
                bool RowEmpty(int row) => !battleField.Skip(row * 6).Take(6).Any(c => c?.Type == CharacterType.PartyMember);
                if (RowEmpty(3))
                    return 4;
                else if (RowEmpty(4))
                    return 3;
                else
                    return (uint)game.RandomInt(3, 4);
            }
            else
            {
                var positions = partyMembers.Where(p => p?.Alive == true)
                    .Select(p => GetCharacterPosition(p)).ToArray();
                return (uint)positions[game.RandomInt(0, positions.Length - 1)];
            }
        }

        uint PickActionParameter(BattleActionType battleAction, Monster monster, bool wantsToFlee, List<int> forbiddenMonsterMoveSpots)
        {
            switch (battleAction)
            {
            case BattleActionType.Move:
                {
                    var moveSpot = GetBestMoveSpot(GetCharacterPosition(monster), monster, wantsToFlee, forbiddenMonsterMoveSpots);
                    forbiddenMonsterMoveSpots.Add((int)moveSpot);
                    return CreateMoveParameter(moveSpot);
                }
            case BattleActionType.Attack:
                {
                    var weaponIndex = monster.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex;
                    var ammoIndex = monster.Equipment.Slots[EquipmentSlot.LeftHand].ItemIndex;
                    if (ammoIndex == weaponIndex) // two-handed weapon?
                        ammoIndex = 0;
                    return CreateAttackParameter(GetBestAttackSpot(GetCharacterPosition(monster), monster), weaponIndex, ammoIndex);
                }
            case BattleActionType.CastSpell:
                {
                    var maxPlayerDamage = averagePlayerDamage.Where((d, i) => partyMembers[i]?.Alive == true && !fledCharacters.Contains(partyMembers[i])).Max();
                    uint getPrio(uint damage) => maxPlayerDamage == 0 ? 100 : damage * 100 / maxPlayerDamage;
                    var maxDamagePlayers = averagePlayerDamage.Select((d, i) => new { Damage = d, Player = partyMembers[i] })
                        .Where(x => x.Damage == maxPlayerDamage && x.Player?.Alive == true && !fledCharacters.Contains(x.Player))
                        .Select(x => new { x.Player, Prio = getPrio(x.Damage), Row = GetCharacterPosition(x.Player) / 6 });
                    var averagePrio = maxDamagePlayers.Average(x => x.Prio);
                    var spells = GetAvailableMonsterSpells(monster);
                    uint targetTileOrRow = 0;
                    void PickBestTargetTile()
                    {
                        var players = maxDamagePlayers.ToList();
                        targetTileOrRow = (uint)GetCharacterPosition(players[game.RandomInt(0, players.Count - 1)].Player);
                    }
                    void PickBestTargetRow()
                    {
                        var rows = maxDamagePlayers.GroupBy(x => x.Row).Select(g => new { Row = g.Key, Prio = g.Average(x => x.Prio) });
                        var maxRowPrio = rows.Max(r => r.Prio);
                        targetTileOrRow = (uint)rows.First(row => row.Prio == maxRowPrio).Row;
                    }
                    if (partyMembers.Count(p => p != null && p.Alive && !fledCharacters.Contains(p)) == 1)
                    {
                        // Only 1 player in battle
                        // Prefer single target spells
                        var singleTargetSpells = spells.Where(s => SpellInfos.Entries[s].Target == SpellTarget.SingleEnemy).ToList();

                        if (singleTargetSpells.Count != 0)
                        {
                            PickBestTargetTile();
                            spells = singleTargetSpells;
                        }
                        else
                        {
                            var rowTargetSpells = spells.Where(s => SpellInfos.Entries[s].Target == SpellTarget.EnemyRow).ToList();

                            if (rowTargetSpells.Count != 0)
                            {
                                PickBestTargetRow();
                                spells = rowTargetSpells;
                            }

                            // Otherwise pick from all spells
                        }
                    }
                    else if (averagePrio >= 75 && spells.Any(s => SpellInfos.Entries[s].Target == SpellTarget.AllEnemies))
                    {
                        spells = spells.Where(s => SpellInfos.Entries[s].Target == SpellTarget.AllEnemies).ToList();
                    }
                    else if (averagePrio >= 50 && spells.Any(s => SpellInfos.Entries[s].Target == SpellTarget.EnemyRow))
                    {
                        PickBestTargetRow();
                        spells = spells.Where(s => SpellInfos.Entries[s].Target == SpellTarget.EnemyRow).ToList();
                    }
                    else // single target spell
                    {
                        PickBestTargetTile();
                        spells = spells.Where(s => SpellInfos.Entries[s].Target == SpellTarget.SingleEnemy).ToList();
                    }
                    // This might happen if the monster only has All or Row spells and the prio forces to use a Single or Row spell.
                    if (spells.Count == 0)
                        return 0; // This will abort casting and disallow casting in this round.
                    var spell = spells[game.RandomInt(0, spells.Count - 1)];
                    return CreateCastSpellParameter(targetTileOrRow, spell);
                }
            default:
                return 0;
            }
        }

        void PlayBattleEffectAnimation(BattleEffect battleEffect, uint tile, uint ticks, Action finishedAction, float scale = 1.0f, Character[] battleField = null)
        {
            PlayBattleEffectAnimation(battleEffect, tile, tile, ticks, finishedAction, scale, battleField);
        }

        void PlayBattleEffectAnimation(BattleEffect battleEffect, uint sourceTile, uint targetTile, uint ticks, Action finishedAction, float scale = 1.0f, Character[] battleField = null)
        {
            battleField ??= this.battleField;
            var effects = BattleEffects.GetEffectInfo(layout.RenderView, battleEffect, sourceTile, targetTile, battleField, scale);
            int numFinishedEffects = 0;

            void FinishEffect()
            {
                if (++numFinishedEffects == effects.Count)
                    finishedAction?.Invoke();
            }

            effectAnimations = layout.CreateBattleEffectAnimations(effects.Count);

            for (int i = 0; i < effects.Count; ++i)
            {
                var effect = effects[i];

                PlayBattleEffectAnimation(i, effect.StartTextureIndex, effect.FrameSize, effect.FrameCount, ticks, FinishEffect,
                    effect.Duration / effect.FrameCount, effect.InitialDisplayLayer, effect.StartPosition, effect.EndPosition,
                    effect.StartScale, effect.EndScale, effect.MirrorX, effect.EndDisplayLayer);
            }
        }

        void PlayBattleEffectAnimation(int index, uint graphicIndex, Size frameSize, uint numFrames, uint ticks,
            Action finishedAction, uint ticksPerFrame, byte initialDisplayLayer, Position startPosition, Position endPosition,
            float initialScale = 1.0f, float endScale = 1.0f, bool mirrorX = false, byte? endDisplayLayer = null)
        {
            var effectAnimation = effectAnimations[index];
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleEffects);
            effectAnimation.SetDisplayLayer(initialDisplayLayer);
            effectAnimation.SetStartFrame(textureAtlas.GetOffset(graphicIndex), frameSize, startPosition, initialScale, mirrorX);
            effectAnimation.AnimationFinished += EffectAnimationFinished;
            effectAnimation.Play(Enumerable.Range(0, (int)numFrames).ToArray(), ticksPerFrame, ticks, endPosition, endScale);
            effectAnimation.Visible = true;

            void EffectAnimationFinished()
            {
                if (endDisplayLayer != initialDisplayLayer)
                    effectAnimation.AnimationUpdated -= UpdateDisplayLayer;
                effectAnimation.AnimationFinished -= EffectAnimationFinished;
                effectAnimation.Visible = false;
                finishedAction?.Invoke();
            }

            void UpdateDisplayLayer(float progress)
            {
                effectAnimation.SetDisplayLayer((byte)Util.Limit(0, Util.Round(initialDisplayLayer +
                    ((int)endDisplayLayer.Value - (int)initialDisplayLayer) * progress), 255));
            }

            if (endDisplayLayer != initialDisplayLayer)
                effectAnimation.AnimationUpdated += UpdateDisplayLayer;
        }

        // Lowest 5 bits: Tile index (0-29) to move to
        public static uint CreateMoveParameter(uint targetTile) => targetTile & 0x1f;
        // Lowest 5 bits: Tile index (0-29) to attack
        // Next 11 bits: Weapon item index (can be 0 for monsters)
        // Next 11 bits: Optional ammunition item index
        public static uint CreateAttackParameter(uint targetTile, uint weaponIndex = 0, uint ammoIndex = 0) =>
            (targetTile & 0x1f) | ((weaponIndex & 0x7ff) << 5) | ((ammoIndex & 0x7ff) << 16);
        public static uint CreateAttackParameter(uint targetTile, Character character, IItemManager itemManager)
        {
            uint weaponIndex = character.Equipment.Slots[EquipmentSlot.RightHand]?.ItemIndex ?? 0;
            uint ammoIndex = 0;

            if (weaponIndex != 0)
            {
                var weapon = itemManager.GetItem(weaponIndex);

                if (weapon.Type == ItemType.LongRangeWeapon && weapon.UsedAmmunitionType != AmmunitionType.None)
                {
                    ammoIndex = character.Equipment.Slots[EquipmentSlot.LeftHand]?.ItemIndex ?? 0;
                }
            }

            return CreateAttackParameter(targetTile, weaponIndex, ammoIndex);
        }
        // Lowest 5 bits: Tile index (0-29) or row (0-4) to cast spell on
        // Next 5 bits: Item slot index (when spell came from an item, otherwise 0x1f)
        // Next bit: 0 = inventory item, 1 = equipped item
        // Next 16 bits: Spell index
        // Next 5 bits: Blink character position (0-29)
        public static uint CreateCastSpellParameter(uint targetTileOrRow, Spell spell, uint? itemSlotIndex = null,
            bool? equippedItem = null, uint blinkCharacterPosition = 0) =>
            (targetTileOrRow & 0x1f) | (((itemSlotIndex ?? 0x1f) & 0x1f) << 5) | ((equippedItem == true) ? 0x400u : 0) |
            (((uint)spell & 0xffff) << 11) | ((blinkCharacterPosition & 0x1f) << 27);
        [Flags]
        enum AttackActionFlags : uint
        {
            BreakWeapon = 0x01000000,
            BreakArmor = 0x02000000,
            LastAmmo = 0x04000000,
            BreakDefenderWeapon = 0x08000000,
            BreakDefenderShield = 0x10000000
        }
        // Tile index (T, 0-29): 5 bits
        // Attack result (R): 3 bits
        // Damage (D): 16 bits
        // Follow action flags (F): 8 bits
        // FFFFFFFF DDDDDDDD DDDDDDDD RRRTTTTT
        public static uint CreateHurtParameter(uint targetTile) => (targetTile & 0x1f);
        static uint UpdateHurtParameter(uint hurtParameter, uint damage, AttackResult attackResult) =>
            (hurtParameter & 0xff00001f) | ((damage & 0x0000ffff) << 8) | ((uint)attackResult << 5);
        static uint UpdateAttackFollowActionParameter(uint parameter, AttackActionFlags additionalFlags) =>
            parameter | (uint)additionalFlags;
        public static uint GetTargetTileOrRowFromParameter(uint actionParameter) => actionParameter & 0x1f;
        static void GetAttackInformation(uint actionParameter, out uint targetTile, out uint weaponIndex, out uint ammoIndex)
        {
            ammoIndex = (actionParameter >> 16) & 0x7ff;
            weaponIndex = (actionParameter >> 5) & 0x7ff;
            targetTile = actionParameter & 0x1f;
        }
        public static bool IsLongRangedAttack(uint actionParameter, IItemManager itemManager)
        {
            var weaponIndex = (actionParameter >> 5) & 0x7ff;

            if (weaponIndex == 0)
                return false;

            return itemManager.GetItem(weaponIndex)?.Type == ItemType.LongRangeWeapon;
        }
        public static Spell GetCastSpell(uint actionParameter) => (Spell)((actionParameter >> 11) & 0xffff);
        public static uint GetBlinkCharacterPosition(uint actionParameter) => (actionParameter >> 27) & 0x1f;
        static void GetCastSpellInformation(uint actionParameter, out uint targetRowOrTile, out Spell spell,
            out uint? itemSlotIndex, out bool equippedItem)
        {
            spell = (Spell)((actionParameter >> 11) & 0xffff);
            itemSlotIndex = (actionParameter >> 5) & 0x1f;
            equippedItem = ((actionParameter >> 10) & 0x01) != 0;
            targetRowOrTile = actionParameter & 0x1f;

            if (itemSlotIndex == 0x1f)
                itemSlotIndex = null;
        }
        public bool IsSelfSpell(PartyMember caster, uint actionParameter) =>
            SpellInfos.Entries[GetCastSpell(actionParameter)].Target == SpellTarget.SingleFriend &&
                GetTargetTileOrRowFromParameter(actionParameter) == GetSlotFromCharacter(caster);
        public static bool IsCastFromItem(uint actionParameter) => GetCastItemSlot(actionParameter) != 0x1f;
        public static uint GetCastItemSlot(uint actionParameter) => (actionParameter >> 5) & 0x1f;
        static void GetAttackFollowUpInformation(uint actionParameter, out uint targetTile, out uint damage,
            out AttackResult attackResult, out AttackActionFlags flags)
        {
            damage = (actionParameter >> 8) & 0x0000ffff;
            targetTile = actionParameter & 0x1f;
            attackResult = (AttackResult)((actionParameter >> 5) & 0x7);
            flags = (AttackActionFlags)(actionParameter & 0xff000000);
        }

        enum AttackResult
        {
            Damage,
            Failed, // Chance depending on attackers ATT skill
            NoDamage, // Chance depending on ATK / DEF
            Missed, // Target moved
            Blocked, // Parry
            Protected, // Magic protection level
            Petrified, // Petrified monsters can't be damaged
            CriticalHit
        }

        AttackResult ProcessAttack(Character attacker, int attackedSlot, out int damage, out bool abortAttacking)
        {
            damage = 0;
            abortAttacking = false;

            if (battleField[attackedSlot] == null)
            {
                abortAttacking = true;
                return AttackResult.Missed;
            }

            var target = GetCharacterAt(attackedSlot);

            if (target.Conditions.HasFlag(Condition.Petrified))
            {
                abortAttacking = true;
                return AttackResult.Petrified;
            }

            bool godMode = attacker is PartyMember && game.Godmode;

            if (!godMode && attacker.MagicAttack >= 0 && target.MagicDefense > attacker.MagicAttack)
            {
                abortAttacking = true;
                return AttackResult.Protected;
            }

            long hitChance = attacker.Skills[Skill.Attack].TotalCurrentValue;

            if (attacker is Monster && foreseeAttack)
                hitChance -= 25;

			if (!godMode && game.RollDice100() > hitChance)
                return AttackResult.Failed;

            if (protectedCharacters.Contains(target))
				return AttackResult.Blocked;

			if (game.RollDice100() < attacker.Skills[Skill.CriticalHit].TotalCurrentValue)
            {
                if (!(target is Monster monster) || !monster.BattleFlags.HasFlag(BattleFlags.Boss))
                {
                    damage = (int)target.HitPoints.CurrentValue;
                    return AttackResult.CriticalHit;
                }
            }

            damage = CalculatePhysicalDamage(attacker, target);

			if (target is Monster monsterTarget && weakenedMonsters.Contains(monsterTarget))
			{
				int bonus = monsterTarget.BattleFlags.HasFlag(BattleFlags.Boss) ? 15 : 30;
				damage = Math.Min((100 + bonus) * damage / 100, (int)monsterTarget.HitPoints.CurrentValue);
			}

			if (damage <= 0)
            {
                if (!godMode)
                {
                    damage = 0;
                    return AttackResult.NoDamage;
                }
                else
                {
                    damage = 1;
                }
            }

            // Note: Monsters can't parry.
            if (target is PartyMember partyMember && parryingPlayers.Contains(partyMember) &&
                game.RollDice100() < partyMember.Skills[Skill.Parry].TotalCurrentValue)
                return AttackResult.Blocked;

            return AttackResult.Damage;
        }

        int CalculatePhysicalDamage(Character attacker, Character target)
        {
            int attackDamage = attacker.BaseAttackDamage + attacker.BonusAttackDamage;
            if (attacker is PartyMember)
                attackDamage = game.AdjustAttackForNotUsedAmmunition(attacker, attackDamage);
            attackDamage += (int)target.Attributes[Attribute.Strength].TotalCurrentValue / 25;
            int defense = target.BaseDefense + target.BonusDefense + (int)target.Attributes[Attribute.Stamina].TotalCurrentValue / 25;

            if (attackDamage > 0)
                attackDamage = (attackDamage * (100 + (int)game.CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
            if (defense > 0)
                defense = (defense * (100 + (int)game.CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;

            return (game.RandomInt(50, 100) * attackDamage) / 100 - (game.RandomInt(50, 100) * defense) / 100;
        }

        uint CalculateSpellDamage(Character caster, Character target, uint baseDamage, uint variableDamage)
        {
            // Note: In contrast to physical attacks this should always deal at least 1 damage
            return Math.Max(1, variableDamage == 0 ? baseDamage : baseDamage + (uint)game.RandomInt(0, (int)variableDamage));
        }
    }

    internal static class BattleActionExtensions
    {
        public static MonsterAnimationType? ToAnimationType(this Battle.BattleActionType battleAction) => battleAction switch
        {
            Battle.BattleActionType.Move => MonsterAnimationType.Move,
            Battle.BattleActionType.Attack => MonsterAnimationType.CloseRangedAttack,
            Battle.BattleActionType.CastSpell => MonsterAnimationType.Cast,
            _ => null
        };

        public static UIGraphic? ToStatusGraphic(this Battle.BattleActionType battleAction, uint parameter = 0, IItemManager itemManager = null) => battleAction switch
        {
            Battle.BattleActionType.Move => UIGraphic.StatusMove,
            Battle.BattleActionType.Attack => Battle.IsLongRangedAttack(parameter, itemManager) ? UIGraphic.StatusRangeAttack : UIGraphic.StatusAttack,
            Battle.BattleActionType.CastSpell => Battle.IsCastFromItem(parameter) ? UIGraphic.StatusUseItem : UIGraphic.StatusUseMagic,
            Battle.BattleActionType.Flee => UIGraphic.StatusFlee,
            Battle.BattleActionType.Parry => UIGraphic.StatusDefend,
            _ => null
        };
    }
}
