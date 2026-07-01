/*
 * AutoBattle.cs - Automatic Battle Planing
 *
 * Copyright (C) 2026  Marcel Hesselbarth <spam@mayavoyage.de>
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

// battle rules - see feature request #417 
// - attack strongest enemy in range
//   - prefere damaged enemies
//   - prefere nearby enemies
// - use healing if required
//   - prefere healer over paladin
// - use black magic only to disable enemy
//   - other spells can be used/planed by player before activating AutoBattle
// - don't use items
//   - use spellpoint items if healer runs out of spellpoints 
//   - use healing items if group has no operating healer
// - don't use imitate
//   - can be used/planed by player before activating AutoBattle
//   - use spells of imitated monster
// - don't use MagicAttack, MagicProtection, RecognizeWeakPoint, SeeWeaknesses, KnowledgeOfTheWeakness
//   - can be used/planed by player before activating AutoBattle
// - abort auto-battle if party member died
// 
// assumptions for planing actions
// - enemy abilities are known (real players learn)
//   - assign threading
// - enemies don't move
// - cast magic is sucessfull
// - attacks damage enemies
//
// algorithm, uses a lot of internal data unknown to the player just to calculate what the player knows: the most dangerous enemy  
// - analyze enemies threading level
//   - enemies use 50% magic
//   - group spells are more dangerous
// - give orders in turn order
//   - delay paladin magic after healer magic

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

namespace Ambermoon;

partial class Battle
{
    internal void CalculateAutoBattleInfo(Monster monster, out int physicalThreat, out int magicThreat, bool ignoreSleep = false)
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
    static internal Data.Graphic AutoBattleButton => new Data.Graphic(32, 13, 0)
    {
        Data = new byte[] {
0,0,30,28,27,27,27,27,27,27,27,27,27,27,27,27,27,27,27,27,28,27,27,27,27,27,28,28,29,30,0,0,
0,0,28,27,28,28,28,28,28,28,28,26,28,28,28,28,28,26,28,28,29,31,30,29,28,28,28,28,28,29,0,0,
0,0,27,28,28,28,28,28,28,28,28,26,26,28,28,28,28,26,26,28,31,28,28,30,26,28,28,28,28,29,0,0,
0,0,27,28,28,28,28,28,28,28,28,26,27,26,28,28,28,26,27,26,29,31,30,29,26,28,28,28,28,29,0,0,
0,0,27,28,28,28,28,28,28,28,28,26,27,27,26,28,29,26,27,27,26,29,30,26,26,28,28,28,31,29,0,0,
0,0,27,29,30,31,31,31,31,31,31,26,27,27,27,26,31,26,27,27,27,26,30,27,30,27,30,27,31,31,0,0,
0,0,29,31,30,29,28,28,28,28,28,26,27,27,27,27,26,26,27,27,27,27,26,26,29,26,29,26,30,31,0,0,
0,0,27,29,29,30,31,31,31,30,31,26,27,27,27,27,27,27,27,27,27,27,27,29,28,27,28,27,29,30,0,0,
0,0,27,27,26,26,26,26,26,26,26,26,27,27,27,27,30,26,27,27,27,27,30,26,26,26,26,26,30,29,0,0,
0,0,27,28,28,28,28,28,28,28,28,26,27,27,27,30,27,26,27,27,27,30,30,29,27,28,28,28,28,27,0,0,
0,0,27,28,28,28,28,28,28,28,28,26,27,27,30,28,28,26,27,27,30,28,28,30,26,28,28,28,27,26,0,0,
0,0,28,28,28,28,28,28,28,28,28,26,27,30,28,28,28,26,27,30,29,31,30,29,26,28,28,28,28,28,0,0,
0,0,29,28,29,29,29,29,29,29,29,27,29,29,29,29,29,27,29,29,29,26,26,26,26,28,29,29,28,27,0,0,
        }
    };

    class AutoBattleInfo 
    {
        required public Monster Monster;
        public int Position;
        public int PhysicalThreat, MagicThreat;
        public bool Sleeping => PhysicalThreat == 1 && MagicThreat == 1;
        public uint Health;
    }

    void AddAutoBattleActions(bool firstRound)
    {
        if (firstRound)
        {
            var orgBattleSpeed = currentBattle!.Speed;
            int orgPartyCount = PartyMembers.Where(a => a.Conditions.CanFight()).Count();
            SetBattleSpeed(400);
            Action<BattleEndInfo> battleEnded = (x) => SetBattleSpeed(orgBattleSpeed);
            currentBattle.BattleEnded += battleEnded;
            Action roundFinish = null;
            currentBattle.RoundFinished += roundFinish = () =>
            {
                if (orgPartyCount == PartyMembers.Where(a => a.Conditions.CanFight()).Count())
                {
                    if (currentBattle != null)
                        AddAutoBattleActions(false);
                    else
                        SetBattleSpeed(orgBattleSpeed);
                }
                else
                {
                    if (currentBattle != null)
                    {
                        currentBattle.BattleEnded -= battleEnded;
                        currentBattle.RoundFinished -= roundFinish;
                    }
                    SetBattleSpeed(orgBattleSpeed);
                }
            };
        }

        if (currentBattle!.CanPartyMoveForward)
        {
            AdvanceParty(() => AddAutoBattleActions(false));
            return;
        }

        // PartyMembers sorted by moving order
        var partyOrder = PartyMembers.Where(a => a.Alive && a.Conditions.CanSelect()).OrderByDescending(c => c!.Attributes[Data.Attribute.Speed].TotalCurrentValue).ThenBy(c => c!.Type).ToList();
        bool partyHasHealer = false;
        bool partyEmptyHealer = false;
        // command paladin after healer
        for (int i = 0, paladinIndex = -1; i < partyOrder.Count; i++)
            if (partyOrder[i].Class == Class.Paladin || partyOrder[i].Class == Class.Healer)
            {
                partyHasHealer = true;
                partyEmptyHealer = partyOrder[i].SpellPoints.CurrentValue < SpellInfos[Spell.SmallHealing].SP;
                if (partyOrder[i].Class == Class.Paladin)
                    paladinIndex = i;
                else
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
            }

        // collect party healing data
        var partyToHeal = PartyMembers.Where(a => a.Alive && a.HitPoints.CurrentValue <= a.HitPoints.TotalMaxValue / 2).OrderBy(a => a.HitPoints.CurrentValue).ToList();
        Condition partyConditions = Condition.None;
        int partyDefense = 0, partyMaxHealth = 0;
        foreach (var partyMember in partyOrder)
        {
            partyConditions |= partyMember.Conditions;
            partyDefense += partyMember.BaseDefense + partyMember.BonusDefense + (int)partyMember.Attributes[Data.Attribute.Stamina].TotalCurrentValue / 25;
            partyMaxHealth += (int)partyMember.HitPoints.TotalMaxValue;
        }
        partyDefense /= partyOrder.Count;
        partyMaxHealth /= partyOrder.Count;

        // list of monsters for attack prio
        var threats = new List<AutoBattleInfo>(currentBattle!.Monsters.Count());
        foreach (var monster in currentBattle!.Monsters)
            if (monster.Alive)
            {
                currentBattle!.CalculateAutoBattleInfo(monster, out int physicalThreat, out int magicThreat);
                threats.Add(new AutoBattleInfo()
                {
                    Monster = monster,
                    PhysicalThreat = physicalThreat > 1 ? Math.Max(0, physicalThreat - partyDefense * monster.AttacksPerRound) : physicalThreat,
                    MagicThreat = magicThreat,
                    Position = currentBattle!.GetSlotFromCharacter(monster),
                    Health = monster.HitPoints.CurrentValue
                });
            }

        // command party members
        var dontMove = new List<PartyMember>();
        var hasMoved = new List<PartyMember>();
        foreach (var partyMember in partyOrder)
        {            
            currentPickingActionMember = partyMember;

            // check stored action and override if outdated 
            int slot = SlotFromPartyMember(partyMember)!.Value;
            if (roundPlayerBattleActions.TryGetValue(slot, out var action))
            {
                switch (action.BattleAction)
                {
                    case Battle.BattleActionType.Attack:
                        if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) is Monster
                            && ((partyToHeal.Count == 0 && partyConditions == Condition.None) || (partyMember.Class != Class.Healer && partyMember.Class != Class.Paladin && partyHasHealer && !partyEmptyHealer)))
                            continue;
                        break;
                    case Battle.BattleActionType.Move:
                        if (currentBattle!.GetCharacterAt((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter)) == null)
                        {
                            hasMoved.Add(partyMember);
                            continue;
                        }
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

            // check inventory for spell point items
            if (partyEmptyHealer && partyToHeal.Count > 0 && !partyMember.InventoryInaccessible)
            {
                Spell itemSpell = Spell.SpellPointsV + 1;
                int itemSlotIndex = -1;
                bool itemIsEquipped = false;
                void CheckItemSlot(ItemSlot slot, int slotIndex, bool isEquipped)
                {
                    if (slot.ItemIndex > 0)
                    {
                        var item = ItemManager.GetItem(slot.ItemIndex);
                        if (item.Spell >= Spell.SpellPointsI && item.Spell < itemSpell && slot.NumRemainingCharges > 0)
                        {
                            itemSpell = item.Spell;
                            itemSlotIndex = slotIndex;
                            itemIsEquipped = isEquipped;
                        }
                    }
                }
                int i = 0;
                foreach (var equipmentSlot in partyMember.Equipment.Slots)
                    CheckItemSlot(equipmentSlot.Value, i++, true);
                i = 0;
                foreach (var itemSlot in partyMember.Inventory.Slots)
                    CheckItemSlot(itemSlot, i++, false);
                if (itemSpell != Spell.SpellPointsV + 1)
                {
                    var toHealMember = partyOrder.Where(a => a.Class == Class.Healer || a.Class == Class.Paladin).OrderBy(a => a.SpellPoints.CurrentValue).FirstOrDefault();
                    if (toHealMember != null)
                    {
                        partyEmptyHealer = false;
                        dontMove.Add(toHealMember);
                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                            Battle.CreateCastSpellParameter((uint)currentBattle.GetSlotFromCharacter(toHealMember), itemSpell, (uint)itemSlotIndex, itemIsEquipped));
                        continue;
                    }
                }
            }

            // check inventory for healing items
            if ((!partyHasHealer || partyEmptyHealer) && partyToHeal.Count > 0 && !partyMember.InventoryInaccessible)
            {
                var toHealMember = partyToHeal[0];
                int toHealPercent = (int)(toHealMember.HitPoints.CurrentValue * 100 / toHealMember.HitPoints.TotalMaxValue);
                Spell itemSpell = Spell.None;
                int itemSlotIndex = -1;
                bool itemIsEquipped = false;
                void CheckItemSlot(ItemSlot slot, int slotIndex, bool isEquipped)
                {
                    if (slot.ItemIndex > 0)
                    {
                        var item = ItemManager.GetItem(slot.ItemIndex);
                        if (item.Spell > itemSpell && item.Spell <= Spell.MassHealing && slot.NumRemainingCharges > 0)
                            if ((partyToHeal.Count >= 3 && item.Spell == Spell.MassHealing)
                                || (toHealPercent <= 15 && item.Spell == Spell.GreatHealing)
                                || (toHealPercent <= 20 && item.Spell == Spell.MediumHealing)
                                || (toHealPercent <= 30 && item.Spell == Spell.SmallHealing)
                                || (toHealPercent <= 40 && item.Spell == Spell.HealingHand))
                            {
                                itemSpell = item.Spell;
                                itemSlotIndex = slotIndex;
                                itemIsEquipped = isEquipped;
                            }
                    }
                }
                int i = 0;
                foreach (var equipmentSlot in partyMember.Equipment.Slots)
                    CheckItemSlot(equipmentSlot.Value, i++, true);
                i = 0;
                foreach (var itemSlot in partyMember.Inventory.Slots)
                    CheckItemSlot(itemSlot, i++, false);
                if (itemSpell != Spell.None)
                {
                    uint characterSlot = 0;
                    if (itemSpell == Spell.MassHealing)
                        partyToHeal.Clear();
                    else
                    {
                        dontMove.Add(toHealMember);
                        characterSlot = (uint)currentBattle.GetSlotFromCharacter(toHealMember);
                        partyToHeal.Remove(toHealMember);
                    }
                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                        Battle.CreateCastSpellParameter(characterSlot, itemSpell, (uint)itemSlotIndex, itemIsEquipped));
                    continue;
                }
            }

            // check magic
            bool hasAction = false;
            if (partyMember.Conditions.CanCastSpell(Features))
            {
                Spell spell = Spell.None;
                Data.Character spellTarget = null;
                bool CanCast(Spell spell) => partyMember.HasSpell(spell) && partyMember.SpellPoints.CurrentValue >= spellInfos[spell].SP;
                bool IsImmuneTo(AutoBattleInfo target, Spell spell) =>
                    (target.Monster.SpellTypeImmunity & (SpellTypeImmunity)spellInfos[spell].SpellType) != 0
                    || target.Monster.IsImmuneToSpell(spell, out var _, Features.HasFlag(Features.Elements));

                if (partyMember.Class == Class.Mystic || partyMember.Class == Class.Ranger)
                {
                    if (partyMember.InventoryInaccessible)
                        // imitated monster -> use all we have starting with most powerfull
                        foreach (Spell myspell in partyMember.LearnedSpells.OrderByDescending(a => SpellInfos[a].SP))
                        {
                            var spellInfo = SpellInfos[myspell];
                            if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle)
                                || partyMember.SpellPoints.CurrentValue < spellInfo.SP)
                                continue;

                            if (spellInfo.Target == SpellTarget.AllEnemies && threats.Count > 1)
                            {
                                spell = myspell;
                                break;
                            }
                            else if (spellInfo.Target == SpellTarget.EnemyRow && threats.Count > 1)
                            {
                                int[] rows = new int[4];    // count enemies in rows
                                foreach (var threat in threats.Where(a => !IsImmuneTo(a, myspell)))
                                    rows[threat.Position / 6]++;
                                int best = 0;
                                for (int i = 1; i < 4; i++)
                                    if (rows[i] > rows[best])
                                        best = i;
                                if (rows[best] > 1)
                                {
                                    spell = myspell;
                                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                                        Battle.CreateCastSpellParameter((uint)best, spell));
                                    hasAction = true;
                                    break;
                                }
                            }
                            else if (spellInfo.Target == SpellTarget.SingleEnemy)
                            {
                                var target = threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => a.Health).Where(a => !IsImmuneTo(a, myspell)).FirstOrDefault();
                                if (target != null)
                                {
                                    spell = myspell;
                                    spellTarget = target.Monster;
                                    break;

                                }
                            }
                        }
                }

                if (partyMember.Class == Class.Mage)
                {
                    bool canSleep = CanCast(Spell.Sleep);
                    bool canLame = CanCast(Spell.Lame);
                    bool canBlind = Features.HasFlag(Features.ExtendedCurseEffects) && CanCast(Spell.Blind);
                    bool canIrritate = CanCast(Spell.Irritate);
                    foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenByDescending(a => a.Health).ThenBy(a => a.Position))
                    {
                        if (threat.Health < threat.Monster.HitPoints.TotalMaxValue / 2
                            || threat.Monster.Attributes[Data.Attribute.AntiMagic].TotalCurrentValue >= 75)
                            continue;
                        bool isPhysicalThread = threat.PhysicalThreat >= partyMaxHealth / 12;
                        bool isMagicThread = threat.MagicThreat >= partyMaxHealth / 12;
                        if (!isPhysicalThread && !isMagicThread)
                            continue;  // do not waste magic on too weak enemies

                        if (isPhysicalThread && isMagicThread && threat.Position < 12  // cast sleep only at back rows as front is attacked
                            && canSleep && !IsImmuneTo(threat, Spell.Sleep))
                        {
                            spell = Spell.Sleep;
                            threat.MagicThreat = threat.PhysicalThreat = 1;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (threat.MagicThreat >= threat.PhysicalThreat
                            && canIrritate && !IsImmuneTo(threat, Spell.Irritate))
                        {
                            spell = Spell.Irritate;
                            threat.MagicThreat = 0;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (isPhysicalThread && canLame && !IsImmuneTo(threat, Spell.Lame))
                        {
                            spell = Spell.Lame;
                            threat.PhysicalThreat = 0;
                            spellTarget = threat.Monster;
                            break;
                        }
                        else if (isPhysicalThread && canBlind && !IsImmuneTo(threat, Spell.Blind) && !threat.Monster.Conditions.HasFlag(Condition.Blind))
                        {
                            spell = Spell.Blind;
                            threat.PhysicalThreat /= 2;
                            spellTarget = threat.Monster;
                            break;
                        }
                        // retry sleep if no irritate, lame or blind and threat is healthy
                        else if ((threat.Position < 12 || (threat.Position < 18 && threat.Monster.HitPoints.CurrentValue > threat.Monster.HitPoints.MaxValue * 9 / 10))
                            && canSleep && !IsImmuneTo(threat, Spell.Sleep))
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
                        if (lowFrac <= 30 && CanCast(Spell.MassHealing) && PartyMembers.All((a) => a.HitPoints.CurrentValue < a.HitPoints.TotalMaxValue * 3 / 4))
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
                            if (lowFrac <= 15 && greatHeal)
                                spell = Spell.GreatHealing;
                            else if (lowFrac <= 25 && (mediumHeal || greatHeal))
                                spell = mediumHeal ? Spell.MediumHealing : Spell.GreatHealing;
                            else if (lowFrac <= 35 && (smallHeal || mediumHeal))
                                spell = smallHeal ? Spell.SmallHealing : Spell.MediumHealing;
                            else if (lowFrac <= 50 && (handHeal || smallHeal))
                                spell = handHeal ? Spell.HealingHand : Spell.SmallHealing;
                            if (spell != Spell.None)
                            {
                                spellTarget = partyToHeal[0];
                                dontMove.Add(partyToHeal[0]);
                                partyToHeal.RemoveAt(0);
                            }
                        }
                    }

                    if (spell == Spell.None && partyConditions != Condition.None)
                    {
                        bool Cure(Condition cond, Spell spellOne, Spell spellAll)
                        {
                            if (!partyConditions.HasFlag(cond))
                                return false;

                            bool canOne = CanCast(spellOne);
                            bool canAll = CanCast(spellAll);
                            if (!canOne && !canAll)
                                return false;

                            var toCure = PartyMembers.Where(a => a.Conditions.HasFlag(cond)).ToArray();
                            if (toCure.Length == 1 || !canAll)
                            {
                                foreach (var target in toCure)
                                    if (!hasMoved.Contains(target))
                                    {
                                        spell = spellOne;
                                        spellTarget = target;
                                        dontMove.Add(target);
                                        break;
                                    }
                            }
                            else
                                spell = spellAll;
                            partyConditions &= ~cond;
                            return true;
                        }
                        if (!Cure(Condition.Panic, Spell.RemoveFear, Spell.RemovePanic))
                            if (!Cure(Condition.Lamed, Spell.RemoveRigidness, Spell.RemoveLamedness))
                                if (!Cure(Condition.Blind, Spell.RemoveShadows, Spell.RemoveBlindness))
                                    if (partyMember.Class != Class.Paladin)  // prefere paladin to attack
                                        if (!Cure(Condition.Sleep, Spell.WakeUp, Spell.None))
                                            if (!Cure(Condition.Irritated, Spell.RemoveIrritation, Spell.None))
                                                if (!Cure(Condition.Poisoned, Spell.RemovePoison, Spell.NeutralizePoison))
                                                    Cure(Condition.Diseased, Spell.RemovePain, Spell.RemoveDisease);
                    }

                    if (spell != Spell.None)
                        partyEmptyHealer |= partyMember.SpellPoints.CurrentValue - SpellInfos[spell].SP < SpellInfos[Spell.SmallHealing].SP;
                }

                if (spell != Spell.None)
                {
                    if (!hasAction)
                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                            Battle.CreateCastSpellParameter(spellTarget != null ? (uint)currentBattle.GetSlotFromCharacter(spellTarget) : 0, spell));
                    continue;
                }
            }

            // attack
            int position = currentBattle.GetSlotFromCharacter(partyMember!);
            if (CheckAbilityToAttack(out bool ranged, true))
                foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => a.Health).ThenByDescending(a => a.Position))
                    if ((ranged || (Math.Abs(threat.Position % 6 - position % 6) <= 1 && Math.Abs(threat.Position / 6 - position / 6) <= 1))
                        && !currentBattle.ImmuneToAttack(threat.Monster, partyMember))
                    {
                        hasAction = true;
                        SetPlayerBattleAction(Battle.BattleActionType.Attack, Battle.CreateAttackParameter((uint)threat.Position));
                        if (threat.Sleeping)    // wake up
                            currentBattle.CalculateAutoBattleInfo(threat.Monster, out threat.PhysicalThreat, out threat.MagicThreat, true);
                        threat.Health -= (uint)Math.Max(0, partyMember!.BaseAttackDamage + partyMember!.BonusAttackDamage - threat.Monster!.BaseDefense - threat.Monster!.BonusDefense);
                        break;
                    }
            currentPickingActionMember = partyMember; // set again as may changed by CheckAbilityToAttack

            // move to next enemy
            if (!hasAction && !ranged && partyMember.Conditions.CanMove() && !dontMove.Contains(partyMember))
                foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => Math.Abs(a.Position % 6 - position % 6)).ThenByDescending(a => a.Health))
                {
                    if (threat.Position < 12)
                        continue;

                    bool IsFree(int pos) => currentBattle!.IsBattleFieldEmpty(pos) && !AnyPlayerMovesTo(pos);
                    int threatCol = threat.Position % 6;
                    int threatRow = threat.Position / 6;
                    int fromCol = position % 6;
                    int maxDist = 1 + (int)partyMember!.Attributes[Data.Attribute.Speed].TotalCurrentValue / 80;
                    int step = threatCol < fromCol ? -1 : 1;
                    int newPosition = -1;
                    if (fromCol == threatCol)
                    {
                        if (IsFree(18 + threatCol))
                            newPosition = 18 + threatCol; 
                        else if (threatCol > 0 && threatCol <= 2 && IsFree(18 + threatCol - 1))
                            newPosition = 18 + threatCol - 1;
                        else if (threatCol < 5 && IsFree(18 + threatCol + 1))
                            newPosition = 18 + threatCol + 1;
                        else if (threatCol > 2 && IsFree(18 + threatCol - 1))
                            newPosition = 18 + threatCol - 1;
                    }
                    else if (fromCol + step == threatCol && IsFree(18 + fromCol))
                        newPosition = 18 + fromCol;
                    else if (threatRow == 3 || IsFree(18 + threatCol)
                        || (threatCol != 0 && IsFree(18 + threatCol - 1))
                        || (threatCol != 5 && IsFree(18 + threatCol + 1)))
                        for (int toCol = step < 0 ? Math.Max(Math.Max(0, threatCol - 1), fromCol - maxDist) : Math.Min(Math.Min(5, threatCol + 1), fromCol + maxDist); toCol != fromCol; toCol -= step)
                            if (IsFree(18 + toCol))
                            {
                                newPosition = 18 + toCol;
                                break;
                            }
                            else if (IsFree(24 + toCol) && (threatRow == 3 || (!(toCol == threatCol && IsFree(18 + toCol - step)) && !(toCol - step == threatCol))))
                            {
                                newPosition = 24 + toCol;
                                break;
                            }
                    if (newPosition != -1)
                    {
                        hasAction = true;
                        SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)newPosition));
                        hasMoved.Add(partyMember);
                        partyToHeal.Remove(partyMember);    // can not heal moved party member
                        break;
                    }
                }

            // nothing -> parry
            if (!hasAction && partyMember.Conditions.CanParry())
            {
                SetPlayerBattleAction(Battle.BattleActionType.Parry);
            }
        }

        ExecuteNextUpdateCycle(() => StartBattleRound(false));
    }
}