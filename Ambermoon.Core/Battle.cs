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

            var longRangedWeapon = itemManager.GetItem(itemIndex.Value);
            bool hasLongRangedWeapon = longRangedWeapon.Type == ItemType.LongRangeWeapon;

            if (hasLongRangedWeapon)
            {
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

                        if (item?.Type != ItemType.Ammunition || item.AmmunitionType != longRangedWeapon.UsedAmmunitionType)
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
            /// </summary>
            Move,
            /// <summary>
            /// No parameter
            /// </summary>
            MoveGroupForward,
            /// <summary>
            /// Parameter: Tile index to attack (0-29)
            /// TODO: If someone dies, call CharacterDied and remove it from the battle field
            /// </summary>
            Attack,
            /// <summary>
            /// This plays a monster or attack animation and prints text about
            /// how much damage the attacker dealt or if he missed etc.
            /// </summary>
            Parry,
            /// <summary>
            /// Parameter:
            /// - Lowest 4 bits: Tile index (0-29) or row (0-4) to cast spell on
            /// - Next 12 bits: Item index (when spell came from an item, otherwise 0)
            /// - Upper 16 bits: Spell index
            /// TODO: Can support spells miss? If not for those spells the
            ///       parameter should be the monster/partymember index instead.
            /// </summary>
            CastSpell,
            /// <summary>
            /// No parameter
            /// </summary>
            Flee,
            /// <summary>
            /// No parameter
            /// This just prints text about what the actor is doing.
            /// The text depends on the following enqueued action.
            /// </summary>
            DisplayActionText,
            /// <summary>
            /// This is playing hurt animations like blood on monsters
            /// or claw on player. It also removed the hitpoints and
            /// displays this as an effect on players.
            /// This is used after spells and attacks. It is added
            /// for every spell cast or attack action but might be
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
         *      3. Hurt
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
        }

        readonly Game game;
        readonly Layout layout;
        readonly PartyMember[] partyMembers;
        readonly Queue<BattleAction> roundBattleActions = new Queue<BattleAction>();
        readonly Character[] battleField = new Character[6 * 5];
        Character[] preRoundBattleField;
        readonly List<PartyMember> parryingPlayers = new List<PartyMember>(Game.MaxPartyMembers);
        uint? animationStartTicks = null;
        Monster currentlyAnimatedMonster = null;
        BattleAnimation currentBattleAnimation = null;
        bool idleAnimationRunning = false;
        uint nextIdleAnimationTicks = 0;
        bool wantsToFlee = false;
        readonly bool needsClickForNextAction;
        public bool ReadyForNextAction { get; private set; } = false;
        public bool WaitForClick { get; private set; } = false;

        public event Action RoundFinished;
        public event Action<Character> CharacterDied;
        public event Action<Character, uint, uint> CharacterMoved;
        public event Action<Game.BattleEndInfo> BattleEnded;
        public event Action<BattleAction> ActionCompleted;
        event Action AnimationFinished;
        public IEnumerable<Monster> Monsters => battleField.Where(c => c?.Type == CharacterType.Monster).Cast<Monster>();
        public IEnumerable<Character> Characters => battleField.Where(c => c != null);
        public Character GetCharacterAt(int column, int row) => battleField[column + row * 6];
        public int GetSlotFromCharacter(Character character) => battleField.ToList().FindIndex(c => c == character);
        public bool RoundActive { get; private set; } = false;
        public bool CanMoveForward => battleField.Skip(12).Take(6).Any(c => c != null) && // middle row empty
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
                    }
                }
            }

            SetupNextIdleAnimation(0);
        }

        void UpdateMonsterDisplayLayer(Monster monster)
        {
            int position = GetCharacterPosition(currentlyAnimatedMonster);
            currentBattleAnimation.SetDisplayLayer((byte)(position * 5));
        }

        void MonsterAnimationFinished(Monster monster)
        {
            animationStartTicks = null;
            idleAnimationRunning = false;
            currentlyAnimatedMonster = null;
            layout.ResetMonsterCombatSprite(monster);

            if (RoundActive)
                ReadyForNextAction = true;
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

            if (ReadyForNextAction && !needsClickForNextAction)
                NextAction(battleTicks);

            if (currentBattleAnimation != null)
            {
                if (!currentBattleAnimation.Update(battleTicks))
                {
                    currentBattleAnimation = null;
                    AnimationFinished?.Invoke();
                }
            }
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
            RunBattleAction(action, battleTicks);
        }

        public void Click(uint battleTicks)
        {
            WaitForClick = false;

            if (ReadyForNextAction && needsClickForNextAction)
            {
                NextAction(battleTicks);
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
                .OrderBy(c => c.Attributes[Data.Attribute.Speed].TotalCurrentValue)
                .ToList();
            parryingPlayers.Clear();
            preRoundBattleField = battleField.ToArray(); // copy

            // This is added in addition to normal monster actions directly
            if (CanMoveForward &&
                !playerBattleActions.Any(a => a.BattleAction == BattleActionType.MoveGroupForward))
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
            }

            foreach (var roundActor in roundActors)
            {
                if (roundActor is Monster monster)
                {
                    AddMonsterActions(monster);
                }
                else
                {
                    var partyMember = roundActor as PartyMember;
                    int playerIndex = partyMembers.ToList().IndexOf(partyMember);
                    var playerAction = playerBattleActions[playerIndex];

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
                        if (playerAction.BattleAction == BattleActionType.Attack ||
                            (playerAction.BattleAction == BattleActionType.CastSpell &&
                            SpellInfos.Entries[GetCastSpell(playerAction.Parameter)].Target.TargetsEnemy()))
                        {
                            roundBattleActions.Enqueue(new BattleAction
                            {
                                Character = partyMember,
                                Action = BattleActionType.Hurt,
                                ActionParameter = 0
                            });
                        }
                    }
                }
            }

            RoundActive = true;
            NextAction(battleTicks);
        }

        void AddMonsterActions(Monster monster)
        {
            bool wantsToFlee = MonsterWantsToFlee(monster);

            if (wantsToFlee && roundBattleActions.Count > 1)
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
            }

            var action = PickMonsterAction(monster, wantsToFlee);

            if (action == BattleActionType.None) // do nothing
                return;

            var actionParameter = PickActionParameter(action, monster);

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
                if (action == BattleActionType.Attack ||
                    (action == BattleActionType.CastSpell &&
                    SpellInfos.Entries[GetCastSpell(actionParameter)].Target.TargetsEnemy()))
                {
                    roundBattleActions.Enqueue(new BattleAction
                    {
                        Character = monster,
                        Action = BattleActionType.Hurt,
                        ActionParameter = 0
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
                if (needsClickForNextAction)
                    WaitForClick = true;
                ReadyForNextAction = true;
                ActionCompleted?.Invoke(battleAction);
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
                            bool retreat = newRow < currentRow;
                            text = next.Character.Name + (retreat ? game.DataNameProvider.BattleMessageRetreats : game.DataNameProvider.BattleMessageMoves);
                            break;
                        }
                        case BattleActionType.Flee:
                            text = next.Character.Name + game.DataNameProvider.BattleMessageFlees;
                            break;
                        case BattleActionType.Attack:
                        {
                            // TODO: handle dropping weapon / no ammunition
                            var weaponIndex = next.Character.Equipment.Slots[EquipmentSlot.RightHand]?.ItemIndex;
                            var weapon = weaponIndex == null ? null : game.ItemManager.GetItem(weaponIndex.Value);
                            var target = preRoundBattleField[next.ActionParameter];

                            if (weapon == null)
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
                        ActionFinished();
                    }

                    // TODO: Test if move fails and then display a message
                    if (false) // TODO: move fails
                    {
                        // layout.SetBattleMessage()
                        // Way could be blocked or character can not move anymore
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(500), () =>
                        {
                            ActionFinished();
                        });
                    }

                    if (battleAction.Character is Monster monster)
                    {
                        uint currentRow = (uint)GetCharacterPosition(monster) / 6;
                        uint newRow = battleAction.ActionParameter / 6;
                        bool retreat = newRow < currentRow;
                        var animation = layout.GetMonsterBattleAnimation(monster);

                        void MoveAnimationFinished()
                        {
                            animation.AnimationFinished -= MoveAnimationFinished;
                            UpdateMonsterDisplayLayer(monster);
                            currentBattleAnimation = null;
                            currentlyAnimatedMonster = null;
                            EndMove();
                        }

                        var newDisplayPosition = layout.GetMonsterCombatPosition((int)battleAction.ActionParameter % 6, (int)newRow, monster);
                        animation.AnimationFinished += MoveAnimationFinished;
                        animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Move), Game.TicksPerSecond / 6,
                            battleTicks, newDisplayPosition.Y, layout.RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)newRow));
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
                    // Parameter: New position index (0-29)
                    // TODO
                    return;
                }
                case BattleActionType.MoveGroupForward:
                    // No parameter
                    // TODO
                    break;
                case BattleActionType.Attack:
                    // Parameter: Tile index to attack (0-29)
                    // TODO: If someone dies, call CharacterDied and remove it from the battle field
                    // TODO
                    break;
                case BattleActionType.CastSpell:
                    // Parameter:
                    // - Low word: Tile index (0-29) or row (0-4) to cast spell on
                    // - High word: Spell index
                    // TODO: Can support spells miss? If not for those spells the
                    //       parameter should be the monster/partymember index instead.
                    // TODO
                    break;
                case BattleActionType.Flee:
                    // No parameter
                    // TODO
                    break;
                case BattleActionType.Hurt:
                    // TODO
                    break;
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
            game.RemoveBattleActor(character);
        }

        void MoveCharacterTo(uint tile, Character character)
        {
            MoveCharacterTo(tile % 6, tile / 6, character);
        }

        void MoveCharacterTo(uint column, uint row, Character character)
        {
            battleField[GetCharacterPosition(character)] = null;
            game.MoveBattleActorTo(column, row, character);
        }

        int GetCharacterPosition(Character character) => battleField.ToList().IndexOf(character);

        BattleActionType PickMonsterAction(Monster monster, bool wantsToFlee)
        {
            var position = GetCharacterPosition(monster);
            List<BattleActionType> possibleActions = new List<BattleActionType>();

            if (position < 6 && wantsToFlee && monster.Ailments.CanFlee())
            {
                return BattleActionType.Flee;
            }
            if (monster.Ailments.CanAttack() && AttackSpotAvailable(position, monster))
                possibleActions.Add(BattleActionType.Attack);
            if ((wantsToFlee || !possibleActions.Contains(BattleActionType.Attack)) && monster.Ailments.CanMove()) // TODO: small chance to move even if the monster could attack?
            {
                // Only move if there is nobody to attack
                if (MoveSpotAvailable(position, monster))
                    possibleActions.Add(BattleActionType.Move);
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

        bool MoveSpotAvailable(int characterPosition, Monster monster)
        {
            int moveRange = monster.Attributes[Data.Attribute.Speed].TotalCurrentValue >= 80 ? 2 : 1;

            if (!GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, moveRange))
                return false;

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6] == null)
                        return true;
                }
            }

            return false;
        }

        bool AttackSpotAvailable(int characterPosition, Monster monster)
        {
            int range = monster.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? int.MaxValue : 1;

            if (!GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, range))
                return false;

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

        bool GetRangeMinMaxValues(int characterPosition, Monster monster, out int minX, out int maxX, out int minY, out int maxY, int range)
        {
            int characterX = characterPosition % 6;
            int characterY = characterPosition / 6;
            minX = Math.Max(0, characterX - range);
            maxX = Math.Min(5, characterX + range);
            minY = Math.Max(0, characterY - range);
            maxY = Math.Min(4, characterY + range);

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

            return true;
        }

        uint GetBestMoveSpot(int characterPosition, Monster monster)
        {
            int moveRange = monster.Attributes[Data.Attribute.Speed].TotalCurrentValue >= 80 ? 2 : 1;
            GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, moveRange);
            int currentColumn = characterPosition % 6;
            int currentRow = characterPosition / 6;
            var possiblePositions = new List<int>();

            if (wantsToFlee)
            {
                for (int row = Math.Max(0, currentRow - moveRange); row < currentRow; ++row)
                {
                    if (battleField[currentColumn + row * 6] == null)
                        return (uint)(currentColumn + row * 6);
                }
            }

            for (int y = minY; y <= maxY; ++y)
            {
                if ((!wantsToFlee && y <= currentRow) ||
                    (wantsToFlee && y >= currentRow))
                    continue;

                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6] == null)
                        possiblePositions.Add(x + y * 6);
                }
            }

            if (!wantsToFlee)
            {
                var nearPlayerPositions = possiblePositions.Where(p => IsPlayerNearby(p)).ToList();

                if (nearPlayerPositions.Count != 0)
                    return (uint)nearPlayerPositions[game.RandomInt(0, nearPlayerPositions.Count - 1)];

                for (int row = Math.Min(4, currentRow + moveRange); row > currentRow; --row)
                {
                    if (battleField[currentColumn + row * 6] == null)
                        return (uint)(currentColumn + row * 6);
                }
            }

            return (uint)possiblePositions[game.RandomInt(0, possiblePositions.Count - 1)];
        }

        bool IsPlayerNearby(int position)
        {
            int minX = Math.Max(0, position - 1);
            int maxX = Math.Min(5, position + 1);
            int minY = Math.Max(0, position - 1);
            int maxY = Math.Min(4, position + 1);

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
                game.RandomInt(0, (int)(monster.HitPoints.MaxValue - monster.HitPoints.TotalCurrentValue)) > monster.HitPoints.MaxValue / 2;
        }

        Spell GetCastSpell(uint actionParameter) => (Spell)(actionParameter >> 16);

        void GetCastSpellInformation(uint actionParameter, out uint targetRowOrTile, out Spell spell, out uint itemIndex)
        {
            spell = (Spell)(actionParameter >> 16);
            itemIndex = (actionParameter >> 4) & 0xfff;
            targetRowOrTile = actionParameter & 0xf;
        }

        uint GetBestAttackSpot(int characterPosition, Monster monster)
        {
            int range = monster.HasLongRangedAttack(game.ItemManager, out bool hasAmmo) && hasAmmo ? int.MaxValue : 1;
            GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, range);
            var possiblePositions = new List<int>();

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    if (battleField[x + y * 6]?.Type == CharacterType.PartyMember)
                        possiblePositions.Add(x + y * 6);
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
                if (spellInfo.Target == SpellTarget.FriendRow)
                {
                    // TODO: pick the row where monsters need healing
                    // TODO: don't cast on rows without friends
                    return (uint)game.RandomInt(0, 3);
                }
                else
                {
                    var positions = Monsters.Where(m => m?.Alive == true)
                        .Select(p => GetCharacterPosition(p)).ToArray();
                    return (uint)positions[game.RandomInt(0, positions.Length - 1)];
                }
            }
        }

        uint PickActionParameter(BattleActionType battleAction, Monster monster)
        {
            switch (battleAction)
            {
            case BattleActionType.Move:
                return CreateMoveParameter(GetBestMoveSpot(GetCharacterPosition(monster), monster));
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

        // Lowest byte: Tile index
        public static uint CreateMoveParameter(uint targetTile) => targetTile & 0xff;
        // Lowest byte: Tile index
        // Above is the weapon index encoded with 12 bits (can be 0 for monsters)
        // Above is the optional ammon index encoded with 12 bits
        public static uint CreateAttackParameter(uint targetTile, uint weaponIndex = 0, uint ammoIndex = 0) =>
            (targetTile & 0xff) | (weaponIndex << 8) | (ammoIndex << 20);
        // Lowest byte: Tile index
        // Above is the spell index
        public static uint CreateCastSpellParameter(uint targetTileOrRow, Spell spell, uint itemIndex = 0) =>
            (targetTileOrRow & 0xf) | (itemIndex << 4) | ((uint)spell << 16);
    }

    internal static class BattleActionExtensions
    {
        public static MonsterAnimationType? ToAnimationType(this Battle.BattleActionType battleAction) => battleAction switch
        {
            Battle.BattleActionType.None => null,
            Battle.BattleActionType.Move => MonsterAnimationType.Move,
            Battle.BattleActionType.MoveGroupForward => null,
            Battle.BattleActionType.Attack => MonsterAnimationType.Attack,
            Battle.BattleActionType.CastSpell => MonsterAnimationType.Cast,
            Battle.BattleActionType.Flee => null,
            _ => null
        };
    }
}
