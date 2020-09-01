using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    // TODO: disabled state
    // TODO: memorize scrollbar positions for inventories
    internal class ItemGrid
    {
        const int SlotWidth = 16;
        const int SlotHeight = 24;
        public static readonly Size SlotSize = new Size(SlotWidth, SlotHeight);
        readonly IRenderView renderView;
        readonly IItemManager itemManager;
        readonly List<Position> slotPositions;
        readonly UIItem[] items;
        IRenderText hoveredItemName;
        readonly bool allowExternalDrop;
        readonly Func<ItemGrid, int, UIItem, Layout.DraggedItem> pickupAction;
        readonly int slotsPerPage;
        readonly int slotsPerScroll;
        Scrollbar scrollbar;
        Layout.DraggedItem dragScrollItem = null; // set when scrolling while dragging an item

        public int SlotCount => items.Length;
        public int ScrollOffset { get; private set; } = 0;

        private ItemGrid(Layout layout, IRenderView renderView, IItemManager itemManager, List<Position> slotPositions,
            bool allowExternalDrop, Func<ItemGrid, int, UIItem, Layout.DraggedItem> pickupAction,
            int slotsPerPage, int slotsPerScroll, int numTotalSlots, Rect scrollbarArea = null, Size scrollbarSize = null,
            ScrollbarType? scrollbarType = null)
        {
            this.renderView = renderView;
            this.itemManager = itemManager;
            this.slotPositions = slotPositions;
            this.allowExternalDrop = allowExternalDrop;
            this.pickupAction = pickupAction;
            this.slotsPerPage = slotsPerPage;
            this.slotsPerScroll = slotsPerScroll;
            items = new UIItem[numTotalSlots];
            scrollbar = slotsPerScroll == 0 ? null :
                new Scrollbar(layout, scrollbarType ?? ScrollbarType.SmallVertical, scrollbarArea,
                scrollbarSize.Width, scrollbarSize.Height, (numTotalSlots - slotsPerPage) / slotsPerScroll);
            if (scrollbar != null)
                scrollbar.Scrolled += Scrollbar_Scrolled;
        }

        public static ItemGrid CreateInventory(Layout layout, int partyMemberIndex, IRenderView renderView, IItemManager itemManager, List<Position> slotPositions)
        {
            return new ItemGrid(layout, renderView, itemManager, slotPositions, true, (ItemGrid itemGrid, int slot, UIItem item) =>
                Layout.DraggedItem.FromInventory(itemGrid, partyMemberIndex, slot, item, false), 12, 3, 24,
                new Rect(109 + 3 * 22, 76, 6, 112), new Size(6, 56), ScrollbarType.LargeVertical);
        }

        public static ItemGrid CreateEquipment(Layout layout, int partyMemberIndex, IRenderView renderView, IItemManager itemManager, List<Position> slotPositions)
        {
            return new ItemGrid(layout, renderView, itemManager, slotPositions, true, (ItemGrid itemGrid, int slot, UIItem item) =>
                Layout.DraggedItem.FromInventory(itemGrid, partyMemberIndex, slot, item, true), 9, 0, 9);
        }

        public static ItemGrid Create(Layout layout, IRenderView renderView, IItemManager itemManager, List<Position> slotPositions,
            bool allowExternalDrop, int slotsPerPage, int slotsPerScroll, int numTotalSlots,
            Rect scrollbarArea, Size scrollbarSize, ScrollbarType scrollbarType)
        {
            return new ItemGrid(layout, renderView, itemManager, slotPositions, allowExternalDrop, (ItemGrid itemGrid, int slot, UIItem item) =>
                Layout.DraggedItem.FromExternal(itemGrid, slot, item), slotsPerPage, slotsPerScroll, numTotalSlots,
                scrollbarArea, scrollbarSize, scrollbarType);
        }

        public void Destroy()
        {
            for (int i = 0; i < items.Length; ++i)
                SetItem(i, null);

            hoveredItemName?.Delete();
            hoveredItemName = null;

            scrollbar?.Destroy();
            scrollbar = null;

            dragScrollItem = null;
        }

        public void SetItem(int slot, ItemSlot item)
        {
            items[slot]?.Destroy();

            if (item == null)
                items[slot] = null;
            else
            {
                var newItem = items[slot] = new UIItem(renderView, itemManager, item);
                bool visible = SlotVisible(slot);
                newItem.Visible = visible;
                if (visible)
                    newItem.Position = slotPositions[slot - ScrollOffset];
            }
        }

        public bool SlotVisible(int slot) => slot >= ScrollOffset && slot < ScrollOffset + slotsPerPage;

        public ItemSlot GetItem(int slot) => items[slot]?.Item;

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

        public int DropItem(int slot, UIItem item)
        {
            var itemSlot = items[slot];

            if (itemSlot == null)
            {
                item.Dragged = false;
                items[slot] = item;
                items[slot].Position = slotPositions[slot];
                return 0;
            }
            else if (itemSlot.Item.Empty || itemSlot.Item.ItemIndex == item.Item.ItemIndex)
            {
                int remaining = itemSlot.Item.Add(item.Item);
                items[slot].Update(false);

                if (remaining == 0)
                    item.Destroy();
                else
                    item.Update(false);

                return remaining;
            }
            else
            {
                itemSlot.Item.Exchange(item.Item);
                items[slot].Update(true);
                items[slot].Position = slotPositions[slot]; // Important to re-position amount display if added
                item.Update(true);
                return itemSlot.Item.Amount;
            }
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
            ScrollOffset = newPosition * slotsPerScroll;
            PostScrollUpdate();
        }

        public bool Drag(Position position)
        {
            if (scrollbar?.Drag(position) == true)
                return true;

            return false;
        }

        public void LeftMouseUp(Position position)
        {
            if (dragScrollItem?.Item != null)
            {
                dragScrollItem.Item.Position = position;
                dragScrollItem.Item.Visible = true;
                dragScrollItem = null;
            }

            scrollbar?.LeftMouseUp();
        }

        public bool Click(Game game, Position position, Layout.DraggedItem draggedItem,
            out Layout.DraggedItem pickedUpItem, bool leftMouseButton, ref CursorType cursorType)
        {
            pickedUpItem = draggedItem;

            if (leftMouseButton && scrollbar?.LeftClick(position) == true)
            {
                if (draggedItem != null)
                {
                    dragScrollItem = draggedItem;
                    draggedItem.Item.Visible = false;
                }

                cursorType = CursorType.None;

                return true;
            }

            var slot = SlotFromPosition(position);

            if (slot == null)
                return false;

            if (draggedItem != null)
            {
                if (!allowExternalDrop && draggedItem.SourceGrid != this)
                    return false;

                if (DropItem(slot.Value, draggedItem.Item) == 0)
                {
                    // fully dropped
                    pickedUpItem = null;
                }

                Hover(position); // This updates the tooltip
            }
            else
            {
                var itemSlot = items[slot.Value];

                if (itemSlot != null && !itemSlot.Item.Empty)
                {
                    pickedUpItem = pickupAction(this, slot.Value, itemSlot);
                    items[slot.Value] = null;
                    Hover(position); // This updates the tooltip
                }
            }

            return true;
        }

        public bool Hover(Position position)
        {
            var slot = SlotFromPosition(position);

            if (slot == null || items[slot.Value]?.Visible != true || items[slot.Value]?.Item?.Empty == true)
            {
                hoveredItemName?.Delete();
                hoveredItemName = null;
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
                        TextColor.White, true
                    );
                }
                else
                    hoveredItemName.Text = itemNameText;
                hoveredItemName.DisplayLayer = 2;
                hoveredItemName.X = Util.Limit(0, position.X - textWidth / 2, Global.VirtualScreenWidth - textWidth);
                hoveredItemName.Y = position.Y - Global.GlyphLineHeight - 2;
                hoveredItemName.Visible = true;

                return true;
            }
        }
    }
}
