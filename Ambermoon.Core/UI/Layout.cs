using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
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

    enum FilledAreaType
    {
        CharacterBar,
        FadeEffect,
        Custom
    }

    public class FilledArea
    {
        readonly List<IColoredRect> filledAreas;
        protected readonly IColoredRect area;
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

    public class Bar : FilledArea
    {
        readonly Rect barArea;

        internal Bar(List<IColoredRect> filledAreas, IColoredRect area)
            : base(filledAreas, area)
        {
            barArea = new Rect(area.X, area.Y, area.Width, area.Height);
        }

        /// <summary>
        /// Fills the bar dependent on the given value.
        /// </summary>
        /// <param name="percentage">Value in the range 0 to 1 (0 to 100%).</param>
        public void Fill(float percentage)
        {
            // 100% = 16 pixels
            int pixels = Util.Round(16.0f * percentage);

            if (pixels == 0)
                area.Visible = false;
            else
            {
                area.X = barArea.Left;
                area.Y = barArea.Bottom - pixels;
                area.Resize(barArea.Width, pixels);
                area.Visible = true;
            }
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
        readonly ILayerSprite sprite;
        readonly ITextureAtlas textureAtlasBackground;
        readonly ITextureAtlas textureAtlasForeground;
        readonly List<ISprite> portraitBorders = new List<ISprite>();
        readonly ISprite[] portraitBackgrounds = new ISprite[Game.MaxPartyMembers];
        readonly ILayerSprite[] portraitBarBackgrounds = new ILayerSprite[Game.MaxPartyMembers];
        readonly ISprite[] portraits = new ISprite[Game.MaxPartyMembers];
        readonly IRenderText[] portraitNames = new IRenderText[Game.MaxPartyMembers];
        readonly Bar[] characterBars = new Bar[Game.MaxPartyMembers * 4]; // 2 bars and each has fill and shadow color
        ISprite sprite80x80Picture;
        readonly List<ItemGrid> itemGrids = new List<ItemGrid>();
        DraggedItem draggedItem = null;
        readonly List<IColoredRect> barAreas = new List<IColoredRect>();
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        readonly List<IColoredRect> fadeEffectAreas = new List<IColoredRect>();
        readonly List<FadeEffect> fadeEffects = new List<FadeEffect>();
        readonly List<ISprite> additionalSprites = new List<ISprite>();
        readonly List<IRenderText> texts = new List<IRenderText>();
        readonly ButtonGrid buttonGrid;
        int buttonGridPage = 0;
        uint? ticksPerMovement = null;
        internal IRenderView RenderView { get; }

        public Layout(Game game, IRenderView renderView)
        {
            this.game = game;
            RenderView = renderView;
            textureAtlasBackground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIBackground);
            textureAtlasForeground = TextureAtlasManager.Instance.GetOrCreate(Layer.UIForeground);

            sprite = RenderView.SpriteFactory.Create(320, 163, false, true) as ILayerSprite;
            sprite.Layer = RenderView.GetLayer(Layer.UIBackground);
            sprite.X = Global.LayoutX;
            sprite.Y = Global.LayoutY;
            sprite.DisplayLayer = 1;
            sprite.PaletteIndex = 0;

            AddStaticSprites();

            buttonGrid = new ButtonGrid(renderView);
            buttonGrid.RightMouseClicked += ButtonGrid_RightMouseClicked;

            SetLayout(LayoutType.None);
        }

        void ButtonGrid_RightMouseClicked()
        {
            if (Type == LayoutType.Map2D ||
                Type == LayoutType.Map3D)
            {
                if (game.CursorType == CursorType.Sword)
                {
                    buttonGridPage = 1 - buttonGridPage;
                    SetLayout(Type, ticksPerMovement);
                }
            }
        }

        void AddStaticSprites()
        {
            var backgroundLayer = RenderView.GetLayer(Layer.UIBackground);

            var barBackgroundTexCoords = textureAtlasBackground.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.CharacterValueBarFrames));
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var barBackgroundSprite = portraitBarBackgrounds[i] = RenderView.SpriteFactory.Create(16, 36, false, true) as ILayerSprite;
                barBackgroundSprite.Layer = backgroundLayer;
                barBackgroundSprite.PaletteIndex = 49;
                barBackgroundSprite.TextureAtlasOffset = barBackgroundTexCoords;
                barBackgroundSprite.X = Global.PartyMemberPortraitAreas[i].Left + 33;
                barBackgroundSprite.Y = Global.PartyMemberPortraitAreas[i].Top;
                barBackgroundSprite.Visible = true;
            }

            // Left portrait border
            var sprite = RenderView.SpriteFactory.Create(16, 36, false, true);
            sprite.Layer = backgroundLayer;
            sprite.PaletteIndex = 49;
            sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.LeftPortraitBorder));
            sprite.X = 0;
            sprite.Y = 0;
            sprite.Visible = true;
            portraitBorders.Add(sprite);

            // Right portrait border
            sprite = RenderView.SpriteFactory.Create(16, 36, false, true);
            sprite.Layer = backgroundLayer;
            sprite.PaletteIndex = 49;
            sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.RightPortraitBorder));
            sprite.X = Global.VirtualScreenWidth - 16;
            sprite.Y = 0;
            sprite.Visible = true;
            portraitBorders.Add(sprite);

            // Thin portrait borders
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                sprite = RenderView.SpriteFactory.Create(32, 1, false, true);
                sprite.Layer = backgroundLayer;
                sprite.PaletteIndex = 49;
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 0;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                sprite = RenderView.SpriteFactory.Create(32, 1, false, true);
                sprite.Layer = backgroundLayer;
                sprite.PaletteIndex = 49;
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 35;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                // LP shadow
                characterBars[i * 4 + 0] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 2, 19, 1, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.LPBarShadow), false, FilledAreaType.CharacterBar));
                // LP fill
                characterBars[i * 4 + 1] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 3, 19, 3, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.LPBar), false, FilledAreaType.CharacterBar));
                // SP shadow
                characterBars[i * 4 + 2] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 10, 19, 1, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.SPBarShadow), false, FilledAreaType.CharacterBar));
                // SP fill
                characterBars[i * 4 + 3] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 11, 19, 3, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.SPBar), false, FilledAreaType.CharacterBar));
            }
        }

        public void SetLayout(LayoutType layoutType, uint? ticksPerMovement = null)
        {
            this.ticksPerMovement = ticksPerMovement;
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

            UpdateLayoutButtons(ticksPerMovement);
        }

        void UpdateLayoutButtons(uint? ticksPerMovement = null)
        {
            switch (Type)
            {
                case LayoutType.Map2D:
                    if (buttonGridPage == 0)
                    {
                        var moveDelay = ticksPerMovement.Value;
                        buttonGrid.SetButton(0, ButtonType.MoveUpLeft, () => game.Move(CursorType.ArrowUpLeft), true, null, moveDelay);
                        buttonGrid.SetButton(1, ButtonType.MoveUp, () => game.Move(CursorType.ArrowUp), true, null, moveDelay);
                        buttonGrid.SetButton(2, ButtonType.MoveUpRight, () => game.Move(CursorType.ArrowUpRight), true, null, moveDelay);
                        buttonGrid.SetButton(3, ButtonType.MoveLeft, () => game.Move(CursorType.ArrowLeft), true, null, moveDelay);
                        buttonGrid.SetButton(4, ButtonType.Wait, null, true); // TODO: wait
                        buttonGrid.SetButton(5, ButtonType.MoveRight, () => game.Move(CursorType.ArrowRight), true, null, moveDelay);
                        buttonGrid.SetButton(6, ButtonType.MoveDownLeft, () => game.Move(CursorType.ArrowDownLeft), true, null, moveDelay);
                        buttonGrid.SetButton(7, ButtonType.MoveDown, () => game.Move(CursorType.ArrowDown), true, null, moveDelay);
                        buttonGrid.SetButton(8, ButtonType.MoveDownRight, () => game.Move(CursorType.ArrowDownRight), true, null, moveDelay);
                    }
                    else
                    {
                        buttonGrid.SetButton(0, ButtonType.Eye, null, false, () => CursorType.Eye);
                        buttonGrid.SetButton(1, ButtonType.Hand, null, false, () => CursorType.Hand);
                        buttonGrid.SetButton(2, ButtonType.Mouth, null, false, () => CursorType.Mouth);
                        buttonGrid.SetButton(3, ButtonType.Transport, () => game.Move(CursorType.ArrowLeft), true);
                        buttonGrid.SetButton(4, ButtonType.Spells, null, false); // TODO: spells
                        buttonGrid.SetButton(5, ButtonType.Camp, null, false); // TODO: camp
                        buttonGrid.SetButton(6, ButtonType.Map, null, false); // TODO: map
                        buttonGrid.SetButton(7, ButtonType.BattlePositions, null, false); // TODO: battle positions
                        buttonGrid.SetButton(8, ButtonType.Options, null, false); // TODO: options
                    }
                    break;
                case LayoutType.Map3D:
                    break;
                // TODO
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
            texts.ForEach(text => text?.Delete());
            texts.Clear();

            // Note: Don't remove fadeEffects here.
        }

        public void SetActiveCharacter(int slot, List<PartyMember> partyMembers)
        {
            for (int i = 0; i < portraitNames.Length; ++i)
            {
                if (portraitNames[i] != null)
                {
                    portraitNames[i].TextColor = i == slot ? TextColor.Yellow : partyMembers[i].Alive ? TextColor.Red : TextColor.PaleGray;
                }
            }
        }

        /// <summary>
        /// Set portait to 0 to remove the portrait.
        /// </summary>
        public void SetCharacter(int slot, PartyMember partyMember)
        {
            var sprite = portraits[slot] ??= RenderView.SpriteFactory.Create(32, 34, false, true, 1);
            sprite.Layer = RenderView.GetLayer(partyMember == null || !partyMember.Alive ? Layer.UIBackground : Layer.UIForeground);
            sprite.X = Global.PartyMemberPortraitAreas[slot].Left + 1;
            sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1;
            if (partyMember == null)
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.EmptyCharacterSlot));
            else if (!partyMember.Alive)
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Skull));
            else
                sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(Graphics.PortraitOffset + partyMember.PortraitIndex - 1);
            sprite.PaletteIndex = 49;
            sprite.Visible = true;

            if (partyMember == null)
            {
                // TODO: in original portrait removing is animated by moving down the
                // gray masked picture infront of the portrait. But this method is
                // also used on game loading where this effect should not be used.

                portraitBackgrounds[slot]?.Delete();
                portraitBackgrounds[slot] = null;
                portraitNames[slot]?.Delete();
                portraitNames[slot] = null;
            }
            else
            {
                sprite = portraitBackgrounds[slot] ??= RenderView.SpriteFactory.Create(32, 34, false, true, 0);
                sprite.Layer = RenderView.GetLayer(Layer.UIBackground);
                sprite.X = Global.PartyMemberPortraitAreas[slot].Left + 1;
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1;
                sprite.TextureAtlasOffset = textureAtlasBackground.GetOffset(Graphics.UIElementOffset + (uint)UICustomGraphic.PortraitBackground);
                sprite.PaletteIndex = 50;
                sprite.Visible = true;

                var text = portraitNames[slot] ??= RenderView.RenderTextFactory.Create(RenderView.GetLayer(Layer.Text),
                    RenderView.TextProcessor.CreateText(partyMember.Name.Substring(0, Math.Min(5, partyMember.Name.Length))), TextColor.Red, true,
                    new Rect(Global.PartyMemberPortraitAreas[slot].Left + 2, Global.PartyMemberPortraitAreas[slot].Top + 31, 30, 6), TextAlign.Center);
                text.DisplayLayer = 1;
                text.TextColor = partyMember.Alive ? TextColor.Red : TextColor.PaleGray;
                text.Visible = true;
            }

            FillCharacterBars(slot, partyMember);
        }

        void FillCharacterBars(int slot, PartyMember partyMember)
        {
            float lpPercentage = partyMember == null ? 0.0f : (float)partyMember.HitPoints.TotalCurrentValue / partyMember.HitPoints.MaxValue;
            float spPercentage = partyMember == null ? 0.0f : (float)partyMember.SpellPoints.TotalCurrentValue / partyMember.SpellPoints.MaxValue;

            characterBars[slot * 4 + 0].Fill(lpPercentage);
            characterBars[slot * 4 + 1].Fill(lpPercentage);
            characterBars[slot * 4 + 2].Fill(spPercentage);
            characterBars[slot * 4 + 3].Fill(spPercentage);
        }

        public void AddSprite(Rect rect, uint textureIndex, byte paletteIndex, bool background, byte displayLayer = 0)
        {
            var sprite = RenderView.SpriteFactory.Create(rect.Width, rect.Height, false, true) as ILayerSprite;
            sprite.TextureAtlasOffset = (background ? textureAtlasBackground : textureAtlasForeground).GetOffset(textureIndex);
            sprite.DisplayLayer = displayLayer;
            sprite.X = rect.Left;
            sprite.Y = rect.Top;
            sprite.PaletteIndex = paletteIndex;
            sprite.Layer = RenderView.GetLayer(background ? Layer.UIBackground : Layer.UIForeground);
            sprite.Visible = true;
            additionalSprites.Add(sprite);
        }

        public void AddText(Rect rect, string text, TextColor color = TextColor.White, TextAlign textAlign = TextAlign.Left, byte displayLayer = 1)
        {
            var renderText = RenderView.RenderTextFactory.Create
            (
                RenderView.GetLayer(Layer.Text),
                RenderView.TextProcessor.CreateText(text),
                color, true, rect, textAlign
            );
            renderText.DisplayLayer = displayLayer;
            renderText.Visible = true;
            texts.Add(renderText);
        }

        public void Set80x80Picture(Picture80x80 picture)
        {
            if (picture == Picture80x80.None)
            {
                if (sprite80x80Picture != null)
                    sprite80x80Picture.Visible = false;
            }
            else
            {
                var sprite = sprite80x80Picture ??= RenderView.SpriteFactory.Create(80, 80, false, true);
                sprite.TextureAtlasOffset = textureAtlasForeground.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = Global.LayoutX + 16;
                sprite.Y = Global.LayoutY + 6;
                sprite.PaletteIndex = 49;
                sprite.Layer = RenderView.GetLayer(Layer.UIForeground);
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

        IColoredRect CreateArea(Rect rect, Color color, bool topMost, FilledAreaType type = FilledAreaType.Custom)
        {
            var coloredRect = RenderView.ColoredRectFactory.Create(rect.Width, rect.Height,
                color, (byte)(topMost ? 255 : type == FilledAreaType.CharacterBar ? 1 : 0));
            coloredRect.Layer = RenderView.GetLayer(topMost ? Layer.Popup : Layer.UIBackground);
            coloredRect.X = rect.Left;
            coloredRect.Y = rect.Top;
            coloredRect.Visible = true;
            switch (type)
            {
                case FilledAreaType.CharacterBar:
                    barAreas.Add(coloredRect);
                    break;
                case FilledAreaType.FadeEffect:
                    fadeEffectAreas.Add(coloredRect);
                    break;
                default:
                    filledAreas.Add(coloredRect);
                    break;
            }               
            return coloredRect;
        }

        public FilledArea FillArea(Rect rect, Color color, bool topMost)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, topMost));
        }

        public void AddColorFader(Rect rect, Color startColor, Color endColor,
            int durationInMilliseconds, bool removeWhenFinished, DateTime? startTime = null)
        {
            fadeEffects.Add(new FadeEffect(fadeEffectAreas, CreateArea(rect, startColor, true, FilledAreaType.FadeEffect), startColor,
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

        public void Update(uint currentTicks)
        {
            buttonGrid.Update(currentTicks);

            for (int i = fadeEffects.Count - 1; i >= 0; --i)
            {
                fadeEffects[i].Update();

                if (fadeEffects[i].Destroyed)
                    fadeEffects.RemoveAt(i);
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

        public void LeftMouseUp(Position position, out CursorType? newCursorType, uint currentTicks)
        {
            newCursorType = null;

            buttonGrid.MouseUp(position, MouseButtons.Left, out CursorType? cursorType, currentTicks);

            if (cursorType != null)
            {
                newCursorType = cursorType;
                return;
            }

            foreach (var itemGrid in itemGrids)
                itemGrid.LeftMouseUp(position);
        }

        public void RightMouseUp(Position position, out CursorType? newCursorType, uint currentTicks)
        {
            buttonGrid.MouseUp(position, MouseButtons.Right, out newCursorType, currentTicks);
        }

        public bool Click(Position position, MouseButtons buttons, ref CursorType cursorType,
            uint currentTicks)
        {
            if (buttonGrid.MouseDown(position, buttons, out CursorType? newCursorType, currentTicks))
            {
                if (newCursorType != null)
                    cursorType = newCursorType.Value;
                return true;
            }

            if (buttons == MouseButtons.Left)
            {
                foreach (var itemGrid in itemGrids)
                {
                    // TODO: If stacked it should ask for amount with left mouse
                    if (itemGrid.Click(position, draggedItem, out DraggedItem pickedUpItem, true, ref cursorType))
                    {
                        if (pickedUpItem != null)
                        {
                            draggedItem = pickedUpItem;
                            draggedItem.Item.Position = position;
                            draggedItem.SourcePlayer = IsInventory ? game.CurrentInventoryIndex : null;
                        }
                        else
                            DropItem();

                        return true;
                    }
                }
            }
            else if (buttons == MouseButtons.Right)
            {
                if (draggedItem == null)
                {
                    cursorType = CursorType.Sword;

                    foreach (var itemGrid in itemGrids)
                    {
                        if (itemGrid.Click(position, null, out DraggedItem pickedUpItem, false, ref cursorType))
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
            else if (cursorType >= CursorType.ArrowUp && cursorType <= CursorType.Wait)
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
