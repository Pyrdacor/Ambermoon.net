using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    public enum LayoutType
    {
        None,
        Map2D,
        Inventory,
        Items, // Chest, merchant, battle loot and other places like trainers etc
        Battle,
        Map3D,
        Unknown2,
        Event, // Game over, airship travel, grandfather intro, valdyn sequence
        Conversation,
        Riddlemouth,
        BattlePositions,
        Automap
    }

    public enum FadeEffectType
    {
        FadeIn,
        FadeOut,
        FadeInAndOut
    }

    public class FilledArea
    {
        readonly List<IColoredRect> filledAreas;
        readonly IColoredRect area;
        internal bool Destroyed { get; private set; } = false;

        internal FilledArea(List<IColoredRect> filledAreas, IColoredRect area)
        {
            this.filledAreas = filledAreas;
            this.area = area;
        }

        public Color Color
        {
            get => Destroyed ? null : area.Color;
            set
            {
                if (!Destroyed)
                    area.Color = value;
            }
        }

        public byte DisplayLayer
        {
            get => Destroyed ? (byte)0 : area.DisplayLayer;
            set
            {
                if (!Destroyed)
                    area.DisplayLayer = value;
            }
        }

        public bool Visible
        {
            get => !Destroyed && area.Visible;
            set
            {
                if (!Destroyed)
                    area.Visible = value;
            }
        }

        public Position Position
        {
            get => Destroyed ? null : new Position(area.X, area.Y);
            set
            {
                if (!Destroyed)
                {
                    area.X = value.X;
                    area.Y = value.Y;
                }
            }
        }

        public void Destroy()
        {
            if (Destroyed)
                return;

            area.Delete();
            filledAreas.Remove(area);
            Destroyed = true;
        }
    }

    public class FadeEffect : FilledArea
    {
        readonly Color startColor;
        readonly Color endColor;
        readonly int duration;
        readonly DateTime startTime;
        readonly bool removeWhenFinished;

        internal FadeEffect(List<IColoredRect> filledAreas, IColoredRect area, Color startColor,
            Color endColor, int durationInMilliseconds, DateTime startTime, bool removeWhenFinished)
            : base(filledAreas, area)
        {
            this.startColor = startColor;
            this.endColor = endColor;
            duration = durationInMilliseconds;
            this.startTime = startTime;
            this.removeWhenFinished = removeWhenFinished;
        }

        public void Update()
        {
            bool Finished()
            {
                if (removeWhenFinished)
                {
                    Destroy();
                    return true;
                }

                return false;
            }

            float percentage;

            if (duration == 0)
            {
                percentage = 1.0f;

                if (Finished())
                    return;
            }
            else
            {
                var now = DateTime.Now;

                if (now <= startTime)
                    percentage = 0.0f;
                else
                {
                    var elapsed = (int)(now - startTime).TotalMilliseconds;

                    if (elapsed >= duration && Finished())
                        return;

                    percentage = (float)elapsed / duration;
                }
            }

            byte CalculateColorComponent(byte start, byte end)
            {
                if (start < end)
                    return (byte)(start + Util.Round((end - start) * percentage));
                else
                    return (byte)(start - Util.Round((start - end) * percentage));
            }

            Color = new Color
            (
                CalculateColorComponent(startColor.R, endColor.R),
                CalculateColorComponent(startColor.G, endColor.G),
                CalculateColorComponent(startColor.B, endColor.B),
                CalculateColorComponent(startColor.A, endColor.A)
            );
        }
    }

    internal class Layout
    {
        // There are a few possibilities:
        // 1. Move item from a player inventory directly to another player via his portrait.
        // 2. Move item from a player inventory, opening a second players inventory with
        //    right mouse and drop it there.
        // 3. Move item from a chest (etc) directly to another player via his portrait.
        internal class DraggedItem
        {
            public UIItem Item { get; set; }
            public ItemGrid SourceGrid { get; set; }
            public int? SourcePlayer { get; set; }
            public bool? Equipped { get; set; }
            public int SourceSlot { get; set; }

            /// <summary>
            /// Drop back to source.
            /// </summary>
            public void Reset(Game game)
            {
                // Reset in case 1: Is only possible while in first player inventory.
                // Reset in case 2: Is also possible while in second player inventory.
                //                  First players inventory is opened in addition on reset.
                // Reset in case 3: Is only possible while in chest screen.
                bool updateGrid = true;

                if (SourcePlayer != null)
                {
                    if (Equipped == true)
                    {
                        var equipment = game.GetPartyMember(SourcePlayer.Value).Equipment;
                        equipment.Slots[(EquipmentSlot)(SourceSlot + 1)] = Item.Item;
                    }
                    else
                    {
                        var inventory = game.GetPartyMember(SourcePlayer.Value).Inventory;
                        inventory.Slots[SourceSlot] = Item.Item;
                    }

                    if (game.CurrentInventoryIndex != SourcePlayer)
                        updateGrid = false;
                }

                if (updateGrid && SourceGrid != null)
                    SourceGrid.SetItem(SourceSlot, Item.Item);

                Item.Destroy();
            }

            private DraggedItem()
            {

            }

            public static DraggedItem FromInventory(ItemGrid itemGrid, int partyMemberIndex, int slotIndex, UIItem item, bool equipped)
            {
                item.Dragged = true;

                return new DraggedItem
                {
                    SourceGrid = itemGrid,
                    SourcePlayer = partyMemberIndex,
                    Equipped = equipped,
                    SourceSlot = slotIndex,
                    Item = item
                };
            }

            /// <summary>
            /// Chests, merchants, etc.
            /// </summary>
            /// <returns></returns>
            public static DraggedItem FromExternal(ItemGrid itemGrid, int slotIndex, UIItem item)
            {
                item.Dragged = true;

                return new DraggedItem
                {
                    SourceGrid = itemGrid,
                    SourceSlot = slotIndex,
                    Item = item
                };
            }
        }

        public LayoutType Type { get; private set; }
        readonly Game game;
        readonly IRenderView renderView;
        readonly ILayerSprite sprite;
        readonly ITextureAtlas textureAtlasBackground;
        readonly ITextureAtlas textureAtlasForeground;
        readonly ISprite[] portraitBackgrounds = new ISprite[Game.MaxPartyMembers];
        readonly ISprite[] portraits = new ISprite[Game.MaxPartyMembers];
        ISprite sprite80x80Picture;
        readonly List<ItemGrid> itemGrids = new List<ItemGrid>();
        DraggedItem draggedItem = null;
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        readonly List<FadeEffect> fadeEffects = new List<FadeEffect>();
        readonly List<ISprite> additionalSprites = new List<ISprite>();
        internal IRenderView RenderView => renderView;

        public Layout(Game game, IRenderView renderView)
        {
            this.game = game;
            this.renderView = renderView;
            textureAtlasBackground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground);
            textureAtlasForeground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIForeground);
            sprite = renderView.SpriteFactory.Create(320, 163, 0, 0, false, true) as ILayerSprite;
            sprite.Layer = renderView.GetLayer(Layer.UIBackground);
            sprite.X = Global.LayoutX;
            sprite.Y = Global.LayoutY;
            sprite.DisplayLayer = 1;
            sprite.PaletteIndex = 0;

            SetLayout(LayoutType.None);
        }

        public void SetLayout(LayoutType layoutType)
        {
            Type = layoutType;

            if (layoutType == LayoutType.None)
            {
                sprite.Visible = false;
            }
            else
            {
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.LayoutOffset + (uint)(layoutType - 1));
                sprite.Visible = true;
            }
        }

        public void Reset()
        {
            sprite80x80Picture?.Delete();
            sprite80x80Picture = null;
            additionalSprites.ForEach(sprite => sprite?.Delete());
            additionalSprites.Clear();
            itemGrids.ForEach(grid => grid.Destroy());
            itemGrids.Clear();
            filledAreas.ForEach(area => area?.Delete());
            filledAreas.Clear();
        }

        /// <summary>
        /// Set portait to 0 to remove the portrait.
        /// </summary>
        public void SetPortrait(int slot, uint portrait)
        {
            if (portrait == 0)
            {
                // TODO: in original portrait removing is animated by moving down the
                // gray masked picture infront of the portrait

                portraitBackgrounds[slot]?.Delete();
                portraitBackgrounds[slot] = null;
                portraits[slot]?.Delete();
                portraits[slot] = null;
            }
            else
            {
                var sprite = portraitBackgrounds[slot] ??= renderView.SpriteFactory.Create(32, 34, 0, 0, false, true, 0);
                sprite.Layer = renderView.GetLayer(Layer.UIForeground);
                sprite.X = Global.PartyMemberPortraitAreas[slot].Left;
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top;
                sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(Graphics.PortraitBackgroundOffset);
                sprite.PaletteIndex = 50;
                sprite.Visible = true;

                sprite = portraits[slot] ??= renderView.SpriteFactory.Create(32, 34, 0, 0, false, true, 1);
                sprite.Layer = renderView.GetLayer(Layer.UIForeground);
                sprite.X = Global.PartyMemberPortraitAreas[slot].Left;
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top;
                sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(Graphics.PortraitOffset + portrait - 1);
                sprite.PaletteIndex = 49;
                sprite.Visible = true;
            }
        }

        public void AddSprite(Rect rect, uint textureIndex, byte paletteIndex)
        {
            var sprite = renderView.SpriteFactory.Create(rect.Size.Width, rect.Size.Height, 0, 0, false, true);
            sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(textureIndex);
            sprite.X = rect.Left;
            sprite.Y = rect.Top;
            sprite.PaletteIndex = paletteIndex;
            sprite.Layer = renderView.GetLayer(Layer.UIForeground);
            sprite.Visible = true;
            additionalSprites.Add(sprite);
        }

        public void Set80x80Picture(Data.Enumerations.Picture80x80 picture)
        {
            if (picture == Data.Enumerations.Picture80x80.None)
            {
                if (sprite80x80Picture != null)
                    sprite80x80Picture.Visible = false;
            }
            else
            {
                var sprite = sprite80x80Picture ??= renderView.SpriteFactory.Create(80, 80, 0, 0, false, true);
                sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = Global.LayoutX + 16;
                sprite.Y = Global.LayoutY + 6;
                sprite.PaletteIndex = 49;
                sprite.Layer = renderView.GetLayer(Layer.UIForeground);
                sprite.Visible = true;
            }
        }

        void DropItem()
        {
            draggedItem = null;
        }

        bool IsInventory => Type == LayoutType.Inventory;

        public void AddItemGrid(ItemGrid itemGrid)
        {
            itemGrids.Add(itemGrid);
        }

        IColoredRect CreateArea(Rect rect, Color color, bool topMost)
        {
            var coloredRect = renderView.ColoredRectFactory.Create(rect.Size.Width, rect.Size.Height,
                color, (byte)(topMost ? 255 : 0));
            coloredRect.Layer = renderView.GetLayer(topMost ? Layer.Popup : Layer.UIBackground);
            coloredRect.X = rect.Left;
            coloredRect.Y = rect.Top;
            coloredRect.Visible = true;
            filledAreas.Add(coloredRect);
            return coloredRect;
        }

        public FilledArea FillArea(Rect rect, Color color, bool topMost)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, topMost));
        }

        public void AddColorFader(Rect rect, Color startColor, Color endColor,
            int durationInMilliseconds, bool removeWhenFinished, DateTime? startTime = null)
        {
            fadeEffects.Add(new FadeEffect(filledAreas, CreateArea(rect, startColor, true), startColor,
                endColor, durationInMilliseconds, startTime ?? DateTime.Now, removeWhenFinished));
        }

        public void AddFadeEffect(Rect rect, Color color, FadeEffectType fadeEffectType,
            int durationInMilliseconds)
        {
            switch (fadeEffectType)
            {
                case FadeEffectType.FadeIn:
                    AddColorFader(rect, new Color(color, 0), color, durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeOut:
                    AddColorFader(rect, color, new Color(color, 0), durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeInAndOut:
                    var halfDuration = durationInMilliseconds / 2;
                    AddColorFader(rect, new Color(color, 0), color, halfDuration, true);
                    AddColorFader(rect, color, new Color(color, 0), halfDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(halfDuration));
                    break;
            }
        }

        public void Update()
        {
            for (int i = fadeEffects.Count - 1; i >= 0; --i)
            {
                fadeEffects[i].Update();

                if (fadeEffects[i].Destroyed)
                    fadeEffects.RemoveAt(i);
            }
        }

        void SyncInventory(Position position, DraggedItem droppedItem, ItemGrid itemGrid, Game game)
        {
            var currentInventory = game.GetPartyMember(game.CurrentInventoryIndex.Value).Inventory;
            var slot = itemGrid.SlotFromPosition(position).Value;
            currentInventory.Slots[slot].Replace(itemGrid.GetItem(slot));

            if (droppedItem.SourcePlayer != null)
            {
                var sourceInventory = game.GetPartyMember(droppedItem.SourcePlayer.Value).Inventory;
                sourceInventory.Slots[droppedItem.SourceSlot]?.Clear(); // TODO: what if only parts have been dragged?
            }
        }

        public void KeyDown(Key key, KeyModifiers keyModifiers)
        {
            switch (key)
            {
                case Key.Up:
                    if (IsInventory)
                        itemGrids[0].ScrollUp();
                    break;
                case Key.Down:
                    if (IsInventory)
                        itemGrids[0].ScrollDown();
                    break;
                case Key.PageUp:
                    if (IsInventory)
                        itemGrids[0].ScrollPageUp();
                    break;
                case Key.PageDown:
                    if (IsInventory)
                        itemGrids[0].ScrollPageDown();
                    break;
                case Key.Home:
                    if (IsInventory)
                        itemGrids[0].ScrollToBegin();
                    break;
                case Key.End:
                    if (IsInventory)
                        itemGrids[0].ScrollToEnd();
                    break;
            }
        }

        public void LeftMouseUp(Position position)
        {
            foreach (var itemGrid in itemGrids)
                itemGrid.LeftMouseUp(position);
        }

        public bool Click(Position position, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                foreach (var itemGrid in itemGrids)
                {
                    // TODO: If stacked it should ask for amount with left mouse
                    if (itemGrid.Click(game, position, draggedItem, out DraggedItem pickedUpItem, true))
                    {
                        DraggedItem dropped = (draggedItem != null && (pickedUpItem == null || pickedUpItem != draggedItem ||
                            pickedUpItem.Item.Item.Amount != draggedItem.Item.Item.Amount)) ? draggedItem : null;

                        if (pickedUpItem != null)
                        {
                            draggedItem = pickedUpItem;
                            draggedItem.Item.Position = position;
                            draggedItem.SourcePlayer = IsInventory ? game.CurrentInventoryIndex : null;
                        }
                        else
                            DropItem();

                        if (dropped != null && IsInventory)
                        {
                            // sync grid with inventory
                            SyncInventory(position, dropped, itemGrid, game);
                        }

                        return true;
                    }
                }
            }
            else if (buttons == MouseButtons.Right)
            {
                if (draggedItem == null)
                {
                    foreach (var itemGrid in itemGrids)
                    {
                        if (itemGrid.Click(game, position, null, out DraggedItem pickedUpItem, false))
                        {
                            if (pickedUpItem != null)
                            {
                                draggedItem = pickedUpItem;
                                draggedItem.Item.Position = position;
                                draggedItem.SourcePlayer = IsInventory ? game.CurrentInventoryIndex : null;
                            }

                            return true;
                        }
                    }
                }
            }

            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var partyMember = game.GetPartyMember(i);

                if (partyMember == null)
                    continue;

                if (draggedItem != null && Global.ExtendedPartyMemberPortraitAreas[i].Contains(position))
                {
                    if (buttons == MouseButtons.Left)
                    {
                        if (draggedItem.SourcePlayer == i)
                        {
                            draggedItem.Reset(game);
                            draggedItem = null;
                        }
                        else
                        {
                            int remaining = game.DropItem(i, null, draggedItem.Item.Item, false);

                            if (remaining == 0)
                            {
                                draggedItem.Item.Destroy();
                                DropItem();
                            }
                            else
                                draggedItem.Item.Update(false);
                        }
                    }
                    else if (buttons == MouseButtons.Right)
                    {
                        game.OpenPartyMember(i);
                    }

                    return true;
                }
                else if (draggedItem == null && Global.PartyMemberPortraitAreas[i].Contains(position))
                {
                    if (buttons == MouseButtons.Left)
                        game.SetActivePartyMember(i);
                    else if (buttons == MouseButtons.Right)
                        game.OpenPartyMember(i);

                    return true;
                }
            }

            if (buttons == MouseButtons.Right && draggedItem != null)
            {
                draggedItem.Reset(game);
                draggedItem = null;
                return true;
            }

            return false;
        }

        public void Drag(Position position, ref CursorType cursorType)
        {
            foreach (var itemGrid in itemGrids)
            {
                if (itemGrid.Drag(position))
                {
                    cursorType = CursorType.None;
                    break;
                }
            }
        }

        public bool Hover(Position position, ref CursorType cursorType)
        {
            if (draggedItem != null)
            {
                draggedItem.Item.Position = position;
                cursorType = CursorType.SmallArrow;
            }
            else
            {
                cursorType = CursorType.Sword;
            }

            bool consumed = false;

            // Note: We must call Hover for all item grids
            // so that the hovered item text can also be
            // removed if not hovered!
            foreach (var itemGrid in itemGrids)
            {
                if (itemGrid.Hover(position))
                    consumed = true;
            }

            return consumed;
        }
    }
}
