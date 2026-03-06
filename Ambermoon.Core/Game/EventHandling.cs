using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using Attribute = Ambermoon.Data.Attribute;

namespace Ambermoon;

partial class GameCore
{
    static Dictionary<uint, Chest> initialChests = [];
    static readonly Dictionary<uint, ushort> PartyMemberCharacterBits = new()
    {
        // Netsrak
        { 2,  0x2000 },
        // Mando
        { 3,  0x2001 },
        // Erik
        { 4,  0x2002 },
        // Chris
        { 5,  0x2003 },
        // Monika
        { 6,  0x2004 },
        // Tar the dark
        { 7,  0x2141 },
        // Egil
        { 8,  0x2163 },
        // Selena
        { 9,  0x22c2 },
        // Nelvin
        { 10, 0x2321 },
        // Sabine
        { 11, 0x23a0 },
        // Valdyn
        { 12, 0x2400 },
        // Targor
        { 13, 0x3320 },
        // Leonaria
        { 14, 0x3440 },
        // Gryban
        { 15, 0x35a0 },
        // Kasimir
        { 16, 0x2203 },
        // S'Ebi
        { 17, 0x3c00 }
    };
    // Some party members like Sabine appear at some different location first (e.g. Luminor's torture chamber)
    // and will spawn somewhere else later (Burnville healers). This stores the initial location bit.
    static readonly Dictionary<uint, ushort> PartyMemberInitialCharacterBits = new Dictionary<uint, ushort>
    {
        // Netsrak
        { 2,  0x2000 },
        // Mando
        { 3,  0x2001 },
        // Erik
        { 4,  0x2002 },
        // Chris
        { 5,  0x2003 },
        // Monika
        { 6,  0x2004 },
        // Tar the dark
        { 7,  0x2141 },
        // Egil
        { 8,  0x2163 },
        // Selena
        { 9,  0x22e0 },
        // Nelvin
        { 10, 0x2321 },
        // Sabine
        { 11, 0x2485 },
        // Valdyn
        { 12, 0x24e0 },
        // Targor
        { 13, 0x3320 },
        // Leonaria
        { 14, 0x3440 },
        // Gryban
        { 15, 0x35a0 },
        // Kasimir
        { 16, 0x2203 },
        // S'Ebi
        { 17, 0x3bc5 }
    };

    // If true, avoid triggering events
    bool noEvents = false;
    bool levitating = false;

    public bool Teleporting { get; set; } = false;
    /// <summary>
    /// Open chest which can be used to store items.
    /// </summary>
    internal IItemStorage? OpenStorage { get; private set; } = null;

    /// <summary>
    /// Triggers map events with the given trigger and position.
    /// </summary>
    /// <param name="trigger">Trigger</param>
    /// <param name="position">Position inside the map view</param>
    bool TriggerMapEvents(EventTrigger trigger, Position position)
    {
        if (is3D)
        {
            throw new AmbermoonException(ExceptionScope.Application, "Triggering map events by map view position is not supported for 3D maps.");
        }
        else // 2D
        {
            var tilePosition = renderMap2D.PositionToTile(position);

            if (CoreConfiguration.IsMobile)
            {
                int range = trigger == EventTrigger.Mouth ? 3 : 2;

                int xDist = Math.Abs(player2D.Position.X - tilePosition.X);
                int yDist = Math.Abs(player2D.Position.Y - tilePosition.Y);

                if (xDist > range || yDist > range)
                    return false;
            }

            return TriggerMapEvents(trigger, (uint)tilePosition.X, (uint)tilePosition.Y);
        }
    }

    internal bool TriggerMapEvents(EventTrigger trigger, uint x, uint y)
    {
        if (noEvents)
            return false;

        if (is3D)
        {
            return renderMap3D.TriggerEvents(this, trigger, x, y, CurrentSavegame);
        }
        else // 2D
        {
            return renderMap2D.TriggerEvents(player2D, trigger, x, y, MapManager,
                CurrentTicks, CurrentSavegame);
        }
    }

    internal bool TestUseItemMapEvent(uint itemIndex, out uint x, out uint y, out EventType eventType)
    {
        x = (uint)player.Position.X;
        y = (uint)player.Position.Y;
        uint eventX = x;
        uint eventY = y;
        var @event = is3D ? Map.GetEvent(x, y, CurrentSavegame) : renderMap2D.GetEvent(x, y, CurrentSavegame);
        var map = is3D ? Map : renderMap2D.GetMapFromTile(x, y);

        // In the remake we allow using keys (including lockpick) to open a nearby chest/door.
        // This will only happen if nearby chests/doors can be opened with it. The chest or
        // door screen is opened and the item is used automatically. If there is a scrollable
        // text, the key using is postponed to after reading through it.
        var item = ItemManager.GetItem(itemIndex);
        bool isKey = item.Type == ItemType.Key;

        bool IsMatchingKeyEvent()
        {
            if (!isKey)
                return false;

            if (@event is ChestEvent chestEvent && CurrentSavegame.IsChestLocked(chestEvent.RealChestIndex - 1))
                return true;
            if (@event is DoorEvent doorEvent && CurrentSavegame.IsDoorLocked(doorEvent.DoorIndex))
                return true;

            return false;
        }

        bool TestEvent(out EventType eventType)
        {
            eventType = @event?.Type ?? EventType.Invalid;

            if (IsMatchingKeyEvent())
                return true;

            if (@event is not ConditionEvent conditionEvent)
                return false;

            if (conditionEvent.TypeOfCondition == ConditionEvent.ConditionType.UseItem &&
                conditionEvent.ObjectIndex == itemIndex)
                return true;

            bool lastEventStatus = true;
            var trigger = (EventTrigger)((uint)EventTrigger.Item0 + itemIndex);

            @event = EventExtensions.ExecuteEvent(conditionEvent, map, this, ref trigger, eventX, eventY, ref lastEventStatus, out bool _, out var _);

            return TestEvent(out eventType);
        }

        if (TestEvent(out eventType))
            return true;

        var mapWidth = Map.IsWorldMap ? int.MaxValue : Map.Width;
        var mapHeight = Map.IsWorldMap ? int.MaxValue : Map.Height;

        if (is3D)
        {
            camera3D.GetForwardPosition(Global.DistancePerBlock, out float px, out float pz, false, false);
            var position = Geometry.Geometry.CameraToBlockPosition(Map, px, pz);

            if (position != player.Position &&
                position.X >= 0 && position.X < Map.Width &&
                position.Y >= 0 && position.Y < Map.Height &&
                renderMap3D.IsBlockingPlayer(position))
            {
                // Only check the forward position if it is blocking.
                // Sometimes use item events might be placed on walls etc.
                // Otherwise don't check the forward position as the
                // player can walk on the empty tile and use the item there.
                x = (uint)position.X;
                y = (uint)position.Y;
                @event = Map.GetEvent(x, y, CurrentSavegame);
                eventX = x;
                eventY = y;

                return TestEvent(out eventType);
            }
            else
            {
                return false;
            }
        }
        else
        {
            Position[] offsets = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
            bool IsOffsetInvalid(Position offset, uint x, uint y) => x + offset.X < 0 || x + offset.X >= mapWidth || y + offset.Y < 0 || y + offset.Y >= mapHeight;
            int shift = (int)player.Direction;
            offsets = offsets.Skip(shift).Concat(offsets.Take(shift)).ToArray();

            foreach (var offset in offsets)
            {
                if (IsOffsetInvalid(offset, x, y))
                    continue;

                if (TestEventAtOffset(offset, x, y, out eventType))
                {
                    x = (uint)(x + offset.X);
                    y = (uint)(y + offset.Y);
                    return true;
                }
            }

            return false;

            bool TestEventAtOffset(Position offset, uint x, uint y, out EventType eventType)
            {
                eventX = (uint)(x + offset.X);
                eventY = (uint)(y + offset.Y);
                @event = renderMap2D.GetEvent(eventX, eventY, CurrentSavegame);

                return TestEvent(out eventType);
            }
        }
    }

