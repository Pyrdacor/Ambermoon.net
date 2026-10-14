using System;
using System.Linq;
using System.Text;
using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon;

/// <summary>
/// Small API for automated test scenarios (see command line option --script).
/// </summary>
partial class GameCore
{
    sealed record ScriptConversationActions(Action<string> SayWord, Action ShowItem, Action GiveItem, Action Leave, Func<int> TakeItems, Action<uint> GiveGold);

    ScriptConversationActions? scriptConversation;
    Func<uint, bool>? scriptLockItemUser;
    Action<string>? scriptRiddlemouthAnswer;
    Action<string>? scriptDictionaryChoice;
    Action<int>? scriptConversationItemSelector;

    public bool ScriptConversationActive => scriptConversation != null && CurrentWindow.Window == UI.Window.Conversation;

    /// <summary>
    /// True if a conversation text waits for a click.
    /// </summary>
    public bool ScriptConversationTextActive => ConversationTextActive;

    /// <summary>
    /// True while an event picture with its text is shown (closed by a click).
    /// </summary>
    public bool ScriptEventPictureActive => CurrentWindow.Window == UI.Window.Event;

    /// <summary>
    /// Starts a conversation with the given NPC like talking to it on the map.
    /// Returns an error message or null on success.
    /// </summary>
    public string? ScriptTalk(uint npcIndex)
    {
        if (WindowOrPopupActive || !InputEnable)
            return "a window or popup is open";

        var npc = CharacterManager.GetNPC(npcIndex);

        if (npc == null)
            return $"there is no NPC {npcIndex}";

        // the map character index is used if the NPC is removed from the map afterwards
        int characterIndex = Array.FindIndex(Map!.CharacterReferences, c => c?.Type == CharacterType.NPC && c.Index == npcIndex);
        npc.ExecuteEvents(this, EventTrigger.Mouth, characterIndex == -1 ? 0u : (uint)characterIndex);

        return null;
    }

    /// <summary>
    /// Says a keyword (dictionary index or word) in the active conversation.
    /// </summary>
    public string? ScriptSay(string keyword)
    {
        if (scriptConversation == null)
            return "no conversation is active";

        if (uint.TryParse(keyword, out uint index))
            keyword = textDictionary.Entries[(int)index];

        scriptConversation.SayWord(keyword);

        return null;
    }

    /// <summary>
    /// Shows or gives an item of the active party member in the active conversation.
    /// </summary>
    public string? ScriptConversationItem(uint itemIndex, bool give)
    {
        if (scriptConversation == null)
            return "no conversation is active";

        int slot = Array.FindIndex(CurrentPartyMember!.Inventory.Slots, s => !s.Empty && s.ItemIndex == itemIndex);

        if (slot == -1)
            return $"the active party member has no item {itemIndex}";

        if (give)
            scriptConversation.GiveItem();
        else
            scriptConversation.ShowItem();

        if (scriptConversationItemSelector == null)
            return "the item selection could not be opened";

        scriptConversationItemSelector(slot);

        return null;
    }

    /// <summary>
    /// Puts the items created in the active conversation into the active party member's inventory.
    /// Returns the number of taken item slots, -1 if the items could not be taken (weight
    /// or no free slot) or null if no conversation is active.
    /// </summary>
    public int? ScriptTakeConversationItems()
    {
        if (scriptConversation == null)
            return null;

        return scriptConversation.TakeItems();
    }

    /// <summary>
    /// Gives gold of the active party member in the active conversation.
    /// </summary>
    public string? ScriptGiveGold(uint amount)
    {
        if (scriptConversation == null)
            return "no conversation is active";

        if (CurrentPartyMember!.Gold < amount)
            return $"the active party member has only {CurrentPartyMember.Gold} gold";

        scriptConversation.GiveGold(amount);

        return null;
    }

    /// <summary>
    /// Leaves the active conversation like the exit button.
    /// </summary>
    public string? ScriptLeaveConversation()
    {
        if (scriptConversation == null)
            return "no conversation is active";

        scriptConversation.Leave();

        return null;
    }

    /// <summary>
    /// Triggers the map events at the given 1-based map position.
    /// </summary>
    public bool ScriptTriggerMapEvent(EventTrigger trigger, uint x, uint y)
    {
        if (WindowOrPopupActive || x == 0 || y == 0 || x > Map!.Width || y > Map.Height)
            return false;

        if (trigger < EventTrigger.Item0)
            return TriggerMapEvents(trigger, x - 1, y - 1);

        // Using an item on a tile. Like using it via the inventory, the item is consumed
        // if it is destroyed after usage.
        uint itemIndex = (uint)(trigger - EventTrigger.Item0);
        var partyMember = CurrentPartyMember!;
        var itemSlot = partyMember.Inventory.Slots.FirstOrDefault(s => !s.Empty && s.ItemIndex == itemIndex);

        if (itemSlot == null || !TriggerMapEvents(trigger, x - 1, y - 1))
            return false;

        if (ItemManager.GetItem(itemIndex).Flags.HasFlag(ItemFlags.DestroyAfterUsage))
        {
            itemSlot.Remove(1);
            InventoryItemRemoved(itemIndex, 1, partyMember);
        }

        return true;
    }

