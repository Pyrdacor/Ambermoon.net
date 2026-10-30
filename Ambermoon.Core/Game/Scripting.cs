using System;
using System.Collections.Generic;
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
    sealed record ScriptConversationActions(Action<string> SayWord, Action ShowItem, Action GiveItem, Action Leave, Func<int> TakeItems, Action<uint> GiveGold, Action AskToJoin);

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

        // An unknown word can only be typed in the dictionary, which adds it to the known words.
        int wordIndex = textDictionary.Entries.FindIndex(entry => string.Compare(entry, keyword, true) == 0);

        if (wordIndex != -1)
            CurrentSavegame!.AddDictionaryWord((uint)wordIndex);

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
    /// <summary>
    /// Asks the conversation partner to join the party (like the button in the conversation).
    /// </summary>
    public string? ScriptAskToJoin()
    {
        if (!ScriptConversationActive || scriptConversation == null)
            return "no conversation is active";

        scriptConversation.AskToJoin();
        return null;
    }

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

    /// <summary>
    /// Picks a spell as the battle action of the active party member (like picking it in the spell list).
    /// </summary>
    public void ScriptPickBattleSpell(Spell spell) => PickBattleSpell(spell);

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
    /// Disables (true) or enables (false) a map event chain (1-based), e.g. for steps of other quests.
    /// </summary>
    public void ScriptSetEventBit(uint mapIndex, uint eventIndex, bool bit) => CurrentSavegame!.SetEventBit(mapIndex, eventIndex - 1, bit);

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
    /// Puts all items of the open chest or battle loot into the active party member's inventory
    /// (or only the given item, then gold and food are left as well).
    /// Returns the number of item slots which could not be taken or null if no chest is open.
    /// </summary>
    public int? ScriptTakeLoot(uint? onlyItemIndex = null)
    {
        // the chest window must be shown completely (the storage is set before the window opens)
        if (OpenStorage is not ITreasureStorage storage || !InputEnable ||
            (CurrentWindow.Window != UI.Window.Chest && CurrentWindow.Window != UI.Window.BattleLoot))
            return null;

        int partyMemberSlot = SlotFromPartyMember(CurrentPartyMember!)!.Value;
        int remaining = 0;

        foreach (var itemSlot in storage.Slots)
        {
            if (onlyItemIndex != null && itemSlot.ItemIndex != onlyItemIndex)
                continue;

            if (!itemSlot.Empty && DropItem(partyMemberSlot, null, itemSlot) != 0)
                ++remaining;
        }

        if (onlyItemIndex != null)
        {
            if (storage is Chest && CurrentWindow.Window == UI.Window.Chest)
                ItemRemovedFromStorage();

            UpdateCharacterInfo();
            return remaining;
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
    /// True if the main quest is expanded in the quest log, null if there is no quest log.
    /// </summary>
    public bool? ScriptQuestExpanded(MainQuestType mainQuestType) => QuestLog?.IsExpanded(mainQuestType);

    /// <summary>
    /// Current 1-based position of the party on the current map.
    /// </summary>
    public Position? ScriptPartyPosition => player == null || Map == null ? null : new Position(player.Position.X + 1, player.Position.Y + 1);

    public bool ScriptAllInputDisabled => allInputDisabled;

    public uint? ScriptMapIndex => Map?.Index;

    public bool ScriptIsLootWindow => CurrentWindow.Window == UI.Window.BattleLoot;

    /// <summary>
    /// True if the party could walk now (no window, popup, battle or disabled input).
    /// </summary>
    public bool ScriptCanWalk => Map != null && !DisallowMoving() && !PopupActive && !BattleActive && CanPartyMove();

    bool ScriptIsFree(int x, int y, bool allowEventWalls = false)
    {
        if (x < 0 || y < 0 || x >= Map!.Width || y >= Map.Height)
            return false;

        if (is3D)
        {
            var block = Map.Blocks[x, y];

            // walls with events are e.g. doors or cracked walls which are triggered by walking against them
            return !block.BlocksPlayer(MapManager.GetLabdataForMap(Map)) || (allowEventWalls && block.MapEventId != 0);
        }

        return Map.Tiles[x, y].AllowMovement(MapManager.GetTilesetForMap(Map), TravelType);
    }

    /// <summary>
    /// Shortest walkable path (0-based positions, without the start) to the given 1-based position
    /// of the current map or null if there is none. 2D maps use 8 directions (diagonal only if both
    /// orthogonal neighbors are free), 3D maps 4 directions. The target itself may be blocked
    /// (e.g. a door or a riddlemouth which is triggered by walking against it).
    /// </summary>
    public List<Position>? ScriptFindPath(uint targetX, uint targetY)
    {
        // walls with events (doors, ...) are only used if there is no other way
        return ScriptFindPath(targetX, targetY, false) ?? ScriptFindPath(targetX, targetY, true);
    }

    List<Position>? ScriptFindPath(uint targetX, uint targetY, bool allowEventWalls)
    {
        if (Map == null || player == null || Map.IsWorldMap)
            return null;

        var start = new Position(player.Position.X, player.Position.Y);
        var target = new Position((int)targetX - 1, (int)targetY - 1);
        var previous = new Dictionary<Position, Position> { { start, start } };
        var queue = new Queue<Position>();
        queue.Enqueue(start);
        (int X, int Y)[] directions = is3D
            ? [(0, -1), (1, 0), (0, 1), (-1, 0)]
            : [(0, -1), (1, 0), (0, 1), (-1, 0), (-1, -1), (1, -1), (1, 1), (-1, 1)];

        while (queue.Count != 0)
        {
            var position = queue.Dequeue();

            if (position == target)
            {
                var path = new List<Position>();

                for (var p = target; p != start; p = previous[p])
                    path.Add(p);

                path.Reverse();
                return path;
            }

            foreach (var (dx, dy) in directions)
            {
                var next = new Position(position.X + dx, position.Y + dy);

                if (previous.ContainsKey(next))
                    continue;

                bool free = next == target ? next.X >= 0 && next.Y >= 0 && next.X < Map.Width && next.Y < Map.Height : ScriptIsFree(next.X, next.Y, allowEventWalls);

                if (!free || (dx != 0 && dy != 0 && (!ScriptIsFree(position.X + dx, position.Y) || !ScriptIsFree(position.X, position.Y + dy))))
                    continue;

                previous[next] = position;
                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    /// Walks one step towards the given 0-based neighbor position like the player does. On 2D maps
    /// this is one tile, on 3D maps a quarter of a block (call it again until the block is reached,
    /// <paramref name="subStep"/> 0 to 3). Returns false if the movement was not possible.
    /// </summary>
    public bool ScriptStep(Position next, int subStep)
    {
        if (player == null)
            return false;

        int dx = Math.Sign(next.X - player.Position.X);
        int dy = Math.Sign(next.Y - player.Position.Y);

        if (!is3D)
            return Move2D(dx, dy);

        if (subStep == 0)
        {
            // face the next block (up = 0, right = 90, down = 180, left = 270 degrees)
            var direction = dy < 0 ? CharacterDirection.Up : dx > 0 ? CharacterDirection.Right : dy > 0 ? CharacterDirection.Down : CharacterDirection.Left;
            player3D!.TurnTowards((float)direction * 90.0f);
            CurrentSavegame!.CharacterDirection = player.Direction = direction;
        }

        return player3D!.MoveForward(Global.DistancePerBlock * 0.25f, CurrentTicks);
    }

    /// <summary>
    /// Puts the party back into the center of its current 3D block (e.g. after a blocked move).
    /// </summary>
    public void ScriptCenterOnBlock()
    {
        if (is3D && player3D != null)
            player3D.SetPosition(player3D.Position.X, player3D.Position.Y, CurrentTicks, false);
    }

    /// <summary>
    /// Absolute world map position of the party (0-based) or null if not on a world map.
    /// </summary>
    Position? ScriptWorldPosition()
    {
        if (Map?.IsWorldMap != true || renderMap2D == null || player == null)
            return null;

        var map = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y);

        return new Position(map.MapOffset.X + player.Position.X % 50, map.MapOffset.Y + player.Position.Y % 50);
    }

    /// <summary>
    /// Moves one tile on the world map towards the given 1-based position of a world map part
    /// (e.g. while flying). Returns the remaining distance (0 = reached) or null if not possible.
    /// </summary>
    public int? ScriptFlyStep(uint mapIndex, uint x, uint y)
    {
        var position = ScriptWorldPosition();
        var targetMap = MapManager.GetMap(mapIndex);

        if (position == null || targetMap == null || !targetMap.IsWorldMap)
            return null;

        int targetX = targetMap.MapOffset.X + (int)x - 1;
        int targetY = targetMap.MapOffset.Y + (int)y - 1;
        int distance = Math.Max(Math.Abs(targetX - position.X), Math.Abs(targetY - position.Y));

        if (distance == 0)
            return 0;

        if (!Move2D(Math.Sign(targetX - position.X), Math.Sign(targetY - position.Y)))
            return null;

        return distance - 1;
    }

    /// <summary>
    /// Leaves the transport (e.g. lands while flying) like the transport button does.
    /// </summary>
    public bool ScriptLand()
    {
        if (Map?.UseTravelTypes != true || TravelType == Data.Enumerations.TravelType.Walk || !ScriptCanWalk)
            return false;

        ToggleTransport();
        return true;
    }

    /// <summary>
    /// Drops all inventory items and all equipment except the body armor and the weapon in the right hand
    /// of the party member in slot 1-6 (e.g. so a strong party member can carry something heavy).
    /// Returns the number of dropped item slots or null if there is no such party member.
    /// </summary>
    public int? ScriptDropItems(int slot)
    {
        var partyMember = GetPartyMember(slot - 1);

        if (partyMember == null)
            return null;

        int dropped = 0;

        foreach (var itemSlot in partyMember.Inventory.Slots.Where(s => !s.Empty))
        {
            InventoryItemRemoved(itemSlot.ItemIndex, itemSlot.Amount, partyMember);
            itemSlot.Clear();
            dropped++;
        }

        foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
        {
            if (equipmentSlot == EquipmentSlot.None || equipmentSlot == EquipmentSlot.Body || equipmentSlot == EquipmentSlot.RightHand)
                continue;

            var itemSlot = partyMember.Equipment.Slots[equipmentSlot];

            if (itemSlot.Empty)
                continue;

            var item = ItemManager.GetItem(itemSlot.ItemIndex);
            EquipmentRemoved(partyMember, item, itemSlot.Amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));
            itemSlot.Clear();
            dropped++;
        }

        layout.UpdateCharacterStatus(partyMember);
        return dropped;
    }

    /// <summary>
    /// Opens the character stats of the party member in slot 1-6 (like clicking the portrait).
    /// </summary>
    public bool ScriptOpenStats(int slot) => !WindowOrPopupActive && GetPartyMember(slot - 1) != null && OpenPartyMember(slot - 1, false);

    /// <summary>
    /// Opens the inventory of the party member in slot 1-6 (like clicking the portrait with the right mouse button).
    /// </summary>
    public bool ScriptOpenInventory(int slot) => !WindowOrPopupActive && GetPartyMember(slot - 1) != null && OpenPartyMember(slot - 1, true);

    /// <summary>
    /// Game position in the center of an item slot (1-based) of the inventory, of a chest or of a party member portrait.
    /// </summary>
    public static Position? ScriptItemSlotPosition(string target, int slot)
    {
        int index = slot - 1;

        return target switch
        {
            "inv" => new Position(Global.InventoryX + (index % Inventory.Width) * Global.InventorySlotWidth + 8,
                Global.InventoryY + (index / Inventory.Width) * Global.InventorySlotHeight + 12),
            "chest" => new Position((index % 6 + 1) * 22 + 8, (index < 6 ? 139 : 168) + 12),
            "portrait" => Global.PartyMemberPortraitAreas[index].Center,
            _ => null
        };
    }

    /// <summary>
    /// Game position inside a text line (1-based) of the quest log, e.g. to click a main quest.
    /// </summary>
    public static Position ScriptQuestLogLinePosition(int line) => QuestLog.GetTextLinePosition(line - 1);

    /// <summary>
    /// State of the given sub quest or null if there is no quest log or no such quest.
    /// </summary>
    public QuestState? ScriptQuestState(SubQuestType subQuestType)
    {
        return QuestLog?.Quests.SelectMany(q => q.SubQuests).FirstOrDefault(q => q.Type == subQuestType)?.State;
    }

    /// <summary>
    /// Current amount of a sub quest with a counter (shown as "(amount/min amount)") or null.
    /// </summary>
    public uint? ScriptQuestAmount(SubQuestType subQuestType)
    {
        var subQuest = QuestLog?.Quests.SelectMany(q => q.SubQuests).FirstOrDefault(q => q.Type == subQuestType);

        return subQuest?.MinAmount == null ? null : subQuest.CurrentAmount;
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