    internal bool TriggerMapEvents(EventTrigger? trigger)
    {
        if (noEvents)
            return false;

        if (trigger == null)
        {
            // If null it was triggered by crosshair cursor. We test mouth, eye and hand in this case.
            if (TriggerMapEvents(EventTrigger.Mouth))
                return true;
            if (TriggerMapEvents(EventTrigger.Eye))
                return true;
            if (TriggerMapEvents(EventTrigger.Hand))
                return true;
            return false;
        }

        bool consumed = TriggerMapEvents(trigger.Value, (uint)player.Position.X, (uint)player.Position.Y);

        if (is3D)
        {
            if (consumed)
                return true;

            // In 3D we might trigger adjacent tile events.
            if (trigger != EventTrigger.Move)
            {
                camera3D.GetForwardPosition(Global.DistancePerBlock, out float x, out float z, false, false);
                var position = Geometry.Geometry.CameraToBlockPosition(Map, x, z);

                if (position != player.Position &&
                    position.X >= 0 && position.X < Map.Width &&
                    position.Y >= 0 && position.Y < Map.Height)
                {
                    return TriggerMapEvents(trigger.Value, (uint)position.X, (uint)position.Y);
                }
            }
        }
        else if (trigger >= EventTrigger.Item0)
        {
            if (consumed)
                return true;

            // In 2D we might trigger adjacent tile events when items are used.
        }

        return false;
    }

    private void GetEventIndex(Position position, out uint? eventIndex, out uint? mapIndex)
    {
        if (Map!.Type == MapType.Map3D)
        {
            eventIndex = Map.GetEventIndex((uint)position.X, (uint)position.Y, CurrentSavegame);
            mapIndex = Map.Index;
        }
        else
        {
            uint x = (uint)position.X;
            uint y = (uint)position.Y;

            var map = Map;

            if (map.IsWorldMap)
            {
                map = renderMap2D.GetMapFromTile(x, y);
                x %= 50;
                y %= 50;
            }

            eventIndex = map.GetEventIndex(x, y, CurrentSavegame);
            mapIndex = map.Index;
        }
    }


    #region Teleport

    public bool Teleport(uint mapIndex, uint x, uint y, CharacterDirection direction, out bool blocked, bool force = false)
    {
        blocked = false;

        if (!ingame || layout.OptionMenuOpen || BattleActive || (!force && (WindowActive || layout.PopupActive)))
            return false;

        if (mapIndex == 0)
            mapIndex = Map.Index;

        var newMap = MapManager.GetMap(mapIndex);

        if (newMap == null)
        {
            blocked = true;
            return false;
        }

        if (!newMap.UseTravelTypes && (TravelType != TravelType.Walk && TravelType != TravelType.Swim))
            return false;

        bool mapChange = newMap.Index != Map.Index;
        var player = is3D ? (IRenderPlayer)player3D : player2D;
        bool mapTypeChanged = Map.Type != newMap.Type;

        // The position (x, y) is 1-based in the data so we subtract 1.
        // If the position is 0,0 the current position should be used.
        uint newX = x == 0 ? (uint)player.Position.X : x - 1;
        uint newY = y == 0 ? (uint)player.Position.Y : y - 1;

        if (newMap.Type == MapType.Map2D)
        {
            // Note: There are cases where teleporting onto a blocking tile is performed and allowed.
            // One example is the Inn in Newlake where you are teleported on top of a table.
            // In this case we force the teleport.
            if (!force && !newMap.Tiles[newX, newY].AllowMovement(MapManager.GetTilesetForMap(newMap), TravelType, true, true))
            {
                blocked = true;
                return false;
            }
        }
        else
        {
            // Note: Normally we won't force teleport to a blocking 3D block as the player would
            // stuck in the wall. But the game logic might use change tile events to remove walls.
            // So we hope that the game only teleports to blocking tiles if it is removed on map enter.
            if (!force && newMap.Blocks[newX, newY].BlocksPlayer(MapManager.GetLabdataForMap(newMap)))
            {
                blocked = true;
                return false;
            }
        }

        if (!is3D && !mapChange)
        {
            renderMap2D.ScrollToPlayer(newX, newY);
        }

        if (direction == CharacterDirection.Keep)
            direction = PlayerDirection;

        player.MoveTo(newMap, newX, newY, CurrentTicks, true, direction, UpdateMapNameAndLight);
        this.player.Position.X = RenderPlayer.Position.X;
        this.player.Position.Y = RenderPlayer.Position.Y;
        // This will update the appearance.
        TravelType = TravelType;

        void UpdateMapNameAndLight(Map map)
        {
            if (mapChange && !WindowActive)
            {
                UpdateMapName(map);
                UpdateLight(true, false, false, map);
            }
        }

        if (!mapTypeChanged)
        {
            PlayerMoved(mapChange);
        }

        if (mapChange && !WindowActive)
        {
            // Color of the filled upper right area may need update cause of palette change.
            mapViewRightFillArea.Color = GetUIColor(28);
        }

        if (!mapChange) // Otherwise the map change handler takes care of this
            ResetMoveKeys();

        if (!WindowActive && !layout.PopupActive && !TravelType.IgnoreEvents())
        {
            // Trigger events after map transition
            TriggerMapEvents(EventTrigger.Move, (uint)this.player.Position.X,
                (uint)this.player.Position.Y);
        }

        return true;
    }

    internal void Teleport(TeleportEvent teleportEvent, uint x, uint y)
    {
        Teleporting = true;

        uint targetX = teleportEvent.X == 0 ? x + 1 : teleportEvent.X;
        uint targetY = teleportEvent.Y == 0 ? y + 1 : teleportEvent.Y;

        ResetMoveKeys();
        ResetMapCharacterInteraction(Map);

        if (PopupActive)
            layout.ClosePopup(false, true);

        void RunTransition()
        {
            levitating = false;
            Teleport(teleportEvent.MapIndex, targetX, targetY, teleportEvent.Direction, out _, true);

            if (Map.IsWorldMap && teleportEvent.NewTravelType != null && teleportEvent.NewTravelType != TravelType)
                TravelType = teleportEvent.NewTravelType.Value;

            if (TravelType.UsesMapObject() &&
                !CheckTeleportDestination(teleportEvent.MapIndex, targetX, targetY))
            {
                ToggleTransport();
                var transport = GetTransportAtPlayerLocation(out int? index);
                CurrentSavegame.TransportLocations[index.Value] = null;
                renderMap2D.RemoveTransport(index.Value);
            }

            Teleporting = false;
        }

        var transition = teleportEvent.Transition;

        if (transition == TeleportEvent.TransitionType.MapChange && levitating)
            transition = TeleportEvent.TransitionType.Climbing;

        switch (transition)
        {
            case TeleportEvent.TransitionType.Teleporter:
                RunTransition();
                break;
            case TeleportEvent.TransitionType.WindGate:
                if (CurrentSavegame.IsSpecialItemActive(SpecialItemPurpose.WindChain))
                    RunTransition();
                else
                    Teleporting = false;
                break;
            case TeleportEvent.TransitionType.Falling:
            {
                if (!is3D)
                {
                    Fade(RunTransition);
                }
                else
                {
                    Pause();
                    Fall(x, y, () => Fade(() =>
                    {
                        noEvents = true;
                        RunTransition();
                        MoveVertically(false, true, () =>
                        {
                            Resume();
                            noEvents = false;
                            TriggerMapEvents(EventTrigger.Move);
                        });
                    }));
                }
                break;
            }
            case TeleportEvent.TransitionType.Climbing:
                if (!is3D)
                {
                    Fade(RunTransition);
                }
                else
                {
                    Pause();
                    Climb(() => Fade(() =>
                    {
                        noEvents = true;
                        RunTransition();
                        MoveVertically(true, true, () =>
                        {
                            Resume();
                            noEvents = false;
                            TriggerMapEvents(EventTrigger.Move);
                        });
                    }));
                }
                break;
            case TeleportEvent.TransitionType.Outro:
                Teleporting = false;
                Hook_Outro();
                break;
            default:
                Fade(RunTransition);
                break;
        }
    }