    /// <summary>
    /// Interacts with a map character (1-based index) like clicking it with the given cursor.
    /// </summary>
    public bool ScriptInteractWithCharacter(uint characterIndex, EventTrigger trigger)
    {
        if (WindowOrPopupActive || characterIndex == 0)
            return false;

        return is3D
            ? renderMap3D!.InteractWithCharacter(characterIndex - 1, trigger)
            : renderMap2D!.InteractWithCharacter(characterIndex - 1, trigger);
    }

    /// <summary>
    /// Makes the party member in the given slot (1-6) the active one (like clicking the portrait).
    /// </summary>
    public bool ScriptSetActivePartyMember(int slot)
    {
        if (slot < 1 || slot > MaxPartyMembers || GetPartyMember(slot - 1) == null)
            return false;

        SetActivePartyMember(slot - 1);
        return true;
    }

    public void ScriptSetGlobalVariable(uint index, bool value) => CurrentSavegame!.SetGlobalVariable(index, value);

    /// <summary>
    /// Sets the time of day (full hour).
    /// </summary>
    public void ScriptSetHour(uint hour)
    {
        CurrentSavegame!.Hour = hour % 24;
        CurrentSavegame.Minute = 0;
    }

    /// <summary>
    /// Sets a skill of the active party member (e.g. searching for hidden things).
    /// </summary>
    public void ScriptSetSkill(Skill skill, uint value)
    {
        var skillValue = CurrentPartyMember!.Skills[skill];
        skillValue.MaxValue = Math.Max(skillValue.MaxValue, value);
        skillValue.CurrentValue = value;
    }

    /// <summary>
    /// Sets an attribute of the active party member (e.g. dexterity for a test).
    /// </summary>
    public void ScriptSetAttribute(Data.Attribute attribute, uint value)
    {
        var attributeValue = CurrentPartyMember!.Attributes[attribute];
        attributeValue.MaxValue = Math.Max(attributeValue.MaxValue, value);
        attributeValue.CurrentValue = value;
    }

    public uint ScriptGetAttribute(Data.Attribute attribute) => CurrentPartyMember!.Attributes[attribute].CurrentValue;

    public uint ScriptGetSpellLearningPoints() => CurrentPartyMember!.SpellLearningPoints;

    /// <summary>
    /// Returns a text of the data name provider by its property name (e.g. DropHowMuchFoodMessage).
    /// </summary>
    public string? ScriptGetText(string propertyName) => typeof(IDataNameProvider).GetProperty(propertyName)?.GetValue(DataNameProvider) as string;

    public string ScriptGetSpellName(Spell spell) => DataNameProvider.GetSpellName(spell);

    public string? ScriptGetMonsterName(uint index) => CharacterManager.GetMonster(index)?.Name;

    public MapFlags? ScriptGetMapFlags(uint mapIndex) => MapManager.GetMap(mapIndex)?.Flags;

    /// <summary>
    /// Returns the name and the savegame bit index of a goto point (1-based) of a map.
    /// </summary>
    public (string Name, uint Bit)? ScriptGetGotoPoint(uint mapIndex, int index)
    {
        var gotoPoints = MapManager.GetMap(mapIndex)?.GotoPoints;

        if (gotoPoints == null || index < 1 || index > gotoPoints.Count)
            return null;

        var gotoPoint = gotoPoints[index - 1];

        return (gotoPoint.Name, gotoPoint.Index);
    }

    /// <summary>
    /// True if the party can walk onto the given 1-based position of a 2D map (initial tiles).
    /// </summary>
    public bool? ScriptIsWalkable(uint mapIndex, uint x, uint y)
    {
        var map = MapManager.GetMap(mapIndex);

        if (map == null || map.Type != MapType.Map2D || x < 1 || y < 1 || x > map.Width || y > map.Height)
            return null;

        return map.InitialTiles[x - 1, y - 1].AllowMovement(MapManager.GetTilesetForMap(map), Data.Enumerations.TravelType.Walk);
    }

    /// <summary>
    /// Informs the quest log about items which were added without the usual item handling (e.g. by cheats).
    /// </summary>
    public void ItemsAddedExternally(uint itemIndex, uint amount) => QuestLog?.CheckItem(ItemManager.GetItem(itemIndex), amount);

