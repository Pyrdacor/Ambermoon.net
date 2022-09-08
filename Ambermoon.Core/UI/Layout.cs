/*
 * Layout.cs - Handles most of the UI interactions
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;

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
        Stats,
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
        Custom,
        CustomEffect
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

        public Render.Color Color
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
        readonly int size;
        readonly bool horizontal;

        internal Bar(List<IColoredRect> filledAreas, IColoredRect area, int size, bool horizontal)
            : base(filledAreas, area)
        {
            barArea = new Rect(area.X, area.Y, area.Width, area.Height);
            this.size = size;
            this.horizontal = horizontal;
        }

        /// <summary>
        /// Fills the bar dependent on the given value.
        /// </summary>
        /// <param name="percentage">Value in the range 0 to 1 (0 to 100%).</param>
        /// <param name="forceNotEmpty">If true at least 1 pixel remaings.</param>
        public void Fill(float percentage, bool forceNotEmpty = false)
        {
            int pixels = Util.Round(size * percentage);

            if (forceNotEmpty && pixels == 0)
                pixels = 1;

            if (pixels == 0)
            {
                area.X = short.MaxValue;
                area.Y = short.MaxValue;
                area.Visible = false;
            }
            else if (horizontal)
            {
                area.X = barArea.Left;
                area.Y = barArea.Top;
                area.Resize(pixels, barArea.Height);
                area.Visible = true;
            }
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
        readonly Render.Color startColor;
        readonly Render.Color endColor;
        readonly int duration;
        readonly DateTime startTime;
        readonly bool removeWhenFinished;

        internal FadeEffect(List<IColoredRect> filledAreas, IColoredRect area, Render.Color startColor,
            Render.Color endColor, int durationInMilliseconds, DateTime startTime, bool removeWhenFinished)
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
                {
                    // Don't draw anything before started.
                    Color = Render.Color.Transparent;
                    return;
                }
                else
                {
                    var elapsed = (int)(now - startTime).TotalMilliseconds;

                    if (elapsed >= duration && Finished())
                        return;

                    percentage = Math.Min(1.0f, (float)elapsed / duration);
                }
            }

            byte CalculateColorComponent(byte start, byte end)
            {
                if (start < end)
                    return (byte)(start + Util.Round((end - start) * percentage));
                else
                    return (byte)(start - Util.Round((start - end) * percentage));
            }

            Color = new Render.Color
            (
                CalculateColorComponent(startColor.R, endColor.R),
                CalculateColorComponent(startColor.G, endColor.G),
                CalculateColorComponent(startColor.B, endColor.B),
                CalculateColorComponent(startColor.A, endColor.A)
            );
        }
    }

    public class Tooltip
    {
        public Rect Area;
        public string Text;
        public TextColor TextColor = TextColor.White;
        public TextAlign TextAlign = TextAlign.Center;
        public Render.Color BackgroundColor = null;
        public bool CenterOnScreen = false;
    }

    internal enum BattleFieldSlotColor
    {
        None,
        Yellow,
        Orange,
        Both = 5 // Only used by blink
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
            /// 
            /// This returns true if dragging should continue.
            /// This is the case when there was an item at the drop location.
            /// </summary>
            public bool Reset(Game game, Layout layout, bool scrollToItem = false)
            {
                // Reset in case 1: Is only possible while in first player inventory.
                // Reset in case 2: Is also possible while in second player inventory.
                //                  First players inventory is opened in addition on reset.
                // Reset in case 3: Is only possible while in chest screen.
                bool updateGrid = true;
                ItemSlot updateSlot = Item.Item;
                ItemSlot previousSlot = new ItemSlot();

                if (SourcePlayer != null)
                {
                    if (game.CurrentInventoryIndex != SourcePlayer)
                    {
                        if (game.OpenPartyMember(SourcePlayer.Value, true))
                        {
                            return layout.draggedItem.Reset(game, layout, true);
                        }                        
                    }

                    var partyMember = game.GetPartyMember(SourcePlayer.Value);

                    if (Equipped == true)
                    {
                        previousSlot.Replace(partyMember.Equipment.Slots[(EquipmentSlot)(SourceSlot + 1)]);
                        if (previousSlot.Empty) // Otherwise DropItem below will handle this
                        {
                            game.EquipmentAdded(Item.Item.ItemIndex, Item.Item.Amount, partyMember);
                            game.UpdateCharacterInfo();
                            layout.FillCharacterBars(partyMember);
                            partyMember.Equipment.Slots[(EquipmentSlot)(SourceSlot + 1)].Add(Item.Item);
                            updateSlot = partyMember.Equipment.Slots[(EquipmentSlot)(SourceSlot + 1)];
                        }
                    }
                    else
                    {
                        previousSlot.Replace(partyMember.Inventory.Slots[SourceSlot]);
                        if (previousSlot.Empty) // Otherwise DropItem below will handle this
                        {
                            game.InventoryItemAdded(Item.Item.ItemIndex, Item.Item.Amount, partyMember);
                            game.UpdateCharacterInfo();
                            partyMember.Inventory.Slots[SourceSlot].Add(Item.Item);
                            updateSlot = partyMember.Inventory.Slots[SourceSlot];
                        }
                    }

                    if (game.CurrentInventoryIndex != SourcePlayer)
                    {
                        updateGrid = false;
                    }
                    else
                    {
                        // Note: When switching to another inventory and back to the
                        // source inventory the current ItemGrid and the SourceGrid
                        // are two different instances even if they represent the
                        // same inventory. Therefore we have to update the SourceGrid.
                        if (SourceGrid != null)
                            SourceGrid = layout.itemGrids[Equipped == true ? 1 : 0];
                    }
                }
                else if (game.OpenStorage != null)
                {
                    previousSlot.Replace(game.OpenStorage.Slots[SourceSlot % 6, SourceSlot / 6]);
                    if (SourceGrid.DropItem(SourceSlot, this) == 0)
                    {
                        if (!previousSlot.Empty)
                            updateGrid = false;
                        previousSlot = new ItemSlot();
                    }
                }

                if (scrollToItem && Equipped != true)
                {
                    // Note: The grid may haved changed through OpenPartyMember!
                    layout.itemGrids[0].ScrollTo(Math.Max(0, SourceSlot - Inventory.VisibleWidth));
                }

                if (!previousSlot.Empty) // There is an item at the target slot (dragged item was exchanged before)
                {
                    if (SourcePlayer != null && SourceGrid.DropItem(SourceSlot, this) == 0)
                        layout.DropItem();
                    return true;
                }

                if (updateGrid && SourceGrid != null)
                    SourceGrid.SetItem(SourceSlot, updateSlot);

                Item.Destroy();

                return false;
            }

            private DraggedItem()
            {

            }

            public static DraggedItem FromInventory(ItemGrid itemGrid, int partyMemberIndex, int slotIndex, UIItem item, bool equipped)
            {
                var clone = item.Clone();
                clone.Dragged = true;
                clone.Visible = true;

                return new DraggedItem
                {
                    SourceGrid = itemGrid,
                    SourcePlayer = partyMemberIndex,
                    Equipped = equipped,
                    SourceSlot = slotIndex,
                    Item = clone
                };
            }

            /// <summary>
            /// Chests, merchants, etc.
            /// </summary>
            /// <returns></returns>
            public static DraggedItem FromExternal(ItemGrid itemGrid, int slotIndex, UIItem item)
            {
                var clone = item.Clone();
                clone.Dragged = true;
                clone.Visible = true;

                return new DraggedItem
                {
                    SourceGrid = itemGrid,
                    SourceSlot = slotIndex,
                    Item = clone
                };
            }
        }

        class MonsterCombatGraphic
        {
            public Monster Monster { get; set; }
            public int Row{ get; set; }
            public int Column { get; set; }
            public BattleAnimation Animation { get; set; }
            public ILayerSprite BattleFieldSprite { get; set; }
            public Tooltip Tooltip { get; set; }
        }

        class PortraitAnimation
        {
            public uint StartTicks;
            public int Offset;
            public ISprite PrimarySprite;
            public ISprite SecondarySprite;
            public event Action Finished;

            public void OnFinished() => Finished?.Invoke();
        }

        enum PartyMemberPortaitState
        {
            None,
            Empty,
            Normal,
            Dead
        }

        class BattleFieldSlotMarker
        {
            public ISprite Sprite = null;
            public uint? BlinkStartTicks = null;
            public bool ToggleColors = false;
        }

        public LayoutType Type { get; private set; }
        readonly Game game;
        readonly ILayerSprite sprite;
        readonly ITextureAtlas textureAtlas;
        readonly IRenderLayer renderLayer;
        readonly IRenderLayer textLayer;
        readonly IItemManager itemManager;
        readonly List<ISprite> portraitBorders = new List<ISprite>();
        readonly ISprite[] portraitBackgrounds = new ISprite[Game.MaxPartyMembers];
        readonly ILayerSprite[] portraitBarBackgrounds = new ILayerSprite[Game.MaxPartyMembers];
        readonly ISprite[] portraits = new ISprite[Game.MaxPartyMembers];
        readonly ILayerSprite healerSymbol = null;
        readonly IRenderText[] portraitNames = new IRenderText[Game.MaxPartyMembers];
        readonly PartyMemberPortaitState[] portraitStates = new PartyMemberPortaitState[Game.MaxPartyMembers];
        readonly ILayerSprite[] characterStatusIcons = new ILayerSprite[Game.MaxPartyMembers];
        readonly Bar[] characterBars = new Bar[Game.MaxPartyMembers * 4]; // 2 bars and each has fill and shadow color
        ISprite sprite80x80Picture;
        ISprite eventPicture;
        readonly Dictionary<SpecialItemPurpose, ILayerSprite> specialItemSprites = new Dictionary<SpecialItemPurpose, ILayerSprite>();
        readonly Dictionary<SpecialItemPurpose, UIText> specialItemTexts = new Dictionary<SpecialItemPurpose, UIText>();
        readonly Dictionary<ActiveSpellType, ILayerSprite> activeSpellSprites = new Dictionary<ActiveSpellType, ILayerSprite>();
        readonly Dictionary<ActiveSpellType, IColoredRect> activeSpellDurationBackgrounds = new Dictionary<ActiveSpellType, IColoredRect>();
        readonly Dictionary<ActiveSpellType, Bar> activeSpellDurationBars = new Dictionary<ActiveSpellType, Bar>();
        readonly List<MonsterCombatGraphic> monsterCombatGraphics = new List<MonsterCombatGraphic>();
        PortraitAnimation portraitAnimation = null;
        readonly List<ItemGrid> itemGrids = new List<ItemGrid>();
        UIText freeScrolledText = null;
        public bool FreeTextScrollingActive => freeScrolledText != null;
        internal UIText ChestText { get; private set; } = null;
        public bool TextWaitsForClick => ChestText?.WithScrolling == true || InventoryMessageWaitsForClick || FreeTextScrollingActive;
        Button questionYesButton = null;
        Button questionNoButton = null;
        DraggedItem draggedItem = null;
        uint draggedGold = 0;
        uint draggedFood = 0;
        public bool OptionMenuOpen { get; private set; } = false;
        public bool IsDragging => draggedItem != null || draggedGold != 0 || draggedFood != 0;
        public DraggedItem GetDraggedItem() => draggedItem;
        Action<uint> draggedGoldOrFoodRemover = null;
        readonly List<IColoredRect> barAreas = new List<IColoredRect>();
        readonly List<IColoredRect> filledAreas = new List<IColoredRect>();
        readonly List<IColoredRect> fadeEffectAreas = new List<IColoredRect>();
        readonly List<FadeEffect> fadeEffects = new List<FadeEffect>();
        readonly List<ISprite> additionalSprites = new List<ISprite>();
        readonly List<UIText> texts = new List<UIText>();
        readonly List<Tooltip> tooltips = new List<Tooltip>();
        readonly Dictionary<int, BattleFieldSlotMarker> battleFieldSlotMarkers = new Dictionary<int, BattleFieldSlotMarker>();
        public const uint TicksPerBlink = Game.TicksPerSecond / 4;
        IColoredRect activeTooltipBackground = null;
        IColoredRect[] activeTooltipBorders = new IColoredRect[4];
        IRenderText activeTooltipText = null;
        Tooltip activeTooltip = null;
        UIText inventoryMessage = null;
        UIText battleMessage = null;
        readonly List<BattleAnimation> battleEffectAnimations = new List<BattleAnimation>();
        readonly ButtonGrid buttonGrid;
        Popup activePopup = null;
        bool ignoreNextMouseUp = false;
        public bool PopupActive => activePopup != null;
        public bool PopupDisableButtons => activePopup?.DisableButtons == true;
        public bool PopupClickCursor => activePopup?.ClickCursor == true;
        public int ButtonGridPage { get; private set; } = 0;
        uint? ticksPerMovement = null;
        internal IRenderView RenderView { get; }
        public bool TransportEnabled { get; set; } = false;
        public event Action<int, int, MouseButtons> BattleFieldSlotClicked;
        public event Action DraggedItemDropped;

        public Layout(Game game, IRenderView renderView, IItemManager itemManager)
        {
            this.game = game;
            RenderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            renderLayer = renderView.GetLayer(Layer.UI);
            textLayer = renderView.GetLayer(Layer.Text);
            this.itemManager = itemManager;
            byte paletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);

            sprite = RenderView.SpriteFactory.Create(320, 163, true) as ILayerSprite;
            sprite.Layer = renderLayer;
            sprite.X = Global.LayoutX;
            sprite.Y = Global.LayoutY;
            sprite.DisplayLayer = 1;
            sprite.PaletteIndex = paletteIndex;

            AddStaticSprites();

            buttonGrid = new ButtonGrid(renderView);
            buttonGrid.RightMouseClicked += ButtonGrid_RightMouseClicked;

            healerSymbol = RenderView.SpriteFactory.Create(32, 29, true) as ILayerSprite;
            healerSymbol.Layer = renderLayer;
            healerSymbol.X = 0;
            healerSymbol.Y = 0;
            healerSymbol.DisplayLayer = 10;
            healerSymbol.PaletteIndex = paletteIndex;
            healerSymbol.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Talisman));
            healerSymbol.Visible = false;

            SetLayout(LayoutType.None);
        }

        public void ShowPortraitArea(bool show)
        {
            portraitBorders.ForEach(b => b.Visible = show);
            portraitBarBackgrounds.ToList().ForEach(b => b.Visible = show);

            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                if (!show)
                {
                    if (portraitBackgrounds[i] != null)
                        portraitBackgrounds[i].Visible = false;
                    if (portraits[i] != null)
                        portraits[i].Visible = false;
                    if (portraitNames[i] != null)
                        portraitNames[i].Visible = false;
                    if (characterStatusIcons[i] != null)
                        characterStatusIcons[i].Visible = false;
                }

                bool showBar = show;

                if (game.CurrentSavegame == null)
                    showBar = false;
                else if (showBar)
                    showBar = game.GetPartyMember(i)?.Alive == true;

                for (int n = 0; n < 2; ++n)
                    characterBars[i * 4 + n].Visible = showBar;

                if (showBar)
                {
                    showBar = game.GetPartyMember(i).Class.IsMagic();
                }

                for (int n = 0; n < 2; ++n)
                    characterBars[i * 4 + 2 + n].Visible = showBar;
            }
        }

        public void ToggleButtonGridPage()
        {
            if (Type == LayoutType.Map2D ||
                Type == LayoutType.Map3D)
            {
                if (game.InputEnable)
                {
                    ButtonGridPage = 1 - ButtonGridPage;
                    SetLayout(Type, ticksPerMovement);
                    buttonGrid?.HideTooltips();
                }
            }
        }

        void ButtonGrid_RightMouseClicked()
        {
            if (game.CursorType == CursorType.Sword)
                ToggleButtonGridPage();
        }

        void AddStaticSprites()
        {
            var barBackgroundTexCoords = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.CharacterValueBarFrames));
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var barBackgroundSprite = portraitBarBackgrounds[i] = RenderView.SpriteFactory.Create(16, 36, true) as ILayerSprite;
                barBackgroundSprite.Layer = renderLayer;
                barBackgroundSprite.PaletteIndex = game.PrimaryUIPaletteIndex;
                barBackgroundSprite.TextureAtlasOffset = barBackgroundTexCoords;
                barBackgroundSprite.X = Global.PartyMemberPortraitAreas[i].Left + 33;
                barBackgroundSprite.Y = Global.PartyMemberPortraitAreas[i].Top;
                barBackgroundSprite.Visible = true;
            }

            // Left portrait border
            var sprite = RenderView.SpriteFactory.Create(16, 36, true);
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.LeftPortraitBorder));
            sprite.X = 0;
            sprite.Y = 0;
            sprite.Visible = true;
            portraitBorders.Add(sprite);

            // Right portrait border
            sprite = RenderView.SpriteFactory.Create(16, 36, true);
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.RightPortraitBorder));
            sprite.X = Global.VirtualScreenWidth - 16;
            sprite.Y = 0;
            sprite.Visible = true;
            portraitBorders.Add(sprite);

            // Thin portrait borders
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                sprite = RenderView.SpriteFactory.Create(32, 1, true);
                sprite.Layer = renderLayer;
                sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 0;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                sprite = RenderView.SpriteFactory.Create(32, 1, true);
                sprite.Layer = renderLayer;
                sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 35;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                // LP shadow
                characterBars[i * 4 + 0] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 2, 19, 1, 16),
                    game.GetNamedPaletteColor(NamedPaletteColors.LPBarShadow), 1, FilledAreaType.CharacterBar), 16, false);
                // LP fill
                characterBars[i * 4 + 1] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 3, 19, 3, 16),
                    game.GetNamedPaletteColor(NamedPaletteColors.LPBar), 1, FilledAreaType.CharacterBar), 16, false);
                // SP shadow
                characterBars[i * 4 + 2] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 10, 19, 1, 16),
                    game.GetNamedPaletteColor(NamedPaletteColors.SPBarShadow), 1, FilledAreaType.CharacterBar), 16, false);
                // SP fill
                characterBars[i * 4 + 3] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 11, 19, 3, 16),
                    game.GetNamedPaletteColor(NamedPaletteColors.SPBar), 1, FilledAreaType.CharacterBar), 16, false);
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
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.LayoutOffset + (uint)(layoutType - 1));
                sprite.DisplayLayer = (byte)(layoutType == LayoutType.Automap ? 10 : 1);
                sprite.Visible = true;
            }

            buttonGrid.Visible = layoutType != LayoutType.None && layoutType != LayoutType.Event && layoutType != LayoutType.Automap;

            UpdateLayoutButtons(ticksPerMovement);
        }

        public void OpenOptionMenu()
        {
            OptionMenuOpen = true;
            game.InputEnable = false;
            game.Pause();
            var area = Type switch
            {
                LayoutType.Map2D => Game.Map2DViewArea,
                LayoutType.Map3D => Game.Map3DViewArea,
                LayoutType.Battle => Global.CombatBackgroundArea,
                _ => throw new AmbermoonException(ExceptionScope.Application, "Open option menu from the current window is not supported.")
            };
            AddSprite(area, Graphics.GetCustomUIGraphicIndex(UICustomGraphic.MapDisableOverlay), game.UIPaletteIndex, 1);
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            string versionString = $"Ambermoon.net V{version.Major}.{version.Minor}.{version.Build:00}^{game.DataNameProvider.DataVersionString}^{game.DataNameProvider.DataInfoString}";
            Rect boxArea;
            Rect textArea;
            if (Type == LayoutType.Battle)
            {
                boxArea = new Rect(88, 56, 144, 26);
                textArea = new Rect(88, 59, 144, 26);
            }
            else
            {
                boxArea = new Rect(32, 82, 144, 26);
                textArea = new Rect(32, 85, 144, 26);
            }
            AddSprite(boxArea, Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BiggerInfoBox), game.UIPaletteIndex, 2);
            AddText(textArea, versionString, TextColor.BrightGray, TextAlign.Center, 3);

            buttonGrid.SetButton(0, ButtonType.Quit, false, game.Quit, false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.Quit));
            buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(2, ButtonType.Exit, false, CloseOptionMenu, false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.Exit));
            buttonGrid.SetButton(3, ButtonType.Opt, false, OpenOptions, false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.Options));
            buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(6, ButtonType.Save, game.BattleActive, OpenSaveMenu, false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.Save));
            buttonGrid.SetButton(7, ButtonType.Load, false, () => OpenLoadMenu(), false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.Load));
            buttonGrid.SetButton(8, ButtonType.Stats, false, NewGame, false, Button.GetTooltip(game.GameLanguage, Button.TooltipType.New));
        }

        void CloseOptionMenu()
        {
            OptionMenuOpen = false;
            additionalSprites.Last()?.Delete(); // info box
            additionalSprites.Remove(additionalSprites.Last());
            additionalSprites.Last()?.Delete(); // map disable overlay
            additionalSprites.Remove(additionalSprites.Last());
            texts.Last()?.Destroy(); // version string
            texts.Remove(texts.Last());
            UpdateLayoutButtons(ticksPerMovement);
            if (game.BattleActive)
                game.SetupBattleButtons();
            game.Resume();
            game.InputEnable = true;
        }

        public void ShowButtons(bool show)
        {
            buttonGrid.Visible = show;
        }

        public void EnableButton(int index, bool enable)
        {
            buttonGrid.EnableButton(index, enable);
        }

        internal Popup OpenPopup(Position position, int columns, int rows,
            bool disableButtons = true, bool closeOnClick = true, byte displayLayerOffset = 0)
        {
            buttonGrid?.HideTooltips();
            activePopup = new Popup(game, RenderView, position, columns, rows, false, displayLayerOffset)
            {
                DisableButtons = disableButtons,
                CloseOnClick = closeOnClick
            };
            return activePopup;
        }

        internal Popup OpenTextPopup(IText text, Position position, int maxWidth, int maxTextHeight,
            bool disableButtons = true, bool closeOnClick = true, bool transparent = false,
            TextColor textColor = TextColor.BrightGray, Action closeAction = null, TextAlign textAlign = TextAlign.Left,
            byte displayLayerOffset = 0, byte? paletteOverride = null)
        {
            buttonGrid?.HideTooltips();
            ClosePopup(false);
            var processedText = RenderView.TextProcessor.WrapText(text,
                new Rect(0, 0, maxWidth, int.MaxValue),
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            var textBounds = new Rect(position.X + (transparent ? 0 : 16), position.Y + (transparent ? 0 : 16),
                maxWidth, Math.Min(processedText.LineCount * Global.GlyphLineHeight, maxTextHeight));
            int popupRows = Math.Max(4, transparent ? maxTextHeight / Global.GlyphLineHeight : 2 + (textBounds.Height + 15) / 16);
            if (!transparent)
                textBounds.Position.Y += ((popupRows - 2) * 16 - textBounds.Height) / 2;
            activePopup = new Popup(game, RenderView, position, transparent ? maxWidth / Global.GlyphWidth : 18, popupRows, transparent, displayLayerOffset)
            {
                DisableButtons = disableButtons,
                CloseOnClick = closeOnClick
            };
            bool scrolling = textBounds.Height / Global.GlyphLineHeight < processedText.LineCount;
            var uiText = activePopup.AddText(textBounds, text, textColor, textAlign, true, 1, scrolling, this);
            if (paletteOverride != null)
                uiText.PaletteIndex = paletteOverride.Value;
            if (closeAction != null)
                activePopup.Closed += closeAction;
            return activePopup;
        }

        internal Popup OpenTextPopup(IText text, Action closeAction, bool disableButtons = false,
            bool closeOnClick = true, bool transparent = false, TextAlign textAlign = TextAlign.Left,
            byte displayLayerOffset = 0, byte? paletteOverride = null)
        {
            const int maxTextWidth = 256;
            const int maxTextHeight = 112;
            var popup = OpenTextPopup(text, new Position(16, 53), maxTextWidth, maxTextHeight, disableButtons,
                closeOnClick, transparent, TextColor.BrightGray, closeAction, textAlign, displayLayerOffset, paletteOverride);
            return popup;
        }

        internal void OpenWaitPopup()
        {
            if (game.MonsterSeesPlayer)
            {
                game.ShowTextPopup(game.ProcessText(game.DataNameProvider.CannotWaitBecauseOfNearbyMonsters), null);
                return;
            }

            buttonGrid?.HideTooltips();
            ClosePopup(false);
            activePopup = new Popup(game, RenderView, new Position(64, 64), 11, 6, false)
            {
                DisableButtons = true,
                CloseOnClick = false
            };
            // Message display
            var messageArea = new Rect(79, 98, 145, 10);
            activePopup.AddSunkenBox(messageArea);
            activePopup.AddText(messageArea.CreateModified(1, 2, -1, -3), game.DataNameProvider.WaitHowManyHours,
                TextColor.LightOrange, TextAlign.Center);
            // Amount input
            var input = activePopup.AddTextInput(new Position(128, 119), 7, TextAlign.Center,
                TextInput.ClickAction.FocusOrSubmit, TextInput.ClickAction.LoseFocus);
            input.DigitsOnly = true;
            input.MaxIntegerValue = 24;
            input.ReactToGlobalClicks = true;
            input.ClearOnNewInput = true;
            input.Text = "0";
            input.Aborted += () => game.CursorType = CursorType.Sword;
            input.InputSubmitted += _ => game.CursorType = CursorType.Sword;
            // Increase and decrease buttons
            var increaseButton = activePopup.AddButton(new Position(80, 110));
            var decreaseButton = activePopup.AddButton(new Position(80, 127));
            increaseButton.ButtonType = ButtonType.MoveUp;
            decreaseButton.ButtonType = ButtonType.MoveDown;
            increaseButton.DisplayLayer = 200;
            decreaseButton.DisplayLayer = 200;
            increaseButton.LeftClickAction = () => ChangeInputValue(1);
            decreaseButton.LeftClickAction = () => ChangeInputValue(-1);
            increaseButton.RightClickAction = () => ChangeInputValueTo(24);
            decreaseButton.RightClickAction = () => ChangeInputValueTo(0);
            increaseButton.InstantAction = true;
            decreaseButton.InstantAction = true;
            increaseButton.ContinuousActionDelayInTicks = Game.TicksPerSecond / 5;
            decreaseButton.ContinuousActionDelayInTicks = Game.TicksPerSecond / 5;
            increaseButton.ContinuousActionDelayReductionInTicks = 1;
            decreaseButton.ContinuousActionDelayReductionInTicks = 1;
            // OK button
            var okButton = activePopup.AddButton(new Position(192, 127));
            okButton.ButtonType = ButtonType.Ok;
            okButton.DisplayLayer = 200;
            okButton.LeftClickAction = Wait;
            activePopup.ReturnAction = Wait;
            activePopup.Closed += () =>
            {
                game.Resume();
                game.InputEnable = true;
                game.CursorType = CursorType.Sword;
                game.UpdateCursor();
            };
            game.Pause();
            game.InputEnable = false;
            game.CursorType = CursorType.Sword;

            void Wait()
            {
                ClosePopup(true, true);
                game.Wait(input.Value);
            }

            void ChangeInputValueTo(long amount)
            {
                input.Text = Util.Limit(0, amount, 24).ToString();
            }

            void ChangeInputValue(int changeAmount)
            {
                ChangeInputValueTo((long)input.Value + changeAmount);
            }
        }

        internal Popup OpenInputPopup(Position position, int inputLength, Action<string> inputHandler)
        {
            var openPopup = activePopup;
            var popup = OpenPopup(position, 2 + ((inputLength + 1) * Global.GlyphWidth + 14) / 16, 3, true, false, 21);
            var input = popup.AddTextInput(position + new Position(16, 18), inputLength, TextAlign.Left,
                TextInput.ClickAction.Submit, TextInput.ClickAction.Abort);
            input.SetFocus();
            input.ReactToGlobalClicks = true;
            void Close()
            {
                input?.LoseFocus();
                game.CursorType = CursorType.Sword;
                ClosePopup();
                activePopup = openPopup;
            }
            input.InputSubmitted += (string input) =>
            {
                Close();
                inputHandler?.Invoke(input);
            };
            input.Aborted += Close;
            return popup;
        }

        internal Popup OpenYesNoPopup(IText text, Action yesAction, Action noAction,
            Action closeAction, int minLines = 1, byte displayLayerOffset = 0,
            TextAlign textAlign = TextAlign.Left)
        {
            buttonGrid?.HideTooltips();
            ClosePopup(false);
            const int maxTextWidth = 192;
            var processedText = RenderView.TextProcessor.WrapText(text,
                new Rect(48, 0, maxTextWidth, int.MaxValue),
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            var textBounds = new Rect(48, 95, maxTextWidth, Math.Max(minLines + 1, processedText.LineCount) * Global.GlyphLineHeight);
            var renderText = RenderView.RenderTextFactory.Create(textLayer,
                processedText, TextColor.BrightGray, true, textBounds, textAlign);
            renderText.PaletteIndex = game.TextPaletteIndex;
            int popupRows = Math.Max(minLines + 2, 2 + (textBounds.Height + 36) / 16);
            activePopup = new Popup(game, RenderView, new Position(32, 74), 14, popupRows, false, displayLayerOffset)
            {
                DisableButtons = true,
                CloseOnClick = false
            };
            activePopup.AddText(renderText);
            activePopup.Closed += closeAction;

            var yesButton = activePopup.AddButton(new Position(111, 41 + popupRows * 16));
            var noButton = activePopup.AddButton(new Position(143, 41 + popupRows * 16));

            yesButton.DisplayLayer = (byte)Util.Limit(200, yesButton.DisplayLayer, 253);
            noButton.DisplayLayer = (byte)Math.Min(254, Util.Max(noButton.DisplayLayer, 210, yesButton.DisplayLayer + 10));

            yesButton.ButtonType = ButtonType.Yes;
            noButton.ButtonType = ButtonType.No;

            yesButton.LeftClickAction = yesAction;
            noButton.LeftClickAction = noAction;

            return activePopup;
        }

        void ClosePopup(Popup popup, bool raiseEvent = true)
        {
            if (raiseEvent)
            {
                // The close event may close the popup itself.
                // In that case we must not destroy it here as
                // it might be a completely new popup.
                var oldPopup = popup;
                popup?.OnClosed();

                if (oldPopup != popup)
                    return;
            }
            popup?.Destroy();
        }

        internal void ClosePopup(bool raiseEvent = true, bool force = false)
        {
            // Note: As ClosePopup may trigger popup?.OnClosed
            // and this event might open a new popup we have
            // to set activePopup to null BEFORE we call it!
            var popup = activePopup;

            if (popup != null && !popup.CanAbort && !force)
                return;

            activePopup = null;
            ClosePopup(popup, raiseEvent);
        }

        internal void ClearLeftUpIgnoring() => ignoreNextMouseUp = false;

        void NewGame()
        {
            void ClosePopup() => this.ClosePopup(false, true);

            OpenYesNoPopup(game.ProcessText(game.GetCustomText(CustomTexts.Index.ReallyStartNewGame)),
                () =>
                {
                    ClosePopup();
                    game.NewGame();
                }, ClosePopup, ClosePopup);
        }

        internal void OpenLoadMenu(Action<Action> preLoadAction = null, Action abortAction = null,
            bool loadInitialSavegameOnFailure = false)
        {
            var savegameNames = game.SavegameManager.GetSavegameNames(RenderView.GameData, out _, 10);
            bool extended = game.Configuration.ExtendedSavegameSlots;
            if (extended)
            {
                var additionalSavegameSlots = game.Configuration.GetOrCreateCurrentAdditionalSavegameSlots();
                int remaining = Game.NumAdditionalSavegameSlots - Math.Min(Game.NumAdditionalSavegameSlots, additionalSavegameSlots?.Names?.Length ?? 0);
                if (additionalSavegameSlots?.Names != null)
                    savegameNames = Enumerable.Concat(savegameNames, additionalSavegameSlots.Names.Take(Game.NumAdditionalSavegameSlots).Select(n => n ?? "")).ToArray();
                if (remaining != 0)
                    savegameNames = Enumerable.Concat(savegameNames, Enumerable.Repeat("", remaining)).ToArray();
            }
            var position = extended ? new Position(13, 38) : new Position(16, 62);
            int maxItems = extended ? 16 : 10;
            var savegamePopup = OpenPopup(position, extended ? 19 : 18, extended ? 10 : 7, true, false);
            activePopup.AddText(new Rect(24, extended ? 54 : 78, 272, 6), game.DataNameProvider.LoadWhichSavegame, TextColor.BrightGray, TextAlign.Center);
            var listBox = activePopup.AddSavegameListBox(savegameNames.Select(name =>
                new KeyValuePair<string, Action<int, string>>(name, (int slot, string name) => Load(slot + 1, name))
            ).ToList(), false, maxItems, extended ? -23 : 0);

            if (extended)
            {
                int scrollRange = Math.Max(0, savegameNames.Length - 16);
                var scrollbar = activePopup.AddScrollbar(this, scrollRange, 2, 9);
                scrollbar.Scrolled += offset => listBox.ScrollTo(offset);
            }

            savegamePopup.Closed += Close;

            void Close()
            {
                ClosePopup(false);
                abortAction?.Invoke();
            }

            void Load(int slot, string name)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    ClosePopup(false);
                    OpenYesNoPopup(game.ProcessText(game.DataNameProvider.ReallyLoad), () =>
                    {
                        ClosePopup();
                        game.LoadGame(slot, true, loadInitialSavegameOnFailure, preLoadAction, false, s =>
                        {
                            if (game.Configuration.ShowSaveLoadMessage)
                            {
                                game.ShowBriefMessagePopup(
                                    s == 0 ? CustomTexts.GetText(game.GameLanguage, CustomTexts.Index.InitialGameLoaded) :
                                    string.Format(CustomTexts.GetText(game.GameLanguage, CustomTexts.Index.GameLoaded), s),
                                    TimeSpan.FromMilliseconds(1500));
                            }
                        }, true);
                    }, Close, Close);
                }
            }
        }

        void OpenSaveMenu()
        {
            var savegameNames = game.SavegameManager.GetSavegameNames(RenderView.GameData, out _, 10);
            bool extended = game.Configuration.ExtendedSavegameSlots;
            if (extended)
            {
                var additionalSavegameSlots = game.Configuration.GetOrCreateCurrentAdditionalSavegameSlots();
                int remaining = Game.NumAdditionalSavegameSlots - Math.Min(Game.NumAdditionalSavegameSlots, additionalSavegameSlots?.Names?.Length ?? 0);
                if (additionalSavegameSlots?.Names != null)
                    savegameNames = Enumerable.Concat(savegameNames, additionalSavegameSlots.Names.Take(Game.NumAdditionalSavegameSlots).Select(n => n ?? "")).ToArray();
                if (remaining != 0)
                    savegameNames = Enumerable.Concat(savegameNames, Enumerable.Repeat("", remaining)).ToArray();
            }
            var position = extended ? new Position(13, 38) : new Position(16, 62);
            int maxItems = extended ? 16 : 10;
            OpenPopup(position, extended ? 19 : 18, extended ? 10 : 7, true, false);
            activePopup.AddText(new Rect(24, extended ? 54 : 78, 272, 6), game.DataNameProvider.SaveWhichSavegame, TextColor.BrightGray, TextAlign.Center);
            var listBox = activePopup.AddSavegameListBox(savegameNames.Select(name =>
                new KeyValuePair<string, Action<int, string>>(name, (int slot, string name) => Save(slot + 1, name))
            ).ToList(), true, maxItems, extended ? -23 : 0);

            if (extended)
            {
                int scrollRange = Math.Max(0, savegameNames.Length - 16);
                var scrollbar = activePopup.AddScrollbar(this, scrollRange, 2, 9);
                scrollbar.Scrolled += offset => listBox.ScrollTo(offset);
            }

            void Close() => ClosePopup(false);

            void Save(int slot, string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    Close();
                    return;
                }

                var additionalSavegameSlots = game.Configuration.GetOrCreateCurrentAdditionalSavegameSlots();

                if (string.IsNullOrEmpty(savegameNames[slot - 1]))
                {
                    ClosePopup();
                    Save(slot, name);
                }
                else
                {
                    OpenYesNoPopup(game.ProcessText(game.DataNameProvider.ReallyOverwriteSave), () =>
                    {
                        ClosePopup();
                        Save(slot, name);
                    }, Close, Close);
                }

                void Save(int slot, string name)
                {
                    game.SaveGame(slot, name);
                    if (additionalSavegameSlots != null)
                        additionalSavegameSlots.ContinueSavegameSlot = slot;
                    if (game.Configuration.ShowSaveLoadMessage)
                    {
                        game.ShowBriefMessagePopup(
                            string.Format(CustomTexts.GetText(game.GameLanguage, CustomTexts.Index.GameSaved), name),
                            TimeSpan.FromMilliseconds(1500));
                    }
                }
            }
        }

        // TODO: add more languages later and/or add these texts to the new game data format
        const int OptionCount = 18;
        const int OptionsPerPage = 7;
        static readonly Dictionary<GameLanguage, string[]> OptionNames = new Dictionary<GameLanguage, string[]>
        {
            {
                GameLanguage.German,
                new string[OptionCount]
                {
                    // Page 1
                    "Musik",
                    "Lautstärke",
                    "Auflösung",
                    "Vollbild",
                    "Grafikfilter",
                    "Grafikoverlay",
                    "Effekt",
                    // Page 2
                    "Kampfgeschwindigkeit",
                    "Button Tooltips anzeigen",
                    "Stats Tooltips anzeigen",
                    "Runen als Text anzeigen",
                    "Cheats aktivieren",
                    "3D Boden und Decke",
                    "Zusätzliche Spielstände",
                    // Page3
                    "Externe Musik",
                    "Pyrdacor Logo zeigen",
                    "Thalion Logo zeigen",
                    "Info beim Speichern/Laden"
                    // TODO
                    //"Intro anzeigen",
                    //"Fantasy Intro anzeigen",
                }
            },
            {
                GameLanguage.English,
                new string[OptionCount]
                {
                    // Page 1
                    "Music",
                    "Volume",
                    "Resolution",
                    "Fullscreen",
                    "Graphic filter",
                    "Graphic overlay",
                    "Effect",
                    // Page 2
                    "Battle speed",
                    "Show button tooltips",
                    "Show stats tooltips",
                    "Show runes as text",
                    "Enable cheats",
                    "3D floor and ceiling",
                    "Additional saveslots",
                    // Page 3
                    "External music",
                    "Show Pyrdacor logo",
                    "Show Thalion logo",
                    "Show save/load info"
                    // TODO
                    //"Show intro",
                    //"Show fantasy intro",
                }
            }
        };
        static readonly Dictionary<GameLanguage, string[]> FloorAndCeilingValues = new Dictionary<GameLanguage, string[]>
        {
            {
                GameLanguage.German,
                new string[4]
                {
                    "Aus",
                    "Boden",
                    "Decke",
                    "Beide"
                }
            },
            {
                GameLanguage.English,
                new string[4]
                {
                    "None",
                    "Floor",
                    "Ceiling",
                    "Both"
                }
            }
        };
        static readonly Dictionary<GameLanguage, string> DefaultBattleSpeedName = new Dictionary<GameLanguage, string>
        {
            {
                GameLanguage.German, "Standard"
            },
            {
                GameLanguage.English, "Default"
            }
        };

        void OpenOptions()
        {
            int page = 0;
            OpenPopup(new Position(48, 62), 14, 7, true, false);
            activePopup.AddText(new Rect(56, 78, 208, 6), game.DataNameProvider.OptionsHeader, TextColor.BrightGray, TextAlign.Center);
            var optionNames = OptionNames[game.GameLanguage];
            bool changedConfiguration = false;
            bool windowChange = false; // an option was changed that affects the window (screen ratio, resolution, fullscreen)
            ListBox listBox = null;
            var on = game.DataNameProvider.On;
            var off = game.DataNameProvider.Off;
            int width = game.Configuration.Width ?? 1280;
            bool cheatsEnabled = !game.Configuration.IsMobile && game.Configuration.EnableCheats;
            var toggleResolutionAction = (Action<int, string>)((index, _) => ToggleResolution());
            var nullOptionAction = (Action<int, string>)null;
            var options = new List<KeyValuePair<string, Action<int, string>>>(OptionCount)
            {
                // Page 1
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleMusic())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleVolume())),
                KeyValuePair.Create("", game.Configuration.Fullscreen || game.Configuration.IsMobile ? null : toggleResolutionAction),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleFullscreen())),
                KeyValuePair.Create("", RenderView.AllowFramebuffer ? ((index, _) => ToggleGraphicFilter()) : nullOptionAction),
                KeyValuePair.Create("", RenderView.AllowFramebuffer ? ((index, _) => ToggleGraphicFilterAddition()) : nullOptionAction),
                KeyValuePair.Create("", RenderView.AllowEffects ? ((index, _) => ToggleEffects()) : nullOptionAction),
                // Page 2
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleBattleSpeed())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleTooltips())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => TogglePlayerStatsTooltips())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleAutoDerune())),
                KeyValuePair.Create("", game.Configuration.IsMobile ? null : (Action<int, string>)((index, _) => ToggleCheats())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleFloorAndCeiling())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleExtendedSaves())),
                // Page 3
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleExternalMusic())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => TogglePyrdacorLogo())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleThalionLogo())),
                KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleSaveLoadInfo()))
                // TODO: later
                //KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleIntro())),              
                //KeyValuePair.Create("", (Action<int, string>)((index, _) => ToggleFantasyIntro())),
            };
            listBox = activePopup.AddOptionsListBox(options.Take(OptionsPerPage).ToList());

            string GetResolutionString()
            {
                var resolution = game.Configuration.GetScreenResolution();
                return $"{resolution.Width}x{resolution.Height}";
            }
            void SetOptionString(int optionIndex, string value)
            {
                int index = optionIndex - page * OptionsPerPage;

                if (index < 0 || index >= OptionsPerPage)
                    return;

                var optionString = optionNames[optionIndex];
                int remainingSpace = 31 - optionString.Length - value.Length;
                optionString += new string(' ', remainingSpace);
                optionString += value;
                listBox.SetItemText(index, optionString);
            }
            string GetFloorAndCeilingValueString()
            {
                int index = 0;

                if (game.Configuration.ShowFloor)
                {
                    if (game.Configuration.ShowCeiling)
                        index = 3;
                    else
                        index = 1;
                }
                else if (game.Configuration.ShowCeiling)
                {
                    index = 2;
                }

                return FloorAndCeilingValues[game.GameLanguage][index];
            }
            // Page 1
            void SetMusic() => SetOptionString(0, game.Configuration.Music ? on : off);
            void SetVolume() => SetOptionString(1, Util.Limit(0, game.Configuration.Volume, 100).ToString());
            void SetResolution() => SetOptionString(2, GetResolutionString());
            void SetFullscreen() => SetOptionString(3, game.Configuration.Fullscreen ? on : off);
            void SetGraphicFilter() => SetOptionString(4, game.Configuration.GraphicFilter == GraphicFilter.None ? off : game.Configuration.GraphicFilter.ToString());
            void SetGraphicFilterOverlay() => SetOptionString(5, game.Configuration.GraphicFilterOverlay == GraphicFilterOverlay.None ? off : game.Configuration.GraphicFilterOverlay.ToString());
            void SetEffects() => SetOptionString(6, game.Configuration.Effects == Effects.None ? off : game.Configuration.Effects.ToString());
            // Page 2
            void SetBattleSpeed() => SetOptionString(7, game.Configuration.BattleSpeed == 0 ? DefaultBattleSpeedName[game.GameLanguage] : $"+{game.Configuration.BattleSpeed}%");
            void SetTooltips() => SetOptionString(8, game.Configuration.ShowButtonTooltips ? on : off);
            void SetPlayerStatsTooltips() => SetOptionString(9, game.Configuration.ShowPlayerStatsTooltips ? on : off);
            void SetAutoDerune() => SetOptionString(10, game.Configuration.AutoDerune ? on : off);
            void SetCheats() => SetOptionString(11, cheatsEnabled ? on : off);
            void SetFloorAndCeiling() => SetOptionString(12, GetFloorAndCeilingValueString());
            void SetExtendedSaves() => SetOptionString(13, game.Configuration.ExtendedSavegameSlots ? on : off);
            // Page 3
            void SetExternalMusic() => SetOptionString(14, game.Configuration.ExternalMusic ? on : off);
            void SetPyrdacorLogo() => SetOptionString(15, game.Configuration.ShowPyrdacorLogo ? on : off);
            void SetThalionLogo() => SetOptionString(16, game.Configuration.ShowThalionLogo ? on : off);
            void SetSaveLoadInfo() => SetOptionString(17, game.Configuration.ShowSaveLoadMessage ? on : off);
            // TODO: void SetIntro() => SetOptionString(?, game.Configuration.ShowIntro ? on : off);
            // TODO: void SetFantasyIntro() => SetOptionString(?, game.Configuration.ShowFantasyIntro ? on : off);

            void ShowOptions()
            {
                switch (page)
                {
                    default:
                    case 0:
                        SetMusic();
                        SetVolume();
                        SetResolution();
                        SetFullscreen();
                        SetGraphicFilter();
                        SetGraphicFilterOverlay();
                        SetEffects();
                        break;
                    case 1:
                        SetBattleSpeed();
                        SetTooltips();
                        SetPlayerStatsTooltips();
                        SetAutoDerune();
                        SetCheats();
                        SetFloorAndCeiling();
                        SetExtendedSaves();
                        break;
                    case 2:
                        SetExternalMusic();
                        SetPyrdacorLogo();
                        SetThalionLogo();
                        SetSaveLoadInfo();
                        break;
                    // TODO
                    //SetIntro();
                    //SetFantasyIntro();
                }
            }

            void ToggleMusic()
            {
                game.Configuration.Music = !game.Configuration.Music;
                game.AudioOutput.Enabled = game.Configuration.Music;
                if (game.AudioOutput.Available && game.AudioOutput.Enabled)
                    game.ContinueMusic();
                SetMusic();
                changedConfiguration = true;
            }
            void ToggleVolume()
            {
                game.Configuration.Volume = ((game.Configuration.Volume + 10) / 10) * 10;
                while (game.Configuration.Volume > 100)
                    game.Configuration.Volume -= 100;
                game.Configuration.Volume = Math.Max(0, game.Configuration.Volume);
                game.AudioOutput.Volume = game.Configuration.Volume / 100.0f;
                SetVolume();
                changedConfiguration = true;
            }
            void ToggleGraphicFilter()
            {
                game.Configuration.GraphicFilter = (GraphicFilter)(((int)game.Configuration.GraphicFilter + 1) % Enum.GetValues<GraphicFilter>().Length);
                SetGraphicFilter();
                changedConfiguration = true;
                game.NotifyConfigurationChange(false);
            }
            void ToggleGraphicFilterAddition()
            {
                game.Configuration.GraphicFilterOverlay = (GraphicFilterOverlay)(((int)game.Configuration.GraphicFilterOverlay + 1) % Enum.GetValues<GraphicFilterOverlay>().Length);
                SetGraphicFilterOverlay();
                changedConfiguration = true;
                game.NotifyConfigurationChange(false);
            }
            void ToggleResolution()
            {
                if (game.Configuration.Fullscreen)
                    return;

                game.NotifyResolutionChange(width);
                width = game.Configuration.Width.Value;
                SetResolution();
                changedConfiguration = true;
                windowChange = true;
            }
            void ToggleFullscreen()
            {
                game.Configuration.Fullscreen = !game.Configuration.Fullscreen;

                listBox.SetItemAction(2, game.Configuration.Fullscreen ? null : toggleResolutionAction);

                if (!game.Configuration.Fullscreen)
                    SetResolution();

                game.RequestFullscreenChange(game.Configuration.Fullscreen);
                SetFullscreen();
                changedConfiguration = true;
                windowChange = true;
            }
            void ToggleBattleSpeed()
            {
                if (game.Configuration.BattleSpeed >= 100)
                    game.Configuration.BattleSpeed = 0;
                else
                    game.Configuration.BattleSpeed += 10;
                SetBattleSpeed();
                game.SetBattleSpeed(game.Configuration.BattleSpeed);
                changedConfiguration = true;
            }
            void ToggleTooltips()
            {
                game.Configuration.ShowButtonTooltips = !game.Configuration.ShowButtonTooltips;
                SetTooltips();
                changedConfiguration = true;
            }
            void TogglePlayerStatsTooltips()
            {
                game.Configuration.ShowPlayerStatsTooltips = !game.Configuration.ShowPlayerStatsTooltips;
                SetPlayerStatsTooltips();
                changedConfiguration = true;
            }
            void ToggleFloorAndCeiling()
            {
                if (!game.Configuration.ShowFloor && !game.Configuration.ShowCeiling)
                {
                    game.Configuration.ShowFloor = true;
                }
                else if (game.Configuration.ShowFloor && !game.Configuration.ShowCeiling)
                {
                    game.Configuration.ShowFloor = false;
                    game.Configuration.ShowCeiling = true;
                }
                else if (!game.Configuration.ShowFloor && game.Configuration.ShowCeiling)
                {
                    game.Configuration.ShowFloor = true;
                }
                else
                {
                    game.Configuration.ShowFloor = false;
                    game.Configuration.ShowCeiling = false;
                }
                SetFloorAndCeiling();
                changedConfiguration = true;
            }
            void ToggleExtendedSaves()
            {
                game.Configuration.ExtendedSavegameSlots = !game.Configuration.ExtendedSavegameSlots;
                SetExtendedSaves();
                changedConfiguration = true;
            }
            void ToggleExternalMusic()
            {
                game.Configuration.ExternalMusic = !game.Configuration.ExternalMusic;
                SetExternalMusic();
                changedConfiguration = true;
            }
            void ToggleCheats()
            {
                cheatsEnabled = !cheatsEnabled;
                SetCheats();
                changedConfiguration = true;
            }
            void ToggleAutoDerune()
            {
                game.Configuration.AutoDerune = !game.Configuration.AutoDerune;
                SetAutoDerune();
                changedConfiguration = true;
            }
            void TogglePyrdacorLogo()
            {
                game.Configuration.ShowPyrdacorLogo = !game.Configuration.ShowPyrdacorLogo;
                SetPyrdacorLogo();
                changedConfiguration = true;
            }
            void ToggleThalionLogo()
            {
                game.Configuration.ShowThalionLogo = !game.Configuration.ShowThalionLogo;
                SetThalionLogo();
                changedConfiguration = true;
            }
            void ToggleEffects()
            {
                game.Configuration.Effects = (Effects)(((int)game.Configuration.Effects + 1) % Enum.GetValues<Effects>().Length);
                SetEffects();
                changedConfiguration = true;
                game.NotifyConfigurationChange(false);
            }
            void ToggleSaveLoadInfo()
            {
                game.Configuration.ShowSaveLoadMessage = !game.Configuration.ShowSaveLoadMessage;
                SetSaveLoadInfo();
                changedConfiguration = true;
            }

            var contentArea = activePopup.ContentArea;
            var exitButton = activePopup.AddButton(new Position(contentArea.Right - 32, contentArea.Bottom - 17));
            exitButton.ButtonType = ButtonType.Exit;
            exitButton.Disabled = false;
            exitButton.InstantAction = false;
            exitButton.LeftClickAction = () =>
            {
                ClosePopup();
                CloseOptionMenu();
                if (changedConfiguration)
                {
                    game.Configuration.EnableCheats = cheatsEnabled;
                    game.NotifyConfigurationChange(windowChange);
                }
            };
            exitButton.Visible = true;

            ShowOptions();

            var changePageButton = activePopup.AddButton(new Position(contentArea.Left, contentArea.Bottom - 17));
            changePageButton.ButtonType = ButtonType.MoveRight;
            changePageButton.Disabled = false;
            changePageButton.InstantAction = false;
            changePageButton.LeftClickAction = () =>
            {
                int numPages = (options.Count + OptionsPerPage - 1) / OptionsPerPage;
                page = (page + 1) % numPages;
                PageChanged();
            };
            changePageButton.Visible = true;

            activePopup.Scrolled += down =>
            {
                int numPages = (options.Count + OptionsPerPage - 1) / OptionsPerPage;

                if (down)
                    page = (page + 1) % numPages;
                else
                    page = (page + numPages + 1) % numPages;

                PageChanged();
            };

            void PageChanged()
            {
                var visibleOptions = options.Skip(page * OptionsPerPage).Take(OptionsPerPage).ToList();
                for (int i = 0; i < OptionsPerPage; ++i)
                {
                    if (i >= visibleOptions.Count)
                    {
                        listBox.SetItemText(i, "");
                        listBox.SetItemAction(i, null);
                    }
                    else
                    {
                        listBox.SetItemAction(i, visibleOptions[i].Value);
                    }
                }
                ShowOptions();
            }

            externalGraphicFilterChanged += SetGraphicFilter;
            externalGraphicFilterOverlayChanged += SetGraphicFilterOverlay;
            externalEffectsChanged += SetEffects;
            battleSpeedChanged += SetBattleSpeed;
            musicChanged += SetMusic;
            volumeChanged += SetVolume;
            activePopup.Closed += () =>
            {
                externalGraphicFilterChanged -= SetGraphicFilter;
                externalGraphicFilterOverlayChanged -= SetGraphicFilterOverlay;
                externalEffectsChanged -= SetEffects;
                battleSpeedChanged -= SetBattleSpeed;
                musicChanged -= SetMusic;
                volumeChanged -= SetVolume;
            };
        }

        event Action externalGraphicFilterChanged;

        public void ExternalGraphicFilterChanged()
        {
            externalGraphicFilterChanged?.Invoke();
        }

        event Action externalGraphicFilterOverlayChanged;

        public void ExternalGraphicFilterOverlayChanged()
        {
            externalGraphicFilterOverlayChanged?.Invoke();
        }

        event Action externalEffectsChanged;

        public void ExternalEffectsChanged()
        {
            externalEffectsChanged?.Invoke();
        }

        event Action battleSpeedChanged;

        public void ExternalBattleSpeedChanged()
        {
            battleSpeedChanged?.Invoke();
        }

        event Action musicChanged;

        public void ExternalMusicChanged()
        {
            musicChanged?.Invoke();
        }

        event Action volumeChanged;

        public void ExternalVolumeChanged()
        {
            volumeChanged?.Invoke();
        }

        public void AttachEventToButton(int index, Action action)
        {
            buttonGrid.SetButtonAction(index, action);
        }

        public void UpdateUIPalette(byte palette)
        {
            buttonGrid.PaletteIndex = palette;
            sprite.PaletteIndex = palette;

            foreach (var specialItemSprite in specialItemSprites)
                specialItemSprite.Value.PaletteIndex = palette;
            foreach (var specialItemText in specialItemTexts)
                specialItemText.Value.PaletteIndex = palette;
            foreach (var activeSpellSprite in activeSpellSprites)
                activeSpellSprite.Value.PaletteIndex = palette;
            foreach (var activeSpellDurationBackground in activeSpellDurationBackgrounds)
                activeSpellDurationBackground.Value.Color = game.GetUIColor(26);
            foreach (var activeSpellDurationBar in activeSpellDurationBars)
                activeSpellDurationBar.Value.Color = game.GetUIColor(31);
        }

        uint lastButtonMoveTicks = 0;
        static readonly CursorType[] MoveButtonCursorMapping2D = new CursorType[9]
        {
            CursorType.ArrowUpLeft,
            CursorType.ArrowUp,
            CursorType.ArrowUpRight,
            CursorType.ArrowLeft,
            CursorType.None,
            CursorType.ArrowRight,
            CursorType.ArrowDownLeft,
            CursorType.ArrowDown,
            CursorType.ArrowDownRight
        };
        static readonly CursorType[] MoveButtonCursorMapping3D = new CursorType[9]
        {
            CursorType.ArrowTurnLeft,
            CursorType.ArrowForward,
            CursorType.ArrowTurnRight,
            CursorType.ArrowStrafeLeft,
            CursorType.None,
            CursorType.ArrowStrafeRight,
            CursorType.ArrowRotateLeft,
            CursorType.ArrowBackward,
            CursorType.ArrowRotateRight
        };
        static CursorType CombineMoveCursorTypes2D(List<CursorType> cursorTypes)
        {
            bool left = cursorTypes.Contains(CursorType.ArrowUpLeft) ||
                        cursorTypes.Contains(CursorType.ArrowLeft) ||
                        cursorTypes.Contains(CursorType.ArrowDownLeft);
            bool right = cursorTypes.Contains(CursorType.ArrowUpRight) ||
                         cursorTypes.Contains(CursorType.ArrowRight) ||
                         cursorTypes.Contains(CursorType.ArrowDownRight);
            bool up = cursorTypes.Contains(CursorType.ArrowUpLeft) ||
                      cursorTypes.Contains(CursorType.ArrowUp) ||
                      cursorTypes.Contains(CursorType.ArrowUpRight);
            bool down = cursorTypes.Contains(CursorType.ArrowDownLeft) ||
                        cursorTypes.Contains(CursorType.ArrowDown) ||
                        cursorTypes.Contains(CursorType.ArrowDownRight);

            if (left && right)
                left = right = false;
            if (up && down)
                up = down = false;

            if (left)
            {
                if (up)
                    return CursorType.ArrowUpLeft;
                else if (down)
                    return CursorType.ArrowDownLeft;
                else
                    return CursorType.ArrowLeft;
            }
            else if (right)
            {
                if (up)
                    return CursorType.ArrowUpRight;
                else if (down)
                    return CursorType.ArrowDownRight;
                else
                    return CursorType.ArrowRight;
            }
            else
            {
                if (up)
                    return CursorType.ArrowUp;
                else if (down)
                    return CursorType.ArrowDown;
                else
                    return CursorType.None;
            }
        }
        static CursorType[] CombineMoveCursorTypes3D(List<CursorType> cursorTypes)
        {
            cursorTypes.Remove(CursorType.Wait);

            if (cursorTypes.Count <= 1)
                return cursorTypes.ToArray();

            if (cursorTypes.Count != 2)
                return new CursorType[0];

            // Only forward plus turn or strafe is allowed as a combination.
            if (cursorTypes.Contains(CursorType.ArrowForward))
            {
                if (cursorTypes.Contains(CursorType.ArrowTurnLeft) ||
                    cursorTypes.Contains(CursorType.ArrowTurnRight) ||
                    cursorTypes.Contains(CursorType.ArrowStrafeLeft) ||
                    cursorTypes.Contains(CursorType.ArrowStrafeRight))
                    return cursorTypes.ToArray();
            }
            // Or backward plus rotate  or strafe.
            else if (cursorTypes.Contains(CursorType.ArrowBackward))
            {
                if (cursorTypes.Contains(CursorType.ArrowRotateLeft) ||
                    cursorTypes.Contains(CursorType.ArrowRotateRight) ||
                    cursorTypes.Contains(CursorType.ArrowStrafeLeft) ||
                    cursorTypes.Contains(CursorType.ArrowStrafeRight))
                    return cursorTypes.ToArray();
            }

            // All other combinations won't work.
            return new CursorType[0];
        }

        string GetTooltip(Button.TooltipType tooltipType) => Button.GetTooltip(game.GameLanguage, tooltipType);

        internal void UpdateLayoutButtons(uint? ticksPerMovement = null)
        {
            var moveDelay = (ticksPerMovement ?? this.ticksPerMovement) ?? 0;

            void HandleButtonMove(CursorType cursorType)
            {
                var pressedCursors = new List<CursorType>();

                if (Type == LayoutType.Map2D)
                {
                    if (game.CurrentTicks - lastButtonMoveTicks < moveDelay)
                        return;

                    for (int i = 0; i < 9; ++i)
                    {
                        if (buttonGrid.IsButtonPressed(i))
                        {
                            pressedCursors.Add(MoveButtonCursorMapping2D[i]);
                        }
                    }

                    cursorType = CombineMoveCursorTypes2D(pressedCursors);

                    if (cursorType == CursorType.None)
                        return;

                    lastButtonMoveTicks = game.CurrentTicks;

                    game.Move(true, 1.0f, cursorType);
                }
                else if (Type == LayoutType.Map3D)
                {
                    for (int i = 0; i < 9; ++i)
                    {
                        if (buttonGrid.IsButtonPressed(i))
                        {
                            pressedCursors.Add(MoveButtonCursorMapping3D[i]);
                        }
                    }

                    var cursorTypes = CombineMoveCursorTypes3D(pressedCursors);

                    if (cursorTypes.Length == 0)
                        return;

                    if (cursorTypes.Length == 1)
                    {
                        if (cursorTypes[0] == CursorType.ArrowRotateLeft)
                            game.ExecuteNextUpdateCycle(() => buttonGrid.ReleaseButton(6, true));
                        else if (cursorTypes[0] == CursorType.ArrowRotateRight)
                            game.ExecuteNextUpdateCycle(() => buttonGrid.ReleaseButton(8, true));
                    }

                    if (game.CurrentTicks - lastButtonMoveTicks < moveDelay)
                        return;

                    lastButtonMoveTicks = game.CurrentTicks;

                    game.Move(true, 1.0f, cursorTypes);
                }
            }

            switch (Type)
            {
                case LayoutType.Map2D:
                    if (ButtonGridPage == 0)
                    {
                        buttonGrid.SetButton(0, ButtonType.MoveUpLeft, false, () => HandleButtonMove(CursorType.ArrowUpLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(1, ButtonType.MoveUp, false, () => HandleButtonMove(CursorType.ArrowUp), true, null, null, moveDelay);
                        buttonGrid.SetButton(2, ButtonType.MoveUpRight, false, () => HandleButtonMove(CursorType.ArrowUpRight), true, null, null, moveDelay);
                        buttonGrid.SetButton(3, ButtonType.MoveLeft, false, () => HandleButtonMove(CursorType.ArrowLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(4, ButtonType.Wait, false, OpenWaitPopup, false, GetTooltip(Button.TooltipType.Wait));
                        buttonGrid.SetButton(5, ButtonType.MoveRight, false, () => HandleButtonMove(CursorType.ArrowRight), true, null, null, moveDelay);
                        buttonGrid.SetButton(6, ButtonType.MoveDownLeft, false, () => HandleButtonMove(CursorType.ArrowDownLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(7, ButtonType.MoveDown, false, () => HandleButtonMove(CursorType.ArrowDown), true, null, null, moveDelay);
                        buttonGrid.SetButton(8, ButtonType.MoveDownRight, false, () => HandleButtonMove(CursorType.ArrowDownRight), true, null, null, moveDelay);
                    }
                    else
                    {
                        buttonGrid.SetButton(0, ButtonType.Eye, false, null, false, GetTooltip(Button.TooltipType.Eye), () => CursorType.Eye);
                        buttonGrid.SetButton(1, ButtonType.Hand, false, null, false, GetTooltip(Button.TooltipType.Hand), () => CursorType.Hand);
                        buttonGrid.SetButton(2, ButtonType.Mouth, false, null, false, GetTooltip(Button.TooltipType.Mouth), () => CursorType.Mouth);
                        buttonGrid.SetButton(3, ButtonType.Transport, !TransportEnabled, game.ToggleTransport, false, GetTooltip(Button.TooltipType.Transport));
                        buttonGrid.SetButton(4, ButtonType.Spells, game?.CanUseSpells() != true, () => game.CastSpell(false), false, GetTooltip(Button.TooltipType.Spells));
                        buttonGrid.SetButton(5, ButtonType.Camp, game?.Map?.CanCamp != true || game?.TravelType.CanCampOn() != true, () => game.OpenCamp(false), false, GetTooltip(Button.TooltipType.Camp));
                        buttonGrid.SetButton(6, ButtonType.Map, true, null, false, null);
                        buttonGrid.SetButton(7, ButtonType.BattlePositions, false, game.ShowBattlePositionWindow, false, GetTooltip(Button.TooltipType.BattlePositions));
                        buttonGrid.SetButton(8, ButtonType.Options, false, OpenOptionMenu, false, GetTooltip(Button.TooltipType.Options));
                    }
                    break;
                case LayoutType.Map3D:
                    if (ButtonGridPage == 0)
                    {
                        buttonGrid.SetButton(0, ButtonType.TurnLeft, false, () => HandleButtonMove(CursorType.ArrowTurnLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(1, ButtonType.MoveForward, false, () => HandleButtonMove(CursorType.ArrowForward), true, null, null, moveDelay);
                        buttonGrid.SetButton(2, ButtonType.TurnRight, false, () => HandleButtonMove(CursorType.ArrowTurnRight), true, null, null, moveDelay);
                        buttonGrid.SetButton(3, ButtonType.StrafeLeft, false, () => HandleButtonMove(CursorType.ArrowStrafeLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(4, ButtonType.Wait, false, OpenWaitPopup, false, GetTooltip(Button.TooltipType.Wait));
                        buttonGrid.SetButton(5, ButtonType.StrafeRight, false, () => HandleButtonMove(CursorType.ArrowStrafeRight), true, null, null, moveDelay);
                        buttonGrid.SetButton(6, ButtonType.RotateLeft, false, () => HandleButtonMove(CursorType.ArrowRotateLeft), true, null, null, moveDelay);
                        buttonGrid.SetButton(7, ButtonType.MoveBackward, false, () => HandleButtonMove(CursorType.ArrowBackward), true, null, null, moveDelay);
                        buttonGrid.SetButton(8, ButtonType.RotateRight, false, () => HandleButtonMove(CursorType.ArrowRotateRight), true, null, null, moveDelay);
                    }
                    else
                    {
                        buttonGrid.SetButton(0, ButtonType.Eye, false, () => game.TriggerMapEvents(EventTrigger.Eye), true, GetTooltip(Button.TooltipType.Eye));
                        buttonGrid.SetButton(1, ButtonType.Hand, false, () => game.TriggerMapEvents(EventTrigger.Hand), true, GetTooltip(Button.TooltipType.Hand));
                        buttonGrid.SetButton(2, ButtonType.Mouth, false, () =>
                        {
                            if (!game.TriggerMapEvents(EventTrigger.Mouth))
                            {
                                game.SpeakToParty();
                            }
                        }, true, GetTooltip(Button.TooltipType.Mouth));
                        buttonGrid.SetButton(3, ButtonType.Transport, true, null, false); // Never enabled or usable in 3D maps
                        buttonGrid.SetButton(4, ButtonType.Spells, game?.CanUseSpells() != true, () => game.CastSpell(false), false, GetTooltip(Button.TooltipType.Spells));
                        buttonGrid.SetButton(5, ButtonType.Camp, game?.Map?.CanCamp != true, () => game.OpenCamp(false), false, GetTooltip(Button.TooltipType.Camp));
                        buttonGrid.SetButton(6, ButtonType.Map, false, game.ShowAutomap, false, GetTooltip(Button.TooltipType.Automap));
                        buttonGrid.SetButton(7, ButtonType.BattlePositions, false, game.ShowBattlePositionWindow, false, GetTooltip(Button.TooltipType.BattlePositions));
                        buttonGrid.SetButton(8, ButtonType.Options, false, OpenOptionMenu, false, GetTooltip(Button.TooltipType.Options));
                    }
                    break;
                case LayoutType.Inventory:
                {
                    bool hasInventoryItems = game.CurrentInventory.Inventory.Slots.Any(item => item.ItemIndex != 0);
                    bool hasEquippedItems = game.CurrentInventory.Equipment.Slots.Any(item => item.Value.ItemIndex != 0);
                    bool canUseItem = (hasInventoryItems || hasEquippedItems) && game.CurrentInventory.Conditions.CanUseItem(game.CurrentInventory.Race == Race.Animal);
                    bool animalOrAbove = game.CurrentInventory.Race >= Race.Animal;
                    bool multiplePartyMembers = game.PartyMembers.Count(p => p != null) > 1;
                    buttonGrid.SetButton(0, ButtonType.Stats, false, () => game.OpenPartyMember(game.CurrentInventoryIndex.Value, false), false, GetTooltip(Button.TooltipType.Stats));
                    buttonGrid.SetButton(1, ButtonType.UseItem, !canUseItem, () => PickInventoryItemForAction(UseItem,
                        true, game.DataNameProvider.WhichItemToUseMessage), true, GetTooltip(Button.TooltipType.UseItem));
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false, GetTooltip(Button.TooltipType.Exit));
                    if (game.OpenStorage?.AllowsItemDrop == true)
                    {
                        buttonGrid.SetButton(3, ButtonType.StoreItem, !hasInventoryItems, () => PickInventoryItemForAction(StoreItem,
                            false, game.DataNameProvider.WhichItemToStoreMessage), false, GetTooltip(Button.TooltipType.StoreItem));
                        buttonGrid.SetButton(4, ButtonType.StoreGold, animalOrAbove || game.CurrentInventory?.Gold == 0, StoreGold, false, GetTooltip(Button.TooltipType.StoreGold));
                        buttonGrid.SetButton(5, ButtonType.StoreFood, animalOrAbove || game.CurrentInventory?.Food == 0, StoreFood, false, GetTooltip(Button.TooltipType.StoreFood));
                    }
                    else
                    {
                        buttonGrid.SetButton(3, ButtonType.DropItem, !hasInventoryItems, () => PickInventoryItemForAction(DropItem,
                            false, game.DataNameProvider.WhichItemToDropMessage), false, GetTooltip(Button.TooltipType.DropItem));
                        buttonGrid.SetButton(4, ButtonType.DropGold, animalOrAbove || game.OpenStorage is IPlace || game.CurrentInventory?.Gold == 0, DropGold, false, GetTooltip(Button.TooltipType.DropGold));
                        buttonGrid.SetButton(5, ButtonType.DropFood, animalOrAbove || game.CurrentInventory?.Food == 0, DropFood, false, GetTooltip(Button.TooltipType.DropFood));
                    }
                    buttonGrid.SetButton(6, ButtonType.ViewItem, !hasInventoryItems && !hasEquippedItems, () => PickInventoryItemForAction(ViewItem,
                        true, game.DataNameProvider.WhichItemToExamineMessage), false, GetTooltip(Button.TooltipType.ExamineItem));
                    buttonGrid.SetButton(7, ButtonType.GiveGold, !multiplePartyMembers || animalOrAbove || game.OpenStorage is IPlace || game.CurrentInventory?.Gold == 0, () => GiveGold(null), false, GetTooltip(Button.TooltipType.GiveGold));
                    buttonGrid.SetButton(8, ButtonType.GiveFood, !multiplePartyMembers || animalOrAbove || game.CurrentInventory?.Food == 0, () => GiveFood(null), false, GetTooltip(Button.TooltipType.GiveFood));
                    break;
                }
                case LayoutType.Stats:
                    buttonGrid.SetButton(0, ButtonType.Inventory, false, () => game.OpenPartyMember(game.CurrentInventoryIndex.Value, true), false, GetTooltip(Button.TooltipType.Inventory));
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false, GetTooltip(Button.TooltipType.Exit));
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                    break;
                case LayoutType.Items:
                {
                    if (game.OpenStorage is Chest chest)
                    {
                        void CloseChest()
                        {
                            if (chest.IsBattleLoot)
                            {
                                if (chest.HasAnyImportantItem(itemManager))
                                {
                                    ShowClickChestMessage(game.DataNameProvider.DontForgetItems +
                                        string.Join(", ", chest.GetImportantItemNames(itemManager)) + ".", null, true);
                                    return;
                                }

                                game.CloseWindow();
                            }
                            else
                            {
                                game.ChestClosed();
                            }
                        }
                        buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(2, ButtonType.Exit, false, CloseChest, false, GetTooltip(Button.TooltipType.Exit));
                        buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(4, ButtonType.DistributeGold, chest.Gold == 0, () => DistributeGold(chest), false, GetTooltip(Button.TooltipType.DistributeGold));
                        buttonGrid.SetButton(5, ButtonType.DistributeFood, chest.Food == 0, () => DistributeFood(chest), false, GetTooltip(Button.TooltipType.DistributeFood));
                        buttonGrid.SetButton(6, ButtonType.ViewItem, false, () => PickChestItemForAction(ViewItem,
                            game.DataNameProvider.WhichItemToExamineMessage), false, GetTooltip(Button.TooltipType.ExamineItem));
                        buttonGrid.SetButton(7, ButtonType.GiveGold, chest.Gold == 0, () => GiveGold(chest), false, GetTooltip(Button.TooltipType.GiveGold));
                        buttonGrid.SetButton(8, ButtonType.GiveFood, chest.Food == 0, () => GiveFood(chest), false, GetTooltip(Button.TooltipType.GiveFood));
                    }
                    else if (game.OpenStorage is Merchant merchant)
                    {
                        buttonGrid.SetButton(0, ButtonType.BuyItem, false, null, false, GetTooltip(Button.TooltipType.Buy)); // this is set later manually
                        buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                        buttonGrid.SetButton(3, ButtonType.SellItem, false, null, false, GetTooltip(Button.TooltipType.Sell)); // this is set later manually
                        buttonGrid.SetButton(4, ButtonType.ViewItem, false, null, false, GetTooltip(Button.TooltipType.ExamineItem)); // this is set later manually
                        buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                    }
                    else if (game.OpenStorage is NonItemPlace place)
                    {
                        switch (place.PlaceType)
                        {
                            case PlaceType.Trainer:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.Train, false, null, false, GetTooltip(Button.TooltipType.Train)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.FoodDealer:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.BuyFood, false, null, false, GetTooltip(Button.TooltipType.Buy)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.DistributeFood, true, null, false, GetTooltip(Button.TooltipType.DistributeFood)); // this is set later manually
                                buttonGrid.SetButton(5, ButtonType.GiveFood, true, null, false, GetTooltip(Button.TooltipType.GiveFood)); // this is set later manually
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.Healer:
                                buttonGrid.SetButton(0, ButtonType.HealPerson, false, null, false, GetTooltip(Button.TooltipType.HealPerson)); // this is set later manually
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.RemoveCurse, false, null, false, GetTooltip(Button.TooltipType.RemoveCurse)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.HealCondition, false, null, false, GetTooltip(Button.TooltipType.HealCondition)); // this is set later manually
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.Inn:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.Camp, false, null, false, GetTooltip(Button.TooltipType.RestInn)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.HorseDealer:
                            case PlaceType.RaftDealer:
                            case PlaceType.ShipDealer:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, place.PlaceType switch
                                {
                                    PlaceType.HorseDealer => ButtonType.BuyHorse,
                                    PlaceType.RaftDealer => ButtonType.BuyRaft,
                                    PlaceType.ShipDealer => ButtonType.BuyBoat,
                                    _ => ButtonType.Empty
                                }, false, null, false, GetTooltip(Button.TooltipType.Buy)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.Sage:
                                buttonGrid.SetButton(0, ButtonType.Grid, false, null, false, GetTooltip(Button.TooltipType.IdentifyEquipment)); // this is set later manually
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.Inventory, false, null, false, GetTooltip(Button.TooltipType.IdentifyInventory)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.Blacksmith:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.RepairItem, false, null, false, GetTooltip(Button.TooltipType.Repair)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            case PlaceType.Enchanter:
                                buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                                buttonGrid.SetButton(3, ButtonType.RechargeItem, false, null, false, GetTooltip(Button.TooltipType.Recharge)); // this is set later manually
                                buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                                buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                                break;
                            default:
                                throw new AmbermoonException(ExceptionScope.Data, "Invalid place type.");
                        }
                    }
                    else // Camp window or Locked screen
                    {
                        if (game.CurrentWindow.Window == Window.Camp)
                        {
                            buttonGrid.SetButton(0, ButtonType.Spells, false, null, false, GetTooltip(Button.TooltipType.Spells)); // this is set later manually
                            buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                            buttonGrid.SetButton(3, ButtonType.ReadScroll, false, null, false, GetTooltip(Button.TooltipType.ReadScroll)); // this is set later manually
                            buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(6, ButtonType.Sleep, false, null, false, GetTooltip(Button.TooltipType.Sleep)); // this is set later manually
                            buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                        }
                        else
                        {
                            buttonGrid.SetButton(0, ButtonType.Lockpick, false, null, false, GetTooltip(Button.TooltipType.Lockpick)); // this is set later manually
                            buttonGrid.SetButton(1, ButtonType.UseItem, false, null, false, GetTooltip(Button.TooltipType.UseItem)); // this is set later manually
                            buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                            buttonGrid.SetButton(3, ButtonType.FindTrap, false, null, false, GetTooltip(Button.TooltipType.FindTrap)); // this is set later manually
                            buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(6, ButtonType.DisarmTrap, false, null, false, GetTooltip(Button.TooltipType.DisarmTrap)); // this is set later manually
                            buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                            buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                        }
                    }
                    break;
                }
                case LayoutType.Riddlemouth:
                    buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Mouth, false, null, false, GetTooltip(Button.TooltipType.SolveRiddle)); // this is set later manually
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Ear, false, null, false, GetTooltip(Button.TooltipType.HearRiddle)); // this is set later manually
                    break;
                case LayoutType.Conversation:
                    buttonGrid.SetButton(0, ButtonType.Mouth, false, null, false, GetTooltip(Button.TooltipType.Say)); // this is set later manually
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, null, false, GetTooltip(Button.TooltipType.Exit)); // this is set later manually
                    buttonGrid.SetButton(3, ButtonType.ViewItem, false, null, false, GetTooltip(Button.TooltipType.ShowItemToNPC)); // this is set later manually
                    buttonGrid.SetButton(4, ButtonType.AskToLeave, false, null, false, GetTooltip(Button.TooltipType.AskToLeave)); // this is set later manually
                    buttonGrid.SetButton(5, ButtonType.AskToJoin, false, null, false, GetTooltip(Button.TooltipType.AskToJoin)); // this is set later manually
                    buttonGrid.SetButton(6, ButtonType.GiveItem, false, null, false, GetTooltip(Button.TooltipType.GiveItemToNPC)); // this is set later manually
                    buttonGrid.SetButton(7, ButtonType.GiveGoldToNPC, false, null, false, GetTooltip(Button.TooltipType.GiveGoldToNPC)); // this is set later manually
                    buttonGrid.SetButton(8, ButtonType.GiveFoodToNPC, false, null, false, GetTooltip(Button.TooltipType.GiveFoodToNPC)); // this is set later manually
                    break;
                case LayoutType.Battle:
                    buttonGrid.SetButton(0, ButtonType.Flee, false, null, false, GetTooltip(Button.TooltipType.Flee)); // this is set later manually
                    buttonGrid.SetButton(1, ButtonType.Options, false, OpenOptionMenu, false, GetTooltip(Button.TooltipType.Options));
                    buttonGrid.SetButton(2, ButtonType.Ok, false, null, false, GetTooltip(Button.TooltipType.StartBattleRound)); // this is set later manually
                    buttonGrid.SetButton(3, ButtonType.BattlePositions, true, null, false, GetTooltip(Button.TooltipType.BattleMove)); // this is set later manually
                    buttonGrid.SetButton(4, ButtonType.MoveForward, true, null, false, GetTooltip(Button.TooltipType.BattleAdvance)); // this is set later manually
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Attack, true, null, false, GetTooltip(Button.TooltipType.BattleAttack)); // this is set later manually
                    buttonGrid.SetButton(7, ButtonType.Defend, true, null, false, GetTooltip(Button.TooltipType.BattleDefend)); // this is set later manually
                    buttonGrid.SetButton(8, ButtonType.Spells, true, null, false, GetTooltip(Button.TooltipType.BattleCast)); // this is set later manually
                    break;
                case LayoutType.BattlePositions:
                    buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false, GetTooltip(Button.TooltipType.Exit));
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                    break;
            }
        }

        public void AddSunkenBox(Rect area, byte displayLayer = 1)
        {
            // TODO: use named palette colors
            var darkBorderColor = game.GetUIColor(26);
            var brightBorderColor = game.GetUIColor(31);
            var fillColor = game.GetUIColor(27);

            // upper dark border
            FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer);
            // left dark border
            FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer);
            // fill
            FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer);
            // right bright border
            FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer);
            // lower bright border
            FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer);
        }

        internal Popup OpenAmountInputBox(string message, uint? imageIndex, string name, uint maxAmount,
            Action<uint> submitAction, Action abortAction = null)
        {
            buttonGrid?.HideTooltips();
            ClosePopup(false);
            activePopup = new Popup(game, RenderView, new Position(64, 64), 11, 6, false)
            {
                DisableButtons = true,
                CloseOnClick = false
            };
            if (imageIndex != null)
            {
                // Item display (also gold or food)
                var itemArea = new Rect(79, 79, 18, 18);
                activePopup.AddSunkenBox(itemArea);
                activePopup.AddItemImage(itemArea.CreateModified(1, 1, -2, -2), imageIndex.Value);
            }
            if (name != null)
            {
                // Item name display (also gold or food)
                var itemNameArea = new Rect(99, 82, 125, 10);
                activePopup.AddSunkenBox(itemNameArea);
                activePopup.AddText(itemNameArea.CreateModified(1, 2, -1, -3), name, TextColor.Red, TextAlign.Center);
            }
            // Message display
            var messageArea = new Rect(79, 98, 145, 10);
            activePopup.AddSunkenBox(messageArea);
            activePopup.AddText(messageArea.CreateModified(1, 2, -1, -3), message, TextColor.LightOrange, TextAlign.Center);
            // Amount input
            var input = activePopup.AddTextInput(new Position(128, 119), 7, TextAlign.Center,
                TextInput.ClickAction.FocusOrSubmit, TextInput.ClickAction.Abort);
            input.DigitsOnly = true;
            input.MaxIntegerValue = maxAmount;
            input.ReactToGlobalClicks = true;
            input.ClearOnNewInput = true;
            input.Text = "0";
            input.Aborted += () => game.CursorType = CursorType.Sword;
            input.InputSubmitted += _ => game.CursorType = CursorType.Sword;
            // Increase and decrease buttons
            var increaseButton = activePopup.AddButton(new Position(80, 110));
            var decreaseButton = activePopup.AddButton(new Position(80, 127));
            increaseButton.ButtonType = ButtonType.MoveUp;
            decreaseButton.ButtonType = ButtonType.MoveDown;
            increaseButton.DisplayLayer = 200;
            decreaseButton.DisplayLayer = 200;
            increaseButton.LeftClickAction = () => ChangeInputValue(1);
            decreaseButton.LeftClickAction = () => ChangeInputValue(-1);
            increaseButton.RightClickAction = () => ChangeInputValueTo(maxAmount);
            decreaseButton.RightClickAction = () => ChangeInputValueTo(0);
            increaseButton.InstantAction = true;
            decreaseButton.InstantAction = true;
            increaseButton.ContinuousActionDelayInTicks = Game.TicksPerSecond / 5;
            decreaseButton.ContinuousActionDelayInTicks = Game.TicksPerSecond / 5;
            increaseButton.ContinuousActionDelayReductionInTicks = 1;
            decreaseButton.ContinuousActionDelayReductionInTicks = 1;
            // OK button
            var okButton = activePopup.AddButton(new Position(192, 127));
            okButton.ButtonType = ButtonType.Ok;
            okButton.DisplayLayer = 200;
            okButton.LeftClickAction = Submit;
            activePopup.ReturnAction = Submit;

            void Submit()
            {
                if (input.Value == 0)
                {
                    if (abortAction != null)
                        abortAction();
                    ClosePopup(false);
                }
                else
                    submitAction?.Invoke(input.Value);
            }

            void ChangeInputValueTo(long amount)
            {
                input.Text = Util.Limit(0, amount, maxAmount).ToString();
            }

            void ChangeInputValue(int changeAmount)
            {
                ChangeInputValueTo((long)input.Value + changeAmount);
            }

            itemGrids.ForEach(itemGrid => itemGrid.HideTooltip());
            HideTooltip();

            return activePopup;
        }

        void Ask(string question, Action yesAction)
        {
            var text = RenderView.TextProcessor.CreateText(question);
            OpenYesNoPopup
            (
                text,
                () =>
                {
                    ClosePopup(false);
                    game.InputEnable = true;
                    game.Resume();
                    yesAction?.Invoke();
                },
                () =>
                {
                    ClosePopup(false);
                    game.InputEnable = true;
                    game.Resume();
                },
                () =>
                {
                    game.InputEnable = true;
                    game.Resume();
                }, 1
            );
            game.Pause();
            game.InputEnable = false;
            game.CursorType = CursorType.Sword;
        }

        bool ShowTextItem(uint index, uint subIndex)
        {
            var text = game.ItemManager.GetText(index, subIndex);

            if (text == null)
                return false;

            game.Pause();
            game.InputEnable = false;

            OpenTextPopup(game.ProcessText(text), new Position(16, 52), 256, 112, true, true, false, TextColor.BrightGray, () =>
            {
                game.InputEnable = true;
                game.Resume();
                game.ResetCursor();
            });
            game.CursorType = CursorType.Click;

            return true;
        }

        internal int? TryEquipmentDrop(ItemSlot itemSlot)
        {
            return itemGrids[1].TryEquipmentDrop(itemSlot);
        }

        internal ItemGrid GetInventoryGrid() => itemGrids[0];
        internal ItemGrid GetEquipmentGrid() => itemGrids[1];


        internal void UseItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            bool wasInputEnabled = game.InputEnable;
            game.InputEnable = false;
            game.ExecuteNextUpdateCycle(itemGrid.HideTooltip);

            if (itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
            {
                SetInventoryMessage(game.DataNameProvider.CannotUseBrokenItems, true);
                return;
            }

            var user = game.CurrentInventory;
            uint itemIndex = itemSlot.ItemIndex;
            var item = itemManager.GetItem(itemIndex);
            int RollDice1000() => game.RandomInt(0, 999);

            if (!game.BattleActive && game.TestUseItemMapEvent(itemIndex, out var eventX, out var eventY))
            {
                void Use(bool broke)
                {
                    ReduceItemCharge(itemSlot, true, itemGrid == itemGrids[1], game.CurrentInventory, () =>
                    {
                        if (broke && itemGrid == itemGrids[1]) // equipped
                        {
                            // Try to unequip
                            var emptyInventorySlot = game.CurrentInventory.Inventory.Slots.FirstOrDefault(s => s.Empty);

                            if (emptyInventorySlot != null)
                            {
                                emptyInventorySlot.Replace(itemSlot);

                                if (slot == (int)EquipmentSlot.RightHand - 1 && item.NumberOfHands == 2)
                                {
                                    // For equipped two-handed weapons also remove the red cross in second hand slot
                                    itemGrids[1].GetItemSlot(slot + 2).Clear();
                                    if (game.CurrentWindow.Window == Window.Inventory)
                                        itemGrids[1].UpdateItem(slot + 2);
                                }

                                itemSlot.Clear();
                                if (game.CurrentWindow.Window == Window.Inventory)
                                    itemGrids[1].UpdateItem(slot);

                            }
                        }

                        var currentInventoryIndex = game.CurrentInventoryIndex; // close window will null this

                        game.CloseWindow(() =>
                        {
                            if (wasInputEnabled)
                                game.InputEnable = true;
                            game.UpdateCursor();
                            game.CurrentInventoryIndex = currentInventoryIndex; // TODO: this won't work long enough if some events open/close windows or are async/event-based
                            game.TriggerMapEvents((EventTrigger)((uint)EventTrigger.Item0 + itemIndex), eventX, eventY);
                            game.CurrentInventoryIndex = null;
                        });
                    });
                }
                if (item.CanBreak && RollDice1000() < item.BreakChance)
                {
                    itemSlot.Flags |= ItemSlotFlags.Broken;
                    UpdateItemSlot(itemSlot);

                    string message = game.CurrentInventory.Name + string.Format(game.DataNameProvider.BattleMessageWasBroken, item.Name);
                    game.ShowMessagePopup(message, () => Use(true));
                }
                else
                {
                    Use(false);
                }
                return;
            }

            if (!item.IsUsable)
            {
                SetInventoryMessage(game.DataNameProvider.ItemHasNoEffectHere, true);
                return;
            }

            if (!item.Classes.Contains(user.Class))
            {
                SetInventoryMessage(game.DataNameProvider.WrongClassToUseItem, true);
                return;
            }

            if (item.Type == ItemType.TextScroll && item.TextIndex != 0)
            {
                if (item.CanBreak && RollDice1000() < item.BreakChance)
                {
                    itemSlot.Flags |= ItemSlotFlags.Broken;
                    UpdateItemSlot(itemSlot);
                    string message = game.CurrentInventory.Name + string.Format(game.DataNameProvider.BattleMessageWasBroken, item.Name);
                    game.ShowMessagePopup(message, ShowText);
                }
                else
                {
                    ShowText();
                }

                void ShowText()
                {
                    if (game.Configuration.AutoDerune && item.Index == 145) // Special case: rune alphabet
                    {
                        game.Pause();
                        game.InputEnable = false;

                        OpenTextPopup(game.ProcessText(CustomTexts.GetText(game.GameLanguage, CustomTexts.Index.RuneTableUsage)),
                            new Position(16, 52), 256, 112, true, true, false, TextColor.BrightGray, () =>
                        {
                            game.InputEnable = true;
                            game.Resume();
                            game.ResetCursor();
                        });
                        game.CursorType = CursorType.Click;
                    }
                    else if (!ShowTextItem(item.TextIndex, item.TextSubIndex))
                        throw new AmbermoonException(ExceptionScope.Data, $"Invalid text index for item '{item.Name}'");
                }
                return;
            }
            else if (item.Type == ItemType.SpecialItem)
            {
                if (game.CurrentSavegame.IsSpecialItemActive(item.SpecialItemPurpose))
                {
                    SetInventoryMessage(game.DataNameProvider.SpecialItemAlreadyInUse, true);
                }
                else
                {
                    game.StartSequence();
                    DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true, () =>
                    {
                        game.EndSequence();
                        game.CurrentSavegame.ActivateSpecialItem(item.SpecialItemPurpose);
                        SetInventoryMessage(game.DataNameProvider.SpecialItemActivated, true);
                    });
                }
                return;
            }

            if (game.BattleActive)
            {
                if (item.Spell != Spell.None)
                {
                    if (!SpellInfos.Entries[item.Spell].ApplicationArea.HasFlag(SpellApplicationArea.Battle))
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                        return;
                    }

                    var worldFlag = (WorldFlag)(1 << (int)game.Map.World);

                    if (!SpellInfos.Entries[item.Spell].Worlds.HasFlag(worldFlag))
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongWorldToUseItem, true);
                        return;
                    }

                    if (itemSlot.NumRemainingCharges == 0)
                    {
                        if (item.Flags.HasFlag(ItemFlags.Stackable))
                            itemSlot.NumRemainingCharges = Math.Max(1, (int)item.InitialCharges);
                        else
                        {
                            SetInventoryMessage(game.DataNameProvider.NoChargesLeft, true);
                            return;
                        }
                    }

                    if (item.Spell == Spell.SelfHealing && !game.CurrentInventory.Alive)
                    {
                        SetInventoryMessage(game.DataNameProvider.ItemHasNoEffectHere, true);
                        return;
                    }

                    // Note: itemGrids[0] is inventory and itemGrids[1] is equipment
                    bool equipped = itemGrid == itemGrids[1];
                    var caster = game.CurrentInventory;
                    game.CloseWindow(() =>
                    {
                        game.PickBattleSpell(item.Spell, (uint)slot, equipped, caster);
                        if (wasInputEnabled)
                            game.InputEnable = true;
                        game.UpdateCursor();
                    });
                    return;
                }
                else
                {
                    SetInventoryMessage(game.DataNameProvider.ItemHasNoEffectHere, true);
                    return;
                }
            }
            else
            {
                if (item.Spell != Spell.None)
                {
                    var worldFlag = (WorldFlag)(1 << (int)game.Map.World);

                    if (!SpellInfos.Entries[item.Spell].Worlds.HasFlag(worldFlag))
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongWorldToUseItem, true);
                        return;
                    }

                    if (game.Map.Flags.HasFlag(MapFlags.NoMarkOrReturn) && (item.Spell == Spell.WordOfMarking ||
                        item.Spell == Spell.WordOfReturning))
                    {
                        SetInventoryMessage(game.DataNameProvider.ItemCannotBeUsedHere, true);
                        return;
                    }

                    bool wrongPlace = false;

                    if (!game.Map.Flags.HasFlag(MapFlags.CanUseMagic) && item.Type == ItemType.SpellScroll)
                    {
                        wrongPlace = true;
                    }
                    else if (game.LastWindow.Window == Window.Camp)
                    {
                        wrongPlace = !SpellInfos.Entries[item.Spell].ApplicationArea.HasFlag(SpellApplicationArea.Camp);
                    }
                    else if (game.LastWindow.Window != Window.Battle)
                    {
                        if (!SpellInfos.Entries[item.Spell].ApplicationArea.HasFlag(SpellApplicationArea.AnyMap))
                        {
                            if (game.Map.IsWorldMap)
                                wrongPlace = !SpellInfos.Entries[item.Spell].ApplicationArea.HasFlag(SpellApplicationArea.WorldMapOnly);
                            else if (game.Map.Type == MapType.Map3D)
                            {
                                if (!game.Map.Flags.HasFlag(MapFlags.Outdoor))
                                    wrongPlace = !SpellInfos.Entries[item.Spell].ApplicationArea.HasFlag(SpellApplicationArea.DungeonOnly);
                                else
                                    wrongPlace = true;
                            }
                            else
                            {
                                wrongPlace = true;
                            }
                        }
                    }
                    else
                    {
                        wrongPlace = true;
                    }

                    if (wrongPlace)
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                        return;
                    }

                    if (item.MaxCharges != 0 && itemSlot.NumRemainingCharges == 0)
                    {
                        SetInventoryMessage(game.DataNameProvider.NoChargesLeft, true);
                        return;
                    }

                    if (item.Spell == Spell.Lockpicking)
                    {
                        // Do not consume. Can be used by Thief/Ranger but has no effect in Ambermoon.
                        return;
                    }
                    else if (item.Spell == Spell.CallEagle)
                    {
                        if (game.TravelType != TravelType.Walk)
                        {
                            SetInventoryMessage(game.DataNameProvider.CannotCallEagleIfNotOnFoot, true);
                        }
                        else if (game.Map.Flags.HasFlag(MapFlags.NoEagleOrBroom))
                        {
                            SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                        }
                        else
                        {
                            itemGrid.HideTooltip();
                            ItemAnimation.Play(game, RenderView, ItemAnimation.Type.Enchant, itemGrid.GetSlotPosition(slot), () =>
                            {
                                if (wasInputEnabled)
                                    game.InputEnable = true;
                                game.UpdateCursor();
                                game.UseSpell(game.CurrentInventory, item.Spell, itemGrid, true);
                            });
                        }
                    }
                    else if (item.Spell == Spell.SelfHealing && !game.CurrentInventory.Alive)
                    {
                        SetInventoryMessage(game.DataNameProvider.ItemHasNoEffectHere, true);
                        return;
                    }
                    else if (item.Spell == Spell.SelfReviving && game.CurrentInventory.Alive)
                    {
                        SetInventoryMessage(game.DataNameProvider.IsNotDead, true);
                        return;
                    }
                    else
                    {
                        // Note: itemGrids[0] is inventory and itemGrids[1] is equipment
                        bool equipped = itemGrid == itemGrids[1];
                        var usingPlayer = game.CurrentInventory;

                        void ConsumeItem(Action effectHandler)
                        {
                            void Done()
                            {
                                effectHandler?.Invoke();
                                if (wasInputEnabled)
                                    game.InputEnable = true;
                                game.UpdateCursor();
                            }

                            if (item.MaxCharges == 0 && item.Flags.HasFlag(ItemFlags.DestroyAfterUsage) && itemSlot.NumRemainingCharges <= 1)
                            {
                                if (game.CurrentInventory == usingPlayer)
                                    DestroyItem(itemSlot, TimeSpan.FromMilliseconds(25), true, Done);
                                else
                                {
                                    var item = itemManager.GetItem(itemSlot.ItemIndex);

                                    usingPlayer.TotalWeight -= item.Weight;

                                    if (equipped)
                                        game.EquipmentRemoved(usingPlayer, itemSlot.ItemIndex, 1, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                                    itemSlot.Remove(1);
                                }
                            }
                            else
                            {
                                if (item.MaxCharges != 0 && itemSlot.NumRemainingCharges > 0)
                                    --itemSlot.NumRemainingCharges;
                                if (game.CurrentInventory == usingPlayer)
                                    ItemAnimation.Play(game, RenderView, ItemAnimation.Type.Enchant, itemGrid.GetSlotPosition(slot), Done);
                            }
                        }

                        game.UseSpell(game.CurrentInventory, item.Spell, itemGrid, true, ConsumeItem);
                    }
                }
                else if (item.Type == ItemType.Transportation)
                {
                    if (game.LastWindow.Window != Window.MapView || !game.Map.IsWorldMap)
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                        return;
                    }

                    if (game.TravelType != TravelType.Walk)
                    {
                        // Note: There is a message especially for the flying disc but
                        // it is not used in this case. Don't know yet where it is actually used.
                        SetInventoryMessage(game.DataNameProvider.CannotUseItHere, true);
                        return;
                    }
                    else if (item.Transportation == Transportation.WitchBroom && game.Map.Flags.HasFlag(MapFlags.NoEagleOrBroom))
                    {
                        SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                    }

                    if (wasInputEnabled)
                        game.InputEnable = true;
                    game.UpdateCursor();

                    switch (item.Transportation)
                    {
                        case Transportation.FlyingDisc:
                            game.ActivateTransport(TravelType.MagicalDisc);
                            break;
                        case Transportation.WitchBroom:
                            game.ActivateTransport(TravelType.WitchBroom);
                            break;
                        default:
                            throw new AmbermoonException(ExceptionScope.Data, $"Unexpected transport type from item '{item.Name}': {item.Transportation}");
                    }
                }
                else
                {
                    SetInventoryMessage(game.DataNameProvider.WrongPlaceToUseItem, true);
                    return;
                }
            }
        }

        internal void ReduceItemCharge(ItemSlot itemSlot, bool slotVisible,
            bool equip, Character character, Action followAction = null)
        {
            itemSlot.NumRemainingCharges = Math.Max(0, itemSlot.NumRemainingCharges - 1);

            if (itemSlot.NumRemainingCharges == 0)
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);

                if (item.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
                {
                    if (slotVisible)
                    {
                        foreach (var itemGrid in itemGrids)
                            itemGrid.HideTooltip();
                        game.InputEnable = false;
                        DestroyItem(itemSlot, TimeSpan.FromMilliseconds(25), true, () =>
                        {
                            if (!itemSlot.Empty)
                                itemSlot.NumRemainingCharges = Math.Max(1, (int)item.InitialCharges);
                            game.InputEnable = true;
                            game.AddTimedEvent(TimeSpan.FromMilliseconds(50), followAction);
                        });
                        return;
                    }
                    else
                    {
                        uint itemIndex = itemSlot.ItemIndex;
                        itemSlot.Remove(1);
                        if (character is PartyMember partyMember)
                        {
                            if (equip)
                                game.EquipmentRemoved(partyMember, itemIndex, 1, false);
                            else
                                game.InventoryItemRemoved(itemIndex, 1, partyMember);
                        }                        
                        if (!itemSlot.Empty)
                            itemSlot.NumRemainingCharges = Math.Max(1, (int)item.InitialCharges);
                    }
                }
            }

            followAction?.Invoke();
        }

        void DistributeGold(Chest chest)
        {
            var initialGold = chest.Gold;
            chest.Gold = game.DistributeGold(chest.Gold, false);

            if (chest.Gold != initialGold)
            {
                game.ChestGoldChanged();
                UpdateLayoutButtons();
            }
        }

        void DistributeFood(Chest chest)
        {
            var initialFood = chest.Food;
            chest.Food = game.DistributeFood(chest.Food, false);

            if (chest.Food != initialFood)
            {
                game.ChestFoodChanged();
                UpdateLayoutButtons();
            }
        }

        void GiveGold(Chest chest)
        {
            // Note: 96 is the object icon index for coins (gold).
            OpenAmountInputBox(game.DataNameProvider.GiveHowMuchGoldMessage,
                96, game.DataNameProvider.GoldName, chest == null ? game.CurrentInventory.Gold : chest.Gold,
                GiveAmount);

            void GiveAmount(uint amount)
            {
                ClosePopup();
                CancelDrag();

                if (!game.PartyMembers.Any(p => p.Race != Race.Animal && p.MaxGoldToTake >= amount))
                {
                    if (chest != null)
                        ShowClickChestMessage(game.DataNameProvider.NoOneCanCarryThatMuch);
                    else
                        SetInventoryMessage(game.DataNameProvider.NoOneCanCarryThatMuch, true);
                    return;
                }

                draggedGold = amount;
                game.CursorType = CursorType.Gold;
                game.TrapMouse(Global.PartyMemberPortraitArea);
                draggedGoldOrFoodRemover = chest == null
                    ? (Action<uint>)(gold => { game.CurrentInventory.RemoveGold(gold); game.UpdateCharacterInfo(); UpdateLayoutButtons(); game.UntrapMouse(); SetInventoryMessage(null); })
                    : gold => { chest.Gold -= gold; game.ChestGoldChanged(); UpdateLayoutButtons(); game.UntrapMouse(); };
                if (chest != null)
                    ShowChestMessage(game.DataNameProvider.GiveToWhom);
                else
                    SetInventoryMessage(game.DataNameProvider.GiveToWhom);

                for (int i = 0; i < Game.MaxPartyMembers; ++i)
                {
                    var partyMember = game.GetPartyMember(i);

                    if (partyMember != null && partyMember != game.CurrentInventory)
                    {
                        UpdateCharacterStatus(i, partyMember == game.CurrentInventory ? (UIGraphic?)null :
                            partyMember.Race != Race.Animal && partyMember.MaxGoldToTake >= amount && !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                    }
                }
            }
        }

        void GiveFood(Chest chest)
        {
            GiveFood(chest == null ? game.CurrentInventory.Food : chest.Food,
                chest == null
                    ? (Action<uint>)(food => { game.CurrentInventory.RemoveFood(food); game.UpdateCharacterInfo(); UpdateLayoutButtons(); game.UntrapMouse(); SetInventoryMessage(null); })
                    : food => { chest.Food -= food; game.ChestFoodChanged(); UpdateLayoutButtons(); game.UntrapMouse(); },
                chest == null
                    ? (Action)(() => SetInventoryMessage(game.DataNameProvider.GiveToWhom))
                    : () => ShowChestMessage(game.DataNameProvider.GiveToWhom), null, () =>
                    {
                        if (chest != null)
                            ShowClickChestMessage(game.DataNameProvider.NoOneCanCarryThatMuch);
                        else
                            SetInventoryMessage(game.DataNameProvider.NoOneCanCarryThatMuch, true);
                    });
        }

        internal void GiveFood(uint food, Action<uint> foodRemover, Action setup, Action abortAction, Action cannotCarryHandler)
        {
            // Note: 109 is the object icon index for food.
            OpenAmountInputBox(game.DataNameProvider.GiveHowMuchFoodMessage,
                109, game.DataNameProvider.FoodName, food, GiveAmount, abortAction);

            void GiveAmount(uint amount)
            {
                ClosePopup();
                CancelDrag();

                if (!game.PartyMembers.Any(p => p.Race != Race.Animal && p.MaxFoodToTake >= amount))
                {
                    cannotCarryHandler?.Invoke();
                    return;
                }

                draggedFood = amount;
                game.CursorType = CursorType.Food;
                game.TrapMouse(Global.PartyMemberPortraitArea);
                draggedGoldOrFoodRemover = foodRemover;
                setup?.Invoke();

                for (int i = 0; i < Game.MaxPartyMembers; ++i)
                {
                    var partyMember = game.GetPartyMember(i);

                    if (partyMember != null && partyMember != game.CurrentInventory)
                    {
                        UpdateCharacterStatus(i, partyMember == game.CurrentInventory ? (UIGraphic?)null :
                            partyMember.Race != Race.Animal && partyMember.MaxFoodToTake >= amount && !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                    }
                }
            }
        }

        internal void ShowChestMessage(string message, TextAlign textAlign = TextAlign.Center)
        {
            ChestText?.Destroy();
            if (message != null)
            {
                var bounds = new Rect(114, 46, 189, 48);
                ChestText = AddText(bounds, game.ProcessText(message, bounds), TextColor.White, textAlign);
            }
            else
            {
                ChestText = null;
            }
        }

        internal void ShowClickChestMessage(string message, Action clickEvent = null, bool remainAfterClick = false)
        {
            var bounds = new Rect(114, 46, 189, 48);
            ChestText?.Destroy();
            ChestText = AddScrollableText(bounds, game.ProcessText(message, bounds));
            ChestText.Clicked += scrolledToEnd =>
            {
                if (scrolledToEnd)
                {
                    if (remainAfterClick)
                    {
                        ChestText.WithScrolling = false;
                    }
                    else
                    {
                        ChestText?.Destroy();
                        ChestText = null;
                    }
                    game.InputEnable = true;
                    game.CursorType = CursorType.Sword;
                    clickEvent?.Invoke();
                }
            };
            game.CursorType = CursorType.Click;
            game.InputEnable = false;
        }

        Button AddButton(Position position, ButtonType type, Action leftClickAction, byte displayLayer,
            List<FilledArea> areas)
        {
            var brightBorderColor = game.GetUIColor(31);
            var darkBorderColor = game.GetUIColor(26);
            displayLayer = Math.Min(displayLayer, (byte)251);

            areas.Add(FillArea(new Rect(position.X, position.Y, Button.Width + 1, Button.Height + 1), brightBorderColor, displayLayer++));
            areas.Add(FillArea(new Rect(position.X - 1, position.Y - 1, Button.Width + 1, Button.Height + 1), darkBorderColor, displayLayer++));
            areas.Add(FillArea(new Rect(position.X, position.Y, Button.Width, Button.Height), Render.Color.Black, displayLayer++));
            var button = new Button(RenderView, position, null);
            button.PaletteIndex = game.UIPaletteIndex;
            button.ButtonType = type;
            button.Disabled = false;
            button.DisplayLayer = displayLayer;
            button.LeftClickAction = leftClickAction;
            return button;
        }

        internal void ShowGameOverButtons(Action<bool> choiceEvent, bool hasSavegames)
        {
            var areas = new List<FilledArea>();
            questionYesButton?.Destroy();
            questionNoButton?.Destroy();
            questionYesButton = AddButton(new Position(128, 169), hasSavegames ? ButtonType.Load : ButtonType.Stats, () => Choose(true), 2, areas);
            questionNoButton = AddButton(new Position(128 + Button.Width, 169), ButtonType.Quit, () => Choose(false), 2, areas);

            void Choose(bool load)
            {
                areas.ForEach(area => area?.Destroy());
                questionYesButton?.Destroy();
                questionYesButton = null;
                questionNoButton?.Destroy();
                questionNoButton = null;
                game.InputEnable = true;
                choiceEvent?.Invoke(load);
            }
        }

        internal void ShowPlaceQuestion(string message, Action<bool> answerEvent, TextAlign textAlign = TextAlign.Center)
        {
            var bounds = new Rect(114, 46, 189, 28);
            var areas = new List<FilledArea>();
            ChestText?.Destroy();
            ChestText = AddText(bounds, game.ProcessText(message, bounds), TextColor.White, textAlign);
            questionYesButton?.Destroy();
            questionNoButton?.Destroy();
            questionYesButton = AddButton(new Position(223, 75), ButtonType.Yes, () => Answer(true), 2, areas);
            questionNoButton = AddButton(new Position(223 + Button.Width, 75), ButtonType.No, () => Answer(false), 2, areas);
            game.CursorType = CursorType.Click;
            game.InputEnable = false;

            void Answer(bool answer)
            {
                areas.ForEach(area => area?.Destroy());
                questionYesButton?.Destroy();
                questionYesButton = null;
                questionNoButton?.Destroy();
                questionNoButton = null;
                ChestText?.Destroy();
                ChestText = null;
                game.InputEnable = true;
                answerEvent?.Invoke(answer);
            }
        }

        void DropGold()
        {
            // Note: 96 is the object icon index for coins (gold).
            OpenAmountInputBox(game.DataNameProvider.DropHowMuchGoldMessage,
                96, game.DataNameProvider.GoldName, game.CurrentInventory.Gold,
                DropAmount);

            void DropAmount(uint amount)
            {
                Ask(game.DataNameProvider.DropGoldQuestion, () => game.DropGold(amount));
            }
        }

        void DropFood()
        {
            // Note: 109 is the object icon index for food.
            OpenAmountInputBox(game.DataNameProvider.DropHowMuchFoodMessage,
                109, game.DataNameProvider.FoodName, game.CurrentInventory.Food,
                DropAmount);

            void DropAmount(uint amount)
            {
                Ask(game.DataNameProvider.DropFoodQuestion, () => game.DropFood(amount));
            }
        }

        void ViewItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            itemGrid.HideTooltip();
            game.ShowItemPopup(itemSlot, game.UpdateCursor);
        }

        void DropItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            var item = itemManager.GetItem(itemSlot.ItemIndex);

            if (!item.Flags.HasFlag(ItemFlags.NotImportant))
            {
                itemGrid.HideTooltip();
                SetInventoryMessage(game.DataNameProvider.ItemIsImportant, true);
                return;
            }

            if (itemSlot.Amount > 1)
            {
                itemGrid.HideTooltip();
                OpenAmountInputBox(game.DataNameProvider.DropHowMuchItemsMessage,
                    item.GraphicIndex, item.Name, (uint)itemSlot.Amount, DropAmount);
            }
            else
            {
                DropAmount(1);
            }

            void DropAmount(uint amount)
            {
                void DropIt()
                {
                    // TODO: animation where the item falls down the screen
                    uint itemIndex = itemSlot.ItemIndex;
                    itemSlot.Remove((int)amount);
                    itemGrid.SetItem(slot, itemSlot); // update appearance                    
                    game.InventoryItemRemoved(itemIndex, (int)amount);
                    game.UpdateCharacterInfo();
                }

                itemGrid.HideTooltip();
                Ask(game.DataNameProvider.DropItemQuestion, DropIt);
            }
        }

        void StoreGold()
        {
            // Note: 96 is the object icon index for coins (gold).
            var chest = game.OpenStorage as Chest;
            OpenAmountInputBox(game.DataNameProvider.StoreHowMuchGoldMessage,
                96, game.DataNameProvider.GoldName, Math.Min(game.CurrentInventory.Gold, 0xffff - chest.Gold),
                amount =>
                {
                    game.StoreGold(amount);
                    if (chest.Gold == 0xffff)
                        SetInventoryMessage(game.DataNameProvider.ChestNowFull, true);
                });
        }

        void StoreFood()
        {
            // Note: 109 is the object icon index for food.
            var chest = game.OpenStorage as Chest;
            OpenAmountInputBox(game.DataNameProvider.StoreHowMuchFoodMessage,
                109, game.DataNameProvider.FoodName, Math.Min(game.CurrentInventory.Food, 0xffff - chest.Food),
                amount =>
                {
                    game.StoreFood(amount);
                    if (chest.Food == 0xffff)
                        SetInventoryMessage(game.DataNameProvider.ChestNowFull, true);
                });
        }

        void StoreItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            var slots = game.OpenStorage.Slots.ToList();
            int maxItemsToStore = 0;
            var item = itemManager.GetItem(itemSlot.ItemIndex);

            if (slots.Any(slot => slot.Empty))
                maxItemsToStore = 99;
            else
            {
                if (item.Flags.HasFlag(ItemFlags.Stackable))
                {
                    foreach (var possibleSlot in slots.Where(s => s.ItemIndex == item.Index))
                    {
                        maxItemsToStore += 99 - possibleSlot.Amount;
                    }
                }
            }

            if (maxItemsToStore == 0)
            {
                SetInventoryMessage(game.DataNameProvider.ChestFull, true);
                return;
            }

            if (itemSlot.Amount > 1)
            {
                OpenAmountInputBox(game.DataNameProvider.StoreHowMuchItemsMessage,
                    item.GraphicIndex, item.Name, (uint)Math.Min(itemSlot.Amount, maxItemsToStore), amount =>
                    {
                        StoreAmount(amount);
                        if (amount == maxItemsToStore && !slots.Any(slot => slot.Empty || (slot.ItemIndex == item.Index && slot.Amount < 99)))
                            SetInventoryMessage(game.DataNameProvider.ChestNowFull, true);
                    });
            }
            else
            {
                StoreAmount(1);

                if ((maxItemsToStore == 1 || (maxItemsToStore == 99 && !item.Flags.HasFlag(ItemFlags.Stackable))) &&
                    !slots.Any(slot => slot.Empty))
                    SetInventoryMessage(game.DataNameProvider.ChestNowFull, true);
            }

            void StoreAmount(uint amount)
            {
                ClosePopup(false);

                uint itemIndex = itemSlot.ItemIndex;

                // TODO: animation where the item flies to the right of the screen
                if (game.StoreItem(itemSlot, amount))
                {
                    game.InventoryItemRemoved(itemIndex, (int)amount);
                    itemGrid.SetItem(slot, itemSlot); // update appearance
                    game.UpdateCharacterInfo();
                }
            }
        }

        internal bool InventoryMessageWaitsForClick => inventoryMessage != null && !game.InputEnable;

        internal void ClickInventoryMessage() => inventoryMessage?.InvokeClickEvent();

        internal void SetInventoryMessage(string message, bool waitForClick = false)
        {
            if (message == null)
            {
                inventoryMessage?.Destroy();
                inventoryMessage = null;
            }
            else
            {
                if (waitForClick)
                {
                    foreach (var itemGrid in itemGrids)
                        itemGrid.HideTooltip();
                    inventoryMessage?.Destroy();
                    game.CursorType = CursorType.Click;
                    inventoryMessage = AddScrollableText(new Rect(21, 51, 156, 20), game.ProcessText(message));
                    inventoryMessage.Clicked += scrolledToEnd =>
                    {
                        if (scrolledToEnd)
                        {
                            inventoryMessage?.Destroy();
                            inventoryMessage = null;
                            game.InputEnable = true;
                            game.CursorType = CursorType.Sword;
                            game.UpdateCursor();
                        }
                    };
                    game.CursorType = CursorType.Click;
                    game.InputEnable = false;
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(50), () => SetActiveTooltip(null, null));
                }
                else if (inventoryMessage == null)
                {
                    inventoryMessage = AddScrollableText(new Rect(21, 51, 156, 14), game.ProcessText(message));
                }
                else
                {
                    inventoryMessage.SetText(game.ProcessText(message));
                }
            }
        }

        void PickChestItemForAction(Action<ItemGrid, int, ItemSlot> itemAction, string message)
        {
            ShowChestMessage(message);
            var itemArea = new Rect(16, 139, 151, 53);
            game.TrapMouse(itemArea);

            void ItemChosen(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                ShowChestMessage(null);
                itemGrids[0].DisableDrag = false;
                itemGrids[0].ItemClicked -= ItemChosen;
                itemGrids[0].RightClicked -= Aborted;
                game.UntrapMouse();

                if (itemGrid != null && itemSlot != null)
                    itemAction?.Invoke(itemGrid, slot, itemSlot);
            }

            bool Aborted()
            {
                ItemChosen(null, 0, null);
                return true;
            }

            itemGrids[0].DisableDrag = true;
            itemGrids[0].ItemClicked += ItemChosen;
            itemGrids[0].RightClicked += Aborted;
        }

        void PickInventoryItemForAction(Action<ItemGrid, int, ItemSlot> itemAction, bool includeEquipment, string message)
        {
            SetInventoryMessage(message);

            // Note: itemGrids[0] is the inventory and itemGrids[1] is the equipment.
            game.TrapMouse(includeEquipment ? Global.InventoryAndEquipTrapArea : Global.InventoryTrapArea);

            void ItemChosen(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                SetInventoryMessage(null);
                itemGrids[0].DisableDrag = false;
                itemGrids[1].DisableDrag = false;
                itemGrids[0].ItemClicked -= ItemChosen;
                itemGrids[1].ItemClicked -= ItemChosen;
                itemGrids[0].RightClicked -= Aborted;
                itemGrids[1].RightClicked -= Aborted;
                game.UntrapMouse();

                if (itemGrid != null && itemSlot != null)
                {
                    // Catch two-handed weapon left hand slot click
                    if (itemSlot.ItemIndex == 0 &&
                        itemGrid == itemGrids[1] && // equip
                        (EquipmentSlot)(slot + 1) == EquipmentSlot.LeftHand)
                    {
                        slot -= 2;
                        itemSlot = itemGrid.GetItemSlot(slot);
                    }

                    itemAction?.Invoke(itemGrid, slot, itemSlot);
                }
            }

            bool Aborted()
            {
                ItemChosen(null, 0, null);
                return true;
            }

            itemGrids[0].DisableDrag = true;
            itemGrids[1].DisableDrag = true;
            itemGrids[0].ItemClicked += ItemChosen;
            itemGrids[1].ItemClicked += ItemChosen;
            itemGrids[0].RightClicked += Aborted;
            itemGrids[1].RightClicked += Aborted;
        }

        public void Destroy()
        {
            Util.SafeCall(() => sprite?.Delete());
            Util.SafeCall(() =>
            {
                foreach (var portraitBackground in portraitBackgrounds)
                    portraitBackground?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var portraitBarBackground in portraitBarBackgrounds)
                    portraitBarBackground?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var portraitBorder in portraitBorders)
                    portraitBorder?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var portraitName in portraitNames)
                    portraitName?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var portrait in portraits)
                    portrait?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var characterStatusIcon in characterStatusIcons)
                    characterStatusIcon?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var barArea in barAreas)
                    barArea?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var characterBar in characterBars)
                    characterBar?.Destroy();
            });
            Util.SafeCall(() =>
            {
                foreach (var fadeEffectArea in fadeEffectAreas)
                    fadeEffectArea?.Delete();
            });
            Util.SafeCall(() =>
            {
                foreach (var fadeEffect in fadeEffects)
                    fadeEffect?.Destroy();
            });
            Util.SafeCall(() => buttonGrid.Visible = false);
        }

        public void Reset(bool keepInventoryMessage = false)
        {
            OptionMenuOpen = false;
            Util.SafeCall(() =>
            {
                ChestText?.Destroy();
                ChestText = null;
            });
            Util.SafeCall(() => tooltips.Clear());
            Util.SafeCall(() =>
            {
                if (keepInventoryMessage)
                {
                    texts.Remove(inventoryMessage);
                    texts.ForEach(text => text?.Destroy());
                    texts.Clear();
                    texts.Add(inventoryMessage);
                }
                else
                {
                    texts.ForEach(text => text?.Destroy());
                    texts.Clear();
                    inventoryMessage?.Destroy();
                    inventoryMessage = null;
                }
            });
            Util.SafeCall(() =>
            {
                additionalSprites.ForEach(sprite => sprite?.Delete());
                additionalSprites.Clear();
            });
            Util.SafeCall(() =>
            {
                sprite80x80Picture?.Delete();
                sprite80x80Picture = null;
            });
            Util.SafeCall(() =>
            {
                eventPicture?.Delete();
                eventPicture = null;
            });
            Util.SafeCall(() =>
            {
                itemGrids.ForEach(grid => grid.Destroy());
                itemGrids.Clear();
            });
            Util.SafeCall(() =>
            {
                filledAreas.ForEach(area => area?.Delete());
                filledAreas.Clear();
            });
            Util.SafeCall(() =>
            {
                activePopup?.Destroy();
                activePopup = null;
            });
            Util.SafeCall(() =>
            {
                activeTooltipText?.Delete();
                activeTooltipText = null;
            });
            Util.SafeCall(() =>
            {
                activeTooltipBackground?.Delete();
                activeTooltipBackground = null;
                if (activeTooltipBorders != null)
                {
                    for (int i = 0; i < activeTooltipBorders.Length; ++i)
                    {
                        activeTooltipBorders[i]?.Delete();
                        activeTooltipBorders[i] = null;
                    }
                }
            });
            Util.SafeCall(() =>
            {
                battleMessage?.Destroy();
                battleMessage = null;
            });
            Util.SafeCall(() =>
            {
                battleEffectAnimations.ForEach(a => a?.Destroy());
                battleEffectAnimations.Clear();
            });
            Util.SafeCall(() =>
            {
                activeSpellSprites?.Clear(); // sprites are destroyed above
                activeSpellDurationBars.Clear(); // areas are destroyed above
                activeSpellDurationBackgrounds?.Values?.ToList()?.ForEach(b => b?.Delete());
                activeSpellDurationBackgrounds?.Clear();                
            });
            Util.SafeCall(() =>
            {
                specialItemSprites?.Clear(); // sprites are destroyed above
                specialItemTexts?.Clear(); // texts are destroyed above
            });
            Util.SafeCall(() =>
            {
                monsterCombatGraphics.ForEach(g => { g.Animation?.Destroy(); g.BattleFieldSprite?.Delete(); RemoveTooltip(g.Tooltip); });
                monsterCombatGraphics.Clear();
            });
            Util.SafeCall(() =>
            {
                questionYesButton?.Destroy();
                questionYesButton = null;
                questionNoButton?.Destroy();
                questionNoButton = null;
            });

            // Note: Don't remove fadeEffects or bars here.
        }

        public void SetActiveCharacter(int slot, List<PartyMember> partyMembers)
        {
            for (int i = 0; i < portraitNames.Length; ++i)
            {
                if (portraitNames[i] != null)
                {
                    if (i == slot)
                        portraitNames[i].TextColor = TextColor.ActivePartyMember;
                    else if (!partyMembers[i].Alive || !partyMembers[i].Conditions.CanSelect())
                        portraitNames[i].TextColor = TextColor.DeadPartyMember;
                    else if (game.HasPartyMemberFled(partyMembers[i]))
                        portraitNames[i].TextColor = TextColor.DeadPartyMember;
                    else
                        portraitNames[i].TextColor = TextColor.PartyMember;
                }
            }
        }

        public void UpdateCharacterNameColors(int activeSlot)
        {
            var partyMembers = Enumerable.Range(0, Game.MaxPartyMembers).Select(i => game.GetPartyMember(i)).ToList();

            for (int i = 0; i < portraitNames.Length; ++i)
            {
                if (portraitNames[i] != null)
                {
                    if (!partyMembers[i].Alive || !partyMembers[i].Conditions.CanSelect())
                        portraitNames[i].TextColor = TextColor.DeadPartyMember;
                    else if (game.HasPartyMemberFled(partyMembers[i]))
                        portraitNames[i].TextColor = TextColor.DeadPartyMember;
                    else
                        portraitNames[i].TextColor = activeSlot == i ? TextColor.ActivePartyMember : TextColor.PartyMember;
                }
            }
        }

        internal void AttachToPortraitAnimationEvent(Action finishAction)
        {
            if (portraitAnimation == null)
                finishAction?.Invoke();
            else
            {
                var tempAnimation = portraitAnimation;
                void Finished()
                {
                    tempAnimation.Finished -= Finished;
                    finishAction?.Invoke();
                }
                tempAnimation.Finished += Finished;
            }
        }

        void PlayPortraitAnimation(int slot, PartyMember partyMember, Action finishAction = null)
        {
            var newState = partyMember == null ? PartyMemberPortaitState.Empty
                : partyMember.Alive ? PartyMemberPortaitState.Normal : PartyMemberPortaitState.Dead;

            if (portraitStates[slot] == newState)
            {
                finishAction?.Invoke();
                return;
            }

            bool animation = portraitStates[slot] != PartyMemberPortaitState.None && portraitStates[slot] != PartyMemberPortaitState.Dead;

            portraitStates[slot] = newState;
            uint newGraphicIndex = newState switch
            {
                PartyMemberPortaitState.Empty => Graphics.GetUIGraphicIndex(UIGraphic.EmptyCharacterSlot),
                PartyMemberPortaitState.Dead => partyMember.Race == Race.Animal ? Graphics.GetUIGraphicIndex(UIGraphic.CatSkull) : Graphics.GetUIGraphicIndex(UIGraphic.Skull),
                _ => Graphics.PortraitOffset + partyMember.PortraitIndex - 1
            };
            if (animation)
            {
                int yOffset = newState == PartyMemberPortaitState.Normal ? 34 : -34;
                var sprite = portraits[slot];
                var overlaySprite = RenderView.SpriteFactory.Create(32, 34, true, 1);
                overlaySprite.Layer = renderLayer;
                overlaySprite.X = Global.PartyMemberPortraitAreas[slot].Left + 1;
                overlaySprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1;
                overlaySprite.ClipArea = Global.PartyMemberPortraitAreas[slot].CreateModified(1, 1, -2, -2);
                overlaySprite.TextureAtlasOffset = sprite.TextureAtlasOffset;
                overlaySprite.PaletteIndex = game.PrimaryUIPaletteIndex;
                overlaySprite.Visible = true;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(newGraphicIndex);
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1 + yOffset;

                portraitAnimation = new PortraitAnimation
                {
                    StartTicks = game.BattleActive ? game.CurrentBattleTicks : game.CurrentAnimationTicks,
                    Offset = yOffset,
                    PrimarySprite = sprite,
                    SecondarySprite = overlaySprite
                };

                if (finishAction != null)
                {
                    void Finished()
                    {
                        if (portraitAnimation != null)
                            portraitAnimation.Finished -= Finished;
                        finishAction?.Invoke();
                    }
                    portraitAnimation.Finished += Finished;
                }
            }
            else
            {
                portraits[slot].TextureAtlasOffset = textureAtlas.GetOffset(newGraphicIndex);
                finishAction?.Invoke();
            }
        }

        /// <summary>
        /// While at a healer there is a golden symbol on the active portrait.
        /// </summary>
        public void SetCharacterHealSymbol(int? slot)
        {
            if (slot == null)
            {
                healerSymbol.Visible = false;
            }
            else
            {
                var area = Global.PartyMemberPortraitAreas[slot.Value];
                healerSymbol.X = area.X + 1;
                healerSymbol.Y = area.Y + 1;
                healerSymbol.Visible = true;
            }
        }

        public void DestroyItem(ItemSlot itemSlot, TimeSpan initialDelay, bool consumed = false, Action finishAction = null,
            Position animationPosition = null, bool applyRemoveEffects = true)
        {
            ItemGrid itemGrid = null;
            int slotIndex = -1;

            foreach (var grid in itemGrids)
            {
                slotIndex = grid.SlotFromItemSlot(itemSlot);

                if (slotIndex != -1)
                {
                    itemGrid = grid;
                    break;
                }
            }

            if (slotIndex == -1)
                throw new AmbermoonException(ExceptionScope.Application, "Invalid item slot");

            bool equipment = game.CurrentWindow.Window == Window.Inventory && itemGrid == itemGrids[1];

            // Scroll inventory into view if item is not visible
            if (!equipment && !itemGrid.SlotVisible(slotIndex))
            {
                int scrollOffset = slotIndex;

                if (scrollOffset % Inventory.VisibleWidth != 0)
                    scrollOffset -= scrollOffset % Inventory.VisibleWidth;

                itemGrid.ScrollTo(scrollOffset);
            }

            void ApplyItemRemoveEffects()
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);
                var partyMember = game.CurrentInventory ?? game.CurrentPartyMember;

                partyMember.TotalWeight -= item.Weight;

                if (equipment)
                    game.EquipmentRemoved(itemSlot.ItemIndex, 1, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                if (game.CurrentWindow.Window == Window.Inventory)
                    game.UpdateCharacterInfo();
            }

            if (consumed)
            {
                ItemAnimation.Play(game, RenderView, ItemAnimation.Type.Consume, animationPosition ?? itemGrid.GetSlotPosition(slotIndex),
                    finishAction, initialDelay);
                if (applyRemoveEffects)
                {
                    game.AddTimedEvent(initialDelay + TimeSpan.FromMilliseconds(200), () =>
                    {
                        ApplyItemRemoveEffects();
                        itemSlot.Remove(1);
                        itemGrid.SetItem(slotIndex, itemSlot);
                    });
                }
            }
            else
            {
                ItemAnimation.Play(game, RenderView, ItemAnimation.Type.Destroy, animationPosition ?? itemGrid.GetSlotPosition(slotIndex),
                    finishAction, initialDelay, null, itemManager.GetItem(itemSlot.ItemIndex));
                if (applyRemoveEffects)
                {
                    game.AddTimedEvent(initialDelay, () =>
                    {
                        ApplyItemRemoveEffects();
                        itemSlot.Remove(1);
                        itemGrid.SetItem(slotIndex, itemSlot);
                    });
                }
            }
        }

        public UIItem GetItem(ItemSlot itemSlot)
        {
            foreach (var itemGrid in itemGrids)
            {
                int slotIndex = itemGrid.SlotFromItemSlot(itemSlot);

                if (slotIndex != -1)
                    return itemGrid.GetItem(slotIndex);
            }

            return null;
        }

        public Position GetItemSlotPosition(ItemSlot itemSlot)
        {
            foreach (var itemGrid in itemGrids)
            {
                int slotIndex = itemGrid.SlotFromItemSlot(itemSlot);

                if (slotIndex != -1)
                    return itemGrid.GetSlotPosition(slotIndex);
            }

            return null;
        }

        public void UpdateCharacter(PartyMember partyMember, Action portraitAnimationFinishedHandler = null)
        {
            SetCharacter(game.SlotFromPartyMember(partyMember).Value, partyMember, false, portraitAnimationFinishedHandler);
        }

        public void UpdateCharacter(int slot, Action portraitAnimationFinishedHandler = null)
        {
            SetCharacter(slot, game.GetPartyMember(slot), false, portraitAnimationFinishedHandler);
        }

        /// <summary>
        /// Set portait to 0 to remove the portrait.
        /// </summary>
        public void SetCharacter(int slot, PartyMember partyMember, bool initialize = false,
            Action portraitAnimationFinishedHandler = null, bool forceAnimation = false)
        {
            var sprite = portraits[slot] ??= RenderView.SpriteFactory.Create(32, 34, true, 2);
            sprite.Layer = renderLayer;
            sprite.X = Global.PartyMemberPortraitAreas[slot].Left + 1;
            sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1;
            sprite.ClipArea = Global.PartyMemberPortraitAreas[slot].CreateModified(1, 1, -2, -2);
            if (initialize)
            {
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.EmptyCharacterSlot));
                portraitStates[slot] = PartyMemberPortaitState.None;
            }
            else
            {
                if (forceAnimation && portraitStates[slot] == PartyMemberPortaitState.None)
                    portraitStates[slot] = PartyMemberPortaitState.Empty;
                PlayPortraitAnimation(slot, partyMember, portraitAnimationFinishedHandler);
            }
            sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
            sprite.Visible = true;

            if (partyMember == null)
            {
                portraitBackgrounds[slot]?.Delete();
                portraitBackgrounds[slot] = null;
                portraitNames[slot]?.Delete();
                portraitNames[slot] = null;
                characterStatusIcons[slot]?.Delete();
                characterStatusIcons[slot] = null;
            }
            else
            {
                sprite = portraitBackgrounds[slot] ??= RenderView.SpriteFactory.Create(32, 34, true, 0);
                sprite.Layer = renderLayer;
                sprite.X = Global.PartyMemberPortraitAreas[slot].Left + 1;
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground);
                sprite.PaletteIndex = 52;
                sprite.Visible = true;

                var text = portraitNames[slot];
                var name = RenderView.TextProcessor.CreateText(partyMember.Name.Substring(0, Math.Min(5, partyMember.Name.Length)));

                if (text == null)
                {
                    text = portraitNames[slot] = RenderView.RenderTextFactory.Create(textLayer, name, TextColor.PartyMember, true,
                        new Rect(Global.PartyMemberPortraitAreas[slot].Left + 2, Global.PartyMemberPortraitAreas[slot].Top + 31, 30, 6),
                        TextAlign.Center);
                }
                else
                {
                    text.Text = name;
                }
                text.DisplayLayer = 3;
                text.PaletteIndex = game.PrimaryUIPaletteIndex;
                text.TextColor = partyMember.Alive ? game.CurrentPartyMember == partyMember ? TextColor.ActivePartyMember : TextColor.PartyMember : TextColor.DeadPartyMember;
                text.Visible = true;
                UpdateCharacterStatus(partyMember);
            }

            FillCharacterBars(slot, partyMember);

            if (initialize)
                portraitAnimationFinishedHandler?.Invoke();
        }

        internal void UpdateCharacterStatus(int slot, UIGraphic? graphicIndex = null)
        {
            var sprite = characterStatusIcons[slot] ??= RenderView.SpriteFactory.Create(16, 16, true, 3) as ILayerSprite;
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = game.PrimaryUIPaletteIndex;
            sprite.X = Global.PartyMemberPortraitAreas[slot].Left + 33;
            sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 2;

            if (graphicIndex != null)
            {
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(graphicIndex.Value));
                sprite.Visible = true;
            }
            else
            {
                sprite.Visible = false;
            }
        }

        internal void UpdateCharacterStatus(PartyMember partyMember)
        {
            int slot = game.SlotFromPartyMember(partyMember).Value;

            if (partyMember.Alive && partyMember.Overweight)
            {
                // Overweight
                UpdateCharacterStatus(slot, UIGraphic.StatusOverweight);
            }
            else if (partyMember.Conditions != Condition.None)
            {
                var conditions = partyMember.VisibleConditions;
                uint conditionCount = (uint)conditions.Count;

                if (conditionCount == 1)
                {
                    UpdateCharacterStatus(slot, Graphics.GetConditionGraphic(conditions[0]));
                }
                else
                {
                    uint ticksPerCondition = Game.TicksPerSecond * 2;
                    int index = (int)((game.CurrentTicks % (conditionCount * ticksPerCondition)) / ticksPerCondition);
                    UpdateCharacterStatus(slot, Graphics.GetConditionGraphic(conditions[index]));
                }
            }
            else
            {
                UpdateCharacterStatus(slot, null);
            }
        }

        public void FillCharacterBars(PartyMember partyMember) => FillCharacterBars(game.SlotFromPartyMember(partyMember).Value, partyMember);

        public void FillCharacterBars(int slot, PartyMember partyMember)
        {
            uint hp = partyMember?.Alive == false ? 0 : partyMember?.HitPoints.CurrentValue ?? 0;
            uint sp = partyMember?.Alive == false ? 0 : partyMember?.SpellPoints.CurrentValue ?? 0;
            float lpPercentage = partyMember == null || !partyMember.Alive ? 0.0f
                : Math.Min(1.0f, (float)hp / partyMember.HitPoints.TotalMaxValue);
            float spPercentage = partyMember == null || !partyMember.Alive || !partyMember.Class.IsMagic() ? 0.0f
                : Math.Min(1.0f, (float)sp / partyMember.SpellPoints.TotalMaxValue);

            characterBars[slot * 4 + 0]?.Fill(lpPercentage, hp != 0);
            characterBars[slot * 4 + 1]?.Fill(lpPercentage, hp != 0);
            characterBars[slot * 4 + 2]?.Fill(spPercentage, sp != 0);
            characterBars[slot * 4 + 3]?.Fill(spPercentage, sp != 0);
        }

        public void AddActiveSpell(ActiveSpellType activeSpellType, ActiveSpell activeSpell, bool battle)
        {
            if (activeSpellSprites.ContainsKey(activeSpellType))
                return;

            var baseLocation = battle ? new Position(0, 170) : new Position(208, 106);
            int index = (int)activeSpellType;
            uint graphicIndex = Graphics.GetUIGraphicIndex(UIGraphic.Candle + index);
            activeSpellSprites.Add(activeSpellType, AddSprite(new Rect(baseLocation.X + index * 16, baseLocation.Y, 16, 16), graphicIndex, game.UIPaletteIndex));

            activeSpellDurationBackgrounds.Add(activeSpellType, CreateArea(new Rect(baseLocation.X + 1 + index * 16, baseLocation.Y + 17, 14, 4),
                game.GetUIColor(26), 2));
            var durationBar = new Bar(filledAreas,
                CreateArea(new Rect(baseLocation.X + 2 + index * 16, baseLocation.Y + 18, 12, 2), game.GetUIColor(31), 3), 12, true);
            activeSpellDurationBars.Add(activeSpellType, durationBar);
            durationBar.Fill(activeSpell.Duration / 200.0f);
        }

        public void RemoveAllActiveSpells()
        {
            foreach (var activeSpell in Enum.GetValues<ActiveSpellType>())
                UpdateActiveSpell(activeSpell, null);
        }

        void UpdateActiveSpell(ActiveSpellType activeSpellType, ActiveSpell activeSpell)
        {
            if (activeSpell == null)
            {
                if (activeSpellSprites.ContainsKey(activeSpellType))
                {
                    activeSpellSprites[activeSpellType]?.Delete();
                    activeSpellSprites.Remove(activeSpellType);
                    activeSpellDurationBackgrounds[activeSpellType]?.Delete();
                    activeSpellDurationBackgrounds.Remove(activeSpellType);
                    activeSpellDurationBars[activeSpellType]?.Destroy();
                    activeSpellDurationBars.Remove(activeSpellType);
                }
            }
            else
            {
                if (!activeSpellSprites.ContainsKey(activeSpellType))
                {
                    AddActiveSpell(activeSpellType, activeSpell, false);
                }
                else // update duration display
                {
                    activeSpellDurationBars[activeSpellType].Fill(activeSpell.Duration / 200.0f, true);
                }
            }
        }

        internal string GetCompassString()
        {
            /// This contains all of theidrection starting with W (West) and going
            /// clock-wise until W again and then additional N-W and N again.
            /// It is used for the compass which can scroll and display
            /// 3 directions partially at once.
            /// English example: "W  N-W  N  N-E  E  S-E  S  S-W  W  N-W  N  "
            /// There are always 2 spaces between each direction. I think 1 space
            /// as a divider and 1 space to the right/left of the 1-character
            /// directions.
            string baseString = game.DataNameProvider.CompassDirections;
            int playerAngle = game.PlayerAngle;

            if (playerAngle < 0)
                playerAngle += 360;
            if (playerAngle >= 360)
                playerAngle -= 360;

            // The display is 32 pixels wide so when displaying for example the W
            // in the center (direction is exactle west), there are two spaces to
            // each size and a 1 pixel line of the S-W and N-W.
            // To accomplish that we display not 5 but 7 characters and clip the
            // text accordingly.

            // There are 32 possible text rotations. The first (0) is 1 left of N-W.
            // The last (31) is a bit left of N-W. Increasing rotates right.
            // Rotating by 45° (e.g. from N to N-E) needs 4 text index increases (1 step ~ 11°).
            // Rotating by 90° (e.g. from N to E) needs 8 text index increases (1 step ~ 11°).
            // There is 1° difference per 45° and therefore 8° for a full rotation of 360°.
            // The exact angle range for one text index is 360°/32 = 11.25°.

            // As the first index is for left of N-W and this is -56.25°, we use it as a base angle
            // by adding 45 to the real player angle.
            int index = Util.Round((playerAngle + 56.25f) / 11.25f);

            if (index >= 32)
                index -= 32;

            return baseString.Substring(index, 7);
        }

        public void AddSpecialItem(SpecialItemPurpose specialItem)
        {
            switch (specialItem)
            {
            case SpecialItemPurpose.Compass:
                {
                    specialItemSprites.Add(specialItem, AddSprite(new Rect(208, 73, 32, 32),
                        Graphics.GetUIGraphicIndex(UIGraphic.Compass), game.UIPaletteIndex, 4)); // Note: The display layer must be greater than the windchain layer
                    var text = AddText(new Rect(203, 86, 42, 7),
                        GetCompassString(), TextColor.BrightGray);
                    specialItemTexts.Add(SpecialItemPurpose.Compass, text);
                    text.Clip(new Rect(208, 86, 32, 7));
                    break;
                }
            case SpecialItemPurpose.MonsterEye:
                {
                    specialItemSprites.Add(specialItem, AddSprite(new Rect(240, 49, 32, 32),
                        Graphics.GetUIGraphicIndex(game.MonsterSeesPlayer ? UIGraphic.MonsterEyeActive
                        : UIGraphic.MonsterEyeInactive), game.UIPaletteIndex, 3));
                    break;
                }
            case SpecialItemPurpose.DayTime:
                {
                    specialItemSprites.Add(specialItem, AddSprite(new Rect(272, 73, 32, 32),
                        Graphics.GetUIGraphicIndex(UIGraphic.Night + (int)game.GameTime.GetDayTime()), game.UIPaletteIndex, 3));
                    break;
                }
            case SpecialItemPurpose.WindChain:
                specialItemSprites.Add(specialItem, AddSprite(new Rect(240, 89, 32, 15),
                    Graphics.GetUIGraphicIndex(UIGraphic.Windchain), game.UIPaletteIndex, 3));
                break;
            case SpecialItemPurpose.MapLocation:
                    specialItemTexts.Add(SpecialItemPurpose.MapLocation, AddText(new Rect(210, 50, 30, 14),
                    $"X:{game.PartyPosition.X + 1,3}^Y:{game.PartyPosition.Y + 1,3}", TextColor.BrightGray));
                break;
            case SpecialItemPurpose.Clock:
                    specialItemTexts.Add(SpecialItemPurpose.Clock, AddText(new Rect(273, 54, 30, 7),
                    $"{game.GameTime.Hour,2}:{game.GameTime.Minute:00}", TextColor.BrightGray));
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"Invalid special item: {specialItem}");
            };
        }

        void UpdateSpecialItems()
        {
            // Update compass
            if (specialItemTexts.ContainsKey(SpecialItemPurpose.Compass))
                specialItemTexts[SpecialItemPurpose.Compass].SetText(game.ProcessText(GetCompassString()));

            // Update monster eye
            if (specialItemSprites.ContainsKey(SpecialItemPurpose.MonsterEye))
                specialItemSprites[SpecialItemPurpose.MonsterEye].TextureAtlasOffset =
                    textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(game.MonsterSeesPlayer ? UIGraphic.MonsterEyeActive : UIGraphic.MonsterEyeInactive));

            // Update daytime display
            if (specialItemSprites.ContainsKey(SpecialItemPurpose.DayTime))
                specialItemSprites[SpecialItemPurpose.DayTime].TextureAtlasOffset =
                    textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Night + (int)game.GameTime.GetDayTime()));

            // Update map location
            if (specialItemTexts.ContainsKey(SpecialItemPurpose.MapLocation))
                specialItemTexts[SpecialItemPurpose.MapLocation].SetText(
                    game.ProcessText($"X:{game.PartyPosition.X + 1,3}^Y:{game.PartyPosition.Y + 1,3}"));

            // Update clock
            if (specialItemTexts.ContainsKey(SpecialItemPurpose.Clock))
                specialItemTexts[SpecialItemPurpose.Clock].SetText(
                    game.ProcessText($"{game.GameTime.Hour,2}:{game.GameTime.Minute:00}"));
        }

        public ISprite AddMapCharacterSprite(Rect rect, uint textureIndex, int baseLineOffset)
        {
            var sprite = RenderView.SpriteFactory.Create(rect.Width, rect.Height, false);
            sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Characters).GetOffset(textureIndex);
            sprite.BaseLineOffset = baseLineOffset;
            sprite.X = rect.Left;
            sprite.Y = rect.Top;
            sprite.PaletteIndex = (byte)(game.Map.PaletteIndex - 1);
            sprite.Layer = RenderView.GetLayer(Layer.Characters);
            sprite.Visible = true;
            additionalSprites.Add(sprite);
            return sprite;
        }

        public ILayerSprite AddSprite(Rect rect, uint textureIndex, byte paletteIndex, byte displayLayer,
            string tooltip, TextColor? tooltipTextColor, Layer? layer, out Tooltip createdTooltip, bool visible = true)
        {
            createdTooltip = null;
            var sprite = RenderView.SpriteFactory.Create(rect.Width, rect.Height, true) as ILayerSprite;
            sprite.TextureAtlasOffset = layer == null ? textureAtlas.GetOffset(textureIndex)
                : TextureAtlasManager.Instance.GetOrCreate(layer.Value).GetOffset(textureIndex);
            sprite.DisplayLayer = displayLayer;
            sprite.X = rect.Left;
            sprite.Y = rect.Top;
            sprite.PaletteIndex = paletteIndex;
            sprite.Layer = layer == null ? renderLayer : RenderView.GetLayer(layer.Value);
            sprite.Visible = visible;
            additionalSprites.Add(sprite);

            if (tooltip != null)
                createdTooltip = AddTooltip(rect, tooltip, tooltipTextColor ?? TextColor.White);

            return sprite;
        }

        public ILayerSprite AddSprite(Rect rect, uint textureIndex, byte paletteIndex, byte displayLayer = 2,
            string tooltip = null, TextColor? tooltipTextColor = null, Layer? layer = null, bool visible = true)
        {
            return AddSprite(rect, textureIndex, paletteIndex, displayLayer, tooltip, tooltipTextColor, layer, out _, visible);
        }

        public IAnimatedLayerSprite AddAnimatedSprite(Rect rect, uint textureIndex, byte paletteIndex,
            uint numFrames, byte displayLayer = 2, Layer? layer = null, bool visible = true)
        {
            var textureAtlas = layer == null ? this.textureAtlas : TextureAtlasManager.Instance.GetOrCreate(layer.Value);
            var sprite = RenderView.SpriteFactory.CreateAnimated(rect.Width, rect.Height, textureAtlas.Texture.Width, numFrames, true, displayLayer) as IAnimatedLayerSprite;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(textureIndex);
            sprite.DisplayLayer = displayLayer;
            sprite.X = rect.Left;
            sprite.Y = rect.Top;
            sprite.PaletteIndex = paletteIndex;
            sprite.Layer = layer == null ? renderLayer : RenderView.GetLayer(layer.Value);
            sprite.Visible = visible;
            additionalSprites.Add(sprite);
            return sprite;
        }

        internal Tooltip AddTooltip(Rect rect, string tooltip, TextColor tooltipTextColor, TextAlign textAlign = TextAlign.Center,
            Render.Color backgroundColor = null, bool centerOnScreen = false)
        {
            var toolTip = new Tooltip
            {
                Area = rect,
                Text = tooltip,
                TextColor = tooltipTextColor,
                TextAlign = textAlign,
                BackgroundColor = backgroundColor,
                CenterOnScreen = centerOnScreen
            };
            tooltips.Add(toolTip);
            return toolTip;
        }

        internal void RemoveTooltip(Tooltip tooltip)
        {
            tooltips.Remove(tooltip);

            if (activeTooltip == tooltip)
                HideTooltip();
        }

        internal void HideTooltip()
        {
            SetActiveTooltip(null, null);
            buttonGrid?.HideTooltips();
        }

        void SetActiveTooltip(Position cursorPosition, Tooltip tooltip)
        {
            if (tooltip == null) // remove
            {
                if (activeTooltipText != null)
                {
                    activeTooltipText?.Delete();
                    activeTooltipText = null;
                }

                if (activeTooltipBackground != null)
                {
                    activeTooltipBackground?.Delete();
                    activeTooltipBackground = null;

                    for (int i = 0; i < activeTooltipBorders.Length; ++i)
                    {
                        activeTooltipBorders[i]?.Delete();
                        activeTooltipBorders[i] = null;
                    }
                }
            }
            else
            {
                if (activeTooltipText == null)
                {
                    activeTooltipText = RenderView.RenderTextFactory.Create();
                    activeTooltipText.Shadow = true;
                    activeTooltipText.DisplayLayer = 250;
                    activeTooltipText.Layer = RenderView.GetLayer(Layer.Text);
                    activeTooltipText.Visible = true;
                }

                var text = RenderView.TextProcessor.CreateText(tooltip.Text);
                int textWidth = text.MaxLineSize * Global.GlyphWidth;

                activeTooltipText.Text = text;
                activeTooltipText.TextColor = tooltip.TextColor;
                int x = Util.Limit(0, tooltip.CenterOnScreen ? (Global.VirtualScreenWidth - textWidth) / 2 : cursorPosition.X - textWidth / 2,
                    Global.VirtualScreenWidth - textWidth);
                int y = cursorPosition.Y - text.LineCount * Global.GlyphLineHeight - 1;
                if (textWidth < Global.VirtualScreenWidth - 1)
                {
                    if (x == 0)
                        x = 2;
                    else if (x + textWidth >= Global.VirtualScreenWidth - 1)
                        x = Global.VirtualScreenWidth - textWidth - 2;
                }
                if (tooltip.BackgroundColor != null)
                {
                    if (y >= 2)
                        y -= 2;
                }
                if (y < 2 && cursorPosition.Y + text.LineCount * Global.GlyphLineHeight + 16 <= Global.VirtualScreenHeight)
                    y = cursorPosition.Y + 16;
                var textArea = new Rect(x, y, textWidth, text.LineCount * Global.GlyphLineHeight);
                activeTooltipText.Place(textArea, tooltip.TextAlign);

                if (tooltip.BackgroundColor != null)
                {
                    textArea = textArea.CreateModified(-2, -2, 4, 4);

                    if (activeTooltipBackground == null)
                    {
                        activeTooltipBackground = RenderView.ColoredRectFactory.Create(textArea.Width, textArea.Height, tooltip.BackgroundColor, 248);
                        activeTooltipBackground.Layer = RenderView.GetLayer(Layer.IntroEffects);
                        activeTooltipBackground.Visible = true;

                        activeTooltipBorders[0] = RenderView.ColoredRectFactory.Create(textArea.Width, 1, Render.Color.Black, 249);
                        activeTooltipBorders[1] = RenderView.ColoredRectFactory.Create(1, textArea.Height - 2, Render.Color.Black, 249);
                        activeTooltipBorders[2] = RenderView.ColoredRectFactory.Create(1, textArea.Height - 2, Render.Color.Black, 249);
                        activeTooltipBorders[3] = RenderView.ColoredRectFactory.Create(textArea.Width, 1, Render.Color.Black, 249);

                        for (int i = 0; i < 4; ++i)
                        {
                            activeTooltipBorders[i].Layer = RenderView.GetLayer(Layer.UI);
                            activeTooltipBorders[i].Visible = true;
                        }
                    }
                    else
                    {
                        activeTooltipBackground.Resize(textArea.Width, textArea.Height);
                        activeTooltipBackground.Color = tooltip.BackgroundColor;

                        activeTooltipBorders[0].Resize(textArea.Width, 1);
                        activeTooltipBorders[1].Resize(1, textArea.Height - 2);
                        activeTooltipBorders[2].Resize(1, textArea.Height - 2);
                        activeTooltipBorders[3].Resize(textArea.Width, 1);
                    }

                    activeTooltipBackground.X = textArea.X;
                    activeTooltipBackground.Y = textArea.Y;

                    activeTooltipBorders[0].X = textArea.X;
                    activeTooltipBorders[0].Y = textArea.Y;
                    activeTooltipBorders[1].X = textArea.X;
                    activeTooltipBorders[1].Y = textArea.Y + 1;
                    activeTooltipBorders[2].X = textArea.X + textArea.Width - 1;
                    activeTooltipBorders[2].Y = textArea.Y + 1;
                    activeTooltipBorders[3].X = textArea.X;
                    activeTooltipBorders[3].Y = textArea.Y + textArea.Height - 1;
                }
                else if (activeTooltipBackground != null)
                {
                    activeTooltipBackground?.Delete();
                    activeTooltipBackground = null;

                    for (int i = 0; i < 4; ++i)
                    {
                        activeTooltipBorders[i]?.Delete();
                        activeTooltipBorders[i] = null;
                    }
                }
            }

            activeTooltip = tooltip;
        }

        public UIText AddText(Rect rect, string text, TextColor color = TextColor.White,
            TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            return AddText(rect, RenderView.TextProcessor.CreateText(text), color, textAlign, displayLayer);
        }

        public UIText AddText(Rect rect, IText text, TextColor color = TextColor.White,
            TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            var uiText = new UIText(RenderView, game.UIPaletteIndex, text, rect, displayLayer, color, true, textAlign, false);
            texts.Add(uiText);
            return uiText;
        }

        public UIText AddScrollableText(Rect rect, IText text, TextColor color = TextColor.White,
            TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            var scrollableText = CreateScrollableText(rect, text, color, textAlign, displayLayer);
            texts.Add(scrollableText);
            return scrollableText;
        }

        public UIText CreateScrollableText(Rect rect, IText text, TextColor color = TextColor.White,
            TextAlign textAlign = TextAlign.Left, byte displayLayer = 2, bool shadow = true, byte? paletteIndex = null)
        {
            var scrollableText = new UIText(RenderView, paletteIndex ?? game.UIPaletteIndex, text, rect,
                displayLayer, color, shadow, textAlign, true, game.AddTimedEvent);
            scrollableText.FreeScrollingStarted += () =>
            {
                freeScrolledText = scrollableText;
                game.ExecuteNextUpdateCycle(() => game.CursorType = CursorType.None);
            };
            scrollableText.FreeScrollingEnded += () =>
            {
                freeScrolledText = null;
                game.ExecuteNextUpdateCycle(() =>
                {
                    game.CursorType = CursorType.Sword;
                    game.UpdateCursor();
                });
            };
            return scrollableText;
        }

        public void Set80x80Picture(Picture80x80 picture, int x = Global.LayoutX + 16, int y = Global.LayoutY + 6)
        {
            if (picture == Picture80x80.None)
            {
                if (sprite80x80Picture != null)
                    sprite80x80Picture.Visible = false;
            }
            else
            {
                var sprite = sprite80x80Picture ??= RenderView.SpriteFactory.Create(80, 80, true);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = x;
                sprite.Y = y;
                sprite.PaletteIndex = game.UIPaletteIndex;
                sprite.Layer = renderLayer;
                sprite.Visible = true;
            }
        }

        public void AddEventPicture(uint index, out byte palette)
        {
            var sprite = eventPicture ??= RenderView.SpriteFactory.Create(320, 92, true, 10) as ILayerSprite;
            palette = sprite.PaletteIndex = index switch
            {
                0 => 26,
                1 => 31,
                2 => 32,
                3 => 32,
                4 => 32,
                5 => 32,
                6 => 32,
                7 => 32,
                8 => 37,
                _ => throw new AmbermoonException(ExceptionScope.Data, $"Invalid event picture index: {index}. Valid indices are 0 to 8.")
            };
            sprite.Layer = renderLayer;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.EventPictureOffset + index);
            sprite.X = 0;
            sprite.Y = 38;
            sprite.Visible = true;
        }

        public void CancelDrag()
        {
            if (draggedItem != null)
            {
                if (draggedItem.Reset(game, this))
                    return;
                DropItem();
            }

            if (draggedGold != 0 || draggedFood != 0)
            {
                draggedGold = 0;
                draggedFood = 0;
                draggedGoldOrFoodRemover = null;
                DropItem();
                game.UntrapMouse();
            }

            // Remove hand icons and set current status icons
            game.PartyMembers.ToList().ForEach(p => UpdateCharacterStatus(p));
        }

        void DropItem()
        {
            draggedItem = null;

            if (game.OpenStorage is Chest ||
                game.OpenStorage is Merchant ||
                game.OpenStorage is NonItemPlace)
            {
                ChestText?.Destroy();
                ChestText = null;
            }
            else if (!(game.OpenStorage is Game.ConversationItems))
            {
                SetInventoryMessage(null);
            }

            // Remove hand icons and set current status icons
            game.PartyMembers.ToList().ForEach(p => UpdateCharacterStatus(p));

            DraggedItemDropped?.Invoke();
        }

        bool IsInventory => Type == LayoutType.Inventory;
        bool HasScrollableItemGrid => IsInventory ||
            ((Type == LayoutType.Items || Type == LayoutType.Conversation) &&
                itemGrids.Count != 0 && !itemGrids[0].Disabled);

        public void AddItemGrid(ItemGrid itemGrid)
        {
            itemGrids.Add(itemGrid);
        }

        internal IColoredRect CreateArea(Rect rect, Render.Color color, byte displayLayer = 0, FilledAreaType type = FilledAreaType.Custom)
        {
            var coloredRect = RenderView.ColoredRectFactory.Create(rect.Width, rect.Height,
                color, displayLayer);
            coloredRect.Layer = type == FilledAreaType.FadeEffect || type == FilledAreaType.CustomEffect
                ? RenderView.GetLayer(Layer.Effects) : renderLayer;
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

        public FilledArea FillArea(Rect rect, Render.Color color, bool topMost)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, (byte)(topMost ? 245 : 0)));
        }

        public FilledArea FillArea(Rect rect, Render.Color color, byte displayLayer)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, displayLayer));
        }

        public Panel AddPanel(Rect rect, byte displayLayer)
        {
            return new Panel(game, rect, filledAreas, this, displayLayer);
        }

        public void AddColorFader(Rect rect, Render.Color startColor, Render.Color endColor,
            int durationInMilliseconds, bool removeWhenFinished, DateTime? startTime = null)
        {
            var now = DateTime.Now;
            var startingTime = startTime ?? now;
            var initialColor = startingTime > now ? Render.Color.Transparent : startColor;

            fadeEffects.Add(new FadeEffect(fadeEffectAreas, CreateArea(rect, initialColor, 255, FilledAreaType.FadeEffect), startColor,
                endColor, durationInMilliseconds, startingTime, removeWhenFinished));
        }

        public void AddFadeEffect(Rect rect, Render.Color color, FadeEffectType fadeEffectType,
            int durationInMilliseconds)
        {
            switch (fadeEffectType)
            {
                case FadeEffectType.FadeIn:
                    AddColorFader(rect, new Render.Color(color, 0), color, durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeOut:
                    AddColorFader(rect, color, new Render.Color(color, 0), durationInMilliseconds, true);
                    break;
                case FadeEffectType.FadeInAndOut:
                    var quarterDuration = durationInMilliseconds / 4;
                    var halfDuration = quarterDuration * 2;
                    AddColorFader(rect, new Render.Color(color, 0), color, quarterDuration, true);
                    AddColorFader(rect, color, color, quarterDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(quarterDuration));
                    AddColorFader(rect, color, new Render.Color(color, 0), halfDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(halfDuration));
                    break;
            }
        }

        public void UpdateItemGrids()
        {
            foreach (var itemGrid in itemGrids)
            {
                itemGrid.Refresh();
            }
        }

        public void UpdateItemSlot(ItemSlot itemSlot)
        {
            foreach (var itemGrid in itemGrids)
            {
                int slotIndex = itemGrid.SlotFromItemSlot(itemSlot);

                if (slotIndex != -1)
                {
                    itemGrid.SetItem(slotIndex, itemSlot);
                    break;
                }
            }
        }

        public void Update(uint currentTicks)
        {
            buttonGrid.Update(currentTicks);
            activePopup?.Update(game.CurrentPopupTicks);

            for (int i = fadeEffects.Count - 1; i >= 0; --i)
            {
                fadeEffects[i].Update();

                if (fadeEffects[i].Destroyed)
                    fadeEffects.RemoveAt(i);
            }

            if (Type == LayoutType.Map2D || Type == LayoutType.Map3D)
            {
                foreach (var activeSpell in Enum.GetValues<ActiveSpellType>())
                {
                    UpdateActiveSpell(activeSpell, game.CurrentSavegame.ActiveSpells[(int)activeSpell]);
                }

                UpdateSpecialItems();
            }

            if (portraitAnimation != null)
            {
                const int animationTime = (int)Game.TicksPerSecond / 2;
                uint elapsed = (game.BattleActive ? game.CurrentBattleTicks : game.CurrentAnimationTicks) - portraitAnimation.StartTicks;

                if (elapsed > animationTime)
                {
                    portraitAnimation.PrimarySprite.Y = 1;
                    portraitAnimation.SecondarySprite.Delete();
                    var tempAnimation = portraitAnimation;
                    portraitAnimation = null;
                    tempAnimation.OnFinished();
                }
                else
                {
                    int diff;

                    if (portraitAnimation.Offset < 0)
                    {
                        portraitAnimation.Offset = Math.Min(0, -34 + (int)elapsed * 34 / animationTime);
                        diff = 34;
                    }
                    else
                    {
                        portraitAnimation.Offset = Math.Max(0, 34 - (int)elapsed * 34 / animationTime);
                        diff = -34;
                    }

                    portraitAnimation.PrimarySprite.Y = 1 + portraitAnimation.Offset;
                    portraitAnimation.SecondarySprite.Y = portraitAnimation.PrimarySprite.Y + diff;
                }
            }

            // The spell Blink uses blinking battle field slot markers
            foreach (var slotMarker in battleFieldSlotMarkers.Values)
            {
                if (slotMarker.BlinkStartTicks == null)
                    slotMarker.Sprite.Visible = true;
                else
                {
                    uint diff = game.CurrentBattleTicks - slotMarker.BlinkStartTicks.Value;
                    if (slotMarker.ToggleColors)
                    {
                        var slotColor = (diff % (TicksPerBlink * 2) < TicksPerBlink) ? BattleFieldSlotColor.Orange : BattleFieldSlotColor.Yellow;
                        uint textureIndex = Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.BattleFieldYellowBorder + (uint)slotColor - 1;
                        slotMarker.Sprite.Visible = true;
                        slotMarker.Sprite.TextureAtlasOffset = textureAtlas.GetOffset(textureIndex);
                    }
                    else
                    {
                        slotMarker.Sprite.Visible = diff % (TicksPerBlink * 2) < TicksPerBlink;
                    }
                }
            }
        }

        public bool KeyChar(char ch)
        {
            if (questionYesButton != null && (char.ToLower(ch) == 'y' || char.ToLower(ch) == 'j'))
            {
                questionYesButton.PressImmediately(game, false, true);
                return true;
            }

            if (questionNoButton != null && char.ToLower(ch) == 'n')
            {
                questionNoButton.PressImmediately(game, false, true);
                return true;
            }

            if (PopupActive && activePopup.KeyChar(ch))
                return true;

            return false;
        }

        public void KeyDown(Key key, KeyModifiers keyModifiers)
        {
            if (PopupActive && activePopup.KeyDown(key))
                return;

            if (!game.InputEnable)
                return;

            switch (key)
            {
                case Key.Up:
                    if (!PopupActive && HasScrollableItemGrid)
                    {
                        if (keyModifiers.HasFlag(KeyModifiers.Shift))
                            itemGrids[0].ScrollToBegin();
                        else
                            itemGrids[0].ScrollUp();
                    }
                    break;
                case Key.Down:
                    if (!PopupActive && HasScrollableItemGrid)
                    {
                        if (keyModifiers.HasFlag(KeyModifiers.Shift))
                            itemGrids[0].ScrollToEnd();
                        else
                            itemGrids[0].ScrollDown();
                    }
                    break;
                case Key.PageUp:
                    if (!PopupActive && HasScrollableItemGrid)
                        itemGrids[0].ScrollPageUp();
                    break;
                case Key.PageDown:
                    if (!PopupActive && HasScrollableItemGrid)
                        itemGrids[0].ScrollPageDown();
                    break;
                case Key.Home:
                    if (!PopupActive && HasScrollableItemGrid)
                        itemGrids[0].ScrollToBegin();
                    break;
                case Key.End:
                    if (!PopupActive && HasScrollableItemGrid)
                        itemGrids[0].ScrollToEnd();
                    break;
            }
        }

        public bool ScrollX(bool right)
        {
            // not used as of now
            return false;
        }

        public bool ScrollY(bool down)
        {
            if (OptionMenuOpen && PopupActive && activePopup.Scroll(down))
                return true;

            if (!game.InputEnable)
                return false;

            if (PopupActive && activePopup.Scroll(down))
                return true;

            if (HasScrollableItemGrid)
                return itemGrids[0].Scroll(down);

            return false;
        }

        public void LeftMouseUp(Position position, out CursorType? newCursorType, uint currentTicks)
        {
            newCursorType = null;

            if (ignoreNextMouseUp)
            {
                ignoreNextMouseUp = false;
                return;
            }

            if (PopupActive)
            {
                activePopup.LeftMouseUp(position);
                return;
            }

            if (questionYesButton != null || questionNoButton != null)
            {
                // If those buttons are existing, only react to those buttons.
                questionYesButton?.LeftMouseUp(position, currentTicks);
                questionNoButton?.LeftMouseUp(position, currentTicks);
                return;
            }

            if (freeScrolledText != null)
                return;

            buttonGrid.MouseUp(position, MouseButtons.Left, out CursorType? cursorType, currentTicks);

            if (cursorType != null)
            {
                newCursorType = cursorType;
                return;
            }

            if (!game.InputEnable)
                return;

            foreach (var itemGrid in itemGrids)
                itemGrid.LeftMouseUp(position);

            if (Type == LayoutType.Battle)
            {
                if (Global.BattleFieldArea.Contains(position))
                {
                    int slotColumn = (position.X - Global.BattleFieldX) / Global.BattleFieldSlotWidth;
                    int slotRow = (position.Y - Global.BattleFieldY) / Global.BattleFieldSlotHeight;

                    BattleFieldSlotClicked?.Invoke(slotColumn, slotRow, MouseButtons.Left);
                }
            }
        }

        public void RightMouseUp(Position position, out CursorType? newCursorType, uint currentTicks)
        {
            if (TextInput.FocusedInput != null)
            {
                newCursorType = CursorType.None;
                return;
            }

            if (PopupActive)
            {
                newCursorType = null;
                activePopup.RightMouseUp(position);
                return;
            }

            buttonGrid.MouseUp(position, MouseButtons.Right, out newCursorType, currentTicks);

            if (!game.InputEnable)
                return;

            if (Type == LayoutType.Battle)
            {
                if (Global.BattleFieldArea.Contains(position))
                {
                    int slotColumn = (position.X - Global.BattleFieldX) / Global.BattleFieldSlotWidth;
                    int slotRow = (position.Y - Global.BattleFieldY) / Global.BattleFieldSlotHeight;

                    BattleFieldSlotClicked?.Invoke(slotColumn, slotRow, MouseButtons.Right);
                }
            }
        }

        public void AbortPickingTargetInventory()
        {
            if (game.CurrentWindow.Window == Window.Inventory)
            {
                itemGrids[0]?.ClearItemClickEventHandlers();
                itemGrids[1]?.ClearItemClickEventHandlers();
            }
            game.AbortPickingTargetInventory();
        }

        public bool Click(Position position, MouseButtons buttons, ref CursorType cursorType,
            uint currentTicks, bool pickingNewLeader = false, bool pickingTargetPlayer = false,
            bool pickingTargetInventory = false, KeyModifiers keyModifiers = KeyModifiers.None)
        {
            if (pickingTargetPlayer)
            {
                if (buttons == MouseButtons.Right)
                {
                    game.AbortPickingTargetPlayer();
                    return true;
                }
            }
            else if (pickingTargetInventory)
            {
                if (Type == LayoutType.Inventory && buttons == MouseButtons.Left)
                {
                    foreach (var itemGrid in itemGrids)
                    {
                        if (itemGrid.Click(position, draggedItem, out ItemGrid.ItemAction _,
                            buttons, ref cursorType, null))
                        {
                            return true;
                        }
                    }
                }

                if (buttons == MouseButtons.Right)
                {
                    AbortPickingTargetInventory();
                    return true;
                }
            }
            else if (!pickingNewLeader)
            {
                if (freeScrolledText != null)
                {
                    freeScrolledText.Click(position);
                    return true;
                }
                else if (Type == LayoutType.Event && !game.GameOverButtonsVisible)
                {
                    cursorType = CursorType.Click;
                    texts[0].Click(position);
                    return true;
                }
                else if (questionYesButton != null || questionNoButton != null)
                {
                    // If those buttons are existing, only react to those buttons.
                    return questionYesButton?.LeftMouseDown(position, currentTicks) == true ||
                           questionNoButton?.LeftMouseDown(position, currentTicks) == true;
                }
                else if (activePopup?.CloseOnClick != true)
                {
                    if (ChestText != null)
                    {
                        if (buttons == MouseButtons.Left || buttons == MouseButtons.Right)
                        {
                            if (ChestText.Click(position))
                            {
                                cursorType = ChestText?.WithScrolling == true ? CursorType.Click : CursorType.Sword;
                                return true;
                            }
                        }
                    }
                    else if (InventoryMessageWaitsForClick)
                    {
                        if (buttons == MouseButtons.Left || buttons == MouseButtons.Right)
                        {
                            inventoryMessage.Click(position);
                            cursorType = inventoryMessage == null ? CursorType.Sword : CursorType.Click;
                            return true;
                        }
                    }
                    else if (game.ConversationTextActive && Type == LayoutType.Conversation)
                    {
                        cursorType = CursorType.Click;
                        var scrollText = texts.Last(text => text.WithScrolling);
                        scrollText?.Click(position);
                        return true;
                    }
                }

                if (PopupActive)
                {
                    if (!activePopup.CloseOnClick && buttons == MouseButtons.Right && activePopup.TestButtonRightClick(position))
                        return true;

                    if (activePopup.CloseOnClick || (buttons == MouseButtons.Right && activePopup.CanAbort &&
                        (!activePopup.HasTextInput() || TextInput.FocusedInput == null)))
                    {
                        ClosePopup();
                        return true;
                    }
                    else
                    {
                        if (activePopup.Click(position, buttons, out ignoreNextMouseUp))
                            return true;
                    }

                    if (activePopup.DisableButtons || TextInput.FocusedInput != null)
                        return false;
                }

                if (draggedItem == null && buttonGrid.MouseDown(position, buttons, out CursorType? newCursorType, currentTicks))
                {
                    if (newCursorType != null)
                        cursorType = newCursorType.Value;
                    return true;
                }

                if (!game.InputEnable || PopupActive)
                    return false;

                if (Type == LayoutType.BattlePositions &&
                    game.BattlePositionWindowClick(position, buttons))
                {
                    cursorType = CursorType.Sword;
                    return true;
                }

                if (buttons == MouseButtons.Left)
                {
                    foreach (var itemGrid in itemGrids)
                    {
                        if
                        (
                            itemGrid.Click(position, draggedItem, out ItemGrid.ItemAction itemAction,
                                buttons, ref cursorType, item =>
                                {
                                    draggedItem = item;
                                    draggedItem.Item.Position = position;
                                    draggedItem.SourcePlayer = IsInventory ? game.CurrentInventoryIndex : null;
                                    PostItemDrag();
                                }, keyModifiers
                            )
                        )
                        {
                            if (itemAction == ItemGrid.ItemAction.Drop)
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
                            if (itemGrid.Click(position, null, out var _, buttons, ref cursorType,
                                item =>
                                {
                                    draggedItem = item;
                                    draggedItem.Item.Position = position;
                                    draggedItem.SourcePlayer = IsInventory ? game.CurrentInventoryIndex : null;
                                    PostItemDrag();
                                }
                            ))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else if (buttons != MouseButtons.Left)
            {
                return false;
            }

            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var partyMember = game.GetPartyMember(i);

                if (partyMember == null)
                    continue;

                if (Global.ExtendedPartyMemberPortraitAreas[i].Contains(position))
                {
                    if (draggedItem != null)
                    {
                        if (buttons == MouseButtons.Left)
                        {
                            if (draggedItem.SourcePlayer == i && draggedItem.Equipped != true)
                            {
                                CancelDrag();
                            }
                            else
                            {
                                bool droppedOnce = false;

                                while (true)
                                {
                                    if (!partyMember.CanTakeItems(itemManager, draggedItem.Item.Item) ||
                                        game.HasPartyMemberFled(partyMember))
                                    {
                                        if (droppedOnce)
                                            break;
                                        else
                                            return false;
                                    }

                                    int remaining = game.DropItem(i, null, draggedItem.Item.Item);

                                    if (remaining == 0)
                                    {
                                        draggedItem.Item.Destroy();

                                        if (draggedItem.SourcePlayer == null && game.OpenStorage != null)
                                            game.ItemRemovedFromStorage();

                                        DropItem();
                                        break;
                                    }
                                    else
                                        draggedItem.Item.Update(false);

                                    droppedOnce = true;
                                }
                            }

                            if (game.CurrentInventoryIndex == i)
                            {
                                itemGrids[0].Refresh();
                            }
                        }
                        else if (buttons == MouseButtons.Right)
                        {
                            // Only allow opening inventory with dragged item if we are
                            // not inside a chest window.
                            if (game.CurrentInventory != null)
                            {
                                if (i != game.CurrentInventoryIndex)
                                {
                                    if (game.HasPartyMemberFled(partyMember))
                                        return false;
                                    else
                                        game.OpenPartyMember(i, Type != LayoutType.Stats);
                                }
                                else
                                    return false;
                            }
                            else // In chest window right click aborts dragging instead
                            {
                                CancelDrag();
                            }
                        }

                        return true;
                    }
                    else if (draggedGold != 0)
                    {
                        if (buttons == MouseButtons.Left)
                        {
                            if (partyMember.MaxGoldToTake >= draggedGold && partyMember.Race != Race.Animal)
                            {
                                partyMember.AddGold(draggedGold);
                                draggedGoldOrFoodRemover?.Invoke(draggedGold);
                                CancelDrag();
                                game.CursorType = CursorType.Sword;
                            }
                            else
                                cursorType = CursorType.Gold;
                        }
                        else if (buttons == MouseButtons.Right)
                        {
                            draggedGoldOrFoodRemover?.Invoke(0);
                            CancelDrag();
                            game.CursorType = CursorType.Sword;
                        }

                        return true;
                    }
                    else if (draggedFood != 0)
                    {
                        if (buttons == MouseButtons.Left)
                        {
                            if (partyMember.MaxFoodToTake >= draggedFood && partyMember.Race != Race.Animal)
                            {
                                partyMember.AddFood(draggedFood);
                                draggedGoldOrFoodRemover?.Invoke(draggedFood);
                                CancelDrag();
                                game.CursorType = CursorType.Sword;
                            }
                            else
                                cursorType = CursorType.Food;
                        }
                        else if (buttons == MouseButtons.Right)
                        {
                            draggedGoldOrFoodRemover?.Invoke(0);
                            CancelDrag();
                            game.CursorType = CursorType.Sword;
                        }

                        return true;
                    }
                    else
                    {
                        if (buttons == MouseButtons.Left)
                        {
                            if (pickingTargetPlayer)
                            {
                                if (partyMember != null)
                                    game.FinishPickingTargetPlayer(i);
                                return true;
                            }
                            else if (pickingTargetInventory)
                            {
                                if (partyMember != null)
                                {
                                    bool canAccessInventory = !game.HasPartyMemberFled(partyMember) && partyMember.Conditions.CanOpenInventory();
                                    if (canAccessInventory)
                                        TargetInventoryPlayerSelected(i, partyMember);
                                }
                                return true;
                            }

                            game.SetActivePartyMember(i);
                        }
                        else if (buttons == MouseButtons.Right)
                            game.OpenPartyMember(i, Type != LayoutType.Stats);

                        return true;
                    }
                }
            }

            if (buttons == MouseButtons.Right && IsDragging)
            {
                CancelDrag();
                return true;
            }

            if (draggedGold != 0)
                cursorType = CursorType.Gold;
            if (draggedFood != 0)
                cursorType = CursorType.Food;

            return false;
        }

        internal void TargetInventoryPlayerSelected(int slot, PartyMember partyMember)
        {
            void FinishPickingTargetItem(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
            {
                if (itemSlot.ItemIndex == 0)
                    return;

                itemGrids[0].ItemClicked -= FinishPickingTargetItem;
                itemGrids[1].ItemClicked -= FinishPickingTargetItem;
                game.FinishPickingTargetInventory(itemGrid, slotIndex, itemSlot);
            }

            if (game.FinishPickingTargetInventory(slot))
            {
                if (partyMember.Conditions.CanOpenInventory())
                {
                    game.OpenPartyMember(slot, true, () =>
                    {
                        SetInventoryMessage(game.DataNameProvider.WhichItemAsTarget);
                        game.TrapMouse(Global.InventoryAndEquipTrapArea);
                        itemGrids[0].DisableDrag = true;
                        itemGrids[1].DisableDrag = true;
                        itemGrids[0].ItemClicked += FinishPickingTargetItem;
                        itemGrids[1].ItemClicked += FinishPickingTargetItem;
                    });
                }
            }
        }

        void PostItemDrag()
        {
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var partyMember = game.GetPartyMember(i);

                if (partyMember?.Alive == true)
                {
                    UpdateCharacterStatus(i, partyMember.CanTakeItems(itemManager, draggedItem.Item.Item) &&
                        !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                }
            }

            if (game.CurrentWindow.Window != Window.Inventory && (game.OpenStorage is Chest || game.OpenStorage is Merchant))
                ShowChestMessage(game.DataNameProvider.WhereToMoveIt);
            else if (!(game.OpenStorage is Game.ConversationItems))
                SetInventoryMessage(game.DataNameProvider.WhereToMoveIt);
        }

        public void SaveListScrollDrag(Position position, ref CursorType cursorType)
        {
            if (PopupActive && activePopup.Drag(position))
                cursorType = CursorType.None;
        }

        public void Drag(Position position, ref CursorType cursorType)
        {
            if (activePopup != null && activePopup.Drag(position))
            {
                cursorType = CursorType.None;
                return;
            }

            foreach (var itemGrid in itemGrids)
            {
                if (itemGrid.Drag(position))
                {
                    cursorType = CursorType.None;
                    return;
                }
            }
        }

        internal void DragItems(UIItem uiItem, bool takeAll, Action<DraggedItem, int> dragAction,
            Func<DraggedItem> dragger)
        {
            void DragItem(uint amount)
            {
                ClosePopup(false);

                if (amount > 0)
                    dragAction?.Invoke(dragger?.Invoke(), (int)amount);
            }

            if (takeAll || uiItem.Item.Amount == 1)
            {
                DragItem((uint)uiItem.Item.Amount);
            }
            else
            {
                var item = itemManager.GetItem(uiItem.Item.ItemIndex);
                OpenAmountInputBox(game.DataNameProvider.TakeHowManyMessage, item.GraphicIndex, item.Name,
                    (uint)uiItem.Item.Amount, DragItem);
            }
        }

        public void UpdateDraggedItemPosition(Position position)
        {
            if (draggedItem != null)
            {
                draggedItem.Item.Position = position;
            }
        }

        public void MouseMoved(Position diff)
        {
            if (freeScrolledText != null)
            {
                freeScrolledText?.MouseMove(diff.Y);
            }
        }

        public void HoverButtonGrid(Position position)
        {
            if (game.Configuration.ShowButtonTooltips)
            {
                HideTooltip();
                buttonGrid?.Hover(position);
            }
        }

        public bool Hover(Position position, ref CursorType cursorType)
        {
            if (PopupActive)
            {
                activePopup.Hover(position);
                return true;
            }

            if (Type == LayoutType.BattlePositions)
            {
                game.BattlePositionWindowDrag(position);
                return true;
            }

            if (draggedItem != null)
            {
                draggedItem.Item.Position = position;
                cursorType = CursorType.SmallArrow;
            }
            else if (cursorType == CursorType.None || (cursorType >= CursorType.ArrowUp && cursorType <= CursorType.Wait))
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

            if (!consumed)
            {
                if (!game.BattleRoundActive)
                {
                    foreach (var tooltip in tooltips)
                    {
                        if (tooltip.Area.Contains(position))
                        {
                            SetActiveTooltip(position, tooltip);
                            consumed = true;
                            break;
                        }
                    }
                }

                if (!consumed)
                {
                    SetActiveTooltip(position, null);
                    HoverButtonGrid(position);
                }
            }

            return consumed;
        }

        public Action GetButtonAction(int index) => buttonGrid.GetButtonAction(index);

        public CursorType? PressButton(int index, uint currentTicks)
        {
            if (PopupActive)
                return null;

            return buttonGrid.PressButton(index, currentTicks);
        }

        public void ReleaseButton(int index, bool immediately = false)
        {
            buttonGrid.ReleaseButton(index, immediately);
        }

        public void ReleaseButtons(bool immediately = false)
        {
            for (int i = 0; i < 9; ++i)
                ReleaseButton(i, immediately);
        }

        public static Position GetPlayerSlotCenterPosition(int column)
        {
            return new Position(40 + column * 40 + 20, Global.CombatBackgroundArea.Center.Y);
        }

        public static Position GetPlayerSlotTargetPosition(int column)
        {
            return new Position(40 + column * 40 + 20, Global.CombatBackgroundArea.Bottom);
        }

        // This is used for spells and effects. X is center of monster and Y is in the upper half.
        public static Position GetMonsterCombatCenterPosition(IRenderView renderView, int position, Monster monster)
        {
            int column = position % 6;
            int row = position / 6;
            var combatBackgroundArea = Global.CombatBackgroundArea;
            int centerX = combatBackgroundArea.Width / 2;
            float sizeMultiplier = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);
            int slotWidth = Util.Round(40 * sizeMultiplier);
            int height = Util.Round(sizeMultiplier * monster.MappedFrameHeight);
            return new Position(centerX - (3 - column) * slotWidth + slotWidth / 2, combatBackgroundArea.Y + BattleEffects.RowYOffsets[row] - height / 2);
        }

        public static Position GetMonsterCombatGroundPosition(IRenderView renderView, int position)
        {
            int column = position % 6;
            int row = position / 6;
            var combatBackgroundArea = Global.CombatBackgroundArea;
            int centerX = combatBackgroundArea.Width / 2;
            float sizeMultiplier = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);
            int slotWidth = Util.Round(40 * sizeMultiplier);
            return new Position(centerX - (3 - column) * slotWidth + slotWidth / 2, combatBackgroundArea.Y + BattleEffects.RowYOffsets[row]);
        }

        public static Position GetMonsterCombatTopPosition(IRenderView renderView, int position, Monster monster)
        {
            int column = position % 6;
            int row = position / 6;
            var combatBackgroundArea = Global.CombatBackgroundArea;
            int centerX = combatBackgroundArea.Width / 2;
            float sizeMultiplier = renderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);
            int slotWidth = Util.Round(40 * sizeMultiplier);
            int height = Util.Round(sizeMultiplier * monster.MappedFrameHeight);
            return new Position(centerX - (3 - column) * slotWidth + slotWidth / 2, combatBackgroundArea.Y + BattleEffects.RowYOffsets[row] - height);
        }

        public Position GetMonsterCombatCenterPosition(int position, Monster monster)
        {
            return GetMonsterCombatCenterPosition(RenderView, position, monster);
        }

        public Position GetMonsterCombatCenterPosition(int column, int row, Monster monster)
        {
            return GetMonsterCombatCenterPosition(column + row * 6, monster);
        }

        public Position GetMonsterCombatTopPosition(int position, Monster monster)
        {
            return GetMonsterCombatTopPosition(RenderView, position, monster);
        }

        public BattleAnimation AddMonsterCombatSprite(int column, int row, Monster monster, byte displayLayer,
            byte paletteIndex)
        {
            float sizeMultiplier = RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);            
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleMonsterRow);
            var sprite = RenderView.SpriteFactory.Create((int)monster.MappedFrameWidth, (int)monster.MappedFrameHeight, true) as ILayerSprite;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(monster.Index);
            sprite.DisplayLayer = displayLayer;
            sprite.PaletteIndex = paletteIndex;
            sprite.Layer = RenderView.GetLayer(Layer.BattleMonsterRow);
            sprite.Visible = true;
            var animation = new BattleAnimation(sprite);
            animation.SetStartFrame(GetMonsterCombatCenterPosition(column, row, monster), sizeMultiplier);
            monsterCombatGraphics.Add(new MonsterCombatGraphic
            {
                Monster = monster,
                Row = row,
                Column = column,
                Animation = animation,
                BattleFieldSprite = AddSprite(new Rect
                (
                    Global.BattleFieldX + column * Global.BattleFieldSlotWidth,
                    Global.BattleFieldY + row * Global.BattleFieldSlotHeight - 1,
                    Global.BattleFieldSlotWidth, Global.BattleFieldSlotHeight + 1
                ), Graphics.BattleFieldIconOffset + (uint)Class.Monster + (uint)monster.CombatGraphicIndex - 1,
                game.PrimaryUIPaletteIndex, (byte)(3 + row), monster.Name, TextColor.BattleMonster, Layer.UI, out Tooltip tooltip),
                Tooltip = tooltip
            });
            return animation;
        }

        public void RemoveMonsterCombatSprite(Monster monster)
        {
            var monsterCombatGraphic = monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster);

            if (monsterCombatGraphic != null)
            {
                monsterCombatGraphic.Animation?.Destroy();
                monsterCombatGraphic.BattleFieldSprite?.Delete();
                RemoveTooltip(monsterCombatGraphic.Tooltip);
                monsterCombatGraphics.Remove(monsterCombatGraphic);
            }
        }

        public BattleAnimation GetMonsterBattleAnimation(Monster monster) => monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster)?.Animation;

        public Tooltip GetMonsterBattleFieldTooltip(Monster monster) => monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster)?.Tooltip;

        public void ResetMonsterCombatSprite(Monster monster)
        {
            int frame = monster.GetAnimationFrameIndices(MonsterAnimationType.Move)[0];
            monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster)?.Animation?.Reset(frame);
        }

        public void ResetMonsterCombatSprites()
        {
            monsterCombatGraphics.ForEach(g =>
            {
                if (g != null)
                {
                    int frame = g.Monster.GetAnimationFrameIndices(MonsterAnimationType.Move)[0];
                    g.Animation?.Reset(frame);
                }
            });
        }

        public BattleAnimation UpdateMonsterCombatSprite(Monster monster, MonsterAnimationType animationType, uint animationTicks, uint totalTicks)
        {
            var monsterCombatGraphic = monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster);

            if (monsterCombatGraphic != null)
            {
                var animation = monsterCombatGraphic.Animation;

                if (animationTicks == 0) // new animation
                    animation.Play(monster.GetAnimationFrameIndices(animationType), Game.TicksPerSecond / 6, totalTicks);

                animation.Update(totalTicks);

                return animation;
            }

            return null;
        }

        public void MoveMonsterTo(uint column, uint row, Monster monster)
        {
            var monsterCombatGraphic = monsterCombatGraphics.FirstOrDefault(g => g.Monster == monster);

            if (monsterCombatGraphic != null)
            {
                // slot: 16x13
                // graphic: 16x14 (1 pixel higher than the slot)
                // x starts at 96, y at 134
                monsterCombatGraphic.BattleFieldSprite.X = Global.BattleFieldX + (int)column * Global.BattleFieldSlotWidth;
                monsterCombatGraphic.BattleFieldSprite.Y = Global.BattleFieldY + (int)row * Global.BattleFieldSlotHeight - 1;
                monsterCombatGraphic.Tooltip.Area = new Rect(monsterCombatGraphic.BattleFieldSprite.X, monsterCombatGraphic.BattleFieldSprite.Y,
                    monsterCombatGraphic.BattleFieldSprite.Width, monsterCombatGraphic.BattleFieldSprite.Height);
                monsterCombatGraphic.BattleFieldSprite.DisplayLayer = (byte)(3 + row);
            }
        }

        public void SetBattleFieldSlotColor(int column, int row, BattleFieldSlotColor slotColor, uint? blinkStartTime = null)
        {
            SetBattleFieldSlotColor(column + row * 6, slotColor, blinkStartTime);
        }

        public void SetBattleFieldSlotColor(int index, BattleFieldSlotColor slotColor, uint? blinkStartTime = null)
        {
            if (slotColor == BattleFieldSlotColor.None)
            {
                if (battleFieldSlotMarkers.ContainsKey(index))
                {
                    battleFieldSlotMarkers[index].Sprite?.Delete();
                    battleFieldSlotMarkers.Remove(index);
                }
            }
            else
            {
                uint textureIndex = Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.BattleFieldYellowBorder + (uint)slotColor % 3 - 1;

                if (!battleFieldSlotMarkers.ContainsKey(index))
                {
                    battleFieldSlotMarkers.Add(index, new BattleFieldSlotMarker
                    {
                        Sprite = AddSprite(Global.BattleFieldSlotArea(index), textureIndex, game.UIPaletteIndex, 2),
                        BlinkStartTicks = blinkStartTime,
                        ToggleColors = slotColor == BattleFieldSlotColor.Both
                    });
                }
                else
                {
                    battleFieldSlotMarkers[index].Sprite.TextureAtlasOffset = textureAtlas.GetOffset(textureIndex);
                    battleFieldSlotMarkers[index].BlinkStartTicks = blinkStartTime;
                    battleFieldSlotMarkers[index].ToggleColors = slotColor == BattleFieldSlotColor.Both;
                }
            }
        }

        public void ClearBattleFieldSlotColors()
        {
            foreach (var slotMarker in battleFieldSlotMarkers.Values)
                slotMarker.Sprite?.Delete();

            battleFieldSlotMarkers.Clear();
        }

        public void ClearBattleFieldSlotColorsExcept(int exceptionSlotIndex)
        {
            foreach (var slotMarker in battleFieldSlotMarkers.Where(s => s.Key != exceptionSlotIndex))
                slotMarker.Value.Sprite?.Delete();

            var exceptionSlot = battleFieldSlotMarkers?[exceptionSlotIndex];

            battleFieldSlotMarkers.Clear();

            if (exceptionSlot != null)
                battleFieldSlotMarkers.Add(exceptionSlotIndex, exceptionSlot);
        }

        public void SetBattleMessage(string message, TextColor textColor = TextColor.White)
        {
            if (message == null)
            {
                battleMessage?.Destroy();
                battleMessage = null;

                game.UpdateActiveBattleSpells();
            }
            else
            {
                var area = new Rect(5, 139, 84, 54);
                var glyphSize = new Size(Global.GlyphWidth, Global.GlyphLineHeight);
                var text = game.ProcessText(message);
                text = RenderView.TextProcessor.WrapText(text, area, glyphSize);

                if (battleMessage == null)
                {
                    battleMessage = AddScrollableText(area, text, textColor);
                }
                else
                {
                    battleMessage.SetText(text);
                    battleMessage.SetTextColor(textColor);
                }

                game.HideActiveBattleSpells();
            }
        }

        public List<BattleAnimation> CreateBattleEffectAnimations(int amount = 1)
        {
            if (battleEffectAnimations.Count != 0)
            {
                battleEffectAnimations.ForEach(a => a?.Destroy());
                battleEffectAnimations.Clear();
            }

            for (int i = 0; i < amount; ++i)
            {
                var sprite = AddSprite(new Rect(0, 0, 16, 16), Graphics.CombatGraphicOffset, 17, 0, null, null, Layer.BattleEffects, false);
                battleEffectAnimations.Add(new BattleAnimation(sprite));
            }

            return battleEffectAnimations;
        }
    }
}