    bool CheckTeleportDestination(uint mapIndex, uint x, uint y)
    {
        if (mapIndex == 0)
            mapIndex = Map!.Index;

        var newMap = MapManager.GetMap(mapIndex);

        if (newMap == null)
            return false;

        uint newX = x == 0 ? (uint)player!.Position.X : x - 1;
        uint newY = y == 0 ? (uint)player!.Position.Y : y - 1;

        if (newMap.Type == MapType.Map3D)
            return !newMap.Blocks[newX, newY].BlocksPlayer(MapManager.GetLabdataForMap(newMap));
        else
            return newMap.Tiles[newX, newY].AllowMovement(MapManager.GetTilesetForMap(newMap), TravelType);
    }

    #endregion


    #region Chests & Doors

    internal Chest GetChest(uint index) => CurrentSavegame.Chests[index];
    internal Chest GetInitialChest(uint index)
    {
        if (initialChests.Count == 0)
        {
            try
            {
                var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);
                initialChests = initialSavegame.Chests;
            }
            catch
            {
                // ignore
            }
        }

        return initialChests[index];
    }

    void RefillChest(uint chestIndex)
    {
        // If not saved, restore initial content
        var initialChest = GetInitialChest(chestIndex);

        if (initialChest != null && initialChest.Slots.OfType<ItemSlot>().Sum(item => item.Amount) +
            initialChest.Gold + initialChest.Food == 1)
        {
            var chest = GetChest(chestIndex);

            chest.Gold = initialChest.Gold;
            chest.Food = initialChest.Food;

            for (int y = 0; y < Chest.SlotRows; ++y)
            {
                for (int x = 0; x < Chest.SlotsPerRow; ++x)
                    chest.Slots[x, y].Replace(initialChest.Slots[x, y]);
            }
        }
    }

    internal void ChestClosed()
    {
        // This is called by manually close the chest window via the Exit button
        var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
        var position = (Position)currentWindow.WindowParameters[4];

        CloseWindow(() =>
        {
            uint chestIndex = chestEvent.RealChestIndex;

            if (chestEvent.NoSave)
            {
                RefillChest(chestIndex);
            }

            if (chestEvent.Next != null)
            {
                Map.TriggerEventChain(this, EventTrigger.Always, (uint)(position?.X ?? 0),
                    (uint)(position?.Y ?? 0), chestEvent.Next, false);
            }
        });
    }

    void ChestRemoved()
    {
        var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
        var position = (Position)currentWindow.WindowParameters[4];

        CloseWindow(() =>
        {
            uint chestIndex = chestEvent.RealChestIndex;

            if (chestEvent.NoSave)
            {
                RefillChest(chestIndex);
            }
            else if (chestEvent.CloseWhenEmpty)
            {
                var chest = GetChest(chestEvent.RealChestIndex);

                if (chest.Empty)
                {
                    GetEventIndex(position, out var eventIndex, out var mapIndex);

                    if (eventIndex != null)
                        CurrentSavegame.SetEventBit(mapIndex.Value, eventIndex.Value - 1, true);
                }
            }

            if (chestEvent.Next != null)
            {
                Map.TriggerEventChain(this, EventTrigger.Always, (uint)(position?.X ?? 0),
                    (uint)(position?.Y ?? 0), chestEvent.Next, true);
            }
        });
    }

    internal void ItemRemovedFromStorage()
    {
        if (OpenStorage is Chest chest)
        {
            if (!chest.IsBattleLoot)
            {
                if (chest.Empty)
                {
                    if (chest.Type == ChestType.Chest)
                        layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                    // If a chest has AllowsItemDrop = false this
                    // means it is removed when it is empty.
                    if (!chest.AllowsItemDrop)
                        ChestRemoved();
                }
                else
                {
                    if (chest.Type == ChestType.Chest)
                        layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                }
            }
        }
        else if (OpenStorage is Merchant merchant)
        {
            // TODO: Show message that he doesn't sell anything if no item is left
        }
    }

    internal void ChestGoldChanged()
    {
        var chest = (OpenStorage as Chest)!;

        if (chest.Gold > 0)
        {
            if (chest.Type == ChestType.Chest)
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
            ShowTextPanel(CharacterInfo.ChestGold, true,
                $"{DataNameProvider.GoldName}^{chest.Gold}", new Rect(111, 104, 43, 15));
        }
        else
        {
            HideTextPanel(CharacterInfo.ChestGold);

            if (chest.Empty && !chest.IsBattleLoot)
            {
                if (chest.Type == ChestType.Chest)
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                if (!chest.AllowsItemDrop)
                    ChestRemoved();
            }
        }
    }

    internal void ChestFoodChanged()
    {
        var chest = (OpenStorage as Chest)!;

        if (chest.Food > 0)
        {
            if (chest.Type == ChestType.Chest)
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
            ShowTextPanel(CharacterInfo.ChestFood, true,
                $"{DataNameProvider.FoodName}^{chest.Food}", new Rect(260, 104, 43, 15));
        }
        else
        {
            HideTextPanel(CharacterInfo.ChestFood);

            if (chest.Empty && !chest.IsBattleLoot)
            {
                if (chest.Type == ChestType.Chest)
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                if (!chest.AllowsItemDrop)
                    ChestRemoved();
            }
        }
    }

    void ShowLoot(ITreasureStorage storage, string? initialText, Action initialTextClosedEvent, ChestEvent? chestEvent = null,
        bool triggerFollowEvents = false, uint eventX = 0, uint eventY = 0)
    {
        if (chestEvent?.Next != null && triggerFollowEvents)
        {
            var oldCloseWindowHandler = closeWindowHandler;
            closeWindowHandler = backToMap =>
            {
                oldCloseWindowHandler?.Invoke(backToMap);

                if (backToMap)
                    EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, eventX, eventY, chestEvent.Next, true);
            };
        }
        OpenStorage = storage;
        OpenStorage.AllowsItemDrop = chestEvent == null ? false : !chestEvent.CloseWhenEmpty;
        layout.SetLayout(LayoutType.Items);
        layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
        var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
        itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
        var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, storage.Slots.ToList(),
            OpenStorage.AllowsItemDrop, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
        itemGrid.Refresh();
        layout.AddItemGrid(itemGrid);
        bool pile = storage.IsBattleLoot || (storage is Chest chest && chest.Type == ChestType.Junk);

        if (pile)
        {
            layout.Set80x80Picture(Picture80x80.Treasure);
        }
        else if (storage.Empty)
        {
            layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);
        }
        else
        {
            layout.Set80x80Picture(Picture80x80.ChestOpenFull);
        }

        itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
        {
            if (updateSlot)
            {
                int column = slotIndex % Chest.SlotsPerRow;
                int row = slotIndex / Chest.SlotsPerRow;
                storage.Slots[column, row].Remove(amount);
            }
        };
        itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
        {
            if (!pile)
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
        };

        if (storage.Gold > 0)
        {
            ShowTextPanel(CharacterInfo.ChestGold, true,
                $"{DataNameProvider.GoldName}^{storage.Gold}", new Rect(111, 104, 43, 15));
        }

        if (storage.Food > 0)
        {
            ShowTextPanel(CharacterInfo.ChestFood, true,
                $"{DataNameProvider.FoodName}^{storage.Food}", new Rect(260, 104, 43, 15));
        }

        if (initialText != null)
        {
            layout.ShowClickChestMessage(initialText, initialTextClosedEvent, true);
        }
    }

    internal bool ShowChest(ChestEvent chestEvent, bool foundTrap, bool disarmedTrap, Map map,
        Position position, bool fromEvent, bool triggerFollowEvents = false, uint usedItem = 0)
    {
        var chest = GetChest(chestEvent.RealChestIndex);
        var keyUser = usedItem != 0 ? CurrentInventory : null;

        if (chestEvent.CloseWhenEmpty && chest.Empty)
        {
            if (!chestEvent.NoSave)
            {
                GetEventIndex(position, out var eventIndex, out var mapIndex);

                if (eventIndex != null)
                    CurrentSavegame.SetEventBit(mapIndex.Value, eventIndex.Value - 1, true);
            }

            return false; // Chest has gone due to looting
        }

        chest.Type = chestEvent.CloseWhenEmpty ? ChestType.Junk : ChestType.Chest;

        void OpenChest()
        {
            bool changed = !chest.Equals(GetInitialChest(chestEvent.RealChestIndex), false);
            string initialText = changed ? null : map != null && fromEvent && chestEvent.TextIndex != 255 ?
                map.GetText((int)chestEvent.TextIndex, DataNameProvider.TextBlockMissing) : null;
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Chest, chestEvent, foundTrap, disarmedTrap, map, position, triggerFollowEvents);
            CursorType = CursorType.Sword;
            ResetMapCharacterInteraction(map ?? Map, true);

            if (chestEvent.LockpickingChanceReduction != 0 && CurrentSavegame.IsChestLocked(chestEvent.RealChestIndex - 1))
            {
                ShowLocked(Picture80x80.ChestClosed, () =>
                {
                    CurrentSavegame.UnlockChest(chestEvent.RealChestIndex - 1);
                    currentWindow.Window = Window.Chest; // This avoids returning to locked screen when closing chest window.
                    ExecuteNextUpdateCycle(() => ShowChest(chestEvent, false, false, map, position, true, true));
                }, null, chestEvent.KeyIndex, chestEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
                chestEvent.UnlockFailedEventIndex == 0xffff ? null : () => map.TriggerEventChain(this, EventTrigger.Always,
                (uint)position.X, (uint)position.Y, map.Events[(int)chestEvent.UnlockFailedEventIndex], true),
                () =>
                {
                    if (chestEvent.Next != null)
                        map.TriggerEventChain(this, EventTrigger.Always, (uint)position.X, (uint)position.Y, chestEvent.Next, false);
                }, usedItem, keyUser);
            }
            else
            {
                ShowLoot(chest, initialText, null, chestEvent, triggerFollowEvents, (uint)position.X, (uint)position.Y);
            }
        }

        if (CurrentWindow.Window == Window.Chest)
            OpenChest();
        else if (CurrentWindow.Window == Window.Inventory && LastWindow.Window == Window.Chest)
            CloseWindow(OpenChest);
        else
            Fade(OpenChest);

        return true;
    }

    internal bool ShowDoor(DoorEvent doorEvent, bool foundTrap, bool disarmedTrap, Map map, uint x, uint y,
        bool fromEvent, bool moved, uint usedItem = 0)
    {
        var keyUser = usedItem != 0 ? CurrentInventory : null;
        if (!CurrentSavegame.IsDoorLocked(doorEvent.DoorIndex))
            return false;

        void ShowDoorAction()
        {
            string initialText = fromEvent && doorEvent.TextIndex != 255 ?
                map.GetText((int)doorEvent.TextIndex, DataNameProvider.TextBlockMissing) : null;
            string unlockText = doorEvent.UnlockTextIndex != 255 ?
                map.GetText((int)doorEvent.UnlockTextIndex, DataNameProvider.TextBlockMissing) : null;
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Door, doorEvent, foundTrap, disarmedTrap, map, x, y, moved);
            ShowLocked(Picture80x80.Door, () =>
            {
                if (moved && !is3D)
                {
                    player2D.Position.X = player.Position.X = (int)x;
                    player2D.Position.Y = player.Position.Y = (int)y;
                    player2D.UpdateAppearance(CurrentTicks);
                }
                CurrentSavegame.UnlockDoor(doorEvent.DoorIndex);
                if (unlockText != null)
                {
                    layout.ShowClickChestMessage(unlockText, Close);
                }
                else
                {
                    Close();
                }
                void Close()
                {
                    CloseWindow(() =>
                    {
                        if (is3D)
                        {
                            // 3D doors that have automap type wall seem to be removed after opening.
                            // This is at least the case for the Newlake library bookshelf.
                            var wallIndex = map.Blocks[x, y].WallIndex;
                            var labdata = MapManager.GetLabdataForMap(map);

                            if (wallIndex != 0 &&
                                labdata.Walls[((int)wallIndex - 1) % labdata.Walls.Count].AutomapType == AutomapType.Wall)
                            {
                                RemoveMapTile(map, x, y, true);
                            }
                        }
                        // If this is a direct map event it is deactivated when the door is opened.
                        if (doorEvent.Next == null)
                        {
                            int eventIndex = map.EventList.IndexOf(doorEvent);
                            if (eventIndex != -1)
                                CurrentSavegame.ActivateEvent(map.Index, (uint)eventIndex, false);
                        }
                        else
                        {
                            EventExtensions.TriggerEventChain(map ?? Map, this, EventTrigger.Always, x, y, doorEvent.Next, true);
                        }
                    });
                }
            }, initialText, doorEvent.KeyIndex, doorEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
            doorEvent.UnlockFailedEventIndex == 0xffff ? null : () => map.TriggerEventChain(this, EventTrigger.Always,
                x, y, map.Events[(int)doorEvent.UnlockFailedEventIndex], true),
            () =>
            {
                if (doorEvent.Next != null)
                    map.TriggerEventChain(this, EventTrigger.Always, x, y, doorEvent.Next, false);
            }, usedItem, keyUser);
        }

        if (CurrentWindow.Window == Window.Door)
            ShowDoorAction();
        else if (CurrentWindow.Window == Window.Inventory && LastWindow.Window == Window.Door)
            CloseWindow(ShowDoorAction);
        else
            Fade(ShowDoorAction);

        return true;
    }

    void ShowLocked(Picture80x80 picture80X80, Action openedAction, string initialMessage,
        uint keyIndex, uint lockpickingChanceReduction, bool foundTrap, bool disarmedTrap, Action failedAction,
        Action abortAction, uint usedKey = 0, PartyMember? keyUser = null)
    {
        layout.SetLayout(LayoutType.Items);
        layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
        var itemArea = new Rect(16, 139, 151, 53);
        var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
        itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
        var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat((ItemSlot)null, 24).ToList(),
            false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
        layout.AddItemGrid(itemGrid);
        itemGrid.Disabled = true;
        layout.Set80x80Picture(picture80X80);
        bool hasTrap = failedAction != null;
        bool chest = picture80X80 == Picture80x80.ChestClosed;

        layout.EnableButton(1, CurrentPartyMember.Inventory.Slots.Any(s => s?.Empty == false));
        layout.EnableButton(3, !foundTrap);
        layout.EnableButton(6, foundTrap && !disarmedTrap);

        void PlayerSwitched()
        {
            itemGrid.HideTooltip();
            itemGrid.Disabled = true;
            layout.ShowChestMessage(null);
            UntrapMouse();
            CursorType = CursorType.Sword;
            inputEnable = true;
            layout.EnableButton(1, CurrentPartyMember.Inventory.Slots.Any(s => s?.Empty == false));
        }

        ActivePlayerChanged = null;
        ActivePlayerChanged += PlayerSwitched;
        closeWindowHandler = _ => ActivePlayerChanged -= PlayerSwitched;

        void Exit()
        {
            CloseWindow(abortAction);
        }

        void StartUseItems()
        {
            if (chest)
                layout.ShowChestMessage(DataNameProvider.WhichItemToOpenChest, TextAlign.Left);
            else
                layout.ShowChestMessage(DataNameProvider.WhichItemToOpenDoor, TextAlign.Left);

            itemGrid.Disabled = false;
            itemGrid.DisableDrag = true;
            itemGrid.Initialize([.. CurrentPartyMember.Inventory.Slots], false);
            TrapMouse(itemArea);
            SetupRightClickAbort();
        }

        void SetupRightClickAbort()
        {
            nextClickHandler = buttons =>
            {
                if (buttons == MouseButtons.Right)
                {
                    itemGrid.HideTooltip();
                    itemGrid.Disabled = true;
                    layout.ShowChestMessage(null);
                    UntrapMouse();
                    CursorType = CursorType.Sword;
                    inputEnable = true;
                    return true;
                }

                return false;
            };
        }

        void Unlocked(bool withLockpick, Action finishAction)
        {
            layout.ShowClickChestMessage(withLockpick ? (chest ? DataNameProvider.UnlockedChestWithLockpick : DataNameProvider.UnlockedDoorWithLockpick)
                : (chest ? DataNameProvider.HasOpenedChest : DataNameProvider.HasOpenedDoor), finishAction);
        }

        itemGrid.ItemClicked += (ItemGrid _, int _, ItemSlot itemSlot) =>
        {
            UntrapMouse();
            nextClickHandler = null;
            layout.ShowChestMessage(null);
            CheckKey(itemSlot, true, true);
        };

        void CheckKey(ItemSlot itemSlot, bool showItemsAfterwards, bool removeItemFromCharacter)
        {
            bool isActivePlayerItem = removeItemFromCharacter || keyUser.Index == CurrentPartyMember.Index;
            var targetPlayer = isActivePlayerItem ? CurrentPartyMember : keyUser;

            StartSequence();
            itemGrid.HideTooltip();
            var targetPosition = chest ? new Position(28, 76) : new Position(73, 102);
            itemGrid.PlayMoveAnimation(itemSlot, targetPosition, () =>
            {
                bool canOpen = keyIndex == itemSlot.ItemIndex || (keyIndex == 0 && itemSlot.ItemIndex == LockpickItemIndex);
                var item = layout.GetItem(itemSlot);
                var itemIndex = itemSlot.ItemIndex;
                item.ShowItemAmount = false;

                itemGrid.PlayShakeAnimation(itemSlot, () =>
                {
                    EndSequence();
                    if (canOpen)
                    {
                        Unlocked(itemSlot.ItemIndex == LockpickItemIndex, () =>
                        {
                            var itemInfo = ItemManager.GetItem(itemSlot.ItemIndex);
                            if (itemInfo.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
                            {
                                ItemAnimation.Play(this, renderView, ItemAnimation.Type.Consume, targetPosition, () =>
                                {
                                    AddTimedEvent(TimeSpan.FromMilliseconds(250), () =>
                                    {
                                        itemGrid.ResetAnimation(itemSlot);
                                        item.ShowItemAmount = false;
                                        item.Visible = false;
                                        itemGrid.HideTooltip();
                                        itemGrid.Disabled = true;
                                        EndSequence();
                                        openedAction?.Invoke();
                                    });
                                }, TimeSpan.FromMilliseconds(50));
                                AddTimedEvent(TimeSpan.FromMilliseconds(250), () =>
                                {
                                    item.Visible = false;
                                    uint itemIndex = itemSlot.ItemIndex;
                                    itemSlot.Remove(1);

                                    if (removeItemFromCharacter)
                                        InventoryItemRemoved(itemIndex, 1, targetPlayer);
                                });
                            }
                            else
                            {
                                // Just move back
                                StartSequence();
                                itemGrid.HideTooltip();
                                itemGrid.PlayMoveAnimation(itemSlot, null, () =>
                                {
                                    itemGrid.ResetAnimation(itemSlot);
                                    item.ShowItemAmount = false;
                                    item.Visible = false;
                                    itemGrid.HideTooltip();
                                    itemGrid.Disabled = true;
                                    EndSequence();
                                    openedAction?.Invoke();
                                });
                            }
                        });
                    }
                    else
                    {
                        if (itemSlot.ItemIndex == LockpickItemIndex) // Lockpick
                        {
                            AddTimedEvent(TimeSpan.FromMilliseconds(50), () => item.Visible = false);
                            ItemAnimation.Play(this, renderView, ItemAnimation.Type.Destroy, targetPosition, () =>
                            {
                                layout.ShowClickChestMessage(DataNameProvider.LockpickBreaks, () =>
                                {
                                    uint itemIndex = itemSlot.ItemIndex;
                                    itemSlot.Remove(1);

                                    if (removeItemFromCharacter)
                                        InventoryItemRemoved(itemIndex, 1, targetPlayer);

                                    if (itemSlot.Amount > 0)
                                    {
                                        StartSequence();
                                        itemGrid.HideTooltip();
                                        itemGrid.PlayMoveAnimation(itemSlot, itemGrid.GetSlotPosition(itemGrid.SlotFromItemSlot(itemSlot)), () =>
                                        {
                                            itemGrid.ResetAnimation(itemSlot);
                                            EndSequence();

                                            if (showItemsAfterwards)
                                                StartUseItems();
                                        });
                                    }
                                    else
                                    {
                                        // This is the only case where an item is removed and the lock is not opened.
                                        // We have to check if this was the last item and the player is still able to
                                        // use items.
                                        if (!CurrentPartyMember.Inventory.Slots.Any(s => s?.Empty == false))
                                        {
                                            layout.EnableButton(1, false);
                                            itemGrid.HideTooltip();
                                            itemGrid.Disabled = true;
                                            layout.ShowChestMessage(null);
                                            UntrapMouse();
                                        }
                                        else if (showItemsAfterwards)
                                        {

                                            itemGrid.ResetAnimation(itemSlot);
                                            item.ShowItemAmount = true;
                                            item.Visible = true;

                                            StartUseItems();
                                        }
                                        else
                                        {
                                            itemGrid.HideTooltip();
                                            itemGrid.Disabled = true;
                                            layout.ShowChestMessage(null);
                                            UntrapMouse();
                                        }
                                    }
                                });
                            }, TimeSpan.FromMilliseconds(50), null, item);
                        }
                        else
                        {
                            layout.ShowClickChestMessage(chest ? DataNameProvider.ThisItemDoesNotOpenChest : DataNameProvider.ThisItemDoesNotOpenDoor, () =>
                            {
                                StartSequence();
                                itemGrid.HideTooltip();
                                itemGrid.PlayMoveAnimation(itemSlot, null, () =>
                                {
                                    itemGrid.ResetAnimation(itemSlot);
                                    EndSequence();

                                    if (showItemsAfterwards)
                                        StartUseItems();
                                    else
                                    {
                                        itemGrid.HideTooltip();
                                        itemGrid.Disabled = true;
                                        layout.ShowChestMessage(null);
                                        UntrapMouse();
                                    }

                                    if (!removeItemFromCharacter)
                                    {
                                        var item = ItemManager.GetItem(itemIndex);

                                        // If the item was used and therefore consumed/destroyed, we have to add it back. 
                                        if (item.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
                                        {
                                            targetPlayer.AddItem(itemIndex, item.Flags.HasFlag(ItemFlags.Stackable));
                                            InventoryItemAdded(itemIndex, 1, targetPlayer);

                                            if (isActivePlayerItem)
                                                layout.EnableButton(1, true);
                                        }
                                    }
                                });
                            });
                        }
                    }
                });
            });
        }

        // Lockpick button
        layout.AttachEventToButton(0, () =>
        {
            // TODO: Can locks theoretically be lockpicked if they need a key? I guess in Ambermoon all locks with key have a lockpickingChanceReduction of 100%.
            //       But what would happen if this value was below 100% for such doors? For now we allow lockpicking those doors as we don't check for key index.
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.LockPicking].TotalCurrentValue, 100) - (int)lockpickingChanceReduction;

            if (chance <= 0 || RollDice100() >= chance)
            {
                // Failed
                // Note: The trap is triggered by the follow-up event (if given) but only if a dice roll against DEX fails.
                bool trapDisarmed = (bool)currentWindow.WindowParameters[2]; // Don't use the parameter as we could have disarmed it just yet.
                if (hasTrap && !trapDisarmed && RollDice100() >= CurrentPartyMember.Attributes[Attribute.Dexterity].TotalCurrentValue)
                {
                    CloseWindow(failedAction);
                }
                else
                {
                    layout.ShowClickChestMessage(DataNameProvider.UnableToPickTheLock);
                }
            }
            else
            {
                // Success
                Unlocked(false, openedAction);
            }
        });
        // Use item button
        layout.AttachEventToButton(1, StartUseItems);
        // Find trap button
        layout.AttachEventToButton(3, () =>
        {
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.FindTraps].TotalCurrentValue, 100);

            if (hasTrap && chance > 0 && RollDice100() < chance)
            {
                layout.ShowClickChestMessage(DataNameProvider.FindTrap);
                currentWindow.WindowParameters[1] = true; // Found trap flag
                layout.EnableButton(3, false);
                layout.EnableButton(6, true);
            }
            else
            {
                layout.ShowClickChestMessage(DataNameProvider.DoesNotFindTrap);
            }
        });
        // Disarm trap button
        layout.AttachEventToButton(6, () =>
        {
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.DisarmTraps].TotalCurrentValue, 100); // TODO: Is there a "find trap" reduction as well?

            if (chance <= 0 || RollDice100() >= chance)
            {
                if (RollDice100() >= CurrentPartyMember.Attributes[Attribute.Dexterity].TotalCurrentValue)
                {
                    CloseWindow(failedAction);
                }
                else
                {
                    layout.ShowClickChestMessage(DataNameProvider.UnableToDisarmTrap);
                }
            }
            else
            {
                // Trap was disarmed
                layout.ShowClickChestMessage(DataNameProvider.DisarmTrap);
                currentWindow.WindowParameters[2] = true; // Disarmed trap flag
                layout.EnableButton(6, false);
            }
        });
        // Exit button
        layout.AttachEventToButton(2, Exit);

        if (!string.IsNullOrWhiteSpace(initialMessage))
            layout.ShowClickChestMessage(initialMessage, CheckImmediateOpen);
        else
            CheckImmediateOpen();

        void CheckImmediateOpen()
        {
            if (usedKey == 0)
                return;

            StartSequence();
            var targetPosition = chest ? new Position(28, 76) : new Position(73, 102);
            itemGrid.Disabled = false;
            itemGrid.DisableDrag = true;
            var itemSlot = new ItemSlot { ItemIndex = usedKey, Amount = 1 };
            itemGrid.Initialize([itemSlot], false);
            CheckKey(itemSlot, false, false);
        }
    }

    #endregion


    #region Places

    readonly Places places;
    IPlace? currentPlace = null;

    internal Merchant GetMerchant(uint index) => CurrentSavegame.Merchants[index];

    #endregion


    #region Traps & Spinners

    internal void TriggerTrap(TrapEvent trapEvent, bool lastEventStatus, uint x, uint y)
    {
        Func<PartyMember, bool> targetFilter = null;
        Func<PartyMember, bool> genderFilter = null;

        if (trapEvent.AffectedGenders != GenderFlag.None && trapEvent.AffectedGenders != GenderFlag.Both)
        {
            genderFilter = p =>
            {
                var genderFlag = (GenderFlag)(1 << (int)p.Gender);
                return trapEvent.AffectedGenders.HasFlag(genderFlag);
            };
        }

        var currentPartyMember = CurrentPartyMember;

        switch (trapEvent.Target)
        {
            case TrapEvent.TrapTarget.ActivePlayer:
                // Note: Don't check against the property CurrentPartyMember
                // directly as it might change if someone dies.
                targetFilter = p => p == currentPartyMember;
                break;
            default:
                break;
        }

        uint GetDamage(PartyMember _)
        {
            if (trapEvent.BaseDamage == 0)
                return 0;

            return trapEvent.BaseDamage + (uint)RandomInt(0, (trapEvent.BaseDamage / 2) - 1);
        }

        DamageAllPartyMembers(GetDamage, p =>
        {
            return targetFilter?.Invoke(p) != false && genderFilter?.Invoke(p) != false &&
                RollDice100() >= p.Attributes[Attribute.Luck].TotalCurrentValue;
        }, (p, finish) =>
        {
            bool allInputWasDisabled = allInputDisabled;

            void Next()
            {
                allInputDisabled = allInputWasDisabled;
                finish?.Invoke();
            }

            if (targetFilter?.Invoke(p) != false)
            {
                allInputDisabled = false;
                ShowMessagePopup(p.Name + DataNameProvider.EscapedTheTrap, Next);
            }
            else
                Next();
        }, Finished, trapEvent.GetAilment());

        void Finished(bool someoneDied)
        {
            if (someoneDied)
            {
                clickMoveActive = false;
                CurrentMobileAction = MobileAction.None;
                ResetMoveKeys(true);
            }

            if (trapEvent.Next != null)
            {
                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, x,
                    y, trapEvent.Next, lastEventStatus);
            }
            else
            {
                ResetMapCharacterInteraction(Map);
            }
        }
    }

    internal void Spin(CharacterDirection direction, Event nextEvent)
    {
        if (!Is3D || WindowActive)
            return; // Should not happen

        if (direction == CharacterDirection.Random)
            direction = (CharacterDirection)RandomInt(0, 3);

        // Spin at least for 180°
        float currentAngle = player3D!.Angle;

        while (currentAngle < 360.0f)
            currentAngle += 360.0f;
        while (currentAngle >= 360.0f)
            currentAngle -= 360.0f;

        float targetAngle = (float)direction * 90.0f;
        bool right = true;

        if (targetAngle <= currentAngle)
        {
            if (currentAngle - targetAngle < 180.0f)
                targetAngle += 360.0f;
            else
                right = false;
        }
        else if (targetAngle - currentAngle < 180.0f)
        {
            currentAngle += 360.0f;
            right = false;
        }

        float dist = targetAngle - currentAngle;
        float stepSize = right ? 15.0f : -15.0f;
        int fullSteps = Math.Max(180 / 15, Util.Round(dist / stepSize));
        float halfStepSize = dist % 15.0f;

        if (!right)
            halfStepSize = -halfStepSize;

        int stepIndex = 0;

        void Step()
        {
            if (stepIndex++ < fullSteps)
                player3D!.TurnRight(stepSize);
            else
                player3D!.TurnRight(halfStepSize);

            CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
        }

        PlayTimedSequence(fullSteps + 1, Step, 65, () =>
        {
            ResetMoveKeys();

            if (nextEvent != null)
            {
                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always,
                    (uint)player!.Position.X, (uint)player.Position.Y, nextEvent, true);
            }
        });
    }

    #endregion


    #region Rewards

    internal void RewardPlayer(PartyMember partyMember, RewardEvent rewardEvent, Action followAction)
    {
        void Change(CharacterValue characterValue, int amount, bool percentage, bool lpLike, bool increaseMax)
        {
            uint max = lpLike && !increaseMax ? characterValue.TotalMaxValue : characterValue.MaxValue;

            if (increaseMax)
                max = Math.Max(max, (uint)Math.Max(0, (int)max + amount));

            if (percentage)
                amount = amount * (int)max / 100;

            if (increaseMax)
            {
                characterValue.MaxValue = max;

                if (characterValue.CurrentValue > characterValue.MaxValue)
                    characterValue.CurrentValue = characterValue.MaxValue;
            }
            else
                characterValue.CurrentValue = (uint)Util.Limit(0, (int)characterValue.CurrentValue + amount, (int)max);
        }

        bool RewardValue(CharacterValue characterValue, bool lpLike, bool increaseMax = false)
        {
            uint value = RandomizeIfNecessary(rewardEvent.Value);

            switch (rewardEvent.Operation)
            {
                case RewardEvent.RewardOperation.Increase:
                    Change(characterValue, (int)value, false, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.Decrease:
                    Change(characterValue, -(int)value, false, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.IncreasePercentage:
                    Change(characterValue, (int)value, true, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.DecreasePercentage:
                    Change(characterValue, -(int)value, true, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.Fill:
                    if (increaseMax)
                    {
                        ShowMessagePopup($"ERROR: Reward operation fill is not allowed on a max value.", followAction);
                        return false;
                    }
                    else
                        characterValue.CurrentValue = lpLike ? characterValue.TotalMaxValue : characterValue.MaxValue;
                    break;
            }

            return true;
        }

        uint RandomizeIfNecessary(uint value) => rewardEvent.Random ? 1u + random.Next() % value : value;

        switch (rewardEvent.TypeOfReward)
        {
            case RewardEvent.RewardType.Attribute:
                if (rewardEvent.Attribute != null && rewardEvent.Attribute < Attribute.Age)
                    RewardValue(partyMember.Attributes[rewardEvent.Attribute.Value], false);
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event attribute type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.Skill:
                if (rewardEvent.Skill != null)
                    RewardValue(partyMember.Skills[rewardEvent.Skill.Value], false);
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event skill type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.HitPoints:
            {
                // Note: Rewards happen silently so there is no damage splash.
                // Looking at the original code there isn't even a die handling
                // when a negative reward would leave the LP at 0 but we do so here.
                RewardValue(partyMember.HitPoints, true);
                if (partyMember.Alive && partyMember.HitPoints.CurrentValue == 0)
                    KillPartyMember(partyMember);
                else
                    layout.UpdateCharacter(partyMember);
                break;
            }
            case RewardEvent.RewardType.SpellPoints:
                RewardValue(partyMember.SpellPoints, true);
                layout.UpdateCharacter(partyMember);
                break;
            case RewardEvent.RewardType.SpellLearningPoints:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.SpellLearningPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.SpellLearningPoints + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.SpellLearningPoints = (ushort)Util.Max(0, (int)partyMember.SpellLearningPoints - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Conditions:
            {
                if (rewardEvent.Conditions == null)
                {
                    ShowMessagePopup($"ERROR: Invalid reward event condition.", followAction);
                    return;
                }

                bool wasDead = !partyMember.Alive;

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        partyMember.Conditions |= rewardEvent.Conditions.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        partyMember.Conditions &= ~rewardEvent.Conditions.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        partyMember.Conditions ^= rewardEvent.Conditions.Value;
                        break;
                }

                if (rewardEvent.Conditions.Value.HasFlag(Condition.Blind) && partyMember == CurrentPartyMember)
                    UpdateLight();

                if (wasDead && partyMember.Alive)
                {
                    partyMember.HitPoints.CurrentValue = 1;
                    layout.UpdateCharacter(partyMember, followAction);
                    return;
                }

                break;
            }
            case RewardEvent.RewardType.UsableSpellTypes:
            {
                if (rewardEvent.UsableSpellTypes == null)
                {
                    ShowMessagePopup($"ERROR: Invalid reward event spell mastery.", followAction);
                    return;
                }

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        partyMember.SpellMastery |= rewardEvent.UsableSpellTypes.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        partyMember.SpellMastery &= ~rewardEvent.UsableSpellTypes.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        partyMember.SpellMastery ^= rewardEvent.UsableSpellTypes.Value;
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Languages:
            {
                if (rewardEvent.Languages == null && (!Features.HasFlag(Features.ExtendedLanguages) || rewardEvent.ExtendedLanguages == null))
                {
                    ShowMessagePopup($"ERROR: Invalid reward event language.", followAction);
                    return;
                }

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages |= rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages |= rewardEvent.ExtendedLanguages.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages &= ~rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages &= ~rewardEvent.ExtendedLanguages.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages ^= rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages ^= rewardEvent.ExtendedLanguages.Value;
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Experience:
            {
                if (partyMember.Race != Race.Animal)
                {
                    switch (rewardEvent.Operation)
                    {
                        case RewardEvent.RewardOperation.Increase:
                            AddExperience(partyMember, RandomizeIfNecessary(rewardEvent.Value), followAction);
                            return;
                        case RewardEvent.RewardOperation.Decrease:
                            partyMember.ExperiencePoints = (uint)Util.Max(0, (long)partyMember.ExperiencePoints - RandomizeIfNecessary(rewardEvent.Value));
                            break;
                    }
                }
                break;
            }
            case RewardEvent.RewardType.MaxAttribute:
                if (rewardEvent.Attribute != null && rewardEvent.Attribute < Attribute.Age)
                {
                    if (!RewardValue(partyMember.Attributes[rewardEvent.Attribute.Value], false, true))
                        return;
                }
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event attribute type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.MaxSkill:
                if (rewardEvent.Skill != null && (int)rewardEvent.Skill < 10)
                {
                    if (!RewardValue(partyMember.Skills[rewardEvent.Skill.Value], false, true))
                        return;
                }
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event skill type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.AttacksPerRound:
            {
                int oldAttacksPerRound = partyMember.AttacksPerRound;

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.AttacksPerRound = (byte)Util.Limit(1, partyMember.AttacksPerRound + RandomizeIfNecessary(rewardEvent.Value), 255);
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.AttacksPerRound = (byte)Util.Limit(1, partyMember.AttacksPerRound - (int)RandomizeIfNecessary(rewardEvent.Value), 255);
                        break;
                }

                if (partyMember.AttacksPerRound != oldAttacksPerRound)
                {
                    var currentAttacksPerRoundIncreaseLevels = partyMember.Level / Math.Max(1, partyMember.AttacksPerRound - 1);

                    if (partyMember.AttacksPerRound == 1 && partyMember.AttacksPerRoundIncreaseLevels != 0)
                        partyMember.AttacksPerRoundIncreaseLevels = (ushort)Math.Max(partyMember.AttacksPerRoundIncreaseLevels, partyMember.Level + 1);
                    else if (partyMember.AttacksPerRound > 1)
                        partyMember.AttacksPerRoundIncreaseLevels = (ushort)Math.Max(1, currentAttacksPerRoundIncreaseLevels);
                }

                break;
            }
            case RewardEvent.RewardType.TrainingPoints:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.TrainingPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.TrainingPoints + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.TrainingPoints = (ushort)Util.Max(0, (int)partyMember.TrainingPoints - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Level:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        if (partyMember.Level >= 50 && Features.HasFlag(Features.LevelShards))
                        {
                            for (int i = 0; i < rewardEvent.Value; i++)
                                partyMember.AddLevelShardEffects(RandomInt, Features);
                            followAction?.Invoke();
                            return;
                        }
                        long levelUps = Util.Limit(0, rewardEvent.Value, 50 - partyMember.Level);
                        if (levelUps == 0)
                        {
                            followAction?.Invoke();
                            return;
                        }
                        partyMember.Level = (byte)(partyMember.Level + levelUps);
                        for (long i = 0; i < levelUps; ++i)
                        {
                            if (partyMember.Race == Race.Animal)
                            {
                                uint lpAdd = partyMember.HitPointsPerLevel * (uint)RandomInt(50, 100) / 100;
                                uint tpAdd = partyMember.TrainingPointsPerLevel * (uint)RandomInt(50, 100) / 100;

                                if (Features.HasFlag(Features.StaminaHPOnLevelUp))
                                    lpAdd += partyMember.Attributes[Attribute.Stamina].TotalCurrentValue / 25;

                                partyMember.HitPoints.MaxValue += lpAdd;
                                partyMember.HitPoints.CurrentValue += lpAdd;
                                partyMember.TrainingPoints = (ushort)Math.Min(ushort.MaxValue, partyMember.TrainingPoints + tpAdd);
                            }
                            else
                            {
                                partyMember.AddLevelUpEffects(RandomInt, Features);
                            }
                            ;
                        }
                        ShowLevelUpWindow(partyMember, followAction);
                        return;
                }
                break;
            }
            case RewardEvent.RewardType.Damage:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.BaseAttackDamage = (short)Util.Min(short.MaxValue, partyMember.BaseAttackDamage + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.BaseAttackDamage = (short)Util.Max(0, partyMember.BaseAttackDamage - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Defense:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.BaseDefense = (short)Util.Min(short.MaxValue, partyMember.BaseDefense + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.BaseDefense = (short)Util.Max(0, partyMember.BaseDefense - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.MaxHitPoints:
            {
                // Note: Rewards happen silently so there is no damage splash.
                // Looking at the original code there isn't even a die handling
                // when a negative reward would leave the LP at 0 but we do so here.
                RewardValue(partyMember.HitPoints, true, true);
                if (partyMember.Alive && partyMember.HitPoints.CurrentValue == 0)
                    KillPartyMember(partyMember);
                else
                    layout.UpdateCharacter(partyMember);
                break;
            }
            case RewardEvent.RewardType.MaxSpellPoints:
                RewardValue(partyMember.SpellPoints, true, true);
                layout.UpdateCharacter(partyMember);
                break;
            case RewardEvent.RewardType.EmpowerSpells:
            {
                if (rewardEvent.Value < 3)
                    partyMember.BattleFlags |= (BattleFlags)(1 << ((int)rewardEvent.Value + 4));
                break;
            }
            case RewardEvent.RewardType.ChangePortrait:
            {
                bool changed = partyMember.PortraitIndex != rewardEvent.Value;
                partyMember.PortraitIndex = (byte)rewardEvent.Value;
                layout.UpdateCharacter(partyMember, followAction, changed);
                return;
            }
            case RewardEvent.RewardType.MagicArmorLevel:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.MagicDefense = (short)Util.Min(short.MaxValue, partyMember.MagicDefense + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.MagicDefense = (short)Util.Max(0, (int)partyMember.MagicDefense - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.MagicWeaponLevel:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.MagicAttack = (short)Util.Min(short.MaxValue, partyMember.MagicAttack + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.MagicAttack = (short)Util.Max(0, (int)partyMember.MagicAttack - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Spells:
            {
                if (rewardEvent.Spells == null)
                {
                    ShowMessagePopup($"ERROR: Invalid reward event spell.", followAction);
                    return;
                }

                int spellTypeIndex = -1;

                for (int i = 0; i < 4; i++)
                {
                    if (partyMember.SpellMastery.HasFlag((SpellTypeMastery)(1 << i)))
                    {
                        spellTypeIndex = i;
                        break;
                    }
                }

                if (spellTypeIndex == -1)
                {
                    followAction?.Invoke();
                    return;
                }

                Action<uint> setter;
                uint currentSpells;

                switch (spellTypeIndex)
                {
                    case 0:
                        currentSpells = partyMember.LearnedHealingSpells;
                        setter = (value) => partyMember.LearnedHealingSpells = value;
                        break;
                    case 1:
                        currentSpells = partyMember.LearnedAlchemisticSpells;
                        setter = (value) => partyMember.LearnedAlchemisticSpells = value;
                        break;
                    case 2:
                        currentSpells = partyMember.LearnedMysticSpells;
                        setter = (value) => partyMember.LearnedMysticSpells = value;
                        break;
                    default:
                        currentSpells = partyMember.LearnedDestructionSpells;
                        setter = (value) => partyMember.LearnedDestructionSpells = value;
                        break;
                }
                ;

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        setter(currentSpells | rewardEvent.Spells.Value);
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        setter(currentSpells & ~rewardEvent.Spells.Value);
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        setter(currentSpells ^ rewardEvent.Spells.Value);
                        break;
                }
                break;
            }
        }

        followAction?.Invoke();
    }

    #endregion


    #region Input events

    internal void SayWord(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
    {
        bool wasPaused = paused;
        Pause();
        void CheckResume()
        {
            if (!wasPaused)
                Resume();
        }
        OpenDictionary(word =>
        {
            layout.ClosePopup();

            bool match = string.Compare(textDictionary.Entries[(int)conditionEvent.ObjectIndex], word, true) == 0;
            var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
            var @event = match ? conditionEvent.Next : mapEventIfFalse;
            CheckResume();
            if (@event != null)
                EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, @event, true);
        }, null, CheckResume);
    }

    internal void EnterNumber(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
    {
        bool wasPaused = paused;
        Pause();
        void CheckResume()
        {
            if (!wasPaused)
                Resume();
        }
        layout.OpenAmountInputBox(DataNameProvider.WhichNumber, null, null, 9999, number =>
        {
            ClosePopup();
            var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
            var @event = (number == conditionEvent.ObjectIndex) == (conditionEvent.Value != 0)
                ? conditionEvent.Next : mapEventIfFalse;
            CheckResume();
            if (@event != null)
                EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, @event, true);
        }, CheckResume);
    }

    #endregion
}