    /// <summary>
    /// True if the given map event chain (1-based) is disabled.
    /// </summary>
    public bool ScriptGetEventBit(uint mapIndex, uint eventIndex) => CurrentSavegame!.GetEventBit(mapIndex, eventIndex - 1);

    /// <summary>
    /// Recreates the quest log from the current savegame like loading a game does.
    /// </summary>
    public void ScriptReloadQuestLog()
    {
        if (QuestLog?.Open == true)
            ClosePopup();

        CreateQuestLog();
    }

    /// <summary>
    /// Uses an item from the active party member's inventory exactly like a
    /// player would do it via the inventory (including item consumption and
    /// map event triggering). Returns false if the item is not in the inventory.
    /// </summary>
    public bool ScriptUseItem(uint itemIndex)
    {
        if (CurrentPartyMember == null || WindowOrPopupActive)
            return false;

        int? slot = SlotFromPartyMember(CurrentPartyMember);

        if (slot == null)
            return false;

        int inventorySlot = Array.FindIndex(CurrentPartyMember.Inventory.Slots, s => !s.Empty && s.ItemIndex == itemIndex);

        if (inventorySlot == -1)
            return false;

        return OpenPartyMember(slot.Value, true, () =>
        {
            var itemSlot = CurrentInventory!.Inventory.Slots[inventorySlot];
            layout.UseItem(layout.GetInventoryGrid(), inventorySlot, itemSlot);
        });
    }

    public bool ScriptGetGlobalVariable(uint index) => CurrentSavegame!.GetGlobalVariable(index);

    /// <summary>
    /// Teleports to the given 1-based map position or the nearest free position around it
    /// (e.g. next to a chest). The party faces the target position. Returns the reached
    /// position or null if no position was possible.
    /// </summary>
    public Position? ScriptGoto(uint mapIndex, uint x, uint y)
    {
        var map = MapManager.GetMap(mapIndex);

        if (map == null)
            return null;

        // nearest positions first and orthogonal neighbors before diagonal ones (to face the target)
        var offsets = Enumerable.Range(-3, 7).SelectMany(dy => Enumerable.Range(-3, 7).Select(dx => (dx, dy)))
            .OrderBy(o => Math.Max(Math.Abs(o.dx), Math.Abs(o.dy))).ThenBy(o => Math.Abs(o.dx) + Math.Abs(o.dy));

        foreach (var (dx, dy) in offsets)
        {
            int tx = (int)x + dx;
            int ty = (int)y + dy;

            if (tx < 1 || ty < 1 || tx > map.Width || ty > map.Height)
                continue;

            // face the target
            var direction = dx == 0 && dy == 0 ? CharacterDirection.Up
                : Math.Abs(dx) >= Math.Abs(dy)
                    ? (dx > 0 ? CharacterDirection.Left : CharacterDirection.Right)
                    : (dy > 0 ? CharacterDirection.Up : CharacterDirection.Down);

            if (Teleport(mapIndex, (uint)tx, (uint)ty, direction, out _, false, true))
                return new Position(tx, ty);
        }

        // e.g. on the sea of a world map without a ship
        if (Teleport(mapIndex, x, y, CharacterDirection.Up, out _, true, true))
            return new Position((int)x, (int)y);

        return null;
    }

    /// <summary>
    /// Says a word (dictionary index or word) to the shown riddlemouth or the shown dictionary
    /// (e.g. when a word is said on a map tile).
    /// </summary>
    public string? ScriptAnswerRiddlemouth(string answer)
    {
        if (uint.TryParse(answer, out uint index))
            answer = textDictionary.Entries[(int)index];

        if (scriptDictionaryChoice != null && PopupActive)
        {
            scriptDictionaryChoice(answer);
            return null;
        }

        if (scriptRiddlemouthAnswer == null || CurrentWindow.Window != UI.Window.Riddlemouth || PopupActive)
            return "no riddlemouth is waiting for an answer";

        scriptRiddlemouthAnswer(answer);

        return null;
    }

    /// <summary>
    /// Uses an item (key or lockpick) of the active party member on the shown lock of a chest or door.
    /// </summary>
    public string? ScriptUseKey(uint itemIndex)
    {
        if (scriptLockItemUser == null || (CurrentWindow.Window != UI.Window.Chest && CurrentWindow.Window != UI.Window.Door))
            return "no lock is shown";

        if (!scriptLockItemUser(itemIndex))
            return $"the active party member has no item {itemIndex}";

        return null;
    }

