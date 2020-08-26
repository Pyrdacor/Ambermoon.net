using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ambermoon.UI
{
    /*{ 22,  37, 139, 162}, 0x0100, {
    { 44,  59, 139, 162}, 0x0200, {
    { 66,  81, 139, 162}, 0x0300, {
    { 88, 103, 139, 162}, 0x0400, {
    {110, 125, 139, 162}, 0x0500, {
    {132, 147, 139, 162}, 0x0600, {
    { 22,  37, 168, 191}, 0x0700, {
    { 44,  59, 168, 191}, 0x0800, {
    { 66,  81, 168, 191}, 0x0900, {
    { 88, 103, 168, 191}, 0x0A00, {
    {110, 125, 168, 191}, 0x0B00, {
    {132, 147, 168, 191}, 0x0C00, {
*/

    public class ItemGrid
    {
        const int SlotWidth = 16;
        const int SlotHeight = 24;
        public static readonly Size SlotSize = new Size(SlotWidth, SlotHeight);
        readonly IRenderView renderView;
        readonly IItemManager itemManager;
        readonly List<Position> slotPositions;
        readonly UIItem[] items;

        public ItemGrid(IRenderView renderView, IItemManager itemManager, List<Position> slotPositions)
        {
            this.renderView = renderView;
            this.itemManager = itemManager;
            this.slotPositions = slotPositions;
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

        public int SlotFromPosition(Position position)
        {
            int slot = 0;

            foreach (var slotPosition in slotPositions)
            {
                if (new Rect(slotPosition, SlotSize).Contains(position))
                    return slot;

                ++slot;
            }

            return -1;
        }
    }
}
