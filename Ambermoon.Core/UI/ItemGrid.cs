using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    // TODO: memorize scrollbar positions for inventories
    // Note: The items are automatically updated in inventories,
    // chests, etc as the UIItems use the same ItemSlot instances
    // and modify them directly.
    internal class ItemGrid
    {
        const int SlotWidth = 16;
        const int SlotHeight = 24;
        public static readonly Size SlotSize = new Size(SlotWidth, SlotHeight);
        readonly Game game;
        readonly IRenderView renderView;
        readonly IItemManager itemManager;
        readonly List<Position> slotPositions;
        readonly List<ItemSlot> slots;
        readonly UIItem[] items;
        readonly ILayerSprite[] slotBackgrounds;
        IRenderText hoveredItemName;
        IRenderText hoveredItemPrice;
        readonly bool allowExternalDrop;
        readonly Action<ItemGrid, int, UIItem, Action<Layout.DraggedItem, int>, bool> pickupAction;
        readonly int slotsPerPage;
        readonly int slotsPerScroll;
        Scrollbar scrollbar;
        Layout.DraggedItem dragScrollItem = null; // set when scrolling while dragging an item
        bool disabled;
        Func<Position, bool, Item, int?> dropSlotProvider = null;
        bool showPrice = false;
        readonly Func<uint> availableGoldProvider = null;

        internal enum ItemAction
        {
            None,
            Drag,
            Drop,
            Exchange
        }

        public event Action<int, ItemSlot, int> ItemDragged;
        public event Action<int, ItemSlot> ItemDropped;
        public event Action<int, ItemSlot, int, ItemSlot> ItemExchanged;
        public event Action<ItemGrid, int, ItemSlot> ItemClicked;
        public event Func<bool> RightClicked;
        public int SlotCount => items.Length;
        public int ScrollOffset { get; private set; } = 0;
        public bool Disabled
        {
            get => disabled;
            set
            {
                if (disabled == value)
                    return;

                disabled = value;

                if (scrollbar != null)
                    scrollbar.Disabled = disabled || items.Length <= slotsPerPage;

                if (disabled)
                {
                    foreach (var item in items)
                        item?.Destroy();
                }

                var slotTexCoords = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(
                    Graphics.GetCustomUIGraphicIndex(disabled ? UICustomGraphic.ItemSlotDisabled : UICustomGraphic.ItemSlotBackground));
                foreach (var background in slotBackgrounds)
                    background.TextureAtlasOffset = slotTexCoords;
            }
        }
        public bool DisableDrag { get; set; } = false;

        public bool ShowPrice
        {
            get => showPrice;
            set
            {
                if (showPrice == value)
                    return;

                showPrice = value;

                if (!showPrice && hoveredItemPrice != null)
                {
                    hoveredItemPrice?.Delete();
                    hoveredItemPrice = null;
                }
            }
        }

        private ItemGrid(Game game, Layout layout, IRenderView renderView, IItemManager itemManager, List<Position> slotPositions,
            List<ItemSlot> slots, bool allowExternalDrop, Action<ItemGrid, int, UIItem, Action<Layout.DraggedItem, int>, bool> pickupAction,
            int slotsPerPage, int slotsPerScroll, int numTotalSlots, Rect scrollbarArea = null, Size scrollbarSize = null,
            ScrollbarType? scrollbarType = null, bool showPrice = false, Func<uint> availableGoldProvider = null)
        {
            this.game = game;
            this.renderView = renderView;
            this.itemManager = itemManager;
            this.slotPositions = slotPositions;
            this.slots = slots;
            this.allowExternalDrop = allowExternalDrop;
            this.pickupAction = pickupAction;
            this.slotsPerPage = slotsPerPage;
            this.slotsPerScroll = slotsPerScroll;
            slotBackgrounds = new ILayerSprite[slotPositions.Count];
            CreateSlotBackgrounds();
            items = new UIItem[numTotalSlots];
            scrollbar = slotsPerScroll == 0 ? null :
                new Scrollbar(game, layout, scrollbarType ?? ScrollbarType.SmallVertical, scrollbarArea,
                scrollbarSize.Width, scrollbarSize.Height, (numTotalSlots - slotsPerPage) / slotsPerScroll);
            if (scrollbar != null)
            {
                if (items.Length <= slotsPerPage)
                    scrollbar.Disabled = true;
                else
                    scrollbar.Scrolled += Scrollbar_Scrolled;
            }
            this.showPrice = showPrice;
            this.availableGoldProvider = availableGoldProvider;
        }

        void CreateSlotBackgrounds()
        {
            var layer = renderView.GetLayer(Layer.UI);
            var texCoords = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.ItemSlotBackground));
            byte paletteIndex = game.UIPaletteIndex;

            for (int i = 0; i < slotBackgrounds.Length; ++i)
            {
                var background = slotBackgrounds[i] = renderView.SpriteFactory.Create(16, 24, true) as ILayerSprite;
                background.Layer = layer;
                background.PaletteIndex = paletteIndex;
                background.TextureAtlasOffset = texCoords;
                background.X = slotPositions[i].X;
                background.Y = slotPositions[i].Y;
                background.Visible = true;
            }
        }

        public static ItemGrid CreateInventory(Game game, Layout layout, int partyMemberIndex, IRenderView renderView,
            IItemManager itemManager, List<Position> slotPositions, List<ItemSlot> slots)
        {
            var grid = new ItemGrid(game, layout, renderView, itemManager, slotPositions, slots, true,
                (ItemGrid itemGrid, int slot, UIItem item, Action<Layout.DraggedItem, int> dragAction, bool takeAll) =>
                    layout.DragItems(item, takeAll, dragAction,
                    () => Layout.DraggedItem.FromInventory(itemGrid, partyMemberIndex, slot, item, false)),
                12, 3, 24, new Rect(109 + 3 * 22, 76, 6, 112), new Size(6, 56), ScrollbarType.LargeVertical);
            grid.dropSlotProvider = (position, broken, _) => grid.SlotFromPosition(position);
            return grid;
        }

        public static ItemGrid CreateEquipment(Game game, Layout layout, int partyMemberIndex, IRenderView renderView,
            IItemManager itemManager, List<Position> slotPositions, List<ItemSlot> slots, Func<ItemSlot, bool> equipChecker)
        {
            var grid = new ItemGrid(game, layout, renderView, itemManager, slotPositions, slots, true,
                (ItemGrid itemGrid, int slot, UIItem item, Action<Layout.DraggedItem, int> dragAction, bool takeAll) =>
                {
                    if (equipChecker(item.Item))
                    {
                        layout.DragItems(item, takeAll, dragAction,
                            () => Layout.DraggedItem.FromInventory(itemGrid, partyMemberIndex, slot, item, true));
                    }
                }, 9, 0, 9);
            grid.dropSlotProvider = (position, broken, item) =>
            {
                if (!new Rect(19, 71, 82, 122).Contains(position))
                    return null;

                if (broken)
                {
                    layout.SetInventoryMessage(game.DataNameProvider.ItemIsBroken, true);
                    return null;
                }
                if (!item.Classes.Contains(game.CurrentInventory.Class))
                {
                    layout.SetInventoryMessage(game.DataNameProvider.WrongClassToEquipItem, true);
                    return null;
                }
                if (!item.Genders.Contains(game.CurrentInventory.Gender))
                {
                    layout.SetInventoryMessage(game.DataNameProvider.WrongSexToEquipItem, true);
                    return null;
                }
                if (game.BattleActive && !item.Flags.HasFlag(ItemFlags.RemovableDuringFight))
                {
                    layout.SetInventoryMessage(game.DataNameProvider.CannotEquipInFight, true);
                    return null;
                }

                var equipmentSlot = item.EquipmentSlot;

                if (equipmentSlot == EquipmentSlot.None)
                {
                    layout.SetInventoryMessage(game.DataNameProvider.CannotEquip, true);
                    return null;
                }

                if (equipmentSlot == EquipmentSlot.RightFinger ||
                    equipmentSlot == EquipmentSlot.LeftFinger)
                {
                    int rightFingerSlot = (int)(EquipmentSlot.RightFinger - 1);

                    if (item.NumberOfFingers == 2)
                    {
                        if (grid.GetItemSlot(rightFingerSlot)?.Empty == false &&
                            grid.GetItemSlot(rightFingerSlot + 2)?.Empty == false)
                        {
                            layout.SetInventoryMessage(game.DataNameProvider.NotEnoughFreeFingers, true);
                            return null;
                        }
                    }

                    var rightFingerItemSlot = grid.GetItemSlot(rightFingerSlot);

                    // place on first free finger starting at right one
                    if (rightFingerItemSlot?.Empty ?? true && rightFingerItemSlot?.Flags.HasFlag(ItemSlotFlags.Cursed) != true)
                        return rightFingerSlot;
                    else if (grid.GetItemSlot(rightFingerSlot + 2)?.Flags.HasFlag(ItemSlotFlags.Cursed) != true)
                        return rightFingerSlot + 2; // left finger
                    else
                    {
                        layout.SetInventoryMessage(game.DataNameProvider.ItemIsCursed, true);
                        return null;
                    }
                }

                if (equipmentSlot == EquipmentSlot.RightHand ||
                    equipmentSlot == EquipmentSlot.LeftHand)
                {
                    if (item.NumberOfHands == 2)
                    {
                        var leftHandSlot = grid.GetItemSlot((int)(EquipmentSlot.LeftHand - 1));

                        if (grid.GetItemSlot((int)(EquipmentSlot.RightHand - 1))?.Empty == false &&
                            leftHandSlot?.Empty == false && (leftHandSlot?.ItemIndex ?? 0) != 0)
                        {
                            layout.SetInventoryMessage(game.DataNameProvider.NotEnoughFreeHands, true);
                            return null;
                        }

                        if (grid.GetItemSlot((int)EquipmentSlot.RightHand - 1)?.Flags.HasFlag(ItemSlotFlags.Cursed) == true ||
                            grid.GetItemSlot((int)EquipmentSlot.LeftHand - 1)?.Flags.HasFlag(ItemSlotFlags.Cursed) == true)
                        {
                            layout.SetInventoryMessage(game.DataNameProvider.ItemIsCursed, true);
                            return null;
                        }
                    }
                }

                var targetSlot = grid.GetItemSlot((int)item.EquipmentSlot - 1);

                if (targetSlot?.Flags.HasFlag(ItemSlotFlags.Cursed) == true)
                {
                    layout.SetInventoryMessage(game.DataNameProvider.ItemIsCursed, true);
                    return null;
                }

                if (game.BattleActive)
                {
                    var itemIndexAtDestinationSlot = grid.GetItemSlot((int)equipmentSlot - 1)?.ItemIndex;
                    var itemAtDestinationSlot = (itemIndexAtDestinationSlot ?? 0) == 0 ? null : itemManager.GetItem(itemIndexAtDestinationSlot.Value);

                    if (itemAtDestinationSlot != null && !itemAtDestinationSlot.Flags.HasFlag(ItemFlags.RemovableDuringFight))
                    {
                        layout.SetInventoryMessage(game.DataNameProvider.CannotUnequipInFight, true);
                        return null;
                    }
                }

                return (int)equipmentSlot - 1;
            };
            return grid;
        }

        public static ItemGrid Create(Game game, Layout layout, IRenderView renderView, IItemManager itemManager,
            List<Position> slotPositions, List<ItemSlot> slots, bool allowExternalDrop, int slotsPerPage,
            int slotsPerScroll, int numTotalSlots, Rect scrollbarArea, Size scrollbarSize, ScrollbarType scrollbarType,
            bool showPrice = false, Func<uint>availableGoldProvider = null)
        {
            var grid =  new ItemGrid(game, layout, renderView, itemManager, slotPositions, slots, allowExternalDrop,
                (ItemGrid itemGrid, int slot, UIItem item, Action<Layout.DraggedItem, int> dragAction, bool takeAll) =>
                    layout.DragItems(item, takeAll, dragAction, () => Layout.DraggedItem.FromExternal(itemGrid, slot, item)),
                    slotsPerPage, slotsPerScroll, numTotalSlots, scrollbarArea, scrollbarSize, scrollbarType, showPrice, availableGoldProvider);
            grid.dropSlotProvider = (position, broken, _) => grid.SlotFromPosition(position);
            return grid;
        }

        public void Destroy()
        {
            for (int i = 0; i < items.Length; ++i)
                SetItem(i, null);

            foreach (var background in slotBackgrounds)
                background?.Delete();

            hoveredItemName?.Delete();
            hoveredItemName = null;
            hoveredItemPrice?.Delete();
            hoveredItemPrice = null;

            scrollbar?.Destroy();
            scrollbar = null;

            dragScrollItem = null;
        }

        public void Initialize(List<ItemSlot> newSlots, bool merchantItems)
        {
            ScrollToBegin();

            for (int i = 0; i < slots.Count; ++i)
            {
                slots[i] = i < newSlots.Count ? newSlots[i] : null;
                items[i]?.Destroy();
                items[i] = null;
                if (slots[i] != null && !slots[i].Empty)
                    SetItem(i, slots[i], merchantItems);
            }
        }

        public void UpdateItem(int slot)
        {
            items[slot]?.Update(false);
        }

        public void Refresh(bool merchantItem = false)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (items[i] == null)
                {
                    if (slots[i]?.Empty == false)
                        SetItem(i, slots[i], merchantItem);
                }
                else
                {
                    items[i].Update(true);
                }
            }
        }

        public void SetItem(int slot, ItemSlot item, bool merchantItem = false)
        {
            items[slot]?.Destroy();

            if (item == null || item.Empty)
            {
                items[slot] = null;
            }
            else
            {
                slots[slot].Replace(item);
                var newItem = items[slot] = new UIItem(renderView, itemManager, slots[slot], merchantItem);
                bool visible = SlotVisible(slot);
                newItem.Visible = visible;
                if (visible)
                    newItem.Position = slotPositions[slot - ScrollOffset];
            }
        }

        public bool SlotVisible(int slot) => slot >= ScrollOffset && slot < ScrollOffset + slotsPerPage;

        public UIItem GetItem(int slot) => items[slot];

        public ItemSlot GetItemSlot(int slot) => slots[slot];

        public Position GetSlotPosition(int slot) => slotPositions[slot - ScrollOffset];

        public int? SlotFromPosition(Position position)
        {
            int slot = 0;

            foreach (var slotPosition in slotPositions)
            {
                if (new Rect(slotPosition, SlotSize).Contains(position))
                    return ScrollOffset + slot;

                ++slot;
            }

            return null;
        }

        public int SlotFromItemSlot(ItemSlot itemSlot)
        {
            return slots.IndexOf(itemSlot);
        }

        public int DropItem(int slot, Layout.DraggedItem item)
        {
            if (DisableDrag)
                return item.Item.Item.Amount;

            if (slot == -1)
                return item.Item.Item.Amount;

            var itemSlot = items[slot];

            if (itemSlot == null)
            {
                item.Item.Dragged = false;
                slots[slot].Replace(item.Item.Item);
                item.Item.SetItem(slots[slot]);
                item.Item.Position = slotPositions[slot - ScrollOffset];
                items[slot] = item.Item;
                ItemDropped?.Invoke(slot, item.Item.Item);
                return 0;
            }
            else if (itemSlot.Item.Empty || itemSlot.Item.ItemIndex == item.Item.Item.ItemIndex)
            {
                int remaining = itemSlot.Item.Add(item.Item.Item);
                itemSlot.Update(false);

                if (remaining == 0)
                    item.Item.Destroy();
                else
                    item.Item.Update(false);

                ItemDropped?.Invoke(slot, itemSlot.Item);

                return remaining;
            }
            else
            {
                if (!itemSlot.Item.Draggable)
                    return item.Item.Item.Amount;

                itemSlot.Item.Exchange(item.Item.Item);
                itemSlot.Update(true);
                itemSlot.Position = itemSlot.Position; // Important to re-position amount display if added
                item.Item.Update(true);
                ItemExchanged?.Invoke(slot, item.Item.Item, item.Item.Item.Amount, itemSlot.Item);
                return itemSlot.Item.Amount;
            }
        }

        public bool Scroll(bool down)
        {
            if (scrollbar != null && !scrollbar.Disabled)
            {
                if (down)
                {
                    ScrollDown();
                }
                else // up
                {
                    ScrollUp();
                }

                return true;
            }

            return false;
        }

        public void ScrollUp()
        {
            if (ScrollOffset > 0)
                ScrollTo(ScrollOffset - slotsPerScroll);
        }

        public void ScrollDown()
        {
            if (ScrollOffset < items.Length - slotsPerPage)
                ScrollTo(ScrollOffset + slotsPerScroll);
        }

        public void ScrollPageUp()
        {
            if (ScrollOffset > 0)
                ScrollTo(ScrollOffset - slotsPerPage);
        }

        public void ScrollPageDown()
        {
            if (ScrollOffset < items.Length - slotsPerPage)
                ScrollTo(ScrollOffset + slotsPerPage);
        }

        public void ScrollToBegin()
        {
            ScrollTo(0);
        }

        public void ScrollToEnd()
        {
            ScrollTo(items.Length - slotsPerPage);
        }

        public void ScrollTo(int offset)
        {
            if (slotsPerScroll == 0) // not scrollable
                return;

            offset = Math.Max(0, Math.Min(offset, SlotCount - slotsPerPage));

            if (ScrollOffset == offset) // already there
                return;

            if (offset % slotsPerScroll != 0)
            {
                throw new AmbermoonException(ExceptionScope.Application,
                    $"Can not scroll the item grid to offset {offset} as a scroll must be a multiple of {slotsPerScroll} slots.");
            }

            ScrollOffset = offset;
            PostScrollUpdate();

            scrollbar?.SetScrollPosition(ScrollOffset / slotsPerScroll);
        }

        void PostScrollUpdate()
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (items[i] == null)
                    continue;

                if (SlotVisible(i))
                {
                    items[i].Position = slotPositions[i - ScrollOffset];
                    items[i].Visible = true;
                }
                else
                {
                    items[i].Visible = false;
                }
            }
        }

        void Scrollbar_Scrolled(int newPosition)
        {
            if (disabled)
                return;

            ScrollOffset = newPosition * slotsPerScroll;
            PostScrollUpdate();
        }

        public bool Drag(Position position)
        {
            if (disabled)
                return false;

            if (scrollbar?.Drag(position) == true)
                return true;

            return false;
        }

        public void LeftMouseUp(Position position)
        {
            if (disabled)
                return;

            if (dragScrollItem?.Item != null)
            {
                dragScrollItem.Item.Position = position;
                dragScrollItem.Item.Visible = true;
                dragScrollItem = null;
            }

            scrollbar?.LeftMouseUp();
        }

        public bool Click(Position position, Layout.DraggedItem draggedItem,
            out ItemAction itemAction, MouseButtons mouseButtons, ref CursorType cursorType,
            Action<Layout.DraggedItem> dragHandler)
        {
            itemAction = ItemAction.None;

            if (disabled)
                return false;

            if (mouseButtons == MouseButtons.Left && scrollbar != null &&
                !scrollbar.Disabled && scrollbar.LeftClick(position))
            {
                if (draggedItem != null)
                {
                    dragScrollItem = draggedItem;
                    draggedItem.Item.Visible = false;
                }

                cursorType = CursorType.None;

                return true;
            }

            if (mouseButtons == MouseButtons.Right)
            {
                if (RightClicked?.Invoke() == true)
                    return true;
            }

            if (draggedItem != null)
            {
                if (!allowExternalDrop && draggedItem.SourceGrid != this)
                    return false;

                var slot = dropSlotProvider?.Invoke(position, draggedItem.Item.Item.Flags.HasFlag(ItemSlotFlags.Broken),
                    itemManager.GetItem(draggedItem.Item.Item.ItemIndex));

                if (slot == null)
                    return false;

                if (DropItem(slot.Value, draggedItem) == 0)
                {
                    // fully dropped
                    itemAction = ItemAction.Drop;
                }
                else
                {
                    itemAction = ItemAction.Exchange;
                    dragHandler?.Invoke(draggedItem);
                }

                Hover(position); // This updates the tooltip
            }
            else
            {
                var slot = SlotFromPosition(position);

                if (slot == null)
                    return false;

                var itemSlot = items[slot.Value];

                if (itemSlot != null)
                {
                    // Note: ItemClicked handler may re-enable dragging
                    //       but we don't want to drag immediately here
                    //       so remember drag disable state for this
                    //       click execution.
                    bool dragDisabled = DisableDrag;

                    if (mouseButtons == MouseButtons.Left)
                        ItemClicked?.Invoke(this, slot.Value, itemSlot.Item);

                    if (!dragDisabled && itemSlot.Item.Draggable)
                    {
                        itemAction = ItemAction.Drag;
                        pickupAction(this, slot.Value, itemSlot, (Layout.DraggedItem item, int amount) =>
                        {
                            item.Item.Item.Amount = amount;
                            item.Item.Update(false);
                            ItemDragged?.Invoke(slot.Value, item.Item.Item, amount);
                            if (items[slot.Value].Item.Empty)
                            {
                                items[slot.Value].Destroy();
                                items[slot.Value] = null;
                            }
                            else
                                items[slot.Value].Update(false);
                            Hover(position); // This updates the tooltip
                            dragHandler?.Invoke(item);
                        }, mouseButtons == MouseButtons.Right);
                    }
                }
            }

            return true;
        }

        public void HideTooltip()
        {
            hoveredItemName?.Delete();
            hoveredItemName = null;
            hoveredItemPrice?.Delete();
            hoveredItemPrice = null;
        }

        public bool Hover(Position position)
        {
            var slot = SlotFromPosition(position);

            if (disabled || slot == null || items[slot.Value]?.Visible != true ||
                items[slot.Value]?.Item?.Empty == true || items[slot.Value]?.Item.ItemIndex == 0)
            {
                HideTooltip();
                return slot != null;
            }
            else
            {
                var item = itemManager.GetItem(items[slot.Value].Item.ItemIndex);
                var itemNameText = renderView.TextProcessor.CreateText(item.Name);
                int textWidth = itemNameText.MaxLineSize * Global.GlyphWidth;

                if (hoveredItemName == null)
                {
                    hoveredItemName = renderView.RenderTextFactory.Create
                    (
                        renderView.GetLayer(Layer.Text),
                        itemNameText,
                        Data.Enumerations.Color.White, true
                    );
                }
                else
                    hoveredItemName.Text = itemNameText;
                hoveredItemName.DisplayLayer = 10;
                hoveredItemName.PaletteIndex = game.UIPaletteIndex;
                hoveredItemName.X = Util.Limit(0, position.X - textWidth / 2, Global.VirtualScreenWidth - textWidth);
                hoveredItemName.Y = position.Y - Global.GlyphLineHeight - 1;
                hoveredItemName.Visible = true;

                if (showPrice)
                {
                    var itemPriceText = renderView.TextProcessor.CreateText(item.Price.ToString());
                    textWidth = itemPriceText.MaxLineSize * Global.GlyphWidth;
                    var color = availableGoldProvider != null && availableGoldProvider() < item.Price ? Data.Enumerations.Color.Red : Data.Enumerations.Color.White;

                    if (hoveredItemPrice == null)
                    {
                        hoveredItemPrice = renderView.RenderTextFactory.Create
                        (
                            renderView.GetLayer(Layer.Text),
                            itemPriceText,
                            color, true
                        );
                    }
                    else
                    {
                        hoveredItemPrice.TextColor = color;
                        hoveredItemPrice.Text = itemPriceText;
                    }

                    hoveredItemPrice.DisplayLayer = 2;
                    hoveredItemPrice.PaletteIndex = hoveredItemName.PaletteIndex;
                    hoveredItemPrice.X = Util.Limit(0, position.X - textWidth / 2, Global.VirtualScreenWidth - textWidth);
                    hoveredItemPrice.Y = position.Y - 2 * Global.GlyphLineHeight - 1;
                    hoveredItemPrice.Visible = true;
                }

                return true;
            }
        }

        public void ResetAnimation(ItemSlot itemSlot)
        {
            int slotIndex = SlotFromItemSlot(itemSlot);
            var item = items[slotIndex];

            if (item != null)
            {
                item.Position = GetSlotPosition(slotIndex);
                item.Dragged = false;
                item.ShowItemAmount = true;
            }
        }

        public void PlayMoveAnimation(ItemSlot itemSlot, Position targetPosition, Action finishAction,
            int pixelsPerSecond = 300)
        {
            var slotPosition = GetSlotPosition(SlotFromItemSlot(itemSlot));
            int slotIndex = SlotFromItemSlot(itemSlot);
            var item = items[slotIndex];
            targetPosition ??= slotPosition;
            var startPosition = item.Position;

            void MoveFinished()
            {
                item.Dragged = false;
                item.ShowItemAmount = true;
                finishAction?.Invoke();
            }

            item.Dragged = true;
            item.ShowItemAmount = false;

            ItemAnimation.Play(game, renderView, ItemAnimation.Type.Move, startPosition, MoveFinished,
                TimeSpan.FromMilliseconds(50), targetPosition, item, pixelsPerSecond);
        }

        public void PlayShakeAnimation(ItemSlot itemSlot, Action finishAction)
        {
            ItemAnimation.Play(game, renderView, ItemAnimation.Type.Shake, null, finishAction, TimeSpan.FromMilliseconds(50),
                null, items[SlotFromItemSlot(itemSlot)]);
        }
    }
}