    /// <summary>
    /// Puts all items of the open chest or battle loot into the active party member's inventory.
    /// Returns the number of item slots which could not be taken or null if no chest is open.
    /// </summary>
    public int? ScriptTakeLoot()
    {
        // the chest window must be shown completely (the storage is set before the window opens)
        if (OpenStorage is not ITreasureStorage storage || !InputEnable ||
            (CurrentWindow.Window != UI.Window.Chest && CurrentWindow.Window != UI.Window.BattleLoot))
            return null;

        int partyMemberSlot = SlotFromPartyMember(CurrentPartyMember!)!.Value;
        int remaining = 0;

        foreach (var itemSlot in storage.Slots)
        {
            if (!itemSlot.Empty && DropItem(partyMemberSlot, null, itemSlot) != 0)
                ++remaining;
        }

        // A chest only counts as emptied if the gold and food were taken as well.
        // Like taking things via the UI, the chest logic is informed (e.g. emptied
        // treasure piles close their window). Once the window is closed, nothing else
        // must be done with it.
        bool ChestWindowOpen() => storage is Chest && CurrentWindow.Window == UI.Window.Chest;

        if (storage.Gold != 0)
        {
            CurrentPartyMember!.AddGold(storage.Gold);
            storage.Gold = 0;

            if (ChestWindowOpen())
                ChestGoldChanged();
        }

        if (storage.Food != 0)
        {
            CurrentPartyMember!.AddFood(storage.Food);
            storage.Food = 0;

            if (ChestWindowOpen())
                ChestFoodChanged();
        }

        if (ChestWindowOpen())
            ItemRemovedFromStorage();

        UpdateCharacterInfo();

        return remaining;
    }

    /// <summary>
    /// Closes the current window (like the exit button). Returns false if only the map is shown.
    /// </summary>
    public bool ScriptCloseWindow()
    {
        // closing a window while it is still busy (animations, messages) breaks the input state
        if (!WindowActive || PopupActive || !InputEnable)
            return false;

        CloseWindow();
        return true;
    }

    /// <summary>
    /// Amount of the given item in the inventories and equipment of all party members.
    /// </summary>
    public int ScriptCountItem(uint itemIndex)
    {
        return PartyMembers.Sum(p =>
            p.Inventory.Slots.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount) +
            p.Equipment.Slots.Values.Where(s => !s.Empty && s.ItemIndex == itemIndex).Sum(s => s.Amount));
    }

    /// <summary>
    /// Combat background index of the active battle or null if no battle window is open.
    /// </summary>
    public uint? ScriptBattleBackground()
    {
        if (CurrentWindow.Window != UI.Window.Battle || CurrentWindow.WindowParameters.Length < 4)
            return null;

        return CurrentWindow.WindowParameters[3] as uint?;
    }

    /// <summary>
    /// Opens or closes the quest log. Returns true if it is open afterwards.
    /// </summary>
    public bool ScriptToggleQuestLog()
    {
        ToggleQuestLog();
        return QuestLog?.Open == true;
    }

    /// <summary>
    /// State of the given sub quest or null if there is no quest log or no such quest.
    /// </summary>
    public QuestState? ScriptQuestState(SubQuestType subQuestType)
    {
        return QuestLog?.Quests.SelectMany(q => q.SubQuests).FirstOrDefault(q => q.Type == subQuestType)?.State;
    }

    /// <summary>
    /// State of the given main quest (derived from its sub quests) or null if there is no quest log.
    /// </summary>
    public QuestState? ScriptMainQuestState(MainQuestType mainQuestType) => QuestLog?.GetMainQuestState(mainQuestType);

    /// <summary>
    /// All sub quests which are not inactive, grouped by main quest.
    /// </summary>
    public string ScriptQuests()
    {
        if (QuestLog == null)
            return "no quest log";

        var builder = new StringBuilder();

        foreach (var quest in QuestLog.Quests)
        {
            var subQuests = quest.SubQuests.Where(q => q.State != QuestState.Inactive).ToArray();

            if (subQuests.Length == 0)
                continue;

            builder.AppendLine($"  {quest.Type}");

            foreach (var subQuest in subQuests)
            {
                string amount = subQuest.MinAmount == null ? "" : $" ({subQuest.CurrentAmount}/{subQuest.MinAmount})";
                builder.AppendLine($"    {subQuest.Type}: {subQuest.State}{amount}");
            }
        }

        return builder.Length == 0 ? "  no active quests" : builder.ToString().TrimEnd();
    }

    public string ScriptState()
    {
        var builder = new StringBuilder();
        builder.Append($"Map {Map?.Index}, position {player?.Position.X + 1},{player?.Position.Y + 1}, direction {player?.Direction}");
        builder.Append($", window {CurrentWindow.Window} ({CurrentWindow.WindowParameters?.Length ?? 0} parameters), popup {PopupActive}, battle {BattleActive}, input {InputEnable}, all input disabled {allInputDisabled}, paused {paused}");
        return builder.ToString();
    }
}
