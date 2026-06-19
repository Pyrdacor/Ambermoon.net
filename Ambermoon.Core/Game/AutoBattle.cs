using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;

namespace Ambermoon;

// rules
// - attack strongest enemy in range
//   - prefere nearby enemies
//   - prefere damaged enemies
// - don't use items
// - use black magic only to disable enemy
// - use healing if required
//   - prefere healer over paladin
// - don't use MagicAttack, MagicProtection, ForeseeMagic, ForeseeAttack, RecognizeWeakPoint, SeeWeaknesses, KnowledgeOfTheWeakness
//   - can be used or commanded by player before activating AutoBattle
// 
// assumptions
// - enemy abilities are known (real players learn)
// - enemies don't move
// - cast magic is sucessfull
// - attacks damage enemies
//
// algorithm
// - analyze enemies threading level
//   - enemies use 50% magic
//   - enemies prefere group spells
// - give orders in turn order
//   - delay paladin magic


// ways to improve party power
//   if (game.Features.HasFlag(Features.ExtendedCurseEffects)) => blind, aging(level)
//   weakenedMonsters.contains(target)
//   foreseeAttack for defense
partial class Battle
{
    internal void CalculateThreat(Monster monster, out int physicalThreat, out int magicThreat, bool ignoreSleep = false)
    {
        physicalThreat = (monster.BaseAttackDamage + monster.BonusAttackDamage) * monster.AttacksPerRound;
        magicThreat = 0;
        foreach (var spell in GetAvailableMonsterSpells(monster))
        {
            var spellInfo = game.SpellInfos[spell];
            uint spellThread;
            if (spell >= Spell.Mudsling && spell <= Spell.Iceshower)
            {
                var damage = game.Features.HasFlag(Features.AdjustedSpellDamage)
                                ? Battle.AdjustedDestructionSpellDamageValues
                                : Battle.DestructionSpellDamageValues;
                spellThread = (damage[spell - Spell.Mudsling].Key + damage[spell - Spell.Mudsling].Value) / 2;
            }
            else switch (spell)
                {
                    case Spell.LPStealer:
                    case Spell.SPStealer:
                        spellThread = (uint)monster.Level * 3 / 2;
                        break;
                    case Spell.GhostWeapon:
                    case Spell.GhostInferno:
                    case Spell.MagicSwordAttack:
                        spellThread = (uint)(monster.BaseAttackDamage + monster.BonusAttackDamage);
                        break;
                    case Spell.MagicalProjectile:
                    case Spell.MagicalArrows:
                        spellThread = monster.Level;
                        break;
                    case Spell.Petrify:
                    case Spell.DissolveVictim:
                        spellThread = 200;
                        break;
                    case Spell.CauseMadness:
                        spellThread = 50;
                        break;
                    case Spell.Lame:
                        spellThread = 25;
                        break;
                    case Spell.Irritate:
                    case Spell.CauseAging:
                    case Spell.CauseDisease:
                        spellThread = 10;
                        break;
                    case Spell.Poison:
                    case Spell.Sleep:
                    case Spell.Drug:
                        spellThread = 5;
                        break;
                    default:
                        spellThread = 0;
                        break;
                }
            if (spellInfo.Target == SpellTarget.AllEnemies)
                spellThread *= 3;
            else if (spellInfo.Target == SpellTarget.EnemyRow)
                spellThread *= 2;
            else if (spellInfo.Target == SpellTarget.EnemyRowInWeaponRange)
                spellThread = spellThread * 3 / 2;
            magicThreat = Math.Max(magicThreat, (int)spellThread);
        }

        // handle conditions except sleep
        if (monster.Conditions.HasFlag(Condition.Panic)
            || monster.Conditions.HasFlag(Condition.Petrified))
            physicalThreat = magicThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Irritated))
            magicThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Lamed))
            physicalThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Blind))
            physicalThreat /= 2;
        if (monster.Conditions.HasFlag(Condition.Crazy))
        {
            physicalThreat /= 2;
            magicThreat = 0;
        }
        if (!ignoreSleep && monster.Conditions.HasFlag(Condition.Sleep))
        {
            physicalThreat = 1;
            magicThreat = 1;
        }
    }
}

partial class GameCore
{
    class AutoBattleThreat 
    {
        required public Monster Monster;
        public int Possition;
        public int PhysicalThreat, MagicThreat;
        public bool Sleeping => PhysicalThreat == 1 && MagicThreat == 1;
        public uint Health;
    }

