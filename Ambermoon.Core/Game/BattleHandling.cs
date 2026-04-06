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
    enum PlayerBattleAction
    {
        /// <summary>
        /// This is the initial action in each round.
        /// The player can select the active party member.
        /// He also can select actions.
        /// </summary>
        PickPlayerAction,
        PickEnemySpellTarget,
        PickEnemySpellTargetRow,
        PickFriendSpellTarget,
        PickMoveSpot,
        PickAttackSpot,
        PickMemberToBlink,
        PickBlinkTarget
    }

    BattleInfo? currentBattleInfo = null;
    Battle? currentBattle = null;
    Func<Position, MouseButtons, bool>? battlePositionClickHandler = null;
    Action<Position>? battlePositionDragHandler = null;
    bool battlePositionDragging = false;
    readonly ILayerSprite?[] partyMemberBattleFieldSprites = new ILayerSprite?[MaxPartyMembers];
    readonly List<ILayerSprite> highlightBattleFieldSprites = [];
    readonly Tooltip?[] partyMemberBattleFieldTooltips = new Tooltip?[MaxPartyMembers];
    PlayerBattleAction currentPlayerBattleAction = PlayerBattleAction.PickPlayerAction;
    readonly Dictionary<int, Battle.PlayerBattleAction> roundPlayerBattleActions = new(MaxPartyMembers);
    readonly ILayerSprite battleRoundActiveSprite; // sword and mace    

    public bool BattleActive => currentBattle != null;
    public bool BattleRoundActive => currentBattle?.RoundActive == true;
    public bool PlayerIsPickingABattleAction => BattleActive && !BattleRoundActive && currentPlayerBattleAction != PlayerBattleAction.PickPlayerAction;
    float BattleTimeFactor => currentBattle != null && CoreConfiguration.BattleSpeed != 0 && currentWindow.Window == Window.Battle
        ? 1.0f + CoreConfiguration.BattleSpeed / 33.0f : 1.0f;

    internal void StartBattle(StartBattleEvent battleEvent, Event nextEvent, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        if (BattleActive)
            return;

        ResetMoveKeys();

        currentBattleInfo = new BattleInfo
        {
            MonsterGroupIndex = battleEvent.MonsterGroupIndex
        };
        ShowBattleWindow(nextEvent, false, x, y, combatBackgroundIndex);
    }

    internal uint GetCombatBackgroundIndex(Map map, uint x, uint y) => is3D
        ? renderMap3D!.CombatBackgroundIndex
        : renderMap2D!.GetCombatBackgroundIndex(map, x, y);

    /// <summary>
    /// This is used by external triggers like a cheat engine.
    /// 
    /// Returns false if the current game state does not allow
    /// to start a fight.
    /// </summary>
    public bool StartBattle(uint monsterGroupIndex)
    {
        if (WindowActive || BattleActive || layout.PopupActive ||
            allInputDisabled || !inputEnable || !Ingame)
            return false;

        uint? combatBackgroundIndex = null;

        if (!Is3D)
        {
            var tile = renderMap2D![player!.Position];

            if (tile != null)
            {
                var tileset = MapManager.GetTilesetForMap(Map);

                if (tile.FrontTileIndex != 0)
                {
                    var frontTile = tileset.Tiles[tile.FrontTileIndex - 1];

                    if (frontTile.UseBackgroundTileFlags)
                        combatBackgroundIndex = tileset.Tiles[tile.BackTileIndex - 1].CombatBackgroundIndex;
                    else
                        combatBackgroundIndex = frontTile.CombatBackgroundIndex;
                }
                else if (tile.BackTileIndex != 0)
                    combatBackgroundIndex = tileset.Tiles[tile.BackTileIndex - 1].CombatBackgroundIndex;
            }
        }

        StartBattle(monsterGroupIndex, false, (uint)player!.Position.X, (uint)player.Position.Y, null, combatBackgroundIndex);
        return true;
    }

    /// <summary>
    /// Starts a battle with the given monster group index.
    /// It is used for monsters that are present on the map.
    /// </summary>
    /// <param name="monsterGroupIndex">Monster group index</param>
    internal void StartBattle(uint monsterGroupIndex, bool failedEscape, uint x, uint y,
        Action<BattleEndInfo>? battleEndHandler, uint? combatBackgroundIndex = null)
    {
        if (BattleActive)
            return;

        currentBattleInfo = new BattleInfo
        {
            MonsterGroupIndex = monsterGroupIndex
        };

        if (battleEndHandler != null)
            currentBattleInfo.BattleEnded += battleEndHandler;

        ShowBattleWindow(null, failedEscape, x, y, combatBackgroundIndex);
    }

    void UpdateBattle(double blinkingTimeFactor)
    {
        if (partyAdvances)
        {
            foreach (var monster in currentBattle!.Monsters)
                layout.GetMonsterBattleAnimation(monster)!.Update(CurrentBattleTicks);
        }
        else
        {
            currentBattle!.Update(CurrentBattleTicks, CurrentNormalizedBattleTicks);
        }

        if (highlightBattleFieldSprites.Count != 0)
        {
            var ticks = Math.Round(CurrentBattleTicks * blinkingTimeFactor);
            bool showBlinkingSprites = !blinkingHighlight || (ticks % (2 * TicksPerSecond / 3)) < TicksPerSecond / 3;

            foreach (var blinkingBattleFieldSprite in highlightBattleFieldSprites)
            {
                blinkingBattleFieldSprite.Visible = showBlinkingSprites;
            }
        }
    }

    internal void ShowBattleLoot(BattleEndInfo battleEndInfo, Action closeAction)
    {
        var gold = battleEndInfo.KilledMonsters.Sum(m => m.Gold);
        var food = battleEndInfo.KilledMonsters.Sum(m => m.Food);
        var loot = new Chest
        {
            Type = ChestType.Junk,
            Gold = (uint)gold,
            Food = (uint)food,
            AllowsItemDrop = false,
            IsBattleLoot = true
        };
        for (int r = 0; r < 4; ++r)
        {
            for (int c = 0; c < 6; ++c)
            {
                loot.Slots[c, r] = new ItemSlot
                {
                    ItemIndex = 0,
                    Amount = 0
                };
            }
        }
        var slots = loot.Slots.ToList();
        foreach (var item in battleEndInfo.KilledMonsters
            .SelectMany(m => Enumerable.Concat(m.Inventory.Slots, m.Equipment.Slots.Values)
                .Where(slot => slot != null && !slot.Empty)))
        {
            bool stackable = ItemManager.GetItem(item.ItemIndex).Flags.HasFlag(ItemFlags.Stackable);

            while (item.Amount > 0)
            {
                ItemSlot? slot = null;

                if (stackable)
                    slot = slots.FirstOrDefault(s => s.ItemIndex == item.ItemIndex && s.Amount < 99);

                slot ??= slots.FirstOrDefault(s => s.Empty);

                if (slot == null) // doesn't fit
                    break;

                slot.Add(item);
            }
        }
        foreach (var brokenItem in battleEndInfo.BrokenItems)
        {
            var slot = slots.FirstOrDefault(s => s.Empty);

            if (slot == null) // doesn't fit
                break;

            slot.ItemIndex = brokenItem.Key;
            slot.Amount = 1;
            slot.Flags = brokenItem.Value | ItemSlotFlags.Broken;
        }
        var expReceivingPartyMembers = PartyMembers.Where(m => m.Alive && !battleEndInfo.FledPartyMembers.Contains(m) && m.Race <= Race.Thalionic).ToList();
        int expPerPartyMember = expReceivingPartyMembers.Count == 0 ? 0 : battleEndInfo.TotalExperience / expReceivingPartyMembers.Count;

        if (loot.Empty)
        {
            Pause();
            void Finish()
            {
                Resume();
                closeAction?.Invoke();
            }
            CloseWindow(() =>
            {
                if (expReceivingPartyMembers.Count == 0)
                {
                    Finish();
                }
                else
                {
                    ShowMessagePopup(string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
                    {
                        Pause();
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, Finish);
                    });
                }
            });
        }
        else
        {
            Fade(() =>
            {
                InputEnable = true;
                SetWindow(Window.BattleLoot, loot, closeAction);
                LastWindow = DefaultWindow;
                ShowBattleLoot(loot, expReceivingPartyMembers, expPerPartyMember, false);
            });
        }
    }

    void ShowBattleLoot(ITreasureStorage storage, List<PartyMember>? expReceivingPartyMembers,
        int expPerPartyMember, bool fade = true)
    {
        void Show()
        {
            InputEnable = true;
            layout.Reset();
            ShowLoot(storage, expReceivingPartyMembers == null || expReceivingPartyMembers.Count == 0 ? null : string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
            {
                if (expReceivingPartyMembers != null)
                {
                    if (expReceivingPartyMembers.Count > 0)
                    {
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, () =>
                        {
                            layout.ShowChestMessage(DataNameProvider.LootAfterBattle, TextAlign.Left);
                        });
                    }
                    else
                    {
                        layout.ShowChestMessage(DataNameProvider.LootAfterBattle, TextAlign.Left);
                    }
                }
            });
        }

        if (fade)
            Fade(Show);
        else
            Show();
    }

    static UIGraphic GetDisabledStatusGraphic(PartyMember partyMember)
    {
        if (!partyMember.Alive)
            return UIGraphic.StatusDead;
        else if (partyMember.Conditions.HasFlag(Condition.Petrified))
            return UIGraphic.StatusPetrified;
        else if (partyMember.Conditions.HasFlag(Condition.Sleep))
            return UIGraphic.StatusSleep;
        else if (partyMember.Conditions.HasFlag(Condition.Panic))
            return UIGraphic.StatusPanic;
        else if (partyMember.Conditions.HasFlag(Condition.Crazy))
            return UIGraphic.StatusCrazy;
        else
            throw new AmbermoonException(ExceptionScope.Application, $"Party member {partyMember.Name} is not disabled.");
    }

    internal void UpdateBattleStatus(PartyMember partyMember)
    {
        UpdateBattleStatus(SlotFromPartyMember(partyMember)!.Value, partyMember);
    }

    void UpdateBattleStatus(int slot)
    {
        UpdateBattleStatus(slot, GetPartyMember(slot));
    }

    void UpdateBattleStatus(int slot, PartyMember? partyMember)
    {
        if (partyMember == null)
        {
            layout.UpdateCharacterStatus(slot, null);
            roundPlayerBattleActions.Remove(slot);
        }
        else if (!partyMember.Conditions.CanSelect())
        {
            // Note: Disabled players will show the status icon next to
            // their portraits instead of an action icon. For mad players
            // when the battle starts the action icon will be shown instead.
            layout.UpdateCharacterStatus(slot, GetDisabledStatusGraphic(partyMember));
            roundPlayerBattleActions.Remove(slot);
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot]!.TextColor = TextColor.DeadPartyMember;
        }
        else if (roundPlayerBattleActions.ContainsKey(slot))
        {
            var action = roundPlayerBattleActions[slot];
            layout.UpdateCharacterStatus(slot, action.BattleAction.ToStatusGraphic(action.Parameter, ItemManager));
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot]!.TextColor = TextColor.White;
        }
        else
        {
            layout.UpdateCharacterStatus(slot, null);
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot]!.TextColor = TextColor.White;
        }
    }

    void UpdateBattleStatus()
    {
        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            UpdateBattleStatus(i);
        }

        layout.UpdateCharacterNameColors(CurrentSavegame!.ActivePartyMemberSlot);
    }

    internal bool BattlePositionWindowClick(Position position, MouseButtons mouseButtons)
    {
        return battlePositionClickHandler?.Invoke(position, mouseButtons) ?? false;
    }

    internal void BattlePositionWindowDrag(Position position)
    {
        battlePositionDragHandler?.Invoke(position);
    }

    internal void ShowBattlePositionWindow()
    {
        Fade(() =>
        {
            SetWindow(Window.BattlePositions);
            layout.SetLayout(LayoutType.BattlePositions);
            ShowMap(false);
            layout.Reset();

            // Upper box
            var backgroundColor = GetUIColor(25);
            var upperBoxBounds = new Rect(14, 43, 290, 80);
            layout.FillArea(upperBoxBounds, GetUIColor(28), 0);
            var positionBoxes = new Rect[12];
            byte paletteIndex = UIPaletteIndex;
            var portraits = PartyMembers.ToDictionary(p => SlotFromPartyMember(p)!.Value,
                p => layout.AddSprite(new Rect(0, 0, 32, 34), Graphics.PortraitOffset + p.PortraitIndex - 1, paletteIndex, 5, p.Name, TextColor.White));
            var portraitBackgrounds = PartyMembers.ToDictionary(p => SlotFromPartyMember(p)!.Value, _ => (FilledArea?)null);
            var battlePositions = CurrentSavegame!.BattlePositions.Select((p, i) => new { p, i }).Where(p => GetPartyMember(p.i) != null).ToDictionary(p => (int)p.p, p => p.i);
            // Each box is 34x36 pixels in size (with border)
            // 43 pixels y-offset to second row
            // Between each box there is a x-offset of 48 pixels
            for (int r = 0; r < 2; ++r)
            {
                for (int c = 0; c < 6; ++c)
                {
                    int index = c + r * 6;
                    var area = positionBoxes[index] = new Rect(15 + c * 48, 44 + r * 43, 34, 36);
                    layout.AddSunkenBox(area, 2);

                    if (battlePositions.TryGetValue(index, out int slot))
                    {
                        portraits[slot].X = area.Left + 1;
                        portraits[slot].Y = area.Top + 1;
                        portraitBackgrounds[slot]?.Destroy();
                        portraitBackgrounds[slot] = layout.FillArea(new Rect(area.Left + 1, area.Top + 1, 32, 34), backgroundColor, 4);
                    }
                }
            }

            // Lower box
            var lowerBoxBounds = new Rect(16, 144, 176, 48);
            layout.FillArea(lowerBoxBounds, GetUIColor(28), 0);
            layout.AddText(lowerBoxBounds, DataNameProvider.ChooseBattlePositions);

            closeWindowHandler = _ =>
            {
                battlePositionClickHandler = null;
                battlePositionDragHandler = null;
                battlePositionDragging = false;

                if (battlePositions.Count != PartyMembers.Count())
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid number of battle positions.");

                foreach (var battlePosition in battlePositions)
                {
                    if (battlePosition.Value < 0 || battlePosition.Value >= MaxPartyMembers || GetPartyMember(battlePosition.Value) == null)
                        throw new AmbermoonException(ExceptionScope.Application, $"Invalid party member slot: {battlePosition.Value}.");
                    if (battlePosition.Key < 0 || battlePosition.Key >= 12)
                        throw new AmbermoonException(ExceptionScope.Application, $"Invalid battle position for party member slot {battlePosition.Value}: {battlePosition.Key}");
                    CurrentSavegame.BattlePositions[battlePosition.Value] = (byte)battlePosition.Key;
                }
            };

            // Quick&dirty dragging logic
            int? slotOfDraggedPartyMember = null;
            int? dragSource = null;
            void Pickup(int position, bool trap = true, int? specificPartyMemberSlot = null)
            {
                slotOfDraggedPartyMember = specificPartyMemberSlot ?? battlePositions[position];
                dragSource = position;
                battlePositionDragging = true;
                if (trap)
                    TrapMouse(upperBoxBounds);
            }
            void Drop(int position, bool untrap = true)
            {
                if (slotOfDraggedPartyMember != null)
                {
                    var area = positionBoxes[position];
                    int slot = slotOfDraggedPartyMember.Value;
                    var draggedPortrait = portraits[slot];
                    draggedPortrait.DisplayLayer = 5;
                    draggedPortrait.X = area.Left + 1;
                    draggedPortrait.Y = area.Top + 1;
                    portraitBackgrounds[slot]?.Destroy();
                    portraitBackgrounds[slot] = layout.FillArea(new Rect(area.Left + 1, area.Top + 1, 32, 34), backgroundColor, 4);
                    slotOfDraggedPartyMember = null;
                    dragSource = null;
                    battlePositionDragging = false;
                    if (untrap)
                        UntrapMouse();
                }
            }
            void Drag(Position position)
            {
                if (slotOfDraggedPartyMember != null)
                {
                    int slot = slotOfDraggedPartyMember.Value;
                    var draggedPortrait = portraits[slot];
                    draggedPortrait.DisplayLayer = 7;
                    draggedPortrait.X = position.X;
                    draggedPortrait.Y = position.Y;
                    portraitBackgrounds[slot]?.Destroy();
                    portraitBackgrounds[slot] = layout.FillArea(new Rect(position.X, position.Y, 32, 34), backgroundColor, 6);
                }
            }
            void Reset(Position position)
            {
                // Reset back to source
                // If there is already a party member, exchange instead
                if (battlePositions[dragSource.Value] == slotOfDraggedPartyMember!.Value)
                    Drop(dragSource.Value);
                else
                {
                    // Exchange portrait
                    int index = dragSource.Value;
                    var temp = battlePositions[index];
                    battlePositions[index] = slotOfDraggedPartyMember!.Value;
                    Drop(index, false);
                    Pickup(index, false, temp);
                    Drag(position);
                }
            }
            battlePositionClickHandler = (position, mouseButtons) =>
            {
                if (mouseButtons == MouseButtons.Left)
                {
                    for (int i = 0; i < positionBoxes.Length; ++i)
                    {
                        if (positionBoxes[i].Contains(position))
                        {
                            if (slotOfDraggedPartyMember == null) // Not dragging
                            {
                                if (battlePositions.ContainsKey(i))
                                {
                                    // Drag portrait
                                    Pickup(i);
                                    Drag(position);
                                }
                            }
                            else // Dragging
                            {
                                if (battlePositions.ContainsKey(i))
                                {
                                    if (battlePositions[i] != slotOfDraggedPartyMember.Value)
                                    {
                                        // Exchange portrait
                                        var temp = battlePositions[i];
                                        battlePositions[i] = slotOfDraggedPartyMember.Value;
                                        if (dragSource!.Value != i && battlePositions[dragSource.Value] == slotOfDraggedPartyMember.Value)
                                            battlePositions.Remove(dragSource.Value);
                                        Drop(i, false);
                                        Pickup(i, false, temp);
                                        Drag(position);
                                    }
                                    else
                                    {
                                        // Put back
                                        Drop(i);
                                    }
                                }
                                else
                                {
                                    // Drop portrait
                                    battlePositions[i] = slotOfDraggedPartyMember.Value;
                                    if (battlePositions[dragSource!.Value] == slotOfDraggedPartyMember.Value)
                                        battlePositions.Remove(dragSource.Value);
                                    Drop(i);
                                }
                            }

                            return true;
                        }
                    }
                }
                else if (mouseButtons == MouseButtons.Right)
                {
                    if (dragSource != null)
                    {
                        Reset(position);
                        return true;
                    }
                }

                return false;
            };
            battlePositionDragHandler = position =>
            {
                Drag(position);
            };
        });
    }

    void ShowBattleWindow(Event? nextEvent, out byte paletteIndex, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        combatBackgroundIndex ??= is3D ? renderMap3D!.CombatBackgroundIndex : Map!.World switch
        {
            World.Lyramion => 0u,
            World.ForestMoon => 6u,
            World.Morag => 4u,
            _ => 0u
        };

        SetWindow(Window.Battle, nextEvent, x, y, combatBackgroundIndex);
        layout.SetLayout(LayoutType.Battle);
        ShowMap(false);
        layout.Reset();

        bool advancedBackgrounds = Features.HasFlag(Features.AdvancedCombatBackgrounds);
        var combatBackground = is3D
        ? renderView.GraphicInfoProvider.Get3DCombatBackground(combatBackgroundIndex.Value, advancedBackgrounds)
        : renderView.GraphicInfoProvider.Get2DCombatBackground(combatBackgroundIndex.Value, advancedBackgrounds);
        paletteIndex = (byte)(combatBackground.Palettes[GameTime!.CombatBackgroundPaletteIndex()] - 1);
        layout.AddSprite(Global.CombatBackgroundArea, combatBackground.GraphicIndex - 1,
            paletteIndex, 1, null, null, Layer.CombatBackground);
        layout.FillArea(new Rect(0, 132, 320, 68), Render.Color.Black, 0);
        layout.FillArea(new Rect(5, 139, 84, 56), GetUIColor(28), 1);

        if (currentBattle != null)
        {
            var monsterBattleAnimations = new Dictionary<int, BattleAnimation>(24);
            foreach (var monster in currentBattle.Monsters)
            {
                int slot = currentBattle.GetSlotFromCharacter(monster);
                monsterBattleAnimations.Add(slot, layout.AddMonsterCombatSprite(slot % 6, slot / 6, monster,
                    currentBattle.GetMonsterDisplayLayer(monster, slot), paletteIndex));
            }
            currentBattle.SetMonsterAnimations(monsterBattleAnimations);
        }

        // Add battle field sprites for party members
        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            var partyMember = GetPartyMember(i);

            if (partyMember == null || !partyMember.Alive || HasPartyMemberFled(partyMember))
            {
                partyMemberBattleFieldSprites[i] = null;
                partyMemberBattleFieldTooltips[i] = null;
            }
            else
            {
                var battlePosition = currentBattle == null ? 18 + CurrentSavegame!.BattlePositions[i] : currentBattle.GetSlotFromCharacter(partyMember);
                var battleColumn = battlePosition % 6;
                var battleRow = battlePosition / 6;

                partyMemberBattleFieldSprites[i] = layout.AddSprite(new Rect
                (
                    Global.BattleFieldX + battleColumn * Global.BattleFieldSlotWidth,
                    Global.BattleFieldY + battleRow * Global.BattleFieldSlotHeight - 1,
                    Global.BattleFieldSlotWidth,
                    Global.BattleFieldSlotHeight + 1
                ), Graphics.BattleFieldIconOffset + (uint)partyMember.Class, PrimaryUIPaletteIndex, (byte)(3 + battleRow),
                $"{partyMember.HitPoints.CurrentValue}/{partyMember.HitPoints.TotalMaxValue}^{partyMember.Name}",
                partyMember.Conditions.CanSelect() ? TextColor.White : TextColor.DeadPartyMember, null, out partyMemberBattleFieldTooltips[i]);
            }
        }

        UpdateBattleStatus();
        UpdateActiveBattleSpells();

        SetupBattleButtons();

        currentBattle?.InitImitatingPlayers();
    }

    internal void ReplacePartyMemberBattleFieldSprite(PartyMember partyMember, MonsterGraphicIndex graphicIndex)
    {
        int index = PartyMembers.ToList().IndexOf(partyMember);

        if (index != -1)
        {
            var textureIndex = Graphics.BattleFieldIconOffset + (uint)Class.Monster + (uint)graphicIndex - 1;
            partyMemberBattleFieldSprites[index]!.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)!.GetOffset(textureIndex);
        }
    }

    internal void SetupBattleButtons()
    {
        // Flee button
        layout.AttachEventToButton(0, () =>
        {
            SetCurrentPlayerBattleAction(Battle.BattleActionType.Flee);
        });
        // OK button
        layout.AttachEventToButton(2, () =>
        {
            StartBattleRound(false);
        });
        // Move button
        layout.AttachEventToButton(3, () =>
        {
            SetCurrentPlayerAction(PlayerBattleAction.PickMoveSpot);
        });
        // Move group forward button
        layout.AttachEventToButton(4, () =>
        {
            SetBattleMessageWithClick(DataNameProvider.BattleMessagePartyAdvances, TextColor.BrightGray, () =>
            {
                InputEnable = false;
                currentBattle!.WaitForClick = true;
                CursorType = CursorType.Click;
                allInputDisabled = true;
                AdvanceParty(() =>
                {
                    allInputDisabled = false;
                    InputEnable = true;
                    currentBattle.WaitForClick = false;
                    CursorType = CursorType.Sword;
                });
            });
        });
        // Attack button
        layout.AttachEventToButton(6, () =>
        {
            SetCurrentPlayerAction(PlayerBattleAction.PickAttackSpot);
        });
        // Parry button
        layout.AttachEventToButton(7, () =>
        {
            SetCurrentPlayerBattleAction(Battle.BattleActionType.Parry);
        });
        // Use magic button
        layout.AttachEventToButton(8, () =>
        {
            if (!CurrentPartyMember!.HasAnySpell())
            {
                ShowMessagePopup(DataNameProvider.YouDontKnowAnySpellsYet);
            }
            else
            {
                StartSequence();
                layout.HideTooltip();
                currentBattle!.HideAllBattleFieldDamage();
                OpenSpellList(CurrentPartyMember,
                    spell =>
                    {
                        var spellInfo = SpellInfos[spell];

                        if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle))
                            return DataNameProvider.WrongArea;

                        var worldFlag = (WorldFlag)(1 << (int)Map!.World);

                        if (!spellInfo.Worlds.HasFlag(worldFlag))
                            return DataNameProvider.WrongWorld;

                        if (SpellInfos.GetSPCost(Features, spell, CurrentPartyMember) > CurrentPartyMember.SpellPoints.CurrentValue)
                            return DataNameProvider.NotEnoughSP;

                        // TODO: Is there more to check? Irritated?

                        return null;
                    },
                    spell => PickBattleSpell(spell)
                );
                EndSequence();
            }
        });
        if (currentBattle != null)
            BattlePlayerSwitched();
    }

    internal void PickBattleSpell(Spell spell, uint? itemSlotIndex = null, bool? itemIsEquipped = null,
        PartyMember? caster = null)
    {
        ExecuteNextUpdateCycle(() =>
        {
            pickedSpell = spell;
            spellItemSlotIndex = itemSlotIndex;
            spellItemIsEquipped = itemIsEquipped;
            currentPickingActionMember = caster ?? CurrentPartyMember;
            SetPlayerBattleAction(Battle.BattleActionType.None);

            if (currentPickingActionMember == CurrentPartyMember)
            {
                highlightBattleFieldSprites.ForEach(s => s?.Delete());
                highlightBattleFieldSprites.Clear();
            }

            var spellInfo = SpellInfos[pickedSpell];

            switch (spellInfo.Target)
            {
                case SpellTarget.SingleEnemy:
                    SetCurrentPlayerAction(PlayerBattleAction.PickEnemySpellTarget);
                    break;
                case SpellTarget.SingleFriend:
                    SetCurrentPlayerAction(PlayerBattleAction.PickFriendSpellTarget);
                    break;
                case SpellTarget.EnemyRow:
                    SetCurrentPlayerAction(PlayerBattleAction.PickEnemySpellTargetRow);
                    break;
                case SpellTarget.BattleField:
                    if (spell == Spell.Blink)
                        SetCurrentPlayerAction(PlayerBattleAction.PickMemberToBlink);
                    else
                        throw new AmbermoonException(ExceptionScope.Data, "Only the Blink spell should have target type BattleField.");
                    break;
                default:
                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                        Battle.CreateCastSpellParameter(0, pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                    break;
            }
        });
    }

    void AdvanceParty(Action finishAction)
    {
        int advancedMonsters = 0;
        var monsters = currentBattle!.Monsters.ToList();
        int totalMonsters = monsters.Count;
        var newPositions = new Dictionary<int, uint>(totalMonsters);
        uint timePerMonster = Math.Max(1u, TicksPerSecond / (2u * (uint)totalMonsters));

        void MoveMonster(Monster monster, int index)
        {
            int position = currentBattle.GetSlotFromCharacter(monster);
            int currentColumn = position % 6;
            int currentRow = position / 6;
            int newRow = currentRow + 1;
            var animation = layout.GetMonsterBattleAnimation(monster);

            void MoveAnimationFinished()
            {
                animation.AnimationFinished -= MoveAnimationFinished;
                currentBattle.SetMonsterDisplayLayer(animation, monster, position);
                newPositions[index] = (uint)(position + 6);

                if (++advancedMonsters == totalMonsters)
                {
                    partyAdvances = false;

                    // Note: It is important to move closer rows first. Otherwise monsters
                    // will move to occupied spots and replace the monsters there before they move.
                    for (int i = monsters.Count - 1; i >= 0; --i)
                        currentBattle.MoveCharacterTo(newPositions[i], monsters[i]);

                    layout.EnableButton(4, currentBattle.CanPartyMoveForward);
                    finishAction?.Invoke();
                }
            }

            var newDisplayPosition = layout.GetMonsterCombatCenterPosition(currentColumn, newRow, monster);
            animation!.AnimationFinished += MoveAnimationFinished;
            animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Move).Take(1).ToArray(),
                timePerMonster, CurrentBattleTicks, newDisplayPosition,
                layout.RenderView.GraphicInfoProvider.GetMonsterRowImageScaleFactor((MonsterRow)newRow));
        }

        for (int i = 0; i < monsters.Count; ++i)
        {
            MoveMonster(monsters[i], i);
        }

        partyAdvances = true;
    }

    internal void UpdateActiveBattleSpells()
    {
        foreach (var activeSpell in EnumHelper.GetValues<ActiveSpellType>())
        {
            if (activeSpell.AvailableInBattle() && CurrentSavegame!.ActiveSpells[(int)activeSpell] != null)
                layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell], true);
        }
    }

    internal void HideActiveBattleSpells()
    {
        layout.RemoveAllActiveSpells();
    }

    public bool EndBattle(bool flee)
    {
        if (currentBattle == null || currentBattle.RoundActive)
            return false;

        if (PopupActive)
            ClosePopup();

        currentBattle.EndBattle(flee);
        return true;
    }

    /// <summary>
    /// Sets the speed of battles.
    /// 
    /// A value of 0 is the normal speed and will need a click to acknowledge battle actions.
    /// </summary>
    /// <param name="speed">Value from 0 to 100 where 0 is the normal speed.</param>
    internal void SetBattleSpeed(int speed)
    {
        if (currentBattle != null)
        {
            currentBattle.NeedsClickForNextAction = speed == 0;
            currentBattle.Speed = speed;
        }
    }

    void ShowBattleWindow(Event? nextEvent, bool failedFlight, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        allInputDisabled = true;
        Fade(() =>
        {
            lastPlayedSong = PlayMusic(Song.SapphireFireballsOfPureLove);
            roundPlayerBattleActions.Clear();
            ShowBattleWindow(nextEvent, out byte paletteIndex, x, y, combatBackgroundIndex);
            // Note: Create clones so we can change the values in battle for each monster.
            var monsterGroup = CloneMonsterGroup(CharacterManager.GetMonsterGroup(currentBattleInfo!.MonsterGroupIndex));

            foreach (var monster in monsterGroup.Monsters)
                InitializeMonster(this, monster);

            if (CharacterManager.MonsterGraphicAtlasProvider != null)
            {
                var atlas = CharacterManager.MonsterGraphicAtlasProvider(monsterGroup);
                TextureAtlasManager.Instance.SetAtlas(Layer.BattleMonsterRow, atlas);
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleMonsterRow)!;
                renderView.GetLayer(Layer.BattleMonsterRow).Texture = textureAtlas.Texture;
            }

            var monsterBattleAnimations = new Dictionary<int, BattleAnimation>(24);
            // Add animated monster combat graphics and battle field sprites
            for (int row = 0; row < 3; ++row)
            {
                for (int column = 0; column < 6; ++column)
                {
                    var monster = monsterGroup.Monsters[column, row];

                    if (monster != null)
                    {
                        monsterBattleAnimations.Add(column + row * 6,
                            layout.AddMonsterCombatSprite(column, row, monster, 0, paletteIndex));
                    }
                }
            }
            currentBattle = new Battle(this, layout, Enumerable.Range(0, MaxPartyMembers).Select(GetPartyMember).ToArray(),
                monsterGroup, monsterBattleAnimations, CoreConfiguration.BattleSpeed == 0);

            foreach (var monsterBattleAnimation in monsterBattleAnimations)
                currentBattle.SetMonsterDisplayLayer(monsterBattleAnimation.Value, (currentBattle!.GetCharacterAt(monsterBattleAnimation.Key) as Monster)!);

            currentBattle.RoundFinished += () =>
            {
                InputEnable = true;
                CursorType = CursorType.Sword;
                layout.ShowButtons(true);
                battleRoundActiveSprite.Visible = false;
                buttonGridBackground?.Destroy();
                buttonGridBackground = null;
                layout.EnableButton(4, currentBattle.CanPartyMoveForward);

                foreach (var action in roundPlayerBattleActions)
                    CheckPlayerActionVisuals(GetPartyMember(action.Key)!, action.Value);
                layout.SetBattleFieldSlotColor(currentBattle.GetSlotFromCharacter(CurrentPartyMember!), BattleFieldSlotColor.Yellow);
                layout.SetBattleMessage(null);
                if (RecheckActivePartyMember(out bool gameOver))
                {
                    if (gameOver)
                        return;
                    BattlePlayerSwitched();
                }
                else
                    AddCurrentPlayerActionVisuals();
                UpdateBattleStatus();
                if (currentBattle != null)
                {
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        if (partyMemberBattleFieldTooltips[i] != null)
                        {
                            var partyMember = GetPartyMember(i)!;
                            int position = currentBattle.GetSlotFromCharacter(partyMember);
                            partyMemberBattleFieldTooltips[i]!.Area = new Rect
                            (
                                Global.BattleFieldX + (position % 6) * Global.BattleFieldSlotWidth,
                                Global.BattleFieldY + (position / 6) * Global.BattleFieldSlotHeight - 1,
                                Global.BattleFieldSlotWidth,
                                Global.BattleFieldSlotHeight + 1
                            );
                            partyMemberBattleFieldTooltips[i]!.Text =
                                $"{partyMember.HitPoints.CurrentValue}/{partyMember.HitPoints.TotalMaxValue}^{partyMember.Name}";
                        }
                    }
                    UpdateActiveBattleSpells();
                }
            };
            currentBattle.CharacterDied += character =>
            {
                if (character is PartyMember partyMember)
                {
                    int slot = SlotFromPartyMember(partyMember)!.Value;
                    layout.SetCharacter(slot, partyMember);
                    layout.UpdateCharacterStatus(slot, null);
                    roundPlayerBattleActions.Remove(slot);
                }
            };
            currentBattle.BattleEnded += battleEndInfo =>
            {
                battleRoundActiveSprite.Visible = false;
                for (int i = 0; i < MaxPartyMembers; ++i)
                {
                    if (GetPartyMember(i) != null)
                        layout.UpdateCharacterStatus(i, null);
                }
                void EndBattle()
                {
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        var partyMember = GetPartyMember(i);

                        if (partyMember != null)
                            partyMember.Conditions = partyMember.Conditions.WithoutBattleOnlyConditions();
                    }
                    roundPlayerBattleActions.Clear();
                    UpdateBattleStatus();
                    if (lastPlayedSong != null)
                    {
                        var temp = lastPlayedSong; // preserve as window close will play the map song otherwise
                        PlayMusic(lastPlayedSong.Value);
                        lastPlayedSong = temp;
                    }
                    else if (Map!.UseTravelMusic)
                        PlayMusic(travelType.TravelSong());
                    else
                        PlayMusic(Song.Default);
                    currentBattleInfo.EndBattle(battleEndInfo);
                    currentBattleInfo = null;
                }
                if (battleEndInfo.MonstersDefeated)
                {
                    currentBattle = null;
                    EndBattle();
                    ShowBattleLoot(battleEndInfo, () =>
                    {
                        if (nextEvent != null)
                        {
                            EventExtensions.TriggerEventChain(Map!, this, EventTrigger.Always,
                                x, y, nextEvent, true);
                        }
                    });
                }
                else if (PartyMembers.Any(p => p.Alive && p.Conditions.CanFight()))
                {
                    // There are fled survivors
                    currentBattle = null;
                    EndBattle();
                    CloseWindow(() =>
                    {
                        this.NewLeaderPicked += NewLeaderPicked;
                        allInputDisabled = false;
                        RecheckActivePartyMember(out bool _);

                        void Finish()
                        {
                            this.NewLeaderPicked -= NewLeaderPicked;
                            InputEnable = true;
                            allInputDisabled = false;
                            if (nextEvent != null)
                            {
                                EventExtensions.TriggerEventChain(Map!, this, EventTrigger.Always, x, y, nextEvent, false);
                            }
                        }

                        if (!pickingNewLeader)
                        {
                            Finish();
                        }

                        void NewLeaderPicked(int _)
                        {
                            Finish();
                        }
                    });
                }
                else
                {
                    currentBattleInfo = null;
                    currentBattle = null;
                    CloseWindow(() =>
                    {
                        InputEnable = true;
                        Hook_GameOver();
                    });
                }
            };
            currentBattle.ActionCompleted += battleAction =>
            {
                CursorType = CursorType.Click;

                if (battleAction.Character is PartyMember partyMember &&
                    (battleAction.Action == Battle.BattleActionType.Move ||
                    battleAction.Action == Battle.BattleActionType.Flee ||
                    battleAction.Action == Battle.BattleActionType.CastSpell))
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember)!.Value, null);
            };
            currentBattle.PlayerWeaponBroke += partyMember =>
            {
                // Note: no need to check action here as it only can break while attacking
                var slot = SlotFromPartyMember(partyMember)!.Value;
                roundPlayerBattleActions.Remove(slot);
                layout.UpdateCharacterStatus(slot, null);
            };
            currentBattle.PlayerLastAmmoUsed += partyMember =>
            {
                // Note: no need to check action here as it only can happen while attacking
                var slot = SlotFromPartyMember(partyMember)!.Value;
                roundPlayerBattleActions.Remove(slot);
                layout.UpdateCharacterStatus(slot, null);
            };
            currentBattle.PlayerLostTarget += partyMember =>
            {
                var slot = SlotFromPartyMember(partyMember)!.Value;
                roundPlayerBattleActions.Remove(slot);
                layout.UpdateCharacterStatus(slot, null);
            };
            BattlePlayerSwitched();

            if (failedFlight)
            {
                void ShowFailedFlightMessage()
                {
                    currentBattle.StartAnimationFinished -= ShowFailedFlightMessage;
                    SetBattleMessageWithClick(DataNameProvider.AttackEscapeFailedMessage, TextColor.BrightGray, () => StartBattleRound(true));
                }

                if (currentBattle.HasStartAnimation)
                    currentBattle.StartAnimationFinished += ShowFailedFlightMessage;
                else
                    ShowFailedFlightMessage();
            }
        }, false);
    }

    void StartBattleRound(bool withoutPlayerActions)
    {
        HideActiveBattleSpells();
        InputEnable = false;
        CursorType = CursorType.Click;
        layout.ResetMonsterCombatSprites();
        layout.ClearBattleFieldSlotColors();
        layout.ShowButtons(false);
        buttonGridBackground = layout.FillArea(new Rect(Global.ButtonGridX, Global.ButtonGridY, 3 * Button.Width, 3 * Button.Height),
            GetUIColor(28), 1);
        battleRoundActiveSprite.Visible = true;
        currentBattle!.StartRound
        (
            withoutPlayerActions ? Enumerable.Repeat(new Battle.PlayerBattleAction(), 6).ToArray() :
                Enumerable.Range(0, MaxPartyMembers)
                .Select(i => roundPlayerBattleActions.ContainsKey(i) ? roundPlayerBattleActions[i] : new Battle.PlayerBattleAction())
                .ToArray(), CurrentBattleTicks
        );
    }

    void CancelSpecificPlayerAction()
    {
        SetCurrentPlayerAction(PlayerBattleAction.PickPlayerAction);
        UntrapMouse();
        AddCurrentPlayerActionVisuals();
        layout.SetBattleMessage(null);
    }

    bool CheckBattleRightClick()
    {
        if (currentPlayerBattleAction == PlayerBattleAction.PickPlayerAction)
            return false; // This is handled by layout/game interaction.

        CancelSpecificPlayerAction();
        return true;
    }

    // Note: In original the max hitpoints are often much higher
    // than the current hitpoints. It seems like the max hitpoints
    // are often a multiple of 99 like 99, 198, 297, etc.
    static void InitializeMonster(GameCore game, Monster monster)
    {
        if (monster == null)
            return;

        static void AdjustMonsterValue(GameCore game, CharacterValue characterValue)
        {
            characterValue.CurrentValue = (uint)Math.Min(100, game.RandomInt(95, 104)) * characterValue.TotalMaxValue / 100u;
        }

        static void FixValue(GameCore game, CharacterValue characterValue)
        {
            characterValue.MaxValue = characterValue.CurrentValue;
            AdjustMonsterValue(game, characterValue);
        }

        // Attributes, skills, LP and SP is special for monsters.
        foreach (var attribute in EnumHelper.GetValues<Attribute>().Take(8))
            FixValue(game, monster.Attributes[attribute]);
        foreach (var skill in EnumHelper.GetValues<Skill>())
            FixValue(game, monster.Skills[skill]);

        monster.HitPoints.MaxValue = monster.HitPoints.CurrentValue;
        monster.SpellPoints.MaxValue = monster.SpellPoints.CurrentValue;

        AdjustMonsterValue(game, monster.HitPoints);
        AdjustMonsterValue(game, monster.SpellPoints);
    }

    internal void MoveBattleActorTo(uint column, uint row, Character character)
    {
        if (character is Monster monster)
            layout.MoveMonsterTo(column, row, monster);
        else if (character is PartyMember partyMember)
        {
            int index = SlotFromPartyMember(partyMember)!.Value;
            var sprite = partyMemberBattleFieldSprites[index]!;
            sprite.X = Global.BattleFieldX + (int)column * Global.BattleFieldSlotWidth;
            sprite.Y = Global.BattleFieldY + (int)row * Global.BattleFieldSlotHeight - 1;
            sprite.DisplayLayer = (byte)(3 + row);
        }
    }

    internal void RemoveBattleActor(Character character)
    {
        if (character is Monster monster)
        {
            layout.RemoveMonsterCombatSprite(monster);
        }
        else if (character is PartyMember partyMember)
        {
            int slot = SlotFromPartyMember(partyMember)!.Value;
            roundPlayerBattleActions.Remove(slot);
            partyMemberBattleFieldSprites[slot]?.Delete();
            partyMemberBattleFieldSprites[slot] = null;

            if (partyMemberBattleFieldTooltips[slot] != null)
            {
                layout.RemoveTooltip(partyMemberBattleFieldTooltips[slot]!);
                partyMemberBattleFieldTooltips[slot] = null;
            }
        }
    }

    void BattlePlayerSwitched()
    {
        int partyMemberSlot = SlotFromPartyMember(CurrentPartyMember!)!.Value;
        layout.ClearBattleFieldSlotColors();
        int battleFieldSlot = currentBattle!.GetSlotFromCharacter(CurrentPartyMember!);
        layout.SetBattleFieldSlotColor(battleFieldSlot, BattleFieldSlotColor.Yellow);
        AddCurrentPlayerActionVisuals();

        if (roundPlayerBattleActions.ContainsKey(partyMemberSlot))
        {
            var action = roundPlayerBattleActions[partyMemberSlot];
            layout.UpdateCharacterStatus(partyMemberSlot, action.BattleAction.ToStatusGraphic(action.Parameter, ItemManager));
        }
        else
        {
            layout.UpdateCharacterStatus(partyMemberSlot, CurrentPartyMember!.Conditions.CanSelect() ? null : GetDisabledStatusGraphic(CurrentPartyMember));
        }

        layout.EnableButton(0, battleFieldSlot >= 24 && CurrentPartyMember!.CanFlee()); // flee button, only enable in last row
        layout.EnableButton(3, CurrentPartyMember!.CanMove()); // Note: If no slot is available the button still is enabled but after clicking you get "You can't move anywhere".
        layout.EnableButton(4, currentBattle.CanPartyMoveForward);
        layout.EnableButton(6, CurrentPartyMember.BaseAttackDamage + CurrentPartyMember.BonusAttackDamage > 0 && CurrentPartyMember.Conditions.CanAttack());
        layout.EnableButton(7, CurrentPartyMember.Conditions.CanParry());
        layout.EnableButton(8, CurrentPartyMember.Conditions.CanCastSpell(Features) && CurrentPartyMember.HasAnySpell());
    }

    /// <summary>
    /// This adds the target slots' coloring.
    /// </summary>
    void AddCurrentPlayerActionVisuals()
    {
        int slot = SlotFromPartyMember(CurrentPartyMember!)!.Value;

        if (roundPlayerBattleActions.TryGetValue(slot, out Battle.PlayerBattleAction? action))
        {
            switch (action.BattleAction)
            {
                case Battle.BattleActionType.Attack:
                case Battle.BattleActionType.Move:
                    layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange);
                    break;
                case Battle.BattleActionType.CastSpell:
                    var spell = Battle.GetCastSpell(action.Parameter);
                    switch (SpellInfos[spell].Target)
                    {
                        case SpellTarget.SingleEnemy:
                        case SpellTarget.SingleFriend:
                            layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange);
                            break;
                        case SpellTarget.FriendRow:
                        {
                            SetBattleRowSlotColors((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter),
                                (c, r) => currentBattle!.GetCharacterAt(c, r)?.Type == CharacterType.PartyMember,
                                BattleFieldSlotColor.Orange);
                            break;
                        }
                        case SpellTarget.EnemyRow:
                        {
                            SetBattleRowSlotColors((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter),
                                (c, r) => currentBattle!.GetCharacterAt(c, r)?.Type == CharacterType.Monster,
                                BattleFieldSlotColor.Orange);
                            break;
                        }
                        case SpellTarget.AllEnemies:
                            for (int i = 0; i < 24; ++i)
                                layout.SetBattleFieldSlotColor(i, BattleFieldSlotColor.Orange);
                            break;
                        case SpellTarget.AllFriends:
                            for (int i = 0; i < 12; ++i)
                                layout.SetBattleFieldSlotColor(18 + i, BattleFieldSlotColor.Orange);
                            break;
                        case SpellTarget.BattleField:
                        {
                            int blinkCharacterSlot = (int)Battle.GetBlinkCharacterPosition(action.Parameter);
                            bool selfBlink = currentBattle!.GetSlotFromCharacter(CurrentPartyMember!) == blinkCharacterSlot;
                            layout.SetBattleFieldSlotColor(blinkCharacterSlot, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks);
                            layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks + Layout.TicksPerBlink);
                            break;
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// This removes the target slots' coloring.
    /// </summary>
    void RemoveCurrentPlayerActionVisuals()
    {
        var action = GetOrCreateBattleAction();

        switch (action.BattleAction)
        {
            case Battle.BattleActionType.Attack:
            case Battle.BattleActionType.Move:
                layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.None);
                break;
            case Battle.BattleActionType.CastSpell:
                layout.ClearBattleFieldSlotColorsExcept(currentBattle!.GetSlotFromCharacter(CurrentPartyMember!));
                if (currentBattle.IsSelfSpell(CurrentPartyMember!, action.Parameter))
                    layout.SetBattleFieldSlotColor(currentBattle.GetSlotFromCharacter(CurrentPartyMember!), BattleFieldSlotColor.Yellow);
                break;
        }
    }

    /// <summary>
    /// Checks if a player action should be still active after
    /// a battle round.
    /// </summary>
    /// <param name="action"></param>
    void CheckPlayerActionVisuals(PartyMember partyMember, Battle.PlayerBattleAction action)
    {
        bool remove = !partyMember.Conditions.CanSelect();

        if (!remove)
        {
            switch (action.BattleAction)
            {
                case Battle.BattleActionType.Move:
                case Battle.BattleActionType.Flee:
                case Battle.BattleActionType.CastSpell:
                    remove = true;
                    break;
                case Battle.BattleActionType.Attack:
                    if (partyMember.BaseAttackDamage + partyMember.BonusAttackDamage <= 0 || !partyMember.Conditions.CanAttack())
                        remove = true;
                    break;
                case Battle.BattleActionType.Parry:
                    if (!partyMember.Conditions.CanParry())
                        remove = true;
                    break;
                default:
                    remove = true;
                    break;
            }
        }

        if (remove) // Note: Don't use 'else' here as remove could be set inside the if-block above as well.
            roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember)!.Value);
    }

    void SetCurrentPlayerBattleAction(Battle.BattleActionType actionType, uint parameter = 0)
    {
        RemoveCurrentPlayerActionVisuals();
        var action = GetOrCreateBattleAction();
        action.BattleAction = actionType;
        action.Parameter = parameter;
        AddCurrentPlayerActionVisuals();

        int slot = SlotFromPartyMember(CurrentPartyMember!)!.Value;
        layout.UpdateCharacterStatus(slot, actionType.ToStatusGraphic(parameter, ItemManager));
    }

    void SetPlayerBattleAction(Battle.BattleActionType actionType, uint parameter = 0)
    {
        if (currentPickingActionMember == CurrentPartyMember)
            SetCurrentPlayerBattleAction(actionType, parameter);
        else
        {
            var action = GetOrCreateBattleAction();
            action.BattleAction = actionType;
            action.Parameter = parameter;
            int slot = SlotFromPartyMember(currentPickingActionMember!)!.Value;
            layout.UpdateCharacterStatus(slot, actionType.ToStatusGraphic(parameter, ItemManager));
        }
    }

    Battle.PlayerBattleAction GetOrCreateBattleAction()
    {
        int slot = SlotFromPartyMember(currentPickingActionMember!)!.Value;

        if (!roundPlayerBattleActions.ContainsKey(slot))
            roundPlayerBattleActions.Add(slot, new Battle.PlayerBattleAction());

        return roundPlayerBattleActions[slot];
    }

    internal void SetBattleMessageWithClick(string message, TextColor textColor = TextColor.BattlePlayer,
        Action? followAction = null, TimeSpan? delay = null)
    {
        layout.HideTooltip();
        layout.SetBattleMessage(message, textColor);

        if (delay == null)
            Setup();
        else
            AddTimedEvent(delay.Value, Setup);

        void Setup()
        {
            InputEnable = false;
            currentBattle!.WaitForClick = true;
            CursorType = CursorType.Click;

            if (followAction != null)
            {
                bool Follow(MouseButtons _)
                {
                    layout.SetBattleMessage(null);
                    InputEnable = true;
                    currentBattle.WaitForClick = false;
                    CursorType = CursorType.Sword;
                    followAction?.Invoke();
                    return true;
                }

                nextClickHandler = Follow;
            }
        }
    }

    bool AnyPlayerMovesTo(int slot)
    {
        var actions = roundPlayerBattleActions.Where(p => p.Key != SlotFromPartyMember(currentPickingActionMember!));
        bool anyMovesTo = actions.Any(p => p.Value.BattleAction == Battle.BattleActionType.Move &&
            Battle.GetTargetTileOrRowFromParameter(p.Value.Parameter) == slot);

        if (anyMovesTo)
            return true;

        // Anyone blinks to? This is different to original where this isn't checked but I guess it's better this way.
        return actions.Any(p =>
        {
            if (p.Value.BattleAction == Battle.BattleActionType.CastSpell &&
                Battle.GetCastSpell(p.Value.Parameter) == Spell.Blink)
            {
                if (Battle.GetTargetTileOrRowFromParameter(p.Value.Parameter) == slot)
                    return true;
            }

            return false;
        });
    }

    void BattleFieldSlotClicked(int column, int row, MouseButtons mouseButtons)
    {
        if (currentBattle!.SkipNextBattleFieldClick)
            return;

        if (currentBattle.RoundActive)
            return;

        if (row < 0 || row > 4 ||
            column < 0 || column > 5)
            return;

        if (mouseButtons == MouseButtons.Right)
        {
            var character = currentBattle.GetCharacterAt(column, row);

            if (character is PartyMember partyMember)
            {
                OpenPartyMember(SlotFromPartyMember(partyMember)!.Value, true);
            }

            return;
        }
        else if (mouseButtons != MouseButtons.Left)
            return;

        switch (currentPlayerBattleAction)
        {
            case PlayerBattleAction.PickPlayerAction:
            {
                var character = currentBattle.GetCharacterAt(column, row);

                if (character?.Type == CharacterType.PartyMember)
                {
                    var partyMember = character as PartyMember;

                    if (currentPickingActionMember != partyMember && partyMember!.Conditions.CanSelect())
                    {
                        int partyMemberSlot = SlotFromPartyMember(partyMember)!.Value;
                        SetActivePartyMember(partyMemberSlot, false);
                        BattlePlayerSwitched();
                    }
                }
                else if (character?.Type == CharacterType.Monster)
                {
                    if (!CheckAbilityToAttack(out bool ranged))
                        return;

                    if (!ranged)
                    {
                        int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember!);
                        if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.BrightGray);
                            return;
                        }
                    }

                    SetPlayerBattleAction(Battle.BattleActionType.Attack,
                        Battle.CreateAttackParameter((uint)(column + row * 6), currentPickingActionMember!, ItemManager));
                }
                else // empty field
                {
                    if (row < 3)
                        return;
                    int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember!);
                    uint maxDist = 1 + currentPickingActionMember!.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                    if (Math.Abs(column - position % 6) > maxDist || Math.Abs(row - position / 6) > maxDist)
                    {
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.BrightGray);
                        return;
                    }
                    if (!currentPickingActionMember.CanMove())
                    {
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotMove, TextColor.BrightGray);
                        return;
                    }
                    int newPosition = column + row * 6;
                    int slot = SlotFromPartyMember(currentPickingActionMember)!.Value;
                    if ((!roundPlayerBattleActions.ContainsKey(slot) ||
                        roundPlayerBattleActions[slot].BattleAction != Battle.BattleActionType.Move ||
                        Battle.GetTargetTileOrRowFromParameter(roundPlayerBattleActions[slot].Parameter) != newPosition) &&
                        AnyPlayerMovesTo(newPosition))
                    {
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageSomeoneAlreadyGoingThere, TextColor.BrightGray);
                        return;
                    }
                    SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)(column + row * 6)));
                }
                break;
            }
            case PlayerBattleAction.PickMemberToBlink:
            {
                var target = currentBattle.GetCharacterAt(column, row);
                if (target != null && target.Type == CharacterType.PartyMember)
                {
                    if (!target.Conditions.CanBlink())
                    {
                        CancelSpecificPlayerAction();
                        SetBattleMessageWithClick(target.Name + DataNameProvider.BattleMessageCannotBlink, TextColor.BrightGray);
                        return;
                    }

                    blinkCharacterPosition = (uint)(column + row * 6);
                    SetCurrentPlayerAction(PlayerBattleAction.PickBlinkTarget);
                }
                break;
            }
            case PlayerBattleAction.PickBlinkTarget:
            {
                // Note: If someone moves to the target spot, it can't be selected (red cross).
                // But someone can move to a spot where someone blinks to in Ambermoon.
                // Here we disallow moving to a spot where someone blinks to by considering
                // blink targets in AnyPlayerMovesTo. This will also disallow 2 characters to
                // blink to the same spot.
                int position = column + row * 6;
                if (row > 2 && currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
                {
                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)(column + row * 6),
                        pickedSpell, spellItemSlotIndex, spellItemIsEquipped, blinkCharacterPosition!.Value));
                    if (currentPickingActionMember == CurrentPartyMember)
                    {
                        int casterSlot = currentBattle.GetSlotFromCharacter(currentPickingActionMember!);
                        bool selfBlink = casterSlot == blinkCharacterPosition.Value;
                        layout.SetBattleFieldSlotColor((int)blinkCharacterPosition.Value, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks);
                        layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks + Layout.TicksPerBlink);
                        if (!selfBlink)
                            layout.SetBattleFieldSlotColor(casterSlot, BattleFieldSlotColor.Yellow);
                    }
                    CancelSpecificPlayerAction();
                }
                break;
            }
            case PlayerBattleAction.PickEnemySpellTarget:
            case PlayerBattleAction.PickFriendSpellTarget:
            {
                var target = currentBattle.GetCharacterAt(column, row);
                if (target != null)
                {
                    if (currentPlayerBattleAction == PlayerBattleAction.PickEnemySpellTarget)
                    {
                        if (target.Type != CharacterType.Monster)
                            return;
                    }
                    else
                    {
                        if (target.Type != CharacterType.PartyMember)
                            return;
                    }

                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)(column + row * 6),
                        pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                    if (currentPickingActionMember == CurrentPartyMember)
                        layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange);
                    CancelSpecificPlayerAction();
                }
                break;
            }
            case PlayerBattleAction.PickEnemySpellTargetRow:
            {
                if (row > 3)
                {
                    return;
                }
                SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)row,
                    pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                if (currentPickingActionMember == CurrentPartyMember)
                {
                    layout.ClearBattleFieldSlotColorsExcept(currentBattle.GetSlotFromCharacter(currentPickingActionMember!));
                    SetBattleRowSlotColors(row, (c, r) => currentBattle.GetCharacterAt(c, r)?.Type != CharacterType.PartyMember, BattleFieldSlotColor.Orange);
                }
                CancelSpecificPlayerAction();
                break;
            }
            case PlayerBattleAction.PickMoveSpot:
            {
                int position = column + row * 6;
                uint maxDist = 1 + currentPickingActionMember!.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                int currentPosition = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                int currentColumn = currentPosition % 6;
                int currentRow = currentPosition / 6;
                if (row > 2 && Math.Abs(column - currentColumn) <= maxDist &&
                    Math.Abs(row - currentRow) <= maxDist &&
                    currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
                {
                    SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)position));
                    CancelSpecificPlayerAction();
                }
                break;
            }
            case PlayerBattleAction.PickAttackSpot:
            {
                if (!CheckAbilityToAttack(out bool ranged))
                    return;

                if (!ranged)
                {
                    int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember!);
                    if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                        return;
                }

                if (currentBattle.GetCharacterAt(column + row * 6)?.Type == CharacterType.Monster)
                {
                    SetPlayerBattleAction(Battle.BattleActionType.Attack,
                        Battle.CreateAttackParameter((uint)(column + row * 6), currentPickingActionMember!, ItemManager));
                    CancelSpecificPlayerAction();
                }
                break;
            }
        }
    }

    void SetBattleRowSlotColors(int row, Func<int, int, bool> condition, BattleFieldSlotColor color)
    {
        for (int column = 0; column < 6; ++column)
        {
            if (condition(column, row))
                layout.SetBattleFieldSlotColor(column, row, color);
        }
    }

    IEnumerable<int> GetValuableBattleFieldSlots(Func<int, bool> condition, int range, int minRow, int maxRow)
    {
        int slot = currentBattle!.GetSlotFromCharacter(currentPickingActionMember!);
        int currentColumn = slot % 6;
        int currentRow = slot / 6;
        for (int row = Math.Max(minRow, currentRow - range); row <= Math.Min(maxRow, currentRow + range); ++row)
        {
            for (int column = Math.Max(0, currentColumn - range); column <= Math.Min(5, currentColumn + range); ++column)
            {
                int index = column + row * 6;

                if (condition(index))
                    yield return index;
            }
        }
    }

    bool CheckAbilityToAttack(out bool ranged, bool silent = false)
    {
        ranged = currentPickingActionMember!.HasLongRangedAttack(ItemManager, out bool hasAmmo);

        if (ranged && !hasAmmo)
        {
            // No ammo for ranged weapon
            CancelSpecificPlayerAction();
            if (!silent)
                SetBattleMessageWithClick(DataNameProvider.BattleMessageNoAmmunition, TextColor.BrightGray);
            return false;
        }

        if (currentPickingActionMember!.BaseAttackDamage + currentPickingActionMember.BonusAttackDamage <= 0 || !currentPickingActionMember.Conditions.CanAttack())
        {
            CancelSpecificPlayerAction();
            if (!silent)
                SetBattleMessageWithClick(DataNameProvider.BattleMessageUnableToAttack, TextColor.BrightGray);
            return false;
        }

        return true;
    }

    void SetCurrentPlayerAction(PlayerBattleAction playerBattleAction)
    {
        currentPlayerBattleAction = playerBattleAction;
        highlightBattleFieldSprites.ForEach(s => s?.Delete());
        highlightBattleFieldSprites.Clear();
        blinkingHighlight = false;

        switch (currentPlayerBattleAction)
        {
            case PlayerBattleAction.PickPlayerAction:
                currentPickingActionMember = CurrentPartyMember;
                break;
            case PlayerBattleAction.PickEnemySpellTarget:
            {
                var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle!.GetCharacterAt(position)?.Type == CharacterType.Monster,
                    6, 0, 3);
                foreach (var slot in valuableSlots)
                {
                    highlightBattleFieldSprites.Add
                    (
                        layout.AddSprite
                        (
                            Global.BattleFieldSlotArea(slot),
                            Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                            UIPaletteIndex
                        )
                    );
                }
                RemoveCurrentPlayerActionVisuals();
                TrapMouse(Global.BattleFieldArea);
                blinkingHighlight = true;
                layout.SetBattleMessage(DataNameProvider.BattleMessageWhichMonsterAsTarget);
                break;
            }
            case PlayerBattleAction.PickEnemySpellTargetRow:
            {
                RemoveCurrentPlayerActionVisuals();
                TrapMouse(Global.BattleFieldArea);
                blinkingHighlight = false;
                layout.SetBattleMessage(DataNameProvider.BattleMessageWhichMonsterRowAsTarget);
                break;
            }
            case PlayerBattleAction.PickFriendSpellTarget:
            case PlayerBattleAction.PickMemberToBlink:
            {
                var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle!.GetCharacterAt(position)?.Type == CharacterType.PartyMember,
                    6, 3, 4);
                foreach (var slot in valuableSlots)
                {
                    highlightBattleFieldSprites.Add
                    (
                        layout.AddSprite
                        (
                            Global.BattleFieldSlotArea(slot),
                            Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                            UIPaletteIndex
                        )
                    );
                }
                RemoveCurrentPlayerActionVisuals();
                TrapMouse(Global.BattleFieldArea);
                blinkingHighlight = true;
                layout.SetBattleMessage(playerBattleAction == PlayerBattleAction.PickMemberToBlink
                    ? DataNameProvider.BattleMessageWhoToBlink
                    : DataNameProvider.BattleMessageWhichPartyMemberAsTarget);
                break;
            }
            case PlayerBattleAction.PickBlinkTarget:
            {
                var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle!.IsBattleFieldEmpty(position),
                    6, 3, 4);
                foreach (var slot in valuableSlots)
                {
                    highlightBattleFieldSprites.Add
                    (
                        layout.AddSprite
                        (
                            Global.BattleFieldSlotArea(slot),
                            Graphics.GetCustomUIGraphicIndex
                            (
                                AnyPlayerMovesTo(slot) ? UICustomGraphic.BattleFieldBlockedMovementCursor : UICustomGraphic.BattleFieldGreenHighlight
                            ), UIPaletteIndex
                        )
                    );
                }
                blinkingHighlight = true;
                layout.SetBattleMessage(DataNameProvider.BattleMessageWhereToBlinkTo);
                break;
            }
            case PlayerBattleAction.PickMoveSpot:
            {
                int maxDist = 1 + (int)currentPickingActionMember!.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle!.IsBattleFieldEmpty(position),
                    maxDist, 3, 4);
                foreach (var slot in valuableSlots)
                {
                    highlightBattleFieldSprites.Add
                    (
                        layout.AddSprite
                        (
                            Global.BattleFieldSlotArea(slot),
                            Graphics.GetCustomUIGraphicIndex
                            (
                                AnyPlayerMovesTo(slot) ? UICustomGraphic.BattleFieldBlockedMovementCursor : UICustomGraphic.BattleFieldGreenHighlight
                            ), UIPaletteIndex
                        )
                    );
                }
                if (highlightBattleFieldSprites.Count == 0)
                {
                    // No movement possible
                    CancelSpecificPlayerAction();
                    SetBattleMessageWithClick(DataNameProvider.BattleMessageNowhereToMoveTo, TextColor.BrightGray);
                }
                else
                {
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhereToMoveTo);
                }
                break;
            }
            case PlayerBattleAction.PickAttackSpot:
            {
                if (!CheckAbilityToAttack(out bool ranged))
                    return;

                var valuableSlots = GetValuableBattleFieldSlots(index => currentBattle!.GetCharacterAt(index)?.Type == CharacterType.Monster,
                    ranged ? 6 : 1, 0, 3);
                foreach (var slot in valuableSlots)
                {
                    highlightBattleFieldSprites.Add
                    (
                        layout.AddSprite
                        (
                            Global.BattleFieldSlotArea(slot),
                            Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                            UIPaletteIndex
                        )
                    );
                }
                if (highlightBattleFieldSprites.Count == 0)
                {
                    // No attack possible
                    CancelSpecificPlayerAction();
                    SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotReachAnyone, TextColor.BrightGray);
                }
                else
                {
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhatToAttack);
                }
                break;
            }
        }
    }

    public bool HasPartyMemberFled(PartyMember partyMember)
    {
        return currentBattle?.HasPartyMemberFled(partyMember) ?? false;
    }

    void RecheckUsedBattleItem(int partyMemberSlot, int slotIndex, bool equipped)
    {
        if (currentBattle != null && roundPlayerBattleActions.ContainsKey(partyMemberSlot))
        {
            var action = roundPlayerBattleActions[partyMemberSlot];

            if (action.BattleAction == Battle.BattleActionType.CastSpell &&
                Battle.IsCastFromItem(action.Parameter))
            {
                if (Battle.GetCastItemSlot(action.Parameter) == slotIndex)
                {
                    roundPlayerBattleActions.Remove(partyMemberSlot);
                    UpdateBattleStatus(partyMemberSlot);
                }
            }
        }
    }

    void RecheckBattleEquipment(int partyMemberSlot, EquipmentSlot equipmentSlot, Item? removedItem)
    {
        if (currentBattle != null)
        {
            if (removedItem != null && roundPlayerBattleActions.ContainsKey(partyMemberSlot))
            {
                var action = roundPlayerBattleActions[partyMemberSlot];

                if (action.BattleAction == Battle.BattleActionType.Attack)
                {
                    bool removedWeapon = equipmentSlot == EquipmentSlot.RightHand ||
                        (equipmentSlot == EquipmentSlot.LeftHand && removedItem.Type == ItemType.Ammunition &&
                        CurrentInventory!.Equipment.Slots[EquipmentSlot.RightHand]?.ItemIndex != null &&
                        ItemManager.GetItem(CurrentInventory.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex).UsedAmmunitionType == removedItem.AmmunitionType);

                    if (removedWeapon || !CheckAbilityToAttack(out _, true))
                    {
                        roundPlayerBattleActions.Remove(partyMemberSlot);
                    }
                }
            }
        }
    }

    internal class BattleEndInfo
    {
        /// <summary>
        /// If true all monsters were defeated or did flee.
        /// If false all party members fled.
        /// If all party members died the game is just over
        /// and this event is not used anymore.
        /// </summary>
        public bool MonstersDefeated;
        /// <summary>
        /// If all monsters were defeated this list contains
        /// the monsters who died.
        /// </summary>
        public List<Monster> KilledMonsters = [];
        /// <summary>
        /// Total experience for the party.
        /// </summary>
        public int TotalExperience;
        /// <summary>
        /// Partymembers who fled.
        /// </summary>
        public List<PartyMember> FledPartyMembers = [];
        /// <summary>
        /// List of broken items.
        /// </summary>
        public List<KeyValuePair<uint, ItemSlotFlags>> BrokenItems = [];
    }

    class BattleInfo
    {
        public uint MonsterGroupIndex;
        public event Action<BattleEndInfo>? BattleEnded;

        internal void EndBattle(BattleEndInfo battleEndInfo) => BattleEnded?.Invoke(battleEndInfo);
    }
}
