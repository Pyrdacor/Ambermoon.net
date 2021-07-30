using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = Ambermoon.Data.Attribute;
using TextColor = Ambermoon.Data.Enumerations.Color;
using InteractionType = Ambermoon.Data.ConversationEvent.InteractionType;
using Ambermoon.Data.Audio;

namespace Ambermoon
{
    public class Game
    {
        internal class ConversationItems : IItemStorage
        {
            public const int SlotsPerRow = 6;
            public const int SlotRows = 4;

            public ItemSlot[,] Slots { get; } = new ItemSlot[6, 4];
            public bool AllowsItemDrop { get; set; } = false;

            public ConversationItems()
            {
                for (int y = 0; y < 4; ++y)
                {
                    for (int x = 0; x < 6; ++x)
                        Slots[x, y] = new ItemSlot();
                }
            }

            public void ResetItem(int slot, ItemSlot item)
            {
                int column = slot % SlotsPerRow;
                int row = slot / SlotsPerRow;

                if (Slots[column, row].Add(item) != 0)
                    throw new AmbermoonException(ExceptionScope.Application, "Unable to reset conversation item.");
            }

            public ItemSlot GetSlot(int slot) => Slots[slot % SlotsPerRow, slot / SlotsPerRow];
        }

        class NameProvider : ITextNameProvider
        {
            readonly Game game;

            public NameProvider(Game game)
            {
                this.game = game;
            }

            Character Subject => game.currentWindow.Window == Window.Healer ? game.currentlyHealedMember : game.CurrentSpellTarget ?? game.CurrentPartyMember;

            /// <inheritdoc />
            public string LeadName => game.CurrentPartyMember?.Name ?? "";
            /// <inheritdoc />
            public string SelfName => game?.PartyMembers?.FirstOrDefault()?.Name ?? LeadName;
            /// <inheritdoc />
            public string CastName => game.CurrentCaster?.Name ?? LeadName;
            /// <inheritdoc />
            public string InvnName => game.CurrentInventory?.Name ?? LeadName;
            /// <inheritdoc />
            public string SubjName => Subject?.Name;
            /// <inheritdoc />
            public string Sex1Name => Subject?.Gender == Gender.Male ? game.DataNameProvider.He : game.DataNameProvider.She;
            /// <inheritdoc />
            public string Sex2Name => Subject?.Gender == Gender.Male ? game.DataNameProvider.His : game.DataNameProvider.Her;
        }

        class Movement
        {
            readonly uint[] tickDivider;

            public uint TickDivider(bool is3D, bool worldMap, TravelType travelType) => tickDivider[is3D ? 0 : !worldMap ? 1 : 2 + (int)travelType];
            public uint MovementTicks(bool is3D, bool worldMap, TravelType travelType) => TicksPerSecond / TickDivider(is3D, worldMap, travelType);
            public float MoveSpeed3D { get; }
            public float TurnSpeed3D { get; }

            public Movement(bool legacyMode)
            {
                tickDivider = new uint[]
                {
                    GetTickDivider3D(legacyMode), // 3D movement
                    // 2D movement
                    7,  // Indoor
                    4,  // Outdoor walk
                    8,  // Horse
                    4,  // Raft
                    8,  // Ship
                    4,  // Magical disc
                    15, // Eagle
                    30, // Fly
                    4,  // Swim
                    10, // Witch broom
                    8,  // Sand lizard
                    8   // Sand ship
                };
                MoveSpeed3D = GetMoveSpeed3D(legacyMode);
                TurnSpeed3D = GetTurnSpeed3D(legacyMode);
            }

            static uint GetTickDivider3D(bool legacyMode) => legacyMode ? 8u : 60u;
            static float GetMoveSpeed3D(bool legacyMode) => legacyMode ? 0.25f : 0.04f;
            static float GetTurnSpeed3D(bool legacyMode) => legacyMode ? 15.0f : 2.0f;
        }

        class TimedGameEvent
        {
            public DateTime ExecutionTime;
            public Action Action;
        }

        internal class BattleEndInfo
        {
            /// <summary>
            /// If true all monsters were defeated or did flee.
            /// If false all party members fled.
            /// If all party members died the game is just over
            /// and this event is not used anymore.
            /// </summary>
            public bool MonstersDefeated;
            /// <summary>
            /// If all monsters were defeated this list contains
            /// the monsters who died.
            /// </summary>
            public List<Monster> KilledMonsters;
            /// <summary>
            /// Total experience for the party.
            /// </summary>
            public int TotalExperience;
            /// <summary>
            /// Partymembers who fled.
            /// </summary>
            public List<PartyMember> FledPartyMembers;
            /// <summary>
            /// List of broken items.
            /// </summary>
            public List<KeyValuePair<uint, ItemSlotFlags>> BrokenItems;
        }

        class BattleInfo
        {
            public uint MonsterGroupIndex;
            public event Action<BattleEndInfo> BattleEnded;

            internal void EndBattle(BattleEndInfo battleEndInfo) => BattleEnded?.Invoke(battleEndInfo);
        }

        enum PlayerBattleAction
        {
            /// <summary>
            /// This is the initial action in each round.
            /// The player can select the active party member.
            /// He also can select actions.
            /// </summary>
            PickPlayerAction,
            PickEnemySpellTarget,
            PickEnemySpellTargetRow,
            PickFriendSpellTarget,
            PickMoveSpot,
            PickAttackSpot,
            PickMemberToBlink,
            PickBlinkTarget
        }

        /// <summary>
        /// Character info texts that may change while
        /// in Inventory/Stats window.
        /// </summary>
        enum CharacterInfo
        {
            Age,
            Level,
            EP,
            LP,
            SP,
            SLPAndTP,
            GoldAndFood,
            Attack,
            Defense,
            Weight,
            /// <summary>
            /// Gold of the conversating party member.
            /// </summary>
            ConversationGold,
            /// <summary>
            /// Food of the conversating party member.
            /// </summary>
            ConversationFood,
            ChestGold,
            ChestFood,
            /// <summary>
            /// Name of the conversating party member.
            /// </summary>
            ConversationPartyMember
        }

        public delegate void FullscreenChangeHandler(bool fullscreen);
        public delegate void ResolutionChangeHandler(int? oldWidth);

        // TODO: cleanup members
        readonly FullscreenChangeHandler fullscreenChangeHandler;
        readonly ResolutionChangeHandler resolutionChangeHandler;
        public IConfiguration Configuration { get; private set; }
        public event Action<IConfiguration, bool> ConfigurationChanged;
        internal GameLanguage GameLanguage { get; private set; }
        CharacterCreator characterCreator = null;
        readonly Random random = new Random();
        internal SavegameTime GameTime { get; private set; } = null;
        readonly List<uint> changedMaps = new List<uint>();
        const int FadeTime = 1000;
        public const int MaxPartyMembers = 6;
        public const uint TicksPerSecond = 60;
        /// <summary>
        /// This is used for screen shaking.
        /// Position is in percentage of the resolution.
        /// </summary>
        public FloatPosition ViewportOffset { get; private set; } = null;
        readonly bool legacyMode = false;
        public event Action QuitRequested;
        public bool Godmode
        {
            get;
            set;
        } = false;
        bool ingame = false;
        bool is3D = false;
        bool noEvents = false;
        bool levitating = false;
        internal const ushort MaxBaseLine = 0x4000;
        // Note: This is half the max base line which is used for the player in complete
        // darkness. Big gaps are needed as the z buffer precision is lower with higher distance.
        const ushort FowBaseLine = 0x2000;
        uint lightIntensity = 0;
        readonly IFow fow2D = null;
        IOutroFactory outroFactory;
        IOutro outro = null;
        internal bool CanSee() => !CurrentPartyMember.Ailments.HasFlag(Ailment.Blind) &&
            (!Map.Flags.HasFlag(MapFlags.Dungeon) || CurrentSavegame.IsSpellActive(ActiveSpellType.Light));
        internal bool GameOverButtonsVisible { get; private set; } = false;
        internal bool WindowActive => currentWindow.Window != Window.MapView;
        internal bool PopupActive => layout?.PopupActive ?? false;
        static readonly WindowInfo DefaultWindow = new WindowInfo { Window = Window.MapView };
        WindowInfo currentWindow = DefaultWindow;
        internal WindowInfo LastWindow { get; private set; } = DefaultWindow;
        internal WindowInfo CurrentWindow => currentWindow;
        Action closeWindowHandler = null;
        FilledArea mapViewRightFillArea = null;
        // Note: These are not meant for ingame stuff but for fade effects etc that use real time.
        readonly List<TimedGameEvent> timedEvents = new List<TimedGameEvent>();
        readonly Movement movement;
        internal uint CurrentTicks { get; private set; } = 0;
        internal uint CurrentBattleTicks { get; private set; } = 0;
        internal uint CurrentPopupTicks { get; private set; } = 0;
        internal uint CurrentAnimationTicks { get; private set; } = 0;
        uint lastMapTicksReset = 0;
        uint lastMoveTicksReset = 0;
        bool weightDisplayBlinking = false;
        readonly TimedGameEvent ouchEvent = new TimedGameEvent();
        readonly TimedGameEvent hurtPlayerEvent = new TimedGameEvent();
        TravelType travelType = TravelType.Walk;
        readonly NameProvider nameProvider;
        readonly TextDictionary textDictionary;
        internal IDataNameProvider DataNameProvider { get; }
        readonly ILightEffectProvider lightEffectProvider;
        readonly Layout layout;
        readonly Dictionary<CharacterInfo, UIText> characterInfoTexts = new Dictionary<CharacterInfo, UIText>();
        readonly Dictionary<CharacterInfo, Panel> characterInfoPanels = new Dictionary<CharacterInfo, Panel>();
        public IMapManager MapManager { get; }
        public IItemManager ItemManager { get; }
        public ICharacterManager CharacterManager { get; }
        readonly Places places;
        readonly IRenderView renderView;
        internal IAudioOutput AudioOutput { get; private set; }
        readonly ISongManager songManager;
        ISong currentSong;
        internal ISavegameManager SavegameManager { get; }
        readonly ISavegameSerializer savegameSerializer;
        Player player;
        internal IRenderPlayer RenderPlayer => is3D ? (IRenderPlayer)player3D: player2D;
        public PartyMember CurrentPartyMember { get; private set; } = null;
        bool pickingNewLeader = false;
        bool pickingTargetPlayer = false;
        bool pickingTargetInventory = false;
        event Action<int> newLeaderPicked;
        event Action<int> targetPlayerPicked;
        event Func<int, bool> targetInventoryPicked;
        event Func<ItemGrid, int, ItemSlot, bool> targetItemPicked;
        bool advancing = false; // party or monsters are advancing
        internal PartyMember CurrentInventory => CurrentInventoryIndex == null ? null : GetPartyMember(CurrentInventoryIndex.Value);
        internal int? CurrentInventoryIndex { get; private set; } = null;
        internal Character CurrentCaster { get; set; } = null;
        internal Character CurrentSpellTarget { get; set; } = null;
        public Map Map => !ingame ? null : is3D ? renderMap3D?.Map : renderMap2D?.Map;
        public Position PartyPosition => !ingame || Map == null || player == null ? new Position() : Map.MapOffset + player.Position;
        internal bool MonsterSeesPlayer { get; set; } = false;
        bool monstersCanMoveImmediately = false; // this is set when the player just moved so that monsters who see the player can instantly move (2D only)
        Position lastPlayerPosition = null;
        BattleInfo currentBattleInfo = null;
        Battle currentBattle = null;
        internal bool BattleActive => currentBattle != null;
        internal bool BattleRoundActive => currentBattle?.RoundActive == true;
        readonly ILayerSprite[] partyMemberBattleFieldSprites = new ILayerSprite[MaxPartyMembers];
        readonly Tooltip[] partyMemberBattleFieldTooltips = new Tooltip[MaxPartyMembers];
        PlayerBattleAction currentPlayerBattleAction = PlayerBattleAction.PickPlayerAction;
        PartyMember currentPickingActionMember = null;
        PartyMember currentlyHealedMember = null;
        SpellAnimation currentAnimation = null;
        Spell pickedSpell = Spell.None;
        uint? spellItemSlotIndex = null;
        bool? spellItemIsEquipped = null;
        uint? blinkCharacterPosition = null;
        readonly Dictionary<int, Battle.PlayerBattleAction> roundPlayerBattleActions = new Dictionary<int, Battle.PlayerBattleAction>(MaxPartyMembers);
        readonly ILayerSprite ouchSprite;
        readonly ILayerSprite[] hurtPlayerSprites = new ILayerSprite[MaxPartyMembers]; // splash
        readonly IRenderText[] hurtPlayerDamageTexts = new IRenderText[MaxPartyMembers];
        readonly ILayerSprite battleRoundActiveSprite; // sword and mace
        readonly List<ILayerSprite> highlightBattleFieldSprites = new List<ILayerSprite>();
        bool blinkingHighlight = false;
        FilledArea buttonGridBackground;
        IColoredRect drugOverlay = null;
        uint lastDrugColorChangeTicks = 0;
        uint lastDrugMouseMoveTicks = 0;
        public Action DrugTicked;
        readonly bool[] keys = new bool[Enum.GetValues<Key>().Length];
        bool allInputWasDisabled = false;
        bool allInputDisabled = false;
        bool inputEnable = true;
        bool paused = false;
        internal bool ConversationTextActive { get; private set; } = false;
        Func<MouseButtons, bool> nextClickHandler = null;
        /// <summary>
        /// The 3x3 buttons will always be enabled!
        /// </summary>
        public bool InputEnable
        {
            get => inputEnable;
            set
            {
                if (inputEnable == value)
                    return;

                inputEnable = value;
                layout.ReleaseButtons();
                clickMoveActive = false;
                UntrapMouse();

                if (!inputEnable)
                    ResetMoveKeys(true);
            }
        }
        internal TravelType TravelType
        {
            get => travelType;
            set
            {
                travelType = value;
                CurrentSavegame.TravelType = value;
                if (Map != null)
                    PlayMusic(travelType.TravelSong());
                player.MovementAbility = travelType.ToPlayerMovementAbility();
                if (Map?.IsWorldMap == true)
                {
                    player2D?.UpdateAppearance(CurrentTicks);
                    player2D.BaselineOffset = !CanSee() ? MaxBaseLine : player.MovementAbility > PlayerMovementAbility.Swimming ? 32 : 0;
                }
                else if (!is3D && player2D != null)
                {
                    player2D.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
                }
            }
        }
        bool clickMoveActive = false;
        bool trappedAfterClickMoveActivation = false;
        Rect trapMouseArea = null;
        Rect trapMouseGameArea = null;
        Rect preFullscreenChangeTrapMouseArea = null;
        Position preFullscreenMousePosition = null;
        bool mouseTrappingActive = false;
        Position lastMousePosition = new Position();
        readonly Position trappedMousePositionOffset = new Position();
        bool trapped => trapMouseArea != null;
        public event Action<bool, Position> MouseTrappedChanged;
        public event Action<Position> MousePositionChanged;
        readonly Func<List<Key>> pressedKeyProvider;
        Func<Position, MouseButtons, bool> battlePositionClickHandler = null;
        Action<Position> battlePositionDragHandler = null;
        bool battlePositionDragging = false;
        static Dictionary<uint, Chest> initialChests = null;
        internal Savegame CurrentSavegame { get; private set; }
        event Action ActivePlayerChanged;

        // Rendering
        readonly Cursor cursor = null;
        RenderMap2D renderMap2D = null;
        Player2D player2D = null;
        RenderMap3D renderMap3D = null;
        Player3D player3D = null;
        readonly ICamera3D camera3D = null;
        readonly IRenderText messageText = null;
        readonly IRenderText windowTitle = null;
        byte currentUIPaletteIndex = 0;
        internal byte PrimaryUIPaletteIndex { get; }
        internal byte SecondaryUIPaletteIndex { get; }
        internal byte AutomapPaletteIndex { get; }
        /// <summary>
        /// Open chest which can be used to store items.
        /// </summary>
        internal IItemStorage OpenStorage { get; private set; }
        readonly int[] spellListScrollOffsets = new int[MaxPartyMembers];
        Rect mapViewArea = Map2DViewArea;
        internal static readonly Rect Map2DViewArea = new Rect(Global.Map2DViewX, Global.Map2DViewY,
            Global.Map2DViewWidth, Global.Map2DViewHeight);
        internal static readonly Rect Map3DViewArea = new Rect(Global.Map3DViewX, Global.Map3DViewY,
            Global.Map3DViewWidth, Global.Map3DViewHeight);
        internal int PlayerAngle => is3D ? Util.Round(player3D.Angle) : (int)player2D.Direction.ToAngle();
        internal CharacterDirection PlayerDirection => is3D ? player3D.Direction : player2D.Direction;
        bool targetMode2DActive = false;
        bool disableUntrapping = false;
        internal CursorType CursorType
        {
            get => cursor.Type;
            set
            {
                if (cursor.Type == value)
                    return;

                cursor.Type = value;

                if (value != CursorType.Eye &&
                    value != CursorType.Mouth &&
                    value != CursorType.Hand &&
                    value != CursorType.Target)
                    targetMode2DActive = false;

                if (!is3D && !WindowActive && !layout.PopupActive &&
                    (cursor.Type == CursorType.Eye ||
                    cursor.Type == CursorType.Hand))
                {
                    int yOffset = Map.IsWorldMap ? 12 : 0;
                    TrapMouse(new Rect(player2D.DisplayArea.X - 9, player2D.DisplayArea.Y - 9 - yOffset, 33, 49));
                }
                else if (!is3D && !WindowActive && !layout.PopupActive &&
                    (cursor.Type == CursorType.Mouth ||
                    cursor.Type == CursorType.Target))
                {
                    int yOffset = Map.IsWorldMap ? 12 : 0;
                    TrapMouse(new Rect(player2D.DisplayArea.X - 25, player2D.DisplayArea.Y - 25 - yOffset, 65, 65));
                }
                else if (!disableUntrapping)
                {
                    if (is3D && clickMoveActive && !trappedAfterClickMoveActivation &&
                        value >= CursorType.ArrowForward && value <= CursorType.Wait)
                        return;
                    UntrapMouse();
                }
            }
        }
        internal IMapCharacter CurrentMapCharacter { get; set; } // This is set when interacting with a map character

        internal void RequestFullscreenChange(bool fullscreen) => fullscreenChangeHandler?.Invoke(fullscreen);
        internal void NotifyResolutionChange(int? oldWidth) => resolutionChangeHandler?.Invoke(oldWidth);

        public Game(IConfiguration configuration, GameLanguage gameLanguage, IRenderView renderView, IMapManager mapManager,
            IItemManager itemManager, ICharacterManager characterManager, ISavegameManager savegameManager,
            ISavegameSerializer savegameSerializer, IDataNameProvider dataNameProvider, TextDictionary textDictionary,
            Places places, Cursor cursor, ILightEffectProvider lightEffectProvider, IAudioOutput audioOutput, ISongManager songManager,
            FullscreenChangeHandler fullscreenChangeHandler, ResolutionChangeHandler resolutionChangeHandler,
            Func<List<Key>> pressedKeyProvider, IOutroFactory outroFactory)
        {
            currentUIPaletteIndex = PrimaryUIPaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);
            SecondaryUIPaletteIndex = (byte)(renderView.GraphicProvider.SecondaryUIPaletteIndex - 1);
            AutomapPaletteIndex = (byte)(renderView.GraphicProvider.AutomapPaletteIndex - 1);

            this.fullscreenChangeHandler = fullscreenChangeHandler;
            this.resolutionChangeHandler = resolutionChangeHandler;
            Configuration = configuration;
            GameLanguage = gameLanguage;
            this.cursor = cursor;
            this.pressedKeyProvider = pressedKeyProvider;
            movement = new Movement(configuration.LegacyMode);
            nameProvider = new NameProvider(this);
            this.renderView = renderView;
            AudioOutput = audioOutput;
            this.songManager = songManager;
            MapManager = mapManager;
            ItemManager = itemManager;
            CharacterManager = characterManager;
            SavegameManager = savegameManager;
            this.places = places;
            this.savegameSerializer = savegameSerializer;
            DataNameProvider = dataNameProvider;
            this.textDictionary = textDictionary;
            this.lightEffectProvider = lightEffectProvider;
            this.outroFactory = outroFactory;
            camera3D = renderView.Camera3D;
            messageText = renderView.RenderTextFactory.Create();
            messageText.Layer = renderView.GetLayer(Layer.Text);
            windowTitle = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(""), TextColor.BrightGray, true,
                new Rect(8, 40, 192, 10), TextAlign.Center);
            windowTitle.DisplayLayer = 2;
            layout = new Layout(this, renderView, itemManager);
            layout.BattleFieldSlotClicked += BattleFieldSlotClicked;
            fow2D = renderView.FowFactory.Create(Global.Map2DViewWidth, Global.Map2DViewHeight,
                new Position(Global.Map2DViewX + Global.Map2DViewWidth / 2, Global.Map2DViewY + Global.Map2DViewHeight / 2), 255);
            fow2D.BaseLineOffset = FowBaseLine;
            fow2D.X = Global.Map2DViewX;
            fow2D.Y = Global.Map2DViewY;
            fow2D.Layer = renderView.GetLayer(Layer.FOW);
            fow2D.Visible = false;
            drugOverlay = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Render.Color.Black, 255);
            drugOverlay.Layer = renderView.GetLayer(Layer.DrugEffect);
            drugOverlay.X = 0;
            drugOverlay.Y = 0;
            drugOverlay.Visible = false;
            ouchSprite = renderView.SpriteFactory.Create(32, 23, true) as ILayerSprite;
            ouchSprite.ClipArea = Map2DViewArea;
            ouchSprite.Layer = renderView.GetLayer(Layer.UI);
            ouchSprite.PaletteIndex = currentUIPaletteIndex;
            ouchSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Ouch));
            ouchSprite.Visible = false;
            ouchEvent.Action = () => ouchSprite.Visible = false;
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                hurtPlayerSprites[i] = renderView.SpriteFactory.Create(32, 26, true, 200) as ILayerSprite;
                hurtPlayerSprites[i].Layer = renderView.GetLayer(Layer.UI);
                hurtPlayerSprites[i].PaletteIndex = PrimaryUIPaletteIndex;
                hurtPlayerSprites[i].TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Explosion));
                hurtPlayerSprites[i].Visible = false;
                hurtPlayerDamageTexts[i] = renderView.RenderTextFactory.Create();
                hurtPlayerDamageTexts[i].Layer = renderView.GetLayer(Layer.Text);
                hurtPlayerDamageTexts[i].DisplayLayer = 201;
                hurtPlayerDamageTexts[i].TextAlign = TextAlign.Center;
                hurtPlayerDamageTexts[i].Shadow = true;
                hurtPlayerDamageTexts[i].TextColor = TextColor.White;
                hurtPlayerDamageTexts[i].Visible = false;
            }
            hurtPlayerEvent.Action = () =>
            {
                for (int i = 0; i < MaxPartyMembers; ++i)
                {
                    hurtPlayerDamageTexts[i].Visible = false;
                    hurtPlayerSprites[i].Visible = false;
                }
            };
            battleRoundActiveSprite = renderView.SpriteFactory.Create(32, 36, true) as ILayerSprite;
            battleRoundActiveSprite.Layer = renderView.GetLayer(Layer.UI);
            battleRoundActiveSprite.PaletteIndex = PrimaryUIPaletteIndex;
            battleRoundActiveSprite.DisplayLayer = 2;
            battleRoundActiveSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)
                .GetOffset(Graphics.CombatGraphicOffset + (uint)CombatGraphicIndex.UISwordAndMace);
            battleRoundActiveSprite.X = 240;
            battleRoundActiveSprite.Y = 150;
            battleRoundActiveSprite.Visible = false;

            // Create texture atlas for monsters in battle
            var textureAtlasManager = TextureAtlasManager.Instance;
            var monsterGraphicDictionary = CharacterManager.Monsters.ToDictionary(m => m.Index, m => m.CombatGraphic);
            textureAtlasManager.AddFromGraphics(Layer.BattleMonsterRow, monsterGraphicDictionary);
            var monsterGraphicAtlas = textureAtlasManager.GetOrCreate(Layer.BattleMonsterRow);
            renderView.GetLayer(Layer.BattleMonsterRow).Texture = monsterGraphicAtlas.Texture;

            layout.ShowPortraitArea(false);
        }

        internal byte UIPaletteIndex => currentUIPaletteIndex;

        internal byte TextPaletteIndex => WindowActive ? UIPaletteIndex : (byte)((Map?.PaletteIndex ?? 1) - 1);

        /// <summary>
        /// This is called when the game starts.
        /// </summary>
        public void Run(bool continueGame, Position startCursorPosition)
        {
            layout.ShowPortraitArea(false);

            lastMousePosition = new Position(startCursorPosition);
            cursor.Type = CursorType.Sword;
            UpdateCursor(lastMousePosition, MouseButtons.None);

            if (continueGame)
            {
                ContinueGame();
            }
            else
            {
                NewGame(false);
            }
        }

        internal void NewGame(bool cleanUp = true)
        {
            if (cleanUp)
            {
                ClosePopup();
                CloseWindow();
                Cleanup();
                layout.ShowPortraitArea(false);
                layout.SetLayout(LayoutType.None);
                windowTitle.Visible = false;
                cursor.Type = CursorType.Sword;
                UpdateCursor(lastMousePosition, MouseButtons.None);
                currentUIPaletteIndex = 0;
                battleRoundActiveSprite.Visible = false;
            }

            currentSong?.Stop();
            currentSong = null;

            PlayMusic(Song.HisMastersVoice);

            characterCreator = new CharacterCreator(renderView, this, (name, female, portraitIndex) =>
            {
                var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);

                initialSavegame.PartyMembers[1].Name = name;
                initialSavegame.PartyMembers[1].Gender = female ? Gender.Female : Gender.Male;
                initialSavegame.PartyMembers[1].PortraitIndex = (ushort)portraitIndex;

                Start(initialSavegame);
                characterCreator = null;
            });
        }

        void Exit()
        {
            QuitRequested?.Invoke();
        }

        public void Quit() => Quit(null);

        void Quit(Action abortAction)
        {
            bool wasPaused = paused;
            ShowDecisionPopup(DataNameProvider.ReallyQuit, response =>
            {
                if (response == PopupTextEvent.Response.Yes)
                {
                    Exit();
                }
                else
                {
                    if (wasPaused)
                        Pause();
                    abortAction?.Invoke();
                }
            }, 1, 50, TextAlign.Center);
        }

        public void Pause()
        {
            if (paused)
                return;

            paused = true;

            GameTime.Pause();

            if (is3D)
                renderMap3D.Pause();
            else
                renderMap2D.Pause();
        }

        public void Resume()
        {
            if (!paused || WindowActive)
                return;

            paused = false;

            GameTime.Resume();

            if (is3D)
                renderMap3D.Resume();
            else
                renderMap2D.Resume();
        }

        uint UpdateTicks(uint ticks, double deltaTime)
        {
            uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

            if (ticks <= uint.MaxValue - add)
                ticks += add;
            else
                ticks = (uint)(((long)ticks + add) % uint.MaxValue);

            return ticks;
        }

        public void Update(double deltaTime)
        {
            if (outro?.Active == true)
            {
                outro.Update(deltaTime);
                return;
            }

            if (characterCreator != null)
            {
                characterCreator.Update(deltaTime);
                return;
            }

            for (int i = timedEvents.Count - 1; i >= 0; --i)
            {
                if (DateTime.Now >= timedEvents[i].ExecutionTime)
                {
                    var timedEvent = timedEvents[i];
                    timedEvents.RemoveAt(i);
                    timedEvent.Action?.Invoke();
                }
            }

            // Might be activated by a timed event and we don't want to
            // process other things in this case.
            if (outro?.Active == true)
                return;

            if (ingame)
            {
                CurrentAnimationTicks = UpdateTicks(CurrentAnimationTicks, deltaTime);

                if (currentAnimation != null)
                    currentAnimation.Update(CurrentAnimationTicks);

                if (!paused)
                {
                    GameTime?.Update();
                    MonsterSeesPlayer = false; // Will be set by the monsters Update methods eventually

                    CurrentTicks = UpdateTicks(CurrentTicks, deltaTime);

                    var animationTicks = CurrentTicks >= lastMapTicksReset ? CurrentTicks - lastMapTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMapTicksReset);

                    if (is3D)
                    {
                        renderMap3D.Update(animationTicks, GameTime);
                    }
                    else // 2D
                    {
                        renderMap2D.Update(animationTicks, GameTime, monstersCanMoveImmediately, lastPlayerPosition);
                    }

                    monstersCanMoveImmediately = false;

                    var moveTicks = CurrentTicks >= lastMoveTicksReset ? CurrentTicks - lastMoveTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMoveTicksReset);

                    if (moveTicks >= movement.MovementTicks(is3D, Map.IsWorldMap, TravelType))
                    {
                        lastMoveTicksReset = CurrentTicks;

                        if (clickMoveActive)
                            HandleClickMovement();
                        else
                            Move();
                    }
                }

                if ((!WindowActive ||
                    currentWindow.Window == Window.Inventory ||
                    currentWindow.Window == Window.Stats ||
                    currentWindow.Window == Window.Chest) && // TODO: healer, etc?
                    !layout.IsDragging)
                {
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        var partyMember = GetPartyMember(i);

                        if (partyMember != null)
                            layout.UpdateCharacterStatus(partyMember);
                    }
                }

                if (layout.PopupActive)
                    CurrentPopupTicks = UpdateTicks(CurrentPopupTicks, deltaTime);
                else
                    CurrentPopupTicks = CurrentTicks;

                if (currentBattle != null)
                {
                    if (!layout.OptionMenuOpen)
                    {
                        double timeFactor = Configuration.FastBattleMode ? 2.0f : 1.0f;
                        CurrentBattleTicks = UpdateTicks(CurrentBattleTicks, deltaTime * timeFactor);
                        UpdateBattle();

                        // Note: The null check for currentBattle is important here even if checking above.
                        if (currentBattle != null && !currentBattle.RoundActive && currentPlayerBattleAction == PlayerBattleAction.PickEnemySpellTargetRow)
                        {
                            var y = renderView.ScreenToGame(GetMousePosition(lastMousePosition)).Y - Global.BattleFieldArea.Top;
                            int hoveredRow = y / Global.BattleFieldSlotHeight;
                            highlightBattleFieldSprites.ForEach(s => s?.Delete());
                            highlightBattleFieldSprites.Clear();
                            for (int row = 0; row < 4; ++row)
                            {
                                for (int column = 0; column < 6; ++column)
                                {
                                    if (hoveredRow == row && currentBattle.GetCharacterAt(column, row)?.Type != CharacterType.PartyMember)
                                    {
                                        highlightBattleFieldSprites.Add
                                        (
                                            layout.AddSprite
                                            (
                                                Global.BattleFieldSlotArea(column + row * 6),
                                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                                                UIPaletteIndex
                                            )
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    CurrentBattleTicks = 0;
                }

                if (!WindowActive && layout.ButtonGridPage == 0)
                {
                    bool canMove = CanPartyMove();
                    layout.EnableButton(0, is3D || canMove);
                    layout.EnableButton(1, canMove);
                    layout.EnableButton(2, is3D || canMove);
                    layout.EnableButton(3, canMove);
                    layout.EnableButton(5, canMove);
                    layout.EnableButton(6, is3D || canMove);
                    layout.EnableButton(7, canMove);
                    layout.EnableButton(8, is3D || canMove);
                }

                if (CurrentWindow.Window == Window.Inventory &&
                    CurrentInventory != null &&
                    weightDisplayBlinking != CurrentInventory.Overweight)
                {
                    SetInventoryWeightDisplay(CurrentInventory);
                }

                if (!WindowActive && player2D != null)
                    fow2D.Center = player2D.DisplayArea.Center;
            }

            layout.Update(CurrentTicks);

            if (CurrentPartyMember != null && CurrentPartyMember.Ailments.HasFlag(Ailment.Drugged) &&
                !layout.OptionMenuOpen)
            {
                if (CurrentAnimationTicks - lastDrugColorChangeTicks >= 16)
                {
                    int colorComponent = RandomInt(0, 1) * 2;
                    renderView.DrugColorComponent = colorComponent;
                    ushort colorMod = (ushort)RandomInt(-9, 6);
                    if (colorComponent == 0) // red
                        colorMod <<= 8;
                    uint r = (colorMod & 0x0f00u) >> 8;
                    uint b = (colorMod & 0x00fu);
                    r = (r << 16) | (r << 20);
                    b |= (b << 4);
                    drugOverlay.Color = new Render.Color(r | b);
                    lastDrugColorChangeTicks = CurrentAnimationTicks;
                }

                if (CurrentAnimationTicks - lastDrugMouseMoveTicks >= 4)
                {
                    DrugTicked?.Invoke();
                    lastDrugMouseMoveTicks = CurrentAnimationTicks;
                }

                drugOverlay.Visible = true;
            }
            else
            {
                renderView.DrugColorComponent = null;
                drugOverlay.Visible = false;
            }
        }

        internal void NotifyConfigurationChange(bool windowChange)
        {
            ConfigurationChanged?.Invoke(Configuration, windowChange);

            if (windowChange && !trapped)
            {
                trappedMousePositionOffset.X = 0;
                trappedMousePositionOffset.Y = 0;
                MouseTrappedChanged?.Invoke(false, lastMousePosition);
                UpdateCursor();
            }
        }

        internal int RollDice100()
        {
            return RandomInt(0, 99);
        }

        public int RandomInt(int min, int max)
        {
            uint range = (uint)(max + 1 - min);
            return min + (int)(random.Next() % range);
        }

        Position GetMousePosition(Position position)
        {
            position = new Position(position); // Important to not modify passed position object!

            if (trapMouseArea != null)
                position += trappedMousePositionOffset;

            return position;
        }

        internal void TrapMouse(Rect area)
        {
            if (clickMoveActive)
                trappedAfterClickMoveActivation = true;

            mouseTrappingActive = true;

            try
            {
                var newTrapArea = renderView.GameToScreen(area);
                if (trapMouseArea == newTrapArea)
                    return;
                trapMouseGameArea = area;
                trapMouseArea = newTrapArea;
                trappedMousePositionOffset.X = 0;
                trappedMousePositionOffset.Y = 0;
                if (!trapMouseArea.Contains(lastMousePosition))
                {
                    bool keepX = lastMousePosition.X >= trapMouseArea.Left && lastMousePosition.X <= trapMouseArea.Right;
                    bool keepY = lastMousePosition.Y >= trapMouseArea.Top && lastMousePosition.Y <= trapMouseArea.Bottom;
                    if (!keepX)
                        lastMousePosition.X = lastMousePosition.X > trapMouseArea.Right ? trapMouseArea.Right : trapMouseArea.Left;
                    if (!keepY)
                        lastMousePosition.Y = lastMousePosition.Y > trapMouseArea.Bottom ? trapMouseArea.Bottom : trapMouseArea.Top;
                    UpdateCursor(lastMousePosition, MouseButtons.None);
                }
                MouseTrappedChanged?.Invoke(true, lastMousePosition);
            }
            finally
            {
                mouseTrappingActive = false;
            }
        }

        internal void UntrapMouse()
        {
            trappedAfterClickMoveActivation = false;

            if (mouseTrappingActive)
                return;

            if (trapMouseArea == null)
                return;

            lastMousePosition = GetMousePosition(lastMousePosition);
            MouseTrappedChanged?.Invoke(false, lastMousePosition);
            trapMouseArea = null;
            trapMouseGameArea = null;
            trappedMousePositionOffset.X = 0;
            trappedMousePositionOffset.Y = 0;
        }

        void ResetMoveKeys(bool forceDisable = false)
        {
            var pressedKeys = pressedKeyProvider?.Invoke();

            void ResetKey(Key key) => keys[(int)key] = !forceDisable && pressedKeys?.Contains(key) == true;

            ResetKey(Key.Up);
            ResetKey(Key.Down);
            ResetKey(Key.Left);
            ResetKey(Key.Right);
            ResetKey(Key.W);
            ResetKey(Key.A);
            ResetKey(Key.S);
            ResetKey(Key.D);

            if (!WindowActive && !layout.PopupActive && layout.ButtonGridPage == 0)
            {
                layout.ReleaseButton(0, true);
                layout.ReleaseButton(1, true);
                layout.ReleaseButton(2, true);
                layout.ReleaseButton(3, true);
                layout.ReleaseButton(5, true);
                layout.ReleaseButton(6, true);
                layout.ReleaseButton(7, true);
                layout.ReleaseButton(8, true);
            }

            lastMoveTicksReset = CurrentTicks;
        }

        public Render.Color GetTextColor(TextColor textColor) => GetUIColor((int)textColor);

        public Render.Color GetNamedPaletteColor(NamedPaletteColors namedPaletteColor) => GetUIColor((int)namedPaletteColor);

        internal Render.Color GetPaletteColor(int paletteIndex, int colorIndex)
        {
            var paletteData = renderView.GraphicProvider.Palettes[paletteIndex].Data;
            return new Render.Color
            (
                paletteData[colorIndex * 4 + 0],
                paletteData[colorIndex * 4 + 1],
                paletteData[colorIndex * 4 + 2],
                paletteData[colorIndex * 4 + 3]
            );
        }

        public Render.Color GetUIColor(int colorIndex) => GetPaletteColor(1 + UIPaletteIndex, colorIndex);

        internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial)
        {
            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

            layout.SetLayout(LayoutType.Map2D, movement.MovementTicks(false, Map?.IsWorldMap == true, TravelType.Walk));
            is3D = false;
            int xOffset = (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2;
            int yOffset = (int)playerY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2;

            if (map.IsWorldMap)
            {
                if (xOffset < 0)
                {
                    map = MapManager.GetMap(map.LeftMapIndex.Value);
                    xOffset += map.Width;
                    playerX += (uint)map.Width;
                }
                if (yOffset < 0)
                {
                    map = MapManager.GetMap(map.UpMapIndex.Value);
                    yOffset += map.Height;
                    playerY += (uint)map.Height;
                }
            }
            else
            {
                xOffset = Util.Limit(0, xOffset, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X);
                yOffset = Util.Limit(0, yOffset, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y);
            }

            if (renderMap2D.Map != map)
                renderMap2D.SetMap(map, (uint)xOffset, (uint)yOffset);
            else
            {
                renderMap2D.ScrollTo((uint)xOffset, (uint)yOffset, true);
                renderMap2D.AddCharacters(map);
                renderMap2D.InvokeMapChange();
            }

            if (player2D == null)
            {
                player2D = new Player2D(this, renderView.GetLayer(Layer.Characters), player, renderMap2D,
                    renderView.SpriteFactory, new Position(0, 0), MapManager);
            }

            player2D.Visible = true;
            player2D.RecheckTopSprite();
            player2D.MoveTo(map, playerX, playerY, CurrentTicks, true, direction);

            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;

            renderMap2D.CheckIfMonstersSeePlayer();

            renderView.GetLayer(Layer.Map3DBackground).Visible = false;
            renderView.GetLayer(Layer.Map3D).Visible = false;
            renderView.GetLayer(Layer.Billboards3D).Visible = false;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = true;

            mapViewArea = Map2DViewArea;

            PlayerMoved(true, null, true);
        }

        internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

            layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));

            is3D = true;
            TravelType = TravelType.Walk;
            renderMap2D.Destroy();
            renderMap3D.SetMap(map, playerX, playerY, direction, CurrentPartyMember?.Race ?? Race.Human);
            UpdateUIPalette(true);
            player3D.SetPosition((int)playerX, (int)playerY, CurrentTicks, !initial);
            player3D.TurnTowards((int)direction * 90.0f);
            if (player2D != null)
                player2D.Visible = false;
            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;

            renderView.GetLayer(Layer.Map3DBackground).Visible = true;
            renderView.GetLayer(Layer.Map3D).Visible = true;
            renderView.GetLayer(Layer.Billboards3D).Visible = true;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = false;

            mapViewArea = Map3DViewArea;

            PlayerMoved(true, null, true);
        }

        void Cleanup()
        {
            highlightBattleFieldSprites.ForEach(s => s?.Delete());
            highlightBattleFieldSprites.Clear();
            currentBattle?.EndBattleCleanup();
            outro?.Destroy();
            layout.Reset();
            renderMap2D?.Destroy();
            renderMap2D = null;
            renderMap3D?.Destroy(true);
            renderMap3D = null;
            CurrentMapCharacter = null;
            player2D?.Destroy();
            player2D = null;
            player3D = null;
            messageText.Visible = false;

            player = null;
            CurrentPartyMember = null;
            CurrentInventoryIndex = null;
            CurrentCaster = null;
            OpenStorage = null;
            weightDisplayBlinking = false;
            levitating = false;

            RenderMap3D.Reset();
            MapCharacter2D.Reset();

            for (int i = 0; i < keys.Length; ++i)
                keys[i] = false;
            clickMoveActive = false;
            trappedAfterClickMoveActivation = false;
            UntrapMouse();
            InputEnable = false;
            paused = false;

            for (int i = 0; i < spellListScrollOffsets.Length; ++i)
                spellListScrollOffsets[i] = 0;

            battleRoundActiveSprite.Visible = false;
        }

        internal void ResetMapCharacterInteraction(Map map, bool leaveMapCharacter = false)
        {
            if (CurrentMapCharacter != null)
            {
                CurrentMapCharacter.ResetLastInteractionTime();
                if (map.Type == MapType.Map3D)
                    RenderMap3D.Reset();
                else
                    MapCharacter2D.Reset();
                if (!leaveMapCharacter)
                    CurrentMapCharacter = null;
            }
        }

        public void Destroy()
        {
            Util.SafeCall(UntrapMouse);
            allInputDisabled = true;
            ingame = false;
            Util.SafeCall(() => AudioOutput?.Stop());
            Util.SafeCall(() => AudioOutput?.Reset());
            Util.SafeCall(Cleanup);
            Util.SafeCall(() => layout.Destroy());
            Util.SafeCall(() => CursorType = CursorType.None);
            Util.SafeCall(() => windowTitle?.Delete());
        }

        void PartyMemberDied(Character partyMember)
        {
            if (!(partyMember is PartyMember member))
                throw new AmbermoonException(ExceptionScope.Application, "PartyMemberDied with a character which is not a party member.");

            int? slot = SlotFromPartyMember(member);

            if (slot != null)
                layout.SetCharacter(slot.Value, member);
        }

        void PartyMemberRevived(PartyMember partyMember, Action finishAction = null, bool showHealAnimation = true)
        {
            if (currentWindow.Window == Window.Healer)
            {
                layout.UpdateCharacter(partyMember, () => layout.ShowClickChestMessage(DataNameProvider.ReviveMessage, finishAction));
            }
            else
            {
                ShowMessagePopup(DataNameProvider.ReviveMessage, () =>
                {
                    layout.SetCharacter(SlotFromPartyMember(partyMember).Value, partyMember, false, finishAction);
                    if (showHealAnimation)
                    {
                        currentAnimation?.Destroy();
                        currentAnimation = new SpellAnimation(this, layout);
                        currentAnimation.CastOn(Spell.WakeTheDead, partyMember, () =>
                        {
                            currentAnimation.Destroy();
                            currentAnimation = null;
                        });
                    }
                });
            }
        }

        void FixPartyMember(PartyMember partyMember)
        {
            // The original has some bugs where bonus values are not right.
            // We set the bonus values here dependent on equipment.
            partyMember.HitPoints.BonusValue = 0;
            partyMember.SpellPoints.BonusValue = 0;

            foreach (var attribute in Enum.GetValues<Attribute>())
            {
                partyMember.Attributes[attribute].BonusValue = 0;
            }

            foreach (var ability in Enum.GetValues<Ability>())
            {
                partyMember.Abilities[ability].BonusValue = 0;
            }

            foreach (var itemSlot in partyMember.Equipment.Slots)
            {
                if (itemSlot.Value.ItemIndex != 0)
                {
                    var item = ItemManager.GetItem(itemSlot.Value.ItemIndex);
                    int factor = itemSlot.Value.Flags.HasFlag(ItemSlotFlags.Cursed) ? -1 : 1;

                    partyMember.HitPoints.BonusValue += factor * item.HitPoints;
                    partyMember.SpellPoints.BonusValue += factor * item.SpellPoints;

                    if (item.Attribute != null)
                        partyMember.Attributes[item.Attribute.Value].BonusValue += factor * item.AttributeValue;
                    if (item.Ability != null)
                        partyMember.Abilities[item.Ability.Value].BonusValue += factor * item.AbilityValue;
                }
            }
        }

        void AddPartyMember(int slot, PartyMember partyMember, Action followAction = null, bool forceAnimation = false)
        {
            FixPartyMember(partyMember);
            partyMember.Died += PartyMemberDied;
            layout.SetCharacter(slot, partyMember, false, followAction, forceAnimation);
            spellListScrollOffsets[slot] = 0;
        }

        void RemovePartyMember(int slot, bool initialize, Action followAction = null)
        {
            var partyMember = GetPartyMember(slot);

            if (partyMember != null)
                partyMember.Died -= PartyMemberDied;

            layout.SetCharacter(slot, null, initialize, followAction);
            spellListScrollOffsets[slot] = 0;
        }

        void ClearPartyMembers()
        {
            for (int i = 0; i < Game.MaxPartyMembers; ++i)
                RemovePartyMember(i, true);
        }

        internal int? SlotFromPartyMember(PartyMember partyMember)
        {
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                if (GetPartyMember(i) == partyMember)
                    return i;
            }

            return null;
        }

        public void Start(Savegame savegame)
        {
            currentSong?.Stop();
            currentSong = null;
            Cleanup();
            MapExtensions.Reset();
            GameOverButtonsVisible = false;
            allInputDisabled = true;
            layout.AddFadeEffect(new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight), Render.Color.Black, FadeEffectType.FadeOut, FadeTime / 2);
            AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime / 2), () => allInputDisabled = false);

            // Reset all maps
            foreach (var changedMap in changedMaps)
            {
                MapManager.GetMap(changedMap).Reset();
            }
            changedMaps.Clear();

            ingame = true;
            CurrentSavegame = savegame;
            GameTime = new SavegameTime(savegame);
            GameTime.GotTired += GameTime_GotTired;
            GameTime.GotExhausted += GameTime_GotExhausted;
            GameTime.NewDay += GameTime_NewDay;
            GameTime.NewYear += GameTime_NewYear;
            GameTime.MinuteChanged += amount =>
            {
                if (Map.Flags.HasFlag(MapFlags.Dungeon) &&
                    !CurrentSavegame.IsSpellActive(ActiveSpellType.Light) &&
                    lightIntensity > 0)
                {
                    if (this.is3D)
                        lightIntensity = (uint)Math.Max(0, (int)lightIntensity - amount * 4);
                    else
                        lightIntensity = 0;
                    UpdateLight();
                }
                else if (Map.Flags.HasFlag(MapFlags.Outdoor) && this.is3D)
                {
                    UpdateLight();
                }
            };
            GameTime.HourChanged += GameTime_HoursPassed;
            currentBattle = null;

            ClearPartyMembers();
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                if (savegame.CurrentPartyMemberIndices[i] != 0)
                {
                    var partyMember = savegame.GetPartyMember(i);
                    AddPartyMember(i, partyMember);
                }
            }
            CurrentPartyMember = GetPartyMember(CurrentSavegame.ActivePartyMemberSlot);

            if (CurrentPartyMember == null)
            {
                CurrentSavegame.ActivePartyMemberSlot = 0;
                CurrentPartyMember = GetPartyMember(0);

                if (CurrentPartyMember == null)
                    throw new AmbermoonException(ExceptionScope.Data, "Invalid party member data in savegame.");
            }

            SetActivePartyMember(CurrentSavegame.ActivePartyMemberSlot);

            player = new Player();
            var map = MapManager.GetMap(savegame.CurrentMapIndex);
            bool is3D = map.Type == MapType.Map3D;
            renderMap2D = new RenderMap2D(this, null, MapManager, renderView);
            renderMap3D = new RenderMap3D(this, null, MapManager, renderView, 0, 0, CharacterDirection.Up);
            player3D = new Player3D(this, player, MapManager, camera3D, renderMap3D, 0, 0);
            player.MovementAbility = PlayerMovementAbility.Walking;
            renderMap2D.MapChanged += RenderMap2D_MapChanged;
            renderMap3D.MapChanged += RenderMap3D_MapChanged;
            TravelType = savegame.TravelType;
            if (is3D)
                Start3D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1, savegame.CharacterDirection, true);
            else
                Start2D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1, savegame.CharacterDirection, true);
            player.Position.X = (int)savegame.CurrentMapX - 1;
            player.Position.Y = (int)savegame.CurrentMapY - 1;
            TravelType = savegame.TravelType; // Yes this is necessary twice.

            ShowMap(true);
            layout.ShowPortraitArea(true);
            UpdateLight(true);

            InputEnable = true;
            paused = false;

            if (layout.ButtonGridPage == 1)
                ToggleButtonGridPage();

            // Trigger events after game load
            TriggerMapEvents(EventTrigger.Move, (uint)player.Position.X,
                (uint)player.Position.Y);
        }

        internal void ProcessPoisonDamage(uint times, Action followAction = null)
        {
            uint GetDamage()
            {
                uint damage = 0;

                for (uint i = 0; i < times; ++i)
                    damage += (uint)RandomInt(1, 5);

                return damage;
            }

            DamageAllPartyMembers(_ => GetDamage(),
                p => p.Alive && p.Ailments.HasFlag(Ailment.Poisoned), null, followAction);
        }

        void Sleep(bool inn)
        {
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                var partyMember = GetPartyMember(i);

                if (partyMember != null && partyMember.Alive)
                {
                    if (partyMember.Ailments.HasFlag(Ailment.Exhausted))
                    {
                        partyMember.Ailments &= ~Ailment.Exhausted;
                        RemoveExhaustion(partyMember);
                        layout.UpdateCharacterStatus(partyMember);
                    }
                }
            }

            void Start(bool toDawn)
            {
                // Set this first to avoid tired/exhausted warning when increasing the game time.
                GameTime.HoursWithoutSleep = 0;
                uint hoursToAdd = 8;
                uint minutesToAdd = 0;

                if (toDawn)
                {
                    if (GameTime.Hour >= 20) // move to next day
                    {
                        hoursToAdd = 7 + 24 - GameTime.Hour - 1;
                        minutesToAdd = 60 - GameTime.Minute % 60;
                    }
                    else
                    {
                        hoursToAdd = 7 - GameTime.Hour - 1;
                        minutesToAdd = 60 - GameTime.Minute % 60;
                    }
                }

                GameTime.Wait(hoursToAdd);

                while (minutesToAdd > 0)
                {
                    minutesToAdd -= 5;
                    GameTime.Tick();
                }

                // Set this again to reset it after game time was increased.
                GameTime.HoursWithoutSleep = 0; // This also resets it inside the savegame.

                // Recovery and food consumption
                void Recover(int slot)
                {
                    void Next() => Recover(slot + 1);

                    if (slot < MaxPartyMembers)
                    {
                        var partyMember = GetPartyMember(slot);

                        if (partyMember != null && partyMember.Alive)
                        {
                            if (!inn && partyMember.Food == 0)
                            {
                                layout.ShowClickChestMessage(partyMember.Name + DataNameProvider.HasNoMoreFood, Next);
                            }
                            else
                            {
                                int lpRecovered = Math.Max(0, (int)partyMember.HitPoints.TotalMaxValue - (int)partyMember.HitPoints.CurrentValue);
                                partyMember.HitPoints.CurrentValue = partyMember.HitPoints.TotalMaxValue;
                                int spRecovered = Math.Max(0, (int)partyMember.SpellPoints.TotalMaxValue - (int)partyMember.SpellPoints.CurrentValue);
                                partyMember.SpellPoints.CurrentValue = partyMember.SpellPoints.TotalMaxValue;
                                layout.FillCharacterBars(partyMember);

                                if (!inn)
                                    --partyMember.Food;

                                if (partyMember.Class.IsMagic()) // Has SP
                                {
                                    layout.ShowClickChestMessage(partyMember.Name + string.Format(DataNameProvider.RecoveredLPAndSP, lpRecovered, spRecovered), Next);
                                }
                                else
                                {
                                    layout.ShowClickChestMessage(partyMember.Name + string.Format(DataNameProvider.RecoveredLP, lpRecovered), Next);
                                }
                            }
                        }
                        else
                        {
                            Next();
                        }
                    }
                }
                Recover(0);
            }

            if (!inn && !Map.Flags.HasFlag(MapFlags.NoSleepUntilDawn) &&
                (GameTime.Hour >= 20 || GameTime.Hour < 4)) // Sleep until dawn
            {
                layout.ShowClickChestMessage(DataNameProvider.SleepUntilDawn, () => Start(true));
            }
            else // sleep 8 hours
            {
                layout.ShowClickChestMessage(DataNameProvider.Sleep8Hours, () => Start(false));
            }
        }

        void GameTime_GotExhausted(uint hoursExhausted, uint hoursPassed)
        {
            bool alreadyExhausted = false;
            uint[] damageValues = new uint[MaxPartyMembers];

            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                var partyMember = GetPartyMember(i);

                if (partyMember != null && partyMember.Alive)
                {
                    if (partyMember.Ailments.HasFlag(Ailment.Exhausted))
                        alreadyExhausted = true;
                    partyMember.Ailments |= Ailment.Exhausted;
                    damageValues[i] = AddExhaustion(partyMember, hoursExhausted);
                    if (damageValues[i] < partyMember.HitPoints.CurrentValue)
                        layout.UpdateCharacterStatus(partyMember);
                }
            }

            void DealDamage()
            {
                DamageAllPartyMembers(p => damageValues[SlotFromPartyMember(p).Value],
                    null, null, () => GameTime_HoursPassed(hoursPassed));
            }

            if (!alreadyExhausted)
                ShowMessagePopup(DataNameProvider.ExhaustedMessage, DealDamage);
            else
                DealDamage();
        }

        void GameTime_GotTired(uint hoursPassed)
        {
            ShowMessagePopup(DataNameProvider.TiredMessage, () => GameTime_HoursPassed(hoursPassed));
        }

        void GameTime_HoursPassed(uint hours)
        {
            UpdateLight();
            ProcessPoisonDamage(hours);
        }

        void AgePlayer(PartyMember partyMember, Action finishAction)
        {
            if (++partyMember.Attributes[Attribute.Age].CurrentValue >= partyMember.Attributes[Attribute.Age].MaxValue)
            {
                ShowMessagePopup(partyMember.Name + DataNameProvider.HasDiedOfAge, () =>
                {
                    partyMember.Die();
                    finishAction?.Invoke();
                });
            }
            else
            {
                ShowMessagePopup(partyMember.Name + DataNameProvider.HasAged, finishAction);
            }
        }

        /// <summary>
        /// Runs an action for each party member. In contrast to a normal foreach loop
        /// the action can contain blocking calls for each party member like popups.
        /// The next party member is processed after an action is finished for the
        /// previous member.
        /// </summary>
        /// <param name="action">Action to perform. Second parameter is the finish handler the action must call.</param>
        /// <param name="condition">Condition to filter affected party members.</param>
        /// <param name="followUpAction">Action to trigger after all party members were processed.</param>
        internal void ForeachPartyMember(Action<PartyMember, Action> action, Func<PartyMember, bool> condition = null,
            Action followUpAction = null)
        {
            void Run(int index)
            {
                if (index == MaxPartyMembers)
                {
                    followUpAction?.Invoke();
                    return;
                }

                var partyMember = GetPartyMember(index);

                if (partyMember == null || condition?.Invoke(partyMember) == false)
                {
                    Run(index + 1);
                }
                else
                {
                    action(partyMember, () => Run(index + 1));
                }
            }

            Run(0);
        }

        void GameTime_NewDay()
        {
            ForeachPartyMember(AgePlayer, partyMember =>
                partyMember.Alive && partyMember.Ailments.HasFlag(Ailment.Aging) &&
                    !partyMember.Ailments.HasFlag(Ailment.Petrified));
        }

        void GameTime_NewYear()
        {
            ForeachPartyMember(AgePlayer, partyMember =>
                partyMember.Alive && !partyMember.Ailments.HasFlag(Ailment.Petrified));
        }

        void RunSavegameTileChangeEvents(uint mapIndex)
        {
            if (CurrentSavegame.TileChangeEvents.ContainsKey(mapIndex))
            {
                var tileChangeEvents = CurrentSavegame.TileChangeEvents[mapIndex];

                foreach (var tileChangeEvent in tileChangeEvents)
                    UpdateMapTile(tileChangeEvent, null, null, false);
            }
        }

        void RenderMap3D_MapChanged(Map map)
        {
            ResetMoveKeys();
            RunSavegameTileChangeEvents(map.Index);
        }

        void RenderMap2D_MapChanged(Map lastMap, Map[] maps)
        {
            if (lastMap == null || !lastMap.IsWorldMap ||
                !maps[0].IsWorldMap || lastMap.World != maps[0].World)
                ResetMoveKeys();

            foreach (var map in maps)
                RunSavegameTileChangeEvents(map.Index);
        }

        internal string GetCustomText(CustomTexts.Index index) => CustomTexts.GetText(GameLanguage, index);

        public void LoadGame(int slot, bool showError = false, bool loadInitialOnError = false,
            Action<Action> preLoadAction = null, bool exitWhenFailing = true)
        {
            void Failed()
            {
                if (exitWhenFailing)
                    Exit();
                else
                    ClosePopup();
            }

            var savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, slot);

            if (savegame == null)
            {
                if (showError)
                {
                    if (loadInitialOnError && slot != 0)
                    {
                        savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, 0);

                        if (savegame == null)
                        {
                            ShowMessagePopup(GetCustomText(CustomTexts.Index.FailedToLoadSavegame),
                                Failed, TextAlign.Center, 200);
                        }
                        else
                        {
                            void ProceedWithInitial()
                            {
                                ShowMessagePopup(GetCustomText(CustomTexts.Index.FailedToLoadSavegameUseInitial),
                                    () => this.Start(savegame), TextAlign.Center, 200);
                            }
                            if (preLoadAction != null)
                                preLoadAction?.Invoke(ProceedWithInitial);
                            else
                                ProceedWithInitial();
                        }
                    }
                    else
                    {
                        if (slot == 0)
                        {
                            ShowMessagePopup(GetCustomText(CustomTexts.Index.FailedToLoadInitialSavegame),
                                Failed, TextAlign.Center, 200);
                        }
                        else
                        {
                            ShowMessagePopup(GetCustomText(CustomTexts.Index.FailedToLoadSavegame),
                                Failed, TextAlign.Center, 200);
                        }
                    }
                    return;
                }
                else if (loadInitialOnError && slot != 0)
                {
                    LoadGame(0, true, false, preLoadAction, exitWhenFailing);
                    return;
                }
                Failed();
                return;
            }

            void Start() => this.Start(savegame);

            if (preLoadAction != null)
                preLoadAction?.Invoke(Start);
            else
                Start();
        }

        public void SaveGame(int slot, string name)
        {
            SavegameManager.Save(renderView.GameData, savegameSerializer, slot, name, CurrentSavegame);
        }

        public void ContinueGame()
        {
            SavegameManager.GetSavegameNames(renderView.GameData, out int current);
            LoadGame(current, false, true);
        }

        // TODO: Optimize to not query this every time
        public List<string> Dictionary => textDictionary.Entries.Where((word, index) =>
            CurrentSavegame.IsDictionaryWordKnown((uint)index)).ToList();

        public IText ProcessText(string text)
        {
            return renderView.TextProcessor.ProcessText(text, nameProvider, Dictionary);
        }

        public IText ProcessText(string text, Rect bounds)
        {
            return renderView.TextProcessor.WrapText(ProcessText(text), bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        public void ShowMessage(Rect bounds, string text, TextColor color, bool shadow, TextAlign textAlign = TextAlign.Left)
        {
            messageText.Text = ProcessText(text, bounds);
            messageText.PaletteIndex = TextPaletteIndex;
            messageText.TextColor = color;
            messageText.Shadow = shadow;
            messageText.Place(bounds, textAlign);
            messageText.Visible = true;
        }

        public void HideMessage()
        {
            messageText.Visible = false;
        }

        internal void DisplayOuch()
        {
            if (is3D)
            {
                ouchSprite.X = 88;
                ouchSprite.Y = 65;
                ouchSprite.Resize(32, 23);
            }
            else
            {
                var playerArea = player2D.DisplayArea;
                ouchSprite.X = playerArea.X + 16;
                ouchSprite.Y = playerArea.Y - 24;
                ouchSprite.Resize(Math.Min(32, Map2DViewArea.Right - ouchSprite.X),
                    Math.Min(23, Map2DViewArea.Bottom - ouchSprite.Y));
            }

            ouchSprite.Visible = true;

            RenewTimedEvent(ouchEvent, TimeSpan.FromMilliseconds(150));
        }

        internal void ShowPlayerDamage(int slot, uint amount)
        {
            var area = new Rect(Global.PartyMemberPortraitAreas[slot]);
            hurtPlayerSprites[slot].X = area.X;
            hurtPlayerSprites[slot].Y = area.Y + 1;
            hurtPlayerSprites[slot].Visible = true;
            hurtPlayerDamageTexts[slot].Text = renderView.TextProcessor.CreateText(amount > 99 ? "**" : amount.ToString());
            area.Position.Y += 11;
            hurtPlayerDamageTexts[slot].Place(area, TextAlign.Center);
            hurtPlayerDamageTexts[slot].Visible = amount != 0;

            RenewTimedEvent(hurtPlayerEvent, TimeSpan.FromMilliseconds(500));
        }

        void HandleClickMovement()
        {
            if (paused || WindowActive || !InputEnable || !clickMoveActive || allInputDisabled || pickingNewLeader || pickingTargetPlayer || pickingTargetInventory)
            {
                if (clickMoveActive)
                {
                    if (!trappedAfterClickMoveActivation)
                        UntrapMouse();
                    clickMoveActive = false;
                }
                return;
            }

            lock (cursor)
            {
                float speedFactor3D = 0.0f;

                if (is3D)
                {
                    var position = GetMousePosition(lastMousePosition);
                    var relativePosition = renderView.ScreenToGame(position);
                    var center = Map3DViewArea.Center;
                    speedFactor3D = Math.Max(2.0f * Math.Abs(relativePosition.X - center.X) / Map3DViewArea.Width,
                        2.0f * Math.Abs(relativePosition.Y - center.Y) / Map3DViewArea.Height);
                }
                Move(false, speedFactor3D, cursor.Type);
            }
        }

        internal void StartSequence()
        {
            allInputWasDisabled = allInputDisabled;
            layout.ReleaseButtons();
            allInputDisabled = true;
            clickMoveActive = false;
            trappedAfterClickMoveActivation = false;
        }

        internal void EndSequence(bool force = true)
        {
            if (force || !allInputWasDisabled)
                allInputDisabled = false;
            allInputWasDisabled = false;
        }

        void PlayTimedSequence(int steps, Action stepAction, int stepTimeInMs, Action followUpAction = null)
        {
            if (steps == 0)
                return;

            StartSequence();
            for (int i = 0; i < steps - 1; ++i)
                AddTimedEvent(TimeSpan.FromMilliseconds(i * stepTimeInMs), stepAction);
            AddTimedEvent(TimeSpan.FromMilliseconds((steps - 1) * stepTimeInMs), () =>
            {
                stepAction?.Invoke();
                EndSequence();
                followUpAction?.Invoke();
            });
        }

        internal void Wait(uint hours)
        {
            if (hours != 0)
                GameTime.Wait(hours);
        }

        bool CanPartyMove() => !PartyMembers.Any(p => !p.CanMove(false));

        internal void Move(bool fromNumpadButton, float speedFactor3D, params CursorType[] cursorTypes)
        {
            if (is3D)
            {
                bool moveForward = cursorTypes.Contains(CursorType.ArrowForward);
                bool moveBackward = cursorTypes.Contains(CursorType.ArrowBackward);
                bool turnLeft = moveForward ? cursorTypes.Contains(CursorType.ArrowTurnLeft) : cursorTypes.Contains(CursorType.ArrowRotateLeft);
                bool turnRight = moveForward ? cursorTypes.Contains(CursorType.ArrowTurnRight) : cursorTypes.Contains(CursorType.ArrowRotateRight);

                if (CanPartyMove())
                {
                    bool strafeLeft = cursorTypes.Contains(CursorType.ArrowStrafeLeft);
                    bool strafeRight = cursorTypes.Contains(CursorType.ArrowStrafeRight);

                    if (moveForward)
                    {
                        if (strafeLeft || turnLeft)
                        {
                            player3D.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                        else if (strafeRight || turnRight)
                        {
                            player3D.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                        else
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                    }
                    else if (moveBackward)
                    {
                        if (strafeLeft || turnLeft)
                        {
                            player3D.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                        else if (strafeRight || turnRight)
                        {
                            player3D.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                        else
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                    }
                    else if (cursorTypes.Contains(CursorType.ArrowStrafeLeft))
                        player3D.MoveLeft(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                    else if (cursorTypes.Contains(CursorType.ArrowStrafeRight))
                        player3D.MoveRight(movement.MoveSpeed3D * Global.DistancePerBlock * speedFactor3D, CurrentTicks);
                }

                if (!moveForward && !moveBackward)
                {
                    if (cursorTypes.Contains(CursorType.ArrowTurnLeft))
                    {
                        player3D.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        if (!fromNumpadButton && CanPartyMove())
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else if (cursorTypes.Contains(CursorType.ArrowTurnRight))
                    {
                        player3D.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                        if (!fromNumpadButton && CanPartyMove())
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                    }
                    else if (cursorTypes.Contains(CursorType.ArrowRotateLeft))
                    {
                        if (fromNumpadButton)
                        {
                            PlayTimedSequence(12, () => player3D.TurnLeft(15.0f), 65);
                        }
                        else
                        {
                            player3D.TurnLeft(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            if (CanPartyMove())
                                player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                    }
                    else if (cursorTypes.Contains(CursorType.ArrowRotateRight))
                    {
                        if (fromNumpadButton)
                        {
                            PlayTimedSequence(12, () => player3D.TurnRight(15.0f), 65);
                        }
                        else
                        {
                            player3D.TurnRight(movement.TurnSpeed3D * 0.7f * speedFactor3D);
                            if (CanPartyMove())
                                player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f * speedFactor3D, CurrentTicks, true);
                        }
                    }
                }

                if (cursorTypes.Length == 1 && (cursorTypes[0] < CursorType.ArrowForward || cursorTypes[0] > CursorType.Wait))
                {
                    clickMoveActive = false;
                    UntrapMouse();
                }

                player.Direction = player3D.Direction;
            }
            else
            {
                switch (cursorTypes[0])
                {
                    case CursorType.ArrowUpLeft:
                        Move2D(-1, -1);
                        break;
                    case CursorType.ArrowUp:
                        Move2D(0, -1);
                        break;
                    case CursorType.ArrowUpRight:
                        Move2D(1, -1);
                        break;
                    case CursorType.ArrowLeft:
                        Move2D(-1, 0);
                        break;
                    case CursorType.ArrowRight:
                        Move2D(1, 0);
                        break;
                    case CursorType.ArrowDownLeft:
                        Move2D(-1, 1);
                        break;
                    case CursorType.ArrowDown:
                        Move2D(0, 1);
                        break;
                    case CursorType.ArrowDownRight:
                        Move2D(1, 1);
                        break;
                    default:
                        clickMoveActive = false;
                        break;
                }

                player.Direction = player2D.Direction;
            }
        }

        bool Move2D(int x, int y)
        {
            if (!CanPartyMove())
                return false;

            bool Move()
            {
                bool diagonal = x != 0 && y != 0;

                if (!player2D.Move(x, y, CurrentTicks, TravelType, out bool eventTriggered, !diagonal, null, !diagonal))
                {
                    if (eventTriggered || !diagonal)
                        return false;

                    var prevDirection = player2D.Direction;

                    if (!player2D.Move(0, y, CurrentTicks, TravelType, out eventTriggered, false, prevDirection, false))
                    {
                        if (eventTriggered)
                            return false;

                        return player2D.Move(x, 0, CurrentTicks, TravelType, out _, true, prevDirection);
                    }
                }

                return true;
            }

            bool result = Move();

            if (result)
                GameTime.MoveTick(Map, travelType);

            return result;
        }

        void Move()
        {
            if (paused || WindowActive || !InputEnable || allInputDisabled || pickingNewLeader || pickingTargetPlayer || pickingTargetInventory)
                return;

            bool left = keys[(int)Key.Left] || keys[(int)Key.A];
            bool right = keys[(int)Key.Right] || keys[(int)Key.D];
            bool up = keys[(int)Key.Up] || keys[(int)Key.W];
            bool down = keys[(int)Key.Down] || keys[(int)Key.S];

            if (left && !right)
            {
                if (!is3D)
                {
                    // diagonal movement is handled in up/down
                    if (!up && !down)
                        Move2D(-1, 0);
                }
                else
                    player3D.TurnLeft(movement.TurnSpeed3D);
            }
            if (right && !left)
            {
                if (!is3D)
                {
                    // diagonal movement is handled in up/down
                    if (!up && !down)
                        Move2D(1, 0);
                }
                else
                    player3D.TurnRight(movement.TurnSpeed3D);
            }
            if (up && !down)
            {
                if (!is3D)
                {
                    int x = left && !right ? -1 :
                        right && !left ? 1 : 0;
                    Move2D(x, -1);
                }
                else
                    player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
            }
            if (down && !up)
            {
                if (!is3D)
                {
                    int x = left && !right ? -1 :
                        right && !left ? 1 : 0;
                    Move2D(x, 1);
                }
                else
                    player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
            }
        }

        internal void SpeakToParty()
        {
            var hero = GetPartyMember(0);

            if (!hero.Alive || !hero.Ailments.CanTalk())
            {
                ShowMessagePopup(DataNameProvider.UnableToTalk);
                return;
            }
            if (CurrentSavegame.ActivePartyMemberSlot != 0)
                SetActivePartyMember(0);

            layout.OpenTextPopup(ProcessText(DataNameProvider.WhoToTalkTo),
                null, true, false, false, TextAlign.Center);
            PickTargetPlayer();
            void TargetPlayerPicked(int characterSlot)
            {
                targetPlayerPicked -= TargetPlayerPicked;
                ClosePopup();
                UntrapMouse();
                InputEnable = true;

                if (characterSlot != -1)
                {
                    if (characterSlot == 0)
                        ExecuteNextUpdateCycle(() => ShowMessagePopup(DataNameProvider.SelfTalkingIsMad));
                    else
                    {
                        var partyMember = GetPartyMember(characterSlot);
                        ExecuteNextUpdateCycle(() => ShowConversation(partyMember, null, new ConversationItems()));
                    }
                }
            }
            targetPlayerPicked += TargetPlayerPicked;
        }

        void PickTargetPlayer()
        {
            pickingTargetPlayer = true;
            CursorType = CursorType.Sword;
            TrapMouse(Global.PartyMemberPortraitArea);
        }

        void PickTargetInventory()
        {
            pickingTargetInventory = true;
            CursorType = CursorType.Sword;
            TrapMouse(Global.PartyMemberPortraitArea);
        }

        internal void FinishPickingTargetPlayer(int characterSlot)
        {
            targetPlayerPicked?.Invoke(characterSlot);
            pickingTargetPlayer = false;
            UntrapMouse();
        }

        internal void AbortPickingTargetPlayer()
        {
            pickingTargetPlayer = false;
            targetPlayerPicked?.Invoke(-1);
        }

        internal bool FinishPickingTargetInventory(int characterSlot)
        {
            bool result = targetInventoryPicked?.Invoke(characterSlot) ?? true;

            if (!result)
            {
                pickingTargetInventory = false;

                if (currentWindow.Window == Window.Inventory)
                    CloseWindow();

                layout.ShowChestMessage(null);
                UntrapMouse();
            }

            return result;
        }

        internal void FinishPickingTargetInventory(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
        {
            pickingTargetInventory = false;

            if (targetItemPicked?.Invoke(itemGrid, slotIndex, itemSlot) != false)
            {
                if (currentWindow.Window == Window.Inventory)
                    CloseWindow();

                layout.ShowChestMessage(null);
                UntrapMouse();
            }
        }

        internal void AbortPickingTargetInventory()
        {
            pickingTargetInventory = false;

            if (targetInventoryPicked?.Invoke(-1) != false)
            {
                if (targetItemPicked?.Invoke(null, 0, null) != false)
                {
                    if (currentWindow.Window == Window.Inventory)
                        CloseWindow();

                    layout.ShowChestMessage(null);
                    EndSequence();
                    UntrapMouse();
                }
            }
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            if (characterCreator != null)
            {
                characterCreator.OnKeyDown(key, modifiers);
                return;
            }

            if (outro?.Active == true)
            {
                if (key == Key.Escape)
                    outro.Abort();

                return;
            }

            if (allInputDisabled || pickingNewLeader || GameOverButtonsVisible)
                return;

            if (currentBattle != null && !currentBattle.RoundActive && trapMouseArea != null)
            {
                if (key == Key.Escape)
                    CancelSpecificPlayerAction();
                return;
            }

            if (!InputEnable)
            {
                if (layout.PopupActive)
                {
                    layout.KeyDown(key, modifiers);
                    return;
                }

                // In battle the space key can be used to click for next action.
                if (key == Key.Space && BattleRoundActive && currentBattle?.WaitForClick == true)
                {
                    currentBattle.Click(CurrentBattleTicks);
                    return;
                }

                if (key != Key.Escape && !(key >= Key.Num1 && key <= Key.Num9))
                    return;
            }

            if (pickingTargetPlayer)
            {
                if (key == Key.Escape)
                    AbortPickingTargetPlayer();
                return;
            }
            if (pickingTargetInventory)
            {
                if (key == Key.Escape)
                    AbortPickingTargetInventory();                
                return;
            }

            keys[(int)key] = true;

            if (!WindowActive && !layout.PopupActive)
                Move();
            else if (currentWindow.Window == Window.BattlePositions && battlePositionDragging)
                return;
            else if (trapMouseArea != null && (currentWindow.Window == Window.Merchant ||
                currentWindow.Window == Window.Healer || currentWindow.Window == Window.Sage ||
                currentWindow.Window == Window.Blacksmith || currentWindow.Window == Window.Enchanter ||
                currentWindow.Window == Window.Door || (currentWindow.Window == Window.Chest && OpenStorage == null)))
                return;

            switch (key)
            {
                case Key.Escape:
                {
                    if (ingame)
                    {
                        if (layout.PopupActive)
                        {
                            if (TextInput.FocusedInput != null)
                                TextInput.FocusedInput.KeyDown(key);
                            else
                                layout.ClosePopup();
                        }
                        else
                        {
                            if (layout.IsDragging)
                            {
                                layout.CancelDrag();
                                CursorType = CursorType.Sword;
                            }
                            else if (currentWindow.Closable)
                                layout.PressButton(2, CurrentTicks);
                            else if (!WindowActive && !is3D)
                            {
                                if (CursorType == CursorType.Eye ||
                                    CursorType == CursorType.Mouth ||
                                    CursorType == CursorType.Hand ||
                                    CursorType == CursorType.Target)
                                {
                                    CursorType = CursorType.Sword;
                                    UpdateCursor(lastMousePosition, MouseButtons.None);
                                }
                            }
                        }
                    }

                    break;
                }
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                    if (!layout.PopupActive && !layout.IsDragging)
                        OpenPartyMember(key - Key.F1, currentWindow.Window != Window.Stats);
                    break;
                case Key.Num1:
                case Key.Num2:
                case Key.Num3:
                case Key.Num4:
                case Key.Num5:
                case Key.Num6:
                case Key.Num7:
                case Key.Num8:
                case Key.Num9:
                {
                    if (layout.PopupDisableButtons || layout.IsDragging || layout.InventoryMessageWaitsForClick)
                        break;

                    int index = key - Key.Num1;
                    int column = index % 3;
                    int row = 2 - index / 3;
                    var newCursorType = layout.PressButton(column + row * 3, CurrentTicks);

                    if (newCursorType != null)
                        CursorType = newCursorType.Value;

                    break;
                }
                default:
                    if (WindowActive || layout.PopupActive)
                        layout.KeyDown(key, modifiers);
                    else if (key == Key.Return)
                        ToggleButtonGridPage();
                    break;
            }

            lastMoveTicksReset = CurrentTicks;
        }

        internal void ToggleButtonGridPage() => layout.ToggleButtonGridPage();

        public void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            if (characterCreator != null || allInputDisabled || pickingTargetPlayer || pickingTargetInventory)
                return;

            if (!InputEnable || pickingNewLeader)
            {
                if (key != Key.Escape && !(key >= Key.Num1 && key <= Key.Num9))
                    return;
            }

            keys[(int)key] = false;

            switch (key)
            {
                case Key.Num1:
                case Key.Num2:
                case Key.Num3:
                case Key.Num4:
                case Key.Num5:
                case Key.Num6:
                case Key.Num7:
                case Key.Num8:
                case Key.Num9:
                {
                    int index = key - Key.Num1;
                    int column = index % 3;
                    int row = 2 - index / 3;
                    layout.ReleaseButton(column + row * 3);

                    break;
                }
            }
        }

        public void OnKeyChar(char keyChar)
        {
            if (characterCreator != null)
            {
                characterCreator.OnKeyChar(keyChar);
                return;
            }

            if (allInputDisabled)
                return;

            if (keyChar >= '1' && keyChar <= '6')
            {
                int slot = keyChar - '1';

                if (keys[(int)Key.Num1 + slot])
                    return;

                var partyMember = GetPartyMember(slot);

                if (pickingTargetPlayer)
                {
                    if (partyMember != null)
                        FinishPickingTargetPlayer(slot);
                    return;
                }
                if (pickingTargetInventory)
                {
                    if (partyMember != null)
                        layout.TargetInventoryPlayerSelected(slot, partyMember);
                    return;
                }
            }

            if (!pickingNewLeader && layout.KeyChar(keyChar))
                return;

            if (!InputEnable)
                return;

            if (keyChar >= '1' && keyChar <= '6')
            {
                SetActivePartyMember(keyChar - '1');
            }
        }

        public void OnMouseUp(Position cursorPosition, MouseButtons buttons)
        {
            if (characterCreator != null)
            {
                characterCreator.OnMouseUp(cursorPosition, buttons);
                return;
            }

            lastMousePosition = new Position(cursorPosition);

            if (allInputDisabled)
            {
                layout.ClearLeftUpIgnoring();
                return;
            }

            var position = renderView.ScreenToGame(GetMousePosition(cursorPosition));

            if (currentBattle != null && buttons == MouseButtons.Right)
            {
                if (CheckBattleRightClick())
                    return;
            }

            if (buttons.HasFlag(MouseButtons.Right))
            {
                layout.RightMouseUp(position, out CursorType? cursorType, CurrentTicks);

                if (cursorType != null)
                    CursorType = cursorType.Value;
                else if (is3D && !WindowActive && !layout.PopupActive && CursorType == CursorType.Target)
                    CursorType = CursorType.Wait;
            }

            if (buttons.HasFlag(MouseButtons.Left))
            {
                if (clickMoveActive)
                {
                    clickMoveActive = false;

                    if (is3D)
                        UntrapMouse();
                }

                layout.LeftMouseUp(position, out CursorType? cursorType, CurrentTicks);

                if (trapMouseArea != null)
                    disableUntrapping = true;

                if (cursorType != null && cursorType != CursorType.None)
                    CursorType = cursorType.Value;
                else // Note: Don't use cursorPosition here as trapping might have updated it
                    UpdateCursor(GetMousePosition(lastMousePosition), MouseButtons.None);

                disableUntrapping = false;
            }

            if (TextInput.FocusedInput != null)
                CursorType = CursorType.None;
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (characterCreator != null)
            {
                characterCreator.OnMouseDown(position, buttons);
                return;
            }

            if (outro?.Active == true)
            {
                outro.Click(buttons == MouseButtons.Right);
                return;
            }

            lastMousePosition = new Position(position);

            if (allInputDisabled)
                return;

            if (nextClickHandler != null)
            {
                if (nextClickHandler(buttons))
                {
                    nextClickHandler = null;
                    return;
                }
            }

            position = GetMousePosition(position);

            if (ingame)
            {
                var relativePosition = renderView.ScreenToGame(position);

                if (!WindowActive && !layout.PopupActive && InputEnable && !pickingNewLeader &&
                    !pickingTargetPlayer && !pickingTargetInventory && mapViewArea.Contains(relativePosition))
                {
                    // click into the map area
                    if (buttons == MouseButtons.Right)
                    {
                        if (is3D)
                        {
                            switch (CursorType)
                            {
                                case CursorType.ArrowTurnLeft:
                                    PlayTimedSequence(6, () => player3D.TurnLeft(15.0f), 65);
                                    return;
                                case CursorType.ArrowTurnRight:
                                    PlayTimedSequence(6, () => player3D.TurnRight(15.0f), 65);
                                    return;
                                case CursorType.ArrowRotateLeft:
                                    PlayTimedSequence(12, () => player3D.TurnLeft(15.0f), 65);
                                    return;
                                case CursorType.ArrowRotateRight:
                                    PlayTimedSequence(12, () => player3D.TurnRight(15.0f), 65);
                                    return;
                                case CursorType.Wait:
                                    CursorType = CursorType.Target;
                                    TriggerMapEvents(null);
                                    return;
                            }
                        }
                        else if (CursorType > CursorType.Sword && CursorType <= CursorType.Wait)
                        {
                            Determine2DTargetMode(position);
                            targetMode2DActive = true;
                            return;
                        }

                        if (cursor.Type > CursorType.Wait)
                            CursorType = CursorType.Sword;
                    }
                    if (!buttons.HasFlag(MouseButtons.Left))
                        return;

                    relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);
                    var previousCursor = cursor.Type;

                    if (cursor.Type == CursorType.Eye)
                        TriggerMapEvents(EventTrigger.Eye, relativePosition);
                    else if (cursor.Type == CursorType.Hand)
                        TriggerMapEvents(EventTrigger.Hand, relativePosition);
                    else if (cursor.Type == CursorType.Mouth)
                    {
                        if (!TriggerMapEvents(EventTrigger.Mouth, relativePosition))
                        {
                            if (!is3D && player2D?.DisplayArea.Contains(mapViewArea.Position + relativePosition) == true)
                            {
                                SpeakToParty();
                            }
                        }
                    }
                    else if (cursor.Type == CursorType.Target && !is3D)
                    {
                        if (!TriggerMapEvents(EventTrigger.Mouth, relativePosition))
                        {
                            if (!TriggerMapEvents(EventTrigger.Eye, relativePosition))
                            {
                                TriggerMapEvents(EventTrigger.Hand, relativePosition);
                            }
                        }
                    }
                    else if (cursor.Type == CursorType.Wait)
                    {
                        GameTime.Tick();
                    }
                    else if (cursor.Type > CursorType.Sword && cursor.Type < CursorType.Wait)
                    {
                        if (is3D)
                            TrapMouse(Map3DViewArea);
                        clickMoveActive = true;
                        lastMoveTicksReset = CurrentTicks;
                        HandleClickMovement();
                    }

                    if (cursor.Type > CursorType.Wait)
                    {
                        if (cursor.Type != CursorType.Click || previousCursor == CursorType.Click)
                            CursorType = CursorType.Sword;
                    }
                    return;
                }
                else
                {
                    if (!pickingNewLeader && currentWindow.Window == Window.Battle)
                    {
                        if (currentBattle?.WaitForClick == true)
                        {
                            CursorType = CursorType.Sword;
                            currentBattle.Click(CurrentBattleTicks);
                            return;
                        }
                        else
                        {
                            currentBattle.ResetClick();
                        }
                    }

                    var cursorType = CursorType.Sword;
                    layout.Click(relativePosition, buttons, ref cursorType, CurrentTicks, pickingNewLeader, pickingTargetPlayer, pickingTargetInventory);
                    disableUntrapping = true;
                    CursorType = cursorType;

                    if (!allInputDisabled && InputEnable && !pickingNewLeader && !pickingTargetPlayer && !pickingTargetInventory)
                    {
                        layout.Hover(relativePosition, ref cursorType); // Update cursor
                        if (cursor.Type != CursorType.None)
                            CursorType = cursorType;
                    }

                    disableUntrapping = false;
                }
            }
            else
            {
                CursorType = CursorType.Sword;
            }

            if (TextInput.FocusedInput != null)
                CursorType = CursorType.None;
        }

        void Determine2DTargetMode(Position cursorPosition)
        {
            var gamePosition = renderView.ScreenToGame(GetMousePosition(cursorPosition));
            var playerArea = player2D.DisplayArea;

            int xDiff = gamePosition.X < playerArea.Left ? playerArea.Left - gamePosition.X : gamePosition.X - playerArea.Right;
            int yDiff = gamePosition.Y < playerArea.Top ? playerArea.Top - gamePosition.Y : gamePosition.Y - playerArea.Bottom;

            if (xDiff <= RenderMap2D.TILE_WIDTH && yDiff <= RenderMap2D.TILE_HEIGHT)
                CursorType = CursorType.Target;
            else
                CursorType = CursorType.Mouth;
        }

        public void PreFullscreenChanged()
        {
            preFullscreenMousePosition = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
            preFullscreenChangeTrapMouseArea = trapMouseGameArea;
            if (trapMouseGameArea != null)
                UntrapMouse();
        }

        public void PostFullscreenChanged()
        {
            lastMousePosition = renderView.GameToScreen(preFullscreenMousePosition);

            if (preFullscreenChangeTrapMouseArea != null)
            {
                TrapMouse(preFullscreenChangeTrapMouseArea);
                preFullscreenChangeTrapMouseArea = null;
            }
            else
            {
                MousePositionChanged?.Invoke(lastMousePosition);
            }
        }

        internal void UpdateCursor()
        {
            UpdateCursor(lastMousePosition, MouseButtons.None);
        }

        void UpdateCursor(Position cursorPosition, MouseButtons buttons)
        {
            lock (cursor)
            {
                cursor.UpdatePosition(cursorPosition, this);

                if (!InputEnable)
                {
                    if (layout.FreeTextScrollingActive)
                    {
                        CursorType = CursorType.None;
                    }
                    else if (layout.PopupActive)
                    {
                        var cursorType = layout.PopupClickCursor ? CursorType.Click : CursorType.Sword;
                        layout.Hover(renderView.ScreenToGame(cursorPosition), ref cursorType);
                        CursorType = cursorType;
                    }
                    else if ((layout.Type == LayoutType.Event && !GameOverButtonsVisible) ||
                        (ConversationTextActive && layout.Type == LayoutType.Conversation) ||
                        (currentBattle?.RoundActive == true && currentBattle?.ReadyForNextAction == true) ||
                        currentBattle?.WaitForClick == true ||
                        layout.ChestText?.WithScrolling == true ||
                        layout.InventoryMessageWaitsForClick)
                        CursorType = CursorType.Click;
                    else
                        CursorType = CursorType.Sword;

                    if (layout.IsDragging && layout.InventoryMessageWaitsForClick &&
                        buttons == MouseButtons.None)
                    {
                        layout.UpdateDraggedItemPosition(renderView.ScreenToGame(cursorPosition));
                    }

                    if (layout.OptionMenuOpen && !layout.PopupActive)
                        layout.HoverButtonGrid(renderView.ScreenToGame(cursorPosition));

                    return;
                }

                var relativePosition = renderView.ScreenToGame(cursorPosition);

                if (!WindowActive && !layout.PopupActive && (mapViewArea.Contains(relativePosition) || clickMoveActive))
                {
                    // Change arrow cursors when hovering the map
                    if (ingame && cursor.Type >= CursorType.Sword && cursor.Type <= CursorType.Wait)
                    {
                        if (Map.Type == MapType.Map2D)
                        {
                            var playerArea = player2D.DisplayArea;
                            playerArea.Position.Y = playerArea.Bottom - RenderMap2D.TILE_HEIGHT;
                            playerArea.Size.Height = RenderMap2D.TILE_HEIGHT;

                            bool left = relativePosition.X < playerArea.Left;
                            bool right = relativePosition.X >= playerArea.Right;
                            bool up = relativePosition.Y < playerArea.Top;
                            bool down = relativePosition.Y >= playerArea.Bottom;

                            if (up)
                            {
                                if (left)
                                    CursorType = CursorType.ArrowUpLeft;
                                else if (right)
                                    CursorType = CursorType.ArrowUpRight;
                                else
                                    CursorType = CursorType.ArrowUp;
                            }
                            else if (down)
                            {
                                if (left)
                                    CursorType = CursorType.ArrowDownLeft;
                                else if (right)
                                    CursorType = CursorType.ArrowDownRight;
                                else
                                    CursorType = CursorType.ArrowDown;
                            }
                            else
                            {
                                if (left)
                                    CursorType = CursorType.ArrowLeft;
                                else if (right)
                                    CursorType = CursorType.ArrowRight;
                                else
                                    CursorType = CursorType.Wait;
                            }
                        }
                        else
                        {
                            relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);

                            int horizontal = relativePosition.X / (mapViewArea.Width / 3);
                            int vertical = relativePosition.Y / (mapViewArea.Height / 3);

                            if (vertical <= 0) // up
                            {
                                if (horizontal <= 0) // left
                                    CursorType = CursorType.ArrowTurnLeft;
                                else if (horizontal >= 2) // right
                                    CursorType = CursorType.ArrowTurnRight;
                                else
                                    CursorType = CursorType.ArrowForward;
                            }
                            else if (vertical >= 2) // down
                            {
                                if (horizontal <= 0) // left
                                    CursorType = CursorType.ArrowRotateLeft;
                                else if (horizontal >= 2) // right
                                    CursorType = CursorType.ArrowRotateRight;
                                else
                                    CursorType = CursorType.ArrowBackward;
                            }
                            else
                            {
                                if (horizontal <= 0) // left
                                    CursorType = CursorType.ArrowStrafeLeft;
                                else if (horizontal >= 2) // right
                                    CursorType = CursorType.ArrowStrafeRight;
                                else
                                    CursorType = CursorType.Wait;
                            }
                        }

                        return;
                    }
                }
                else
                {
                    if (buttons == MouseButtons.None && !allInputDisabled)
                    {
                        var cursorType = cursor.Type;
                        layout.Hover(relativePosition, ref cursorType);
                        CursorType = cursorType;
                    }
                    else if (buttons == MouseButtons.Left)
                    {
                        var cursorType = cursor.Type;
                        layout.Drag(relativePosition, ref cursorType);
                        CursorType = cursorType;
                    }
                }

                if (cursor.Type >= CursorType.ArrowUp && cursor.Type <= CursorType.Wait)
                    CursorType = CursorType.Sword;
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            if (outro?.Active != true && !InputEnable && !layout.PopupActive)
                UntrapMouse();

            if (trapped)
            {
                var trappedPosition = position + trappedMousePositionOffset;

                if (trappedPosition.X < trapMouseArea.Left)
                {
                    if (position.X < lastMousePosition.X)
                        trappedMousePositionOffset.X += lastMousePosition.X - position.X;
                }                    
                else if (trappedPosition.X >= trapMouseArea.Right)
                {
                    if (position.X > lastMousePosition.X)
                        trappedMousePositionOffset.X -= position.X - lastMousePosition.X;
                }

                if (trappedPosition.Y < trapMouseArea.Top)
                {
                    if (position.Y < lastMousePosition.Y)
                        trappedMousePositionOffset.Y += lastMousePosition.Y - position.Y;
                }
                else if (trappedPosition.Y >= trapMouseArea.Bottom)
                {
                    if (position.Y > lastMousePosition.Y)
                        trappedMousePositionOffset.Y -= position.Y - lastMousePosition.Y;
                }

                if (!WindowActive && !layout.PopupActive && !is3D && targetMode2DActive)
                {
                    Determine2DTargetMode(position);
                }
            }

            if (outro?.Active == true)
            {
                lastMousePosition = new Position(position);
                CursorType = CursorType.None;
            }
            else
            {
                layout.MouseMoved(position - lastMousePosition);

                lastMousePosition = new Position(position);
                position = GetMousePosition(position);
                UpdateCursor(position, buttons);
            }
        }

        public void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
        {
            if (characterCreator != null)
            {
                characterCreator.OnMouseWheel(xScroll, yScroll, mousePosition);
                return;
            }

            if (allInputDisabled)
                return;

            bool scrolled = false;

            if (xScroll != 0)
                scrolled = layout.ScrollX(xScroll < 0);
            if (yScroll != 0 && layout.ScrollY(yScroll < 0))
                scrolled = true;

            if (scrolled)
            {
                mousePosition = GetMousePosition(mousePosition);
                UpdateCursor(mousePosition, MouseButtons.None);
            }
            else if (yScroll != 0 && !WindowActive && !layout.PopupActive && !is3D)
            {
                ScrollCursor(mousePosition, yScroll < 0);
            }
        }

        void ScrollCursor(Position cursorPosition, bool down)
        {
            if (down)
            {
                if (CursorType < CursorType.Eye)
                    CursorType = CursorType.Eye;
                else if (CursorType == CursorType.Eye)
                    CursorType = CursorType.Mouth;
                else if (CursorType == CursorType.Mouth)
                    CursorType = CursorType.Hand;
                else if (CursorType == CursorType.Hand)
                {
                    CursorType = CursorType.Sword;
                    UpdateCursor(cursorPosition, MouseButtons.None);
                    return;
                }
                else
                    return;
            }
            else // up
            {
                if (CursorType < CursorType.Eye)
                    CursorType = CursorType.Hand;
                else if (CursorType == CursorType.Eye)
                {
                    CursorType = CursorType.Sword;
                    UpdateCursor(cursorPosition, MouseButtons.None);
                    return;
                }
                else if (CursorType == CursorType.Mouth)
                    CursorType = CursorType.Eye;
                else if (CursorType == CursorType.Hand)
                    CursorType = CursorType.Mouth;
                else
                    return;
            }
        }

        public IEnumerable<PartyMember> PartyMembers => Enumerable.Range(0, MaxPartyMembers)
            .Select(i => GetPartyMember(i)).Where(p => p != null);
        public PartyMember GetPartyMember(int slot) => CurrentSavegame.GetPartyMember(slot);
        internal Chest GetChest(uint index) => CurrentSavegame.Chests[index];
        internal Chest GetInitialChest(uint index)
        {
            if (initialChests == null)
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

            return initialChests?[index];
        }
        internal Merchant GetMerchant(uint index) => CurrentSavegame.Merchants[index];

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
                return TriggerMapEvents(trigger, (uint)tilePosition.X, (uint)tilePosition.Y);
            }
        }

        bool TriggerMapEvents(EventTrigger trigger, uint x, uint y)
        {
            if (noEvents)
                return false;

            if (is3D)
            {
                return renderMap3D.TriggerEvents(this, trigger, x, y, CurrentTicks, CurrentSavegame);
            }
            else // 2D
            {
                return renderMap2D.TriggerEvents(player2D, trigger, x, y, MapManager,
                    CurrentTicks, CurrentSavegame);
            }
        }

        internal bool TestUseItemMapEvent(uint itemIndex)
        {
            uint x = (uint)player.Position.X;
            uint y = (uint)player.Position.Y;
            var @event = is3D ? Map.GetEvent(x, y, CurrentSavegame) : renderMap2D.GetEvent(x, y, CurrentSavegame);

            if (@event is ConditionEvent conditionEvent &&
                conditionEvent.TypeOfCondition == ConditionEvent.ConditionType.UseItem &&
                conditionEvent.ObjectIndex == itemIndex)
            {
                return true;
            }

            var mapWidth = Map.IsWorldMap ? int.MaxValue : Map.Width;
            var mapHeight = Map.IsWorldMap ? int.MaxValue : Map.Height;

            if (is3D)
            {
                camera3D.GetForwardPosition(Global.DistancePerBlock, out float px, out float pz, false, false);
                var position = Geometry.Geometry.CameraToBlockPosition(Map, px, pz);

                if (position != player.Position &&
                    position.X >= 0 && position.X < Map.Width &&
                    position.Y >= 0 && position.Y < Map.Height)
                {
                    @event = Map.GetEvent((uint)position.X, (uint)position.Y, CurrentSavegame);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                switch (player.Direction)
                {
                    case CharacterDirection.Left:
                        if (x == 0)
                            return false;
                        --x;
                        break;
                    case CharacterDirection.Right:
                        if (x == mapWidth - 1)
                            return false;
                        ++x;
                        break;
                    case CharacterDirection.Up:
                        if (y == 0)
                            return false;
                        --y;
                        break;
                    case CharacterDirection.Down:
                        if (y == mapHeight - 1)
                            return false;
                        ++y;
                        break;
                }

                @event = renderMap2D.GetEvent(x, y, CurrentSavegame);
            }

            return  @event is ConditionEvent adjacentConditionEvent &&
                    adjacentConditionEvent.TypeOfCondition == ConditionEvent.ConditionType.UseItem &&
                    adjacentConditionEvent.ObjectIndex == itemIndex;
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

        public void UpdateCharacterBars()
        {
            if (!ingame || layout == null || CurrentSavegame == null)
                return;

            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                layout.FillCharacterBars(i, GetPartyMember(i));
            }
        }

        void UpdateMapName()
        {
            string mapName = Map.IsWorldMap
                ? DataNameProvider.GetWorldName(Map.World)
                : Map.Name;
            windowTitle.Text = renderView.TextProcessor.CreateText(mapName);
            windowTitle.PaletteIndex = UIPaletteIndex;
            windowTitle.TextColor = TextColor.BrightGray;
        }

        void UpdateUIPalette(bool map)
        {
            if (map)
            {
                // TODO: MapFlags.SecondaryUI2D / MapFlags.SecondaryUI3D?
                currentUIPaletteIndex = (byte)(Map.PaletteIndex - 1);
            }
            else
            {
                currentUIPaletteIndex = PrimaryUIPaletteIndex;
            }

            ouchSprite.PaletteIndex = currentUIPaletteIndex;
            layout.UpdateUIPalette(currentUIPaletteIndex);
            cursor.UpdatePalette(this);
        }

        void ShowMap(bool show)
        {
            layout.HideTooltip();

            if (show)
            {
                UpdateUIPalette(true);
                currentBattle = null;
                layout.CancelDrag();
                ResetCursor();
                OpenStorage = null;
                UpdateMapName();
                Resume();
                ResetMoveKeys();
                UpdateLight();
            }
            else
            {
                UpdateUIPalette(false);
                Pause();
            }

            windowTitle.Visible = show;

            if (is3D)
            {
                if (show)
                    layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));
                renderView.GetLayer(Layer.Map3DBackground).Visible = show;
                renderView.GetLayer(Layer.Map3D).Visible = show;
                renderView.GetLayer(Layer.Billboards3D).Visible = show;
            }
            else
            {
                if (show)
                    layout.SetLayout(LayoutType.Map2D, movement.MovementTicks(false, Map.IsWorldMap, TravelType));
                for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                    renderView.GetLayer((Layer)i).Visible = show;
            }

            if (show)
            {
                layout.Reset();
                mapViewRightFillArea = layout.FillArea(new Rect(208, 49, 96, 80), GetUIColor(28), false);
                SetWindow(Window.MapView);

                foreach (var specialItem in Enum.GetValues<SpecialItemPurpose>())
                {
                    if (CurrentSavegame.IsSpecialItemActive(specialItem))
                        layout.AddSpecialItem(specialItem);
                }

                foreach (var activeSpell in Enum.GetValues<ActiveSpellType>())
                {
                    if (CurrentSavegame.ActiveSpells[(int)activeSpell] != null)
                        layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell], false);
                }
            }
        }

        public void UpdateInventory()
        {
            if (CurrentWindow.Window == Window.Inventory)
            {
                layout.UpdateItemGrids();
                UpdateCharacterInfo();
            }
        }

        void SetInventoryWeightDisplay(PartyMember partyMember)
        {
            var weightArea = new Rect(27, 152, 68, 15);
            string weightText = string.Format(DataNameProvider.CharacterInfoWeightString,
                partyMember.TotalWeight / 1000, partyMember.MaxWeight / 1000);
            if (partyMember.Overweight)
            {
                weightDisplayBlinking = true;
                if (characterInfoTexts.ContainsKey(CharacterInfo.Weight))
                    characterInfoTexts[CharacterInfo.Weight]?.Destroy();
                characterInfoTexts[CharacterInfo.Weight] = AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align, 5),
                    weightArea.CreateModified(0, 8, 0, 0), weightText, TextAlign.Center, () => weightDisplayBlinking &&
                        CurrentWindow.Window == Window.Inventory, 50, false);
            }
            else
            {
                weightDisplayBlinking = false;
                ExecuteNextUpdateCycle(() =>
                {
                    if (characterInfoTexts.ContainsKey(CharacterInfo.Weight))
                        characterInfoTexts[CharacterInfo.Weight]?.Destroy();
                    characterInfoTexts[CharacterInfo.Weight] = layout.AddText(weightArea.CreateModified(0, 8, 0, 0),
                        weightText, TextColor.White, TextAlign.Center, 5);
                });
            }
        }

        internal bool OpenPartyMember(int slot, bool inventory, Action openedAction = null,
            bool changeInputEnableStateWhileFading = true)
        {
            if (CurrentSavegame.CurrentPartyMemberIndices[slot] == 0)
                return false;

            bool switchedFromOtherPartyMember = CurrentInventory != null;
            var partyMember = GetPartyMember(slot);
            bool canAccessInventory = !HasPartyMemberFled(partyMember) && partyMember.Ailments.CanOpenInventory();

            if (inventory && !canAccessInventory)
            {
                // When fled you can only access the stats.
                // When coming from inventory of another party member
                // you won't be able to open the inventory but if
                // you open the character with F1-F6 or right click
                // you will enter the stats window instead.
                if (switchedFromOtherPartyMember)
                    return false;
                else
                    inventory = false;
            }

            void OpenInventory()
            {
                CurrentInventoryIndex = slot;
                var partyMember = GetPartyMember(slot);

                layout.Reset(switchedFromOtherPartyMember);
                ShowMap(false);
                SetWindow(Window.Inventory, slot);
                layout.SetLayout(LayoutType.Inventory);

                // As the inventory can be opened from the healer (which displays the healing symbol)
                // we will update the portraits here to hide it.
                SetActivePartyMember(SlotFromPartyMember(CurrentPartyMember).Value, false);

                windowTitle.Text = renderView.TextProcessor.CreateText(DataNameProvider.InventoryTitleString);
                windowTitle.PaletteIndex = UIPaletteIndex;
                windowTitle.TextColor = TextColor.White;
                windowTitle.Visible = true;

                #region Equipment and Inventory
                var equipmentSlotPositions = new List<Position>
                {
                    new Position(20, 72),  new Position(52, 72),  new Position(84, 72),
                    new Position(20, 124), new Position(84, 97), new Position(84, 124),
                    new Position(20, 176), new Position(52, 176), new Position(84, 176),
                };
                var inventorySlotPositions = Enumerable.Range(0, Inventory.VisibleWidth * Inventory.VisibleHeight).Select
                (
                    slot => new Position(Global.InventoryX + (slot % Inventory.Width) * Global.InventorySlotWidth,
                        Global.InventoryY + (slot / Inventory.Width) * Global.InventorySlotHeight)
                ).ToList();
                var inventoryGrid = ItemGrid.CreateInventory(this, layout, slot, renderView, ItemManager,
                    inventorySlotPositions, partyMember.Inventory.Slots.ToList());
                layout.AddItemGrid(inventoryGrid);
                for (int i = 0; i < partyMember.Inventory.Slots.Length; ++i)
                {
                    if (!partyMember.Inventory.Slots[i].Empty)
                    {
                        if (partyMember.Inventory.Slots[i].ItemIndex == 0) // Item index 0 but amount is not 0 -> not allowed for inventory
                            partyMember.Inventory.Slots[i].Amount = 0;

                        inventoryGrid.SetItem(i, partyMember.Inventory.Slots[i]);
                    }
                }
                var equipmentGrid = ItemGrid.CreateEquipment(this, layout, slot, renderView, ItemManager,
                    equipmentSlotPositions, partyMember.Equipment.Slots.Values.ToList(), itemSlot =>
                    {
                        if (itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed))
                        {
                            layout.SetInventoryMessage(DataNameProvider.ItemIsCursed, true);
                            return false;
                        }

                        if (currentBattle != null)
                        {
                            var item = ItemManager.GetItem(itemSlot.ItemIndex);

                            if (!item.Flags.HasFlag(ItemFlags.RemovableDuringFight))
                            {
                                layout.SetInventoryMessage(DataNameProvider.CannotUnequipInFight, true);
                                return false;
                            }
                        }
                        return true;
                    });
                layout.AddItemGrid(equipmentGrid);
                foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>().Skip(1))
                {
                    if (!partyMember.Equipment.Slots[equipmentSlot].Empty)
                    {
                        if (equipmentSlot != EquipmentSlot.LeftHand &&
                            partyMember.Equipment.Slots[equipmentSlot].ItemIndex == 0) // Item index 0 but amount is not 0 -> only allowed for left hand
                            partyMember.Equipment.Slots[equipmentSlot].Amount = 0;

                        equipmentGrid.SetItem((int)equipmentSlot - 1, partyMember.Equipment.Slots[equipmentSlot]);
                    }
                }
                void RemoveEquipment(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    RecheckUsedBattleItem(CurrentInventoryIndex.Value, slotIndex, true);
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    EquipmentRemoved(item, amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                    if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                    {
                        equipmentGrid.SetItem(slotIndex + 2, null);
                        partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Clear();
                    }

                    // TODO: rings/fingers
                    UpdateCharacterInfo();
                }
                void AddEquipment(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    EquipmentAdded(item, amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                    if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                    {
                        var secondHandItemSlot = new ItemSlot { ItemIndex = 0, Amount = 1 };
                        equipmentGrid.SetItem((int)EquipmentSlot.LeftHand - 1, secondHandItemSlot);
                        partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Replace(secondHandItemSlot);
                    }

                    // TODO: rings/fingers
                    UpdateCharacterInfo();
                }
                void RemoveInventoryItem(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    RecheckUsedBattleItem(CurrentInventoryIndex.Value, slotIndex, false);
                    InventoryItemRemoved(ItemManager.GetItem(itemSlot.ItemIndex), amount);
                    UpdateCharacterInfo();
                }
                void AddInventoryItem(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    InventoryItemAdded(ItemManager.GetItem(itemSlot.ItemIndex), amount);
                    UpdateCharacterInfo();
                }
                equipmentGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
                {
                    RemoveEquipment(slotIndex, draggedItem, draggedAmount);
                    AddEquipment(slotIndex, droppedItem, droppedItem.Amount);
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager.GetItem(draggedItem.ItemIndex));
                };
                equipmentGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    RemoveEquipment(slotIndex, itemSlot, amount);
                    partyMember.Equipment.Slots[(EquipmentSlot)(slotIndex + 1)].Remove(amount);
                    // TODO: When resetting the item back to the slot (even just dropping it there) the previous battle action should be restored.
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager.GetItem(itemSlot.ItemIndex));
                };
                equipmentGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    AddEquipment(slotIndex, itemSlot, amount);
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), null);
                };
                inventoryGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
                {
                    RemoveInventoryItem(slotIndex, draggedItem, draggedAmount);
                    AddInventoryItem(slotIndex, droppedItem, droppedItem.Amount);
                };
                inventoryGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    RemoveInventoryItem(slotIndex, itemSlot, amount);
                    partyMember.Inventory.Slots[slotIndex].Remove(amount);
                };
                inventoryGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    AddInventoryItem(slotIndex, itemSlot, amount);
                };
                #endregion
                #region Character info
                DisplayCharacterInfo(partyMember, false);
                // Weight display
                var weightArea = new Rect(27, 152, 68, 15);
                layout.AddPanel(weightArea, 2);
                layout.AddText(weightArea.CreateModified(0, 1, 0, 0), DataNameProvider.CharacterInfoWeightHeaderString,
                    TextColor.White, TextAlign.Center, 5);
                SetInventoryWeightDisplay(partyMember);
                #endregion
            }

            void OpenCharacterStats()
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Stats, slot);
                layout.SetLayout(LayoutType.Stats);
                layout.EnableButton(0, canAccessInventory);
                layout.FillArea(new Rect(16, 49, 176, 145), GetUIColor(28), false);

                // As the stats can be opened from the healer (which displays the healing symbol)
                // we will update the portraits here to hide it.
                SetActivePartyMember(SlotFromPartyMember(CurrentPartyMember).Value, false);

                windowTitle.Visible = false;

                CurrentInventoryIndex = slot;
                var partyMember = GetPartyMember(slot);
                int index;

                #region Character info
                DisplayCharacterInfo(partyMember, false);
                #endregion
                #region Attributes
                layout.AddText(new Rect(22, 50, 72, Global.GlyphLineHeight), DataNameProvider.AttributesHeaderString, TextColor.LightGreen, TextAlign.Center);
                index = 0;
                foreach (var attribute in Enum.GetValues<Attribute>())
                {
                    if (attribute == Attribute.Age)
                        break;

                    int y = 57 + index++ * Global.GlyphLineHeight;
                    var attributeValues = partyMember.Attributes[attribute];
                    if (attribute == Attribute.AntiMagic && CurrentSavegame.IsSpellActive(ActiveSpellType.AntiMagic))
                    {
                        uint bonus = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.AntiMagic);
                        void AddAnimatedText(Rect area, string text)
                        {
                            this.AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align), area, text, TextAlign.Left,
                                () => CurrentWindow.Window == Window.Stats, 100, true);
                        }
                        AddAnimatedText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeShortName(attribute));
                        AddAnimatedText(new Rect(52, y, 42, Global.GlyphLineHeight),
                            (attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue+bonus:000}") + $"/{attributeValues.MaxValue:000}");
                    }
                    else
                    {
                        layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeShortName(attribute));
                        layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                            (attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}");
                    }
                }
                #endregion
                #region Abilities
                layout.AddText(new Rect(22, 115, 72, Global.GlyphLineHeight), DataNameProvider.AbilitiesHeaderString, TextColor.LightGreen, TextAlign.Center);
                index = 0;
                foreach (var ability in Enum.GetValues<Ability>())
                {
                    int y = 122 + index++ * Global.GlyphLineHeight;
                    var abilityValues = partyMember.Abilities[ability];
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAbilityShortName(ability));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (abilityValues.TotalCurrentValue > 99 ? "**" : $"{abilityValues.TotalCurrentValue:00}") + $"%/{abilityValues.MaxValue:00}%");
                }
                #endregion
                #region Languages
                layout.AddText(new Rect(106, 50, 72, Global.GlyphLineHeight), DataNameProvider.LanguagesHeaderString, TextColor.LightGreen, TextAlign.Center);
                index = 0;
                foreach (var language in Enum.GetValues<Language>().Skip(1)) // skip Language.None
                {
                    int y = 57 + index++ * Global.GlyphLineHeight;
                    bool learned = partyMember.SpokenLanguages.HasFlag(language);
                    if (learned)
                        layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), DataNameProvider.GetLanguageName(language));
                }
                #endregion
                #region Ailments
                layout.AddText(new Rect(106, 115, 72, Global.GlyphLineHeight), DataNameProvider.AilmentsHeaderString, TextColor.LightGreen, TextAlign.Center);
                index = 0;
                // Total space is 80 pixels wide. Each ailment icon is 16 pixels wide. So there is space for 5 ailment icons per line.
                const int ailmentsPerRow = 5;
                foreach (var ailment in partyMember.VisibleAilments)
                {
                    if (ailment == Ailment.DeadAshes || ailment == Ailment.DeadDust)
                        continue; // TODO: is dead corpse set if those are set

                    if (!partyMember.Ailments.HasFlag(ailment))
                        continue;

                    int column = index % ailmentsPerRow;
                    int row = index / ailmentsPerRow;
                    ++index;

                    int x = 96 + column * 16;
                    int y = 124 + row * 17;
                    layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetAilmentGraphicIndex(ailment), UIPaletteIndex,
                        2, DataNameProvider.GetAilmentName(ailment), ailment == Ailment.DeadCorpse ? TextColor.DeadPartyMember : TextColor.ActivePartyMember);
                }
                #endregion
            }

            Action openAction = inventory ? (Action)OpenInventory : OpenCharacterStats;

            if ((currentWindow.Window == Window.Inventory && inventory) ||
                (currentWindow.Window == Window.Stats && !inventory))
            {
                openAction();
                openedAction?.Invoke();
            }
            else
            {
                Fade(() =>
                {
                    openAction();
                    openedAction?.Invoke();
                }, changeInputEnableStateWhileFading);
            }

            return true;
        }

        void DisplayCharacterInfo(Character character, bool conversation)
        {
            int offsetY = conversation ? -6 : 0;

            characterInfoTexts.Clear();
            characterInfoPanels.Clear();
            layout.FillArea(new Rect(208, offsetY + 49, 96, 80), GetUIColor(28), false);
            layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground, 52, 1);
            layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.PortraitOffset + character.PortraitIndex - 1, PrimaryUIPaletteIndex, 2);
            layout.AddText(new Rect(242, offsetY + 49, 62, 7), DataNameProvider.GetRaceName(character.Race));
            layout.AddText(new Rect(242, offsetY + 56, 62, 7), DataNameProvider.GetGenderName(character.Gender));
            characterInfoTexts.Add(CharacterInfo.Age, layout.AddText(new Rect(242, offsetY + 63, 62, 7),
                string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                character.Attributes[Attribute.Age].CurrentValue)));
            characterInfoTexts.Add(CharacterInfo.Level, layout.AddText(new Rect(242, offsetY + 70, 62, 7),
                $"{DataNameProvider.GetClassName(character.Class)} {character.Level}"));
            layout.AddText(new Rect(208, offsetY + 84, 96, 7), character.Name, conversation ? TextColor.PartyMember : TextColor.ActivePartyMember, TextAlign.Center);
            if (!conversation)
            {
                bool magicClass = character.Class.IsMagic();
                characterInfoTexts.Add(CharacterInfo.EP, layout.AddText(new Rect(242, 77, 62, 7),
                    string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                    character.ExperiencePoints)));
                characterInfoTexts.Add(CharacterInfo.LP, layout.AddText(new Rect(208, 92, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoHitPointsString,
                    character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue),
                    TextColor.White, TextAlign.Center));
                if (magicClass)
                {
                    characterInfoTexts.Add(CharacterInfo.SP, layout.AddText(new Rect(208, 99, 96, 7),
                        string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                        character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue),
                        TextColor.White, TextAlign.Center));
                }
                characterInfoTexts.Add(CharacterInfo.SLPAndTP, layout.AddText(new Rect(208, 106, 96, 7),
                    (magicClass ? string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) : new string(' ', 7)) + " " +
                    string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints), TextColor.White, TextAlign.Center));
                var displayGold = OpenStorage is IPlace ? 0 : character.Gold;
                characterInfoTexts.Add(CharacterInfo.GoldAndFood, layout.AddText(new Rect(208, 113, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, displayGold, character.Food),
                    TextColor.White, TextAlign.Center));
                layout.AddSprite(new Rect(214, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), UIPaletteIndex);
                if (CurrentSavegame.IsSpellActive(ActiveSpellType.Attack))
                {
                    int attack = character.BaseAttack + (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25;
                    if (attack > 0)
                        attack = (attack * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
                    string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack));
                    characterInfoTexts.Add(CharacterInfo.Attack, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                        new Rect(220, 122, 30, 7), attackString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
                }
                else
                {
                    string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', character.BaseAttack < 0 ? '-' : '+'), Math.Abs(character.BaseAttack));
                    characterInfoTexts.Add(CharacterInfo.Attack, layout.AddText(new Rect(220, 122, 30, 7), attackString, TextColor.White, TextAlign.Left));
                }
                layout.AddSprite(new Rect(261, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), UIPaletteIndex);
                if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
                {
                    int defense = character.BaseDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
                    if (defense > 0)
                        defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
                    string defenseString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense));
                    characterInfoTexts.Add(CharacterInfo.Defense, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                        new Rect(268, 122, 30, 7), defenseString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
                }
                else
                {
                    string defenseString = string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', character.BaseDefense < 0 ? '-' : '+'), Math.Abs(character.BaseDefense));
                    characterInfoTexts.Add(CharacterInfo.Defense, layout.AddText(new Rect(268, 122, 30, 7), defenseString, TextColor.White, TextAlign.Left));
                }
            }
            else
            {
                characterInfoTexts.Add(CharacterInfo.ConversationPartyMember,
                    layout.AddText(new Rect(208, 99, 96, 7), CurrentPartyMember.Name, TextColor.ActivePartyMember, TextAlign.Center));
                if (CurrentPartyMember.Gold > 0)
                {
                    ShowTextPanel(CharacterInfo.ConversationGold, CurrentPartyMember.Gold > 0,
                        $"{DataNameProvider.GoldName}^{CurrentPartyMember.Gold}", new Rect(209, 107, 43, 15));
                }
                if (CurrentPartyMember.Food > 0)
                {
                    ShowTextPanel(CharacterInfo.ConversationFood, CurrentPartyMember.Food > 0,
                        $"{DataNameProvider.FoodName}^{CurrentPartyMember.Food}", new Rect(257, 107, 43, 15));
                }
            }
        }

        internal void UpdateCharacterInfo(Character conversationPartner = null)
        {
            if (currentWindow.Window != Window.Inventory &&
                currentWindow.Window != Window.Stats &&
                currentWindow.Window != Window.Conversation)
                return;

            if (currentWindow.Window == Window.Conversation)
            {
                if (conversationPartner == null || CurrentPartyMember == null)
                    return;
            }
            else if (CurrentInventory == null)
            {
                return;
            }

            void UpdateText(CharacterInfo characterInfo, Func<string> text, bool checkNextCycle = false)
            {
                if (characterInfoTexts.ContainsKey(characterInfo))
                    characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text()));
                else if (checkNextCycle)
                {
                    // The weight display and maybe others might only be added in next cycle so
                    // re-check in two cycles.
                    ExecuteNextUpdateCycle(() => ExecuteNextUpdateCycle(() => UpdateText(characterInfo, text, false)));
                }
            }

            var character = conversationPartner ?? CurrentInventory;

            UpdateText(CharacterInfo.Age, () => string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                character.Attributes[Attribute.Age].CurrentValue));
            UpdateText(CharacterInfo.Level, () => $"{DataNameProvider.GetClassName(character.Class)} {character.Level}");
            UpdateText(CharacterInfo.EP, () => string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                character.ExperiencePoints));
            UpdateText(CharacterInfo.LP, () => string.Format(DataNameProvider.CharacterInfoHitPointsString,
                character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue));
            UpdateText(CharacterInfo.SP, () => string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue));
            UpdateText(CharacterInfo.SLPAndTP, () =>
                string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints));
            UpdateText(CharacterInfo.GoldAndFood, () =>
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, character.Gold, character.Food));
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Attack))
            {
                int attack = character.BaseAttack + (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25;
                if (attack > 0)
                    attack = (attack * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
                UpdateText(CharacterInfo.Attack, () =>
                    string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack)));
            }
            else
            {
                UpdateText(CharacterInfo.Attack, () =>
                    string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', character.BaseAttack < 0 ? '-' : '+'), Math.Abs(character.BaseAttack)));
            }
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
            {
                int defense = character.BaseDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
                if (defense > 0)
                    defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
                UpdateText(CharacterInfo.Defense, () =>
                    string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)));
            }
            else
            {
                UpdateText(CharacterInfo.Defense, () =>
                    string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', character.BaseDefense < 0 ? '-' : '+'), Math.Abs(character.BaseDefense)));
            }
            UpdateText(CharacterInfo.Weight, () => string.Format(DataNameProvider.CharacterInfoWeightString,
                character.TotalWeight / 1000, (character as PartyMember).MaxWeight / 1000), true);
            if (conversationPartner != null)
            {
                UpdateText(CharacterInfo.ConversationPartyMember, () => CurrentPartyMember.Name);
                ShowTextPanel(CharacterInfo.ConversationGold, CurrentPartyMember.Gold > 0,
                    $"{DataNameProvider.GoldName}^{CurrentPartyMember.Gold}", new Rect(209, 107, 43, 15));
                ShowTextPanel(CharacterInfo.ConversationFood, CurrentPartyMember.Food > 0,
                    $"{DataNameProvider.FoodName}^{CurrentPartyMember.Food}", new Rect(257, 107, 43, 15));
            }
        }

        void HideTextPanel(CharacterInfo characterInfo)
        {
            ShowTextPanel(characterInfo, false, null, null);
        }

        void ShowTextPanel(CharacterInfo characterInfo, bool show, string text, Rect area)
        {
            if (show)
            {
                if (!characterInfoPanels.ContainsKey(characterInfo))
                    characterInfoPanels[characterInfo] = layout.AddPanel(area, 2);
                if (!characterInfoTexts.ContainsKey(characterInfo))
                {
                    characterInfoTexts[characterInfo] = layout.AddText(area.CreateOffset(0, 1),
                        text, TextColor.White, TextAlign.Center, 4);
                }
                else
                    characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text));
            }
            else
            {
                if (characterInfoPanels.ContainsKey(characterInfo))
                {
                    characterInfoPanels[characterInfo].Destroy();
                    characterInfoPanels.Remove(characterInfo);
                }
                if (characterInfoTexts.ContainsKey(characterInfo))
                {
                    characterInfoTexts[characterInfo].Destroy();
                    characterInfoTexts.Remove(characterInfo);
                }
            }
        }

        void InventoryItemAdded(Item item, int amount, PartyMember partyMember = null)
        {
            partyMember ??= CurrentInventory;

            partyMember.TotalWeight += (uint)amount * item.Weight;
        }

        internal void InventoryItemAdded(uint itemIndex, int amount, PartyMember partyMember)
        {
            InventoryItemAdded(ItemManager.GetItem(itemIndex), amount, partyMember);
        }

        void InventoryItemRemoved(Item item, int amount, PartyMember partyMember = null)
        {
            partyMember ??= CurrentInventory;

            partyMember.TotalWeight -= (uint)amount * item.Weight;
        }

        internal void InventoryItemRemoved(uint itemIndex, int amount, PartyMember partyMember = null)
        {
            InventoryItemRemoved(ItemManager.GetItem(itemIndex), amount, partyMember);
        }

        void EquipmentAdded(Item item, int amount, bool cursed, Character character = null)
        {
            character ??= CurrentInventory;

            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            character.BaseAttack = (short)(character.BaseAttack + item.Damage);
            character.BaseDefense = (short)(character.BaseDefense + item.Defense);
            character.MagicAttack = (short)(character.MagicAttack + item.MagicAttackLevel);
            character.MagicDefense = (short)(character.MagicDefense + item.MagicArmorLevel);
            character.HitPoints.BonusValue += (cursed ? -1 : 1) * item.HitPoints;
            character.SpellPoints.BonusValue += (cursed ? -1 : 1) * item.SpellPoints;
            if (character.HitPoints.CurrentValue > character.HitPoints.TotalMaxValue)
                character.HitPoints.CurrentValue = character.HitPoints.TotalMaxValue;
            if (character.SpellPoints.CurrentValue > character.SpellPoints.TotalMaxValue)
                character.SpellPoints.CurrentValue = character.SpellPoints.TotalMaxValue;
            if (item.Attribute != null)
                character.Attributes[item.Attribute.Value].BonusValue += (cursed ? -1 : 1) * item.AttributeValue;
            if (item.Ability != null)
                character.Abilities[item.Ability.Value].BonusValue += (cursed ? -1 : 1) * item.AbilityValue;
            character.Abilities[Ability.Attack].BonusValue -= (int)item.AttackReduction;
            character.Abilities[Ability.Parry].BonusValue -= (int)item.ParryReduction;
            character.TotalWeight += (uint)amount * item.Weight;
        }

        internal void EquipmentAdded(uint itemIndex, int amount, bool cursed, Character character)
        {
            EquipmentAdded(ItemManager.GetItem(itemIndex), amount, cursed, character);
        }

        void EquipmentRemoved(Character character, Item item, int amount, bool cursed)
        {
            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            character.BaseAttack = (short)(character.BaseAttack - item.Damage);
            character.BaseDefense = (short)(character.BaseDefense - item.Defense);
            character.MagicAttack = (short)(character.MagicAttack - item.MagicAttackLevel);
            character.MagicDefense = (short)(character.MagicDefense - item.MagicArmorLevel);
            character.HitPoints.BonusValue -= (cursed ? -1 : 1) * item.HitPoints;
            character.SpellPoints.BonusValue -= (cursed ? -1 : 1) * item.SpellPoints;
            if (character.HitPoints.CurrentValue > character.HitPoints.TotalMaxValue)
                character.HitPoints.CurrentValue = character.HitPoints.TotalMaxValue;
            if (character.SpellPoints.CurrentValue > character.SpellPoints.TotalMaxValue)
                character.SpellPoints.CurrentValue = character.SpellPoints.TotalMaxValue;
            if (item.Attribute != null)
                character.Attributes[item.Attribute.Value].BonusValue -= (cursed ? -1 : 1) * item.AttributeValue;
            if (item.Ability != null)
                character.Abilities[item.Ability.Value].BonusValue -= (cursed ? -1 : 1) * item.AbilityValue;
            character.Abilities[Ability.Attack].BonusValue += (int)item.AttackReduction;
            character.Abilities[Ability.Parry].BonusValue += (int)item.ParryReduction;
            character.TotalWeight -= (uint)amount * item.Weight;
        }

        void EquipmentRemoved(Item item, int amount, bool cursed)
        {
            EquipmentRemoved(CurrentInventory, item, amount, cursed);
        }

        internal void EquipmentRemoved(uint itemIndex, int amount, bool cursed)
        {
            EquipmentRemoved(ItemManager.GetItem(itemIndex), amount, cursed);
        }

        internal void EquipmentRemoved(Character character, uint itemIndex, int amount, bool cursed)
        {
            EquipmentRemoved(character, ItemManager.GetItem(itemIndex), amount, cursed);
        }

        void RenewTimedEvent(TimedGameEvent timedGameEvent, TimeSpan delay)
        {
            timedGameEvent.ExecutionTime = DateTime.Now + delay;

            if (!timedEvents.Contains(timedGameEvent))
                timedEvents.Add(timedGameEvent);
        }

        internal void AddTimedEvent(TimeSpan delay, Action action)
        {
            if (currentBattle != null && Configuration.FastBattleMode)
                delay = TimeSpan.FromMilliseconds(Math.Max(1.0, delay.TotalMilliseconds / 2));

            timedEvents.Add(new TimedGameEvent
            {
                ExecutionTime = DateTime.Now + delay,
                Action = action
            });
        }

        static readonly float[] ShakeOffsetFactors = new float[]
        {
            -0.5f, 0.0f, 1.0f, 0.5f, -1.0f, 0.0f, 0.5f
        };

        internal void ShakeScreen(TimeSpan durationPerShake, int numShakes, float amplitude)
        {
            int shakeIndex = 0;

            void Shake()
            {
                if (++shakeIndex == numShakes)
                {
                    ViewportOffset = null;
                }
                else
                {
                    ViewportOffset = new FloatPosition(0.0f, amplitude * ShakeOffsetFactors[(shakeIndex - 1) % ShakeOffsetFactors.Length]);
                    AddTimedEvent(durationPerShake, Shake);
                }
            }

            Shake();
        }

        void Fade(Action midFadeAction, bool changeInputEnableState = true)
        {
            if (changeInputEnableState)
                allInputDisabled = true;
            layout.AddFadeEffect(new Rect(0, 36, Global.VirtualScreenWidth, Global.VirtualScreenHeight - 36), Render.Color.Black, FadeEffectType.FadeInAndOut, FadeTime);
            AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime / 2), midFadeAction);
            if (changeInputEnableState)
                AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), () => allInputDisabled = false);
        }

        internal void DamageAllPartyMembers(Func<PartyMember, uint> damageProvider, Func<PartyMember, bool> affectChecker = null,
            Action<PartyMember, Action> notAffectedHandler = null, Action followAction = null, Ailment inflictAilment = Ailment.None,
            bool showDamageSplash = true)
        {
            // In original all players are damaged one after the other
            // without showing the damage splash immediately. If a character
            // dies the skull is shown. If this was the active character
            // the "new leader" logic kicks in. Only after that the next
            // party member is checked.
            // At the end all affected living characters will show the damage splash.
            List<PartyMember> damagedPlayers = new List<PartyMember>();
            ForeachPartyMember(Damage, p => p.Alive && !p.Ailments.HasFlag(Ailment.Petrified), () =>
            {
                if (showDamageSplash)
                {
                    ForeachPartyMember(ShowDamageSplash, p => damagedPlayers.Contains(p), () =>
                    {
                        layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
                        followAction?.Invoke();
                    });
                }
                else
                {
                    layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
                    followAction?.Invoke();
                }
            });

            void Damage(PartyMember partyMember, Action finished)
            {
                if (affectChecker?.Invoke(partyMember) == false)
                {
                    if (notAffectedHandler == null)
                        finished?.Invoke();
                    else
                        notAffectedHandler?.Invoke(partyMember, finished);
                    return;
                }

                var damage = Godmode ? 0 : damageProvider?.Invoke(partyMember) ?? 0;

                if (damage > 0 || inflictAilment != Ailment.None)
                {
                    partyMember.Damage(damage);

                    if (partyMember.Alive && inflictAilment >= Ailment.DeadCorpse)
                    {
                        partyMember.Die(inflictAilment);
                    }

                    if (partyMember.Alive) // update HP etc if not died already
                    {
                        damagedPlayers.Add(partyMember);

                        if (inflictAilment != Ailment.None && inflictAilment < Ailment.DeadCorpse)
                        {
                            partyMember.Ailments |= inflictAilment;

                            if (inflictAilment == Ailment.Blind && partyMember == CurrentPartyMember)
                                UpdateLight();
                        }
                    }

                    if (partyMember.Alive && partyMember.Ailments.CanSelect())
                    {
                        finished?.Invoke();
                    }
                    else
                    {
                        if (CurrentPartyMember == partyMember && currentBattle == null)
                        {
                            if (!PartyMembers.Any(p => p.Alive && p.Ailments.CanSelect()))
                            {
                                GameOver();
                                return;
                            }

                            bool inputWasEnabled = InputEnable;
                            newLeaderPicked += NewLeaderPicked;
                            RecheckActivePartyMember(out bool gameOver);

                            if (gameOver)
                                newLeaderPicked -= NewLeaderPicked;

                            void NewLeaderPicked(int index)
                            {
                                newLeaderPicked -= NewLeaderPicked;
                                finished?.Invoke();
                                InputEnable = inputWasEnabled;
                            }
                        }
                        else
                        {
                            layout.AttachToPortraitAnimationEvent(finished);
                        }
                    }
                }
                else
                {
                    finished?.Invoke();
                }
            }
            void ShowDamageSplash(PartyMember partyMember, Action finished)
            {
                int slot = SlotFromPartyMember(partyMember).Value;
                layout.SetCharacter(slot, partyMember);
                ShowPlayerDamage(slot, damageProvider?.Invoke(partyMember) ?? 0);
                finished?.Invoke();
            }
        }

        void DamageAllPartyMembers(uint damage, Func<PartyMember, bool> affectChecker = null,
            Action < PartyMember, Action> notAffectedHandler = null, Action followAction = null)
        {
            DamageAllPartyMembers(_ => damage, affectChecker, notAffectedHandler, followAction);
        }

        internal void TriggerTrap(TrapEvent trapEvent)
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
                    // TODO: are there more like random?
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
                if (targetFilter?.Invoke(p) != false)
                    ShowMessagePopup(p.Name + DataNameProvider.EscapedTheTrap, finish);
                else
                    finish?.Invoke();
            }, Finished, trapEvent.GetAilment());

            void Finished()
            {
                if (trapEvent.Next != null)
                {
                    EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, (uint)player.Position.X,
                        (uint)player.Position.Y, CurrentTicks, trapEvent.Next, true);
                }
                else
                {
                    ResetMapCharacterInteraction(Map);
                }
            }
        }

        internal void AwardPlayer(PartyMember partyMember, AwardEvent awardEvent, Action followAction)
        {
            void Change(CharacterValue characterValue, int amount, bool percentage, bool lpLike)
            {
                uint max = lpLike ? characterValue.TotalMaxValue : characterValue.MaxValue;

                if (percentage)
                    amount = amount * (int)max / 100;

                characterValue.CurrentValue = (uint)Util.Limit(0, (int)characterValue.CurrentValue + amount, (int)max);
            }

            void AwardValue(CharacterValue characterValue, bool lpLike)
            {
                switch (awardEvent.Operation)
                {
                    case AwardEvent.AwardOperation.Increase:
                        Change(characterValue, (int)awardEvent.Value, false, lpLike);
                        break;
                    case AwardEvent.AwardOperation.Decrease:
                        Change(characterValue, -(int)awardEvent.Value, false, lpLike);
                        break;
                    case AwardEvent.AwardOperation.IncreasePercentage:
                        Change(characterValue, (int)awardEvent.Value, true, lpLike);
                        break;
                    case AwardEvent.AwardOperation.DecreasePercentage:
                        Change(characterValue, -(int)awardEvent.Value, true, lpLike);
                        break;
                    case AwardEvent.AwardOperation.Fill:
                        characterValue.CurrentValue = lpLike ? characterValue.TotalMaxValue : characterValue.MaxValue;
                        break;
                }
            }

            switch (awardEvent.TypeOfAward)
            {
                case AwardEvent.AwardType.Attribute:
                    if (awardEvent.Attribute != null && awardEvent.Attribute < Attribute.Age)
                        AwardValue(partyMember.Attributes[awardEvent.Attribute.Value], false);
                    else
                    {
                        ShowMessagePopup($"ERROR: Invalid award event attribute type.", followAction);
                        return;
                    }
                    break;
                case AwardEvent.AwardType.Ability:
                    if (awardEvent.Ability != null)
                        AwardValue(partyMember.Abilities[awardEvent.Ability.Value], false);
                    else
                    {
                        ShowMessagePopup($"ERROR: Invalid award event ability type.", followAction);
                        return;
                    }
                    break;
                case AwardEvent.AwardType.HitPoints:
                {
                    // Note: Awards happen silently so there is no damage splash.
                    // Looking at the original code there isn't even a die handling
                    // when a negative award would leave the LP at 0 but we do so here.
                    AwardValue(partyMember.HitPoints, true);
                    if (partyMember.Alive && partyMember.HitPoints.CurrentValue == 0)
                        partyMember.Die();
                    else
                        layout.UpdateCharacter(partyMember);
                    break;
                }
                case AwardEvent.AwardType.SpellPoints:
                    AwardValue(partyMember.SpellPoints, true);
                    layout.UpdateCharacter(partyMember);
                    break;
                case AwardEvent.AwardType.SpellLearningPoints:
                {
                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Increase:
                            partyMember.SpellLearningPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.SpellLearningPoints + awardEvent.Value);
                            break;
                        case AwardEvent.AwardOperation.Decrease:
                            partyMember.SpellLearningPoints = (ushort)Util.Max(0, (int)partyMember.SpellLearningPoints - (int)awardEvent.Value);
                            break;
                    }
                    break;
                }
                case AwardEvent.AwardType.Ailments:
                {
                    if (awardEvent.Ailments == null)
                    {
                        ShowMessagePopup($"ERROR: Invalid award event ailment.", followAction);
                        return;
                    }

                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Add:
                            partyMember.Ailments |= awardEvent.Ailments.Value;
                            break;
                        case AwardEvent.AwardOperation.Remove:
                            partyMember.Ailments &= ~awardEvent.Ailments.Value;
                            break;
                        case AwardEvent.AwardOperation.Toggle:
                            partyMember.Ailments ^= awardEvent.Ailments.Value;
                            break;
                    }

                    if (awardEvent.Ailments.Value.HasFlag(Ailment.Blind) && partyMember == CurrentPartyMember)
                        UpdateLight();
                    break;
                }
                case AwardEvent.AwardType.UsableSpellTypes:
                {
                    if (awardEvent.UsableSpellTypes == null)
                    {
                        ShowMessagePopup($"ERROR: Invalid award event spell mastery.", followAction);
                        return;
                    }

                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Add:
                            partyMember.SpellMastery |= awardEvent.UsableSpellTypes.Value;
                            break;
                        case AwardEvent.AwardOperation.Remove:
                            partyMember.SpellMastery &= ~awardEvent.UsableSpellTypes.Value;
                            break;
                        case AwardEvent.AwardOperation.Toggle:
                            partyMember.SpellMastery ^= awardEvent.UsableSpellTypes.Value;
                            break;
                    }
                    break;
                }
                case AwardEvent.AwardType.Languages:
                {
                    if (awardEvent.Languages == null)
                    {
                        ShowMessagePopup($"ERROR: Invalid award event language.", followAction);
                        return;
                    }

                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Add:
                            partyMember.SpokenLanguages |= awardEvent.Languages.Value;
                            break;
                        case AwardEvent.AwardOperation.Remove:
                            partyMember.SpokenLanguages &= ~awardEvent.Languages.Value;
                            break;
                        case AwardEvent.AwardOperation.Toggle:
                            partyMember.SpokenLanguages ^= awardEvent.Languages.Value;
                            break;
                    }
                    break;
                }
                case AwardEvent.AwardType.Experience:
                {
                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Increase:
                            AddExperience(partyMember, awardEvent.Value, followAction);
                            return;
                        case AwardEvent.AwardOperation.Decrease:
                            partyMember.ExperiencePoints = (uint)Util.Max(0, (long)partyMember.ExperiencePoints - awardEvent.Value);
                            break;
                    }
                    break;
                }
                case AwardEvent.AwardType.TrainingPoints:
                {
                    switch (awardEvent.Operation)
                    {
                        case AwardEvent.AwardOperation.Increase:
                            partyMember.TrainingPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.TrainingPoints + awardEvent.Value);
                            break;
                        case AwardEvent.AwardOperation.Decrease:
                            partyMember.TrainingPoints = (ushort)Util.Max(0, (int)partyMember.TrainingPoints - (int)awardEvent.Value);
                            break;
                    }
                    break;
                }
            }

            followAction?.Invoke();
        }

        internal void SayWord(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
        {
            OpenDictionary(word =>
            {
                bool match = string.Compare(textDictionary.Entries[(int)conditionEvent.ObjectIndex], word, true) == 0;
                var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                    ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
                var @event = match ? conditionEvent.Next : mapEventIfFalse;
                if (@event != null)
                    EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, CurrentTicks, @event, true);
            });
        }

        internal void EnterNumber(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
        {
            layout.OpenAmountInputBox(DataNameProvider.WhichNumber, null, null, 9999, number =>
            {
                var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                    ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
                var @event = (number == conditionEvent.ObjectIndex)
                    ? conditionEvent.Next : mapEventIfFalse;
                if (@event != null)
                    EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, CurrentTicks, @event, true);
            });
        }

        void Levitate(Action failAction, bool climbIfNoEvent = true)
        {
            ConditionEvent climbEvent = null;
            var levitatePosition = new Position(player.Position);
            bool HasClimbEvent(uint x, uint y)
            {
                var mapEventId = Map.Blocks[x, y].MapEventId;

                if (mapEventId == 0)
                    return false;

                var @event = Map.EventList[(int)mapEventId - 1];

                if (!(@event is ConditionEvent conditionEvent))
                    return false;

                climbEvent = conditionEvent;

                return conditionEvent.TypeOfCondition == ConditionEvent.ConditionType.Levitating;
            }
            if (!HasClimbEvent((uint)player.Position.X, (uint)player.Position.Y))
            {
                // Also try forward position
                camera3D.GetForwardPosition(Global.DistancePerBlock, out float x, out float z, false, false);
                var position = Geometry.Geometry.CameraToBlockPosition(Map, x, z);

                if (position == player.Position ||
                    position.X < 0 || position.X >= Map.Width ||
                    position.Y < 0 || position.Y >= Map.Height ||
                    !HasClimbEvent((uint)position.X, (uint)position.Y))
                {
                    climbEvent = null;
                }
                else
                {
                    levitatePosition = position;
                }
            }
            if (climbEvent != null)
            {
                // Attach player to ladder or hole
                float angle = camera3D.Angle;
                Geometry.Geometry.BlockToCameraPosition(Map, levitatePosition, out float x, out float z);
                camera3D.SetPosition(-x, z);
                camera3D.TurnTowards(angle);
                camera3D.GetBackwardPosition(0.5f * Global.DistancePerBlock, out x, out z, false, false);
                camera3D.SetPosition(-x, z);
                camera3D.TurnTowards(angle);
            }
            if (climbIfNoEvent || climbEvent != null)
            {
                void StartClimbing()
                {
                    Pause();
                    Climb(() =>
                    {
                        if (climbEvent == null)
                            failAction?.Invoke();
                        else
                        {
                            levitating = true;
                            EventExtensions.TriggerEventChain(Map, this, EventTrigger.Levitating, 0u, 0u, CurrentTicks, climbEvent, true);
                        }
                    });
                }
                if (WindowActive)
                    CloseWindow(StartClimbing);
                else
                    StartClimbing();
            }
            else
            {
                failAction?.Invoke();
            }
        }

        void Levitate()
        {
            Levitate(() =>
            {
                ShowMessagePopup(DataNameProvider.YouLevitate, () =>
                {
                    MoveVertically(false, true, Resume);
                });
            });
        }

        void Climb(Action finishAction = null)
        {
            MoveVertically(true, false, finishAction);
        }

        void Fall(Action finishAction = null)
        {
            MoveVertically(false, false, finishAction);
        }

        void MoveVertically(bool up, bool mapChange, Action finishAction = null)
        {
            if (!is3D || WindowActive)
            {
                finishAction?.Invoke();
                return;
            }

            var sourceY = !mapChange ? camera3D.Y : (up ? renderMap3D.GetFloorY() : renderMap3D.GetLevitatingY());
            player3D.SetY(sourceY);
            var targetY = mapChange ? camera3D.GroundY : (up ? renderMap3D.GetLevitatingY() : renderMap3D.GetFloorY());
            float stepSize = renderMap3D.GetLevitatingStepSize();
            float dist = Math.Abs(targetY - camera3D.Y);
            int steps = Math.Max(1, Util.Round(dist / stepSize));

            PlayTimedSequence(steps, () =>
            {
                if (up)
                    camera3D.LevitateUp(stepSize);
                else
                    camera3D.LevitateDown(stepSize);
            }, 75, finishAction);
        }

        /// <summary>
        /// Immediately moves 2 blocks forward.
        /// Can not pass walls.
        /// </summary>
        void Jump()
        {
            if (!is3D || WindowActive)
                return; // Should not happen

            // Note: Even if the player looks diagonal (e.g. south west)
            // the jump is always performed into one of the 4 main directions.
            Position targetPosition = new Position(player3D.Position);

            switch (player3D.Direction)
            {
                default:
                case CharacterDirection.Up:
                    targetPosition.Y -= 2;
                    break;
                case CharacterDirection.Right:
                    targetPosition.X += 2;
                    break;
                case CharacterDirection.Down:
                    targetPosition.Y += 2;
                    break;
                case CharacterDirection.Left:
                    targetPosition.X -= 2;
                    break;
            }

            var labdata = MapManager.GetLabdataForMap(Map);
            var checkPosition = new Position(player3D.Position);

            for (int i = 0; i < 2; ++i)
            {
                checkPosition.X += Math.Sign(targetPosition.X - checkPosition.X);
                checkPosition.Y += Math.Sign(targetPosition.Y - checkPosition.Y);

                if (Map.Blocks[(uint)checkPosition.X, (uint)checkPosition.Y].BlocksPlayer(labdata, true))
                {
                    ShowMessagePopup(DataNameProvider.CannotJumpThroughWalls);
                    return;
                }
            }

            player3D.SetPosition(targetPosition.X, targetPosition.Y, CurrentTicks, true);
            player3D.TurnTowards((float)player3D.Direction * 90.0f);
            camera3D.MoveBackward(0.35f * Global.DistancePerBlock, false, false);
        }

        internal void Spin(CharacterDirection direction, Event nextEvent)
        {
            if (!is3D || WindowActive)
                return; // Should not happen

            if (direction == CharacterDirection.Random)
                direction = (CharacterDirection)RandomInt(0, 3);

            // Spin at least for 180°
            float currentAngle = player3D.Angle;
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
            int stepIndex = 0;

            void Step()
            {
                if (stepIndex++ < fullSteps)
                    player3D.TurnRight(stepSize);
                else
                    player3D.TurnRight(halfStepSize);
            }

            PlayTimedSequence(fullSteps + 1, Step, 65, () =>
            {
                ResetMoveKeys();

                if (nextEvent != null)
                {
                    EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always,
                        (uint)player3D.Position.X, (uint)player.Position.Y, CurrentTicks, nextEvent, true);
                }
            });
        }

        /// <summary>
        /// This is used by external triggers like a cheat engine.
        /// </summary>
        public bool Teleport(uint mapIndex, uint x, uint y, CharacterDirection direction, out bool blocked, bool force = false)
        {
            blocked = false;

            if (!ingame || layout.OptionMenuOpen || BattleActive || (!force && (WindowActive || layout.PopupActive)))
                return false;

            if (mapIndex == 0)
                mapIndex = Map.Index;

            var newMap = MapManager.GetMap(mapIndex);
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
                if (!force && !newMap.Tiles[newX, newY].AllowMovement(MapManager.GetTilesetForMap(newMap), TravelType.Walk))
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

            player.MoveTo(newMap, newX, newY, CurrentTicks, true, direction);
            this.player.Position.X = RenderPlayer.Position.X;
            this.player.Position.Y = RenderPlayer.Position.Y;
            TravelType = TravelType.Walk; // After teleport you always walk (important when falling into a town etc)

            if (!mapTypeChanged)
            {
                PlayerMoved(mapChange);
            }

            if (mapChange && !WindowActive)
            {
                // Color of the filled upper right area may need update cause of palette change.
                mapViewRightFillArea.Color = GetUIColor(28);
                UpdateMapName();
                UpdateLight(true);
            }

            if (!mapChange) // Otherwise the map change handler takes care of this
                ResetMoveKeys();

            if (!WindowActive && !layout.PopupActive)
            {
                // Trigger events after map transition
                TriggerMapEvents(EventTrigger.Move, (uint)this.player.Position.X,
                    (uint)this.player.Position.Y);
            }

            return true;
        }

        internal void Teleport(TeleportEvent teleportEvent)
        {
            ResetMoveKeys();
            ResetMapCharacterInteraction(Map);

            void RunTransition()
            {
                levitating = false;
                Teleport(teleportEvent.MapIndex, teleportEvent.X, teleportEvent.Y, teleportEvent.Direction, out _, true);
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
                    break;
                case TeleportEvent.TransitionType.Falling:
                    Pause();
                    Fall(() => Fade(() =>
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
                    break;
                case TeleportEvent.TransitionType.Climbing:
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
                    break;
                case TeleportEvent.TransitionType.Outro:
                    ShowOutro();
                    break;
                default:
                    Fade(RunTransition);
                    break;
            }
        }

        void ShowOutro()
        {
            ClosePopup();
            CloseWindow();
            Pause();
            StartSequence();
            ExecuteNextUpdateCycle(() =>
            {
                Cleanup();
                layout.ShowPortraitArea(false);
                layout.SetLayout(LayoutType.None);
                windowTitle.Visible = false;
                TrapMouse(new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight));
                cursor.Type = CursorType.None;
                UpdateCursor(lastMousePosition, MouseButtons.None);
                currentUIPaletteIndex = 0;
                battleRoundActiveSprite.Visible = false;
                paused = true;

                PlayMusic(Song.Outro);
                outro ??= outroFactory.Create(() => NewGame(true));
                outro.Start(CurrentSavegame);
            });
        }

        public bool ActivateTransport(TravelType travelType)
        {
            if (travelType == TravelType.Walk ||
                travelType == TravelType.Swim)
                throw new AmbermoonException(ExceptionScope.Application, "Walking and swimming should not be set via ActivateTransport");

            if (!Map.IsWorldMap)
                return false;

            if (TravelType != TravelType.Walk)
                return false;

            void Activate()
            {
                TravelType = travelType;
                layout.TransportEnabled = true;
                if (layout.ButtonGridPage == 1)
                {
                    layout.EnableButton(3, true);
                    layout.EnableButton(5, travelType.CanCampOn());
                }
            }

            if (WindowActive)
                CloseWindow(Activate);
            else
                Activate();

            return true;
        }

        internal void ToggleTransport()
        {
            uint x = (uint)player.Position.X;
            uint y = (uint)player.Position.Y;
            var mapIndex = renderMap2D.GetMapFromTile(x, y).Index;
            var transport = GetTransportAtPlayerLocation(out int? index);

            if (transport == null)
            {
                if (TravelType.UsesMapObject())
                {
                    for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
                    {
                        if (CurrentSavegame.TransportLocations[i] == null)
                        {
                            CurrentSavegame.TransportLocations[i] = new TransportLocation
                            {
                                MapIndex = mapIndex,
                                Position = new Position((int)x % 50 + 1, (int)y % 50 + 1),
                                TravelType = TravelType
                            };
                            break;
                        }
                    }

                    renderMap2D.PlaceTransport(mapIndex, x % 50, y % 50, TravelType);
                }
                else
                {
                    layout.TransportEnabled = false;
                    if (layout.ButtonGridPage == 1)
                        layout.EnableButton(3, false);
                }

                var tile = renderMap2D[player.Position];

                if (tile.Type == Map.TileType.Water &&
                    (!TravelType.UsesMapObject() ||
                    !TravelType.CanStandOn()))
                    StartSwimming();
                else
                    TravelType = TravelType.Walk;

                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(5, TravelType.CanCampOn());

                renderMap2D.TriggerEvents(player2D, EventTrigger.Move, x, y, MapManager, CurrentTicks, CurrentSavegame);
            }
            else if (transport != null && TravelType == TravelType.Walk)
            {
                CurrentSavegame.TransportLocations[index.Value] = null;
                renderMap2D.RemoveTransportAt(mapIndex, x % 50, y % 50);
                ActivateTransport(transport.TravelType);
            }
        }

        TransportLocation GetTransportAtPlayerLocation(out int? index)
        {
            index = null;
            var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
            // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
            var position = new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1);

            for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
            {
                var transport = CurrentSavegame.TransportLocations[i];

                if (transport != null)
                {
                    if (transport.MapIndex == mapIndex && transport.Position == position)
                    {
                        index = i;
                        return transport;
                    }
                }
            }

            return null;
        }

        List<TransportLocation> GetTransportsInVisibleArea(out TransportLocation transportAtPlayerIndex)
        {
            transportAtPlayerIndex = null;
            var transports = new List<TransportLocation>();

            if (!Map.IsWorldMap)
                return transports;

            var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
            // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
            var position = new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1);

            for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
            {
                var transport = CurrentSavegame.TransportLocations[i];

                if (transport != null && renderMap2D.IsMapVisible(transport.MapIndex))
                {
                    transports.Add(transport);

                    if (transport.MapIndex == mapIndex && transport.Position == position)
                        transportAtPlayerIndex = transport;
                }
            }

            return transports;
        }

        void StartSwimming()
        {
            TravelType = TravelType.Swim;
            DoSwimDamage();
        }

        void DoSwimDamage()
        {
            // TODO
            // This is now called on each movement in water.
            // But it also has to be called each 5 minutes (but not twice if also moving).

            static uint CalculateDamage(PartyMember partyMember)
            {
                var swimAbility = partyMember.Abilities[Ability.Swim].TotalCurrentValue;

                if (swimAbility >= 100)
                    return 0;

                var factor = (100 - swimAbility) / 2;
                return Math.Max(1, factor * partyMember.HitPoints.CurrentValue / 100);
            }

            DamageAllPartyMembers(CalculateDamage);
        }

        internal void PlayerMoved(bool mapChange, Position lastPlayerPosition = null, bool updateSavegame = true,
            Map lastMap = null)
        {
            if (updateSavegame)
            {
                var map = is3D ? Map : renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y);
                CurrentSavegame.CurrentMapIndex = map.Index;
                CurrentSavegame.CurrentMapX = 1u + (uint)player.Position.X;
                CurrentSavegame.CurrentMapY = 1u + (uint)player.Position.Y;
                CurrentSavegame.CharacterDirection = player.Direction;
            }

            // Enable/disable transport button and show transports
            if (!WindowActive)
            {
                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(3, false);

                if (mapChange && Map.Type == MapType.Map2D)
                {
                    renderMap2D.ClearTransports();

                    if (player.MovementAbility <= PlayerMovementAbility.Walking)
                        player2D.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
                }

                void EnableTransport(bool enable = true)
                {
                    layout.TransportEnabled = enable;
                    if (layout.ButtonGridPage == 1)
                        layout.EnableButton(3, enable);
                }

                if (Map.IsWorldMap)
                {
                    var transports = GetTransportsInVisibleArea(out TransportLocation transportAtPlayerIndex);
                    var tile = renderMap2D[player.Position];
                    var tileType = tile.Type;

                    if (tileType == Map.TileType.Water && transportAtPlayerIndex != null &&
                        transportAtPlayerIndex.TravelType.CanStandOn())
                        tileType = Map.TileType.Normal;

                    if (tileType == Map.TileType.Water)
                    {
                        if (TravelType == TravelType.Walk)
                            StartSwimming();
                        else if (TravelType == TravelType.Swim)
                            DoSwimDamage();
                    }
                    else if (tileType != Map.TileType.Water && TravelType == TravelType.Swim)
                        TravelType = TravelType.Walk;

                    foreach (var transport in transports)
                    {
                        renderMap2D.PlaceTransport(transport.MapIndex,
                            (uint)transport.Position.X - 1, (uint)transport.Position.Y - 1, transport.TravelType);
                    }

                    if (transportAtPlayerIndex != null && TravelType == TravelType.Walk)
                    {
                        EnableTransport();
                    }
                    else if (TravelType.IsStoppable() && transportAtPlayerIndex == null)
                    {
                        // Only allow if we could stand or swim there.
                        var tileset = MapManager.GetTilesetForMap(renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y));

                        if (tile.AllowMovement(tileset, TravelType.Walk) ||
                            tile.AllowMovement(tileset, TravelType.Swim))
                            EnableTransport();
                        else
                            EnableTransport(false);
                    }
                    else
                    {
                        EnableTransport(false);
                    }
                }
                else
                {
                    EnableTransport(false);
                }
            }

            if (mapChange)
            {
                monstersCanMoveImmediately = false;
                if (lastMap == null || !lastMap.IsWorldMap ||
                    !Map.IsWorldMap || Map.World != lastMap.World)
                    ResetMoveKeys(lastMap == null || lastMap.Type != Map.Type);
                if (!WindowActive)
                    layout.UpdateLayoutButtons(movement.MovementTicks(Map.Type == MapType.Map3D, Map.IsWorldMap, TravelType.Walk));

                // Update UI palette
                UpdateUIPalette(true);

                if (!Map.IsWorldMap || TravelType == TravelType.Walk)
                    PlayMapMusic();
            }
            else
            {
                this.lastPlayerPosition = lastPlayerPosition;
                monstersCanMoveImmediately = Map.Type == MapType.Map2D && !Map.IsWorldMap;
            }

            if (Map.Type == MapType.Map3D)
            {
                // Explore
                if (CurrentSavegame.Automaps.TryGetValue(Map.Index, out var automap))
                {
                    var labdata = MapManager.GetLabdataForMap(Map);

                    for (int y = -1; y <= 1; ++y)
                    {
                        for (int x = -1; x <= 1; ++x)
                        {
                            int totalX = player3D.Position.X + x;
                            int totalY = player3D.Position.Y + y;

                            if (totalX < 0 || totalX >= Map.Width ||
                                totalY < 0 || totalY >= Map.Height)
                                continue;

                            automap.ExploreBlock(Map, (uint)totalX, (uint)totalY);

                            if (Map.Blocks[totalX, totalY].BlocksPlayerSight(labdata))
                                continue;

                            if (x != 0) // left or right column
                            {
                                int adjacentX = totalX + x;

                                if (adjacentX >= 0 && adjacentX < Map.Width)
                                {
                                    for (int i = -1; i <= 1; ++i)
                                    {
                                        int adjacentY = totalY + i;

                                        if (adjacentY >= 0 && adjacentY < Map.Height)
                                            automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                                    }
                                }
                            }
                            if (y != 0) // upper or lower row
                            {
                                int adjacentY = totalY + y;

                                if (adjacentY >= 0 && adjacentY < Map.Height)
                                {
                                    for (int i = -1; i <= 1; ++i)
                                    {
                                        int adjacentX = totalX + i;

                                        if (adjacentX >= 0 && adjacentX < Map.Width)
                                            automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                                    }
                                }
                            }
                            if (x != 0 && y != 0) // corners
                            {
                                int adjacentX = totalX + x;
                                int adjacentY = totalY + y;

                                if (adjacentX >= 0 && adjacentX < Map.Width &&
                                    adjacentY >= 0 && adjacentY < Map.Height)
                                    automap.ExploreBlock(Map, (uint)adjacentX, (uint)adjacentY);
                            }
                        }
                    }
                }

                // Save goto points
                uint testX = 1u + (uint)player.Position.X;
                uint testY = 1u + (uint)player.Position.Y;
                var gotoPoint = Map.GotoPoints.FirstOrDefault(p => p.X == testX && p.Y == testY);
                if (gotoPoint != null)
                {
                    if (!CurrentSavegame.IsGotoPointActive(gotoPoint.Index))
                    {
                        CurrentSavegame.ActivateGotoPoint(gotoPoint.Index);
                        ShowMessagePopup(DataNameProvider.GotoPointSaved, null, TextAlign.Left);
                        return;
                    }
                }

                // Clairvoyance
                if (CurrentSavegame.IsSpellActive(ActiveSpellType.Clairvoyance))
                {
                    bool trapFound = false;
                    bool spinnerFound = false;
                    var labdata = MapManager.GetLabdataForMap(Map);

                    foreach (var touchedPosition in player3D.GetTouchedPositions(1.45f * Global.DistancePerBlock))
                    {
                        var type = renderMap3D.AutomapTypeFromBlock((uint)touchedPosition.X, (uint)touchedPosition.Y);
                        if (type == AutomapType.Trapdoor)
                        {
                            // TODO: It seems that only trap doors are detected and not traps.
                            trapFound = true;
                            break;
                        }
                        else if (type == AutomapType.Spinner)
                            spinnerFound = true;
                    }

                    if (trapFound)
                        ShowMessagePopup(DataNameProvider.YouNoticeATrap);
                    else if (spinnerFound)
                        ShowMessagePopup(DataNameProvider.SeeRoundDiskInFloor);
                }
            }
        }

        void RemoveMapTile(Map map, uint x, uint y, bool save)
        {
            UpdateMapTile(new ChangeTileEvent
            {
                Type = EventType.ChangeTile,
                Index = uint.MaxValue,
                FrontTileIndex = 0,
                MapIndex = map.Index,
                X = x + 1,
                Y = y + 1
            }, null, null, save);
        }

        internal void UpdateMapTile(ChangeTileEvent changeTileEvent, uint? currentX = null, uint? currentY = null,
            bool save = true)
        {
            bool sameMap = changeTileEvent.MapIndex == 0 || changeTileEvent.MapIndex == Map.Index;
            var map = sameMap ? Map : MapManager.GetMap(changeTileEvent.MapIndex);
            uint x = changeTileEvent.X == 0
                ? (currentX ?? throw new AmbermoonException(ExceptionScope.Data, "No change tile position given"))
                : changeTileEvent.X - 1;
            uint y = changeTileEvent.Y == 0
                ? (currentY ?? throw new AmbermoonException(ExceptionScope.Data, "No change tile position given"))
                : changeTileEvent.Y - 1;

            if (save)
            {
                // Add it to the savegame as well.                
                CurrentSavegame.TileChangeEvents.SafeAdd(map.Index, changeTileEvent);
            }

            if (!changedMaps.Contains(map.Index))
                changedMaps.Add(map.Index);

            if (is3D)
            {
                var block = map.Blocks[x, y];
                block.ObjectIndex = changeTileEvent.ObjectIndex;
                block.WallIndex = changeTileEvent.WallIndex;
                block.MapBorder = false;

                if (sameMap)
                    renderMap3D.UpdateBlock(x, y);
            }
            else // 2D
            {
                map.UpdateTile(x, y, changeTileEvent.FrontTileIndex, MapManager.GetTilesetForMap(map));

                if (renderMap2D.IsMapVisible(changeTileEvent.MapIndex, ref x, ref y))
                    renderMap2D.UpdateTile(x, y);
            }
        }

        internal void SetMapEventBit(uint mapIndex, uint eventListIndex, bool bit)
        {
            CurrentSavegame.SetEventBit(mapIndex, eventListIndex, bit);
        }

        internal void SetMapCharacterBit(uint mapIndex, uint characterIndex, bool bit)
        {
            CurrentSavegame.SetCharacterBit(mapIndex, characterIndex, bit);

            // Note: That might not work for world maps but there are no characters on those maps.
            if (Map.Index == mapIndex)
            {
                if (is3D)
                {
                    renderMap3D.UpdateCharacterVisibility(characterIndex);
                }
                else
                {
                    renderMap2D.UpdateCharacterVisibility(characterIndex);
                }
            }
        }

        void ChestRemoved()
        {
            var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
            var position = (Position)currentWindow.WindowParameters[4];

            CloseWindow(() =>
            {
                if (chestEvent.AutoRemove)
                {
                    bool RemoveFromMap(Map map)
                    {
                        for (int i = 0; i < map.EventList.Count; ++i)
                        {
                            var @event = map.EventList[i];

                            while (@event != null)
                            {
                                if (@event == chestEvent)
                                {
                                    // If the chest event chain was triggered by a character interaction
                                    // only remove that character but keep the event active as it might
                                    // be used by other characters (e.g. mushrooms in Dor Grestin).
                                    // But if this was triggered by a normal map event, just deactivate
                                    // the map event so that the chest is removed from the map.
                                    if (CurrentMapCharacter?.CheckDeactivation((uint)i + 1) != true)
                                        CurrentSavegame.ActivateEvent(map.Index, (uint)i, false);
                                    return true;
                                }

                                @event = @event.Next;
                            }
                        }

                        return false;
                    }

                    if (!Map.IsWorldMap)
                        RemoveFromMap(Map);
                    else
                    {
                        if (!RemoveFromMap(Map) &&
                            !RemoveFromMap(MapManager.GetMap(Map.RightMapIndex.Value)) &&
                            !RemoveFromMap(MapManager.GetMap(Map.DownMapIndex.Value)))
                            RemoveFromMap(MapManager.GetMap(Map.DownRightMapIndex.Value));
                    }
                }
                if (chestEvent.Next != null)
                {
                    Map.TriggerEventChain(this, EventTrigger.Always, (uint)(position?.X ?? 0),
                        (uint)(position?.Y ?? 0), CurrentTicks, chestEvent.Next, true);
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
                        layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                        // If a chest has AllowsItemDrop = false this
                        // means it is removed when it is empty.
                        if (!chest.AllowsItemDrop)
                            ChestRemoved();
                    }
                    else
                    {
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
            var chest = OpenStorage as Chest;

            if (chest.Gold > 0)
            {
                if (!chest.IsBattleLoot)
                    layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                ShowTextPanel(CharacterInfo.ChestGold, true,
                    $"{DataNameProvider.GoldName}^{chest.Gold}", new Rect(111, 104, 43, 15));
            }
            else
            {
                HideTextPanel(CharacterInfo.ChestGold);

                if (chest.Empty && !chest.IsBattleLoot)
                {
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                    if (!chest.AllowsItemDrop)
                        ChestRemoved();
                }
            }
        }

        internal void ChestFoodChanged()
        {
            var chest = OpenStorage as Chest;

            if (chest.Food > 0)
            {
                if (!chest.IsBattleLoot)
                    layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                ShowTextPanel(CharacterInfo.ChestFood, true,
                    $"{DataNameProvider.FoodName}^{chest.Food}", new Rect(260, 104, 43, 15));
            }
            else
            {
                HideTextPanel(CharacterInfo.ChestFood);

                if (chest.Empty && !chest.IsBattleLoot)
                {
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                    if (!chest.AllowsItemDrop)
                        ChestRemoved();
                }
            }
        }

        void ShowLoot(ITreasureStorage storage, string initialText, Action initialTextClosedEvent, ChestEvent chestEvent = null)
        {
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

            if (storage.IsBattleLoot)
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

            itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                int column = slotIndex % Chest.SlotsPerRow;
                int row = slotIndex / Chest.SlotsPerRow;
                storage.Slots[column, row].Remove(amount);
            };
            itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                if (!storage.IsBattleLoot)
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

        internal void ShowChest(ChestEvent chestEvent, bool foundTrap, bool disarmedTrap, Map map, Position position)
        {
            var chest = GetChest(1 + chestEvent.ChestIndex);

            if (chestEvent.CloseWhenEmpty && chest.Empty)
            {
                // This is used by the flowers on Kire's moon
                // or the flowers on Lyramion.
                // Chest events reuse the same chest and it is
                // refilled each time another flower is looted.
                var initialChest = GetInitialChest(1 + chestEvent.ChestIndex);
                if (initialChest == null)
                    return;
                chest.Gold = initialChest.Gold;
                chest.Food = initialChest.Food;

                for (int y = 0; y < Chest.SlotRows; ++y)
                {
                    for (int x = 0; x < Chest.SlotsPerRow; ++x)
                        chest.Slots[x, y].Replace(initialChest.Slots[x, y]);
                }
            }

            void OpenChest()
            {
                string initialText = map != null && chestEvent.TextIndex != 255 ?
                    map.Texts[(int)chestEvent.TextIndex] : null;
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Chest, chestEvent, foundTrap, disarmedTrap, map, position);
                CursorType = CursorType.Sword;
                ResetMapCharacterInteraction(map ?? Map, true);

                if (chestEvent.LockpickingChanceReduction != 0 && CurrentSavegame.IsChestLocked(chestEvent.ChestIndex))
                {
                    ShowLocked(Picture80x80.ChestClosed, () =>
                    {
                        CurrentSavegame.UnlockChest(chestEvent.ChestIndex);
                        currentWindow.Window = Window.Chest; // This avoids returning to locked screen when closing chest window.
                        ExecuteNextUpdateCycle(() => ShowChest(chestEvent, false, false, map, position));
                    }, initialText, chestEvent.KeyIndex, chestEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
                    chestEvent.UnlockFailedEventIndex == 0xffff ? (Action)null : () => map.TriggerEventChain(this, EventTrigger.Always,
                    (uint)player.Position.X, (uint)player.Position.Y, CurrentTicks, map.Events[(int)chestEvent.UnlockFailedEventIndex], true));
                }
                else
                {
                    ShowLoot(chest, initialText, null, chestEvent);
                }
            }

            if (CurrentWindow.Window == Window.Chest)
                OpenChest();
            else
                Fade(OpenChest);
        }

        internal bool ShowDoor(DoorEvent doorEvent, bool foundTrap, bool disarmedTrap, Map map, uint x, uint y)
        {
            if (!CurrentSavegame.IsDoorLocked(doorEvent.DoorIndex))
                return false;

            Fade(() =>
            {
                string initialText = doorEvent.TextIndex != 255 ?
                    map.Texts[(int)doorEvent.TextIndex] : null;
                string unlockText = doorEvent.UnlockTextIndex != 255 ?
                    map.Texts[(int)doorEvent.UnlockTextIndex] : null;
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Door, doorEvent, foundTrap, disarmedTrap, map, x, y);
                ShowLocked(Picture80x80.Door, () =>
                {
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
                            int eventIndex = map.EventList.IndexOf(doorEvent);
                            if (eventIndex != -1)
                                CurrentSavegame.ActivateEvent(map.Index, (uint)eventIndex, false);
                            if (doorEvent.Next != null)
                            {
                                EventExtensions.TriggerEventChain(map ?? Map, this, EventTrigger.Always, (uint)player.Position.X,
                                    (uint)player.Position.Y, CurrentTicks, doorEvent.Next, true);
                            }
                        });
                    }
                }, initialText, doorEvent.KeyIndex, doorEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
                doorEvent.UnlockFailedEventIndex == 0xffff ? (Action)null : () => map.TriggerEventChain(this, EventTrigger.Always,
                    (uint)player.Position.X, (uint)player.Position.Y, CurrentTicks, map.Events[(int)doorEvent.UnlockFailedEventIndex], true));
            });

            return true;
        }

        void ShowLocked(Picture80x80 picture80X80, Action openedAction, string initialMessage,
            uint keyIndex, uint lockpickingChanceReduction, bool foundTrap, bool disarmedTrap, Action failedAction)
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
            const uint LockpickItemIndex = 138;

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

            ActivePlayerChanged += PlayerSwitched;

            void Exit()
            {
                ActivePlayerChanged -= PlayerSwitched;
                CloseWindow();
            }

            void StartUseItems()
            {
                if (chest)
                    layout.ShowChestMessage(DataNameProvider.WhichItemToOpenChest, TextAlign.Left);
                else
                    layout.ShowChestMessage(DataNameProvider.WhichItemToOpenDoor, TextAlign.Left);

                itemGrid.Disabled = false;
                itemGrid.DisableDrag = true;
                itemGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
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

            itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
            {
                UntrapMouse();
                nextClickHandler = null;
                layout.ShowChestMessage(null);
                StartSequence();
                itemGrid.HideTooltip();
                var targetPosition = chest ? new Position(28, 76) : new Position(73, 102);
                itemGrid.PlayMoveAnimation(itemSlot, targetPosition, () =>
                {
                    bool canOpen = keyIndex == itemSlot.ItemIndex || (keyIndex == 0 && itemSlot.ItemIndex == LockpickItemIndex);
                    var item = layout.GetItem(itemSlot);
                    item.ShowItemAmount = false;

                    itemGrid.PlayShakeAnimation(itemSlot, () =>
                    {
                        EndSequence();
                        if (canOpen)
                        {
                            Unlocked(itemSlot.ItemIndex == LockpickItemIndex, () =>
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
                                    InventoryItemRemoved(itemSlot.ItemIndex, 1, CurrentPartyMember);
                                    itemSlot.Remove(1);                                    
                                });
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
                                        InventoryItemRemoved(itemSlot.ItemIndex, 1, CurrentPartyMember);
                                        itemSlot.Remove(1);
                                        if (itemSlot.Amount > 0)
                                        {
                                            StartSequence();
                                            itemGrid.HideTooltip();
                                            itemGrid.PlayMoveAnimation(itemSlot, itemGrid.GetSlotPosition(itemGrid.SlotFromItemSlot(itemSlot)), () =>
                                            {
                                                itemGrid.ResetAnimation(itemSlot);
                                                EndSequence();
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
                                            else
                                            {
                                                itemGrid.ResetAnimation(itemSlot);
                                                item.ShowItemAmount = true;
                                                item.Visible = true;
                                                StartUseItems();
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
                                        StartUseItems();
                                    });
                                });
                            }
                        }
                    });
                });
            };

            // Lockpick button
            layout.AttachEventToButton(0, () =>
            {
                // TODO: Can locks theoretically be lockpicked if they need a key? I guess in Ambermoon all locks with key have a lockpickingChanceReduction of 100%.
                //       But what would happen if this value was below 100% for such doors? For now we allow lockpicking those doors as we don't check for key index.
                int chance = Util.Limit(0, (int)CurrentPartyMember.Abilities[Ability.LockPicking].TotalCurrentValue, 100) - (int)lockpickingChanceReduction;

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
                int chance = Util.Limit(0, (int)CurrentPartyMember.Abilities[Ability.FindTraps].TotalCurrentValue, 100);

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
                int chance = Util.Limit(0, (int)CurrentPartyMember.Abilities[Ability.DisarmTraps].TotalCurrentValue, 100); // TODO: Is there a "find trap" reduction as well?

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
        }

        /// <summary>
        /// A conversation is started with a Conversation event but the
        /// displayed text depends on the following events. Mostly
        /// Condition and PrintText events. The argument conversationEvent
        /// is the initial conversation event of interaction type 'Talk'
        /// and should be used to determine the text to print etc.
        /// 
        /// The event chain may also contain rewards, new keywords, etc.
        /// </summary>
        internal void ShowConversation(IConversationPartner conversationPartner, Event conversationEvent,
            ConversationItems createdItems, bool showInitialText = true)
        {
            if (!(conversationPartner is Character character))
                throw new AmbermoonException(ExceptionScope.Application, "Conversation partner is no character.");

            if ((character.SpokenLanguages & CurrentPartyMember.SpokenLanguages) == 0)
            {
                ShowMessagePopup(DataNameProvider.YouDontSpeakSameLanguage);
                return;
            }

            IEnumerable<ConversationEvent> GetMatchingEvents(Func<ConversationEvent, bool> filter)
                => conversationPartner.EventList.OfType<ConversationEvent>().Where(filter);

            ConversationEvent GetFirstMatchingEvent(Func<ConversationEvent, bool> filter)
                => conversationPartner.EventList.OfType<ConversationEvent>().FirstOrDefault(filter);

            void SwitchPlayer()
            {
                if (CurrentWindow.Window == Window.Conversation)
                {
                    UpdateCharacterInfo(character);
                    UpdateButtons();
                }
            }

            OpenStorage = createdItems;
            ActivePlayerChanged += SwitchPlayer;

            conversationEvent ??= GetFirstMatchingEvent(e => e.Interaction == InteractionType.Talk);

            var createdItemSlots = createdItems.Slots.ToList();
            var currentInteractionType = InteractionType.Talk;
            bool lastEventStatus = true;
            bool aborted = false;
            var textArea = new Rect(17, 44, 174, 79);
            UIText conversationText = null;
            ItemGrid itemGrid = null;
            var oldKeywords = new List<string>(Dictionary);
            var newKeywords = new List<string>();
            uint amount = 0; // gold, food, etc
            UIText moveItemMessage = null;
            layout.DraggedItemDropped += DraggedItemDropped;
            closeWindowHandler = CleanUp;            

            void SetText(string text, Action followAction = null)
            {
                conversationText.Visible = true;
                conversationText.SetText(ProcessText(text));
                conversationText.Clicked += TextClicked;
                CursorType = CursorType.Click;
                InputEnable = false;
                ConversationTextActive = true;

                void TextClicked(bool toEnd)
                {
                    if (toEnd)
                    {
                        conversationText.Clicked -= TextClicked;
                        conversationText.Visible = false;
                        InputEnable = true;
                        ConversationTextActive = false;
                        ExecuteNextUpdateCycle(() =>
                        {
                            CursorType = CursorType.Sword;
                            followAction?.Invoke();
                        });
                    }
                }
            }

            void ShowDictionary()
            {
                OpenDictionary(SayWord, word => !oldKeywords.Contains(word) || newKeywords.Contains(word)
                    ? TextColor.LightYellow : TextColor.BrightGray);
            }

            void SayWord(string keyword)
            {
                ClosePopup();
                UntrapMouse();

                foreach (var e in GetMatchingEvents(e => e.Interaction == InteractionType.Keyword))
                {
                    var expectedKeyword = textDictionary.Entries[(int)e.KeywordIndex];

                    if (string.Compare(keyword, expectedKeyword, true) == 0)
                    {
                        currentInteractionType = InteractionType.Keyword;
                        conversationEvent = e;
                        aborted = false;
                        lastEventStatus = true;
                        HandleNextEvent();
                        return;
                    }
                }

                // There is no event for it so just display a message.
                SetText(DataNameProvider.DontKnowAnythingSpecialAboutIt);
            }

            void ShowItems(string text, InteractionType interactionType)
            {
                currentInteractionType = interactionType;

                var message = layout.AddText(textArea, ProcessText(text, textArea), TextColor.BrightGray);

                void Abort()
                {
                    itemGrid.HideTooltip();
                    itemGrid.ItemClicked -= ItemClicked;
                    message?.Destroy();
                    UntrapMouse();
                    nextClickHandler = null;
                    ShowCreatedItems();
                }

                itemGrid.Disabled = false;
                itemGrid.DisableDrag = true;
                CursorType = CursorType.Sword;
                var itemArea = new Rect(16, 139, 151, 53);
                TrapMouse(itemArea);
                itemGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                void SetupRightClickAbort()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            Abort();
                            return true;
                        }

                        return false;
                    };
                }
                SetupRightClickAbort();
                void CheckItem(ItemSlot itemSlot)
                {
                    void MoveBack(Action followAction)
                    {
                        StartSequence();
                        itemGrid.HideTooltip();
                        itemGrid.PlayMoveAnimation(itemSlot, itemGrid.GetSlotPosition(itemGrid.SlotFromItemSlot(itemSlot)), () =>
                        {
                            itemGrid.ResetAnimation(itemSlot);
                            EndSequence();
                            Abort();
                        }, 650);
                    }
                    EndSequence();
                    message?.Destroy();
                    message = null;
                    layout.GetItem(itemSlot).Dragged = true; // Keep it above UI
                    UntrapMouse();
                    conversationEvent = GetFirstMatchingEvent(e => e.Interaction == interactionType && e.ItemIndex == itemSlot.ItemIndex);

                    if (conversationEvent == null)
                    {
                        SetText(DataNameProvider.NotInterestedInItem, () => MoveBack(null));
                    }
                    else
                    {
                        void HandleInteraction()
                        {
                            HandleNextEvent(eventType =>
                            {
                                // Note: A create event must also trigger the item consumption.
                                // Otherwise we might have two item grids interfering.
                                if (eventType == EventType.Interact || eventType == EventType.Create)
                                {
                                    // If we are here the user clicked the associated text etc.
                                    if (interactionType == InteractionType.GiveItem)
                                    {
                                        // Consume
                                        StartSequence();
                                        itemGrid.HideTooltip();
                                        layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true, () =>
                                        {
                                            InventoryItemRemoved(itemSlot.ItemIndex, 1, CurrentPartyMember);
                                            itemSlot.Remove(1);
                                            ShowCreatedItems();
                                            EndSequence();
                                            Abort();
                                            HandleNextEvent(null);
                                        }, new Position(215, 75), false);
                                    }
                                    else // Show item
                                    {
                                        MoveBack(() => HandleNextEvent(null));
                                    }
                                }
                                else if (eventType == EventType.Invalid) // End of event chain
                                {
                                    MoveBack(null);
                                }
                                else
                                {
                                    HandleInteraction();
                                }
                            });
                        }

                        HandleInteraction();
                    }
                }
                void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    itemGrid.ItemClicked -= ItemClicked;
                    nextClickHandler = null;
                    UntrapMouse();
                    StartSequence();
                    itemGrid.HideTooltip();
                    itemGrid.PlayMoveAnimation(itemSlot, new Position(215, 75), () => CheckItem(itemSlot), 650);
                }
                itemGrid.ItemClicked += ItemClicked;
            }

            void ShowItem()
            {
                ShowItems(DataNameProvider.WhichItemToShow, InteractionType.ShowItem);
            }

            void GiveItem()
            {
                ShowItems(DataNameProvider.WhichItemToGive, InteractionType.GiveItem);
            }

            void GiveGold()
            {
                layout.OpenAmountInputBox(DataNameProvider.GiveHowMuchGoldToNPC, 96, DataNameProvider.GoldName,
                    CurrentPartyMember.Gold, gold =>
                    {
                        ClosePopup();
                        var @event = GetFirstMatchingEvent(e => e.Interaction == InteractionType.GiveGold);
                        if (@event != null)
                        {
                            if (gold < @event.Value)
                            {
                                SetText(DataNameProvider.MoreGoldNeeded);
                            }
                            else
                            {
                                conversationEvent = @event;
                                currentInteractionType = InteractionType.GiveGold;
                                amount = @event.Value;
                                aborted = false;
                                lastEventStatus = true;
                                HandleNextEvent();
                            }
                        }
                        else
                        {
                            SetText(DataNameProvider.NotInterestedInGold);
                        }
                    });
            }

            void GiveFood()
            {
                layout.OpenAmountInputBox(DataNameProvider.GiveHowMuchFoodToNPC, 109, DataNameProvider.FoodName,
                    CurrentPartyMember.Food, food =>
                    {
                        ClosePopup();
                        var @event = GetFirstMatchingEvent(e => e.Interaction == InteractionType.GiveFood);
                        if (@event != null)
                        {
                            if (food < @event.Value)
                            {
                                SetText(DataNameProvider.MoreFoodNeeded);
                            }
                            else
                            {
                                conversationEvent = @event;
                                currentInteractionType = InteractionType.GiveFood;
                                amount = @event.Value;
                                aborted = false;
                                lastEventStatus = true;
                                HandleNextEvent();
                            }
                        }
                        else
                        {
                            SetText(DataNameProvider.NotInterestedInFood);
                        }
                    });
            }

            void AskToJoin()
            {
                if (PartyMembers.Count() == MaxPartyMembers)
                {
                    conversationEvent = null;
                    SetText(DataNameProvider.PartyFull);
                    return;
                }

                if (character is PartyMember &&
                    (conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.JoinParty)) != null)
                {
                    currentInteractionType = InteractionType.JoinParty;
                    aborted = false;
                    lastEventStatus = true;
                    HandleNextEvent();
                }
                else
                {
                    SetText(DataNameProvider.DenyJoiningParty);
                }
            }

            void AskToLeave()
            {
                if (character is PartyMember partyMember && PartyMembers.Contains(partyMember))
                {
                    if (Map.World == World.ForestMoon)
                    {
                        SetText(DataNameProvider.DenyLeavingPartyOnForestMoon);
                        return;
                    }

                    if ((conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.LeaveParty)) != null)
                    {
                        currentInteractionType = InteractionType.LeaveParty;
                        aborted = false;
                        lastEventStatus = true;
                        HandleNextEvent();
                    }
                    else
                        RemovePartyMember(() => Exit()); // Just remove from party and close
                }
            }

            void AddPartyMember(Action followAction)
            {
                for (int i = 0; i < MaxPartyMembers; ++i)
                {
                    if (GetPartyMember(i) == null)
                    {
                        var partyMember = character as PartyMember;
                        var index = partyMember.CharacterBitIndex;
                        uint mapIndex = 1 + ((uint)index >> 5);
                        uint characterIndex = (uint)index & 0x1f;
                        CurrentSavegame.CurrentPartyMemberIndices[i] =
                            CurrentSavegame.PartyMembers.FirstOrDefault(p => p.Value == partyMember).Key;
                        this.AddPartyMember(i, partyMember, followAction, true);
                        // Set battle position
                        CurrentSavegame.BattlePositions[i] = 0xff;
                        var usePositions = CurrentSavegame.BattlePositions.ToList();
                        for (int p = 11; p >= 0; --p)
                        {
                            if (!usePositions.Contains((byte)p))
                            {
                                CurrentSavegame.BattlePositions[i] = (byte)p;
                                break;
                            }
                        }
                        layout.EnableButton(4, true); // Enable "Ask to leave"
                        layout.EnableButton(5, false); // Disable "Ask to join"
                        SetMapCharacterBit(mapIndex, characterIndex, true);
                        break;
                    }
                }
            }

            void RemovePartyMember(Action followAction)
            {
                var partyMember = character as PartyMember;
                var index = partyMember.CharacterBitIndex;
                uint mapIndex = 1 + ((uint)index >> 5);
                uint characterIndex = (uint)index & 0x1f;
                this.RemovePartyMember(SlotFromPartyMember(character as PartyMember).Value, false, followAction);
                CurrentSavegame.CurrentPartyMemberIndices[SlotFromPartyMember(partyMember).Value] = 0;
                SetMapCharacterBit(mapIndex, characterIndex, false);
            }

            void ShowCreatedItems()
            {
                if (createdItemSlots.Any(item => !item.Empty))
                {
                    itemGrid.Disabled = false;
                    itemGrid.DisableDrag = false;
                    itemGrid.Initialize(createdItemSlots, false);
                }
                else
                {
                    itemGrid.Disabled = true;
                }
            }

            void CreateItem(uint itemIndex, uint amount)
            {
                // Note: Multiple items can be created. While at least one
                // item was created and is not picked up, the item grid is
                // enabled.
                for (int i = 0; i < 24; ++i)
                {
                    if (createdItemSlots[i].Empty)
                    {
                        var item = ItemManager.GetItem(itemIndex);
                        createdItemSlots[i].ItemIndex = itemIndex;
                        createdItemSlots[i].Amount = (int)amount;
                        createdItemSlots[i].NumRemainingCharges = Math.Max(1, (int)item.InitialCharges);
                        createdItemSlots[i].Flags = item.DefaultSlotFlags;
                        createdItemSlots[i].RechargeTimes = 0;
                        break;
                    }
                }
                ShowCreatedItems();
            }

            void Exit(bool showLeaveMessage = false)
            {
                if (showLeaveMessage)
                {
                    if (createdItems.HasAnyImportantItem(ItemManager))
                    {
                        SetText(DataNameProvider.DontForgetItems +
                            string.Join(", ", createdItems.GetImportantItemNames(ItemManager)) + ".");
                        return;
                    }

                    conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.Leave);

                    if (conversationEvent != null)
                    {
                        currentInteractionType = InteractionType.Leave;
                        aborted = false;
                        lastEventStatus = true;
                        HandleNextEvent();
                        return;
                    }
                }

                CloseWindow();
            }

            void CleanUp()
            {
                layout.DraggedItemDropped -= DraggedItemDropped;
                ActivePlayerChanged -= SwitchPlayer;
                ConversationTextActive = false;
            }

            void HandleNextEvent(Action<EventType> followAction = null)
            {
                conversationEvent = conversationEvent?.Next;
                HandleEvent(followAction);
            }

            void HandleEvent(Action<EventType> followAction = null)
            {
                if (conversationEvent == null || aborted)
                {
                    if (currentInteractionType == InteractionType.LeaveParty ||
                        currentInteractionType == InteractionType.Leave)
                    {
                        Exit(); // After leaving the party or just leaving the conversation, close the window.
                    }

                    followAction?.Invoke(EventType.Invalid);

                    return;
                }

                var nextAction = followAction ?? (_ => HandleNextEvent());

                if (conversationEvent is PrintTextEvent printTextEvent)
                {
                    SetText(conversationPartner.Texts[(int)printTextEvent.NPCTextIndex], () => nextAction?.Invoke(EventType.PrintText));
                }
                else if (conversationEvent is ExitEvent)
                {
                    Exit();
                    nextAction?.Invoke(EventType.Exit);
                }
                else if (conversationEvent is CreateEvent createEvent)
                {
                    // Note: It is important to trigger the next action first
                    // as it might trigger a consumption of a previously given item.
                    // The create item only updates the grid of created items.
                    nextAction?.Invoke(EventType.Create);

                    switch (createEvent.TypeOfCreation)
                    {
                        case CreateEvent.CreateType.Item:
                            CreateItem(createEvent.ItemIndex, createEvent.Amount);
                            break;
                        case CreateEvent.CreateType.Gold:
                            CurrentPartyMember.AddGold(createEvent.Amount);
                            UpdateCharacterInfo(character);
                            break;
                        default: // food
                            CurrentPartyMember.AddFood(createEvent.Amount);
                            UpdateCharacterInfo(character);
                            break;
                    }
                    
                }
                else if (conversationEvent is InteractEvent)
                {
                    switch (currentInteractionType)
                    {
                        case InteractionType.GiveItem:
                        {
                            // Note: The ShowItems method will take care of it.
                            nextAction?.Invoke(EventType.Interact);
                            break;
                        }
                        case InteractionType.GiveGold:
                            CurrentPartyMember.RemoveGold(amount);
                            UpdateCharacterInfo(character);
                            nextAction?.Invoke(EventType.Interact);
                            break;
                        case InteractionType.GiveFood:
                            CurrentPartyMember.RemoveFood(amount);
                            UpdateCharacterInfo(character);
                            nextAction?.Invoke(EventType.Interact);
                            break;
                        case InteractionType.JoinParty:
                            AddPartyMember(() => nextAction?.Invoke(EventType.Interact));
                            break;
                        case InteractionType.LeaveParty:
                            RemovePartyMember(() => nextAction?.Invoke(EventType.Interact));
                            break;
                        case InteractionType.Leave:
                            Exit();
                            nextAction?.Invoke(EventType.Interact);
                            break;
                        default:
                            nextAction?.Invoke(EventType.Interact);
                            break;
                    }
                }
                else
                {
                    if (conversationEvent is ActionEvent actionEvent &&
                        actionEvent.TypeOfAction == ActionEvent.ActionType.AddKeyword)
                    {
                        string keyword = textDictionary.Entries[(int)actionEvent.ObjectIndex];

                        if (!newKeywords.Contains(keyword))
                            newKeywords.Add(keyword);
                    }
                    
                    if (conversationEvent.Type == EventType.Teleport ||
                        conversationEvent.Type == EventType.Chest ||
                        conversationEvent.Type == EventType.Door ||
                        conversationEvent.Type == EventType.EnterPlace ||
                        conversationEvent.Type == EventType.Riddlemouth ||
                        conversationEvent.Type == EventType.StartBattle)
                    {
                        CloseWindow(() => EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always,
                            (uint)player.Position.X, (uint)player.Position.Y, CurrentTicks, conversationEvent, true));
                    }
                    else
                    {
                        var trigger = EventTrigger.Always;
                        conversationEvent = EventExtensions.ExecuteEvent(conversationEvent, Map, this, ref trigger,
                            (uint)player.Position.X, (uint)player.Position.Y, CurrentTicks, ref lastEventStatus, out aborted,
                            out var eventProvider, conversationPartner);
                        if (conversationEvent == null && eventProvider != null)
                        {
                            if (eventProvider.Event != null)
                            {
                                conversationEvent = eventProvider.Event;
                                HandleEvent(followAction);
                            }
                            else
                            {
                                eventProvider.Provided += @event =>
                                {
                                    conversationEvent = @event;
                                    HandleEvent(followAction);
                                };
                            }
                        }
                        else
                        {
                            HandleEvent(followAction);
                        }
                    }
                }
            }

            void ItemDragged(int slotIndex, ItemSlot itemSlot, int amount)
            {
                ExecuteNextUpdateCycle(() =>
                {
                    moveItemMessage = layout.AddText(textArea, DataNameProvider.WhereToMoveIt,
                        TextColor.BrightGray, TextAlign.Center);
                    var draggedSourceSlot = itemGrid.GetItemSlot(slotIndex);
                    draggedSourceSlot.Remove(amount);
                    createdItemSlots[slotIndex].Replace(draggedSourceSlot);
                    itemGrid.SetItem(slotIndex, draggedSourceSlot);
                });
            }

            void DraggedItemDropped()
            {
                itemGrid.Disabled = !createdItemSlots.Any(slot => !slot.Empty);
                moveItemMessage?.Destroy();
                moveItemMessage = null;
            }

            void UpdateButtons()
            {
                bool enableItemButtons = CurrentPartyMember.Inventory.Slots.Any(s => s?.Empty == false);
                layout.EnableButton(3, enableItemButtons);
                layout.EnableButton(6, enableItemButtons);
                layout.EnableButton(7, CurrentPartyMember.Gold != 0);
                layout.EnableButton(8, CurrentPartyMember.Food != 0);
            }

            Fade(() =>
            {
                SetWindow(Window.Conversation, conversationPartner, conversationEvent, createdItems);
                layout.SetLayout(LayoutType.Conversation);
                ShowMap(false);
                layout.Reset();

                layout.FillArea(new Rect(15, 43, 177, 80), GetUIColor(28), false);
                layout.FillArea(new Rect(15, 136, 152, 57), GetUIColor(28), false);

                DisplayCharacterInfo(character, true);

                if (character.Type != CharacterType.PartyMember ||
                    SlotFromPartyMember(character as PartyMember) == null)
                    layout.EnableButton(4, false); // Disable "Ask to leave" if not in party
                if (character is PartyMember partyMember && PartyMembers.Contains(partyMember))
                    layout.EnableButton(5, false); // Disable "Ask to join" if already in party

                UpdateButtons();

                layout.AttachEventToButton(0, ShowDictionary);
                layout.AttachEventToButton(2, () => Exit(true));
                layout.AttachEventToButton(3, ShowItem);
                layout.AttachEventToButton(4, AskToLeave);
                layout.AttachEventToButton(5, AskToJoin);
                layout.AttachEventToButton(6, GiveItem);
                layout.AttachEventToButton(7, GiveGold);
                layout.AttachEventToButton(8, GiveFood);

                // Add item grid
                var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
                itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
                itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat(null as ItemSlot, 24).ToList(),
                    false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
                itemGrid.ItemDragged += ItemDragged;
                layout.AddItemGrid(itemGrid);
                ShowCreatedItems();

                // Note: Mouse handling in Layout assumes this is the last text (text[^1]) so ensure that.
                conversationText = layout.AddScrollableText(textArea, ProcessText(""), TextColor.BrightGray);
                conversationText.Visible = false;

                if (showInitialText)
                    HandleNextEvent();
            });
        }

        /// <summary>
        /// This is used by external triggers like a cheat engine.
        /// 
        /// Returns false if the current game state does not allow
        /// to start a fight.
        /// </summary>
        public bool StartBattle(uint monsterGroupIndex)
        {
            if (WindowActive || BattleActive || layout.PopupActive ||
                allInputDisabled || !inputEnable || !ingame)
                return false;

            StartBattle(monsterGroupIndex, false, null);
            return true;
        }

        /// <summary>
        /// Starts a battle with the given monster group index.
        /// It is used for monsters that are present on the map.
        /// </summary>
        /// <param name="monsterGroupIndex">Monster group index</param>
        internal void StartBattle(uint monsterGroupIndex, bool failedEscape,
            Action<BattleEndInfo> battleEndHandler, uint? combatBackgroundIndex = null)
        {
            if (BattleActive)
                return;

            currentBattleInfo = new BattleInfo
            {
                MonsterGroupIndex = monsterGroupIndex
            };
            currentBattleInfo.BattleEnded += battleEndHandler;
            ShowBattleWindow(null, failedEscape, combatBackgroundIndex);
        }

        void UpdateBattle()
        {
            if (advancing)
            {
                foreach (var monster in currentBattle.Monsters)
                    layout.GetMonsterBattleAnimation(monster).Update(CurrentBattleTicks);
            }
            else
            {
                currentBattle.Update(CurrentBattleTicks);
            }

            if (highlightBattleFieldSprites.Count != 0)
            {
                bool showBlinkingSprites = !blinkingHighlight || (CurrentBattleTicks % (2 * TicksPerSecond / 3)) < TicksPerSecond / 3;

                foreach (var blinkingBattleFieldSprite in highlightBattleFieldSprites)
                {
                    blinkingBattleFieldSprite.Visible = showBlinkingSprites;
                }
            }
        }

        UIGraphic GetDisabledStatusGraphic(PartyMember partyMember)
        {
            if (!partyMember.Alive)
                return UIGraphic.StatusDead;
            else if (partyMember.Ailments.HasFlag(Ailment.Petrified))
                return UIGraphic.StatusPetrified;
            else if (partyMember.Ailments.HasFlag(Ailment.Sleep))
                return UIGraphic.StatusSleep;
            else if (partyMember.Ailments.HasFlag(Ailment.Panic))
                return UIGraphic.StatusPanic;
            else if (partyMember.Ailments.HasFlag(Ailment.Crazy))
                return UIGraphic.StatusCrazy;
            else
                throw new AmbermoonException(ExceptionScope.Application, $"Party member {partyMember.Name} is not disabled.");
        }

        internal void UpdateBattleStatus(PartyMember partyMember)
        {
            UpdateBattleStatus(SlotFromPartyMember(partyMember).Value, partyMember);
        }

        void UpdateBattleStatus(int slot)
        {
            UpdateBattleStatus(slot, GetPartyMember(slot));
        }

        void UpdateBattleStatus(int slot, PartyMember partyMember)
        {
            if (partyMember == null)
            {
                layout.UpdateCharacterStatus(slot, null);
                roundPlayerBattleActions.Remove(slot);
            }
            else if (!partyMember.Ailments.CanSelect())
            {
                // Note: Disabled players will show the status icon next to
                // their portraits instead of an action icon. For mad players
                // when the battle starts the action icon will be shown instead.
                layout.UpdateCharacterStatus(slot, GetDisabledStatusGraphic(partyMember));
                roundPlayerBattleActions.Remove(slot);
            }
            else if (roundPlayerBattleActions.ContainsKey(slot))
            {
                var action = roundPlayerBattleActions[slot];
                layout.UpdateCharacterStatus(slot, action.BattleAction.ToStatusGraphic(action.Parameter, ItemManager));
            }
            else
            {
                layout.UpdateCharacterStatus(slot, null);
            }
        }

        void UpdateBattleStatus()
        {
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                UpdateBattleStatus(i);
            }

            layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
        }

        internal bool BattlePositionWindowClick(Position position, MouseButtons mouseButtons)
        {
            return battlePositionClickHandler?.Invoke(position, mouseButtons) ?? false;
        }

        internal void BattlePositionWindowDrag(Position position)
        {
            battlePositionDragHandler?.Invoke(position);
        }

        internal void ShowBattlePositionWindow()
        {
            Fade(() =>
            {
                SetWindow(Window.BattlePositions);
                layout.SetLayout(LayoutType.BattlePositions);
                ShowMap(false);
                layout.Reset();

                // Upper box
                var backgroundColor = GetUIColor(25);
                var upperBoxBounds = new Rect(14, 43, 290, 80);
                layout.FillArea(upperBoxBounds, GetUIColor(28), 0);
                var positionBoxes = new Rect[12];
                byte paletteIndex = UIPaletteIndex;
                var portraits = PartyMembers.ToDictionary(p => SlotFromPartyMember(p),
                    p => layout.AddSprite(new Rect(0, 0, 32, 34), Graphics.PortraitOffset + p.PortraitIndex - 1, paletteIndex, 5, p.Name, TextColor.White));
                var portraitBackgrounds = PartyMembers.ToDictionary(p => SlotFromPartyMember(p), _ => (FilledArea)null);
                var battlePositions = CurrentSavegame.BattlePositions.Select((p, i) => new { p, i }).Where(p => GetPartyMember(p.i) != null).ToDictionary(p => (int)p.p, p => p.i);
                // Each box is 34x36 pixels in size (with border)
                // 43 pixels y-offset to second row
                // Between each box there is a x-offset of 48 pixels
                for (int r = 0; r < 2; ++r)
                {
                    for (int c = 0; c < 6; ++c)
                    {
                        int index = c + r * 6;
                        var area = positionBoxes[index] = new Rect(15 + c * 48, 44 + r * 43, 34, 36);
                        layout.AddSunkenBox(area, 2);

                        if (battlePositions.ContainsKey(index))
                        {
                            int slot = battlePositions[index];
                            portraits[slot].X = area.Left + 1;
                            portraits[slot].Y = area.Top + 1;
                            portraitBackgrounds[slot]?.Destroy();
                            portraitBackgrounds[slot] = layout.FillArea(new Rect(area.Left + 1, area.Top + 1, 32, 34), backgroundColor, 4);
                        }
                    }
                }

                // Lower box
                var lowerBoxBounds = new Rect(16, 144, 176, 48);
                layout.FillArea(lowerBoxBounds, GetUIColor(28), 0);
                layout.AddText(lowerBoxBounds, DataNameProvider.ChooseBattlePositions);

                closeWindowHandler = () =>
                {
                    battlePositionClickHandler = null;
                    battlePositionDragHandler = null;
                    battlePositionDragging = false;

                    if (battlePositions.Count != PartyMembers.Count())
                        throw new AmbermoonException(ExceptionScope.Application, "Invalid number of battle positions.");

                    foreach (var battlePosition in battlePositions)
                    {
                        if (battlePosition.Value < 0 || battlePosition.Value >= MaxPartyMembers || GetPartyMember(battlePosition.Value) == null)
                            throw new AmbermoonException(ExceptionScope.Application, $"Invalid party member slot: {battlePosition.Value}.");
                        if (battlePosition.Key < 0 || battlePosition.Key >= 12)
                            throw new AmbermoonException(ExceptionScope.Application, $"Invalid battle position for party member slot {battlePosition.Value}: {battlePosition.Key}");
                        CurrentSavegame.BattlePositions[battlePosition.Value] = (byte)battlePosition.Key;
                    }
                };

                // Quick&dirty dragging logic
                int? slotOfDraggedPartyMember = null;
                int? dragSource = null;
                void Pickup(int position, bool trap = true, int? specificPartyMemberSlot = null)
                {
                    slotOfDraggedPartyMember = specificPartyMemberSlot ?? battlePositions[position];
                    dragSource = position;
                    battlePositionDragging = true;
                    if (trap)
                        TrapMouse(upperBoxBounds);
                }
                void Drop(int position, bool untrap = true)
                {
                    if (slotOfDraggedPartyMember != null)
                    {
                        var area = positionBoxes[position];
                        int slot = slotOfDraggedPartyMember.Value;
                        var draggedPortrait = portraits[slot];
                        draggedPortrait.DisplayLayer = 5;
                        draggedPortrait.X = area.Left + 1;
                        draggedPortrait.Y = area.Top + 1;
                        portraitBackgrounds[slot]?.Destroy();
                        portraitBackgrounds[slot] = layout.FillArea(new Rect(area.Left + 1, area.Top + 1, 32, 34), backgroundColor, 4);
                        slotOfDraggedPartyMember = null;
                        dragSource = null;
                        battlePositionDragging = false;
                        if (untrap)
                            UntrapMouse();
                    }
                }
                void Drag(Position position)
                {
                    if (slotOfDraggedPartyMember != null)
                    {
                        int slot = slotOfDraggedPartyMember.Value;
                        var draggedPortrait = portraits[slot];
                        draggedPortrait.DisplayLayer = 7;
                        draggedPortrait.X = position.X;
                        draggedPortrait.Y = position.Y;
                        portraitBackgrounds[slot]?.Destroy();
                        portraitBackgrounds[slot] = layout.FillArea(new Rect(position.X, position.Y, 32, 34), backgroundColor, 6);
                    }
                }
                void Reset(Position position)
                {
                    // Reset back to source
                    // If there is already a party member, exchange instead
                    if (battlePositions[dragSource.Value] == slotOfDraggedPartyMember.Value)
                        Drop(dragSource.Value);
                    else
                    {
                        // Exchange portrait
                        int index = dragSource.Value;
                        var temp = battlePositions[index];
                        battlePositions[index] = slotOfDraggedPartyMember.Value;
                        Drop(index, false);
                        Pickup(index, false, temp);
                        Drag(position);
                    }
                }
                battlePositionClickHandler = (position, mouseButtons) =>
                {
                    if (mouseButtons == MouseButtons.Left)
                    {
                        for (int i = 0; i < positionBoxes.Length; ++i)
                        {
                            if (positionBoxes[i].Contains(position))
                            {
                                if (slotOfDraggedPartyMember == null) // Not dragging
                                {
                                    if (battlePositions.ContainsKey(i))
                                    {
                                        // Drag portrait
                                        Pickup(i);
                                        Drag(position);
                                    }
                                }
                                else // Dragging
                                {
                                    if (battlePositions.ContainsKey(i))
                                    {
                                        if (battlePositions[i] != slotOfDraggedPartyMember.Value)
                                        {
                                            // Exchange portrait
                                            var temp = battlePositions[i];
                                            battlePositions[i] = slotOfDraggedPartyMember.Value;
                                            if (battlePositions[dragSource.Value] == slotOfDraggedPartyMember.Value)
                                                battlePositions.Remove(dragSource.Value);
                                            Drop(i, false);
                                            Pickup(i, false, temp);
                                            Drag(position);
                                        }
                                        else
                                        {
                                            // Put back
                                            Drop(i);
                                        }
                                    }
                                    else
                                    {
                                        // Drop portrait
                                        battlePositions[i] = slotOfDraggedPartyMember.Value;
                                        if (battlePositions[dragSource.Value] == slotOfDraggedPartyMember.Value)
                                            battlePositions.Remove(dragSource.Value);
                                        Drop(i);
                                    }
                                }

                                return true;
                            }
                        }
                    }
                    else if (mouseButtons == MouseButtons.Right)
                    {
                        if (dragSource != null)
                        {
                            Reset(position);
                            return true;
                        }
                    }

                    return false;
                };
                battlePositionDragHandler = position =>
                {
                    Drag(position);
                };
            });
        }

        void ShowBattleWindow(Event nextEvent, out byte paletteIndex, uint? combatBackgroundIndex = null)
        {
            combatBackgroundIndex ??= is3D ? renderMap3D.CombatBackgroundIndex : Map.World switch
            {
                World.Lyramion => 0u,
                World.ForestMoon => 6u,
                World.Morag => 4u,
                _ => 0u
            };

            SetWindow(Window.Battle, nextEvent, combatBackgroundIndex);
            layout.SetLayout(LayoutType.Battle);
            ShowMap(false);
            layout.Reset();

            var combatBackground = is3D
                ? renderView.GraphicProvider.Get3DCombatBackground(combatBackgroundIndex.Value)
                : renderView.GraphicProvider.Get2DCombatBackground(combatBackgroundIndex.Value);
            paletteIndex = (byte)(combatBackground.Palettes[GameTime.CombatBackgroundPaletteIndex()] - 1);
            layout.AddSprite(Global.CombatBackgroundArea, Graphics.CombatBackgroundOffset + combatBackground.GraphicIndex - 1,
                paletteIndex, 1, null, null, Layer.CombatBackground);
            layout.FillArea(new Rect(0, 132, 320, 68), Render.Color.Black, 0);
            layout.FillArea(new Rect(5, 139, 84, 56), GetUIColor(28), 1);

            if (currentBattle != null)
            {
                var monsterBattleAnimations = new Dictionary<int, BattleAnimation>(24);
                foreach (var monster in currentBattle.Monsters)
                {
                    int slot = currentBattle.GetSlotFromCharacter(monster);
                    monsterBattleAnimations.Add(slot, layout.AddMonsterCombatSprite(slot % 6, slot / 6, monster,
                        currentBattle.GetMonsterDisplayLayer(monster, slot), paletteIndex));
                }
                currentBattle.SetMonsterAnimations(monsterBattleAnimations);
            }

            // Add battle field sprites for party members
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                var partyMember = GetPartyMember(i);

                if (partyMember == null || !partyMember.Alive || HasPartyMemberFled(partyMember))
                {
                    partyMemberBattleFieldSprites[i] = null;
                    partyMemberBattleFieldTooltips[i] = null;
                }
                else
                {
                    var battlePosition = currentBattle == null ? 18 + CurrentSavegame.BattlePositions[i] : currentBattle.GetSlotFromCharacter(partyMember);
                    var battleColumn = battlePosition % 6;
                    var battleRow = battlePosition / 6;

                    partyMemberBattleFieldSprites[i] = layout.AddSprite(new Rect
                    (
                        Global.BattleFieldX + battleColumn * Global.BattleFieldSlotWidth,
                        Global.BattleFieldY + battleRow * Global.BattleFieldSlotHeight - 1,
                        Global.BattleFieldSlotWidth,
                        Global.BattleFieldSlotHeight + 1
                    ), Graphics.BattleFieldIconOffset + (uint)partyMember.Class, PrimaryUIPaletteIndex, (byte)(3 + battleRow),
                    $"{partyMember.HitPoints.CurrentValue}/{partyMember.HitPoints.TotalMaxValue}^{partyMember.Name}",
                    partyMember.Ailments.CanSelect() ? TextColor.White : TextColor.DeadPartyMember, null, out partyMemberBattleFieldTooltips[i]);
                }
            }
            UpdateBattleStatus();
            UpdateActiveBattleSpells();

            SetupBattleButtons();
        }

        internal void SetupBattleButtons()
        {
            // Flee button
            layout.AttachEventToButton(0, () =>
            {
                SetCurrentPlayerBattleAction(Battle.BattleActionType.Flee);
            });
            // OK button
            layout.AttachEventToButton(2, () =>
            {
                StartBattleRound(false);
            });
            // Move button
            layout.AttachEventToButton(3, () =>
            {
                SetCurrentPlayerAction(PlayerBattleAction.PickMoveSpot);
            });
            // Move group forward button
            layout.AttachEventToButton(4, () =>
            {
                SetBattleMessageWithClick(DataNameProvider.BattleMessagePartyAdvances, TextColor.BrightGray, () =>
                {
                    InputEnable = false;
                    currentBattle.WaitForClick = true;
                    CursorType = CursorType.Click;
                    allInputDisabled = true;
                    AdvanceParty(() =>
                    {
                        allInputDisabled = false;
                        InputEnable = true;
                        currentBattle.WaitForClick = false;
                        CursorType = CursorType.Sword;
                    });
                });
            });
            // Attack button
            layout.AttachEventToButton(6, () =>
            {
                SetCurrentPlayerAction(PlayerBattleAction.PickAttackSpot);
            });
            // Parry button
            layout.AttachEventToButton(7, () =>
            {
                SetCurrentPlayerBattleAction(Battle.BattleActionType.Parry);
            });
            // Use magic button
            layout.AttachEventToButton(8, () =>
            {
                if (!CurrentPartyMember.HasAnySpell())
                {
                    ShowMessagePopup(DataNameProvider.YouDontKnowAnySpellsYet);
                }
                else
                {
                    OpenSpellList(CurrentPartyMember,
                        spell =>
                        {
                            var spellInfo = SpellInfos.Entries[spell];

                            if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle))
                                return DataNameProvider.WrongArea;

                            var worldFlag = (WorldFlag)(1 << (int)Map.World);

                            if (!spellInfo.Worlds.HasFlag(worldFlag))
                                return DataNameProvider.WrongWorld;

                            if (spellInfo.SP > CurrentPartyMember.SpellPoints.CurrentValue)
                                return DataNameProvider.NotEnoughSP;

                            // TODO: Is there more to check? Irritated?

                            return null;
                        },
                        spell => PickBattleSpell(spell)
                    );
                }
            });
            if (currentBattle != null)
                BattlePlayerSwitched();
        }

        internal void PickBattleSpell(Spell spell, uint? itemSlotIndex = null, bool? itemIsEquipped = null,
            PartyMember caster = null)
        {
            ExecuteNextUpdateCycle(() =>
            {
                highlightBattleFieldSprites.ForEach(s => s?.Delete());
                highlightBattleFieldSprites.Clear();
                SetCurrentPlayerBattleAction(Battle.BattleActionType.None);

                pickedSpell = spell;
                spellItemSlotIndex = itemSlotIndex;
                spellItemIsEquipped = itemIsEquipped;
                currentPickingActionMember = caster ?? CurrentPartyMember;

                var spellInfo = SpellInfos.Entries[pickedSpell];

                switch (spellInfo.Target)
                {
                    case SpellTarget.SingleEnemy:
                        SetCurrentPlayerAction(PlayerBattleAction.PickEnemySpellTarget);
                        break;
                    case SpellTarget.SingleFriend:
                        SetCurrentPlayerAction(PlayerBattleAction.PickFriendSpellTarget);
                        break;
                    case SpellTarget.EnemyRow:
                        SetCurrentPlayerAction(PlayerBattleAction.PickEnemySpellTargetRow);
                        break;
                    case SpellTarget.BattleField:
                        if (spell == Spell.Blink)
                            SetCurrentPlayerAction(PlayerBattleAction.PickMemberToBlink);
                        else
                            throw new AmbermoonException(ExceptionScope.Data, "Only the Blink spell should have target type BattleField.");
                        break;
                    default:
                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell,
                            Battle.CreateCastSpellParameter(0, pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                        break;
                }
            });
        }

        void AdvanceParty(Action finishAction)
        {
            int advancedMonsters = 0;
            var monsters = currentBattle.Monsters.ToList();
            int totalMonsters = monsters.Count;
            var newPositions = new Dictionary<int, uint>(totalMonsters);
            uint timePerMonster = Math.Max(1u, TicksPerSecond / (2u * (uint)totalMonsters));

            void MoveMonster(Monster monster, int index)
            {
                int position = currentBattle.GetSlotFromCharacter(monster);
                int currentColumn = position % 6;
                int currentRow = position / 6;
                int newRow = currentRow + 1;
                var animation = layout.GetMonsterBattleAnimation(monster);

                void MoveAnimationFinished()
                {
                    animation.AnimationFinished -= MoveAnimationFinished;
                    currentBattle.SetMonsterDisplayLayer(animation, monster, position);
                    newPositions[index] = (uint)(position + 6);

                    if (++advancedMonsters == totalMonsters)
                    {
                        advancing = false;

                        for (int i = 0; i < monsters.Count; ++i)
                            currentBattle.MoveCharacterTo(newPositions[i], monsters[i]);

                        layout.EnableButton(4, currentBattle.CanMoveForward);
                        finishAction?.Invoke();
                    }
                }

                var newDisplayPosition = layout.GetMonsterCombatCenterPosition(currentColumn, newRow, monster);
                animation.AnimationFinished += MoveAnimationFinished;
                animation.Play(monster.GetAnimationFrameIndices(MonsterAnimationType.Move).Take(1).ToArray(),
                    timePerMonster, CurrentBattleTicks, newDisplayPosition,
                    layout.RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)newRow));
            }

            for (int i = 0; i < monsters.Count; ++i)
            {
                MoveMonster(monsters[i], i);
            }

            advancing = true;
        }

        internal void UpdateActiveBattleSpells()
        {
            foreach (var activeSpell in Enum.GetValues<ActiveSpellType>())
            {
                if (activeSpell.AvailableInBattle() && CurrentSavegame.ActiveSpells[(int)activeSpell] != null)
                    layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell], true);
            }
        }

        internal void HideActiveBattleSpells()
        {
            layout.RemoveAllActiveSpells();
        }

        internal void AddAilment(Ailment ailment, PartyMember target = null)
        {
            target ??= CurrentPartyMember;

            if (ailment >= Ailment.DeadCorpse && target.Alive)
            {
                target.Die(ailment);
                return;
            }

            target.Ailments |= ailment;

            if (CurrentPartyMember == target)
            {
                if (RecheckActivePartyMember(out bool gameOver))
                {
                    if (gameOver)
                        return;

                    if (ailment == Ailment.Blind)
                        UpdateLight();
                }
            }

            layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
            layout.UpdateCharacter(target);
        }

        internal void RemoveAilment(Ailment ailment, Character target)
        {
            // Healing spells or potions.
            // Sleep can be removed by attacking as well.
            target.Ailments &= ~ailment;

            if (target is PartyMember partyMember)
            {
                if (BattleActive)
                    UpdateBattleStatus(partyMember);
                layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);

                if (ailment == Ailment.Exhausted)
                    RemoveExhaustion(partyMember);
                else if (ailment == Ailment.Blind && partyMember == CurrentPartyMember)
                    UpdateLight();
            }
        }

        uint AddExhaustion(PartyMember partyMember, uint hours)
        {
            uint totalDamage = 0;
            long hitPoints = partyMember.HitPoints.CurrentValue;

            for (uint i = 0; i < hours; ++i)
            {
                // Do at least 1 damage per hour
                uint damage = Math.Max(1, (uint)hitPoints / 10);
                totalDamage += damage;
                hitPoints -= damage;

                if (hitPoints <= 0)
                    break;
            }

            if (hitPoints > 0)
            {
                foreach (var attribute in Enum.GetValues<Attribute>())
                {
                    partyMember.Attributes[attribute].StoredValue = partyMember.Attributes[attribute].CurrentValue;
                    partyMember.Attributes[attribute].CurrentValue /= 2;
                }
            }
            return Math.Min(totalDamage, partyMember.HitPoints.CurrentValue);
        }

        void RemoveExhaustion(PartyMember partyMember)
        {
            foreach (var attribute in Enum.GetValues<Attribute>())
            {
                partyMember.Attributes[attribute].CurrentValue = partyMember.Attributes[attribute].StoredValue;
                partyMember.Attributes[attribute].StoredValue = 0;
            }
        }

        /// <summary>
        /// Adds a spell effect.
        /// </summary>
        /// <param name="spell">Spell</param>
        /// <param name="caster">Casting party member or monster.</param>
        /// <param name="target">Party member or item or null.</param>
        /// <param name="finishAction">Action to call after effect was applied.</param>
        /// <param name="checkFail">If true check if the spell cast fails.</param>
        internal void ApplySpellEffect(Spell spell, Character caster, object target, Action finishAction = null, bool checkFail = true)
        {
            if (target == null)
                ApplySpellEffect(spell, caster, finishAction, checkFail);
            else if (target is Character character)
                ApplySpellEffect(spell, caster, character, finishAction, checkFail);
            else if (target is ItemSlot itemSlot)
                ApplySpellEffect(spell, caster, itemSlot, finishAction, checkFail);
            else
                throw new AmbermoonException(ExceptionScope.Application, $"Invalid spell target type: {target.GetType()}");
        }

        void ApplySpellEffect(Spell spell, Character caster, Action finishAction, bool checkFail)
        {
            CurrentSpellTarget = null;

            void Cast(Action action)
            {
                if (checkFail)
                    TrySpell(action);
                else
                    action?.Invoke();
            }

            void ActivateLight(uint duration, uint level)
            {
                CurrentSavegame.ActivateSpell(ActiveSpellType.Light, duration, level);
                UpdateLight(false, true);
            }

            switch (spell)
            {
                case Spell.Light:
                    // Duration: 30 (150 minutes = 2h30m)
                    // Level: 1 (Light radius 1)
                    Cast(() => ActivateLight(30, 1));
                    break;
                case Spell.MagicalTorch:
                    // Duration: 60 (300 minutes = 5h)
                    // Level: 1 (Light radius 1)
                    Cast(() => ActivateLight(60, 1));
                    break;
                case Spell.MagicalLantern:
                    // Duration: 120 (600 minutes = 10h)
                    // Level: 2 (Light radius 2)
                    Cast(() => ActivateLight(120, 2));
                    break;
                case Spell.MagicalSun:
                    // Duration: 180 (900 minutes = 15h)
                    // Level: 3 (Light radius 3)
                    Cast(() => ActivateLight(180, 3));
                    break;
                case Spell.Jump:
                    Cast(Jump);
                    break;
                case Spell.WordOfMarking:
                {
                    Cast(() =>
                    {
                        if (caster is PartyMember partyMember)
                        {
                            partyMember.MarkOfReturnMapIndex = (ushort)(Map.IsWorldMap ?
                                renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index : Map.Index);
                            partyMember.MarkOfReturnX = (ushort)(player.Position.X + 1); // stored 1-based
                            partyMember.MarkOfReturnY = (ushort)(player.Position.Y + 1); // stored 1-based
                            ShowMessagePopup(DataNameProvider.MarksPosition);
                        }
                    });
                    break;
                }
                case Spell.WordOfReturning:
                {
                    Cast(() =>
                    {
                        if (caster is PartyMember partyMember)
                        {
                            if (partyMember.MarkOfReturnMapIndex == 0)
                            {
                                ShowMessagePopup(DataNameProvider.HasntMarkedAPosition);
                            }
                            else
                            {
                                void Return() => Teleport(partyMember.MarkOfReturnMapIndex, partyMember.MarkOfReturnX, partyMember.MarkOfReturnY, player.Direction, out _, true);
                                ShowMessagePopup(DataNameProvider.ReturnToMarkedPosition, () =>
                                {
                                    var targetMap = MapManager.GetMap(partyMember.MarkOfReturnMapIndex);
                                    // Note: The original fades always if the map index does not match.
                                    // But we improve it here a bit so that moving inside the same world map won't fade.
                                    if (targetMap.Index == Map.Index || (targetMap.IsWorldMap && Map.IsWorldMap && targetMap.World == Map.World))
                                        Return();
                                    else
                                        Fade(Return);
                                });
                            }
                        }
                    });
                    break;
                }
                case Spell.MagicalShield:
                    // Duration: 30 (150 minutes = 2h30m)
                    // Level: 10 (10% defense increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 30, 10));
                    break;
                case Spell.MagicalWall:
                    // Duration: 90 (450 minutes = 7h30m)
                    // Level: 20 (20% defense increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 90, 20));
                    break;
                case Spell.MagicalBarrier:
                    // Duration: 180 (900 minutes = 15h)
                    // Level: 30 (30% defense increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30));
                    break;
                case Spell.MagicalWeapon:
                    // Duration: 30 (150 minutes = 2h30m)
                    // Level: 10 (10% damage increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 30, 10));
                    break;
                case Spell.MagicalAssault:
                    // Duration: 90 (450 minutes = 7h30m)
                    // Level: 20 (20% damage increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 90, 20));
                    break;
                case Spell.MagicalAttack:
                    // Duration: 180 (900 minutes = 15h)
                    // Level: 30 (30% damage increase)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30));
                    break;
                case Spell.Levitation:
                    Cast(Levitate);
                    break;
                case Spell.Rope:
                {
                    if (!is3D)
                    {
                        ShowMessagePopup(DataNameProvider.CannotClimbHere);
                    }
                    else
                    {
                        Levitate(() => ShowMessagePopup(DataNameProvider.CannotClimbHere), false);
                    }
                    break;
                }
                case Spell.AntiMagicWall:
                    // Duration: 30 (150 minutes = 2h30m)
                    // Level: 15 (15% anti-magic protection)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 30, 15));
                    break;
                case Spell.AntiMagicSphere:
                    // Duration: 180 (900 minutes = 15h)
                    // Level: 25 (25% anti-magic protection)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25));
                    break;
                case Spell.AlchemisticGlobe:
                    // Duration: 180 (900 minutes = 15h)
                    Cast(() =>
                    {
                        ActivateLight(180, 3);
                        CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30);
                        CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30);
                        CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25);
                    });
                    break;
                case Spell.Knowledge:
                    // Duration: 30 (150 minutes = 2h30m)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 30, 1));
                    break;
                case Spell.Clairvoyance:
                    // Duration: 90 (450 minutes = 7h30m)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 90, 1));
                    break;
                case Spell.SeeTheTruth:
                    // Duration: 180 (900 minutes = 15h)
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 1));
                    break;
                case Spell.MapView:
                    Cast(OpenMiniMap);
                    break;
                case Spell.MagicalCompass:
                {
                    Cast(() =>
                    {
                        Pause();
                        var popup = layout.OpenPopup(new Position(48, 64), 4, 4);
                        TrapMouse(popup.ContentArea);
                        popup.AddImage(new Rect(64, 80, 32, 32), Graphics.GetUIGraphicIndex(UIGraphic.Compass), Layer.UI, 1, UIPaletteIndex);
                        var text = popup.AddText(new Rect(59, 93, 42, 7), layout.GetCompassString(), TextColor.BrightGray);
                        text.Clip(new Rect(64, 93, 32, 7));
                        popup.Closed += () =>
                        {
                            UntrapMouse();
                            Resume();
                        };
                    });
                    break;
                }
                case Spell.FindTraps:
                    Cast(() => ShowAutomap(new AutomapOptions
                    {
                        SecretDoorsVisible = false,
                        MonstersVisible = false,
                        PersonsVisible = false,
                        TrapsVisible = true
                    }));
                    break;
                case Spell.FindMonsters:
                    Cast(() => ShowAutomap(new AutomapOptions
                    {
                        SecretDoorsVisible = false,
                        MonstersVisible = true,
                        PersonsVisible = false,
                        TrapsVisible = false
                    }));
                    break;
                case Spell.FindPersons:
                    Cast(() => ShowAutomap(new AutomapOptions
                    {
                        SecretDoorsVisible = false,
                        MonstersVisible = false,
                        PersonsVisible = true,
                        TrapsVisible = false
                    }));
                    break;
                case Spell.FindSecretDoors:
                    Cast(() => ShowAutomap(new AutomapOptions
                    {
                        SecretDoorsVisible = true,
                        MonstersVisible = false,
                        PersonsVisible = false,
                        TrapsVisible = false
                    }));
                    break;
                case Spell.MysticalMapping:
                    Cast(() => ShowAutomap(new AutomapOptions
                    {
                        SecretDoorsVisible = true,
                        MonstersVisible = true,
                        PersonsVisible = true,
                        TrapsVisible = true
                    }));
                    break;
                case Spell.MysticalMapI:
                    // Duration: 32 (160 minutes = 2h40m)
                    // TODO: level?
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 32, 1));
                    break;
                case Spell.MysticalMapII:
                    // Duration: 60 (300 minutes = 5h)
                    // TODO: level?
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 60, 1));
                    break;
                case Spell.MysticalMapIII:
                    // Duration: 90 (450 minutes = 7h30m)
                    // TODO: level?
                    Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 90, 1));
                    break;
                case Spell.MysticalGlobe:
                    // Duration: 180 (900 minutes = 15h)
                    // TODO: level?
                    Cast(() =>
                    {
                        CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 1);
                        CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 180, 1);
                    });
                    break;
                case Spell.Lockpicking:
                    // Do nothing. Can be used by Thief/Ranger but has no effect in Ambermoon.
                    break;
                case Spell.CallEagle:
                    if (TravelType != TravelType.Walk)
                    {
                        ShowMessagePopup(DataNameProvider.CannotCallEagleIfNotOnFoot, null, TextAlign.Left);
                    }
                    else
                    {
                        ShowMessagePopup(DataNameProvider.BlowsTheFlute, () =>
                        {
                            CloseWindow(() =>
                            {
                                StartSequence();
                                var travelInfoEagle = renderView.GameData.GetTravelGraphicInfo(TravelType.Eagle, CharacterDirection.Right);
                                var currentTravelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, player.Direction);
                                int diffX = (int)travelInfoEagle.OffsetX - (int)currentTravelInfo.OffsetX;
                                int diffY = (int)travelInfoEagle.OffsetY - (int)currentTravelInfo.OffsetY;
                                var targetPosition = player2D.DisplayArea.Position + new Position(diffX, diffY);
                                var position = new Position(Global.Map2DViewX - (int)travelInfoEagle.Width, targetPosition.Y - (int)travelInfoEagle.Height);
                                var eagle = layout.AddMapCharacterSprite(new Rect(position, new Size((int)travelInfoEagle.Width, (int)travelInfoEagle.Height)),
                                    3 * 17 + (uint)TravelType.Eagle * 4 + 1, ushort.MaxValue);
                                eagle.ClipArea = Map2DViewArea;
                                AddTimedEvent(TimeSpan.FromMilliseconds(200), AnimateEagle);
                                void AnimateEagle()
                                {
                                    if (position.X < targetPosition.X)
                                        position.X = Math.Min(targetPosition.X, position.X + 12);
                                    if (position.Y < targetPosition.Y)
                                        position.Y = Math.Min(targetPosition.Y, position.Y + 5);

                                    eagle.X = position.X;
                                    eagle.Y = position.Y;

                                    if (position == targetPosition)
                                    {
                                        EndSequence();
                                        eagle.Delete();
                                        ActivateTransport(TravelType.Eagle);
                                        // Update direction to right
                                        player.Direction = CharacterDirection.Right; // Set this before player2D.MoveTo!
                                        player2D.MoveTo(Map, (uint)player2D.Position.X, (uint)player2D.Position.Y, CurrentTicks, true, CharacterDirection.Right);
                                    }
                                    else
                                    {
                                        AddTimedEvent(TimeSpan.FromMilliseconds(40), AnimateEagle);
                                    }
                                }
                            });
                        }, TextAlign.Left);
                    }
                    break;
                case Spell.PlayElfHarp:
                    OpenMusicList();
                    break;
                case Spell.MagicalMap:
                    // TODO: In original this has no effect. Maybe it was planned to show
                    // the real map that was inside the original package.
                    // For now we show the minimap instead.
                    OpenMiniMap();
                    break;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no spell without target.");
            }
        }

        void TrySpell(Action successAction, Action failAction)
        {
            if (RollDice100() < CurrentPartyMember.Abilities[Ability.UseMagic].TotalCurrentValue)
                successAction?.Invoke();
            else
                failAction?.Invoke();
        }

        void TrySpell(Action successAction)
        {
            TrySpell(successAction, () => ShowMessagePopup(DataNameProvider.TheSpellFailed));
        }

        void ApplySpellEffect(Spell spell, Character caster, ItemSlot itemSlot, Action finishAction, bool checkFail)
        {
            CurrentSpellTarget = null;

            void PlayItemMagicAnimation(Action animationFinishAction = null)
            {
                ItemAnimation.Play(this, renderView, ItemAnimation.Type.Enchant, layout.GetItemSlotPosition(itemSlot),
                    animationFinishAction ?? finishAction, TimeSpan.FromMilliseconds(50));
            }

            void Error(string message)
            {
                EndSequence();
                ShowMessagePopup(message, finishAction, TextAlign.Left);
            }

            void Cast(Action successAction, Action failAction)
            {
                if (checkFail)
                    TrySpell(successAction, failAction);
                else
                    successAction?.Invoke();
            }

            switch (spell)
            {
                case Spell.Identification:
                {
                    if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
                    {
                        Error(DataNameProvider.ItemAlreadyIdentified);
                        return;
                    }
                    Cast(() =>
                    {
                        itemSlot.Flags |= ItemSlotFlags.Identified;
                        PlayItemMagicAnimation(() =>
                        {
                            EndSequence();
                            UntrapMouse();
                            ShowItemPopup(itemSlot, finishAction);
                        });
                    }, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                    });
                    break;
                }
                case Spell.ChargeItem:
                {
                    // Note: Even broken items can be charged.
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    if (item.Spell == Spell.None || item.MaxCharges == 0)
                    {
                        Error(DataNameProvider.ThisIsNotAMagicalItem);
                        return;
                    }
                    if (itemSlot.NumRemainingCharges >= item.MaxCharges)
                    {
                        Error(DataNameProvider.ItemAlreadyFullyCharged);
                        return;
                    }
                    Cast(() =>
                    {
                        itemSlot.NumRemainingCharges += RandomInt(1, Math.Min(item.MaxCharges - itemSlot.NumRemainingCharges, caster.Level));
                        PlayItemMagicAnimation();
                    }, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed)) // Don't destroy cursed items via failed charging
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                            else
                                finishAction?.Invoke();
                        });
                    });
                    break;
                }
                case Spell.RepairItem:
                {
                    if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                    {
                        Error(DataNameProvider.ItemIsNotBroken);
                        return;
                    }
                    Cast(() =>
                    {
                        itemSlot.Flags &= ~ItemSlotFlags.Broken;
                        layout.UpdateItemSlot(itemSlot);
                        PlayItemMagicAnimation();
                    }, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                        });
                    });
                    break;
                }
                case Spell.DuplicateItem:
                {
                    // Note: Even broken items can be duplicated. The broken state is also duplicated.
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    if (!item.Flags.HasFlag(ItemFlags.Clonable))
                    {
                        Error(DataNameProvider.CannotBeDuplicated);
                        return;
                    }
                    Cast(() =>
                    {
                        PlayItemMagicAnimation(() =>
                        {
                            bool couldDuplicate = false;
                            var inventorySlots = CurrentInventory.Inventory.Slots;

                            if (item.Flags.HasFlag(ItemFlags.Stackable))
                            {
                                // Look for slots with free stacks
                                var freeSlot = inventorySlots.FirstOrDefault(s => s.ItemIndex == item.Index && s.Amount < 99);

                                if (freeSlot != null)
                                {
                                    ++freeSlot.Amount;
                                    layout.UpdateItemSlot(freeSlot);
                                    couldDuplicate = true;
                                }
                            }

                            if (!couldDuplicate)
                            {
                                // Look for empty slots
                                var freeSlot = inventorySlots.FirstOrDefault(s => s.Empty);

                                if (freeSlot != null)
                                {
                                    var copy = itemSlot.Copy();
                                    copy.Amount = 1;
                                    freeSlot.Replace(copy);
                                    layout.UpdateItemSlot(freeSlot);
                                    couldDuplicate = true;
                                }
                            }

                            if (!couldDuplicate)
                            {
                                EndSequence();
                                ShowMessagePopup(DataNameProvider.NoRoomForItem, finishAction);
                            }
                            else
                            {
                                finishAction?.Invoke();
                            }
                        });
                    }, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed)) // Don't destroy cursed items via failed duplicating
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), false, finishAction);
                            else
                                finishAction?.Invoke();
                        });
                    });
                    break;
                }
                case Spell.RemoveCurses:
                {
                    void Fail()
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                    }

                    Cast(() =>
                    {
                        if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed))
                        {
                            Fail();
                        }
                        else
                        {
                            PlayItemMagicAnimation(() =>
                            {
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(10), false, () =>
                                {
                                    EndSequence();
                                    finishAction?.Invoke();
                                });
                            });
                        }
                    }, Fail);
                    break;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no item-targeted spell.");
            }
        }

        void ApplySpellEffect(Spell spell, Character caster, Character target, Action finishAction, bool checkFail)
        {
            CurrentSpellTarget = target;

            void Cast(Action action)
            {
                if (checkFail)
                    TrySpell(action);
                else
                    action?.Invoke();
            }

            switch (spell)
            {
                case Spell.Hurry:
                case Spell.MassHurry:
                    // Note: This is handled by battle code
                    break;
                case Spell.RemoveFear:
                case Spell.RemovePanic:
                    Cast(() => RemoveAilment(Ailment.Panic, target));
                    break;
                case Spell.RemoveShadows:
                case Spell.RemoveBlindness:
                    Cast(() => RemoveAilment(Ailment.Blind, target));
                    break;
                case Spell.RemovePain:
                case Spell.RemoveDisease:
                    Cast(() => RemoveAilment(Ailment.Diseased, target));
                    break;
                case Spell.RemovePoison:
                case Spell.NeutralizePoison:
                    Cast(() => RemoveAilment(Ailment.Poisoned, target));
                    break;
                case Spell.HealingHand:
                    Cast(() => Heal(target.HitPoints.TotalMaxValue / 10)); // 10%
                    break;
                case Spell.SmallHealing:
                case Spell.MassHealing:
                    Cast(() => Heal(target.HitPoints.TotalMaxValue / 4)); // 25%
                    break;
                case Spell.MediumHealing:
                    Cast(() => Heal(target.HitPoints.TotalMaxValue / 2)); // 50%
                    break;
                case Spell.GreatHealing:
                    Cast(() => Heal(target.HitPoints.TotalMaxValue * 3 / 4)); // 75%
                    break;
                case Spell.RemoveRigidness:
                case Spell.RemoveLamedness:
                    Cast(() => RemoveAilment(Ailment.Lamed, target));
                    break;
                case Spell.HealAging:
                case Spell.StopAging:
                    Cast(() => RemoveAilment(Ailment.Aging, target));
                    break;
                case Spell.WakeUp:
                    Cast(() => RemoveAilment(Ailment.Sleep, target));
                    break;
                case Spell.RemoveIrritation:
                    Cast(() => RemoveAilment(Ailment.Irritated, target));
                    break;
                case Spell.RemoveDrugged:
                    Cast(() => RemoveAilment(Ailment.Drugged, target));
                    break;
                case Spell.RemoveMadness:
                    Cast(() => RemoveAilment(Ailment.Crazy, target));
                    break;
                case Spell.RestoreStamina:
                    Cast(() => RemoveAilment(Ailment.Exhausted, target));
                    break;
                case Spell.CreateFood:
                    Cast(() => ++target.Food);
                    break;
                case Spell.Resurrection:
                {
                    TrySpell(() =>
                    {
                        target.Ailments &= ~Ailment.DeadCorpse;
                        target.HitPoints.CurrentValue = 1;
                        PartyMemberRevived(target as PartyMember, finishAction, false);
                    });
                    break;
                }
                case Spell.WakeTheDead:
                {
                    if (!(target is PartyMember targetPlayer))
                    {
                        // Should not happen
                        return;
                    }
                    void Revive()
                    {
                        if (!target.Ailments.HasFlag(Ailment.DeadCorpse) ||
                            target.Ailments.HasFlag(Ailment.DeadAshes) ||
                            target.Ailments.HasFlag(Ailment.DeadDust))
                        {
                            if (target.Alive)
                                ShowMessagePopup(DataNameProvider.IsNotDead, finishAction);
                            else
                                ShowMessagePopup(DataNameProvider.CannotBeResurrected, finishAction);
                            return;
                        }
                        target.Ailments &= ~Ailment.DeadCorpse;
                        target.HitPoints.CurrentValue = 1;
                        PartyMemberRevived(targetPlayer, finishAction);
                    }
                    if (checkFail)
                    {
                        TrySpell(Revive, () =>
                        {
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                            {
                                if (target.Ailments.HasFlag(Ailment.DeadCorpse))
                                {
                                    target.Ailments &= ~Ailment.DeadCorpse;
                                    target.Ailments |= Ailment.DeadAshes;
                                    ShowMessagePopup(DataNameProvider.BodyBurnsUp, finishAction);
                                }
                            });
                        });
                    }
                    else
                    {
                        Revive();
                    }
                    break;
                }
                case Spell.ChangeAshes:
                {
                    if (!(target is PartyMember targetPlayer))
                    {
                        // Should not happen
                        return;
                    }
                    void TransformToBody()
                    {
                        if (!target.Ailments.HasFlag(Ailment.DeadAshes) ||
                            target.Ailments.HasFlag(Ailment.DeadDust))
                        {
                            ShowMessagePopup(DataNameProvider.IsNotAsh, finishAction);
                            return;
                        }
                        target.Ailments &= ~Ailment.DeadAshes;
                        target.Ailments |= Ailment.DeadCorpse;
                        ShowMessagePopup(DataNameProvider.AshesChangedToBody, finishAction);
                    }
                    if (checkFail)
                    {
                        TrySpell(TransformToBody, () =>
                        {
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                            {
                                if (target.Ailments.HasFlag(Ailment.DeadAshes))
                                {
                                    target.Ailments &= ~Ailment.DeadAshes;
                                    target.Ailments |= Ailment.DeadDust;
                                    ShowMessagePopup(DataNameProvider.AshesFallToDust, finishAction);
                                }
                            });
                        });
                    }
                    else
                    {
                        TransformToBody();
                    }
                    break;
                }
                case Spell.ChangeDust:
                {
                    if (!(target is PartyMember targetPlayer))
                    {
                        // Should not happen
                        return;
                    }
                    void TransformToAshes()
                    {
                        if (!target.Ailments.HasFlag(Ailment.DeadDust))
                        {
                            ShowMessagePopup(DataNameProvider.IsNotDust, finishAction);
                            return;
                        }
                        target.Ailments &= ~Ailment.DeadDust;
                        target.Ailments |= Ailment.DeadAshes;
                        ShowMessagePopup(DataNameProvider.DustChangedToAshes, finishAction);
                    }
                    if (checkFail)
                    {
                        TrySpell(TransformToAshes, () =>
                        {
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.TheSpellFailed, finishAction);
                        });
                    }
                    else
                    {
                        TransformToAshes();
                    }
                    break;
                }
                case Spell.SpellPointsI:
                    FillSP(target.SpellPoints.TotalMaxValue / 10); // 10%
                    break;
                case Spell.SpellPointsII:
                    FillSP(target.SpellPoints.TotalMaxValue / 4); // 25%
                    break;
                case Spell.SpellPointsIII:
                    FillSP(target.SpellPoints.TotalMaxValue / 2); // 50%
                    break;
                case Spell.SpellPointsIV:
                    FillSP(target.SpellPoints.TotalMaxValue * 3 / 4); // 75%
                    break;
                case Spell.SpellPointsV:
                    FillSP(target.SpellPoints.TotalMaxValue); // 100%
                    break;
                case Spell.AllHealing:
                {
                    void HealAll()
                    {
                        // Removes all curses and heals full LP
                        Heal(target.HitPoints.TotalMaxValue);
                        foreach (var ailment in Enum.GetValues<Ailment>())
                        {
                            if (ailment != Ailment.None && target.Ailments.HasFlag(ailment))
                                RemoveAilment(ailment, target);
                        }
                        finishAction?.Invoke();
                    }
                    if (!target.Alive)
                    {
                        target.Ailments &= ~Ailment.DeadCorpse;
                        target.Ailments &= ~Ailment.DeadAshes;
                        target.Ailments &= ~Ailment.DeadDust;
                        target.HitPoints.CurrentValue = 1;
                        PartyMemberRevived(target as PartyMember, HealAll, false);
                    }
                    else
                    {
                        HealAll();
                    }
                    break;
                }
                case Spell.AddStrength:
                    IncreaseAttribute(Attribute.Strength);
                    break;
                case Spell.AddIntelligence:
                    IncreaseAttribute(Attribute.Intelligence);
                    break;
                case Spell.AddDexterity:
                    IncreaseAttribute(Attribute.Dexterity);
                    break;
                case Spell.AddSpeed:
                    IncreaseAttribute(Attribute.Speed);
                    break;
                case Spell.AddStamina:
                    IncreaseAttribute(Attribute.Stamina);
                    break;
                case Spell.AddCharisma:
                    IncreaseAttribute(Attribute.Charisma);
                    break;
                case Spell.AddLuck:
                    IncreaseAttribute(Attribute.Luck);
                    break;
                case Spell.AddAntiMagic:
                    IncreaseAttribute(Attribute.AntiMagic);
                    break;
                case Spell.DecreaseAge:
                    if (target.Alive && !target.Ailments.HasFlag(Ailment.Petrified))
                        target.Attributes[Attribute.Age].CurrentValue = (uint)Math.Max(18, (int)target.Attributes[Attribute.Age].CurrentValue - RandomInt(1, 10));
                    break;
                case Spell.Drugs:
                    if (target is PartyMember partyMember)
                        AddAilment(Ailment.Drugged, partyMember);
                    break;
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no character-targeted spell.");
            }

            void IncreaseAttribute(Attribute attribute)
            {
                if (target.Alive)
                {
                    var value = target.Attributes[attribute];
                    value.CurrentValue = Math.Min(value.CurrentValue + (uint)RandomInt(1, 5), value.MaxValue);
                }
            }

            void Heal(uint amount)
            {
                target.Heal(amount);

                if (target is PartyMember partyMember)
                    layout.FillCharacterBars(partyMember);
            }

            void FillSP(uint amount)
            {
                target.SpellPoints.CurrentValue = Math.Min(target.SpellPoints.TotalMaxValue, target.SpellPoints.CurrentValue + amount);
                layout.FillCharacterBars(target as PartyMember);
            }
        }

        internal void SetFastBattleMode(bool active)
        {
            if (currentBattle != null)
                currentBattle.NeedsClickForNextAction = !active;
        }

        void ShowBattleWindow(Event nextEvent, bool failedFlight, uint? combatBackgroundIndex = null)
        {
            Fade(() =>
            {
                PlayMusic(Song.SapphireFireballsOfPureLove);
                roundPlayerBattleActions.Clear();
                ShowBattleWindow(nextEvent, out byte paletteIndex, combatBackgroundIndex);
                // Note: Create clones so we can change the values in battle for each monster.
                var monsterGroup = CharacterManager.GetMonsterGroup(currentBattleInfo.MonsterGroupIndex).Clone();
                foreach (var monster in monsterGroup.Monsters)
                    InitializeMonster(this, monster);
                var monsterBattleAnimations = new Dictionary<int, BattleAnimation>(24);
                // Add animated monster combat graphics and battle field sprites
                for (int row = 0; row < 3; ++row)
                {
                    for (int column = 0; column < 6; ++column)
                    {
                        var monster = monsterGroup.Monsters[column, row];

                        if (monster != null)
                        {
                            monsterBattleAnimations.Add(column + row * 6,
                                layout.AddMonsterCombatSprite(column, row, monster, 0, paletteIndex));
                        }
                    }
                }
                currentBattle = new Battle(this, layout, Enumerable.Range(0, MaxPartyMembers).Select(i => GetPartyMember(i)).ToArray(),
                    monsterGroup, monsterBattleAnimations, !Configuration.FastBattleMode);
                foreach (var monsterBattleAnimation in monsterBattleAnimations)
                    currentBattle.SetMonsterDisplayLayer(monsterBattleAnimation.Value, currentBattle.GetCharacterAt(monsterBattleAnimation.Key) as Monster);
                currentBattle.RoundFinished += () =>
                {
                    InputEnable = true;
                    CursorType = CursorType.Sword;
                    layout.ShowButtons(true);
                    battleRoundActiveSprite.Visible = false;
                    buttonGridBackground?.Destroy();
                    buttonGridBackground = null;
                    layout.EnableButton(4, currentBattle.CanMoveForward);

                    foreach (var action in roundPlayerBattleActions)
                        CheckPlayerActionVisuals(GetPartyMember(action.Key), action.Value);
                    layout.SetBattleFieldSlotColor(currentBattle.GetSlotFromCharacter(CurrentPartyMember), BattleFieldSlotColor.Yellow);
                    layout.SetBattleMessage(null);
                    if (RecheckActivePartyMember(out bool gameOver))
                    {
                        if (gameOver)
                            return;
                        BattlePlayerSwitched();
                    }
                    else
                        AddCurrentPlayerActionVisuals();
                    UpdateBattleStatus();
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        if (partyMemberBattleFieldTooltips[i] != null)
                        {
                            var partyMember = GetPartyMember(i);

                            partyMemberBattleFieldTooltips[i].Text =
                                $"{partyMember.HitPoints.CurrentValue}/{partyMember.HitPoints.TotalMaxValue}^{partyMember.Name}";
                        }
                    }
                    UpdateActiveBattleSpells();
                };
                currentBattle.CharacterDied += character =>
                {
                    if (character is PartyMember partyMember)
                    {
                        int slot = SlotFromPartyMember(partyMember).Value;
                        layout.SetCharacter(slot, partyMember);
                        layout.UpdateCharacterStatus(slot, null);
                        roundPlayerBattleActions.Remove(slot);
                    }
                };
                currentBattle.BattleEnded += battleEndInfo =>
                {
                    battleRoundActiveSprite.Visible = false;
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        if (GetPartyMember(i) != null)
                            layout.UpdateCharacterStatus(i, null);
                    }
                    void EndBattle()
                    {
                        for (int i = 0; i < MaxPartyMembers; ++i)
                        {
                            var partyMember = GetPartyMember(i);

                            if (partyMember != null)
                                partyMember.Ailments = partyMember.Ailments.WithoutBattleOnlyAilments();
                        }
                        roundPlayerBattleActions.Clear();
                        UpdateBattleStatus();
                        PlayMusic(Song.Default);
                        currentBattleInfo.EndBattle(battleEndInfo);
                        currentBattleInfo = null;
                    }
                    if (battleEndInfo.MonstersDefeated)
                    {
                        currentBattle = null;
                        EndBattle();
                        ShowBattleLoot(battleEndInfo, () =>
                        {
                            if (nextEvent != null)
                            {
                                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, (uint)RenderPlayer.Position.X,
                                    (uint)RenderPlayer.Position.Y, CurrentTicks, nextEvent, true);
                            }
                        });
                    }
                    else if (PartyMembers.Any(p => p.Alive && p.Ailments.CanFight()))
                    {
                        // There are fled survivors
                        currentBattle = null;
                        EndBattle();
                        CloseWindow(() =>
                        {
                            InputEnable = true;
                            if (nextEvent != null)
                            {
                                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, (uint)RenderPlayer.Position.X,
                                    (uint)RenderPlayer.Position.Y, CurrentTicks, nextEvent, false);
                            }
                        });
                    }
                    else
                    {
                        currentBattleInfo = null;
                        currentBattle = null;
                        CloseWindow(() =>
                        {
                            InputEnable = true;
                            GameOver();
                        });
                    }
                };
                currentBattle.ActionCompleted += battleAction =>
                {
                    CursorType = CursorType.Click;

                    if (battleAction.Character is PartyMember partyMember &&
                        (battleAction.Action == Battle.BattleActionType.Move ||
                        battleAction.Action == Battle.BattleActionType.Flee ||
                        battleAction.Action == Battle.BattleActionType.CastSpell))
                        layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                currentBattle.PlayerWeaponBroke += partyMember =>
                {
                    // Note: no need to check action here as it only can break while attacking
                    roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember).Value);
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                currentBattle.PlayerLastAmmoUsed += partyMember =>
                {
                    // Note: no need to check action here as it only can happen while attacking
                    roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember).Value);
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                currentBattle.PlayerLostTarget += partyMember =>
                {
                    roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember).Value);
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                BattlePlayerSwitched();

                if (failedFlight)
                {
                    SetBattleMessageWithClick(DataNameProvider.AttackEscapeFailedMessage, TextColor.BrightGray, () => StartBattleRound(true));
                }
            });
        }

        void StartBattleRound(bool withoutPlayerActions)
        {
            HideActiveBattleSpells();
            InputEnable = false;
            CursorType = CursorType.Click;
            layout.ResetMonsterCombatSprites();
            layout.ClearBattleFieldSlotColors();
            layout.ShowButtons(false);
            buttonGridBackground = layout.FillArea(new Rect(Global.ButtonGridX, Global.ButtonGridY, 3 * Button.Width, 3 * Button.Height),
                GetUIColor(28), 1);
            battleRoundActiveSprite.Visible = true;
            currentBattle.StartRound
            (
                withoutPlayerActions ? Enumerable.Repeat(new Battle.PlayerBattleAction(), 6).ToArray() :
                    Enumerable.Range(0, MaxPartyMembers)
                    .Select(i => roundPlayerBattleActions.ContainsKey(i) ? roundPlayerBattleActions[i] : new Battle.PlayerBattleAction())
                    .ToArray(), CurrentBattleTicks
            );
        }

        void CancelSpecificPlayerAction()
        {
            SetCurrentPlayerAction(PlayerBattleAction.PickPlayerAction);
            UntrapMouse();
            AddCurrentPlayerActionVisuals();
            layout.SetBattleMessage(null);
        }

        bool CheckBattleRightClick()
        {
            if (currentPlayerBattleAction == PlayerBattleAction.PickPlayerAction)
                return false; // This is handled by layout/game interaction.

            CancelSpecificPlayerAction();
            return true;
        }

        // Note: In original the max hitpoints are often much higher
        // than the current hitpoints. It seems like the max hitpoints
        // are often a multiple of 99 like 99, 198, 297, etc.
        static void InitializeMonster(Game game, Monster monster)
        {
            if (monster == null)
                return;

            static void AdjustMonsterValue(Game game, CharacterValue characterValue)
            {
                characterValue.CurrentValue = (uint)Math.Min(100, game.RandomInt(95, 104)) * characterValue.TotalMaxValue / 100u;
            }

            static void FixValue(Game game, CharacterValue characterValue)
            {
                characterValue.MaxValue = characterValue.CurrentValue;
                AdjustMonsterValue(game, characterValue);
            }

            // Attributes, abilities, LP and SP is special for monsters.
            foreach (var attribute in Enum.GetValues<Attribute>())
                FixValue(game, monster.Attributes[attribute]);
            foreach (var ability in Enum.GetValues<Ability>())
                FixValue(game, monster.Abilities[ability]);
            // TODO: the given max value might be used for something else
            monster.HitPoints.MaxValue = monster.HitPoints.CurrentValue;
            monster.SpellPoints.MaxValue = monster.SpellPoints.CurrentValue;
            AdjustMonsterValue(game, monster.HitPoints);
            AdjustMonsterValue(game, monster.SpellPoints);
        }

        internal void MoveBattleActorTo(uint column, uint row, Character character)
        {
            if (character is Monster monster)
                layout.MoveMonsterTo(column, row, monster);
            else
            {
                var partyMember = character as PartyMember;
                int index = SlotFromPartyMember(partyMember).Value;
                var sprite = partyMemberBattleFieldSprites[index];
                sprite.X = Global.BattleFieldX + (int)column * Global.BattleFieldSlotWidth;
                sprite.Y = Global.BattleFieldY + (int)row * Global.BattleFieldSlotHeight - 1;
                sprite.DisplayLayer = (byte)(3 + row);
            }
        }

        internal void RemoveBattleActor(Character character)
        {
            if (character is Monster monster)
            {
                layout.RemoveMonsterCombatSprite(monster);
            }
            else if (character is PartyMember partyMember)
            {
                int slot = SlotFromPartyMember(partyMember).Value;
                roundPlayerBattleActions.Remove(slot);
                partyMemberBattleFieldSprites[slot]?.Delete();
                partyMemberBattleFieldSprites[slot] = null;

                if (partyMemberBattleFieldTooltips[slot] != null)
                {
                    layout.RemoveTooltip(partyMemberBattleFieldTooltips[slot]);
                    partyMemberBattleFieldTooltips[slot] = null;
                }
            }
        }

        void BattlePlayerSwitched()
        {
            int partyMemberSlot = SlotFromPartyMember(CurrentPartyMember).Value;
            layout.ClearBattleFieldSlotColors();
            int battleFieldSlot = currentBattle.GetSlotFromCharacter(CurrentPartyMember);
            layout.SetBattleFieldSlotColor(battleFieldSlot, BattleFieldSlotColor.Yellow);
            AddCurrentPlayerActionVisuals();

            if (roundPlayerBattleActions.ContainsKey(partyMemberSlot))
            {
                var action = roundPlayerBattleActions[partyMemberSlot];
                layout.UpdateCharacterStatus(partyMemberSlot, action.BattleAction.ToStatusGraphic(action.Parameter, ItemManager));
            }
            else
            {
                layout.UpdateCharacterStatus(partyMemberSlot, CurrentPartyMember.Ailments.CanSelect() ? (UIGraphic?)null : GetDisabledStatusGraphic(CurrentPartyMember));
            }

            layout.EnableButton(0, battleFieldSlot >= 24 && CurrentPartyMember.CanFlee()); // flee button, only enable in last row
            layout.EnableButton(3, CurrentPartyMember.CanMove()); // Note: If no slot is available the button still is enabled but after clicking you get "You can't move anywhere".
            layout.EnableButton(4, currentBattle.CanMoveForward);
            layout.EnableButton(6, CurrentPartyMember.BaseAttack > 0 && CurrentPartyMember.Ailments.CanAttack());
            layout.EnableButton(7, CurrentPartyMember.Ailments.CanParry());
            layout.EnableButton(8, CurrentPartyMember.Ailments.CanCastSpell() && CurrentPartyMember.HasAnySpell());
        }

        /// <summary>
        /// This adds the target slots' coloring.
        /// </summary>
        void AddCurrentPlayerActionVisuals()
        {
            int slot = SlotFromPartyMember(CurrentPartyMember).Value;

            if (roundPlayerBattleActions.ContainsKey(slot))
            {
                var action = roundPlayerBattleActions[slot];

                switch (action.BattleAction)
                {
                    case Battle.BattleActionType.Attack:
                    case Battle.BattleActionType.Move:
                        layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange);
                        break;
                    case Battle.BattleActionType.CastSpell:
                        var spell = Battle.GetCastSpell(action.Parameter);
                        switch (SpellInfos.Entries[spell].Target)
                        {
                            case SpellTarget.SingleEnemy:
                            case SpellTarget.SingleFriend:
                                layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange);
                                break;
                            case SpellTarget.FriendRow:
                            {
                                SetBattleRowSlotColors((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter),
                                    (c, r) => currentBattle.GetCharacterAt(c, r)?.Type == CharacterType.PartyMember,
                                    BattleFieldSlotColor.Orange);
                                break;
                            }
                            case SpellTarget.EnemyRow:
                            {
                                SetBattleRowSlotColors((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter),
                                    (c, r) => currentBattle.GetCharacterAt(c, r)?.Type == CharacterType.Monster,
                                    BattleFieldSlotColor.Orange);
                                break;
                            }
                            case SpellTarget.AllEnemies:
                                for (int i = 0; i < 24; ++i)
                                    layout.SetBattleFieldSlotColor(i, BattleFieldSlotColor.Orange);
                                break;
                            case SpellTarget.AllFriends:
                                for (int i = 0; i < 12; ++i)
                                    layout.SetBattleFieldSlotColor(18 + i, BattleFieldSlotColor.Orange);
                                break;
                            case SpellTarget.BattleField:
                            {
                                int blinkCharacterSlot = (int)Battle.GetBlinkCharacterPosition(action.Parameter);
                                bool selfBlink = currentBattle.GetSlotFromCharacter(CurrentPartyMember) == blinkCharacterSlot;
                                layout.SetBattleFieldSlotColor(blinkCharacterSlot, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentBattleTicks);
                                layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange, CurrentBattleTicks + Layout.TicksPerBlink);
                                break;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// This removes the target slots' coloring.
        /// </summary>
        void RemoveCurrentPlayerActionVisuals()
        {
            var action = GetOrCreateBattleAction();

            switch (action.BattleAction)
            {
                case Battle.BattleActionType.Attack:
                case Battle.BattleActionType.Move:
                    layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.None);
                    break;
                case Battle.BattleActionType.CastSpell:
                    layout.ClearBattleFieldSlotColorsExcept(currentBattle.GetSlotFromCharacter(CurrentPartyMember));
                    if (currentBattle.IsSelfSpell(CurrentPartyMember, action.Parameter))
                        layout.SetBattleFieldSlotColor(currentBattle.GetSlotFromCharacter(CurrentPartyMember), BattleFieldSlotColor.Yellow);
                    break;
            }
        }

        /// <summary>
        /// Checks if a player action should be still active after
        /// a battle round.
        /// </summary>
        /// <param name="action"></param>
        void CheckPlayerActionVisuals(PartyMember partyMember, Battle.PlayerBattleAction action)
        {
            bool remove = !partyMember.Ailments.CanSelect();

            if (!remove)
            {
                switch (action.BattleAction)
                {
                    case Battle.BattleActionType.Move:
                    case Battle.BattleActionType.Flee:
                    case Battle.BattleActionType.CastSpell:
                        remove = true;
                        break;
                    case Battle.BattleActionType.Attack:
                        if (partyMember.BaseAttack <= 0 || !partyMember.Ailments.CanAttack())
                            remove = true;
                        break;
                    case Battle.BattleActionType.Parry:
                        if (!partyMember.Ailments.CanParry())
                            remove = true;
                        break;
                    default:
                        remove = true;
                        break;
                }
            }

            if (remove) // Note: Don't use 'else' here as remove could be set inside the if-block above as well.
                roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember).Value);
        }

        void SetCurrentPlayerBattleAction(Battle.BattleActionType actionType, uint parameter = 0)
        {
            RemoveCurrentPlayerActionVisuals();
            var action = GetOrCreateBattleAction();
            action.BattleAction = actionType;
            action.Parameter = parameter;
            AddCurrentPlayerActionVisuals();

            int slot = SlotFromPartyMember(CurrentPartyMember).Value;
            layout.UpdateCharacterStatus(slot, actionType.ToStatusGraphic(parameter, ItemManager));
        }

        void SetPlayerBattleAction(Battle.BattleActionType actionType, uint parameter = 0)
        {
            if (currentPickingActionMember == CurrentPartyMember)
                SetCurrentPlayerBattleAction(actionType, parameter);
            else
            {
                var action = GetOrCreateBattleAction();
                action.BattleAction = actionType;
                action.Parameter = parameter;
                int slot = SlotFromPartyMember(currentPickingActionMember).Value;
                layout.UpdateCharacterStatus(slot, actionType.ToStatusGraphic(parameter, ItemManager));
            }
        }

        Battle.PlayerBattleAction GetOrCreateBattleAction()
        {
            int slot = SlotFromPartyMember(currentPickingActionMember).Value;

            if (!roundPlayerBattleActions.ContainsKey(slot))
                roundPlayerBattleActions.Add(slot, new Battle.PlayerBattleAction());

            return roundPlayerBattleActions[slot];
        }

        internal void SetBattleMessageWithClick(string message, TextColor textColor = TextColor.BattlePlayer,
            Action followAction = null, TimeSpan? delay = null)
        {
            layout.HideTooltip();
            layout.SetBattleMessage(message, textColor);

            if (delay == null)
                Setup();
            else
                AddTimedEvent(delay.Value, Setup);

            void Setup()
            {
                InputEnable = false;
                currentBattle.WaitForClick = true;
                CursorType = CursorType.Click;

                if (followAction != null)
                {
                    bool Follow(MouseButtons _)
                    {
                        layout.SetBattleMessage(null);
                        InputEnable = true;
                        currentBattle.WaitForClick = false;
                        CursorType = CursorType.Sword;
                        followAction?.Invoke();
                        return true;
                    }

                    nextClickHandler = Follow;
                }
            }
        }

        bool AnyPlayerMovesTo(int slot)
        {
            var actions = roundPlayerBattleActions.Where(p => p.Key != SlotFromPartyMember(currentPickingActionMember));
            bool anyMovesTo = actions.Any(p => p.Value.BattleAction == Battle.BattleActionType.Move &&
                Battle.GetTargetTileOrRowFromParameter(p.Value.Parameter) == slot);

            if (anyMovesTo)
                return true;

            // Anyone blinks to? This is different to original where this isn't checked but I guess it's better this way.
            return actions.Any(p =>
            {
                if (p.Value.BattleAction == Battle.BattleActionType.CastSpell &&
                    Battle.GetCastSpell(p.Value.Parameter) == Spell.Blink)
                {
                    if (Battle.GetTargetTileOrRowFromParameter(p.Value.Parameter) == slot)
                        return true;
                }

                return false;
            });
        }

        void BattleFieldSlotClicked(int column, int row, MouseButtons mouseButtons)
        {
            if (currentBattle.SkipNextBattleFieldClick)
                return;

            if (row < 0 || row > 4 ||
                column < 0 || column > 5)
                return;

            if (mouseButtons == MouseButtons.Right)
            {
                var character = currentBattle.GetCharacterAt(column, row);

                if (character is PartyMember partyMember)
                {
                    OpenPartyMember(SlotFromPartyMember(partyMember).Value, true);
                }

                return;
            }
            else if (mouseButtons != MouseButtons.Left)
                return;

            switch (currentPlayerBattleAction)
            {
                case PlayerBattleAction.PickPlayerAction:
                {
                    var character = currentBattle.GetCharacterAt(column, row);

                    if (character?.Type == CharacterType.PartyMember)
                    {
                        var partyMember = character as PartyMember;

                        if (currentPickingActionMember != partyMember && partyMember.Ailments.CanSelect())
                        {
                            int partyMemberSlot = SlotFromPartyMember(partyMember).Value;
                            SetActivePartyMember(partyMemberSlot, false);
                            BattlePlayerSwitched();
                        }
                    }
                    else if (character?.Type == CharacterType.Monster)
                    {
                        if (!CheckAbilityToAttack(out bool ranged))
                            return;

                        if (!ranged)
                        {
                            int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                            if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                            {
                                SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.BrightGray);
                                return;
                            }
                        }

                        SetPlayerBattleAction(Battle.BattleActionType.Attack,
                            Battle.CreateAttackParameter((uint)(column + row * 6), currentPickingActionMember, ItemManager));
                    }
                    else // empty field
                    {
                        if (row < 3)
                            return;
                        int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                        if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.BrightGray);
                            return;
                        }
                        if (!currentPickingActionMember.CanMove())
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotMove, TextColor.BrightGray);
                            return;
                        }
                        int newPosition = column + row * 6;
                        int slot = SlotFromPartyMember(currentPickingActionMember).Value;
                        if ((!roundPlayerBattleActions.ContainsKey(slot) ||
                            roundPlayerBattleActions[slot].BattleAction != Battle.BattleActionType.Move ||
                            Battle.GetTargetTileOrRowFromParameter(roundPlayerBattleActions[slot].Parameter) != newPosition) &&
                            AnyPlayerMovesTo(newPosition))
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageSomeoneAlreadyGoingThere, TextColor.BrightGray);
                            return;
                        }
                        SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)(column + row * 6)));
                    }
                    break;
                }
                case PlayerBattleAction.PickMemberToBlink:
                {
                    var target = currentBattle.GetCharacterAt(column, row);
                    if (target != null && target.Type == CharacterType.PartyMember)
                    {
                        if (!target.CanMove())
                        {
                            CancelSpecificPlayerAction();
                            // TODO: Test this later. Is CanMove equal to CanBlink?
                            SetBattleMessageWithClick(target.Name + DataNameProvider.BattleMessageCannotBlink, TextColor.BrightGray);
                            return;
                        }

                        blinkCharacterPosition = (uint)(column + row * 6);
                        SetCurrentPlayerAction(PlayerBattleAction.PickBlinkTarget);
                    }
                    break;
                }
                case PlayerBattleAction.PickBlinkTarget:
                {
                    // Note: If someone moves to the target spot, it can't be selected (red cross).
                    // But someone can move to a spot where someone blinks to in Ambermoon.
                    // Here we disallow moving to a spot where someone blinks to by considering
                    // blink targets in AnyPlayerMovesTo. This will also disallow 2 characters to
                    // blink to the same spot.
                    int position = column + row * 6;
                    if (row > 2 && currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
                    {
                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)(column + row * 6),
                            pickedSpell, spellItemSlotIndex, spellItemIsEquipped, blinkCharacterPosition.Value));
                        if (currentPickingActionMember == CurrentPartyMember)
                        {
                            int casterSlot = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                            bool selfBlink = casterSlot == blinkCharacterPosition.Value;
                            layout.SetBattleFieldSlotColor((int)blinkCharacterPosition.Value, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentBattleTicks);
                            layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange, CurrentBattleTicks + Layout.TicksPerBlink);
                            if (!selfBlink)
                                layout.SetBattleFieldSlotColor(casterSlot, BattleFieldSlotColor.Yellow);
                        }
                        CancelSpecificPlayerAction();
                    }
                    break;
                }
                case PlayerBattleAction.PickEnemySpellTarget:
                case PlayerBattleAction.PickFriendSpellTarget:
                {
                    var target = currentBattle.GetCharacterAt(column, row);
                    if (target != null)
                    {
                        if (currentPlayerBattleAction == PlayerBattleAction.PickEnemySpellTarget)
                        {
                            if (target.Type != CharacterType.Monster)
                                return;
                        }
                        else
                        {
                            if (target.Type != CharacterType.PartyMember)
                                return;
                        }

                        SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)(column + row * 6),
                            pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                        if (currentPickingActionMember == CurrentPartyMember)
                            layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange);
                        CancelSpecificPlayerAction();
                    }
                    break;
                }
                case PlayerBattleAction.PickEnemySpellTargetRow:
                {
                    if (row > 3)
                    {
                        return;
                    }
                    SetPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)row,
                        pickedSpell, spellItemSlotIndex, spellItemIsEquipped));
                    if (currentPickingActionMember == CurrentPartyMember)
                    {
                        layout.ClearBattleFieldSlotColorsExcept(currentBattle.GetSlotFromCharacter(currentPickingActionMember));
                        SetBattleRowSlotColors(row, (c, r) => currentBattle.GetCharacterAt(c, r)?.Type != CharacterType.PartyMember, BattleFieldSlotColor.Orange);
                    }
                    CancelSpecificPlayerAction();
                    break;
                }
                case PlayerBattleAction.PickMoveSpot:
                {
                    int position = column + row * 6;
                    if (currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
                    {
                        SetPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)position));
                        CancelSpecificPlayerAction();
                    }
                    break;
                }
                case PlayerBattleAction.PickAttackSpot:
                {
                    if (!CheckAbilityToAttack(out bool ranged))
                        return;

                    if (!ranged)
                    {
                        int position = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                        if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                            return;
                    }

                    if (currentBattle.GetCharacterAt(column + row * 6)?.Type == CharacterType.Monster)
                    {
                        SetPlayerBattleAction(Battle.BattleActionType.Attack,
                            Battle.CreateAttackParameter((uint)(column + row * 6), currentPickingActionMember, ItemManager));
                        CancelSpecificPlayerAction();
                    }
                    break;
                }
            }
        }

        void SetBattleRowSlotColors(int row, Func<int, int, bool> condition, BattleFieldSlotColor color)
        {
            for (int column = 0; column < 6; ++column)
            {
                if (condition(column, row))
                    layout.SetBattleFieldSlotColor(column, row, color);
            }
        }

        IEnumerable<int> GetValuableBattleFieldSlots(Func<int, bool> condition, int range, int minRow, int maxRow)
        {
            int slot = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
            int currentColumn = slot % 6;
            int currentRow = slot / 6;
            for (int row = Math.Max(minRow, currentRow - range); row <= Math.Min(maxRow, currentRow + range); ++row)
            {
                for (int column = Math.Max(0, currentColumn - range); column <= Math.Min(5, currentColumn + range); ++column)
                {
                    int index = column + row * 6;

                    if (condition(index))
                        yield return index;
                }
            }
        }

        bool CheckAbilityToAttack(out bool ranged)
        {
            ranged = currentPickingActionMember.HasLongRangedAttack(ItemManager, out bool hasAmmo);

            if (ranged && !hasAmmo)
            {
                // No ammo for ranged weapon
                CancelSpecificPlayerAction();
                SetBattleMessageWithClick(DataNameProvider.BattleMessageNoAmmunition, TextColor.BrightGray);
                return false;
            }

            if (currentPickingActionMember.BaseAttack <= 0 || !currentPickingActionMember.Ailments.CanAttack())
            {
                CancelSpecificPlayerAction();
                SetBattleMessageWithClick(DataNameProvider.BattleMessageUnableToAttack, TextColor.BrightGray);
                return false;
            }

            return true;
        }

        void SetCurrentPlayerAction(PlayerBattleAction playerBattleAction)
        {
            currentPlayerBattleAction = playerBattleAction;
            highlightBattleFieldSprites.ForEach(s => s?.Delete());
            highlightBattleFieldSprites.Clear();
            blinkingHighlight = false;

            switch (currentPlayerBattleAction)
            {
                case PlayerBattleAction.PickPlayerAction:
                    currentPickingActionMember = CurrentPartyMember;
                    break;
                case PlayerBattleAction.PickEnemySpellTarget:
                {
                    var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.GetCharacterAt(position)?.Type == CharacterType.Monster,
                        6, 0, 3);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                                UIPaletteIndex
                            )
                        );
                    }
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhichMonsterAsTarget);
                    break;
                }
                case PlayerBattleAction.PickEnemySpellTargetRow:
                {
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = false;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhichMonsterRowAsTarget);
                    break;
                }
                case PlayerBattleAction.PickFriendSpellTarget:
                case PlayerBattleAction.PickMemberToBlink:
                {
                    var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.GetCharacterAt(position)?.Type == CharacterType.PartyMember,
                        6, 3, 4);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                                UIPaletteIndex
                            )
                        );
                    }
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(playerBattleAction == PlayerBattleAction.PickMemberToBlink
                        ? DataNameProvider.BattleMessageWhoToBlink
                        : DataNameProvider.BattleMessageWhichPartyMemberAsTarget);
                    break;
                }
                case PlayerBattleAction.PickBlinkTarget:
                {
                    var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.IsBattleFieldEmpty(position),
                        6, 3, 4);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex
                                (
                                    AnyPlayerMovesTo(slot) ? UICustomGraphic.BattleFieldBlockedMovementCursor : UICustomGraphic.BattleFieldGreenHighlight
                                ), UIPaletteIndex
                            )
                        );
                    }
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhereToBlinkTo);
                    break;
                }
                case PlayerBattleAction.PickMoveSpot:
                {
                    var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.IsBattleFieldEmpty(position),
                        1, 3, 4);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex
                                (
                                    AnyPlayerMovesTo(slot) ? UICustomGraphic.BattleFieldBlockedMovementCursor : UICustomGraphic.BattleFieldGreenHighlight
                                ), UIPaletteIndex
                            )
                        );
                    }
                    if (highlightBattleFieldSprites.Count == 0)
                    {
                        // No movement possible
                        CancelSpecificPlayerAction();
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageNowhereToMoveTo, TextColor.BrightGray);
                    }
                    else
                    {
                        RemoveCurrentPlayerActionVisuals();
                        TrapMouse(Global.BattleFieldArea);
                        blinkingHighlight = true;
                        layout.SetBattleMessage(DataNameProvider.BattleMessageWhereToMoveTo);
                    }
                    break;
                }
                case PlayerBattleAction.PickAttackSpot:
                {
                    if (!CheckAbilityToAttack(out bool ranged))
                        return;

                    var valuableSlots = GetValuableBattleFieldSlots(index => currentBattle.GetCharacterAt(index)?.Type == CharacterType.Monster,
                        ranged ? 6 : 1, 0, 3);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight),
                                UIPaletteIndex
                            )
                        );
                    }
                    if (highlightBattleFieldSprites.Count == 0)
                    {
                        // No attack possible
                        CancelSpecificPlayerAction();
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotReachAnyone, TextColor.BrightGray);
                    }
                    else
                    {
                        RemoveCurrentPlayerActionVisuals();
                        TrapMouse(Global.BattleFieldArea);
                        blinkingHighlight = true;
                        layout.SetBattleMessage(DataNameProvider.BattleMessageWhatToAttack);
                    }
                    break;
                }
            }
        }

        void GameOver()
        {
            PlayMusic(Song.GameOver);
            ShowEvent(ProcessText(DataNameProvider.GameOverMessage), 8, null, true);
        }

        float Get3DLight()
        {
            uint usedLightIntensity;

            if (CurrentPartyMember.Ailments.HasFlag(Ailment.Blind))
                return 0.0f;

            if (Map.Flags.HasFlag(MapFlags.Outdoor))
            {
                // This is handled by palette color replacement.
                return 1.0f;
            }
            else if (Map.Flags.HasFlag(MapFlags.Indoor))
            {
                // Indoor always use full brightness.
                usedLightIntensity = 255;
            }
            else
            {
                usedLightIntensity = lightIntensity;
            }

            if (usedLightIntensity == 0)
                return 0.0f;
            else if (usedLightIntensity == 255)
                return 1.0f;

            return usedLightIntensity / 255.0f;
        }

        internal void UpdateLight(bool mapChange = false, bool lightActivated = false, bool playerSwitched = false)
        {
            if (Map == null)
                return;

            void ChangeLightRadius(int lastRadius, int newRadius)
            {
                var map = Map;
                var lightLevel = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light);
                const int timePerChange = 75;
                var timeSpan = TimeSpan.FromMilliseconds(timePerChange);

                void ChangeLightRadius()
                {
                    if (map != Map || // map changed
                        lightLevel != CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light)) // light buff changed
                        return;

                    int diff = newRadius - lastRadius;

                    if (diff != 0)
                    {
                        int change = mapChange || playerSwitched ? diff : Math.Sign(diff) * Math.Min(Math.Abs(diff), 8);
                        lastRadius += change;
                        fow2D.Radius = (byte)lastRadius;
                        fow2D.Visible = lastRadius < 112;

                        if (newRadius - lastRadius != 0)
                            AddTimedEvent(timeSpan, ChangeLightRadius);
                    }
                }

                if (mapChange || playerSwitched)
                    ChangeLightRadius();
                else
                    AddTimedEvent(timeSpan, ChangeLightRadius);
            }

            if (CurrentPartyMember.Ailments.HasFlag(Ailment.Blind))
            {
                lightIntensity = 0;

                if (!is3D)
                {
                    fow2D.Radius = 0;
                    fow2D.Visible = true;
                }
                else
                {
                    renderMap3D.HideSky();
                }
            }
            else if (Map.Flags.HasFlag(MapFlags.Outdoor))
            {
                // Light is based on daytime and own light sources
                // 17:00-18:59: 128
                // 19:00-19:59: 80
                // 20:00-05:59: 32
                // 06:00-06:59: 80
                // 07:00-07:59: 128
                // 08:00-16:59: 255
                // Each light spell level adds an additional 32.

                uint lastIntensity = lightIntensity;

                if (GameTime.Hour < 6 || GameTime.Hour >= 20)
                    lightIntensity = 32;
                else if (GameTime.Hour < 7)
                    lightIntensity = 80;
                else if (GameTime.Hour < 8)
                    lightIntensity = 128;
                else if (GameTime.Hour < 17)
                    lightIntensity = 255;
                else if (GameTime.Hour < 19)
                    lightIntensity = 128;
                else if (GameTime.Hour < 20)
                    lightIntensity = 80;
                else
                    lightIntensity = 32;

                lightIntensity = Math.Min(255, lightIntensity + CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light) * 32);

                if (!is3D && (lastIntensity != lightIntensity || mapChange))
                {
                    var lastRadius = mapChange ? 0 : (int)(lastIntensity >> 1);
                    var newRadius = (int)(lightIntensity >> 1);
                    fow2D.Visible = lastIntensity < 224;
                    ChangeLightRadius(lastRadius, newRadius);
                }
            }
            else if (Map.Flags.HasFlag(MapFlags.Indoor))
            {
                // Full light
                lightIntensity = 255;
                fow2D.Visible = false;

                if (is3D)
                    renderMap3D.HideSky();
            }
            else // Dungeon
            {
                // Otherwise light is based on own light sources only.
                if (lightActivated || mapChange || playerSwitched)
                {
                    if (is3D)
                    {
                        if (mapChange && !CurrentSavegame.IsSpellActive(ActiveSpellType.Light))
                            lightIntensity = 0;
                        else
                        {
                            var lightLevel = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light);
                            if (lightLevel > 0 || !playerSwitched)
                            {
                                lightIntensity = Math.Min(255, 176 + CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light) * 32);
                                if (lightLevel == 1)
                                    lightIntensity = Math.Min(255, lightIntensity + 16);
                            }
                        }
                    }
                    else
                    {
                        uint lastIntensity = lightIntensity;
                        lightIntensity = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light) * 32;

                        if (lastIntensity != lightIntensity)
                        {
                            var lastRadius = (int)(lastIntensity >> 1);
                            var newRadius = (int)(lightIntensity >> 1);
                            fow2D.Visible = lastIntensity < 224;
                            ChangeLightRadius(lastRadius, newRadius);
                        }
                    }
                }
                else if (!is3D && lightIntensity < 224)
                {
                    fow2D.Radius = (byte)(lightIntensity >> 1);
                    fow2D.Visible = true;
                }
                if (is3D)
                {
                    fow2D.Visible = false;
                    renderMap3D.HideSky();
                }
            }

            if (is3D)
            {
                fow2D.Visible = false;
                var light3D = Get3DLight();
                renderView.SetLight(light3D);
                renderMap3D.UpdateSky(lightEffectProvider, GameTime);
            }
            else // 2D
            {
                player2D.BaselineOffset = !CanSee() ? MaxBaseLine : player.MovementAbility > PlayerMovementAbility.Walking ? 32 : 0;
            }
        }

        internal uint DistributeGold(uint gold, bool force)
        {
            var partyMembers = PartyMembers.ToList();

            while (gold != 0)
            {
                int numTargetPlayers = partyMembers.Count;
                uint goldPerPlayer = gold / (uint)numTargetPlayers;
                bool anyCouldTake = false;

                if (goldPerPlayer == 0)
                {
                    numTargetPlayers = (int)gold;
                    goldPerPlayer = 1;
                }

                foreach (var partyMember in partyMembers)
                {
                    uint goldToTake = force ? goldPerPlayer : Math.Min(partyMember.MaxGoldToTake, goldPerPlayer);
                    gold -= goldToTake;
                    partyMember.AddGold(goldToTake);

                    if (goldToTake != 0)
                    {
                        anyCouldTake = true;

                        if (--numTargetPlayers == 0)
                            break;
                    }
                }

                if (!anyCouldTake)
                    return gold;
            }

            return gold;
        }

        internal uint DistributeFood(uint food, bool force)
        {
            var partyMembers = PartyMembers.ToList();

            while (food != 0)
            {
                int numTargetPlayers = partyMembers.Count;
                uint foodPerPlayer = food / (uint)numTargetPlayers;
                bool anyCouldTake = false;

                if (foodPerPlayer == 0)
                {
                    numTargetPlayers = (int)food;
                    foodPerPlayer = 1;
                }

                foreach (var partyMember in partyMembers)
                {
                    uint foodToTake = force ? foodPerPlayer : Math.Min(partyMember.MaxFoodToTake, foodPerPlayer);
                    food -= foodToTake;
                    partyMember.AddFood(foodToTake);

                    if (foodToTake != 0)
                    {
                        anyCouldTake = true;

                        if (--numTargetPlayers == 0)
                            break;
                    }
                }

                if (!anyCouldTake)
                    return food;
            }

            return food;
        }

        void PlayHealAnimation(PartyMember partyMember, Action finishAction = null)
        {
            currentAnimation?.Destroy();
            currentAnimation = new SpellAnimation(this, layout);
            currentAnimation.CastOn(Spell.SmallHealing, partyMember, () =>
            {
                currentAnimation.Destroy();
                currentAnimation = null;
                finishAction?.Invoke();
            });
        }

        void OpenEnchanter(Places.Enchanter enchanter, bool showWelcome = true)
        {
            if (showWelcome)
                enchanter.AvailableGold = 0;

            Action updatePartyGold = null;
            ItemGrid itemsGrid = null;

            void SetupEnchanter(Action updateGold, ItemGrid itemGrid)
            {
                updatePartyGold = updateGold;
                itemsGrid = itemGrid;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Enchanter, enchanter);
                ShowPlaceWindow(enchanter.Name, showWelcome ? DataNameProvider.WelcomeEnchanter : null,
                    Picture80x80.Enchantress, enchanter, SetupEnchanter, null, null, null, 24);
                void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.WhichItemToEnchant, TextAlign.Left);
                var itemArea = new Rect(16, 139, 151, 53);
                // Enchant item button
                layout.AttachEventToButton(3, () =>
                {
                    itemsGrid.Disabled = false;
                    itemsGrid.DisableDrag = true;
                    ShowDefaultMessage();
                    CursorType = CursorType.Sword;
                    TrapMouse(itemArea);
                    itemsGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                    itemsGrid.ItemClicked += ItemClicked;
                    SetupRightClickAbort();
                });
                void SetupRightClickAbort()
                {
                    nextClickHandler = buttons =>
                    {
                        if (buttons == MouseButtons.Right)
                        {
                            DisableItemGrid();
                            layout.ShowChestMessage(null);
                            UntrapMouse();
                            return true;
                        }

                        return false;
                    };
                }
                void DisableItemGrid()
                {
                    itemsGrid.HideTooltip();
                    itemsGrid.ItemClicked -= ItemClicked;
                    itemsGrid.Disabled = true;
                }
                void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    itemsGrid.HideTooltip();

                    void Error(string message, bool abort)
                    {
                        layout.ShowClickChestMessage(message, () =>
                        {
                            if (!abort)
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                                ShowDefaultMessage();
                            }
                            else
                            {
                                DisableItemGrid();
                            }
                        });
                    }

                    var item = ItemManager.GetItem(itemSlot.ItemIndex);

                    if (item.Spell == Spell.None || item.InitialCharges == 0)
                    {
                        Error(DataNameProvider.CannotEnchantOrdinaryItem, false);
                        return;
                    }

                    if (item.MaxRecharges != 0 && itemSlot.RechargeTimes >= item.MaxRecharges)
                    {
                        Error(DataNameProvider.CannotRechargeAnymore, false);
                        return;
                    }

                    int numMissingCharges = itemSlot.NumRemainingCharges >= item.MaxCharges ? 0 : item.MaxCharges - itemSlot.NumRemainingCharges;

                    if (numMissingCharges == 0)
                    {
                        Error(DataNameProvider.AlreadyFullyCharged, false);
                        return;
                    }

                    if (enchanter.AvailableGold < enchanter.Cost)
                    {
                        Error(DataNameProvider.NotEnoughMoney, true);
                        return;
                    }

                    void Enchant(uint charges)
                    {
                        ClosePopup();
                        uint cost = charges * (uint)enchanter.Cost;

                        layout.ShowPlaceQuestion($"{DataNameProvider.PriceForEnchanting}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            nextClickHandler = null;
                            EndSequence();
                            UntrapMouse();

                            if (answer) // yes
                            {
                                void Enchant()
                                {
                                    layout.ShowChestMessage(null);
                                    enchanter.AvailableGold -= cost;
                                    updatePartyGold?.Invoke();
                                    itemSlot.NumRemainingCharges += (int)charges;
                                    itemSlot.RechargeTimes = (byte)Math.Min(255, itemSlot.RechargeTimes + 1);
                                    DisableItemGrid();
                                }

                                if (item.MaxRecharges > 0 && itemSlot.RechargeTimes == item.MaxRecharges - 1)
                                    layout.ShowClickChestMessage(DataNameProvider.LastTimeEnchanting, Enchant);
                                else
                                    Enchant();
                            }
                            else
                            {
                                layout.ShowChestMessage(null);
                                DisableItemGrid();
                            }
                        }, TextAlign.Left);
                    }

                    nextClickHandler = null;
                    UntrapMouse();

                    layout.OpenAmountInputBox(DataNameProvider.HowManyCharges,
                        item.GraphicIndex, item.Name, (uint)Util.Min(enchanter.AvailableGold / enchanter.Cost, numMissingCharges), Enchant,
                        () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        }
                    );
                };
            });
        }

        void OpenSage(Places.Sage sage, bool showWelcome = true)
        {
            if (showWelcome)
                sage.AvailableGold = 0;

            Action updatePartyGold = null;
            ItemGrid itemsGrid = null;

            void SetupSage(Action updateGold, ItemGrid itemGrid)
            {
                updatePartyGold = updateGold;
                itemsGrid = itemGrid;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Sage, sage);
                ShowPlaceWindow(sage.Name, showWelcome ? DataNameProvider.WelcomeSage : null,
                    Picture80x80.Sage, sage, SetupSage, null, null, null, 24);
                void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.ExamineWhichItemSage, TextAlign.Left);
                void ShowItems(bool equipment)
                {
                    itemsGrid.Disabled = false;
                    itemsGrid.DisableDrag = true;
                    ShowDefaultMessage();
                    CursorType = CursorType.Sword;
                    var itemArea = new Rect(16, 139, 151, 53);
                    TrapMouse(itemArea);
                    itemsGrid.Initialize(equipment ? CurrentPartyMember.Equipment.Slots.Select(s => s.Value).ToList()
                        : CurrentPartyMember.Inventory.Slots.ToList(), false);
                    void SetupRightClickAbort()
                    {
                        nextClickHandler = buttons =>
                        {
                            if (buttons == MouseButtons.Right)
                            {
                                DisableItemGrid();
                                layout.ShowChestMessage(null);
                                UntrapMouse();
                                return true;
                            }

                            return false;
                        };
                    }
                    SetupRightClickAbort();
                    void DisableItemGrid()
                    {
                        itemsGrid.HideTooltip();
                        itemsGrid.ItemClicked -= ItemClicked;
                        itemsGrid.Disabled = true;
                    }
                    itemsGrid.ItemClicked += ItemClicked;
                    void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                    {
                        itemsGrid.HideTooltip();

                        void Error(string message, bool abort)
                        {
                            layout.ShowClickChestMessage(message, () =>
                            {
                                if (!abort)
                                {
                                    TrapMouse(itemArea);
                                    SetupRightClickAbort();
                                    ShowDefaultMessage();
                                }
                                else
                                {
                                    DisableItemGrid();
                                }
                            });
                        }

                        if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
                        {
                            Error(DataNameProvider.ItemAlreadyIdentified, false);
                            return;
                        }

                        if (sage.AvailableGold < sage.Cost)
                        {
                            Error(DataNameProvider.NotEnoughMoney, true);
                            return;
                        }

                        layout.ShowPlaceQuestion($"{DataNameProvider.PriceForExamining}{sage.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            nextClickHandler = null;
                            EndSequence();
                            UntrapMouse();
                            DisableItemGrid();
                            layout.ShowChestMessage(null);

                            if (answer) // yes
                            {
                                sage.AvailableGold -= (uint)sage.Cost;
                                updatePartyGold?.Invoke();
                                itemSlot.Flags |= ItemSlotFlags.Identified;
                                ShowItemPopup(itemSlot, null);
                            }
                        }, TextAlign.Left);
                    };
                }
                // Examine equipment button
                layout.AttachEventToButton(0, () => ShowItems(true));
                // Examine inventory item button
                layout.AttachEventToButton(3, () => ShowItems(false));
            });
        }

        void OpenHealer(Places.Healer healer, bool showWelcome = true)
        {
            if (showWelcome)
                healer.AvailableGold = 0;

            Action updatePartyGold = null;
            ItemGrid conditionGrid = null;

            void SetupHealer(Action updateGold, ItemGrid itemGrid)
            {
                updatePartyGold = updateGold;
                conditionGrid = itemGrid;
            }

            void Heal(uint lp)
            {
                layout.ShowPlaceQuestion($"{DataNameProvider.PriceForHealing}{lp * healer.HealLPCost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        healer.AvailableGold -= lp * (uint)healer.HealLPCost;
                        updatePartyGold?.Invoke();
                        currentlyHealedMember.HitPoints.CurrentValue += lp;
                        PlayerSwitched();
                        PlayHealAnimation(currentlyHealedMember, () => layout.FillCharacterBars(currentlyHealedMember));
                    }
                }, TextAlign.Left);
            }

            void HealAilment(Ailment ailment, Action<bool> healedHandler)
            {
                // TODO: At the moment DeadAshes and DeadDust will be healed fully so that the
                // character is alive afterwards. As this is bugged in original I don't know how
                // it was supposed to be. Either reviving completely or transform to next stage
                // like dust to ashes and ashes to body first.

                var cost = (uint)healer.GetCostForHealingAilment(ailment);

                layout.ShowPlaceQuestion($"{DataNameProvider.PriceForHealingCondition}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        healer.AvailableGold -= cost;
                        updatePartyGold?.Invoke();
                        RemoveAilment(ailment, currentlyHealedMember);
                        PlayerSwitched();
                        PlayHealAnimation(currentlyHealedMember);
                        layout.UpdateCharacterStatus(currentlyHealedMember);
                        healedHandler?.Invoke(true);
                        if (ailment >= Ailment.DeadCorpse) // dead
                        {
                            currentlyHealedMember.HitPoints.CurrentValue = Math.Max(1, currentlyHealedMember.HitPoints.CurrentValue);
                            PartyMemberRevived(currentlyHealedMember);
                        }
                    }
                    else
                    {
                        healedHandler?.Invoke(false);
                    }
                }, TextAlign.Left);
            }

            var healableAilments = Ailment.Lamed | Ailment.Poisoned | Ailment.Petrified | Ailment.Diseased |
                Ailment.Aging | Ailment.DeadCorpse | Ailment.DeadAshes | Ailment.DeadDust | Ailment.Crazy |
                Ailment.Blind | Ailment.Drugged;

            void PlayerSwitched()
            {
                layout.EnableButton(0, currentlyHealedMember.HitPoints.CurrentValue < currentlyHealedMember.HitPoints.TotalMaxValue);
                layout.EnableButton(3, currentlyHealedMember.Equipment.Slots.Any(slot => slot.Value.Flags.HasFlag(ItemSlotFlags.Cursed)));
                layout.EnableButton(6, ((uint)currentlyHealedMember.Ailments & (uint)healableAilments) != 0);
            }

            uint GetMaxLPHealing() => Math.Max(0, Util.Min(healer.AvailableGold / (uint)healer.HealLPCost,
                currentlyHealedMember.HitPoints.TotalMaxValue - currentlyHealedMember.HitPoints.CurrentValue));

            Fade(() =>
            {
                if (showWelcome)
                    currentlyHealedMember = CurrentPartyMember;

                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Healer, healer);
                ShowPlaceWindow(healer.Name, showWelcome ? DataNameProvider.WelcomeHealer : null,
                    Picture80x80.Healer, healer, SetupHealer, PlayerSwitched);
                // This will show the healing symbol on top of the portrait.
                SetActivePartyMember(SlotFromPartyMember(currentlyHealedMember).Value);
                // Heal LP button
                layout.AttachEventToButton(0, () =>
                {
                    conditionGrid.Disabled = true;

                    if (healer.AvailableGold < healer.HealLPCost)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                        return;
                    }

                    layout.OpenAmountInputBox(DataNameProvider.HowManyLP, null, null, GetMaxLPHealing(), lp =>
                    {
                        ClosePopup();
                        Heal(lp);
                    }, ClosePopup);
                });
                // Remove curse button
                layout.AttachEventToButton(3, () =>
                {
                    conditionGrid.Disabled = true;

                    if (healer.AvailableGold < healer.RemoveCurseCost)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                        return;
                    }

                    int maxCursesToRemove = Math.Min((int)healer.AvailableGold / healer.RemoveCurseCost,
                        currentlyHealedMember.Equipment.Slots.Count(slot => slot.Value.Flags.HasFlag(ItemSlotFlags.Cursed)));

                    layout.ShowPlaceQuestion($"{DataNameProvider.PriceForRemovingCurses}{maxCursesToRemove * healer.RemoveCurseCost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            healer.AvailableGold -= (uint)(maxCursesToRemove * healer.RemoveCurseCost);
                            updatePartyGold?.Invoke();
                            PlayerSwitched();
                            allInputDisabled = true;
                            OpenPartyMember(SlotFromPartyMember(currentlyHealedMember).Value, true, () =>
                            {
                                var equipSlots = currentlyHealedMember.Equipment.Slots.ToList();

                                for (int i = 0; i < maxCursesToRemove; ++i)
                                {
                                    var cursedItemSlot = equipSlots.First(s => s.Value.Flags.HasFlag(ItemSlotFlags.Cursed));
                                    layout.DestroyItem(cursedItemSlot.Value, TimeSpan.FromMilliseconds(800));
                                }

                                AddTimedEvent(TimeSpan.FromSeconds(2), () =>
                                {
                                    CloseWindow();
                                    allInputDisabled = false;
                                });
                            }, false);
                        }
                    }, TextAlign.Left);
                });
                layout.AttachEventToButton(6, () =>
                {
                    conditionGrid.Disabled = false;
                    conditionGrid.DisableDrag = true;
                    layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                    CursorType = CursorType.Sword;
                    var itemArea = new Rect(16, 139, 151, 53);
                    TrapMouse(itemArea);
                    var slots = new List<ItemSlot>(12);
                    var slotAilments = new List<Ailment>(12);
                    // Ensure that only one dead state is present
                    if (currentlyHealedMember.Ailments.HasFlag(Ailment.DeadDust))
                        currentlyHealedMember.Ailments = Ailment.DeadDust;
                    else if (currentlyHealedMember.Ailments.HasFlag(Ailment.DeadAshes))
                        currentlyHealedMember.Ailments = Ailment.DeadAshes;
                    else if (currentlyHealedMember.Ailments.HasFlag(Ailment.DeadCorpse))
                        currentlyHealedMember.Ailments = Ailment.DeadCorpse;
                    for (int i = 0; i < 16; ++i)
                    {
                        if (((uint)healableAilments & (1u << i)) != 0)
                        {
                            var ailment = (Ailment)(1 << i);

                            if (currentlyHealedMember.Ailments.HasFlag(ailment))
                            {
                                slots.Add(new ItemSlot
                                {
                                    ItemIndex = ailment switch
                                    {
                                        Ailment.Lamed => 1,
                                        Ailment.Poisoned => 2,
                                        Ailment.Petrified => 3,
                                        Ailment.Diseased => 4,
                                        Ailment.Aging => 5,
                                        Ailment.Crazy => 7,
                                        Ailment.Blind => 8,
                                        Ailment.Drugged => 9,
                                        _ => 6 // dead states
                                    },
                                    Amount = 1
                                });
                                slotAilments.Add(ailment);
                            }
                        }
                    }
                    while (slots.Count < 12)
                        slots.Add(new ItemSlot());
                    conditionGrid.Initialize(slots, false);
                    void SetupRightClickAbort()
                    {
                        nextClickHandler = buttons =>
                        {
                            if (buttons == MouseButtons.Right)
                            {
                                DisableConditionGrid();
                                layout.ShowChestMessage(null);
                                UntrapMouse();
                                return true;
                            }

                            return false;
                        };
                    }
                    SetupRightClickAbort();
                    void DisableConditionGrid()
                    {
                        conditionGrid.HideTooltip();
                        conditionGrid.ItemClicked -= ConditionClicked;
                        conditionGrid.Disabled = true;
                    }
                    conditionGrid.ItemClicked += ConditionClicked;
                    void ConditionClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                    {
                        if (slotIndex < slotAilments.Count)
                        {
                            conditionGrid.HideTooltip();

                            if (healer.AvailableGold < healer.GetCostForHealingAilment(slotAilments[slotIndex]))
                            {
                                layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney, () =>
                                {
                                    TrapMouse(itemArea);
                                    SetupRightClickAbort();
                                });
                                return;
                            }

                            nextClickHandler = null;
                            UntrapMouse();

                            HealAilment(slotAilments[slotIndex], healed =>
                            {
                                if (healed)
                                {
                                    if (currentlyHealedMember.Ailments != Ailment.None)
                                    {
                                        conditionGrid.SetItem(slotIndex, null);
                                        TrapMouse(itemArea);
                                        SetupRightClickAbort();
                                        layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                                    }
                                    else
                                    {
                                        DisableConditionGrid();
                                    }
                                }
                                else
                                {
                                    TrapMouse(itemArea);
                                    SetupRightClickAbort();
                                    layout.ShowChestMessage(DataNameProvider.WhichConditionToHeal, TextAlign.Left);
                                }
                            });
                        }
                    };
                });
                PlayerSwitched();
            });
        }

        void OpenBlacksmith(Places.Blacksmith blacksmith, bool showWelcome = true)
        {
            if (showWelcome)
                blacksmith.AvailableGold = 0;

            // Note: The blacksmith uses the same 80x80 image as the sage.
            Action updatePartyGold = null;
            ItemGrid itemsGrid = null;

            void SetupBlacksmith(Action updateGold, ItemGrid itemGrid)
            {
                updatePartyGold = updateGold;
                itemsGrid = itemGrid;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Blacksmith, blacksmith);
                ShowPlaceWindow(blacksmith.Name, showWelcome ? DataNameProvider.WelcomeBlacksmith : null,
                    Picture80x80.Sage, blacksmith, SetupBlacksmith, null, null, null, 24);
                void ShowDefaultMessage() => layout.ShowChestMessage(DataNameProvider.WhichItemToRepair, TextAlign.Left);
                // Repair item button
                layout.AttachEventToButton(3, () =>
                {
                    itemsGrid.Disabled = false;
                    itemsGrid.DisableDrag = true;
                    ShowDefaultMessage();
                    CursorType = CursorType.Sword;
                    var itemArea = new Rect(16, 139, 151, 53);
                    TrapMouse(itemArea);
                    itemsGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                    void SetupRightClickAbort()
                    {
                        nextClickHandler = buttons =>
                        {
                            if (buttons == MouseButtons.Right)
                            {
                                DisableItemGrid();
                                layout.ShowChestMessage(null);
                                UntrapMouse();
                                return true;
                            }

                            return false;
                        };
                    }
                    SetupRightClickAbort();
                    void DisableItemGrid()
                    {
                        itemsGrid.HideTooltip();
                        itemsGrid.ItemClicked -= ItemClicked;
                        itemsGrid.Disabled = true;
                    }
                    itemsGrid.ItemClicked += ItemClicked;
                    void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                    {
                        itemsGrid.HideTooltip();

                        void Error(string message, bool abort)
                        {
                            layout.ShowClickChestMessage(message, () =>
                            {
                                if (!abort)
                                {
                                    TrapMouse(itemArea);
                                    SetupRightClickAbort();
                                    ShowDefaultMessage();
                                }
                                else
                                {
                                    DisableItemGrid();
                                }
                            });
                        }

                        if (!itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                        {
                            Error(DataNameProvider.CannotRepairUnbreakableItem, false);
                            return;
                        }

                        var item = ItemManager.GetItem(itemSlot.ItemIndex);
                        uint cost = (uint)blacksmith.Cost * item.Price / 100u;

                        if (blacksmith.AvailableGold < cost)
                        {
                            Error(DataNameProvider.NotEnoughMoney, true);
                            return;
                        }

                        layout.ShowPlaceQuestion($"{DataNameProvider.PriceForRepair}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            nextClickHandler = null;
                            EndSequence();
                            UntrapMouse();
                            layout.ShowChestMessage(null);

                            if (answer) // yes
                            {
                                blacksmith.AvailableGold -= cost;
                                updatePartyGold?.Invoke();
                                itemSlot.Flags &= ~ItemSlotFlags.Broken;
                            }

                            DisableItemGrid();
                        }, TextAlign.Left);
                    };
                });
            });
        }

        void OpenInn(Places.Inn inn, bool showWelcome = true)
        {
            if (showWelcome)
                inn.AvailableGold = 0;

            Action updatePartyGold = null;

            void SetupInn(Action updateGold, ItemGrid _)
            {
                updatePartyGold = updateGold;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Inn, inn);
                ShowPlaceWindow(inn.Name, showWelcome ? DataNameProvider.WelcomeInnkeeper : null, Picture80x80.Innkeeper,
                    inn, SetupInn, null, null, () => InputEnable = true);
                // Rest button
                layout.AttachEventToButton(3, () =>
                {
                    int totalCost = PartyMembers.Where(p => p.Alive).Count() * inn.Cost;
                    if (inn.AvailableGold < totalCost)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                        return;
                    }
                    layout.ShowPlaceQuestion($"{DataNameProvider.StayWillCost}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            inn.AvailableGold -= (uint)totalCost;
                            updatePartyGold?.Invoke();
                            layout.ShowClickChestMessage(DataNameProvider.InnkeeperGoodSleepWish, () =>
                            {
                                currentWindow.Window = Window.MapView; // This way closing the camp will return to map and not the Inn
                                layout.GetButtonAction(2)?.Invoke(); // Call close handler
                                OpenStorage = null;
                                Teleport((uint)inn.BedroomMapIndex, (uint)inn.BedroomX,
                                    (uint)inn.BedroomY, player.Direction, out _, true);
                                OpenCamp(true);
                            });
                        }
                    }, TextAlign.Left);
                });
            });
        }

        void OpenHorseSalesman(Places.HorseSalesman horseSalesman, string buyText, bool showWelcome = true)
        {
            if (showWelcome)
                horseSalesman.AvailableGold = 0;

            OpenTransportSalesman(horseSalesman, buyText, TravelType.Horse, Window.HorseSalesman,
                Picture80x80.Horse, showWelcome ? DataNameProvider.WelcomeHorseSeller : null);
        }

        void OpenRaftSalesman(Places.RaftSalesman raftSalesman, string buyText, bool showWelcome = true)
        {
            if (showWelcome)
                raftSalesman.AvailableGold = 0;

            OpenTransportSalesman(raftSalesman, buyText, TravelType.Raft, Window.RaftSalesman,
                Picture80x80.Merchant, showWelcome ? DataNameProvider.WelcomeRaftSeller : null);
        }

        void OpenShipSalesman(Places.ShipSalesman shipSalesman, string buyText, bool showWelcome = true)
        {
            if (showWelcome)
                shipSalesman.AvailableGold = 0;

            OpenTransportSalesman(shipSalesman, buyText, TravelType.Ship, Window.ShipSalesman,
                Picture80x80.Captain, showWelcome ? DataNameProvider.WelcomeShipSeller : null);
        }

        void OpenTransportSalesman(Places.Salesman salesman, string buyText, TravelType travelType,
            Window window, Picture80x80 picture80X80, string welcomeMessage)
        {
            Action updatePartyGold = null;

            void SetupSalesman(Action updateGold, ItemGrid _)
            {
                updatePartyGold = updateGold;
            }

            bool EnableBuying()
            {
                // Buying is enabled if on the target location isn't already
                // the given transport. Invalid data always disallows buying.

                if (salesman.SpawnMapIndex <= 0 || salesman.SpawnX <= 0 || salesman.SpawnY <= 0)
                    return false;

                var map = MapManager.GetMap((uint)salesman.SpawnMapIndex);

                if (map == null || map.Type == MapType.Map3D || !map.IsWorldMap || // Should not happen but never allow buying in these cases
                    salesman.SpawnX > map.Width || salesman.SpawnY > map.Height)
                    return false;

                var tile = map.Tiles[salesman.SpawnX - 1, salesman.SpawnY - 1];
                var tileset = MapManager.GetTilesetForMap(map);

                if (!tile.AllowMovement(tileset, travelType)) // Can't be placed there
                    return false;

                if (CurrentSavegame.TransportLocations.Any(t => t != null && t.MapIndex == map.Index &&
                    t.Position.X == salesman.SpawnX - 1 && t.Position.Y == salesman.SpawnY - 1))
                    return false;

                // TODO: Maybe change later
                // Allow 12 ships, 10 rafts and 10 horses
                int allowedCount = travelType == TravelType.Ship ? 12 : 10;
                return CurrentSavegame.TransportLocations.Count(t => t?.TravelType == travelType) < allowedCount;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(window, salesman, buyText);
                ShowPlaceWindow(salesman.Name, welcomeMessage, picture80X80,
                    salesman, SetupSalesman, null, null, () => InputEnable = true);
                if (!EnableBuying())
                {
                    layout.EnableButton(3, false);
                }
                else
                {
                    // Buy transport button
                    layout.AttachEventToButton(3, () =>
                    {
                        int totalCost = (salesman.PlaceType == PlaceType.HorseDealer ? PartyMembers.Where(p => p.Alive).Count() : 1) * salesman.Cost;
                        if (salesman.AvailableGold < totalCost)
                        {
                            layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                            return;
                        }
                        string costText = salesman.PlaceType switch
                        {
                            PlaceType.HorseDealer => DataNameProvider.PriceForHorse,
                            PlaceType.RaftDealer => DataNameProvider.PriceForRaft,
                            PlaceType.ShipDealer => DataNameProvider.PriceForShip,
                            _ => throw new AmbermoonException(ExceptionScope.Application, $"Invalid salesman place type: {salesman.PlaceType}")
                        };
                        layout.ShowPlaceQuestion($"{costText}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            if (answer) // yes
                            {
                                salesman.AvailableGold -= (uint)totalCost;
                                updatePartyGold?.Invoke();
                                void Buy()
                                {
                                    SpawnTransport((uint)salesman.SpawnMapIndex, (uint)salesman.SpawnX, (uint)salesman.SpawnY, travelType);
                                    layout.EnableButton(3, false);
                                }
                                if (string.IsNullOrWhiteSpace(buyText))
                                {
                                    Buy();
                                }
                                else
                                {
                                    layout.ShowClickChestMessage(buyText, Buy);
                                }
                            }
                        }, TextAlign.Left);
                    });
                }
            });
        }

        internal void SpawnTransport(uint mapIndex, uint x, uint y, TravelType travelType)
        {
            // TODO: This could theoretically spawn the transport on the current
            // map. In this case the transport should be added to the visual map immediately.
            for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
            {
                if (CurrentSavegame.TransportLocations[i] == null)
                {
                    CurrentSavegame.TransportLocations[i] = new TransportLocation
                    {
                        TravelType = travelType,
                        MapIndex = mapIndex,
                        Position = new Position((int)x, (int)y)
                    };
                    break;
                }
                else if (CurrentSavegame.TransportLocations[i].TravelType == TravelType.Walk)
                {
                    CurrentSavegame.TransportLocations[i].TravelType = travelType;
                    CurrentSavegame.TransportLocations[i].MapIndex = mapIndex;
                    CurrentSavegame.TransportLocations[i].Position = new Position((int)x, (int)y);
                    break;
                }
            }
        }

        void OpenFoodDealer(Places.FoodDealer foodDealer, bool showWelcome = true)
        {
            if (showWelcome)
                foodDealer.AvailableGold = 0;

            Action updatePartyGold = null;

            void SetupFoodDealer(Action updateGold, ItemGrid _)
            {
                updatePartyGold = updateGold;
            }

            void UpdateButtons()
            {
                layout.EnableButton(3, foodDealer.AvailableGold >= foodDealer.Cost);
                layout.EnableButton(4, foodDealer.AvailableFood > 0);
                layout.EnableButton(5, foodDealer.AvailableFood > 0);
            }

            void ShowDefaultMessage()
            {
                layout.ShowChestMessage(string.Format(DataNameProvider.OneFoodCosts, foodDealer.Cost), TextAlign.Center);
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.FoodDealer, foodDealer);
                ShowPlaceWindow(foodDealer.Name, showWelcome ? DataNameProvider.WelcomeFoodDealer : null,
                    Map.World switch
                    {
                        World.Lyramion => Picture80x80.Merchant,
                        World.ForestMoon => Picture80x80.DwarfMerchant,
                        World.Morag => Picture80x80.MoragMerchant,
                        _ => Picture80x80.Merchant
                    }, foodDealer, SetupFoodDealer, null,
                    () => foodDealer.AvailableFood == 0 ? null : DataNameProvider.WantToLeaveRestOfFood,
                    () => InputEnable = true);
                // Buy food button
                layout.AttachEventToButton(3, () =>
                {
                    layout.OpenAmountInputBox(DataNameProvider.BuyHowMuchFood, 109, DataNameProvider.FoodName,
                        Math.Min(99, foodDealer.AvailableGold / (uint)foodDealer.Cost), amount =>
                    {
                        ClosePopup();
                        layout.ShowPlaceQuestion($"{DataNameProvider.PriceOfFood}{amount * foodDealer.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            if (answer) // yes
                            {
                                foodDealer.AvailableGold -= amount * (uint)foodDealer.Cost;
                                foodDealer.AvailableFood += amount;
                                updatePartyGold?.Invoke();
                                UpdateFoodDisplay();
                                UpdateButtons();
                            }
                            ShowDefaultMessage();
                        }, TextAlign.Left);
                    }, () => { ClosePopup(); ShowDefaultMessage(); });
                });
                // Distribute food button
                layout.AttachEventToButton(4, () =>
                {
                    foodDealer.AvailableFood = DistributeFood(foodDealer.AvailableFood, false);
                    UpdateFoodDisplay();
                    UpdateButtons();

                    layout.ShowClickChestMessage(foodDealer.AvailableFood == 0
                        ? DataNameProvider.FoodDividedEqually : DataNameProvider.FoodLeftAfterDividing,
                        ShowDefaultMessage);
                });
                // Give food button
                layout.AttachEventToButton(5, () =>
                {
                    layout.GiveFood(foodDealer.AvailableFood, food =>
                    {
                        foodDealer.AvailableFood -= food;
                        UpdateFoodDisplay();
                        UpdateButtons();
                        UntrapMouse();
                        ExecuteNextUpdateCycle(ShowDefaultMessage);
                    }, () => layout.ShowChestMessage(DataNameProvider.GiveToWhom), ShowDefaultMessage,
                    () => layout.ShowClickChestMessage(DataNameProvider.NoOneCanCarryThatMuch));
                });
                void UpdateFoodDisplay()
                {
                    if (foodDealer.AvailableFood > 0)
                    {
                        ShowTextPanel(CharacterInfo.ChestFood, true,
                            $"{DataNameProvider.FoodName}^{foodDealer.AvailableFood}", new Rect(260, 104, 43, 15));
                    }
                    else
                    {
                        HideTextPanel(CharacterInfo.ChestFood);
                    }
                }
                UpdateButtons();
                if (!showWelcome)
                    ShowDefaultMessage();
                else
                {
                    void ClickedWelcomeMessage(bool _)
                    {
                        if (layout.ChestText != null)
                            layout.ChestText.Clicked -= ClickedWelcomeMessage;
                        ExecuteNextUpdateCycle(ShowDefaultMessage);
                    }
                    layout.ChestText.Clicked += ClickedWelcomeMessage;
                }
            });
        }

        internal void ExecuteNextUpdateCycle(Action action)
        {
            AddTimedEvent(TimeSpan.FromMilliseconds(0), action);
        }

        void OpenTrainer(Places.Trainer trainer, bool showWelcome = true)
        {
            if (showWelcome)
                trainer.AvailableGold = 0;

            Action updatePartyGold = null;

            void SetupTrainer(Action updateGold, ItemGrid _)
            {
                updatePartyGold = updateGold;
            }

            void Train(uint times)
            {
                layout.ShowPlaceQuestion($"{DataNameProvider.PriceForTraining}{times * trainer.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        trainer.AvailableGold -= times * (uint)trainer.Cost;
                        updatePartyGold?.Invoke();
                        CurrentPartyMember.Abilities[trainer.Ability].CurrentValue += times;
                        CurrentPartyMember.TrainingPoints -= (ushort)times;
                        PlayerSwitched();
                        layout.ShowClickChestMessage(DataNameProvider.IncreasedAfterTraining);
                    }
                }, TextAlign.Left);
            }

            void PlayerSwitched()
            {
                layout.EnableButton(3, CurrentPartyMember.Abilities[trainer.Ability].CurrentValue < CurrentPartyMember.Abilities[trainer.Ability].MaxValue);
            }

            uint GetMaxTrains() => Math.Max(0, Util.Min(trainer.AvailableGold / (uint)trainer.Cost, CurrentPartyMember.TrainingPoints,
                CurrentPartyMember.Abilities[trainer.Ability].MaxValue - CurrentPartyMember.Abilities[trainer.Ability].CurrentValue));

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Trainer, trainer);
                ShowPlaceWindow(trainer.Name, showWelcome ?
                    trainer.Ability switch
                    {
                        Ability.Attack => DataNameProvider.WelcomeAttackTrainer,
                        Ability.Parry => DataNameProvider.WelcomeParryTrainer,
                        Ability.Swim => DataNameProvider.WelcomeSwimTrainer,
                        Ability.CriticalHit => DataNameProvider.WelcomeCriticalHitTrainer,
                        Ability.FindTraps => DataNameProvider.WelcomeFindTrapTrainer,
                        Ability.DisarmTraps => DataNameProvider.WelcomeDisarmTrapTrainer,
                        Ability.LockPicking => DataNameProvider.WelcomeLockPickingTrainer,
                        Ability.Searching => DataNameProvider.WelcomeSearchTrainer,
                        Ability.ReadMagic => DataNameProvider.WelcomeReadMagicTrainer,
                        Ability.UseMagic => DataNameProvider.WelcomeUseMagicTrainer,
                        _ => throw new AmbermoonException(ExceptionScope.Data, "Invalid ability for trainer")
                    } : null,
                    trainer.Ability switch
                    {
                        Ability.Attack => Picture80x80.Knight,
                        Ability.Parry => Picture80x80.Knight,
                        Ability.Swim => Picture80x80.Knight, // TODO: right?
                        Ability.CriticalHit => Picture80x80.Thief,
                        Ability.FindTraps => Picture80x80.Thief,
                        Ability.DisarmTraps => Picture80x80.Thief,
                        Ability.LockPicking => Picture80x80.Thief,
                        Ability.Searching => Picture80x80.Thief,
                        Ability.ReadMagic => Picture80x80.Magician,
                        Ability.UseMagic => Picture80x80.Magician,
                        _ =>  Picture80x80.Knight
                    }, trainer, SetupTrainer, PlayerSwitched
                );
                // train button
                layout.AttachEventToButton(3, () =>
                {
                    if (trainer.AvailableGold < trainer.Cost)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                        return;
                    }

                    if (CurrentPartyMember.TrainingPoints == 0)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughTrainingPoints);
                        return;
                    }

                    layout.OpenAmountInputBox(DataNameProvider.TrainHowOften, null, null, GetMaxTrains(), times =>
                    {
                        ClosePopup();
                        Train(times);
                    }, ClosePopup);
                });
                PlayerSwitched();
            });
        }

        void ShowPlaceWindow(string placeName, string welcomeText, Picture80x80 picture, IPlace place, Action<Action, ItemGrid> placeSetup,
            Action activePlayerSwitchedHandler, Func<string> exitChecker = null, Action closeAction = null, int numItemSlots = 12)
        {
            OpenStorage = place;
            layout.SetLayout(LayoutType.Items);
            layout.AddText(new Rect(120, 37, 29 * Global.GlyphWidth, Global.GlyphLineHeight),
                renderView.TextProcessor.CreateText(placeName), TextColor.White);
            layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
            var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
            itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
            var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat(null as ItemSlot, numItemSlots).ToList(),
                false, 12, 6, numItemSlots, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
            itemGrid.Disabled = true;
            layout.AddItemGrid(itemGrid);
            layout.Set80x80Picture(picture);

            // Put all gold on the table!
            foreach (var partyMember in PartyMembers)
            {
                place.AvailableGold += partyMember.Gold;
                partyMember.RemoveGold(partyMember.Gold);
            }

            ShowTextPanel(CharacterInfo.ChestGold, true,
                $"{DataNameProvider.GoldName}^{place.AvailableGold}", new Rect(111, 104, 43, 15));

            if (welcomeText != null)
            {
                layout.ShowClickChestMessage(welcomeText);
            }

            void UpdateGoldDisplay()
                => characterInfoTexts[CharacterInfo.ChestGold].SetText(renderView.TextProcessor.CreateText($"{DataNameProvider.GoldName}^{place.AvailableGold}"));

            placeSetup?.Invoke(UpdateGoldDisplay, itemGrid);
            ActivePlayerChanged += activePlayerSwitchedHandler;

            // exit button
            layout.AttachEventToButton(2, () =>
            {
                var exitQuestion = exitChecker?.Invoke();

                if (exitQuestion != null)
                {
                    layout.OpenYesNoPopup(ProcessText(exitQuestion), Exit, ClosePopup, ClosePopup, 2);
                }
                else
                {
                    Exit();
                }

                void Exit()
                {
                    ActivePlayerChanged -= activePlayerSwitchedHandler;
                    CloseWindow();

                    // Distribute the gold
                    var partyMembers = PartyMembers.ToList();
                    uint availableGold = place.AvailableGold;
                    availableGold = DistributeGold(availableGold, false);
                    int goldPerPartyMember = (int)availableGold / partyMembers.Count;
                    int restGold = (int)availableGold % partyMembers.Count;

                    if (availableGold != 0)
                    {
                        for (int i = 0; i < partyMembers.Count; ++i)
                        {
                            int gold = goldPerPartyMember + (i < restGold ? 1 : 0);
                            partyMembers[i].AddGold((uint)gold);
                        }
                    }

                    closeAction?.Invoke();
                }
            });
        }

        void OpenMerchant(uint merchantIndex, string placeName, string buyText, bool isLibrary,
            bool showWelcome, ItemSlot[] boughtItems)
        {
            var merchant = GetMerchant(1 + merchantIndex);
            merchant.Name = placeName;
            if (showWelcome)
                merchant.AvailableGold = 0;

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Merchant, merchantIndex, placeName, buyText, isLibrary, boughtItems);
                ShowMerchantWindow(merchant, placeName, showWelcome ? isLibrary ? DataNameProvider.WelcomeMagician :
                    DataNameProvider.WelcomeMerchant : null, buyText,
                    isLibrary ? Picture80x80.Librarian : Map.World switch
                    {
                        World.Lyramion => Picture80x80.Merchant,
                        World.ForestMoon => Picture80x80.DwarfMerchant,
                        World.Morag => Picture80x80.MoragMerchant,
                        _ => Picture80x80.Merchant
                    },
                !isLibrary, boughtItems);
            });
        }

        void ShowMerchantWindow(Merchant merchant, string placeName, string initialText,
            string buyText, Picture80x80 picture, bool buysGoods, ItemSlot[] boughtItems)
        {
            // TODO: use buyText?

            OpenStorage = merchant;
            layout.SetLayout(LayoutType.Items);
            layout.AddText(new Rect(120, 37, 29 * Global.GlyphWidth, Global.GlyphLineHeight),
                renderView.TextProcessor.CreateText(placeName), TextColor.White);
            layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
            var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
            itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
            var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, merchant.Slots.ToList(),
                false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical, false,
                () => merchant.AvailableGold);
            itemGrid.Disabled = false;
            layout.AddItemGrid(itemGrid);
            layout.Set80x80Picture(picture);
            var itemArea = new Rect(16, 139, 151, 53);
            int mode = -1; // -1: show bought items, 0: buy, 3: sell, 4: examine (= button index)
            boughtItems ??= Enumerable.Repeat(new ItemSlot(), 24).ToArray();
            currentWindow.WindowParameters[4] = boughtItems;

            void SetupRightClickAbort()
            {
                nextClickHandler = buttons =>
                {
                    if (buttons == MouseButtons.Right)
                    {
                        itemGrid.HideTooltip();
                        layout.ShowChestMessage(null);
                        UntrapMouse();
                        ShowBoughtItems();
                        return true;
                    }

                    return false;
                };
            }

            void AssignButton(int index, bool merchantItems, string messageText, TextAlign textAlign, Func<bool> checker)
            {
                layout.AttachEventToButton(index, () =>
                {
                    if (checker?.Invoke() == false)
                        return;

                    mode = index;
                    itemGrid.DisableDrag = true;
                    layout.ShowChestMessage(messageText, textAlign);
                    CursorType = CursorType.Sword;
                    TrapMouse(itemArea);
                    FillItems(merchantItems);
                    itemGrid.ShowPrice = mode == 0; // buy
                    SetupRightClickAbort();
                });
            }

            // Buy button
            AssignButton(0, true, DataNameProvider.BuyWhichItem, TextAlign.Center, null);
            // Sell button
            if (buysGoods)
            {
                AssignButton(3, false, DataNameProvider.SellWhichItem, TextAlign.Left, () =>
                {
                    if (!merchant.HasEmptySlots())
                    {
                        layout.ShowClickChestMessage(DataNameProvider.MerchantFull);
                        return false;
                    }
                    return true;
                });
            }
            else
            {
                layout.EnableButton(3, false);
            }
            // Examine button
            AssignButton(4, true, DataNameProvider.ExamineWhichItemMerchant, TextAlign.Left, null);
            // Exit button
            layout.AttachEventToButton(2, () =>
            {
                void Exit()
                {
                    CloseWindow();

                    // Distribute the gold
                    var partyMembers = PartyMembers.ToList();
                    uint availableGold = merchant.AvailableGold;
                    availableGold = DistributeGold(availableGold, false);
                    int goldPerPartyMember = (int)availableGold / partyMembers.Count;
                    int restGold = (int)availableGold % partyMembers.Count;

                    if (availableGold != 0)
                    {
                        for (int i = 0; i < partyMembers.Count; ++i)
                        {
                            int gold = goldPerPartyMember + (i < restGold ? 1 : 0);
                            partyMembers[i].AddGold((uint)gold);
                        }
                    }
                }

                if (boughtItems.Any(item => item != null && !item.Empty))
                {
                    layout.OpenYesNoPopup(ProcessText(DataNameProvider.WantToGoWithoutItemsMerchant), Exit, ClosePopup, ClosePopup, 2);
                }
                else
                {
                    Exit();
                }
            });

            void UpdateButtons()
            {
                // Note: Disabling the buy button if no slot is free in bought items grid might be bad in rare
                // cases because you still might buy some stackable items like arrows. But this is very rare cause
                // you would have to buy some of this items before.
                layout.EnableButton(0, boughtItems.Any(slot => slot == null || slot.Empty) && merchant.AvailableGold > 0);
                bool anyItemsToSell = merchant.Slots.ToList().Any(s => !s.Empty);
                layout.EnableButton(3, anyItemsToSell && buysGoods);
                layout.EnableButton(4, anyItemsToSell);
            }

            void FillItems(bool fromMerchant)
            {
                itemGrid.Initialize(fromMerchant ? merchant.Slots.ToList() : CurrentPartyMember.Inventory.Slots.ToList(), fromMerchant);
            }

            void ShowBoughtItems()
            {
                mode = -1;
                itemGrid.DisableDrag = false;
                itemGrid.ShowPrice = false;
                itemGrid.Initialize(boughtItems.ToList(), false);
            }

            uint CalculatePrice(uint price)
            {
                var charisma = CurrentPartyMember.Attributes[Attribute.Charisma].TotalCurrentValue;
                var basePrice = price / 3;
                var bonus = (uint)Util.Floor(Util.Floor(charisma / 10) * (price / 100.0f));
                return basePrice + bonus;
            }
            itemGrid.DisableDrag = false;
            itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                // This can only happen for bought items but we check for safety here
                if (mode != -1)
                    throw new AmbermoonException(ExceptionScope.Application, "Non-bought items should not be draggable.");

                boughtItems[slotIndex].Remove(amount);
                layout.EnableButton(0, boughtItems.Any(slot => slot == null || slot.Empty) && merchant.AvailableGold > 0);
            };
            itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
            {
                if (mode == -1)
                {
                    foreach (var partyMember in PartyMembers)
                        layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value);
                }
            };
            itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
            {
                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                if (mode == -1) // show bought items
                {
                    // No interaction
                    return;
                }
                else if (mode == 0) // buy
                {
                    itemGrid.HideTooltip();

                    if (merchant.AvailableGold < item.Price)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoneyToBuy, () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        });
                        return;
                    }

                    nextClickHandler = null;
                    UntrapMouse();

                    uint GetMaxItemsToBuy(uint itemIndex)
                    {
                        var item = ItemManager.GetItem(itemIndex);

                        if (item.Flags.HasFlag(ItemFlags.Stackable))
                        {
                            if (boughtItems.Any(slot => slot == null || slot.Empty))
                                return 99;

                            var slotWithItem = boughtItems.FirstOrDefault(slot => slot.ItemIndex == itemIndex && slot.Amount < 99);

                            return slotWithItem == null ? 0 : 99u - (uint)slotWithItem.Amount;
                        }
                        else
                        {
                            return (uint)boughtItems.Count(slot => slot == null || slot.Empty);
                        }
                    }

                    void Buy(uint amount)
                    {
                        ClosePopup();
                        layout.ShowPlaceQuestion($"{DataNameProvider.ThisWillCost}{amount * item.Price}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            if (answer) // yes
                            {
                                int column = slotIndex % Merchant.SlotsPerRow;
                                int row = slotIndex / Merchant.SlotsPerRow;
                                int numCharges = itemSlot.NumRemainingCharges;
                                byte rechargeTimes = itemSlot.RechargeTimes;
                                var flags = itemSlot.Flags;
                                merchant.TakeItems(column, row, amount);
                                itemGrid.SetItem(slotIndex, merchant.Slots[column, row], true);
                                merchant.AvailableGold -= amount * item.Price;
                                UpdateGoldDisplay();
                                if (item.Flags.HasFlag(ItemFlags.Stackable))
                                {
                                    for (int i = 0; i < boughtItems.Length; ++i)
                                    {
                                        if (boughtItems[i] != null && boughtItems[i].ItemIndex == item.Index &&
                                            boughtItems[i].Amount < 99)
                                        {
                                            int space = 99 - boughtItems[i].Amount;
                                            int add = Math.Min(space, (int)amount);
                                            boughtItems[i].Amount += add;
                                            amount -= (uint)add;
                                            if (amount == 0)
                                                break;
                                        }
                                    }
                                    if (amount != 0)
                                    {
                                        for (int i = 0; i < boughtItems.Length; ++i)
                                        {
                                            if (boughtItems[i] == null || boughtItems[i].Empty)
                                            {
                                                boughtItems[i] = new ItemSlot
                                                {
                                                    ItemIndex = item.Index,
                                                    Amount = (int)amount,
                                                    NumRemainingCharges = numCharges,
                                                    RechargeTimes = rechargeTimes,
                                                    Flags = flags
                                                };
                                                amount = 0;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < boughtItems.Length; ++i)
                                    {
                                        if (boughtItems[i] == null || boughtItems[i].Empty)
                                        {
                                            boughtItems[i] = new ItemSlot
                                            {
                                                ItemIndex = item.Index,
                                                Amount = 1,
                                                NumRemainingCharges = numCharges,
                                                RechargeTimes = rechargeTimes,
                                                Flags = flags
                                            };
                                            if (--amount == 0)
                                                break;
                                        }
                                    }
                                }
                                UpdateButtons();
                            }

                            ShowBoughtItems();
                        }, TextAlign.Left);
                    }

                    if (itemSlot.Amount > 1)
                    {
                        layout.OpenAmountInputBox(DataNameProvider.BuyHowMuchItems,
                            item.GraphicIndex, item.Name, Util.Min((uint)itemSlot.Amount, merchant.AvailableGold / item.Price, GetMaxItemsToBuy(item.Index)), Buy,
                            () =>
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                            }
                        );
                    }
                    else
                    {
                        Buy(1);
                    }
                }
                else if (mode == 3) // sell
                {
                    itemGrid.HideTooltip();

                    if (!item.Flags.HasFlag(ItemFlags.NotImportant) || item.Price < 9) // TODO: Don't know if this is right
                    {
                        layout.ShowClickChestMessage(DataNameProvider.NotInterestedInItemMerchant, () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        });
                        return;
                    }

                    if (itemSlot.Flags.HasFlag(ItemSlotFlags.Broken))
                    {
                        layout.ShowClickChestMessage(DataNameProvider.WontBuyBrokenStuff, () =>
                        {
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        });
                        return;
                    }

                    nextClickHandler = null;
                    UntrapMouse();

                    uint GetMaxItemsToSell(uint itemIndex)
                    {
                        var item = ItemManager.GetItem(itemIndex);

                        var slots = merchant.Slots.ToList();

                        if (slots.Any(slot => slot == null || slot.Empty))
                            return 99;

                        var slotWithItem = slots.FirstOrDefault(slot => slot.ItemIndex == itemIndex && slot.Amount < 99);

                        return slotWithItem == null ? 0 : 99u - (uint)slotWithItem.Amount;
                    }

                    void Sell(uint amount)
                    {
                        ClosePopup();
                        var sellPrice = amount * CalculatePrice(item.Price);
                        layout.ShowPlaceQuestion($"{DataNameProvider.ForThisIllGiveYou}{sellPrice}{DataNameProvider.AgreeOnPrice}", answer =>
                        {
                            if (answer) // yes
                            {
                                merchant.AddItems(ItemManager, item.Index, amount, itemSlot);
                                CurrentPartyMember.Inventory.Slots[slotIndex].Remove((int)amount);
                                InventoryItemRemoved(item.Index, (int)amount, CurrentPartyMember);
                                itemGrid.SetItem(slotIndex, CurrentPartyMember.Inventory.Slots[slotIndex], true);
                                merchant.AvailableGold += sellPrice;
                                UpdateGoldDisplay();
                                UpdateButtons();
                            }

                            if (!merchant.Slots.ToList().Any(s => s.Empty))
                                ShowBoughtItems();
                            else
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                            }
                        }, TextAlign.Left);
                    }

                    if (itemSlot.Amount > 1)
                    {
                        layout.OpenAmountInputBox(DataNameProvider.SellHowMuchItems,
                            item.GraphicIndex, item.Name, Util.Min((uint)itemSlot.Amount, GetMaxItemsToSell(item.Index)), Sell,
                            () =>
                            {
                                TrapMouse(itemArea);
                                SetupRightClickAbort();
                            }
                        );
                    }
                    else
                    {
                        Sell(1);
                    }
                }
                else if (mode == 4) // examine
                {
                    itemGrid.HideTooltip();
                    nextClickHandler = null;
                    UntrapMouse();
                    ShowItemPopup(itemSlot, () =>
                    {
                        TrapMouse(itemArea);
                        SetupRightClickAbort();
                    });
                }
                else
                {
                    throw new AmbermoonException(ExceptionScope.Application, "Invalid merchant mode.");
                }
            };

            // Put all gold on the table!
            foreach (var partyMember in PartyMembers)
            {
                merchant.AvailableGold += partyMember.Gold;
                partyMember.RemoveGold(partyMember.Gold);
            }

            ShowTextPanel(CharacterInfo.ChestGold, true,
                $"{DataNameProvider.GoldName}^{merchant.AvailableGold}", new Rect(111, 104, 43, 15));

            UpdateButtons();
            ShowBoughtItems();

            if (initialText != null)
            {
                layout.ShowClickChestMessage(initialText);
            }

            void UpdateGoldDisplay()
                => characterInfoTexts[CharacterInfo.ChestGold].SetText(renderView.TextProcessor.CreateText($"{DataNameProvider.GoldName}^{merchant.AvailableGold}"));
        }

        internal void UseSpell(PartyMember caster, Spell spell, ItemGrid itemGrid, bool fromItem, Action<Action> consumeHandler = null)
        {
            CurrentCaster = caster;
            CurrentSpellTarget = null;

            // Some special care for the mystic map spells
            if (!is3D && spell >= Spell.FindTraps && spell <= Spell.MysticalMapping)
            {
                ShowMessagePopup(DataNameProvider.UseSpellOnlyInCitiesOrDungeons);
                return;
            }

            var spellInfo = SpellInfos.Entries[spell];

            void ConsumeSP()
            {
                if (!fromItem) // Item spells won't consume SP
                {
                    caster.SpellPoints.CurrentValue -= spellInfo.SP;
                    layout.FillCharacterBars(caster);
                }
            }

            bool checkFail = !fromItem; // Item spells can't fail

            switch (spellInfo.Target)
            {
                case SpellTarget.SingleFriend:
                {
                    layout.OpenTextPopup(ProcessText(DataNameProvider.BattleMessageWhichPartyMemberAsTarget), null, true, false, false, TextAlign.Center);
                    PickTargetPlayer();
                    void TargetPlayerPicked(int characterSlot)
                    {
                        targetPlayerPicked -= TargetPlayerPicked;
                        ClosePopup();
                        UntrapMouse();

                        if (characterSlot != -1)
                        {
                            void Consume()
                            {
                                ConsumeSP();
                                bool reviveSpell = spell >= Spell.WakeTheDead && spell <= Spell.ChangeDust;
                                void Cast()
                                {
                                    var target = GetPartyMember(characterSlot);
                                    if (target != null && (reviveSpell || spell == Spell.AllHealing || target.Alive))
                                    {
                                        if (reviveSpell)
                                        {
                                            ApplySpellEffect(spell, caster, target, null, checkFail);
                                        }
                                        else
                                        {
                                            currentAnimation?.Destroy();
                                            currentAnimation = new SpellAnimation(this, layout);
                                            currentAnimation.CastOn(spell, target, () =>
                                            {
                                                currentAnimation.Destroy();
                                                currentAnimation = null;
                                                ApplySpellEffect(spell, caster, target, null, false);
                                            });
                                        }
                                    }
                                }
                                if (!reviveSpell && checkFail)
                                    TrySpell(Cast);
                                else
                                    Cast();
                            }
                            if (consumeHandler != null)
                                consumeHandler(Consume);
                            else
                                Consume();
                        }
                    }
                    targetPlayerPicked += TargetPlayerPicked;
                    break;
                }
                case SpellTarget.FriendRow:
                    throw new AmbermoonException(ExceptionScope.Application, $"Friend row spells are not implemented as there are none in Ambermoon.");
                case SpellTarget.AllFriends:
                {
                    void Consume()
                    {
                        ConsumeSP();
                        void Cast()
                        {
                            currentAnimation?.Destroy();
                            currentAnimation = new SpellAnimation(this, layout);

                            currentAnimation.CastHealingOnPartyMembers(() =>
                            {
                                currentAnimation.Destroy();
                                currentAnimation = null;

                                if (spell == Spell.Resurrection)
                                {
                                    void Revive(PartyMember target, Action finishAction) =>
                                        ApplySpellEffect(Spell.Resurrection, caster, target, finishAction, checkFail);
                                    ForeachPartyMember(Revive, p => p.Ailments.HasFlag(Ailment.DeadCorpse));
                                }
                                else
                                {
                                    foreach (var partyMember in PartyMembers.Where(p => p.Alive))
                                        ApplySpellEffect(spell, caster, partyMember, null, false);
                                }
                            }, spell == Spell.Resurrection);
                        }
                        if (checkFail)
                            TrySpell(Cast);
                        else
                            Cast();
                    }
                    if (consumeHandler != null)
                        consumeHandler(Consume);
                    else
                        Consume();
                    break;
                }
                case SpellTarget.Item:
                    // Item spells will never come from an item so don't bother with consume logic here.
                    layout.ShowChestMessage(spell == Spell.RemoveCurses ? DataNameProvider.BattleMessageWhichPartyMemberAsTarget
                        : DataNameProvider.WhichInventoryAsTarget);
                    PickTargetInventory();
                    bool TargetInventoryPicked(int characterSlot)
                    {
                        targetInventoryPicked -= TargetInventoryPicked;

                        if (characterSlot == -1)
                            return true; // abort, TargetItemPicked is called and will cleanup

                        if (spell == Spell.RemoveCurses)
                        {
                            var target = GetPartyMember(characterSlot);
                            var firstCursedItem = target.Equipment.Slots.Values.FirstOrDefault(s => s.Flags.HasFlag(ItemSlotFlags.Cursed));

                            if (firstCursedItem == null)
                            {
                                void CleanUp()
                                {
                                    itemGrid?.HideTooltip();
                                    layout.SetInventoryMessage(null);
                                    UntrapMouse();
                                    EndSequence();
                                    layout.ShowChestMessage(null);
                                }

                                ConsumeSP();
                                EndSequence();
                                ShowMessagePopup(DataNameProvider.NoCursedItemFound, CleanUp);
                                return false; // no item selection
                            }
                        }

                        return true; // move forward to item selection
                    }
                    bool TargetItemPicked(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
                    {
                        targetItemPicked -= TargetItemPicked;
                        itemGrid?.HideTooltip();
                        layout.SetInventoryMessage(null);
                        if (itemSlot != null)
                        {
                            ConsumeSP();
                            StartSequence();
                            ApplySpellEffect(spell, caster, itemSlot, () =>
                            {
                                CloseWindow();
                                UntrapMouse();
                                EndSequence();
                                layout.ShowChestMessage(null);
                            }, checkFail);
                            return false; // manual window closing etc
                        }
                        else
                        {
                            layout.ShowChestMessage(null);
                            return true; // auto-close window and cleanup
                        }
                    }
                    targetInventoryPicked += TargetInventoryPicked;
                    targetItemPicked += TargetItemPicked;
                    break;
                case SpellTarget.None:
                {
                    void Consume()
                    {
                        ConsumeSP();
                        ApplySpellEffect(spell, caster, null, checkFail);
                    }
                    if (consumeHandler != null)
                        consumeHandler(Consume);
                    else
                        Consume();
                    break;
                }
                default:
                    throw new AmbermoonException(ExceptionScope.Application, $"Spells with target {spellInfo.Target} should not be usable in camps.");
            }
        }

        /// <summary>
        /// Cast a spell on the map or in a camp.
        /// </summary>
        /// <param name="camp"></param>
        internal void CastSpell(bool camp, ItemGrid itemGrid = null)
        {
            if (!CurrentPartyMember.HasAnySpell())
            {
                ShowMessagePopup(DataNameProvider.YouDontKnowAnySpellsYet);
            }
            else
            {
                OpenSpellList(CurrentPartyMember,
                    spell =>
                    {
                        var spellInfo = SpellInfos.Entries[spell];

                        if (camp && !spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Camp))
                            return DataNameProvider.WrongArea;

                        if (!camp)
                        {
                            if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.AnyMap))
                            {
                                if (spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.WorldMapOnly))
                                {
                                    if (!Map.IsWorldMap)
                                        return DataNameProvider.WrongArea;
                                }
                                else if (spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.DungeonOnly))
                                {
                                    if (Map.Type != MapType.Map3D || Map.Flags.HasFlag(MapFlags.Outdoor))
                                        return DataNameProvider.WrongArea;
                                }
                                else
                                {
                                    return DataNameProvider.WrongArea;
                                }
                            }
                        }

                        var worldFlag = (WorldFlag)(1 << (int)Map.World);

                        if (!spellInfo.Worlds.HasFlag(worldFlag))
                            return DataNameProvider.WrongWorld;

                        if (spellInfo.SP > CurrentPartyMember.SpellPoints.CurrentValue)
                            return DataNameProvider.NotEnoughSP;

                        // TODO: Is there more to check? Irritated?

                        return null;
                    },
                    spell => UseSpell(CurrentPartyMember, spell, itemGrid, false)
                );
            }
        }

        internal void OpenCamp(bool inn)
        {
            if (!inn && MonsterSeesPlayer)
            {
                ShowMessagePopup(DataNameProvider.RestingTooDangerous);
                return;
            }

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Camp, inn);
                layout.SetLayout(LayoutType.Items);
                layout.Set80x80Picture(inn ? Picture80x80.RestInn : Map.Flags.HasFlag(MapFlags.Outdoor) ? Picture80x80.RestOutdoor : Picture80x80.RestDungeon);
                layout.FillArea(new Rect(110, 43, 194, 80), GetUIColor(28), false);
                var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
                itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
                var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, Enumerable.Repeat(null as ItemSlot, 24).ToList(),
                    false, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
                itemGrid.Disabled = true;
                layout.AddItemGrid(itemGrid);
                var itemArea = new Rect(16, 139, 151, 53);

                void PlayerSwitched()
                {
                    itemGrid.HideTooltip();
                    itemGrid.Disabled = true;
                    layout.ShowChestMessage(null);
                    UntrapMouse();
                    CursorType = CursorType.Sword;
                    inputEnable = true;
                    bool magicClass = CurrentPartyMember.Class.IsMagic();
                    layout.EnableButton(0, magicClass);
                    layout.EnableButton(3, magicClass);
                }

                ActivePlayerChanged += PlayerSwitched;

                void Exit()
                {
                    ActivePlayerChanged -= PlayerSwitched;
                    CloseWindow();
                }

                // exit button
                layout.AttachEventToButton(2, Exit);

                // use magic button
                layout.AttachEventToButton(0, () => CastSpell(true, itemGrid));

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

                // read magic button
                layout.AttachEventToButton(3, () =>
                {
                    layout.ShowChestMessage(DataNameProvider.WhichScrollToRead, TextAlign.Left);
                    itemGrid.Disabled = false;
                    itemGrid.DisableDrag = true;
                    CursorType = CursorType.Sword;
                    TrapMouse(itemArea);
                    itemGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                    SetupRightClickAbort();
                });

                // sleep button
                layout.AttachEventToButton(6, () =>
                {
                    if (CurrentSavegame.HoursWithoutSleep < 8)
                    {
                        layout.ShowClickChestMessage(DataNameProvider.RestingWouldHaveNoEffect);
                    }
                    else
                    {
                        Sleep(inn);
                    }
                });

                itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
                {
                    itemGrid.HideTooltip();

                    void Error(string message, Action additionalAction = null)
                    {
                        layout.ShowClickChestMessage(message, () =>
                        {
                            layout.ShowChestMessage(DataNameProvider.WhichScrollToRead);
                            additionalAction?.Invoke();
                            TrapMouse(itemArea);
                            SetupRightClickAbort();
                        });
                    }

                    // This is only used in "read magic".
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);

                    if (item.Type != ItemType.SpellScroll || item.Spell == Spell.None)
                    {
                        Error(DataNameProvider.ThatsNotASpellScroll);
                    }
                    else if (item.SpellSchool != CurrentPartyMember.Class.ToSpellSchool())
                    {
                        Error(DataNameProvider.CantLearnSpellsOfType);
                    }
                    else if (CurrentPartyMember.HasSpell(item.Spell))
                    {
                        Error(DataNameProvider.AlreadyKnowsSpell);
                    }
                    else
                    {
                        var spellInfo = SpellInfos.Entries[item.Spell];

                        if (CurrentPartyMember.SpellLearningPoints < spellInfo.SLP)
                        {
                            Error(DataNameProvider.NotEnoughSpellLearningPoints);
                        }
                        else
                        {
                            CurrentPartyMember.SpellLearningPoints -= (ushort)spellInfo.SLP;

                            if (RollDice100() < CurrentPartyMember.Abilities[Ability.ReadMagic].TotalCurrentValue)
                            {
                                // Learned spell
                                Error(DataNameProvider.ManagedToLearnSpell, () =>
                                {
                                    CurrentPartyMember.AddSpell(item.Spell);
                                    layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true);
                                });
                            }
                            else
                            {
                                // Failed to learn the spell
                                Error(DataNameProvider.FailedToLearnSpell, () =>
                                {
                                    layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50));
                                });
                            }
                        }
                    }
                };
            });
        }

        internal void ShowItemPopup(ItemSlot itemSlot, Action closeAction)
        {
            var item = ItemManager.GetItem(itemSlot.ItemIndex);
            var popup = layout.OpenPopup(new Position(16, 84), 18, 6, true, false);
            var itemArea = new Rect(31, 99, 18, 18);
            popup.AddSunkenBox(itemArea);
            popup.AddItemImage(itemArea.CreateModified(1, 1, -2, -2), item.GraphicIndex);
            popup.AddText(new Position(51, 101), item.Name, TextColor.White);
            popup.AddText(new Position(51, 109), DataNameProvider.GetItemTypeName(item.Type), TextColor.White);
            popup.AddText(new Position(32, 120), string.Format(DataNameProvider.ItemWeightDisplay.Replace("{0:00000}", "{0,5}"), item.Weight), TextColor.White);
            popup.AddText(new Position(32, 130), string.Format(DataNameProvider.ItemHandsDisplay, item.NumberOfHands), TextColor.White);
            popup.AddText(new Position(32, 138), string.Format(DataNameProvider.ItemFingersDisplay, item.NumberOfFingers), TextColor.White);
            popup.AddText(new Position(32, 146), DataNameProvider.ItemDamageDisplay.Replace(" {0:000}", item.Damage.ToString("+#;-#;0")), TextColor.White);
            popup.AddText(new Position(32, 154), DataNameProvider.ItemDefenseDisplay.Replace(" {0:000}", item.Defense.ToString("+#;-#;0")), TextColor.White);

            popup.AddText(new Position(177, 99), DataNameProvider.ClassesHeaderString, TextColor.LightGray);
            int column = 0;
            int row = 0;
            foreach (var @class in Enum.GetValues<Class>())
            {
                var classFlag = (ClassFlag)(1 << (int)@class);

                if (item.Classes.HasFlag(classFlag))
                {
                    popup.AddText(new Position(177 + column * 54, 107 + row * Global.GlyphLineHeight), DataNameProvider.GetClassName(@class), TextColor.White);

                    if (++row == 5)
                    {
                        ++column;
                        row = 0;
                    }
                }
            }
            popup.AddText(new Position(177, 146), DataNameProvider.GenderHeaderString, TextColor.LightGray);
            popup.AddText(new Position(177, 154), DataNameProvider.GetGenderName(item.Genders), TextColor.White);

            void Close()
            {
                ClosePopup();
                // Note: If we call closeAction directly any new nextClickAction
                // assignment will be lost when we return true below because the
                // nextClickHandler processing will set it to null then afterwards.
                ExecuteNextUpdateCycle(closeAction);
            }

            void HandleRightClick()
            {
                if (!popup.HasChildPopup)
                {
                    Close();
                }
                else
                {
                    ExecuteNextUpdateCycle(() =>
                    {
                        popup.CloseChildPopup();
                        SetupRightClickHandler();
                    });
                }
            }

            void SetupRightClickHandler()
            {
                nextClickHandler = button =>
                {
                    if (button == MouseButtons.Right)
                    {
                        HandleRightClick();
                        return true;
                    }
                    return false;
                };
            }

            // This can only be closed with right click
            SetupRightClickHandler();

            if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
            {
                var eyeButton = popup.AddButton(new Position(popup.ContentArea.Right - Button.Width + 1, popup.ContentArea.Bottom - Button.Height + 1));
                eyeButton.ButtonType = ButtonType.Eye;
                eyeButton.Disabled = false;
                eyeButton.LeftClickAction += () => ShowItemDetails(popup, itemSlot);
                eyeButton.RightClickAction += Close;
                eyeButton.Visible = true;
            }
        }

        void ShowItemDetails(Popup itemPopup, ItemSlot itemSlot)
        {
            layout.HideTooltip();
            var item = ItemManager.GetItem(itemSlot.ItemIndex);
            bool cursed = itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed) || item.Flags.HasFlag(ItemFlags.Accursed);
            int factor = cursed ? -1 : 1;
            var detailsPopup = itemPopup.AddPopup(new Position(32, 52), 12, 6);

            void AddValueDisplay(Position position, string formatString, int value)
            {
                detailsPopup.AddText(position, formatString.Replace("000", "00")
                    .Replace(" {0:00}", value.ToString("+#;-#; 0")), TextColor.White);
            }

            AddValueDisplay(new Position(48, 68), DataNameProvider.MaxLPDisplay, factor * item.HitPoints);
            AddValueDisplay(new Position(128, 68), DataNameProvider.MaxSPDisplay, factor * item.SpellPoints);
            AddValueDisplay(new Position(48, 75), DataNameProvider.MBWDisplay, item.MagicAttackLevel);
            AddValueDisplay(new Position(128, 75), DataNameProvider.MBRDisplay, item.MagicArmorLevel);
            detailsPopup.AddText(new Position(48, 82), DataNameProvider.AttributeHeader, TextColor.LightOrange);
            if (item.Attribute != null && item.AttributeValue != 0)
            {
                detailsPopup.AddText(new Position(48, 89), DataNameProvider.GetAttributeName(item.Attribute.Value), TextColor.White);
                detailsPopup.AddText(new Position(170, 89), (factor * item.AttributeValue).ToString("+#;-#; 0"), TextColor.White);
            }
            detailsPopup.AddText(new Position(48, 96), DataNameProvider.AbilitiesHeaderString, TextColor.LightOrange);
            if (item.Ability != null && item.AbilityValue != 0)
            {
                detailsPopup.AddText(new Position(48, 103), DataNameProvider.GetAbilityName(item.Ability.Value), TextColor.White);
                detailsPopup.AddText(new Position(170, 103), (factor * item.AbilityValue).ToString("+#;-#; 0"), TextColor.White);
            }
            detailsPopup.AddText(new Position(48, 110), DataNameProvider.FunctionHeader, TextColor.LightOrange);
            if (item.Spell != Spell.None && item.InitialCharges != 0)
            {
                detailsPopup.AddText(new Position(48, 117),
                    $"{DataNameProvider.GetSpellName(item.Spell)} ({(itemSlot.NumRemainingCharges > 99 ? "**" : itemSlot.NumRemainingCharges.ToString())})",
                    TextColor.White);
            }
            if (cursed)
            {
                var contentArea = detailsPopup.ContentArea;
                AddAnimatedText((area, text, color, align) => detailsPopup.AddText(area, text, color, align),
                    new Rect(contentArea.X, 124, contentArea.Width, Global.GlyphLineHeight), DataNameProvider.Cursed,
                    TextAlign.Center, () => layout.PopupActive && itemPopup?.HasChildPopup == true, 50, false);
            }
        }

        UIText AddAnimatedText(Func<Rect, string, TextColor, TextAlign, UIText> textAdder, Rect area,
            string text, TextAlign textAlign, Func<bool> continueChecker, int timePerFrame, bool blink)
        {
            int textColorIndex = 0;
            var textColors = blink
                ? TextColors.TextBlinkColors
                : TextColors.TextAnimationColors;
            var animatedText = textAdder(area, text, textColors[0], textAlign);
            void AnimateText()
            {
                if (animatedText != null && continueChecker?.Invoke() == true)
                {
                    animatedText.SetTextColor(textColors[textColorIndex]);
                    textColorIndex = (textColorIndex + 1) % textColors.Length;
                    AddTimedEvent(TimeSpan.FromMilliseconds(timePerFrame), AnimateText);
                }
            }
            AnimateText();
            return animatedText;
        }

        internal bool EnterPlace(Map map, EnterPlaceEvent enterPlaceEvent)
        {
            if (WindowActive)
                return false;

            ResetMoveKeys();

            int openingHour = enterPlaceEvent.OpeningHour;
            int closingHour = enterPlaceEvent.ClosingHour == 0 ? 24 : enterPlaceEvent.ClosingHour;

            if (GameTime.Hour >= openingHour && GameTime.Hour < closingHour)
            {
                switch (enterPlaceEvent.PlaceType)
                {
                    case PlaceType.Trainer:
                    {
                        var trainerData = new Places.Trainer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenTrainer(trainerData);
                        return true;
                    }
                    case PlaceType.Healer:
                    {
                        var healerData = new Places.Healer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenHealer(healerData);
                        return true;
                    }
                    case PlaceType.Sage:
                    {
                        var sageData = new Places.Sage(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenSage(sageData);
                        return true;
                    }
                    case PlaceType.Enchanter:
                    {
                        var enchanterData = new Places.Enchanter(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenEnchanter(enchanterData);
                        return true;
                    }
                    case PlaceType.Inn:
                    {
                        var innData = new Places.Inn(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenInn(innData);
                        return true;
                    }
                    case PlaceType.Merchant:
                    case PlaceType.Library:
                        OpenMerchant(enterPlaceEvent.MerchantDataIndex, places.Entries[(int)enterPlaceEvent.PlaceIndex - 1].Name,
                            enterPlaceEvent.UsePlaceTextIndex == 0xff ? null : map.Texts[enterPlaceEvent.UsePlaceTextIndex],
                            enterPlaceEvent.PlaceType == PlaceType.Library, true, null);
                        return true;
                    case PlaceType.FoodDealer:
                    {
                        var foodDealerData = new Places.FoodDealer(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenFoodDealer(foodDealerData);
                        return true;
                    }
                    case PlaceType.HorseDealer:
                    {
                        var horseDealerData = new Places.HorseSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenHorseSalesman(horseDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null : map.Texts[enterPlaceEvent.UsePlaceTextIndex]);
                        return true;
                    }
                    case PlaceType.RaftDealer:
                    {
                        var raftDealerData = new Places.RaftSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenRaftSalesman(raftDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null : map.Texts[enterPlaceEvent.UsePlaceTextIndex]);
                        return true;
                    }
                    case PlaceType.ShipDealer:
                    {
                        var shipDealerData = new Places.ShipSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenShipSalesman(shipDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null : map.Texts[enterPlaceEvent.UsePlaceTextIndex]);
                        return true;
                    }
                    case PlaceType.Blacksmith:
                    {
                        var blacksmithData = new Places.Blacksmith(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                        OpenBlacksmith(blacksmithData);
                        return true;
                    }
                    default:
                        throw new AmbermoonException(ExceptionScope.Data, "Unknown place type.");
                }
            }
            else if (enterPlaceEvent.ClosedTextIndex != 255)
            {
                string closedText = map.Texts[enterPlaceEvent.ClosedTextIndex];
                ShowTextPopup(ProcessText(closedText), null);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void StartBattle(StartBattleEvent battleEvent, Event nextEvent, uint? combatBackgroundIndex = null)
        {
            if (BattleActive)
                return;

            ResetMoveKeys();

            currentBattleInfo = new BattleInfo
            {
                MonsterGroupIndex = battleEvent.MonsterGroupIndex
            };
            ShowBattleWindow(nextEvent, false, combatBackgroundIndex);
        }

        internal uint GetCombatBackgroundIndex(Map map, uint x, uint y) => is3D ? renderMap3D.CombatBackgroundIndex : renderMap2D.GetCombatBackgroundIndex(map, x, y);

        void AddExperience(List<PartyMember> partyMembers, uint amount, Action finishedEvent = null)
        {
            void Add(int index)
            {
                if (index == partyMembers.Count)
                {
                    finishedEvent?.Invoke();
                    return;
                }

                AddExperience(partyMembers[index], amount, () => Add(index + 1));
            }

            Add(0);
        }

        void AddExperience(PartyMember partyMember, uint amount, Action finishedEvent)
        {
            if (partyMember.AddExperiencePoints(amount, RandomInt))
            {
                // Level-up
                ShowLevelUpWindow(partyMember, finishedEvent);
            }
            else
            {
                finishedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Starts playing the map's music.
        /// </summary>
        void PlayMapMusic() => PlayMusic(Song.Default);

        /// <summary>
        /// Starts playing a specific music. If Song.Default is given
        /// the current map music is played instead.
        /// 
        /// Returns the previously played song.
        /// </summary>
        internal Song PlayMusic(Song song)
        {
            if (song == Song.Default || (int)song == 255)
            {
                return PlayMusic(Map.MusicIndex == 0 || Map.MusicIndex == 255 ? Song.PloddingAlong : (Song)Map.MusicIndex);
            }

            var newSong = songManager.GetSong(song);
            var oldSong = currentSong?.Song ?? Song.Default;

            if (currentSong != newSong)
            {
                currentSong?.Stop();
                currentSong = newSong;
                ContinueMusic();
            }

            return oldSong;
        }

        internal void ContinueMusic()
        {
            if (Configuration.Music)
                currentSong?.Play(AudioOutput);
        }

        void ShowLevelUpWindow(PartyMember partyMember, Action finishedEvent)
        {
            bool inputWasEnabled = InputEnable;
            InputEnable = false;
            var previousSong = PlayMusic(Song.StairwayToLevel50);
            var popup = layout.OpenPopup(new Position(16, 62), 18, 6);
            bool magicClass = partyMember.Class.IsMagic();

            void AddValueText<T>(int y, string text, T value, T? maxValue = null, string unit = "") where T : struct
            {
                popup.AddText(new Position(32, y), text, TextColor.BrightGray);
                popup.AddText(new Position(212, y), maxValue == null ? $"{value}{unit}" : $"{value}/{maxValue}{unit}", TextColor.BrightGray);
            }

            popup.AddText(new Rect(32, 78, 256, Global.GlyphLineHeight), partyMember.Name + string.Format(DataNameProvider.HasReachedLevel, partyMember.Level),
                TextColor.BrightGray, TextAlign.Center);

            AddValueText(92, DataNameProvider.LPAreNow, partyMember.HitPoints.CurrentValue, partyMember.HitPoints.MaxValue);
            if (magicClass)
            {
                AddValueText(99, DataNameProvider.SPAreNow, partyMember.SpellPoints.CurrentValue, partyMember.SpellPoints.MaxValue);
                AddValueText(106, DataNameProvider.SLPAreNow, partyMember.SpellLearningPoints);
            }
            AddValueText(113, DataNameProvider.TPAreNow, partyMember.TrainingPoints);
            AddValueText(120, DataNameProvider.APRAreNow, partyMember.AttacksPerRound);

            if (partyMember.Level >= 50)
                popup.AddText(new Position(32, 134), DataNameProvider.MaxLevelReached, TextColor.BrightGray);
            else
                AddValueText(134, DataNameProvider.NextLevelAt, partyMember.GetNextLevelExperiencePoints(), null, " " + DataNameProvider.EP);

            popup.Closed += () =>
            {
                InputEnable = inputWasEnabled;
                PlayMusic(previousSong);
                finishedEvent?.Invoke();
            };
        }

        internal void ShowBattleLoot(BattleEndInfo battleEndInfo, Action closeAction)
        {
            var gold = battleEndInfo.KilledMonsters.Sum(m => m.Gold);
            var food = battleEndInfo.KilledMonsters.Sum(m => m.Food);
            var loot = new Chest
            {
                Type = ChestType.Pile,
                Gold = (uint)gold,
                Food = (uint)food,
                AllowsItemDrop = false,
                IsBattleLoot = true
            };
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 6; ++c)
                {
                    loot.Slots[c, r] = new ItemSlot
                    {
                        ItemIndex = 0,
                        Amount = 0
                    };
                }
            }
            var slots = loot.Slots.ToList();
            foreach (var item in battleEndInfo.KilledMonsters
                .SelectMany(m => Enumerable.Concat(m.Inventory.Slots, m.Equipment.Slots.Values)
                    .Where(slot => slot != null && !slot.Empty)))
            {
                bool stackable = ItemManager.GetItem(item.ItemIndex).Flags.HasFlag(ItemFlags.Stackable);

                while (item.Amount > 0)
                {
                    ItemSlot slot = null;

                    if (stackable)
                        slot = slots.FirstOrDefault(s => s.ItemIndex == item.ItemIndex && s.Amount < 99);

                    if (slot == null)
                        slot = slots.FirstOrDefault(s => s.Empty);

                    if (slot == null) // doesn't fit
                        break;

                    slot.Add(item);
                }
            }
            foreach (var brokenItem in battleEndInfo.BrokenItems)
            {
                var slot = slots.FirstOrDefault(s => s.Empty);

                if (slot == null) // doesn't fit
                    break;

                slot.ItemIndex = brokenItem.Key;
                slot.Amount = 1;
                slot.Flags = brokenItem.Value | ItemSlotFlags.Broken;
            }
            var expReceivingPartyMembers = PartyMembers.Where(m => m.Alive && !battleEndInfo.FledPartyMembers.Contains(m)).ToList();
            int expPerPartyMember = battleEndInfo.TotalExperience / expReceivingPartyMembers.Count;

            if (loot.Empty)
            {
                CloseWindow(() =>
                {
                    ShowMessagePopup(string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
                    {
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, closeAction);
                    });
                });
            }
            else
            {
                Fade(() =>
                {
                    InputEnable = true;
                    SetWindow(Window.BattleLoot, loot, closeAction);
                    LastWindow = DefaultWindow;
                    ShowBattleLoot(loot, expReceivingPartyMembers, expPerPartyMember, false);
                });
            }
        }

        void ShowBattleLoot(ITreasureStorage storage, List<PartyMember> expReceivingPartyMembers,
            int expPerPartyMember, bool fade = true)
        {
            void Show()
            {
                InputEnable = true;
                layout.Reset();
                ShowLoot(storage, expReceivingPartyMembers == null ? null : string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
                {
                    if (expReceivingPartyMembers != null)
                    {
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, () =>
                        {
                            layout.ShowChestMessage(DataNameProvider.LootAfterBattle, TextAlign.Left);
                        });
                    }
                });
            }

            if (fade)
                Fade(Show);
            else
                Show();
        }

        internal struct AutomapOptions
        {
            public bool SecretDoorsVisible;
            public bool MonstersVisible;
            public bool PersonsVisible;
            public bool TrapsVisible;
        }

        // Elf harp
        void OpenMusicList()
        {
            bool wasPaused = paused;
            Pause();
            const int columns = 15;
            const int rows = 10;
            var popupArea = new Rect(16, 36, columns * 16, rows * 16);
            TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
            var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
            var songList = popup.AddSongListBox(Enumerable.Range(0, 32).Select(index => new KeyValuePair<string, Action<int, string>>
            (
                DataNameProvider.GetSongName((Song)(index + 1)), PlaySong
            )).ToList());
            void PlaySong(int index, string name) => PlayMusic((Song)(index + 1));
            var exitButton = popup.AddButton(new Position(190, 166));
            exitButton.ButtonType = ButtonType.Exit;
            exitButton.Disabled = false;
            exitButton.LeftClickAction = ClosePopup;
            exitButton.Visible = true;
            popup.Closed += () =>
            {
                UntrapMouse();
                if (!wasPaused)
                    Resume();
            };
            int scrollRange = Math.Max(0, 16); // = 32 songs - 16 songs visible
            var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
            scrollbar.Scrolled += offset =>
            {
                songList.ScrollTo(offset);
            };
        }

        void OpenMiniMap()
        {
            CloseWindow(() =>
            {
                Pause();
                var popup = layout.OpenPopup(Map2DViewArea.Position, 11, 9, true, false);
                var contentArea = popup.ContentArea;
                CursorType = CursorType.Sword;
                TrapMouse(contentArea);
                const int numVisibleTilesX = 72; // (11 - 2) * 16 / 2
                const int numVisibleTilesY = 56; // (9 - 2) * 16 / 2
                int displayWidth = Map.IsWorldMap ? numVisibleTilesX : Math.Min(numVisibleTilesX, Map.Width);
                int displayHeight = Map.IsWorldMap ? numVisibleTilesY : Math.Min(numVisibleTilesY, Map.Height);
                var baseX = popup.ContentArea.Position.X + (numVisibleTilesX - displayWidth); // 1 tile = 2 pixel, half of it is 1, it's actually * 1 here
                var baseY = popup.ContentArea.Position.Y + (numVisibleTilesY - displayHeight); // 1 tile = 2 pixel, half of it is 1, it's actually * 1 here
                var backgroundFill = layout.FillArea(popup.ContentArea, Render.Color.Black, 90);
                var filledAreas = new List<FilledArea>();
                int drawX = baseX;
                int drawY = baseY;

                var rightMap = Map.IsWorldMap ? MapManager.GetMap(Map.RightMapIndex.Value) : null;
                var downMap = Map.IsWorldMap ? MapManager.GetMap(Map.DownMapIndex.Value) : null;
                var downRightMap = Map.IsWorldMap ? MapManager.GetMap(Map.DownRightMapIndex.Value) : null;
                Func<Map, int, int, KeyValuePair<byte, byte?>> tileColorProvider = null;

                if (is3D)
                {
                    var labdata = MapManager.GetLabdataForMap(Map);
                    tileColorProvider = (map, x, y) =>
                    {
                        // Note: In original this seems bugged. The map border is drawn in different colors depending on savegame and who knows what.
                        // We just skip map border drawing at all by using color index 0 if there is no wall.
                        if (map.Blocks[x, y].WallIndex == 0)
                            return KeyValuePair.Create((byte)0, (byte?)null);
                        else
                            return KeyValuePair.Create(labdata.Walls[(int)map.Blocks[x, y].WallIndex - 1].ColorIndex, (byte?)null);
                    };
                }
                else // 2D
                {
                    // Possible adjacent maps should use the same tileset so don't bother to provide 4 tilesets here.
                    var tileset = MapManager.GetTilesetForMap(Map);
                    tileColorProvider = (map, x, y) =>
                    {
                        var backTileIndex = map.Tiles[x, y].BackTileIndex;
                        var frontTileIndex = map.Tiles[x, y].FrontTileIndex;
                        byte backColorIndex = tileset.Tiles[backTileIndex - 1].ColorIndex;
                        byte? frontColorIndex = frontTileIndex == 0 ? (byte?)null : tileset.Tiles[frontTileIndex - 1].ColorIndex;
                        return KeyValuePair.Create(backColorIndex, frontColorIndex);
                    };
                }
                void DrawTile(Map map, int x, int y)
                {
                    bool visible = popup.ContentArea.Contains(drawX + 1, drawY + 1);
                    var tileColors = tileColorProvider(map, x, y);
                    var backArea = layout.FillArea(new Rect(drawX, drawY, 2, 2),
                        GetPaletteColor((int)map.PaletteIndex, renderView.GraphicProvider.PaletteIndexFromColorIndex(map, tileColors.Key)), 100);
                    filledAreas.Add(backArea);
                    backArea.Visible = visible;
                    if (tileColors.Value != null)
                    {
                        var color = GetPaletteColor((int)map.PaletteIndex, renderView.GraphicProvider.PaletteIndexFromColorIndex(map, tileColors.Value.Value));
                        var upperRightArea = layout.FillArea(new Rect(drawX + 1, drawY, 1, 1), color, 110);
                        var lowerLeftArea = layout.FillArea(new Rect(drawX, drawY + 1, 1, 1), color, 110);
                        filledAreas.Add(upperRightArea);
                        filledAreas.Add(lowerLeftArea);
                        upperRightArea.Visible = visible;
                        lowerLeftArea.Visible = visible;
                    }
                }
                for (int y = 0; y < Map.Height; ++y)
                {
                    drawX = baseX;

                    for (int x = 0; x < Map.Width; ++x)
                    {
                        DrawTile(Map, x, y);
                        drawX += 2;
                    }

                    if (rightMap != null)
                    {
                        for (int x = 0; x < rightMap.Width; ++x)
                        {
                            DrawTile(rightMap, x, y);
                            drawX += 2;
                        }
                    }

                    drawY += 2;
                }
                if (downMap != null)
                {
                    for (int y = 0; y < downMap.Height; ++y)
                    {
                        drawX = baseX;

                        for (int x = 0; x < downMap.Width; ++x)
                        {
                            DrawTile(downMap, x, y);
                            drawX += 2;
                        }

                        if (downRightMap != null)
                        {
                            for (int x = 0; x < downRightMap.Width; ++x)
                            {
                                DrawTile(downRightMap, x, y);
                                drawX += 2;
                            }
                        }

                        drawY += 2;
                    }
                }
                bool closed = false;
                // 16x10 pixels per frame, stored as one image of 16x40 pixels
                // The real position inside each frame has an offset of 7,4
                var positionMarkerGraphicIndex = Graphics.GetUIGraphicIndex(UIGraphic.PlusBlinkAnimation);
                var positionMarker = popup.AddImage(new Rect(baseX + player.Position.X * 2 - 7, baseY + player.Position.Y * 2 - 4, 16, 10),
                    positionMarkerGraphicIndex, Layer.UI, 120, UIPaletteIndex);
                positionMarker.ClipArea = contentArea;
                var positionMarkerBaseTextureOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(positionMarkerGraphicIndex);
                int positionMarkerFrame = 0;
                void AnimatePosition()
                {
                    if (!closed)
                    {
                        positionMarker.TextureAtlasOffset = positionMarkerBaseTextureOffset + new Position(0, positionMarkerFrame * 10);
                        positionMarkerFrame = (positionMarkerFrame + 1) % 4; // 4 frames in total
                        AddTimedEvent(TimeSpan.FromMilliseconds(75), AnimatePosition);
                    }
                }
                AnimatePosition();
                popup.Closed += () =>
                {
                    closed = true;
                    positionMarker.Delete();
                    backgroundFill.Destroy();
                    filledAreas.ForEach(area => area.Destroy());
                    UntrapMouse();
                    Resume();
                };
                nextClickHandler = buttons =>
                {
                    if (buttons == MouseButtons.Right)
                    {
                        ClosePopup();
                        return true;
                    }

                    return false;
                };
                if (Map.IsWorldMap)
                {
                    // Only world maps can be scrolled.
                    // We assume that every map has a size of 50x50.
                    // Each scrolling will scroll at least 4 tiles.
                    const int tilesPerScroll = 4;
                    const int maxScrollX = (100 - numVisibleTilesX) / tilesPerScroll; // 7
                    const int maxScrollY = (100 - numVisibleTilesY) / tilesPerScroll; // 11
                    int scrollOffsetX = 0; // in 4 pixel chunks
                    int scrollOffsetY = 0; // in 4 pixel chunks

                    void Scroll(int x, int y)
                    {
                        int newX = Util.Limit(0, scrollOffsetX + x, maxScrollX);
                        int newY = Util.Limit(0, scrollOffsetY + y, maxScrollY);

                        if (scrollOffsetX != newX || scrollOffsetY != newY)
                        {
                            int diffX = (newX - scrollOffsetX) * tilesPerScroll;
                            int diffY = (newY - scrollOffsetY) * tilesPerScroll;
                            scrollOffsetX = newX;
                            scrollOffsetY = newY;
                            var diff = new Position(diffX, diffY);

                            foreach (var area in filledAreas)
                            {
                                if (area?.Position != null)
                                {
                                    area.Position -= diff;
                                    area.Visible = contentArea.Contains(area.Position.X + 1, area.Position.Y + 1);
                                }
                            }

                            positionMarker.X -= diffX;
                            positionMarker.Y -= diffY;
                        }
                    }

                    void CheckScroll()
                    {
                        if (!closed)
                        {
                            AddTimedEvent(TimeSpan.FromMilliseconds(50), () =>
                            {
                                if (InputEnable)
                                {
                                    var position = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
                                    int x = position.X < contentArea.Left + 4 ? -1 : position.X > contentArea.Right - 4 ? 1 : 0;
                                    int y = position.Y < contentArea.Top + 4 ? -1 : position.Y > contentArea.Bottom - 4 ? 1 : 0;

                                    if (x != 0 || y != 0)
                                        Scroll(x, y);
                                }

                                CheckScroll();
                            });
                        }
                    }

                    CheckScroll();
                }
            });
        }

        internal void ShowAutomap()
        {
            if (!Map.Flags.HasFlag(MapFlags.Automapper))
            {
                ShowMessagePopup(DataNameProvider.AutomapperNotWorkingHere);
                return;
            }

            bool showAll = CurrentSavegame.IsSpellActive(ActiveSpellType.MysticMap);
            ShowAutomap(new AutomapOptions
            {
                SecretDoorsVisible = showAll,
                MonstersVisible = showAll,
                PersonsVisible = showAll,
                TrapsVisible = showAll
            });
        }

        struct AutomapWall
        {
            public int TileX;
            public int TileY;
            public int DrawX;
            public int DrawY;
            public bool? NormalWall; // true: normal, false: fake wall, null: wall with automap graphic on it
            public bool BlocksSight;
        }

        internal void ShowAutomap(AutomapOptions automapOptions)
        {
            CloseWindow(() =>
            {
                Fade(() =>
                {
                    // Note: Each tile is displayed as 8x8.
                    //       The automap type icons are 16x16 but the lower-left 8x8 area is placed on a tile.
                    //       The player pin is 16x32 at the lower-left 8x8 is placed on the tile.
                    //       Each horizontal map background tile is 16 pixels wide and can contain 2 map tiles/blocks.
                    //       Each vertical map background tile is 32 pixels height and can contain 4 map tiles/blocks.
                    //       Fill inner map area with AA7744 (index 6). Lines (like walls) are drawn with 663300 (index 7).
                    byte paletteIndex = (byte)(renderView.GraphicProvider.AutomapPaletteIndex - 1);
                    var backgroundColor = GetPaletteColor(renderView.GraphicProvider.AutomapPaletteIndex, 6);
                    var foregroundColor = GetPaletteColor(renderView.GraphicProvider.AutomapPaletteIndex, 7);
                    var labdata = MapManager.GetLabdataForMap(Map);
                    int legendPage = 0;
                    ILayerSprite[] legendSprites = new ILayerSprite[8];
                    UIText[] legendTexts = new UIText[8];
                    var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
                    int scrollOffsetX = 0; // in 16 pixel chunks
                    int scrollOffsetY = 0; // in 16 pixel chunks

                    InputEnable = true;
                    ShowMap(false);
                    SetWindow(Window.Automap);
                    layout.Reset();
                    layout.SetLayout(LayoutType.Automap);
                    CursorType = CursorType.Sword;

                    var sprites = new List<ISprite>();
                    var animatedSprites = new List<IAnimatedLayerSprite>();
                    // key = tile index, value = tileX, tileY, drawX, drawY, boolean -> true = normal blocking wall, false = fake wall, null = count as wall but has automap graphic on it
                    var walls = new Dictionary<int, AutomapWall>();
                    var gotoPoints = new List<KeyValuePair<Map.GotoPoint, Tooltip>>();
                    var automapIcons = new Dictionary<int, ISprite>();
                    bool animationsPaused = false;

                    #region Legend
                    layout.FillArea(new Rect(208, 37, Global.VirtualScreenWidth - 208, Global.VirtualScreenHeight - 37), Render.Color.Black, 9);
                    // Legend panels
                    var headerArea = new Rect(217, 46, 86, 8);
                    layout.AddPanel(headerArea, 11);
                    layout.AddText(headerArea.CreateModified(0, 1, 0, -1), DataNameProvider.LegendHeader, TextColor.White, TextAlign.Center, 15);
                    var legendArea = new Rect(217, 56, 86, 108);
                    layout.AddPanel(legendArea, 11);
                    for (int i = 0; i < 8; ++i)
                    {
                        legendSprites[i] = layout.AddSprite(new Rect(legendArea.X + 2, legendArea.Y + 4 + i * 13 + Global.GlyphLineHeight - 16, 16, 16),
                            0u, paletteIndex, (byte)(15 + i));
                        legendTexts[i] = layout.AddText(new Rect(legendArea.X + 18, legendArea.Y + 4 + i * 13, 68, Global.GlyphLineHeight), "",
                            TextColor.White, TextAlign.Left, 15);
                    }
                    void ShowLegendPage(int page)
                    {
                        legendPage = page;
                        AddTimedEvent(TimeSpan.FromSeconds(4), ToggleLegendPage);

                        void SetLegendEntry(int index, AutomapType? automapType)
                        {
                            if (automapType == null)
                            {
                                legendSprites[index].Visible = false;
                                legendTexts[index].Visible = false;
                            }
                            else
                            {
                                legendSprites[index].TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetAutomapGraphicIndex(automapType.Value.ToGraphic().Value));
                                legendTexts[index].SetText(renderView.TextProcessor.CreateText(DataNameProvider.GetAutomapName(automapType.Value)));
                                legendSprites[index].Visible = true;
                                legendTexts[index].Visible = true;
                            }
                        }

                        if (page == 0)
                        {
                            SetLegendEntry(0, AutomapType.Riddlemouth);
                            SetLegendEntry(1, AutomapType.Teleporter);
                            SetLegendEntry(2, AutomapType.Door);
                            SetLegendEntry(3, AutomapType.Chest);
                            if (automapOptions.TrapsVisible)
                            {
                                SetLegendEntry(4, AutomapType.Spinner);
                                SetLegendEntry(5, AutomapType.Merchant);
                                SetLegendEntry(6, AutomapType.Tavern);
                                SetLegendEntry(7, AutomapType.Special);
                            }
                            else
                            {
                                SetLegendEntry(4, AutomapType.Merchant);
                                SetLegendEntry(5, AutomapType.Tavern);
                                SetLegendEntry(6, AutomapType.Special);
                                SetLegendEntry(7, null);
                            }
                        }
                        else
                        {
                            SetLegendEntry(0, AutomapType.Exit);
                            SetLegendEntry(1, AutomapType.Pile);
                            int index = 2;
                            if (automapOptions.TrapsVisible)
                            {
                                SetLegendEntry(2, AutomapType.Trap);
                                SetLegendEntry(3, AutomapType.Trapdoor);
                                index = 4;
                            }
                            if (automapOptions.MonstersVisible)
                            {
                                SetLegendEntry(index++, AutomapType.Monster);
                            }
                            if (automapOptions.PersonsVisible)
                            {
                                SetLegendEntry(index++, AutomapType.Person);
                            }
                            SetLegendEntry(index++, AutomapType.GotoPoint);
                            while (index < 8)
                                SetLegendEntry(index++, null);
                        }
                    }
                    void ToggleLegendPage()
                    {
                        ShowLegendPage(1 - legendPage);
                    }
                    ShowLegendPage(0);
                    var locationArea = new Rect(217, 166, 86, 22);
                    layout.AddPanel(locationArea, 11);
                    if (CurrentSavegame.IsSpecialItemActive(SpecialItemPurpose.MapLocation))
                    {
                        layout.AddText(new Rect(locationArea.X + 2, locationArea.Y + 3, 70, Global.GlyphLineHeight), DataNameProvider.Location, TextColor.White, TextAlign.Left, 15);
                        layout.AddText(new Rect(locationArea.X + 2, locationArea.Y + 12, 70, Global.GlyphLineHeight), $"X:{player3D.Position.X + 1,-2} Y:{player3D.Position.Y + 1}", TextColor.White, TextAlign.Left, 15);
                    }
                    DrawPin(locationArea.Right - 16, locationArea.Bottom - 32, 16, 16, false);
                    #endregion

                    #region Map
                    var automap = CurrentSavegame.Automaps.TryGetValue(Map.Index, out var a) ? a : null;
                    void DrawPin(int x, int y, byte upperDisplayLayer, byte lowerDisplayLayer, bool onMap)
                    {
                        var pinHead = !CurrentSavegame.IsSpecialItemActive(SpecialItemPurpose.Compass)
                            ? AutomapGraphic.PinUpperHalf
                            : AutomapGraphic.PinDirectionUp + (int)player3D.PreciseDirection;
                        var upperSprite = layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetAutomapGraphicIndex(pinHead), paletteIndex, upperDisplayLayer);
                        var lowerSprite = layout.AddSprite(new Rect(x, y + 16, 16, 16), Graphics.GetAutomapGraphicIndex(AutomapGraphic.PinLowerHalf), paletteIndex, lowerDisplayLayer);

                        if (onMap)
                        {
                            upperSprite.ClipArea = Global.AutomapArea;
                            lowerSprite.ClipArea = Global.AutomapArea;
                            sprites.Add(upperSprite);
                            sprites.Add(lowerSprite);
                        }
                    }
                    var displayLayers = new Dictionary<int, byte>();
                    displayLayers[RenderPlayer.Position.X + RenderPlayer.Position.Y * Map.Width] = 100;
                    ILayerSprite AddGraphic(int x, int y, AutomapGraphic automapGraphic, int width, int height, byte displayLayer = 2)
                    {
                        ILayerSprite sprite;

                        switch (automapGraphic)
                        {
                            case AutomapGraphic.Riddlemouth:
                            case AutomapGraphic.Teleport:
                            case AutomapGraphic.Spinner:
                            case AutomapGraphic.Trap:
                            case AutomapGraphic.TrapDoor:
                            case AutomapGraphic.Special:
                            case AutomapGraphic.Monster: // this and all above have 4 frames
                            case AutomapGraphic.GotoPoint: // this has 7 frames
                            {
                                var animatedSprite = layout.AddAnimatedSprite(new Rect(x, y, width, height), Graphics.GetAutomapGraphicIndex(automapGraphic),
                                    paletteIndex, automapGraphic == AutomapGraphic.GotoPoint ? 7u : 4u, displayLayer);
                                animatedSprites.Add(animatedSprite);
                                sprite = animatedSprite;
                                break;
                            }
                            default:
                                sprite = layout.AddSprite(new Rect(x, y, width, height), Graphics.GetAutomapGraphicIndex(automapGraphic), paletteIndex, displayLayer);
                                break;
                        }

                        sprite.ClipArea = Global.AutomapArea;
                        sprites.Add(sprite);
                        return sprite;
                    }
                    void AddAutomapType(int tx, int ty, int x, int y, AutomapType automapType,
                        byte displayLayer = 5) // 5: above walls, fake wall overlays and player pin lower half (2, 3 and 4)
                    {
                        if (!automapOptions.TrapsVisible && (automapType == AutomapType.Trap ||
                            automapType == AutomapType.Trapdoor || automapType == AutomapType.Spinner))
                            return;

                        byte baseDisplayLayer = displayLayer;
                        var graphic = automapType.ToGraphic();

                        if (graphic != null)
                        {
                            if (tx > 0)
                            {
                                if (displayLayers.ContainsKey(tx - 1 + ty * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx - 1 + ty * Map.Width] + 1);
                                else if (ty > 0)
                                {
                                    if (tx < Map.Width - 1 && displayLayers.ContainsKey(tx + 1 + (ty - 1) * Map.Width))
                                        displayLayer = (byte)Math.Min(255, displayLayers[tx + 1 + (ty - 1) * Map.Width] + 1);
                                    else if (displayLayers.ContainsKey(tx + (ty - 1) * Map.Width))
                                        displayLayer = (byte)Math.Min(255, displayLayers[tx + (ty - 1) * Map.Width] + 1);
                                    else if (tx > 0 && displayLayers.ContainsKey(tx - 1 + (ty - 1) * Map.Width))
                                        displayLayer = (byte)Math.Min(255, displayLayers[tx - 1 + (ty - 1) * Map.Width] + 1);
                                }
                            }
                            else if (ty > 0)
                            {
                                if (tx < Map.Width - 1 && displayLayers.ContainsKey(tx + 1 + (ty - 1) * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx + 1 + (ty - 1) * Map.Width] + 1);
                                else if (displayLayers.ContainsKey(tx + (ty - 1) * Map.Width))
                                    displayLayer = (byte)Math.Min(255, displayLayers[tx + (ty - 1) * Map.Width] + 1);
                            }

                            int tileIndex = tx + ty * Map.Width;

                            if (automapIcons.ContainsKey(tileIndex))
                            {
                                // Already an automap icon there -> remove it
                                automapIcons[tileIndex]?.Delete();
                            }

                            automapIcons[tileIndex] = AddGraphic(x, y - 8, graphic.Value, 16, 16, displayLayer);
                            if (!displayLayers.ContainsKey(tileIndex) || displayLayers[tileIndex] < displayLayer)
                                displayLayers[tileIndex] = displayLayer;
                        }
                    }
                    void AddTile(int tx, int ty, int x, int y)
                    {
                        renderMap3D.CharacterTypeFromBlock((uint)tx, (uint)ty, out var automapType);

                        if (automapType == AutomapType.None)
                            automapType = renderMap3D.AutomapTypeFromBlock((uint)tx, (uint)ty);

                        if (automapType == AutomapType.Monster)
                        {
                            if (automapOptions.MonstersVisible)
                                AddAutomapType(tx, ty, x, y, AutomapType.Monster, 6);
                        }
                        else if (automapType == AutomapType.Person)
                        {
                            if (automapOptions.PersonsVisible)
                                AddAutomapType(tx, ty, x, y, AutomapType.Person, 6);
                        }

                        if (automap != null && !automap.IsBlockExplored(Map, (uint)tx, (uint)ty))
                            return;

                        // Note: Maps are always 3D
                        var block = Map.Blocks[tx, ty];

                        if (block.MapBorder)
                        {
                            // draw nothing
                            return;
                        }
                        var gotoPoint = Map.GotoPoints.FirstOrDefault(p => p.X == tx + 1 && p.Y == ty + 1); // positions of goto points are 1-based
                        if (gotoPoint != null && CurrentSavegame.IsGotoPointActive(gotoPoint.Index))
                        {
                            AddAutomapType(tx, ty, x, y, AutomapType.GotoPoint);
                            gotoPoints.Add(KeyValuePair.Create(gotoPoint,
                                layout.AddTooltip(new Rect(x, y, 8, 8), gotoPoint.Name, TextColor.White)));
                        }
                        if (automapType != AutomapType.None)
                            AddAutomapType(tx, ty, x, y, automapType);
                        if (block.WallIndex != 0)
                        {
                            var wall = labdata.Walls[(int)block.WallIndex - 1];
                            bool blockingWall = block.BlocksPlayer(labdata);

                            // Walls that don't block and use transparency are not considered walls
                            // nor fake walls. For example a destroyed cobweb uses this.
                            // Fake walls on the other hand won't block but are not transparent.
                            if (wall.AutomapType == AutomapType.Wall || blockingWall || !wall.Flags.HasFlag(Tileset.TileFlags.Transparency))
                            {
                                bool draw = automapType == AutomapType.None || wall.AutomapType == AutomapType.Wall ||
                                    automapType == AutomapType.Tavern || automapType == AutomapType.Merchant;

                                walls.Add(tx + ty * Map.Width, new AutomapWall
                                {
                                    TileX = tx,
                                    TileY = ty,
                                    DrawX = x,
                                    DrawY = y,
                                    NormalWall = draw ? blockingWall : (bool?)null,
                                    BlocksSight = wall.Flags.HasFlag(Tileset.TileFlags.BlockSight)
                                });
                            }
                        }
                    }

                    int x = Global.AutomapArea.X;
                    int y = Global.AutomapArea.Y;
                    int xParts = (Map.Width + 1) / 2;
                    int yParts = (Map.Height + 3) / 4;
                    var totalArea = new Rect(Global.AutomapArea.X, Global.AutomapArea.Y, 64 + xParts * 16, 64 + yParts * 32);
                    var mapNameBounds = new Rect(Global.AutomapArea.X, Global.AutomapArea.Y + 32, totalArea.Width, Global.GlyphLineHeight);
                    var mapName = layout.AddText(mapNameBounds, Map.Name, TextColor.White, TextAlign.Center, 3);

                    // Fill background black
                    layout.FillArea(Global.AutomapArea, Render.Color.Black, 0);

                    #region Upper border
                    AddGraphic(x, y, AutomapGraphic.MapUpperLeft, 32, 32);
                    x += 32;
                    for (int tx = 0; tx < xParts; ++tx)
                    {
                        AddGraphic(x, y, AutomapGraphic.MapBorderTop1 + tx % 4, 16, 32);
                        x += 16;
                    }
                    AddGraphic(x, y, AutomapGraphic.MapUpperRight, 32, 32);
                    x = Global.AutomapArea.X;
                    y += 32;
                    #endregion

                    #region Map content
                    FilledArea mapFill = null;
                    void FillMap()
                    {
                        mapFill?.Destroy();
                        var fillArea = new Rect(Global.AutomapArea.X + 32 - scrollOffsetX * 16, Global.AutomapArea.Y + 32 - scrollOffsetY * 16, xParts * 16, yParts * 32);
                        var clipArea = new Rect(Global.AutomapArea);
                        int maxScrollX = (totalArea.Width - 208) / 16;
                        int maxScrollY = (totalArea.Height - 160) / 16;
                        if (scrollOffsetX >= maxScrollX - 1)
                            clipArea = clipArea.SetWidth(clipArea.Width - (2 - (maxScrollX - scrollOffsetX)) * 16);
                        if (scrollOffsetY >= maxScrollY - 1)
                            clipArea = clipArea.SetHeight(clipArea.Height - (2 - (maxScrollY - scrollOffsetY)) * 16);
                        fillArea.Clip(clipArea);
                        mapFill = layout.FillArea(fillArea, backgroundColor, 1);
                    }
                    FillMap();
                    for (int ty = 0; ty < Map.Height; ++ty)
                    {
                        if (ty % 4 == 0)
                        {
                            AddGraphic(Global.AutomapArea.X, y, AutomapGraphic.MapBorderLeft1 + (ty % 8) / 4, 32, 32);
                        }

                        x = Global.AutomapArea.X + 32;

                        for (int tx = 0; tx < Map.Width; ++tx)
                        {
                            AddTile(tx, ty, x, y);
                            x += 8;
                        }

                        if (ty % 4 == 0)
                        {
                            if (Map.Width % 2 != 0)
                                x += 8;
                            AddGraphic(x, y, AutomapGraphic.MapBorderRight1 + (ty % 8) / 4, 32, 32);
                        }

                        y += 8;
                    }
                    // Draw walls
                    foreach (var wall in walls)
                    {
                        int tx = wall.Value.TileX;
                        int ty = wall.Value.TileY;
                        int dx = wall.Value.DrawX;
                        int dy = wall.Value.DrawY;
                        bool? type = wall.Value.NormalWall;
                        bool blocksSight = wall.Value.BlocksSight;

                        if (type != null)
                        {
                            bool ContainsSameWall(int x, int y, out bool otherWall)
                            {
                                otherWall = false;

                                // Note: This is used to detect if walls should be
                                // merged visually. There are some special walls that
                                // have a different block sight state (e.g. the crystal
                                // wall in the temple of brotherhood).
                                // Those should be treated as "another" wall so we will
                                // return false here if the color indices do not match.
                                if (!walls.TryGetValue(x + y * Map.Width, out var wall))
                                    return false;

                                otherWall = blocksSight != wall.BlocksSight;

                                return !otherWall || (type == null) != (wall.NormalWall == null);
                            }

                            bool hasOtherWallLeft = false;
                            bool hasOtherWallUp = false;
                            bool hasOtherWallRight = false;
                            bool hasOtherWallDown = false;
                            bool hasWallLeft = tx > 0 && ContainsSameWall(tx - 1, ty, out hasOtherWallLeft);
                            bool hasWallUp = ty > 0 && ContainsSameWall(tx, ty - 1, out hasOtherWallUp);
                            bool hasWallRight = tx < Map.Width - 1 && ContainsSameWall(tx + 1, ty, out hasOtherWallRight);
                            bool hasWallDown = ty < Map.Height - 1 && ContainsSameWall(tx, ty + 1, out hasOtherWallDown);
                            int wallGraphicType = 15; // closed

                            if (hasWallLeft)
                            {
                                if (hasWallRight)
                                {
                                    if (hasWallUp)
                                    {
                                        if (hasWallDown)
                                        {
                                            // all directions open (+ crossing)
                                            wallGraphicType = 12;
                                        }
                                        else
                                        {
                                            // left, right and top open (T crossing)
                                            wallGraphicType = 8;
                                        }
                                    }
                                    else if (hasWallDown)
                                    {
                                        // left, right and bottom open (T crossing)
                                        wallGraphicType = 10;
                                    }
                                    else
                                    {
                                        // left and right open
                                        wallGraphicType = 14;
                                    }
                                }
                                else
                                {
                                    if (hasWallUp)
                                    {
                                        if (hasWallDown)
                                        {
                                            // left, top and bottom open (T crossing)
                                            wallGraphicType = 11;
                                        }
                                        else
                                        {
                                            // left and top open (corner)
                                            wallGraphicType = 7;
                                        }
                                    }
                                    else if (hasWallDown)
                                    {
                                        // left and bottom open (corner)
                                        wallGraphicType = 5;
                                    }
                                    else
                                    {
                                        // only left open
                                        wallGraphicType = 3;
                                    }
                                }
                            }
                            else if (hasWallRight)
                            {
                                if (hasWallUp)
                                {
                                    if (hasWallDown)
                                    {
                                        // right, top and bottom open (T crossing)
                                        wallGraphicType = 9;
                                    }
                                    else
                                    {
                                        // right and top open
                                        wallGraphicType = 6;
                                    }
                                }
                                else if (hasWallDown)
                                {
                                    // right and bottom open (corner)
                                    wallGraphicType = 4;
                                }
                                else
                                {
                                    // only right open
                                    wallGraphicType = 1;
                                }
                            }
                            else
                            {
                                if (hasWallUp)
                                {
                                    if (hasWallDown)
                                    {
                                        // top and bottom open
                                        wallGraphicType = 13;
                                    }
                                    else
                                    {
                                        // only top open
                                        wallGraphicType = 0;
                                    }
                                }
                                else if (hasWallDown)
                                {
                                    // only bottom open
                                    wallGraphicType = 2;
                                }
                                else
                                {
                                    if (hasOtherWallLeft || hasOtherWallRight)
                                    {
                                        // left and right open
                                        wallGraphicType = 14;
                                    }
                                    else if (hasOtherWallUp || hasOtherWallDown)
                                    {
                                        // top and bottom open
                                        wallGraphicType = 13;
                                    }
                                    else
                                    {
                                        // closed single wall
                                        wallGraphicType = 15;
                                    }
                                }
                            }

                            var sprite = layout.AddSprite(new Rect(dx, dy, 8, 8), Graphics.GetCustomUIGraphicIndex(UICustomGraphic.AutomapWallFrames), paletteIndex, 2);
                            sprite.TextureAtlasOffset = new Position(sprite.TextureAtlasOffset.X + wallGraphicType * 8, sprite.TextureAtlasOffset.Y);
                            sprite.ClipArea = Global.AutomapArea;
                            sprites.Add(sprite);

                            if (type == false && automapOptions.SecretDoorsVisible) // fake wall
                            {
                                sprite = layout.AddSprite(new Rect(dx, dy, 8, 8), Graphics.GetCustomUIGraphicIndex(UICustomGraphic.FakeWallOverlay), paletteIndex, 3);
                                sprite.ClipArea = Global.AutomapArea;
                                sprites.Add(sprite);
                            }
                        }
                    }
                    // Animate automap icons
                    void Animate()
                    {
                        if (CurrentWindow.Window == Window.Automap && !animationsPaused)
                        {
                            foreach (var animatedSprite in animatedSprites)
                                ++animatedSprite.CurrentFrame;

                            AddTimedEvent(TimeSpan.FromMilliseconds(100), Animate);
                        }
                    }
                    Animate();
                    // Draw player pin
                    DrawPin(Global.AutomapArea.X + 32 + RenderPlayer.Position.X * 8, Global.AutomapArea.Y + 32 + RenderPlayer.Position.Y * 8 - 24, 100, 100, true);
                    #endregion

                    #region Lower border
                    x = Global.AutomapArea.X;
                    while ((y - Global.AutomapArea.Y) % 32 != 0)
                        y += 8;
                    AddGraphic(x, y, AutomapGraphic.MapLowerLeft, 32, 32);
                    x += 32;
                    for (int tx = 0; tx < xParts; ++tx)
                    {
                        AddGraphic(x, y, AutomapGraphic.MapBorderBottom1 + tx % 4, 16, 32);
                        x += 16;
                    }
                    AddGraphic(x, y, AutomapGraphic.MapLowerRight, 32, 32);
                    #endregion

                    void Scroll(int x, int y)
                    {
                        // The automap screen is 208x163 but we use 208x160 so they are both dividable by 16.
                        // If scrolled to the left there is the 32 pixel wide border so you can see max 22 tiles (208 - 32) / 8 = 22.
                        // Scrolling right is possible unless the 32 pixel wide border on the right is fully visible.
                        // The total automap width is 64 + xParts * 16. So max scroll offset X in tiles is (64 + xParts * 16 - 208) / 16.
                        // We will always scroll by 2 tiles (16 pixel chunks) in both directions.

                        int maxScrollX = (totalArea.Width - 208) / 16;
                        int maxScrollY = (totalArea.Height - 160) / 16;
                        int newX = Util.Limit(0, scrollOffsetX + x, maxScrollX);
                        int newY = Util.Limit(0, scrollOffsetY + y, maxScrollY);

                        if (scrollOffsetX != newX || scrollOffsetY != newY)
                        {
                            int diffX = (newX - scrollOffsetX) * 16;
                            int diffY = (newY - scrollOffsetY) * 16;
                            scrollOffsetX = newX;
                            scrollOffsetY = newY;

                            mapName.SetBounds(mapNameBounds.CreateOffset(-newX * 16, -newY * 16));
                            mapName.Clip(Global.AutomapArea);
                            FillMap();

                            foreach (var sprite in sprites)
                            {
                                sprite.X -= diffX;
                                sprite.Y -= diffY;
                            }

                            foreach (var gotoPoint in gotoPoints)
                            {
                                gotoPoint.Value.Area.Position.X -= diffX;
                                gotoPoint.Value.Area.Position.Y -= diffY;
                            }

                            // Update active tooltips
                            CursorType cursorType = CursorType.None;
                            layout.Hover(GetMousePosition(lastMousePosition), ref cursorType);
                        }
                    }

                    void SetupClickHandlers()
                    {
                        nextClickHandler = buttons =>
                        {
                            if (buttons == MouseButtons.Right)
                            {
                                Exit();
                                return true;
                            }
                            else if (buttons == MouseButtons.Left && gotoPoints.Count != 0)
                            {
                                var mousePosition = renderView.ScreenToGame(GetMousePosition(lastMousePosition));

                                foreach (var gotoPoint in gotoPoints)
                                {
                                    if (gotoPoint.Value.Area.Contains(mousePosition))
                                    {
                                        void AbortGoto()
                                        {
                                            animationsPaused = false;
                                            Animate();
                                            TrapMouse(Global.AutomapArea);
                                            SetupClickHandlers();
                                        }

                                        layout.HideTooltip();
                                        UntrapMouse();
                                        animationsPaused = true;
                                        if (MonsterSeesPlayer)
                                        {
                                            ShowMessagePopup(DataNameProvider.WayBackTooDangerous, AbortGoto, TextAlign.Left, 202);
                                        }
                                        else
                                        {
                                            ShowDecisionPopup(DataNameProvider.ReallyWantToGoThere, response =>
                                            {
                                                if (response == PopupTextEvent.Response.Yes)
                                                {
                                                    if (player3D.Position.X + 1 == gotoPoint.Key.X && player3D.Position.Y + 1 == gotoPoint.Key.Y)
                                                    {
                                                        ShowMessagePopup(DataNameProvider.AlreadyAtGotoPoint, AbortGoto, TextAlign.Center, 202);
                                                    }
                                                    else
                                                    {
                                                        Exit(() => Teleport(Map.Index, gotoPoint.Key.X, gotoPoint.Key.Y, gotoPoint.Key.Direction, out _, true));
                                                    }
                                                }
                                                else
                                                {
                                                    AbortGoto();
                                                }
                                            }, 1, 202, TextAlign.Center);
                                        }
                                        return true;
                                    }
                                }
                            }

                            return false;
                        };
                    }
                    SetupClickHandlers();

                    #endregion

                    bool closed = false;

                    void Exit(Action followAction = null)
                    {
                        closed = true;
                        UntrapMouse();
                        CloseWindow(followAction);
                    }

                    void CheckScroll()
                    {
                        if (!closed)
                        {
                            AddTimedEvent(TimeSpan.FromMilliseconds(100), () =>
                            {
                                if (InputEnable)
                                {
                                    var position = renderView.ScreenToGame(GetMousePosition(lastMousePosition));
                                    int x = position.X < 4 ? -1 : position.X > 204 ? 1 : 0;
                                    int y = position.Y < 41 ? -1 : position.Y > 196 ? 1 : 0;

                                    if (x != 0 || y != 0)
                                        Scroll(x, y);
                                    else
                                    {
                                        bool left = keys[(int)Key.Left] || keys[(int)Key.A] || keys[(int)Key.Home];
                                        bool right = keys[(int)Key.Right] || keys[(int)Key.D] || keys[(int)Key.End];
                                        bool up = keys[(int)Key.Up] || keys[(int)Key.W] || keys[(int)Key.PageUp];
                                        bool down = keys[(int)Key.Down] || keys[(int)Key.S] || keys[(int)Key.PageDown];

                                        if (left && !right)
                                            x = -1;
                                        else if (right && !left)
                                            x = 1;
                                        if (up && !down)
                                            y = -1;
                                        else if (down && !up)
                                            y = 1;

                                        if (x != 0 || y != 0)
                                            Scroll(x, y);
                                    }
                                }

                                CheckScroll();
                            });
                        }
                    }

                    CheckScroll();

                    // Initial scroll
                    int startScrollX = Math.Max(0, (player.Position.X - 6) / 2);
                    int startScrollY = Math.Max(0, (player.Position.Y - 8) / 2);
                    Scroll(startScrollX, startScrollY);

                    lastMousePosition = renderView.GameToScreen(Global.AutomapArea.Center);
                    TrapMouse(Global.AutomapArea);
                    UpdateCursor();
                });
            });
        }

        internal void ShowRiddlemouth(Map map, RiddlemouthEvent riddlemouthEvent, Action solvedHandler, bool showRiddle = true)
        {
            Fade(() =>
            {
                SetWindow(Window.Riddlemouth, riddlemouthEvent, solvedHandler);
                layout.SetLayout(LayoutType.Riddlemouth);
                ShowMap(false);
                layout.Reset();
                var riddleArea = new Rect(16, 50, 176, 144);
                layout.FillArea(riddleArea, GetUIColor(28), false);
                var riddleText = ProcessText(map.Texts[(int)riddlemouthEvent.RiddleTextIndex]);
                var solutionResponseText = ProcessText(map.Texts[(int)riddlemouthEvent.SolutionTextIndex]);
                void ShowRiddle()
                {
                    InputEnable = false;
                    HeadSpeak();
                    layout.OpenTextPopup(riddleText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                    {
                        InputEnable = true;
                    };
                }
                void TestSolution(string solution)
                {
                    if (string.Compare(textDictionary.Entries[(int)riddlemouthEvent.CorrectAnswerDictionaryIndex1], solution, true) == 0 ||
                        (riddlemouthEvent.CorrectAnswerDictionaryIndex1 != riddlemouthEvent.CorrectAnswerDictionaryIndex2 &&
                            string.Compare(textDictionary.Entries[(int)riddlemouthEvent.CorrectAnswerDictionaryIndex2], solution, true) == 0))
                    {
                        InputEnable = false;
                        HeadSpeak();
                        layout.OpenTextPopup(solutionResponseText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White, () =>
                        {
                            Exit(() =>
                            {
                                InputEnable = true;
                                solvedHandler?.Invoke();
                            });
                        });
                    }
                    else
                    {
                        if (!textDictionary.Entries.Any(entry => string.Compare(entry, solution, true) == 0))
                            solution = DataNameProvider.That;
                        var failedText = ProcessText(solution + DataNameProvider.WrongRiddlemouthSolutionText);
                        InputEnable = false;
                        HeadSpeak();
                        layout.OpenTextPopup(failedText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                        {
                            InputEnable = true;
                        };
                    }
                }

                // Show stone head
                layout.Set80x80Picture(Picture80x80.Riddlemouth, 224, 49);
                var eyes = layout.AddAnimatedSprite(new Rect(240, 72, 48, 9), Graphics.RiddlemouthEyeIndex, UIPaletteIndex, 4);
                var mouth = layout.AddAnimatedSprite(new Rect(240, 90, 48, 15), Graphics.RiddlemouthMouthIndex, UIPaletteIndex, 7);

                if (showRiddle)
                {
                    // Open eyes on start (and show the riddle)
                    AddTimedEvent(TimeSpan.FromMilliseconds(250), () => HeadChangeEyes(true, () =>
                    {
                        ShowRiddle();
                    }));
                }
                else
                {
                    // Eyes already open
                    eyes.CurrentFrame = 3;
                }

                layout.AttachEventToButton(6, () => OpenDictionary(TestSolution));
                layout.AttachEventToButton(8, ShowRiddle);
                layout.AttachEventToButton(2, () => Exit(null));

                void Exit(Action followAction)
                {
                    HeadChangeEyes(false, () => CloseWindow(followAction));
                }

                void HeadChangeEyes(bool open, Action followAction = null)
                {
                    void NextFrame()
                    {
                        void Next() => AddTimedEvent(TimeSpan.FromMilliseconds(150), NextFrame);

                        if (open)
                        {
                            if (++eyes.CurrentFrame == 3)
                                followAction?.Invoke();
                            else
                                Next();

                        }
                        else // close
                        {
                            if (--eyes.CurrentFrame == 0)
                                followAction?.Invoke();
                            else
                                Next();
                        }
                    }

                    NextFrame();
                }

                void HeadSpeak()
                {
                    void NextFrame()
                    {
                        void Next() => AddTimedEvent(TimeSpan.FromMilliseconds(150), NextFrame);

                        ++mouth.CurrentFrame;

                        // Note: The property will reset the frame to 0 when animation is done.
                        // But don't use an inline increment operator inside the if. This won't work!
                        if (mouth.CurrentFrame != 0)
                            Next();
                    }

                    mouth.CurrentFrame = 0;
                    NextFrame();
                }
            });
        }

        internal uint GetPlayerPaletteIndex() => Math.Max(1, Map.PaletteIndex) - 1;

        internal Position GetPlayerDrawOffset()
        {
            if (Map.IsWorldMap)
            {
                var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, player.Direction);
                return new Position((int)travelInfo.OffsetX - 16, (int)travelInfo.OffsetY - 16);
            }
            else
            {
                return new Position();
            }
        }

        internal Character2DAnimationInfo GetPlayerAnimationInfo()
        {
            if (Map.IsWorldMap)
            {
                var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, player.Direction);
                return new Character2DAnimationInfo
                {
                    FrameWidth = (int)travelInfo.Width,
                    FrameHeight = (int)travelInfo.Height,
                    StandFrameIndex = 3 * 17 + (uint)TravelType * 4,
                    SitFrameIndex = 0,
                    SleepFrameIndex = 0,
                    NumStandFrames = 1,
                    NumSitFrames = 0,
                    NumSleepFrames = 0,
                    TicksPerFrame = 0,
                    NoDirections = false,
                    IgnoreTileType = false,
                    UseTopSprite = false
                };
            }
            else
            {
                var animationInfo = renderView.GameData.PlayerAnimationInfo;
                uint offset = (uint)Map.World * 17;
                animationInfo.StandFrameIndex += offset;
                animationInfo.SitFrameIndex += offset;
                animationInfo.SleepFrameIndex += offset;
                return animationInfo;
            }
        }

        internal void OpenDictionary(Action<string> choiceHandler, Func<string, TextColor> colorProvider = null)
        {
            void WordEntered(string word)
            {
                // Add to known words if the entered word is a valid dictionary word.
                int index = textDictionary.Entries.FindIndex(entry => string.Compare(entry, word, true) == 0);

                if (index != -1)
                    CurrentSavegame.AddDictionaryWord((uint)index);

                choiceHandler?.Invoke(word);
            }

            const int columns = 11;
            const int rows = 10;
            var popupArea = new Rect(32, 34, columns * 16, rows * 16);
            TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
            var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
            var mouthButton = popup.AddButton(new Position(popupArea.Left + 16, popupArea.Bottom - 30));
            var exitButton = popup.AddButton(new Position(popupArea.Right - 32 - Button.Width, popupArea.Bottom - 30));
            mouthButton.ButtonType = ButtonType.Mouth;
            exitButton.ButtonType = ButtonType.Exit;
            mouthButton.DisplayLayer = 200;
            exitButton.DisplayLayer = 200;
            mouthButton.LeftClickAction = () =>
                layout.OpenInputPopup(new Position(51, 87), 20, WordEntered);
            exitButton.LeftClickAction = () => layout.ClosePopup();
            var dictionaryList = popup.AddDictionaryListBox(Dictionary.Select(entry => new KeyValuePair<string, Action<int, string>>
            (
                entry, (int _, string text) =>
                {
                    layout.ClosePopup(false);
                    choiceHandler?.Invoke(text);
                }
            )).ToList(), colorProvider);
            int scrollRange = Math.Max(0, Dictionary.Count - 16);
            var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
            scrollbar.Scrolled += offset =>
            {
                dictionaryList.ScrollTo(offset);
            };
            popup.Closed += UntrapMouse;
        }

        /// <summary>
        /// Opens the list of spells.
        /// </summary>
        /// <param name="partyMember">Party member who want to use a spell.</param>
        /// <param name="spellAvailableChecker">Returns null if the spell can be used, otherwise the error message.</param>
        /// <param name="choiceHandler">Handler which receives the selected spell.</param>
        internal void OpenSpellList(PartyMember partyMember, Func<Spell, string> spellAvailableChecker, Action<Spell> choiceHandler)
        {
            Pause();
            const int columns = 13;
            const int rows = 10;
            var popupArea = new Rect(32, 40, columns * 16, rows * 16);
            TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
            var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
            var spells = partyMember.LearnedSpells.Select(spell => new KeyValuePair<Spell, string>(spell, spellAvailableChecker(spell))).ToList();
            string GetSpellEntry(Spell spell, bool available)
            {
                var spellInfo = SpellInfos.Entries[spell];
                string entry = DataNameProvider.GetSpellName(spell);

                if (available)
                {
                    // append usage amount
                    entry = entry.PadRight(21) + $"({Math.Min(99, partyMember.SpellPoints.CurrentValue / spellInfo.SP)})";
                }

                return entry;
            }
            var spellList = popup.AddSpellListBox(spells.Select(spell => new KeyValuePair<string, Action<int, string>>
            (
                GetSpellEntry(spell.Key, spell.Value == null), spell.Value != null ? null : (Action<int, string>)((int index, string _) =>
                {
                    UntrapMouse();
                    layout.ClosePopup(false);
                    Resume();
                    choiceHandler?.Invoke(spells[index].Key);
                })
            )).ToList());
            popup.AddSunkenBox(new Rect(48, 173, 174, 10));
            var spellMessage = popup.AddText(new Rect(49, 175, 172, 6), "", TextColor.Bright, TextAlign.Center, true, 2);
            popup.Closed += () =>
            {
                UntrapMouse();
                Resume();
            };
            spellList.HoverItem += index =>
            {
                var message = index == -1 ? null : spells[index].Value;

                if (message == null)
                    spellMessage.SetText(renderView.TextProcessor.CreateText(""));
                else
                    spellMessage.SetText(ProcessText(message));
            };
            int scrollRange = Math.Max(0, spells.Count - 16);
            var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
            int slot = SlotFromPartyMember(partyMember).Value;
            scrollbar.Scrolled += offset =>
            {
                spellListScrollOffsets[slot] = offset;
                spellList.ScrollTo(offset);
            };
            // Initial scroll
            if (spellListScrollOffsets[slot] > scrollRange)
                spellListScrollOffsets[slot] = scrollRange;
            scrollbar.SetScrollPosition(spellListScrollOffsets[slot], true);
        }

        internal void ShowMessagePopup(string text, Action closeAction = null,
            TextAlign textAlign = TextAlign.Center, byte displayLayerOffset = 0)
        {
            Pause();
            InputEnable = false;
            // Simple text popup
            var popup = layout.OpenTextPopup(ProcessText(text), () =>
            {
                InputEnable = true;
                Resume();
                ResetCursor();
                closeAction?.Invoke();
            }, true, true, false, textAlign, displayLayerOffset);
            CursorType = CursorType.Click;
            TrapMouse(popup.ContentArea);
        }

        internal void ShowTextPopup(IText text, Action<PopupTextEvent.Response> responseHandler, byte displayLayer = 0)
        {
            Pause();
            InputEnable = false;
            // Simple text popup
            layout.OpenTextPopup(text, () =>
            {
                InputEnable = true;
                Resume();
                ResetCursor();
                responseHandler?.Invoke(PopupTextEvent.Response.Close);
            }, true, true);
            CursorType = CursorType.Click;
        }

        void ShowEvent(IText text, uint imageIndex, Action closeAction,
            bool gameOver = false)
        {
            GameOverButtonsVisible = false;

            Fade(() =>
            {
                SetWindow(Window.Event);
                layout.SetLayout(LayoutType.Event);
                ShowMap(false);
                layout.Reset();
                layout.AddEventPicture(imageIndex, out currentUIPaletteIndex);
                layout.UpdateUIPalette(currentUIPaletteIndex);
                cursor.UpdatePalette(this);
                layout.FillArea(new Rect(16, 138, 288, 55), GetUIColor(28), false);

                // Position = 18,139, max 40 chars per line and 7 lines.
                var textArea = new Rect(18, 139, 285, 49);
                var scrollableText = layout.AddScrollableText(textArea, text, TextColor.BrightGray);
                scrollableText.Clicked += scrolledToEnd =>
                {
                    if (scrolledToEnd)
                    {
                        if (gameOver)
                        {
                            scrollableText?.Destroy();
                            scrollableText = null;
                            AddLoadQuitOptions();
                        }
                        else
                        {
                            // Special case, we show a small game introduction
                            // when playing for the first time and closing the
                            // initial event with grandfather.
                            if (Configuration.FirstStart && imageIndex == 1)
                            {
                                // This avoids asking for introduction twice in the same sessions.
                                Configuration.FirstStart = false;
                                CloseWindow(() =>
                                {
                                    closeAction?.Invoke();
                                    ShowTutorial();
                                });
                            }
                            else
                            {
                                CloseWindow(closeAction);
                            }
                        }
                    }
                };
                CursorType = CursorType.Click;
                InputEnable = false;
                void AddLoadQuitOptions()
                {
                    GameOverButtonsVisible = true;
                    InputEnable = true;
                    layout.AddText(textArea, ProcessText(DataNameProvider.GameOverLoadOrQuit), TextColor.BrightGray);
                    void ShowButtons()
                    {
                        ExecuteNextUpdateCycle(() => CursorType = CursorType.Sword);
                        layout.ShowGameOverButtons(load =>
                        {
                            if (load)
                            {
                                layout.OpenLoadMenu(CloseWindow, ShowButtons, true);
                            }
                            else
                            {
                                Quit(ShowButtons);
                            }
                        });
                    }
                    ShowButtons();
                }
            });
        }

        void ShowTutorial()
        {
            new Tutorial(this).Run(renderView);
        }

        internal void ShowTextPopup(Map map, PopupTextEvent popupTextEvent, Action<PopupTextEvent.Response> responseHandler)
        {
            var text = ProcessText(map.Texts[(int)popupTextEvent.TextIndex]);

            if (popupTextEvent.HasImage)
            {
                // Those always use a custom layout
                ShowEvent(text, popupTextEvent.EventImageIndex,
                    () => responseHandler?.Invoke(PopupTextEvent.Response.Close));
            }
            else
            {
                ShowTextPopup(text, responseHandler);
            }
        }

        internal void ShowDecisionPopup(string text, Action<PopupTextEvent.Response> responseHandler,
            int minLines = 3, byte displayLayerOffset = 0, TextAlign textAlign = TextAlign.Left,
            bool canAbort = true)
        {
            var popup = layout.OpenYesNoPopup
            (
                ProcessText(text),
                () =>
                {
                    layout.ClosePopup(false, true);
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.Yes);
                },
                () =>
                {
                    layout.ClosePopup(false, true);
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.No);
                },
                () =>
                {
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.Close);
                }, minLines, displayLayerOffset, textAlign
            );
            popup.CanAbort = canAbort;
            Pause();
            InputEnable = false;
            CursorType = CursorType.Sword;
        }

        internal void ShowDecisionPopup(Map map, DecisionEvent decisionEvent, Action<PopupTextEvent.Response> responseHandler)
        {
            ShowDecisionPopup(map.Texts[(int)decisionEvent.TextIndex], responseHandler);
        }

        void RecheckUsedBattleItem(int partyMemberSlot, int slotIndex, bool equipped)
        {
            if (currentBattle != null && roundPlayerBattleActions.ContainsKey(partyMemberSlot))
            {
                var action = roundPlayerBattleActions[partyMemberSlot];

                if (action.BattleAction == Battle.BattleActionType.CastSpell &&
                    Battle.IsCastFromItem(action.Parameter))
                {
                    if (Battle.GetCastItemSlot(action.Parameter) == slotIndex)
                    {
                        roundPlayerBattleActions.Remove(partyMemberSlot);
                        UpdateBattleStatus(partyMemberSlot);
                    }
                }
            }
        }

        void RecheckBattleEquipment(int partyMemberSlot, EquipmentSlot equipmentSlot, Item removedItem)
        {
            if (currentBattle != null)
            {
                if (removedItem != null && roundPlayerBattleActions.ContainsKey(partyMemberSlot))
                {
                    var action = roundPlayerBattleActions[partyMemberSlot];

                    if (action.BattleAction == Battle.BattleActionType.Attack)
                    {
                        bool removedWeapon = equipmentSlot == EquipmentSlot.RightHand ||
                            (equipmentSlot == EquipmentSlot.LeftHand && removedItem.Type == ItemType.Ammunition &&
                            CurrentInventory.Equipment.Slots[EquipmentSlot.RightHand]?.ItemIndex != null &&
                            ItemManager.GetItem(CurrentInventory.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex).UsedAmmunitionType == removedItem.AmmunitionType);

                        if (removedWeapon || !CheckAbilityToAttack(out _))
                        {
                            roundPlayerBattleActions.Remove(partyMemberSlot);
                        }
                    }
                }
            }
        }

        bool RecheckActivePartyMember(out bool gameOver)
        {
            gameOver = false;

            if (!CurrentPartyMember.Ailments.CanSelect() || currentBattle?.GetSlotFromCharacter(CurrentPartyMember) == -1)
            {
                layout.ClearBattleFieldSlotColors();

                if (!PartyMembers.Any(p => p.Ailments.CanSelect()))
                {
                    if (battleRoundActiveSprite != null)
                        battleRoundActiveSprite.Visible = false;
                    currentBattleInfo = null;
                    currentBattle = null;
                    CloseWindow(() =>
                    {
                        InputEnable = true;
                        GameOver();
                    });
                    gameOver = true;
                    return true;
                }

                Pause();
                // Simple text popup
                var popup = layout.OpenTextPopup(ProcessText(DataNameProvider.SelectNewLeaderMessage), () =>
                {
                    UntrapMouse();
                    if (currentBattle == null && !WindowActive)
                        Resume();
                    ResetCursor();
                }, true, false);
                popup.CanAbort = false;
                pickingNewLeader = true;
                CursorType = CursorType.Sword;
                TrapMouse(Global.PartyMemberPortraitArea);
                return false;
            }
            else
            {
                layout.UpdateCharacterNameColors(SlotFromPartyMember(CurrentPartyMember).Value);
                return true;
            }
        }

        internal bool HasPartyMemberFled(PartyMember partyMember)
        {
            return currentBattle?.HasPartyMemberFled(partyMember) == true;
        }

        internal void SetPlayerDirection(CharacterDirection direction)
        {
            if (direction == CharacterDirection.Random)
                direction = (CharacterDirection)RandomInt(0, 3);

            CurrentSavegame.CharacterDirection = direction;

            if (is3D)
                player3D.TurnTowards((int)direction * 90.0f);
            else
                player2D.SetDirection(direction, CurrentTicks);
        }

        internal void SetActivePartyMember(int index, bool updateBattlePosition = true)
        {
            var partyMember = GetPartyMember(index);

            bool TestConversationLanguage(WindowInfo windowInfo)
            {
                var conversationPartner = windowInfo.WindowParameters[0] as IConversationPartner;

                if (((conversationPartner as Character).SpokenLanguages & partyMember.SpokenLanguages) == 0)
                {
                    ShowMessagePopup(DataNameProvider.YouDontSpeakSameLanguage);
                    return false;
                }

                return true;
            }

            // This avoids switching to a player that doesn't speak the same language.
            if (CurrentWindow.Window == Window.Conversation && !TestConversationLanguage(CurrentWindow))
                return;
            if (LastWindow.Window == Window.Conversation && !TestConversationLanguage(LastWindow))
                return;
            if (BattleActive && !BattleRoundActive && currentPlayerBattleAction != PlayerBattleAction.PickPlayerAction)
                return;

            if (partyMember != null && (partyMember.Ailments.CanSelect() || currentWindow.Window == Window.Healer))
            {
                bool switched = CurrentPartyMember != partyMember;

                if (currentWindow.Window == Window.Healer)
                {
                    currentlyHealedMember = partyMember;
                    layout.SetCharacterHealSymbol(index);
                }
                else
                {
                    if (HasPartyMemberFled(partyMember))
                        return;

                    CurrentSavegame.ActivePartyMemberSlot = index;
                    currentPickingActionMember = CurrentPartyMember = partyMember;
                    layout.SetActiveCharacter(index, Enumerable.Range(0, MaxPartyMembers).Select(i => GetPartyMember(i)).ToList());
                    layout.SetCharacterHealSymbol(null);

                    if (currentBattle != null && updateBattlePosition && layout.Type == LayoutType.Battle)
                        BattlePlayerSwitched();

                    if (pickingNewLeader)
                    {
                        pickingNewLeader = false;
                        layout.ClosePopup(true, true);
                        newLeaderPicked?.Invoke(index);
                    }

                    if (is3D)
                        renderMap3D?.SetCameraHeight(partyMember.Race);
                }

                if (switched)
                    UpdateLight(false, false, true);

                ActivePlayerChanged?.Invoke();
            }
        }

        internal void DropGold(uint amount)
        {
            layout.ClosePopup(false, true);
            CurrentInventory.RemoveGold(amount);
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void DropFood(uint amount)
        {
            layout.ClosePopup(false, true);
            CurrentInventory.RemoveFood(amount);
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void StoreGold(uint amount)
        {
            layout.ClosePopup(false, true);
            var chest = OpenStorage as Chest;
            const uint MaxGoldPerChest = 0xffff;
            amount = Math.Min(amount, MaxGoldPerChest - chest.Gold);
            CurrentInventory.RemoveGold(amount);
            chest.Gold += amount;
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void StoreFood(uint amount)
        {
            layout.ClosePopup(false, true);
            var chest = OpenStorage as Chest;
            const uint MaxFoodPerChest = 0xffff;
            amount = Math.Min(amount, MaxFoodPerChest - chest.Food);
            CurrentInventory.RemoveFood(amount);
            chest.Food += amount;
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        /// <summary>
        /// Tries to store the item inside the opened storage.
        /// </summary>
        /// <param name="itemSlot">Item to store. Don't change the itemSlot itself!</param>
        /// <returns>Status of dropping</returns>
        internal bool StoreItem(ItemSlot itemSlot, uint maxAmount)
        {
            if (OpenStorage == null)
                return false; // should not happen

            var slots = OpenStorage.Slots.ToList();

            if (ItemManager.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
            {
                foreach (var slot in slots)
                {
                    if (!slot.Empty && slot.ItemIndex == itemSlot.ItemIndex)
                    {
                        // This will update itemSlot
                        int oldAmount = itemSlot.Amount;
                        slot.Add(itemSlot, (int)maxAmount);
                        int dropped = oldAmount - itemSlot.Amount;
                        maxAmount -= (uint)dropped;
                        if (maxAmount == 0)
                            return true;
                    }
                }
            }

            foreach (var slot in slots)
            {
                if (slot.Empty)
                {
                    // This will update itemSlot
                    slot.Add(itemSlot, (int)maxAmount);
                    return true;
                }
            }

            return false;
        }

        internal int DropItem(PartyMember partyMember, uint itemIndex, int amount)
        {
            return DropItem(SlotFromPartyMember(partyMember).Value, null, new ItemSlot
            {
                ItemIndex = itemIndex,
                Amount = amount
            });
        }

        /// <summary>
        /// Drops the item in the inventory of the given player.
        /// Returns the remaining amount of items that could not
        /// be dropped or 0 if all items were dropped successfully.
        /// </summary>
        internal int DropItem(int partyMemberIndex, int? slotIndex, ItemSlot item)
        {
            var partyMember = GetPartyMember(partyMemberIndex);

            if (partyMember == null || !partyMember.CanTakeItems(ItemManager, item))
                return item.Amount;

            bool stackable = ItemManager.GetItem(item.ItemIndex).Flags.HasFlag(ItemFlags.Stackable);

            var slots = slotIndex == null
                ? stackable ? partyMember.Inventory.Slots.Where(s => s.ItemIndex == item.ItemIndex && s.Amount < 99).ToArray() : new ItemSlot[0]
                : new ItemSlot[1] { partyMember.Inventory.Slots[slotIndex.Value] };
            int amountToAdd = item.Amount;

            if (slots.Length == 0) // no slot found -> try any empty slot
            {
                var emptySlot = partyMember.Inventory.Slots.FirstOrDefault(s => s.Empty);

                if (emptySlot == null) // no free slot
                    return item.Amount;

                // This reduces item.Amount internally.
                int remaining = emptySlot.Add(item);
                int added = amountToAdd - remaining;

                InventoryItemAdded(ItemManager.GetItem(emptySlot.ItemIndex), added, partyMember);

                return remaining;
            }

            var itemToAdd = ItemManager.GetItem(item.ItemIndex);

            foreach (var slot in slots)
            {
                // This reduces item.Amount internally.
                slot.Add(item);

                if (item.Empty)
                    break;
            }

            int addedAmount = amountToAdd - item.Amount;
            InventoryItemAdded(itemToAdd, addedAmount, partyMember);

            return item.Amount;
        }

        void SetWindow(Window window, params object[] parameters)
        {
            if ((window != Window.Inventory && window != Window.Stats) ||
                (currentWindow.Window != Window.Inventory && currentWindow.Window != Window.Stats))
                LastWindow = currentWindow;
            if (currentWindow.Window == window)
                currentWindow.WindowParameters = parameters;
            else
                currentWindow = new WindowInfo { Window = window, WindowParameters = parameters };
        }

        internal void ResetCursor()
        {
            if (CursorType == CursorType.Click ||
                CursorType == CursorType.SmallArrow ||
                CursorType == CursorType.None)
            {
                CursorType = CursorType.Sword;
            }
            UpdateCursor(lastMousePosition, MouseButtons.None);
        }

        internal void ClosePopup() => layout?.ClosePopup();

        internal void CloseWindow() => CloseWindow(null);

        internal void CloseWindow(Action finishAction)
        {
            layout.HideTooltip();

            if (!WindowActive)
            {
                finishAction?.Invoke();
                return;
            }

            ResetMapCharacterInteraction(Map);
            layout.SetCharacterHealSymbol(null);

            closeWindowHandler?.Invoke();
            closeWindowHandler = null;

            characterInfoTexts.Clear();
            characterInfoPanels.Clear();
            CurrentInventoryIndex = null;
            windowTitle.Visible = false;
            weightDisplayBlinking = false;

            if (currentWindow.Window == Window.Event || currentWindow.Window == Window.Riddlemouth)
            {
                InputEnable = true;
                ResetCursor();
            }

            var closedWindow = currentWindow;

            if (currentWindow.Window == LastWindow.Window)
                currentWindow = DefaultWindow;
            else
                currentWindow = LastWindow;

            switch (currentWindow.Window)
            {
                case Window.MapView:
                {
                    Fade(() =>
                    {
                        if (CurrentMapCharacter != null &&
                            (closedWindow.Window == Window.Battle ||
                            closedWindow.Window == Window.BattleLoot ||
                            closedWindow.Window == Window.Chest ||
                            closedWindow.Window == Window.Event))
                            CurrentMapCharacter = null;

                        ShowMap(true);
                        finishAction?.Invoke();

                        if (closedWindow.Window == Window.BattleLoot)
                            (closedWindow.WindowParameters[1] as Action)?.Invoke();
                    });
                    break;
                }
                case Window.Inventory:
                {
                    int partyMemberIndex = (int)currentWindow.WindowParameters[0];
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, true);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Stats:
                {
                    int partyMemberIndex = (int)currentWindow.WindowParameters[0];
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Chest:
                {
                    var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
                    bool trapFound = (bool)currentWindow.WindowParameters[1];
                    bool trapDisarmed = (bool)currentWindow.WindowParameters[2];
                    var map = (Map)currentWindow.WindowParameters[3];
                    var position = (Position)currentWindow.WindowParameters[4];
                    currentWindow = DefaultWindow;
                    ShowChest(chestEvent, trapFound, trapDisarmed, map, position);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Door:
                {
                    var doorEvent = (DoorEvent)currentWindow.WindowParameters[0];
                    bool trapFound = (bool)currentWindow.WindowParameters[1];
                    bool trapDisarmed = (bool)currentWindow.WindowParameters[2];
                    var map = (Map)currentWindow.WindowParameters[3];
                    var x = (uint)currentWindow.WindowParameters[4];
                    var y = (uint)currentWindow.WindowParameters[5];
                    currentWindow = DefaultWindow;
                    ShowDoor(doorEvent, trapFound, trapDisarmed, map, x, y);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Merchant:
                {
                    uint merchantIndex = (uint)currentWindow.WindowParameters[0];
                    string placeName = (string)currentWindow.WindowParameters[1];
                    string buyText = (string)currentWindow.WindowParameters[2];
                    bool isLibrary = (bool)currentWindow.WindowParameters[3];
                    var boughtItems = (ItemSlot[])currentWindow.WindowParameters[4];
                    OpenMerchant(merchantIndex, placeName, buyText, isLibrary, false, boughtItems);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Riddlemouth:
                {
                    var riddlemouthEvent = (RiddlemouthEvent)currentWindow.WindowParameters[0];
                    var solvedEvent = currentWindow.WindowParameters[1] as Action;
                    currentWindow = DefaultWindow;
                    ShowRiddlemouth(Map, riddlemouthEvent, solvedEvent, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Conversation:
                {
                    var conversationPartner = currentWindow.WindowParameters[0] as IConversationPartner;
                    var conversationEvent = currentWindow.WindowParameters[1] as Event;
                    var conversationItems = currentWindow.WindowParameters[2] as ConversationItems;
                    currentWindow = DefaultWindow;
                    ShowConversation(conversationPartner, conversationEvent, conversationItems, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Battle:
                {
                    var nextEvent = (Event)currentWindow.WindowParameters[0];
                    var combatBackgroundIndex = (uint?)currentWindow.WindowParameters[1];
                    currentWindow = DefaultWindow;
                    Fade(() => { ShowBattleWindow(nextEvent, out _, combatBackgroundIndex); finishAction?.Invoke(); });
                    break;
                }
                case Window.BattleLoot:
                {
                    var storage = (ITreasureStorage)currentWindow.WindowParameters[0];
                    LastWindow = DefaultWindow;
                    ShowBattleLoot(storage, null, 0);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.BattlePositions:
                {
                    ShowBattlePositionWindow();
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Trainer:
                {
                    var trainer = (Places.Trainer)currentWindow.WindowParameters[0];
                    OpenTrainer(trainer, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.FoodDealer:
                {
                    var foodDealer = (Places.FoodDealer)currentWindow.WindowParameters[0];
                    OpenFoodDealer(foodDealer, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Healer:
                {
                    var healer = (Places.Healer)currentWindow.WindowParameters[0];
                    OpenHealer(healer, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Camp:
                {
                    bool inn = (bool)currentWindow.WindowParameters[0];
                    OpenCamp(inn);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Inn:
                {
                    var inn = (Places.Inn)currentWindow.WindowParameters[0];
                    OpenInn(inn, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.HorseSalesman:
                {
                    var salesman = (Places.HorseSalesman)currentWindow.WindowParameters[0];
                    var buyText = (string)currentWindow.WindowParameters[1];
                    OpenHorseSalesman(salesman, buyText, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.RaftSalesman:
                {
                    var salesman = (Places.RaftSalesman)currentWindow.WindowParameters[0];
                    var buyText = (string)currentWindow.WindowParameters[1];
                    OpenRaftSalesman(salesman, buyText, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.ShipSalesman:
                {
                    var salesman = (Places.ShipSalesman)currentWindow.WindowParameters[0];
                    var buyText = (string)currentWindow.WindowParameters[1];
                    OpenShipSalesman(salesman, buyText, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Sage:
                {
                    var sage = (Places.Sage)currentWindow.WindowParameters[0];
                    OpenSage(sage, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Blacksmith:
                {
                    var blacksmith = (Places.Blacksmith)currentWindow.WindowParameters[0];
                    OpenBlacksmith(blacksmith, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Enchanter:
                {
                    var enchanter = (Places.Enchanter)currentWindow.WindowParameters[0];
                    OpenEnchanter(enchanter, false);
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                case Window.Automap:
                {
                    ShowAutomap();
                    if (finishAction != null)
                        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                    break;
                }
                default:
                    break;
            }
        }
    }
}
