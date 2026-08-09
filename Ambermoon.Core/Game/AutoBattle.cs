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
//   - prefer damaged enemies
//   - prefer nearby enemies
// - use healing if required
//   - prefer healer over paladin
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

using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class Battle
{
    IRenderText? autoBattleRoundText = null;
    Dictionary<int, uint> lastRoundSpells = []; // Value = Action Parameter

    internal uint GetLastRoundSpell(int partyMemberIndex)
    {
        return lastRoundSpells.GetValueOrDefault(partyMemberIndex, uint.MaxValue);
    }

    internal void SetLastRoundSpell(int partyMemberIndex, uint actionParameter)
    {
        lastRoundSpells[partyMemberIndex] = actionParameter;
    }

    internal void RemoveLastRoundSpell(int partyMemberIndex)
    {
        lastRoundSpells.Remove(partyMemberIndex);
    }

    internal void ClearLastRoundSpells()
    {
        lastRoundSpells.Clear();
    }

    internal void ShowAutoBattleRoundText(bool show, int rounds)
    {
        autoBattleRoundText?.Delete();

        if (show)
        {
            autoBattleRoundText = layout.RenderView.RenderTextFactory.Create((byte)(layout.RenderView.GraphicInfoProvider.DefaultTextPaletteIndex - 1));
            autoBattleRoundText.Layer = layout.RenderView.GetLayer(Layer.Text);
            autoBattleRoundText.DisplayLayer = 201;
            autoBattleRoundText.Shadow = true;
            autoBattleRoundText.TextColor = TextColor.BrightGray;
            autoBattleRoundText.Text = layout.RenderView.TextProcessor.CreateText(rounds.ToString());
            autoBattleRoundText.Place(new Rect(Global.ButtonGridX + Button.Width * 2 + 17, Global.ButtonGridY + Button.Height + 5, 12, 7), TextAlign.Center);
            autoBattleRoundText.Visible = true;
        }
    }

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
            else spellThread = spell switch
            {
                Spell.LPStealer or Spell.SPStealer => (uint)monster.Level * 3 / 2,
                Spell.GhostWeapon or Spell.GhostInferno or Spell.MagicSwordAttack => (uint)(monster.BaseAttackDamage + monster.BonusAttackDamage),
                Spell.MagicalProjectile or Spell.MagicalArrows => monster.Level,
                Spell.Petrify or Spell.DissolveVictim => 200,
                Spell.CauseMadness => 100,
                Spell.Lame => 50,
                Spell.CauseAging or Spell.CauseDisease => 20,
                Spell.Irritate => 10,
                Spell.Poison or Spell.Sleep or Spell.Drug => 5,
                _ => 0,
            };

            if (spellInfo.Target == SpellTarget.AllEnemies)
                spellThread *= 3;
            else if (spellInfo.Target == SpellTarget.EnemyRow)
                spellThread *= 2;
            else if (spellInfo.Target == SpellTarget.EnemyRowInWeaponRange)
                spellThread = spellThread * 3 / 2;

            magicThreat = Math.Max(magicThreat, (int)spellThread);
        }

        if (monster.Conditions.HasFlag(Condition.Panic)
            || monster.Conditions.HasFlag(Condition.Petrified))
            physicalThreat = magicThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Irritated))
            magicThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Lamed))
            physicalThreat = 0;
        if (monster.Conditions.HasFlag(Condition.Crazy))
        {
            physicalThreat /= 2;
            magicThreat = 0;
        }
        if (game.Features.HasFlag(Features.ExtendedCurseEffects))
        {
            if (monster.Conditions.HasFlag(Condition.Blind))
                physicalThreat /= 2;
            if (monster.Conditions.HasFlag(Condition.Diseased))
                physicalThreat /= 2;
            if (monster.Conditions.HasFlag(Condition.Aging))
            {
                agingValues.TryGetValue(monster, out var aging);
                physicalThreat = physicalThreat * (100 - (int)aging) / 100;
            }
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
    static internal Graphic AutoBattleButton => new(32, 13, 0)
    {
        Data =
        [
            0,0,30,28,27,27,27,27,27,27,27,27,27,27,27,27,27,27,27,28,27,27,27,27,27,27,28,28,29,30,0,0,
            0,0,28,27,26,28,28,28,28,28,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,26,28,28,28,28,26,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,26,28,28,28,26,27,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,26,28,28,26,27,27,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,27,26,28,26,27,27,27,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,27,27,26,26,27,27,27,27,26,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,27,27,27,27,27,27,27,27,27,29,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,27,27,28,26,27,27,27,27,30,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,27,28,26,27,27,27,30,28,26,27,27,27,30,28,28,28,28,28,28,28,28,28,28,28,28,28,28,27,0,0,
            0,0,27,28,26,27,27,30,28,28,26,27,27,30,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,29,0,0,
            0,0,28,28,26,27,30,28,28,28,26,27,30,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,0,0,
            0,0,29,28,27,29,29,29,29,29,27,29,29,29,29,29,29,29,29,29,28,29,29,29,28,29,29,29,28,27,0,0,
        ]
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
            var battle = currentBattle!;
            int remainingRounds = CoreConfiguration.AutoBattleRounds;
            var orgBattleSpeed = currentBattle!.Speed;
            int orgPartyCount = PartyMembers.Count(a => a.Conditions.CanFight());
            Action roundFinish = null!;
            SetBattleSpeed(400);
            StartSequence();
            battle.ShowAutoBattleRoundText(false, 0);

            void EndAutoBattle(bool ended = false)
            {
                battle.BattleEnded -= BattleEnded;
                battle.RoundFinished -= roundFinish;
                EndSequence();
                SetBattleSpeed(orgBattleSpeed);

                if (!ended)
                    battle.ShowAutoBattleRoundText(true, CoreConfiguration.AutoBattleRounds);
            }

            void BattleEnded(BattleEndInfo _) => EndAutoBattle(ended: true);
            battle.BattleEnded += BattleEnded;
            battle.RoundFinished += roundFinish = () =>
            {
                if (orgPartyCount == PartyMembers.Count(a => a.Conditions.CanFight()))
                {
                    if (currentBattle != null && remainingRounds-- > 0)
                        AddAutoBattleActions(firstRound: false);
                    else
                        EndAutoBattle();
                }
                else
                {
                    EndAutoBattle();
                }
            };
        }

        if (currentBattle!.CanPartyMoveForward)
        {
            AdvanceParty(() => AddAutoBattleActions(firstRound: false));
            return;
        }

        // PartyMembers sorted by moving order
        var partyOrder = PartyMembers.Where(a => a.Conditions.CanSelect()).OrderByDescending(c => c.Attributes[Data.Attribute.Speed].TotalCurrentValue).ThenBy(c => c.Type).ToList();
        bool partyHasHealer = false;
        bool partyEmptyHealer = false;

        // command paladin after healer
        for (int i = 0, paladinIndex = -1; i < partyOrder.Count; i++)
        {
            if (partyOrder[i].Class == Class.Paladin || partyOrder[i].Class == Class.Healer)
            {
                partyHasHealer = true;
                partyEmptyHealer = partyOrder[i].SpellPoints.CurrentValue < SpellInfos[Spell.SmallHealing].SP;

                if (partyOrder[i].Class == Class.Paladin)
                {
                    paladinIndex = i;
                }
                else // Healer
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
        {
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
                        if (firstRound)
                            currentBattle.SetLastRoundSpell(slot, action.Parameter);

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

                        currentBattle.RemoveLastRoundSpell(slot);
                        break;
                    case Battle.BattleActionType.Parry:
                        break;
                    default:
                        continue;
                }

                roundPlayerBattleActions.Remove(slot);
            }

            #region Check inventory for spell point items

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

            #endregion

            #region Check inventory for healing items

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

            #endregion

            bool hasAction = false;

            #region Check magic

            if (partyMember.Conditions.CanCastSpell(Features))
            {
                Spell spell = Spell.None;
                Character? spellTarget = null;

                bool CanCast(Spell spell) => partyMember.HasSpell(spell) && partyMember.SpellPoints.CurrentValue >= spellInfos[spell].SP;
                bool IsImmuneTo(Monster monster, Spell spell) =>
                    (monster.SpellTypeImmunity & (SpellTypeImmunity)spellInfos[spell].SpellType) != 0
                    || monster.IsImmuneToSpell(spell, out var _, Features.HasFlag(Features.Elements));

                #region Mystic / Ranger

                if (partyMember.Class == Class.Mystic || partyMember.Class == Class.Ranger)
                {
                    if (partyMember.InventoryInaccessible)
                    {
                        // imitated monster -> use all we have starting with most powerful
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
                                foreach (var threat in threats.Where(a => !IsImmuneTo(a.Monster, myspell)))
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
                                var target = threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => a.Health).Where(a => !IsImmuneTo(a.Monster, myspell)).FirstOrDefault();
                                if (target != null)
                                {
                                    spell = myspell;
                                    spellTarget = target.Monster;
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion

                bool CheckLastRoundSpell()
                {
                    var lastRoundSpell = currentBattle.GetLastRoundSpell(slot);

                    if (lastRoundSpell != uint.MaxValue)
                    {
                        var lastSpell = Battle.GetCastSpell(lastRoundSpell);

                        if (lastSpell == Spell.None)
                        {
                            currentBattle.RemoveLastRoundSpell(slot);
                            return false;
                        }

                        var lastSpellInfo = spellInfos[lastSpell];

                        if (lastSpellInfo.SP <= partyMember.SpellPoints.CurrentValue)
                        {
                            if (lastSpellInfo.Target == SpellTarget.SingleEnemy)
                            {
                                var lastSpellTargetTile = Battle.GetTargetTileOrRowFromParameter(lastRoundSpell);

                                if (currentBattle.GetCharacterAt((int)lastSpellTargetTile) is Monster monster && !IsImmuneTo(monster, lastSpell))
                                {
                                    spell = lastSpell;
                                    spellTarget = monster;
                                    return true;
                                }
                            }
                            else if (lastSpellInfo.Target == SpellTarget.EnemyRow)
                            {
                                var lastSpellTargetRow = Battle.GetTargetTileOrRowFromParameter(lastRoundSpell);
                                var hasThreatsInRow = threats.Any(threat => threat.Position / 6 == lastSpellTargetRow && !IsImmuneTo(threat.Monster, lastSpell));

                                if (hasThreatsInRow)
                                {
                                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                                        Battle.CreateCastSpellParameter(lastSpellTargetRow, lastSpell));
                                    hasAction = true;
                                    return true;
                                }
                            }
                            else if (lastSpellInfo.Target == SpellTarget.AllEnemies)
                            {
                                spell = lastSpell;
                                return true;
                            }                            
                        }

                        currentBattle.RemoveLastRoundSpell(slot);
                    }

                    return false;
                }

                #region Alchemist

                if (partyMember.Class == Class.Alchemist)
                    CheckLastRoundSpell();

                #endregion

                #region Mage

                if (partyMember.Class == Class.Mage)
                {
                    if (!CheckLastRoundSpell())
                    {
                        bool CanCause(AutoBattleInfo target, Spell spell, Condition effect) =>
                            CanCast(spell) && !IsImmuneTo(target.Monster, spell) && !target.Monster.Conditions.HasFlag(effect);
                        bool ece = Features.HasFlag(Features.ExtendedCurseEffects);
                        foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenByDescending(a => a.Health).ThenBy(a => a.Position))
                        {
                            if (threat.Health < threat.Monster.HitPoints.TotalMaxValue / 2
                                || threat.Monster.Attributes[Data.Attribute.AntiMagic].TotalCurrentValue >= 75)
                                continue;
                            bool isPhysicalThread = threat.PhysicalThreat >= partyMaxHealth / 12;
                            bool isMagicThread = threat.MagicThreat >= partyMaxHealth / 12;
                            if (!isPhysicalThread && !isMagicThread)
                                continue;  // do not waste magic on too weak enemies

                            if (threats.Count > 1 && threat.Position < 12 // cast sleep only at back rows as front is attacked
                                && isPhysicalThread && (isMagicThread || threat.Monster.Skills[Skill.CriticalHit].TotalCurrentValue > 0)
                                && CanCause(threat, Spell.Sleep, Condition.Sleep))  // no action
                            {
                                spell = Spell.Sleep;
                                threat.MagicThreat = threat.PhysicalThreat = 1;
                                spellTarget = threat.Monster;
                                break;
                            }
                            else if (threat.MagicThreat >= threat.PhysicalThreat
                                && CanCause(threat, Spell.Irritate, Condition.Irritated)) // no magic
                            {
                                spell = Spell.Irritate;
                                threat.MagicThreat = 0;
                                spellTarget = threat.Monster;
                                break;
                            }
                            else if (threat.MagicThreat >= threat.PhysicalThreat
                                && CanCause(threat, Spell.CauseMadness, Condition.Crazy)) // no magic, random move/attack
                            {
                                spell = Spell.CauseMadness;
                                threat.MagicThreat = 0;
                                threat.PhysicalThreat /= 2;
                                break;
                            }
                            else if (isPhysicalThread && CanCause(threat, Spell.Lame, Condition.Lamed)) // no attack
                            {
                                spell = Spell.Lame;
                                threat.PhysicalThreat = 0;
                                spellTarget = threat.Monster;
                                break;
                            }
                            else if (isPhysicalThread && ece && CanCause(threat, Spell.Blind, Condition.Blind)) // attack fails
                            {
                                spell = Spell.Blind;
                                threat.PhysicalThreat /= 2;
                                spellTarget = threat.Monster;
                                break;
                            }
                            else if (isPhysicalThread && ece && CanCause(threat, Spell.CauseAging, Condition.Aging) // -10..-50% attacks & damage
                                && !threat.Monster.Conditions.HasFlag(Condition.Blind) && threat.Monster.HitPoints.CurrentValue > threat.Monster.HitPoints.MaxValue * 9 / 10)
                            {
                                spell = Spell.CauseAging;
                                threat.PhysicalThreat = threat.PhysicalThreat * 9 / 10;
                                spellTarget = threat.Monster;
                                break;
                            }
                            else if (isPhysicalThread && ece && CanCause(threat, Spell.CauseDisease, Condition.Diseased) // -50% damage
                                && !threat.Monster.Conditions.HasFlag(Condition.Aging))
                            {
                                spell = Spell.CauseDisease;
                                threat.PhysicalThreat /= 2;
                                spellTarget = threat.Monster;
                                break;
                            }
                            // retry sleep if no irritate, mad, lame, blind, disease or aging and threat is healthy
                            else if (threats.Count > 1 && (threat.Position < 12 || (threat.Position < 18 && threat.Monster.HitPoints.CurrentValue > threat.Monster.HitPoints.MaxValue * 9 / 10))
                                && CanCause(threat, Spell.Sleep, Condition.Sleep) && threats.Count > 1)
                            {
                                spell = Spell.Sleep;
                                threat.MagicThreat = threat.PhysicalThreat = 1;
                                spellTarget = threat.Monster;
                                break;
                            }
                        }
                    }
                }

                #endregion

                #region Healer / Paladin

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
                                {
                                    if (!hasMoved.Contains(target))
                                    {
                                        spell = spellOne;
                                        spellTarget = target;
                                        dontMove.Add(target);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                spell = spellAll;
                            }

                            partyConditions &= ~cond;

                            return true;
                        }

                        if (!Cure(Condition.Panic, Spell.RemoveFear, Spell.RemovePanic))
                            if (!Cure(Condition.Lamed, Spell.RemoveRigidness, Spell.RemoveLamedness))
                                if (!Cure(Condition.Blind, Spell.RemoveShadows, Spell.RemoveBlindness))
                                    if (partyMember.Class != Class.Paladin) // prefer paladin to attack
                                        if (!Cure(Condition.Sleep, Spell.WakeUp, Spell.None))
                                            if (!Cure(Condition.Irritated, Spell.RemoveIrritation, Spell.None))
                                                if (!Cure(Condition.Poisoned, Spell.RemovePoison, Spell.NeutralizePoison))
                                                    Cure(Condition.Diseased, Spell.RemovePain, Spell.RemoveDisease);
                    }

                    if (spell != Spell.None)
                        partyEmptyHealer |= partyMember.SpellPoints.CurrentValue - SpellInfos[spell].SP < SpellInfos[Spell.SmallHealing].SP;
                }

                #endregion

                if (spell != Spell.None)
                {
                    if (!hasAction)
                    {
                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                            Battle.CreateCastSpellParameter(spellTarget != null ? (uint)currentBattle.GetSlotFromCharacter(spellTarget) : 0, spell));
                    }

                    continue;
                }
            }
            else
            {
                currentBattle.RemoveLastRoundSpell(slot);
            }

            #endregion

            #region Attack

            int position = currentBattle.GetSlotFromCharacter(partyMember!);

            if (CheckAbilityToAttack(out bool ranged, true))
            {
                foreach (var threat in threats.OrderByDescending(a => a.PhysicalThreat + a.MagicThreat).ThenBy(a => a.Health).ThenByDescending(a => a.Position))
                {
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
                }
            }

            currentPickingActionMember = partyMember; // set again as may changed by CheckAbilityToAttack

            #endregion

            #region Move to next enemy

            if (!hasAction && !ranged && partyMember.Conditions.CanMove() && !dontMove.Contains(partyMember))
            {
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
            }

            #endregion

            #region Nothing to do -> parry

            if (!hasAction && partyMember.Conditions.CanParry())
            {
                SetPlayerBattleAction(Battle.BattleActionType.Parry);
            }

            #endregion
        }

        ExecuteNextUpdateCycle(() => StartBattleRound(withoutPlayerActions: false));
    }
}