    void AddAutoBattleActions(bool firstRound)
    {
        if (firstRound)
        {
            var oldBattleSpeed = currentBattle!.Speed;
            SetBattleSpeed(300);
            currentBattle.BattleEnded += (x) => SetBattleSpeed(oldBattleSpeed);
            currentBattle.RoundFinished += () => AddAutoBattleActions(false);
        }

        if (currentBattle!.CanPartyMoveForward)
        {
            AdvanceParty(() => AddAutoBattleActions(false));
            return;
        }

        // PartyMembers sorted by moving order
        var partyOrder = PartyMembers.Where(a => a.Alive).OrderByDescending(c => c!.Attributes[Data.Attribute.Speed].TotalCurrentValue).ThenBy(c => c!.Type).ToList();
        // command paladin after healer
        for (int i = 0, paladinIndex = -1; i < partyOrder.Count(); i++)
            if (partyOrder[i].Class == Class.Paladin)
                paladinIndex = i;
            else if (partyOrder[i].Class == Class.Healer)
            {
                if (paladinIndex >= 0)
                {
                    var paladin = partyOrder[paladinIndex];
                    for (; paladinIndex < i; paladinIndex++)
                        partyOrder[paladinIndex] = partyOrder[paladinIndex + 1];
                    partyOrder[i] = paladin;
                }
                break;
            }

        // party healing data
        var partyToHeal = PartyMembers.Where(a => a.Alive && a.HitPoints.CurrentValue <= a.HitPoints.TotalMaxValue * 4 / 10).OrderBy(a => a.HitPoints.CurrentValue).ToList();
        Condition partyConditions = Condition.None;
        int partyDefense = 0, partyMaxHealth = 0;
        foreach (var partyMember in partyOrder)
        {
            partyConditions |= partyMember.Conditions;
            partyDefense += partyMember.BaseDefense + partyMember.BonusDefense + (int)partyMember.Attributes[Data.Attribute.Stamina].TotalCurrentValue / 25;
            partyMaxHealth += (int)partyMember.HitPoints.MaxValue;
        }
        partyDefense /= partyOrder.Count();
        partyMaxHealth /= partyOrder.Count();

        // list of monsters for attack prio
        var threats = new List<AutoBattleThreat>(currentBattle!.Monsters.Count());
        foreach (var monster in currentBattle!.Monsters)
        {
            if (!monster.Alive)
                continue;
            currentBattle!.CalculateThreat(monster, out int physicalThreat, out int magicThreat);
            threats.Add(new AutoBattleThreat()
            {
                Monster = monster,
                PhysicalThreat = physicalThreat > 1 ? Math.Max(0, physicalThreat - partyDefense * monster.AttacksPerRound) : physicalThreat,
                MagicThreat = magicThreat,
                Possition = currentBattle!.GetSlotFromCharacter(monster),
                Health = monster.HitPoints.CurrentValue
            });
        }

        // command party members
        foreach (var partyMember in partyOrder)
        {
            // partyMember out of control?
            if (partyMember.Conditions.HasFlag(Condition.Panic)
                || partyMember.Conditions.HasFlag(Condition.Crazy))
                continue;
            currentPickingActionMember = partyMember;

            // check stored action and override if outdated 
            int slot = SlotFromPartyMember(partyMember)!.Value;
            if (roundPlayerBattleActions.TryGetValue(slot, out var action))
            {
                switch (action.BattleAction)
                {
                    case Battle.BattleActionType.Attack:
                        if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) is Monster)
                            continue;
                        break;
                    case Battle.BattleActionType.Move:
                        if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) == null)
                            continue;
                        break;
                    case Battle.BattleActionType.CastSpell:
                        var spell = Battle.GetCastSpell(action.Parameter);
                        switch (SpellInfos[spell].Target)
                        {
                            case SpellTarget.SingleEnemy:
                                if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) is Monster)
                                    continue;
                                break;
                            case SpellTarget.SingleFriend:
                                if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) is PartyMember)
                                    continue;
                                break;
                            default:
                                continue;
                        }
                        break;
                    case Battle.BattleActionType.Parry:
                        break;
                    default:
                        continue;
                }
                roundPlayerBattleActions.Remove(slot);
            }

            // check magic
            if (partyMember.Conditions.CanCastSpell(Features))
            {
                Spell spell = Spell.None;
                Data.Character spellTarget = null;
                bool CanCast(Spell spell) => partyMember.HasSpell(spell) && partyMember.SpellPoints.CurrentValue >= spellInfos[spell].SP;

                if (partyMember.Class == Class.Mage)
                {
                    bool canSleep = CanCast(Spell.Sleep);
                    bool canLame = CanCast(Spell.Lame);
                    bool canBlind = Features.HasFlag(Features.ExtendedCurseEffects) && CanCast(Spell.Blind);
                    bool canIrritate = CanCast(Spell.Irritate);
                    foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenByDescending(a => a.Health).ThenByDescending(a => a.Possition))
                    {
                        if (threat.Health < threat.Monster.HitPoints.TotalMaxValue / 2)
                            continue;
                        bool isPhysicalThread = threat.PhysicalThreat >= partyMaxHealth / 10;
                        bool isMagicThread = threat.MagicThreat >= partyMaxHealth / 10;
                        if (!isPhysicalThread && !isMagicThread)
                            break;
                        // cast sleep only at back rows
                        if (isPhysicalThread && isMagicThread && threat.Possition < 12
                            && canSleep && !threat.Monster.IsImmuneToSpell(Spell.Sleep, out var _, Features.HasFlag(Features.Elements)))
                        {
                            spell = Spell.Sleep;
                            threat.MagicThreat = threat.PhysicalThreat = 1;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (threat.MagicThreat >= threat.PhysicalThreat
                            && canIrritate && !threat.Monster.IsImmuneToSpell(Spell.Irritate, out var _, Features.HasFlag(Features.Elements)))
                        {
                            spell = Spell.Irritate;
                            threat.MagicThreat = 0;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (isPhysicalThread && canLame && !threat.Monster.IsImmuneToSpell(Spell.Lame, out var _, Features.HasFlag(Features.Elements)))
                        {
                            spell = Spell.Lame;
                            threat.PhysicalThreat = 0;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (isPhysicalThread && canBlind && !threat.Monster.IsImmuneToSpell(Spell.Blind, out var _, Features.HasFlag(Features.Elements)))
                        {
                            spell = Spell.Blind;
                            threat.PhysicalThreat /= 2;
                            spellTarget = threat.Monster;
                            break;
                        }
                        // retry sleep if no irritate, lame or blind
                        else if (threat.Possition < 12
                            && canSleep && !threat.Monster.IsImmuneToSpell(Spell.Sleep, out var _, Features.HasFlag(Features.Elements)))
                        {
                            spell = Spell.Sleep;
                            threat.MagicThreat = threat.PhysicalThreat = 1;
                            spellTarget = threat.Monster;
                            break;
                        }
                    }
                }

                if ((partyMember.Class == Class.Healer || partyMember.Class == Class.Paladin))
                {
                    if (partyToHeal.Count > 0)
                    {
                        // check healing first
                        var lowFrac = partyToHeal[0].HitPoints.CurrentValue * 100 / partyToHeal[0].HitPoints.TotalMaxValue;
                        if (lowFrac < 25 && CanCast(Spell.MassHealing) && PartyMembers.All((pm2) => pm2.HitPoints.CurrentValue < pm2.HitPoints.TotalMaxValue * 3 / 4))
                        {
                            spell = Spell.MassHealing;
                            partyToHeal.Clear();
                        }
                        else
                        {
                            bool greatHeal = CanCast(Spell.GreatHealing);
                            bool mediumHeal = CanCast(Spell.MediumHealing);
                            bool smallHeal = CanCast(Spell.SmallHealing);
                            bool handHeal = CanCast(Spell.HealingHand);
                            if (lowFrac < 10 && greatHeal)
                                spell = Spell.GreatHealing;
                            else if (lowFrac < 20 && (mediumHeal || greatHeal))
                                spell = mediumHeal ? Spell.MediumHealing : Spell.GreatHealing;
                            else if (lowFrac < 30 && (smallHeal || mediumHeal))
                                spell = smallHeal ? Spell.SmallHealing : Spell.MediumHealing;
                            else if (lowFrac < 40 && (handHeal || smallHeal))
                                spell = handHeal ? Spell.HealingHand : Spell.SmallHealing;
                            if (spell != Spell.None)
                            {
                                spellTarget = partyToHeal[0];
                                partyToHeal.RemoveAt(0);
                            }
                        }
                    }

                    if (spell == Spell.None)
                    {
                        bool Cure(Condition cond, Spell spellOne, Spell spellAll)
                        {
                            if (!partyConditions.HasFlag(cond))
                                return false;

                            bool canOne = CanCast(spellOne);
                            bool canAll = CanCast(spellAll);
                            if (!canOne && !canAll)
                                return false;

                            var toCure = PartyMembers.Where((pm2) => pm2.Conditions.HasFlag(cond)).ToArray();
                            if (toCure.Length == 1 || !canAll)
                            {
                                spell = spellOne;
                                spellTarget = toCure[0];
                            }
                            else
                                spell = spellAll;
                            partyConditions &= ~cond;
                            return true;
                        }
                        if (partyConditions != Condition.None)
                            if (!Cure(Condition.Panic, Spell.RemoveFear, Spell.RemovePanic))
                                if (!Cure(Condition.Lamed, Spell.RemoveRigidness, Spell.RemoveLamedness))
                                    if (!Cure(Condition.Blind, Spell.RemoveShadows, Spell.RemoveBlindness))
                                        if (partyMember.Class != Class.Paladin)  // prefere paladin to attack
                                            if (!Cure(Condition.Sleep, Spell.WakeUp, Spell.None))
                                                if (!Cure(Condition.Irritated, Spell.RemoveIrritation, Spell.None))
                                                    if (!Cure(Condition.Poisoned, Spell.RemovePoison, Spell.NeutralizePoison))
                                                        Cure(Condition.Diseased, Spell.RemovePain, Spell.RemoveDisease);

                    }
                }

                if (spell != Spell.None)
                {
                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                        Battle.CreateCastSpellParameter(spellTarget != null ? (uint)currentBattle.GetSlotFromCharacter(spellTarget) : 0, spell));
                    continue;
                }
            }

            // attack
            int position = currentBattle.GetSlotFromCharacter(partyMember!);
            bool hasAction = false;
            threats = threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => a.Health).ThenBy(a => a.Possition).ToList();
            if (CheckAbilityToAttack(out bool ranged, true))
                foreach (var threat in threats)
                    if ((ranged || (Math.Abs(threat.Possition % 6 - position % 6) <= 1 && Math.Abs(threat.Possition / 6 - position / 6) <= 1))
                        && !currentBattle.ImmuneToAttack(threat.Monster, partyMember))
                    {
                        hasAction = true;
                        SetPlayerBattleAction(Battle.BattleActionType.Attack, Battle.CreateAttackParameter((uint)threat.Possition));
                        if (threat.Sleeping)    // wake up
                            currentBattle.CalculateThreat(threat.Monster, out threat.PhysicalThreat, out threat.MagicThreat, true);
                        threat.Health -= (uint)Math.Max(0, partyMember!.BaseAttackDamage + partyMember!.BonusAttackDamage - threat.Monster!.BaseDefense - threat.Monster!.BonusDefense);
                        break;
                    }

            // move to enemy
            if (!hasAction && !ranged && partyMember.Conditions.CanMove())
                foreach (var threat in threats)
                {
                    if (threat.Possition < 12)
                        continue;

                    int threatCol = threat.Possition % 6;
                    int threatRow = threat.Possition / 6;
                    int partyCol = position % 6;
                    int maxDist = 1 + (int)partyMember!.Attributes[Data.Attribute.Speed].TotalCurrentValue / 80;
                    int delta = threatCol < position % 6 ? -1 : 1;
                    int newPosition = -1;
                    if (threatCol == partyCol)
                    {
                        if (threatCol > 0 && currentBattle.IsBattleFieldEmpty(18 + threatCol - 1) && !AnyPlayerMovesTo(18 + threatCol - 1))
                            newPosition = 18 + threatCol - 1;
                        else if (threatCol < 5 && currentBattle.IsBattleFieldEmpty(18 + threatCol + 1) && !AnyPlayerMovesTo(18 + threatCol + 1))
                            newPosition = 18 + threatCol + 1;
                        else if (position >= 24 && currentBattle.IsBattleFieldEmpty(18 + threatCol))
                            newPosition = 18 + threatCol;
                    }
                    else if (threatRow == 3 || currentBattle.IsBattleFieldEmpty(18 + threatCol) 
                        || (threatCol != 0 && currentBattle.IsBattleFieldEmpty(18 + threatCol - 1))
                        || (threatCol != 5 && currentBattle.IsBattleFieldEmpty(18 + threatCol + 1)))
                        for (int newCol = delta < 0 ? Math.Max(0, partyCol + maxDist * delta) : Math.Min(5, partyCol + maxDist * delta); newCol != partyCol; newCol -= delta)
                            if (currentBattle.IsBattleFieldEmpty(18 + newCol) && !AnyPlayerMovesTo(18 + newCol))
                            {
                                newPosition = 18 + newCol;
                                break;
                            }
                            else if (currentBattle.IsBattleFieldEmpty(24 + newCol) && !AnyPlayerMovesTo(24 + newCol)
                                && (threatRow == 3 || threatCol != newCol || (newCol != 0 && newCol != 5 && currentBattle.IsBattleFieldEmpty(18 + threatCol + delta))))
                            {
                                newPosition = 24 + newCol;
                                break;
                            }
                    if (newPosition != -1)
                    {
                        hasAction = true;
                        SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)newPosition));
                        if (partyToHeal.Contains(partyMember))
                            partyToHeal.Remove(partyMember);
                        break;
                    }
                }

            // nothing -> parry
            if (!hasAction && partyMember.Conditions.CanParry())
            {
                SetCurrentPlayerBattleAction(Battle.BattleActionType.Parry);
            }
        }

        StartBattleRound(false);
    }
}