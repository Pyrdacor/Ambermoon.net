using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public void Fill(float percentage)
        {
            int pixels = Util.Round(size * percentage);

            if (pixels == 0)
                area.Visible = false;
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
                {
                    // Don't draw anything before started.
                    Color = Color.Transparent;
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

            Color = new Color
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
        public TextColor TextColor;
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
            /// </summary>
            public void Reset(Game game, Layout layout)
            {
                // Reset in case 1: Is only possible while in first player inventory.
                // Reset in case 2: Is also possible while in second player inventory.
                //                  First players inventory is opened in addition on reset.
                // Reset in case 3: Is only possible while in chest screen.
                bool updateGrid = true;
                ItemSlot updateSlot = Item.Item;

                if (SourcePlayer != null)
                {
                    var partyMember = game.GetPartyMember(SourcePlayer.Value);

                    if (Equipped == true)
                    {
                        game.EquipmentAdded(Item.Item.ItemIndex, Item.Item.Amount, partyMember);
                        game.UpdateCharacterInfo();
                        partyMember.Equipment.Slots[(EquipmentSlot)(SourceSlot + 1)].Add(Item.Item);
                        updateSlot = partyMember.Equipment.Slots[(EquipmentSlot)(SourceSlot + 1)];
                    }
                    else
                    {
                        game.InventoryItemAdded(Item.Item.ItemIndex, Item.Item.Amount, partyMember);
                        game.UpdateCharacterInfo();
                        partyMember.Inventory.Slots[SourceSlot].Add(Item.Item);
                        updateSlot = partyMember.Inventory.Slots[SourceSlot];
                    }

                    if (game.CurrentInventoryIndex != SourcePlayer)
                        updateGrid = false;
                    else
                    {
                        // Note: When switching to another inventory and back to the
                        // source inventory the current ItemGrid and the SourceGrid
                        // are two different instances even if they represent the
                        // same inventory. Therefore we have to update the SourceGrid.
                        if (SourceGrid != null)
                            SourceGrid = layout.itemGrids[0];
                    }
                }
                else if (game.OpenStorage != null)
                {
                    SourceGrid.DropItem(SourceSlot, this);
                }

                if (updateGrid && SourceGrid != null)
                    SourceGrid.SetItem(SourceSlot, updateSlot);

                Item.Destroy();
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
        DraggedItem draggedItem = null;
        uint draggedGold = 0;
        uint draggedFood = 0;
        public bool OptionMenuOpen { get; private set; } = false;
        public bool IsDragging => draggedItem != null || draggedGold != 0 || draggedFood != 0;
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
        IRenderText activeTooltip = null;
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
        public event Action<int, int> BattleFieldSlotClicked;

        public Layout(Game game, IRenderView renderView, IItemManager itemManager)
        {
            this.game = game;
            RenderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            renderLayer = renderView.GetLayer(Layer.UI);
            textLayer = renderView.GetLayer(Layer.Text);
            this.itemManager = itemManager;

            sprite = RenderView.SpriteFactory.Create(320, 163, true) as ILayerSprite;
            sprite.Layer = renderLayer;
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
                if (game.CursorType == CursorType.Sword &&
                    game.InputEnable)
                {
                    ButtonGridPage = 1 - ButtonGridPage;
                    SetLayout(Type, ticksPerMovement);
                }
            }
        }

        void AddStaticSprites()
        {
            var barBackgroundTexCoords = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.CharacterValueBarFrames));
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var barBackgroundSprite = portraitBarBackgrounds[i] = RenderView.SpriteFactory.Create(16, 36, true) as ILayerSprite;
                barBackgroundSprite.Layer = renderLayer;
                barBackgroundSprite.PaletteIndex = 49;
                barBackgroundSprite.TextureAtlasOffset = barBackgroundTexCoords;
                barBackgroundSprite.X = Global.PartyMemberPortraitAreas[i].Left + 33;
                barBackgroundSprite.Y = Global.PartyMemberPortraitAreas[i].Top;
                barBackgroundSprite.Visible = true;
            }

            // Left portrait border
            var sprite = RenderView.SpriteFactory.Create(16, 36, true);
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = 49;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.LeftPortraitBorder));
            sprite.X = 0;
            sprite.Y = 0;
            sprite.Visible = true;
            portraitBorders.Add(sprite);

            // Right portrait border
            sprite = RenderView.SpriteFactory.Create(16, 36, true);
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = 49;
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
                sprite.PaletteIndex = 49;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 0;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                sprite = RenderView.SpriteFactory.Create(32, 1, true);
                sprite.Layer = renderLayer;
                sprite.PaletteIndex = 49;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetCustomUIGraphicIndex(UICustomGraphic.PortraitBorder));
                sprite.X = 16 + i * 48;
                sprite.Y = 35;
                sprite.Visible = true;
                portraitBorders.Add(sprite);

                // LP shadow
                characterBars[i * 4 + 0] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 2, 19, 1, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.LPBarShadow), 1, FilledAreaType.CharacterBar), 16, false);
                // LP fill
                characterBars[i * 4 + 1] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 3, 19, 3, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.LPBar), 1, FilledAreaType.CharacterBar), 16, false);
                // SP shadow
                characterBars[i * 4 + 2] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 10, 19, 1, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.SPBarShadow), 1, FilledAreaType.CharacterBar), 16, false);
                // SP fill
                characterBars[i * 4 + 3] = new Bar(barAreas, CreateArea(new Rect((i + 1) * 48 + 11, 19, 3, 16),
                    game.GetPaletteColor(50, (int)NamedPaletteColors.SPBar), 1, FilledAreaType.CharacterBar), 16, false);
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
                sprite.Visible = true;
            }

            buttonGrid.Visible = layoutType != LayoutType.None && layoutType != LayoutType.Event;

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
            AddSprite(area, Graphics.GetCustomUIGraphicIndex(UICustomGraphic.MapDisableOverlay), 49, 1);
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            string versionString = $"Ambermoon.net V{version.Major}.{version.Minor}.{version.Build:00}^{game.DataNameProvider.DataVersionString}^{game.DataNameProvider.DataInfoString}";
            Rect boxArea;
            Rect textArea;
            if (Type == LayoutType.Battle)
            {
                boxArea = new Rect(88, 56, 144, 26);
                textArea = new Rect(88, 58, 144, 26);
            }
            else
            {
                boxArea = new Rect(32, 82, 144, 26);
                textArea = new Rect(32, 84, 144, 26);
            }
            AddSprite(boxArea, Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BiggerInfoBox), 49, 2);
            AddText(textArea, versionString, TextColor.White, TextAlign.Center, 3);

            buttonGrid.SetButton(0, ButtonType.Quit, false, game.Quit, false); // TODO: ask to really quit etc
            buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(2, ButtonType.Exit, false, CloseOptionMenu, false);
            buttonGrid.SetButton(3, ButtonType.Opt, true, null, false); // TODO: options
            buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
            buttonGrid.SetButton(6, ButtonType.Save, true, null, false); // TODO: save
            buttonGrid.SetButton(7, ButtonType.Load, false, OpenLoadMenu, false);
            buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
        }

        void CloseOptionMenu()
        {
            OptionMenuOpen = false;
            additionalSprites.Last()?.Delete();
            additionalSprites.Remove(additionalSprites.Last());
            additionalSprites.Last()?.Delete();
            additionalSprites.Remove(additionalSprites.Last());
            texts.Last()?.Destroy();
            texts.Remove(texts.Last());
            UpdateLayoutButtons(ticksPerMovement);
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
            activePopup = new Popup(game, RenderView, position, columns, rows, false, displayLayerOffset)
            {
                DisableButtons = disableButtons,
                CloseOnClick = closeOnClick
            };
            return activePopup;
        }

        internal Popup OpenTextPopup(IText text, Position position, int maxWidth, int maxTextHeight,
            bool disableButtons = true, bool closeOnClick = true, bool transparent = false,
            TextColor textColor = TextColor.Gray, Action closeAction = null, TextAlign textAlign = TextAlign.Left)
        {
            ClosePopup(false);
            var processedText = RenderView.TextProcessor.WrapText(text,
                new Rect(0, 0, maxWidth, int.MaxValue),
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            var textBounds = new Rect(position.X + (transparent ? 0 : 16), position.Y + (transparent ? 0 : 16),
                maxWidth, Math.Min(processedText.LineCount * Global.GlyphLineHeight, maxTextHeight));
            int popupRows = Math.Max(4, transparent ? maxTextHeight / Global.GlyphLineHeight : 2 + (textBounds.Height + 15) / 16);
            if (!transparent)
                textBounds.Position.Y += ((popupRows - 2) * 16 - textBounds.Height) / 2;
            activePopup = new Popup(game, RenderView, position, transparent ? maxWidth / Global.GlyphWidth : 18, popupRows, transparent)
            {
                DisableButtons = disableButtons,
                CloseOnClick = closeOnClick
            };
            bool scrolling = textBounds.Height / Global.GlyphLineHeight < processedText.LineCount;
            activePopup.AddText(textBounds, text, textColor, textAlign, true, 1, scrolling);
            if (closeAction != null)
                activePopup.Closed += closeAction;
            return activePopup;
        }

        internal Popup OpenTextPopup(IText text, Action closeAction, bool disableButtons = false,
            bool closeOnClick = true, bool transparent = false, TextAlign textAlign = TextAlign.Left)
        {
            const int maxTextWidth = 256;
            const int maxTextHeight = 112;
            var popup = OpenTextPopup(text, new Position(16, 53), maxTextWidth, maxTextHeight, disableButtons,
                closeOnClick, transparent, TextColor.Gray, closeAction, textAlign);
            return popup;
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

        internal Popup OpenYesNoPopup(IText text, Action yesAction, Action noAction, Action closeAction, int minLines = 3)
        {
            ClosePopup(false);
            const int maxTextWidth = 192;
            var processedText = RenderView.TextProcessor.WrapText(text,
                new Rect(48, 0, maxTextWidth, int.MaxValue),
                new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            var textBounds = new Rect(48, 95, maxTextWidth, Math.Max(minLines + 1, processedText.LineCount) * Global.GlyphLineHeight);
            var renderText = RenderView.RenderTextFactory.Create(textLayer,
                processedText, TextColor.Gray, true, textBounds);
            int popupRows = Math.Max(minLines + 2, 2 + (textBounds.Height + 31) / 16);
            activePopup = new Popup(game, RenderView, new Position(32, 74), 14, popupRows, false)
            {
                DisableButtons = true,
                CloseOnClick = false
            };
            activePopup.AddText(renderText);
            activePopup.Closed += closeAction;

            var yesButton = activePopup.AddButton(new Position(111, 41 + popupRows * 16));
            var noButton = activePopup.AddButton(new Position(143, 41 + popupRows * 16));

            yesButton.DisplayLayer = 200;
            noButton.DisplayLayer = 210;

            yesButton.ButtonType = ButtonType.Yes;
            noButton.ButtonType = ButtonType.No;

            yesButton.Action = yesAction;
            noButton.Action = noAction;

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

        void OpenLoadMenu()
        {
            var savegameNames = game.SavegameManager.GetSavegameNames(RenderView.GameData, out int current);
            OpenPopup(new Position(16, 62), 18, 7, true, false);
            activePopup.AddText(new Rect(24, 78, 272, 6), game.DataNameProvider.LoadWhichSavegameString, TextColor.Gray, TextAlign.Center);
            activePopup.AddSavegameListBox(savegameNames.Select(name =>
                new KeyValuePair<string, Action<int, string>>(name, (int slot, string name) => game.LoadGame(slot + 1))
            ).ToList());
        }

        public void AttachEventToButton(int index, Action action)
        {
            buttonGrid.SetButtonAction(index, action);
        }

        internal void UpdateLayoutButtons(uint? ticksPerMovement = null)
        {
            switch (Type)
            {
                case LayoutType.Map2D:
                    if (ButtonGridPage == 0)
                    {
                        var moveDelay = ticksPerMovement.Value;
                        buttonGrid.SetButton(0, ButtonType.MoveUpLeft, false, () => game.Move(CursorType.ArrowUpLeft), true, null, moveDelay);
                        buttonGrid.SetButton(1, ButtonType.MoveUp, false, () => game.Move(CursorType.ArrowUp), true, null, moveDelay);
                        buttonGrid.SetButton(2, ButtonType.MoveUpRight, false, () => game.Move(CursorType.ArrowUpRight), true, null, moveDelay);
                        buttonGrid.SetButton(3, ButtonType.MoveLeft, false, () => game.Move(CursorType.ArrowLeft), true, null, moveDelay);
                        buttonGrid.SetButton(4, ButtonType.Wait, true, null, true); // TODO: wait
                        buttonGrid.SetButton(5, ButtonType.MoveRight, false, () => game.Move(CursorType.ArrowRight), true, null, moveDelay);
                        buttonGrid.SetButton(6, ButtonType.MoveDownLeft, false, () => game.Move(CursorType.ArrowDownLeft), true, null, moveDelay);
                        buttonGrid.SetButton(7, ButtonType.MoveDown, false, () => game.Move(CursorType.ArrowDown), true, null, moveDelay);
                        buttonGrid.SetButton(8, ButtonType.MoveDownRight, false, () => game.Move(CursorType.ArrowDownRight), true, null, moveDelay);
                    }
                    else
                    {
                        buttonGrid.SetButton(0, ButtonType.Eye, false, null, false, () => CursorType.Eye);
                        buttonGrid.SetButton(1, ButtonType.Hand, false, null, false, () => CursorType.Hand);
                        buttonGrid.SetButton(2, ButtonType.Mouth, false, null, false, () => CursorType.Mouth);
                        buttonGrid.SetButton(3, ButtonType.Transport, !TransportEnabled, game.ToggleTransport, false);
                        buttonGrid.SetButton(4, ButtonType.Spells, true, null, false); // TODO: spells
                        buttonGrid.SetButton(5, ButtonType.Camp, true, null, false); // TODO: camp
                        buttonGrid.SetButton(6, ButtonType.Map, true, null, false); // TODO: map
                        buttonGrid.SetButton(7, ButtonType.BattlePositions, false, game.ShowBattlePositionWindow, false);
                        buttonGrid.SetButton(8, ButtonType.Options, false, OpenOptionMenu, false);
                    }
                    break;
                case LayoutType.Map3D:
                    if (ButtonGridPage == 0)
                    {
                        var moveDelay = ticksPerMovement.Value;
                        buttonGrid.SetButton(0, ButtonType.TurnLeft, false, () => game.Move(CursorType.ArrowTurnLeft, true), true, null, moveDelay);
                        buttonGrid.SetButton(1, ButtonType.MoveForward, false, () => game.Move(CursorType.ArrowForward, true), true, null, moveDelay);
                        buttonGrid.SetButton(2, ButtonType.TurnRight, false, () => game.Move(CursorType.ArrowTurnRight, true), true, null, moveDelay);
                        buttonGrid.SetButton(3, ButtonType.StrafeLeft, false, () => game.Move(CursorType.ArrowStrafeLeft, true), true, null, moveDelay);
                        buttonGrid.SetButton(4, ButtonType.Wait, true, null, true); // TODO: wait
                        buttonGrid.SetButton(5, ButtonType.StrafeRight, false, () => game.Move(CursorType.ArrowStrafeRight, true), true, null, moveDelay);
                        buttonGrid.SetButton(6, ButtonType.RotateLeft, false, () => game.Move(CursorType.ArrowRotateLeft, true), false, null, null);
                        buttonGrid.SetButton(7, ButtonType.MoveBackward, false, () => game.Move(CursorType.ArrowBackward, true), true, null, moveDelay);
                        buttonGrid.SetButton(8, ButtonType.RotateRight, false, () => game.Move(CursorType.ArrowRotateRight, true), false, null, null);
                    }
                    else
                    {
                        buttonGrid.SetButton(0, ButtonType.Eye, false, () => game.TriggerMapEvents(EventTrigger.Eye), true);
                        buttonGrid.SetButton(1, ButtonType.Hand, false, () => game.TriggerMapEvents(EventTrigger.Hand), true);
                        buttonGrid.SetButton(2, ButtonType.Mouth, false, () => game.TriggerMapEvents(EventTrigger.Mouth), true);
                        buttonGrid.SetButton(3, ButtonType.Transport, true, null, false); // Never enabled or usable in 3D maps
                        buttonGrid.SetButton(4, ButtonType.Spells, true, null, false); // TODO: spells
                        buttonGrid.SetButton(5, ButtonType.Camp, true, null, false); // TODO: camp
                        buttonGrid.SetButton(6, ButtonType.Map, true, null, false); // TODO: map
                        buttonGrid.SetButton(7, ButtonType.BattlePositions, false, game.ShowBattlePositionWindow, false);
                        buttonGrid.SetButton(8, ButtonType.Options, false, OpenOptionMenu, false);
                    }
                    break;
                case LayoutType.Inventory:
                    buttonGrid.SetButton(0, ButtonType.Stats, false, () => game.OpenPartyMember(game.CurrentInventoryIndex.Value, false), false);
                    buttonGrid.SetButton(1, ButtonType.UseItem, false, () => PickInventoryItemForAction(UseItem,
                        false, game.DataNameProvider.WhichItemToUseMessage), true);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false);
                    if (game.OpenStorage?.AllowsItemDrop == true)
                    {
                        buttonGrid.SetButton(3, ButtonType.StoreItem, false, () => PickInventoryItemForAction(StoreItem,
                            false, game.DataNameProvider.WhichItemToStoreMessage), false);
                        buttonGrid.SetButton(4, ButtonType.StoreGold, game.CurrentInventory?.Gold == 0, StoreGold, false);
                        buttonGrid.SetButton(5, ButtonType.StoreFood, game.CurrentInventory?.Food == 0, StoreFood, false);
                    }
                    else
                    {
                        buttonGrid.SetButton(3, ButtonType.DropItem, false, () => PickInventoryItemForAction(DropItem,
                            false, game.DataNameProvider.WhichItemToDropMessage), false);
                        buttonGrid.SetButton(4, ButtonType.DropGold, game.CurrentInventory?.Gold == 0, DropGold, false);
                        buttonGrid.SetButton(5, ButtonType.DropFood, game.CurrentInventory?.Food == 0, DropFood, false);
                    }
                    buttonGrid.SetButton(6, ButtonType.ViewItem, true, null, false); // TODO: view item
                    buttonGrid.SetButton(7, ButtonType.GiveGold, game.CurrentInventory?.Gold == 0, () => GiveGold(null), false);
                    buttonGrid.SetButton(8, ButtonType.GiveFood, game.CurrentInventory?.Food == 0, () => GiveFood(null), false);
                    break;
                case LayoutType.Stats:
                    buttonGrid.SetButton(0, ButtonType.Inventory, false, () => game.OpenPartyMember(game.CurrentInventoryIndex.Value, true), false);
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false);
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                    break;
                case LayoutType.Items:
                {
                    // TODO: this is only for open chests now
                    if (game.OpenStorage is Chest chest)
                    {
                        void CloseChest()
                        {
                            if (chest.IsBattleLoot)
                            {
                                if (chest.HasAnyImportantItem(itemManager))
                                {
                                    ShowClickChestMessage(game.DataNameProvider.DontForgetItems +
                                        string.Join(", ", chest.GetImportantItemNames(itemManager)) + ".");
                                    return;
                                }
                            }
                            game.CloseWindow();
                        }
                        buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(2, ButtonType.Exit, false, CloseChest, false);
                        buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                        buttonGrid.SetButton(4, ButtonType.DistributeGold, chest.Gold == 0, () => DistributeGold(chest), false);
                        buttonGrid.SetButton(5, ButtonType.DistributeFood, chest.Food == 0, () => DistributeFood(chest), false);
                        buttonGrid.SetButton(6, ButtonType.ViewItem, true, null, false); // TODO: view item
                        buttonGrid.SetButton(7, ButtonType.GiveGold, chest.Gold == 0, () => GiveGold(chest), false);
                        buttonGrid.SetButton(8, ButtonType.GiveFood, chest.Food == 0, () => GiveFood(chest), false);
                    }
                    break;
                }
                case LayoutType.Riddlemouth:
                    buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false);
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Mouth, false, null, false); // this is set later manually
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Ear, false, null, false); // this is set later manually
                    break;
                case LayoutType.Conversation:
                    buttonGrid.SetButton(0, ButtonType.Mouth, false, null, false); // this is set later manually
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false);
                    buttonGrid.SetButton(3, ButtonType.ViewItem, true, null, false); // TODO
                    buttonGrid.SetButton(4, ButtonType.AskToJoin, true, null, false); // TODO
                    buttonGrid.SetButton(5, ButtonType.AskToLeave, true, null, false); // TODO
                    buttonGrid.SetButton(6, ButtonType.GiveItem, true, null, false); // TODO
                    buttonGrid.SetButton(7, ButtonType.GiveGoldToNPC, true, null, false); // TODO
                    buttonGrid.SetButton(8, ButtonType.GiveFoodToNPC, true, null, false); // TODO
                    break;
                case LayoutType.Battle:
                    buttonGrid.SetButton(0, ButtonType.Flee, false, null, false); // this is set later manually
                    buttonGrid.SetButton(1, ButtonType.Options, false, OpenOptionMenu, false);
                    buttonGrid.SetButton(2, ButtonType.Ok, false, null, false); // this is set later manually
                    buttonGrid.SetButton(3, ButtonType.BattlePositions, true, null, false); // this is set later manually
                    buttonGrid.SetButton(4, ButtonType.MoveForward, true, null, false); // this is set later manually
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Attack, true, null, false); // this is set later manually
                    buttonGrid.SetButton(7, ButtonType.Defend, true, null, false); // this is set later manually
                    buttonGrid.SetButton(8, ButtonType.Spells, true, null, false); // this is set later manually
                    break;
                case LayoutType.BattlePositions:
                    buttonGrid.SetButton(0, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(1, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(2, ButtonType.Exit, false, game.CloseWindow, false);
                    buttonGrid.SetButton(3, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(4, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(5, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(6, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(7, ButtonType.Empty, false, null, false);
                    buttonGrid.SetButton(8, ButtonType.Empty, false, null, false);
                    break;
                    // TODO
            }
        }

        public void AddSunkenBox(Rect area, byte displayLayer = 1)
        {
            // TODO: use named palette colors
            var darkBorderColor = game.GetPaletteColor(50, 26);
            var brightBorderColor = game.GetPaletteColor(50, 31);
            var fillColor = game.GetPaletteColor(50, 27);

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

        Popup OpenAmountInputBox(string message, uint imageIndex, string name, uint maxAmount,
            Action<uint> submitAction)
        {
            ClosePopup(false);
            activePopup = new Popup(game, RenderView, new Position(64, 64), 11, 6, false)
            {
                DisableButtons = true,
                CloseOnClick = false
            };
            // Item display (also gold or food)
            var itemArea = new Rect(79, 79, 18, 18);
            activePopup.AddSunkenBox(itemArea);
            activePopup.AddItemImage(itemArea.CreateModified(1, 1, -2, -2), imageIndex);
            // Item name display (also gold or food)
            var itemNameArea = new Rect(99, 82, 125, 10);
            activePopup.AddSunkenBox(itemNameArea);
            activePopup.AddText(itemNameArea.CreateModified(1, 2, -1, -3), name, TextColor.Red, TextAlign.Center);
            // Message display
            var messageArea = new Rect(79, 98, 145, 10);
            activePopup.AddSunkenBox(messageArea);
            activePopup.AddText(messageArea.CreateModified(1, 2, -1, -3), message, TextColor.Orange, TextAlign.Center);
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
            increaseButton.Action = () => ChangeInputValue(1);
            decreaseButton.Action = () => ChangeInputValue(-1);
            // OK button
            var okButton = activePopup.AddButton(new Position(192, 127));
            okButton.ButtonType = ButtonType.Ok;
            okButton.DisplayLayer = 200;
            okButton.Action = () => submitAction?.Invoke(input.Value);

            void ChangeInputValue(int changeAmount)
            {
                if (changeAmount < 0)
                {
                    input.Text = Math.Max(0, (int)input.Value + changeAmount).ToString();
                }
                else if (changeAmount > 0)
                {
                    input.Text = Math.Min(maxAmount, input.Value + (uint)changeAmount).ToString();
                }
            }

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

        void UseItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            if (itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
            {
                SetInventoryMessage(game.DataNameProvider.CannotUseBrokenItems, true);
                return;
            }

            // TODO: cannot use it here, wrong place, wrong world
            var user = game.CurrentInventory;
            var item = itemManager.GetItem(itemSlot.ItemIndex);

            if (!item.IsUsable)
            {
                // TODO: correct message?
                SetInventoryMessage(game.DataNameProvider.ItemHasNoEffectHere, true);
                return;
            }

            // TODO: check available charges

            if (!item.Classes.Contains(user.Class))
            {
                SetInventoryMessage(game.DataNameProvider.WrongClassToUseItem, true);
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

                    // Note: itemGrids[0] is inventory and itemGrids[1] is equipment
                    bool equipped = itemGrid == itemGrids[1];
                    var caster = game.CurrentInventory;
                    game.CloseWindow(() => game.PickBattleSpell(item.Spell, (uint)slot, equipped, caster));
                }
            }
            else if (game.TestUseItemMapEvent(itemSlot.ItemIndex))
            {
                ReduceItemCharge(itemSlot);
                game.CloseWindow(() => game.TriggerMapEvents((EventTrigger)((uint)EventTrigger.Item0 + itemSlot.ItemIndex)));
            }
            else
            {
                // do other things
                // TODO: item has no effect here (only if it does nothing else, e.g. torches can be used on spider webs and without them)
            }
        }

        internal void ReduceItemCharge(ItemSlot itemSlot, bool slotVisible = true)
        {
            itemSlot.NumRemainingCharges = Math.Max(0, itemSlot.NumRemainingCharges - 1);

            if (itemSlot.NumRemainingCharges == 0)
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);

                if (item.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
                {
                    itemSlot.Remove(1);

                    if (slotVisible)
                    {
                        // TODO: play destroy animation
                    }
                }
            }
        }

        void DistributeGold(Chest chest)
        {
            var partyMembers = game.PartyMembers.ToList();
            var initialGold = chest.Gold;

            while (chest.Gold != 0)
            {
                int numTargetPlayers = partyMembers.Count;
                uint goldPerPlayer = chest.Gold / (uint)numTargetPlayers;
                bool anyCouldTake = false;

                if (goldPerPlayer == 0)
                {
                    numTargetPlayers = (int)chest.Gold;
                    goldPerPlayer = 1;
                }

                foreach (var partyMember in partyMembers)
                {
                    uint goldToTake = Math.Min(partyMember.MaxGoldToTake, goldPerPlayer);
                    chest.Gold -= goldToTake;
                    partyMember.AddGold(goldToTake);

                    if (goldToTake != 0)
                    {
                        anyCouldTake = true;

                        if (--numTargetPlayers == 0)
                            break;
                    }
                }

                if (!anyCouldTake)
                    return;
            }

            if (chest.Gold != initialGold)
            {
                game.ChestGoldChanged();
                UpdateLayoutButtons();
            }
        }

        void DistributeFood(Chest chest)
        {
            var partyMembers = game.PartyMembers.ToList();
            var initialFood = chest.Food;

            while (chest.Food != 0)
            {
                int numTargetPlayers = partyMembers.Count;
                uint foodPerPlayer = chest.Food / (uint)numTargetPlayers;
                bool anyCouldTake = false;

                if (foodPerPlayer == 0)
                {
                    numTargetPlayers = (int)chest.Food;
                    foodPerPlayer = 1;
                }

                foreach (var partyMember in partyMembers)
                {
                    uint foodToTake = Math.Min(partyMember.MaxFoodToTake, foodPerPlayer);
                    chest.Food -= foodToTake;
                    partyMember.Food += (ushort)foodToTake;
                    partyMember.TotalWeight += foodToTake * 250;

                    if (foodToTake != 0)
                    {
                        anyCouldTake = true;

                        if (--numTargetPlayers == 0)
                            break;
                    }
                }

                if (!anyCouldTake)
                    return;
            }

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
                draggedGold = amount;
                game.CursorType = CursorType.Gold;
                game.TrapMouse(Global.PartyMemberPortraitArea);
                draggedGoldOrFoodRemover = chest == null
                    ? (Action<uint>)(gold => { game.CurrentInventory.RemoveGold(gold); game.UpdateCharacterInfo(); UpdateLayoutButtons(); game.UntrapMouse(); SetInventoryMessage(null); })
                    : gold => { chest.Gold -= gold; game.ChestGoldChanged(); UpdateLayoutButtons(); game.UntrapMouse(); game.HideMessage(); };
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
                            partyMember.MaxGoldToTake >= amount && !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                    }
                }
            }
        }

        void GiveFood(Chest chest)
        {
            // Note: 109 is the object icon index for food.
            OpenAmountInputBox(game.DataNameProvider.GiveHowMuchFoodMessage,
                109, game.DataNameProvider.FoodName, chest == null ? game.CurrentInventory.Food : chest.Food,
                GiveAmount);

            void GiveAmount(uint amount)
            {
                ClosePopup();
                CancelDrag();
                draggedFood = amount;
                game.CursorType = CursorType.Food;
                game.TrapMouse(Global.PartyMemberPortraitArea);
                draggedGoldOrFoodRemover = chest == null
                    ? (Action<uint>)(food => { game.CurrentInventory.RemoveFood(food); game.UpdateCharacterInfo(); UpdateLayoutButtons(); game.UntrapMouse(); SetInventoryMessage(null); })
                    : food => { chest.Food -= food; game.ChestFoodChanged(); UpdateLayoutButtons(); game.UntrapMouse(); game.HideMessage(); };
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
                            partyMember.MaxFoodToTake >= amount && !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                    }
                }
            }
        }

        internal void ShowChestMessage(string message, TextAlign textAlign = TextAlign.Center)
        {
            var bounds = new Rect(114, 46, 189, 48);
            game.ChestText?.Destroy();
            game.ChestText = AddText(bounds, game.ProcessText(message, bounds), TextColor.White, textAlign);
        }

        internal void ShowClickChestMessage(string message, Action clickEvent = null, bool remainAfterClick = true)
        {
            var bounds = new Rect(114, 46, 189, 48);
            game.ChestText?.Destroy();
            game.ChestText = AddScrollableText(bounds, game.ProcessText(message, bounds));
            game.ChestText.Clicked += scrolledToEnd =>
            {
                if (scrolledToEnd)
                {
                    if (remainAfterClick)
                    {
                        game.ChestText.WithScrolling = false;
                    }
                    else
                    {
                        game.ChestText?.Destroy();
                        game.ChestText = null;
                    }
                    game.InputEnable = true;
                    game.CursorType = CursorType.Sword;
                    clickEvent?.Invoke();
                }
            };
            game.CursorType = CursorType.Click;
            game.InputEnable = false;
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

        void DropItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            if (itemSlot.Amount > 1)
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);
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
                    game.InventoryItemRemoved(itemSlot.ItemIndex, (int)amount);
                    if (amount >= itemSlot.Amount)
                        itemSlot.Clear();
                    else
                        itemSlot.Amount -= (int)amount;
                    itemGrid.SetItem(slot, itemSlot); // update appearance
                    game.UpdateCharacterInfo();
                }

                Ask(game.DataNameProvider.DropItemQuestion, DropIt);
            }
        }

        void StoreGold()
        {
            // Note: 96 is the object icon index for coins (gold).
            OpenAmountInputBox(game.DataNameProvider.StoreHowMuchGoldMessage,
                96, game.DataNameProvider.GoldName, game.CurrentInventory.Gold,
                amount => game.StoreGold(amount));
        }

        void StoreFood()
        {
            // Note: 109 is the object icon index for food.
            OpenAmountInputBox(game.DataNameProvider.StoreHowMuchFoodMessage,
                109, game.DataNameProvider.FoodName, game.CurrentInventory.Food,
                amount => game.StoreFood(amount));
        }

        void StoreItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
        {
            if (itemSlot.Amount > 1)
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);
                OpenAmountInputBox(game.DataNameProvider.StoreHowMuchItemsMessage,
                    item.GraphicIndex, item.Name, (uint)itemSlot.Amount, StoreAmount);
            }
            else
            {
                StoreAmount(1);
            }

            void StoreAmount(uint amount)
            {
                ClosePopup(false);

                // TODO: animation where the item flies to the right of the screen
                if (game.StoreItem(itemSlot, amount))
                {
                    game.InventoryItemRemoved(itemSlot.ItemIndex, (int)amount);
                    itemGrid.SetItem(slot, itemSlot); // update appearance
                    game.UpdateCharacterInfo();
                }
            }
        }

        internal bool InventoryMessageWaitsForClick => inventoryMessage != null && !game.InputEnable;

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
                    inventoryMessage?.Destroy();
                    game.CursorType = CursorType.Click;
                    inventoryMessage = AddScrollableText(new Rect(21, 51, 162, 14), game.ProcessText(message));
                    inventoryMessage.Clicked += scrolledToEnd =>
                    {
                        if (scrolledToEnd)
                        {
                            inventoryMessage?.Destroy();
                            inventoryMessage = null;
                            game.InputEnable = true;
                            game.CursorType = CursorType.Sword;
                        }
                    };
                    game.CursorType = CursorType.Click;
                    game.InputEnable = false;
                    game.AddTimedEvent(TimeSpan.FromMilliseconds(50), () => SetActiveTooltip(null, null));
                }
                else if (inventoryMessage == null)
                {
                    inventoryMessage = AddScrollableText(new Rect(21, 51, 162, 14), game.ProcessText(message));
                }
                else
                {
                    inventoryMessage.SetText(game.ProcessText(message));
                }
            }
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
                    itemAction?.Invoke(itemGrid, slot, itemSlot);
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

        public void Reset(bool keepInventoryMessage = false)
        {
            OptionMenuOpen = false;
            sprite80x80Picture?.Delete();
            sprite80x80Picture = null;
            eventPicture?.Delete();
            eventPicture = null;
            additionalSprites.ForEach(sprite => sprite?.Delete());
            additionalSprites.Clear();
            itemGrids.ForEach(grid => grid.Destroy());
            itemGrids.Clear();
            filledAreas.ForEach(area => area?.Delete());
            filledAreas.Clear();
            activePopup?.Destroy();
            activePopup = null;
            activeTooltip?.Delete();
            activeTooltip = null;
            tooltips.Clear();
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
            battleMessage?.Destroy();
            battleMessage = null;
            battleEffectAnimations.ForEach(a => a?.Destroy());
            battleEffectAnimations.Clear();
            activeSpellSprites.Clear(); // sprites are destroyed above
            activeSpellDurationBackgrounds.Values.ToList().ForEach(b => b?.Delete());
            activeSpellDurationBackgrounds.Clear();
            activeSpellDurationBars.Clear(); // areas are destroyed above
            specialItemSprites.Clear(); // sprites are destroyed above
            specialItemTexts.Clear(); // texts are destroyed above
            monsterCombatGraphics.ForEach(g => { g.Animation?.Destroy(); g.BattleFieldSprite?.Delete(); RemoveTooltip(g.Tooltip); });
            monsterCombatGraphics.Clear();

            // Note: Don't remove fadeEffects or bars here.
        }

        public void SetActiveCharacter(int slot, List<PartyMember> partyMembers)
        {
            for (int i = 0; i < portraitNames.Length; ++i)
            {
                if (portraitNames[i] != null)
                {
                    if (i == slot)
                        portraitNames[i].TextColor = TextColor.Yellow;
                    else if (!partyMembers[i].Alive || !partyMembers[i].Ailments.CanSelect())
                        portraitNames[i].TextColor = TextColor.PaleGray;
                    else if (game.HasPartyMemberFled(partyMembers[i]))
                        portraitNames[i].TextColor = TextColor.PaleGray;
                    else
                        portraitNames[i].TextColor = TextColor.Red;
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
                    if (!partyMembers[i].Alive || !partyMembers[i].Ailments.CanSelect())
                        portraitNames[i].TextColor = TextColor.PaleGray;
                    else if (game.HasPartyMemberFled(partyMembers[i]))
                        portraitNames[i].TextColor = TextColor.PaleGray;
                    else
                        portraitNames[i].TextColor = activeSlot == i ? TextColor.Yellow : TextColor.Red;
                }
            }
        }

        void PlayPortraitAnimation(int slot, PartyMember partyMember)
        {
            var newState = partyMember == null ? PartyMemberPortaitState.Empty
                : partyMember.Alive ? PartyMemberPortaitState.Normal : PartyMemberPortaitState.Dead;

            if (portraitStates[slot] == newState)
                return;

            bool animation = portraitStates[slot] != PartyMemberPortaitState.None;

            portraitStates[slot] = newState;
            uint newGraphicIndex = newState switch
            {
                PartyMemberPortaitState.Empty => Graphics.GetUIGraphicIndex(UIGraphic.EmptyCharacterSlot),
                PartyMemberPortaitState.Dead => Graphics.GetUIGraphicIndex(UIGraphic.Skull),
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
                overlaySprite.PaletteIndex = 49;
                overlaySprite.Visible = true;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(newGraphicIndex);
                sprite.Y = Global.PartyMemberPortraitAreas[slot].Top + 1 + yOffset;

                portraitAnimation = new PortraitAnimation
                {
                    StartTicks = game.BattleActive ? game.CurrentBattleTicks : game.CurrentTicks,
                    Offset = yOffset,
                    PrimarySprite = sprite,
                    SecondarySprite = overlaySprite
                };
            }
            else
            {
                portraits[slot].TextureAtlasOffset = textureAtlas.GetOffset(newGraphicIndex);
            }
        }

        /// <summary>
        /// Set portait to 0 to remove the portrait.
        /// </summary>
        public void SetCharacter(int slot, PartyMember partyMember, bool initialize = false)
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
                PlayPortraitAnimation(slot, partyMember);
            }
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
                sprite.PaletteIndex = 51;
                sprite.Visible = true;

                var text = portraitNames[slot];
                var name = RenderView.TextProcessor.CreateText(partyMember.Name.Substring(0, Math.Min(5, partyMember.Name.Length)));

                if (text == null)
                {
                    text = portraitNames[slot] = RenderView.RenderTextFactory.Create(textLayer,  name, TextColor.Red, true,
                        new Rect(Global.PartyMemberPortraitAreas[slot].Left + 2, Global.PartyMemberPortraitAreas[slot].Top + 31, 30, 6), TextAlign.Center);
                }
                else
                {
                    text.Text = name;
                }
                text.DisplayLayer = 3;
                text.TextColor = partyMember.Alive ? TextColor.Red : TextColor.PaleGray;
                text.Visible = true;
                UpdateCharacterStatus(partyMember);
            }

            FillCharacterBars(slot, partyMember);
        }

        internal void UpdateCharacterStatus(int slot, UIGraphic? graphicIndex = null)
        {
            var sprite = characterStatusIcons[slot] ??= RenderView.SpriteFactory.Create(16, 16, true, 3) as ILayerSprite;
            sprite.Layer = renderLayer;
            sprite.PaletteIndex = 49;
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

            if (partyMember.Ailments != Ailment.None)
            {
                var ailments = partyMember.VisibleAilments;
                uint ailmentCount = (uint)ailments.Count;

                if (ailmentCount == 1)
                {
                    UpdateCharacterStatus(slot, Graphics.GetAilmentGraphic(ailments[0]));
                }
                else
                {
                    uint ticksPerAilment = Game.TicksPerSecond * 2;
                    int index = (int)((game.CurrentTicks % (ailmentCount * ticksPerAilment)) / ticksPerAilment);
                    UpdateCharacterStatus(slot, Graphics.GetAilmentGraphic(ailments[index]));
                }
            }
            else
            {
                UpdateCharacterStatus(slot, null);
            }
        }

        public void FillCharacterBars(int slot, PartyMember partyMember)
        {
            float lpPercentage = partyMember == null ? 0.0f : Math.Min(1.0f, (float)partyMember.HitPoints.TotalCurrentValue / partyMember.HitPoints.MaxValue);
            float spPercentage = partyMember == null ? 0.0f : Math.Min(1.0f, (float)partyMember.SpellPoints.TotalCurrentValue / partyMember.SpellPoints.MaxValue);

            characterBars[slot * 4 + 0].Fill(lpPercentage);
            characterBars[slot * 4 + 1].Fill(lpPercentage);
            characterBars[slot * 4 + 2].Fill(spPercentage);
            characterBars[slot * 4 + 3].Fill(spPercentage);
        }

        public void AddActiveSpell(ActiveSpellType activeSpellType, ActiveSpell activeSpell, bool battle)
        {
            if (activeSpellSprites.ContainsKey(activeSpellType))
                return;

            var baseLocation = battle ? new Position(0, 170) : new Position(208, 106);
            int index = (int)activeSpellType;
            uint graphicIndex = Graphics.GetUIGraphicIndex(UIGraphic.Candle + index);
            activeSpellSprites.Add(activeSpellType, AddSprite(new Rect(baseLocation.X + index * 16, baseLocation.Y, 16, 16), graphicIndex, 49));

            activeSpellDurationBackgrounds.Add(activeSpellType, CreateArea(new Rect(baseLocation.X + 1 + index * 16, baseLocation.Y + 17, 14, 4),
                game.GetPaletteColor(50, 26), 2));
            var durationBar = new Bar(filledAreas,
                CreateArea(new Rect(baseLocation.X + 2 + index * 16, baseLocation.Y + 18, 12, 2), game.GetPaletteColor(50, 31), 3), 12, true);
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
                    activeSpellDurationBars[activeSpellType].Fill(activeSpell.Duration / 200.0f);
                }
            }
        }

        string GetCompassString()
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
                        Graphics.GetUIGraphicIndex(UIGraphic.Compass), 49, 3)); // Note: The display layer must be greater than the windchain layer
                    var text = AddText(new Rect(203, 86, 42, 7),
                        GetCompassString(), TextColor.Gray);
                    specialItemTexts.Add(SpecialItemPurpose.Compass, text);
                    text.Clip(new Rect(208, 86, 32, 7));
                    break;
                }
            case SpecialItemPurpose.MonsterEye:
                specialItemSprites.Add(specialItem, AddSprite(new Rect(240, 49, 32, 32),
                    Graphics.GetUIGraphicIndex(game.MonsterSeesPlayer ? UIGraphic.MonsterEyeActive : UIGraphic.MonsterEyeInactive), 49));
                break;
            case SpecialItemPurpose.DayTime:
                specialItemSprites.Add(specialItem, AddSprite(new Rect(272, 73, 32, 32),
                    Graphics.GetUIGraphicIndex(UIGraphic.Night + (int)game.GameTime.GetDayTime()), 49));
                break;
            case SpecialItemPurpose.WindChain:
                specialItemSprites.Add(specialItem, AddSprite(new Rect(240, 89, 32, 15),
                    Graphics.GetUIGraphicIndex(UIGraphic.Windchain), 49));
                break;
            case SpecialItemPurpose.MapLocation:
                specialItemTexts.Add(SpecialItemPurpose.MapLocation, AddText(new Rect(210, 50, 30, 14),
                    $"X:{game.PartyPosition.X + 1,3}^Y:{game.PartyPosition.Y + 1,3}", TextColor.Gray));
                break;
            case SpecialItemPurpose.Clock:
                specialItemTexts.Add(SpecialItemPurpose.Clock, AddText(new Rect(273, 54, 30, 7),
                    $"{game.GameTime.Hour,2}:{game.GameTime.Minute:00}", TextColor.Gray));
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

        Tooltip AddTooltip(Rect rect, string tooltip, TextColor tooltipTextColor)
        {
            var toolTip = new Tooltip
            {
                Area = rect,
                Text = tooltip,
                TextColor = tooltipTextColor
            };
            tooltips.Add(toolTip);
            return toolTip;
        }

        internal void RemoveTooltip(Tooltip tooltip)
        {
            tooltips.Remove(tooltip);

            if (activeTooltip == tooltip)
                SetActiveTooltip(null, null);
        }

        void SetActiveTooltip(Position cursorPosition, Tooltip tooltip)
        {
            if (tooltip == null) // remove
            {
                if (activeTooltip != null)
                {
                    activeTooltip?.Delete();
                    activeTooltip = null;
                }
            }
            else
            {
                if (activeTooltip == null)
                {
                    activeTooltip = RenderView.RenderTextFactory.Create();
                    activeTooltip.Shadow = true;
                    activeTooltip.DisplayLayer = 250;
                    activeTooltip.Layer = RenderView.GetLayer(Layer.Text);
                    activeTooltip.Visible = true;
                }

                var text = RenderView.TextProcessor.CreateText(tooltip.Text);
                int textWidth = text.MaxLineSize * Global.GlyphWidth;

                activeTooltip.Text = text;
                activeTooltip.TextColor = tooltip.TextColor;
                int x = Util.Limit(0, cursorPosition.X - textWidth / 2, Global.VirtualScreenWidth - textWidth);
                int y = cursorPosition.Y - text.LineCount * Global.GlyphLineHeight - 1;
                activeTooltip.Place(new Rect(x, y, textWidth, text.LineCount * Global.GlyphLineHeight), TextAlign.Center);
            }
        }

        public UIText AddText(Rect rect, string text, TextColor color = TextColor.White, TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            return AddText(rect, RenderView.TextProcessor.CreateText(text), color, textAlign, displayLayer);
        }

        public UIText AddText(Rect rect, IText text, TextColor color = TextColor.White, TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            var uiText = new UIText(RenderView, text, rect, displayLayer, color, true, textAlign, false);
            texts.Add(uiText);
            return uiText;
        }

        public UIText AddScrollableText(Rect rect, IText text, TextColor color = TextColor.White, TextAlign textAlign = TextAlign.Left, byte displayLayer = 2)
        {
            var scrollableText = new UIText(RenderView, text, rect, displayLayer, color, true, textAlign, true);
            texts.Add(scrollableText);
            return scrollableText;
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
                var sprite = sprite80x80Picture ??= RenderView.SpriteFactory.Create(80, 80, true);
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.Pics80x80Offset + (uint)(picture - 1));
                sprite.X = Global.LayoutX + 16;
                sprite.Y = Global.LayoutY + 6;
                sprite.PaletteIndex = 49;
                sprite.Layer = renderLayer;
                sprite.Visible = true;
            }
        }

        public void AddEventPicture(uint index)
        {
            var sprite = eventPicture ??= RenderView.SpriteFactory.Create(320, 92, true, 10) as ILayerSprite;
            sprite.PaletteIndex = index switch
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
                draggedItem.Reset(game, this);
                DropItem();
            }

            if (draggedGold != 0 || draggedFood != 0)
            {
                draggedGold = 0;
                draggedFood = 0;
                draggedGoldOrFoodRemover = null;
                DropItem();
            }

            // Remove hand icons and set current status icons
            game.PartyMembers.ToList().ForEach(p => UpdateCharacterStatus(p));
        }

        void DropItem()
        {
            draggedItem = null;

            if (game.OpenStorage is Chest)
            {
                game.ChestText?.Destroy();
                game.ChestText = null;
            }
            else
                SetInventoryMessage(null);
        }

        bool IsInventory => Type == LayoutType.Inventory;
        bool HasScrollableItemGrid => IsInventory ||
            (Type == LayoutType.Items && itemGrids.Count != 0 && !itemGrids[0].Disabled);

        public void AddItemGrid(ItemGrid itemGrid)
        {
            itemGrids.Add(itemGrid);
        }

        internal IColoredRect CreateArea(Rect rect, Color color, byte displayLayer = 0, FilledAreaType type = FilledAreaType.Custom)
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

        public FilledArea FillArea(Rect rect, Color color, bool topMost)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, (byte)(topMost ? 245 : 0)));
        }

        public FilledArea FillArea(Rect rect, Color color, byte displayLayer)
        {
            return new FilledArea(filledAreas, CreateArea(rect, color, displayLayer));
        }

        public Panel AddPanel(Rect rect, byte displayLayer)
        {
            return new Panel(game, rect, filledAreas, this, displayLayer);
        }

        public void AddColorFader(Rect rect, Color startColor, Color endColor,
            int durationInMilliseconds, bool removeWhenFinished, DateTime? startTime = null)
        {
            var now = DateTime.Now;
            var startingTime = startTime ?? now;
            var initialColor = startingTime > now ? Color.Transparent : startColor;

            fadeEffects.Add(new FadeEffect(fadeEffectAreas, CreateArea(rect, initialColor, 255, FilledAreaType.FadeEffect), startColor,
                endColor, durationInMilliseconds, startingTime, removeWhenFinished));
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
                    var quarterDuration = durationInMilliseconds / 4;
                    var halfDuration = quarterDuration * 2;
                    AddColorFader(rect, new Color(color, 0), color, quarterDuration, true);
                    AddColorFader(rect, color, color, quarterDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(quarterDuration));
                    AddColorFader(rect, color, new Color(color, 0), halfDuration, true,
                        DateTime.Now + TimeSpan.FromMilliseconds(halfDuration));
                    break;
            }
        }

        public void Update(uint currentTicks)
        {
            buttonGrid.Update(currentTicks);
            activePopup?.Update(currentTicks);

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
                uint elapsed = (game.BattleActive ? game.CurrentBattleTicks : currentTicks) - portraitAnimation.StartTicks;

                if (elapsed > Game.TicksPerSecond)
                {
                    portraitAnimation.PrimarySprite.Y = 1;
                    portraitAnimation.SecondarySprite.Delete();
                    portraitAnimation = null;
                }
                else
                {
                    int diff;

                    if (portraitAnimation.Offset < 0)
                    {
                        portraitAnimation.Offset = Math.Min(0, -34 + (int)elapsed * 34 / (int)Game.TicksPerSecond);
                        diff = 34;
                    }
                    else
                    {
                        portraitAnimation.Offset = Math.Max(0, 34 - (int)elapsed * 34 / (int)Game.TicksPerSecond);
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
            if (!game.InputEnable)
                return false;

            if (PopupActive && activePopup.KeyChar(ch))
                return true;

            return false;
        }

        public void KeyDown(Key key, KeyModifiers keyModifiers)
        {
            if (!game.InputEnable)
                return;

            if (PopupActive && activePopup.KeyDown(key))
                return;

            switch (key)
            {
                case Key.Up:
                    if (HasScrollableItemGrid)
                        itemGrids[0].ScrollUp();
                    break;
                case Key.Down:
                    if (HasScrollableItemGrid)
                        itemGrids[0].ScrollDown();
                    break;
                case Key.PageUp:
                    if (HasScrollableItemGrid)
                        itemGrids[0].ScrollPageUp();
                    break;
                case Key.PageDown:
                    if (HasScrollableItemGrid)
                        itemGrids[0].ScrollPageDown();
                    break;
                case Key.Home:
                    if (HasScrollableItemGrid)
                        itemGrids[0].ScrollToBegin();
                    break;
                case Key.End:
                    if (HasScrollableItemGrid)
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

                    BattleFieldSlotClicked?.Invoke(slotColumn, slotRow);
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

            buttonGrid.MouseUp(position, MouseButtons.Right, out newCursorType, currentTicks);

            if (!game.InputEnable)
                return;
        }

        public bool Click(Position position, MouseButtons buttons, ref CursorType cursorType,
            uint currentTicks, bool pickingNewLeader = false)
        {
            if (!pickingNewLeader)
            {
                if (Type == LayoutType.Event)
                {
                    if (buttons == MouseButtons.Right)
                        game.CloseWindow();
                    else if (buttons == MouseButtons.Left)
                    {
                        texts[0].Click(position);
                        cursorType = CursorType.Click;
                    }
                    return true;
                }
                else if (game.ChestText != null)
                {
                    if (buttons == MouseButtons.Left)
                    {
                        if (game.ChestText.Click(position))
                        {
                            cursorType = game.ChestText?.WithScrolling == true ? CursorType.Click : CursorType.Sword;
                            return true;
                        }
                    }
                }
                else if (InventoryMessageWaitsForClick)
                {
                    if (buttons == MouseButtons.Left)
                    {
                        inventoryMessage.Click(position);
                        cursorType = inventoryMessage == null ? CursorType.Sword : CursorType.Click;
                        return true;
                    }
                }

                if (PopupActive)
                {
                    if (activePopup.CloseOnClick || (buttons == MouseButtons.Right &&
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
                                }
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
                return false;

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
                            if (draggedItem.SourcePlayer == i)
                            {
                                CancelDrag();
                            }
                            else
                            {
                                if (!partyMember.CanTakeItems(itemManager, draggedItem.Item.Item) ||
                                    game.HasPartyMemberFled(partyMember))
                                    return false;

                                int remaining = game.DropItem(i, null, draggedItem.Item.Item);

                                if (remaining == 0)
                                {
                                    draggedItem.Item.Destroy();

                                    if (draggedItem.SourcePlayer == null && game.OpenStorage != null)
                                        game.ItemRemovedFromStorage();

                                    DropItem();
                                }
                                else
                                    draggedItem.Item.Update(false);
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
                            if (partyMember.MaxGoldToTake >= draggedGold)
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
                            CancelDrag();
                            game.CursorType = CursorType.Sword;
                        }

                        return true;
                    }
                    else if (draggedFood != 0)
                    {
                        if (buttons == MouseButtons.Left)
                        {
                            if (partyMember.MaxFoodToTake >= draggedFood)
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
                            CancelDrag();
                            game.CursorType = CursorType.Sword;
                        }

                        return true;
                    }
                    else
                    {
                        if (buttons == MouseButtons.Left)
                            game.SetActivePartyMember(i);
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

            return false;
        }

        void PostItemDrag()
        {
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
            {
                var partyMember = game.GetPartyMember(i);

                if (partyMember != null && partyMember != game.CurrentInventory)
                {
                    UpdateCharacterStatus(i, partyMember.CanTakeItems(itemManager, draggedItem.Item.Item) &&
                        !game.HasPartyMemberFled(partyMember) ? UIGraphic.StatusHandTake : UIGraphic.StatusHandStop);
                }
            }

            if (game.OpenStorage is Chest)
                ShowChestMessage(game.DataNameProvider.WhereToMoveIt);
            else
                SetInventoryMessage(game.DataNameProvider.WhereToMoveIt);
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
            else if (cursorType == CursorType.None || cursorType >= CursorType.ArrowUp && cursorType <= CursorType.Wait)
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
                    SetActiveTooltip(position, null);
            }

            return consumed;
        }

        public CursorType? PressButton(int index, uint currentTicks)
        {
            if (PopupActive)
                return null;

            return buttonGrid.PressButton(index, currentTicks);
        }

        public void ReleaseButton(int index)
        {
            buttonGrid.ReleaseButton(index);
        }

        public void ReleaseButtons()
        {
            for (int i = 0; i < 9; ++i)
                ReleaseButton(i);
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

        public BattleAnimation AddMonsterCombatSprite(int column, int row, Monster monster, byte displayLayer)
        {
            float sizeMultiplier = RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)row);            
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.BattleMonsterRow);
            var sprite = RenderView.SpriteFactory.Create((int)monster.MappedFrameWidth, (int)monster.MappedFrameHeight, true) as ILayerSprite;
            sprite.TextureAtlasOffset = textureAtlas.GetOffset(monster.Index);
            sprite.DisplayLayer = displayLayer;
            sprite.PaletteIndex = monster.CombatGraphicIndex switch // TODO
            {
                MonsterGraphicIndex.Gizzek => 36,
                MonsterGraphicIndex.Tornak => 19,
                MonsterGraphicIndex.MoragMachine => 19,
                _ => 17
            };
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
                49, (byte)(3 + row), monster.Name, TextColor.Orange, Layer.UI, out Tooltip tooltip),
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
                    animation.Play(monster.GetAnimationFrameIndices(animationType), Game.TicksPerSecond / 6, totalTicks); // TODO: ticks per frame

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
                        Sprite = AddSprite(Global.BattleFieldSlotArea(index), textureIndex, 50, 2),
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
