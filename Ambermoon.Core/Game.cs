/*
 * Game.cs - Game core of Ambermoon
 *
 * Copyright (C) 2020-2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using static Ambermoon.UI.BuiltinTooltips;

namespace Ambermoon;

public class Game
{
    internal enum MobileAction
    {
        None,
        Move,
        Hand,
        Eye,
        Mouth,
        Interact,
        ButtonMove
    }

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

        Character Subject => game.currentWindow.Window switch
        {
            Window.Healer => game.currentlyHealedMember,
            Window.Battle => game.BattleRoundActive ? game.CurrentPartyMember : game.CurrentSpellTarget ?? game.CurrentPartyMember,
            _ => game.CurrentSpellTarget ?? game.CurrentPartyMember
        };

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
        readonly bool mobile;

        uint TickDivider(bool is3D, bool worldMap, TravelType travelType) => tickDivider[is3D ? 0 : !worldMap ? (mobile ? 2 : 1) : 2 + (int)travelType];
        public uint MovementTicks(bool is3D, bool worldMap, TravelType travelType) => TicksPerSecond / TickDivider(is3D, worldMap, travelType);
        public float MoveSpeed3D { get; }
        public float TurnSpeed3D { get; }

        public Movement(bool legacyMode, bool mobile)
        {
            this.mobile = mobile;
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
                8,  // Sand ship
                15, // Wasp
            };
            MoveSpeed3D = GetMoveSpeed3D(legacyMode, mobile);
            TurnSpeed3D = GetTurnSpeed3D(legacyMode, mobile);
        }

        static uint GetTickDivider3D(bool legacyMode) => legacyMode ? 8u : 60u;
        static float GetMoveSpeed3D(bool legacyMode, bool mobile) => mobile ? 0.03f : legacyMode ? 0.25f : 0.04f;
        static float GetTurnSpeed3D(bool legacyMode, bool mobile) => mobile ? 1.5f : legacyMode ? 15.0f : 2.0f;
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
    readonly IAdditionalSaveSlotProvider additionalSaveSlotProvider;
    internal GameLanguage GameLanguage { get; private set; }
    CharacterCreator characterCreator = null;
    readonly Random random = new Random();
    bool disableMusicChange = false;
    bool disableTimeEvents = false;
    readonly string gameVersionName;
    readonly string fullVersion;
    readonly Action<bool, string> keyboardRequest;
	public Features Features { get; }
    public bool Advanced => renderView.GameData.Advanced;
    public const int NumBaseSavegameSlots = 10;
    public const int NumAdditionalSavegameSlots = 20;
    internal SavegameTime GameTime { get; private set; } = null;
    readonly List<uint> changedMaps = new List<uint>();
    internal const int FadeTime = 1000;
    public const int MaxPartyMembers = 6;
    public const uint TicksPerSecond = 60;
    bool swamLastTick = false;
    bool swimDamageHandled = false;
    uint lastSwimDamageHour = 0;
    uint lastSwimDamageMinute = 0;
    public bool Teleporting { get; set; } = false;
    /// <summary>
    /// This is used for screen shaking.
    /// Position is in percentage of the resolution.
    /// </summary>
    public FloatPosition ViewportOffset { get; private set; } = null;
    public event Action QuitRequested;
    public bool Godmode
    {
        get;
        set;
    } = false;
    public bool NoClip
    {
        get;
        set;
    } = false;
    bool ingame = false;
    bool is3D = false;
    bool noEvents = false;
    bool levitating = false;
    const string schnismEasterEgg = "schnismschnismschnism";
    string schnism = "";
    internal const ushort MaxBaseLine = 0x4000;
    // Note: This is half the max base line which is used for the player in complete
    // darkness. Big gaps are needed as the z buffer precision is lower with higher distance.
    const ushort FowBaseLine = 0x2000;
    uint lightIntensity = 0;
    readonly IFow fow2D = null;
    readonly IOutroFactory outroFactory;
    IOutro outro = null;
    CustomOutro customOutro = null;
    internal bool CanSee() => !CurrentPartyMember.Conditions.HasFlag(Condition.Blind) &&
        (!Map.Flags.HasFlag(MapFlags.Dungeon) || lightIntensity > 0);
    internal bool GameOverButtonsVisible { get; private set; } = false;
    public bool WindowActive => currentWindow.Window != Window.MapView;
    public bool PopupActive => layout?.PopupActive ?? false;
    public bool WindowOrPopupActive => WindowActive || PopupActive;
    public bool CampActive => WindowActive && CurrentWindow.Window == Window.Camp;
    static readonly WindowInfo DefaultWindow = new WindowInfo { Window = Window.MapView };
    WindowInfo currentWindow = DefaultWindow;
    internal WindowInfo LastWindow { get; private set; } = DefaultWindow;
    internal WindowInfo CurrentWindow => currentWindow;
    Action<bool> closeWindowHandler = null;
    FilledArea mapViewRightFillArea = null;
    // Note: These are not meant for ingame stuff but for fade effects etc that use real time.
    readonly List<TimedGameEvent> timedEvents = new List<TimedGameEvent>();
    readonly Movement movement;
    internal uint CurrentTicks { get; private set; } = 0;
    internal uint CurrentMapTicks { get; private set; } = 0;
    internal uint CurrentBattleTicks { get; private set; } = 0;
    internal uint CurrentNormalizedBattleTicks { get; private set; } = 0;
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
    readonly Dictionary<SecondaryStat, Tooltip> characterInfoStatTooltips = new Dictionary<SecondaryStat, Tooltip>();
    public IMapManager MapManager { get; }
    public IItemManager ItemManager { get; }
    public ICharacterManager CharacterManager { get; }
    readonly Places places;
    IPlace currentPlace = null;
    readonly IRenderView renderView;
    internal IAudioOutput AudioOutput { get; private set; }
    readonly ISongManager songManager;
    ISong currentSong;
    Song? lastPlayedSong = null;
    internal ISavegameManager SavegameManager { get; }
    readonly ISavegameSerializer savegameSerializer;
    Player player;
    internal IRenderPlayer RenderPlayer => is3D ? (IRenderPlayer)player3D : player2D;
    internal Layout Layout => layout;
    public PartyMember CurrentPartyMember { get; private set; } = null;
    bool pickingNewLeader = false;
    bool pickingTargetPlayer = false;
    bool pickingTargetInventory = false;
    event Action<int> NewLeaderPicked;
    event Action<int> TargetPlayerPicked;
    event Func<int, bool> TargetInventoryPicked;
    event Func<ItemGrid, int, ItemSlot, bool> TargetItemPicked;
    bool partyAdvances = false; // party or monsters are advancing
    internal PartyMember CurrentInventory => CurrentInventoryIndex == null ? null : GetPartyMember(CurrentInventoryIndex.Value);
    internal int? CurrentInventoryIndex { get; set; } = null;
    internal Character CurrentCaster { get; set; } = null;
    internal Character CurrentSpellTarget { get; set; } = null;
    public Map Map => !ingame ? null : is3D ? renderMap3D?.Map : renderMap2D?.Map;
    public Position PartyPosition => !ingame || Map == null || player == null ? new Position() : LimitPartyPosition(Map.MapOffset + player.Position);
    Position LimitPartyPosition(Position position)
    {
        int width = Map.IsWorldMap ? (int)Map.WorldMapDimension * 50 : Map.Width;
        int height = Map.IsWorldMap ? (int)Map.WorldMapDimension * 50 : Map.Height;

        while (position.X < 0)
            position.X += width;
        while (position.Y < 0)
            position.Y += height;
        position.X %= width;
        position.Y %= height;
        return position;
    }
    internal bool MonsterSeesPlayer { get; set; } = false;
    bool monstersCanMoveImmediately = false; // this is set when the player just moved so that monsters who see the player can instantly move (2D only)
    Position lastPlayerPosition = null;
    BattleInfo currentBattleInfo = null;
    Battle currentBattle = null;
    public bool BattleActive => currentBattle != null;
    public bool BattleRoundActive => currentBattle?.RoundActive == true;
    public bool PlayerIsPickingABattleAction => BattleActive && !BattleRoundActive && currentPlayerBattleAction != PlayerBattleAction.PickPlayerAction;
    readonly ILayerSprite[] partyMemberBattleFieldSprites = new ILayerSprite[MaxPartyMembers];
    readonly Tooltip[] partyMemberBattleFieldTooltips = new Tooltip[MaxPartyMembers];
    PlayerBattleAction currentPlayerBattleAction = PlayerBattleAction.PickPlayerAction;
    PartyMember currentPickingActionMember = null;
    PartyMember currentlyHealedMember = null;
    SpellAnimation currentAnimation = null;
    readonly Dictionary<Spell, SpellInfo> spellInfos;
    internal IReadOnlyDictionary<Spell, SpellInfo> SpellInfos => spellInfos.AsReadOnly();
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
    ILayerSprite mobileClickIndicator = null;
		IColoredRect drugOverlay = null;
    uint lastDrugColorChangeTicks = 0;
    uint lastDrugMouseMoveTicks = 0;
    public Action DrugTicked;
    readonly bool[] keys = new bool[EnumHelper.GetValues<Key>().Length];
    bool allInputWasDisabled = false;
    bool allInputDisabled = false;
    bool inputEnable = true;
    bool paused = false;
    public bool Fading { get; private set; } = false;
    internal bool ConversationTextActive { get; private set; } = false;
    Func<MouseButtons, bool> nextClickHandler = null;
    Action itemDragCancelledHandler = null;
		MobileAction currentMobileAction = MobileAction.None;
    readonly ILayerSprite mobileActionIndicator;
    const int Mobile3DThreshold = 32;
    bool fingerDown = false;
		internal MobileAction CurrentMobileAction
    {
        get => currentMobileAction;
        set
        {
            if (!Configuration.IsMobile || currentMobileAction == value)
                return;

            if (value != MobileAction.ButtonMove)
					CurrentMobileButtonMoveCursor = null;

				currentMobileAction = value;
            var layer = currentMobileAction == MobileAction.Move || currentMobileAction == MobileAction.ButtonMove
                ? Layer.UI
                : Layer.Cursor;
				var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(layer);
				mobileActionIndicator.Visible = false;
            try
            {
                mobileActionIndicator.TextureAtlasOffset = currentMobileAction switch
                {
                    MobileAction.Move => textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.StatusMove)),
                    MobileAction.ButtonMove => textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.StatusMove)),
                    MobileAction.Hand => textureAtlas.GetOffset((uint)CursorType.Hand),
                    MobileAction.Eye => textureAtlas.GetOffset((uint)CursorType.Eye),
                    MobileAction.Mouth => textureAtlas.GetOffset((uint)CursorType.Mouth),
                    MobileAction.Interact => textureAtlas.GetOffset((uint)CursorType.Target),
                    _ => textureAtlas.GetOffset(0)
                };
                mobileActionIndicator.Layer = renderView.GetLayer(layer);
                mobileActionIndicator.Visible = currentMobileAction != MobileAction.None;
                UpdateMobileActionIndicatorPosition();
            }
            catch
            {
                mobileActionIndicator.Visible = false;
            }
        }
    }
		CursorType? currentMobileButtonMoveCursor;
		CursorType? CurrentMobileButtonMoveCursor
    {
        get => currentMobileButtonMoveCursor;
        set
        {
            if (currentMobileButtonMoveCursor == value)
                return;

            currentMobileButtonMoveCursor = value;
            var mapping = layout.GetMoveButtonCursorMapping();

            for (int i = 0; i < 9; i++)
            {
                if (i == 4)
                    continue;

                layout.GetButton(i).Pressed = mapping[i] == currentMobileButtonMoveCursor;
            }
			}
    }
    Func<bool> currentMobileButtonMoveAllowProvider;
    public Rect CurrentMapViewArea => new(mapViewArea);

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
            if (!inputEnable)
                layout.HideTooltip();
            UntrapMouse();

            if (!inputEnable)
            {
                ResetMoveKeys(true);
                CurrentMobileAction = MobileAction.None;
            }
        }
    }
    internal TravelType TravelType
    {
        get => travelType;
        set
        {
            bool superman = travelType == TravelType.Fly || value == TravelType.Fly;
            travelType = value;
            CurrentSavegame.TravelType = value;
            if (Map != null && Map.UseTravelMusic)
                PlayMusic(travelType.TravelSong());
            player.MovementAbility = travelType.ToPlayerMovementAbility();
            if (Map?.UseTravelTypes == true)
            {
                player2D?.UpdateAppearance(CurrentTicks);
                GetTransportsInVisibleArea(out TransportLocation transportAtPlayerIndex);
                player2D.BaselineOffset = !CanSee() || transportAtPlayerIndex != null ? MaxBaseLine :
                    player.MovementAbility > PlayerMovementAbility.Swimming ? 32 : 0;
            }
            else if (!is3D && player2D != null)
            {
                player2D.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
            }

            if (Map != null && layout.ButtonGridPage == 1)
            {
                if (Map.Flags.HasFlag(MapFlags.CanRest) && travelType.CanCampOn())
                {
                    layout.EnableButton(5, true);
                }
                else
                {
                    layout.EnableButton(5, false);
                }
            }

            if (superman)
                UpdateLight();
        }
    }
    bool clickMoveActive = false;
    bool trappedAfterClickMoveActivation = false;
    Rect trapMouseArea = null;
    Rect trapMouseGameArea = null;
    Rect preFullscreenChangeTrapMouseArea = null;
    Position preFullscreenMousePosition = null;
    bool mouseTrappingActive = false;
    Position lastMousePosition = new();
    FloatPosition mobileAutomapScroll = new();
    Position lastMobileAutomapFingerPosition = new();
    readonly Position trappedMousePositionOffset = new();
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
    public event Action<ILegacyGameData, int, int, int> RequestAdvancedSavegamePatching;

    // Rendering
    readonly Cursor cursor = null;
    RenderMap2D renderMap2D = null;
    Player2D player2D = null;
    RenderMap3D renderMap3D = null;
    Player3D player3D = null;
    readonly ICamera3D camera3D = null;
    readonly IRenderText windowTitle = null;
    byte currentUIPaletteIndex = 0;
    internal byte PrimaryUIPaletteIndex { get; }
    internal byte SecondaryUIPaletteIndex { get; }
    internal byte AutomapPaletteIndex { get; }
    internal byte CustomGraphicPaletteIndex => (byte)(PrimaryUIPaletteIndex + 3);
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

            if (value != CursorType.Eye &&
                value != CursorType.Mouth &&
                value != CursorType.Hand &&
                value != CursorType.Target)
                targetMode2DActive = false;

            if (Configuration.IsMobile)
            {
                if (value != CursorType.Click)
                    HideMobileClickIndicator();

					if (value >= CursorType.ArrowUp && value <= CursorType.ArrowRotateRight)
                {
                    CursorType = CursorType.Sword;
                    return; // Don't allow mouse cursor movement on mobile
                }

                if (value == CursorType.Hand)
                    CurrentMobileAction = MobileAction.Hand;
                else if (value == CursorType.Eye)
                    CurrentMobileAction = MobileAction.Eye;
                else if (value == CursorType.Mouth)
                    CurrentMobileAction = MobileAction.Mouth;
                else if (value == CursorType.Target)
                    CurrentMobileAction = MobileAction.Interact;
                
                cursor.Type = value;

                if (cursor.Type == CursorType.Click && layout.PopupActive)
                {
                    ShowMobileClickIndicatorForPopup();
                }
				}
            else
            {
                cursor.Type = value;

                if (!is3D && !WindowActive && !layout.PopupActive &&
                    (cursor.Type == CursorType.Eye ||
                    cursor.Type == CursorType.Hand))
                {
                    int yOffset = Map.UseTravelTypes ? 12 : 0;
                    TrapMouse(new Rect(player2D.DisplayArea.X - 9, player2D.DisplayArea.Y - 9 - yOffset, 33, 49));
                }
                else if (!is3D && !WindowActive && !layout.PopupActive &&
                    (cursor.Type == CursorType.Mouth ||
                    cursor.Type == CursorType.Target))
                {
                    int yOffset = Map.UseTravelTypes ? 12 : 0;
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
    }
    internal IMapCharacter CurrentMapCharacter { get; set; } // This is set when interacting with a map character

    internal void RequestFullscreenChange(bool fullscreen) => fullscreenChangeHandler?.Invoke(fullscreen);
    internal void NotifyResolutionChange(int? oldWidth) => resolutionChangeHandler?.Invoke(oldWidth);

    public string GetFullVersion() => fullVersion;

    public delegate void DrawTouchFingerHandler(int x, int y, bool longPress, Rect clipArea, bool behindPopup);

    readonly DrawTouchFingerHandler drawTouchFingerRequest;

	public Game(IConfiguration configuration, GameLanguage gameLanguage, IRenderView renderView, IGraphicProvider graphicProvider,
        ISavegameManager savegameManager, ISavegameSerializer savegameSerializer, TextDictionary textDictionary,
        Cursor cursor, IAudioOutput audioOutput, ISongManager songManager, FullscreenChangeHandler fullscreenChangeHandler,
        ResolutionChangeHandler resolutionChangeHandler, Func<List<Key>> pressedKeyProvider, IOutroFactory outroFactory,
        Features features, string gameVersionName, string version, Action<bool, string> keyboardRequest,
        IAdditionalSaveSlotProvider additionalSaveSlotProvider, DrawTouchFingerHandler drawTouchFingerRequest = null)
    {
        spellInfos = new(Data.SpellInfos.Entries);

        // In Advanced limit All Healing to Camp and Battle
        if (features.HasFlag(Features.AdvancedSpells))
            spellInfos[Spell.AllHealing] = spellInfos[Spell.AllHealing] with { ApplicationArea = SpellApplicationArea.CampAndBattle };

        this.drawTouchFingerRequest = drawTouchFingerRequest;
			this.keyboardRequest = keyboardRequest;
			Features = features;
        this.gameVersionName = gameVersionName;
        Character.FoodWeight = Features.HasFlag(Features.ReducedFoodWeight) ? 25u : 250u;
        currentUIPaletteIndex = PrimaryUIPaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);
        SecondaryUIPaletteIndex = (byte)(renderView.GraphicProvider.SecondaryUIPaletteIndex - 1);
        AutomapPaletteIndex = (byte)(renderView.GraphicProvider.AutomapPaletteIndex - 1);

        this.fullscreenChangeHandler = fullscreenChangeHandler;
        this.resolutionChangeHandler = resolutionChangeHandler;
        Configuration = configuration;
        GameLanguage = gameLanguage;
        this.additionalSaveSlotProvider = additionalSaveSlotProvider;
        this.cursor = cursor;
        this.pressedKeyProvider = pressedKeyProvider;
        movement = new Movement(configuration.LegacyMode, configuration.IsMobile);
        nameProvider = new NameProvider(this);
        this.renderView = renderView;
        AudioOutput = audioOutput;
        this.songManager = songManager;
        MapManager = renderView.GameData.MapManager;
        ItemManager = renderView.GameData.ItemManager;
        CharacterManager = renderView.GameData.CharacterManager;
        SavegameManager = savegameManager;
        layout = new Layout(this, renderView, ItemManager);
        layout.BattleFieldSlotClicked += BattleFieldSlotClicked;
        places = renderView.GameData.Places;
        this.savegameSerializer = savegameSerializer;
        DataNameProvider = renderView.GameData.DataNameProvider;
			fullVersion = version + $"^{GameVersion.RemakeReleaseDate}^^{DataNameProvider.DataVersionString}^{DataNameProvider.DataInfoString}";
			this.textDictionary = textDictionary;
        this.lightEffectProvider = renderView.GameData.LightEffectProvider;
        this.outroFactory = outroFactory;
        camera3D = renderView.Camera3D;
        windowTitle = renderView.RenderTextFactory.Create(
            (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
            renderView.GetLayer(Layer.Text),
            renderView.TextProcessor.CreateText(""), TextColor.BrightGray, true,
            layout.GetTextRect(8, 40, 192, 10), TextAlign.Center);
        windowTitle.DisplayLayer = 2;
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

        if (Configuration.IsMobile)
        {
            mobileClickIndicator = renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite;
				mobileClickIndicator.Layer = renderView.GetLayer(Layer.Cursor);
				mobileClickIndicator.Visible = false;
				mobileClickIndicator.PaletteIndex = PrimaryUIPaletteIndex;
				mobileClickIndicator.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Cursor).GetOffset((uint)CursorType.Click);
			}

			for (int i = 0; i < MaxPartyMembers; ++i)
        {
            hurtPlayerSprites[i] = renderView.SpriteFactory.Create(32, 26, true, 200) as ILayerSprite;
            hurtPlayerSprites[i].Layer = renderView.GetLayer(Layer.UI);
            hurtPlayerSprites[i].PaletteIndex = PrimaryUIPaletteIndex;
            hurtPlayerSprites[i].TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.DamageSplash));
            hurtPlayerSprites[i].Visible = false;
            hurtPlayerDamageTexts[i] = renderView.RenderTextFactory.Create((byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1));
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

			// Mobile action indicator
			mobileActionIndicator = renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite;
        mobileActionIndicator.Layer = renderView.GetLayer(Layer.UI);
        mobileActionIndicator.PaletteIndex = PrimaryUIPaletteIndex;
        mobileActionIndicator.DisplayLayer = 0;
        mobileActionIndicator.Visible = false;

			TextInput.FocusChanged += InputFocusChanged;
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
            customOutro = null;
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
            LoadInitial(name, female, (uint)portraitIndex, FixSavegameValues);
            characterCreator = null;
        });
    }

    private void FixSavegameValues(Savegame savegame)
    {
        foreach (var member in savegame.PartyMembers.Values)
        {
            uint weight = 0;

            // Add gold and food
            weight += member.Gold * Character.GoldWeight;
            weight += member.Food * Character.FoodWeight;

            // Add items
            foreach (var itemSlot in member.Inventory.Slots)
            {
                if (itemSlot == null || itemSlot.ItemIndex == 0)
                    continue;

                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                weight += (uint)itemSlot.Amount * item.Weight;
            }

            foreach (var itemSlot in member.Equipment.Slots.Values)
            {
                if (itemSlot == null || itemSlot.ItemIndex == 0)
                    continue;

                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                weight += (uint)itemSlot.Amount * item.Weight;
            }

            member.TotalWeight = weight;
        }
    }

    internal void LoadInitial(string name, bool female, uint portraitIndex, Action<Savegame> setup = null,
        Action postStartAction = null)
    {
        var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);

        initialSavegame.PartyMembers[1].Name = name;
        initialSavegame.PartyMembers[1].Gender = female ? Gender.Female : Gender.Male;
        initialSavegame.PartyMembers[1].PortraitIndex = (byte)portraitIndex;

        setup?.Invoke(initialSavegame);

        Start(initialSavegame, postStartAction);
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

    // Technical game pause settings
    bool audioWasEnabled = false;
    bool musicWasPlaying = false;
    bool gameWasPaused = false;
    bool gamePaused = false;

    public void PauseGame()
    {
        if (gamePaused)
            return;

        gamePaused = true;
        audioWasEnabled = AudioOutput?.Available == true && AudioOutput?.Enabled == true;
        musicWasPlaying = currentSong != null;
        gameWasPaused = paused;
        AudioOutput.Enabled = false;
        Pause();
    }

    public void ResumeGame()
    {
        if (!gamePaused)
            return;
        gamePaused = false;
        if (!gameWasPaused)
            Resume();
        if (audioWasEnabled)
        {
            AudioOutput.Enabled = true;
            if (musicWasPlaying)
                ContinueMusic();
        }
    }

    public void Pause()
    {
        if (paused)
            return;

        paused = true;

        GameTime?.Pause();

        if (is3D)
            renderMap3D?.Pause();
        else
            renderMap2D?.Pause();
    }

    public void Resume()
    {
        if (!paused || WindowActive)
            return;

        paused = false;

        GameTime?.Resume();

        if (is3D)
            renderMap3D?.Resume();
        else
            renderMap2D?.Resume();
    }

    internal static uint UpdateTicks(uint ticks, double deltaTime)
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

        if (customOutro?.CreditsActive == true)
        {
            customOutro.Update(deltaTime);
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
                swamLastTick = false;
                MonsterSeesPlayer = false; // Will be set by the monsters Update methods eventually

                CurrentTicks = UpdateTicks(CurrentTicks, deltaTime);

                CurrentMapTicks = CurrentTicks >= lastMapTicksReset ? CurrentTicks - lastMapTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMapTicksReset);

                if (is3D)
                {
                    renderMap3D.Update(CurrentMapTicks, GameTime);
                }
                else // 2D
                {
                    renderMap2D.Update(CurrentMapTicks, GameTime, monstersCanMoveImmediately, lastPlayerPosition);
                }

                monstersCanMoveImmediately = false;

                if (Configuration.IsMobile && CurrentMobileAction == MobileAction.ButtonMove)
                {
						if (CurrentMobileButtonMoveCursor != null && CurrentMobileButtonMoveCursor != CursorType.None)
						{
							if (currentMobileButtonMoveAllowProvider?.Invoke() == true)
								Move(false, 1.0f, CurrentMobileButtonMoveCursor.Value);
						}
					}
                else
                {
                    var moveTicks = CurrentTicks >= lastMoveTicksReset ? CurrentTicks - lastMoveTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMoveTicksReset);

                    if (moveTicks >= movement.MovementTicks(is3D, Map.UseTravelTypes, TravelType))
                    {
                        lastMoveTicksReset = CurrentTicks;

                        if (clickMoveActive)
                            HandleClickMovement();
                        else
                            Move();
                    }
                }
            }

            if ((!WindowActive ||
                currentWindow.Window == Window.Inventory ||
                currentWindow.Window == Window.Stats ||
                currentWindow.Window == Window.Chest) &&
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
                    double timeFactor = BattleTimeFactor;
                    CurrentBattleTicks = UpdateTicks(CurrentBattleTicks, deltaTime * timeFactor);
                    CurrentNormalizedBattleTicks = UpdateTicks(CurrentNormalizedBattleTicks, deltaTime);
                    UpdateBattle(1.0 / timeFactor);

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
                CurrentNormalizedBattleTicks = 0;
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

        if (CurrentPartyMember != null && CurrentPartyMember.Conditions.HasFlag(Condition.Drugged) &&
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
        if (is3D)
        {
            renderMap3D?.UpdateFloorAndCeilingVisibility(Configuration.ShowFloor, Configuration.ShowCeiling);
            renderMap3D?.SetFog(Map, MapManager.GetLabdataForMap(Map));
        }

        ConfigurationChanged?.Invoke(Configuration, windowChange);

        // Ensure the music is updated
        UpdateMusic();

        if (windowChange && !trapped)
        {
            trappedMousePositionOffset.X = 0;
            trappedMousePositionOffset.Y = 0;
            MouseTrappedChanged?.Invoke(false, lastMousePosition);
            UpdateCursor();
        }
    }

    public void ExternalGraphicFilterChanged() => layout.ExternalGraphicFilterChanged();
    public void ExternalGraphicFilterOverlayChanged() => layout.ExternalGraphicFilterOverlayChanged();
    public void ExternalEffectsChanged() => layout.ExternalEffectsChanged();
    public void ExternalBattleSpeedChanged()
    {
        SetBattleSpeed(Configuration.BattleSpeed);
        layout.ExternalBattleSpeedChanged();
    }
    public void ExternalMusicChanged() => layout.ExternalMusicChanged();
    public void ExternalVolumeChanged() => layout.ExternalVolumeChanged();

    internal int RollDice100()
    {
        return RandomInt(0, 99);
    }

    public int RandomInt(int min, int max)
    {
        uint range = (uint)(max + 1 - min);
        if (range == 0) // this avoid a possible division by zero crash
            return min;
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

    internal void ResetMoveKeys(bool forceDisable = false)
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
        ResetKey(Key.Q);
        ResetKey(Key.E);

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

    public Render.Color GetPrimaryUIColor(int colorIndex) => GetPaletteColor(renderView.GraphicProvider.PrimaryUIPaletteIndex, colorIndex);

    public Render.Color GetUIColor(int colorIndex) => GetPaletteColor(1 + UIPaletteIndex, colorIndex);

    internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial, Action<Map> mapInitAction = null)
    {
        if (map.Type != MapType.Map2D)
            throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

        layout.SetLayout(LayoutType.Map2D, movement.MovementTicks(false, Map?.UseTravelTypes == true, TravelType.Walk));
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
        {
            renderMap2D.SetMap(map, (uint)xOffset, (uint)yOffset);
            mapInitAction?.Invoke(map);
        }
        else
        {
            renderMap2D.ScrollTo((uint)xOffset, (uint)yOffset, true);
            mapInitAction?.Invoke(map);
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
        renderView.GetLayer(Layer.Map3DBackgroundFog).Visible = false;
        renderView.GetLayer(Layer.Map3DCeiling).Visible = false;
        renderView.GetLayer(Layer.Map3D).Visible = false;
        renderView.GetLayer(Layer.Billboards3D).Visible = false;
        for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
            renderView.GetLayer((Layer)i).Visible = true;

        mapViewArea = Map2DViewArea;

        PlayerMoved(true, null, true);
    }

    internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial, Action<Map> mapInitAction = null)
    {
        if (map.Type != MapType.Map3D)
            throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

        layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));

        is3D = true;
        TravelType = TravelType.Walk;
        renderMap2D.Destroy();
        renderMap3D.SetMap(map, playerX, playerY, direction, CurrentPartyMember?.Race ?? Race.Human, true);
        UpdateUIPalette(true);
        mapInitAction?.Invoke(map);
        player3D.SetPosition((int)playerX, (int)playerY, CurrentTicks, !initial);
        player3D.TurnTowards((int)direction * 90.0f);
        if (player2D != null)
            player2D.Visible = false;
        player.Position.X = (int)playerX;
        player.Position.Y = (int)playerY;
        player.Direction = direction;

        renderView.GetLayer(Layer.Map3DBackground).Visible = true;
        renderView.GetLayer(Layer.Map3DBackgroundFog).Visible = true;
        renderView.GetLayer(Layer.Map3DCeiling).Visible = true;
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
        CurrentMobileAction = MobileAction.None;
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

            if (!leaveMapCharacter)
                CurrentMapCharacter = null;
        }

        if (map.Type == MapType.Map3D)
            RenderMap3D.Reset();
        else
            MapCharacter2D.Reset();
    }

    public void Destroy()
    {
        drugOverlay?.Delete();
        ouchSprite?.Delete();
        mobileClickIndicator?.Delete();

			Util.SafeCall(UntrapMouse);
        allInputDisabled = true;
        ingame = false;
        Util.SafeCall(() => AudioOutput?.Stop());
        Util.SafeCall(() => AudioOutput?.Reset());
        Util.SafeCall(Cleanup);
        Util.SafeCall(() => layout.Destroy());
        Util.SafeCall(() => CursorType = CursorType.None);
        Util.SafeCall(() => windowTitle?.Delete());
        TextInput.FocusChanged -= InputFocusChanged;
    }

    void PartyMemberDied(Character partyMember)
    {
        if (partyMember is not PartyMember member)
            throw new AmbermoonException(ExceptionScope.Application, "PartyMemberDied with a character which is not a party member.");

        member.HitPoints.CurrentValue = 0;

        int? slot = SlotFromPartyMember(member);

        if (slot != null)
            layout.SetCharacter(slot.Value, member, false, () => ResetMoveKeys(true));
    }

    void PartyMemberRevived(PartyMember partyMember, Action finishAction = null, bool showHealAnimation = true, bool selfRevive = false)
    {
        string reviveMessage = selfRevive && partyMember.Race == Race.Animal && !string.IsNullOrWhiteSpace(DataNameProvider.ReviveCatMessage) ? DataNameProvider.ReviveCatMessage : DataNameProvider.ReviveMessage;

        if (currentWindow.Window == Window.Healer)
        {
            layout.UpdateCharacter(partyMember, () => layout.ShowClickChestMessage(reviveMessage, finishAction));
        }
        else
        {
            bool allInputWasDisabled = allInputDisabled;
            allInputDisabled = false;
            ShowMessagePopup(reviveMessage, () =>
            {
                allInputDisabled = allInputWasDisabled;

                void Finish()
                {
                    if (showHealAnimation)
                    {
                        currentAnimation?.Destroy();
                        currentAnimation = new SpellAnimation(this, layout);
                        // This will just show the heal animation
                        currentAnimation.CastOn(Spell.SelfHealing, partyMember, () =>
                        {
                            currentAnimation.Destroy();
                            currentAnimation = null;
                            finishAction?.Invoke();
                        });
                    }
                    else
                    {
                        finishAction?.Invoke();
                    }
                }

                layout.SetCharacter(SlotFromPartyMember(partyMember).Value, partyMember, false, Finish);

                if (currentWindow.Window == Window.Inventory && partyMember == CurrentInventory)
                    UpdateCharacterInfo();

                layout.FillCharacterBars(partyMember);
            });
        }
    }

    void FixPartyMember(PartyMember partyMember)
    {
        // Don't do it for animals though!
        if (partyMember.Race > Race.Thalionic)
            return;

        // The original has some bugs where bonus values are not right.
        // We set the bonus values here dependent on equipment.
        partyMember.HitPoints.BonusValue = 0;
        partyMember.SpellPoints.BonusValue = 0;
        partyMember.BonusDefense = 0;
        partyMember.BonusAttackDamage = 0;

        foreach (var attribute in EnumHelper.GetValues<Attribute>())
        {
            partyMember.Attributes[attribute].BonusValue = 0;
        }

        foreach (var skill in EnumHelper.GetValues<Skill>())
        {
            partyMember.Skills[skill].BonusValue = 0;
        }

        foreach (var itemSlot in partyMember.Equipment.Slots)
        {
            if (itemSlot.Value.ItemIndex != 0)
            {
                var item = ItemManager.GetItem(itemSlot.Value.ItemIndex);
                int factor = itemSlot.Value.Flags.HasFlag(ItemSlotFlags.Cursed) ? -1 : 1;

                partyMember.HitPoints.BonusValue += factor * item.HitPoints;
                partyMember.SpellPoints.BonusValue += factor * item.SpellPoints;
                partyMember.BonusDefense = (short)(partyMember.BonusDefense + factor * item.Defense);
                partyMember.BonusAttackDamage = (short)(partyMember.BonusAttackDamage + factor * item.Damage);

                if (item.Attribute != null)
                    partyMember.Attributes[item.Attribute.Value].BonusValue += factor * item.AttributeValue;
                if (item.Skill != null)
                    partyMember.Skills[item.Skill.Value].BonusValue += factor * item.SkillValue;
            }
        }
    }

    /// <summary>
    /// Is used for external cheats.
    /// </summary>
    /// <param name="partyMember">The party member to add</param>
    /// <returns>0: Success, -1: Wrong window, -2: No free slot</returns>
    public int AddPartyMember(PartyMember partyMember)
    {
        if (CurrentWindow.Window != Window.MapView || WindowActive)
        {
            return -1; // Wrong window
        }

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            if (GetPartyMember(i) == null)
            {
                CurrentSavegame.CurrentPartyMemberIndices[i] =
                    CurrentSavegame.PartyMembers.FirstOrDefault(p => p.Value == partyMember).Key;
                this.AddPartyMember(i, partyMember, null, true);
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
                ushort characterBit;
                if (IsMapCharacterActive(PartyMemberInitialCharacterBits[partyMember.Index]))
                    characterBit = PartyMemberInitialCharacterBits[partyMember.Index];
                else
                    characterBit = PartyMemberCharacterBits[partyMember.Index];
                SetMapCharacterBit(characterBit, true);
                if (partyMember.CharacterBitIndex == 0xffff || partyMember.CharacterBitIndex == 0x0000)
                    partyMember.CharacterBitIndex = characterBit;
                return 0;
            }
        }

        return -2; // No free slot
    }

    void AddPartyMember(int slot, PartyMember partyMember, Action followAction = null, bool forceAnimation = false)
    {
        FixPartyMember(partyMember);
        partyMember.Died += PartyMemberDied;
        layout.SetCharacter(slot, partyMember, false, followAction, forceAnimation);
        spellListScrollOffsets[slot] = 0;
    }

    internal void RemovePartyMember(int slot, bool initialize, Action followAction = null)
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

    private void UpdateOutdoorLight(uint minutesPassed, bool lightOff)
    {
        bool lightBuffBurningOut = false;

        if (!CurrentSavegame.IsSpellActive(ActiveSpellType.Light) && minutesPassed == 5)
        {
            uint lastHour;

            if (GameTime.Minute == 0) // hour changed
            {
                lastHour = GameTime.Hour == 0 ? 23 : GameTime.Hour - 1;
            }
            else
            {
                lastHour = GameTime.Hour;
            }

            uint expectedLightIntensity = GetDaytimeLightIntensity(lastHour);

            if (lightIntensity > expectedLightIntensity)
                lightBuffBurningOut = true;
        }

        uint newExpectedLightIntensity = GetDaytimeLightIntensity();

        if (lightBuffBurningOut)
            lightIntensity = (uint)Math.Max(newExpectedLightIntensity, (int)lightIntensity - 16);
        else
            lightIntensity = newExpectedLightIntensity;

        UpdateLight(false, false, false, null, lightBuffBurningOut ? lightIntensity : (uint?)null);

        if (lightOff)
            renderMap3D?.SetFog(Map, MapManager.GetLabdataForMap(Map), lightOff);
    }

    public void Start(Savegame savegame, Action postAction = null)
    {
        lastPlayedSong = null;
        currentSong?.Stop();
        currentSong = null;
        Cleanup();
        MapExtensions.Reset();
        GameOverButtonsVisible = false;
        allInputDisabled = true;
        layout.AddFadeEffect(new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight), Render.Color.Black, FadeEffectType.FadeOut, FadeTime / 2);
        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime / 2), () => { allInputDisabled = false; postAction?.Invoke(); });

        // Reset all maps
        foreach (var changedMap in changedMaps)
        {
            MapManager.GetMap(changedMap).Reset();
        }
        changedMaps.Clear();

        ingame = true;
        CurrentSavegame = savegame;
        GameTime = new SavegameTime(savegame);
        lastSwimDamageHour = GameTime.Hour;
        lastSwimDamageMinute = GameTime.Minute;
        GameTime.GotTired += GameTime_GotTired;
        GameTime.GotExhausted += GameTime_GotExhausted;
        GameTime.NewDay += GameTime_NewDay;
        GameTime.NewYear += GameTime_NewYear;
        GameTime.MinuteChanged += amount =>
        {
            if (disableTimeEvents)
                return;

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
            else if (Map.Flags.HasFlag(MapFlags.Dungeon) &&
                CurrentSavegame.IsSpellActive(ActiveSpellType.Light) &&
                CurrentSavegame.GetActiveSpellDuration(ActiveSpellType.Light) * 5 < amount)
            {
                lightIntensity = 0;
                CurrentSavegame.ActiveSpells
                    .Where(s => s?.Type == ActiveSpellType.Light)
                    .ToList().ForEach(s => s.Duration = 0);
                UpdateLight(true);
            }
            else if (Map.Flags.HasFlag(MapFlags.Outdoor))
            {
                if (this.is3D)
                {
                    bool lightOff = CurrentSavegame.IsSpellActive(ActiveSpellType.Light) && CurrentSavegame.GetActiveSpellDuration(ActiveSpellType.Light) * 5 < amount;
                    UpdateOutdoorLight(amount, lightOff);
                }
                else if (GameTime.Minute % 60 == 0 || amount > GameTime.Minute % 60) // hour changed
                    UpdateLight();
            }

            if (!swamLastTick && Map.UseTravelTypes && TravelType == TravelType.Swim)
            {
                // Waiting or if a hour passes, it handles the swim damage instead.
                // This is important as hour changes might also trigger exhaustion or tired
                // messages and will also process poison damage.
                // As this event comes before the hour change event, we will check only next cycle.
                ExecuteNextUpdateCycle(() =>
                {
                    if (!swimDamageHandled)
                        DoSwimDamage(amount / 5);
                    else
                        swimDamageHandled = false;
                });
            }
            else
            {
                swimDamageHandled = false;
            }
        };
        GameTime.HourChanged += hours => GameTime_HoursPassed(hours, true);
        currentBattle = null;

        ClearPartyMembers();
        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            if (savegame.CurrentPartyMemberIndices[i] != 0)
            {
                var partyMember = savegame.GetPartyMember(i);
                CheckWeight(partyMember);
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

        if (map == null)
            throw new AmbermoonException(ExceptionScope.Data, $"Map with index {savegame.CurrentMapIndex} does not exist.");

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
        if (!map.IsWorldMap)
        {
            player.Position.X = (int)savegame.CurrentMapX - 1;
            player.Position.Y = (int)savegame.CurrentMapY - 1;
        }
        TravelType = savegame.TravelType; // Yes this is necessary twice.

        ShowMap(true);
        layout.ShowPortraitArea(true);
        UpdateLight(true);

        if (is3D)
            Fade3DMapIn(20, FadeTime / 40);

        InputEnable = true;
        paused = false;

        if (layout.ButtonGridPage == 1)
            ToggleButtonGridPage();

        if (!is3D)
            player2D.Visible = true;

        if (!TravelType.IgnoreEvents())
        {
            // Trigger events after game load
            TriggerMapEvents(EventTrigger.Move, (uint)player.Position.X,
                (uint)player.Position.Y);
        }

        PlayerMoved(false, new Position(player.Position), false);

        void CheckWeight(PartyMember partyMember)
        {
            // Adjust weight in case it was set to a wrong value before.
            partyMember.TotalWeight = (uint)partyMember.Gold * Character.GoldWeight + (uint)partyMember.Food * Character.FoodWeight;

            foreach (var item in partyMember.Inventory.Slots)
            {
                if (item != null && item.Amount != 0 && item.ItemIndex != 0)
                {
                    var itemInfo = ItemManager.GetItem(item.ItemIndex);
                    partyMember.TotalWeight += (uint)item.Amount * itemInfo.Weight;
                }
            }

            foreach (var item in partyMember.Equipment.Slots.Values)
            {
                if (item != null && item.Amount != 0 && item.ItemIndex != 0)
                {
                    var itemInfo = ItemManager.GetItem(item.ItemIndex);
                    partyMember.TotalWeight += (uint)item.Amount * itemInfo.Weight;
                }
            }
        }
    }

    internal void ProcessPoisonDamage(uint times, Action<bool> followAction = null)
    {
        uint GetDamage()
        {
            uint damage = 0;

            for (uint i = 0; i < times; ++i)
                damage += (uint)RandomInt(1, 5);

            return damage;
        }

        DamageAllPartyMembers(_ => GetDamage(),
            p => p.Alive && p.Conditions.HasFlag(Condition.Poisoned), null, followAction);
    }

    void Sleep(bool inn, int healing)
    {
        healing = Util.Limit(0, healing, 100);

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            var partyMember = GetPartyMember(i);

            if (partyMember != null && partyMember.Alive)
            {
                if (partyMember.Conditions.HasFlag(Condition.Exhausted))
                {
                    partyMember.Conditions &= ~Condition.Exhausted;
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
                        if (!inn && partyMember.Food == 0 && partyMember.Race < Race.Animal)
                        {
                            layout.ShowClickChestMessage(partyMember.Name + DataNameProvider.HasNoMoreFood, Next);
                        }
                        else
                        {
                            int lpRecovered = Util.Limit(0, healing * (int)partyMember.HitPoints.TotalMaxValue / 100,
                                (int)partyMember.HitPoints.TotalMaxValue - (int)partyMember.HitPoints.CurrentValue);
                            partyMember.HitPoints.CurrentValue += (uint)lpRecovered;
                            int spRecovered = Util.Limit(0, healing * (int)partyMember.SpellPoints.TotalMaxValue / 100,
                                (int)partyMember.SpellPoints.TotalMaxValue - (int)partyMember.SpellPoints.CurrentValue);
                            partyMember.SpellPoints.CurrentValue += (uint)spRecovered;
                            layout.FillCharacterBars(partyMember);

                            if (!inn && partyMember.Race < Race.Animal)
                                --partyMember.Food;

                            if (partyMember.Class.IsMagic() && spRecovered != 0) // Has SP and was recovered
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
        if (disableTimeEvents)
            return;

        swimDamageHandled = true;
        bool alreadyExhausted = false;
        uint[] damageValues = new uint[MaxPartyMembers];

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            var partyMember = GetPartyMember(i);

            if (partyMember != null && partyMember.Alive)
            {
                bool exhausted = partyMember.Conditions.HasFlag(Condition.Exhausted);
                if (exhausted)
                    alreadyExhausted = true;
                partyMember.Conditions |= Condition.Exhausted;
                damageValues[i] = AddExhaustion(partyMember, hoursExhausted, !exhausted);
                if (damageValues[i] < partyMember.HitPoints.CurrentValue)
                    layout.UpdateCharacterStatus(partyMember);
            }
        }

        void DealDamage()
        {
            DamageAllPartyMembers(p => damageValues[SlotFromPartyMember(p).Value],
                null, null, someoneDied =>
                {
                    GameTime_HoursPassed(hoursPassed);

                    if (someoneDied)
                    {
							CurrentMobileAction = MobileAction.None;
							clickMoveActive = false;
                        ResetMoveKeys(true);
                    }
                });
        }

        if (!alreadyExhausted)
            ShowMessagePopup(DataNameProvider.ExhaustedMessage, DealDamage);
        else
            DealDamage();
    }

    void GameTime_GotTired(uint hoursPassed)
    {
        if (disableTimeEvents)
            return;

        swimDamageHandled = true;
        ShowMessagePopup(DataNameProvider.TiredMessage, () => GameTime_HoursPassed(hoursPassed));
    }

    void GameTime_HoursPassed(uint hours, bool notTiredNorExhausted = false)
    {
        if (disableTimeEvents)
            return;

        ProcessPoisonDamage(hours, someoneDied =>
        {
            if (!notTiredNorExhausted && !swamLastTick && Map.UseTravelTypes && TravelType == TravelType.Swim)
            {
                int hours = (int)(24 + GameTime.Hour - lastSwimDamageHour) % 24;
                int minutes = (int)GameTime.Minute - (int)lastSwimDamageMinute;
                DoSwimDamage((uint)(hours * 12 + minutes / 5), someoneDrown =>
                {
                    if (someoneDied || someoneDrown)
                    {
							CurrentMobileAction = MobileAction.None;
							clickMoveActive = false;
                        ResetMoveKeys(true);
                    }
                });
            }
        });
    }

    void AgePlayer(PartyMember partyMember, Action finishAction, uint ageIncrease)
    {
        partyMember.Attributes[Attribute.Age].CurrentValue += ageIncrease;

        bool allInputWasDisabled = allInputDisabled;
        allInputDisabled = false;

        void Finish()
        {
            allInputDisabled = allInputWasDisabled;
            finishAction?.Invoke();
        }

        if (partyMember.Attributes[Attribute.Age].CurrentValue >= partyMember.Attributes[Attribute.Age].MaxValue)
        {
            partyMember.Attributes[Attribute.Age].CurrentValue = partyMember.Attributes[Attribute.Age].MaxValue;
            ShowMessagePopup(partyMember.Name + DataNameProvider.HasDiedOfAge, () =>
            {
                KillPartyMember(partyMember);
                Finish();
            });
        }
        else
        {
            ShowMessagePopup(partyMember.Name + DataNameProvider.HasAged, Finish);
        }
    }

    public void KillPartyMember(PartyMember partyMember, Condition deathCondition = Condition.DeadCorpse)
    {
        RemoveCondition(Condition.Exhausted, partyMember);
        partyMember.Die(deathCondition);
    }

    // Note: Only used external for cheats
    public void RecheckActivePlayer()
    {
        if (RecheckActivePartyMember(out bool gameOver))
        {
            if (gameOver || !BattleActive)
                return;
            BattlePlayerSwitched();
        }
        else if (BattleActive)
        {
            AddCurrentPlayerActionVisuals();
        }
    }

    public bool HasPartyMemberFled(PartyMember partyMember)
    {
        return currentBattle?.HasPartyMemberFled(partyMember) ?? false;
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
        bool wasClickMoveActive = clickMoveActive;
        StartSequence();

        void Run(int index)
        {
            if (index == MaxPartyMembers)
            {
                Finish();
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

        void Finish()
        {
            EndSequence();
            clickMoveActive = wasClickMoveActive;
				CurrentMobileAction = MobileAction.None;
				followUpAction?.Invoke();
        }
    }

    void GameTime_NewDay(uint exhaustedHours, uint passedHours)
    {
        if (disableTimeEvents)
            return;

        void Age(PartyMember partyMember, Action finishAction)
            => AgePlayer(partyMember, finishAction, 1);

        ForeachPartyMember(Age, partyMember =>
            partyMember.Alive && partyMember.Conditions.HasFlag(Condition.Aging) &&
                !partyMember.Conditions.HasFlag(Condition.Petrified), () =>
                {
                    if (exhaustedHours > 0)
                        GameTime_GotExhausted(exhaustedHours, passedHours);
                    else if (CurrentSavegame.HoursWithoutSleep >= 24)
                        GameTime_GotTired(passedHours);
                    else
                        GameTime_HoursPassed(passedHours, true);
                });
    }

    void GameTime_NewYear(uint exhaustedHours, uint passedHours)
    {
        if (disableTimeEvents)
            return;

        void Age(PartyMember partyMember, Action finishAction)
        {
            uint ageIncrease = partyMember.Conditions.HasFlag(Condition.Aging) ? 2u : 1u;
            AgePlayer(partyMember, finishAction, ageIncrease);
        }

        ForeachPartyMember(Age, partyMember =>
            partyMember.Alive && !partyMember.Conditions.HasFlag(Condition.Petrified), () =>
            {
                if (exhaustedHours > 0)
                    GameTime_GotExhausted(exhaustedHours, passedHours);
                else if (CurrentSavegame.HoursWithoutSleep >= 24)
                    GameTime_GotTired(passedHours);
                else
                    GameTime_HoursPassed(passedHours, true);
            });
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
        Action<Action> preLoadAction = null, bool exitWhenFailing = true, Action<int> postAction = null,
        bool updateSlot = false)
    {
        void Failed()
        {
            if (exitWhenFailing)
                Exit();
            else
                ClosePopup();
        }

        int totalSavegames = Configuration.ExtendedSavegameSlots ? 30 : 10;

        var savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, slot, totalSavegames);

        if (savegame == null)
        {
            if (showError)
            {
                if (loadInitialOnError && slot != 0)
                {
                    savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, 0, totalSavegames);

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

        if (updateSlot && slot > 0)
        {
            if (slot <= 10)
                SavegameManager.SetActiveSavegame(renderView.GameData, slot);

            if (Configuration.AdditionalSavegameSlots != null)
            {
                var additionalSavegameSlots = GetAdditionalSavegameSlots();
                additionalSavegameSlots.ContinueSavegameSlot = slot;
            }
        }

        // Upgrade old Ambermoon Advanced save games
        if (renderView.GameData.Advanced && slot > 0)
        {
            // TODO: will only work for legacy data for now!
            int sourceEpisode;
            int targetEpisode = 3; // TODO: increase when there is a new one

            if (!savegame.PartyMembers.ContainsKey(16)) // If Kasimir is not there, it is episode 1
                sourceEpisode = 1;
            else if (!savegame.Chests.ContainsKey(280)) // If chest 280 is not there it is episode 2
                sourceEpisode = 2;
            else // If both are there, it is episode 3
                sourceEpisode = 3;

            if (sourceEpisode < targetEpisode)
            {
                RequestAdvancedSavegamePatching((ILegacyGameData)renderView.GameData, slot, sourceEpisode, targetEpisode);
                savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, slot, totalSavegames);
            }
        }

        void Start() => this.Start(savegame, () => postAction?.Invoke(slot));

        if (preLoadAction != null)
            preLoadAction?.Invoke(Start);
        else
            Start();
    }

    void PrepareSaving(Action saveAction)
    {
        // Note: In 3D it is possible to walk partly on tiles that block the player. For example
        // small objects. But when you save and load you will get stuck with that position.
        // We could avoid this partial movement but this feels bad ingame as you have to move around
        // small objects in a larger way. So we will adjust the position only on saving. It won't
        // have the same position after reload but you won't get stuck.
        Position restorePosition = null;
        try
        {
            if (is3D && renderMap3D.IsBlockingPlayer(CurrentSavegame.CurrentMapX - 1, CurrentSavegame.CurrentMapY - 1))
            {
                var touchedPositions = player3D.GetTouchedPositions(Global.DistancePerBlock);
                var availablePositions = touchedPositions.Skip(1).Where(position => !renderMap3D.IsBlockingPlayer(position)).ToList();

                if (availablePositions.Count != 0)
                {
                    float tileX = (-camera3D.X - 0.5f * Global.DistancePerBlock) / Global.DistancePerBlock;
                    float tileY = Map.Height - (camera3D.Z + 0.5f * Global.DistancePerBlock) / Global.DistancePerBlock;
                    var basePosition = new FloatPosition(tileX, tileY);
                    var savegamePosition = availablePositions.Count == 1 ? availablePositions[0] :
                        availablePositions.OrderBy(position => basePosition.Distance(position)).First();
                    restorePosition = new Position((int)CurrentSavegame.CurrentMapX, (int)CurrentSavegame.CurrentMapY);
                    CurrentSavegame.CurrentMapX = 1 + (uint)savegamePosition.X;
                    CurrentSavegame.CurrentMapY = 1 + (uint)savegamePosition.Y;
                }
            }
        }
        catch
        {
            // ignore
        }

        // If a crash save is stored and the game crashes inside a place (like merchants),
        // all the gold is at the place and none at the party.
        if (currentPlace != null && currentPlace.AvailableGold != 0)
        {
            DistributeGold(currentPlace.AvailableGold, true);
        }

        saveAction?.Invoke();

        try
        {
            if (restorePosition != null)
            {
                CurrentSavegame.CurrentMapX = (uint)restorePosition.X;
                CurrentSavegame.CurrentMapY = (uint)restorePosition.Y;
            }
        }
        catch
        {
            // ignore
        }
    }

    internal AdditionalSavegameSlots GetAdditionalSavegameSlots() => additionalSaveSlotProvider.GetOrCreateAdditionalSavegameNames(gameVersionName);

    public void SaveCrashedGame()
    {
        PrepareSaving(() => SavegameManager.SaveCrashedGame(savegameSerializer, CurrentSavegame));
    }

    public void SaveGame(int slot, string name)
    {
        PrepareSaving(() =>
        {
            SavegameManager.Save(renderView.GameData, savegameSerializer, slot, name, CurrentSavegame);

            if (Configuration.ExtendedSavegameSlots) // extended slots
            {
                var additionalSavegameSlots = GetAdditionalSavegameSlots();

                if (additionalSavegameSlots.Names == null)
                    additionalSavegameSlots.Names = new string[NumAdditionalSavegameSlots];
                else if (additionalSavegameSlots.Names.Length > NumAdditionalSavegameSlots)
                    additionalSavegameSlots.Names = additionalSavegameSlots.Names.Take(NumAdditionalSavegameSlots).ToArray();
                else if (additionalSavegameSlots.Names.Length < NumAdditionalSavegameSlots)
                    additionalSavegameSlots.Names = Enumerable.Concat(additionalSavegameSlots.Names,
                        Enumerable.Repeat("", NumAdditionalSavegameSlots - additionalSavegameSlots.Names.Length)).ToArray();

                if (slot > Game.NumBaseSavegameSlots) // 1-based slot
                    additionalSavegameSlots.Names[slot - Game.NumBaseSavegameSlots - 1] = name;
                else
                    additionalSavegameSlots.BaseNames[slot - 1] = name;

                additionalSavegameSlots.ContinueSavegameSlot = slot;

                additionalSaveSlotProvider.RequestSave(SavegameManager, renderView.GameData);
            }
        });
    }

    public void ContinueGame()
    {
        if (SavegameManager.HasCrashSavegame())
        {
            ingame = true;
            ShowDecisionPopup(GetCustomText(CustomTexts.Index.LoadCrashedGame), response =>
            {
                if (response == PopupTextEvent.Response.Yes)
                {
                    LoadGame(99, false, true);
                    if (!SavegameManager.RemoveCrashedSavegame())
                        ShowMessagePopup(GetCustomText(CustomTexts.Index.FailedToRemoveCrashSavegame));
                }
                else
                {
                    Continue();
                }
            }, 1, 0, TextAlign.Center, false);
            return;
        }

        Continue();

        void Continue()
        {
            int current = Configuration.ExtendedSavegameSlots ? GetAdditionalSavegameSlots()?.ContinueSavegameSlot ?? 0 : 0;

            if (current <= 0)
                SavegameManager.GetSavegameNames(renderView.GameData, out current, NumBaseSavegameSlots);

            LoadGame(current, false, true);
        }
    }

    // TODO: Optimize to not query this every time
    public List<string> Dictionary => CurrentSavegame == null ? null : textDictionary.Entries.Where((word, index) =>
        CurrentSavegame.IsDictionaryWordKnown((uint)index)).ToList();

    public bool AutoDerune => TravelType == TravelType.Fly || // Superman mode can also read runes as text
        (Configuration.AutoDerune && PartyMembers.Any(p => p.HasItem(145))); // 145: Rune Table

    public IText ProcessText(string text)
    {
        if (text.Contains("~RUN1~") && AutoDerune) // has rune alphabet and auto derune is active
        {
            // ~INK 32~ resets to default color (at least in our implementation)
            // ~INK cc~
            // ~RUN1~text~NORM~ or ~"text"~
            text = text
                .Replace("~RUN1~ ", $"~INK {(int)TextColor.Beige}~")
                .Replace("~RUN1~", $"~INK {(int)TextColor.Beige}~")
                .Replace("~NORM~ ", "~INK 32~")
                .Replace("~NORM~", "~INK 32~");
        }

        return renderView.TextProcessor.ProcessText(text, nameProvider, Dictionary);
    }

    public IText ProcessText(string text, Rect bounds)
    {
        return renderView.TextProcessor.WrapText(ProcessText(text), bounds, new Size(Global.GlyphWidth, Global.GlyphLineHeight));
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
        if (!clickMoveActive || DisallowMoving())
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

    public void StartSequence()
    {
        allInputWasDisabled = allInputDisabled;
        layout.ReleaseButtons();
        allInputDisabled = true;
        clickMoveActive = false;
        CurrentMobileAction = MobileAction.None;
        trappedAfterClickMoveActivation = false;
    }

    public void EndSequence(bool force = true)
    {
        if (force || !allInputWasDisabled)
            allInputDisabled = false;
        allInputWasDisabled = false;
    }

    public class GameSequence : IDisposable
    {
        private readonly Game game;

        public GameSequence(Game game)
        {
            this.game = game;
            game.StartSequence();
        }

        public void Dispose()
        {
            game.EndSequence();
        }
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
            ResetMoveKeys();
            followUpAction?.Invoke();
        });
    }

    internal void Wait(uint hours)
    {
        if (hours != 0)
            GameTime.Wait(hours);
    }

    internal bool IsNight()
    {
        return CurrentSavegame.Hour >= 22 || CurrentSavegame.Hour < 5;
    }

    public bool CanRevive() => CurrentWindow.Window == Window.Camp;

    // Note: Eagle and wasp allow movement even with overweight.
    bool CanPartyMove() => TravelType == TravelType.Eagle || TravelType == TravelType.Wasp || !PartyMembers.Any(p => !p.CanMove(false));

		internal void StartVirtualButtonMovement(CursorType cursorType, int buttonIndex, Func<bool> allowMovementProvider)
    {
        if (!fingerDown)
            return;

        var button = layout.GetButton(buttonIndex);
        button.Pressed = true;
        CurrentMobileAction = MobileAction.ButtonMove;
			CurrentMobileButtonMoveCursor = cursorType;
        currentMobileButtonMoveAllowProvider = allowMovementProvider;
		}

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
                void PlayTurnSequence(int steps, Action turnAction)
                {
                    PlayTimedSequence(steps, () =>
                    {
                        turnAction?.Invoke();
                        CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
                    }, 65);
                }

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
                        PlayTurnSequence(12, () => player3D.TurnLeft(15.0f));
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
                        PlayTurnSequence(12, () => player3D.TurnRight(15.0f));
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

            CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
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

            CurrentSavegame.CharacterDirection = player.Direction = player2D.Direction;
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

    bool DisallowMoving() => paused || WindowActive || !InputEnable || allInputDisabled || pickingNewLeader || pickingTargetPlayer || pickingTargetInventory;

		void Move()
    {
        if (DisallowMoving())
        {
            if (CurrentMobileAction == MobileAction.ButtonMove)
                CurrentMobileAction = MobileAction.None;
            return;
        }

        if (Configuration.IsMobile && CurrentMobileAction == MobileAction.ButtonMove && CurrentMobileButtonMoveCursor != null && CurrentMobileButtonMoveCursor != CursorType.None)
        {
            if (currentMobileButtonMoveAllowProvider?.Invoke() == true)
                Move(false, 1.0f, CurrentMobileButtonMoveCursor.Value);
            return;
			}

        bool left = ((!is3D || !Configuration.TurnWithArrowKeys) && keys[(int)Key.Left]) || ((!is3D || Configuration.Movement3D == Movement3D.WASDQE) && keys[(int)Key.A]);
        bool right = ((!is3D || !Configuration.TurnWithArrowKeys) && keys[(int)Key.Right]) || ((!is3D || Configuration.Movement3D == Movement3D.WASDQE) && keys[(int)Key.D]);
        bool up = keys[(int)Key.Up] || keys[(int)Key.W];
        bool down = keys[(int)Key.Down] || keys[(int)Key.S];
        bool turnLeft = (Configuration.TurnWithArrowKeys && keys[(int)Key.Left]) || (Configuration.Movement3D == Movement3D.WASDQE ? keys[(int)Key.Q] : keys[(int)Key.A]);
        bool turnRight = (Configuration.TurnWithArrowKeys && keys[(int)Key.Right]) || (Configuration.Movement3D == Movement3D.WASDQE ? keys[(int)Key.E] : keys[(int)Key.D]);

        if (left && !right)
        {
            if (!is3D)
            {
                // diagonal movement is handled in up/down
                if (!up && !down)
                    Move2D(-1, 0);
            }
            else if (CanPartyMove())
            {
                player3D.MoveLeft(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
        }
        else if (right && !left)
        {
            if (!is3D)
            {
                // diagonal movement is handled in up/down
                if (!up && !down)
                    Move2D(1, 0);
            }
				else if (CanPartyMove())
				{
                player3D.MoveRight(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
        }
        if (is3D)
        {
            if (turnLeft && !turnRight)
            {
                player3D.TurnLeft(movement.TurnSpeed3D);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
            else if (!turnLeft && turnRight)
            {
                player3D.TurnRight(movement.TurnSpeed3D);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
        }
        if (up && !down)
        {
            if (!is3D)
            {
                int x = left && !right ? -1 :
                    right && !left ? 1 : 0;
                Move2D(x, -1);
            }
				else if (CanPartyMove())
				{
                player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
        }
        else if (down && !up)
        {
            if (!is3D)
            {
                int x = left && !right ? -1 :
                    right && !left ? 1 : 0;
                Move2D(x, 1);
            }
				else if (CanPartyMove())
				{
                player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
            }
        }
		}

    internal void SpeakToParty()
    {
        var hero = GetPartyMember(0);

        if (!hero.Alive || !hero.Conditions.CanTalk())
        {
            ShowMessagePopup(DataNameProvider.UnableToTalk);
            return;
        }
        if (CurrentSavegame.ActivePartyMemberSlot != 0)
            SetActivePartyMember(0);

        Pause();
        layout.OpenTextPopup(ProcessText(DataNameProvider.WhoToTalkTo),
            null, true, false, false, TextAlign.Center);
        PickTargetPlayer();
        void TargetPlayerPicked(int characterSlot)
        {
            ResetMoveKeys(true);

            if (characterSlot != -1)
            {
                var partyMember = GetPartyMember(characterSlot);

                if (!partyMember.Alive || partyMember.Conditions.HasFlag(Condition.Petrified))
                {
                    ExecuteNextUpdateCycle(PickTargetPlayer);
                    return;
                }
            }

            this.TargetPlayerPicked -= TargetPlayerPicked;
            ClosePopup();
            UntrapMouse();
            InputEnable = true;
            if (!WindowActive)
                Resume();

            if (characterSlot != -1)
            {
                if (characterSlot == 0)
                    ExecuteNextUpdateCycle(() => ShowMessagePopup(DataNameProvider.SelfTalkingIsMad));
                else
                {
                    var partyMember = GetPartyMember(characterSlot);
                    ExecuteNextUpdateCycle(() => ShowConversation(partyMember, null, null, new ConversationItems()));
                }
            }
        }
        this.TargetPlayerPicked += TargetPlayerPicked;
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
        TargetPlayerPicked?.Invoke(characterSlot);
        pickingTargetPlayer = false;
        UntrapMouse();
    }

    internal void AbortPickingTargetPlayer()
    {
        pickingTargetPlayer = false;
        TargetPlayerPicked?.Invoke(-1);
        ClosePopup();
    }

    internal bool FinishPickingTargetInventory(int characterSlot)
    {
        bool result = TargetInventoryPicked?.Invoke(characterSlot) ?? true;

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

        if (TargetItemPicked?.Invoke(itemGrid, slotIndex, itemSlot) != false)
        {
            if (currentWindow.Window == Window.Inventory)
                CloseWindow();

            layout.ShowChestMessage(null);
            ClosePopup();
            UntrapMouse();
        }
    }

    internal void AbortPickingTargetInventory()
    {
        pickingTargetInventory = false;

        if (TargetInventoryPicked?.Invoke(-1) != false)
        {
            if (TargetItemPicked?.Invoke(null, 0, null) != false)
            {
                if (currentWindow.Window == Window.Inventory)
                    CloseWindow();

                layout.ShowChestMessage(null);
                ClosePopup();
                EndSequence();
                UntrapMouse();
            }
        }
    }

    public void OnKeyDown(Key key, KeyModifiers modifiers)
    {
#if DEBUG
        if (key == Key.F5 && modifiers == KeyModifiers.Control)
            System.Diagnostics.Debugger.Break();
#endif
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
            if (layout.PopupActive && !pickingNewLeader && !pickingTargetPlayer && !pickingTargetInventory)
            {
                layout.KeyDown(key, modifiers);
                return;
            }

            // In battle the space key can be used to click for next action.
            if (key == Key.Space && currentBattle?.WaitForClick == true)
            {
                if (!currentBattle.RoundActive && nextClickHandler != null)
                {
                    nextClickHandler(MouseButtons.Left);
                    nextClickHandler = null;
                }
                else
                    currentBattle.Click(CurrentBattleTicks);
                return;
            }

            if (key == Key.Escape && currentBattle?.WaitForClick == true && !currentBattle.RoundActive && nextClickHandler != null)
            {
                nextClickHandler(MouseButtons.Left);
                nextClickHandler = null;
                return;
            }

            if (key == Key.Return && layout.HasQuestionYesButton())
            {
                layout.KeyDown(Key.Return, KeyModifiers.None);
                return;
            }

            if (key != Key.Escape && !(key >= Key.Num1 && key <= Key.Num9))
                return;

            if (layout.TextWaitsForClick || currentBattle?.WaitForClick == true) // allow only if there is no text active which waits for a click
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
                layout.AbortPickingTargetInventory();
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
        if (!WindowActive && !PopupActive && key >= Key.Number0 && key <= Key.Number9 && modifiers.HasFlag(KeyModifiers.Control))
        {
            var saveGameId = key - Key.Number0;
            if (saveGameId == 0)
                saveGameId = 10;
            if (modifiers.HasFlag(KeyModifiers.Shift))
            {
                LoadGame(saveGameId, false, false, null, false, _ =>
                {
                    if (Configuration.ShowSaveLoadMessage)
                    {
                        ShowBriefMessagePopup(
                            string.Format(CustomTexts.GetText(GameLanguage, CustomTexts.Index.GameLoaded), saveGameId),
                            TimeSpan.FromMilliseconds(1500));
                    }
                });
            }
            else
            {
                string name = $"QuickSave{saveGameId}";
                SaveGame(saveGameId, name);
                if (Configuration.ShowSaveLoadMessage)
                {
                    ShowBriefMessagePopup(
                        string.Format(CustomTexts.GetText(GameLanguage, CustomTexts.Index.GameSaved), name),
                        TimeSpan.FromMilliseconds(1500));
                }
            }
        }
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
                    else if (layout.InventoryMessageWaitsForClick)
                    {
                        layout.ClickInventoryMessage();
                    }
                    else
                    {
                        if (layout.IsDragging)
                        {
                            layout.CancelDrag();
                            CursorType = CursorType.Sword;
                        }
                        else if (currentWindow.Window == Window.Automap)
                        {
                            nextClickHandler?.Invoke(MouseButtons.Right);
                            nextClickHandler = null;
                        }
                        else if (nextClickHandler != null && nextClickHandler?.Invoke(MouseButtons.Right) == true)
                        {
                            nextClickHandler = null;
                        }
                        else if (layout.OptionMenuOpen)
                        {
                            layout.PressButton(2, CurrentTicks);
                        }
                        else if (InputEnable)
                        {
                            if (currentWindow.Closable)
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
                        else
                        {
                            if (layout.HasQuestionNoButton())
                                layout.KeyDown(Key.Escape, KeyModifiers.None);
                            return;
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
                if (InputEnable && is3D && currentWindow.Window == Window.MapView && key == Key.Space)
                    TriggerMapEvents(null);
                else if (currentWindow.Window == Window.Automap && (key == Key.Space || key == Key.M))
                {
                    nextClickHandler?.Invoke(MouseButtons.Right);
                    nextClickHandler = null;
                }
                else if (WindowActive || layout.PopupActive)
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

            if (layout.TextWaitsForClick) // allow only if there is no text active which waits for a click
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
                bool immediately = CurrentWindow.Window == Window.MapView && index != 4;
                layout.ReleaseButton(column + row * 3, immediately);

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

            if (!keys[(int)Key.Num1 + slot])
            {
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
        }

        if (!pickingNewLeader && layout.KeyChar(keyChar))
            return;

        if (!InputEnable)
            return;

        if (!PopupActive && (keyChar >= '1' && keyChar <= '6'))
        {
            int slot = keyChar - '1';

            if (!keys[(int)Key.Num1 + slot])
                SetActivePartyMember(slot);
        }

        if (!WindowActive && !layout.PopupActive)
        {
            schnism += keyChar;

            if (!schnismEasterEgg.StartsWith(schnism.ToLower()))
                schnism = "";
            else if (schnism.ToLower() == schnismEasterEgg)
            {
                schnism = "";
                ShowMessagePopup(DataNameProvider.TurnOnTuneInAndDropOut, () =>
                    DamageAllPartyMembers(p => 0u, p => p.Alive, null, null, Condition.Drugged));
                return;
            }

            if (char.ToLower(keyChar) == 'm' && ingame && is3D)
                ShowAutomap();
        }
    }

    public void OnFingerDown(Position position)
    {
        fingerDown = true;
        lastMobileAutomapFingerPosition = renderView.ScreenToGame(position);
    }

    public void OnFingerUp(Position position)
    {
        fingerDown = false;
        lastMobileAutomapFingerPosition = renderView.ScreenToGame(position);

        if (!Configuration.IsMobile)
				return;

			keys[(int)Key.W] = false;
			keys[(int)Key.A] = false;
			keys[(int)Key.S] = false;
			keys[(int)Key.D] = false;

			CurrentMobileAction = MobileAction.None;
		}

    public void OnFingerMoveTo(Position position)
    {
        fingerDown = true;

        if (!Configuration.IsMobile)
            return;

        if (currentWindow.Window == Window.Automap)
        {
            position = renderView.ScreenToGame(position);
            var diff = position - lastMobileAutomapFingerPosition;
            lastMobileAutomapFingerPosition = position;
            mobileAutomapScroll.X -= 6.0f * diff.X / Global.VirtualScreenWidth;
            mobileAutomapScroll.Y -= 6.0f * diff.Y / Global.VirtualScreenHeight;
            return;
        }

        if (CurrentWindow.Window != Window.MapView)
            return;

        CurrentMobileButtonMoveCursor = null;

			if (CurrentMobileAction == MobileAction.Move)
        {
            // We just press the keys and let the move logic move the player in a timed manner.
            keys[(int)Key.W] = false;
            keys[(int)Key.A] = false;
            keys[(int)Key.S] = false;
            keys[(int)Key.D] = false;

            var relativePosition = renderView.ScreenToGame(position);

            if (is3D)
            {
                var center = mapViewArea.Center;

                if (relativePosition.X <= center.X - Mobile3DThreshold)
						keys[(int)Key.A] = true;
                else if (relativePosition.X >= center.X + Mobile3DThreshold)
						keys[(int)Key.D] = true;
					if (relativePosition.Y <= center.Y - Mobile3DThreshold)
						keys[(int)Key.W] = true;
					else if (relativePosition.Y >= center.Y + Mobile3DThreshold)
						keys[(int)Key.S] = true;
				}
            else
            {
					relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);
					var tilePosition = renderMap2D.PositionToTile(relativePosition);

                if (tilePosition != null)
                {
                    if (player2D.Position.X < tilePosition.X)
                        keys[(int)Key.D] = true;
                    else if (player2D.Position.X > tilePosition.X)
                        keys[(int)Key.A] = true;
                    if (player2D.Position.Y < tilePosition.Y)
                        keys[(int)Key.S] = true;
                    else if (player2D.Position.Y > tilePosition.Y)
                        keys[(int)Key.W] = true;
                }
            }
        }
        else if (CurrentMobileAction == MobileAction.ButtonMove)
        {
				var relativePosition = renderView.ScreenToGame(position);

				if (Global.ButtonGridArea.Contains(relativePosition))
				{
                var moveCursors = layout.GetMoveButtonCursorMapping();

					for (int i = 0; i < 9; i++)
					{
                    if (ButtonGrid.ButtonAreas[i].Contains(relativePosition))
                    {
                        if (i != 4)
                            CurrentMobileButtonMoveCursor = moveCursors[i];
							return;
                    }
					}
				}
			}
    }

    public void OnLongPress(Position position)
    {
        if (!Configuration.IsMobile)
            return;

        if (CurrentWindow.Window != Window.MapView)
        {
            OnMouseDown(position, MouseButtons.Right);
            OnMouseUp(position, MouseButtons.Right);
        }
        else
        {
            var relativePosition = renderView.ScreenToGame(position);

            if (!mapViewArea.Contains(relativePosition))
            {
                /*if (Global.UpperRightArea.Contains(relativePosition))
                {
                    if (!mobileMovementIndicatorEnabled)
                        MobileMovementIndicatorEnabled = true;
                    else if (!Global.MobileMovementIndicator.Contains(relativePosition))
                        MobileMovementIndicatorEnabled = false;
                }*/

                // If long press on an arrow button, we start button movement
                if (layout.ButtonGridPage == 0 && Global.ButtonGridArea.Contains(relativePosition))
                {
                    for (int i = 0; i < 9; i++)
                    {
                        if (i == 4)
                            continue;

                        if (ButtonGrid.ButtonAreas[i].Contains(relativePosition))
                        {
                            layout.PressButton(i, CurrentTicks);
                            return;
                        }
                    }
                }

					OnMouseDown(position, MouseButtons.Right);
                OnMouseUp(position, MouseButtons.Right);
                return;
            }

				relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);

            if (is3D)
            {
                if (!TriggerMapEvents(null))
                {
						if (CurrentMobileAction != MobileAction.None)
							return;

						CurrentMobileAction = MobileAction.Move;
                    var center = mapViewArea.Center;

                    if (relativePosition.X <= center.X - Mobile3DThreshold)
                        keys[(int)Key.A] = true;
                    else if (relativePosition.X >= center.X + Mobile3DThreshold)
                        keys[(int)Key.D] = true;
                    if (relativePosition.Y <= center.Y - Mobile3DThreshold)
                        keys[(int)Key.W] = true;
                    else if (relativePosition.Y >= center.Y + Mobile3DThreshold)
                        keys[(int)Key.S] = true;
                }
            }
            else
            {
                var tilePosition = renderMap2D.PositionToTile(relativePosition);

                if (tilePosition != null)
                {
                    uint tileX = (uint)tilePosition.X;
                    uint tileY = (uint)tilePosition.Y;

						var character = renderMap2D.GetCharacterFromTile(tileX, tileY);

						if (character?.IsConversationPartner == true)
						{
							int xDist = Math.Abs(player2D.Position.X - tilePosition.X);
							int yDist = Math.Abs(player2D.Position.Y - tilePosition.Y);

							if (xDist > 3 || yDist > 3)
							{
								ShowMessagePopup(GetCustomText(CustomTexts.Index.MobileTargetOutOfReach));
								return;
							}

							if (character.Interact(EventTrigger.Mouth, renderMap2D[tileX, tileY].Type == Map.TileType.Bed))
								return;
						}

						var @event = renderMap2D.GetEvent(tileX, tileY, CurrentSavegame);

                    void TriggerEvent(EventTrigger trigger)
                    {
                        int range = trigger == EventTrigger.Mouth ? 3 : 2;

                        int xDist = Math.Abs(player2D.Position.X - tilePosition.X);
                        int yDist = Math.Abs(player2D.Position.Y - tilePosition.Y);

                        if (xDist > range || yDist > range)
							{
								ShowMessagePopup(GetCustomText(CustomTexts.Index.MobileTargetOutOfReach));
								return;
							}

							var map = renderMap2D.GetMapFromTile(tileX, tileY);
                        map.TriggerEventChain(this, trigger, tileX % (uint)map.Width, tileY % (uint)map.Height, @event);
                    }

                    if (@event is ConditionEvent condition)
                    {
                        if (condition.TypeOfCondition == ConditionEvent.ConditionType.EnterNumber ||
                            condition.TypeOfCondition == ConditionEvent.ConditionType.Eye ||
                            condition.TypeOfCondition == ConditionEvent.ConditionType.Hand ||
                            condition.TypeOfCondition == ConditionEvent.ConditionType.Mouth ||
                            condition.TypeOfCondition == ConditionEvent.ConditionType.MultiCursor ||
                            condition.TypeOfCondition == ConditionEvent.ConditionType.SayWord)
                        {
                            EventTrigger GetMultiCursorTrigger()
                            {
                                var flags = condition.ObjectIndex;
                                if ((flags & 0x4) != 0)
                                    return EventTrigger.Mouth; // check and return this first as it has a higher range
                                if ((flags & 0x1) != 0)
                                    return EventTrigger.Hand;
                                if ((flags & 0x2) != 0)
                                    return EventTrigger.Eye;

                                return EventTrigger.Always;
                            }

                            var trigger = condition.TypeOfCondition switch
                            {
                                ConditionEvent.ConditionType.Eye => EventTrigger.Eye,
                                ConditionEvent.ConditionType.Hand => EventTrigger.Hand,
                                ConditionEvent.ConditionType.EnterNumber => EventTrigger.Hand,
                                ConditionEvent.ConditionType.MultiCursor => GetMultiCursorTrigger(),
                                _ => EventTrigger.Mouth
                            };

                            TriggerEvent(trigger);
                            return;
                        }
                    }
                    else if (@event is ChestEvent chestEvent)
                    {
                        if (!chestEvent.CloseWhenEmpty || chestEvent.NoSave)
                        {
                            TriggerEvent(EventTrigger.Eye);
                            return;
                        }
                        else
                        {
                            var chest = GetChest(chestEvent.RealChestIndex);

                            if (!chest.Empty)
                            {
                                TriggerEvent(EventTrigger.Eye);
                                return;
                            }
                        }
                    }
                    else if (@event is DoorEvent doorEvent)
                    {
                        if (CurrentSavegame.IsDoorLocked(doorEvent.Index))
                        {
                            TriggerEvent(EventTrigger.Eye);
                            return;
                        }
                    }

						if (CurrentMobileAction != MobileAction.None)
							return;

						CurrentMobileAction = MobileAction.Move;

                    if (player2D.Position.X < tilePosition.X)
                        keys[(int)Key.D] = true;
                    else if (player2D.Position.X > tilePosition.X)
                        keys[(int)Key.A] = true;
                    if (player2D.Position.Y < tilePosition.Y)
                        keys[(int)Key.S] = true;
                    else if (player2D.Position.Y > tilePosition.Y)
                        keys[(int)Key.W] = true;
                }
            }
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

    public void OnMouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers = KeyModifiers.None)
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

        // Special case to abort multiple monster start animations in battle
        if (ingame && allInputDisabled && !pickingNewLeader && currentWindow.Window == Window.Battle &&
            currentBattle?.StartAnimationPlaying == true && currentBattle.WaitForClick)
        {
            currentBattle.Click(CurrentBattleTicks);
            return;
        }

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
                        void PlayTurnSequence(int steps, Action turnAction)
                        {
                            PlayTimedSequence(steps, () =>
                            {
                                turnAction?.Invoke();
                                CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
                            }, 65);
                        }

                        switch (CursorType)
                        {
                            case CursorType.ArrowTurnLeft:
                                PlayTurnSequence(6, () => player3D.TurnLeft(15.0f));
                                return;
                            case CursorType.ArrowTurnRight:
                                PlayTurnSequence(6, () => player3D.TurnRight(15.0f));
                                return;
                            case CursorType.ArrowRotateLeft:
                                PlayTurnSequence(12, () => player3D.TurnLeft(15.0f));
                                return;
                            case CursorType.ArrowRotateRight:
                                PlayTurnSequence(12, () => player3D.TurnRight(15.0f));
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
                {
                    CurrentMobileAction = MobileAction.None;
                    TriggerMapEvents(EventTrigger.Eye, relativePosition);
                }
                else if (cursor.Type == CursorType.Hand)
                {
                    CurrentMobileAction = MobileAction.None;
                    TriggerMapEvents(EventTrigger.Hand, relativePosition);
                }
                else if (cursor.Type == CursorType.Mouth)
                {
						if (!TriggerMapEvents(EventTrigger.Mouth, relativePosition))
                    {
                        if (!is3D && player2D?.DisplayArea.Contains(mapViewArea.Position + relativePosition) == true)
                        {
								CurrentMobileAction = MobileAction.None;
								SpeakToParty();
                        }
                    }
                    else
                    {
							CurrentMobileAction = MobileAction.None;
						}
                }
                else if (cursor.Type == CursorType.Target && !is3D)
                {
						if (!TriggerMapEvents(EventTrigger.Mouth, relativePosition))
                    {
                        if (!TriggerMapEvents(EventTrigger.Eye, relativePosition))
                        {
                            if (TriggerMapEvents(EventTrigger.Hand, relativePosition))
									CurrentMobileAction = MobileAction.None;
							}
                        else
                        {
								CurrentMobileAction = MobileAction.None;
							}
                    }
                    else
                    {
							CurrentMobileAction = MobileAction.None;
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
                else if (Configuration.IsMobile)
                {
                    TriggerMapEvents(null);
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
                if (!pickingNewLeader && currentBattle != null && currentWindow.Window == Window.Battle)
                {
                    if (currentBattle.WaitForClick)
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
                layout.Click(relativePosition, buttons, ref cursorType, CurrentTicks, pickingNewLeader, pickingTargetPlayer, pickingTargetInventory, keyModifiers);
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

        bool yTargetRange = gamePosition.Y < playerArea.Top ? playerArea.Top - gamePosition.Y <= 3 * RenderMap2D.TILE_HEIGHT / 2
            : gamePosition.Y - playerArea.Bottom <= RenderMap2D.TILE_HEIGHT;
        int yDiff = gamePosition.Y < playerArea.Top ? playerArea.Top - gamePosition.Y : gamePosition.Y - playerArea.Bottom;

        if (xDiff <= RenderMap2D.TILE_WIDTH && yTargetRange)
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

    private void InputFocusChanged()
    {
        UpdateCursor();
        keyboardRequest?.Invoke(TextInput.FocusedInput != null, TextInput.FocusedInput?.Text ?? "");
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

                if (layout.OptionMenuOpen)
                {
                    if (!layout.PopupActive)
                        layout.HoverButtonGrid(renderView.ScreenToGame(cursorPosition));
                    else
                    {
                        var cursorType = cursor.Type;
                        layout.SaveListScrollDrag(renderView.ScreenToGame(cursorPosition), ref cursorType);
                        CursorType = cursorType;
                    }
                }

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
            characterCreator.OnMouseWheel(xScroll, yScroll, mousePosition, Configuration.IsMobile);
            return;
        }

        if (allInputDisabled)
            return;

			if (Configuration.IsMobile && currentWindow.Window == Window.Automap)
			{
            lastMobileAutomapFingerPosition = renderView.ScreenToGame(mousePosition);
            mobileAutomapScroll.X += xScroll * 4;
				mobileAutomapScroll.Y += yScroll * 4;
            return;
			}

			bool scrolled = false;

        if (xScroll != 0)
            scrolled = layout.ScrollX(xScroll < 0);
        if (yScroll != 0 && layout.ScrollY(Configuration.IsMobile ? yScroll > 0 : yScroll < 0))
            scrolled = true;

        if (scrolled)
        {
            mousePosition = GetMousePosition(mousePosition);
            UpdateCursor(mousePosition, MouseButtons.None);
        }
        else if (yScroll != 0 && !WindowActive && !layout.PopupActive && !is3D && !Configuration.IsMobile)
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
    public PartyMember GetPartyMember(int slot) => CurrentSavegame?.GetPartyMember(slot);
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

            if (Configuration.IsMobile)
            {
                int range = trigger == EventTrigger.Mouth ? 3 : 2;

                int xDist = Math.Abs(player2D.Position.X - tilePosition.X);
                int yDist = Math.Abs(player2D.Position.Y - tilePosition.Y);

                if (xDist > range || yDist > range)
                    return false;
            }

				return TriggerMapEvents(trigger, (uint)tilePosition.X, (uint)tilePosition.Y);
        }
    }

    internal bool TriggerMapEvents(EventTrigger trigger, uint x, uint y)
    {
        if (noEvents)
            return false;

        if (is3D)
        {
            return renderMap3D.TriggerEvents(this, trigger, x, y, CurrentSavegame);
        }
        else // 2D
        {
            return renderMap2D.TriggerEvents(player2D, trigger, x, y, MapManager,
                CurrentTicks, CurrentSavegame);
        }
    }

    internal bool TestUseItemMapEvent(uint itemIndex, out uint x, out uint y)
    {
        x = (uint)player.Position.X;
        y = (uint)player.Position.Y;
        uint eventX = x;
        uint eventY = y;
        var @event = is3D ? Map.GetEvent(x, y, CurrentSavegame) : renderMap2D.GetEvent(x, y, CurrentSavegame);
        var map = is3D ? Map : renderMap2D.GetMapFromTile(x, y);

        bool TestEvent()
        {
            if (@event is not ConditionEvent conditionEvent)
                return false;

            if (conditionEvent.TypeOfCondition == ConditionEvent.ConditionType.UseItem &&
                conditionEvent.ObjectIndex == itemIndex)
                return true;

            bool lastEventStatus = true;
            var trigger = (EventTrigger)((uint)EventTrigger.Item0 + itemIndex);
            
            @event = EventExtensions.ExecuteEvent(conditionEvent, map, this, ref trigger, eventX, eventY, ref lastEventStatus, out bool _, out var _);

            return TestEvent();
        }

        if (TestEvent())
            return true;

        var mapWidth = Map.IsWorldMap ? int.MaxValue : Map.Width;
        var mapHeight = Map.IsWorldMap ? int.MaxValue : Map.Height;

        if (is3D)
        {
            camera3D.GetForwardPosition(Global.DistancePerBlock, out float px, out float pz, false, false);
            var position = Geometry.Geometry.CameraToBlockPosition(Map, px, pz);

            if (position != player.Position &&
                position.X >= 0 && position.X < Map.Width &&
                position.Y >= 0 && position.Y < Map.Height &&
                renderMap3D.IsBlockingPlayer(position))
            {
                // Only check the forward position if it is blocking.
                // Sometimes use item events might be placed on walls etc.
                // Otherwise don't check the forward position as the
                // player can walk on the empty tile and use the item there.
                x = (uint)position.X;
                y = (uint)position.Y;
                @event = Map.GetEvent(x, y, CurrentSavegame);
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

        eventX = x;
        eventY = y;

        return TestEvent();
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

    public void UpdateCharacterStatus(PartyMember partyMember)
    {
        if (!ingame || layout == null || CurrentSavegame == null)
            return;

        layout.UpdateCharacterStatus(partyMember);
    }

    public void UpdateCharacters(Action finishAction, IEnumerable<PartyMember> partyMembers = null)
    {
        var queue = new Queue<PartyMember>(partyMembers ?? PartyMembers);

        void UpdateNext()
        {
            if (queue.Count == 0)
            {
                finishAction?.Invoke();
                return;
            }

            var partyMember = queue.Dequeue();

            if (partyMember == null)
            {
                UpdateNext();
                return;
            }

            var slot = SlotFromPartyMember(partyMember);

            if (slot == null)
            {
                UpdateNext();
                return;
            }

            layout.SetCharacter(slot.Value, partyMember, false, UpdateNext, false, true);
        }

        UpdateNext();
    }

    void UpdateMapName(Map map = null)
    {
        map ??= Map;
        string mapName = map.IsWorldMap
            ? DataNameProvider.GetWorldName(map.World)
            : map.Name;
        windowTitle.Text = ProcessText(mapName);
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

    void ShowMap(bool show, bool playMusic = true)
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
            ResetMoveKeys(true);
            UpdateLight();
            if (playMusic)
            {
                if (lastPlayedSong != null && lastPlayedSong != Song.BarBrawlin)
                    PlayMusic(lastPlayedSong.Value);
                else if (Map.UseTravelMusic)
                    PlayMusic(TravelType.TravelSong());
                else
                    PlayMusic(Song.Default);
            }
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
            renderView.GetLayer(Layer.Map3DBackgroundFog).Visible = show;
            renderView.GetLayer(Layer.Map3DCeiling).Visible = show;
            renderView.GetLayer(Layer.Map3D).Visible = show;
            renderView.GetLayer(Layer.Billboards3D).Visible = show;
        }
        else
        {
            if (show)
                layout.SetLayout(LayoutType.Map2D, movement.MovementTicks(false, Map.UseTravelTypes, TravelType));
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = show;
        }

        if (show)
        {
            layout.Reset();
            mapViewRightFillArea = layout.FillArea(new Rect(208, 49, 96, 80), GetUIColor(28), false);
            SetWindow(Window.MapView);

            foreach (var specialItem in EnumHelper.GetValues<SpecialItemPurpose>())
            {
                if (CurrentSavegame.IsSpecialItemActive(specialItem))
                    layout.AddSpecialItem(specialItem);
            }

            foreach (var activeSpell in EnumHelper.GetValues<ActiveSpellType>())
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
        currentBattle?.HideAllBattleFieldDamage();

        if (CurrentSavegame.CurrentPartyMemberIndices[slot] == 0)
            return false;

        var partyMember = GetPartyMember(slot);

        if (partyMember.InventoryInaccessible)
        {
            // Note: In original you can't access the player stats as well so
            // we do it the same way here even though the message is misleading.
            // This feature is now used in AA for mystic imitation spell.
            var oldActivePartyMember = CurrentPartyMember;
            CurrentPartyMember = partyMember; // Needed to display the right name here
            ShowMessagePopup(DataNameProvider.NotAllowingToLookIntoBackpack);
            CurrentPartyMember = oldActivePartyMember;
            return false;
        }

        bool switchedFromOtherPartyMember = CurrentInventory != null;
        bool canAccessInventory = !HasPartyMemberFled(partyMember) && partyMember.Conditions.CanOpenInventory();

        if (canAccessInventory && partyMember.Race == Race.Animal && layout.IsDragging)
        {
            var draggedItem = layout.GetDraggedItem();

            if (draggedItem == null) // gold or food
                canAccessInventory = false;
            else // for animals only allow if the item is usable by animals (fallback item index 1 is never usable as it is a condition item)
                canAccessInventory = (ItemManager?.GetItem(draggedItem?.Item?.Item?.ItemIndex ?? 1)?.Classes)?.HasFlag(ClassFlag.Animal) ?? false;
        }

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
            if (currentWindow.Window == Window.Automap)
            {
                currentWindow.Window = Window.Inventory;
                nextClickHandler?.Invoke(MouseButtons.Right);
                nextClickHandler = null;
                currentWindow.Window = Window.Automap;
            }

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
            ItemGrid equipmentGrid = null;
            equipmentGrid = ItemGrid.CreateEquipment(this, layout, slot, renderView, ItemManager,
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
                }, UnequipItem, layout.UseItem);
            var inventoryGrid = ItemGrid.CreateInventory(this, layout, slot, renderView, ItemManager,
                inventorySlotPositions, [.. partyMember.Inventory.Slots], EquipItem, layout.UseItem);
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
            var rightHandSlot = partyMember.Equipment.Slots[EquipmentSlot.RightHand];
            if (rightHandSlot != null && rightHandSlot.ItemIndex != 0)
            {
                var rightHandItem = ItemManager.GetItem(rightHandSlot.ItemIndex);

                if (rightHandItem.NumberOfHands == 2)
                {
                    var leftHandSlot = partyMember.Equipment.Slots[EquipmentSlot.LeftHand];

                    if (leftHandSlot == null)
                        leftHandSlot = new ItemSlot { Amount = 1, ItemIndex = 0 };
                    else if (leftHandSlot.Empty)
                        leftHandSlot.Amount = 1;
                }
            }
            void UpdateOccupiedHandsAndFingers()
            {
                CurrentInventory.NumberOfOccupiedHands = 0;
                CurrentInventory.NumberOfOccupiedFingers = 0;

                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.RightHand].Empty)
                    CurrentInventory.NumberOfOccupiedHands++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.LeftHand].Empty)
                    CurrentInventory.NumberOfOccupiedHands++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.RightFinger].Empty)
                    CurrentInventory.NumberOfOccupiedFingers++;
                if (!CurrentInventory.Equipment.Slots[EquipmentSlot.LeftFinger].Empty)
                    CurrentInventory.NumberOfOccupiedFingers++;
            }
            void EquipItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                if (itemSlot.Empty)
                    return;

                if (itemSlot.ItemIndex == 0)
                {
                    if (slot != (int)EquipmentSlot.LeftHand - 1 || itemGrid.GetItemSlot(3).Empty)
                        return;

                    slot -= 2; // used on two-handed secondary hand slot -> switch to primary hand slot
                }

                var targetSlot = layout.TryEquipmentDrop(itemSlot);

                if (targetSlot != null)
                {
                    var equipGrid = layout.GetEquipmentGrid();
                    var targetItemSlot = equipGrid.GetItemSlot(targetSlot.Value);


                    if (itemSlot.Amount > 1)
                    {
                        // Allow equipping arrows (but only if the slot is free)
                        if (targetSlot != (int)EquipmentSlot.LeftHand - 1 || !CurrentInventory.Equipment.Slots[EquipmentSlot.LeftHand].Empty)
                            return;

                        itemSlot.Remove(1);
                        targetItemSlot.ItemIndex = itemSlot.ItemIndex;
                        targetItemSlot.Amount = 1;
                        CurrentInventory.NumberOfOccupiedHands++;
                    }
                    else
                    {
                        targetItemSlot.Exchange(itemSlot);
                    }
                    RemoveInventoryItem(slot, targetItemSlot, targetItemSlot.Amount);
                    equipGrid.SetItem(targetSlot.Value, targetItemSlot);
                    itemGrid.SetItem(slot, itemSlot);
                    AddEquipment(targetSlot.Value, targetItemSlot, targetItemSlot.Amount);

                    if (itemSlot.Amount != 0 && itemSlot.ItemIndex != 0)
                    {
                        RemoveEquipment(targetSlot.Value, itemSlot, 1);
                        AddInventoryItem(slot, itemSlot, 1);
                        RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(targetSlot.Value + 1), ItemManager.GetItem(itemSlot.ItemIndex));
                    }

                    UpdateOccupiedHandsAndFingers();
                }
            }
            void UnequipItem(ItemGrid itemGrid, int slot, ItemSlot itemSlot)
            {
                var inventoryGrid = layout.GetInventoryGrid();
                int targetSlot = -1;

                if (ItemManager.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
                {
                    for (int i = 0; i < inventoryGrid.SlotCount; ++i)
                    {
                        var inventorySlot = inventoryGrid.GetItemSlot(i);

                        if (inventorySlot.ItemIndex == itemSlot.ItemIndex && inventorySlot.Amount + itemSlot.Amount <= 99)
                        {
                            targetSlot = i;
                            break;
                        }
                    }
                }

                if (targetSlot == -1)
                {
                    for (int i = 0; i < inventoryGrid.SlotCount; ++i)
                    {
                        var inventorySlot = inventoryGrid.GetItemSlot(i);

                        if (inventorySlot.Empty)
                        {
                            targetSlot = i;
                            break;
                        }
                    }
                }

                if (targetSlot == -1)
                    return;

                RemoveEquipment(slot, itemSlot, itemSlot.Amount, true);
                AddInventoryItem(targetSlot, itemSlot, itemSlot.Amount);

                var targetItemSlot = inventoryGrid.GetItemSlot(targetSlot);
                targetItemSlot.Add(itemSlot);
                itemSlot.Clear();

                inventoryGrid.SetItem(targetSlot, targetItemSlot);
                itemGrid.SetItem(slot, itemSlot);

                RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slot + 1), ItemManager.GetItem(targetItemSlot.ItemIndex));
                UpdateOccupiedHandsAndFingers();
            }
            layout.AddItemGrid(equipmentGrid);
            foreach (var equipmentSlot in EnumHelper.GetValues<EquipmentSlot>().Skip(1))
            {
                if (!partyMember.Equipment.Slots[equipmentSlot].Empty)
                {
                    if (equipmentSlot != EquipmentSlot.LeftHand &&
                        partyMember.Equipment.Slots[equipmentSlot].ItemIndex == 0) // Item index 0 but amount is not 0 -> only allowed for left hand
                        partyMember.Equipment.Slots[equipmentSlot].Amount = 0;

                    equipmentGrid.SetItem((int)equipmentSlot - 1, partyMember.Equipment.Slots[equipmentSlot]);
                }
            }
            void RemoveEquipment(int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot = true)
            {
                RecheckUsedBattleItem(CurrentInventoryIndex.Value, slotIndex, true);
                var item = ItemManager.GetItem(itemSlot.ItemIndex);
                EquipmentRemoved(item, amount, itemSlot.Flags.HasFlag(ItemSlotFlags.Cursed));

                if (updateSlot)
                {
                    if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                    {
                        equipmentGrid.SetItem(slotIndex + 2, null);
                        partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Clear();
                    }
                }

                UpdateCharacterInfo();
                layout.FillCharacterBars(partyMember);
            }
            void AddEquipment(int slotIndex, ItemSlot itemSlot, int amount)
            {
                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                if (item.Flags.HasFlag(ItemFlags.Accursed))
                    itemSlot.Flags |= ItemSlotFlags.Cursed;

                EquipmentAdded(item, amount);

                if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                {
                    var secondHandItemSlot = new ItemSlot { ItemIndex = 0, Amount = 1 };
                    equipmentGrid.SetItem((int)EquipmentSlot.LeftHand - 1, secondHandItemSlot);
                    partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Replace(secondHandItemSlot);
                }

                UpdateCharacterInfo();
                layout.FillCharacterBars(partyMember);
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
            equipmentGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
            {
                RemoveEquipment(slotIndex, itemSlot, amount, updateSlot);
                if (updateSlot)
                {
                    partyMember.Equipment.Slots[(EquipmentSlot)(slotIndex + 1)].Remove(amount);
                    if (CurrentWindow.Window == Window.Inventory)
                    {
                        layout.UpdateLayoutButtons();
                        UpdateCharacterInfo();
                    }
                }
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
            inventoryGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
            {
                RemoveInventoryItem(slotIndex, itemSlot, amount);
                if (updateSlot)
                {
                    partyMember.Inventory.Slots[slotIndex].Remove(amount);
                    if (CurrentWindow.Window == Window.Inventory)
                        layout.UpdateLayoutButtons();
                }
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

            void AddTooltip(Rect area, string tooltip)
            {
                layout.AddTooltip(area, tooltip, TextColor.White, TextAlign.Left, new Render.Color(GetPrimaryUIColor(15), 0xb0));
            }

            #region Character info
            DisplayCharacterInfo(partyMember, false);
            #endregion
            #region Attributes
            layout.AddText(new Rect(22, 50, 72, Global.GlyphLineHeight), DataNameProvider.AttributesHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var attribute in EnumHelper.GetValues<Attribute>())
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
                        (attributeValues.TotalCurrentValue + bonus > 999 ? "***" : $"{attributeValues.TotalCurrentValue + bonus:000}") + $"/{attributeValues.MaxValue:000}");
                }
                else
                {
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeShortName(attribute));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}");
                }
                if (Configuration.ShowPlayerStatsTooltips)
                    AddTooltip(new Rect(22, y, 72, Global.GlyphLineHeight), GetAttributeTooltip(GameLanguage, attribute, partyMember));
            }
            #endregion
            #region Skills
            layout.AddText(new Rect(22, 115, 72, Global.GlyphLineHeight), DataNameProvider.SkillsHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                int y = 122 + index++ * Global.GlyphLineHeight;
                var skillValues = partyMember.Skills[skill];
                var current = skillValues.TotalCurrentValue;

                if (skill == Skill.Searching && Features.HasFlag(Features.ClairvoyanceGrantsSearchSkill) &&
                    CurrentSavegame.IsSpellActive(ActiveSpellType.Clairvoyance))
                {
                    uint bonus = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Clairvoyance);
                    void AddAnimatedText(Rect area, string text)
                    {
                        this.AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align), area, text, TextAlign.Left,
                            () => CurrentWindow.Window == Window.Stats, 100, true);
                    }
                    AddAnimatedText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetSkillShortName(skill));
                    AddAnimatedText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (skillValues.TotalCurrentValue + bonus > 99 ? "**" : $"{skillValues.TotalCurrentValue + bonus:00}") + $"%/{skillValues.MaxValue:00}%");
                }
                else
                {
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetSkillShortName(skill));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (skillValues.TotalCurrentValue > 99 ? "**" : $"{skillValues.TotalCurrentValue:00}") + $"%/{skillValues.MaxValue:00}%");
                }

					if (Configuration.ShowPlayerStatsTooltips)
						AddTooltip(new Rect(22, y, 72, Global.GlyphLineHeight), GetSkillTooltip(GameLanguage, skill, partyMember));
				}
            #endregion
            #region Languages
            layout.AddText(new Rect(106, 50, 72, Global.GlyphLineHeight), DataNameProvider.LanguagesHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            foreach (var language in EnumHelper.GetValues<Language>().Skip(1)) // skip Language.None
            {
                int y = 57 + index++ * Global.GlyphLineHeight;
                bool learned = partyMember.SpokenLanguages.HasFlag(language);
                if (learned)
                    layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), DataNameProvider.GetLanguageName(language));
            }
            if (renderView.GameData.Advanced)
            {
                foreach (var extendedLanguage in EnumHelper.GetValues<ExtendedLanguage>().Skip(1)) // skip ExtendedLanguage.None
                {
                    int y = 57 + index++ * Global.GlyphLineHeight;
                    bool learned = partyMember.SpokenExtendedLanguages.HasFlag(extendedLanguage);
                    if (learned)
                    {
                        string name = DataNameProvider.GetExtendedLanguageName(extendedLanguage);

                        if (string.IsNullOrWhiteSpace(name))
                            index--;
                        else
                            layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), name);
                }
            }
            #endregion
            #region Conditions
            layout.AddText(new Rect(106, 115, 72, Global.GlyphLineHeight), DataNameProvider.ConditionsHeaderString, TextColor.LightGreen, TextAlign.Center);
            index = 0;
            // Total space is 80 pixels wide. Each condition icon is 16 pixels wide. So there is space for 5 condition icons per line.
            const int conditionsPerRow = 5;
            foreach (var condition in partyMember.VisibleConditions)
            {
                if (condition == Condition.DeadAshes || condition == Condition.DeadDust)
                    continue;

                if (condition != Condition.DeadCorpse && !partyMember.Conditions.HasFlag(condition))
                    continue;

                int column = index % conditionsPerRow;
                int row = index / conditionsPerRow;
                ++index;

                int x = 96 + column * 16;
                int y = 124 + row * 17;
                var area = new Rect(x, y, 16, 16);
                string conditionName = DataNameProvider.GetConditionName(condition);
                string tooltip = Configuration.ShowPlayerStatsTooltips ? null : conditionName;
                layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetConditionGraphicIndex(condition), UIPaletteIndex,
                    2, tooltip, condition == Condition.DeadCorpse ? TextColor.DeadPartyMember : TextColor.ActivePartyMember);
                if (Configuration.ShowPlayerStatsTooltips)
                {
                    var tooltipCondition = condition;
                    if (tooltipCondition == Condition.DeadCorpse)
                    {
                        if (partyMember.Conditions.HasFlag(Condition.DeadDust))
                            tooltipCondition = Condition.DeadDust;
                        else if (partyMember.Conditions.HasFlag(Condition.DeadAshes))
                            tooltipCondition = Condition.DeadAshes;
                    }
                    AddTooltip(new Rect(x, y, 16, 16), conditionName + "^^" + GetConditionTooltip(GameLanguage, tooltipCondition, partyMember));
                }
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
            closeWindowHandler?.Invoke(false);
            closeWindowHandler = null;

            Fade(() =>
            {
                openAction();
                openedAction?.Invoke();
            }, changeInputEnableStateWhileFading);
        }

        return true;
    }

    Tooltip ShowSecondaryStatTooltip(Rect area, SecondaryStat secondaryStat, Character character)
    {
        if (character is PartyMember partyMember && Configuration.ShowPlayerStatsTooltips)
        {
            var tooltip = GetSecondaryStatTooltip(Features, GameLanguage, secondaryStat, partyMember);
            return layout.AddTooltip(area, tooltip, TextColor.White, TextAlign.Left, new Render.Color(GetPrimaryUIColor(15), 0xb0));
        }

        return null;
    }

    void UpdateSecondaryStatTooltip(Tooltip tooltip, SecondaryStat secondaryStat, Character character)
    {
        if (secondaryStat == SecondaryStat.EP50 && character.Level < 50)
            secondaryStat = SecondaryStat.EPPre50;
        else if (secondaryStat == SecondaryStat.LevelWithAPRIncrease && character.AttacksPerRoundIncreaseLevels == 0)
            secondaryStat = SecondaryStat.LevelWithoutAPRIncrease;

        if (tooltip != null && character is PartyMember partyMember && Configuration.ShowPlayerStatsTooltips)
            tooltip.Text = GetSecondaryStatTooltip(Features, GameLanguage, secondaryStat, partyMember);
    }

    public int AdjustAttackForNotUsedAmmunition(Character character, int attack)
    {
        var leftHandItemSlot = character.Equipment.Slots[EquipmentSlot.LeftHand];

        if (leftHandItemSlot != null && leftHandItemSlot.ItemIndex != 0 && leftHandItemSlot.Amount != 0)
        {
            var leftHandItem = ItemManager.GetItem(leftHandItemSlot.ItemIndex);

            if (leftHandItem.Type == ItemType.Ammunition && leftHandItem.Damage != 0)
            {
                var rightHandItemSlot = character.Equipment.Slots[EquipmentSlot.RightHand];

                if (rightHandItemSlot == null || rightHandItemSlot.ItemIndex == 0 || rightHandItemSlot.Amount == 0)
                    return attack - leftHandItem.Damage;

                var rightHandItem = ItemManager.GetItem(rightHandItemSlot.ItemIndex);

                if (rightHandItem.UsedAmmunitionType != leftHandItem.AmmunitionType)
                    return attack - leftHandItem.Damage;
            }
        }

        return attack;
    }

    void DisplayCharacterInfo(Character character, bool conversation)
    {
        void SetupSecondaryStatTooltip(Rect area, SecondaryStat secondaryStat)
        {
            characterInfoStatTooltips[secondaryStat] = ShowSecondaryStatTooltip(area, secondaryStat, character);
        }

        int offsetY = conversation ? -6 : 0;

        characterInfoTexts.Clear();
        characterInfoPanels.Clear();
        characterInfoStatTooltips.Clear();
        layout.FillArea(new Rect(208, offsetY + 49, 96, 80), GetUIColor(28), false);
        layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground, CustomGraphicPaletteIndex, 1);
        layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.PortraitOffset + character.PortraitIndex - 1, PrimaryUIPaletteIndex, 2);
        if (!string.IsNullOrEmpty(DataNameProvider.GetRaceName(character.Race)))
            layout.AddText(new Rect(242, offsetY + 49, 62, 7), DataNameProvider.GetRaceName(character.Race));
        layout.AddText(new Rect(242, offsetY + 56, 62, 7), DataNameProvider.GetGenderName(character.Gender));
        var area = new Rect(242, offsetY + 63, 62, 7);
        characterInfoTexts.Add(CharacterInfo.Age, layout.AddText(area,
            string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
            character.Attributes[Attribute.Age].CurrentValue)));
        SetupSecondaryStatTooltip(area, SecondaryStat.Age);
        if (character.Class < Class.Monster && !string.IsNullOrEmpty(DataNameProvider.GetClassName(character.Class)))
        {
            if (!conversation || character.Class < Class.Animal)
            {
                characterInfoTexts.Add(CharacterInfo.Level, layout.AddText(new Rect(242, offsetY + 70, 62, 7),
                    $"{DataNameProvider.GetClassName(character.Class)} {character.Level}"));
                characterInfoStatTooltips[SecondaryStat.LevelWithAPRIncrease] = ShowSecondaryStatTooltip(new Rect(242, offsetY + 70, 62, 7),
                    character.AttacksPerRoundIncreaseLevels == 0 ? SecondaryStat.LevelWithoutAPRIncrease : SecondaryStat.LevelWithAPRIncrease, character);
            }
        }
        layout.AddText(new Rect(208, offsetY + 84, 96, 7), character.Name, conversation ? TextColor.PartyMember : TextColor.ActivePartyMember, TextAlign.Center);
        if (!conversation)
        {
            bool magicClass = character.Class.IsMagic();

            if (character.Class != Class.Animal)
            {
                area = new Rect(242, 77, 62, 7);
                characterInfoTexts.Add(CharacterInfo.EP, layout.AddText(area,
                    string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                    character.ExperiencePoints)));
                characterInfoStatTooltips[SecondaryStat.EP50] = ShowSecondaryStatTooltip(area, character.Level < 50 ?
                    SecondaryStat.EPPre50 : SecondaryStat.EP50, character);
            }
            area = new Rect(208, 92, 96, 7);
            characterInfoTexts.Add(CharacterInfo.LP, layout.AddText(area,
                string.Format(DataNameProvider.CharacterInfoHitPointsString,
                Math.Min(character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue), character.HitPoints.TotalMaxValue),
                TextColor.White, TextAlign.Center));
            SetupSecondaryStatTooltip(area, SecondaryStat.LP);
            if (magicClass)
            {
                area = new Rect(208, 99, 96, 7);
                characterInfoTexts.Add(CharacterInfo.SP, layout.AddText(area,
                    string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                    Math.Min(character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue), character.SpellPoints.TotalMaxValue),
                    TextColor.White, TextAlign.Center));
                SetupSecondaryStatTooltip(area, SecondaryStat.SP);
            }
            characterInfoTexts.Add(CharacterInfo.SLPAndTP, layout.AddText(new Rect(208, 106, 96, 7),
                (magicClass ? string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) : new string(' ', 7)) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints), TextColor.White, TextAlign.Center));
            if (magicClass)
                SetupSecondaryStatTooltip(new Rect(214, 106, 42, 7), SecondaryStat.SLP);
            SetupSecondaryStatTooltip(new Rect(262, 106, 36, 7), SecondaryStat.TP);
            var displayGold = OpenStorage is IPlace ? 0 : character.Gold;
            characterInfoTexts.Add(CharacterInfo.GoldAndFood, layout.AddText(new Rect(208, 113, 96, 7),
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, displayGold, character.Food),
                TextColor.White, TextAlign.Center));
            SetupSecondaryStatTooltip(new Rect(214, 113, 42, 7), SecondaryStat.Gold);
            SetupSecondaryStatTooltip(new Rect(262, 113, 36, 7), SecondaryStat.Food);
            layout.AddSprite(new Rect(214, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), UIPaletteIndex);
            int attack = character.BaseAttackDamage + AdjustAttackForNotUsedAmmunition(character, character.BonusAttackDamage) + (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25;
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Attack))
            {
                if (attack > 0)
                    attack = (attack * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
                string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack));
                characterInfoTexts.Add(CharacterInfo.Attack, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                    new Rect(220, 122, 30, 7), attackString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
            }
            else
            {
                string attackString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack));
                characterInfoTexts.Add(CharacterInfo.Attack, layout.AddText(new Rect(220, 122, 30, 7), attackString, TextColor.White, TextAlign.Left));
            }
            SetupSecondaryStatTooltip(new Rect(214, 120, 36, 9), SecondaryStat.Damage);
            layout.AddSprite(new Rect(261, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), UIPaletteIndex);
            int defense = character.BaseDefense + character.BonusDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
            {
                if (defense > 0)
                    defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
                string defenseString = string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense));
                characterInfoTexts.Add(CharacterInfo.Defense, AddAnimatedText((area, text, color, align) => layout.AddText(area, text, color, align),
                    new Rect(268, 122, 30, 7), defenseString, TextAlign.Left, () => CurrentWindow.Window == Window.Inventory, 100, true));
            }
            else
            {
                string defenseString = string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense));
                characterInfoTexts.Add(CharacterInfo.Defense, layout.AddText(new Rect(268, 122, 30, 7), defenseString, TextColor.White, TextAlign.Left));
            }
            SetupSecondaryStatTooltip(new Rect(261, 120, 37, 9), SecondaryStat.Defense);
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
        bool magicClass = character.Class.IsMagic();

        void UpdateSecondaryStatTooltip(SecondaryStat secondaryStat)
        {
            if (Configuration.ShowPlayerStatsTooltips && characterInfoStatTooltips.TryGetValue(secondaryStat, out var toolip) && toolip != null)
                this.UpdateSecondaryStatTooltip(toolip, secondaryStat, character);
        }

        UpdateText(CharacterInfo.Age, () => string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
            character.Attributes[Attribute.Age].CurrentValue));
        UpdateSecondaryStatTooltip(SecondaryStat.Age);
        UpdateText(CharacterInfo.Level, () => $"{DataNameProvider.GetClassName(character.Class)} {character.Level}");
        UpdateSecondaryStatTooltip(SecondaryStat.LevelWithAPRIncrease);
        UpdateText(CharacterInfo.EP, () => string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
            character.ExperiencePoints));
        UpdateSecondaryStatTooltip(SecondaryStat.EP50);
        UpdateText(CharacterInfo.LP, () => string.Format(DataNameProvider.CharacterInfoHitPointsString,
            character.HitPoints.CurrentValue, character.HitPoints.TotalMaxValue));
        UpdateSecondaryStatTooltip(SecondaryStat.LP);
        if (magicClass)
        {
            UpdateText(CharacterInfo.SP, () => string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                character.SpellPoints.CurrentValue, character.SpellPoints.TotalMaxValue));
            UpdateSecondaryStatTooltip(SecondaryStat.SP);
            UpdateSecondaryStatTooltip(SecondaryStat.SLP);
        }
        UpdateText(CharacterInfo.SLPAndTP, () =>
            (magicClass ? string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) : new string(' ', 7)) + " " +
            string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints));
        UpdateSecondaryStatTooltip(SecondaryStat.TP);
        UpdateText(CharacterInfo.GoldAndFood, () =>
            string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, character.Gold, character.Food));
        UpdateSecondaryStatTooltip(SecondaryStat.Gold);
        UpdateSecondaryStatTooltip(SecondaryStat.Food);
        int attack = character.BaseAttackDamage + AdjustAttackForNotUsedAmmunition(character, character.BonusAttackDamage) + (int)character.Attributes[Attribute.Strength].TotalCurrentValue / 25;
        if (CurrentSavegame.IsSpellActive(ActiveSpellType.Attack))
        {
            if (attack > 0)
                attack = (attack * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Attack))) / 100;
            UpdateText(CharacterInfo.Attack, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack)));
        }
        else
        {
            UpdateText(CharacterInfo.Attack, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', attack < 0 ? '-' : '+'), Math.Abs(attack)));
        }
        UpdateSecondaryStatTooltip(SecondaryStat.Damage);
        int defense = character.BaseDefense + character.BonusDefense + (int)character.Attributes[Attribute.Stamina].TotalCurrentValue / 25;
        if (CurrentSavegame.IsSpellActive(ActiveSpellType.Protection))
        {
            if (defense > 0)
                defense = (defense * (100 + (int)CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Protection))) / 100;
            UpdateText(CharacterInfo.Defense, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)));
        }
        else
        {
            UpdateText(CharacterInfo.Defense, () =>
                string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', defense < 0 ? '-' : '+'), Math.Abs(defense)));
        }
        UpdateSecondaryStatTooltip(SecondaryStat.Defense);
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

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void InventoryItemAdded(uint itemIndex, int amount, PartyMember partyMember)
    {
        InventoryItemAdded(ItemManager.GetItem(itemIndex), amount, partyMember);
    }

    void InventoryItemRemoved(Item item, int amount, PartyMember partyMember = null)
    {
        partyMember ??= CurrentInventory;

        partyMember.TotalWeight -= (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void InventoryItemRemoved(uint itemIndex, int amount, PartyMember partyMember = null)
    {
        InventoryItemRemoved(ItemManager.GetItem(itemIndex), amount, partyMember);
    }

    void EquipmentAdded(Item item, int amount, Character character = null)
    {
        bool cursed = item.Flags.HasFlag(ItemFlags.Accursed);

        character ??= CurrentInventory;

        // Note: amount is only used for ammunition. The weight is
        // influenced by the amount but not the damage/defense etc.
        character.BonusAttackDamage = (short)(character.BonusAttackDamage + (cursed ? -1 : 1) * item.Damage);
        character.BonusDefense = (short)(character.BonusDefense + (cursed ? -1 : 1) * item.Defense);
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
        if (item.Skill != null)
            character.Skills[item.Skill.Value].BonusValue += (cursed ? -1 : 1) * item.SkillValue;
        if (item.SkillPenalty1Value != 0)
            character.Skills[item.SkillPenalty1].BonusValue -= (int)item.SkillPenalty1Value;
        if (item.SkillPenalty2Value != 0)
            character.Skills[item.SkillPenalty2].BonusValue -= (int)item.SkillPenalty2Value;
        character.TotalWeight += (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
    }

    internal void EquipmentAdded(uint itemIndex, int amount, Character character)
    {
        EquipmentAdded(ItemManager.GetItem(itemIndex), amount, character);
    }

    void EquipmentRemoved(Character character, Item item, int amount, bool cursed)
    {
        // Note: amount is only used for ammunition. The weight is
        // influenced by the amount but not the damage/defense etc.
        character.BonusAttackDamage = (short)(character.BonusAttackDamage - (cursed ? -1 : 1) * item.Damage);
        character.BonusDefense = (short)(character.BonusDefense - (cursed ? -1 : 1) * item.Defense);
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
        if (item.Skill != null)
            character.Skills[item.Skill.Value].BonusValue -= (cursed ? -1 : 1) * item.SkillValue;
        if (item.SkillPenalty1Value != 0)
            character.Skills[item.SkillPenalty1].BonusValue += (int)item.SkillPenalty1Value;
        if (item.SkillPenalty2Value != 0)
            character.Skills[item.SkillPenalty2].BonusValue += (int)item.SkillPenalty2Value;
        character.TotalWeight -= (uint)amount * item.Weight;

        if (CurrentWindow.Window == Window.Inventory)
            layout.UpdateLayoutButtons();
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

    float BattleTimeFactor => currentBattle != null && Configuration.BattleSpeed != 0 && currentWindow.Window == Window.Battle
        ? 1.0f + Configuration.BattleSpeed / 33.0f : 1.0f;

    internal void AddTimedEvent(TimeSpan delay, Action action) => AddTimedEvent(delay, action, false);

    internal void AddTimedEvent(TimeSpan delay, Action action, bool ignoreFastBattleMode)
    {
        if (!ignoreFastBattleMode && currentBattle != null && Configuration.BattleSpeed != 0 && currentWindow.Window == Window.Battle)
            delay = TimeSpan.FromMilliseconds(Math.Max(1.0, delay.TotalMilliseconds / BattleTimeFactor));

        timedEvents.Add(new TimedGameEvent
        {
            ExecutionTime = DateTime.Now + delay,
            Action = action
        });
    }

    internal void SetClickHandler(Action action)
    {
        nextClickHandler = _ => { action?.Invoke(); return true; };
    }

    static readonly float[] ShakeOffsetFactors = new float[]
    {
        -0.5f, 0.0f, 1.0f, 0.5f, -1.0f, 0.0f, 0.5f
    };

    internal void ShakeScreen(TimeSpan durationPerShake, int numShakes, int pixelAmplitude)
        => ShakeScreen(durationPerShake, numShakes, (float)pixelAmplitude / Global.VirtualScreenHeight);

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

    static float FadeAlphaToLight(float alpha)
    {
        // 3D shaders use: outColor = vec4(pixelColor.rgb + vec3(light) - vec3(1), pixelColor.a);
        // Or in short: result = color + light - 1.0f
        // This means that darker colors are faster darkened and lighter colors are almost linear.
        // A linear light factor change will darken too quickly and lighten too slowly.
        // We want fade alpha which is: result = color * alpha

        // But light must increase faster than alpha. As both values are smaller than 1, we can
        // just use the square root here.
        return (float)Math.Sqrt(alpha);
		}

    void Set3DLight(float fade)
    {
        renderView.Set3DFade(fade);
        // TODO: ceiling/floor color
    }

		void Fade3DMapOut(int totalSteps, int timePerStep)
		{
        float div = totalSteps;

        for (int i = 0; i <= totalSteps; i++)
        {
            float light = FadeAlphaToLight(1.0f - i / div);
				AddTimedEvent(TimeSpan.FromMilliseconds(i * timePerStep), () =>
                Set3DLight(light));
			}
		}

		void Fade3DMapIn(int totalSteps, int timePerStep)
    {
        float div = totalSteps;

			for (int i = 0; i <= totalSteps; i++)
			{
				float light = FadeAlphaToLight(i / div);
				AddTimedEvent(TimeSpan.FromMilliseconds(i * timePerStep), () =>
                Set3DLight(light));
			}
		}

    void Fade(Action midFadeAction, bool changeInputEnableState = true)
    {
        Fading = true;
        if (changeInputEnableState)
            allInputDisabled = true;
			layout.AddFadeEffect(new Rect(0, 36, Global.VirtualScreenWidth, Global.VirtualScreenHeight - 36), Render.Color.Black, FadeEffectType.FadeInAndOut, FadeTime);
        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime / 2), () =>
        {
            midFadeAction?.Invoke();

				if (currentWindow.Window == Window.MapView && is3D)
					Fade3DMapIn(20, FadeTime / 40);
			});
        if (changeInputEnableState)
            AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), () => allInputDisabled = false);
        AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime + 1), () => Fading = false);

        if (currentWindow.Window == Window.MapView && is3D)
            Fade3DMapOut(10, FadeTime / 40);
    }

    internal void DamageAllPartyMembers(Func<PartyMember, uint> damageProvider, Func<PartyMember, bool> affectChecker = null,
        Action<PartyMember, Action> notAffectedHandler = null, Action<bool> followAction = null, Condition inflictCondition = Condition.None,
        bool showDamageSplash = true)
    {
        // In original all players are damaged one after the other
        // without showing the damage splash immediately. If a character
        // dies the skull is shown. If this was the active character
        // the "new leader" logic kicks in. Only after that the next
        // party member is checked.
        // At the end all affected living characters will show the damage splash.
        List<PartyMember> damagedPlayers = new List<PartyMember>();
        ForeachPartyMember(Damage, p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified), () =>
        {
            if (showDamageSplash)
            {
                ForeachPartyMember(ShowDamageSplash, p => damagedPlayers.Contains(p), () =>
                {
                    layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
                    followAction?.Invoke(damagedPlayers.Any(player => !player.Alive));
                });
            }
            else
            {
                layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
                followAction?.Invoke(damagedPlayers.Any(player => !player.Alive));
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

            if (damage > 0 || inflictCondition != Condition.None)
            {
                partyMember.Damage(damage, _ => KillPartyMember(partyMember, Condition.DeadCorpse));

                if (!Godmode && partyMember.Alive && inflictCondition >= Condition.DeadCorpse)
                {
                    KillPartyMember(partyMember, inflictCondition);
                }

                if (partyMember.Alive) // update HP etc if not died already
                {
                    damagedPlayers.Add(partyMember);

                    if (!Godmode && inflictCondition != Condition.None && inflictCondition < Condition.DeadCorpse)
                    {
                        partyMember.Conditions |= inflictCondition;

                        if (inflictCondition == Condition.Blind && partyMember == CurrentPartyMember)
                            UpdateLight();
                    }
                }

                if (partyMember.Alive && partyMember.Conditions.CanSelect())
                {
                    finished?.Invoke();
                }
                else
                {
                    if (CurrentPartyMember == partyMember && currentBattle == null)
                    {
                        if (!PartyMembers.Any(p => p.Alive && p.Conditions.CanSelect()))
                        {
                            GameOver();
                            return;
                        }

                        bool inputWasEnabled = InputEnable;
                        bool allInputWasDisabled = allInputDisabled;
                        this.NewLeaderPicked += NewLeaderPicked;
                        allInputDisabled = false;
                        RecheckActivePartyMember(out bool gameOver);

                        if (gameOver || !pickingNewLeader)
                            this.NewLeaderPicked -= NewLeaderPicked;

                        if (gameOver)
                            allInputDisabled = false;
                        else if (!pickingNewLeader)
                            allInputDisabled = allInputWasDisabled;

                        void NewLeaderPicked(int index)
                        {
                            this.NewLeaderPicked -= NewLeaderPicked;
                            allInputDisabled = allInputWasDisabled;
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

        void ShowDamageSplash(PartyMember partyMember, Action finished) => this.ShowDamageSplash(partyMember, damageProvider, finished);
    }

    internal void ShowDamageSplash(PartyMember partyMember, Func<PartyMember, uint> damageProvider, Action finished)
    {
        int slot = SlotFromPartyMember(partyMember).Value;
        layout.SetCharacter(slot, partyMember);
        ShowPlayerDamage(slot, damageProvider?.Invoke(partyMember) ?? 0);
        finished?.Invoke();
    }

    void DamageAllPartyMembers(uint damage, Func<PartyMember, bool> affectChecker = null,
        Action<PartyMember, Action> notAffectedHandler = null, Action<bool> followAction = null)
    {
        DamageAllPartyMembers(_ => damage, affectChecker, notAffectedHandler, followAction);
    }

    internal void TriggerTrap(TrapEvent trapEvent, bool lastEventStatus, uint x, uint y)
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
            bool allInputWasDisabled = allInputDisabled;

            void Next()
            {
                allInputDisabled = allInputWasDisabled;
                finish?.Invoke();
            }

            if (targetFilter?.Invoke(p) != false)
            {
                allInputDisabled = false;
                ShowMessagePopup(p.Name + DataNameProvider.EscapedTheTrap, Next);
            }
            else
                Next();
        }, Finished, trapEvent.GetAilment());

        void Finished(bool someoneDied)
        {
            if (someoneDied)
            {
                clickMoveActive = false;
					CurrentMobileAction = MobileAction.None;
					ResetMoveKeys(true);
            }

            if (trapEvent.Next != null)
            {
                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, x,
                    y, trapEvent.Next, lastEventStatus);
            }
            else
            {
                ResetMapCharacterInteraction(Map);
            }
        }
    }

    internal void RewardPlayer(PartyMember partyMember, RewardEvent rewardEvent, Action followAction)
    {
        void Change(CharacterValue characterValue, int amount, bool percentage, bool lpLike, bool increaseMax)
        {
            uint max = lpLike && !increaseMax ? characterValue.TotalMaxValue : characterValue.MaxValue;

            if (increaseMax)
                max = Math.Max(max, (uint)Math.Max(0, (int)max + amount));

            if (percentage)
                amount = amount * (int)max / 100;

            if (increaseMax)
            {
                characterValue.MaxValue = max;

                if (characterValue.CurrentValue > characterValue.MaxValue)
                    characterValue.CurrentValue = characterValue.MaxValue;
            }
            else
                characterValue.CurrentValue = (uint)Util.Limit(0, (int)characterValue.CurrentValue + amount, (int)max);
        }

        bool RewardValue(CharacterValue characterValue, bool lpLike, bool increaseMax = false)
        {
            uint value = RandomizeIfNecessary(rewardEvent.Value);

            switch (rewardEvent.Operation)
            {
                case RewardEvent.RewardOperation.Increase:
                    Change(characterValue, (int)value, false, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.Decrease:
                    Change(characterValue, -(int)value, false, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.IncreasePercentage:
                    Change(characterValue, (int)value, true, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.DecreasePercentage:
                    Change(characterValue, -(int)value, true, lpLike, increaseMax);
                    break;
                case RewardEvent.RewardOperation.Fill:
                    if (increaseMax)
                    {
                        ShowMessagePopup($"ERROR: Reward operation fill is not allowed on a max value.", followAction);
                        return false;
                    }
                    else
                        characterValue.CurrentValue = lpLike ? characterValue.TotalMaxValue : characterValue.MaxValue;
                    break;
            }

            return true;
        }

        uint RandomizeIfNecessary(uint value) => rewardEvent.Random ? 1u + random.Next() % value : value;

        switch (rewardEvent.TypeOfReward)
        {
            case RewardEvent.RewardType.Attribute:
                if (rewardEvent.Attribute != null && rewardEvent.Attribute < Attribute.Age)
                    RewardValue(partyMember.Attributes[rewardEvent.Attribute.Value], false);
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event attribute type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.Skill:
                if (rewardEvent.Skill != null)
                    RewardValue(partyMember.Skills[rewardEvent.Skill.Value], false);
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event skill type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.HitPoints:
            {
                // Note: Rewards happen silently so there is no damage splash.
                // Looking at the original code there isn't even a die handling
                // when a negative reward would leave the LP at 0 but we do so here.
                RewardValue(partyMember.HitPoints, true);
                if (partyMember.Alive && partyMember.HitPoints.CurrentValue == 0)
                    KillPartyMember(partyMember);
                else
                    layout.UpdateCharacter(partyMember);
                break;
            }
            case RewardEvent.RewardType.SpellPoints:
                RewardValue(partyMember.SpellPoints, true);
                layout.UpdateCharacter(partyMember);
                break;
            case RewardEvent.RewardType.SpellLearningPoints:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.SpellLearningPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.SpellLearningPoints + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.SpellLearningPoints = (ushort)Util.Max(0, (int)partyMember.SpellLearningPoints - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Conditions:
            {
                if (rewardEvent.Conditions == null)
                {
                    ShowMessagePopup($"ERROR: Invalid reward event condition.", followAction);
                    return;
                }

                bool wasDead = !partyMember.Alive;

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        partyMember.Conditions |= rewardEvent.Conditions.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        partyMember.Conditions &= ~rewardEvent.Conditions.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        partyMember.Conditions ^= rewardEvent.Conditions.Value;
                        break;
                }

                if (rewardEvent.Conditions.Value.HasFlag(Condition.Blind) && partyMember == CurrentPartyMember)
                    UpdateLight();

                if (wasDead && partyMember.Alive)
                {
                    partyMember.HitPoints.CurrentValue = 1;
                    layout.UpdateCharacter(partyMember, followAction);
                    return;
                }

                break;
            }
            case RewardEvent.RewardType.UsableSpellTypes:
            {
                if (rewardEvent.UsableSpellTypes == null)
                {
                    ShowMessagePopup($"ERROR: Invalid reward event spell mastery.", followAction);
                    return;
                }

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        partyMember.SpellMastery |= rewardEvent.UsableSpellTypes.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        partyMember.SpellMastery &= ~rewardEvent.UsableSpellTypes.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        partyMember.SpellMastery ^= rewardEvent.UsableSpellTypes.Value;
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Languages:
            {
                if (rewardEvent.Languages == null && (!Advanced || rewardEvent.ExtendedLanguages == null))
                {
                    ShowMessagePopup($"ERROR: Invalid reward event language.", followAction);
                    return;
                }

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Add:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages |= rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages |= rewardEvent.ExtendedLanguages.Value;
                        break;
                    case RewardEvent.RewardOperation.Remove:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages &= ~rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages &= ~rewardEvent.ExtendedLanguages.Value;
                        break;
                    case RewardEvent.RewardOperation.Toggle:
                        if (rewardEvent.Languages != null)
                            partyMember.SpokenLanguages ^= rewardEvent.Languages.Value;
                        else
                            partyMember.SpokenExtendedLanguages ^= rewardEvent.ExtendedLanguages.Value;
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Experience:
            {
                if (partyMember.Race != Race.Animal)
                {
                    switch (rewardEvent.Operation)
                    {
                        case RewardEvent.RewardOperation.Increase:
                            AddExperience(partyMember, RandomizeIfNecessary(rewardEvent.Value), followAction);
                            return;
                        case RewardEvent.RewardOperation.Decrease:
                            partyMember.ExperiencePoints = (uint)Util.Max(0, (long)partyMember.ExperiencePoints - RandomizeIfNecessary(rewardEvent.Value));
                            break;
                    }
                }
                break;
            }
            case RewardEvent.RewardType.MaxAttribute:
                if (rewardEvent.Attribute != null && rewardEvent.Attribute < Attribute.Age)
                {
                    if (!RewardValue(partyMember.Attributes[rewardEvent.Attribute.Value], false, true))
                        return;
                }
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event attribute type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.MaxSkill:
                if (rewardEvent.Skill != null && (int)rewardEvent.Skill < 10)
                {
                    if (!RewardValue(partyMember.Skills[rewardEvent.Skill.Value], false, true))
                        return;
                }
                else
                {
                    ShowMessagePopup($"ERROR: Invalid reward event skill type.", followAction);
                    return;
                }
                break;
            case RewardEvent.RewardType.AttacksPerRound:
            {
                int oldAttacksPerRound = partyMember.AttacksPerRound;

                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.AttacksPerRound = (byte)Util.Limit(1, partyMember.AttacksPerRound + RandomizeIfNecessary(rewardEvent.Value), 255);
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.AttacksPerRound = (byte)Util.Limit(1, partyMember.AttacksPerRound - (int)RandomizeIfNecessary(rewardEvent.Value), 255);
                        break;
                }

                if (partyMember.AttacksPerRound != oldAttacksPerRound)
                {
                    var currentAttacksPerRoundIncreaseLevels = partyMember.Level / Math.Max(1, (int)partyMember.AttacksPerRound);

                    if (partyMember.AttacksPerRound == 1 && partyMember.AttacksPerRoundIncreaseLevels != 0)
                        partyMember.AttacksPerRoundIncreaseLevels = (ushort)currentAttacksPerRoundIncreaseLevels;
                    else if (partyMember.AttacksPerRound > 1)
                        partyMember.AttacksPerRoundIncreaseLevels = (ushort)Math.Max(1, currentAttacksPerRoundIncreaseLevels);
                }

                break;
            }
            case RewardEvent.RewardType.TrainingPoints:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.TrainingPoints = (ushort)Util.Min(ushort.MaxValue, partyMember.TrainingPoints + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.TrainingPoints = (ushort)Util.Max(0, (int)partyMember.TrainingPoints - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Level:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        long levelUps = Util.Limit(0, rewardEvent.Value, 50 - partyMember.Level);
                        if (levelUps == 0)
                        {
                            followAction?.Invoke();
                            return;
                        }
                        partyMember.Level = (byte)(partyMember.Level + levelUps);
                        for (long i = 0; i < levelUps; ++i)
                        {
                            if (partyMember.Race == Race.Animal)
                            {
                                uint lpAdd = partyMember.HitPointsPerLevel * (uint)RandomInt(50, 100) / 100;
                                uint tpAdd = partyMember.TrainingPointsPerLevel * (uint)RandomInt(50, 100) / 100;
                                partyMember.HitPoints.MaxValue += lpAdd;
                                partyMember.HitPoints.CurrentValue += lpAdd;
                                partyMember.TrainingPoints = (ushort)Math.Min(ushort.MaxValue, partyMember.TrainingPoints + tpAdd);
                            }
                            else
                            {
                                partyMember.AddLevelUpEffects(RandomInt);
                            };
                        }
                        ShowLevelUpWindow(partyMember, followAction);
                        return;
                }
                break;
            }
            case RewardEvent.RewardType.Damage:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.BaseAttackDamage = (short)Util.Min(short.MaxValue, partyMember.BaseAttackDamage + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.BaseAttackDamage = (short)Util.Max(0, partyMember.BaseAttackDamage - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.Defense:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.BaseDefense = (short)Util.Min(short.MaxValue, partyMember.BaseDefense + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.BaseDefense = (short)Util.Max(0, partyMember.BaseDefense - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.MaxHitPoints:
            {
                // Note: Rewards happen silently so there is no damage splash.
                // Looking at the original code there isn't even a die handling
                // when a negative reward would leave the LP at 0 but we do so here.
                RewardValue(partyMember.HitPoints, true, true);
                if (partyMember.Alive && partyMember.HitPoints.CurrentValue == 0)
                    KillPartyMember(partyMember);
                else
                    layout.UpdateCharacter(partyMember);
                break;
            }
            case RewardEvent.RewardType.MaxSpellPoints:
                RewardValue(partyMember.SpellPoints, true, true);
                layout.UpdateCharacter(partyMember);
                break;
            case RewardEvent.RewardType.EmpowerSpells:
            {
                if (rewardEvent.Value < 3)
                    partyMember.BattleFlags |= (BattleFlags)(1 << ((int)rewardEvent.Value + 4));
                break;
            }
            case RewardEvent.RewardType.ChangePortrait:
            {
                bool changed = partyMember.PortraitIndex != rewardEvent.Value;
                partyMember.PortraitIndex = (byte)rewardEvent.Value;
                layout.UpdateCharacter(partyMember, followAction, changed);
                return;
            }
            case RewardEvent.RewardType.MagicArmorLevel:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.MagicDefense = (short)Util.Min(short.MaxValue, partyMember.MagicDefense + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.MagicDefense = (short)Util.Max(0, (int)partyMember.MagicDefense - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
            case RewardEvent.RewardType.MagicWeaponLevel:
            {
                switch (rewardEvent.Operation)
                {
                    case RewardEvent.RewardOperation.Increase:
                        partyMember.MagicAttack = (short)Util.Min(short.MaxValue, partyMember.MagicAttack + RandomizeIfNecessary(rewardEvent.Value));
                        break;
                    case RewardEvent.RewardOperation.Decrease:
                        partyMember.MagicAttack = (short)Util.Max(0, (int)partyMember.MagicAttack - (int)RandomizeIfNecessary(rewardEvent.Value));
                        break;
                }
                break;
            }
				case RewardEvent.RewardType.Spells:
				{
					if (rewardEvent.Spells == null)
					{
						ShowMessagePopup($"ERROR: Invalid reward event spell.", followAction);
						return;
					}

                int spellTypeIndex = -1;

                for (int i = 0; i < 4; i++)
                {
                    if (partyMember.SpellMastery.HasFlag((SpellTypeMastery)(1 << i)))
                    {
                        spellTypeIndex = i;
                        break;
						} 
                }

                if (spellTypeIndex == -1)
                {
                    followAction?.Invoke();
                    return;
                }

                Action<uint> setter;
                uint currentSpells;
                
                switch (spellTypeIndex)
                {
                    case 0:
							currentSpells = partyMember.LearnedHealingSpells;
                        setter = (value) => partyMember.LearnedHealingSpells = value;
							break;
						case 1:
							currentSpells = partyMember.LearnedAlchemisticSpells;
							setter = (value) => partyMember.LearnedAlchemisticSpells = value;
							break;
						case 2:
							currentSpells = partyMember.LearnedMysticSpells;
							setter = (value) => partyMember.LearnedMysticSpells = value;
							break;
						default:
							currentSpells = partyMember.LearnedDestructionSpells;
							setter = (value) => partyMember.LearnedDestructionSpells = value;
							break;
                };

					switch (rewardEvent.Operation)
					{
						case RewardEvent.RewardOperation.Add:
                        setter(currentSpells | rewardEvent.Spells.Value);
							break;
						case RewardEvent.RewardOperation.Remove:
							setter(currentSpells & ~rewardEvent.Spells.Value);
							break;
						case RewardEvent.RewardOperation.Toggle:
							setter(currentSpells ^ rewardEvent.Spells.Value);
							break;
					}
					break;
				}
			}

        followAction?.Invoke();
    }

    internal void SayWord(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
    {
        bool wasPaused = paused;
        Pause();
        void CheckResume()
        {
            if (!wasPaused)
                Resume();
        }
        OpenDictionary(word =>
        {
            layout.ClosePopup();

            bool match = string.Compare(textDictionary.Entries[(int)conditionEvent.ObjectIndex], word, true) == 0;
            var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
            var @event = match ? conditionEvent.Next : mapEventIfFalse;
            CheckResume();
            if (@event != null)
                EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, @event, true);
        }, null, CheckResume);
    }

    internal void EnterNumber(Map map, uint x, uint y, List<Event> events, ConditionEvent conditionEvent)
    {
        bool wasPaused = paused;
        Pause();
        void CheckResume()
        {
            if (!wasPaused)
                Resume();
        }
        layout.OpenAmountInputBox(DataNameProvider.WhichNumber, null, null, 9999, number =>
        {
            ClosePopup();
            var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                ? null : events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex];
            var @event = (number == conditionEvent.ObjectIndex) == (conditionEvent.Value != 0)
                ? conditionEvent.Next : mapEventIfFalse;
            CheckResume();
            if (@event != null)
                EventExtensions.TriggerEventChain(map, this, EventTrigger.Always, x, y, @event, true);
        }, CheckResume);
    }

    void Levitate(Action failAction, bool climbIfNoEvent = true)
    {
        ConditionEvent climbEvent = null;
        var levitatePosition = new Position(player.Position);
        bool HasClimbEvent(uint x, uint y)
        {
            var mapEventId = Map.Blocks[x, y].MapEventId;

            if (mapEventId == 0 || !CurrentSavegame.IsEventActive(Map.Index, mapEventId - 1))
                return false;

            var @event = Map.EventList[(int)mapEventId - 1];

            if (@event is not ConditionEvent conditionEvent)
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
                        EventExtensions.TriggerEventChain(Map, this, EventTrigger.Levitating, (uint)levitatePosition.X,
                            (uint)levitatePosition.Y, climbEvent, true);
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

    void Fall(uint tileX, uint tileY, Action finishAction = null)
    {
        // Attach player to ladder or hole
        float angle = camera3D.Angle;
        Geometry.Geometry.BlockToCameraPosition(Map, new Position((int)tileX, (int)tileY), out float x, out float z);
        camera3D.SetPosition(-x, z);
        camera3D.TurnTowards(angle);
        camera3D.GetBackwardPosition(0.45f * Global.DistancePerBlock, out x, out z, false, false);
        camera3D.SetPosition(-x, z);
        camera3D.TurnTowards(angle);

        MoveVertically(false, false, finishAction);
    }

    void MoveVertically(bool up, bool mapChange, Action finishAction = null)
    {
        if (!is3D || WindowActive)
        {
            finishAction?.Invoke();
            return;
        }

        var sourceY = !mapChange ? camera3D.Y : (up ? RenderMap3D.GetFloorY() : RenderMap3D.GetLevitatingY());
        player3D.SetY(sourceY);
        var targetY = mapChange ? camera3D.GroundY : (up ? RenderMap3D.GetLevitatingY() : RenderMap3D.GetFloorY());
        float stepSize = RenderMap3D.GetLevitatingStepSize();
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
        if (!is3D)
            return; // Should not happen

        if (WindowActive)
        {
            if (currentWindow.Window == Window.Inventory)
					CloseWindow(() => AddTimedEvent(TimeSpan.FromMilliseconds(250), Jump));
            return;
			}

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

            var @event = Map.GetEvent((uint)checkPosition.X, (uint)checkPosition.Y, CurrentSavegame);

            // Avoid jumping through closed doors, riddlemouths and place entrances.
            if (@event != null)
            {
                var trigger = EventTrigger.Move;
                bool lastEventStatus = true;
                bool aborted = false;

                while (@event is ConditionEvent condition)
                {
                    @event = condition.ExecuteEvent(Map, this, ref trigger,
                        (uint)checkPosition.X, (uint)checkPosition.Y, ref lastEventStatus,
                        out aborted, out _);

                    if (aborted)
                        break;
                }

                if (!aborted &&
                    ((@event is DoorEvent door && CurrentSavegame.IsDoorLocked(door.DoorIndex)) ||
                    @event.Type == EventType.Riddlemouth ||
                    @event.Type == EventType.EnterPlace))
                {
                    ShowMessagePopup(DataNameProvider.CannotJumpThroughWalls);
                    return;
                }
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
        if (!right)
            halfStepSize = -halfStepSize;
        int stepIndex = 0;

        void Step()
        {
            if (stepIndex++ < fullSteps)
                player3D.TurnRight(stepSize);
            else
                player3D.TurnRight(halfStepSize);

            CurrentSavegame.CharacterDirection = player.Direction = player3D.Direction;
        }

        PlayTimedSequence(fullSteps + 1, Step, 65, () =>
        {
            ResetMoveKeys();

            if (nextEvent != null)
            {
                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always,
                    (uint)player.Position.X, (uint)player.Position.Y, nextEvent, true);
            }
        });
    }

    bool CheckTeleportDestination(uint mapIndex, uint x, uint y)
    {
        if (mapIndex == 0)
            mapIndex = Map.Index;

        var newMap = MapManager.GetMap(mapIndex);

        if (newMap == null)
            return false;

        uint newX = x == 0 ? (uint)player.Position.X : x - 1;
        uint newY = y == 0 ? (uint)player.Position.Y : y - 1;

        if (newMap.Type == MapType.Map3D)
            return !newMap.Blocks[newX, newY].BlocksPlayer(MapManager.GetLabdataForMap(newMap));
        else
            return newMap.Tiles[newX, newY].AllowMovement(MapManager.GetTilesetForMap(newMap), TravelType);
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

        if (newMap == null)
        {
            blocked = true;
            return false;
        }

        if (!newMap.UseTravelTypes && (TravelType != TravelType.Walk && TravelType != TravelType.Swim))
            return false;

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
            if (!force && !newMap.Tiles[newX, newY].AllowMovement(MapManager.GetTilesetForMap(newMap), TravelType, true, true))
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

        if (direction == CharacterDirection.Keep)
            direction = PlayerDirection;

        player.MoveTo(newMap, newX, newY, CurrentTicks, true, direction, UpdateMapNameAndLight);
        this.player.Position.X = RenderPlayer.Position.X;
        this.player.Position.Y = RenderPlayer.Position.Y;
        // This will update the appearance.
        TravelType = TravelType;

        void UpdateMapNameAndLight(Map map)
        {
            if (mapChange && !WindowActive)
            {
                UpdateMapName(map);
                UpdateLight(true, false, false, map);
            }
        }

        if (!mapTypeChanged)
        {
            PlayerMoved(mapChange);
        }

        if (mapChange && !WindowActive)
        {
            // Color of the filled upper right area may need update cause of palette change.
            mapViewRightFillArea.Color = GetUIColor(28);
        }

        if (!mapChange) // Otherwise the map change handler takes care of this
            ResetMoveKeys();

        if (!WindowActive && !layout.PopupActive && !TravelType.IgnoreEvents())
        {
            // Trigger events after map transition
            TriggerMapEvents(EventTrigger.Move, (uint)this.player.Position.X,
                (uint)this.player.Position.Y);
        }

        return true;
    }

    internal void Teleport(TeleportEvent teleportEvent, uint x, uint y)
    {
        Teleporting = true;

        uint targetX = teleportEvent.X == 0 ? x + 1 : teleportEvent.X;
        uint targetY = teleportEvent.Y == 0 ? y + 1 : teleportEvent.Y;

        ResetMoveKeys();
        ResetMapCharacterInteraction(Map);

        if (PopupActive)
            layout.ClosePopup(false, true);

        void RunTransition()
        {
            levitating = false;
            Teleport(teleportEvent.MapIndex, targetX, targetY, teleportEvent.Direction, out _, true);

            if (Map.IsWorldMap && teleportEvent.NewTravelType != null && teleportEvent.NewTravelType != TravelType)
                TravelType = teleportEvent.NewTravelType.Value;

            if (TravelType.UsesMapObject() &&
                !CheckTeleportDestination(teleportEvent.MapIndex, targetX, targetY))
            {
                ToggleTransport();
                var transport = GetTransportAtPlayerLocation(out int? index);
                CurrentSavegame.TransportLocations[index.Value] = null;
                renderMap2D.RemoveTransport(index.Value);
            }

            Teleporting = false;
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
                else
                    Teleporting = false;
                break;
            case TeleportEvent.TransitionType.Falling:
            {
                if (!is3D)
                {
                    Fade(RunTransition);
                }
                else
                {
                    Pause();
                    Fall(x, y, () => Fade(() =>
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
                }
                break;
            }
            case TeleportEvent.TransitionType.Climbing:
                if (!is3D)
                {
                    Fade(RunTransition);
                }
                else
                {
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
                }
                break;
            case TeleportEvent.TransitionType.Outro:
                Teleporting = false;
                ShowOutro();
                break;
            default:
                Fade(RunTransition);
                break;
        }
    }

    internal void PrepareOutro()
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
    }

    void ShowOutro()
    {
        ClosePopup();
        CloseWindow();
        Pause();
        StartSequence();
        ExecuteNextUpdateCycle(() =>
        {
            PrepareOutro();

            PlayMusic(Song.Outro);
            outro ??= outroFactory.Create(ShowCustomOutro);
            outro.Start(CurrentSavegame);
        });
    }

    void ShowCustomOutro()
    {
        customOutro = new CustomOutro(this, layout, CurrentSavegame);
        customOutro.Start();
    }

    public bool ExploreMap()
    {
        if (!ingame || Map is null || !is3D)
            return false;

        if (!CurrentSavegame.Automaps.TryGetValue(Map.Index, out var automap))
        {
            automap = new Automap { ExplorationBits = Enumerable.Repeat((byte)0xff, (Map.Width * Map.Height + 7) / 8).ToArray() };
            CurrentSavegame.Automaps[Map.Index] = automap;
        }
        else
        {
            automap.ExplorationBits = Enumerable.Repeat((byte)0xff, (Map.Width * Map.Height + 7) / 8).ToArray();
        }

        if (Map.GotoPoints?.Count > 0)
        {
            foreach (var gotoPoint in Map.GotoPoints)
            {
                CurrentSavegame.ActivateGotoPoint(gotoPoint.Index);
            }
        }

        if (currentWindow.Window == Window.Automap && nextClickHandler != null)
        {
            var automapOptions = (AutomapOptions)currentWindow.WindowParameters[0];
            var oldCloseWindowHandler = closeWindowHandler;
            closeWindowHandler = backToMap =>
            {
                oldCloseWindowHandler?.Invoke(backToMap);
                ShowAutomap(automapOptions);
            };
            var nextClickHandler = this.nextClickHandler;
            this.nextClickHandler = null;
            nextClickHandler(MouseButtons.Right); // This closes the automap
        }

        return true;
    }

    public bool ActivateTransport(TravelType travelType)
    {
        if (travelType == TravelType.Walk ||
            travelType == TravelType.Swim)
            throw new AmbermoonException(ExceptionScope.Application, "Walking and swimming should not be set via ActivateTransport");

        if (!Map.UseTravelTypes)
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
                index = null;
                for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
                {
                    if (CurrentSavegame.TransportLocations[i] == null)
                    {
                        CurrentSavegame.TransportLocations[i] = new TransportLocation
                        {
                            MapIndex = mapIndex,
                            Position = Map.IsWorldMap
                                ? new Position((int)x % 50 + 1, (int)y % 50 + 1)
                                : new Position((int)x + 1, (int)y + 1),
                            TravelType = TravelType
                        };
                        index = i;
                        break;
                    }
                }

                if (index != null)
                    renderMap2D.PlaceTransport(mapIndex, Map.IsWorldMap ? x % 50 : x, Map.IsWorldMap ? y % 50 : y, TravelType, index.Value);
                else
                    return;
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
            renderMap2D.RemoveTransport(index.Value);
            ActivateTransport(transport.TravelType);
        }
    }

    TransportLocation GetTransportAtPlayerLocation(out int? index)
    {
        index = null;
        var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
        // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
        var position = Map.IsWorldMap
            ? new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1)
            : new Position(player.Position.X + 1, player.Position.Y + 1);

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

        if (!Map.UseTravelTypes)
            return transports;

        var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
        // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
        var position = Map.IsWorldMap
            ? new Position(player.Position.X % 50 + 1, player.Position.Y % 50 + 1)
            : new Position(player.Position.X + 1, player.Position.Y + 1);

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

    void DoSwimDamage(uint numTicks = 1, Action<bool> finishAction = null)
    {
        lastSwimDamageHour = GameTime.Hour;
        lastSwimDamageMinute = GameTime.Minute;
        swamLastTick = true;

        uint CalculateDamage(PartyMember partyMember)
        {
            var swimSkill = partyMember.Skills[Skill.Swim].TotalCurrentValue;

            if (swimSkill >= 99)
                return 0;

            var factor = (100 - swimSkill) / 2;
            uint hitPoints = partyMember.HitPoints.CurrentValue;
            uint totalDamage = 0;

            for (uint i = 0; i < numTicks; ++i)
            {
                uint damage = Math.Max(1, factor * hitPoints / 100);
                totalDamage += damage;
                hitPoints -= damage;
            }

            return totalDamage;
        }

        // Make sure the party stops moving after someone died
        finishAction ??= someoneDied =>
        {
            if (someoneDied)
            {
                clickMoveActive = false;
					CurrentMobileAction = MobileAction.None;
					ResetMoveKeys(true);
            }
        };

        DamageAllPartyMembers(CalculateDamage, null, null, finishAction);
    }

    private void UpdateMobileActionIndicatorPosition()
    {
        if (!Configuration.IsMobile)
            return;

        if (is3D)
        {
            var mapViewCenter = mapViewArea.Center;
				mobileActionIndicator.X = mapViewCenter.X - mobileActionIndicator.Width / 2;
				mobileActionIndicator.Y = mapViewCenter.Y - mobileActionIndicator.Height / 2;
			}
        else
        {
            mobileActionIndicator.X = player2D.DisplayArea.X;
				mobileActionIndicator.Y = player2D.DisplayArea.Y - mobileActionIndicator.Height;
        }
    }

		internal void PlayerMoved(bool mapChange, Position lastPlayerPosition = null, bool updateSavegame = true,
        Map lastMap = null)
    {
        if (mapChange)
            lastMapTicksReset = CurrentTicks;

        if (updateSavegame)
        {
            var map = is3D ? Map : renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y);
            CurrentSavegame.CurrentMapIndex = map.Index;
            CurrentSavegame.CurrentMapX = 1u + (uint)(player.Position.X % Map.Width);
            CurrentSavegame.CurrentMapY = 1u + (uint)(player.Position.Y % Map.Height);
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

                if (player.MovementAbility <= PlayerMovementAbility.Swimming)
                    player2D.BaselineOffset = CanSee() ? 0 : MaxBaseLine;
            }

            void EnableTransport(bool enable = true)
            {
                layout.TransportEnabled = enable;
                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(3, enable);
            }

            if (Map.UseTravelTypes)
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

                var transportLocations = CurrentSavegame.TransportLocations.ToList();
                foreach (var transport in transports)
                {
                    renderMap2D.PlaceTransport(transport.MapIndex,
                        (uint)transport.Position.X - 1, (uint)transport.Position.Y - 1, transport.TravelType, transportLocations.IndexOf(transport));
                }

                if (transportAtPlayerIndex != null && TravelType == TravelType.Walk)
                {
                    EnableTransport();
                    player2D.BaselineOffset = MaxBaseLine;
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

            // Check auto poison
            if (!is3D && renderMap2D is not null && !TravelType.IgnoreAutoPoison())
            {
					var playerPosition = PartyPosition - Map.MapOffset;

					if (renderMap2D.IsTilePoisoning(playerPosition.X, playerPosition.Y))
                {
						ForeachPartyMember((p, f) =>
						{
							if (RollDice100() >= p.Attributes[Data.Attribute.Luck].TotalCurrentValue)
							{
								AddCondition(Condition.Poisoned, p);
								ShowDamageSplash(p, _ => 0, f);
							}
							else
							{
								f?.Invoke();
							}
						}, p => p.Alive && !p.Conditions.HasFlag(Condition.Petrified), () => ResetMoveKeys());
					}
            }

				UpdateMobileActionIndicatorPosition();
			}

        if (mapChange)
        {
            monstersCanMoveImmediately = false;
            if (lastMap == null || !lastMap.IsWorldMap ||
                !Map.IsWorldMap || Map.World != lastMap.World)
                ResetMoveKeys(lastMap == null || lastMap.Type != Map.Type);
            if (!WindowActive)
                layout.UpdateLayoutButtons(movement.MovementTicks(Map.Type == MapType.Map3D, Map.UseTravelTypes, TravelType.Walk));

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
            if (!CurrentSavegame.Automaps.TryGetValue(Map.Index, out var automap))
            {
                automap = new Automap { ExplorationBits = new byte[(Map.Width * Map.Height + 7) / 8] };
                CurrentSavegame.Automaps.Add(Map.Index, automap);
            }

            if (CanSee())
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
                    ShowMessagePopup(DataNameProvider.GotoPointSaved, () =>
                    {
                        // If a goto point save message appears after map change,
                        // it will avoid triggering of map events so we have to call
                        // it on closing the popup.
                        if (mapChange)
                        {
                            TriggerMapEvents(EventTrigger.Move, (uint)this.player.Position.X,
                                (uint)this.player.Position.Y);
                        }

                    }, TextAlign.Left);
                    return;
                }
            }

            // Clairvoyance
            if (CurrentSavegame.IsSpellActive(ActiveSpellType.Clairvoyance))
            {
                bool trapFound = false;
                bool spinnerFound = false;
                player3D.Camera.GetForwardPosition(1.05f * Global.DistancePerBlock, out float x, out float z, false, false);

                var checkPositions = new Position[2]
                {
                    player3D.Position,
                    Geometry.Geometry.CameraToBlockPosition(Map, x, z)
                };

                foreach (var checkPosition in checkPositions)
                {
                    if (checkPosition == lastPlayerPosition)
                        continue;

                    var type = renderMap3D.FindEventTypesOnBlock((uint)checkPosition.X, (uint)checkPosition.Y, EventType.Trap, EventType.Spinner);

                    if (type == EventType.Trap)
                    {
                        trapFound = true;
                        break;
                    }
                    else if (type == EventType.Spinner)
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
            var changeEvents = CurrentSavegame.TileChangeEvents;
            if (!changeEvents.ContainsKey(map.Index))
                changeEvents[map.Index] = new List<ChangeTileEvent> { changeTileEvent };
            else
            {
                var existing = changeEvents[map.Index].FirstOrDefault(e => e.X == changeTileEvent.X && e.Y == changeTileEvent.Y);

                if (existing != null)
                    changeEvents[map.Index].Remove(existing);

                changeEvents[map.Index].Add(changeTileEvent);
            }
        }

        if (!changedMaps.Contains(map.Index))
            changedMaps.Add(map.Index);

        if (map.Type == MapType.Map3D)
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

        if (changeTileEvent.Next == null)
            ResetMapCharacterInteraction(Map);
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

    internal void SetMapCharacterBit(uint characterBit, bool bit)
    {
        var mapIndex = 1 + characterBit / 32;
        var characterIndex = characterBit % 32;

        SetMapCharacterBit(mapIndex, characterIndex, bit);
    }

    private bool IsMapCharacterActive(uint characterBit)
    {
        var mapIndex = 1 + characterBit / 32;
        var characterIndex = characterBit % 32;

        return !CurrentSavegame.GetCharacterBit(mapIndex, characterIndex);
    }

    void RefillChest(uint chestIndex)
    {
        // If not saved, restore initial content
        var initialChest = GetInitialChest(chestIndex);

        if (initialChest != null && initialChest.Slots.OfType<ItemSlot>().Sum(item => item.Amount) +
            initialChest.Gold + initialChest.Food == 1)
        {
            var chest = GetChest(chestIndex);

            chest.Gold = initialChest.Gold;
            chest.Food = initialChest.Food;

            for (int y = 0; y < Chest.SlotRows; ++y)
            {
                for (int x = 0; x < Chest.SlotsPerRow; ++x)
                    chest.Slots[x, y].Replace(initialChest.Slots[x, y]);
            }
        }
    }

    private void GetEventIndex(Position position, out uint? eventIndex, out uint? mapIndex)
    {
        if (Map.Type == MapType.Map3D)
        {
            eventIndex = Map.GetEventIndex((uint)position.X, (uint)position.Y, CurrentSavegame);
            mapIndex = Map.Index;
        }
        else
        {
            uint x = (uint)position.X;
            uint y = (uint)position.Y;

            var map = Map;

            if (map.IsWorldMap)
            {
                map = renderMap2D.GetMapFromTile(x, y);
                x %= 50;
                y %= 50;
            }

            eventIndex = map.GetEventIndex(x, y, CurrentSavegame);
            mapIndex = map.Index;
        }
    }

    internal void ChestClosed()
    {
        // This is called by manually close the chest window via the Exit button
        var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
        var position = (Position)currentWindow.WindowParameters[4];

        CloseWindow(() =>
        {
            uint chestIndex = chestEvent.RealChestIndex;

            if (chestEvent.NoSave)
            {
                RefillChest(chestIndex);
            }

            if (chestEvent.Next != null)
            {
                Map.TriggerEventChain(this, EventTrigger.Always, (uint)(position?.X ?? 0),
                    (uint)(position?.Y ?? 0), chestEvent.Next, false);
            }
        });
    }

    void ChestRemoved()
    {
        var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
        var position = (Position)currentWindow.WindowParameters[4];

        CloseWindow(() =>
        {
            uint chestIndex = chestEvent.RealChestIndex;

            if (chestEvent.NoSave)
            {
                RefillChest(chestIndex);
            }
            else if (chestEvent.CloseWhenEmpty)
            {
                var chest = GetChest(chestEvent.RealChestIndex);

                if (chest.Empty)
                {
                    GetEventIndex(position, out var eventIndex, out var mapIndex);

                    if (eventIndex != null)
                        CurrentSavegame.SetEventBit(mapIndex.Value, eventIndex.Value - 1, true);
                }
            }

            if (chestEvent.Next != null)
            {
                Map.TriggerEventChain(this, EventTrigger.Always, (uint)(position?.X ?? 0),
                    (uint)(position?.Y ?? 0), chestEvent.Next, true);
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
                    if (chest.Type == ChestType.Chest)
                        layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                    // If a chest has AllowsItemDrop = false this
                    // means it is removed when it is empty.
                    if (!chest.AllowsItemDrop)
                        ChestRemoved();
                }
                else
                {
                    if (chest.Type == ChestType.Chest)
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
            if (chest.Type == ChestType.Chest)
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
            ShowTextPanel(CharacterInfo.ChestGold, true,
                $"{DataNameProvider.GoldName}^{chest.Gold}", new Rect(111, 104, 43, 15));
        }
        else
        {
            HideTextPanel(CharacterInfo.ChestGold);

            if (chest.Empty && !chest.IsBattleLoot)
            {
                if (chest.Type == ChestType.Chest)
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
            if (chest.Type == ChestType.Chest)
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
            ShowTextPanel(CharacterInfo.ChestFood, true,
                $"{DataNameProvider.FoodName}^{chest.Food}", new Rect(260, 104, 43, 15));
        }
        else
        {
            HideTextPanel(CharacterInfo.ChestFood);

            if (chest.Empty && !chest.IsBattleLoot)
            {
                if (chest.Type == ChestType.Chest)
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                if (!chest.AllowsItemDrop)
                    ChestRemoved();
            }
        }
    }

    void ShowLoot(ITreasureStorage storage, string initialText, Action initialTextClosedEvent, ChestEvent chestEvent = null,
        bool triggerFollowEvents = false, uint eventX = 0, uint eventY = 0)
    {
        if (chestEvent?.Next != null && triggerFollowEvents)
        {
            var oldCloseWindowHandler = closeWindowHandler;
            closeWindowHandler = backToMap =>
            {
                oldCloseWindowHandler?.Invoke(backToMap);

                if (backToMap)
                    EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, eventX, eventY, chestEvent.Next, true);
            };
        }
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
        bool pile = storage.IsBattleLoot || (storage is Chest chest && chest.Type == ChestType.Junk);

        if (pile)
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

        itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
        {
            if (updateSlot)
            {
                int column = slotIndex % Chest.SlotsPerRow;
                int row = slotIndex / Chest.SlotsPerRow;
                storage.Slots[column, row].Remove(amount);
            }
        };
        itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
        {
            if (!pile)
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

    internal bool ShowChest(ChestEvent chestEvent, bool foundTrap, bool disarmedTrap, Map map,
        Position position, bool fromEvent, bool triggerFollowEvents = false)
    {
        var chest = GetChest(chestEvent.RealChestIndex);

        if (chestEvent.CloseWhenEmpty && chest.Empty)
        {
            if (!chestEvent.NoSave)
            {
                GetEventIndex(position, out var eventIndex, out var mapIndex);

                if (eventIndex != null)
                    CurrentSavegame.SetEventBit(mapIndex.Value, eventIndex.Value - 1, true);
            }

            return false; // Chest has gone due to looting
        }

        chest.Type = chestEvent.CloseWhenEmpty ? ChestType.Junk : ChestType.Chest;

        void OpenChest()
        {
            bool changed = !chest.Equals(GetInitialChest(chestEvent.RealChestIndex), false);
            string initialText = changed ? null : map != null && fromEvent && chestEvent.TextIndex != 255 ?
                map.GetText((int)chestEvent.TextIndex, DataNameProvider.TextBlockMissing) : null;
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Chest, chestEvent, foundTrap, disarmedTrap, map, position, triggerFollowEvents);
            CursorType = CursorType.Sword;
            ResetMapCharacterInteraction(map ?? Map, true);

            if (chestEvent.LockpickingChanceReduction != 0 && CurrentSavegame.IsChestLocked(chestEvent.RealChestIndex - 1))
            {
                ShowLocked(Picture80x80.ChestClosed, () =>
                {
                    CurrentSavegame.UnlockChest(chestEvent.RealChestIndex - 1);
                    currentWindow.Window = Window.Chest; // This avoids returning to locked screen when closing chest window.
                    ExecuteNextUpdateCycle(() => ShowChest(chestEvent, false, false, map, position, true, true));
                }, null, chestEvent.KeyIndex, chestEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
                chestEvent.UnlockFailedEventIndex == 0xffff ? null : () => map.TriggerEventChain(this, EventTrigger.Always,
                (uint)position.X, (uint)position.Y, map.Events[(int)chestEvent.UnlockFailedEventIndex], true),
                () =>
                {
                    if (chestEvent.Next != null)
                        map.TriggerEventChain(this, EventTrigger.Always, (uint)position.X, (uint)position.Y, chestEvent.Next, false);
                });
            }
            else
            {
                ShowLoot(chest, initialText, null, chestEvent, triggerFollowEvents, (uint)position.X, (uint)position.Y);
            }
        }

        if (CurrentWindow.Window == Window.Chest)
            OpenChest();
        else
            Fade(OpenChest);

        return true;
    }

    internal bool ShowDoor(DoorEvent doorEvent, bool foundTrap, bool disarmedTrap, Map map, uint x, uint y, bool fromEvent, bool moved)
    {
        if (!CurrentSavegame.IsDoorLocked(doorEvent.DoorIndex))
            return false;

        Fade(() =>
        {
            string initialText = fromEvent && doorEvent.TextIndex != 255 ?
                map.GetText((int)doorEvent.TextIndex, DataNameProvider.TextBlockMissing) : null;
            string unlockText = doorEvent.UnlockTextIndex != 255 ?
                map.GetText((int)doorEvent.UnlockTextIndex, DataNameProvider.TextBlockMissing) : null;
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Door, doorEvent, foundTrap, disarmedTrap, map, x, y, moved);
            ShowLocked(Picture80x80.Door, () =>
            {
                if (moved && !is3D)
                {
                    player2D.Position.X = player.Position.X = (int)x;
                    player2D.Position.Y = player.Position.Y = (int)y;
                    player2D.UpdateAppearance(CurrentTicks);
                }
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
                        if (doorEvent.Next == null)
                        {
                            int eventIndex = map.EventList.IndexOf(doorEvent);
                            if (eventIndex != -1)
                                CurrentSavegame.ActivateEvent(map.Index, (uint)eventIndex, false);
                        }
                        else
                        {
                            EventExtensions.TriggerEventChain(map ?? Map, this, EventTrigger.Always, x, y, doorEvent.Next, true);
                        }
                    });
                }
            }, initialText, doorEvent.KeyIndex, doorEvent.LockpickingChanceReduction, foundTrap, disarmedTrap,
            doorEvent.UnlockFailedEventIndex == 0xffff ? null : () => map.TriggerEventChain(this, EventTrigger.Always,
                x, y, map.Events[(int)doorEvent.UnlockFailedEventIndex], true),
            () =>
            {
                if (doorEvent.Next != null)
                    map.TriggerEventChain(this, EventTrigger.Always, x, y, doorEvent.Next, false);
            });
        });

        return true;
    }

    void ShowLocked(Picture80x80 picture80X80, Action openedAction, string initialMessage,
        uint keyIndex, uint lockpickingChanceReduction, bool foundTrap, bool disarmedTrap, Action failedAction,
        Action abortAction)
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
        closeWindowHandler = _ => ActivePlayerChanged -= PlayerSwitched;

        void Exit()
        {
            CloseWindow(abortAction);
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
                    CursorType = CursorType.Sword;
                    inputEnable = true;
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
                            var itemInfo = ItemManager.GetItem(itemSlot.ItemIndex);
                            if (itemInfo.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
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
                                    uint itemIndex = itemSlot.ItemIndex;
                                    itemSlot.Remove(1);
                                    InventoryItemRemoved(itemIndex, 1, CurrentPartyMember);                                        
                                });
                            }
                            else
                            {
                                // Just move back
                                StartSequence();
                                itemGrid.HideTooltip();
                                itemGrid.PlayMoveAnimation(itemSlot, null, () =>
                                {
                                    itemGrid.ResetAnimation(itemSlot);
                                    item.ShowItemAmount = false;
                                    item.Visible = false;
                                    itemGrid.HideTooltip();
                                    itemGrid.Disabled = true;
                                    EndSequence();
                                    openedAction?.Invoke();
                                });
                            }
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
                                    uint itemIndex = itemSlot.ItemIndex;
                                    itemSlot.Remove(1);
                                    InventoryItemRemoved(itemIndex, 1, CurrentPartyMember);                                        
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
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.LockPicking].TotalCurrentValue, 100) - (int)lockpickingChanceReduction;

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
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.FindTraps].TotalCurrentValue, 100);

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
            int chance = Util.Limit(0, (int)CurrentPartyMember.Skills[Skill.DisarmTraps].TotalCurrentValue, 100); // TODO: Is there a "find trap" reduction as well?

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

        if (!string.IsNullOrWhiteSpace(initialMessage))
            layout.ShowClickChestMessage(initialMessage);
    }

    static readonly Dictionary<uint, ushort> PartyMemberCharacterBits = new Dictionary<uint, ushort>
    {
        // Netsrak
        { 2,  0x2000 },
        // Mando
        { 3,  0x2001 },
        // Erik
        { 4,  0x2002 },
        // Chris
        { 5,  0x2003 },
        // Monika
        { 6,  0x2004 },
        // Tar the dark
        { 7,  0x2141 },
        // Egil
        { 8,  0x2163 },
        // Selena
        { 9,  0x22c2 },
        // Nelvin
        { 10, 0x2321 },
        // Sabine
        { 11, 0x23a0 },
        // Valdyn
        { 12, 0x2400 },
        // Targor
        { 13, 0x3320 },
        // Leonaria
        { 14, 0x3440 },
        // Gryban
        { 15, 0x35a0 },
        // Kasimir
        { 16, 0x2203 },
        // S'Ebi
        { 17, 0x3c00 }
    };

    // Some party members like Sabine appear at some different location first (e.g. Luminor's torture chamber)
    // and will spawn somewhere else later (Burnville healers). This stores the initial location bit.
    static readonly Dictionary<uint, ushort> PartyMemberInitialCharacterBits = new Dictionary<uint, ushort>
    {
        // Netsrak
        { 2,  0x2000 },
        // Mando
        { 3,  0x2001 },
        // Erik
        { 4,  0x2002 },
        // Chris
        { 5,  0x2003 },
        // Monika
        { 6,  0x2004 },
        // Tar the dark
        { 7,  0x2141 },
        // Egil
        { 8,  0x2163 },
        // Selena
        { 9,  0x22e0 },
        // Nelvin
        { 10, 0x2321 },
        // Sabine
        { 11, 0x2485 },
        // Valdyn
        { 12, 0x24e0 },
        // Targor
        { 13, 0x3320 },
        // Leonaria
        { 14, 0x3440 },
        // Gryban
        { 15, 0x35a0 },
        // Kasimir
        { 16, 0x2203 },
        // S'Ebi
        { 17, 0x3bc5 }
    };

    /// <summary>
    /// A conversation is started with a Conversation event but the
    /// displayed text depends on the following events. Mostly
    /// Condition and PrintText events. The argument conversationEvent
    /// is the initial conversation event of interaction type 'Talk'
    /// and should be used to determine the text to print etc.
    /// 
    /// The event chain may also contain rewards, new keywords, etc.
    /// </summary>
    internal void ShowConversation(IConversationPartner conversationPartner, uint? characterIndex,
        Event conversationEvent, ConversationItems createdItems, bool showInitialText = true)
    {
        if (!(conversationPartner is Character character))
            throw new AmbermoonException(ExceptionScope.Application, "Conversation partner is no character.");

        if ((character.SpokenLanguages & CurrentPartyMember.SpokenLanguages) == 0 &&
            (character.SpokenExtendedLanguages & CurrentPartyMember.SpokenExtendedLanguages) == 0)
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

        bool creatingItems = false;
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
        closeWindowHandler = _ => CleanUp();

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
            aborted = false;
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
                    layout.ButtonsDisabled = true;
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
					layout.ButtonsDisabled = false;
					nextClickHandler = null;
                ShowCreatedItems();
            }

            itemGrid.Disabled = false;
            itemGrid.DisableDrag = true;
            CursorType = CursorType.Sword;
            var itemArea = new Rect(16, 139, 151, 53);
            TrapMouse(itemArea);
				layout.ButtonsDisabled = true;
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
                        followAction?.Invoke();
                    }, 650);
                }
                EndSequence();
                message?.Destroy();
                message = null;
                layout.GetItem(itemSlot).Dragged = true; // Keep it above UI
                UntrapMouse();
					layout.ButtonsDisabled = false;
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
                                    bool consume = eventType == EventType.Interact;

                                    if (!consume)
                                    {
                                        var @event = conversationEvent;

                                        while (@event != null)
                                        {
                                            if (@event.Type == EventType.Interact)
                                            {
                                                consume = true;
                                                break;
                                            }

                                            @event = @event.Next;
                                        }
                                    }

                                    if (consume)
                                    {
                                        // Consume
                                        StartSequence();
                                        itemGrid.HideTooltip();
                                        layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true, () =>
                                        {
                                            uint itemIndex = itemSlot.ItemIndex;
                                            itemSlot.Remove(1);
                                            InventoryItemRemoved(itemIndex, 1, CurrentPartyMember);                                                
                                            //ShowCreatedItems();
                                            EndSequence();
                                            Abort();
                                            if (eventType == EventType.Interact)
                                                HandleNextEvent(null);
                                            else
                                                HandleEvent(null);
                                        }, new Position(215, 75), false);
                                    }
                                    else
                                        ExecuteNextUpdateCycle(() => HandleNextEvent(null));
                                }
                                else // Show item
                                {
                                    if (eventType == EventType.Interact)
                                        MoveBack(() => HandleNextEvent(null));
                                    else
                                        ExecuteNextUpdateCycle(() => HandleNextEvent(null));
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

                    layout.ButtonsDisabled = true;
                    HandleInteraction();
                }
            }
            void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
            {
                itemGrid.ItemClicked -= ItemClicked;
                nextClickHandler = null;
                UntrapMouse();
					layout.ButtonsDisabled = false;
					StartSequence();
                itemGrid.HideTooltip();
                itemGrid.PlayMoveAnimation(itemSlot, new Position(215, 75), () => CheckItem(itemSlot), 650);
            }
            itemGrid.ItemClicked += ItemClicked;
        }

        void ShowItem()
        {
            aborted = false;
            ShowItems(DataNameProvider.WhichItemToShow, InteractionType.ShowItem);
        }

        void GiveItem()
        {
            aborted = false;
            ShowItems(DataNameProvider.WhichItemToGive, InteractionType.GiveItem);
        }

        void GiveGold()
        {
            aborted = false;
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
                            layout.ButtonsDisabled = true;
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
            aborted = false;
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
                            layout.ButtonsDisabled = true;
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
            aborted = false;
            if (PartyMembers.Count() == MaxPartyMembers)
            {
                conversationEvent = null;
                SetText(DataNameProvider.PartyFull);
                layout.ButtonsDisabled = false;
                return;
            }

            if (character is PartyMember &&
                (conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.JoinParty)) != null)
            {
                layout.ButtonsDisabled = true;
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
            aborted = false;
            if (character is PartyMember partyMember && PartyMembers.Contains(partyMember))
            {
                if (!partyMember.Alive)
                {
                    SetText(DataNameProvider.CannotSendDeadPeopleAway);
                    return;
                }

                if (partyMember.Conditions.HasFlag(Condition.Crazy))
                {
                    SetText(DataNameProvider.CrazyPeopleDontFollowCommands);
                    return;
                }

                if (partyMember.Conditions.HasFlag(Condition.Petrified))
                {
                    SetText(DataNameProvider.PetrifiedPeopleCantGoHome);
                    return;
                }

                if (!partyMember.Alive)
                {
                    SetText(DataNameProvider.CannotSendDeadPeopleAway);
                    return;
                }

                if (Map.World != World.Lyramion) // TODO: You can still leave in Morag hangar and prison like in the original
                {
                    SetText(DataNameProvider.DenyLeavingPartyOnMoon);
                    return;
                }

                if ((conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.LeaveParty)) != null)
                {
                    currentInteractionType = InteractionType.LeaveParty;
                    layout.ButtonsDisabled = true;
                    aborted = false;
                    lastEventStatus = true;
                    HandleNextEvent();
                }
                else
                {
                    SetText(DataNameProvider.WellIShouldLeave);
                    RemovePartyMember(() => Exit()); // Just remove from party and close
                }
            }
        }

        void AddPartyMember(Action followAction)
        {
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                if (GetPartyMember(i) == null)
                {
                    var partyMember = character as PartyMember;
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
                    SetMapCharacterBit(Map.Index, characterIndex.Value, true);
                    if (partyMember.CharacterBitIndex == 0xffff || partyMember.CharacterBitIndex == 0x0000)
                        partyMember.CharacterBitIndex = (ushort)(((Map.Index - 1) << 5) | characterIndex.Value);
                    break;
                }
            }
        }

        void RemovePartyMember(Action followAction)
        {
            var partyMember = character as PartyMember;
            var index = partyMember.CharacterBitIndex;
            if (index == 0xffff)
                index = PartyMemberCharacterBits[partyMember.Index];
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
            int remainingCount = (int)amount;

            for (int i = 0; i < 24; ++i)
            {
                if (createdItemSlots[i].Empty)
                {
                    createdItemSlots[i].FillWithNewItem(ItemManager, itemIndex, ref remainingCount);

                    if (remainingCount == 0)
                        break;
                }
            }
            ShowCreatedItems();
        }

        void Exit(bool showLeaveMessage = false)
        {
            aborted = false;
            if (showLeaveMessage)
            {
                if (createdItems.HasAnyImportantItem(ItemManager))
                {
                    aborted = true;
                    SetText(DataNameProvider.DontForgetItems +
                        string.Join(", ", createdItems.GetImportantItemNames(ItemManager)) + ".");
                    return;
                }

                if (createdItems.Slots.Cast<ItemSlot>().Any(s => !s.Empty))
                {
                    ShowDecisionPopup(DataNameProvider.LeaveConversationWithoutItems, response =>
                    {
                        if (response == PopupTextEvent.Response.Yes)
                        {
                            ExitConversation();
                            return;
                        }

                        aborted = true;
                    }, 1);
                    return;
                }

                ExitConversation();
                return;

                void ExitConversation()
                {
                    conversationEvent = GetFirstMatchingEvent(e => e.Interaction == InteractionType.Leave);

                    if (conversationEvent != null)
                    {
                        currentInteractionType = InteractionType.Leave;
                        layout.ButtonsDisabled = true;
                        aborted = false;
                        lastEventStatus = true;
                        HandleNextEvent();
                        return;
                    }
                    else
                    {
                        SetText(DataNameProvider.GoodBye, CloseWindow);
                        return;
                    }
                }
            }

            CloseWindow();
        }

        void CleanUp()
        {
            layout.DraggedItemDropped -= DraggedItemDropped;
            ActivePlayerChanged -= SwitchPlayer;
            ConversationTextActive = false;
            layout.ButtonsDisabled = false;
        }

        void HandleNextEvent(Action<EventType> followAction = null)
        {
            conversationEvent = conversationEvent?.Next;
            layout.ButtonsDisabled = conversationEvent != null;
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
                // Exit event triggered after create event -> abort.
                if (creatingItems)
                    return;

                Exit();
                nextAction?.Invoke(EventType.Exit);
            }
            else if (conversationEvent is CreateEvent createEvent)
            {
                creatingItems = true;

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

                if (conversationEvent == createEvent)
                {
                    conversationEvent = conversationEvent.Next;
                    layout.ButtonsDisabled = conversationEvent != null;

                    // Sometimes multiple items are created, so do them all at once.
                    if (conversationEvent is CreateEvent)
                    {
                        HandleEvent();
                    }
                }
                layout.ButtonsDisabled = conversationEvent != null;
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
                        if (CurrentPartyMember.Gold == 0)
                            layout.EnableButton(7, false);
                        nextAction?.Invoke(EventType.Interact);
                        break;
                    case InteractionType.GiveFood:
                        CurrentPartyMember.RemoveFood(amount);
                        UpdateCharacterInfo(character);
                        if (CurrentPartyMember.Food == 0)
                            layout.EnableButton(8, false);
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
                        (uint)player.Position.X, (uint)player.Position.Y, conversationEvent, true));
                }
                else
                {
                    var trigger = EventTrigger.Always;
                    conversationEvent = EventExtensions.ExecuteEvent(conversationEvent, Map, this, ref trigger,
                        (uint)player.Position.X, (uint)player.Position.Y, ref lastEventStatus, out aborted,
                        out var eventProvider, conversationPartner);
                    layout.ButtonsDisabled = conversationEvent != null;

                    // Might be reduced or added by action events
                    layout.EnableButton(7, CurrentPartyMember.Gold != 0);
                    layout.EnableButton(8, CurrentPartyMember.Food != 0);

                    if (conversationEvent == null && eventProvider != null)
                    {
                        if (eventProvider.Event != null)
                        {
                            conversationEvent = eventProvider.Event;
                            layout.ButtonsDisabled = conversationEvent != null;
                            HandleEvent(followAction);
                        }
                        else
                        {
                            eventProvider.Provided += @event =>
                            {
                                conversationEvent = @event;
                                layout.ButtonsDisabled = conversationEvent != null;

                                if (@event == null)
                                    followAction?.Invoke(EventType.Invalid);
                                else
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

        void ItemDragged(int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot)
        {
            ExecuteNextUpdateCycle(() =>
            {
                moveItemMessage = layout.AddText(textArea, DataNameProvider.WhereToMoveIt,
                    TextColor.BrightGray, TextAlign.Center);
                var draggedSourceSlot = itemGrid.GetItemSlot(slotIndex);
                if (updateSlot)
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

            if (creatingItems && itemGrid.Disabled)
            {
                creatingItems = false;
                HandleEvent();
            }
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
            SetWindow(Window.Conversation, conversationPartner, characterIndex, conversationEvent, createdItems);
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
            {
                if (conversationEvent != null)
                {
                    layout.ButtonsDisabled = true;
                    HandleNextEvent();
                }
                else
                {
                    SetText(DataNameProvider.Hello);
                }
            }
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

        uint? combatBackgroundIndex = null;

        if (!is3D)
        {
            var tile = renderMap2D[player.Position];

            if (tile != null)
            {
                var tileset = MapManager.GetTilesetForMap(Map);

                if (tile.FrontTileIndex != 0)
                    combatBackgroundIndex = tileset.Tiles[tile.FrontTileIndex - 1].CombatBackgroundIndex;
                else if (tile.BackTileIndex != 0)
                    combatBackgroundIndex = tileset.Tiles[tile.BackTileIndex - 1].CombatBackgroundIndex;
            }
        }

        StartBattle(monsterGroupIndex, false, (uint)player.Position.X, (uint)player.Position.Y, null, combatBackgroundIndex);
        return true;
    }

    /// <summary>
    /// Starts a battle with the given monster group index.
    /// It is used for monsters that are present on the map.
    /// </summary>
    /// <param name="monsterGroupIndex">Monster group index</param>
    internal void StartBattle(uint monsterGroupIndex, bool failedEscape, uint x, uint y,
        Action<BattleEndInfo> battleEndHandler, uint? combatBackgroundIndex = null)
    {
        if (BattleActive)
            return;

        currentBattleInfo = new BattleInfo
        {
            MonsterGroupIndex = monsterGroupIndex
        };
        currentBattleInfo.BattleEnded += battleEndHandler;
        ShowBattleWindow(null, failedEscape, x, y, combatBackgroundIndex);
    }

    void UpdateBattle(double blinkingTimeFactor)
    {
        if (partyAdvances)
        {
            foreach (var monster in currentBattle.Monsters)
                layout.GetMonsterBattleAnimation(monster).Update(CurrentBattleTicks);
        }
        else
        {
            currentBattle.Update(CurrentBattleTicks, CurrentNormalizedBattleTicks);
        }

        if (highlightBattleFieldSprites.Count != 0)
        {
            var ticks = Math.Round(CurrentBattleTicks * blinkingTimeFactor);
            bool showBlinkingSprites = !blinkingHighlight || (ticks % (2 * TicksPerSecond / 3)) < TicksPerSecond / 3;

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
        else if (partyMember.Conditions.HasFlag(Condition.Petrified))
            return UIGraphic.StatusPetrified;
        else if (partyMember.Conditions.HasFlag(Condition.Sleep))
            return UIGraphic.StatusSleep;
        else if (partyMember.Conditions.HasFlag(Condition.Panic))
            return UIGraphic.StatusPanic;
        else if (partyMember.Conditions.HasFlag(Condition.Crazy))
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
        else if (!partyMember.Conditions.CanSelect())
        {
            // Note: Disabled players will show the status icon next to
            // their portraits instead of an action icon. For mad players
            // when the battle starts the action icon will be shown instead.
            layout.UpdateCharacterStatus(slot, GetDisabledStatusGraphic(partyMember));
            roundPlayerBattleActions.Remove(slot);
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot].TextColor = TextColor.DeadPartyMember;
        }
        else if (roundPlayerBattleActions.ContainsKey(slot))
        {
            var action = roundPlayerBattleActions[slot];
            layout.UpdateCharacterStatus(slot, action.BattleAction.ToStatusGraphic(action.Parameter, ItemManager));
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot].TextColor = TextColor.White;
        }
        else
        {
            layout.UpdateCharacterStatus(slot, null);
            if (partyMemberBattleFieldTooltips[slot] != null)
                partyMemberBattleFieldTooltips[slot].TextColor = TextColor.White;
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

            closeWindowHandler = _ =>
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
                                        if (dragSource.Value != i && battlePositions[dragSource.Value] == slotOfDraggedPartyMember.Value)
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

    void ShowBattleWindow(Event nextEvent, out byte paletteIndex, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        combatBackgroundIndex ??= is3D ? renderMap3D.CombatBackgroundIndex : Map.World switch
        {
            World.Lyramion => 0u,
            World.ForestMoon => 6u,
            World.Morag => 4u,
            _ => 0u
        };

        SetWindow(Window.Battle, nextEvent, x, y, combatBackgroundIndex);
        layout.SetLayout(LayoutType.Battle);
        ShowMap(false);
        layout.Reset();

        bool advancedBackgrounds = Features.HasFlag(Features.AdvancedCombatBackgrounds);
			var combatBackground = is3D
            ? renderView.GraphicProvider.Get3DCombatBackground(combatBackgroundIndex.Value, advancedBackgrounds)
            : renderView.GraphicProvider.Get2DCombatBackground(combatBackgroundIndex.Value, advancedBackgrounds);
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
                partyMember.Conditions.CanSelect() ? TextColor.White : TextColor.DeadPartyMember, null, out partyMemberBattleFieldTooltips[i]);
            }
        }

        UpdateBattleStatus();
        UpdateActiveBattleSpells();

        SetupBattleButtons();

        currentBattle?.InitImitatingPlayers();
    }

    internal void ReplacePartyMemberBattleFieldSprite(PartyMember partyMember, MonsterGraphicIndex graphicIndex)
    {
        int index = PartyMembers.ToList().IndexOf(partyMember);

        if (index != -1)
        {
            var textureIndex = Graphics.BattleFieldIconOffset + (uint)Class.Monster + (uint)graphicIndex - 1;
            partyMemberBattleFieldSprites[index].TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(textureIndex);
        }
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
                StartSequence();
                layout.HideTooltip();
                currentBattle.HideAllBattleFieldDamage();
                OpenSpellList(CurrentPartyMember,
                    spell =>
                    {
                        var spellInfo = SpellInfos[spell];

                        if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle))
                            return DataNameProvider.WrongArea;

                        var worldFlag = (WorldFlag)(1 << (int)Map.World);

                        if (!spellInfo.Worlds.HasFlag(worldFlag))
                            return DataNameProvider.WrongWorld;

                        if (SpellInfos.GetSPCost(Features, spell, CurrentPartyMember) > CurrentPartyMember.SpellPoints.CurrentValue)
                            return DataNameProvider.NotEnoughSP;

                        // TODO: Is there more to check? Irritated?

                        return null;
                    },
                    spell => PickBattleSpell(spell)
                );
                EndSequence();
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
            pickedSpell = spell;
            spellItemSlotIndex = itemSlotIndex;
            spellItemIsEquipped = itemIsEquipped;
            currentPickingActionMember = caster ?? CurrentPartyMember;
            SetPlayerBattleAction(Battle.BattleActionType.None);

            if (currentPickingActionMember == CurrentPartyMember)
            {
                highlightBattleFieldSprites.ForEach(s => s?.Delete());
                highlightBattleFieldSprites.Clear();
            }

            var spellInfo = SpellInfos[pickedSpell];

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
                    partyAdvances = false;

                    // Note: It is important to move closer rows first. Otherwise monsters
                    // will move to occupied spots and replace the monsters there before they move.
                    for (int i = monsters.Count - 1; i >= 0; --i)
                        currentBattle.MoveCharacterTo(newPositions[i], monsters[i]);

                    layout.EnableButton(4, currentBattle.CanPartyMoveForward);
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

        partyAdvances = true;
    }

    internal void UpdateActiveBattleSpells()
    {
        foreach (var activeSpell in EnumHelper.GetValues<ActiveSpellType>())
        {
            if (activeSpell.AvailableInBattle() && CurrentSavegame.ActiveSpells[(int)activeSpell] != null)
                layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell], true);
        }
    }

    internal void HideActiveBattleSpells()
    {
        layout.RemoveAllActiveSpells();
    }

    internal void AddCondition(Condition condition, PartyMember target = null)
    {
        if (Godmode)
            return;

        target ??= CurrentPartyMember;

        if (condition >= Condition.DeadCorpse && target.Alive)
        {
            KillPartyMember(target, condition);
            return;
        }

        target.Conditions |= condition;

        if (CurrentPartyMember == target)
        {
            if (RecheckActivePartyMember(out bool gameOver))
            {
                if (gameOver)
                    return;

                if (condition == Condition.Blind)
                    UpdateLight();
            }
        }

        layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
        layout.UpdateCharacter(target);
    }

    internal void RemoveCondition(Condition condition, Character target)
    {
        bool removeExhaustion = condition == Condition.Exhausted && target.Conditions.HasFlag(Condition.Exhausted);

        // Healing spells or potions.
        // Sleep can be removed by attacking as well.
        target.Conditions &= ~condition;

        if (target is PartyMember partyMember)
        {
            if (BattleActive)
            {
                UpdateBattleStatus(partyMember);
                currentBattle.RemoveCondition(condition, target);
            }
            layout.UpdateCharacterNameColors(CurrentSavegame.ActivePartyMemberSlot);
            layout.UpdateCharacter(partyMember);

            if (removeExhaustion)
                RemoveExhaustion(partyMember);
            else if (condition == Condition.Blind && partyMember == CurrentPartyMember)
                UpdateLight();
        }
    }

    uint AddExhaustion(PartyMember partyMember, uint hours, bool crippleAttributes)
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

        if (crippleAttributes && hitPoints > 0)
        {
            foreach (var attribute in EnumHelper.GetValues<Attribute>())
            {
                partyMember.Attributes[attribute].StoredValue = partyMember.Attributes[attribute].CurrentValue;
                partyMember.Attributes[attribute].CurrentValue >>= 1;
            }

            foreach (var skill in EnumHelper.GetValues<Skill>())
            {
                partyMember.Skills[skill].StoredValue = partyMember.Skills[skill].CurrentValue;
                partyMember.Skills[skill].CurrentValue >>= 1;
            }
        }

        return Math.Min(totalDamage, partyMember.HitPoints.CurrentValue);
    }

    void RemoveExhaustion(PartyMember partyMember)
    {
        foreach (var attribute in EnumHelper.GetValues<Attribute>())
        {
            partyMember.Attributes[attribute].CurrentValue = partyMember.Attributes[attribute].StoredValue;
            partyMember.Attributes[attribute].StoredValue = 0;
        }

        foreach (var skill in EnumHelper.GetValues<Skill>())
        {
            partyMember.Skills[skill].CurrentValue = partyMember.Skills[skill].StoredValue;
            partyMember.Skills[skill].StoredValue = 0;
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

    public void KillAllMapMonsters()
    {
        if (Map == null || Map.CharacterReferences == null)
            return;

        for (uint characterIndex = 0; characterIndex < Map.CharacterReferences.Length; ++characterIndex)
        {
            var characterReference = Map.CharacterReferences[characterIndex];

            if (characterReference == null)
                break;

            if (characterReference.Type == CharacterType.Monster)
                SetMapCharacterBit(Map.Index, characterIndex, true);
        }
    }

    public bool EndBattle(bool flee)
    {
        if (currentBattle == null || currentBattle.RoundActive)
            return false;

        if (PopupActive)
            ClosePopup();

        currentBattle.EndBattle(flee);
        return true;
    }

    public Savegame GetCurrentSavegame()
    {
        return CurrentSavegame;
    }

    public void ActivateLight(uint level)
    {
        ActivateLight(180, level);
    }

    void ActivateLight(uint duration, uint level)
    {
        CurrentSavegame.ActivateSpell(ActiveSpellType.Light, duration, level);
        UpdateLight(false, true);
    }

    internal void ActivateBuff(ActiveSpellType buff, uint value, uint duration)
    {
        if (buff == ActiveSpellType.Light)
            ActivateLight(duration, value);
        else
            CurrentSavegame.ActivateSpell(buff, duration, value);
    }

    void Cast(Action action, Action finishAction = null, Action failAction = null, bool checkFail = true)
    {
        failAction ??= () => ShowMessagePopup(DataNameProvider.TheSpellFailed);

        if (finishAction == null)
        {
            if (checkFail)
                TrySpell(action, failAction);
            else
                action?.Invoke();
        }
        else
        {
            if (checkFail)
            {
                TrySpell(() =>
                {
                    action?.Invoke();
                    finishAction();
                }, () =>
                {
                    failAction?.Invoke();
                    finishAction();
                });
            }
            else
            {
                action?.Invoke();
                finishAction();
            }
        }
    }

    void ApplySpellEffect(Spell spell, Character caster, Action finishAction, bool checkFail)
    {
        CurrentSpellTarget = null;

        void Cast(Action action, Action finishAction = null, Action failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        switch (spell)
        {
            case Spell.Light:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 1 (Light radius 1)
                Cast(() => ActivateLight(30, 1), finishAction);
                break;
            case Spell.MagicalTorch:
                // Duration: 60 (300 minutes = 5h)
                // Level: 1 (Light radius 1)
                Cast(() => ActivateLight(60, 1), finishAction);
                break;
            case Spell.MagicalLantern:
                // Duration: 120 (600 minutes = 10h)
                // Level: 2 (Light radius 2)
                Cast(() => ActivateLight(120, 2), finishAction);
                break;
            case Spell.MagicalSun:
                // Duration: 180 (900 minutes = 15h)
                // Level: 3 (Light radius 3)
                Cast(() => ActivateLight(180, 3), finishAction);
                break;
            case Spell.Jump:
					Cast(Jump, finishAction);
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
                        ShowMessagePopup(DataNameProvider.MarksPosition, finishAction);
                    }
                    else
                    {
                        finishAction?.Invoke();
                    }
                }, null, finishAction);
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
                            ShowMessagePopup(DataNameProvider.HasntMarkedAPosition, finishAction);
                        }
                        else
                        {
                            void Return()
                            {
                                Teleport(partyMember.MarkOfReturnMapIndex, partyMember.MarkOfReturnX, partyMember.MarkOfReturnY, player.Direction, out _, true);
                                finishAction?.Invoke();
                            }
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
                    else
                    {
                        finishAction?.Invoke();
                    }
                }, null, finishAction);
                break;
            }
            case Spell.MagicalShield:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 10 (10% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 30, 10), finishAction);
                break;
            case Spell.MagicalWall:
                // Duration: 90 (450 minutes = 7h30m)
                // Level: 20 (20% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 90, 20), finishAction);
                break;
            case Spell.MagicalBarrier:
                // Duration: 180 (900 minutes = 15h)
                // Level: 30 (30% defense increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30), finishAction);
                break;
            case Spell.MagicalWeapon:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 10 (10% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 30, 10), finishAction);
                break;
            case Spell.MagicalAssault:
                // Duration: 90 (450 minutes = 7h30m)
                // Level: 20 (20% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 90, 20), finishAction);
                break;
            case Spell.MagicalAttack:
                // Duration: 180 (900 minutes = 15h)
                // Level: 30 (30% damage increase)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30), finishAction);
                break;
            case Spell.Levitation:
                Cast(Levitate, finishAction);
                break;
            case Spell.Rope:
            {
                if (!is3D)
                {
                    ShowMessagePopup(DataNameProvider.CannotClimbHere, finishAction);
                }
                else
                {
                    Levitate(() => ShowMessagePopup(DataNameProvider.CannotClimbHere, finishAction), false);
                }
                break;
            }
            case Spell.AntiMagicWall:
                // Duration: 30 (150 minutes = 2h30m)
                // Level: 15 (15% anti-magic protection)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 30, 15), finishAction);
                break;
            case Spell.AntiMagicSphere:
                // Duration: 180 (900 minutes = 15h)
                // Level: 25 (25% anti-magic protection)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25), finishAction);
                break;
            case Spell.AlchemisticGlobe:
                // Duration: 180 (900 minutes = 15h)
                Cast(() =>
                {
                    ActivateLight(180, 3);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Protection, 180, 30);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Attack, 180, 30);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.AntiMagic, 180, 25);
                }, finishAction);
                break;
            case Spell.Knowledge:
                // Duration: 30 (150 minutes = 2h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 30, 20), finishAction);
                break;
            case Spell.Clairvoyance:
                // Duration: 90 (450 minutes = 7h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 90, 40), finishAction);
                break;
            case Spell.SeeTheTruth:
                // Duration: 180 (900 minutes = 15h)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 60), finishAction);
                break;
            case Spell.MapView:
                Cast(() => OpenMiniMap(finishAction), null, finishAction);
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
                        finishAction?.Invoke();
                    };
                }, null, finishAction);
                break;
            }
            case Spell.FindTraps:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = false,
                    PersonsVisible = false,
                    TrapsVisible = true
                }, finishAction), null, finishAction);
                break;
            case Spell.FindMonsters:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = true,
                    PersonsVisible = false,
                    TrapsVisible = false
                }, finishAction), null, finishAction);
                break;
            case Spell.FindPersons:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = false,
                    MonstersVisible = false,
                    PersonsVisible = true,
                    TrapsVisible = false
                }, finishAction), null, finishAction);
                break;
            case Spell.FindSecretDoors:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = true,
                    MonstersVisible = false,
                    PersonsVisible = false,
                    TrapsVisible = false
                }, finishAction), null, finishAction);
                break;
            case Spell.MysticalMapping:
                Cast(() => ShowAutomap(new AutomapOptions
                {
                    SecretDoorsVisible = true,
                    MonstersVisible = true,
                    PersonsVisible = true,
                    TrapsVisible = true
                }, finishAction), null, finishAction);
                break;
            case Spell.MysticalMapI:
                // Duration: 32 (160 minutes = 2h40m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 32, 1), finishAction);
                break;
            case Spell.MysticalMapII:
                // Duration: 60 (300 minutes = 5h)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 60, 1), finishAction);
                break;
            case Spell.MysticalMapIII:
                // Duration: 90 (450 minutes = 7h30m)
                Cast(() => CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 90, 1), finishAction);
                break;
            case Spell.MysticalGlobe:
                // Duration: 180 (900 minutes = 15h)
                Cast(() =>
                {
                    CurrentSavegame.ActivateSpell(ActiveSpellType.Clairvoyance, 180, 60);
                    CurrentSavegame.ActivateSpell(ActiveSpellType.MysticMap, 180, 1);
                }, finishAction);
                break;
            case Spell.Lockpicking:
                // Do nothing. Can be used by Thief/Ranger but has no effect in Ambermoon.
                finishAction?.Invoke();
                break;
            case Spell.MountWasp:
                ShowMessagePopup(DataNameProvider.MountTheWasp, () => CloseWindow(() => ActivateTransport(TravelType.Wasp)), TextAlign.Left);
                break;
            case Spell.CallEagle:
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
                            Graphics.TravelGraphicOffset + (uint)TravelType.Eagle * 4 + 1, ushort.MaxValue);
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
                                finishAction?.Invoke();
                            }
                            else
                            {
                                AddTimedEvent(TimeSpan.FromMilliseconds(40), AnimateEagle);
                            }
                        }
                    });
                }, TextAlign.Left);
                break;
            case Spell.PlayElfHarp:
                OpenMusicList(finishAction);
                break;
            case Spell.MagicalMap:
                // TODO: In original this has no effect. Maybe it was planned to show
                // the real map that was inside the original package.
                // For now we show the minimap instead.
                OpenMiniMap(finishAction);
                break;
            case Spell.SelfHealing:
            case Spell.SelfReviving:
                ApplySpellEffect(spell, caster, caster, finishAction, checkFail);
                break;
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no spell without target.");
        }
    }

    void TrySpell(Action successAction, Action failAction)
    {
        long chance = CurrentPartyMember.Skills[Skill.UseMagic].TotalCurrentValue;

        if (Features.HasFlag(Features.ExtendedCurseEffects) &&
            CurrentPartyMember.Conditions.HasFlag(Condition.Drugged))
            chance -= 25;

        if (RollDice100() < chance)
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

        void Cast(Action action, Action finishAction = null, Action failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        void PlayItemMagicAnimation(Action animationFinishAction = null)
        {
            ItemAnimation.Play(this, renderView, ItemAnimation.Type.Enchant, layout.GetItemSlotPosition(itemSlot, true),
                animationFinishAction ?? finishAction, TimeSpan.FromMilliseconds(50));
        }

        void Error(string message)
        {
            EndSequence();
            ShowMessagePopup(message, finishAction, TextAlign.Left);
        }

        switch (spell)
        {
            case Spell.Identification:
            {
                Cast(() =>
                {
                    itemSlot.Flags |= ItemSlotFlags.Identified;
                    PlayItemMagicAnimation(() =>
                    {
                        EndSequence();
                        UntrapMouse();
                        ShowItemPopup(itemSlot, finishAction);
                    });
                }, null, () =>
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
                if (item.MaxRechargesSpell != 0 && item.MaxRechargesSpell != 255 && itemSlot.RechargeTimes >= item.MaxRechargesSpell)
                {
                    Error(DataNameProvider.CannotRechargeAnymore);
                    return;                        
                }
                Cast(() =>
                {
                    itemSlot.NumRemainingCharges += RandomInt(1, Math.Min(item.MaxCharges - itemSlot.NumRemainingCharges, caster.Level));
                    PlayItemMagicAnimation(finishAction);
                }, null, () =>
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
                    PlayItemMagicAnimation(finishAction);
                }, null, () =>
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
                if (!item.Flags.HasFlag(ItemFlags.Cloneable))
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
                }, null, () =>
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
                }, null, Fail);
                break;
            }
            default:
                throw new AmbermoonException(ExceptionScope.Application, $"The spell {spell} is no item-targeted spell.");
        }
    }

    void ExchangeExp(PartyMember caster, PartyMember target, Action finishAction)
    {
        uint casterExp = caster.ExperiencePoints;
        uint targetExp = target.ExperiencePoints;

        if (casterExp == targetExp)
            return;

        if (caster.MaxReachedLevel == 0)
            caster.MaxReachedLevel = caster.Level;
        if (target.MaxReachedLevel == 0)
            target.MaxReachedLevel = target.Level;

        var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);
        var initialCaster = initialSavegame.PartyMembers[caster.Index];
        var initialTarget = initialSavegame.PartyMembers[target.Index];

        static void Init(Character character, Character initialCharacter)
        {
            character.Level = initialCharacter.Level;
            character.HitPoints.CurrentValue = initialCharacter.HitPoints.CurrentValue;
            character.HitPoints.MaxValue = initialCharacter.HitPoints.MaxValue;
            character.SpellPoints.CurrentValue = initialCharacter.SpellPoints.CurrentValue;
            character.SpellPoints.MaxValue = initialCharacter.SpellPoints.MaxValue;
            character.ExperiencePoints = 0;
            character.AttacksPerRound = 1;

            while (character.Level > 1)
            {
                --character.Level;
                character.HitPoints.CurrentValue -= character.HitPointsPerLevel;
                character.HitPoints.MaxValue -= character.HitPointsPerLevel;
                character.SpellPoints.CurrentValue -= character.SpellPointsPerLevel;
                character.SpellPoints.MaxValue -= character.SpellPointsPerLevel;
            }
        }

        Init(caster, initialCaster);
        Init(target, initialTarget);

        AddExperience(caster, targetExp, () =>
        {
            AddExperience(target, casterExp, () =>
            {
                UpdateCharacterInfo();
                layout.FillCharacterBars(caster);
                layout.FillCharacterBars(target);
                finishAction?.Invoke();
            });
        });
    }

    void ApplySpellEffect(Spell spell, Character caster, Character target, Action finishAction, bool checkFail)
    {
        CurrentSpellTarget = target;

        void Cast(Action action, Action finishAction = null, Action failAction = null)
        {
            this.Cast(action, finishAction, failAction, checkFail);
        }

        switch (spell)
        {
            case Spell.Hurry:
            case Spell.MassHurry:
                // Note: This is handled by battle code
                finishAction?.Invoke();
                break;
            case Spell.RemoveFear:
            case Spell.RemovePanic:
                Cast(() => RemoveCondition(Condition.Panic, target), finishAction);
                break;
            case Spell.RemoveShadows:
            case Spell.RemoveBlindness:
                Cast(() => RemoveCondition(Condition.Blind, target), finishAction);
                break;
            case Spell.RemovePain:
            case Spell.RemoveDisease:
                Cast(() => RemoveCondition(Condition.Diseased, target), finishAction);
                break;
            case Spell.RemovePoison:
            case Spell.NeutralizePoison:
                Cast(() => RemoveCondition(Condition.Poisoned, target), finishAction);
                break;
            case Spell.HealingHand:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 10), finishAction); // 10%
                break;
            case Spell.SmallHealing:
            case Spell.MassHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 4), finishAction); // 25%
                break;
            case Spell.MediumHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue / 2), finishAction); // 50%
                break;
            case Spell.GreatHealing:
                Cast(() => Heal(target.HitPoints.TotalMaxValue * 3 / 4), finishAction); // 75%
                break;
            case Spell.RemoveRigidness:
            case Spell.RemoveLamedness:
                Cast(() => RemoveCondition(Condition.Lamed, target), finishAction);
                break;
            case Spell.HealAging:
            case Spell.StopAging:
                Cast(() => RemoveCondition(Condition.Aging, target), finishAction);
                break;
            case Spell.StoneToFlesh:
                Cast(() => RemoveCondition(Condition.Petrified, target), finishAction);
                break;
            case Spell.WakeUp:
                Cast(() => RemoveCondition(Condition.Sleep, target), finishAction);
                break;
            case Spell.RemoveIrritation:
                Cast(() => RemoveCondition(Condition.Irritated, target), finishAction);
                break;
            case Spell.RemoveDrugged:
                Cast(() => RemoveCondition(Condition.Drugged, target), finishAction);
                break;
            case Spell.RemoveMadness:
                Cast(() => RemoveCondition(Condition.Crazy, target), finishAction);
                break;
            case Spell.RestoreStamina:
                Cast(() => RemoveCondition(Condition.Exhausted, target), finishAction);
                break;
            case Spell.CreateFood:
                Cast(() => ++target.Food, finishAction);
                break;
            case Spell.ExpExchange:
                Cast(() => ExchangeExp(caster as PartyMember, target as PartyMember, finishAction), null, finishAction);
                break;
            case Spell.SelfHealing:
                Cast(() =>
                {
                    if (target.Alive)
                        Heal(5 + target.HitPoints.TotalMaxValue / 4); // 5 HP + 25% of MaxHP
                }, finishAction);
                break;
            case Spell.Resurrection:
            {
                Cast(() =>
                {
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.HitPoints.CurrentValue = target.HitPoints.TotalMaxValue;
                    PartyMemberRevived(target as PartyMember, finishAction, false);
                }, null, finishAction);
                break;
            }
            case Spell.SelfReviving:
            case Spell.WakeTheDead:
            {
                if (!(target is PartyMember targetPlayer))
                {
                    // Should not happen
                    finishAction?.Invoke();
                    return;
                }
                void Revive()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadCorpse) ||
                        target.Conditions.HasFlag(Condition.DeadAshes) ||
                        target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        if (target.Alive)
                            ShowMessagePopup(DataNameProvider.IsNotDead, finishAction);
                        else
                            ShowMessagePopup(DataNameProvider.CannotBeResurrected, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.HitPoints.CurrentValue = 1;
                    PartyMemberRevived(targetPlayer, finishAction, true, spell == Spell.SelfReviving);
                }
                if (checkFail)
                {
                    TrySpell(Revive, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (spell != Spell.SelfReviving && target.Conditions.HasFlag(Condition.DeadCorpse))
                            {
                                target.Conditions &= ~Condition.DeadCorpse;
                                target.Conditions |= Condition.DeadAshes;
                                ShowMessagePopup(DataNameProvider.BodyBurnsUp, finishAction);
                            }
                            else
                            {
                                finishAction?.Invoke();
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
                    finishAction?.Invoke();
                    return;
                }
                void TransformToBody()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadAshes) ||
                        target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        ShowMessagePopup(DataNameProvider.IsNotAsh, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadAshes;
                    target.Conditions |= Condition.DeadCorpse;
                    ShowMessagePopup(DataNameProvider.AshesChangedToBody, finishAction);
                }
                if (checkFail)
                {
                    TrySpell(TransformToBody, () =>
                    {
                        EndSequence();
                        ShowMessagePopup(DataNameProvider.TheSpellFailed, () =>
                        {
                            if (target.Conditions.HasFlag(Condition.DeadAshes))
                            {
                                target.Conditions &= ~Condition.DeadAshes;
                                target.Conditions |= Condition.DeadDust;
                                ShowMessagePopup(DataNameProvider.AshesFallToDust, finishAction);
                            }
                            else
                            {
                                finishAction?.Invoke();
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
                    finishAction?.Invoke();
                    return;
                }
                void TransformToAshes()
                {
                    if (!target.Conditions.HasFlag(Condition.DeadDust))
                    {
                        ShowMessagePopup(DataNameProvider.IsNotDust, finishAction);
                        return;
                    }
                    target.Conditions &= ~Condition.DeadDust;
                    target.Conditions |= Condition.DeadAshes;
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
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsII:
                FillSP(target.SpellPoints.TotalMaxValue / 4); // 25%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsIII:
                FillSP(target.SpellPoints.TotalMaxValue / 2); // 50%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsIV:
                FillSP(target.SpellPoints.TotalMaxValue * 3 / 4); // 75%
                finishAction?.Invoke();
                break;
            case Spell.SpellPointsV:
                FillSP(target.SpellPoints.TotalMaxValue); // 100%
                finishAction?.Invoke();
                break;
            case Spell.AllHealing:
            {
                void HealAll()
                {
                    // Removes all curses and heals full LP
                    Heal(target.HitPoints.TotalMaxValue);
                    foreach (var condition in EnumHelper.GetValues<Condition>())
                    {
                        if (condition != Condition.None && target.Conditions.HasFlag(condition))
                            RemoveCondition(condition, target);
                    }
                    finishAction?.Invoke();
                }
                if (!target.Alive)
                {
                    target.Conditions &= ~Condition.DeadCorpse;
                    target.Conditions &= ~Condition.DeadAshes;
                    target.Conditions &= ~Condition.DeadDust;
                    target.HitPoints.CurrentValue = 1;
                    PartyMemberRevived(target as PartyMember, HealAll);
                }
                else
                {
                    HealAll();
                }
                break;
            }
            case Spell.AddStrength:
                IncreaseAttribute(Attribute.Strength);
                finishAction?.Invoke();
                break;
            case Spell.AddIntelligence:
                IncreaseAttribute(Attribute.Intelligence);
                finishAction?.Invoke();
                break;
            case Spell.AddDexterity:
                IncreaseAttribute(Attribute.Dexterity);
                finishAction?.Invoke();
                break;
            case Spell.AddSpeed:
                IncreaseAttribute(Attribute.Speed);
                finishAction?.Invoke();
                break;
            case Spell.AddStamina:
                IncreaseAttribute(Attribute.Stamina);
                finishAction?.Invoke();
                break;
            case Spell.AddCharisma:
                IncreaseAttribute(Attribute.Charisma);
                finishAction?.Invoke();
                break;
            case Spell.AddLuck:
                IncreaseAttribute(Attribute.Luck);
                finishAction?.Invoke();
                break;
            case Spell.AddAntiMagic:
                IncreaseAttribute(Attribute.AntiMagic);
                finishAction?.Invoke();
                break;
            case Spell.DecreaseAge:
                if (target.Alive && !target.Conditions.HasFlag(Condition.Petrified) && target.Attributes[Attribute.Age].CurrentValue > 18)
                {
                    target.Attributes[Attribute.Age].CurrentValue = (uint)Math.Max(18, (int)target.Attributes[Attribute.Age].CurrentValue - RandomInt(1, 10));

                    if (CurrentWindow.Window == Window.Inventory && CurrentInventory == target)
                        UpdateCharacterInfo();
                }
                finishAction?.Invoke();
                break;
            case Spell.Drugs:
                if (target is PartyMember partyMember)
                    AddCondition(Condition.Drugged, partyMember);
                finishAction?.Invoke();
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
                UpdateCharacterInfo();
            }
        }

        void Heal(uint amount)
        {
            target.Heal(amount);

            if (target is PartyMember partyMember)
            {
                layout.FillCharacterBars(partyMember);

                if (CurrentInventory == partyMember)
                    UpdateCharacterInfo();
            }
        }

        void FillSP(uint amount)
        {
            target.SpellPoints.CurrentValue = Math.Min(target.SpellPoints.TotalMaxValue, target.SpellPoints.CurrentValue + amount);
            layout.FillCharacterBars(target as PartyMember);

            if (CurrentInventory == target)
                UpdateCharacterInfo();
        }
    }

    /// <summary>
    /// Sets the speed of battles.
    /// 
    /// A value of 0 is the normal speed and will need a click to acknowledge battle actions.
    /// </summary>
    /// <param name="speed">Value from 0 to 100 where 0 is the normal speed.</param>
    internal void SetBattleSpeed(int speed)
    {
        if (currentBattle != null)
        {
            currentBattle.NeedsClickForNextAction = speed == 0;
            currentBattle.Speed = speed;
        }
    }

    MonsterGroup CloneMonsterGroup(MonsterGroup monsterGroup)
    {
        Monster CloneMonster(Monster monster)
        {
            if (monster == null)
                return null;

            return CharacterManager.CloneMonster(monster);
        }

        var clone = new MonsterGroup();

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                clone.Monsters[x, y] = CloneMonster(monsterGroup.Monsters[x, y]);
            }
        }

        return clone;
		}

    void ShowBattleWindow(Event nextEvent, bool failedFlight, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        allInputDisabled = true;
        Fade(() =>
        {
            lastPlayedSong = PlayMusic(Song.SapphireFireballsOfPureLove);
            roundPlayerBattleActions.Clear();
            ShowBattleWindow(nextEvent, out byte paletteIndex, x, y, combatBackgroundIndex);
            // Note: Create clones so we can change the values in battle for each monster.
            var monsterGroup = CloneMonsterGroup(CharacterManager.GetMonsterGroup(currentBattleInfo.MonsterGroupIndex));
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
                monsterGroup, monsterBattleAnimations, Configuration.BattleSpeed == 0);
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
                layout.EnableButton(4, currentBattle.CanPartyMoveForward);

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
                if (currentBattle != null)
                {
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        if (partyMemberBattleFieldTooltips[i] != null)
                        {
                            var partyMember = GetPartyMember(i);
                            int position = currentBattle.GetSlotFromCharacter(partyMember);
                            partyMemberBattleFieldTooltips[i].Area = new Rect
                            (
                                Global.BattleFieldX + (position % 6) * Global.BattleFieldSlotWidth,
                                Global.BattleFieldY + (position / 6) * Global.BattleFieldSlotHeight - 1,
                                Global.BattleFieldSlotWidth,
                                Global.BattleFieldSlotHeight + 1
                            );
                            partyMemberBattleFieldTooltips[i].Text =
                                $"{partyMember.HitPoints.CurrentValue}/{partyMember.HitPoints.TotalMaxValue}^{partyMember.Name}";
                        }
                    }
                    UpdateActiveBattleSpells();
                }
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
                            partyMember.Conditions = partyMember.Conditions.WithoutBattleOnlyConditions();
                    }
                    roundPlayerBattleActions.Clear();
                    UpdateBattleStatus();
                    if (lastPlayedSong != null)
                    {
                        var temp = lastPlayedSong; // preserve as window close will play the map song otherwise
                        PlayMusic(lastPlayedSong.Value);
                        lastPlayedSong = temp;
                    }
                    else if (Map.UseTravelMusic)
                        PlayMusic(travelType.TravelSong());
                    else
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
                            EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always,
                                x, y, nextEvent, true);
                        }
                    });
                }
                else if (PartyMembers.Any(p => p.Alive && p.Conditions.CanFight()))
                {
                    // There are fled survivors
                    currentBattle = null;
                    EndBattle();
                    CloseWindow(() =>
                    {
                        this.NewLeaderPicked += NewLeaderPicked;
                        allInputDisabled = false;
                        RecheckActivePartyMember(out bool _);

                        void Finish()
                        {
                            this.NewLeaderPicked -= NewLeaderPicked;
                            InputEnable = true;
                            allInputDisabled = false;
                            if (nextEvent != null)
                            {
                                EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, x, y, nextEvent, false);
                            }
                        }

                        if (!pickingNewLeader)
                        {
                            Finish();
                        }

                        void NewLeaderPicked(int _)
                        {
                            Finish();
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
                void ShowFailedFlightMessage()
                {
                    currentBattle.StartAnimationFinished -= ShowFailedFlightMessage;
                    SetBattleMessageWithClick(DataNameProvider.AttackEscapeFailedMessage, TextColor.BrightGray, () => StartBattleRound(true));
                }

                if (currentBattle.HasStartAnimation)
                    currentBattle.StartAnimationFinished += ShowFailedFlightMessage;
                else
                    ShowFailedFlightMessage();
            }
        }, false);
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

        // Attributes, skills, LP and SP is special for monsters.
        foreach (var attribute in EnumHelper.GetValues<Attribute>().Take(8))
            FixValue(game, monster.Attributes[attribute]);
        foreach (var skill in EnumHelper.GetValues<Skill>())
            FixValue(game, monster.Skills[skill]);
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
            layout.UpdateCharacterStatus(partyMemberSlot, CurrentPartyMember.Conditions.CanSelect() ? (UIGraphic?)null : GetDisabledStatusGraphic(CurrentPartyMember));
        }

        layout.EnableButton(0, battleFieldSlot >= 24 && CurrentPartyMember.CanFlee()); // flee button, only enable in last row
        layout.EnableButton(3, CurrentPartyMember.CanMove()); // Note: If no slot is available the button still is enabled but after clicking you get "You can't move anywhere".
        layout.EnableButton(4, currentBattle.CanPartyMoveForward);
        layout.EnableButton(6, CurrentPartyMember.BaseAttackDamage + CurrentPartyMember.BonusAttackDamage > 0 && CurrentPartyMember.Conditions.CanAttack());
        layout.EnableButton(7, CurrentPartyMember.Conditions.CanParry());
        layout.EnableButton(8, CurrentPartyMember.Conditions.CanCastSpell(Features) && CurrentPartyMember.HasAnySpell());
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
                    switch (SpellInfos[spell].Target)
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
                            layout.SetBattleFieldSlotColor(blinkCharacterSlot, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks);
                            layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileOrRowFromParameter(action.Parameter), BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks + Layout.TicksPerBlink);
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
        bool remove = !partyMember.Conditions.CanSelect();

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
                    if (partyMember.BaseAttackDamage + partyMember.BonusAttackDamage <= 0 || !partyMember.Conditions.CanAttack())
                        remove = true;
                    break;
                case Battle.BattleActionType.Parry:
                    if (!partyMember.Conditions.CanParry())
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

        if (currentBattle.RoundActive)
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

                    if (currentPickingActionMember != partyMember && partyMember.Conditions.CanSelect())
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
                    uint maxDist = 1 + currentPickingActionMember.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                    if (Math.Abs(column - position % 6) > maxDist || Math.Abs(row - position / 6) > maxDist)
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
                    if (!target.Conditions.CanBlink())
                    {
                        CancelSpecificPlayerAction();
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
                        layout.SetBattleFieldSlotColor((int)blinkCharacterPosition.Value, selfBlink ? BattleFieldSlotColor.Both : BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks);
                        layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange, CurrentNormalizedBattleTicks + Layout.TicksPerBlink);
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
                uint maxDist = 1 + currentPickingActionMember.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                int currentPosition = currentBattle.GetSlotFromCharacter(currentPickingActionMember);
                int currentColumn = currentPosition % 6;
                int currentRow = currentPosition / 6;
                if (row > 2 && Math.Abs(column - currentColumn) <= maxDist &&
                    Math.Abs(row - currentRow) <= maxDist &&
                    currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
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

    bool CheckAbilityToAttack(out bool ranged, bool silent = false)
    {
        ranged = currentPickingActionMember.HasLongRangedAttack(ItemManager, out bool hasAmmo);

        if (ranged && !hasAmmo)
        {
            // No ammo for ranged weapon
            CancelSpecificPlayerAction();
            if (!silent)
                SetBattleMessageWithClick(DataNameProvider.BattleMessageNoAmmunition, TextColor.BrightGray);
            return false;
        }

        if (currentPickingActionMember.BaseAttackDamage + currentPickingActionMember.BonusAttackDamage <= 0 || !currentPickingActionMember.Conditions.CanAttack())
        {
            CancelSpecificPlayerAction();
            if (!silent)
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
                int maxDist = 1 + (int)currentPickingActionMember.Attributes[Attribute.Speed].TotalCurrentValue / 80;
                var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.IsBattleFieldEmpty(position),
                    maxDist, 3, 4);
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

    void ShowMobileClickIndicator(int x, int y)
    {
        if (Configuration.IsMobile && mobileClickIndicator != null)
        {
            mobileClickIndicator.PaletteIndex = UIPaletteIndex;
            mobileClickIndicator.X = x;
				mobileClickIndicator.Y = y;
				mobileClickIndicator.Visible = true;
			}
    }

    internal void ShowMobileClickIndicatorForPopup()
    {
        var position = layout.GetPopupClickIndicatorPosition();
			ShowMobileClickIndicator(position.X, position.Y);
		}

		void HideMobileClickIndicator()
		{
        if (mobileClickIndicator != null)
			    mobileClickIndicator.Visible = false;
		}

		void GameOver()
    {
        PlayMusic(Song.GameOver);
        ShowEvent(ProcessText(DataNameProvider.GameOverMessage), 8, null, true);
    }

    float Get3DLight()
    {
        uint usedLightIntensity;

        if (CurrentPartyMember.Conditions.HasFlag(Condition.Blind))
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

    private uint GetDaytimeLightIntensity()
    {
        uint hour = GameTime.Hour;

        if (GameTime.Minute == 60) // this might happen during a minute tick just before the hours are adjusted
            hour = (hour + 1) % 24;

        return GetDaytimeLightIntensity(hour);
    }

    private uint GetDaytimeLightIntensity(uint hour)
    {
        // 17:00-18:59: 128
        // 19:00-19:59: 80
        // 20:00-05:59: 32
        // 06:00-06:59: 80
        // 07:00-07:59: 128
        // 08:00-16:59: 255

        if (hour < 6 || hour >= 20)
            return 32;
        else if (hour < 7)
            return 80;
        else if (hour < 8)
            return 128;
        else if (hour < 17)
            return 255;
        else if (hour < 19)
            return 128;
        else if (hour < 20)
            return 80;
        else
            return 32;
    }

    internal void UpdateLight(bool mapChange = false, bool lightActivated = false, bool playerSwitched = false, Map map = null,
        uint? customOutdoorLightIntensity = null)
    {
        map ??= Map;

        if (map == null)
            return;

        void ChangeLightRadius(int lastRadius, int newRadius)
        {
            var oldMap = map;
            var lightLevel = CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light);
            const int timePerChange = 75;
            var timeSpan = TimeSpan.FromMilliseconds(timePerChange);

            void ChangeLightRadius()
            {
                if (oldMap != map || // map changed
                    lightLevel != CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light)) // light buff changed
                    return;

                int diff = newRadius - lastRadius;

                if (diff != 0)
                {
                    int change = mapChange || playerSwitched ? diff : Math.Sign(diff) * Math.Min(Math.Abs(diff), 8);
                    lastRadius += change;
                    fow2D.Radius = (byte)lastRadius;
                    fow2D.Visible = !is3D && lastRadius < 112;

                    if (newRadius - lastRadius != 0)
                        AddTimedEvent(timeSpan, ChangeLightRadius);
                }
            }

            if (mapChange || playerSwitched)
                ChangeLightRadius();
            else
                AddTimedEvent(timeSpan, ChangeLightRadius);
        }

        if (TravelType == TravelType.Fly)
        {
            // Full light
            lightIntensity = 255;
            fow2D.Visible = false;
        }
        else if (CurrentPartyMember.Conditions.HasFlag(Condition.Blind))
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
            // Each light spell level adds an additional 32.

            if (!is3D || customOutdoorLightIntensity == null)
            {
                uint lastIntensity = lightIntensity;

                lightIntensity = GetDaytimeLightIntensity();
                lightIntensity = Math.Min(255, lightIntensity + CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light) * 32);

                if (!is3D && (lastIntensity != lightIntensity || mapChange))
                {
                    var lastRadius = mapChange ? 0 : (int)(lastIntensity >> 1);
                    var newRadius = (int)(lightIntensity >> 1);
                    fow2D.Visible = lastIntensity < 224;
                    ChangeLightRadius(lastRadius, newRadius);
                }
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
            uint lightBuffIntensity = Map.Flags.HasFlag(MapFlags.Outdoor)
                ? (uint)Math.Max(0, (customOutdoorLightIntensity ?? lightIntensity) - (long)GetDaytimeLightIntensity())
                : lightIntensity;
            renderMap3D.UpdateSky(lightEffectProvider, GameTime, lightBuffIntensity);
            renderMap3D.SetColorLightFactor(light3D);
            renderMap3D.SetFog(Map, MapManager.GetLabdataForMap(Map));
        }
        else // 2D
        {
            GetTransportsInVisibleArea(out TransportLocation transportAtPlayerIndex);
            player2D ??= new Player2D(this, renderView.GetLayer(Layer.Characters), player, renderMap2D,
                renderView.SpriteFactory, new Position(0, 0), MapManager);
            player2D.BaselineOffset = !CanSee() || transportAtPlayerIndex != null ? MaxBaseLine :
                player.MovementAbility > PlayerMovementAbility.Swimming ? 32 : 0;
        }
    }

    internal uint DistributeGold(uint gold, bool force)
    {
        var partyMembers = PartyMembers.Where(p => p.Race != Race.Animal).ToList();

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
        var partyMembers = PartyMembers.Where(p => p.Race != Race.Animal).ToList();

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
        currentPlace = enchanter;

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
                layout.ButtonsDisabled = true;
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
							layout.ButtonsDisabled = false;
							CursorType = CursorType.Sword;
                        inputEnable = true;
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
					layout.ButtonsDisabled = false;
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

                if (enchanter.AvailableGold < enchanter.Cost)
                {
                    Error(DataNameProvider.NotEnoughMoney, true);
                    return;
                }

                var item = ItemManager.GetItem(itemSlot.ItemIndex);

                if (item.Spell == Spell.None || (item.InitialCharges == 0 && item.MaxCharges == 0))
                {
                    Error(DataNameProvider.CannotEnchantOrdinaryItem, false);
                    return;
                }

                if (item.MaxCharges == 0)
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

                if (item.MaxRecharges != 0 && item.MaxRecharges != 255 && itemSlot.RechargeTimes >= item.MaxRecharges)
                {
                    Error(DataNameProvider.CannotRechargeAnymore, false);
                    return;
                }

                void Enchant(uint charges)
                {
                    ClosePopup();
                    uint cost = charges * (uint)enchanter.Cost;

                    nextClickHandler = null;
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

                            if (item.MaxRecharges != 0 && item.MaxRecharges != 255 && itemSlot.RechargeTimes == item.MaxRecharges - 1)
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
        currentPlace = sage;

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
            void ShowItems(bool equipment, bool scrollIdentification = false)
            {
                itemsGrid.Disabled = false;
                itemsGrid.DisableDrag = true;
                ShowDefaultMessage();
                CursorType = CursorType.Sword;
                var itemArea = new Rect(16, 139, 151, 53);
                TrapMouse(itemArea);
					layout.ButtonsDisabled = true;
					itemsGrid.Initialize(equipment ? CurrentPartyMember.Equipment.Slots.Where(s => s.Value.ItemIndex != 0).Select(s => s.Value).ToList()
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
								layout.ButtonsDisabled = false;
								CursorType = CursorType.Sword;
                            inputEnable = true;
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
						layout.ButtonsDisabled = false;
					}
                itemsGrid.ItemClicked += ItemClicked;
                void ItemClicked(ItemGrid _, int slotIndex, ItemSlot itemSlot)
                {
                    itemsGrid.HideTooltip();

                    void Message(string message, bool abort)
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

                    if (scrollIdentification)
                    {
                        if (ItemManager.GetItem(itemSlot.ItemIndex).Type != ItemType.SpellScroll)
                        {
                            Message(DataNameProvider.ThatsNotASpellScroll, false);
                            return;
                        }
                    }
                    else if (itemSlot.Flags.HasFlag(ItemSlotFlags.Identified))
                    {
                        Message(DataNameProvider.ItemAlreadyIdentified, false);
                        return;
                    }

                    int cost = scrollIdentification ? sage.TellingSLPCost : sage.IdentificationCost;

                    if (sage.AvailableGold < cost)
                    {
                        Message(DataNameProvider.NotEnoughMoney, true);
                        return;
                    }

                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.PriceForExamining}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        nextClickHandler = null;

                        void Finish()
                        {
                            EndSequence();
                            UntrapMouse();
                            DisableItemGrid();
                            layout.ShowChestMessage(null);

                            if (answer) // yes
                            {
                                sage.AvailableGold -= (uint)cost;
                                updatePartyGold?.Invoke();

                                if (scrollIdentification)
                                {
                                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                                    var slp = SpellInfos.GetSLPCost(Features, item.Spell);
                                    Message(DataNameProvider.SageIdentifyScroll + slp.ToString() + DataNameProvider.SageSLP, true);
                                }
                                else
                                {
                                    itemSlot.Flags |= ItemSlotFlags.Identified;
                                    ShowItemPopup(itemSlot, null);
                                }
                            }
                        }

                        if (answer)
                        {
                            ItemAnimation.Play(this, renderView, ItemAnimation.Type.Enchant, layout.GetItemSlotPosition(itemSlot, true),
                                Finish, TimeSpan.FromMilliseconds(50));
                        }
                        else
                        {
                            Finish();
                        }
                        
                    }, TextAlign.Left);
                };
            }
            // Examine equipment button
            layout.AttachEventToButton(0, () => ShowItems(true));
            // Examine inventory item button
            layout.AttachEventToButton(3, () => ShowItems(false));
            if (Features.HasFlag(Features.SageScrollIdentification))
                layout.AttachEventToButton(6, () => ShowItems(false, true));
        });
    }

    void OpenHealer(Places.Healer healer, bool showWelcome = true)
    {
        currentPlace = healer;

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
            nextClickHandler = null;
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

        void HealCondition(Condition condition, Action<bool> healedHandler)
        {
            // TODO: At the moment DeadAshes and DeadDust will be healed fully so that the
            // character is alive afterwards. As this is bugged in original I don't know how
            // it was supposed to be. Either reviving completely or transform to next stage
            // like dust to ashes and ashes to body first.

            var cost = (uint)healer.GetCostForHealingCondition(condition);
            nextClickHandler = null;
            layout.ShowPlaceQuestion($"{DataNameProvider.PriceForHealingCondition}{cost}{DataNameProvider.AgreeOnPrice}", answer =>
            {
                if (answer) // yes
                {
                    healer.AvailableGold -= cost;
                    updatePartyGold?.Invoke();
                    RemoveCondition(condition, currentlyHealedMember);
                    PlayerSwitched();
                    PlayHealAnimation(currentlyHealedMember);
                    layout.UpdateCharacterStatus(currentlyHealedMember);
                    healedHandler?.Invoke(true);
                    if (condition >= Condition.DeadCorpse) // dead
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

        var healableConditions = Condition.Lamed | Condition.Poisoned | Condition.Petrified | Condition.Diseased |
            Condition.Aging | Condition.DeadCorpse | Condition.DeadAshes | Condition.DeadDust | Condition.Crazy |
            Condition.Blind | Condition.Drugged;

        void PlayerSwitched()
        {
            layout.EnableButton(0, currentlyHealedMember.HitPoints.CurrentValue < currentlyHealedMember.HitPoints.TotalMaxValue);
            layout.EnableButton(3, currentlyHealedMember.Equipment.Slots.Any(slot => slot.Value.Flags.HasFlag(ItemSlotFlags.Cursed)));
            layout.EnableButton(6, ((uint)currentlyHealedMember.Conditions & (uint)healableConditions) != 0);
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
                }, () => ClosePopup());
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
                nextClickHandler = null;
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
                var slotConditions = new List<Condition>(12);
                // Ensure that only one dead state is present
                if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadDust))
                    currentlyHealedMember.Conditions = Condition.DeadDust;
                else if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadAshes))
                    currentlyHealedMember.Conditions = Condition.DeadAshes;
                else if (currentlyHealedMember.Conditions.HasFlag(Condition.DeadCorpse))
                    currentlyHealedMember.Conditions = Condition.DeadCorpse;
                for (int i = 0; i < 16; ++i)
                {
                    if (((uint)healableConditions & (1u << i)) != 0)
                    {
                        var condition = (Condition)(1 << i);

                        if (currentlyHealedMember.Conditions.HasFlag(condition))
                        {
                            slots.Add(new ItemSlot
                            {
                                ItemIndex = condition switch
                                {
                                    Condition.Lamed => 1,
                                    Condition.Poisoned => 2,
                                    Condition.Petrified => 3,
                                    Condition.Diseased => 4,
                                    Condition.Aging => 5,
                                    Condition.Crazy => 7,
                                    Condition.Blind => 8,
                                    Condition.Drugged => 9,
                                    _ => 6 // dead states
                                },
                                Amount = 1
                            });
                            slotConditions.Add(condition);
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
                            CursorType = CursorType.Sword;
                            inputEnable = true;
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
                    if (slotIndex < slotConditions.Count)
                    {
                        conditionGrid.HideTooltip();

                        if (healer.AvailableGold < healer.GetCostForHealingCondition(slotConditions[slotIndex]))
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

                        HealCondition(slotConditions[slotIndex], healed =>
                        {
                            if (healed)
                            {
                                if (currentlyHealedMember.Conditions != Condition.None)
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
        currentPlace = blacksmith;

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
                Map.World switch
                {
                    World.Lyramion => Picture80x80.Sage,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Sage
                }, blacksmith, SetupBlacksmith, null, null, null, 24);
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
					layout.ButtonsDisabled = true;
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
                            layout.ButtonsDisabled = false;
								CursorType = CursorType.Sword;
                            inputEnable = true;
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
						layout.ButtonsDisabled = false;
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

                    nextClickHandler = null;
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

    void OpenInn(Places.Inn inn, string useText, bool showWelcome = true)
    {
        currentPlace = inn;

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
            SetWindow(Window.Inn, inn, useText);
            ShowPlaceWindow(inn.Name, showWelcome ? DataNameProvider.WelcomeInnkeeper : null,
                Map.World switch
                {
                    World.Lyramion => Picture80x80.Innkeeper,
                    World.ForestMoon => Picture80x80.DwarfMerchant,
                    World.Morag => Picture80x80.MoragMerchant,
                    _ => Picture80x80.Innkeeper
                },
                inn, SetupInn, null, null, () => InputEnable = true);
            // Rest button
            layout.AttachEventToButton(3, () =>
            {
                // Animals etc don't need to pay
                int totalCost = Math.Max(1, PartyMembers.Where(p => p.Alive && p.Race < Race.Animal).Count()) * inn.Cost;
                if (inn.AvailableGold < totalCost)
                {
                    layout.ShowClickChestMessage(DataNameProvider.NotEnoughMoney);
                    return;
                }
                nextClickHandler = null;
                layout.ShowPlaceQuestion($"{DataNameProvider.StayWillCost}{totalCost}{DataNameProvider.AgreeOnPrice}", answer =>
                {
                    if (answer) // yes
                    {
                        inn.AvailableGold -= (uint)totalCost;
                        updatePartyGold?.Invoke();
                        layout.ShowClickChestMessage(useText, () =>
                        {
                            currentWindow.Window = Window.MapView; // This way closing the camp will return to map and not the Inn
                            layout.GetButtonAction(2)?.Invoke(); // Call close handler
                            OpenStorage = null;
                            Teleport((uint)inn.BedroomMapIndex, (uint)inn.BedroomX,
                                (uint)inn.BedroomY, player.Direction, out _, true);
                            OpenCamp(true, inn.Healing);
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
            Picture80x80.Captain, showWelcome ? DataNameProvider.WelcomeRaftSeller : null);
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
        currentPlace = salesman;
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

            if (map == null || map.Type == MapType.Map3D || !map.UseTravelTypes || // Should not happen but never allow buying in these cases
                salesman.SpawnX > map.Width || salesman.SpawnY > map.Height)
                return false;

            var tile = map.Tiles[salesman.SpawnX - 1, salesman.SpawnY - 1];
            var tileset = MapManager.GetTilesetForMap(map);

            if (!tile.AllowMovement(tileset, travelType)) // Can't be placed there
                return false;

            if (CurrentSavegame.TransportLocations.Any(t => t != null && t.MapIndex == map.Index &&
                t.Position.X == salesman.SpawnX && t.Position.Y == salesman.SpawnY))
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
                    // Animals don't have to pay for a transport
                    int totalCost = (salesman.PlaceType == PlaceType.HorseDealer ? Math.Max(1, PartyMembers.Where(p => p.Alive && p.Race < Race.Animal).Count()) : 1) * salesman.Cost;
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
                    nextClickHandler = null;
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
        if (x == 0)
            x = 1u + (uint)player.Position.X;
        if (y == 0)
            y = 1u + (uint)player.Position.Y;

        int spawnIndex = -1;

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
                spawnIndex = i;
                break;
            }
            else if (CurrentSavegame.TransportLocations[i].TravelType == TravelType.Walk)
            {
                CurrentSavegame.TransportLocations[i].TravelType = travelType;
                CurrentSavegame.TransportLocations[i].MapIndex = mapIndex;
                CurrentSavegame.TransportLocations[i].Position = new Position((int)x, (int)y);
                spawnIndex = i;
                break;
            }
        }

        if (mapIndex == 0)
            mapIndex = Map.Index;

        if (mapIndex == Map.Index && spawnIndex != -1)
        {
            // TODO: In theory the transport could be visible even if the map index
            // does not match as there might be adjacent maps visible. But for now
            // there is no use case in Ambermoon nor Ambermoon Advanced.
            renderMap2D.PlaceTransport(mapIndex, x - 1, y - 1, travelType, spawnIndex);
        }
    }

    void OpenFoodDealer(Places.FoodDealer foodDealer, bool showWelcome = true)
    {
        currentPlace = foodDealer;

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
                    nextClickHandler = null;
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
        currentPlace = trainer;

        if (showWelcome)
            trainer.AvailableGold = 0;

        Action updatePartyGold = null;

        void SetupTrainer(Action updateGold, ItemGrid _)
        {
            updatePartyGold = updateGold;
        }

        void Train(uint times)
        {
            nextClickHandler = null;
            layout.ShowPlaceQuestion($"{DataNameProvider.PriceForTraining}{times * trainer.Cost}{DataNameProvider.AgreeOnPrice}", answer =>
            {
                if (answer) // yes
                {
                    trainer.AvailableGold -= times * (uint)trainer.Cost;
                    updatePartyGold?.Invoke();
                    CurrentPartyMember.Skills[trainer.Skill].CurrentValue += times;
                    CurrentPartyMember.TrainingPoints -= (ushort)times;
                    PlayerSwitched();
                    layout.ShowClickChestMessage(DataNameProvider.IncreasedAfterTraining);
                }
            }, TextAlign.Left);
        }

        void PlayerSwitched()
        {
            layout.EnableButton(3, CurrentPartyMember.Skills[trainer.Skill].CurrentValue < CurrentPartyMember.Skills[trainer.Skill].MaxValue);
        }

        uint GetMaxTrains() => Math.Max(0, Util.Min(trainer.AvailableGold / (uint)trainer.Cost, CurrentPartyMember.TrainingPoints,
            CurrentPartyMember.Skills[trainer.Skill].MaxValue - CurrentPartyMember.Skills[trainer.Skill].CurrentValue));

        Fade(() =>
        {
            layout.Reset();
            ShowMap(false);
            SetWindow(Window.Trainer, trainer);
            ShowPlaceWindow(trainer.Name, showWelcome ?
                trainer.Skill switch
                {
                    Skill.Attack => DataNameProvider.WelcomeAttackTrainer,
                    Skill.Parry => DataNameProvider.WelcomeParryTrainer,
                    Skill.Swim => DataNameProvider.WelcomeSwimTrainer,
                    Skill.CriticalHit => DataNameProvider.WelcomeCriticalHitTrainer,
                    Skill.FindTraps => DataNameProvider.WelcomeFindTrapTrainer,
                    Skill.DisarmTraps => DataNameProvider.WelcomeDisarmTrapTrainer,
                    Skill.LockPicking => DataNameProvider.WelcomeLockPickingTrainer,
                    Skill.Searching => DataNameProvider.WelcomeSearchTrainer,
                    Skill.ReadMagic => DataNameProvider.WelcomeReadMagicTrainer,
                    Skill.UseMagic => DataNameProvider.WelcomeUseMagicTrainer,
                    _ => throw new AmbermoonException(ExceptionScope.Data, "Invalid skill for trainer")
                } : null,
                trainer.Skill switch
                {
                    Skill.Attack => Picture80x80.Knight,
                    Skill.Parry => Picture80x80.Knight,
                    Skill.Swim => Picture80x80.Knight,
                    Skill.CriticalHit => Picture80x80.Knight,
                    Skill.FindTraps => Picture80x80.Thief,
                    Skill.DisarmTraps => Picture80x80.Thief,
                    Skill.LockPicking => Picture80x80.Thief,
                    Skill.Searching => Picture80x80.Thief,
                    Skill.ReadMagic => Picture80x80.Magician,
                    Skill.UseMagic => Picture80x80.Magician,
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
                }, () => ClosePopup());
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
        closeWindowHandler = _ => ActivePlayerChanged -= activePlayerSwitchedHandler;

        // exit button
        layout.AttachEventToButton(2, () =>
        {
            var exitQuestion = exitChecker?.Invoke();

            if (exitQuestion != null)
            {
                layout.OpenYesNoPopup(ProcessText(exitQuestion), Exit, () => ClosePopup(), () => ClosePopup(), 2);
            }
            else
            {
                Exit();
            }

            void Exit()
            {
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
        currentPlace = merchant;
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

    internal void ItemDraggingCancelled()
    {
        itemDragCancelledHandler?.Invoke();
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
        if (boughtItems == null)
        {
            // Note: Don't use boughtItems ??= Enumerable.Repeat(new ItemSlot(), 24).ToArray();
            // as this would use the exact same ItemSlot instance for all slots!
            boughtItems = new ItemSlot[24];
            for (int i = 0; i < boughtItems.Length; ++i)
                boughtItems[i] = new ItemSlot();
        }
        boughtItems ??= Enumerable.Repeat(new ItemSlot(), 24).ToArray();
        currentWindow.WindowParameters[4] = boughtItems;

        void UpdateSellButton()
        {
            layout.EnableButton(3, buysGoods && CurrentPartyMember.Inventory.Slots.Any(s => !s.Empty));
        }

        void SetupRightClickAbort()
        {
            nextClickHandler = buttons =>
            {
                if (buttons == MouseButtons.Right)
                {
                    itemGrid.HideTooltip();
                    layout.ShowChestMessage(null);
                    UntrapMouse();
                    CursorType = CursorType.Sword;
                    inputEnable = true;
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
                void ExitAndReturnItems()
                {
                    var merchantSlots = merchant.Slots.ToList();

                    foreach (var items in boughtItems)
                        ReturnItems(items);

                    void ReturnItems(ItemSlot items)
                    {
                        var slot = merchantSlots.FirstOrDefault(s => s.ItemIndex == items.ItemIndex) ??
                            merchantSlots.FirstOrDefault(s => s.ItemIndex == 0 || s.Amount == 0);

                        if (slot != null)
                        {
                            if (slot.ItemIndex == 0)
                                slot.Amount = 0;

                            slot.ItemIndex = items.ItemIndex;
                            slot.Amount += items.Amount;
                        }
                    }

                    Exit();
                }

                layout.OpenYesNoPopup(ProcessText(DataNameProvider.WantToGoWithoutItemsMerchant), ExitAndReturnItems, () => ClosePopup(), () => ClosePopup(), 2);
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
            layout.EnableButton(4, anyItemsToSell);
            UpdateSellButton();
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
        itemDragCancelledHandler += ShowBoughtItems;
        itemGrid.DisableDrag = false;
        itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount, bool updateSlot) =>
        {
            // This can only happen for bought items but we check for safety here
            if (mode != -1)
                throw new AmbermoonException(ExceptionScope.Application, "Non-bought items should not be draggable.");

            if (updateSlot)
                boughtItems[slotIndex].Remove(amount);
            layout.EnableButton(0, boughtItems.Any(slot => slot == null || slot.Empty) && merchant.AvailableGold > 0);
        };
        itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot, int amount) =>
        {
            if (mode == -1)
            {
                foreach (var partyMember in PartyMembers)
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value);
                itemGrid.Refresh();
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
                    nextClickHandler = null;
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
                    nextClickHandler = null;
                    layout.ShowPlaceQuestion($"{DataNameProvider.ForThisIllGiveYou}{sellPrice}{DataNameProvider.AgreeOnPrice}", answer =>
                    {
                        if (answer) // yes
                        {
                            allInputDisabled = true;
                            merchant.AddItems(ItemManager, item.Index, amount, itemSlot);
                            CurrentPartyMember.Inventory.Slots[slotIndex].Remove((int)amount);
                            InventoryItemRemoved(item.Index, (int)amount, CurrentPartyMember);
                            itemGrid.SetItem(slotIndex, CurrentPartyMember.Inventory.Slots[slotIndex], true);
                            merchant.AvailableGold += sellPrice;
                            UpdateGoldDisplay();
                            UpdateButtons();
                            allInputDisabled = false;
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

        ActivePlayerChanged += UpdateSellButton;
        layout.DraggedItemDropped += UpdateSellButton;

        void CleanUp()
        {
            itemDragCancelledHandler = null;
            ActivePlayerChanged -= UpdateSellButton;
            layout.DraggedItemDropped -= UpdateSellButton;
        }

        closeWindowHandler = _ => CleanUp();

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

        if (Map.Flags.HasFlag(MapFlags.NoMarkOrReturn) && (spell == Spell.WordOfMarking || spell == Spell.WordOfReturning))
        {
            ShowMessagePopup(DataNameProvider.CannotUseItHere);
            return;
        }

        if (!Map.Flags.HasFlag(MapFlags.Automapper))
        {
            if (spell == Spell.MapView)
            {
                ShowMessagePopup(DataNameProvider.MapViewNotWorkingHere);
                return;
            }

            if (spell == Spell.FindMonsters ||
                spell == Spell.FindPersons ||
                spell == Spell.FindSecretDoors ||
                spell == Spell.FindTraps ||
                spell == Spell.MysticalMapping)
            {
                ShowMessagePopup(DataNameProvider.AutomapperNotWorkingHere);
                return;
            }
        }

        var spellInfo = SpellInfos[spell];

        void ConsumeSP()
        {
            if (!fromItem) // Item spells won't consume SP
            {
                caster.SpellPoints.CurrentValue -= SpellInfos.GetSPCost(Features, spell, caster);
                layout.FillCharacterBars(caster);
            }
        }

        void SpellFinished() => CurrentSpellTarget = null;

        bool checkFail = !fromItem; // Item spells can't fail

        switch (spellInfo.Target)
        {
            case SpellTarget.SingleFriend:
            {
                Pause();
                layout.OpenTextPopup(ProcessText(DataNameProvider.BattleMessageWhichPartyMemberAsTarget), null, true, false, false, TextAlign.Center);
                PickTargetPlayer();
                void TargetPlayerPicked(int characterSlot)
                {
                    this.TargetPlayerPicked -= TargetPlayerPicked;
                    ClosePopup();
                    UntrapMouse();
                    InputEnable = true;
                    if (!WindowActive)
                        Resume();

                    if (characterSlot != -1)
                    {
                        bool reviveSpell = spell >= Spell.WakeTheDead && spell <= Spell.ChangeDust;
                        var target = GetPartyMember(characterSlot);

                        void Consume()
                        {
                            ConsumeSP();

                            if (spell == Spell.ExpExchange)
                            {
                                var target = GetPartyMember(characterSlot);

                                if (caster.Race == Race.Animal || target.Race == Race.Animal)
                                {
                                    ShowMessagePopup(DataNameProvider.CannotExchangeExpWithAnimals);
                                    return;
                                }

                                if (caster.Index == target.Index)
                                {
                                    // Changing exp with self doesn't change anything but also will
                                    // not require a message.
                                    return;
                                }
                                
                                if (!caster.Alive || !target.Alive)
                                {
                                    ShowMessagePopup(DataNameProvider.CannotExchangeExpWithDead);
                                    return;
                                }
                            }

                            void Cast()
                            {
                                if (target != null && (reviveSpell || spell == Spell.AllHealing || target.Alive))
                                {
                                    if (reviveSpell)
                                    {
                                        ApplySpellEffect(spell, caster, target, SpellFinished, checkFail);
                                    }
                                    else
                                    {
                                        currentAnimation?.Destroy();
                                        currentAnimation = new SpellAnimation(this, layout);
                                        currentAnimation.CastOn(spell, target, () =>
                                        {
                                            currentAnimation.Destroy();
                                            currentAnimation = null;
                                            ApplySpellEffect(spell, caster, target, SpellFinished, false);
                                        });
                                    }
                                }
                            }

                            if (!reviveSpell && checkFail)
                                TrySpell(Cast, SpellFinished);
                            else
                                Cast();
                        }

                        if (consumeHandler != null)
                        {
                            // Don't waste items on dead players
                            if (fromItem && !reviveSpell && spell != Spell.AllHealing && target?.Alive != true)
                                return;
                            consumeHandler(Consume);
                        }
                        else
                            Consume();
                    }
                }
                this.TargetPlayerPicked += TargetPlayerPicked;
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
                        if (spell == Spell.Resurrection)
                        {
                            var affectedMembers = PartyMembers.Where(p => p.Conditions.HasFlag(Condition.DeadCorpse)).ToList();
                            Revive(caster, affectedMembers, SpellFinished);
                        }
                        else
                        {
                            currentAnimation?.Destroy();
                            currentAnimation = new SpellAnimation(this, layout);

                            currentAnimation.CastHealingOnPartyMembers(() =>
                            {
                                currentAnimation.Destroy();
                                currentAnimation = null;

                                foreach (var partyMember in PartyMembers.Where(p => p.Alive))
                                    ApplySpellEffect(spell, caster, partyMember, null, false);

                                SpellFinished();
                            });
                        }
                    }
                    if (checkFail)
                        TrySpell(Cast, spell == Spell.CreateFood ? () => ShowMessagePopup(DataNameProvider.TheSpellFailed, SpellFinished) : SpellFinished);
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
            {
                string message = spell == Spell.RemoveCurses ? DataNameProvider.BattleMessageWhichPartyMemberAsTarget
                    : DataNameProvider.WhichInventoryAsTarget;
                layout.OpenTextPopup(ProcessText(message), null, true, false, false, TextAlign.Center);
                if (CurrentWindow.Window == Window.Inventory)
                    InputEnable = true;
                else
                    Pause();
                PickTargetInventory();
                bool TargetInventoryPicked(int characterSlot)
                {
                    this.TargetInventoryPicked -= TargetInventoryPicked;

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
                                UntrapMouse();
                                EndSequence();
                                layout.ShowChestMessage(null);
                                layout.SetInventoryMessage(null);
                                ClosePopup();
                            }

                            this.TargetItemPicked -= TargetItemPicked;
                            Consume();
                            EndSequence();
                            ShowMessagePopup(DataNameProvider.NoCursedItemFound, CleanUp);
                            return false; // no item selection
                        }
                    }

                    return true; // move forward to item selection
                }
                bool TargetItemPicked(ItemGrid itemGrid, int slotIndex, ItemSlot itemSlot)
                {
                    this.TargetItemPicked -= TargetItemPicked;
                    itemGrid?.HideTooltip();
                    layout.SetInventoryMessage(null);
                    ClosePopup();
                    if (itemSlot != null)
                    {
                        Consume();
                        StartSequence();
                        ApplySpellEffect(spell, caster, itemSlot, () =>
                        {
                            if (!fromItem)
                                CloseWindow();
                            UntrapMouse();
                            EndSequence();
                            if (!WindowActive)
                                Resume();
                            layout.SetInventoryMessage(null);
                            layout.ShowChestMessage(null);
                        }, checkFail);
                        return false; // manual window closing etc
                    }
                    else
                    {
                        layout.SetInventoryMessage(null);
                        layout.ShowChestMessage(null);
                        if (!WindowActive)
                            Resume();
                        if (fromItem)
                        {
                            EndSequence();
                            UntrapMouse();
                            return false;
                        }
                        else
                        {
                            return true; // auto-close window and cleanup
                        }
                    }
                }
                this.TargetInventoryPicked += TargetInventoryPicked;
                this.TargetItemPicked += TargetItemPicked;

                void Consume()
                {
                    if (consumeHandler != null)
                        consumeHandler(ConsumeSP);
                    else
                        ConsumeSP();
                }
                break;
            }
            case SpellTarget.None:
            {
                void Consume()
                {
                    ConsumeSP();

                    if (spell == Spell.SelfHealing || spell == Spell.SelfReviving)
                    {
                        void Cast()
                        {
                            currentAnimation?.Destroy();
                            currentAnimation = new SpellAnimation(this, layout);
                            currentAnimation.CastOn(spell, caster, () =>
                            {
                                currentAnimation.Destroy();
                                currentAnimation = null;
                                ApplySpellEffect(spell, caster, null, false);
                            });
                        }
                        if (checkFail)
                            TrySpell(Cast);
                        else
                            Cast();
                    }
                    else
                    {
                        ApplySpellEffect(spell, caster, null, checkFail);
                    }
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
                    var spellInfo = SpellInfos[spell];

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

                    if (SpellInfos.GetSPCost(Features, spell, CurrentPartyMember) > CurrentPartyMember.SpellPoints.CurrentValue)
                        return DataNameProvider.NotEnoughSP;

                    return null;
                },
                spell => UseSpell(CurrentPartyMember, spell, itemGrid, false)
            );
        }
    }

    internal void OpenCamp(bool inn, int healing = 50) // 50 when camping outside of inns
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
            SetWindow(Window.Camp, inn, healing);
            lastPlayedSong = PlayMusic(Song.BarBrawlin);
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
            closeWindowHandler = _ => ActivePlayerChanged -= PlayerSwitched;

            void Exit()
            {
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
							layout.ButtonsDisabled = false;
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
					layout.ButtonsDisabled = true;
					itemGrid.Initialize(CurrentPartyMember.Inventory.Slots.ToList(), false);
                SetupRightClickAbort();
            });

            // sleep button
            layout.AttachEventToButton(6, () =>
            {
                if (!inn && CurrentSavegame.HoursWithoutSleep < 8)
                {
                    layout.ShowClickChestMessage(DataNameProvider.RestingWouldHaveNoEffect);
                }
                else
                {
                    Sleep(inn, healing);
                }
            });

            itemGrid.ItemClicked += (ItemGrid _, int slotIndex, ItemSlot itemSlot) =>
            {
                itemGrid.HideTooltip();

                void ShowMessage(string message, Action additionalAction = null)
                {
                    nextClickHandler = null;
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
                    ShowMessage(DataNameProvider.ThatsNotASpellScroll);
                }
                else if (item.SpellSchool != CurrentPartyMember.Class.ToSpellSchool())
                {
                    ShowMessage(DataNameProvider.CantLearnSpellsOfType);
                }
                else if (CurrentPartyMember.HasSpell(item.Spell))
                {
                    ShowMessage(DataNameProvider.AlreadyKnowsSpell);
                }
                else
                {
                    uint slpCost = SpellInfos.GetSLPCost(Features, item.Spell);

                    if (CurrentPartyMember.SpellLearningPoints < slpCost)
                    {
                        ShowMessage(DataNameProvider.NotEnoughSpellLearningPoints);
                    }
                    else
                    {
                        CurrentPartyMember.SpellLearningPoints -= (ushort)slpCost;

                        if (RollDice100() < CurrentPartyMember.Skills[Skill.ReadMagic].TotalCurrentValue)
                        {
                            // Learned spell
                            ShowMessage(DataNameProvider.ManagedToLearnSpell, () =>
                            {
                                CurrentPartyMember.AddSpell(item.Spell);
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50), true);
                            });
                        }
                        else
                        {
                            // Failed to learn the spell
                            ShowMessage(DataNameProvider.FailedToLearnSpell, () =>
                            {
                                layout.DestroyItem(itemSlot, TimeSpan.FromMilliseconds(50));
                            });
                        }
                    }
                }
            };

            PlayerSwitched();
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
        bool showCursed = item.Flags.HasFlag(ItemFlags.Accursed) && itemSlot.Flags.HasFlag(ItemSlotFlags.Identified);
        int damage = showCursed ? -item.Damage : item.Damage;
        int defense = showCursed ? -item.Defense : item.Defense;
        int valueOffset = DataNameProvider.ItemFingersDisplay.IndexOf("{") - 1;
        popup.AddText(new Position(32, 146), DataNameProvider.ItemDamageDisplay.Substring(0, valueOffset) + damage.ToString("+#;-#; 0"), TextColor.White);
        popup.AddText(new Position(32, 154), DataNameProvider.ItemDefenseDisplay.Substring(0, valueOffset) + defense.ToString("+#;-#; 0"), TextColor.White);

        popup.AddText(new Position(177, 99), DataNameProvider.ClassesHeaderString, TextColor.LightGray);
        int column = 0;
        int row = 0;
        foreach (var @class in EnumHelper.GetValues<Class>())
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
        detailsPopup.AddText(new Position(48, 96), DataNameProvider.SkillsHeaderString, TextColor.LightOrange);
        if (item.Skill != null && item.SkillValue != 0)
        {
            detailsPopup.AddText(new Position(48, 103), DataNameProvider.GetSkillName(item.Skill.Value), TextColor.White);
            detailsPopup.AddText(new Position(170, 103), (factor * item.SkillValue).ToString("+#;-#; 0"), TextColor.White);
        }
        detailsPopup.AddText(new Position(48, 110), DataNameProvider.FunctionHeader, TextColor.LightOrange);
        if (item.Spell != Spell.None && (item.InitialCharges != 0 || item.MaxCharges != 0))
        {
            detailsPopup.AddText(new Position(48, 117),
                $"{DataNameProvider.GetSpellName(item.Spell)} ({(itemSlot.NumRemainingCharges > 99 ? "**" : itemSlot.NumRemainingCharges.ToString())})",
                TextColor.White);
        }
        if (cursed)
        {
            var contentArea = detailsPopup.ContentArea;
            AddAnimatedText((area, text, color, align) => detailsPopup.AddText(area, text, color, align),
                new Rect(contentArea.X, 127, contentArea.Width, Global.GlyphLineHeight), DataNameProvider.Cursed,
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
                    OpenInn(innData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? DataNameProvider.InnkeeperGoodSleepWish :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.InnkeeperGoodSleepWish));
                    return true;
                }
                case PlaceType.Merchant:
                case PlaceType.Library:
                    OpenMerchant(enterPlaceEvent.MerchantDataIndex, places.Entries[(int)enterPlaceEvent.PlaceIndex - 1].Name,
                        enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                            map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing),
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
                    OpenHorseSalesman(horseDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
                    return true;
                }
                case PlaceType.RaftDealer:
                {
                    var raftDealerData = new Places.RaftSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenRaftSalesman(raftDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
                    return true;
                }
                case PlaceType.ShipDealer:
                {
                    var shipDealerData = new Places.ShipSalesman(places.Entries[(int)enterPlaceEvent.PlaceIndex - 1]);
                    OpenShipSalesman(shipDealerData, enterPlaceEvent.UsePlaceTextIndex == 0xff ? null :
                        map.GetText(enterPlaceEvent.UsePlaceTextIndex, DataNameProvider.TextBlockMissing));
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
            string closedText = map.GetText((int)enterPlaceEvent.ClosedTextIndex, DataNameProvider.TextBlockMissing);
            ShowTextPopup(ProcessText(closedText), null);
            return true;
        }
        else
        {
            return true;
        }
    }

    internal void StartBattle(StartBattleEvent battleEvent, Event nextEvent, uint x, uint y, uint? combatBackgroundIndex = null)
    {
        if (BattleActive)
            return;

        ResetMoveKeys();

        currentBattleInfo = new BattleInfo
        {
            MonsterGroupIndex = battleEvent.MonsterGroupIndex
        };
        ShowBattleWindow(nextEvent, false, x, y, combatBackgroundIndex);
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

    public void AddExperience(PartyMember partyMember, uint amount, Action finishedEvent)
    {
        if (partyMember.AddExperiencePoints(amount, RandomInt, Features))
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

    internal void EnableMusicChange(bool enable) => disableMusicChange = !enable;

    internal void EnableTimeEvents(bool enable) => disableTimeEvents = !enable;

    /// <summary>
    /// Starts playing a specific music. If Song.Default is given
    /// the current map music is played instead.
    /// 
    /// Returns the previously played song.
    /// </summary>
    internal Song PlayMusic(Song song)
    {
        var lastSong = lastPlayedSong;
        lastPlayedSong = null;

        if (disableMusicChange)
            return currentSong?.Song ?? Song.Default;

        if (song == Song.Default || (int)song == 255)
        {
            if (Map.UseTravelMusic)
            {
                var travelSong = TravelType.TravelSong();

                if (travelSong == Song.Default)
                    travelSong = Song.PloddingAlong;

                return PlayMusic(travelSong);
            }

            return PlayMusic(Map.MusicIndex == 0 || Map.MusicIndex == 255 ? (lastSong ?? Song.PloddingAlong) : (Song)Map.MusicIndex);
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

    internal TimeSpan? GetCurrentSongDuration() => currentSong?.SongDuration;

    public void ContinueMusic()
    {
        if (Configuration.Music)
            currentSong?.Play(AudioOutput);
    }

    internal void UpdateMusic()
    {
        if (Configuration.Music && currentSong != null)
            PlayMusic(currentSong.Song);
    }

    void ShowLevelUpWindow(PartyMember partyMember, Action finishedEvent)
    {
        bool allInputWasDisabled = allInputDisabled;
        bool inputWasEnabled = InputEnable;
        InputEnable = false;
        allInputDisabled = false;
        CursorType = CursorType.Click;
        var lastPlayedSong = this.lastPlayedSong;
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

        if (partyMember.Class < Class.Animal)
        {
            if (partyMember.Level >= 50)
                popup.AddText(new Position(32, 134), DataNameProvider.MaxLevelReached, TextColor.BrightGray);
            else
                AddValueText(134, DataNameProvider.NextLevelAt, partyMember.GetNextLevelExperiencePoints(Features), null, " " + DataNameProvider.EP);
        }

        popup.Closed += () =>
        {
            InputEnable = inputWasEnabled;
            allInputDisabled = allInputWasDisabled;
            PlayMusic(previousSong);
            this.lastPlayedSong = lastPlayedSong;
            finishedEvent?.Invoke();
        };
    }

    internal void ShowBattleLoot(BattleEndInfo battleEndInfo, Action closeAction)
    {
        var gold = battleEndInfo.KilledMonsters.Sum(m => m.Gold);
        var food = battleEndInfo.KilledMonsters.Sum(m => m.Food);
        var loot = new Chest
        {
            Type = ChestType.Junk,
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
        var expReceivingPartyMembers = PartyMembers.Where(m => m.Alive && !battleEndInfo.FledPartyMembers.Contains(m) && m.Race <= Race.Thalionic).ToList();
        int expPerPartyMember = expReceivingPartyMembers.Count == 0 ? 0 : battleEndInfo.TotalExperience / expReceivingPartyMembers.Count;

        if (loot.Empty)
        {
            Pause();
            void Finish()
            {
                Resume();
                closeAction?.Invoke();
            }
            CloseWindow(() =>
            {
                if (expReceivingPartyMembers.Count == 0)
                {
                    Finish();
                }
                else
                {
                    ShowMessagePopup(string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
                    {
                        Pause();
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, Finish);
                    });
                }
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
            ShowLoot(storage, expReceivingPartyMembers == null || expReceivingPartyMembers.Count == 0 ? null : string.Format(DataNameProvider.ReceiveExp, expPerPartyMember), () =>
            {
                if (expReceivingPartyMembers != null)
                {
                    if (expReceivingPartyMembers.Count > 0)
                    {
                        AddExperience(expReceivingPartyMembers, (uint)expPerPartyMember, () =>
                        {
                            layout.ShowChestMessage(DataNameProvider.LootAfterBattle, TextAlign.Left);
                        });
                    }
                    else
                    {
                        layout.ShowChestMessage(DataNameProvider.LootAfterBattle, TextAlign.Left);
                    }
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
    void OpenMusicList(Action finishAction = null)
    {
        bool wasPaused = paused;
        Pause();
        const int columns = 15;
        const int rows = 10;
        var popupArea = new Rect(16, 35, columns * 16, rows * 16);
        TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
        var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
        var songList = popup.AddSongListBox(Enumerable.Range(0, 32).Select(index => new KeyValuePair<string, Action<int, string>>
        (
            DataNameProvider.GetSongName((Song)(index + 1)), PlaySong
        )).ToList());
        void PlaySong(int index, string name)
        {
            if (AudioOutput.Available)
            {
                AudioOutput.Enabled = Configuration.Music = true;
                PlayMusic((Song)(index + 1));
            }
        }
        var exitButton = popup.AddButton(new Position(190, 166));
        exitButton.ButtonType = ButtonType.Exit;
        exitButton.Disabled = false;
        exitButton.LeftClickAction = () => ClosePopup();
        exitButton.Visible = true;
        popup.Closed += () =>
        {
            UntrapMouse();
            if (!wasPaused)
                Resume();
            finishAction?.Invoke();
        };
        int scrollRange = Math.Max(0, 16); // = 32 songs - 16 songs visible
        var scrollbar = popup.AddScrollbar(layout, scrollRange, 2);
        scrollbar.Scrolled += offset =>
        {
            songList.ScrollTo(offset);
        };
    }

    void OpenMiniMap(Action finishAction = null)
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
                    if (map.Blocks[x, y].WallIndex == 0 || map.Blocks[x, y].WallIndex >= labdata.Walls.Count)
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
                    int textureFactor = (int)(positionMarker.Layer?.TextureFactor ?? 1);
                    positionMarker.TextureAtlasOffset = positionMarkerBaseTextureOffset + new Position(0, positionMarkerFrame * textureFactor);
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
                finishAction?.Invoke();
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

    internal void ShowAutomap(AutomapOptions automapOptions, Action finishAction = null)
    {
        mobileAutomapScroll.X = 0;
        mobileAutomapScroll.Y = 0;

			void Create()
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
                SetWindow(Window.Automap, automapOptions);
                layout.Reset();
                layout.SetLayout(LayoutType.Automap);
                ResetMoveKeys(true);
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
                    if (CurrentWindow.Window == Window.Automap)
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
                        case AutomapGraphic.GotoPoint: // this has 8 frames
                        {
                            var animatedSprite = layout.AddAnimatedSprite(new Rect(x, y, width, height), Graphics.GetAutomapGraphicIndex(automapGraphic),
                                paletteIndex, automapGraphic == AutomapGraphic.GotoPoint ? 8u : 4u, displayLayer);
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
                    if (automapType != AutomapType.None && automapType != AutomapType.Monster && automapType != AutomapType.Person)
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
                                automapType == AutomapType.Tavern || automapType == AutomapType.Merchant || automapType == AutomapType.Door;

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

                            if (!walls.TryGetValue(x + y * Map.Width, out var wall))
                                return false;

                            // Note: This is used to detect if walls should be
                            // merged visually. There are some special walls that
                            // have a different block sight state (e.g. the crystal
                            // wall in the temple of brotherhood).
                            // Those should be treated as "another" wall so we will
                            // return false here if the block sight states do not match.
                            otherWall = blocksSight != wall.BlocksSight;

                            return !otherWall || wall.NormalWall == null;
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
                        int textureFactor = (int)renderView.GetLayer(Layer.UI).TextureFactor;
                        sprite.TextureAtlasOffset = new Position(sprite.TextureAtlasOffset.X + wallGraphicType * 8 * textureFactor, sprite.TextureAtlasOffset.Y);
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

                        AddTimedEvent(TimeSpan.FromMilliseconds(120), Animate);
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

                            var clickedGotoPoint = gotoPoints.FirstOrDefault(gotoPoint => gotoPoint.Value.Area.Contains(mousePosition));

                            // Be a bit more forgiving on mobile devices if they not exactly hit the small circle
                            if (clickedGotoPoint.Key == null && Configuration.IsMobile)
									clickedGotoPoint = gotoPoints.FirstOrDefault(gotoPoint => gotoPoint.Value.Area.CreateModified(-6, -6, 12, 12).Contains(mousePosition));

								if (clickedGotoPoint.Key != null)
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
                                if (!CanSee())
                                {
                                    ShowMessagePopup(DataNameProvider.DarkDontFindWayBack, AbortGoto, TextAlign.Left, 202);
                                }
                                else if (MonsterSeesPlayer)
                                {
                                    ShowMessagePopup(DataNameProvider.WayBackTooDangerous, AbortGoto, TextAlign.Left, 202);
                                }
                                else
                                {
                                    ShowDecisionPopup(DataNameProvider.ReallyWantToGoThere, response =>
                                    {
                                        if (response == PopupTextEvent.Response.Yes)
                                        {
                                            if (player3D.Position.X + 1 == clickedGotoPoint.Key.X && player3D.Position.Y + 1 == clickedGotoPoint.Key.Y)
                                            {
                                                ShowMessagePopup(DataNameProvider.AlreadyAtGotoPoint, AbortGoto, TextAlign.Center, 202);
                                            }
                                            else
                                            {
                                                Exit(() =>
                                                {
                                                    var xDiff = Math.Abs((int)clickedGotoPoint.Key.X - (player3D.Position.X + 1));
                                                    var yDiff = Math.Abs((int)clickedGotoPoint.Key.Y - (player3D.Position.Y + 1));
                                                    uint ticks = (uint)Util.Round((xDiff + yDiff) * 0.2f);
                                                    GameTime.Ticks(ticks);
                                                    Teleport(Map.Index, clickedGotoPoint.Key.X, clickedGotoPoint.Key.Y, clickedGotoPoint.Key.Direction, out _, true);
                                                });
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

                        return false;
                    };
                }
                SetupClickHandlers();

                #endregion

                bool closed = false;

                void Exit(Action followAction = null)
                {
                    var exitAction = finishAction == null ? followAction : () =>
                    {
                        followAction?.Invoke();
                        finishAction();
                    };

                    closed = true;
                    UntrapMouse();
                    if (currentWindow.Window == Window.Automap)
                        CloseWindow(exitAction);
                    else
                        exitAction?.Invoke();
                }

                void CheckScroll()
                {
                    if (!closed)
                    {
                        AddTimedEvent(TimeSpan.FromMilliseconds(100), () =>
                        {
                            if (InputEnable)
                            {
                                if (Configuration.IsMobile)
                                {
                                    int x = Util.Round(mobileAutomapScroll.X);
                                    int y = Util.Round(mobileAutomapScroll.Y);

                                    if (x != 0 || y != 0)
                                    {
                                        if (x != 0)
                                            mobileAutomapScroll.X -= x;
                                        if (y != 0)
                                            mobileAutomapScroll.Y -= y;
                                        Scroll(x, y);
                                    }
                                }
                                else
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
        }

        if (currentWindow.Window == Window.Automap)
            Create();
        else
            CloseWindow(Create);
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
            var riddleText = ProcessText(map.GetText((int)riddlemouthEvent.RiddleTextIndex, DataNameProvider.TextBlockMissing));
            var solutionResponseText = ProcessText(map.GetText((int)riddlemouthEvent.SolutionTextIndex, DataNameProvider.TextBlockMissing));
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

    internal Position GetPlayerDrawOffset(CharacterDirection? direction)
    {
        if (Map.UseTravelTypes)
        {
            var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, direction ?? player.Direction);
            return new Position((int)travelInfo.OffsetX - 16, (int)travelInfo.OffsetY - 16);
        }
        else
        {
            return new Position();
        }
    }

    internal Character2DAnimationInfo GetPlayerAnimationInfo(CharacterDirection? direction = null)
    {
        if (Map.UseTravelTypes)
        {
            var travelInfo = renderView.GameData.GetTravelGraphicInfo(TravelType, direction ?? player.Direction);
            return new Character2DAnimationInfo
            {
                FrameWidth = (int)travelInfo.Width,
                FrameHeight = (int)travelInfo.Height,
                StandFrameIndex = Graphics.TravelGraphicOffset + (uint)TravelType * 4,
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

    internal void OpenDictionary(Action<string> choiceHandler, Func<string, TextColor> colorProvider = null, Action abortAction = null)
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
        exitButton.LeftClickAction = () =>
        {
            layout.ClosePopup();
            abortAction?.Invoke();
        };
        var dictionaryList = popup.AddDictionaryListBox(Dictionary.OrderBy(entry => entry).Select(entry => new KeyValuePair<string, Action<int, string>>
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
            var spellInfo = SpellInfos[spell];
            string entry = DataNameProvider.GetSpellName(spell);

            if (available)
            {
                // append usage amount
                entry = entry.PadRight(21) + $"({Math.Min(99, partyMember.SpellPoints.CurrentValue / SpellInfos.GetSPCost(Features, spell, partyMember))})";
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

    internal void ShowBriefMessagePopup(string text, TimeSpan displayTime,
        TextAlign textAlign = TextAlign.Center, byte displayLayerOffset = 0)
    {
        if (layout.PopupActive)
            return;

        bool paused = this.paused;
        bool inputEnabled = InputEnable;
        Pause();
        InputEnable = false;
        // Simple text popup
        Popup popup;
        popup = layout.OpenTextPopup(ProcessText(text), () =>
        {
            popup = null;
            if (inputEnabled)
                InputEnable = true;
            if (!paused)
                Resume();
            ResetCursor();
        }, true, true, false, textAlign, displayLayerOffset, PrimaryUIPaletteIndex);
        CursorType = CursorType.Wait;
        TrapMouse(popup.ContentArea);
        AddTimedEvent(displayTime, () =>
        {
            if (popup != null)
                ClosePopup();
        });
    }

    internal void ShowMessagePopup(string text, Action closeAction = null,
        TextAlign textAlign = TextAlign.Center, byte displayLayerOffset = 0)
    {
        if (layout.PopupActive)
        {
            closeAction?.Invoke();
            return;
        }

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
            SetWindow(Window.Event, gameOver);
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

				ShowMobileClickIndicator(Global.VirtualScreenWidth / 2 - 8, Global.VirtualScreenHeight - 16 + (gameOver ? -8 : 3));

            scrollableText.Clicked += scrolledToEnd =>
            {
                if (scrolledToEnd)
                {
						HideMobileClickIndicator();

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
                bool hasSavegames = SavegameManager.GetSavegameNames(renderView.GameData, out _, NumBaseSavegameSlots).Any(n => !string.IsNullOrWhiteSpace(n));
                if (!hasSavegames)
                    hasSavegames = Configuration.ExtendedSavegameSlots && GetAdditionalSavegameSlots()?.Names?.Any(s => !string.IsNullOrWhiteSpace(s)) == true;
                layout.AddText(textArea, ProcessText(hasSavegames
                    ? DataNameProvider.GameOverLoadOrQuit
                    : GetCustomText(CustomTexts.Index.StartNewGameOrQuit)),
                    TextColor.BrightGray);
                void ShowButtons()
                {
                    ExecuteNextUpdateCycle(() => CursorType = CursorType.Sword);
                    layout.ShowGameOverButtons(load =>
                    {
                        if (load)
                        {
                            if (hasSavegames)
                                layout.OpenLoadMenu(CloseWindow, ShowButtons, true);
                            else
                                NewGame();
                        }
                        else
                        {
                            Quit(ShowButtons);
                        }
                    }, hasSavegames);
                }
                ShowButtons();
            }
        });
    }

    void ShowTutorial()
    {
        new Tutorial(this, drawTouchFingerRequest).Run(renderView);
    }

    internal void ShowTextPopup(Map map, PopupTextEvent popupTextEvent, Action<PopupTextEvent.Response> responseHandler)
    {
        var text = ProcessText(map.GetText((int)popupTextEvent.TextIndex, DataNameProvider.TextBlockMissing));

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
        ShowDecisionPopup(map.GetText((int)decisionEvent.TextIndex, DataNameProvider.TextBlockMissing), responseHandler, 0);
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

                    if (removedWeapon || !CheckAbilityToAttack(out _, true))
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

        if (!CurrentPartyMember.Conditions.CanSelect() || currentBattle?.GetSlotFromCharacter(CurrentPartyMember) == -1)
        {
            layout.ClearBattleFieldSlotColors();

            if (!PartyMembers.Any(p => p.Conditions.CanSelect()))
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
            else if (BattleActive && !PartyMembers.Any(p => p.Conditions.CanSelect() && !currentBattle.HasPartyMemberFled(p)))
            {
                // All dead or fled but at least one is still alive but fled.
                EndBattle(true);
                return false;
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

            if (((conversationPartner as Character).SpokenLanguages & partyMember.SpokenLanguages) == 0 &&
                ((conversationPartner as Character).SpokenExtendedLanguages & partyMember.SpokenExtendedLanguages) == 0)
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
        if (PlayerIsPickingABattleAction)
            return;

        if (partyMember != null && (partyMember.Conditions.CanSelect() || currentWindow.Window == Window.Healer))
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
                    ResetMoveKeys(true);
                    NewLeaderPicked?.Invoke(index);
                }

                if (is3D)
                    renderMap3D?.SetCameraHeight(partyMember.Race);
            }

            if (switched)
            {
                UpdateLight(false, false, true);

                if (!WindowActive)
                    layout.UpdateLayoutButtons();
            }

            ActivePlayerChanged?.Invoke();
        }
    }

    internal bool CanUseSpells()
    {
        if (Map?.CanUseSpells != true)
            return false;

        if (CurrentPartyMember?.Class.IsMagic() != true)
            return false;

        if (CurrentPartyMember?.Conditions.CanCastSpell(Features) != true)
            return false;

        return true;
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
        return DropItem(SlotFromPartyMember(partyMember).Value, null, ItemSlot.CreateFromItem(ItemManager, itemIndex, ref amount, true));
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

    internal void UpdateTransportPosition(int index)
    {
        if (!is3D && Map.UseTravelTypes)
        {
            var transport = CurrentSavegame.TransportLocations[index];
            renderMap2D.RemoveTransport(index);
            renderMap2D.PlaceTransport(transport.MapIndex, (uint)transport.Position.X - 1,
                (uint)transport.Position.Y - 1, transport.TravelType, index);
        }
    }

    void SetWindow(Window window, params object[] parameters)
    {
        CurrentMobileAction = MobileAction.None;

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

    internal void ClosePopup(bool raiseEvent = true, bool force = false) => layout?.ClosePopup(raiseEvent, force);

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

        closeWindowHandler?.Invoke(true);
        closeWindowHandler = null;

        characterInfoTexts.Clear();
        characterInfoPanels.Clear();
        characterInfoStatTooltips.Clear();
        CurrentInventoryIndex = null;
        windowTitle.Visible = false;
        weightDisplayBlinking = false;
        layout.ButtonsDisabled = false;

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
                currentPlace = null;

                Fade(() =>
                {
                    if (CurrentMapCharacter != null &&
                        (closedWindow.Window == Window.Battle ||
                        closedWindow.Window == Window.BattleLoot ||
                        closedWindow.Window == Window.Chest ||
                        closedWindow.Window == Window.Event))
                        CurrentMapCharacter = null;

                    bool wasGameOver = closedWindow.Window == Window.Event && (bool)closedWindow.WindowParameters[0] == true;

                    ShowMap(true, !wasGameOver); // avoid playing music after gameover as Start() will start the music as well afterwards
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
                var triggerFollowEvents = (bool)currentWindow.WindowParameters[5];
                currentWindow = DefaultWindow;
                ShowChest(chestEvent, trapFound, trapDisarmed, map, position, false, triggerFollowEvents);
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
                var moved = (bool)currentWindow.WindowParameters[6];
                currentWindow = DefaultWindow;
                ShowDoor(doorEvent, trapFound, trapDisarmed, map, x, y, false, moved);
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
                var characterIndex = currentWindow.WindowParameters[1] as uint?;
                var conversationEvent = currentWindow.WindowParameters[2] as Event;
                var conversationItems = currentWindow.WindowParameters[3] as ConversationItems;
                currentWindow = DefaultWindow;
                ShowConversation(conversationPartner, characterIndex, conversationEvent, conversationItems, false);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Battle:
            {
                var nextEvent = (Event)currentWindow.WindowParameters[0];
                var x = (uint)currentWindow.WindowParameters[1];
                var y = (uint)currentWindow.WindowParameters[2];
                var combatBackgroundIndex = (uint?)currentWindow.WindowParameters[3];
                currentWindow = DefaultWindow;
                Fade(() => { ShowBattleWindow(nextEvent, out _, x, y, combatBackgroundIndex); finishAction?.Invoke(); });
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
                int healing = (int)currentWindow.WindowParameters[1];
                OpenCamp(inn, healing);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            case Window.Inn:
            {
                var inn = (Places.Inn)currentWindow.WindowParameters[0];
                var useText = (string)currentWindow.WindowParameters[1];
                OpenInn(inn, useText, false);
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
                ShowAutomap((AutomapOptions)currentWindow.WindowParameters[0]);
                if (finishAction != null)
                    AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), finishAction);
                break;
            }
            default:
                break;
        }
    }

    public void Revive(Character caster, List<PartyMember> affectedMembers, Action finishAction = null)
    {
        void Revive(PartyMember target, Action finishAction) =>
            ApplySpellEffect(Spell.Resurrection, caster, target, finishAction, false);
        ForeachPartyMember(Revive, p => affectedMembers.Contains(p) && p.Conditions.HasFlag(Condition.DeadCorpse), () =>
        {
            currentAnimation?.Destroy();
            currentAnimation = new SpellAnimation(this, layout);

            currentAnimation.CastHealingOnPartyMembers(() =>
            {
                currentAnimation.Destroy();
                currentAnimation = null;
                finishAction?.Invoke();
            }, affectedMembers);
        });
    }
}
