using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using static Ambermoon.UI.BuiltinTooltips;
using Attribute = Ambermoon.Data.Attribute;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    static readonly WindowInfo DefaultWindow = new() { Window = Window.MapView };

    private protected readonly Layout layout;
    Action<bool>? closeWindowHandler = null;
    WindowInfo currentWindow = DefaultWindow;
    readonly IRenderText? windowTitle = null;

    internal WindowInfo LastWindow { get; private set; } = DefaultWindow;
    internal WindowInfo CurrentWindow => currentWindow;
    public bool WindowActive => currentWindow.Window != Window.MapView;
    public bool PopupActive => layout?.PopupActive ?? false;
    public bool WindowOrPopupActive => WindowActive || PopupActive;
    internal Layout Layout => layout;
    public bool TransportEnabled => layout.TransportEnabled;
    public bool CampEnabled => Map?.CanCamp == true && TravelType.CanCampOn() == true;
    public bool SpellBookEnabled => CanUseSpells();
    internal bool GameOverButtonsVisible { get; private set; } = false;
    public bool CampActive => WindowActive && CurrentWindow.Window == Window.Camp;

    void UpdateMapName(Map? map = null)
    {
        map ??= Map;
        string mapName = map!.IsWorldMap
            ? DataNameProvider.GetWorldName(map.World)
            : map.Name;
        windowTitle!.Text = ProcessText(mapName);
        windowTitle.PaletteIndex = UIPaletteIndex;
        windowTitle.TextColor = TextColor.BrightGray;
    }

    public void UpdateInventory()
    {
        if (CurrentWindow.Window == Window.Inventory)
        {
            layout.UpdateItemGrids();
            UpdateCharacterInfo();
        }
    }

    void SetInventoryWeightDisplay(PartyMember partyMember)
    {
        var weightArea = new Rect(27, 152, 68, 15);
        string weightText = string.Format(DataNameProvider.CharacterInfoWeightString,
            partyMember.TotalWeight / 1000, partyMember.MaxWeight / 1000);
        if (partyMember.Overweight)
        {
            weightDisplayBlinking = true;
            if (characterInfoTexts.ContainsKey(CharacterInfo.Weight))
                characterInfoTexts[CharacterInfo.Weight]?.Destroy();
            characterInfoTexts[CharacterInfo.Weight] = AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align, 5),
                weightArea.CreateModified(0, 8, 0, 0), weightText, TextAlign.Center, () => weightDisplayBlinking &&
                    CurrentWindow.Window == Window.Inventory, 50, false);
        }
        else
        {
            weightDisplayBlinking = false;
            ExecuteNextUpdateCycle(() =>
            {
                if (characterInfoTexts.TryGetValue(CharacterInfo.Weight, out UIText? value))
                    value?.Destroy();
                characterInfoTexts[CharacterInfo.Weight] = layout.AddText(weightArea.CreateModified(0, 8, 0, 0),
                    weightText, TextColor.White, TextAlign.Center, 5);
            });
        }
    }

    internal bool OpenPartyMember(int slot, bool inventory, Action? openedAction = null,
        bool changeInputEnableStateWhileFading = true)
    {
        currentBattle?.HideAllBattleFieldDamage();

        if (CurrentSavegame!.CurrentPartyMemberIndices[slot] == 0)
            return false;

        var partyMember = GetPartyMember(slot)!;

        if (partyMember.InventoryInaccessible)
        {
            // Note: In original you can't access the player stats as well so
            // we do it the same way here even though the message is misleading.
            // This feature is now used in AA for mystic imitation spell.
            var oldActivePartyMember = CurrentPartyMember;
            CurrentPartyMember = partyMember; // Needed to display the right name here
            ShowMessagePopup(DataNameProvider.NotAllowingToLookIntoBackpack);
            CurrentPartyMember = oldActivePartyMember;
            return false;
        }

        bool switchedFromOtherPartyMember = CurrentInventory != null;
        bool canAccessInventory = !HasPartyMemberFled(partyMember) && partyMember.Conditions.CanOpenInventory();

        if (canAccessInventory && partyMember.Race == Race.Animal && layout.IsDragging)
        {
            var draggedItem = layout.GetDraggedItem();

            if (draggedItem == null) // gold or food
                canAccessInventory = false;
            else // for animals only allow if the item is usable by animals (fallback item index 1 is never usable as it is a condition item)
                canAccessInventory = (ItemManager?.GetItem(draggedItem?.Item?.Item?.ItemIndex ?? 1)?.Classes)?.HasFlag(ClassFlag.Animal) ?? false;
        }

        if (inventory && !canAccessInventory)
        {
            // When fled you can only access the stats.
            // When coming from inventory of another party member
            // you won't be able to open the inventory but if
            // you open the character with F1-F6 or right click
            // you will enter the stats window instead.
            if (switchedFromOtherPartyMember)
                return false;
            else
                inventory = false;
        }

        void OpenInventory()
        {
            if (currentWindow.Window == Window.Automap)
            {
                currentWindow.Window = Window.Inventory;
                nextClickHandler?.Invoke(MouseButtons.Right);
                nextClickHandler = null;
                currentWindow.Window = Window.Automap;
            }

            CurrentInventoryIndex = slot;
            var partyMember = GetPartyMember(slot);

            layout.Reset(switchedFromOtherPartyMember);
            ShowMap(false);
            SetWindow(Window.Inventory, slot);
            layout.SetLayout(LayoutType.Inventory);

            // As the inventory can be opened from the healer (which displays the healing symbol)
            // we will update the portraits here to hide it.
            SetActivePartyMember(SlotFromPartyMember(CurrentPartyMember!)!.Value, false);

            windowTitle!.Text = renderView.TextProcessor.CreateText(DataNameProvider.InventoryTitleString);
            windowTitle.PaletteIndex = UIPaletteIndex;
            windowTitle.TextColor = TextColor.White;
            windowTitle.Visible = true;

            #region Equipment and Inventory
            var equipmentSlotPositions = new List<Position>
            {
                new(20, 72),  new(52, 72),  new(84, 72),
                new(20, 124), new(84, 97),  new(84, 124),
                new(20, 176), new(52, 176), new(84, 176),
            };
            var inventorySlotPositions = Enumerable.Range(0, Inventory.VisibleWidth * Inventory.VisibleHeight).Select
            (
                slot => new Position(Global.InventoryX + (slot % Inventory.Width) * Global.InventorySlotWidth,
                    Global.InventoryY + (slot / Inventory.Width) * Global.InventorySlotHeight)
            ).ToList();
            ItemGrid? equipmentGrid = null;
            equipmentGrid = ItemGrid.CreateEquipment(this, layout, slot, renderView, ItemManager!,
                equipmentSlotPositions, partyMember!.Equipment.Slots.Values.ToList(), itemSlot =>
                {
                    if (itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed))
                    {
                        layout.SetInventoryMessage(DataNameProvider.ItemIsCursed, true);
                        return false;
                    }

                    if (currentBattle != null)
                    {
                        var item = ItemManager!.GetItem(itemSlot.ItemIndex);

                        if (!item.Flags.HasFlag(ItemFlags.RemovableDuringFight))
                        {
                            layout.SetInventoryMessage(DataNameProvider.CannotUnequipInFight, true);
                            return false;
                        }
                    }
                    return true;
                }, UnequipItem, layout.UseItem);
            var inventoryGrid = ItemGrid.CreateInventory(this, layout, slot, renderView, ItemManager!,
                inventorySlotPositions, [.. partyMember.Inventory.Slots], EquipItem, layout.UseItem);
            layout.AddItemGrid(inventoryGrid);
            for (int i = 0; i < partyMember.Inventory.Slots.Length; ++i)
            {
                if (!partyMember.Inventory.Slots[i].Empty)
                {
                    if (partyMember.Inventory.Slots[i].ItemIndex == 0) // Item index 0 but amount is not 0 -> not allowed for inventory
                        partyMember.Inventory.Slots[i].Amount = 0;

                    inventoryGrid.SetItem(i, partyMember.Inventory.Slots[i]);
                }
            }
            var rightHandSlot = partyMember.Equipment.Slots[EquipmentSlot.RightHand];
            if (rightHandSlot != null && rightHandSlot.ItemIndex != 0)
            {
                var rightHandItem = ItemManager!.GetItem(rightHandSlot.ItemIndex);

                if (rightHandItem.NumberOfHands == 2)
                {
                    var leftHandSlot = partyMember.Equipment.Slots[EquipmentSlot.LeftHand];

                    if (leftHandSlot == null)
                        leftHandSlot = new ItemSlot { Amount = 1, ItemIndex = 0 };
                    else if (leftHandSlot.Empty)
                        leftHandSlot.Amount = 1;
                }
            }
            void UpdateOccupiedHandsAndFingers()
            {
                CurrentInventory!.NumberOfOccupiedHands = 0;
                CurrentInventory.NumberOfOccupiedFingers = 0;

                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.RightHand].Empty)
                    CurrentInventory.NumberOfOccupiedHands++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.LeftHand].Empty)
                    CurrentInventory.NumberOfOccupiedHands++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.RightFinger].Empty)
                    CurrentInventory.NumberOfOccupiedFingers++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.LeftFinger].Empty)
                    CurrentInventory.NumberOfOccupiedFingers++;
            }
            void EquipItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                if (itemSlot.Empty)
                    return;

                if (itemSlot.ItemIndex == 0)
                {
                    if (slot != (int)EquipmentSlot.LeftHand - 1 || itemGrid.GetItemSlot(3)!.Empty)
                        return;

                    slot -= 2; // used on two-handed secondary hand slot -> switch to primary hand slot
                }

                var targetSlot = layout.TryEquipmentDrop(itemSlot);

                if (targetSlot != null)
                {
                    var equipGrid = layout.GetEquipmentGrid();
                    var targetItemSlot = equipGrid.GetItemSlot(targetSlot.Value);


                    if (itemSlot.Amount > 1)
                    {
                        // Allow equipping arrows (but only if the slot is free)
                        if (targetSlot != (int)EquipmentSlot.LeftHand - 1 || !CurrentInventory!.Equipment.Slots[EquipmentSlot.LeftHand].Empty)
                            return;

                        itemSlot.Remove(1);
                        targetItemSlot!.ItemIndex = itemSlot.ItemIndex;
                        targetItemSlot.Amount = 1;
                        CurrentInventory.NumberOfOccupiedHands++;
                    }
                    else
                    {
                        targetItemSlot!.Exchange(itemSlot);
                    }
                    RemoveInventoryItem(slot, targetItemSlot, targetItemSlot.Amount);
                    equipGrid.SetItem(targetSlot.Value, targetItemSlot);
                    itemGrid.SetItem(slot, itemSlot);
                    AddEquipment(targetSlot.Value, targetItemSlot, targetItemSlot.Amount);

                    if (itemSlot.Amount != 0 && itemSlot.ItemIndex != 0)
                    {
                        RemoveEquipment(targetSlot.Value, itemSlot, 1);
                        AddInventoryItem(slot, itemSlot, 1);
                        RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(targetSlot.Value + 1), ItemManager!.GetItem(itemSlot.ItemIndex));
                    }

                    UpdateOccupiedHandsAndFingers();
                }
            }
            void UnequipItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                var inventoryGrid = layout.GetInventoryGrid();
                int targetSlot = -1;

                if (ItemManager!.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
                {
                    for (int i = 0; i < inventoryGrid.SlotCount; ++i)
                    {
                        var inventorySlot = inventoryGrid.GetItemSlot(i);

                        if (inventorySlot!.ItemIndex == itemSlot.ItemIndex && inventorySlot.Amount + itemSlot.Amount <= 99)
                        {
                            targetSlot = i;
                            break;
                        }
                    }
                }

                if (targetSlot == -1)
                {
                    for (int i = 0; i < inventoryGrid.SlotCount; ++i)
                    {
                        var inventorySlot = inventoryGrid.GetItemSlot(i);

                        if (inventorySlot!.Empty)
                        {
                            targetSlot = i;
                            break;
                        }
                    }
                }

                if (targetSlot == -1)
                    return;

                RemoveEquipment(slot, itemSlot, itemSlot.Amount, true);
                AddInventoryItem(targetSlot, itemSlot, itemSlot.Amount);

                var targetItemSlot = inventoryGrid.GetItemSlot(targetSlot);
                targetItemSlot!.Add(itemSlot);
                itemSlot.Clear();

                inventoryGrid.SetItem(targetSlot, targetItemSlot);
                itemGrid.SetItem(slot, itemSlot);

                RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slot + 1), ItemManager.GetItem(targetItemSlot.ItemIndex));
                UpdateOccupiedHandsAndFingers();
            }
            layout.AddItemGrid(equipmentGrid);
            foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>().Skip(1))
            {
                if (!partyMember.Equipment.Slots[equipmentSlot].Empty)
                {
                    if (equipmentSlot != EquipmentSlot.LeftHand &&
                        partyMember.Equipment.Slots[equipmentSlot].ItemIndex == 0) // Item index 0 but amount is not 0 -> only allowed for left hand
                        partyMember.Equipment.Slots[equipmentSlot].Amount = 0;

                    equipmentGrid.SetItem((int)equipmentSlot - 1, partyMember.Equipment.Slots[equipmentSlot]);
                }
            }
            void RemoveEquipment(int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot = true)
            {
                RecheckUsedBattleItem(CurrentInventoryIndex.Value, slotIndex, true);
                var item = ItemManager!.GetItem(itemSlot.ItemIndex);
                EquipmentRemoved(item, amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                if (updateSlot)
                {
                    if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                    {
                        equipmentGrid!.SetItem(slotIndex + 2, null);
                        partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Clear();
                    }
                }

                UpdateCharacterInfo();
                layout.FillCharacterBars(partyMember);
            }
            void AddEquipment(int slotIndex, ItemSlot itemSlot, int amount)
            {
                var item = ItemManager!.GetItem(itemSlot.ItemIndex);

                if (item.Flags.HasFlag(ItemFlags.Accursed))
                    itemSlot.Flags |= ItemSlotFlags.Cursed;

                EquipmentAdded(item, amount);

                if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                {
                    var secondHandItemSlot = new ItemSlot { ItemIndex = 0, Amount = 1 };
                    equipmentGrid.SetItem((int)EquipmentSlot.LeftHand - 1, secondHandItemSlot);
                    partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Replace(secondHandItemSlot);
                }

                UpdateCharacterInfo();
                layout.FillCharacterBars(partyMember);
            }
            void RemoveInventoryItem(int slotIndex, ItemSlot itemSlot, int amount)
            {
                RecheckUsedBattleItem(CurrentInventoryIndex.Value, slotIndex, false);
                InventoryItemRemoved(ItemManager!.GetItem(itemSlot.ItemIndex), amount);
                UpdateCharacterInfo();
            }
            void AddInventoryItem(int slotIndex, ItemSlot itemSlot, int amount)
            {
                InventoryItemAdded(ItemManager!.GetItem(itemSlot.ItemIndex), amount);
                UpdateCharacterInfo();
            }
            equipmentGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
            {
                RemoveEquipment(slotIndex, draggedItem, draggedAmount);
                AddEquipment(slotIndex, droppedItem, droppedItem.Amount);
                RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager!.GetItem(draggedItem.ItemIndex));
            };
            equipmentGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
            {
                RemoveEquipment(slotIndex, itemSlot, amount, updateSlot);
                if (updateSlot)
                {
                    partyMember.Equipment.Slots[(EquipmentSlot)(slotIndex + 1)].Remove(amount);
                    if (CurrentWindow.Window == Window.Inventory)
                    {
                        layout.UpdateLayoutButtons();
                        UpdateCharacterInfo();
                    }
                }
                // TODO: When resetting the item back to the slot (even just dropping it there) the previous battle action should be restored.
                RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager!.GetItem(itemSlot.ItemIndex));
            };
            equipmentGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                AddEquipment(slotIndex, itemSlot, amount);
                RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), null);
            };
            inventoryGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
            {
                RemoveInventoryItem(slotIndex, draggedItem, draggedAmount);
                AddInventoryItem(slotIndex, droppedItem, droppedItem.Amount);
            };
            inventoryGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
            {
                RemoveInventoryItem(slotIndex, itemSlot, amount);
                if (updateSlot)
                {
                    partyMember.Inventory.Slots[slotIndex].Remove(amount);
                    if (CurrentWindow.Window == Window.Inventory)
                        layout.UpdateLayoutButtons();
                }
            };
            inventoryGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                AddInventoryItem(slotIndex, itemSlot, amount);
            };
            #endregion
            #region Character info
            DisplayCharacterInfo(partyMember, false);
            // Weight display
            var weightArea = new Rect(27, 152, 68, 15);
            layout.AddPanel(weightArea, 2);
            layout.AddText(weightArea.CreateModified(0, 1, 0, 0), DataNameProvider.CharacterInfoWeightHeaderString,
                TextColor.White, TextAlign.Center, 5);
            SetInventoryWeightDisplay(partyMember);
            #endregion
        }

        void OpenCharacterStats()
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Stats, slot);
            layout.SetLayout(LayoutType.Stats);
            layout.EnableButton(0, canAccessInventory);
            layout.FillArea(new Rect(16, 49, 176, 145), GetUIColor(28), false);

            // As the stats can be opened from the healer (which displays the healing symbol)
            // we will update the portraits here to hide it.
            SetActivePartyMember(SlotFromPartyMember(CurrentPartyMember!)!.Value, false);

            HideWindowTitle();

            CurrentInventoryIndex = slot;
            var partyMember = GetPartyMember(slot);
            int index;

            void AddTooltip(Rect area, string tooltip)
            {
                layout.AddTooltip(area, tooltip, TextColor.White, TextAlign.Left, new Render.Color(GetPrimaryUIColor(15), 0xb0));
            }

            bool extendedLanguages = Features.HasFlag(Features.ExtendedLanguages);

            #region Character info
            DisplayCharacterInfo(partyMember!, false);
            #endregion
            #region Attributes
            layout.AddText(new Rect(22, 50, 72, Global.GlyphLineHeight), DataNameProvider.AttributesHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var attribute in EnumHelper.GetValues<Attribute>())
            {
                if (attribute == Attribute.Age)
                    break;

                int y = 57 + index++ * Global.GlyphLineHeight;
                var attributeValues = partyMember!.Attributes[attribute];
                if (attribute == Attribute.AntiMagic && CurrentSavegame.IsSpellActive(ActiveSpellType.AntiMagic))
                {
                    uint bonus = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.AntiMagic);
                    void AddAnimatedText(Rect area, string text)
                    {
                        this.AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align), area, text, TextAlign.Left,
                            () => CurrentWindow.Window == Window.Stats, 100, true);
                    }
                    AddAnimatedText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeShortName(attribute));
                    AddAnimatedText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (attributeValues.TotalCurrentValue + bonus > 999 ? "***" : $"{attributeValues.TotalCurrentValue + bonus:000}") + $"/{attributeValues.MaxValue:000}");
                }
                else
                {
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeShortName(attribute));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}");
                }
                if (CoreConfiguration.ShowPlayerStatsTooltips)
                    AddTooltip(new Rect(22, y, 72, Global.GlyphLineHeight), GetAttributeTooltip(Features, GameLanguage, attribute, partyMember));
            }
            #endregion
            #region Skills
            layout.AddText(new Rect(22, 115, 72, Global.GlyphLineHeight), DataNameProvider.SkillsHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                int y = 122 + index++ * Global.GlyphLineHeight;
                var skillValues = partyMember!.Skills[skill];
                var current = skillValues.TotalCurrentValue;

                if (skill == Skill.Searching && Features.HasFlag(Features.ClairvoyanceGrantsSearchSkill) &&
                    CurrentSavegame.IsSpellActive(ActiveSpellType.Clairvoyance))
                {
                    uint bonus = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Clairvoyance);
                    void AddAnimatedText(Rect area, string text)
                    {
                        this.AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align), area, text, TextAlign.Left,
                            () => CurrentWindow.Window == Window.Stats, 100, true);
                    }
                    AddAnimatedText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetSkillShortName(skill));
                    AddAnimatedText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (skillValues.TotalCurrentValue + bonus > 99 ? "**" : $"{skillValues.TotalCurrentValue + bonus:00}") + $"%/{skillValues.MaxValue:00}%");
                }
                else
                {
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetSkillShortName(skill));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (skillValues.TotalCurrentValue > 99 ? "**" : $"{skillValues.TotalCurrentValue:00}") + $"%/{skillValues.MaxValue:00}%");
                }

                if (CoreConfiguration.ShowPlayerStatsTooltips)
                    AddTooltip(new Rect(22, y, 72, Global.GlyphLineHeight), GetSkillTooltip(GameLanguage, skill, partyMember));
            }
            #endregion
            #region Languages
            int languageY = extendedLanguages ? 115 : 50;
            layout.AddText(new Rect(106, languageY, 72, Global.GlyphLineHeight), DataNameProvider.LanguagesHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var language in EnumHelper.GetValues<Language>().Skip(1)) // skip Language.None
            {
                int y = languageY + 7 + index++ * Global.GlyphLineHeight;
                bool learned = partyMember!.SpokenLanguages.HasFlag(language);
                if (learned)
                    layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), DataNameProvider.GetLanguageName(language));
            }
            if (extendedLanguages)
            {
                foreach (var extendedLanguage in EnumHelper.GetValues<ExtendedLanguage>().Skip(1)) // skip ExtendedLanguage.None
                {
                    int y = languageY + 7 + index++ * Global.GlyphLineHeight;
                    bool learned = partyMember!.SpokenExtendedLanguages.HasFlag(extendedLanguage);
                    if (learned)
                    {
                        string name = DataNameProvider.GetExtendedLanguageName(extendedLanguage);

                        if (string.IsNullOrWhiteSpace(name))
                            index--;
                        else
                            layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), name);
                    }
                }
            }
            #endregion
            #region Conditions
            int conditionY = extendedLanguages ? 50 : 115;
            layout.AddText(new Rect(106, conditionY, 72, Global.GlyphLineHeight), DataNameProvider.ConditionsHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            // Total space is 80 pixels wide. Each condition icon is 16 pixels wide. So there is space for 5 condition icons per line.
            const int conditionsPerRow = 5;
            foreach (var condition in partyMember!.VisibleConditions)
            {
                if (condition == Condition.DeadAshes || condition == Condition.DeadDust)
                    continue;

                if (condition != Condition.DeadCorpse && !partyMember.Conditions.HasFlag(condition))
                    continue;

                int column = index % conditionsPerRow;
                int row = index / conditionsPerRow;
                ++index;

                int x = 96 + column * 16;
                int y = conditionY + 9 + row * 17;
                var area = new Rect(x, y, 16, 16);
                string conditionName = DataNameProvider.GetConditionName(condition);
                string? tooltip = CoreConfiguration.ShowPlayerStatsTooltips ? null : conditionName;
                layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetConditionGraphicIndex(condition), UIPaletteIndex,
                    2, tooltip, condition == Condition.DeadCorpse ? TextColor.DeadPartyMember : TextColor.ActivePartyMember);
                if (CoreConfiguration.ShowPlayerStatsTooltips)
                {
                    var tooltipCondition = condition;
                    if (tooltipCondition == Condition.DeadCorpse)
                    {
                        if (partyMember.Conditions.HasFlag(Condition.DeadDust))
                            tooltipCondition = Condition.DeadDust;
                        else if (partyMember.Conditions.HasFlag(Condition.DeadAshes))
                            tooltipCondition = Condition.DeadAshes;
                    }
                    AddTooltip(new Rect(x, y, 16, 16), conditionName + "^^" + GetConditionTooltip(GameLanguage, tooltipCondition, partyMember));
                }
            }
            #endregion
        }

        Action openAction = inventory ? (Action)OpenInventory : OpenCharacterStats;

        if ((currentWindow.Window == Window.Inventory && inventory) ||
            (currentWindow.Window == Window.Stats && !inventory))
        {
            openAction();
            openedAction?.Invoke();
        }
        else
        {
            closeWindowHandler?.Invoke(false);
            closeWindowHandler = null;

            Fade(() =>
            {
                openAction();
                openedAction?.Invoke();
            }, changeInputEnableStateWhileFading);
        }

        return true;
    }

    internal void OpenDictionary(Action<string> choiceHandler, Func<string, TextColor>? colorProvider = null, Action? abortAction = null)
    {
        void WordEntered(string word)
        {
            // Add to known words if the entered word is a valid dictionary word.
            int index = textDictionary.Entries.FindIndex(entry => string.Compare(entry, word, true) == 0);

            if (index != -1)
                CurrentSavegame!.AddDictionaryWord((uint)index);

            choiceHandler?.Invoke(word);
        }

        const int columns = 11;
        const int rows = 10;
        var popupArea = new Rect(32, 34, columns * 16, rows * 16);
        TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
        var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
        var mouthButton = popup.AddButton(new Position(popupArea.Left + 16, popupArea.Bottom - 30));
        var exitButton = popup.AddButton(new Position(popupArea.Right - 32 - Button.Width, popupArea.Bottom - 30));
        mouthButton.ButtonType = ButtonType.Mouth;
        exitButton.ButtonType = ButtonType.Exit;
        mouthButton.DisplayLayer = 200;
        exitButton.DisplayLayer = 200;
        mouthButton.LeftClickAction = () =>
            layout.OpenInputPopup(new Position(51, 87), 20, WordEntered);
        exitButton.LeftClickAction = () =>
        {
            layout.ClosePopup();
            abortAction?.Invoke();
        };
        var dictionaryList = popup.AddDictionaryListBox(Dictionary!.OrderBy(entry => entry).Select(entry => new KeyValuePair<string, Action<int, string>?>
        (
            entry, (int _, string text) =>
            {
                layout.ClosePopup(false);
                choiceHandler?.Invoke(text!);
            }
        )).ToList(), colorProvider);
        int scrollRange = Math.Max(0, Dictionary!.Count - 16);
        var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
        scrollbar.Scrolled += offset =>
        {
            dictionaryList.ScrollTo(offset);
        };
        popup.Closed += UntrapMouse;
    }

    /// <summary>
    /// Opens the list of spells.
    /// </summary>
    /// <param name="partyMember">Party member who want to use a spell.</param>
    /// <param name="spellAvailableChecker">Returns null if the spell can be used, otherwise the error message.</param>
    /// <param name="choiceHandler">Handler which receives the selected spell.</param>
    internal void OpenSpellList(PartyMember partyMember, Func<Spell, string?> spellAvailableChecker, Action<Spell> choiceHandler)
    {
        Pause();
        const int columns = 13;
        const int rows = 10;
        var popupArea = new Rect(32, 40, columns * 16, rows * 16);
        TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
        var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
        var spells = partyMember.LearnedSpells.Select(spell => new KeyValuePair<Spell, string?>(spell, spellAvailableChecker(spell))).ToList();
        string GetSpellEntry(Spell spell, bool available)
        {
            var spellInfo = SpellInfos[spell];
            string entry = DataNameProvider.GetSpellName(spell);

            if (available)
            {
                // append usage amount
                entry = entry.PadRight(21) + $"({Math.Min(99, partyMember.SpellPoints.CurrentValue / SpellInfos.GetSPCost(Features, spell, partyMember))})";
            }

            return entry;
        }
        var spellList = popup.AddSpellListBox(spells.Select(spell => new KeyValuePair<string, Action<int, string>?>
        (
            GetSpellEntry(spell.Key, spell.Value == null), spell.Value != null ? null : ((int index, string _) =>
            {
                UntrapMouse();
                layout.ClosePopup(false);
                Resume();
                choiceHandler?.Invoke(spells[index].Key);
            })
        )).ToList());
        popup.AddSunkenBox(new Rect(48, 173, 174, 10));
        var spellMessage = popup.AddText(new Rect(49, 175, 172, 6), "", TextColor.Bright, TextAlign.Center, true, 2);
        popup.Closed += () =>
        {
            UntrapMouse();
            Resume();
        };
        spellList.HoverItem += index =>
        {
            var message = index == -1 ? null : spells[index].Value;

            if (message == null)
                spellMessage.SetText(renderView.TextProcessor.CreateText(""));
            else
                spellMessage.SetText(ProcessText(message));
        };
        int scrollRange = Math.Max(0, spells.Count - 16);
        var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
        int slot = SlotFromPartyMember(partyMember)!.Value;
        scrollbar.Scrolled += offset =>
        {
            spellListScrollOffsets[slot] = offset;
            spellList.ScrollTo(offset);
        };
        // Initial scroll
        if (spellListScrollOffsets[slot] > scrollRange)
            spellListScrollOffsets[slot] = scrollRange;
        scrollbar.SetScrollPosition(spellListScrollOffsets[slot], true);
    }

    internal void ShowBriefMessagePopup(string text, TimeSpan displayTime,
        TextAlign textAlign = TextAlign.Center, byte displayLayerOffset = 0)
    {
        if (layout.PopupActive)
            return;

        bool paused = this.paused;
        bool inputEnabled = InputEnable;
        Pause();
        InputEnable = false;
        // Simple text popup
        Popup? popup;
        popup = layout.OpenTextPopup(ProcessText(text), () =>
        {
            popup = null;
            if (inputEnabled)
                InputEnable = true;
            if (!paused)
                Resume();
            ResetCursor();
        }, true, true, false, textAlign, displayLayerOffset, PrimaryUIPaletteIndex);
        CursorType = CursorType.Wait;
        TrapMouse(popup.ContentArea);
        AddTimedEvent(displayTime, () =>
        {
            if (popup != null)
                ClosePopup();
        });
    }

    internal void ShowMessagePopup(string text, Action? closeAction = null,
        TextAlign textAlign = TextAlign.Center, byte displayLayerOffset = 0,
        Position? offset = null)
    {
        if (layout.PopupActive)
        {
            closeAction?.Invoke();
            return;
        }

        Pause();
        InputEnable = false;
        // Simple text popup
        var popup = layout.OpenTextPopup(ProcessText(text), () =>
        {
            InputEnable = true;
            Resume();
            ResetCursor();
            closeAction?.Invoke();
        }, true, true, false, textAlign, displayLayerOffset, null, offset);
        CursorType = CursorType.Click;
        TrapMouse(popup.ContentArea);
    }

    internal void ShowTextPopup(IText text, Action<PopupTextEvent.Response>? responseHandler)
    {
        Pause();
        InputEnable = false;
        // Simple text popup
        layout.OpenTextPopup(text, () =>
        {
            InputEnable = true;
            Resume();
            ResetCursor();
            responseHandler?.Invoke(PopupTextEvent.Response.Close);
        }, true, true);
        CursorType = CursorType.Click;
    }

    protected void ShowEvent(IText text, uint imageIndex, Action? closeAction,
        bool gameOver = false)
    {
        GameOverButtonsVisible = false;

        Fade(() =>
        {
            SetWindow(Window.Event, gameOver);
            layout.SetLayout(LayoutType.Event);
            ShowMap(false);
            layout.Reset();
            layout.AddEventPicture(imageIndex, out currentUIPaletteIndex);
            layout.UpdateUIPalette(currentUIPaletteIndex);
            cursor.UpdatePalette(this);
            layout.FillArea(new Rect(16, 138, 288, 55), GetUIColor(28), false);

            // Position = 18,139, max 40 chars per line and 7 lines.
            var textArea = new Rect(18, 139, 285, 49);
            var scrollableText = layout.AddScrollableText(textArea, text, TextColor.BrightGray);

            ShowMobileClickIndicator(Global.VirtualScreenWidth / 2 - 8, Global.VirtualScreenHeight - 16 + (gameOver ? -8 : 3));

            scrollableText.Clicked += scrolledToEnd =>
            {
                if (scrolledToEnd)
                {
                    HideMobileClickIndicator();

                    if (gameOver)
                    {
                        scrollableText?.Destroy();
                        scrollableText = null;
                        AddLoadQuitOptions();
                    }
                    else
                    {
                        // Special case, we show a small game introduction
                        // when playing for the first time and closing the
                        // initial event with grandfather.
                        if (CoreConfiguration.FirstStart && imageIndex == 1)
                        {
                            // This avoids asking for introduction twice in the same sessions.
                            CoreConfiguration.FirstStart = false;
                            CloseWindow(() =>
                            {
                                closeAction?.Invoke();
                                ShowTutorial();
                            });
                        }
                        else
                        {
                            CloseWindow(closeAction);
                        }
                    }
                }
            };
            CursorType = CursorType.Click;
            InputEnable = false;
            void AddLoadQuitOptions()
            {
                GameOverButtonsVisible = true;
                InputEnable = true;
                bool hasSavegames = Provider_HasSavegames();
                
                layout.AddText(textArea, ProcessText(hasSavegames
                    ? DataNameProvider.GameOverLoadOrQuit
                    : GetCustomText(CustomTexts.Index.StartNewGameOrQuit)),
                    TextColor.BrightGray);
                void ShowButtons()
                {
                    ExecuteNextUpdateCycle(() => CursorType = CursorType.Sword);
                    layout.ShowGameOverButtons(load =>
                    {
                        if (load)
                        {
                            if (hasSavegames)
                                layout.OpenLoadMenu(CloseWindow, ShowButtons, true);
                            else
                                NewGame();
                        }
                        else
                        {
                            Quit(ShowButtons);
                        }
                    }, hasSavegames);
                }
                ShowButtons();
            }
        });
    }

    internal void ShowTextPopup(Map map, PopupTextEvent popupTextEvent, Action<PopupTextEvent.Response>? responseHandler)
    {
        var text = ProcessText(map.GetText((int)popupTextEvent.TextIndex, DataNameProvider.TextBlockMissing));

        if (popupTextEvent.HasImage)
        {
            // Those always use a custom layout
            ShowEvent(text, popupTextEvent.EventImageIndex,
                () => responseHandler?.Invoke(PopupTextEvent.Response.Close));
        }
        else
        {
            ShowTextPopup(text, responseHandler);
        }
    }

    internal void ShowDecisionPopup(string text, Action<PopupTextEvent.Response>? responseHandler,
        int minLines = 3, byte displayLayerOffset = 0, TextAlign textAlign = TextAlign.Left,
        bool canAbort = true)
    {
        var popup = layout.OpenYesNoPopup
        (
            ProcessText(text),
            () =>
            {
                layout.ClosePopup(false, true);
                InputEnable = true;
                Resume();
                responseHandler?.Invoke(PopupTextEvent.Response.Yes);
            },
            () =>
            {
                layout.ClosePopup(false, true);
                InputEnable = true;
                Resume();
                responseHandler?.Invoke(PopupTextEvent.Response.No);
            },
            () =>
            {
                InputEnable = true;
                Resume();
                responseHandler?.Invoke(PopupTextEvent.Response.Close);
            }, minLines, displayLayerOffset, textAlign
        );
        popup.CanAbort = canAbort;
        Pause();
        InputEnable = false;
        CursorType = CursorType.Sword;
    }

    internal void ShowDecisionPopup(Map map, DecisionEvent decisionEvent, Action<PopupTextEvent.Response>? responseHandler)
    {
        ShowDecisionPopup(map.GetText((int)decisionEvent.TextIndex, DataNameProvider.TextBlockMissing), responseHandler, 0);
    }

    void DisplayCharacterInfo(Character character, bool conversation)
    {
        void SetupSecondaryStatTooltip(Rect area, SecondaryStat secondaryStat)
        {
            characterInfoStatTooltips[secondaryStat] = ShowSecondaryStatTooltip(area, secondaryStat, character);
        }

        int offsetY = conversation ? -6 : 0;

        characterInfoTexts.Clear();
        characterInfoPanels.Clear();
        characterInfoStatTooltips.Clear();
        layout.FillArea(new Rect(208, offsetY + 49, 96, 80), GetUIColor(28), false);
        layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground, CustomGraphicPaletteIndex, 1);
        layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.PortraitOffset + character.PortraitIndex - 1, PrimaryUIPaletteIndex, 2);
        if (!string.IsNullOrEmpty(DataNameProvider.GetRaceName(character.Race)))
            layout.AddText(new Rect(242, offsetY + 49, 62, 7), DataNameProvider.GetRaceName(character.Race));
        layout.AddText(new Rect(242, offsetY + 56, 62, 7), DataNameProvider.GetGenderName(character.Gender));
        var area = new Rect(242, offsetY + 63, 62, 7);
        characterInfoTexts.Add(CharacterInfo.Age, layout.AddText(area,
            string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
            character.Attributes[Attribute.Age].CurrentValue)));
        SetupSecondaryStatTooltip(area, SecondaryStat.Age);
        if (character.Class < Class.Monster && !string.IsNullOrEmpty(DataNameProvider.GetClassName(character.Class)))
        {
            if (!conversation || character.Class < Class.Animal)
            {
                characterInfoTexts.Add(CharacterInfo.Level, layout.AddText(new Rect(242, offsetY + 70, 62, 7),
                    $"{DataNameProvider.GetClassName(character.Class)} {character.Level}"));
                characterInfoStatTooltips[SecondaryStat.LevelWithAPRIncrease] = ShowSecondaryStatTooltip(new Rect(242, offsetY + 70, 62, 7),
                    character.AttacksPerRoundIncreaseLevels == 0 ? SecondaryStat.LevelWithoutAPRIncrease : SecondaryStat.LevelWithAPRIncrease, character);
            }
        }
        layout.AddText(new Rect(208, offsetY + 84, 96, 7), character.Name, conversation ? TextColor.PartyMember : TextColor.ActivePartyMember, TextAlign.Center);
        if (!conversation)
        {
            bool magicClass = character.Class.IsMagic();

            if (character.Class != Class.Animal)
            {
                area = new Rect(242, 77, 62, 7);
                characterInfoTexts.Add(CharacterInfo.EP, layout.AddText(area,
                    string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                    character.ExperiencePoints)));
                characterInfoStatTooltips[SecondaryStat.EP50] = ShowSecondaryStatTooltip(area, character.Level < 50 ?
                    SecondaryStat.EPPre50 : SecondaryStat.EP50, character);
            }
            area = new Rect(208, 92, 96, 7);
            characterInfoTexts.Add(CharacterInfo.LP, layout.AddText(area,
                string.Format(DataNameProvider.CharacterInfoHitPointsString,
                Math.Min(character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue), character.HitPoints.TotalMaxValue),
                TextColor.White, TextAlign.Center));
            SetupSecondaryStatTooltip(area, SecondaryStat.LP);
            if (magicClass)
            {
                area = new Rect(208, 99, 96, 7);
                characterInfoTexts.Add(CharacterInfo.SP, layout.AddText(area,
                    string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                    Math.Min(character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue), character.SpellPoints.TotalMaxValue),
                    TextColor.White, TextAlign.Center));
                SetupSecondaryStatTooltip(area, SecondaryStat.SP);
            }
            characterInfoTexts.Add(CharacterInfo.SLPAndTP, layout.AddText(new Rect(208, 106, 96, 7),
                (magicClass ? string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) : new string(' ', 7)) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints), TextColor.White, TextAlign.Center));
            if (magicClass)
                SetupSecondaryStatTooltip(new Rect(214, 106, 42, 7), SecondaryStat.SLP);
            SetupSecondaryStatTooltip(new Rect(262, 106, 36, 7), SecondaryStat.TP);
            var displayGold = OpenStorage is IPlace ? 0 : character.Gold;
            characterInfoTexts.Add(CharacterInfo.GoldAndFood, layout.AddText(new Rect(208, 113, 96, 7),
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, displayGold, character.Food),
                TextColor.White, TextAlign.Center));
            SetupSecondaryStatTooltip(new Rect(214, 113, 42, 7), SecondaryStat.Gold);
            SetupSecondaryStatTooltip(new Rect(262, 113, 36, 7), SecondaryStat.Food);
            layout.AddSprite(new Rect(214, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), UIPaletteIndex);
            int attack = character.BaseAttackDamage + AddAttributeDamageBonus(character, AdjustAttackDamageForNotUsedAmmunition(character, character.BonusAttackDamage));
            if (CurrentSavegame!.IsSpellActive(ActiveSpellType.Attack))
            {
                if (attack > 0)
                    attack = (attack * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
                string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack));
                characterInfoTexts.Add(CharacterInfo.Attack, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                    new Rect(220, 122, 30, 7), attackString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
            }
            else
            {
                string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack));
                characterInfoTexts.Add(CharacterInfo.Attack, layout.AddText(new Rect(220, 122, 30, 7), attackString, TextColor.White, TextAlign.Left));
            }
            SetupSecondaryStatTooltip(new Rect(214, 120, 36, 9), SecondaryStat.Damage);
            layout.AddSprite(new Rect(261, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), UIPaletteIndex);
            int defense = character.BaseDefense + character.BonusDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
            {
                if (defense > 0)
                    defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
                string defenseString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense));
                characterInfoTexts.Add(CharacterInfo.Defense, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                    new Rect(268, 122, 30, 7), defenseString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
            }
            else
            {
                string defenseString = string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense));
                characterInfoTexts.Add(CharacterInfo.Defense, layout.AddText(new Rect(268, 122, 30, 7), defenseString, TextColor.White, TextAlign.Left));
            }
            SetupSecondaryStatTooltip(new Rect(261, 120, 37, 9), SecondaryStat.Defense);
        }
        else
        {
            characterInfoTexts.Add(CharacterInfo.ConversationPartyMember,
                layout.AddText(new Rect(208, 99, 96, 7), CurrentPartyMember!.Name, TextColor.ActivePartyMember, TextAlign.Center));
            if (CurrentPartyMember.Gold > 0)
            {
                ShowTextPanel(CharacterInfo.ConversationGold, CurrentPartyMember.Gold > 0,
                    $"{DataNameProvider.GoldName}^{CurrentPartyMember.Gold}", new Rect(209, 107, 43, 15));
            }
            if (CurrentPartyMember.Food > 0)
            {
                ShowTextPanel(CharacterInfo.ConversationFood, CurrentPartyMember.Food > 0,
                    $"{DataNameProvider.FoodName}^{CurrentPartyMember.Food}", new Rect(257, 107, 43, 15));
            }
        }
    }

    internal void UpdateCharacterInfo(Character? conversationPartner = null)
    {
        if (currentWindow.Window != Window.Inventory &&
            currentWindow.Window != Window.Stats &&
            currentWindow.Window != Window.Conversation)
            return;

        if (currentWindow.Window == Window.Conversation)
        {
            if (conversationPartner == null || CurrentPartyMember == null)
                return;
        }
        else if (CurrentInventory == null)
        {
            return;
        }

        void UpdateText(CharacterInfo characterInfo, Func<string> text, bool checkNextCycle = false)
        {
            if (characterInfoTexts.ContainsKey(characterInfo))
                characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text()));
            else if (checkNextCycle)
            {
                // The weight display and maybe others might only be added in next cycle so
                // re-check in two cycles.
                ExecuteNextUpdateCycle(() => ExecuteNextUpdateCycle(() => UpdateText(characterInfo, text, false)));
            }
        }

        var character = conversationPartner ?? CurrentInventory;
        bool magicClass = character!.Class.IsMagic();

        void UpdateSecondaryStatTooltip(SecondaryStat secondaryStat)
        {
            if (CoreConfiguration.ShowPlayerStatsTooltips && characterInfoStatTooltips.TryGetValue(secondaryStat, out var toolip) && toolip != null)
                this.UpdateSecondaryStatTooltip(toolip, secondaryStat, character);
        }

        UpdateText(CharacterInfo.Age, () => string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
            character.Attributes[Attribute.Age].CurrentValue));
        UpdateSecondaryStatTooltip(SecondaryStat.Age);
        UpdateText(CharacterInfo.Level, () => $"{DataNameProvider.GetClassName(character.Class)} {character.Level}");
        UpdateSecondaryStatTooltip(SecondaryStat.LevelWithAPRIncrease);
        UpdateText(CharacterInfo.EP, () => string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
            character.ExperiencePoints));
        UpdateSecondaryStatTooltip(SecondaryStat.EP50);
        UpdateText(CharacterInfo.LP, () => string.Format(DataNameProvider.CharacterInfoHitPointsString,
            character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue));
        UpdateSecondaryStatTooltip(SecondaryStat.LP);
        if (magicClass)
        {
            UpdateText(CharacterInfo.SP, () => string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue));
            UpdateSecondaryStatTooltip(SecondaryStat.SP);
            UpdateSecondaryStatTooltip(SecondaryStat.SLP);
        }
        UpdateText(CharacterInfo.SLPAndTP, () =>
            (magicClass ? string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) : new string(' ', 7)) + " " +
            string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints));
        UpdateSecondaryStatTooltip(SecondaryStat.TP);
        UpdateText(CharacterInfo.GoldAndFood, () =>
            string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, character.Gold, character.Food));
        UpdateSecondaryStatTooltip(SecondaryStat.Gold);
        UpdateSecondaryStatTooltip(SecondaryStat.Food);
        int attackDamage = character.BaseAttackDamage + AddAttributeDamageBonus(character, AdjustAttackDamageForNotUsedAmmunition(character, character.BonusAttackDamage));
        if (CurrentSavegame!.IsSpellActive(ActiveSpellType.Attack))
        {
            if (attackDamage > 0)
                attackDamage = (attackDamage * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
            UpdateText(CharacterInfo.Attack, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attackDamage < 0 ? '-' : '+'), Math.Abs(attackDamage)));
        }
        else
        {
            UpdateText(CharacterInfo.Attack, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attackDamage < 0 ? '-' : '+'), Math.Abs(attackDamage)));
        }
        UpdateSecondaryStatTooltip(SecondaryStat.Damage);
        int defense = character.BaseDefense + character.BonusDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
        if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
        {
            if (defense > 0)
                defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
            UpdateText(CharacterInfo.Defense, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)));
        }
        else
        {
            UpdateText(CharacterInfo.Defense, () =>
                string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)));
        }
        UpdateSecondaryStatTooltip(SecondaryStat.Defense);
        UpdateText(CharacterInfo.Weight, () => string.Format(DataNameProvider.CharacterInfoWeightString,
            character.TotalWeight / 1000, (character as PartyMember)!.MaxWeight / 1000), true);
        if (conversationPartner != null)
        {
            UpdateText(CharacterInfo.ConversationPartyMember, () => CurrentPartyMember!.Name);
            ShowTextPanel(CharacterInfo.ConversationGold, CurrentPartyMember!.Gold > 0,
                $"{DataNameProvider.GoldName}^{CurrentPartyMember.Gold}", new Rect(209, 107, 43, 15));
            ShowTextPanel(CharacterInfo.ConversationFood, CurrentPartyMember.Food > 0,
                $"{DataNameProvider.FoodName}^{CurrentPartyMember.Food}", new Rect(257, 107, 43, 15));
        }
    }

    void HideTextPanel(CharacterInfo characterInfo)
    {
        ShowTextPanel(characterInfo, false, null, null);
    }

    void ShowTextPanel(CharacterInfo characterInfo, bool show, string? text, Rect? area)
    {
        if (show)
        {
            if (area == null)
                return;

            if (!characterInfoPanels.ContainsKey(characterInfo))
                characterInfoPanels[characterInfo] = layout.AddPanel(area, 2);
            if (!characterInfoTexts.TryGetValue(characterInfo, out UIText? value))
            {
                characterInfoTexts[characterInfo] = layout.AddText(area.CreateOffset(0, 1),
                    text ?? "", TextColor.White, TextAlign.Center, 4);
            }
            else
                value.SetText(renderView.TextProcessor.CreateText(text));
        }
        else
        {
            if (characterInfoPanels.TryGetValue(characterInfo, out Panel? value))
            {
                value.Destroy();
                characterInfoPanels.Remove(characterInfo);
            }
            if (characterInfoTexts.TryGetValue(characterInfo, out UIText? value1))
            {
                value1.Destroy();
                characterInfoTexts.Remove(characterInfo);
            }
        }
    }

    void SetWindow(Window window, params object?[] parameters)
    {
        showMobileTouchPadHandler?.Invoke(window == Window.MapView);

        CurrentMobileAction = MobileAction.None;

        if ((window != Window.Inventory && window != Window.Stats) ||
            (currentWindow.Window != Window.Inventory && currentWindow.Window != Window.Stats))
            LastWindow = currentWindow;
        if (currentWindow.Window == window)
            currentWindow.WindowParameters = parameters;
        else
            currentWindow = new WindowInfo { Window = window, WindowParameters = parameters };
    }

    internal void ClosePopup(bool raiseEvent = true, bool force = false) => layout?.ClosePopup(raiseEvent, force);

    internal void CloseWindow() => CloseWindow(null);

    internal void CloseWindow(Action? finishAction)
    {
        layout.HideTooltip();

        if (!WindowActive)
        {
            finishAction?.Invoke();
            return;
        }

        ResetMapCharacterInteraction(Map!);
        layout.SetCharacterHealSymbol(null);

        closeWindowHandler?.Invoke(true);
        closeWindowHandler = null;

        characterInfoTexts.Clear();
        characterInfoPanels.Clear();
        characterInfoStatTooltips.Clear();
        CurrentInventoryIndex = null;
        HideWindowTitle();
        weightDisplayBlinking = false;
        layout.ButtonsDisabled = false;

        if (currentWindow.Window == Window.Event || currentWindow.Window == Window.Riddlemouth)
        {
            InputEnable = true;
            ResetCursor();
        }

        var closedWindow = currentWindow;

        if (currentWindow.Window == LastWindow.Window)
            currentWindow = DefaultWindow;
        else
            currentWindow = LastWindow;

        switch (currentWindow.Window)
        {
            case Window.MapView:
            {
                currentPlace = null;

                Fade(() =>
                {
                    if (CurrentMapCharacter != null &&
                        (closedWindow.Window == Window.Battle ||
                        closedWindow.Window == Window.BattleLoot ||
                        closedWindow.Window == Window.Chest ||
                        closedWindow.Window == Window.Event))
                        CurrentMapCharacter = null;

                    bool wasGameOver = closedWindow.Window == Window.Event && (bool)closedWindow.WindowParameters[0]! == true;

                    ShowMap(true, !wasGameOver); // avoid playing music after gameover as Start() will start the music as well afterwards
                    finishAction?.Invoke();

                    if (closedWindow.Window == Window.BattleLoot)
                        (closedWindow.WindowParameters[1] as Action)?.Invoke();
                });
                break;
            }
            case Window.Inventory:
            {
                int partyMemberIndex = (int)currentWindow.WindowParameters[0]!;
                currentWindow = DefaultWindow;
                OpenPartyMember(partyMemberIndex, true);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Stats:
            {
                int partyMemberIndex = (int)currentWindow.WindowParameters[0]!;
                currentWindow = DefaultWindow;
                OpenPartyMember(partyMemberIndex, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Chest:
            {
                var chestEvent = (ChestEvent)currentWindow.WindowParameters[0]!;
                bool trapFound = (bool)currentWindow.WindowParameters[1]!;
                bool trapDisarmed = (bool)currentWindow.WindowParameters[2]!;
                var map = (Map)currentWindow.WindowParameters[3]!;
                var position = (Position)currentWindow.WindowParameters[4]!;
                var triggerFollowEvents = (bool)currentWindow.WindowParameters[5]!;
                currentWindow = DefaultWindow;
                ShowChest(chestEvent, trapFound, trapDisarmed, map, position, false, triggerFollowEvents);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Door:
            {
                var doorEvent = (DoorEvent)currentWindow.WindowParameters[0]!;
                bool trapFound = (bool)currentWindow.WindowParameters[1]!;
                bool trapDisarmed = (bool)currentWindow.WindowParameters[2]!;
                var map = (Map)currentWindow.WindowParameters[3]!;
                var x = (uint)currentWindow.WindowParameters[4]!;
                var y = (uint)currentWindow.WindowParameters[5]!;
                var moved = (bool)currentWindow.WindowParameters[6]!;
                currentWindow = DefaultWindow;
                ShowDoor(doorEvent, trapFound, trapDisarmed, map, x, y, false, moved);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Merchant:
            {
                uint merchantIndex = (uint)currentWindow.WindowParameters[0]!;
                string placeName = (string)currentWindow.WindowParameters[1]!;
                string buyText = (string)currentWindow.WindowParameters[2]!;
                bool isLibrary = (bool)currentWindow.WindowParameters[3]!;
                var boughtItems = (ItemSlot[])currentWindow.WindowParameters[4]!;
                OpenMerchant(merchantIndex, placeName, buyText, isLibrary, false, boughtItems);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Riddlemouth:
            {
                var riddlemouthEvent = (RiddlemouthEvent)currentWindow.WindowParameters[0]!;
                var solvedEvent = (currentWindow.WindowParameters[1] as Action)!;
                currentWindow = DefaultWindow;
                ShowRiddlemouth(Map!, riddlemouthEvent, solvedEvent, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Conversation:
            {
                var conversationPartner = (currentWindow.WindowParameters[0] as IConversationPartner)!;
                var characterIndex = currentWindow.WindowParameters[1] as uint?;
                var conversationEvent = (currentWindow.WindowParameters[2] as Event)!;
                var conversationItems = (currentWindow.WindowParameters[3] as ConversationItems)!;
                currentWindow = DefaultWindow;
                ShowConversation(conversationPartner, characterIndex, conversationEvent, conversationItems, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Battle:
            {
                var nextEvent = (Event)currentWindow.WindowParameters[0]!;
                var x = (uint)currentWindow.WindowParameters[1]!;
                var y = (uint)currentWindow.WindowParameters[2]!;
                var combatBackgroundIndex = (uint?)currentWindow.WindowParameters[3];
                currentWindow = DefaultWindow;
                Fade(() => { ShowBattleWindow(nextEvent, out _, x, y, combatBackgroundIndex); finishAction?.Invoke(); });
                break;
            }
            case Window.BattleLoot:
            {
                var storage = (ITreasureStorage)currentWindow.WindowParameters[0]!;
                LastWindow = DefaultWindow;
                ShowBattleLoot(storage, null, 0);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.BattlePositions:
            {
                ShowBattlePositionWindow();
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Trainer:
            {
                var trainer = (Places.Trainer)currentWindow.WindowParameters[0]!;
                OpenTrainer(trainer, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.FoodDealer:
            {
                var foodDealer = (Places.FoodDealer)currentWindow.WindowParameters[0]!;
                OpenFoodDealer(foodDealer, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Healer:
            {
                var healer = (Places.Healer)currentWindow.WindowParameters[0]!;
                OpenHealer(healer, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Camp:
            {
                bool inn = (bool)currentWindow.WindowParameters[0]!;
                int healing = (int)currentWindow.WindowParameters[1]!;
                OpenCamp(inn, healing);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Inn:
            {
                var inn = (Places.Inn)currentWindow.WindowParameters[0]!;
                var useText = (string)currentWindow.WindowParameters[1]!;
                OpenInn(inn, useText, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.HorseSalesman:
            {
                var salesman = (Places.HorseSalesman)currentWindow.WindowParameters[0]!;
                var buyText = (string)currentWindow.WindowParameters[1]!;
                OpenHorseSalesman(salesman, buyText, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.RaftSalesman:
            {
                var salesman = (Places.RaftSalesman)currentWindow.WindowParameters[0]!;
                var buyText = (string)currentWindow.WindowParameters[1]!;
                OpenRaftSalesman(salesman, buyText, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.ShipSalesman:
            {
                var salesman = (Places.ShipSalesman)currentWindow.WindowParameters[0]!;
                var buyText = (string)currentWindow.WindowParameters[1]!;
                OpenShipSalesman(salesman, buyText, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Sage:
            {
                var sage = (Places.Sage)currentWindow.WindowParameters[0]!;
                OpenSage(sage, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Blacksmith:
            {
                var blacksmith = (Places.Blacksmith)currentWindow.WindowParameters[0]!;
                OpenBlacksmith(blacksmith, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Enchanter:
            {
                var enchanter = (Places.Enchanter)currentWindow.WindowParameters[0]!;
                OpenEnchanter(enchanter, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Automap:
            {
                ShowAutomap((AutomapOptions)currentWindow.WindowParameters[0]!);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            default:
                break;
        }
    }

    internal void ToggleButtonGridPage() => layout.ToggleButtonGridPage();

    public void ShowWindowTitle(bool show = true)
    {
        if (windowTitle != null)
            windowTitle.Visible = show;
    }

    public void HideWindowTitle() => ShowWindowTitle(false);

    void ShowTutorial()
    {
        new Tutorial(this, drawTouchFingerRequest).Run(renderView);
    }
}
