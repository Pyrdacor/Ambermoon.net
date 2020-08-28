using Ambermoon.Data;
using Ambermoon.Render;
using System.Collections.Generic;

namespace Ambermoon.UI
{
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

        public int SlotCount => slotPositions.Count;

        public ItemGrid(IRenderView renderView, IItemManager itemManager, List<Position> slotPositions,
            bool allowExternalDrop)
        {
            this.renderView = renderView;
            this.itemManager = itemManager;
            this.slotPositions = slotPositions;
            this.allowExternalDrop = allowExternalDrop;
            items = new UIItem[slotPositions.Count];
        }

        public void Destroy()
        {
            for (int i = 0; i < items.Length; ++i)
                SetItem(i, null);
        }

        public void SetItem(int slot, ItemSlot item)
        {
            items[slot]?.Destroy();

            if (item == null)
                items[slot] = null;
            else
            {
                var newItem = items[slot] = new UIItem(renderView, itemManager, item);
                newItem.Position = slotPositions[slot];
                newItem.Visible = true;
            }
        }

        public ItemSlot GetItem(int slot) => items[slot]?.Item;

        public int? SlotFromPosition(Position position)
        {
            int slot = 0;

            foreach (var slotPosition in slotPositions)
            {
                if (new Rect(slotPosition, SlotSize).Contains(position))
                    return slot;

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

        public bool Click(Game game, Position position, Layout.DraggedItem draggedItem, out Layout.DraggedItem pickedUpItem)
        {
            pickedUpItem = draggedItem;

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
                    pickedUpItem = Layout.DraggedItem.FromExternal(this, slot.Value, itemSlot);
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
