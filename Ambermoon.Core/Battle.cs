using Ambermoon.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal class Battle
    {
        internal enum BattleAction
        {
            None,
            Move,
            MoveGroupForward,
            Attack,
            CastSpell,
            Flee
        }

        readonly Game game;
        readonly PartyMember[] partyMembers;
        readonly Monster[,] monsters;
        List<Character> roundActors;
        BattleAction[] roundBattleActions;
        uint[] roundBattleActionParameters;
        public event Action RoundFinished;
        Character currentActor;
        uint currentActorAction = 0;
        uint currentActorActionCount = 0;
        Character[] battleField = new Character[6 * 5];
        bool wantsToFlee = false;

        public Battle(Game game, PartyMember[] partyMembers, MonsterGroup monsterGroup)
        {
            this.game = game;
            this.partyMembers = partyMembers;
            monsters = monsterGroup.Monsters;

            // place characters
            for (int i = 0; i < partyMembers.Length; ++i)
            {
                battleField[18 + game.CurrentSavegame.BattlePositions[i]] = partyMembers[i];
            }
            for (int y = 0; y < 3; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    battleField[x + y * 6] = monsters[x, y];
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
        public void NextAction()
        {
            void RunActor()
            {
                int actorIndex = roundActors.Count - 1;
                bool idle;

                if (currentActor is Monster monster)
                    RunMonsterAction(actorIndex, monster, ref currentActorAction, currentActorActionCount, out idle);
                else if (currentActor is PartyMember player)
                    RunPlayerAction(actorIndex, player, ref currentActorAction, currentActorActionCount, out idle);
                else
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid battle character.");

                if (currentActorAction == currentActorActionCount)
                {
                    roundActors.RemoveAt(actorIndex);

                    if (roundActors.Count == 0)
                        RoundFinished?.Invoke();
                    else if (idle)
                        NextAction();
                }
            }

            if (currentActor != null && currentActorAction < currentActorActionCount)
            {
                RunActor();
                return;
            }

            // roundActors are ordered by their speed value.
            // Last one has highest speed.
            currentActor = roundActors.Last();
            currentActorAction = 0;
            currentActorActionCount = currentActor.AttacksPerRound;
            RunActor();
        }

        /// <summary>
        /// Starts a new battle round.
        /// </summary>
        /// <param name="battleActions">Battle actions for party members 1-6.</param>
        /// <param name="battleActionParameters">Parameters for those battle actions.</param>
        internal void StartRound(BattleAction[] battleActions, uint[] battleActionParameters)
        {
            roundActors = partyMembers.Where(p => p != null && p.Alive).Cast<Character>()
                .Concat(monsters.ToList().Where(m => m != null && m.Alive).Cast<Character>())
                .OrderBy(c => c.Attributes[Data.Attribute.Speed].TotalCurrentValue).ToList();
            roundBattleActions = new BattleAction[roundActors.Count];
            roundBattleActionParameters = new uint[roundActors.Count];

            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                if (partyMembers[i] != null && partyMembers[i].Alive)
                {
                    int index = roundActors.IndexOf(partyMembers[i]);
                    roundBattleActions[index] = battleActions[i];
                    roundBattleActionParameters[index] = battleActionParameters[i];
                }
            }

            NextAction();
        }

        void RunMonsterAction(int roundActorIndex, Monster monster, ref uint actionIndex, uint actionCount, out bool idle)
        {
            idle = false;

            if (actionIndex == 0)
            {
                wantsToFlee = MonsterWantsToFlee(monster);
                var action = PickMonsterAction(monster, wantsToFlee);

                if (action == BattleAction.None) // do nothing
                {
                    actionIndex = actionCount;
                    idle = true;
                    return;
                }

                roundBattleActions[roundActorIndex] = action;
                roundBattleActionParameters[roundActorIndex] = PickActionParameter(action, monster);
            }

            RunCharacterAction(roundActorIndex, monster, ref actionIndex, actionCount, wantsToFlee);
        }

        void RunPlayerAction(int roundActorIndex, PartyMember partyMember, ref uint actionIndex, uint actionCount, out bool idle)
        {
            wantsToFlee = false;
            idle = roundBattleActions[roundActorIndex] == BattleAction.None;

            if (idle)
                actionIndex = actionCount;
            else
                RunCharacterAction(roundActorIndex, partyMember, ref actionIndex, actionCount);
        }

        void RunCharacterAction(int roundActorIndex, Character character, ref uint actionIndex, uint actionCount, bool wantsToFlee = false)
        {
            if (actionIndex != 0 && roundBattleActions[roundActorIndex] != BattleAction.Attack)
            {
                // Only attacks can cause multiple actions per round.
                actionIndex = actionCount;
                return;
            }
            else
            {
                switch (roundBattleActions[roundActorIndex])
                {
                case BattleAction.Move:
                    // Parameter: New position index (0-29)
                    // TODO
                    break;
                case BattleAction.MoveGroupForward:
                    // No parameter
                    // TODO
                    break;
                case BattleAction.Attack:
                    // Parameter: Tile index to attack (0-29)
                    // TODO
                    break;
                case BattleAction.CastSpell:
                    // Parameter: Tile index (0-29) or row (0-4) to cast spell on
                    // TODO: Can support spells miss? If not for those spells the
                    //       parameter should be the monster/partymember index instead.
                    // TODO
                    break;
                case BattleAction.Flee:
                    // No parameter
                    // TODO
                    break;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid battle action.");
                }

                ++actionIndex;
            }
        }

        int GetCharacterPosition(Character character) => battleField.ToList().IndexOf(character);

        BattleAction PickMonsterAction(Monster monster, bool wantsToFlee)
        {
            var position = GetCharacterPosition(monster);
            List<BattleAction> possibleActions = new List<BattleAction>();

            if (position < 6 && wantsToFlee && monster.Ailments.CanFlee())
            {
                return BattleAction.Flee;
            }
            if (monster.Ailments.CanAttack() && AttackSpotAvailable(position, monster))
                possibleActions.Add(BattleAction.Attack);
            if (!possibleActions.Contains(BattleAction.Attack) && monster.Ailments.CanMove())
            {
                // Only move if there is nobody to attack
                if (MoveSpotAvailable(position, monster))
                    possibleActions.Add(BattleAction.Move);
            }
            if (monster.HasAnySpell() && monster.Ailments.CanCastSpell() && CanCastSpell(monster))
                possibleActions.Add(BattleAction.CastSpell);
            if (!battleField.Skip(12).Take(6).Any(c => c != null)) // middle row is empty
                possibleActions.Add(BattleAction.MoveGroupForward);

            if (possibleActions.Count == 0)
                return BattleAction.None;
            if (possibleActions.Count == 1)
                return possibleActions[0];

            // TODO: maybe prioritize some actions? dependent on what?
            return possibleActions[game.RandomInt(0, possibleActions.Count - 1)];
        }

        bool CanCastSpell(Monster monster)
        {
            // First check the spells the monster has enough SP for.
            var sp = monster.SpellPoints.TotalCurrentValue;
            var possibleSpells = monster.LearnedSpells.Where(s => SpellInfos.Entries[s].SP <= sp).ToList();

            if (possibleSpells.Count == 0)
                return false;

            bool monsterNeedsHealing = monsters.ToList().Any(m => m.HitPoints.TotalCurrentValue < m.HitPoints.MaxValue / 2);

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
            // TODO: monsters like spiders can move more than 1 field at once. where is this info stored?
            if (!GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, 1))
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
            int range = 1; // TODO: long-ranged attacks

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
            // TODO: monsters like spiders can move more than 1 field at once. where is this info stored?
            GetRangeMinMaxValues(characterPosition, monster, out int minX, out int maxX, out int minY, out int maxY, 1);
            var possiblePositions = new List<int>();

            for (int y = minY; y <= maxY; ++y)
            {
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

        uint GetBestAttackSpot(int characterPosition, Monster monster)
        {
            int range = 1; // TODO: long-ranged attacks
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

        uint PickActionParameter(BattleAction battleAction, Monster monster)
        {
            switch (battleAction)
            {
            case BattleAction.Move:
                return CreateMoveParameter(GetBestMoveSpot(GetCharacterPosition(monster), monster));
            case BattleAction.Attack:
                {
                    var weaponIndex = monster.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex;
                    var ammoIndex = monster.Equipment.Slots[EquipmentSlot.LeftHand].ItemIndex;
                    if (ammoIndex == weaponIndex) // two-handed weapon?
                        ammoIndex = 0;
                    return CreateAttackParameter(GetBestAttackSpot(GetCharacterPosition(monster), monster), weaponIndex, ammoIndex);
                }
            case BattleAction.CastSpell:
                // TODO: return CreateCastSpellParameter ...
                return 0;
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
        public static uint CreateCastSpellParameter(uint targetTile, uint spell) =>
            (targetTile & 0xff) | (spell << 8);
    }
}
