using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using Attribute = Ambermoon.Data.Attribute;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    Spell pickedSpell = Spell.None;
    uint? spellItemSlotIndex = null;
    bool? spellItemIsEquipped = null;
    uint? blinkCharacterPosition = null;
    SpellAnimation? currentAnimation = null;
    private protected readonly Dictionary<Spell, SpellInfo> spellInfos;
    readonly int[] spellListScrollOffsets = new int[MaxPartyMembers];

    internal IReadOnlyDictionary<Spell, SpellInfo> SpellInfos => spellInfos.AsReadOnly();

    /// <summary>
    /// Adds a spell effect.
    /// </summary>
    /// <param name="spell">Spell</param>
    /// <param name="caster">Casting party member or monster.</param>
    /// <param name="target">Party member or item or null.</param>
    /// <param name="finishAction">Action to call after effect was applied.</param>
    /// <param name="checkFail">If true check if the spell cast fails.</param>
    internal void ApplySpellEffect(Spell spell, Character caster, object target, Action? finishAction = null, bool checkFail = true)
    {
        if (target == null)
            ApplySpellEffect(spell, caster, finishAction, checkFail);
        else if (target is Character character)
            ApplySpellEffect(spell, caster, character, finishAction, checkFail);
        else if (target is ItemSlot itemSlot)
            ApplySpellEffect(spell, caster, itemSlot, finishAction, checkFail);
        else
            throw new AmbermoonException(ExceptionScope.Application, $"Invalid spell target type: {target.GetType()}");
    }


    void Cast(Action action, Action? finishAction = null, Action? failAction = null, bool checkFail = true)
    {
        failAction ??= () => ShowMessagePopup(DataNameProvider.TheSpellFailed);

        if (finishAction == null)
        {
            if (checkFail)
                TrySpell(action, failAction);
            else
                action?.Invoke();
        }
        else
        {
            if (checkFail)
            {
                TrySpell(() =>
                {
                    action?.Invoke();
                    finishAction();
                }, () =>
                {
                    failAction?.Invoke();
                    finishAction();
                });
            }
            else
            {
                action?.Invoke();
                finishAction();
            }
        }
    }

    void ApplySpellEffect(Spell spell, Character caster, Action? finishAction, bool checkFail)
    {
        CurrentSpellTarget = null;

        void Cast(Action action, Action? finishAction = null, Action? failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        switch (spell)
        {
            case Spell.Light:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 1 (Light radius 1)
                Cast(() => ActivateLight(30, 1), finishAction);
                break;
            case Spell.MagicalTorch:
                // Duration: 60 (300 minutes = 5h)
                // Level: 1 (Light radius 1)
                Cast(() => ActivateLight(60, 1), finishAction);
                break;
            case Spell.MagicalLantern:
                // Duration: 120 (600 minutes = 10h)
                // Level: 2 (Light radius 2)
                Cast(() => ActivateLight(120, 2), finishAction);
                break;
            case Spell.MagicalSun:
                // Duration: 180 (900 minutes = 15h)
                // Level: 3 (Light radius 3)
                Cast(() => ActivateLight(180, 3), finishAction);
                break;
            case Spell.Jump:
                Cast(Jump, finishAction);
                break;
            case Spell.WordOfMarking:
            {
                Cast(() =>
                {
                    if (caster is PartyMember partyMember)
                    {
                        partyMember.MarkOfReturnMapIndex = (ushort)(Map.IsWorldMap ?
                            renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index : Map.Index);
                        partyMember.MarkOfReturnX = (ushort)(player.Position.X + 1); // stored 1-based
                        partyMember.MarkOfReturnY = (ushort)(player.Position.Y + 1); // stored 1-based
                        ShowMessagePopup(DataNameProvider.MarksPosition, finishAction);
                    }
                    else
                    {
                        finishAction?.Invoke();
                    }
                }, null, finishAction);
                break;
            }
            case Spell.WordOfReturning:
            {
                Cast(() =>
                {
                    if (caster is PartyMember partyMember)
                    {
                        if (partyMember.MarkOfReturnMapIndex == 0)
                        {
                            ShowMessagePopup(DataNameProvider.HasntMarkedAPosition, finishAction);
                        }
                        else
                        {
                            void Return()
                            {
                                Teleport(partyMember.MarkOfReturnMapIndex, partyMember.MarkOfReturnX, partyMember.MarkOfReturnY, player.Direction, out _, true);
                                finishAction?.Invoke();
                            }
                            ShowMessagePopup(DataNameProvider.ReturnToMarkedPosition, () =>
                            {
                                var targetMap = MapManager.GetMap(partyMember.MarkOfReturnMapIndex);
                                // Note: The original fades always if the map index does not match.
                                // But we improve it here a bit so that moving inside the same world map won't fade.
                                if (targetMap.Index == Map.Index || (targetMap.IsWorldMap && Map.IsWorldMap && targetMap.World == Map.World))
                                    Return();
                                else
                                    Fade(Return);
                            });
                        }
                    }
                    else
                    {
                        finishAction?.Invoke();
                    }
                }, null, finishAction);
                break;
            }
            case Spell.MagicalShield:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 10 (10% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 30, 10), finishAction);
                break;
            case Spell.MagicalWall:
                // Duration: 90 (450 minutes = 7h30m)
                // Level: 20 (20% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 90, 20), finishAction);
                break;
            case Spell.MagicalBarrier:
                // Duration: 180 (900 minutes = 15h)
                // Level: 30 (30% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30), finishAction);
                break;
            case Spell.MagicalWeapon:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 10 (10% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 30, 10), finishAction);
                break;
            case Spell.MagicalAssault:
                // Duration: 90 (450 minutes = 7h30m)
                // Level: 20 (20% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 90, 20), finishAction);
                break;
            case Spell.MagicalAttack:
                // Duration: 180 (900 minutes = 15h)
                // Level: 30 (30% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30), finishAction);
                break;
            case Spell.Levitation:
                Cast(Levitate, finishAction);
                break;
            case Spell.Rope:
            {
                if (!is3D)
                {
                    ShowMessagePopup(DataNameProvider.CannotClimbHere, finishAction);
                }
                else
                {
                    Levitate(() => ShowMessagePopup(DataNameProvider.CannotClimbHere, finishAction), false);
                }
                break;
            }
            case Spell.AntiMagicWall:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 15 (15% anti-magic protection)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 30, 15), finishAction);
                break;
            case Spell.AntiMagicSphere:
                // Duration: 180 (900 minutes = 15h)
                // Level: 25 (25% anti-magic protection)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25), finishAction);
                break;
            case Spell.AlchemisticGlobe:
                // Duration: 180 (900 minutes = 15h)
                Cast(() =>
                {
                    ActivateLight(180, 3);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25);
                }, finishAction);
                break;
            case Spell.Knowledge:
                // Duration: 30 (150 minutes = 2h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 30, 20), finishAction);
                break;
            case Spell.Clairvoyance:
                // Duration: 90 (450 minutes = 7h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 90, 40), finishAction);
                break;
            case Spell.SeeTheTruth:
                // Duration: 180 (900 minutes = 15h)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 60), finishAction);
                break;
            case Spell.MapView:
                Cast(() => OpenMiniMap(finishAction), null, finishAction);
                break;
            case Spell.MagicalCompass:
            {
                Cast(() =>
                {
                    Pause();
                    var popup = layout.OpenPopup(new Position(48, 64), 4, 4);
                    TrapMouse(popup.ContentArea);
                    popup.AddImage(new Rect(64, 80, 32, 32), Graphics.GetUIGraphicIndex(UIGraphic.Compass), Layer.UI, 1, UIPaletteIndex);
                    var text = popup.AddText(new Rect(59, 93, 42, 7), layout.GetCompassString(), TextColor.BrightGray);
                    text.Clip(new Rect(64, 93, 32, 7));
                    popup.Closed += () =>
                    {
                        UntrapMouse();
                        Resume();
                        finishAction?.Invoke();
                    };
                }, null, finishAction);
                break;
            }
            case Spell.FindTraps:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = false,
                    PersonsVisible = false,
                    TrapsVisible = true,
                    ShowGotoPoints = true
                }, finishAction), null, finishAction);
                break;
            case Spell.FindMonsters:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = true,
                    PersonsVisible = false,
                    TrapsVisible = false,
                    ShowGotoPoints = true
                }, finishAction), null, finishAction);
                break;
            case Spell.FindPersons:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = false,
                    PersonsVisible = true,
                    TrapsVisible = false,
                    ShowGotoPoints = true
                }, finishAction), null, finishAction);
                break;
            case Spell.FindSecretDoors:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = true,
                    MonstersVisible = false,
                    PersonsVisible = false,
                    TrapsVisible = false,
                    ShowGotoPoints = true
                }, finishAction), null, finishAction);
                break;
            case Spell.MysticalMapping:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = true,
                    MonstersVisible = true,
                    PersonsVisible = true,
                    TrapsVisible = true,
                    ShowGotoPoints = true
                }, finishAction), null, finishAction);
                break;
            case Spell.MysticalMapI:
                // Duration: 32 (160 minutes = 2h40m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 32, 1), finishAction);
                break;
            case Spell.MysticalMapII:
                // Duration: 60 (300 minutes = 5h)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 60, 1), finishAction);
                break;
            case Spell.MysticalMapIII:
                // Duration: 90 (450 minutes = 7h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 90, 1), finishAction);
                break;
            case Spell.MysticalGlobe:
                // Duration: 180 (900 minutes = 15h)
                Cast(() =>
                {
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 60);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 180, 1);
                }, finishAction);
                break;
            case Spell.Lockpicking:
                // Do nothing. Can be used by Thief/Ranger but has no effect in Ambermoon.
                finishAction?.Invoke();
                break;
            case Spell.MountWasp:
                ShowMessagePopup(DataNameProvider.MountTheWasp, () => CloseWindow(() => ActivateTransport(TravelType.Wasp)), TextAlign.Left);
                break;
            case Spell.CallEagle:
                ShowMessagePopup(DataNameProvider.BlowsTheFlute, () =>
                {
                    CloseWindow(() =>
                    {
                        StartSequence();
                        var travelInfoEagle = renderView.GameData.GetTravelGraphicInfo(TravelType.Eagle, CharacterDirection.Right);
                        var currentTravelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, player.Direction);
                        int diffX = (int)travelInfoEagle.OffsetX - (int)currentTravelInfo.OffsetX;
                        int diffY = (int)travelInfoEagle.OffsetY - (int)currentTravelInfo.OffsetY;
                        var targetPosition = player2D.DisplayArea.Position + new Position(diffX, diffY);
                        var position = new Position(Global.Map2DViewX - (int)travelInfoEagle.Width, targetPosition.Y - (int)travelInfoEagle.Height);
                        var eagle = layout.AddMapCharacterSprite(new Rect(position, new Size((int)travelInfoEagle.Width, (int)travelInfoEagle.Height)),
                            Graphics.TravelGraphicOffset + (uint)TravelType.Eagle * 4 + 1, ushort.MaxValue);
                        eagle.ClipArea = Map2DViewArea;
                        AddTimedEvent(TimeSpan.FromMilliseconds(200), AnimateEagle);
                        void AnimateEagle()
                        {
                            if (position.X < targetPosition.X)
                                position.X = Math.Min(targetPosition.X, position.X + 12);
                            if (position.Y < targetPosition.Y)
                                position.Y = Math.Min(targetPosition.Y, position.Y + 5);

                            eagle.X = position.X;
                            eagle.Y = position.Y;

                            if (position == targetPosition)
                            {
                                EndSequence();
                                eagle.Delete();
                                ActivateTransport(TravelType.Eagle);
                                // Update direction to right
                                player.Direction = CharacterDirection.Right; // Set this before player2D.MoveTo!
                                player2D.MoveTo(Map, (uint)player2D.Position.X, (uint)player2D.Position.Y, CurrentTicks, true, CharacterDirection.Right);
                                finishAction?.Invoke();
                            }
                            else
                            {
                                AddTimedEvent(TimeSpan.FromMilliseconds(40), AnimateEagle);
                            }
                        }
                    });
                }, TextAlign.Left);
                break;
            case Spell.PlayElfHarp:
                OpenMusicList(finishAction);
                break;
            case Spell.MagicalMap:
                // TODO: In original this has no effect. Maybe it was planned to show
                // the real map that was inside the original package.
                // For now we show the minimap instead.
                OpenMiniMap(finishAction);
                break;
            case Spell.SelfHealing:
            case Spell.SelfReviving:
                ApplySpellEffect(spell, caster, caster, finishAction, checkFail);
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no spell without target.");
        }
    }

    void TrySpell(Action successAction, Action failAction)
    {
        long chance = CurrentPartyMember.Skills[Skill.UseMagic].TotalCurrentValue;

        if (Features.HasFlag(Features.ExtendedCurseEffects) &&
            CurrentPartyMember.Conditions.HasFlag(Condition.Drugged))
            chance -= 25;

        if (RollDice100() < chance)
            successAction?.Invoke();
        else
            failAction?.Invoke();
    }

    void TrySpell(Action successAction)
    {
        TrySpell(successAction, () => ShowMessagePopup(DataNameProvider.TheSpellFailed));
    }

    void ApplySpellEffect(Spell spell, Character caster, ItemSlot itemSlot, Action finishAction, bool checkFail)
    {
        CurrentSpellTarget = null;

        void Cast(Action action, Action finishAction = null, Action failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        void PlayItemMagicAnimation(Action animationFinishAction = null)
        {
            ItemAnimation.Play(this, renderView, ItemAnimation.Type.Enchant, layout.GetItemSlotPosition(itemSlot, true),
                animationFinishAction ?? finishAction, TimeSpan.FromMilliseconds(50));
        }

        void Error(string message)
        {
            EndSequence();
            ShowMessagePopup(message, finishAction, TextAlign.Left);
        }

        switch (spell)
        {
            case Spell.Identification:
            {
                Cast(() =>
                {
                    itemSlot.Flags |= ItemSlotFlags.Identified;
                    PlayItemMagicAnimation(() =>
                    {
                        EndSequence();
                        UntrapMouse();
                        ShowItemPopup(itemSlot, finishAction);
                    });
                }, null, () =>
                {
                    EndSequence();
                    ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                });
                break;
            }
            case Spell.ChargeItem:
            {
                // Note: Even broken items can be charged.
                var item = ItemManager.GetItem(itemSlot.ItemIndex);
                if (item.Spell == Spell.None || item.MaxCharges == 0)
                {
                    Error(DataNameProvider.ThisIsNotAMagicalItem);
                    return;
                }
                if (itemSlot.NumRemainingCharges >= item.MaxCharges)
                {
                    Error(DataNameProvider.ItemAlreadyFullyCharged);
                    return;
                }
                if (item.MaxRecharges != 0 && item.MaxRecharges != 255 && itemSlot.RechargeTimes >= item.MaxRecharges)
                {
                    Error(DataNameProvider.CannotRechargeAnymore);
                    return;
                }
                Cast(() =>
                {
                    itemSlot.NumRemainingCharges += RandomInt(1, Math.Min(item.MaxCharges - itemSlot.NumRemainingCharges, caster.Level));
                    PlayItemMagicAnimation(finishAction);
                }, null, () =>
                {
                    EndSequence();
                    ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                    {
                        if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed)) // Don't destroy cursed items via failed charging
                            layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                        else
                            finishAction?.Invoke();
                    });
                });
                break;
            }
            case Spell.RepairItem:
            {
                if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                {
                    Error(DataNameProvider.ItemIsNotBroken);
                    return;
                }
                Cast(() =>
                {
                    itemSlot.Flags &= ~ItemSlotFlags.Broken;
                    layout.UpdateItemSlot(itemSlot);
                    PlayItemMagicAnimation(finishAction);
                }, null, () =>
                {
                    EndSequence();
                    ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                    {
                        layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                    });
                });
                break;
            }
            case Spell.DuplicateItem:
            {
                // Note: Even broken items can be duplicated. The broken state is also duplicated.
                var item = ItemManager.GetItem(itemSlot.ItemIndex);
                if (!item.Flags.HasFlag(ItemFlags.Cloneable))
                {
                    Error(DataNameProvider.CannotBeDuplicated);
                    return;
                }
                Cast(() =>
                {
                    PlayItemMagicAnimation(() =>
                    {
                        bool couldDuplicate = false;
                        var inventorySlots = CurrentInventory.Inventory.Slots;

                        if (item.Flags.HasFlag(ItemFlags.Stackable))
                        {
                            // Look for slots with free stacks
                            var freeSlot = inventorySlots.FirstOrDefault(s => s.ItemIndex == item.Index && s.Amount < 99);

                            if (freeSlot != null)
                            {
                                ++freeSlot.Amount;
                                layout.UpdateItemSlot(freeSlot);
                                couldDuplicate = true;
                            }
                        }

                        if (!couldDuplicate)
                        {
                            // Look for empty slots
                            var freeSlot = inventorySlots.FirstOrDefault(s => s.Empty);

                            if (freeSlot != null)
                            {
                                var copy = itemSlot.Copy();
                                copy.Amount = 1;
                                freeSlot.Replace(copy);
                                layout.UpdateItemSlot(freeSlot);
                                couldDuplicate = true;
                            }
                        }

                        if (!couldDuplicate)
                        {
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.NoRoomForItem, finishAction);
                        }
                        else
                        {
                            finishAction?.Invoke();
                        }
                    });
                }, null, () =>
                {
                    EndSequence();
                    ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                    {
                        if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed)) // Don't destroy cursed items via failed duplicating
                            layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                        else
                            finishAction?.Invoke();
                    });
                });
                break;
            }
            case Spell.RemoveCurses:
            {
                void Fail()
                {
                    EndSequence();
                    ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                }

                Cast(() =>
                {
                    if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed))
                    {
                        Fail();
                    }
                    else
                    {
                        PlayItemMagicAnimation(() =>
                        {
                            layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(10), false, () =>
                            {
                                EndSequence();
                                finishAction?.Invoke();
                            });
                        });
                    }
                }, null, Fail);
                break;
            }
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no item-targeted spell.");
        }
    }

    void ExchangeExp(PartyMember caster, PartyMember target, Action finishAction)
    {
        uint casterExp = caster.ExperiencePoints;
        uint targetExp = target.ExperiencePoints;

        if (casterExp == targetExp)
            return;

        if (caster.MaxReachedLevel == 0)
            caster.MaxReachedLevel = caster.Level;
        if (target.MaxReachedLevel == 0)
            target.MaxReachedLevel = target.Level;

        var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);
        var initialCaster = initialSavegame.PartyMembers[caster.Index];
        var initialTarget = initialSavegame.PartyMembers[target.Index];

        static void Init(Character character, Character initialCharacter)
        {
            character.Level = initialCharacter.Level;
            character.HitPoints.CurrentValue = initialCharacter.HitPoints.CurrentValue;
            character.HitPoints.MaxValue = initialCharacter.HitPoints.MaxValue;
            character.SpellPoints.CurrentValue = initialCharacter.SpellPoints.CurrentValue;
            character.SpellPoints.MaxValue = initialCharacter.SpellPoints.MaxValue;
            character.ExperiencePoints = 0;
            character.AttacksPerRound = 1;

            while (character.Level > 1)
            {
                --character.Level;
                character.HitPoints.CurrentValue -= character.HitPointsPerLevel;
                character.HitPoints.MaxValue -= character.HitPointsPerLevel;
                character.SpellPoints.CurrentValue -= character.SpellPointsPerLevel;
                character.SpellPoints.MaxValue -= character.SpellPointsPerLevel;
            }
        }

        Init(caster, initialCaster);
        Init(target, initialTarget);

        AddExperience(caster, targetExp, () =>
        {
            AddExperience(target, casterExp, () =>
            {
                UpdateCharacterInfo();
                layout.FillCharacterBars(caster);
                layout.FillCharacterBars(target);
                finishAction?.Invoke();
            });
        });
    }

    void ApplySpellEffect(Spell spell, Character caster, Character target, Action finishAction, bool checkFail)
    {
        CurrentSpellTarget = target;

        void Cast(Action action, Action finishAction = null, Action failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        switch (spell)
        {
            case Spell.Hurry:
            case Spell.MassHurry:
                // Note: This is handled by battle code
                finishAction?.Invoke();
                break;
            case Spell.RemoveFear:
            case Spell.RemovePanic:
                Cast(() => RemoveCondition(Condition.Panic, target), finishAction);
                break;
            case Spell.RemoveShadows:
            case Spell.RemoveBlindness:
                Cast(() => RemoveCondition(Condition.Blind, target), finishAction);
                break;
            case Spell.RemovePain:
            case Spell.RemoveDisease:
                Cast(() => RemoveCondition(Condition.Diseased, target), finishAction);
                break;
            case Spell.RemovePoison:
            case Spell.NeutralizePoison:
                Cast(() => RemoveCondition(Condition.Poisoned, target), finishAction);
                break;
            case Spell.HealingHand:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 10), finishAction); // 10%
                break;
            case Spell.SmallHealing:
            case Spell.MassHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 4), finishAction); // 25%
                break;
            case Spell.MediumHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 2), finishAction); // 50%
                break;
            case Spell.GreatHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue * 3 / 4), finishAction); // 75%
                break;
            case Spell.RemoveRigidness:
            case Spell.RemoveLamedness:
                Cast(() => RemoveCondition(Condition.Lamed, target), finishAction);
                break;
            case Spell.HealAging:
            case Spell.StopAging:
                Cast(() => RemoveCondition(Condition.Aging, target), finishAction);
                break;
            case Spell.StoneToFlesh:
                Cast(() => RemoveCondition(Condition.Petrified, target), finishAction);
                break;
            case Spell.WakeUp:
                Cast(() => RemoveCondition(Condition.Sleep, target), finishAction);
                break;
            case Spell.RemoveIrritation:
                Cast(() => RemoveCondition(Condition.Irritated, target), finishAction);
                break;
            case Spell.RemoveDrugged:
                Cast(() => RemoveCondition(Condition.Drugged, target), finishAction);
                break;
            case Spell.RemoveMadness:
                Cast(() => RemoveCondition(Condition.Crazy, target), finishAction);
                break;
            case Spell.RestoreStamina:
                Cast(() => RemoveCondition(Condition.Exhausted, target), finishAction);
                break;
            case Spell.CreateFood:
                Cast(() => ++target.Food, finishAction);
                break;
            case Spell.ExpExchange:
                Cast(() => ExchangeExp(caster as PartyMember, target as PartyMember, finishAction), null, finishAction);
                break;
            case Spell.SelfHealing:
                Cast(() =>
                {
                    if (target.Alive)
                        Heal(5 + target.HitPoints.TotalMaxValue / 4); // 5 HP + 25% of MaxHP
                }, finishAction);
                break;
            case Spell.Resurrection:
            {
                Cast(() =>
                {
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.HitPoints.CurrentValue = target.HitPoints.TotalMaxValue;
                    PartyMemberRevived(target as PartyMember, finishAction, false);
                }, null, finishAction);
                break;
            }
            case Spell.SelfReviving:
            case Spell.WakeTheDead:
            {
                if (!(target is PartyMember targetPlayer))
                {
                    // Should not happen
                    finishAction?.Invoke();
                    return;
                }
                void Revive()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadCorpse) ||
                        target.Conditions.HasFlag(Condition.DeadAshes) ||
                        target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        if (target.Alive)
                            ShowMessagePopup(DataNameProvider.IsNotDead, finishAction);
                        else
                            ShowMessagePopup(DataNameProvider.CannotBeResurrected, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.HitPoints.CurrentValue = 1;
                    PartyMemberRevived(targetPlayer, finishAction, true, spell == Spell.SelfReviving);
                }
                if (checkFail)
                {
                    TrySpell(Revive, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (spell != Spell.SelfReviving && target.Conditions.HasFlag(Condition.DeadCorpse))
                            {
                                target.Conditions &= ~Condition.DeadCorpse;
                                target.Conditions |= Condition.DeadAshes;
                                ShowMessagePopup(DataNameProvider.BodyBurnsUp, finishAction);
                            }
                            else
                            {
                                finishAction?.Invoke();
                            }
                        });
                    });
                }
                else
                {
                    Revive();
                }
                break;
            }
            case Spell.ChangeAshes:
            {
                if (!(target is PartyMember targetPlayer))
                {
                    // Should not happen
                    finishAction?.Invoke();
                    return;
                }
                void TransformToBody()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadAshes) ||
                        target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        ShowMessagePopup(DataNameProvider.IsNotAsh, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadAshes;
                    target.Conditions |= Condition.DeadCorpse;
                    ShowMessagePopup(DataNameProvider.AshesChangedToBody, finishAction);
                }
                if (checkFail)
                {
                    TrySpell(TransformToBody, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (target.Conditions.HasFlag(Condition.DeadAshes))
                            {
                                target.Conditions &= ~Condition.DeadAshes;
                                target.Conditions |= Condition.DeadDust;
                                ShowMessagePopup(DataNameProvider.AshesFallToDust, finishAction);
                            }
                            else
                            {
                                finishAction?.Invoke();
                            }
                        });
                    });
                }
                else
                {
                    TransformToBody();
                }
                break;
            }
            case Spell.ChangeDust:
            {
                if (!(target is PartyMember targetPlayer))
                {
                    // Should not happen
                    finishAction?.Invoke();
                    return;
                }
                void TransformToAshes()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        ShowMessagePopup(DataNameProvider.IsNotDust, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadDust;
                    target.Conditions |= Condition.DeadAshes;
                    ShowMessagePopup(DataNameProvider.DustChangedToAshes, finishAction);
                }
                if (checkFail)
                {
                    TrySpell(TransformToAshes, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                    });
                }
                else
                {
                    TransformToAshes();
                }
                break;
            }
            case Spell.SpellPointsI:
                FillSP(target.SpellPoints.TotalMaxValue / 10); // 10%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsII:
                FillSP(target.SpellPoints.TotalMaxValue / 4); // 25%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsIII:
                FillSP(target.SpellPoints.TotalMaxValue / 2); // 50%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsIV:
                FillSP(target.SpellPoints.TotalMaxValue * 3 / 4); // 75%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsV:
                FillSP(target.SpellPoints.TotalMaxValue); // 100%
                finishAction?.Invoke();
                break;
            case Spell.AllHealing:
            {
                void HealAll()
                {
                    // Removes all curses and heals full LP
                    Heal(target.HitPoints.TotalMaxValue);
                    foreach (var condition in EnumHelper.GetValues<Condition>())
                    {
                        if (condition != Condition.None && target.Conditions.HasFlag(condition))
                            RemoveCondition(condition, target);
                    }
                    finishAction?.Invoke();
                }
                if (!target.Alive)
                {
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.Conditions &= ~Condition.DeadAshes;
                    target.Conditions &= ~Condition.DeadDust;
                    target.HitPoints.CurrentValue = 1;
                    PartyMemberRevived(target as PartyMember, HealAll);
                }
                else
                {
                    HealAll();
                }
                break;
            }
            case Spell.AddStrength:
                IncreaseAttribute(Attribute.Strength);
                finishAction?.Invoke();
                break;
            case Spell.AddIntelligence:
                IncreaseAttribute(Attribute.Intelligence);
                finishAction?.Invoke();
                break;
            case Spell.AddDexterity:
                IncreaseAttribute(Attribute.Dexterity);
                finishAction?.Invoke();
                break;
            case Spell.AddSpeed:
                IncreaseAttribute(Attribute.Speed);
                finishAction?.Invoke();
                break;
            case Spell.AddStamina:
                IncreaseAttribute(Attribute.Stamina);
                finishAction?.Invoke();
                break;
            case Spell.AddCharisma:
                IncreaseAttribute(Attribute.Charisma);
                finishAction?.Invoke();
                break;
            case Spell.AddLuck:
                IncreaseAttribute(Attribute.Luck);
                finishAction?.Invoke();
                break;
            case Spell.AddAntiMagic:
                IncreaseAttribute(Attribute.AntiMagic);
                finishAction?.Invoke();
                break;
            case Spell.DecreaseAge:
                if (target.Alive && !target.Conditions.HasFlag(Condition.Petrified) && target.Attributes[Attribute.Age].CurrentValue > 18)
                {
                    target.Attributes[Attribute.Age].CurrentValue = (uint)Math.Max(18, (int)target.Attributes[Attribute.Age].CurrentValue - RandomInt(1, 10));

                    if (CurrentWindow.Window == Window.Inventory && CurrentInventory == target)
                        UpdateCharacterInfo();
                }
                finishAction?.Invoke();
                break;
            case Spell.Drugs:
                if (target is PartyMember partyMember)
                    AddCondition(Condition.Drugged, partyMember);
                finishAction?.Invoke();
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no character-targeted spell.");
        }

        void IncreaseAttribute(Attribute attribute)
        {
            if (target.Alive)
            {
                var value = target.Attributes[attribute];
                value.CurrentValue = Math.Min(value.CurrentValue + (uint)RandomInt(1, 5), value.MaxValue);
                UpdateCharacterInfo();
            }
        }

        void Heal(uint amount)
        {
            target.Heal(amount);

            if (target is PartyMember partyMember)
            {
                layout.FillCharacterBars(partyMember);

                if (CurrentInventory == partyMember)
                    UpdateCharacterInfo();
            }
        }

        void FillSP(uint amount)
        {
            target.SpellPoints.CurrentValue = Math.Min(target.SpellPoints.TotalMaxValue, target.SpellPoints.CurrentValue + amount);
            layout.FillCharacterBars((target as PartyMember)!);

            if (CurrentInventory == target)
                UpdateCharacterInfo();
        }
    }

    internal void UseSpell(PartyMember caster, Spell spell, ItemGrid? itemGrid, bool fromItem, Action<Action>? consumeHandler = null)
    {
        CurrentCaster = caster;
        CurrentSpellTarget = null;

        // Some special care for the mystic map spells
        if (!is3D && spell >= Spell.FindTraps && spell <= Spell.MysticalMapping)
        {
            ShowMessagePopup(DataNameProvider.UseSpellOnlyInCitiesOrDungeons);
            return;
        }

        if (Map!.Flags.HasFlag(MapFlags.NoMarkOrReturn) && (spell == Spell.WordOfMarking || spell == Spell.WordOfReturning))
        {
            ShowMessagePopup(DataNameProvider.CannotUseItHere);
            return;
        }

        if (!Map.Flags.HasFlag(MapFlags.Automapper))
        {
            if (spell == Spell.MapView)
            {
                ShowMessagePopup(DataNameProvider.MapViewNotWorkingHere);
                return;
            }

            if (spell == Spell.FindMonsters ||
                spell == Spell.FindPersons ||
                spell == Spell.FindSecretDoors ||
                spell == Spell.FindTraps ||
                spell == Spell.MysticalMapping)
            {
                ShowMessagePopup(DataNameProvider.AutomapperNotWorkingHere);
                return;
            }
        }

        var spellInfo = SpellInfos[spell];

        void ConsumeSP()
        {
            if (!fromItem) // Item spells won't consume SP
            {
                caster.SpellPoints.CurrentValue -= SpellInfos.GetSPCost(Features, spell, caster);
                layout.FillCharacterBars(caster);
            }
        }

        void SpellFinished() => CurrentSpellTarget = null;

        bool checkFail = !fromItem; // Item spells can't fail

        switch (spellInfo.Target)
        {
            case SpellTarget.SingleFriend:
            {
                Pause();
                layout.OpenTextPopup(ProcessText(DataNameProvider.BattleMessageWhichPartyMemberAsTarget), null, true, false, false, TextAlign.Center);
                PickTargetPlayer();
                void TargetPlayerPicked(int characterSlot)
                {
                    this.TargetPlayerPicked -= TargetPlayerPicked;
                    ClosePopup();
                    UntrapMouse();
                    InputEnable = true;
                    if (!WindowActive)
                        Resume();

                    if (characterSlot != -1)
                    {
                        bool reviveSpell = spell >= Spell.WakeTheDead && spell <= Spell.ChangeDust;
                        var target = GetPartyMember(characterSlot);

                        void Consume()
                        {
                            ConsumeSP();

                            if (spell == Spell.ExpExchange)
                            {
                                var target = GetPartyMember(characterSlot);

                                if (caster.Race == Race.Animal || target.Race == Race.Animal)
                                {
                                    ShowMessagePopup(DataNameProvider.CannotExchangeExpWithAnimals);
                                    return;
                                }

                                if (caster.Index == target.Index)
                                {
                                    // Changing exp with self doesn't change anything but also will
                                    // not require a message.
                                    return;
                                }

                                if (!caster.Alive || !target.Alive)
                                {
                                    ShowMessagePopup(DataNameProvider.CannotExchangeExpWithDead);
                                    return;
                                }
                            }

                            void Cast()
                            {
                                if (target != null && (reviveSpell || spell == Spell.AllHealing || target.Alive))
                                {
                                    if (reviveSpell)
                                    {
                                        ApplySpellEffect(spell, caster, target, SpellFinished, checkFail);
                                    }
                                    else
                                    {
                                        currentAnimation?.Destroy();
                                        currentAnimation = new SpellAnimation(this, layout);
                                        currentAnimation.CastOn(spell, target, () =>
                                        {
                                            currentAnimation.Destroy();
                                            currentAnimation = null;
                                            ApplySpellEffect(spell, caster, target, SpellFinished, false);
                                        });
                                    }
                                }
                            }

                            if (!reviveSpell && checkFail)
                                TrySpell(Cast, SpellFinished);
                            else
                                Cast();
                        }

                        if (consumeHandler != null)
                        {
                            // Don't waste items on dead players
                            if (fromItem && !reviveSpell && spell != Spell.AllHealing && target?.Alive != true)
                                return;
                            consumeHandler(Consume);
                        }
                        else
                            Consume();
                    }
                }
                this.TargetPlayerPicked += TargetPlayerPicked;
                break;
            }
            case SpellTarget.FriendRow:
                throw new AmbermoonException(ExceptionScope.Application, $"Friend row spells are not implemented as there are none in Ambermoon.");
            case SpellTarget.AllFriends:
            {
                void Consume()
                {
                    ConsumeSP();
                    void Cast()
                    {
                        if (spell == Spell.Resurrection)
                        {
                            var affectedMembers = PartyMembers.Where(p => p.Conditions.HasFlag(Condition.DeadCorpse)).ToList();
                            Revive(caster, affectedMembers, SpellFinished);
                        }
                        else
                        {
                            currentAnimation?.Destroy();
                            currentAnimation = new SpellAnimation(this, layout);

                            currentAnimation.CastHealingOnPartyMembers(() =>
                            {
                                currentAnimation.Destroy();
                                currentAnimation = null;

                                foreach (var partyMember in PartyMembers.Where(p => p.Alive))
                                    ApplySpellEffect(spell, caster, partyMember, null, false);

                                SpellFinished();
                            });
                        }
                    }
                    if (checkFail)
                        TrySpell(Cast, spell == Spell.CreateFood ? () => ShowMessagePopup(DataNameProvider.TheSpellFailed, SpellFinished) : SpellFinished);
                    else
                        Cast();
                }
                if (consumeHandler != null)
                    consumeHandler(Consume);
                else
                    Consume();
                break;
            }
            case SpellTarget.Item:
            {
                string message = spell == Spell.RemoveCurses ? DataNameProvider.BattleMessageWhichPartyMemberAsTarget
                    : DataNameProvider.WhichInventoryAsTarget;
                layout.OpenTextPopup(ProcessText(message), null, true, false, false, TextAlign.Center);
                if (CurrentWindow.Window == Window.Inventory)
                    InputEnable = true;
                else
                    Pause();
                PickTargetInventory();
                bool TargetInventoryPicked(int characterSlot)
                {
                    this.TargetInventoryPicked -= TargetInventoryPicked;

                    if (characterSlot == -1)
                        return true; // abort, TargetItemPicked is called and will cleanup

                    if (spell == Spell.RemoveCurses)
                    {
                        var target = GetPartyMember(characterSlot);
                        var firstCursedItem = target.Equipment.Slots.Values.FirstOrDefault(s => s.Flags.HasFlag(ItemSlotFlags.Cursed));

                        if (firstCursedItem == null)
                        {
                            void CleanUp()
                            {
                                itemGrid?.HideTooltip();
                                UntrapMouse();
                                EndSequence();
                                layout.ShowChestMessage(null);
                                layout.SetInventoryMessage(null);
                                ClosePopup();
                            }

                            this.TargetItemPicked -= TargetItemPicked;
                            Consume();
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.NoCursedItemFound, CleanUp);
                            return false; // no item selection
                        }
                    }

                    return true; // move forward to item selection
                }
                bool TargetItemPicked(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
                {
                    this.TargetItemPicked -= TargetItemPicked;
                    itemGrid?.HideTooltip();
                    layout.SetInventoryMessage(null);
                    ClosePopup();
                    if (itemSlot != null)
                    {
                        Consume();
                        StartSequence();
                        ApplySpellEffect(spell, caster, itemSlot, () =>
                        {
                            if (!fromItem)
                                CloseWindow();
                            UntrapMouse();
                            EndSequence();
                            if (!WindowActive)
                                Resume();
                            layout.SetInventoryMessage(null);
                            layout.ShowChestMessage(null);
                        }, checkFail);
                        return false; // manual window closing etc
                    }
                    else
                    {
                        layout.SetInventoryMessage(null);
                        layout.ShowChestMessage(null);
                        if (!WindowActive)
                            Resume();
                        if (fromItem)
                        {
                            EndSequence();
                            UntrapMouse();
                            return false;
                        }
                        else
                        {
                            return true; // auto-close window and cleanup
                        }
                    }
                }
                this.TargetInventoryPicked += TargetInventoryPicked;
                this.TargetItemPicked += TargetItemPicked;

                void Consume()
                {
                    if (consumeHandler != null)
                        consumeHandler(ConsumeSP);
                    else
                        ConsumeSP();
                }
                break;
            }
            case SpellTarget.None:
            {
                void Consume()
                {
                    ConsumeSP();

                    if (spell == Spell.SelfHealing || spell == Spell.SelfReviving)
                    {
                        void Cast()
                        {
                            currentAnimation?.Destroy();
                            currentAnimation = new SpellAnimation(this, layout);
                            currentAnimation.CastOn(spell, caster, () =>
                            {
                                currentAnimation.Destroy();
                                currentAnimation = null;
                                ApplySpellEffect(spell, caster, null, false);
                            });
                        }
                        if (checkFail)
                            TrySpell(Cast);
                        else
                            Cast();
                    }
                    else
                    {
                        ApplySpellEffect(spell, caster, null, checkFail);
                    }
                }
                if (consumeHandler != null)
                    consumeHandler(Consume);
                else
                    Consume();
                break;
            }
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"Spells with target {spellInfo.Target} should not be usable in camps.");
        }
    }

    /// <summary>
    /// Cast a spell on the map or in a camp.
    /// </summary>
    /// <param name="camp"></param>
    internal void CastSpell(bool camp, ItemGrid? itemGrid = null)
    {
        if (!CurrentPartyMember!.HasAnySpell())
        {
            ShowMessagePopup(DataNameProvider.YouDontKnowAnySpellsYet);
        }
        else
        {
            OpenSpellList(CurrentPartyMember,
                spell =>
                {
                    var spellInfo = SpellInfos[spell];

                    if (camp && !spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Camp))
                        return DataNameProvider.WrongArea;

                    if (!camp)
                    {
                        if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.AnyMap))
                        {
                            if (spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.WorldMapOnly))
                            {
                                if (!Map!.IsWorldMap)
                                    return DataNameProvider.WrongArea;
                            }
                            else if (spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.DungeonOnly))
                            {
                                if (Map!.Type != MapType.Map3D || Map.Flags.HasFlag(MapFlags.Outdoor))
                                    return DataNameProvider.WrongArea;
                            }
                            else
                            {
                                return DataNameProvider.WrongArea;
                            }
                        }
                    }

                    var worldFlag = (WorldFlag)(1 << (int)Map!.World);

                    if (!spellInfo.Worlds.HasFlag(worldFlag))
                        return DataNameProvider.WrongWorld;

                    if (SpellInfos.GetSPCost(Features, spell, CurrentPartyMember) > CurrentPartyMember.SpellPoints.CurrentValue)
                        return DataNameProvider.NotEnoughSP;

                    return null;
                },
                spell => UseSpell(CurrentPartyMember, spell, itemGrid, false)
            );
        }
    }
}
