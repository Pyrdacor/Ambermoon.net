using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = Ambermoon.Data.Attribute;

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

    // TODO: Check reset later (e.g. when loading while in battle)
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
            /// to move forward this is done as the first action in a battle
            /// round even if a party member is the first actor in the round.
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
            /// 
            /// TODO: If someone dies, call CharacterDied and remove it from the battle field
            /// </summary>
            Attack,
            /// <summary>
            /// No parameter
            /// 
            /// This is not used as a real action and it is only available for party members.
            /// Each player who picks this action will get a chance equal to his Parry
            /// ability to block physical attacks. This is only checked if the attack did
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
            /// 
            /// TODO: Can support spells miss? If not for those spells the
            ///       parameter should be the monster/partymember index instead.
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
            WeaponBreak,
            ArmorBreak,
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
        readonly Queue<BattleAction> roundBattleActions = new Queue<BattleAction>();
        readonly Character[] battleField = new Character[6 * 5];
        readonly List<PartyMember> parryingPlayers = new List<PartyMember>(Game.MaxPartyMembers);
        readonly List<Character> fledCharacters = new List<Character>();
        readonly Dictionary<int, int> monsterSizeDisplayLayerMapping = new Dictionary<int, int>();
        uint? animationStartTicks = null;
        Monster currentlyAnimatedMonster = null;
        BattleAnimation currentBattleAnimation = null;
        bool idleAnimationRunning = false;
        uint nextIdleAnimationTicks = 0;
        List<BattleAnimation> effectAnimations = null;
        SpellAnimation currentSpellAnimation = null;
        readonly Dictionary<ActiveSpellType, ILayerSprite> activeSpellSprites = new Dictionary<ActiveSpellType, ILayerSprite>();
        readonly Dictionary<ActiveSpellType, IColoredRect> activeSpellDurationBackgrounds = new Dictionary<ActiveSpellType, IColoredRect>();
        readonly Dictionary<ActiveSpellType, Bar> activeSpellDurationBars = new Dictionary<ActiveSpellType, Bar>();
        readonly List<KeyValuePair<uint, ItemSlotFlags>> brokenItems = new List<KeyValuePair<uint, ItemSlotFlags>>();
        readonly List<Monster> droppedWeaponMonsters = new List<Monster>();
        readonly uint[] totalPlayerDamage = new uint[Game.MaxPartyMembers];
        readonly uint[] numSuccessfulPlayerHits = new uint[Game.MaxPartyMembers];
        readonly uint[] averagePlayerDamage = new uint[Game.MaxPartyMembers];
        readonly List<uint> monsterMorale = new List<uint>();
        readonly List<uint> totalMonsterDamage = new List<uint>();
        readonly List<uint> numSuccessfulMonsterHits = new List<uint>();
        readonly List<uint> averageMonsterDamage = new List<uint>();
        uint relativeDamageEfficiency = 0;
        bool showMonsterLP = false;
        readonly bool needsClickForNextAction;
        public bool ReadyForNextAction { get; private set; } = false;
        public bool WaitForClick { get; set; } = false;
        public bool SkipNextBattleFieldClick { get; private set; } = false;

        public event Action RoundFinished;
        public event Action<Character> CharacterDied;
        public event Action<Character, uint, uint> CharacterMoved;
        public event Action<Game.BattleEndInfo> BattleEnded;
        public event Action<BattleAction> ActionCompleted;
        public event Action<PartyMember> PlayerWeaponBroke;
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
        public bool CanMoveForward => !battleField.Skip(12).Take(6).Any(c => c != null) && // middle row empty
            !battleField.Skip(18).Take(6).Any(c => c?.Type == CharacterType.Monster); // and no monster in front row

        public Battle(Game game, Layout layout, PartyMember[] partyMembers, MonsterGroup monsterGroup,
            Dictionary<int, BattleAnimation> monsterBattleAnimations, bool needsClickForNextAction)
        {
            this.game = game;
            this.layout = layout;
            this.partyMembers = partyMembers;
            this.needsClickForNextAction = needsClickForNextAction;

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

            SetupNextIdleAnimation(0);
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
            animationStartTicks = null;
            idleAnimationRunning = false;
            currentlyAnimatedMonster = null;
            layout.ResetMonsterCombatSprite(monster);
        }

        void SetupNextIdleAnimation(uint battleTicks)
        {
            // TODO: adjust to work like original
            // TODO: in original idle animations can also occur in active battle round while no other animation is played
            nextIdleAnimationTicks = battleTicks + (uint)game.RandomInt(1, 16) * Game.TicksPerSecond / 4;
        }

        public void Update(uint battleTicks)
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

            if (idleAnimationRunning)
            {
                var animationTicks = battleTicks - animationStartTicks.Value;

                // Note: Idle animations use the move animation.
                if (layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Move, animationTicks, battleTicks)?.Finished != false)
                {
                    animationStartTicks = null;
                    idleAnimationRunning = false;
                    if (currentlyAnimatedMonster != null)
                        layout.ResetMonsterCombatSprite(currentlyAnimatedMonster);
                    SetupNextIdleAnimation(battleTicks);
                }
            }
            else if (!RoundActive)
            {
                if (battleTicks >= nextIdleAnimationTicks)
                {
                    var monsters = Monsters.ToList();

                    if (monsters.Count != 0)
                    {
                        int index = game.RandomInt(0, monsters.Count - 1);
                        animationStartTicks = battleTicks;
                        idleAnimationRunning = true;
                        currentlyAnimatedMonster = monsters[index];
                        layout.UpdateMonsterCombatSprite(currentlyAnimatedMonster, MonsterAnimationType.Move, 0, battleTicks);
                    }
                }
            }

            if (ReadyForNextAction && (!needsClickForNextAction || !WaitForClick))
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
                RoundActive = false;
                if (showMonsterLP)
                {
                    foreach (var monster in Monsters)
                    {
                        layout.GetMonsterBattleFieldTooltip(monster).Text =
                            $"{monster.HitPoints.CurrentValue}/{monster.HitPoints.TotalMaxValue}^{monster.Name}";
                    }
                }
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
            SkipNextBattleFieldClick = false;

            if (!WaitForClick)
                return;

            WaitForClick = false;
            SkipNextBattleFieldClick = true;

            if (RoundActive)
            {
                if (ReadyForNextAction && needsClickForNextAction)
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

        /// <summary>
        /// Starts a new battle round.
        /// </summary>
        /// <param name="playerBattleActions">Battle actions for party members 1-6.</param>
        /// <param name="battleTicks">Battle ticks when starting the round.</param>
        internal void StartRound(PlayerBattleAction[] playerBattleActions, uint battleTicks)
        {
            // Recalculate the RDE value each round
            var partyDamage = Util.Limit(1, (uint)averagePlayerDamage.Where((d, i) => partyMembers[i] != null && battleField.Contains(partyMembers[i])).Sum(x => x), 0x7fff);
            var monsterDamage = Util.Limit(1, (uint)averageMonsterDamage.Where((d, i) => battleField.Contains(initialMonsters[i])).Sum(x => x), 0x7fff);
            relativeDamageEfficiency = Math.Min(partyDamage * 50 / monsterDamage, 100);

            var roundActors = battleField
                .Where(f => f != null)
                .OrderByDescending(c => c.Attributes[Attribute.Speed].TotalCurrentValue)
                .ToList();
            parryingPlayers.Clear();
            bool monstersAdvance = false;

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

            // This is added in addition to normal monster actions directly
            // TODO: removed for now, check later when this is used (it seems awkward at the moment, maybe only later in battle?)
            /*if (CanMoveForward)
            {
                var firstMonster = roundActors.FirstOrDefault(c => c.Type == CharacterType.Monster && c.Alive);
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
                monstersAdvance = true;
            }*/

            var forbiddenMoveSpots = playerBattleActions.Where(a => a != null && a.BattleAction == BattleActionType.Move)
                .Select(a => (int)GetTargetTileOrRowFromParameter(a.Parameter)).ToList();
            var forbiddenMonsterMoveSpots = new List<int>();

            foreach (var roundActor in roundActors)
            {
                if (roundActor is Monster monster)
                {
                    AddMonsterActions(monster, ref monstersAdvance, forbiddenMonsterMoveSpots);
                }
                else
                {
                    var partyMember = roundActor as PartyMember;
                    int playerIndex = partyMembers.ToList().IndexOf(partyMember);
                    var playerAction = playerBattleActions[playerIndex];

                    if (partyMember.Ailments.HasFlag(Ailment.Panic))
                    {
                        PickPanicAction(partyMember, playerAction, forbiddenMoveSpots);
                    }
                    else if (partyMember.Ailments.HasFlag(Ailment.Crazy))
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

                    int numActions = playerAction.BattleAction == BattleActionType.Attack
                        ? partyMember.AttacksPerRound : 1;

                    for (int i = 0; i < numActions; ++i)
                    {
                        roundBattleActions.Enqueue(new BattleAction
                        {
                            Character = partyMember,
                            Action = BattleActionType.DisplayActionText,
                            ActionParameter = 0
                        });
                        roundBattleActions.Enqueue(new BattleAction
                        {
                            Character = partyMember,
                            Action = playerAction.BattleAction,
                            ActionParameter = playerAction.Parameter
                        });
                        if (playerAction.BattleAction == BattleActionType.Attack)
                        {
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = partyMember,
                                Action = BattleActionType.WeaponBreak,
                                ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter))
                            });
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = partyMember,
                                Action = BattleActionType.ArmorBreak,
                                ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter))
                            });
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = partyMember,
                                Action = BattleActionType.LastAmmo,
                                ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter))
                            });
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = partyMember,
                                Action = BattleActionType.Hurt,
                                ActionParameter = CreateHurtParameter(GetTargetTileOrRowFromParameter(playerAction.Parameter))
                            });
                        }
                    }
                }
            }

            RoundActive = true;
            NextAction(battleTicks);
        }

        void AddMonsterActions(Monster monster, ref bool monstersAdvance, List<int> forbiddenMonsterMoveSpots)
        {
            bool wantsToFlee = MonsterWantsToFlee(monster);

            if (wantsToFlee && monstersAdvance && roundBattleActions.Count > 1)
            {
                // The second action might be a monster advance.
                // Remove this if any monster wants to flee.
                var secondAction = roundBattleActions.Skip(1).First();

                if (secondAction.Character.Type == CharacterType.Monster &&
                    secondAction.Action == BattleActionType.MoveGroupForward)
                {
                    // Remove first two actions (display about monster advance and the actual advance).
                    roundBattleActions.Dequeue();
                    roundBattleActions.Dequeue();
                }

                monstersAdvance = false;
            }

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

        void KillMonster(PartyMember attacker, Character target)
        {
            CharacterDied?.Invoke(target);
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

        void EndBattleCleanup()
        {
            foreach (var battleFieldDamageText in battleFieldDamageTexts)
                battleFieldDamageText.Value?.Delete();
            battleFieldDamageTexts.Clear();
            currentSpellAnimation?.Destroy();
            currentSpellAnimation = null;
        }

        void KillPlayer(Character target)
        {
            CharacterDied?.Invoke(target);

            if (!partyMembers.Any(p => p != null && p.Alive && p.Ailments.CanFight()))
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

        void RunBattleAction(BattleAction battleAction, uint battleTicks)
        {
            game.CursorType = CursorType.Sword;

            void ActionFinished(bool needClickAfterwards = true)
            {
                ActionCompleted?.Invoke(battleAction);
                if (needsClickForNextAction && needClickAfterwards)
                    WaitForClick = true;
                ReadyForNextAction = true;
            }

            switch (battleAction.Action)
            {
                case BattleActionType.DropWeapon:
                {
                    // Note: This only displays the message. The PickMonsterAction method will drop/switch ranged weapons automatically.
                    layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageHasDroppedWeapon, Data.Enumerations.Color.BrightGray);
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () => ActionFinished());
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
                            GetAttackInformation(next.ActionParameter, out uint targetTile, out uint weaponIndex, out uint _);
                            var weapon = weaponIndex == 0 ? null : game.ItemManager.GetItem(weaponIndex);
                            var target = battleField[targetTile];

                            if (target == null)
                            {
                                text = next.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget;
                                foreach (var action in roundBattleActions.Where(a => a.Character == next.Character))
                                    action.Skip = true;
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
                            string spellName = game.DataNameProvider.GetSpellname(spell);

                            if (itemSlotIndex != null)
                            {
                                var itemSlot = equippedItem
                                    ? battleAction.Character.Equipment.Slots[(EquipmentSlot)itemSlotIndex.Value]
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
                    layout.SetBattleMessage(text, next.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () => ActionFinished());
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
                            battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                        moveFailed = true;
                    }
                    else if (battleField[battleAction.ActionParameter & 0x1f] != null)
                    {
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageWayWasBlocked,
                            battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                        moveFailed = true;
                    }

                    if (moveFailed)
                    {
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                        {
                            ActionFinished();
                        });
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
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                        {
                            EndMove();
                        });
                    }
                    return;
                }
                case BattleActionType.MoveGroupForward:
                    // No parameter
                    layout.SetBattleMessage(null);
                    // TODO
                    break;
                case BattleActionType.Attack:
                {
                    GetAttackInformation(battleAction.ActionParameter, out uint targetTile, out uint weaponIndex, out uint ammoIndex);
                    var attackResult = ProcessAttack(battleAction.Character, (int)targetTile, out int damage, out bool abort);
                    var textColor = battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
                    var target = GetCharacterAt((int)targetTile);
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
                            layout.SetBattleMessage(game.DataNameProvider.BattleMessageCannotDamagePetrifiedMonsters, textColor);
                        else
                            layout.SetBattleMessage(null);
                        ActionFinished(true);
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
                    void SkipAllFollowingAttacks()
                    {
                        bool foundNextDisplayAction = false;
                        foreach (var action in roundBattleActions.Where(a => a.Character == battleAction.Character))
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
                    // Check for last ammunition consumption
                    if (weapon?.Type == ItemType.LongRangeWeapon)
                    {
                        void LastAmmoUsed()
                        {
                            followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.LastAmmo);
                            SkipAllFollowingAttacks();
                        }
                        var attacker = battleAction.Character;
                        if (weapon.UsedAmmunitionType != AmmunitionType.None)
                        {
                            var slot = attacker.Inventory.Slots.FirstOrDefault(slot => slot.ItemIndex == ammoIndex && slot.Amount > 0);

                            if (slot != null)
                            {
                                slot.Remove(1);

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

                                if (ammoSlot.Amount == 0)
                                {
                                    LastAmmoUsed();
                                }
                            }
                        }
                    }
                    // TODO: they break quiet often at the moment
                    // Check weapon or armor breakage
                    if (attackResult != AttackResult.Missed)
                    {
                        if (weapon != null && game.RollDice100() < weapon.BreakChance)
                        {
                            followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakWeapon);
                            SkipAllFollowingAttacks();
                        }

                        var enemyArmorIndex = target.Equipment.Slots[EquipmentSlot.Body].ItemIndex;

                        if (enemyArmorIndex != 0)
                        {
                            var enemyArmor = game.ItemManager.GetItem(enemyArmorIndex);

                            if (game.RollDice100() < enemyArmor.BreakChance)
                                followAction.ActionParameter = UpdateAttackFollowActionParameter(followAction.ActionParameter, AttackActionFlags.BreakArmor);
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
                        ActionFinished(true);
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
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Attack), Game.TicksPerSecond / 6,
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
                            PlayBattleEffectAnimation(BattleEffect.PlayerAtack, targetTile, battleTicks, ShowAttackMessage);
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
                        battleAction.Character.SpellPoints.CurrentValue = Math.Max(0, battleAction.Character.SpellPoints.CurrentValue - spellInfo.SP);

                        if (battleAction.Character is PartyMember castingPartyMember)
                            layout.FillCharacterBars(castingPartyMember);
                    }

                    if (!CheckSpellCast(battleAction.Character, spellInfo))
                    {
                        EndCast(true);
                        return;
                    }

                    void EndCast(bool needClickAfterwards = false)
                    {
                        if (itemSlotIndex != null)
                        {
                            // Note: It will always be a party member as monsters can't use item spells.
                            var itemSlot = equippedItem
                                ? battleAction.Character.Equipment.Slots[(EquipmentSlot)itemSlotIndex.Value]
                                : battleAction.Character.Inventory.Slots[itemSlotIndex.Value];
                            layout.ReduceItemCharge(itemSlot, false);
                        }

                        if (currentSpellAnimation != null)
                        {
                            currentSpellAnimation.PostCast(() =>
                            {
                                currentSpellAnimation?.Destroy();
                                currentSpellAnimation = null;
                                ActionFinished(needClickAfterwards);
                            });
                        }
                        else
                        {
                            ActionFinished(needClickAfterwards);
                        }
                    }

                    switch (spellInfo.Target)
                    {
                        case SpellTarget.SingleEnemy:
                            if (GetCharacterAt((int)targetRowOrTile) == null)
                            {
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                    battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                                game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                                {
                                    EndCast();
                                });
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

                        game.CurrentSpellTarget = target;
                        int position = GetCharacterPosition(target);
                        bool failed = false;
                        bool spellBlocked = false;
                        // Note: Some spells like Fireball or Whirlwind move to the target.
                        currentSpellAnimation.MoveTo(position, (ticks, playHurt, finish) =>
                        {
                            if (finish && spellBlocked)
                            {
                                ShowSpellFailMessage(battleAction.Character, spellInfo, target.Name + game.DataNameProvider.BattleMessageDeflectedSpell, needsClickForNextAction ? finishAction : null);
                                PlayBattleEffectAnimation(BattleEffect.BlockSpell, (uint)GetSlotFromCharacter(target), game.CurrentBattleTicks, needsClickForNextAction ? null : finishAction);
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
                                else if (!CheckSpell(battleAction.Character, target, spell, blocked =>
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
                                    if (currentBattleAnimation == null)
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
                        // TODO: If this is a player we should decrease item charge if item was used. For monster items as well?
                        StartCasting();
                    }

                    void StartCasting()
                    {
                        currentSpellAnimation.Play(() =>
                        {
                            switch (spellInfo.Target)
                            {
                                case SpellTarget.None:
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
                                            battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () => EndCast());
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
                                            battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () => EndCast());
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
                                            battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                                        {
                                            EndCast();
                                        });
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
                        var textColor = battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
                        var weaponSlot = battleAction.Character.Equipment.Slots[EquipmentSlot.RightHand];
                        var itemIndex = weaponSlot.ItemIndex;
                        game.EquipmentRemoved(battleAction.Character, itemIndex, 1, weaponSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
                        brokenItems.Add(KeyValuePair.Create(itemIndex, weaponSlot.Flags));
                        weaponSlot.Clear();
                        var weapon = game.ItemManager.GetItem(itemIndex);
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
                                    game.EquipmentAdded(weaponSlot.ItemIndex, 1, weaponSlot.Flags.HasFlag(ItemSlotFlags.Cursed), monster);
                                }
                            }

                            if (monster.BaseAttack == 0)
                                monsterMorale[initialMonsters.IndexOf(monster)] /= 2;
                        }
                        ActionFinished(true);
                    }
                    else
                    {
                        ActionFinished(false);
                    }
                    return;
                }
                case BattleActionType.ArmorBreak:
                {
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out _, out var flags);
                    var target = GetCharacterAt((int)tile);
                    var nextAction = roundBattleActions.Peek();
                    nextAction.ActionParameter = battleAction.ActionParameter;
                    if (flags.HasFlag(AttackActionFlags.BreakArmor))
                    {
                        var textColor = battleAction.Character.Type == CharacterType.PartyMember ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
                        var armorSlot = target.Equipment.Slots[EquipmentSlot.Body];
                        var itemIndex = armorSlot.ItemIndex;
                        game.EquipmentRemoved(target, itemIndex, 1, armorSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
                        brokenItems.Add(KeyValuePair.Create(itemIndex, armorSlot.Flags));
                        armorSlot.Clear();
                        layout.SetBattleMessage(target.Name + string.Format(game.DataNameProvider.BattleMessageWasBroken, game.ItemManager.GetItem(itemIndex).Name), textColor);
                        ActionFinished(true);
                    }
                    else
                    {
                        ActionFinished(false);
                    }
                    return;
                }
                case BattleActionType.LastAmmo:
                {
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out _, out var flags);
                    var nextAction = roundBattleActions.Peek();
                    nextAction.ActionParameter = battleAction.ActionParameter;
                    if (flags.HasFlag(AttackActionFlags.LastAmmo))
                    {
                        var textColor = battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageUsedLastAmmunition, textColor);
                        if (battleAction.Character is Monster monster)
                            droppedWeaponMonsters.Add(monster);
                        ActionFinished(true);
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
                    var textColor = battleAction.Character.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
                    GetAttackFollowUpInformation(battleAction.ActionParameter, out uint tile, out uint damage, out var attackResult, out var flags);
                    var target = GetCharacterAt((int)tile);
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
                    }
                    ShowBattleFieldDamage((int)tile, damage);
                    if (target.Ailments.HasFlag(Ailment.Sleep))
                        game.RemoveAilment(Ailment.Sleep, target);
                    if (!game.Godmode || target is Monster)
                        target.Damage(damage);
                    return;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid battle action.");
            }

            throw new AmbermoonException(ExceptionScope.Application, "Not processed battle action.");
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
            var layer = layout.RenderView.GetLayer(Layer.Text);
            var text = layout.RenderView.TextProcessor.CreateText(damage > 999 ? "***" : $"{damage:000}");
            var area = Global.BattleFieldSlotArea(tile).CreateModified(-5, 9, 12, 0);
            var damageText = layout.RenderView.RenderTextFactory.CreateDigits(layer, text, Data.Enumerations.Color.Red, false, area, TextAlign.Center);
            var colors = game.GetTextAnimationColors();
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
            if (game.RollDice100() >= caster.Abilities[Ability.UseMagic].TotalCurrentValue)
            {
                layout.SetBattleMessage(caster.Name + game.DataNameProvider.SpellFailed,
                    caster.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer);
                return false;
            }

            return true;
        }

        void ShowSpellFailMessage(Character caster, SpellInfo spellInfo, string message, Action finishAction)
        {
            var color = caster.Type == CharacterType.Monster ? Data.Enumerations.Color.BattleMonster : Data.Enumerations.Color.BattlePlayer;
            var delay = TimeSpan.FromMilliseconds(700);

            if (needsClickForNextAction && !spellInfo.Target.TargetsMultipleEnemies())
            {
                game.SetBattleMessageWithClick(message, color, finishAction, delay);
            }
            else
            {
                layout.SetBattleMessage(message, color);
                game.AddTimedEvent(delay, () =>
                {
                    layout.SetBattleMessage(null);
                    finishAction?.Invoke();
                });
            }
        }

        bool CheckSpell(Character caster, Character target, Spell spell, Action<bool> failAction, bool playBlocked)
        {
            var spellInfo = SpellInfos.Entries[spell];
            void Fail() => failAction?.Invoke(false);
            void ShowFailMessage(string message, Action finishAction) => ShowSpellFailMessage(caster, spellInfo, message, finishAction);

            if (target.Type != caster.Type)
            {
                if (target.SpellTypeImmunity.HasFlag((SpellTypeImmunity)spellInfo.SpellType))
                {
                    ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageImmuneToSpellType, Fail);
                    return false;
                }

                if (target.IsImmuneToSpell(spell, out bool silent))
                {
                    if (silent)
                        Fail();
                    else
                        ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageImmuneToSpell, Fail);
                    return false;
                }

                uint antiMagicBuffValue = game.CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.AntiMagic);

                if (game.RollDice100() < (int)(target.Attributes[Attribute.AntiMagic].TotalCurrentValue + antiMagicBuffValue))
                {
                    Action blockedAction = () => failAction?.Invoke(true);
                    // Blocked
                    if (playBlocked)
                    {
                        ShowFailMessage(target.Name + game.DataNameProvider.BattleMessageDeflectedSpell, needsClickForNextAction ? blockedAction : null);
                        PlayBattleEffectAnimation(BattleEffect.BlockSpell, (uint)GetSlotFromCharacter(target), game.CurrentBattleTicks, needsClickForNextAction ? null : blockedAction);
                    }
                    else
                    {
                        blockedAction();
                    }
                    return false;
                }
            }

            if (target.Ailments.HasFlag(Ailment.Petrified) && spell.FailsAgainstPetrifiedEnemy())
            {
                // Note: In original there is no message in this case but I think
                //       it's better to show the reason.
                ShowFailMessage(game.DataNameProvider.BattleMessageCannotDamagePetrifiedMonsters, Fail);
                return false;
            }

            return true;
        }

        void RemoveAilment(Ailment ailment, Character target)
        {
            // Healing spells or potions.
            // Sleep can be removed by attacking as well.
            target.Ailments &= ~ailment;

            if (target is PartyMember partyMember)
            {
                game.UpdateBattleStatus(partyMember);
                layout.UpdateCharacterNameColors(game.CurrentSavegame.ActivePartyMemberSlot);
            }
        }

        void AddAilment(Ailment ailment, Character target)
        {
            target.Ailments |= ailment;

            if (target is PartyMember partyMember)
            {
                game.ShowPlayerDamage(game.SlotFromPartyMember(partyMember).Value, 0);
                game.UpdateBattleStatus(partyMember);
                layout.UpdateCharacterNameColors(game.CurrentSavegame.ActivePartyMemberSlot);
            }

            if (!ailment.CanSelect()) // disabled
            {
                foreach (var action in roundBattleActions.Where(a => a.Character == target))
                    action.Skip = true;
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

            if (attacker is PartyMember partyMember)
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
                    KillMonster(partyMember, target);
                }, GetMonsterDeathScale(target as Monster), battleFieldCopy);
            }
            else
            {
                RemoveCharacterFromBattleField(target);
                finishAction?.Invoke();
                KillPlayer(target);
            }
        }

        void ShowMonsterInfo(Monster monster, Action finishAction)
        {
            var area = new Rect(64, 38, 12 * 16, 10 * 16);
            var popup = layout.OpenPopup(area.Position, 12, 10, true, true, 225);
            area = area.CreateShrinked(16);
            int panelWidth = 12 * Global.GlyphWidth;
            // Attributes
            popup.AddText(new Rect(area.Position, new Size(panelWidth, Global.GlyphLineHeight)),
                game.DataNameProvider.AttributesHeaderString, Data.Enumerations.Color.MonsterInfoHeader, TextAlign.Center);
            var position = new Position(area.Position.X, area.Position.Y + Global.GlyphLineHeight + 1);
            foreach (var attribute in Enum.GetValues<Attribute>())
            {
                if (attribute == Attribute.Age)
                    break;

                var attributeValues = monster.Attributes[attribute];
                popup.AddText(position,
                    $"{game.DataNameProvider.GetAttributeShortName(attribute)}  {((attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}")}",
                    Data.Enumerations.Color.BrightGray);
                position.Y += Global.GlyphLineHeight;
            }
            // Abilities
            position = area.Position + new Position(panelWidth + Global.GlyphWidth, 0);
            popup.AddText(new Rect(position, new Size(panelWidth, Global.GlyphLineHeight)),
                game.DataNameProvider.AbilitiesHeaderString, Data.Enumerations.Color.MonsterInfoHeader, TextAlign.Center);
            position.Y += Global.GlyphLineHeight + 1;
            foreach (var ability in Enum.GetValues<Ability>())
            {
                var abilityValues = monster.Abilities[ability];
                popup.AddText(position,
                    $"{game.DataNameProvider.GetAbilityShortName(ability)}  {((abilityValues.TotalCurrentValue > 99 ? "**" : $"{abilityValues.TotalCurrentValue:00}") + $"%/{abilityValues.MaxValue:00}%")}",
                    Data.Enumerations.Color.BrightGray);
                position.Y += Global.GlyphLineHeight - 1;
            }
            // Data
            position.X = area.X;
            position.Y += 3;
            popup.AddText(new Rect(position, new Size(area.Width, Global.GlyphLineHeight)),
                game.DataNameProvider.DataHeaderString, Data.Enumerations.Color.MonsterInfoHeader, TextAlign.Center);
            position.Y += Global.GlyphLineHeight + 1;
            popup.AddText(position,
                string.Format(game.DataNameProvider.CharacterInfoHitPointsString, monster.HitPoints.CurrentValue, monster.HitPoints.TotalMaxValue) + " " +
                string.Format(game.DataNameProvider.CharacterInfoSpellPointsString, monster.SpellPoints.CurrentValue, monster.SpellPoints.TotalMaxValue),
                Data.Enumerations.Color.BrightGray);
            position.Y += Global.GlyphLineHeight;
            popup.AddText(position,
                string.Format(game.DataNameProvider.CharacterInfoGoldAndFoodString.Replace(" ", "      "), monster.Gold, monster.Food),
                Data.Enumerations.Color.BrightGray);
            position.Y += Global.GlyphLineHeight;
            popup.AddImage(new Rect(position.X, position.Y, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), Layer.UI, 1, 0);
            popup.AddText(position + new Position(6, 2),
                string.Format(game.DataNameProvider.CharacterInfoDamageString.Replace(' ', monster.BaseAttack < 0 ? '-' : '+'), Math.Abs(monster.BaseAttack)),
                Data.Enumerations.Color.BrightGray);
            position.X = area.X + panelWidth + Global.GlyphWidth;
            popup.AddImage(new Rect(position.X, position.Y, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), Layer.UI, 1, 0);
            popup.AddText(position + new Position(7, 2),
                string.Format(game.DataNameProvider.CharacterInfoDefenseString.Replace(' ', monster.BaseDefense < 0 ? '-' : '+'), Math.Abs(monster.BaseDefense)),
                Data.Enumerations.Color.BrightGray);
            position.X = area.X;
            position.Y += Global.GlyphLineHeight + 4;
            popup.AddText(position, $"{game.DataNameProvider.CharacterInfoAPRString.TrimEnd()}{monster.AttacksPerRound}", Data.Enumerations.Color.BrightGray);
            // Icon and level
            --position.X;
            position.Y += Global.GlyphLineHeight;
            popup.AddSunkenBox(new Rect(position, new Size(18, 18)));
            popup.AddImage(new Rect(position.X + 1, position.Y + 2, 16, 14), Graphics.BattleFieldIconOffset + (uint)Class.Monster + (uint)monster.CombatGraphicIndex - 1, Layer.UI, 2);
            popup.AddText(position + new Position(21, 5), $"{monster.Name} {monster.Level}", Data.Enumerations.Color.BrightGray);
            // Closing
            game.TrapMouse(area);
            popup.Closed += () =>
            {
                game.CursorType = CursorType.Sword;
                game.UntrapMouse();
                finishAction?.Invoke();
            };
        }

        /// <summary>
        /// The boolean argument of the finish action means: NeedsClickAfterwards
        /// </summary>
        void ApplySpellEffect(Character caster, Character target, Spell spell, uint ticks, Action finishAction, uint? targetField = null)
        {
            switch (spell)
            {
                case Spell.GhostWeapon:
                    DealDamage(25, 0);
                    return;
                case Spell.Blink:
                    game.SetBattleMessageWithClick(target.Name + game.DataNameProvider.BattleMessageHasBlinked, Data.Enumerations.Color.BattlePlayer,
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
                    RemoveCharacterFromBattleField(target);
                    if (caster is PartyMember partyMember)
                    {
                        KillMonster(partyMember, target);
                    }
                    else
                    {
                        KillPlayer(target);
                    }
                    break;
                case Spell.Lame:
                    AddAilment(Ailment.Lamed, target);
                    break;
                case Spell.Poison:
                    AddAilment(Ailment.Poisoned, target);
                    break;
                case Spell.Petrify:
                    AddAilment(Ailment.Petrified, target);
                    break;
                case Spell.CauseDisease:
                    AddAilment(Ailment.Diseased, target);
                    break;
                case Spell.CauseAging:
                    AddAilment(Ailment.Aging, target);
                    break;
                case Spell.Irritate:
                    AddAilment(Ailment.Irritated, target);
                    break;
                case Spell.CauseMadness:
                    AddAilment(Ailment.Crazy, target);
                    break;
                case Spell.Sleep:
                    AddAilment(Ailment.Sleep, target);
                    break;
                case Spell.Fear:
                    AddAilment(Ailment.Panic, target);
                    break;
                case Spell.Blind:
                    AddAilment(Ailment.Blind, target);
                    break;
                case Spell.Drug:
                    AddAilment(Ailment.Drugged, target);
                    break;
                case Spell.Mudsling:
                    // 4-8 damage
                    DealDamage(4, 4);
                    return;
                case Spell.Rockfall:
                    // 10-25 damage
                    DealDamage(10, 15);
                    return;
                case Spell.Earthslide:
                    // 8-16 damage
                    DealDamage(8, 8);
                    return;
                case Spell.Earthquake:
                    // 8-22 damage
                    DealDamage(8, 14);
                    return;
                case Spell.Winddevil:
                    // 8-16 damage
                    DealDamage(8, 8);
                    return;
                case Spell.Windhowler:
                    // 16-48 damage
                    DealDamage(16, 32);
                    return;
                case Spell.Thunderbolt:
                    // 20-32 damage
                    DealDamage(20, 12);
                    return;
                case Spell.Whirlwind:
                    // 20-35 damage
                    DealDamage(20, 15);
                    return;
                case Spell.Firebeam:
                    // 20-30 damage
                    DealDamage(20, 10);
                    return;
                case Spell.Fireball:
                    // 40-85 damage
                    DealDamage(40, 45);
                    return;
                case Spell.Firestorm:
                    // 35-65 damage
                    DealDamage(35, 30);
                    return;
                case Spell.Firepillar:
                    // 40-70 damage
                    DealDamage(40, 30);
                    return;
                case Spell.Waterfall:
                    // 32-60 damage
                    DealDamage(32, 28);
                    return;
                case Spell.Iceball:
                    // 90-180 damage
                    DealDamage(90, 90);
                    return;
                case Spell.Icestorm:
                    // 64-128 damage
                    DealDamage(64, 64);
                    return;
                case Spell.Iceshower:
                    // 128-256 damage
                    DealDamage(128, 128);
                    return;
                case Spell.MagicalProjectile:
                case Spell.MagicalArrows:
                    // Those deal half the caster level as damage.
                    DealDamage(Math.Max(1, (uint)caster.Level / 2), 0);
                    return;
                case Spell.LPStealer:
                {
                    DealDamage(caster.Level, 0);
                    caster.HitPoints.CurrentValue = Math.Min(caster.HitPoints.TotalMaxValue, caster.HitPoints.CurrentValue +
                        Math.Min(caster.Level, caster.HitPoints.TotalMaxValue - caster.HitPoints.CurrentValue));
                    if (caster is PartyMember castingMember)
                        layout.FillCharacterBars(castingMember);
                    return;
                }
                case Spell.SPStealer:
                    // TODO: what happens if a monster wants to cast a spell afterwards but has not enough SP through SP stealer anymore?
                    target.SpellPoints.CurrentValue = (uint)Math.Max(0, (int)target.SpellPoints.CurrentValue - caster.Level);
                    caster.SpellPoints.CurrentValue += Math.Min(caster.Level, caster.SpellPoints.TotalMaxValue - caster.SpellPoints.CurrentValue);
                    if (target is PartyMember targetMember)
                        layout.FillCharacterBars(targetMember);
                    else if (caster is PartyMember castingMember)
                        layout.FillCharacterBars(castingMember);
                    break;
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
                            layout.GetMonsterBattleFieldTooltip(monster).Text =
                                $"{monster.HitPoints.CurrentValue}/{monster.HitPoints.TotalMaxValue}^{monster.Name}";
                        }
                        showMonsterLP = true;
                    }
                    break;
                }
                default:
                    game.ApplySpellEffect(spell, caster, target, null, false);
                    break;
            }

            finishAction?.Invoke();

            void DealDamage(uint baseDamage, uint variableDamage)
            {
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
                uint position = (uint)GetSlotFromCharacter(target);
                PlayBattleEffectAnimation(target.Type == CharacterType.Monster ? BattleEffect.HurtMonster : BattleEffect.HurtPlayer,
                    position, ticks, () =>
                    {
                        if (target is PartyMember partyMember)
                        {
                            game.ShowPlayerDamage(game.SlotFromPartyMember(partyMember).Value,
                                Math.Min(damage, partyMember.HitPoints.CurrentValue));
                        }
                        if (target.Ailments.HasFlag(Ailment.Sleep))
                            RemoveAilment(Ailment.Sleep, target);
                        if (!game.Godmode || target is Monster)
                            target.Damage(damage);
                        EndHurt();
                    }
                );
                ShowBattleFieldDamage(GetSlotFromCharacter(target), damage);
            }
        }

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

                for (int column = Math.Max(0, playerColumn - 1); column <= Math.Min(5, playerColumn + 1); ++column)
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
                        throw new AmbermoonException(ExceptionScope.Application, "No move spot found for panic player."); // should never happen

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
                if (partyMember.Ailments.CanAttack() &&
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
                    playerBattleAction.BattleAction = BattleActionType.Move;
                    uint moveSpot = GetRandomMoveSpot(position, partyMember, forbiddenMoveSpots);
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
            if (monster.Ailments.HasFlag(Ailment.Crazy))
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
                    if (canCast && monster.HasAnySpell() && monster.Ailments.CanCastSpell() && CanCastAnySpell(monster))
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
                                game.EquipmentAdded(weaponSlot.ItemIndex, 1, weaponSlot.Flags.HasFlag(ItemSlotFlags.Cursed), monster);
                            }
                            else
                            {
                                // Just drop the ranged weapon
                                var emptyInventorySlot = monster.Inventory.Slots.FirstOrDefault(s => s.Empty);
                                if (emptyInventorySlot != null)
                                    emptyInventorySlot.Replace(weaponSlot);
                                weaponSlot.Clear();

                                if (monster.BaseAttack == 0)
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
                        monster.Ailments.CanAttack() &&
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
                    if (monster.Ailments.CanAttack() && AttackSpotAvailable(position, monster, false))
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
                return sp >= spellInfo.SP &&
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
            int moveRange = monster.Attributes[Data.Attribute.Speed].TotalCurrentValue >= 80 ? 2 : 1;
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

                // Prefer moving straight down if not on left or right column
                if (currentColumn != 0 && currentColumn != 5)
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
            if (monster.MonsterFlags.HasFlag(MonsterFlags.Boss))
                return false;

            if (monster.Ailments.HasFlag(Ailment.Panic))
                return true;

            if (monster.Ailments.HasFlag(Ailment.Crazy))
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
            bool mad = monster.Ailments.HasFlag(Ailment.Crazy);
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
                return (uint)possiblePositions.Single().Key;

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
                    var maxPlayerDamage = averagePlayerDamage.Max();
                    uint getPrio(uint damage) => maxPlayerDamage == 0 ? 100 : damage * 100 / maxPlayerDamage;
                    var maxDamagePlayers = averagePlayerDamage.Select((d, i) => new { Damage = d, Player = partyMembers[i] })
                        .Where(x => x.Damage == maxPlayerDamage && x.Player?.Alive == true)
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
                    if (averagePrio >= 75 && spells.Any(s => SpellInfos.Entries[s].Target == SpellTarget.AllEnemies))
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

            effectAnimation.AnimationFinished += EffectAnimationFinished;

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
            BreakWeapon = 0x10000000,
            BreakArmor = 0x20000000,
            LastAmmo = 0x40000000
        }
        // Lowest 5 bits: Tile index (0-29) where a character is hurt
        // Rest: Damage
        public static uint CreateHurtParameter(uint targetTile) => (targetTile & 0x1f);
        static uint UpdateHurtParameter(uint hurtParameter, uint damage, AttackResult attackResult) =>
            (hurtParameter & 0xe000001f) | ((damage & 0x000fffff) << 8) | ((uint)attackResult << 5);
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
            damage = (actionParameter >> 8) & 0x000fffff;
            targetTile = actionParameter & 0x1f;
            attackResult = (AttackResult)((actionParameter >> 5) & 0x7);
            flags = (AttackActionFlags)(actionParameter & 0xf0000000);
        }

        enum AttackResult
        {
            Damage,
            Failed, // Chance depending on attackers ATT ability
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

            if (target.Ailments.HasFlag(Ailment.Petrified))
            {
                abortAttacking = true;
                return AttackResult.Petrified;
            }

            if (attacker.MagicAttack >= 0 && target.MagicDefense > attacker.MagicAttack)
            {
                abortAttacking = true;
                return AttackResult.Protected;
            }

            if (game.RollDice100() > attacker.Abilities[Ability.Attack].TotalCurrentValue)
                return AttackResult.Failed;

            if (game.RollDice100() < attacker.Abilities[Ability.CriticalHit].TotalCurrentValue)
            {
                if (!(target is Monster monster) || !monster.MonsterFlags.HasFlag(MonsterFlags.Boss))
                {
                    damage = (int)target.HitPoints.CurrentValue;
                    return AttackResult.CriticalHit;
                }
            }

            damage = CalculatePhysicalDamage(attacker, target);

            if (damage <= 0)
            {
                damage = 0;
                return AttackResult.NoDamage;
            }

            // TODO: can monsters parry?
            if (target is PartyMember partyMember && parryingPlayers.Contains(partyMember) &&
                game.RollDice100() < partyMember.Abilities[Ability.Parry].TotalCurrentValue)
                return AttackResult.Blocked;

            return AttackResult.Damage;
        }

        int CalculatePhysicalDamage(Character attacker, Character target)
        {
            return Math.Min((int)target.HitPoints.CurrentValue,
                attacker.BaseAttack + game.RandomInt(0, attacker.VariableAttack)
                - (target.BaseDefense + game.RandomInt(0, target.VariableDefense) + (int)target.Attributes[Attribute.Stamina].TotalCurrentValue / 25));
        }

        uint CalculateSpellDamage(Character caster, Character target, uint baseDamage, uint variableDamage)
        {
            // Note: In contrast to physical attacks this should always deal at least 1 damage
            return Math.Min(target.HitPoints.CurrentValue, baseDamage + (uint)game.RandomInt(0, (int)variableDamage));
        }
    }

    internal static class BattleActionExtensions
    {
        public static MonsterAnimationType? ToAnimationType(this Battle.BattleActionType battleAction) => battleAction switch
        {
            Battle.BattleActionType.Move => MonsterAnimationType.Move,
            Battle.BattleActionType.Attack => MonsterAnimationType.Attack,
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
