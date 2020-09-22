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
        readonly IRenderView renderView;
        readonly IItemManager itemManager;
        readonly List<Position> slotPositions;
        readonly UIItem[] items;
        readonly ILayerSprite[] slotBackgrounds;
        IRenderText hoveredItemName;
        readonly bool allowExternalDrop;
        readonly Func<ItemGrid, int, UIItem, Layout.DraggedItem> pickupAction;
        readonly int slotsPerPage;
        readonly int slotsPerScroll;
        Scrollbar scrollbar;
        Layout.DraggedItem dragScrollItem = null; // set when scrolling while dragging an item
        bool disabled;

        public event Action<int, Item> ItemDragged;
        public event Action<int, Item> ItemDropped;
        /// <summary>
        /// Called when starting dropping. Should return
        /// the slot index to drop or -1 if dropping is
        /// denied.
        /// </summary>
        public event Func<int, Item, int> Dropping;
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
                    scrollbar.Disabled = disabled;

                if (disabled)
                {
                    foreach (var item in items)
                        item?.Destroy();
                }

                var slotTexCoords = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset +
                    (disabled ? (uint)UICustomGraphic.ItemSlotDisabled : (uint)UICustomGraphic.ItemSlotBackground));
                foreach (var background in slotBackgrounds)
                    background.TextureAtlasOffset = slotTexCoords;
            }
        }


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
            slotBackgrounds = new ILayerSprite[slotPositions.Count];
            CreateSlotBackgrounds();
            items = new UIItem[numTotalSlots];
            scrollbar = slotsPerScroll == 0 ? null :
                new Scrollbar(layout, scrollbarType ?? ScrollbarType.SmallVertical, scrollbarArea,
                scrollbarSize.Width, scrollbarSize.Height, (numTotalSlots - slotsPerPage) / slotsPerScroll);
            if (scrollbar != null)
                scrollbar.Scrolled += Scrollbar_Scrolled;
        }

        void CreateSlotBackgrounds()
        {
            var layer = renderView.GetLayer(Layer.UI);
            var texCoords = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.ItemSlotBackground);

            for (int i = 0; i < slotBackgrounds.Length; ++i)
            {
                var background = slotBackgrounds[i] = renderView.SpriteFactory.Create(16, 24, false, true) as ILayerSprite;
                background.Layer = layer;
                background.PaletteIndex = 49;
                background.TextureAtlasOffset = texCoords;
                background.X = slotPositions[i].X;
                background.Y = slotPositions[i].Y;
                background.Visible = true;
            }
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

            foreach (var background in slotBackgrounds)
                background?.Delete();

            hoveredItemName?.Delete();
            hoveredItemName = null;

            scrollbar?.Destroy();
            scrollbar = null;

            dragScrollItem = null;
        }

        public void SetItem(int slot, ItemSlot item)
        {
            items[slot]?.Destroy();

            if (item == null || item.Empty)
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
            int? newSlot = Dropping?.Invoke(slot, itemManager.GetItem(item.Item.ItemIndex));

            if (newSlot != null)
                slot = newSlot.Value;

            if (slot == -1)
                return item.Item.Amount;

            var itemSlot = items[slot];

            if (itemSlot == null)
            {
                item.Dragged = false;
                items[slot] = item;
                item.Position = slotPositions[slot - ScrollOffset];
                ItemDropped?.Invoke(slot, itemManager.GetItem(item.Item.ItemIndex));
                return 0;
            }
            else if (itemSlot.Item.Empty || itemSlot.Item.ItemIndex == item.Item.ItemIndex)
            {
                int remaining = itemSlot.Item.Add(item.Item);
                itemSlot.Update(false);

                if (remaining == 0)
                    item.Destroy();
                else
                    item.Update(false);

                ItemDropped?.Invoke(slot, itemManager.GetItem(itemSlot.Item.ItemIndex));

                return remaining;
            }
            else
            {
                if (!itemSlot.Item.Draggable)
                    return item.Item.Amount;

                itemSlot.Item.Exchange(item.Item);
                itemSlot.Update(true);
                itemSlot.Position = itemSlot.Position; // Important to re-position amount display if added
                item.Update(true);
                ItemDragged?.Invoke(slot, itemManager.GetItem(item.Item.ItemIndex));
                ItemDropped?.Invoke(slot, itemManager.GetItem(itemSlot.Item.ItemIndex));
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
            out Layout.DraggedItem pickedUpItem, bool leftMouseButton, ref CursorType cursorType)
        {
            pickedUpItem = draggedItem;

            if (disabled)
                return false;

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

                if (itemSlot != null && itemSlot.Item.Draggable)
                {
                    pickedUpItem = pickupAction(this, slot.Value, itemSlot);
                    ItemDragged?.Invoke(slot.Value, itemManager.GetItem(pickedUpItem.Item.Item.ItemIndex));
                    items[slot.Value] = null;
                    Hover(position); // This updates the tooltip
                }
            }

            return true;
        }

        public bool Hover(Position position)
        {
            var slot = SlotFromPosition(position);

            if (disabled || slot == null || items[slot.Value]?.Visible != true ||
                items[slot.Value]?.Item?.Empty == true || items[slot.Value]?.Item.ItemIndex == 0)
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
                hoveredItemName.Y = position.Y - Global.GlyphLineHeight - 1;
                hoveredItemName.Visible = true;

                return true;
            }
        }
    }
}
