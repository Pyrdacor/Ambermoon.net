using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Attribute = Ambermoon.Data.Attribute;

namespace Ambermoon;

partial class GameCore
{
    internal bool EnterPlace(Map map, EnterPlaceEvent enterPlaceEvent)
    {
        if (WindowActive)
            return false;

        ResetMoveKeys();

        int openingHour = enterPlaceEvent.OpeningHour;
        int closingHour = enterPlaceEvent.ClosingHour == 0 ? 24 : enterPlaceEvent.ClosingHour;

        if (GameTime.Hour >= openingHour && GameTime.Hour < closingHour)
        {
            switch (enterPlaceEvent.PlaceType)
            {
                case PlaceType.Trainer:
                {
                    var trainerData = new Places.Trainer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenTrainer(trainerData);
                    return true;
                }
                case PlaceType.Healer:
                {
                    var healerData = new Places.Healer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenHealer(healerData);
                    return true;
                }
                case PlaceType.Sage:
                {
                    var sageData = new Places.Sage(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenSage(sageData);
                    return true;
                }
                case PlaceType.Enchanter:
                {
                    var enchanterData = new Places.Enchanter(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenEnchanter(enchanterData);
                    return true;
                }
                case PlaceType.Inn:
                {
                    var innData = new Places.Inn(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenInn(innData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? DataNameProvider.InnkeeperGoodSleepWish :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.InnkeeperGoodSleepWish));
                    return true;
                }
                case PlaceType.Merchant:
                case PlaceType.Library:
                    OpenMerchant(enterPlaceEvent.MerchantDataIndex, places.Entries[(int)enterPlaceEvent.PlaceIndex - 1].Name,
                        enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                            map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing),
                        enterPlaceEvent.PlaceType == PlaceType.Library, true, null);
                    return true;
                case PlaceType.FoodDealer:
                {
                    var foodDealerData = new Places.FoodDealer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenFoodDealer(foodDealerData);
                    return true;
                }
                case PlaceType.HorseDealer:
                {
                    var horseDealerData = new Places.HorseSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenHorseSalesman(horseDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
                    return true;
                }
                case PlaceType.RaftDealer:
                {
                    var raftDealerData = new Places.RaftSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenRaftSalesman(raftDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
                    return true;
                }
                case PlaceType.ShipDealer:
                {
                    var shipDealerData = new Places.ShipSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenShipSalesman(shipDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
                    return true;
                }
                case PlaceType.Blacksmith:
                {
                    var blacksmithData = new Places.Blacksmith(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenBlacksmith(blacksmithData);
                    return true;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Data, "Unknown place type.");
            }
        }
        else if (enterPlaceEvent.ClosedTextIndex != 255)
        {
            string closedText = map.GetText((int)enterPlaceEvent.ClosedTextIndex, DataNameProvider.TextBlockMissing);
            ShowTextPopup(ProcessText(closedText), null);
            return true;
        }
        else
        {
            return true;
        }
    }

    void ShowPlaceWindow(string placeName, string? welcomeText, Picture80x80 picture, IPlace place, Action<Action, ItemGrid>? placeSetup,
        Action? activePlayerSwitchedHandler, Func<string>? exitChecker = null, Action? closeAction = null, int numItemSlots = 12)
    {
        OpenStorage = place;
        layout.SetLayout(LayoutType.Items);
        layout.AddText(new Rect(120, 37, 29 * Global.GlyphWidth, Global.GlyphLineHeight),
            renderView.TextProcessor.CreateText(placeName), TextColor.White);
        layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
        var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
        itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
        var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat((ItemSlot?)null, numItemSlots).ToList(),
            false, 12, 6, numItemSlots, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
        itemGrid.Disabled = true;
        layout.AddItemGrid(itemGrid);
        layout.Set80x80Picture(picture);

        // Put all gold on the table!
        foreach (var partyMember in PartyMembers)
        {
            place.AvailableGold += partyMember.Gold;
            partyMember.RemoveGold(partyMember.Gold);
        }

        ShowTextPanel(CharacterInfo.ChestGold, true,
            $"{DataNameProvider.GoldName}^{place.AvailableGold}", new Rect(111, 104, 43, 15));

        if (welcomeText != null)
        {
            layout.ShowClickChestMessage(welcomeText);
        }

        void UpdateGoldDisplay()
            => characterInfoTexts[CharacterInfo.ChestGold].SetText(renderView.TextProcessor.CreateText($"{DataNameProvider.GoldName}^{place.AvailableGold}"));

        placeSetup?.Invoke(UpdateGoldDisplay, itemGrid);
        ActivePlayerChanged += activePlayerSwitchedHandler;
        closeWindowHandler = _ => ActivePlayerChanged -= activePlayerSwitchedHandler;

        // exit button
        layout.AttachEventToButton(2, () =>
        {
            var exitQuestion = exitChecker?.Invoke();

            if (exitQuestion != null)
            {
                layout.OpenYesNoPopup(ProcessText(exitQuestion), Exit, () => ClosePopup(), () => ClosePopup(), 2);
            }
            else
            {
                Exit();
            }

            void Exit()
            {
                CloseWindow();

                // Distribute the gold
                var partyMembers = PartyMembers.ToList();
                uint availableGold = place.AvailableGold;
                availableGold = DistributeGold(availableGold, false);
                int goldPerPartyMember = (int)availableGold / partyMembers.Count;
                int restGold = (int)availableGold % partyMembers.Count;

                if (availableGold != 0)
                {
                    for (int i = 0; i < partyMembers.Count; ++i)
                    {
                        int gold = goldPerPartyMember + (i < restGold ? 1 : 0);
                        partyMembers[i].AddGold((uint)gold);
                    }
                }

                closeAction?.Invoke();
            }
        });
    }

    void OpenEnchanter(Places.Enchanter enchanter, bool showWelcome = true)
    {
        currentPlace = enchanter;

        if (showWelcome)
            enchanter.AvailableGold = 0;

        Action? updatePartyGold = null;
        ItemGrid? itemsGrid = null;

        void SetupEnchanter(Action updateGold, ItemGrid itemGrid)
        {
            updatePartyGold = updateGold;
            itemsGrid = itemGrid;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Enchanter, enchanter);
            ShowPlaceWindow(enchanter.Name, showWelcome ? DataNameProvider.WelcomeEnchanter : null,
                Picture80x80.Enchantress, enchanter, SetupEnchanter, null, null, null, 24);
            void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.WhichItemToEnchant, TextAlign.Left);
            var itemArea = new Rect(16, 139, 151, 53);
            // Enchant item button
            layout.AttachEventToButton(3, () =>
            {
                itemsGrid!.Disabled = false;
                itemsGrid.DisableDrag = true;
                ShowDefaultMessage();
                CursorType = CursorType.Sword;
                TrapMouse(itemArea);
                layout.ButtonsDisabled = true;
                itemsGrid.Initialize(CurrentPartyMember!.Inventory.Slots.ToList(), false);
                itemsGrid.ItemClicked += ItemClicked;
                SetupRightClickAbort();
            });
            void SetupRightClickAbort()
            {
                nextClickHandler = buttons =>
                {
                    if (buttons == MouseButtons.Right)
                    {
                        DisableItemGrid();
                        layout.ShowChestMessage(null);
                        UntrapMouse();
                        layout.ButtonsDisabled = false;
                        CursorType = CursorType.Sword;
                        inputEnable = true;
                        return true;
                    }

                    return false;
                };
            }
            void DisableItemGrid()
            {
                itemsGrid.HideTooltip();
                itemsGrid.ItemClicked -= ItemClicked;
                itemsGrid.Disabled = true;
                layout.ButtonsDisabled = false;
            }
            void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
            {
                itemsGrid.HideTooltip();

                void Error(string message, bool abort)
                {
                    layout.ShowClickChestMessage(message, () =>
                    {
                        if (!abort)
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                            ShowDefaultMessage();
                        }
                        else
                        {
                            DisableItemGrid();
                        }
                    });
                }

                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                int costPerCharge = item.RechargePrice;

                if (costPerCharge == 0)
                    costPerCharge = enchanter.Cost;

                if (enchanter.AvailableGold < costPerCharge)
                {
                    Error(DataNameProvider.NotEnoughMoney, true);
                    return;
                }

                if (item.Spell == Spell.None || (item.InitialCharges == 0 && item.MaxCharges == 0))
                {
                    Error(DataNameProvider.CannotEnchantOrdinaryItem, false);
                    return;
                }

                if (item.MaxCharges == 0)
                {
                    Error(DataNameProvider.CannotRechargeAnymore, false);
                    return;
                }

                int numMissingCharges = itemSlot.NumRemainingCharges >= item.MaxCharges ? 0 : item.MaxCharges - itemSlot.NumRemainingCharges;

                if (numMissingCharges == 0)
                {
                    Error(DataNameProvider.AlreadyFullyCharged, false);
                    return;
                }

                if (item.MaxRecharges != 0 && item.MaxRecharges != 255 && itemSlot.RechargeTimes >= item.MaxRecharges)
                {
                    Error(DataNameProvider.CannotRechargeAnymore, false);
                    return;
                }

                void Enchant(uint charges)
                {
                    ClosePopup();
                    uint totalCost = charges * (uint)costPerCharge;

                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.PriceForEnchanting}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        nextClickHandler = null;
                        EndSequence();
                        UntrapMouse();

                        if (answer) // yes
                        {
                            void Enchant()
                            {
                                layout.ShowChestMessage(null);
                                enchanter.AvailableGold -= totalCost;
                                updatePartyGold?.Invoke();
                                itemSlot.NumRemainingCharges += (int)charges;
                                itemSlot.RechargeTimes = (byte)Math.Min(255, itemSlot.RechargeTimes + 1);
                                DisableItemGrid();
                            }

                            if (item.MaxRecharges != 0 && item.MaxRecharges != 255 && itemSlot.RechargeTimes == item.MaxRecharges - 1)
                                layout.ShowClickChestMessage(DataNameProvider.LastTimeEnchanting, Enchant);
                            else
                                Enchant();
                        }
                        else
                        {
                            layout.ShowChestMessage(null);
                            DisableItemGrid();
                        }
                    }, TextAlign.Left);
                }

                nextClickHandler = null;
                UntrapMouse();

                layout.OpenAmountInputBox(DataNameProvider.HowManyCharges,
                    item.GraphicIndex, item.Name, (uint)Util.Min(enchanter.AvailableGold / costPerCharge, numMissingCharges), Enchant,
                    () =>
                    {
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    }
                );
            }
            ;
        });
    }

    void OpenSage(Places.Sage sage, bool showWelcome = true)
    {
        currentPlace = sage;

        if (showWelcome)
            sage.AvailableGold = 0;

        Action updatePartyGold = null;
        ItemGrid itemsGrid = null;

        void SetupSage(Action updateGold, ItemGrid itemGrid)
        {
            updatePartyGold = updateGold;
            itemsGrid = itemGrid;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Sage, sage);
            ShowPlaceWindow(sage.Name, showWelcome ? DataNameProvider.WelcomeSage : null,
                Picture80x80.Sage, sage, SetupSage, null, null, null, 24);
            void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.ExamineWhichItemSage, TextAlign.Left);
            void ShowItems(bool equipment, bool scrollIdentification = false)
            {
                itemsGrid.Disabled = false;
                itemsGrid.DisableDrag = true;
                ShowDefaultMessage();
                CursorType = CursorType.Sword;
                var itemArea = new Rect(16, 139, 151, 53);
                TrapMouse(itemArea);
                layout.ButtonsDisabled = true;
                itemsGrid.Initialize(equipment ? CurrentPartyMember.Equipment.Slots.Where(s => s.Value.ItemIndex != 0).Select(s => s.Value).ToList()
                : CurrentPartyMember.Inventory.Slots.ToList(), false);
                void SetupRightClickAbort()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            DisableItemGrid();
                            layout.ShowChestMessage(null);
                            UntrapMouse();
                            layout.ButtonsDisabled = false;
                            CursorType = CursorType.Sword;
                            inputEnable = true;
                            return true;
                        }

                        return false;
                    };
                }
                SetupRightClickAbort();
                void DisableItemGrid()
                {
                    itemsGrid.HideTooltip();
                    itemsGrid.ItemClicked -= ItemClicked;
                    itemsGrid.Disabled = true;
                    layout.ButtonsDisabled = false;
                }
                itemsGrid.ItemClicked += ItemClicked;
                void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    itemsGrid.HideTooltip();

                    void Message(string message, bool abort)
                    {
                        layout.ShowClickChestMessage(message, () =>
                        {
                            if (!abort)
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                                ShowDefaultMessage();
                            }
                            else
                            {
                                DisableItemGrid();
                            }
                        });
                    }

                    if (scrollIdentification)
                    {
                        if (ItemManager.GetItem(itemSlot.ItemIndex).Type != ItemType.SpellScroll)
                        {
                            Message(DataNameProvider.ThatsNotASpellScroll, false);
                            return;
                        }
                    }
                    else if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
                    {
                        Message(DataNameProvider.ItemAlreadyIdentified, false);
                        return;
                    }

                    int cost = scrollIdentification ? sage.TellingSLPCost : sage.IdentificationCost;

                    if (sage.AvailableGold < cost)
                    {
                        Message(DataNameProvider.NotEnoughMoney, true);
                        return;
                    }

                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.PriceForExamining}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        nextClickHandler = null;

                        void Finish()
                        {
                            EndSequence();
                            UntrapMouse();
                            DisableItemGrid();
                            layout.ShowChestMessage(null);

                            if (answer) // yes
                            {
                                sage.AvailableGold -= (uint)cost;
                                updatePartyGold?.Invoke();

                                if (scrollIdentification)
                                {
                                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                                    var slp = SpellInfos.GetSLPCost(Features, item.Spell);
                                    Message(DataNameProvider.SageIdentifyScroll + slp.ToString() + DataNameProvider.SageSLP, true);
                                }
                                else
                                {
                                    itemSlot.Flags |= ItemSlotFlags.Identified;
                                    ShowItemPopup(itemSlot, null);
                                }
                            }
                        }

                        if (answer)
                        {
                            ItemAnimation.Play(this, renderView, ItemAnimation.Type.Enchant, layout.GetItemSlotPosition(itemSlot, true),
                                Finish, TimeSpan.FromMilliseconds(50));
                        }
                        else
                        {
                            Finish();
                        }

                    }, TextAlign.Left);
                }
                ;
            }
            // Examine equipment button
            layout.AttachEventToButton(0, () => ShowItems(true));
            // Examine inventory item button
            layout.AttachEventToButton(3, () => ShowItems(false));
            if (Features.HasFlag(Features.SageScrollIdentification))
                layout.AttachEventToButton(6, () => ShowItems(false, true));
        });
    }

    void OpenHealer(Places.Healer healer, bool showWelcome = true)
    {
        currentPlace = healer;

        if (showWelcome)
            healer.AvailableGold = 0;

        Action updatePartyGold = null;
        ItemGrid conditionGrid = null;

        void SetupHealer(Action updateGold, ItemGrid itemGrid)
        {
            updatePartyGold = updateGold;
            conditionGrid = itemGrid;
        }

        void Heal(uint lp)
        {
            nextClickHandler = null;
            layout.ShowPlaceQuestion($"{DataNameProvider.PriceForHealing}{lp * healer.HealLPCost}{DataNameProvider.AgreeOnPrice}", answer =>
            {
                if (answer) // yes
                {
                    healer.AvailableGold -= lp * (uint)healer.HealLPCost;
                    updatePartyGold?.Invoke();
                    currentlyHealedMember.HitPoints.CurrentValue += lp;
                    PlayerSwitched();
                    PlayHealAnimation(currentlyHealedMember, () => layout.FillCharacterBars(currentlyHealedMember));
                }
            }, TextAlign.Left);
        }

        void HealCondition(Condition condition, Action<bool> healedHandler)
        {
            // TODO: At the moment DeadAshes and DeadDust will be healed fully so that the
            // character is alive afterwards. As this is bugged in original I don't know how
            // it was supposed to be. Either reviving completely or transform to next stage
            // like dust to ashes and ashes to body first.

            var cost = (uint)healer.GetCostForHealingCondition(condition);
            nextClickHandler = null;
            layout.ShowPlaceQuestion($"{DataNameProvider.PriceForHealingCondition}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
            {
                if (answer) // yes
                {
                    healer.AvailableGold -= cost;
                    updatePartyGold?.Invoke();
                    RemoveCondition(condition, currentlyHealedMember);
                    PlayerSwitched();
                    PlayHealAnimation(currentlyHealedMember);
                    layout.UpdateCharacterStatus(currentlyHealedMember);
                    healedHandler?.Invoke(true);
                    if (condition >= Condition.DeadCorpse) // dead
                    {
                        currentlyHealedMember.HitPoints.CurrentValue = Math.Max(1, currentlyHealedMember.HitPoints.CurrentValue);
                        PartyMemberRevived(currentlyHealedMember);
                    }
                }
                else
                {
                    healedHandler?.Invoke(false);
                }
            }, TextAlign.Left);
        }

        var healableConditions = Condition.Lamed | Condition.Poisoned | Condition.Petrified | Condition.Diseased |
            Condition.Aging | Condition.DeadCorpse | Condition.DeadAshes | Condition.DeadDust | Condition.Crazy |
            Condition.Blind | Condition.Drugged;

        void PlayerSwitched()
        {
            layout.EnableButton(0, currentlyHealedMember.HitPoints.CurrentValue < currentlyHealedMember.HitPoints.TotalMaxValue);
            layout.EnableButton(3, currentlyHealedMember.Equipment.Slots.Any(slot => slot.Value.Flags.HasFlag(ItemSlotFlags.Cursed)));
            layout.EnableButton(6, ((uint)currentlyHealedMember.Conditions & (uint)healableConditions) != 0);
        }

        uint GetMaxLPHealing() => Math.Max(0, Util.Min(healer.AvailableGold / (uint)healer.HealLPCost,
            currentlyHealedMember.HitPoints.TotalMaxValue - currentlyHealedMember.HitPoints.CurrentValue));

        Fade(() =>
        {
            if (showWelcome)
                currentlyHealedMember = CurrentPartyMember;

            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Healer, healer);
            ShowPlaceWindow(healer.Name, showWelcome ? DataNameProvider.WelcomeHealer : null,
                Picture80x80.Healer, healer, SetupHealer, PlayerSwitched);
            // This will show the healing symbol on top of the portrait.
            SetActivePartyMember(SlotFromPartyMember(currentlyHealedMember).Value);
            // Heal LP button
            layout.AttachEventToButton(0, () =>
            {
                conditionGrid.Disabled = true;

                if (healer.AvailableGold < healer.HealLPCost)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                    return;
                }

                layout.OpenAmountInputBox(DataNameProvider.HowManyLP, null, null, GetMaxLPHealing(), lp =>
                {
                    ClosePopup();
                    Heal(lp);
                }, () => ClosePopup());
            });
            // Remove curse button
            layout.AttachEventToButton(3, () =>
            {
                conditionGrid.Disabled = true;

                if (healer.AvailableGold < healer.RemoveCurseCost)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                    return;
                }

                int maxCursesToRemove = Math.Min((int)healer.AvailableGold / healer.RemoveCurseCost,
                    currentlyHealedMember.Equipment.Slots.Count(slot => slot.Value.Flags.HasFlag(ItemSlotFlags.Cursed)));
                nextClickHandler = null;
                layout.ShowPlaceQuestion($"{DataNameProvider.PriceForRemovingCurses}{maxCursesToRemove * healer.RemoveCurseCost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        healer.AvailableGold -= (uint)(maxCursesToRemove * healer.RemoveCurseCost);
                        updatePartyGold?.Invoke();
                        PlayerSwitched();
                        allInputDisabled = true;
                        OpenPartyMember(SlotFromPartyMember(currentlyHealedMember).Value, true, () =>
                        {
                            var equipSlots = currentlyHealedMember.Equipment.Slots.ToList();

                            for (int i = 0; i < maxCursesToRemove; ++i)
                            {
                                var cursedItemSlot = equipSlots.First(s => s.Value.Flags.HasFlag(ItemSlotFlags.Cursed));
                                layout.DestroyItem(cursedItemSlot.Value, TimeSpan.FromMilliseconds(800));
                            }

                            AddTimedEvent(TimeSpan.FromSeconds(2), () =>
                            {
                                CloseWindow();
                                allInputDisabled = false;
                            });
                        }, false);
                    }
                }, TextAlign.Left);
            });
            layout.AttachEventToButton(6, () =>
            {
                conditionGrid.Disabled = false;
                conditionGrid.DisableDrag = true;
                layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                CursorType = CursorType.Sword;
                var itemArea = new Rect(16, 139, 151, 53);
                TrapMouse(itemArea);
                var slots = new List<ItemSlot>(12);
                var slotConditions = new List<Condition>(12);
                // Ensure that only one dead state is present
                if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadDust))
                    currentlyHealedMember.Conditions = Condition.DeadDust;
                else if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadAshes))
                    currentlyHealedMember.Conditions = Condition.DeadAshes;
                else if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadCorpse))
                    currentlyHealedMember.Conditions = Condition.DeadCorpse;
                for (int i = 0; i < 16; ++i)
                {
                    if (((uint)healableConditions & (1u << i)) != 0)
                    {
                        var condition = (Condition)(1 << i);

                        if (currentlyHealedMember.Conditions.HasFlag(condition))
                        {
                            slots.Add(new ItemSlot
                            {
                                ItemIndex = condition switch
                                {
                                    Condition.Lamed => 1,
                                    Condition.Poisoned => 2,
                                    Condition.Petrified => 3,
                                    Condition.Diseased => 4,
                                    Condition.Aging => 5,
                                    Condition.Crazy => 7,
                                    Condition.Blind => 8,
                                    Condition.Drugged => 9,
                                    _ => 6 // dead states
                                },
                                Amount = 1
                            });
                            slotConditions.Add(condition);
                        }
                    }
                }
                while (slots.Count < 12)
                    slots.Add(new ItemSlot());
                conditionGrid.Initialize(slots, false);
                void SetupRightClickAbort()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            DisableConditionGrid();
                            layout.ShowChestMessage(null);
                            UntrapMouse();
                            CursorType = CursorType.Sword;
                            inputEnable = true;
                            return true;
                        }

                        return false;
                    };
                }
                SetupRightClickAbort();
                void DisableConditionGrid()
                {
                    conditionGrid.HideTooltip();
                    conditionGrid.ItemClicked -= ConditionClicked;
                    conditionGrid.Disabled = true;
                }
                conditionGrid.ItemClicked += ConditionClicked;
                void ConditionClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    if (slotIndex < slotConditions.Count)
                    {
                        conditionGrid.HideTooltip();

                        if (healer.AvailableGold < healer.GetCostForHealingCondition(slotConditions[slotIndex]))
                        {
                            layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney, () =>
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                            });
                            return;
                        }

                        nextClickHandler = null;
                        UntrapMouse();

                        HealCondition(slotConditions[slotIndex], healed =>
                        {
                            if (healed)
                            {
                                if (currentlyHealedMember.Conditions != Condition.None)
                                {
                                    conditionGrid.SetItem(slotIndex, null);
                                    TrapMouse(itemArea);
                                    SetupRightClickAbort();
                                    layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                                }
                                else
                                {
                                    DisableConditionGrid();
                                }
                            }
                            else
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                                layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                            }
                        });
                    }
                }
                ;
            });
            PlayerSwitched();
        });
    }

    void OpenBlacksmith(Places.Blacksmith blacksmith, bool showWelcome = true)
    {
        currentPlace = blacksmith;

        if (showWelcome)
            blacksmith.AvailableGold = 0;

        // Note: The blacksmith uses the same 80x80 image as the sage.
        Action updatePartyGold = null;
        ItemGrid itemsGrid = null;

        void SetupBlacksmith(Action updateGold, ItemGrid itemGrid)
        {
            updatePartyGold = updateGold;
            itemsGrid = itemGrid;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Blacksmith, blacksmith);
            ShowPlaceWindow(blacksmith.Name, showWelcome ? DataNameProvider.WelcomeBlacksmith : null,
                Map.World switch
                {
                    World.Lyramion => Picture80x80.Sage,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Sage
                }, blacksmith, SetupBlacksmith, null, null, null, 24);
            void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.WhichItemToRepair, TextAlign.Left);
            // Repair item button
            layout.AttachEventToButton(3, () =>
            {
                itemsGrid.Disabled = false;
                itemsGrid.DisableDrag = true;
                ShowDefaultMessage();
                CursorType = CursorType.Sword;
                var itemArea = new Rect(16, 139, 151, 53);
                TrapMouse(itemArea);
                layout.ButtonsDisabled = true;
                itemsGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                void SetupRightClickAbort()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            DisableItemGrid();
                            layout.ShowChestMessage(null);
                            UntrapMouse();
                            layout.ButtonsDisabled = false;
                            CursorType = CursorType.Sword;
                            inputEnable = true;
                            return true;
                        }

                        return false;
                    };
                }
                SetupRightClickAbort();
                void DisableItemGrid()
                {
                    itemsGrid.HideTooltip();
                    itemsGrid.ItemClicked -= ItemClicked;
                    itemsGrid.Disabled = true;
                    layout.ButtonsDisabled = false;
                }
                itemsGrid.ItemClicked += ItemClicked;
                void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    itemsGrid.HideTooltip();

                    void Error(string message, bool abort)
                    {
                        layout.ShowClickChestMessage(message, () =>
                        {
                            if (!abort)
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                                ShowDefaultMessage();
                            }
                            else
                            {
                                DisableItemGrid();
                            }
                        });
                    }

                    if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                    {
                        Error(DataNameProvider.CannotRepairUnbreakableItem, false);
                        return;
                    }

                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    uint cost = (uint)blacksmith.Cost * item.Price / 100u;

                    if (blacksmith.AvailableGold < cost)
                    {
                        Error(DataNameProvider.NotEnoughMoney, true);
                        return;
                    }

                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.PriceForRepair}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        nextClickHandler = null;
                        EndSequence();
                        UntrapMouse();
                        layout.ShowChestMessage(null);

                        if (answer) // yes
                        {
                            blacksmith.AvailableGold -= cost;
                            updatePartyGold?.Invoke();
                            itemSlot.Flags &= ~ItemSlotFlags.Broken;
                        }

                        DisableItemGrid();
                    }, TextAlign.Left);
                }
                ;
            });
        });
    }

    void OpenInn(Places.Inn inn, string useText, bool showWelcome = true)
    {
        currentPlace = inn;

        if (showWelcome)
            inn.AvailableGold = 0;

        Action updatePartyGold = null;

        void SetupInn(Action updateGold, ItemGrid _)
        {
            updatePartyGold = updateGold;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Inn, inn, useText);
            ShowPlaceWindow(inn.Name, showWelcome ? DataNameProvider.WelcomeInnkeeper : null,
                Map.World switch
                {
                    World.Lyramion => Picture80x80.Innkeeper,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Innkeeper
                },
                inn, SetupInn, null, null, () => InputEnable = true);
            // Rest button
            layout.AttachEventToButton(3, () =>
            {
                // Animals etc don't need to pay
                int totalCost = Math.Max(1, PartyMembers.Where(p => p.Alive && p.Race < Race.Animal).Count()) * inn.Cost;
                if (inn.AvailableGold < totalCost)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                    return;
                }
                nextClickHandler = null;
                layout.ShowPlaceQuestion($"{DataNameProvider.StayWillCost}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        inn.AvailableGold -= (uint)totalCost;
                        updatePartyGold?.Invoke();
                        layout.ShowClickChestMessage(useText, () =>
                        {
                            currentWindow.Window = Window.MapView; // This way closing the camp will return to map and not the Inn
                            layout.GetButtonAction(2)?.Invoke(); // Call close handler
                            OpenStorage = null;
                            Teleport((uint)inn.BedroomMapIndex, (uint)inn.BedroomX,
                                (uint)inn.BedroomY, player.Direction, out _, true);
                            OpenCamp(true, inn.Healing);
                        });
                    }
                }, TextAlign.Left);
            });
        });
    }

    void OpenHorseSalesman(Places.HorseSalesman horseSalesman, string? buyText, bool showWelcome = true)
    {
        if (showWelcome)
            horseSalesman.AvailableGold = 0;

        OpenTransportSalesman(horseSalesman, buyText, TravelType.Horse, Window.HorseSalesman,
            Picture80x80.Horse, showWelcome ? DataNameProvider.WelcomeHorseSeller : null);
    }

    void OpenRaftSalesman(Places.RaftSalesman raftSalesman, string? buyText, bool showWelcome = true)
    {
        if (showWelcome)
            raftSalesman.AvailableGold = 0;

        OpenTransportSalesman(raftSalesman, buyText, TravelType.Raft, Window.RaftSalesman,
            Picture80x80.Captain, showWelcome ? DataNameProvider.WelcomeRaftSeller : null);
    }

    void OpenShipSalesman(Places.ShipSalesman shipSalesman, string? buyText, bool showWelcome = true)
    {
        if (showWelcome)
            shipSalesman.AvailableGold = 0;

        OpenTransportSalesman(shipSalesman, buyText, TravelType.Ship, Window.ShipSalesman,
            Picture80x80.Captain, showWelcome ? DataNameProvider.WelcomeShipSeller : null);
    }

    void OpenTransportSalesman(Places.Salesman salesman, string? buyText, TravelType travelType,
        Window window, Picture80x80 picture80X80, string? welcomeMessage)
    {
        currentPlace = salesman;
        Action? updatePartyGold = null;

        void SetupSalesman(Action updateGold, ItemGrid _)
        {
            updatePartyGold = updateGold;
        }

        bool EnableBuying()
        {
            // Buying is enabled if on the target location isn't already
            // the given transport. Invalid data always disallows buying.

            if (salesman.SpawnMapIndex <= 0 || salesman.SpawnX <= 0 || salesman.SpawnY <= 0)
                return false;

            var map = MapManager.GetMap((uint)salesman.SpawnMapIndex);

            if (map == null || map.Type == MapType.Map3D || !map.UseTravelTypes || // Should not happen but never allow buying in these cases
                salesman.SpawnX > map.Width || salesman.SpawnY > map.Height)
                return false;

            var tile = map.Tiles[salesman.SpawnX - 1, salesman.SpawnY - 1];
            var tileset = MapManager.GetTilesetForMap(map);

            if (!tile.AllowMovement(tileset, travelType)) // Can't be placed there
                return false;

            if (CurrentSavegame.TransportLocations.Any(t => t != null && t.MapIndex == map.Index &&
                t.Position.X == salesman.SpawnX && t.Position.Y == salesman.SpawnY))
                return false;

            // TODO: Maybe change later
            // Allow 12 ships, 10 rafts and 10 horses
            int allowedCount = travelType == TravelType.Ship ? 12 : 10;
            return CurrentSavegame.TransportLocations.Count(t => t?.TravelType == travelType) < allowedCount;
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(window, salesman, buyText);
            ShowPlaceWindow(salesman.Name, welcomeMessage, picture80X80,
                salesman, SetupSalesman, null, null, () => InputEnable = true);
            if (!EnableBuying())
            {
                layout.EnableButton(3, false);
            }
            else
            {
                // Buy transport button
                layout.AttachEventToButton(3, () =>
                {
                    // Animals don't have to pay for a transport
                    int totalCost = (salesman.PlaceType == PlaceType.HorseDealer ? Math.Max(1, PartyMembers.Where(p => p.Alive && p.Race < Race.Animal).Count()) : 1) * salesman.Cost;
                    if (salesman.AvailableGold < totalCost)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                        return;
                    }
                    string costText = salesman.PlaceType switch
                    {
                        PlaceType.HorseDealer => DataNameProvider.PriceForHorse,
                        PlaceType.RaftDealer => DataNameProvider.PriceForRaft,
                        PlaceType.ShipDealer => DataNameProvider.PriceForShip,
                        _ => throw new AmbermoonException(ExceptionScope.Application, $"Invalid salesman place type: {salesman.PlaceType}")
                    };
                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{costText}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            salesman.AvailableGold -= (uint)totalCost;
                            updatePartyGold?.Invoke();
                            void Buy()
                            {
                                SpawnTransport((uint)salesman.SpawnMapIndex, (uint)salesman.SpawnX, (uint)salesman.SpawnY, travelType);
                                layout.EnableButton(3, false);
                            }
                            if (string.IsNullOrWhiteSpace(buyText))
                            {
                                Buy();
                            }
                            else
                            {
                                layout.ShowClickChestMessage(buyText, Buy);
                            }
                        }
                    }, TextAlign.Left);
                });
            }
        });
    }

    internal void SpawnTransport(uint mapIndex, uint x, uint y, TravelType travelType)
    {
        if (x == 0)
            x = 1u + (uint)player!.Position.X;
        if (y == 0)
            y = 1u + (uint)player!.Position.Y;

        int spawnIndex = -1;

        for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
        {
            if (CurrentSavegame.TransportLocations[i] == null)
            {
                CurrentSavegame.TransportLocations[i] = new TransportLocation
                {
                    TravelType = travelType,
                    MapIndex = mapIndex,
                    Position = new Position((int)x, (int)y)
                };
                spawnIndex = i;
                break;
            }
            else if (CurrentSavegame.TransportLocations[i].TravelType == TravelType.Walk)
            {
                CurrentSavegame.TransportLocations[i].TravelType = travelType;
                CurrentSavegame.TransportLocations[i].MapIndex = mapIndex;
                CurrentSavegame.TransportLocations[i].Position = new Position((int)x, (int)y);
                spawnIndex = i;
                break;
            }
        }

        if (mapIndex == 0)
            mapIndex = Map!.Index;

        if (mapIndex == Map!.Index && spawnIndex != -1)
        {
            // TODO: In theory the transport could be visible even if the map index
            // does not match as there might be adjacent maps visible. But for now
            // there is no use case in Ambermoon nor Ambermoon Advanced as transports
            // are usually spawned on different maps.
            renderMap2D!.PlaceTransport(mapIndex, x - 1, y - 1, travelType, spawnIndex);
        }
    }

    void OpenFoodDealer(Places.FoodDealer foodDealer, bool showWelcome = true)
    {
        currentPlace = foodDealer;

        if (showWelcome)
            foodDealer.AvailableGold = 0;

        Action updatePartyGold = null;

        void SetupFoodDealer(Action updateGold, ItemGrid _)
        {
            updatePartyGold = updateGold;
        }

        void UpdateButtons()
        {
            layout.EnableButton(3, foodDealer.AvailableGold >= foodDealer.Cost);
            layout.EnableButton(4, foodDealer.AvailableFood > 0);
            layout.EnableButton(5, foodDealer.AvailableFood > 0);
        }

        void ShowDefaultMessage()
        {
            layout.ShowChestMessage(string.Format(DataNameProvider.OneFoodCosts, foodDealer.Cost), TextAlign.Center);
        }

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.FoodDealer, foodDealer);
            ShowPlaceWindow(foodDealer.Name, showWelcome ? DataNameProvider.WelcomeFoodDealer : null,
                Map.World switch
                {
                    World.Lyramion => Picture80x80.Merchant,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Merchant
                }, foodDealer, SetupFoodDealer, null,
                () => foodDealer.AvailableFood == 0 ? null : DataNameProvider.WantToLeaveRestOfFood,
                () => InputEnable = true);
            // Buy food button
            layout.AttachEventToButton(3, () =>
            {
                layout.OpenAmountInputBox(DataNameProvider.BuyHowMuchFood, 109, DataNameProvider.FoodName,
                    Math.Min(99, foodDealer.AvailableGold / (uint)foodDealer.Cost), amount =>
                    {
                        ClosePopup();
                        nextClickHandler = null;
                        layout.ShowPlaceQuestion($"{DataNameProvider.PriceOfFood}{amount * foodDealer.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            if (answer) // yes
                            {
                                foodDealer.AvailableGold -= amount * (uint)foodDealer.Cost;
                                foodDealer.AvailableFood += amount;
                                updatePartyGold?.Invoke();
                                UpdateFoodDisplay();
                                UpdateButtons();
                            }
                            ShowDefaultMessage();
                        }, TextAlign.Left);
                    }, () => { ClosePopup(); ShowDefaultMessage(); });
            });
            // Distribute food button
            layout.AttachEventToButton(4, () =>
            {
                foodDealer.AvailableFood = DistributeFood(foodDealer.AvailableFood, false);
                UpdateFoodDisplay();
                UpdateButtons();

                layout.ShowClickChestMessage(foodDealer.AvailableFood == 0
                    ? DataNameProvider.FoodDividedEqually : DataNameProvider.FoodLeftAfterDividing,
                    ShowDefaultMessage);
            });
            // Give food button
            layout.AttachEventToButton(5, () =>
            {
                layout.GiveFood(foodDealer.AvailableFood, food =>
                {
                    foodDealer.AvailableFood -= food;
                    UpdateFoodDisplay();
                    UpdateButtons();
                    UntrapMouse();
                    ExecuteNextUpdateCycle(ShowDefaultMessage);
                }, () => layout.ShowChestMessage(DataNameProvider.GiveToWhom), ShowDefaultMessage,
                () => layout.ShowClickChestMessage(DataNameProvider.NoOneCanCarryThatMuch));
            });
            void UpdateFoodDisplay()
            {
                if (foodDealer.AvailableFood > 0)
                {
                    ShowTextPanel(CharacterInfo.ChestFood, true,
                        $"{DataNameProvider.FoodName}^{foodDealer.AvailableFood}", new Rect(260, 104, 43, 15));
                }
                else
                {
                    HideTextPanel(CharacterInfo.ChestFood);
                }
            }
            UpdateButtons();
            if (!showWelcome)
                ShowDefaultMessage();
            else
            {
                void ClickedWelcomeMessage(bool _)
                {
                    if (layout.ChestText != null)
                        layout.ChestText.Clicked -= ClickedWelcomeMessage;
                    ExecuteNextUpdateCycle(ShowDefaultMessage);
                }
                layout.ChestText.Clicked += ClickedWelcomeMessage;
            }
        });
    }

    void OpenTrainer(Places.Trainer trainer, bool showWelcome = true)
    {
        currentPlace = trainer;

        if (showWelcome)
            trainer.AvailableGold = 0;

        Action updatePartyGold = null;

        void SetupTrainer(Action updateGold, ItemGrid _)
        {
            updatePartyGold = updateGold;
        }

        void Train(uint times)
        {
            nextClickHandler = null;
            layout.ShowPlaceQuestion($"{DataNameProvider.PriceForTraining}{times * trainer.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
            {
                if (answer) // yes
                {
                    trainer.AvailableGold -= times * (uint)trainer.Cost;
                    updatePartyGold?.Invoke();
                    CurrentPartyMember.Skills[trainer.Skill].CurrentValue += times;
                    CurrentPartyMember.TrainingPoints -= (ushort)times;
                    PlayerSwitched();
                    layout.ShowClickChestMessage(DataNameProvider.IncreasedAfterTraining);
                }
            }, TextAlign.Left);
        }

        void PlayerSwitched()
        {
            layout.EnableButton(3, CurrentPartyMember.Skills[trainer.Skill].CurrentValue < CurrentPartyMember.Skills[trainer.Skill].MaxValue);
        }

        uint GetMaxTrains() => Math.Max(0, Util.Min(trainer.AvailableGold / (uint)trainer.Cost, CurrentPartyMember.TrainingPoints,
            CurrentPartyMember.Skills[trainer.Skill].MaxValue - CurrentPartyMember.Skills[trainer.Skill].CurrentValue));

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Trainer, trainer);
            ShowPlaceWindow(trainer.Name, showWelcome ?
                trainer.Skill switch
                {
                    Skill.Attack => DataNameProvider.WelcomeAttackTrainer,
                    Skill.Parry => DataNameProvider.WelcomeParryTrainer,
                    Skill.Swim => DataNameProvider.WelcomeSwimTrainer,
                    Skill.CriticalHit => DataNameProvider.WelcomeCriticalHitTrainer,
                    Skill.FindTraps => DataNameProvider.WelcomeFindTrapTrainer,
                    Skill.DisarmTraps => DataNameProvider.WelcomeDisarmTrapTrainer,
                    Skill.LockPicking => DataNameProvider.WelcomeLockPickingTrainer,
                    Skill.Searching => DataNameProvider.WelcomeSearchTrainer,
                    Skill.ReadMagic => DataNameProvider.WelcomeReadMagicTrainer,
                    Skill.UseMagic => DataNameProvider.WelcomeUseMagicTrainer,
                    _ => throw new AmbermoonException(ExceptionScope.Data, "Invalid skill for trainer")
                } : null,
                trainer.Skill switch
                {
                    Skill.Attack => Picture80x80.Knight,
                    Skill.Parry => Picture80x80.Knight,
                    Skill.Swim => Picture80x80.Knight,
                    Skill.CriticalHit => Picture80x80.Knight,
                    Skill.FindTraps => Picture80x80.Thief,
                    Skill.DisarmTraps => Picture80x80.Thief,
                    Skill.LockPicking => Picture80x80.Thief,
                    Skill.Searching => Picture80x80.Thief,
                    Skill.ReadMagic => Picture80x80.Magician,
                    Skill.UseMagic => Picture80x80.Magician,
                    _ => Picture80x80.Knight
                }, trainer, SetupTrainer, PlayerSwitched
            );
            // train button
            layout.AttachEventToButton(3, () =>
            {
                if (trainer.AvailableGold < trainer.Cost)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                    return;
                }

                if (CurrentPartyMember.TrainingPoints == 0)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughTrainingPoints);
                    return;
                }

                layout.OpenAmountInputBox(DataNameProvider.TrainHowOften, null, null, GetMaxTrains(), times =>
                {
                    ClosePopup();
                    Train(times);
                }, () => ClosePopup());
            });
            PlayerSwitched();
        });
    }

    void OpenMerchant(uint merchantIndex, string placeName, string buyText, bool isLibrary,
        bool showWelcome, ItemSlot[] boughtItems)
    {
        var merchant = GetMerchant(1 + merchantIndex);
        currentPlace = merchant;
        merchant.Name = placeName;
        if (showWelcome)
            merchant.AvailableGold = 0;

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Merchant, merchantIndex, placeName, buyText, isLibrary, boughtItems);
            ShowMerchantWindow(merchant, placeName, showWelcome ? isLibrary ? DataNameProvider.WelcomeMagician :
                DataNameProvider.WelcomeMerchant : null, buyText,
                isLibrary ? Picture80x80.Librarian : Map.World switch
                {
                    World.Lyramion => Picture80x80.Merchant,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Merchant
                },
            !isLibrary, boughtItems);
        });
    }

    void ShowMerchantWindow(Merchant merchant, string placeName, string initialText,
        string buyText, Picture80x80 picture, bool buysGoods, ItemSlot[] boughtItems)
    {
        // TODO: use buyText?

        OpenStorage = merchant;
        layout.SetLayout(LayoutType.Items);
        layout.AddText(new Rect(120, 37, 29 * Global.GlyphWidth, Global.GlyphLineHeight),
            renderView.TextProcessor.CreateText(placeName), TextColor.White);
        layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
        var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
        itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
        var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, merchant.Slots.ToList(),
            false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical, false,
            () => merchant.AvailableGold);
        itemGrid.Disabled = false;
        layout.AddItemGrid(itemGrid);
        layout.Set80x80Picture(picture);
        var itemArea = new Rect(16, 139, 151, 53);
        int mode = -1; // -1: show bought items, 0: buy, 3: sell, 4: examine (= button index)
        if (boughtItems == null)
        {
            // Note: Don't use boughtItems ??= Enumerable.Repeat(new ItemSlot(), 24).ToArray();
            // as this would use the exact same ItemSlot instance for all slots!
            boughtItems = new ItemSlot[24];
            for (int i = 0; i < boughtItems.Length; ++i)
                boughtItems[i] = new ItemSlot();
        }
        boughtItems ??= Enumerable.Repeat(new ItemSlot(), 24).ToArray();
        currentWindow.WindowParameters[4] = boughtItems;

        void UpdateSellButton()
        {
            layout.EnableButton(3, buysGoods && CurrentPartyMember.Inventory.Slots.Any(s => !s.Empty));
        }

        void SetupRightClickAbort()
        {
            nextClickHandler = buttons =>
            {
                if (buttons == MouseButtons.Right)
                {
                    itemGrid.HideTooltip();
                    layout.ShowChestMessage(null);
                    UntrapMouse();
                    CursorType = CursorType.Sword;
                    inputEnable = true;
                    ShowBoughtItems();
                    return true;
                }

                return false;
            };
        }

        void AssignButton(int index, bool merchantItems, string messageText, TextAlign textAlign, Func<bool> checker)
        {
            layout.AttachEventToButton(index, () =>
            {
                if (checker?.Invoke() == false)
                    return;

                mode = index;
                itemGrid.DisableDrag = true;
                layout.ShowChestMessage(messageText, textAlign);
                CursorType = CursorType.Sword;
                TrapMouse(itemArea);
                FillItems(merchantItems);
                itemGrid.ShowPrice = mode == 0; // buy
                SetupRightClickAbort();
            });
        }

        // Buy button
        AssignButton(0, true, DataNameProvider.BuyWhichItem, TextAlign.Center, null);
        // Sell button
        if (buysGoods)
        {
            AssignButton(3, false, DataNameProvider.SellWhichItem, TextAlign.Left, () =>
            {
                if (!merchant.HasEmptySlots())
                {
                    layout.ShowClickChestMessage(DataNameProvider.MerchantFull);
                    return false;
                }
                return true;
            });
        }
        else
        {
            layout.EnableButton(3, false);
        }
        // Examine button
        AssignButton(4, true, DataNameProvider.ExamineWhichItemMerchant, TextAlign.Left, null);
        // Exit button
        layout.AttachEventToButton(2, () =>
        {
            void Exit()
            {
                CloseWindow();

                // Distribute the gold
                var partyMembers = PartyMembers.ToList();
                uint availableGold = merchant.AvailableGold;
                availableGold = DistributeGold(availableGold, false);
                int goldPerPartyMember = (int)availableGold / partyMembers.Count;
                int restGold = (int)availableGold % partyMembers.Count;

                if (availableGold != 0)
                {
                    for (int i = 0; i < partyMembers.Count; ++i)
                    {
                        int gold = goldPerPartyMember + (i < restGold ? 1 : 0);
                        partyMembers[i].AddGold((uint)gold);
                    }
                }
            }

            if (boughtItems.Any(item => item != null && !item.Empty))
            {
                void ExitAndReturnItems()
                {
                    var merchantSlots = merchant.Slots.ToList();

                    foreach (var items in boughtItems)
                        ReturnItems(items);

                    void ReturnItems(ItemSlot items)
                    {
                        var slot = merchantSlots.FirstOrDefault(s => s.ItemIndex == items.ItemIndex) ??
                            merchantSlots.FirstOrDefault(s => s.ItemIndex == 0 || s.Amount == 0);

                        if (slot != null)
                        {
                            if (slot.ItemIndex == 0)
                                slot.Amount = 0;

                            slot.ItemIndex = items.ItemIndex;
                            slot.Amount += items.Amount;
                        }
                    }

                    Exit();
                }

                layout.OpenYesNoPopup(ProcessText(DataNameProvider.WantToGoWithoutItemsMerchant), ExitAndReturnItems, () => ClosePopup(), () => ClosePopup(), 2);
            }
            else
            {
                Exit();
            }
        });

        void UpdateButtons()
        {
            // Note: Disabling the buy button if no slot is free in bought items grid might be bad in rare
            // cases because you still might buy some stackable items like arrows. But this is very rare cause
            // you would have to buy some of this items before.
            layout.EnableButton(0, boughtItems.Any(slot => slot == null || slot.Empty) && merchant.AvailableGold > 0);
            bool anyItemsToSell = merchant.Slots.ToList().Any(s => !s.Empty);
            layout.EnableButton(4, anyItemsToSell);
            UpdateSellButton();
        }

        void FillItems(bool fromMerchant)
        {
            itemGrid.Initialize(fromMerchant ? merchant.Slots.ToList() : CurrentPartyMember.Inventory.Slots.ToList(), fromMerchant);
        }

        void ShowBoughtItems()
        {
            mode = -1;
            itemGrid.DisableDrag = false;
            itemGrid.ShowPrice = false;
            itemGrid.Initialize(boughtItems.ToList(), false);
        }

        uint CalculatePrice(uint price)
        {
            var charisma = CurrentPartyMember.Attributes[Attribute.Charisma].TotalCurrentValue;
            var basePrice = price / 3;
            var bonus = (uint)Util.Floor(Util.Floor(charisma / 10) * (price / 100.0f));
            return basePrice + bonus;
        }
        itemDragCancelledHandler += ShowBoughtItems;
        itemGrid.DisableDrag = false;
        itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
        {
            // This can only happen for bought items but we check for safety here
            if (mode != -1)
                throw new AmbermoonException(ExceptionScope.Application, "Non-bought items should not be draggable.");

            if (updateSlot)
                boughtItems[slotIndex].Remove(amount);
            layout.EnableButton(0, boughtItems.Any(slot => slot == null || slot.Empty) && merchant.AvailableGold > 0);
        };
        itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
        {
            if (mode == -1)
            {
                foreach (var partyMember in PartyMembers)
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value);
                itemGrid.Refresh();
            }
        };
        itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
        {
            var item = ItemManager.GetItem(itemSlot.ItemIndex);

            if (mode == -1) // show bought items
            {
                // No interaction
                return;
            }
            else if (mode == 0) // buy
            {
                itemGrid.HideTooltip();

                if (merchant.AvailableGold < item.Price)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoneyToBuy, () =>
                    {
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    });
                    return;
                }

                nextClickHandler = null;
                UntrapMouse();

                uint GetMaxItemsToBuy(uint itemIndex)
                {
                    var item = ItemManager.GetItem(itemIndex);

                    if (item.Flags.HasFlag(ItemFlags.Stackable))
                    {
                        if (boughtItems.Any(slot => slot == null || slot.Empty))
                            return 99;

                        var slotWithItem = boughtItems.FirstOrDefault(slot => slot.ItemIndex == itemIndex && slot.Amount < 99);

                        return slotWithItem == null ? 0 : 99u - (uint)slotWithItem.Amount;
                    }
                    else
                    {
                        return (uint)boughtItems.Count(slot => slot == null || slot.Empty);
                    }
                }

                void Buy(uint amount)
                {
                    ClosePopup();
                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.ThisWillCost}{amount * item.Price}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            int column = slotIndex % Merchant.SlotsPerRow;
                            int row = slotIndex / Merchant.SlotsPerRow;
                            int numCharges = itemSlot.NumRemainingCharges;
                            byte rechargeTimes = itemSlot.RechargeTimes;
                            var flags = itemSlot.Flags;
                            merchant.TakeItems(column, row, amount);
                            itemGrid.SetItem(slotIndex, merchant.Slots[column, row], true);
                            merchant.AvailableGold -= amount * item.Price;
                            UpdateGoldDisplay();
                            if (item.Flags.HasFlag(ItemFlags.Stackable))
                            {
                                for (int i = 0; i < boughtItems.Length; ++i)
                                {
                                    if (boughtItems[i] != null && boughtItems[i].ItemIndex == item.Index &&
                                        boughtItems[i].Amount < 99)
                                    {
                                        int space = 99 - boughtItems[i].Amount;
                                        int add = Math.Min(space, (int)amount);
                                        boughtItems[i].Amount += add;
                                        amount -= (uint)add;
                                        if (amount == 0)
                                            break;
                                    }
                                }
                                if (amount != 0)
                                {
                                    for (int i = 0; i < boughtItems.Length; ++i)
                                    {
                                        if (boughtItems[i] == null || boughtItems[i].Empty)
                                        {
                                            boughtItems[i] = new ItemSlot
                                            {
                                                ItemIndex = item.Index,
                                                Amount = (int)amount,
                                                NumRemainingCharges = numCharges,
                                                RechargeTimes = rechargeTimes,
                                                Flags = flags
                                            };
                                            amount = 0;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < boughtItems.Length; ++i)
                                {
                                    if (boughtItems[i] == null || boughtItems[i].Empty)
                                    {
                                        boughtItems[i] = new ItemSlot
                                        {
                                            ItemIndex = item.Index,
                                            Amount = 1,
                                            NumRemainingCharges = numCharges,
                                            RechargeTimes = rechargeTimes,
                                            Flags = flags
                                        };
                                        if (--amount == 0)
                                            break;
                                    }
                                }
                            }
                            UpdateButtons();
                        }

                        ShowBoughtItems();
                    }, TextAlign.Left);
                }

                if (itemSlot.Amount > 1)
                {
                    layout.OpenAmountInputBox(DataNameProvider.BuyHowMuchItems,
                        item.GraphicIndex, item.Name, Util.Min((uint)itemSlot.Amount, merchant.AvailableGold / item.Price, GetMaxItemsToBuy(item.Index)), Buy,
                        () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        }
                    );
                }
                else
                {
                    Buy(1);
                }
            }
            else if (mode == 3) // sell
            {
                itemGrid.HideTooltip();

                if (!item.Flags.HasFlag(ItemFlags.NotImportant) || item.Price < 9) // TODO: Don't know if this is right
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotInterestedInItemMerchant, () =>
                    {
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    });
                    return;
                }

                if (itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                {
                    layout.ShowClickChestMessage(DataNameProvider.WontBuyBrokenStuff, () =>
                    {
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    });
                    return;
                }

                nextClickHandler = null;
                UntrapMouse();

                uint GetMaxItemsToSell(uint itemIndex)
                {
                    var item = ItemManager.GetItem(itemIndex);

                    var slots = merchant.Slots.ToList();

                    if (slots.Any(slot => slot == null || slot.Empty))
                        return 99;

                    var slotWithItem = slots.FirstOrDefault(slot => slot.ItemIndex == itemIndex && slot.Amount < 99);

                    return slotWithItem == null ? 0 : 99u - (uint)slotWithItem.Amount;
                }

                void Sell(uint amount)
                {
                    ClosePopup();
                    var sellPrice = amount * CalculatePrice(item.Price);
                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.ForThisIllGiveYou}{sellPrice}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            allInputDisabled = true;
                            merchant.AddItems(ItemManager, item.Index, amount, itemSlot);
                            CurrentPartyMember!.Inventory.Slots[slotIndex].Remove((int)amount);
                            InventoryItemRemoved(item.Index, (int)amount, CurrentPartyMember);
                            itemGrid.SetItem(slotIndex, CurrentPartyMember.Inventory.Slots[slotIndex], true);
                            merchant.AvailableGold += sellPrice;
                            UpdateGoldDisplay();
                            UpdateButtons();
                            allInputDisabled = false;
                        }

                        if (!merchant.Slots.ToList().Any(s => s.Empty))
                            ShowBoughtItems();
                        else
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        }
                    }, TextAlign.Left);
                }

                if (itemSlot.Amount > 1)
                {
                    layout.OpenAmountInputBox(DataNameProvider.SellHowMuchItems,
                        item.GraphicIndex, item.Name, Util.Min((uint)itemSlot.Amount, GetMaxItemsToSell(item.Index)), Sell,
                        () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        }
                    );
                }
                else
                {
                    Sell(1);
                }
            }
            else if (mode == 4) // examine
            {
                itemGrid.HideTooltip();
                nextClickHandler = null;
                UntrapMouse();
                ShowItemPopup(itemSlot, () =>
                {
                    TrapMouse(itemArea);
                    SetupRightClickAbort();
                });
            }
            else
            {
                throw new AmbermoonException(ExceptionScope.Application, "Invalid merchant mode.");
            }
        };

        // Put all gold on the table!
        foreach (var partyMember in PartyMembers)
        {
            merchant.AvailableGold += partyMember.Gold;
            partyMember.RemoveGold(partyMember.Gold);
        }

        ShowTextPanel(CharacterInfo.ChestGold, true,
            $"{DataNameProvider.GoldName}^{merchant.AvailableGold}", new Rect(111, 104, 43, 15));

        UpdateButtons();
        ShowBoughtItems();

        if (initialText != null)
        {
            layout.ShowClickChestMessage(initialText);
        }

        ActivePlayerChanged += UpdateSellButton;
        layout.DraggedItemDropped += UpdateSellButton;

        void CleanUp()
        {
            itemDragCancelledHandler = null;
            ActivePlayerChanged -= UpdateSellButton;
            layout.DraggedItemDropped -= UpdateSellButton;
        }

        closeWindowHandler = _ => CleanUp();

        void UpdateGoldDisplay()
            => characterInfoTexts[CharacterInfo.ChestGold].SetText(renderView.TextProcessor.CreateText($"{DataNameProvider.GoldName}^{merchant.AvailableGold}"));
    }
}
