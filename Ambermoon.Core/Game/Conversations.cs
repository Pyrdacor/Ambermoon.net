using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using static Ambermoon.Data.ConversationEvent;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    internal bool ConversationTextActive { get; private set; } = false;

    /// <summary>
    /// A conversation is started with a Conversation event but the
    /// displayed text depends on the following events. Mostly
    /// Condition and PrintText events. The argument conversationEvent
    /// is the initial conversation event of interaction type 'Talk'
    /// and should be used to determine the text to print etc.
    /// 
    /// The event chain may also contain rewards, new keywords, etc.
    /// </summary>
    internal void ShowConversation(IConversationPartner conversationPartner, uint? characterIndex,
        Event? conversationEvent, ConversationItems createdItems, bool showInitialText = true)
    {
        if (!(conversationPartner is Character character))
            throw new AmbermoonException(ExceptionScope.Application, "Conversation partner is no character.");

        if ((character.SpokenLanguages & CurrentPartyMember!.SpokenLanguages) == 0 &&
            (character.SpokenExtendedLanguages & CurrentPartyMember.SpokenExtendedLanguages) == 0)
        {
            ShowMessagePopup(DataNameProvider.YouDontSpeakSameLanguage);
            return;
        }

        IEnumerable<ConversationEvent> GetMatchingEvents(Func<ConversationEvent, bool> filter)
            => conversationPartner.EventList.OfType<ConversationEvent>().Where(filter);

        ConversationEvent? GetFirstMatchingEvent(Func<ConversationEvent, bool> filter)
            => conversationPartner.EventList.OfType<ConversationEvent>().FirstOrDefault(filter);

        void SwitchPlayer()
        {
            if (CurrentWindow.Window == Window.Conversation)
            {
                UpdateCharacterInfo(character);
                UpdateButtons();
            }
        }

        OpenStorage = createdItems;
        ActivePlayerChanged += SwitchPlayer;

        conversationEvent ??= GetFirstMatchingEvent(e => e.Interaction == InteractionType.Talk);

        bool creatingItems = false;
        var createdItemSlots = createdItems.Slots.ToList();
        var currentInteractionType = InteractionType.Talk;
        bool lastEventStatus = true;
        bool aborted = false;
        var textArea = new Rect(17, 44, 174, 79);
        UIText? conversationText = null;
        ItemGrid? itemGrid = null;
        var oldKeywords = new List<string>(Dictionary!);
        var newKeywords = new List<string>();
        uint amount = 0; // gold, food, etc
        UIText? moveItemMessage = null;
        layout.DraggedItemDropped += DraggedItemDropped;
        closeWindowHandler = _ => CleanUp();

        void SetText(string text, Action? followAction = null)
        {
            conversationText!.Visible = true;
            conversationText.SetText(ProcessText(text));
            conversationText.Clicked += TextClicked;
            CursorType = CursorType.Click;
            InputEnable = false;
            ConversationTextActive = true;

            void TextClicked(bool toEnd)
            {
                if (toEnd)
                {
                    conversationText.Clicked -= TextClicked;
                    conversationText.Visible = false;
                    InputEnable = true;
                    ConversationTextActive = false;
                    ExecuteNextUpdateCycle(() =>
                    {
                        CursorType = CursorType.Sword;
                        followAction?.Invoke();
                    });
                }
            }
        }

        void ShowDictionary()
        {
            aborted = false;
            OpenDictionary(SayWord, word => !oldKeywords.Contains(word) || newKeywords.Contains(word)
            ? TextColor.LightYellow : TextColor.BrightGray);
        }

        void SayWord(string keyword)
        {
            ClosePopup();
            UntrapMouse();

            foreach (var e in GetMatchingEvents(e => e.Interaction == InteractionType.Keyword))
            {
                var expectedKeyword = textDictionary.Entries[(int)e.KeywordIndex];

                if (string.Compare(keyword, expectedKeyword, true) == 0)
                {
                    currentInteractionType = InteractionType.Keyword;
                    conversationEvent = e;
                    layout.ButtonsDisabled = true;
                    aborted = false;
                    lastEventStatus = true;
                    HandleNextEvent();
                    return;
                }
            }

            // There is no event for it so just display a message.
            SetText(DataNameProvider.DontKnowAnythingSpecialAboutIt);
        }

        void ShowItems(string text, InteractionType interactionType)
        {
            currentInteractionType = interactionType;

            var message = layout.AddText(textArea, ProcessText(text, textArea), TextColor.BrightGray);

            void Abort()
            {
                itemGrid.HideTooltip();
                itemGrid.ItemClicked -= ItemClicked;
                message?.Destroy();
                UntrapMouse();
                layout.ButtonsDisabled = false;
                nextClickHandler = null;
                ShowCreatedItems();
            }

            itemGrid!.Disabled = false;
            itemGrid.DisableDrag = true;
            CursorType = CursorType.Sword;
            var itemArea = new Rect(16, 139, 151, 53);
            TrapMouse(itemArea);
            layout.ButtonsDisabled = true;
            itemGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
            void SetupRightClickAbort()
            {
                nextClickHandler = buttons =>
                {
                    if (buttons == MouseButtons.Right)
                    {
                        Abort();
                        return true;
                    }

                    return false;
                };
            }
            SetupRightClickAbort();
            void CheckItem(ItemSlot itemSlot)
            {
                void MoveBack(Action? followAction)
                {
                    StartSequence();
                    itemGrid.HideTooltip();
                    itemGrid.PlayMoveAnimation(itemSlot, itemGrid.GetSlotPosition(itemGrid.SlotFromItemSlot(itemSlot)), () =>
                    {
                        itemGrid.ResetAnimation(itemSlot);
                        EndSequence();
                        Abort();
                        followAction?.Invoke();
                    }, 650);
                }
                EndSequence();
                message?.Destroy();
                message = null;
                layout.GetItem(itemSlot)!.Dragged = true; // Keep it above UI
                UntrapMouse();
                layout.ButtonsDisabled = false;
                conversationEvent = GetFirstMatchingEvent(e => e.Interaction == interactionType && e.ItemIndex == itemSlot.ItemIndex);

                if (conversationEvent == null)
                {
                    SetText(DataNameProvider.NotInterestedInItem, () => MoveBack(null));
                }
                else
                {
                    void HandleInteraction()
                    {
                        HandleNextEvent(eventType =>
                        {
                            // Note: A create event must also trigger the item consumption.
                            // Otherwise we might have two item grids interfering.
                            if (eventType == EventType.Interact || eventType == EventType.Create)
                            {
                                // If we are here the user clicked the associated text etc.
                                if (interactionType == InteractionType.GiveItem)
                                {
                                    bool consume = eventType == EventType.Interact;

                                    if (!consume)
                                    {
                                        var @event = conversationEvent;

                                        while (@event != null)
                                        {
                                            if (@event.Type == EventType.Interact)
                                            {
                                                consume = true;
                                                break;
                                            }

                                            @event = @event.Next;
                                        }
                                    }

                                    if (consume)
                                    {
                                        // Consume
                                        StartSequence();
                                        itemGrid.HideTooltip();
                                        layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true, () =>
                                        {
                                            uint itemIndex = itemSlot.ItemIndex;
                                            itemSlot.Remove(1);
                                            InventoryItemRemoved(itemIndex, 1, CurrentPartyMember);
                                            //ShowCreatedItems();
                                            EndSequence();
                                            Abort();
                                            if (eventType == EventType.Interact)
                                                HandleNextEvent(null);
                                            else
                                                HandleEvent(null);
                                        }, new Position(215, 75), false);
                                    }
                                    else
                                        ExecuteNextUpdateCycle(() => HandleNextEvent(null));
                                }
                                else // Show item
                                {
                                    if (eventType == EventType.Interact)
                                        MoveBack(() => HandleNextEvent(null));
                                    else
                                        ExecuteNextUpdateCycle(() => HandleNextEvent(null));
                                }
                            }
                            else if (eventType == EventType.Invalid) // End of event chain
                            {
                                MoveBack(null);
                            }
                            else
                            {
                                HandleInteraction();
                            }
                        });
                    }

                    layout.ButtonsDisabled = true;
                    HandleInteraction();
                }
            }
            void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
            {
                itemGrid.ItemClicked -= ItemClicked;
                nextClickHandler = null;
                UntrapMouse();
                layout.ButtonsDisabled = false;
                StartSequence();
                itemGrid.HideTooltip();
                itemGrid.PlayMoveAnimation(itemSlot, new Position(215, 75), () => CheckItem(itemSlot), 650);
            }
            itemGrid.ItemClicked += ItemClicked;
        }

        void ShowItem()
        {
            aborted = false;
            ShowItems(DataNameProvider.WhichItemToShow, InteractionType.ShowItem);
        }

        void GiveItem()
        {
            aborted = false;
            ShowItems(DataNameProvider.WhichItemToGive, InteractionType.GiveItem);
        }

        void GiveGold()
        {
            aborted = false;
            layout.OpenAmountInputBox(DataNameProvider.GiveHowMuchGoldToNPC, 96, DataNameProvider.GoldName,
                CurrentPartyMember.Gold, gold =>
                {
                    ClosePopup();
                    var @event = GetFirstMatchingEvent(e => e.Interaction == InteractionType.GiveGold);
                    if (@event != null)
                    {
                        if (gold < @event.Value)
                        {
                            SetText(DataNameProvider.MoreGoldNeeded);
                        }
                        else
                        {
                            conversationEvent = @event;
                            layout.ButtonsDisabled = true;
                            currentInteractionType = InteractionType.GiveGold;
                            amount = @event.Value;
                            aborted = false;
                            lastEventStatus = true;
                            HandleNextEvent();
                        }
                    }
                    else
                    {
                        SetText(DataNameProvider.NotInterestedInGold);
                    }
                });
        }

        void GiveFood()
        {
            aborted = false;
            layout.OpenAmountInputBox(DataNameProvider.GiveHowMuchFoodToNPC, 109, DataNameProvider.FoodName,
                CurrentPartyMember.Food, food =>
                {
                    ClosePopup();
                    var @event = GetFirstMatchingEvent(e => e.Interaction == InteractionType.GiveFood);
                    if (@event != null)
                    {
                        if (food < @event.Value)
                        {
                            SetText(DataNameProvider.MoreFoodNeeded);
                        }
                        else
                        {
                            conversationEvent = @event;
                            layout.ButtonsDisabled = true;
                            currentInteractionType = InteractionType.GiveFood;
                            amount = @event.Value;
                            aborted = false;
                            lastEventStatus = true;
                            HandleNextEvent();
                        }
                    }
                    else
                    {
                        SetText(DataNameProvider.NotInterestedInFood);
                    }
                });
        }

        void AskToJoin()
        {
            aborted = false;
            if (PartyMembers.Count() == MaxPartyMembers)
            {
                conversationEvent = null;
                SetText(DataNameProvider.PartyFull);
                layout.ButtonsDisabled = false;
                return;
            }

            if (character is PartyMember &&
                (conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.JoinParty)) != null)
            {
                layout.ButtonsDisabled = true;
                currentInteractionType = InteractionType.JoinParty;
                aborted = false;
                lastEventStatus = true;
                HandleNextEvent();
            }
            else
            {
                SetText(DataNameProvider.DenyJoiningParty);
            }
        }

        void AskToLeave()
        {
            aborted = false;
            if (character is PartyMember partyMember && PartyMembers.Contains(partyMember))
            {
                if (!partyMember.Alive)
                {
                    SetText(DataNameProvider.CannotSendDeadPeopleAway);
                    return;
                }

                if (partyMember.Conditions.HasFlag(Condition.Crazy))
                {
                    SetText(DataNameProvider.CrazyPeopleDontFollowCommands);
                    return;
                }

                if (partyMember.Conditions.HasFlag(Condition.Petrified))
                {
                    SetText(DataNameProvider.PetrifiedPeopleCantGoHome);
                    return;
                }

                if (!partyMember.Alive)
                {
                    SetText(DataNameProvider.CannotSendDeadPeopleAway);
                    return;
                }

                if (Map!.World != World.Lyramion) // TODO: You can still leave in Morag hangar and prison like in the original
                {
                    SetText(DataNameProvider.DenyLeavingPartyOnMoon);
                    return;
                }

                if ((conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.LeaveParty)) != null)
                {
                    currentInteractionType = InteractionType.LeaveParty;
                    layout.ButtonsDisabled = true;
                    aborted = false;
                    lastEventStatus = true;
                    HandleNextEvent();
                }
                else
                {
                    SetText(DataNameProvider.WellIShouldLeave);
                    RemovePartyMember(() => Exit()); // Just remove from party and close
                }
            }
        }

        void AddPartyMember(Action followAction)
        {
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                if (GetPartyMember(i) == null)
                {
                    var partyMember = (character as PartyMember)!;
                    CurrentSavegame!.CurrentPartyMemberIndices[i] =
                        CurrentSavegame.PartyMembers.FirstOrDefault(p => p.Value == partyMember).Key;
                    this.AddPartyMember(i, partyMember, followAction, true);
                    // Set battle position
                    CurrentSavegame.BattlePositions[i] = 0xff;
                    var usePositions = CurrentSavegame.BattlePositions.ToList();
                    for (int p = 11; p >= 0; --p)
                    {
                        if (!usePositions.Contains((byte)p))
                        {
                            CurrentSavegame.BattlePositions[i] = (byte)p;
                            break;
                        }
                    }
                    layout.EnableButton(4, true); // Enable "Ask to leave"
                    layout.EnableButton(5, false); // Disable "Ask to join"
                    SetMapCharacterBit(Map!.Index, characterIndex!.Value, true);
                    if (partyMember.CharacterBitIndex == 0xffff || partyMember.CharacterBitIndex == 0x0000)
                        partyMember.CharacterBitIndex = (ushort)(((Map.Index - 1) << 5) | characterIndex.Value);
                    break;
                }
            }
        }

        void RemovePartyMember(Action? followAction)
        {
            var partyMember = (character as PartyMember)!;
            var index = partyMember.CharacterBitIndex;
            if (index == 0xffff)
                index = PartyMemberCharacterBits[partyMember.Index];
            uint mapIndex = 1 + ((uint)index >> 5);
            uint characterIndex = (uint)index & 0x1f;
            this.RemovePartyMember(SlotFromPartyMember(partyMember)!.Value, false, followAction);
            CurrentSavegame!.CurrentPartyMemberIndices[SlotFromPartyMember(partyMember)!.Value] = 0;
            SetMapCharacterBit(mapIndex, characterIndex, false);
        }

        void ShowCreatedItems()
        {
            if (createdItemSlots.Any(item => !item.Empty))
            {
                itemGrid!.Disabled = false;
                itemGrid.DisableDrag = false;
                itemGrid.Initialize(createdItemSlots, false);
            }
            else
            {
                itemGrid!.Disabled = true;
            }
        }

        void CreateItem(uint itemIndex, uint amount)
        {
            // Note: Multiple items can be created. While at least one
            // item was created and is not picked up, the item grid is
            // enabled.
            int remainingCount = (int)amount;

            for (int i = 0; i < 24; ++i)
            {
                if (createdItemSlots[i].Empty)
                {
                    createdItemSlots[i].FillWithNewItem(ItemManager, itemIndex, ref remainingCount);

                    if (remainingCount == 0)
                        break;
                }
            }
            ShowCreatedItems();
        }

        void Exit(bool showLeaveMessage = false)
        {
            aborted = false;
            if (showLeaveMessage)
            {
                if (createdItems.HasAnyImportantItem(ItemManager))
                {
                    aborted = true;
                    SetText(DataNameProvider.DontForgetItems +
                        string.Join(", ", createdItems.GetImportantItemNames(ItemManager)) + ".");
                    return;
                }

                if (createdItems.Slots.Cast<ItemSlot>().Any(s => !s.Empty))
                {
                    ShowDecisionPopup(DataNameProvider.LeaveConversationWithoutItems, response =>
                    {
                        if (response == PopupTextEvent.Response.Yes)
                        {
                            ExitConversation();
                            return;
                        }

                        aborted = true;
                    }, 1);
                    return;
                }

                ExitConversation();
                return;

                void ExitConversation()
                {
                    conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.Leave);

                    if (conversationEvent != null)
                    {
                        currentInteractionType = InteractionType.Leave;
                        layout.ButtonsDisabled = true;
                        aborted = false;
                        lastEventStatus = true;
                        HandleNextEvent();
                        return;
                    }
                    else
                    {
                        SetText(DataNameProvider.GoodBye, CloseWindow);
                        return;
                    }
                }
            }

            CloseWindow();
        }

        void CleanUp()
        {
            layout.DraggedItemDropped -= DraggedItemDropped;
            ActivePlayerChanged -= SwitchPlayer;
            ConversationTextActive = false;
            layout.ButtonsDisabled = false;
        }

        void HandleNextEvent(Action<EventType>? followAction = null)
        {
            conversationEvent = conversationEvent?.Next;
            layout.ButtonsDisabled = conversationEvent != null;
            HandleEvent(followAction);
        }

        void HandleEvent(Action<EventType>? followAction = null)
        {
            if (conversationEvent == null || aborted)
            {
                if (currentInteractionType == InteractionType.LeaveParty ||
                    currentInteractionType == InteractionType.Leave)
                {
                    Exit(); // After leaving the party or just leaving the conversation, close the window.
                }

                followAction?.Invoke(EventType.Invalid);

                return;
            }

            var nextAction = followAction ?? (_ => HandleNextEvent());

            if (conversationEvent is PrintTextEvent printTextEvent)
            {
                SetText(conversationPartner.Texts[(int)printTextEvent.NPCTextIndex], () => nextAction?.Invoke(EventType.PrintText));
            }
            else if (conversationEvent is ExitEvent)
            {
                // Exit event triggered after create event -> abort.
                if (creatingItems)
                    return;

                Exit();
                nextAction?.Invoke(EventType.Exit);
            }
            else if (conversationEvent is CreateEvent createEvent)
            {
                creatingItems = true;

                // Note: It is important to trigger the next action first
                // as it might trigger a consumption of a previously given item.
                // The create item only updates the grid of created items.
                nextAction?.Invoke(EventType.Create);

                switch (createEvent.TypeOfCreation)
                {
                    case CreateEvent.CreateType.Item:
                        CreateItem(createEvent.ItemIndex, createEvent.Amount);
                        break;
                    case CreateEvent.CreateType.Gold:
                        CurrentPartyMember.AddGold(createEvent.Amount);
                        UpdateCharacterInfo(character);
                        break;
                    default: // food
                        CurrentPartyMember.AddFood(createEvent.Amount);
                        UpdateCharacterInfo(character);
                        break;
                }

                if (conversationEvent == createEvent)
                {
                    conversationEvent = conversationEvent.Next;
                    layout.ButtonsDisabled = conversationEvent != null;

                    // Sometimes multiple items are created, so do them all at once.
                    if (conversationEvent is CreateEvent)
                    {
                        HandleEvent();
                    }
                }
                layout.ButtonsDisabled = conversationEvent != null;
            }
            else if (conversationEvent is InteractEvent)
            {
                switch (currentInteractionType)
                {
                    case InteractionType.GiveItem:
                    {
                        // Note: The ShowItems method will take care of it.
                        nextAction?.Invoke(EventType.Interact);
                        break;
                    }
                    case InteractionType.GiveGold:
                        CurrentPartyMember.RemoveGold(amount);
                        UpdateCharacterInfo(character);
                        if (CurrentPartyMember.Gold == 0)
                            layout.EnableButton(7, false);
                        nextAction?.Invoke(EventType.Interact);
                        break;
                    case InteractionType.GiveFood:
                        CurrentPartyMember.RemoveFood(amount);
                        UpdateCharacterInfo(character);
                        if (CurrentPartyMember.Food == 0)
                            layout.EnableButton(8, false);
                        nextAction?.Invoke(EventType.Interact);
                        break;
                    case InteractionType.JoinParty:
                        AddPartyMember(() => nextAction?.Invoke(EventType.Interact));
                        break;
                    case InteractionType.LeaveParty:
                        RemovePartyMember(() => nextAction?.Invoke(EventType.Interact));
                        break;
                    case InteractionType.Leave:
                        Exit();
                        nextAction?.Invoke(EventType.Interact);
                        break;
                    default:
                        nextAction?.Invoke(EventType.Interact);
                        break;
                }
            }
            else
            {
                if (conversationEvent is ActionEvent actionEvent &&
                    actionEvent.TypeOfAction == ActionEvent.ActionType.AddKeyword)
                {
                    string keyword = textDictionary.Entries[(int)actionEvent.ObjectIndex];

                    if (!newKeywords.Contains(keyword))
                        newKeywords.Add(keyword);
                }

                if (conversationEvent.Type == EventType.Teleport ||
                    conversationEvent.Type == EventType.Chest ||
                    conversationEvent.Type == EventType.Door ||
                    conversationEvent.Type == EventType.EnterPlace ||
                    conversationEvent.Type == EventType.Riddlemouth ||
                    conversationEvent.Type == EventType.StartBattle)
                {
                    CloseWindow(() => EventExtensions.TriggerEventChain(Map!, this, EventTrigger.Always,
                        (uint)player!.Position.X, (uint)player.Position.Y, conversationEvent, true));
                }
                else
                {
                    var trigger = EventTrigger.Always;
                    conversationEvent = EventExtensions.ExecuteEvent(conversationEvent, Map!, this, ref trigger,
                        (uint)player!.Position.X, (uint)player.Position.Y, ref lastEventStatus, out aborted,
                        out var eventProvider, conversationPartner);
                    layout.ButtonsDisabled = conversationEvent != null;

                    // Might be reduced or added by action events
                    layout.EnableButton(7, CurrentPartyMember.Gold != 0);
                    layout.EnableButton(8, CurrentPartyMember.Food != 0);

                    if (conversationEvent == null && eventProvider != null)
                    {
                        if (eventProvider.Event != null)
                        {
                            conversationEvent = eventProvider.Event;
                            layout.ButtonsDisabled = conversationEvent != null;
                            HandleEvent(followAction);
                        }
                        else
                        {
                            eventProvider.Provided += @event =>
                            {
                                conversationEvent = @event;
                                layout.ButtonsDisabled = conversationEvent != null;

                                if (@event == null)
                                    followAction?.Invoke(EventType.Invalid);
                                else
                                    HandleEvent(followAction);
                            };
                        }
                    }
                    else
                    {
                        HandleEvent(followAction);
                    }
                }
            }
        }

        void ItemDragged(int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot)
        {
            ExecuteNextUpdateCycle(() =>
            {
                moveItemMessage = layout.AddText(textArea, DataNameProvider.WhereToMoveIt,
                    TextColor.BrightGray, TextAlign.Center);
                var draggedSourceSlot = itemGrid.GetItemSlot(slotIndex)!;
                if (updateSlot)
                    draggedSourceSlot.Remove(amount);
                createdItemSlots[slotIndex].Replace(draggedSourceSlot);
                itemGrid.SetItem(slotIndex, draggedSourceSlot);
            });
        }

        void DraggedItemDropped()
        {
            itemGrid!.Disabled = !createdItemSlots.Any(slot => !slot.Empty);
            moveItemMessage?.Destroy();
            moveItemMessage = null;

            if (creatingItems && itemGrid.Disabled)
            {
                creatingItems = false;
                UpdateButtons();
                HandleEvent();
            }
        }

        void UpdateButtons()
        {
            bool enableItemButtons = CurrentPartyMember.Inventory.Slots.Any(s => s?.Empty == false);
            layout.EnableButton(3, enableItemButtons);
            layout.EnableButton(6, enableItemButtons);
            layout.EnableButton(7, CurrentPartyMember.Gold != 0);
            layout.EnableButton(8, CurrentPartyMember.Food != 0);
        }

        Fade(() =>
        {
            SetWindow(Window.Conversation, conversationPartner, characterIndex, conversationEvent, createdItems);
            layout.SetLayout(LayoutType.Conversation);
            ShowMap(false);
            layout.Reset();

            layout.FillArea(new Rect(15, 43, 177, 80), GetUIColor(28), false);
            layout.FillArea(new Rect(15, 136, 152, 57), GetUIColor(28), false);

            DisplayCharacterInfo(character, true);

            if (character.Type != CharacterType.PartyMember ||
                SlotFromPartyMember((character as PartyMember)!) == null)
                layout.EnableButton(4, false); // Disable "Ask to leave" if not in party
            if (character is PartyMember partyMember && PartyMembers.Contains(partyMember))
                layout.EnableButton(5, false); // Disable "Ask to join" if already in party

            UpdateButtons();

            layout.AttachEventToButton(0, ShowDictionary);
            layout.AttachEventToButton(2, () => Exit(true));
            layout.AttachEventToButton(3, ShowItem);
            layout.AttachEventToButton(4, AskToLeave);
            layout.AttachEventToButton(5, AskToJoin);
            layout.AttachEventToButton(6, GiveItem);
            layout.AttachEventToButton(7, GiveGold);
            layout.AttachEventToButton(8, GiveFood);

            // Add item grid
            var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
            itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
            itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat(null as ItemSlot, 24).ToList(),
                false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
            itemGrid.ItemDragged += ItemDragged;
            layout.AddItemGrid(itemGrid);
            ShowCreatedItems();

            // Note: Mouse handling in Layout assumes this is the last text (text[^1]) so ensure that.
            conversationText = layout.AddScrollableText(textArea, ProcessText(""), TextColor.BrightGray);
            conversationText.Visible = false;

            if (showInitialText)
            {
                if (conversationEvent != null)
                {
                    layout.ButtonsDisabled = true;
                    HandleNextEvent();
                }
                else
                {
                    SetText(DataNameProvider.Hello);
                }
            }
        });
    }

    internal class ConversationItems : IItemStorage
    {
        public const int SlotsPerRow = 6;
        public const int SlotRows = 4;

        public ItemSlot[,] Slots { get; } = new ItemSlot[6, 4];
        public bool AllowsItemDrop { get; set; } = false;

        public ConversationItems()
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                    Slots[x, y] = new ItemSlot();
            }
        }

        public void ResetItem(int slot, ItemSlot item)
        {
            int column = slot % SlotsPerRow;
            int row = slot / SlotsPerRow;

            if (Slots[column, row].Add(item) != 0)
                throw new AmbermoonException(ExceptionScope.Application, "Unable to reset conversation item.");
        }

        public ItemSlot GetSlot(int slot) => Slots[slot % SlotsPerRow, slot / SlotsPerRow];
    }
}
