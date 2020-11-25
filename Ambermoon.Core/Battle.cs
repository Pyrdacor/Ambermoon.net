using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal static class CharacterBattleExtensions
    {
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
                hasAmmo = ammoSlot?.ItemIndex != null && ammoSlot?.ItemIndex != 0 && ammoSlot?.Amount > 0;

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
    // TODO: monsters should not pick a move spot where another monster is going to
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
            Hurt
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
        uint? animationStartTicks = null;
        Monster currentlyAnimatedMonster = null;
        BattleAnimation currentBattleAnimation = null;
        bool idleAnimationRunning = false;
        uint nextIdleAnimationTicks = 0;
        List<BattleAnimation> effectAnimations = null;
        SpellAnimation currentSpellAnimation = null;
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
        List<Monster> initialMonsters = new List<Monster>();
        public IEnumerable<Monster> Monsters => battleField.Where(c => c?.Type == CharacterType.Monster).Cast<Monster>();
        public IEnumerable<Character> Characters => battleField.Where(c => c != null);
        public Character GetCharacterAt(int index) => battleField[index];
        public Character GetCharacterAt(int column, int row) => GetCharacterAt(column + row * 6);
        public int GetSlotFromCharacter(Character character) => battleField.ToList().IndexOf(character);
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
                    }
                }
            }

            effectAnimations = layout.GetOrCreateBattleEffectAnimations();

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
                    currentBattleAnimation.SetDisplayLayer((byte)(GetCharacterPosition(currentlyAnimatedMonster) * 5));
                }
            }

            foreach (var effectAnimation in effectAnimations)
            {
                if (effectAnimation != null && !effectAnimation.Finished)
                {
                    effectAnimation.Update(battleTicks);
                }
            }

            if (currentSpellAnimation != null)
                currentSpellAnimation.Update(battleTicks);
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
            var roundActors = battleField
                .Where(f => f != null)
                .OrderByDescending(c => c.Attributes[Data.Attribute.Speed].TotalCurrentValue)
                .ToList();
            parryingPlayers.Clear();
            bool monstersAdvance = false;

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
                .Select(a => (int)GetTargetTileFromParameter(a.Parameter)).ToList();
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

                    if (partyMember.Ailments.HasFlag(Ailment.Crazy))
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
                                Action = BattleActionType.Hurt,
                                ActionParameter = CreateHurtParameter(GetTargetTileFromParameter(playerAction.Parameter))
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

            var action = PickMonsterAction(monster, wantsToFlee, forbiddenMonsterMoveSpots);

            if (action == BattleActionType.None) // do nothing
                return;

            var actionParameter = PickActionParameter(action, monster, wantsToFlee, forbiddenMonsterMoveSpots);

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
                        Action = BattleActionType.Hurt,
                        ActionParameter = CreateHurtParameter(GetTargetTileFromParameter(actionParameter))
                    });
                }
            }
        }

        void RunBattleAction(BattleAction battleAction, uint battleTicks)
        {
            layout.SetBattleMessage(null);
            game.CursorType = CursorType.Sword;

            void ActionFinished()
            {
                ActionCompleted?.Invoke(battleAction);
                if (needsClickForNextAction)
                    WaitForClick = true;
                ReadyForNextAction = true;
            }

            switch (battleAction.Action)
            {
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
                            // TODO: handle dropping weapon / no ammunition
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
                            GetCastSpellInformation(next.ActionParameter, out _, out Spell spell, out uint itemIndex);
                            string spellName = game.DataNameProvider.GetSpellname(spell);

                            if (itemIndex != 0)
                            {
                                var item = game.ItemManager.GetItem(itemIndex);
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
                    layout.SetBattleMessage(text, next.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                    {
                        ActionFinished();
                    });
                    return;
                }
                case BattleActionType.Move:
                {
                    void EndMove()
                    {
                        MoveCharacterTo(battleAction.ActionParameter, battleAction.Character);
                        ActionCompleted?.Invoke(battleAction);
                        ReadyForNextAction = true;
                    }

                    bool moveFailed = false;
                    if (!battleAction.Character.Ailments.CanMove())
                    {
                        // TODO: is this right or is the action just skipped?
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageCannotMove,
                            battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
                        moveFailed = true;
                    }
                    else if (battleField[battleAction.ActionParameter & 0x1f] != null)
                    {
                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageWayWasBlocked,
                            battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
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

                    if (battleAction.Character is Monster monster)
                    {
                        uint currentRow = (uint)GetCharacterPosition(monster) / 6;
                        uint newPosition = GetTargetTileFromParameter(battleAction.ActionParameter);
                        uint newRow = newPosition / 6;
                        bool retreat = newRow < currentRow;
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void MoveAnimationFinished()
                        {
                            animation.AnimationFinished -= MoveAnimationFinished;
                            animation.SetDisplayLayer((byte)(newPosition * 5));
                            EndMove();
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                        }

                        var newDisplayPosition = layout.GetMonsterCombatCenterPosition((int)battleAction.ActionParameter % 6, (int)newRow, monster);
                        animation.AnimationFinished += MoveAnimationFinished;
                        var frames = monster.GetAnimationFrameIndices(MonsterAnimationType.Move);
                        animation.Play(frames, (uint)Math.Abs((int)newRow - (int)currentRow) * Game.TicksPerSecond / (2 * (uint)frames.Length),
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
                    // TODO
                    break;
                case BattleActionType.Attack:
                {
                    // TODO: first check if the weapon breaks. If so add a message which states it.
                    // After a click this attack action should follow.
                    GetAttackInformation(battleAction.ActionParameter, out uint targetTile, out uint weaponIndex, out uint ammoIndex);
                    var attackResult = ProcessAttack(battleAction.Character, (int)targetTile, out int damage, out bool abort);
                    // Next action is a hurt action
                    var hurtAction = roundBattleActions.Peek();
                    if (attackResult != AttackResult.Damage)
                    {
                        hurtAction.Skip = true;
                        var textColor = battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White;

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
                        }

                        if (abort)
                        {
                            foreach (var action in roundBattleActions.Where(a => a.Character == battleAction.Character))
                                action.Skip = true;
                            if (battleAction.Character is PartyMember partyMember)
                                PlayerLostTarget?.Invoke(partyMember);
                        }
                    }
                    else
                    {
                        hurtAction.ActionParameter = UpdateHurtParameter(hurtAction.ActionParameter, (uint)damage);
                        layout.SetBattleMessage(battleAction.Character.Name + string.Format(game.DataNameProvider.BattleMessageDidPointsOfDamage, damage),
                            battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
                    }
                    Item weapon = weaponIndex == 0 ? null : game.ItemManager.GetItem(weaponIndex);
                    if (battleAction.Character is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void AttackAnimationFinished()
                        {
                            animation.AnimationFinished -= AttackAnimationFinished;
                            if (weapon == null || weapon.Type != ItemType.LongRangeWeapon) // in this case the ammunition effect calls it
                                ActionFinished();
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
                            }, (uint)GetCharacterPosition(battleAction.Character), targetTile, battleTicks, ActionFinished);
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
                            }, (uint)GetCharacterPosition(battleAction.Character), targetTile, battleTicks, ActionFinished);
                        }
                        else
                        {
                            PlayBattleEffectAnimation(BattleEffect.PlayerAtack, targetTile, battleTicks, ActionFinished);
                        }
                    }
                    return;
                }
                case BattleActionType.CastSpell:
                {
                    GetCastSpellInformation(battleAction.ActionParameter, out uint targetRowOrTile, out Spell spell, out uint itemIndex);

                    // Note: Support spells like healing can also miss. In this case no message is displayed but the SP is spent.
                    // I guess the same is true for offensive spells.

                    var spellInfo = SpellInfos.Entries[spell];

                    battleAction.Character.SpellPoints.CurrentValue -= spellInfo.SP;

                    if (spell != Spell.Fireball) // TODO: REMOVE. For now we only allow fire ball for testing.
                    {
                        break;
                    }

                    void EndCast()
                    {
                        currentSpellAnimation?.Destroy();
                        currentSpellAnimation = null;
                        ActionCompleted?.Invoke(battleAction);
                        ReadyForNextAction = true;
                    }

                    switch (spellInfo.Target)
                    {
                        case SpellTarget.SingleEnemy:
                            if (GetCharacterAt((int)targetRowOrTile) == null)
                            {
                                layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                    battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
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
                        // Note: For row spells the initial animation is cast and in move to the miss message is displayed.
                    }


                    currentSpellAnimation = new SpellAnimation(game, layout, this, spell,
                        battleAction.Character.Type == CharacterType.Monster, GetCharacterPosition(battleAction.Character));

                    void CastSpellOn(Character target, Action finishAction)
                    {
                        if (target == null)
                        {
                            finishAction?.Invoke();
                            return;
                        }

                        int position = GetCharacterPosition(target);
                        // Note: Some spells like Whirlwind move to the target.
                        currentSpellAnimation.MoveTo(position, ticks =>
                        {
                            PlayBattleEffectAnimation(target.Type == CharacterType.Monster ? BattleEffect.HurtMonster : BattleEffect.HurtPlayer,
                                (uint)position, ticks, () =>
                                {
                                    // TODO: calculate and deal damage, heal, etc
                                    finishAction?.Invoke();
                                }
                            );
                        });
                    }

                    void CastSpellOnRow(CharacterType characterType, int row, Action finishAction)
                    {
                        var targets = Enumerable.Range(0, 6).Select(column => battleField[column + row * 6])
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
                                if (row == maxRow)
                                    finishAction?.Invoke();
                                else
                                    Cast(row + 1);
                            });
                        }

                        Cast(minRow);
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
                                case SpellTarget.Self:
                                case SpellTarget.SingleEnemy:
                                case SpellTarget.SingleFriend:
                                    CastSpellOn(GetCharacterAt((int)targetRowOrTile), EndCast);
                                    break;
                                case SpellTarget.AllEnemies:
                                    CastSpellOnAll(battleAction.Character.Type == CharacterType.Monster
                                        ? CharacterType.PartyMember : CharacterType.Monster, EndCast);
                                    break;
                                case SpellTarget.AllFriends:
                                    CastSpellOnAll(battleAction.Character.Type, EndCast);
                                    break;
                                case SpellTarget.EnemyRow:
                                {
                                    var enemyType = battleAction.Character.Type == CharacterType.Monster ?
                                        CharacterType.PartyMember : CharacterType.Monster;
                                    if (!Enumerable.Range((int)targetRowOrTile * 6, 6).Any(p => GetCharacterAt(p)?.Type == enemyType))
                                    {
                                        layout.SetBattleMessage(battleAction.Character.Name + game.DataNameProvider.BattleMessageMissedTheTarget,
                                            battleAction.Character.Type == CharacterType.Monster ? TextColor.Orange : TextColor.White);
                                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                                        {
                                            EndCast();
                                        });
                                        return;
                                    }
                                    CastSpellOnRow(battleAction.Character.Type == CharacterType.Monster
                                        ? CharacterType.PartyMember : CharacterType.Monster, (int)targetRowOrTile, EndCast);
                                    break;
                                }
                            }
                        });
                    }
                    return;
                }
                case BattleActionType.Flee:
                {
                    void EndFlee()
                    {
                        fledCharacters.Add(battleAction.Character);
                        RemoveCharacterFromBattleField(battleAction.Character);
                        ActionCompleted?.Invoke(battleAction);

                        if (battleAction.Character.Type == CharacterType.Monster &&
                            Monsters.Count() == 0)
                        {
                            BattleEnded?.Invoke(new Game.BattleEndInfo
                            {
                                MonstersDefeated = true,
                                KilledMonsters = initialMonsters.Where(m => !fledCharacters.Contains(m)).ToList()
                            });
                            return;
                        }
                        else if (battleAction.Character.Type == CharacterType.PartyMember &&
                            !battleField.Any(c => c?.Type == CharacterType.PartyMember))
                        {
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
                            battleTicks, Global.CombatBackgroundArea.Center, 0.0f);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;
                    }
                    else
                    {
                        EndFlee();
                    }
                    return;
                }
                case BattleActionType.Hurt:
                {
                    GetHurtInformation(battleAction.ActionParameter, out uint tile, out uint damage);

                    var target = GetCharacterAt((int)tile);

                    target.Damage(damage);

                    void EndHurt()
                    {
                        ActionFinished();

                        if (!target.Alive)
                        {
                            static float GetMonsterDeathScale(Monster monster)
                            {
                                // 48 is the normal frame width
                                return monster.MappedFrameWidth / 48.0f;
                            }
                            foreach (var action in roundBattleActions.Where(a => a.Character == battleAction.Character || a.Character == target))
                                action.Skip = true;
                            if (battleAction.Character is PartyMember partyMember)
                            {
                                var battleFieldCopy = (Character[])battleField.Clone();
                                RemoveCharacterFromBattleField(target);
                                PlayBattleEffectAnimation(BattleEffect.Death, tile, battleTicks, () =>
                                {
                                    CharacterDied?.Invoke(target);
                                    PlayerLostTarget?.Invoke(partyMember);
                                    if (Monsters.Count() == 0)
                                    {
                                        BattleEnded?.Invoke(new Game.BattleEndInfo
                                        {
                                            MonstersDefeated = true,
                                            KilledMonsters = initialMonsters.Where(m => !fledCharacters.Contains(m)).ToList()
                                        });
                                    }
                                }, GetMonsterDeathScale(target as Monster), battleFieldCopy);
                            }
                            else
                            {
                                RemoveCharacterFromBattleField(target);
                                CharacterDied?.Invoke(target);

                                if (!partyMembers.Any(p => p != null && p.Alive && p.Ailments.CanFight()))
                                {
                                    BattleEnded?.Invoke(new Game.BattleEndInfo
                                    {
                                        MonstersDefeated = false
                                    });
                                }
                            }
                        }
                    }

                    if (target is PartyMember partyMember)
                    {
                        // TODO: show damage splash at portrait

                        PlayBattleEffectAnimation(BattleEffect.HurtPlayer, tile, battleTicks, EndHurt);
                    }
                    else if (target is Monster monster)
                    {
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void HurtAnimationFinished()
                        {
                            animation.AnimationFinished -= HurtAnimationFinished;
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                        }

                        animation.AnimationFinished += HurtAnimationFinished;
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Hurt), Game.TicksPerSecond / 3,
                            battleTicks);
                        currentBattleAnimation = animation;
                        currentlyAnimatedMonster = monster;

                        PlayBattleEffectAnimation(BattleEffect.HurtMonster, tile, battleTicks, EndHurt);
                    }
                    return;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid battle action.");
            }

            // TODO: REMOVE
            layout.SetBattleMessage("TODO^Click to continue ;)", TextColor.Green);
            game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
            {
                ActionFinished();
            });
        }

        void RemoveCharacterFromBattleField(Character character)
        {
            battleField[GetCharacterPosition(character)] = null;
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

        void PickMadAction(PartyMember partyMember, PlayerBattleAction playerBattleAction, List<int> forbiddenMoveSpots)
        {
            // Mad players can attack, move, flee or parry.
            // TODO: Can they cast spells too?
            var position = GetCharacterPosition(partyMember);
            List<BattleActionType> possibleActions = new List<BattleActionType>();

            if (position < 6 && partyMember.Ailments.CanFlee() && game.RollDice100() < 10) // TODO
                possibleActions.Add(BattleActionType.Flee);
            if (partyMember.Ailments.CanAttack() && AttackSpotAvailable(position, partyMember))
                possibleActions.Add(BattleActionType.Attack);
            if (partyMember.Ailments.CanMove() && MoveSpotAvailable(position, partyMember, false, forbiddenMoveSpots))
                possibleActions.Add(BattleActionType.Move);
            if (partyMember.Ailments.CanParry())
                possibleActions.Add(BattleActionType.Parry);
            playerBattleAction.BattleAction = possibleActions.Count == 0 ? BattleActionType.None
                : possibleActions.Count == 1 ? possibleActions[0] : possibleActions[game.RandomInt(0, possibleActions.Count - 1)];
            switch (playerBattleAction.BattleAction)
            {
                case BattleActionType.Move:
                {
                    uint moveSpot = GetRandomMoveSpot(position, partyMember, forbiddenMoveSpots);
                    playerBattleAction.Parameter = CreateMoveParameter(moveSpot);
                    forbiddenMoveSpots.Add((int)moveSpot);
                    break;
                }
                case BattleActionType.Attack:
                    playerBattleAction.Parameter = CreateAttackParameter(GetRandomAttackSpot(position, partyMember), partyMember, game.ItemManager);
                    break;
            }
            layout.UpdateCharacterStatus(game.SlotFromPartyMember(partyMember).Value,
                playerBattleAction.BattleAction.ToStatusGraphic(playerBattleAction.Parameter, game.ItemManager));
        }

        BattleActionType PickMonsterAction(Monster monster, bool wantsToFlee, List<int> forbiddenMonsterMoveSpots)
        {
            var position = GetCharacterPosition(monster);
            List<BattleActionType> possibleActions = new List<BattleActionType>();

            if (position < 6 && wantsToFlee && monster.Ailments.CanFlee())
            {
                return BattleActionType.Flee;
            }
            bool willFlee = wantsToFlee && monster.Ailments.CanMove();
            if (!willFlee && monster.Ailments.CanAttack() && AttackSpotAvailable(position, monster))
                possibleActions.Add(BattleActionType.Attack);
            if ((wantsToFlee || !possibleActions.Contains(BattleActionType.Attack)) && monster.Ailments.CanMove()) // TODO: small chance to move even if the monster could attack?
            {
                // Only move if there is nobody to attack
                if (MoveSpotAvailable(position, monster, wantsToFlee, forbiddenMonsterMoveSpots))
                {
                    if (wantsToFlee)
                        return BattleActionType.Move;
                    else
                        possibleActions.Add(BattleActionType.Move);
                }
            }
            if (monster.HasAnySpell() && monster.Ailments.CanCastSpell() && CanCastSpell(monster))
                possibleActions.Add(BattleActionType.CastSpell);
            if (possibleActions.Count == 0)
                return BattleActionType.None;
            if (possibleActions.Count == 1)
                return possibleActions[0];

            // TODO: maybe prioritize some actions? dependent on what?
            return possibleActions[game.RandomInt(0, possibleActions.Count - 1)];
        }

        bool CanCastSpell(Monster monster)
        {
            // First check the spells the monster has enough SP for.
            var sp = monster.SpellPoints.TotalCurrentValue;
            var possibleSpells = monster.LearnedSpells.Where(s =>
            {
                var spellInfo = SpellInfos.Entries[s];
                return spellInfo.SP <= sp &&
                    spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle) &&
                    spellInfo.Target != SpellTarget.Item;

            }).ToList();

            if (possibleSpells.Count == 0)
                return false;

            bool monsterNeedsHealing = battleField.Where(c => c?.Type == CharacterType.Monster)
                .Any(m => m.HitPoints.TotalCurrentValue < m.HitPoints.MaxValue / 2);

            if (monsterNeedsHealing)
            {
                // if the monster can heal, do so
                if (possibleSpells.Contains(Spell.SmallHealing) ||
                    possibleSpells.Contains(Spell.MediumHealing) ||
                    possibleSpells.Contains(Spell.GreatHealing) ||
                    possibleSpells.Contains(Spell.MassHealing))
                    return true;
            }

            return possibleSpells.Any(s => SpellInfos.Entries[s].Target.TargetsEnemy());
        }

        bool MoveSpotAvailable(int characterPosition, Character character, bool wantsToFlee, List<int> forbiddenMoveSpots = null)
        {
            int moveRange = character.Type == CharacterType.Monster && character.Attributes[Data.Attribute.Speed].TotalCurrentValue >= 80 ? 2 : 1;

            if (!GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY,
                moveRange, RangeType.Move, wantsToFlee))
                return false;
            int currentRow = characterPosition / 6;

            for (int y = minY; y <= maxY; ++y)
            {
                if ((!wantsToFlee && y < currentRow) ||
                    (wantsToFlee && y >= currentRow))
                    continue;

                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6] == null && (forbiddenMoveSpots == null || !forbiddenMoveSpots.Contains(x + y * 6)))
                        return true;
                }
            }

            return false;
        }

        bool AttackSpotAvailable(int characterPosition, Character character)
        {
            int range = character.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? int.MaxValue : 1;

            if (!GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, range, RangeType.Enemy))
                return false;

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;
                    if (battleField[position] != null && battleField[position].Type != character.Type)
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
            GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, 1, RangeType.Enemy);
            var possiblePositions = new List<int>();
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;

                    if (battleField[position] != null && battleField[position].Type != character.Type)
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
                    if (battleField[x + y * 6] == null && !forbiddenMonsterMoveSpots.Contains(x + y * 6))
                        possiblePositions.Add(x + y * 6);
                }
            }

            if (!wantsToFlee)
            {
                // Prefer moving to positions where a player is nearby.
                // Also prefer moving straight down.
                var nearPlayerPositions = possiblePositions.Where(p => IsPlayerNearby(p)).ToList();

                if (nearPlayerPositions.Count != 0)
                {
                    var nearPlayerPositionInSameRow = nearPlayerPositions
                        .Where(p => p % 6 == currentColumn)
                        .Select(p => (int?)p).FirstOrDefault();

                    if (nearPlayerPositionInSameRow != null)
                        return (uint)nearPlayerPositionInSameRow.Value;

                    return (uint)nearPlayerPositions[game.RandomInt(0, nearPlayerPositions.Count - 1)];
                }

                // Prefer moving straight down
                for (int row = maxY; row > currentRow; --row)
                {
                    if (battleField[currentColumn + row * 6] == null)
                    {
                        return (uint)(currentColumn + row * 6);
                    }
                }
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

        bool MonsterWantsToFlee(Monster monster)
        {
            // TODO
            return !monster.MonsterFlags.HasFlag(MonsterFlags.Boss) &&
                monster.HitPoints.TotalCurrentValue < monster.HitPoints.MaxValue / 2 &&
                game.RollDice100() < (monster.HitPoints.MaxValue - monster.HitPoints.TotalCurrentValue) * 100 / monster.HitPoints.MaxValue;
        }

        uint GetBestAttackSpot(int characterPosition, Character character)
        {
            int range = character.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? int.MaxValue : 1;
            GetRangeMinMaxValues(characterPosition, character, out int minX, out int maxX, out int minY, out int maxY, range, RangeType.Enemy);
            var possiblePositions = new List<int>();

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    int position = x + y * 6;
                    if (battleField[position] != null && battleField[position].Type != character.Type)
                        possiblePositions.Add(position);
                }
            }

            // TODO: prioritize weaker players? dependent on what?
            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        uint GetBestSpellSpotOrRow(Monster monster, Spell spell)
        {
            var spellInfo = SpellInfos.Entries[spell];

            if (spellInfo.Target.TargetsEnemy())
            {
                if (spellInfo.Target == SpellTarget.EnemyRow)
                {
                    // TODO: maybe pick the row with most players for some clever monsters?
                    // TODO: don't cast on rows without enemies
                    return (uint)game.RandomInt(3, 4);
                }
                else
                {
                    var positions = partyMembers.Where(p => p?.Alive == true)
                        .Select(p => GetCharacterPosition(p)).ToArray();
                    return (uint)positions[game.RandomInt(0, positions.Length - 1)];
                }
            }
            else
            {
                // Note: Friend spells can only target single targets or all.
                // For all no target has to be selected so here only a single target is selected.
                var positions = Monsters.Where(m => m?.Alive == true)
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
                    var sp = monster.SpellPoints.TotalCurrentValue;
                    var possibleSpells = monster.LearnedSpells.Where(s =>
                    {
                        var spellInfo = SpellInfos.Entries[s];
                        return spellInfo.SP <= sp &&
                            spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle) &&
                            spellInfo.Target != SpellTarget.Item;

                    }).ToList();
                    bool monsterNeedsHealing = battleField.Where(c => c?.Type == CharacterType.Monster)
                        .Any(m => m.HitPoints.TotalCurrentValue < m.HitPoints.MaxValue / 2);
                    Spell spell = Spell.None;

                    if (monsterNeedsHealing &&
                        (possibleSpells.Contains(Spell.SmallHealing) ||
                        possibleSpells.Contains(Spell.MediumHealing) ||
                        possibleSpells.Contains(Spell.GreatHealing) ||
                        possibleSpells.Contains(Spell.MassHealing)))
                    {
                        if (!possibleSpells.Any(s => SpellInfos.Entries[s].Target.TargetsEnemy()) || game.RollDice100() < 50)
                        {
                            var healingSpells = possibleSpells.Where(s => s.ToString().ToLower().Contains("healing")).ToArray();
                            spell = healingSpells.Length == 1 ? healingSpells[0] : healingSpells[game.RandomInt(0, healingSpells.Length - 1)];
                        }
                    }

                    if (spell == Spell.None)
                    {
                        var spells = possibleSpells.ToArray();
                        spell = spells.Length == 1 ? spells[0] : spells[game.RandomInt(0, spells.Length - 1)];
                    }

                    uint targetSpot = SpellInfos.Entries[spell].Target.GetTargetType() == SpellTargetType.HalfBattleField
                        ? 0 : GetBestSpellSpotOrRow(monster, spell);

                    return CreateCastSpellParameter(targetSpot, spell);
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

            effectAnimations = layout.GetOrCreateBattleEffectAnimations(effects.Count);

            for (int i = 0; i < effects.Count; ++i)
            {
                var effect = effects[i];

                PlayBattleEffectAnimation(i, effect.StartTextureIndex, effect.FrameSize, effect.FrameCount, ticks, FinishEffect,
                    effect.Duration / effect.FrameCount, effect.InitialDisplayLayer, effect.StartPosition, effect.EndPosition,
                    effect.StartScale, effect.EndScale);
            }
        }

        void PlayBattleEffectAnimation(int index, uint graphicIndex, Size frameSize, uint numFrames, uint ticks,
            Action finishedAction, uint ticksPerFrame, byte initialDisplayLayer, Position startPosition, Position endPosition,
            float initialScale = 1.0f, float endScale = 1.0f)
        {
            var effectAnimation = effectAnimations[index];
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            effectAnimation.SetDisplayLayer(initialDisplayLayer);
            effectAnimation.SetStartFrame(textureAtlas.GetOffset(graphicIndex), frameSize, startPosition, initialScale);
            effectAnimation.Play(Enumerable.Range(0, (int)numFrames).ToArray(), ticksPerFrame, ticks, endPosition, endScale);
            effectAnimation.Visible = true;

            void EffectAnimationFinished()
            {
                effectAnimation.AnimationFinished -= EffectAnimationFinished;
                effectAnimation.Visible = false;
                finishedAction?.Invoke();
            }

            effectAnimation.AnimationFinished += EffectAnimationFinished;
        }

        // Lowest 5 bits: Tile index (0-29) to move to
        public static uint CreateMoveParameter(uint targetTile) => targetTile & 0x1f;
        // Lowest 5 bits: Tile index (0-29) to attack
        // Next 11 bits: Weapon item index (can be 0 for monsters)
        // Next 11 bits: Optional ammunition item index
        public static uint CreateAttackParameter(uint targetTile, uint weaponIndex = 0, uint ammoIndex = 0) =>
            (targetTile & 0x1f) | ((weaponIndex & 0x7ff) << 5) | ((ammoIndex & 0x7ff) << 16);
        public static uint CreateAttackParameter(uint targetTile, PartyMember partyMember, IItemManager itemManager)
        {
            uint weaponIndex = partyMember.Equipment.Slots[EquipmentSlot.RightHand]?.ItemIndex ?? 0;
            uint ammoIndex = 0;

            if (weaponIndex != 0)
            {
                var weapon = itemManager.GetItem(weaponIndex);

                if (weapon.Type == ItemType.LongRangeWeapon && weapon.UsedAmmunitionType != AmmunitionType.None)
                {
                    ammoIndex = partyMember.Equipment.Slots[EquipmentSlot.LeftHand]?.ItemIndex ?? 0;
                }
            }

            return CreateAttackParameter(targetTile, weaponIndex, ammoIndex);
        }
        // Lowest 5 bits: Tile index (0-29) or row (0-4) to cast spell on
        // Next 11 bits: Item index (when spell came from an item, otherwise 0)
        // Upper 16 bits: Spell index
        public static uint CreateCastSpellParameter(uint targetTileOrRow, Spell spell, uint itemIndex = 0) =>
            (targetTileOrRow & 0x1f) | ((itemIndex & 0x7ff) << 5) | ((uint)spell << 16);
        // Lowest 5 bits: Tile index (0-29) where a character is hurt
        // Rest: Damage
        public static uint CreateHurtParameter(uint targetTile) => targetTile & 0x1f;
        static uint UpdateHurtParameter(uint hurtParameter, uint damage) =>
            (hurtParameter & 0x1f) | ((damage & 0x7ffffff) << 5);
        public static uint GetTargetTileFromParameter(uint actionParameter) => actionParameter & 0x1f;
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
        static Spell GetCastSpell(uint actionParameter) => (Spell)(actionParameter >> 16);
        static void GetCastSpellInformation(uint actionParameter, out uint targetRowOrTile, out Spell spell, out uint itemIndex)
        {
            spell = (Spell)(actionParameter >> 16);
            itemIndex = (actionParameter >> 5) & 0x7ff;
            targetRowOrTile = actionParameter & 0x1f;
        }
        public static bool IsCastFromItem(uint actionParameter) => ((actionParameter >> 5) & 0x7ff) != 0;
        static void GetHurtInformation(uint actionParameter, out uint targetTile, out uint damage)
        {
            damage = (actionParameter >> 5) & 0x7ffffff;
            targetTile = actionParameter & 0x1f;
        }

        enum AttackResult
        {
            Damage,
            Failed, // Chance depending on attackers ATT ability
            NoDamage, // Chance depending on ATK / DEF
            Missed, // Target moved
            Blocked, // Parry
            Protected // magic protection level
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

            if (attacker.MagicAttack >= 0 && target.MagicDefense > attacker.MagicAttack)
            {
                abortAttacking = true;
                return AttackResult.Protected;
            }

            if (game.RollDice100() > attacker.Abilities[Ability.Attack].TotalCurrentValue)
                return AttackResult.Failed;

            // TODO: how is damage calculated?
            damage = attacker.BaseAttack + game.RandomInt(0, attacker.VariableAttack) - (target.BaseDefense + game.RandomInt(0, target.VariableDefense));

            if (damage <= 0)
                return AttackResult.NoDamage;

            // TODO: can monsters parry?
            if (target is PartyMember partyMember && parryingPlayers.Contains(partyMember) &&
                game.RollDice100() < partyMember.Abilities[Ability.Parry].TotalCurrentValue)
                return AttackResult.Blocked;

            return AttackResult.Damage;
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
