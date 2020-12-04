using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = Ambermoon.Data.Attribute;

namespace Ambermoon
{
    public class Game
    {
        class NameProvider : ITextNameProvider
        {
            readonly Game game;

            public NameProvider(Game game)
            {
                this.game = game;
            }

            /// <inheritdoc />
            public string LeadName => game.CurrentPartyMember.Name;
            /// <inheritdoc />
            public string SelfName => game.CurrentPartyMember.Name; // TODO: maybe this is the active actor in battle?
            /// <inheritdoc />
            public string CastName => game.CurrentCaster?.Name ?? LeadName;
            /// <inheritdoc />
            public string InvnName => game.CurrentInventory?.Name ?? LeadName;
            /// <inheritdoc />
            public string SubjName => game.CurrentPartyMember.Name; // TODO
            /// <inheritdoc />
            public string Sex1Name => game.CurrentPartyMember.Gender == Gender.Male ? game.DataNameProvider.He : game.DataNameProvider.She;
            /// <inheritdoc />
            public string Sex2Name => game.CurrentPartyMember.Gender == Gender.Male ? game.DataNameProvider.His : game.DataNameProvider.Her;
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
                    // TODO: these have to be corrected later after testing them
                    // 2D movement
                    6, // Indoor
                    4, // Outdoor walk
                    8, // Horse
                    4, // Raft
                    8, // Ship
                    4, // Magical disc
                    16, // Eagle
                    8, // Fly
                    10, // Witch broom
                    8, // Sand lizard
                    8  // Sand ship
                };
                MoveSpeed3D = GetMoveSpeed3D(legacyMode);
                TurnSpeed3D = GetTurnSpeed3D(legacyMode);
            }

            static uint GetTickDivider3D(bool legacyMode) => legacyMode ? 8u : 60u;
            static float GetMoveSpeed3D(bool legacyMode) => legacyMode ? 0.25f : 0.0625f;
            static float GetTurnSpeed3D(bool legacyMode) => legacyMode ? 15.0f : 3.00f;
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
            PickAttackSpot
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
            ChestFood
        }

        // TODO: cleanup members
        readonly Random random = new Random();
        internal SavegameTime GameTime { get; private set; } = null;
        const int FadeTime = 1000;
        public const int MaxPartyMembers = 6;
        internal const uint TicksPerSecond = 60;
        readonly bool legacyMode = false;
        public event Action QuitRequested;
        bool ingame = false;
        bool is3D = false;
        internal bool WindowActive => currentWindow.Window != Window.MapView;
        static readonly WindowInfo DefaultWindow = new WindowInfo { Window = Window.MapView };
        WindowInfo currentWindow = DefaultWindow;
        WindowInfo lastWindow = DefaultWindow;
        // Note: These are not meant for ingame stuff but for fade effects etc that use real time.
        readonly List<TimedGameEvent> timedEvents = new List<TimedGameEvent>();
        readonly Movement movement;
        internal uint CurrentTicks { get; private set; } = 0;
        internal uint CurrentBattleTicks { get; private set; } = 0;
        uint lastMapTicksReset = 0;
        uint lastMoveTicksReset = 0;
        readonly TimedGameEvent ouchEvent = new TimedGameEvent();
        readonly TimedGameEvent hurtPlayerEvent = new TimedGameEvent();
        TravelType travelType = TravelType.Walk;
        readonly NameProvider nameProvider;
        readonly TextDictionary textDictionary;
        internal IDataNameProvider DataNameProvider { get; }
        readonly Layout layout;
        readonly Dictionary<CharacterInfo, UIText> characterInfoTexts = new Dictionary<CharacterInfo, UIText>();
        readonly Dictionary<CharacterInfo, Panel> characterInfoPanels = new Dictionary<CharacterInfo, Panel>();
        readonly IMapManager mapManager;
        internal IItemManager ItemManager { get; }
        internal ICharacterManager CharacterManager { get; }
        readonly Places places;
        readonly IRenderView renderView;
        internal ISavegameManager SavegameManager { get; }
        readonly ISavegameSerializer savegameSerializer;
        Player player;
        internal IRenderPlayer RenderPlayer => is3D ? (IRenderPlayer)player3D: player2D;
        PartyMember CurrentPartyMember { get; set; } = null;
        bool pickingNewLeader = false;
        bool advancing = false; // party or monsters are advancing
        internal PartyMember CurrentInventory => CurrentInventoryIndex == null ? null : GetPartyMember(CurrentInventoryIndex.Value);
        internal int? CurrentInventoryIndex { get; private set; } = null;
        PartyMember CurrentCaster { get; set; } = null;
        public Map Map => !ingame ? null : is3D ? renderMap3D?.Map : renderMap2D?.Map;
        public Position PartyPosition => !ingame || Map == null || player == null ? new Position() : Map.MapOffset + player.Position;
        internal bool MonsterSeesPlayer { get; set; } = false;
        BattleInfo currentBattleInfo = null;
        Battle currentBattle = null;
        internal bool BattleActive => currentBattle != null;
        internal bool BattleRoundActive => currentBattle?.RoundActive == true;
        internal UIText ChestText { get; private set; } = null;
        readonly ILayerSprite[] partyMemberBattleFieldSprites = new ILayerSprite[MaxPartyMembers];
        PlayerBattleAction currentPlayerBattleAction = PlayerBattleAction.PickPlayerAction;
        Spell pickedSpell = Spell.None;
        ItemSlot spellItemSlot = null;
        readonly Dictionary<int, Battle.PlayerBattleAction> roundPlayerBattleActions = new Dictionary<int, Battle.PlayerBattleAction>(MaxPartyMembers);
        readonly ILayerSprite ouchSprite;
        readonly ILayerSprite hurtPlayerSprite; // splash
        readonly IRenderText hurtPlayerDamageText;
        readonly ILayerSprite battleRoundActiveSprite; // sword and mace
        readonly List<ILayerSprite> highlightBattleFieldSprites = new List<ILayerSprite>();
        bool blinkingHighlight = false;
        FilledArea buttonGridBackground;
        readonly bool[] keys = new bool[Enum.GetValues<Key>().Length];
        bool allInputDisabled = false;
        bool inputEnable = true;
        bool paused = false;
        Action nextClickHandler = null;
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
                    ResetMoveKeys();
            }
        }
        TravelType TravelType
        {
            get => travelType;
            set
            {
                travelType = value;
                player.MovementAbility = travelType.ToPlayerMovementAbility();
                if (Map?.IsWorldMap == true)
                {
                    player2D?.UpdateAppearance(CurrentTicks);
                    player2D.BaselineOffset = player.MovementAbility > PlayerMovementAbility.Walking ? 32 : 0;
                }
                else if (!is3D && player2D != null)
                {
                    player2D.BaselineOffset = 0;
                }
            }
        }
        bool leftMouseDown = false;
        bool clickMoveActive = false;
        Rect trapMouseArea = null;
        Position lastMousePosition = new Position();
        readonly Position trappedMousePositionOffset = new Position();
        bool trapped => trapMouseArea != null;
        public event Action<bool, Position> MouseTrappedChanged;
        internal Savegame CurrentSavegame { get; private set; }

        // Rendering
        readonly Cursor cursor = null;
        RenderMap2D renderMap2D = null;
        Player2D player2D = null;
        RenderMap3D renderMap3D = null;
        Player3D player3D = null;
        readonly ICamera3D camera3D = null;
        readonly IRenderText messageText = null;
        readonly IRenderText windowTitle = null;
        /// <summary>
        /// Open chest which can be used to store items.
        /// </summary>
        internal IItemStorage OpenStorage { get; private set; }
        Rect mapViewArea = Map2DViewArea;
        internal static readonly Rect Map2DViewArea = new Rect(Global.Map2DViewX, Global.Map2DViewY,
            Global.Map2DViewWidth, Global.Map2DViewHeight);
        internal static readonly Rect Map3DViewArea = new Rect(Global.Map3DViewX, Global.Map3DViewY,
            Global.Map3DViewWidth, Global.Map3DViewHeight);
        internal int PlayerAngle => is3D ? Util.Round(player3D.Angle) : (int)player2D.Direction.ToAngle();
        internal CursorType CursorType
        {
            get => cursor.Type;
            set
            {
                if (cursor.Type == value)
                    return;

                cursor.Type = value;

                if (!is3D && !WindowActive &&
                    (cursor.Type == CursorType.Eye ||
                    cursor.Type == CursorType.Hand))
                {
                    int yOffset = Map.IsWorldMap ? 12 : 0;
                    TrapMouse(new Rect(player2D.DisplayArea.X - 9, player2D.DisplayArea.Y - 9 - yOffset, 33, 49));
                }
                else if (!is3D && !WindowActive &&
                    cursor.Type == CursorType.Mouth)
                {
                    int yOffset = Map.IsWorldMap ? 12 : 0;
                    TrapMouse(new Rect(player2D.DisplayArea.X - 25, player2D.DisplayArea.Y - 25 - yOffset, 65, 65));
                }
                else
                {
                    UntrapMouse();
                }
            }
        }

        public Game(IRenderView renderView, IMapManager mapManager, IItemManager itemManager,
            ICharacterManager characterManager, ISavegameManager savegameManager, ISavegameSerializer savegameSerializer,
            IDataNameProvider dataNameProvider, IPlacesReader placesReader, TextDictionary textDictionary, Cursor cursor,
            bool legacyMode)
        {
            this.cursor = cursor;
            this.legacyMode = legacyMode;
            movement = new Movement(legacyMode);
            nameProvider = new NameProvider(this);
            this.renderView = renderView;
            this.mapManager = mapManager;
            this.ItemManager = itemManager;
            CharacterManager = characterManager;
            SavegameManager = savegameManager;
            places = Places.Load(placesReader, renderView.GameData.Files["Place_data"].Files[1]);
            this.savegameSerializer = savegameSerializer;
            DataNameProvider = dataNameProvider;
            this.textDictionary = textDictionary;
            camera3D = renderView.Camera3D;
            messageText = renderView.RenderTextFactory.Create();
            messageText.Layer = renderView.GetLayer(Layer.Text);
            windowTitle = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(""), TextColor.Gray, true,
                new Rect(8, 40, 192, 10), TextAlign.Center);
            windowTitle.DisplayLayer = 2;
            layout = new Layout(this, renderView, itemManager);
            layout.BattleFieldSlotClicked += BattleFieldSlotClicked;
            ouchSprite = renderView.SpriteFactory.Create(32, 23, true) as ILayerSprite;
            ouchSprite.Layer = renderView.GetLayer(Layer.UI);
            ouchSprite.PaletteIndex = 0;
            ouchSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Ouch));
            ouchSprite.Visible = false;
            ouchEvent.Action = () => ouchSprite.Visible = false;
            hurtPlayerSprite = renderView.SpriteFactory.Create(32, 26, true, 200) as ILayerSprite;
            hurtPlayerSprite.Layer = renderView.GetLayer(Layer.UI);
            hurtPlayerSprite.PaletteIndex = 49;
            hurtPlayerSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Explosion));
            hurtPlayerSprite.Visible = false;
            hurtPlayerDamageText = renderView.RenderTextFactory.Create();
            hurtPlayerDamageText.Layer = renderView.GetLayer(Layer.Text);
            hurtPlayerDamageText.DisplayLayer = 201;
            hurtPlayerDamageText.TextAlign = TextAlign.Center;
            hurtPlayerDamageText.Shadow = true;
            hurtPlayerDamageText.TextColor = TextColor.White;
            hurtPlayerDamageText.Visible = false;
            hurtPlayerEvent.Action = () => { hurtPlayerDamageText.Visible = false; hurtPlayerSprite.Visible = false; };
            battleRoundActiveSprite = renderView.SpriteFactory.Create(32, 36, true) as ILayerSprite;
            battleRoundActiveSprite.Layer = renderView.GetLayer(Layer.UI);
            battleRoundActiveSprite.PaletteIndex = 0;
            battleRoundActiveSprite.DisplayLayer = 2;
            battleRoundActiveSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset((uint)Graphics.CombatGraphicOffset + (uint)CombatGraphicIndex.UISwordAndMace);
            battleRoundActiveSprite.X = 240;
            battleRoundActiveSprite.Y = 150;
            battleRoundActiveSprite.Visible = false;

            // Create texture atlas for monsters in battle
            var textureAtlasManager = TextureAtlasManager.Instance;
            var monsterGraphicDictionary = CharacterManager.Monsters.ToDictionary(m => m.Index, m => m.CombatGraphic);
            textureAtlasManager.AddFromGraphics(Layer.BattleMonsterRow, monsterGraphicDictionary);
            var monsterGraphicAtlas = textureAtlasManager.GetOrCreate(Layer.BattleMonsterRow);
            renderView.GetLayer(Layer.BattleMonsterRow).Texture = monsterGraphicAtlas.Texture;
        }

        /// <summary>
        /// This is called when the game starts.
        /// This includes intro, main menu, etc.
        /// </summary>
        public void Run()
        {
            // TODO: For now we just start a new game.
            var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);
            Start(initialSavegame);
        }

        public void Quit()
        {
            QuitRequested?.Invoke();
        }

        public void Pause()
        {
            if (paused)
                return;

            paused = true;

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

            if (is3D)
                renderMap3D.Resume();
            else
                renderMap2D.Resume();
        }

        public void Update(double deltaTime)
        {
            for (int i = timedEvents.Count - 1; i >= 0; --i)
            {
                if (DateTime.Now >= timedEvents[i].ExecutionTime)
                {
                    timedEvents[i].Action?.Invoke();
                    timedEvents.RemoveAt(i);
                }
            }

            if (ingame)
            {
                if (!paused)
                {
                    GameTime?.Update();
                    MonsterSeesPlayer = false; // Will be set by the monsters Update methods eventually

                    uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

                    if (CurrentTicks <= uint.MaxValue - add)
                        CurrentTicks += add;
                    else
                        CurrentTicks = (uint)(((long)CurrentTicks + add) % uint.MaxValue);

                    var animationTicks = CurrentTicks >= lastMapTicksReset ? CurrentTicks - lastMapTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMapTicksReset);

                    if (is3D)
                    {
                        renderMap3D.Update(animationTicks, GameTime);
                    }
                    else // 2D
                    {
                        renderMap2D.Update(animationTicks, GameTime);
                    }

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

                if (currentBattle != null)
                {
                    if (!layout.OptionMenuOpen)
                    {
                        uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

                        if (CurrentBattleTicks <= uint.MaxValue - add)
                            CurrentBattleTicks += add;
                        else
                            CurrentBattleTicks = (uint)(((long)CurrentBattleTicks + add) % uint.MaxValue);

                        UpdateBattle();
                    }
                }
                else
                    CurrentBattleTicks = 0;
            }

            layout.Update(CurrentTicks);
        }

        internal int RollDice100()
        {
            return RandomInt(0, 99);
        }

        internal int RandomInt(int min, int max)
        {
            uint range = (uint)(max + 1 - min);
            return min + (int)(random.Next() % range);
        }

        Position GetMousePosition(Position position)
        {
            if (trapMouseArea != null)
                position += trappedMousePositionOffset;

            return position;
        }

        internal void TrapMouse(Rect area, bool keepX = false, bool maxY = false)
        {
            trapMouseArea = renderView.GameToScreen(area);
            trappedMousePositionOffset.X = keepX ? 0 : trapMouseArea.X - lastMousePosition.X;
            trappedMousePositionOffset.Y = (maxY ? trapMouseArea.Bottom : trapMouseArea.Y) - lastMousePosition.Y;
            UpdateCursor(trapMouseArea.Position, MouseButtons.None);
            MouseTrappedChanged?.Invoke(true, GetMousePosition(lastMousePosition));
        }

        internal void UntrapMouse()
        {
            if (trapMouseArea == null)
                return;

            MouseTrappedChanged?.Invoke(false, GetMousePosition(lastMousePosition));
            trapMouseArea = null;
            trappedMousePositionOffset.X = 0;
            trappedMousePositionOffset.Y = 0;
        }

        void ResetMoveKeys()
        {
            keys[(int)Key.Up] = false;
            keys[(int)Key.Down] = false;
            keys[(int)Key.Left] = false;
            keys[(int)Key.Right] = false;
            lastMoveTicksReset = CurrentTicks;
        }

        public Color GetTextColor(TextColor textColor) => GetPaletteColor(51, (int)textColor);

        public Color GetNamedPaletteColor(NamedPaletteColors namedPaletteColor) => GetPaletteColor(50, (int)namedPaletteColor);

        public Color GetPaletteColor(int paletteIndex, int colorIndex)
        {
            var paletteData = renderView.GraphicProvider.Palettes[paletteIndex].Data;
            return new Color
            (
                paletteData[colorIndex * 4 + 0],
                paletteData[colorIndex * 4 + 1],
                paletteData[colorIndex * 4 + 2],
                paletteData[colorIndex * 4 + 3]
            );
        }

        float GetLight3D()
        {
            if (Map.Flags.HasFlag(MapFlags.Outdoor))
            {
                // Light is based on daytime and own light sources
                float daytimeFactor = 1.0f - (Math.Abs((int)CurrentSavegame.Hour * 60 + CurrentSavegame.Minute - 12 * 60)) / (24.0f * 60.0f);
                return daytimeFactor * daytimeFactor * daytimeFactor; // TODO: light sources
            }
            else
            {
                // Light is based on own light sources
                return 1.0f; // TODO: light sources
            }
        }

        internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial)
        {
            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

            layout.SetLayout(LayoutType.Map2D,  movement.MovementTicks(false, Map?.IsWorldMap == true, TravelType.Walk));
            is3D = false;
            uint scrollRefY = playerY + (map.Flags.HasFlag(MapFlags.Indoor) ? 1u : 0u);

            if (renderMap2D.Map != map)
            {
                renderMap2D.SetMap
                (
                    map,
                    (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                    (uint)Util.Limit(0, (int)scrollRefY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y)
                );
            }
            else
            {
                renderMap2D.ScrollTo
                (
                    (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                    (uint)Util.Limit(0, (int)scrollRefY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y),
                    true
                );
            }

            if (player2D == null)
            {
                player2D = new Player2D(this, renderView.GetLayer(Layer.Characters), player, renderMap2D,
                    renderView.SpriteFactory, renderView.GameData, new Position(0, 0), mapManager);
            }

            player2D.Visible = true;
            player2D.MoveTo(map, playerX, playerY, CurrentTicks, true, direction);

            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;
            
            renderView.GetLayer(Layer.Map3D).Visible = false;
            renderView.GetLayer(Layer.Billboards3D).Visible = false;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = true;

            mapViewArea = Map2DViewArea;

            PlayerMoved(true);
        }

        internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

            layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));

            is3D = true;
            TravelType = TravelType.Walk;
            renderMap2D.Destroy();
            renderMap3D.SetMap(map, playerX, playerY, direction);
            renderView.SetLight(GetLight3D());
            player3D.SetPosition((int)playerX, (int)playerY, CurrentTicks, !initial);
            if (player2D != null)
                player2D.Visible = false;
            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;
            
            renderView.GetLayer(Layer.Map3D).Visible = true;
            renderView.GetLayer(Layer.Billboards3D).Visible = true;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = false;

            mapViewArea = Map3DViewArea;

            PlayerMoved(true);
        }

        void Cleanup()
        {
            layout.Reset();
            renderMap2D?.Destroy();
            renderMap2D = null;
            renderMap3D?.Destroy();
            renderMap3D = null;
            player2D?.Destroy();
            player2D = null;
            player3D = null;
            messageText.Visible = false;

            player = null;
            CurrentPartyMember = null;
            CurrentInventoryIndex = null;
            CurrentCaster = null;
            OpenStorage = null;

            for (int i = 0; i < keys.Length; ++i)
                keys[i] = false;
            leftMouseDown = false;
            clickMoveActive = false;
            UntrapMouse();
            InputEnable = false;
            paused = false;
        }

        void PartyMemberDied(Character partyMember)
        {
            if (!(partyMember is PartyMember member))
                throw new AmbermoonException(ExceptionScope.Application, "PartyMemberDied with a character which is not a party member.");

            int? slot = SlotFromPartyMember(member);

            if (slot != null)
                layout.SetCharacter(slot.Value, member);
        }

        void PartyMemberRevived(Character partyMember)
        {
            // TODO
        }

        void AddPartyMember(int slot, PartyMember partyMember)
        {
            partyMember.Died += PartyMemberDied;
            layout.SetCharacter(slot, partyMember);
        }

        void RemovePartyMember(int slot, bool initialize)
        {
            var partyMember = GetPartyMember(slot);

            if (partyMember != null)
                partyMember.Died -= PartyMemberDied;

            layout.SetCharacter(slot, null, initialize);
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
            Cleanup();

            ingame = true;
            CurrentSavegame = savegame;
            GameTime = new SavegameTime(savegame);
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
            SetActivePartyMember(CurrentSavegame.ActivePartyMemberSlot);

            player = new Player();
            var map = mapManager.GetMap(savegame.CurrentMapIndex);
            bool is3D = map.Type == MapType.Map3D;
            renderMap2D = new RenderMap2D(this, null, mapManager, renderView);
            renderMap3D = new RenderMap3D(this, null, mapManager, renderView, 0, 0, CharacterDirection.Up);
            player3D = new Player3D(this, player, mapManager, camera3D, renderMap3D, 0, 0);
            player.MovementAbility = PlayerMovementAbility.Walking;
            renderMap2D.MapChanged += RenderMap2D_MapChanged;
            renderMap3D.MapChanged += RenderMap3D_MapChanged;
            TravelType = savegame.TravelType;
            if (is3D)
                Start3D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1, savegame.CharacterDirection, true);
            else
                Start2D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1 - (map.IsWorldMap ? 0u : 1u), savegame.CharacterDirection, true);
            player.Position.X = (int)savegame.CurrentMapX - 1;
            player.Position.Y = (int)savegame.CurrentMapY - 1;
            TravelType = savegame.TravelType; // Yes this is necessary twice.

            ShowMap(true);

            InputEnable = true;
            paused = false;

            // Trigger events after game load
            TriggerMapEvents(EventTrigger.Move, (uint)player.Position.X,
                (uint)player.Position.Y + (Map.IsWorldMap || is3D ? 0u : 1u));
        }

        void RunSavegameTileChangeEvents(uint mapIndex)
        {
            if (CurrentSavegame.TileChangeEvents.ContainsKey(mapIndex))
            {
                var tileChangeEvents = CurrentSavegame.TileChangeEvents[mapIndex];

                foreach (var tileChangeEvent in tileChangeEvents)
                    UpdateMapTile(tileChangeEvent);
            }
        }

        void RenderMap3D_MapChanged(Map map)
        {
            ResetMoveKeys();
            RunSavegameTileChangeEvents(map.Index);
        }

        void RenderMap2D_MapChanged(Map[] maps)
        {
            ResetMoveKeys();

            foreach (var map in maps)
                RunSavegameTileChangeEvents(map.Index);
        }

        public bool LoadGame(int slot)
        {
            var savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, slot);

            if (savegame == null)
                return false;

            Start(savegame);
            return true;
        }

        public bool HasContinueGame()
        {
            return SavegameManager.Load(renderView.GameData, savegameSerializer, 0) != null;
        }

        public void ContinueGame()
        {
            LoadGame(0);
        }

        // TODO: Optimize to not query this every time
        public List<string> Dictionary => textDictionary.Entries.Where((word, index) =>
            CurrentSavegame.IsDictionaryWordKnown((uint)index)).ToList();

        public IText ProcessText(string text)
        {
            return renderView.TextProcessor.ProcessText(text, nameProvider, Dictionary);
        }

        public void ShowMessage(Rect bounds, string text, TextColor color, bool shadow, TextAlign textAlign = TextAlign.Left)
        {
            messageText.Text = ProcessText(text);
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
            if (amount == 0)
                return;

            var area = new Rect(Global.PartyMemberPortraitAreas[slot]);
            hurtPlayerSprite.X = area.X;
            hurtPlayerSprite.Y = area.Y + 1;
            hurtPlayerSprite.Visible = true;
            hurtPlayerDamageText.Text = renderView.TextProcessor.CreateText(amount.ToString());
            area.Position.Y += 11;
            hurtPlayerDamageText.Place(area, TextAlign.Center);
            hurtPlayerDamageText.Visible = true;

            RenewTimedEvent(hurtPlayerEvent, TimeSpan.FromMilliseconds(500));
        }

        void HandleClickMovement()
        {
            if (paused || WindowActive || !InputEnable || !clickMoveActive || allInputDisabled || pickingNewLeader)
            {
                clickMoveActive = false;
                return;
            }

            lock (cursor)
            {
                Move(cursor.Type);
            }
        }

        void StartSequence()
        {
            layout.ReleaseButtons();
            allInputDisabled = true;
            clickMoveActive = false;
        }

        void EndSequence()
        {
            allInputDisabled = false;
        }

        void PlayTimedSequence(int steps, Action stepAction, int stepTimeInMs)
        {
            if (steps == 0)
                return;

            StartSequence();
            for (int i = 0; i < steps - 1; ++i)
                AddTimedEvent(TimeSpan.FromMilliseconds(i * stepTimeInMs), stepAction);
            AddTimedEvent(TimeSpan.FromMilliseconds((steps - 1) * stepTimeInMs), () => { stepAction?.Invoke(); EndSequence(); });
        }

        internal void Move(CursorType cursorType, bool fromNumpadButton = false)
        {
            if (is3D)
            {
                switch (cursorType)
                {
                    case CursorType.ArrowForward:
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                        break;
                    case CursorType.ArrowBackward:
                        player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                        break;
                    case CursorType.ArrowStrafeLeft:
                        player3D.MoveLeft(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                        break;
                    case CursorType.ArrowStrafeRight:
                        player3D.MoveRight(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
                        break;
                    case CursorType.ArrowTurnLeft:
                        player3D.TurnLeft(movement.TurnSpeed3D * 0.7f);
                        if (!fromNumpadButton)
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f, CurrentTicks);
                        break;
                    case CursorType.ArrowTurnRight:
                        player3D.TurnRight(movement.TurnSpeed3D * 0.7f);
                        if (!fromNumpadButton)
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f, CurrentTicks);
                        break;
                    case CursorType.ArrowRotateLeft:
                        if (fromNumpadButton)
                        {
                            PlayTimedSequence(12, () => player3D.TurnLeft(15.0f), 75);
                        }
                        else
                        {
                            player3D.TurnLeft(movement.TurnSpeed3D * 0.7f);
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f, CurrentTicks);
                        }
                        break;
                    case CursorType.ArrowRotateRight:
                        if (fromNumpadButton)
                        {
                            PlayTimedSequence(12, () => player3D.TurnRight(15.0f), 75);
                        }
                        else
                        {
                            player3D.TurnRight(movement.TurnSpeed3D * 0.7f);
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock * 0.75f, CurrentTicks);
                        }
                        break;
                    default:
                        clickMoveActive = false;
                        break;
                }

                player.Direction = player3D.Direction;
            }
            else
            {
                switch (cursorType)
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
            bool Move()
            {
                bool diagonal = x != 0 && y != 0;

                if (!player2D.Move(x, y, CurrentTicks, TravelType, !diagonal, null, !diagonal))
                {
                    if (!diagonal)
                        return false;

                    var prevDirection = player2D.Direction;

                    if (!player2D.Move(0, y, CurrentTicks, TravelType, false, prevDirection, false))
                        return player2D.Move(x, 0, CurrentTicks, TravelType, true, prevDirection);
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
            if (paused || WindowActive || !InputEnable || allInputDisabled || pickingNewLeader)
                return;

            if (keys[(int)Key.Left] && !keys[(int)Key.Right])
            {
                if (!is3D)
                {
                    // diagonal movement is handled in up/down
                    if (!keys[(int)Key.Up] && !keys[(int)Key.Down])
                        Move2D(-1, 0);
                }
                else
                    player3D.TurnLeft(movement.TurnSpeed3D);
            }
            if (keys[(int)Key.Right] && !keys[(int)Key.Left])
            {
                if (!is3D)
                {
                    // diagonal movement is handled in up/down
                    if (!keys[(int)Key.Up] && !keys[(int)Key.Down])
                        Move2D(1, 0);
                }
                else
                    player3D.TurnRight(movement.TurnSpeed3D);
            }
            if (keys[(int)Key.Up] && !keys[(int)Key.Down])
            {
                if (!is3D)
                {
                    int x = keys[(int)Key.Left] && !keys[(int)Key.Right] ? -1 :
                        keys[(int)Key.Right] && !keys[(int)Key.Left] ? 1 : 0;
                    Move2D(x, -1);
                }
                else
                    player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
            }
            if (keys[(int)Key.Down] && !keys[(int)Key.Up])
            {
                if (!is3D)
                {
                    int x = keys[(int)Key.Left] && !keys[(int)Key.Right] ? -1 :
                        keys[(int)Key.Right] && !keys[(int)Key.Left] ? 1 : 0;
                    Move2D(x, 1);
                }
                else
                    player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerBlock, CurrentTicks);
            }
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            if (allInputDisabled || pickingNewLeader)
                return;

            if (!InputEnable)
            {
                if (key != Key.Escape && !(key >= Key.Num1 && key <= Key.Num9))
                    return;
            }

            keys[(int)key] = true;

            if (!WindowActive)
                Move();

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
                    if (layout.PopupDisableButtons || layout.IsDragging)
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
                    if (WindowActive)
                        layout.KeyDown(key, modifiers);
                    break;
            }

            lastMoveTicksReset = CurrentTicks;
        }

        public void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            if (allInputDisabled)
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
            if (!InputEnable || allInputDisabled)
                return;

            if (!pickingNewLeader && layout.KeyChar(keyChar))
                return;

            if (keyChar >= '1' && keyChar <= '6')
            {
                SetActivePartyMember(keyChar - '1');
            }
        }

        public void OnMouseUp(Position cursorPosition, MouseButtons buttons)
        {
            if (allInputDisabled)
                return;

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
            }

            if (buttons.HasFlag(MouseButtons.Left))
            {
                leftMouseDown = false;
                clickMoveActive = false;

                layout.LeftMouseUp(position, out CursorType? cursorType, CurrentTicks);

                if (cursorType != null && cursorType != CursorType.None)
                    CursorType = cursorType.Value;
                else
                    UpdateCursor(GetMousePosition(cursorPosition), MouseButtons.None);
            }

            if (TextInput.FocusedInput != null)
                CursorType = CursorType.None;
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (allInputDisabled)
                return;

            if (nextClickHandler != null)
            {
                nextClickHandler?.Invoke();
                nextClickHandler = null;
                return;
            }

            position = GetMousePosition(position);

            if (buttons.HasFlag(MouseButtons.Left))
                leftMouseDown = true;

            if (ingame)
            {
                var relativePosition = renderView.ScreenToGame(position);

                if (!WindowActive && InputEnable && !pickingNewLeader && mapViewArea.Contains(relativePosition))
                {
                    // click into the map area
                    if (buttons == MouseButtons.Right)
                        CursorType = CursorType.Sword;
                    if (!buttons.HasFlag(MouseButtons.Left))
                        return;

                    relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);
                    var previousCursor = cursor.Type;

                    if (cursor.Type == CursorType.Eye)
                        TriggerMapEvents(EventTrigger.Eye, relativePosition);
                    else if (cursor.Type == CursorType.Hand)
                        TriggerMapEvents(EventTrigger.Hand, relativePosition);
                    else if (cursor.Type == CursorType.Mouth)
                        TriggerMapEvents(EventTrigger.Mouth, relativePosition);
                    else if (cursor.Type > CursorType.Sword && cursor.Type < CursorType.Wait)
                    {
                        clickMoveActive = true;
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
                    layout.Click(relativePosition, buttons, ref cursorType, CurrentTicks, pickingNewLeader);
                    CursorType = cursorType;

                    if (InputEnable && !pickingNewLeader)
                    {
                        layout.Hover(relativePosition, ref cursorType); // Update cursor
                        if (cursor.Type != CursorType.None)
                            CursorType = cursorType;
                    }

                    // TODO: check for other clicks
                }
            }
            else
            {
                CursorType = CursorType.Sword;
            }

            if (TextInput.FocusedInput != null)
                CursorType = CursorType.None;
        }

        void UpdateCursor(Position cursorPosition, MouseButtons buttons)
        {
            lock (cursor)
            {
                cursor.UpdatePosition(cursorPosition);

                if (!InputEnable)
                {
                    if (layout.PopupActive)
                    {
                        var cursorType = layout.PopupClickCursor ? CursorType.Click : CursorType.Sword;
                        layout.Hover(renderView.ScreenToGame(cursorPosition), ref cursorType);
                        CursorType = cursorType;
                    }
                    else if (layout.Type == LayoutType.Event ||
                        (currentBattle?.RoundActive == true && currentBattle?.ReadyForNextAction == true) ||
                        currentBattle?.WaitForClick == true ||
                        ChestText != null ||
                        layout.InventoryMessageWaitsForClick)
                        CursorType = CursorType.Click;
                    else
                        CursorType = CursorType.Sword;

                    if (layout.IsDragging && layout.InventoryMessageWaitsForClick &&
                        buttons == MouseButtons.None)
                    {
                        layout.UpdateDraggedItemPosition(renderView.ScreenToGame(cursorPosition));
                    }

                    return;
                }

                var relativePosition = renderView.ScreenToGame(cursorPosition);

                if (!WindowActive && (mapViewArea.Contains(relativePosition) || clickMoveActive))
                {
                    // Change arrow cursors when hovering the map
                    if (ingame && cursor.Type >= CursorType.Sword && cursor.Type <= CursorType.Wait)
                    {
                        if (Map.Type == MapType.Map2D)
                        {
                            var playerArea = player2D.DisplayArea;

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
                    if (buttons == MouseButtons.None)
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
            if (!InputEnable && !layout.PopupActive)
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
            }

            lastMousePosition = new Position(position);
            position = GetMousePosition(position);
            UpdateCursor(position, buttons);
        }

        public void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
        {
            bool updateHover = false;

            if (xScroll != 0)
                updateHover = layout.ScrollX(xScroll < 0);
            if (yScroll != 0 && layout.ScrollY(yScroll < 0))
                updateHover = true;

            if (updateHover)
            {
                mousePosition = GetMousePosition(mousePosition);
                UpdateCursor(mousePosition, MouseButtons.None);
            }
        }

        internal IEnumerable<PartyMember> PartyMembers => Enumerable.Range(0, MaxPartyMembers)
            .Select(i => GetPartyMember(i)).Where(p => p != null);
        internal PartyMember GetPartyMember(int slot) => CurrentSavegame.GetPartyMember(slot);
        internal Chest GetChest(uint index) => CurrentSavegame.Chests[(int)index];
        internal Merchant GetMerchant(uint index) => CurrentSavegame.Merchants[(int)index];

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
            if (is3D)
            {
                return renderMap3D.TriggerEvents(this, trigger, x, y, CurrentTicks, CurrentSavegame);
            }
            else // 2D
            {
                return renderMap2D.TriggerEvents(player2D, trigger, x, y, mapManager,
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

        internal bool TriggerMapEvents(EventTrigger trigger)
        {
            bool consumed = TriggerMapEvents(trigger, (uint)player.Position.X, (uint)player.Position.Y);

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
                        return TriggerMapEvents(trigger, (uint)position.X, (uint)position.Y);
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

        void ShowMap(bool show)
        {
            if (show)
            {
                currentBattle = null;
                layout.CancelDrag();
                ResetCursor();
                OpenStorage = null;
                string mapName = Map.IsWorldMap
                    ? DataNameProvider.GetWorldName(Map.World)
                    : Map.Name;
                windowTitle.Text = renderView.TextProcessor.CreateText(mapName);
                windowTitle.TextColor = TextColor.Gray;
                Resume();
                ResetMoveKeys();
            }
            else
            {
                Pause();
            }

            windowTitle.Visible = show;

            if (is3D)
            {
                if (show)
                    layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));
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
                layout.FillArea(new Rect(208, 49, 96, 80), GetPaletteColor(50, 28), false);
                SetWindow(Window.MapView);

                foreach (var specialItem in Enum.GetValues<SpecialItemPurpose>())
                {
                    if (CurrentSavegame.IsSpecialItemActive(specialItem))
                        layout.AddSpecialItem(specialItem);
                }

                foreach (var activeSpell in Enum.GetValues<ActiveSpellType>())
                {
                    if (CurrentSavegame.ActiveSpells[(int)activeSpell] != null)
                        layout.AddActiveSpell(activeSpell, CurrentSavegame.ActiveSpells[(int)activeSpell]);
                }
            }
        }

        internal void OpenPartyMember(int slot, bool inventory)
        {
            if (CurrentSavegame.CurrentPartyMemberIndices[slot] == 0)
                return;

            bool switchedFromOtherPartyMember = CurrentInventory != null;
            bool canAccessInventory = !HasPartyMemberFled(GetPartyMember(slot));

            if (inventory && !canAccessInventory)
            {
                // When fled you can only access the stats.
                // When coming from inventory of another party member
                // you won't be able to open the inventory but if
                // you open the character with F1-F6 or right click
                // you will enter the stats window instead.
                if (switchedFromOtherPartyMember)
                    return;
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

                windowTitle.Text = renderView.TextProcessor.CreateText(DataNameProvider.InventoryTitleString);
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
                        inventoryGrid.SetItem(i, partyMember.Inventory.Slots[i]);
                }
                var equipmentGrid = ItemGrid.CreateEquipment(this, layout, slot, renderView, ItemManager,
                    equipmentSlotPositions, partyMember.Equipment.Slots.Values.ToList(), itemSlot =>
                    {
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
                        equipmentGrid.SetItem((int)equipmentSlot - 1, partyMember.Equipment.Slots[equipmentSlot]);
                }
                void RemoveEquipment(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    EquipmentRemoved(item, amount);

                    if (item.NumberOfHands == 2 && slotIndex == (int)EquipmentSlot.RightHand - 1)
                    {
                        equipmentGrid.SetItem(slotIndex + 2, null);
                        partyMember.Equipment.Slots[EquipmentSlot.LeftHand].Clear();
                    }

                    // TODO: rings/fingers
                    UpdateCharacterInfo();
                }
                void AddEquipment(int slotIndex, ItemSlot itemSlot)
                {
                    var item = ItemManager.GetItem(itemSlot.ItemIndex);
                    EquipmentAdded(item, itemSlot.Amount);

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
                    InventoryItemRemoved(ItemManager.GetItem(itemSlot.ItemIndex), amount);
                    UpdateCharacterInfo();
                }
                void AddInventoryItem(int slotIndex, ItemSlot itemSlot)
                {
                    InventoryItemAdded(ItemManager.GetItem(itemSlot.ItemIndex), itemSlot.Amount);
                    UpdateCharacterInfo();
                }
                equipmentGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
                {
                    RemoveEquipment(slotIndex, draggedItem, draggedAmount);
                    AddEquipment(slotIndex, droppedItem);
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager.GetItem(draggedItem.ItemIndex));
                };
                equipmentGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    RemoveEquipment(slotIndex, itemSlot, amount);
                    partyMember.Equipment.Slots[(EquipmentSlot)(slotIndex + 1)].Remove(amount);
                    // TODO: When resetting the item back to the slot (even just dropping it there) the previous battle action should be restored.
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), ItemManager.GetItem(itemSlot.ItemIndex));
                };
                equipmentGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot) =>
                {
                    AddEquipment(slotIndex, itemSlot);
                    RecheckBattleEquipment(CurrentInventoryIndex.Value, (EquipmentSlot)(slotIndex + 1), null);
                };
                inventoryGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
                {
                    RemoveInventoryItem(slotIndex, draggedItem, draggedAmount);
                    AddInventoryItem(slotIndex, droppedItem);
                };
                inventoryGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    RemoveInventoryItem(slotIndex, itemSlot, amount);
                    partyMember.Inventory.Slots[slotIndex].Remove(amount);
                };
                inventoryGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot) =>
                {
                    AddInventoryItem(slotIndex, itemSlot);
                };
                #endregion
                #region Character info
                DisplayCharacterInfo(partyMember, false);
                // Weight display
                var weightArea = new Rect(27, 152, 68, 15);
                layout.AddPanel(weightArea, 2);
                layout.AddText(weightArea.CreateModified(0, 1, 0, 0), DataNameProvider.CharacterInfoWeightHeaderString,
                    TextColor.White, TextAlign.Center, 5);
                characterInfoTexts.Add(CharacterInfo.Weight, layout.AddText(weightArea.CreateModified(0, 8, 0, 0),
                    string.Format(DataNameProvider.CharacterInfoWeightString, Util.Round(partyMember.TotalWeight / 1000.0f),
                    partyMember.MaxWeight / 1000), TextColor.White, TextAlign.Center, 5));
                #endregion
            }

            void OpenCharacterStats()
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Stats, slot);
                layout.SetLayout(LayoutType.Stats);
                layout.EnableButton(0, canAccessInventory);
                layout.FillArea(new Rect(16, 49, 176, 145), Color.LightGray, false);

                windowTitle.Visible = false;

                CurrentInventoryIndex = slot;
                var partyMember = GetPartyMember(slot);
                int index;

                #region Character info
                DisplayCharacterInfo(partyMember, false);
                #endregion
                #region Attributes
                layout.AddText(new Rect(22, 50, 72, Global.GlyphLineHeight), DataNameProvider.AttributesHeaderString, TextColor.Green, TextAlign.Center);
                index = 0;
                foreach (var attribute in Enum.GetValues<Data.Attribute>())
                {
                    if (attribute == Data.Attribute.Age)
                        break;

                    int y = 57 + index++ * Global.GlyphLineHeight;
                    var attributeValues = partyMember.Attributes[attribute];
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAttributeUIName(attribute));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (attributeValues.TotalCurrentValue > 999 ? "***" : $"{attributeValues.TotalCurrentValue:000}") + $"/{attributeValues.MaxValue:000}");
                }
                #endregion
                #region Abilities
                layout.AddText(new Rect(22, 115, 72, Global.GlyphLineHeight), DataNameProvider.AbilitiesHeaderString, TextColor.Green, TextAlign.Center);
                index = 0;
                foreach (var ability in Enum.GetValues<Ability>())
                {
                    int y = 122 + index++ * Global.GlyphLineHeight;
                    var abilityValues = partyMember.Abilities[ability];
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAbilityUIName(ability));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (abilityValues.TotalCurrentValue > 99 ? "**" : $"{abilityValues.TotalCurrentValue:00}") + $"%/{abilityValues.MaxValue:00}%");
                }
                #endregion
                #region Languages
                layout.AddText(new Rect(106, 50, 72, Global.GlyphLineHeight), DataNameProvider.LanguagesHeaderString, TextColor.Green, TextAlign.Center);
                index = 0;
                foreach (var language in Enum.GetValues<Language>().Skip(1)) // skip Language.None
                {
                    int y = 57 + index++ * Global.GlyphLineHeight;
                    bool learned = partyMember.SpokenLanguages.HasFlag(language);
                    if (learned)
                        layout.AddText(new Rect(106, y, 72, Global.GlyphLineHeight), DataNameProvider.GetLanguageName(language));
                }
                #endregion
                #region Abilities
                layout.AddText(new Rect(22, 115, 72, Global.GlyphLineHeight), DataNameProvider.AbilitiesHeaderString, TextColor.Green, TextAlign.Center);
                index = 0;
                foreach (var ability in Enum.GetValues<Ability>())
                {
                    int y = 122 + index++ * Global.GlyphLineHeight;
                    var abilityValues = partyMember.Abilities[ability];
                    layout.AddText(new Rect(22, y, 30, Global.GlyphLineHeight), DataNameProvider.GetAbilityUIName(ability));
                    layout.AddText(new Rect(52, y, 42, Global.GlyphLineHeight),
                        (abilityValues.TotalCurrentValue > 99 ? "**" : $"{abilityValues.TotalCurrentValue:00}") + $"%/{abilityValues.MaxValue:00}%");
                }
                #endregion
                #region Ailments
                layout.AddText(new Rect(106, 115, 72, Global.GlyphLineHeight), DataNameProvider.AilmentsHeaderString, TextColor.Green, TextAlign.Center);
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
                    layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetAilmentGraphicIndex(ailment), 49,
                        2, DataNameProvider.GetAilmentName(ailment), ailment == Ailment.DeadCorpse ? TextColor.PaleGray : TextColor.Yellow);
                }
                #endregion
            }

            Action openAction = inventory ? (Action)OpenInventory: OpenCharacterStats;

            if ((currentWindow.Window == Window.Inventory && inventory) ||
                (currentWindow.Window == Window.Stats && !inventory))
                openAction();
            else
                Fade(openAction);
        }

        void DisplayCharacterInfo(Character character, bool conversation)
        {
            int offsetY = conversation ? -6 : 0;

            characterInfoTexts.Clear();
            characterInfoPanels.Clear();
            layout.FillArea(new Rect(208, offsetY + 49, 96, 80), Color.LightGray, false);
            layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground, 51, 1);
            layout.AddSprite(new Rect(208, offsetY + 49, 32, 34), Graphics.PortraitOffset + character.PortraitIndex - 1, 49, 2);
            layout.AddText(new Rect(242, offsetY + 49, 62, 7), DataNameProvider.GetRaceName(character.Race));
            layout.AddText(new Rect(242, offsetY + 56, 62, 7), DataNameProvider.GetGenderName(character.Gender));
            characterInfoTexts.Add(CharacterInfo.Age, layout.AddText(new Rect(242, offsetY + 63, 62, 7),
                string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                character.Attributes[Data.Attribute.Age].CurrentValue)));
            characterInfoTexts.Add(CharacterInfo.Level, layout.AddText(new Rect(242, offsetY + 70, 62, 7),
                $"{DataNameProvider.GetClassName(character.Class)} {character.Level}"));
            layout.AddText(new Rect(208, offsetY + 84, 96, 7), character.Name, conversation ? TextColor.Red : TextColor.Yellow, TextAlign.Center);
            if (!conversation)
            {
                characterInfoTexts.Add(CharacterInfo.EP, layout.AddText(new Rect(242, 77, 62, 7),
                    string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                    character.ExperiencePoints)));
                characterInfoTexts.Add(CharacterInfo.LP, layout.AddText(new Rect(208, 92, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoHitPointsString,
                    character.HitPoints.CurrentValue, character.HitPoints.MaxValue),
                    TextColor.White, TextAlign.Center));
                characterInfoTexts.Add(CharacterInfo.SP, layout.AddText(new Rect(208, 99, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                    character.SpellPoints.CurrentValue, character.SpellPoints.MaxValue),
                    TextColor.White, TextAlign.Center));
                characterInfoTexts.Add(CharacterInfo.SLPAndTP, layout.AddText(new Rect(208, 106, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) + " " +
                    string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints), TextColor.White, TextAlign.Center));
                characterInfoTexts.Add(CharacterInfo.GoldAndFood, layout.AddText(new Rect(208, 113, 96, 7),
                    string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, character.Gold, character.Food),
                    TextColor.White, TextAlign.Center));
                layout.AddSprite(new Rect(214, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), 0);
                characterInfoTexts.Add(CharacterInfo.Attack, layout.AddText(new Rect(220, 122, 30, 7),
                    string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', character.BaseAttack < 0 ? '-' : '+'), Math.Abs(character.BaseAttack)),
                    TextColor.White, TextAlign.Left));
                layout.AddSprite(new Rect(261, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), 0);
                characterInfoTexts.Add(CharacterInfo.Defense, layout.AddText(new Rect(268, 122, 30, 7),
                    string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', character.BaseDefense < 0 ? '-' : '+'), Math.Abs(character.BaseDefense)),
                    TextColor.White, TextAlign.Left));
            }
            else
            {
                layout.AddText(new Rect(208, 99, 96, 7), CurrentPartyMember.Name, TextColor.Yellow, TextAlign.Center);
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

        internal void UpdateCharacterInfo(NPC npc = null)
        {
            if (currentWindow.Window != Window.Inventory &&
                currentWindow.Window != Window.Stats &&
                currentWindow.Window != Window.Conversation)
                return;

            if (currentWindow.Window == Window.Conversation)
            {
                if (npc == null || CurrentPartyMember == null)
                    return;
            }
            else if (CurrentInventory == null)
            {
                return;
            }

            void UpdateText(CharacterInfo characterInfo, Func<string> text)
            {
                if (characterInfoTexts.ContainsKey(characterInfo))
                    characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text()));
            }

            var character = (Character)npc ?? CurrentInventory;

            UpdateText(CharacterInfo.Age, () => string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                character.Attributes[Attribute.Age].CurrentValue));
            UpdateText(CharacterInfo.Level, () => $"{DataNameProvider.GetClassName(character.Class)} {character.Level}");
            UpdateText(CharacterInfo.EP, () => string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                character.ExperiencePoints));
            UpdateText(CharacterInfo.LP, () => string.Format(DataNameProvider.CharacterInfoHitPointsString,
                character.HitPoints.CurrentValue, character.HitPoints.MaxValue));
            UpdateText(CharacterInfo.SP, () => string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                character.SpellPoints.CurrentValue, character.SpellPoints.MaxValue));
            UpdateText(CharacterInfo.SLPAndTP, () =>
                string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, character.SpellLearningPoints) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, character.TrainingPoints));
            UpdateText(CharacterInfo.GoldAndFood, () =>
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, character.Gold, character.Food));
            UpdateText(CharacterInfo.Attack, () =>
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', character.BaseAttack < 0 ? '-' : '+'), Math.Abs(character.BaseAttack)));
            UpdateText(CharacterInfo.Defense, () =>
                string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', character.BaseDefense < 0 ? '-' : '+'), Math.Abs(character.BaseDefense)));
            UpdateText(CharacterInfo.Weight, () => string.Format(DataNameProvider.CharacterInfoWeightString,
                Util.Round(character.TotalWeight / 1000.0f), (character as PartyMember).MaxWeight / 1000));
            if (npc != null)
            {
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
            // TODO ...
        }

        internal void InventoryItemAdded(uint itemIndex, int amount, PartyMember partyMember)
        {
            InventoryItemAdded(ItemManager.GetItem(itemIndex), amount);
        }

        void InventoryItemRemoved(Item item, int amount)
        {
            var partyMember = CurrentInventory;

            partyMember.TotalWeight -= (uint)amount * item.Weight;
            // TODO ...
        }

        internal void InventoryItemRemoved(uint itemIndex, int amount)
        {
            InventoryItemRemoved(ItemManager.GetItem(itemIndex), amount);
        }

        void EquipmentAdded(Item item, int amount, PartyMember partyMember = null)
        {
            partyMember ??= CurrentInventory;

            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            partyMember.BaseAttack = (short)(partyMember.BaseAttack + item.Damage);
            partyMember.BaseDefense = (short)(partyMember.BaseDefense + item.Defense);
            partyMember.TotalWeight += (uint)amount * item.Weight;
            // TODO ...
        }

        internal void EquipmentAdded(uint itemIndex, int amount, PartyMember partyMember)
        {
            EquipmentAdded(ItemManager.GetItem(itemIndex), amount, partyMember);
        }

        void EquipmentRemoved(Item item, int amount)
        {
            var partyMember = CurrentInventory;

            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            partyMember.BaseAttack = (short)(partyMember.BaseAttack - item.Damage);
            partyMember.BaseDefense = (short)(partyMember.BaseDefense - item.Defense);
            partyMember.TotalWeight -= (uint)amount * item.Weight;
            // TODO ...
        }

        internal void EquipmentRemoved(uint itemIndex, int amount)
        {
            EquipmentRemoved(ItemManager.GetItem(itemIndex), amount);
        }

        void RenewTimedEvent(TimedGameEvent timedGameEvent, TimeSpan delay)
        {
            timedGameEvent.ExecutionTime = DateTime.Now + delay;

            if (!timedEvents.Contains(timedGameEvent))
                timedEvents.Add(timedGameEvent);
        }

        internal void AddTimedEvent(TimeSpan delay, Action action)
        {
            timedEvents.Add(new TimedGameEvent
            {
                ExecutionTime = DateTime.Now + delay,
                Action = action
            });
        }

        void Fade(Action midFadeAction)
        {
            allInputDisabled = true;
            layout.AddFadeEffect(new Rect(0, 36, Global.VirtualScreenWidth, Global.VirtualScreenHeight - 36), Color.Black, FadeEffectType.FadeInAndOut, FadeTime);
            AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime / 2), midFadeAction);
            AddTimedEvent(TimeSpan.FromMilliseconds(FadeTime), () => allInputDisabled = false);
        }

        internal void Teleport(MapChangeEvent mapChangeEvent)
        {
            Fade(() =>
            {
                var newMap = mapManager.GetMap(mapChangeEvent.MapIndex);
                bool mapChange = newMap.Index != Map.Index;
                var player = is3D ? (IRenderPlayer)player3D : player2D;
                bool mapTypeChanged = Map.Type != newMap.Type;

                // The position (x, y) is 1-based in the data so we subtract 1.
                // Moreover the players position is 1 tile below its drawing position
                // in non-world 2D so subtract another 1 from y.
                player.MoveTo(newMap, mapChangeEvent.X - 1,
                    mapChangeEvent.Y - (newMap.Type == MapType.Map2D && !newMap.IsWorldMap ? 2u : 1u),
                    CurrentTicks, true, mapChangeEvent.Direction);
                this.player.Position.X = RenderPlayer.Position.X;
                this.player.Position.Y = RenderPlayer.Position.Y;

                if (!mapTypeChanged)
                {
                    // Trigger events after map transition
                    TriggerMapEvents(EventTrigger.Move, (uint)player.Position.X,
                        (uint)player.Position.Y + (Map.IsWorldMap || is3D ? 0u : 1u));

                    PlayerMoved(mapChange);
                }
            });
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
                                Position = new Position((int)x + 1, (int)y + 1),
                                TravelType = TravelType
                            };
                            break;
                        }
                    }

                    renderMap2D.PlaceTransport(mapIndex, x, y, TravelType);
                }
                else
                {
                    layout.TransportEnabled = false;
                    if (layout.ButtonGridPage == 1)
                        layout.EnableButton(3, false);
                }

                var tile = renderMap2D[player.Position];

                if (tile.Type == Map.TileType.Water)
                    StartSwimming();
                else
                    TravelType = TravelType.Walk;

                Map.TriggerEvents(this, EventTrigger.Move, x, y, CurrentTicks, CurrentSavegame);
            }
            else if (transport != null && TravelType == TravelType.Walk)
            {
                CurrentSavegame.TransportLocations[index.Value] = null;
                renderMap2D.RemoveTransportAt(mapIndex, x, y);
                TravelType = transport.TravelType;
            }
        }

        TransportLocation GetTransportAtPlayerLocation(out int? index)
        {
            index = null;
            var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
            // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
            var position = new Position(player.Position.X + 1, player.Position.Y + 1);

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

        List<TransportLocation> GetTransportsInVisibleArea(out int? transportAtPlayerIndex)
        {
            transportAtPlayerIndex = null;
            var transports = new List<TransportLocation>();

            if (!Map.IsWorldMap)
                return transports;

            var mapIndex = renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y).Index;
            // Note: Savegame stores positions 1-based but we 0-based so increase by 1,1 for tests below.
            var position = new Position(player.Position.X + 1, player.Position.Y + 1);

            for (int i = 0; i < CurrentSavegame.TransportLocations.Length; ++i)
            {
                var transport = CurrentSavegame.TransportLocations[i];

                if (transport != null && renderMap2D.IsMapVisible(transport.MapIndex))
                {
                    transports.Add(transport);

                    if (transport.MapIndex == mapIndex && transport.Position == position)
                        transportAtPlayerIndex = i;
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

            // TODO: Not sure about the damage formula
            static uint CalculateDamage(PartyMember partyMember)
            {
                var swimAbility = partyMember.Abilities[Ability.Swim].TotalCurrentValue;

                if (swimAbility >= 99)
                    return 0;

                uint baseValue = partyMember.HitPoints.CurrentValue / 2;
                float factor = 0.99f - partyMember.Abilities[Ability.Swim].TotalCurrentValue / 100.0f;
                return (uint)Math.Max(2, Util.Round(factor * baseValue)) - 1;
            }

            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                var partyMember = GetPartyMember(i);

                if (partyMember != null)
                {
                    var damage = CalculateDamage(partyMember);

                    if (damage != 0)
                    {
                        // TODO: show damage splash
                        partyMember.Damage(damage);

                        if (partyMember.Alive) // update HP etc if not died already
                            layout.SetCharacter(i, partyMember);
                    }
                }
            }
        }

        internal void PlayerMoved(bool mapChange)
        {
            // Enable/disable transport button and show transports
            if (!WindowActive)
            {
                if (layout.ButtonGridPage == 1)
                    layout.EnableButton(3, false);

                if (mapChange && Map.Type == MapType.Map2D)
                {
                    renderMap2D.ClearTransports();

                    if (player.MovementAbility <= PlayerMovementAbility.Walking)
                        player2D.BaselineOffset = 0;
                }

                if (!WindowActive && Map.IsWorldMap)
                {
                    var tile = renderMap2D[player.Position];

                    if (tile.Type == Map.TileType.Water)
                    {
                        if (TravelType == TravelType.Walk)
                            StartSwimming();
                        else if (TravelType == TravelType.Swim)
                            DoSwimDamage();
                    }
                    else if (tile.Type != Map.TileType.Water && TravelType == TravelType.Swim)
                        TravelType = TravelType.Walk;

                    var transports = GetTransportsInVisibleArea(out int? transportAtPlayerIndex);

                    foreach (var transport in transports)
                    {
                        renderMap2D.PlaceTransport(transport.MapIndex,
                            (uint)transport.Position.X - 1, (uint)transport.Position.Y - 1, transport.TravelType);
                    }

                    void EnableTransport()
                    {
                        layout.TransportEnabled = true;
                        if (layout.ButtonGridPage == 1)
                            layout.EnableButton(3, true);
                    }

                    if (transportAtPlayerIndex != null && TravelType == TravelType.Walk)
                    {
                        EnableTransport();
                    }
                    else if (TravelType.IsStoppable())
                    {
                        if (TravelType == TravelType.MagicalDisc ||
                            TravelType == TravelType.Raft ||
                            TravelType == TravelType.Ship ||
                            TravelType == TravelType.SandShip)
                        {
                            // We can always leave them as we would stay on them.
                            EnableTransport();
                        }
                        else
                        {
                            // Only allow if we could stand or swim there.
                            var tileset = mapManager.GetTilesetForMap(renderMap2D.GetMapFromTile((uint)player.Position.X, (uint)player.Position.Y));

                            if (tile.AllowMovement(tileset, TravelType.Walk) ||
                                tile.AllowMovement(tileset, TravelType.Swim))
                                EnableTransport();
                        }
                    }
                }
            }

            if (mapChange)
                ResetMoveKeys();
            // TODO
        }

        internal void UpdateMapTile(ChangeTileEvent changeTileEvent)
        {
            bool sameMap = changeTileEvent.MapIndex == 0 || changeTileEvent.MapIndex == Map.Index;
            var map = sameMap ? Map : mapManager.GetMap(changeTileEvent.MapIndex);
            uint x = changeTileEvent.X - 1;
            uint y = changeTileEvent.Y - 1;

            if (is3D)
            {
                map.Blocks[x, y].ObjectIndex = changeTileEvent.FrontTileIndex <= 100 ? changeTileEvent.FrontTileIndex : 0;
                map.Blocks[x, y].WallIndex = changeTileEvent.FrontTileIndex >= 101 && changeTileEvent.FrontTileIndex < 255 ? changeTileEvent.FrontTileIndex - 100 : 0;

                if (sameMap)
                    renderMap3D.UpdateBlock(x, y);
            }
            else // 2D
            {
                map.UpdateTile(x, y, changeTileEvent.BackTileIndex, changeTileEvent.FrontTileIndex, mapManager.GetTilesetForMap(map));

                if (sameMap) // TODO: what if we change an adjacent world map which is visible instead? is there even a use case?
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

            // TODO: what if we change an adjacent world map which is visible instead? is there even a use case?
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

            if (chestEvent.Next != null)
                Map.TriggerEventChain(this, EventTrigger.Always, 0, 0, CurrentTicks, chestEvent.Next, true);

            CloseWindow();
        }

        internal void ItemRemovedFromStorage()
        {
            if (OpenStorage is Chest chest)
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
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                ShowTextPanel(CharacterInfo.ChestGold, true,
                    $"{DataNameProvider.GoldName}^{chest.Gold}", new Rect(111, 104, 43, 15));
            }
            else
            {
                HideTextPanel(CharacterInfo.ChestGold);

                if (chest.Empty)
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
                layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                ShowTextPanel(CharacterInfo.ChestFood, true,
                    $"{DataNameProvider.FoodName}^{chest.Food}", new Rect(260, 104, 43, 15));
            }
            else
            {
                HideTextPanel(CharacterInfo.ChestFood);

                if (chest.Empty)
                {
                    layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);

                    if (!chest.AllowsItemDrop)
                        ChestRemoved();
                }
            }
        }

        internal void ShowChest(ChestEvent chestMapEvent, Map map = null)
        {
            var chest = GetChest(chestMapEvent.ChestIndex);

            if (chestMapEvent.RemoveWhenEmpty && chest.Empty)
                return;

            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Chest, chestMapEvent);
                OpenStorage = chest;
                OpenStorage.AllowsItemDrop = !chestMapEvent.RemoveWhenEmpty;
                layout.SetLayout(LayoutType.Items);
                layout.FillArea(new Rect(110, 43, 194, 80), GetPaletteColor(50, 28), false);
                var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
                itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
                var itemGrid = ItemGrid.Create(this, layout, renderView, ItemManager, itemSlotPositions, chest.Slots.ToList(),
                    !chestMapEvent.RemoveWhenEmpty, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
                layout.AddItemGrid(itemGrid);

                if (chestMapEvent.Lock != ChestEvent.LockFlags.Open && CurrentSavegame.IsChestLocked(chestMapEvent.ChestIndex))
                {
                    layout.Set80x80Picture(Picture80x80.ChestClosed);
                    itemGrid.Disabled = true;
                }
                else
                {
                    if (chest.Empty)
                    {
                        layout.Set80x80Picture(Picture80x80.ChestOpenEmpty);
                    }
                    else
                    {
                        layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                    }

                    for (int y = 0; y < 2; ++y)
                    {
                        for (int x = 0; x < 6; ++x)
                        {
                            var slot = chest.Slots[x, y];

                            if (!slot.Empty)
                                itemGrid.SetItem(x + y * 6, slot);
                        }
                    }

                    itemGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                    {
                        int column = slotIndex % Chest.SlotsPerRow;
                        int row = slotIndex / Chest.SlotsPerRow;
                        chest.Slots[column, row].Remove(amount);
                    };
                    itemGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot) =>
                    {
                        layout.Set80x80Picture(Picture80x80.ChestOpenFull);
                    };

                    if (chest.Gold > 0)
                    {
                        ShowTextPanel(CharacterInfo.ChestGold, true,
                            $"{DataNameProvider.GoldName}^{chest.Gold}", new Rect(111, 104, 43, 15));
                    }

                    if (chest.Food > 0)
                    {
                        ShowTextPanel(CharacterInfo.ChestFood, true,
                            $"{DataNameProvider.FoodName}^{chest.Food}", new Rect(260, 104, 43, 15));
                    }

                    if (map != null && chestMapEvent.TextIndex != 255)
                    {
                        ChestText = layout.AddScrollableText(new Rect(114, 46, 189, 48), ProcessText(map.Texts[(int)chestMapEvent.TextIndex]));
                        ChestText.Clicked += scrolledToEnd =>
                        {
                            if (scrolledToEnd)
                            {
                                ChestText?.Destroy();
                                ChestText = null;
                                InputEnable = true;
                                CursorType = CursorType.Sword;
                            }
                        };
                        CursorType = CursorType.Click;
                        InputEnable = false;
                    }
                }
            });
        }

        /// <summary>
        /// A conversation is started with a Conversation event but the
        /// displayed text depends on the following events. Mostly
        /// Condition and PrintText events. The argument conversationEvent
        /// is the first event after the initial event and should be used
        /// to determine the text to print etc.
        /// 
        /// The event chain may also contain rewards, new keywords, etc.
        /// </summary>
        internal void ShowConversation(IConversationPartner conversationPartner, Event conversationEvent)
        {
            void SayWord(string word)
            {
                UntrapMouse();
                // TODO
            }

            bool lastEventStatus = false;
            bool aborted = false;
            var textArea = new Rect(15, 43, 177, 80);

            void HandleNextEvent()
            {
                if (conversationEvent is PrintTextEvent printTextEvent)
                {
                    var text = conversationPartner.Texts[(int)printTextEvent.NPCTextIndex];
                    layout.AddScrollableText(textArea, ProcessText(text));
                    // TODO: it is added as scrollable but it isn't scrollable yet
                    // TODO: clear old text
                }

                // TODO: handle Create events as we need to take the items before progressing!

                conversationEvent = conversationEvent.ExecuteEvent(Map, this, EventTrigger.Always, 0, 0, // TODO: do we care about x and y here?
                    CurrentTicks, ref lastEventStatus, out aborted, conversationPartner);
                SetWindow(Window.Conversation, conversationPartner, conversationEvent);
            }

            Fade(() =>
            {
                SetWindow(Window.Conversation, conversationPartner, conversationEvent);
                layout.SetLayout(LayoutType.Conversation);
                ShowMap(false);
                layout.Reset();

                layout.FillArea(textArea, GetPaletteColor(50, 28), false);
                layout.FillArea(new Rect(15, 136, 152, 57), GetPaletteColor(50, 28), false);

                if (!(conversationPartner is Character character))
                    throw new AmbermoonException(ExceptionScope.Application, "Conversation partner is no character.");
                DisplayCharacterInfo(character, true);

                layout.AttachEventToButton(0, () => OpenDictionary(SayWord));

                while (conversationEvent != null && !aborted)
                    HandleNextEvent();

                // TODO
            });
        }

        /// <summary>
        /// Starts a battle with the given monster group index.
        /// It is used for monsters that are present on the map.
        /// </summary>
        /// <param name="monsterGroupIndex">Monster group index</param>
        internal void StartBattle(uint monsterGroupIndex, bool failedEscape, Action<BattleEndInfo> battleEndHandler)
        {
            currentBattleInfo = new BattleInfo
            {
                MonsterGroupIndex = monsterGroupIndex
            };
            currentBattleInfo.BattleEnded += battleEndHandler;
            ShowBattleWindow(null, failedEscape);
        }

        void UpdateBattle()
        {
            currentBattle.Update(CurrentBattleTicks);

            if (advancing)
            {
                foreach (var monster in currentBattle.Monsters)
                    layout.GetMonsterBattleAnimation(monster).Update(CurrentBattleTicks);
            }

            if (highlightBattleFieldSprites.Count != 0)
            {
                bool showBlinkingSprites = !blinkingHighlight || (CurrentBattleTicks % (2 * TicksPerSecond / 3)) < TicksPerSecond / 3;

                foreach (var blinkingBattleFieldSprite in highlightBattleFieldSprites)
                {
                    blinkingBattleFieldSprite.Visible = showBlinkingSprites;
                }

                if (showBlinkingSprites)
                    RemoveCurrentPlayerActionVisuals();
                else
                    AddCurrentPlayerActionVisuals();
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

        void ShowBattleWindow(Event nextEvent)
        {
            SetWindow(Window.Battle, nextEvent);
            layout.SetLayout(LayoutType.Battle);
            ShowMap(false);
            layout.Reset();

            // TODO: where is the combat background index for 2D maps?
            uint combatBackgroundIndex = is3D ? renderMap3D.CombatBackgroundIndex : Map.World switch
            {
                World.Lyramion => 0u,
                World.ForestMoon => 6u,
                World.Morag => 4u,
                _ => 0u
            };
            var combatBackground = is3D
                ? renderView.GraphicProvider.Get3DCombatBackground(combatBackgroundIndex)
                : renderView.GraphicProvider.Get2DCombatBackground(combatBackgroundIndex);
            layout.AddSprite(Global.CombatBackgroundArea, Graphics.CombatBackgroundOffset + combatBackground.GraphicIndex - 1,
                (byte)(combatBackground.Palettes[GameTime.CombatBackgroundPaletteIndex()] - 1), 1, null, null, Layer.CombatBackground);
            layout.FillArea(new Rect(0, 132, 320, 68), Color.Black, 0);
            layout.FillArea(new Rect(5, 139, 84, 56), GetPaletteColor(50, 28), 1);

            if (currentBattle != null)
            {
                var monsterBattleAnimations = new Dictionary<int, BattleAnimation>(24);
                foreach (var monster in currentBattle.Monsters)
                {
                    int slot = currentBattle.GetSlotFromCharacter(monster);
                    monsterBattleAnimations.Add(slot, layout.AddMonsterCombatSprite(slot % 6, slot / 6, monster,
                        currentBattle.GetMonsterDisplayLayer(monster, slot)));
                }
                currentBattle.SetMonsterAnimations(monsterBattleAnimations);
            }

            // Add battle field sprites for party members
            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                var partyMember = GetPartyMember(i);

                if (partyMember == null || !partyMember.Alive || HasPartyMemberFled(partyMember))
                    partyMemberBattleFieldSprites[i] = null;
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
                    ), Graphics.BattleFieldIconOffset + (uint)partyMember.Class, 49, (byte)(3 + battleRow),
                    $"{partyMember.HitPoints.TotalCurrentValue}/{partyMember.HitPoints.MaxValue}^{partyMember.Name}",
                    partyMember.Ailments.CanSelect() ? TextColor.White : TextColor.PaleGray);
                }
            }
            UpdateBattleStatus();

            // Flee button
            layout.AttachEventToButton(0, () =>
            {
                SetCurrentPlayerBattleAction(Battle.BattleActionType.Flee);
            });
            // OK button
            layout.AttachEventToButton(2, () =>
            {
                StartBattleRound(false);
                /*var battleEndInfo = new BattleEndInfo
                {
                    MonstersDefeated = true,
                    FledMonsterIndices = new List<uint>()
                };
                */
            });
            // Move button
            layout.AttachEventToButton(3, () =>
            {
                SetCurrentPlayerAction(PlayerBattleAction.PickMoveSpot);
            });
            // Move group forward button
            layout.AttachEventToButton(4, () =>
            {
                SetBattleMessageWithClick(DataNameProvider.BattleMessagePartyAdvances, TextColor.Gray, () =>
                {
                    InputEnable = false;
                    currentBattle.WaitForClick = true;
                    CursorType = CursorType.Click;
                    AdvanceParty(() =>
                    {
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
                OpenSpellList(CurrentPartyMember,
                    spell =>
                    {
                        var spellInfo = SpellInfos.Entries[spell];

                        if (!spellInfo.ApplicationArea.HasFlag(SpellApplicationArea.Battle))
                            return DataNameProvider.WrongArea;

                        if (spellInfo.SP > CurrentPartyMember.SpellPoints.TotalCurrentValue)
                            return DataNameProvider.NotEnoughSP;

                        // TODO: Is there more to check? Irritated?

                        return null;
                    },
                    spell =>
                    {
                        // pickedSpell = spell;
                        if (spell != Spell.Firebeam &&
                            spell != Spell.Fireball &&
                            spell != Spell.Firestorm &&
                            spell != Spell.Firepillar &&
                            spell != Spell.Iceball &&
                            spell != Spell.DissolveVictim &&
                            !(spell >= Spell.Lame && spell <= Spell.Drug))
                            pickedSpell = Spell.Iceball; // TODO
                        else
                            pickedSpell = spell;
                        // TODO: spellItemSlot -> item used to cast the spell or null
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
                            default:
                                SetCurrentPlayerBattleAction(Battle.BattleActionType.CastSpell,
                                    Battle.CreateCastSpellParameter(0, pickedSpell, spellItemSlot?.ItemIndex ?? 0));
                                break;
                        }
                    }
                );
            });

            if (currentBattle != null)
                BattlePlayerSwitched();
        }

        void AdvanceParty(Action finishAction)
        {
            int advancedMonsters = 0;
            int totalMonsters = currentBattle.Monsters.Count();

            void MoveMonster(Monster monster)
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
                    currentBattle.MoveCharacterTo((uint)(position + 6), monster);

                    if (++advancedMonsters == totalMonsters)
                    {
                        advancing = false;
                        layout.EnableButton(4, currentBattle.CanMoveForward);
                        finishAction?.Invoke();
                    }
                }

                var newDisplayPosition = layout.GetMonsterCombatCenterPosition(currentColumn, newRow, monster);
                animation.AnimationFinished += MoveAnimationFinished;
                animation.Play(new int[] { 0 }, TicksPerSecond / 2, CurrentBattleTicks, newDisplayPosition,
                    layout.RenderView.GraphicProvider.GetMonsterRowImageScaleFactor((MonsterRow)newRow));
            }

            foreach (var monster in currentBattle.Monsters)
            {
                MoveMonster(monster);
            }

            advancing = true;
        }

        void ShowBattleWindow(Event nextEvent, bool surpriseAttack)
        {
            Fade(() =>
            {
                roundPlayerBattleActions.Clear();
                ShowBattleWindow(nextEvent);
                // Note: Create clones so we can change the values in battle for each monster.
                var monsterGroup = CharacterManager.GetMonsterGroup(currentBattleInfo.MonsterGroupIndex).Clone();
                foreach (var monster in monsterGroup.Monsters)
                    InitializeMonster(monster);
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
                                layout.AddMonsterCombatSprite(column, row, monster, 0));
                        }
                    }
                }
                currentBattle = new Battle(this, layout, Enumerable.Range(0, MaxPartyMembers).Select(i => GetPartyMember(i)).ToArray(),
                    monsterGroup, monsterBattleAnimations, true); // TODO: make last param dependent on game options
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
                    if (RecheckActivePartyMember())
                        BattlePlayerSwitched();
                    else
                        AddCurrentPlayerActionVisuals();
                    UpdateBattleStatus();
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
                    for (int i = 0; i < MaxPartyMembers; ++i)
                    {
                        if (GetPartyMember(i) != null)
                            layout.UpdateCharacterStatus(i, null);
                    }
                    void EndBattle(BattleEndInfo battleEndInfo)
                    {
                        for (int i = 0; i < MaxPartyMembers; ++i)
                        {
                            var partyMember = GetPartyMember(i);

                            if (partyMember != null)
                                partyMember.Ailments = partyMember.Ailments.WithoutBattleOnlyAilments();
                        }
                        UpdateBattleStatus();
                        currentBattleInfo.EndBattle(battleEndInfo);
                        currentBattleInfo = null;
                        if (nextEvent != null)
                        {
                            EventExtensions.TriggerEventChain(Map, this, EventTrigger.Always, (uint)RenderPlayer.Position.X,
                                (uint)RenderPlayer.Position.Y, CurrentTicks, nextEvent, true);
                        }
                    }
                    if (battleEndInfo.MonstersDefeated)
                    {
                        currentBattle = null;
                        ShowBattleLoot(battleEndInfo, () =>
                        {
                            EndBattle(battleEndInfo);
                        });
                    }
                    else if (PartyMembers.Any(p => p.Alive && p.Ailments.CanFight()))
                    {
                        // There are fled survivors
                        currentBattle = null;
                        EndBattle(battleEndInfo);
                        CloseWindow();
                        InputEnable = true;
                    }
                    else
                    {
                        currentBattleInfo = null;
                        currentBattle = null;
                        CloseWindow();
                        InputEnable = true;
                        GameOver();
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
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                currentBattle.PlayerLostTarget += partyMember =>
                {
                    roundPlayerBattleActions.Remove(SlotFromPartyMember(partyMember).Value);
                    layout.UpdateCharacterStatus(SlotFromPartyMember(partyMember).Value, null);
                };
                BattlePlayerSwitched();

                if (surpriseAttack)
                {
                    StartBattleRound(true);
                }
            });
        }

        void StartBattleRound(bool withoutPlayerActions)
        {
            InputEnable = false;
            CursorType = CursorType.Click;
            layout.ResetMonsterCombatSprites();
            layout.ClearBattleFieldSlotColors();
            layout.ShowButtons(false);
            buttonGridBackground = layout.FillArea(new Rect(Global.ButtonGridX, Global.ButtonGridY, 3 * Button.Width, 3 * Button.Height),
                GetPaletteColor(50, 28), 1);
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
        // I assume that the max hitpoints should be the same
        // as current hit points.
        static void InitializeMonster(Monster monster)
        {
            if (monster == null)
                return;

            static void FixValue(CharacterValue characterValue)
            {
                if (characterValue.CurrentValue < characterValue.MaxValue && characterValue.MaxValue % 99 == 0)
                    characterValue.MaxValue = characterValue.CurrentValue;
            }

            // Attributes, abilities, LP and SP is special for monsters.
            foreach (var attribute in Enum.GetValues<Attribute>())
                FixValue(monster.Attributes[attribute]);
            foreach (var ability in Enum.GetValues<Ability>())
                FixValue(monster.Abilities[ability]);
            // TODO: the given max value might be used for something else
            monster.HitPoints.MaxValue = monster.HitPoints.CurrentValue;
            monster.SpellPoints.MaxValue = monster.SpellPoints.CurrentValue;

            // TODO: some values seem to be a bit different (use monster knowledge on skeleton for examples)
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

            layout.EnableButton(0, battleFieldSlot >= 24 && CurrentPartyMember.Ailments.CanFlee()); // flee button, only enable in last row
            layout.EnableButton(3, CurrentPartyMember.Ailments.CanMove()); // Note: If no slot is available the button still is enabled but after clicking you get "You can't move anywhere".
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
                        layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileFromParameter(action.Parameter), BattleFieldSlotColor.Orange);
                        break;
                    case Battle.BattleActionType.CastSpell:
                        var spell = (Spell)(action.Parameter >> 16);
                        switch (SpellInfos.Entries[spell].Target)
                        {
                            case SpellTarget.Self:
                                layout.SetBattleFieldSlotColor(currentBattle.GetSlotFromCharacter(CurrentPartyMember), BattleFieldSlotColor.Orange);
                                break;
                            case SpellTarget.SingleEnemy:
                            case SpellTarget.SingleFriend:
                                layout.SetBattleFieldSlotColor((int)action.Parameter & 0xf, BattleFieldSlotColor.Orange);
                                break;
                            case SpellTarget.EnemyRow:
                            {
                                SetBattleRowSlotColors((int)action.Parameter & 0xf,
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
                    layout.SetBattleFieldSlotColor((int)Battle.GetTargetTileFromParameter(action.Parameter), BattleFieldSlotColor.None);
                    break;
                case Battle.BattleActionType.CastSpell:
                    layout.ClearBattleFieldSlotColorsExcept(currentBattle.GetSlotFromCharacter(CurrentPartyMember));
                    if (Battle.IsSelfSpell(action.Parameter))
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

        Battle.PlayerBattleAction GetOrCreateBattleAction()
        {
            int slot = SlotFromPartyMember(CurrentPartyMember).Value;

            if (!roundPlayerBattleActions.ContainsKey(slot))
                roundPlayerBattleActions.Add(slot, new Battle.PlayerBattleAction());

            return roundPlayerBattleActions[slot];
        }

        internal void SetBattleMessageWithClick(string message, TextColor textColor = TextColor.White, Action followAction = null, TimeSpan? delay = null)
        {
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
                    void Follow()
                    {
                        layout.SetBattleMessage(null);
                        InputEnable = true;
                        currentBattle.WaitForClick = false;
                        CursorType = CursorType.Sword;
                        followAction?.Invoke();
                    }

                    nextClickHandler = Follow;
                }
            }
        }

        bool AnyPlayerMovesTo(int slot)
        {
            return roundPlayerBattleActions.Any(p => p.Value.BattleAction == Battle.BattleActionType.Move &&
                Battle.GetTargetTileFromParameter(p.Value.Parameter) == slot);
        }

        void BattleFieldSlotClicked(int column, int row)
        {
            if (currentBattle.SkipNextBattleFieldClick)
                return;

            if (row < 0 || row > 4 ||
                column < 0 || column > 5)
                return;

            switch (currentPlayerBattleAction)
            {
                case PlayerBattleAction.PickPlayerAction:
                {
                    var character = currentBattle.GetCharacterAt(column, row);

                    if (character?.Type == CharacterType.PartyMember)
                    {
                        var partyMember = character as PartyMember;

                        if (CurrentPartyMember != partyMember && partyMember.Ailments.CanSelect())
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
                            int position = currentBattle.GetSlotFromCharacter(CurrentPartyMember);
                            if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                            {
                                SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.Gray);
                                return;
                            }
                        }

                        SetCurrentPlayerBattleAction(Battle.BattleActionType.Attack,
                            Battle.CreateAttackParameter((uint)(column + row * 6), CurrentPartyMember, ItemManager));
                    }
                    else // empty field
                    {
                        if (row < 3)
                            return;
                        int position = currentBattle.GetSlotFromCharacter(CurrentPartyMember);
                        if (Math.Abs(column - position % 6) > 1 || Math.Abs(row - position / 6) > 1)
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageTooFarAway, TextColor.Gray);
                            return;
                        }
                        if (!CurrentPartyMember.Ailments.CanMove())
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotMove, TextColor.Gray);
                            return;
                        }
                        int newPosition = column + row * 6;
                        int slot = SlotFromPartyMember(CurrentPartyMember).Value;
                        if ((!roundPlayerBattleActions.ContainsKey(slot) ||
                            roundPlayerBattleActions[slot].BattleAction != Battle.BattleActionType.Move ||
                            Battle.GetTargetTileFromParameter(roundPlayerBattleActions[slot].Parameter) != newPosition) &&
                            AnyPlayerMovesTo(newPosition))
                        {
                            SetBattleMessageWithClick(DataNameProvider.BattleMessageSomeoneAlreadyGoingThere, TextColor.Gray);
                            return;
                        }
                        SetCurrentPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)(column + row * 6)));
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

                        SetCurrentPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)(column + row * 6),
                            pickedSpell, spellItemSlot?.ItemIndex ?? 0));
                        layout.SetBattleFieldSlotColor(column, row, BattleFieldSlotColor.Orange);
                        SetCurrentPlayerAction(PlayerBattleAction.PickPlayerAction);
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
                    SetCurrentPlayerBattleAction(Battle.BattleActionType.CastSpell, Battle.CreateCastSpellParameter((uint)row,
                        pickedSpell, spellItemSlot?.ItemIndex ?? 0));
                    layout.ClearBattleFieldSlotColorsExcept(currentBattle.GetSlotFromCharacter(CurrentPartyMember));
                    SetBattleRowSlotColors(row, (c, r) => currentBattle.GetCharacterAt(c, r)?.Type == CharacterType.Monster, BattleFieldSlotColor.Orange);
                    CancelSpecificPlayerAction();
                    break;
                }
                case PlayerBattleAction.PickMoveSpot:
                {
                    int position = column + row * 6;
                    if (currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position))
                    {
                        SetCurrentPlayerBattleAction(Battle.BattleActionType.Move, Battle.CreateMoveParameter((uint)position));
                        CancelSpecificPlayerAction();
                    }
                    break;
                }
                case PlayerBattleAction.PickAttackSpot:
                {
                    if (!CheckAbilityToAttack(out bool ranged))
                        return;

                    if (currentBattle.GetCharacterAt(column + row * 6)?.Type == CharacterType.Monster)
                    {
                        SetCurrentPlayerBattleAction(Battle.BattleActionType.Attack,
                            Battle.CreateAttackParameter((uint)(column + row * 6), CurrentPartyMember, ItemManager));
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
            int slot = currentBattle.GetSlotFromCharacter(CurrentPartyMember);
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
            ranged = CurrentPartyMember.HasLongRangedAttack(ItemManager, out bool hasAmmo);

            if (ranged && !hasAmmo)
            {
                // No ammo for ranged weapon
                CancelSpecificPlayerAction();
                SetBattleMessageWithClick(DataNameProvider.BattleMessageNoAmmunition, TextColor.Gray);
                return false;
            }

            if (CurrentPartyMember.BaseAttack <= 0)
            {
                CancelSpecificPlayerAction();
                SetBattleMessageWithClick(DataNameProvider.BattleMessageUnableToAttack, TextColor.Gray);
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

            // TODO: show possible fields etc
            switch (currentPlayerBattleAction)
            {
                case PlayerBattleAction.PickPlayerAction:
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
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight), 50
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
                    // TODO: only show 1 row and only when hovering the row
                    var valuableRows = Enumerable.Range(0, 4).Where(r => Enumerable.Range(0, 6).Any(c => currentBattle.GetCharacterAt(c + r * 6)?.Type == CharacterType.Monster));
                    foreach (var row in valuableRows)
                    {
                        for (int column = 0; column < 6; ++column)
                        {
                            highlightBattleFieldSprites.Add
                            (
                                layout.AddSprite
                                (
                                    Global.BattleFieldSlotArea(column + row * 6),
                                    Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight), 50
                                )
                            );
                        }
                    }
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhichMonsterRowAsTarget);
                    break;
                }
                case PlayerBattleAction.PickFriendSpellTarget:
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
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight), 50
                            )
                        );
                    }
                    RemoveCurrentPlayerActionVisuals();
                    TrapMouse(Global.BattleFieldArea);
                    blinkingHighlight = true;
                    layout.SetBattleMessage(DataNameProvider.BattleMessageWhichPartyMemberAsTarget);
                    break;
                }
                case PlayerBattleAction.PickMoveSpot:
                {
                    // TODO: In original game if a slot is empty but someone moves there, it is still highlighted but with a red cross icon.
                    var valuableSlots = GetValuableBattleFieldSlots(position => currentBattle.IsBattleFieldEmpty(position) && !AnyPlayerMovesTo(position),
                        1, 3, 4);
                    foreach (var slot in valuableSlots)
                    {
                        highlightBattleFieldSprites.Add
                        (
                            layout.AddSprite
                            (
                                Global.BattleFieldSlotArea(slot),
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight), 50
                            )
                        );
                    }
                    if (highlightBattleFieldSprites.Count == 0)
                    {
                        // No movement possible
                        CancelSpecificPlayerAction();
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageNowhereToMoveTo, TextColor.Gray);
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
                                Graphics.GetCustomUIGraphicIndex(UICustomGraphic.BattleFieldGreenHighlight), 50
                            )
                        );
                    }
                    if (highlightBattleFieldSprites.Count == 0)
                    {
                        // No attack possible
                        CancelSpecificPlayerAction();
                        SetBattleMessageWithClick(DataNameProvider.BattleMessageCannotReachAnyone, TextColor.Gray);
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
            // TODO
        }

        internal void StartBattle(StartBattleEvent battleEvent, Event nextEvent)
        {
            currentBattleInfo = new BattleInfo
            {
                MonsterGroupIndex = battleEvent.MonsterGroupIndex
            };
            ShowBattleWindow(nextEvent, true);
        }

        internal void ShowBattleLoot(BattleEndInfo battleEndInfo, Action closeAction)
        {
            InputEnable = true;
            // TODO
            CloseWindow();
            closeAction?.Invoke();
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
                layout.FillArea(riddleArea, GetPaletteColor(50, 28), false);
                var riddleText = ProcessText(map.Texts[(int)riddlemouthEvent.RiddleTextIndex]);
                var solutionResponseText = ProcessText(map.Texts[(int)riddlemouthEvent.SolutionTextIndex]);
                void ShowRiddle()
                {
                    InputEnable = false;
                    layout.OpenTextPopup(riddleText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                    {
                        InputEnable = true;
                    };
                }
                void TestSolution(string solution)
                {
                    if (string.Compare(textDictionary.Entries[(int)riddlemouthEvent.CorrectAnswerDictionaryIndex], solution, true) == 0)
                    {
                        InputEnable = false;
                        layout.OpenTextPopup(solutionResponseText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White, () =>
                        {
                            Fade(() =>
                            {
                                CloseWindow();
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
                        layout.OpenTextPopup(failedText, riddleArea.Position, riddleArea.Width, riddleArea.Height, true, true, true, TextColor.White).Closed += () =>
                        {
                            InputEnable = true;
                        };
                    }
                }
                if (showRiddle)
                    ShowRiddle();
                layout.AttachEventToButton(6, () => OpenDictionary(TestSolution));
                layout.AttachEventToButton(8, ShowRiddle);
                // TODO
            });
        }

        internal uint GetPlayerPaletteIndex() => Map.PaletteIndex;

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
                    IgnoreTileType = false
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

        internal void OpenDictionary(Action<string> choiceHandler)
        {
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
            mouthButton.Action = () =>
                layout.OpenInputPopup(new Position(51, 87), 20, (string solution) => choiceHandler?.Invoke(solution));
            exitButton.Action = () => layout.ClosePopup();
            popup.AddDictionaryListBox(Dictionary.Select(entry => new KeyValuePair<string, Action<int, string>>
            (
                entry, (int _, string text) =>
                {
                    layout.ClosePopup(false);
                    choiceHandler?.Invoke(text);
                }
            )).ToList());
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
            const int columns = 13;
            const int rows = 10;
            var popupArea = new Rect(32, 40, columns * 16, rows * 16);
            TrapMouse(new Rect(popupArea.Left + 16, popupArea.Top + 16, popupArea.Width - 32, popupArea.Height - 32));
            var popup = layout.OpenPopup(popupArea.Position, columns, rows, true, false);
            var spells = partyMember.LearnedSpells.Select(spell => new KeyValuePair<Spell, string>(spell, spellAvailableChecker(spell))).ToList();
            var spellList = popup.AddSpellListBox(spells.Select(spell => new KeyValuePair<string, Action<int, string>>
            (
                DataNameProvider.GetSpellname(spell.Key), spell.Value != null ? null : (Action<int, string>)((int index, string _) =>
                {
                    UntrapMouse();
                    layout.ClosePopup(false);
                    choiceHandler?.Invoke(spells[index].Key);
                })
            )).ToList());
            popup.AddSunkenBox(new Rect(48, 173, 174, 10));
            var spellMessage = popup.AddText(new Rect(49, 175, 172, 6), "", TextColor.White, TextAlign.Center, true, 2);
            popup.Closed += UntrapMouse;
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
            scrollbar.Scrolled += offset =>
            {
                spellList.ScrollTo(offset);
            };
        }

        internal void ShowMessagePopup(string text, Action closeAction = null)
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
            }, true, true, false, TextAlign.Center);
            CursorType = CursorType.Click;
            TrapMouse(popup.ContentArea);
        }

        internal void ShowTextPopup(IText text, Action<PopupTextEvent.Response> responseHandler)
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

        internal void ShowTextPopup(Map map, PopupTextEvent popupTextEvent, Action<PopupTextEvent.Response> responseHandler)
        {
            var text = ProcessText(map.Texts[(int)popupTextEvent.TextIndex]);

            if (popupTextEvent.HasImage)
            {
                // Those always use a custom layout
                Fade(() =>
                {
                    SetWindow(Window.Event);
                    layout.SetLayout(LayoutType.Event);
                    ShowMap(false);
                    layout.Reset();
                    layout.AddEventPicture(popupTextEvent.EventImageIndex);
                    layout.FillArea(new Rect(16, 138, 288, 55), GetPaletteColor(50, 28), false);

                    // Position = 18,139, max 40 chars per line and 7 lines.
                    var textArea = new Rect(18, 139, 285, 49);
                    var scrollableText = layout.AddScrollableText(textArea, text, TextColor.Gray);
                    scrollableText.Clicked += scrolledToEnd =>
                    {
                        if (scrolledToEnd)
                            CloseWindow();
                    };
                    CursorType = CursorType.Click;
                    InputEnable = false;
                });
            }
            else
            {
                ShowTextPopup(text, responseHandler);
            }
        }

        internal void ShowDecisionPopup(string text, Action<PopupTextEvent.Response> responseHandler, int minLines = 3)
        {
            layout.OpenYesNoPopup
            (
                ProcessText(text),
                () =>
                {
                    layout.ClosePopup(false);
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.Yes);
                },
                () =>
                {
                    layout.ClosePopup(false);
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.No);
                },
                () =>
                {
                    InputEnable = true;
                    Resume();
                    responseHandler?.Invoke(PopupTextEvent.Response.Close);
                }, minLines
            );
            Pause();
            InputEnable = false;
            CursorType = CursorType.Sword;
        }

        internal void ShowDecisionPopup(Map map, DecisionEvent decisionEvent, Action<PopupTextEvent.Response> responseHandler)
        {
            ShowDecisionPopup(map.Texts[(int)decisionEvent.TextIndex], responseHandler);
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

        bool RecheckActivePartyMember()
        {
            if (!CurrentPartyMember.Ailments.CanSelect() || currentBattle.GetSlotFromCharacter(CurrentPartyMember) == -1)
            {
                layout.ClearBattleFieldSlotColors();
                Pause();
                // Simple text popup
                var popup = layout.OpenTextPopup(ProcessText(DataNameProvider.SelectNewLeaderMessage), () =>
                {
                    UntrapMouse();
                    Resume();
                    ResetCursor();
                }, true, false);
                popup.CanAbort = false;
                pickingNewLeader = true;
                CursorType = CursorType.Sword;
                TrapMouse(Global.PartyMemberPortraitArea);
                // TODO: What happens if all party members are no longer selectable? E.g. all sleeping?
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

        internal void SetActivePartyMember(int index, bool updateBattlePosition = true)
        {
            var partyMember = GetPartyMember(index);

            if (partyMember != null && partyMember.Ailments.CanSelect())
            {
                if (HasPartyMemberFled(partyMember))
                    return;

                CurrentSavegame.ActivePartyMemberSlot = index;
                CurrentPartyMember = partyMember;
                layout.SetActiveCharacter(index, Enumerable.Range(0, MaxPartyMembers).Select(i => GetPartyMember(i)).ToList());

                if (currentBattle != null && updateBattlePosition && layout.Type == LayoutType.Battle)
                    BattlePlayerSwitched();

                if (pickingNewLeader)
                {
                    pickingNewLeader = false;
                    layout.ClosePopup(true, true);
                }
            }
        }

        internal void DropGold(uint amount)
        {
            layout.ClosePopup(false, true);
            CurrentInventory.Gold = (ushort)Math.Max(0, CurrentInventory.Gold - (int)amount);
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void DropFood(uint amount)
        {
            layout.ClosePopup(false, true);
            CurrentInventory.Food = (ushort)Math.Max(0, CurrentInventory.Food - (int)amount);
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void StoreGold(uint amount)
        {
            layout.ClosePopup(false, true);
            var chest = OpenStorage as Chest;
            const uint MaxGoldPerChest = 50000; // TODO
            amount = Math.Min(amount, MaxGoldPerChest - chest.Gold);
            CurrentInventory.Gold = (ushort)Math.Max(0, CurrentInventory.Gold - (int)amount);
            chest.Gold += amount;
            layout.UpdateLayoutButtons();
            UpdateCharacterInfo();
        }

        internal void StoreFood(uint amount)
        {
            layout.ClosePopup(false, true);
            var chest = OpenStorage as Chest;
            const uint MaxFoodPerChest = 5000; // TODO
            amount = Math.Min(amount, MaxFoodPerChest - chest.Food);
            CurrentInventory.Food = (ushort)Math.Max(0, CurrentInventory.Food - (int)amount);
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

            if (ItemManager.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
            {
                foreach (var slot in OpenStorage.Slots)
                {
                    if (!slot.Empty && slot.ItemIndex == itemSlot.ItemIndex)
                    {
                        // This will update itemSlot
                        slot.Add(itemSlot, (int)maxAmount);
                        return true;
                    }
                }
            }

            foreach (var slot in OpenStorage.Slots)
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

            var slots = slotIndex == null
                ? partyMember.Inventory.Slots.Where(s => s.ItemIndex == item.ItemIndex && s.Amount < 99).ToArray()
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
                lastWindow = currentWindow;
            if (currentWindow.Window == window)
                currentWindow.WindowParameters = parameters;
            else
                currentWindow = new WindowInfo { Window = window, WindowParameters = parameters };
        }

        void ResetCursor()
        {
            if (CursorType == CursorType.Click ||
                CursorType == CursorType.SmallArrow ||
                CursorType == CursorType.None)
            {
                CursorType = CursorType.Sword;
            }
            UpdateCursor(lastMousePosition, MouseButtons.None);
        }

        internal void CloseWindow()
        {
            if (!WindowActive)
                return;

            characterInfoTexts.Clear();
            characterInfoPanels.Clear();
            CurrentInventoryIndex = null;

            if (currentWindow.Window == Window.Event || currentWindow.Window == Window.Riddlemouth)
            {
                InputEnable = true;
                ResetCursor();
            }

            if (currentWindow.Window == lastWindow.Window)
                currentWindow = DefaultWindow;
            else
                currentWindow = lastWindow;

            switch (currentWindow.Window)
            {
                case Window.MapView:
                    Fade(() => ShowMap(true));
                    break;
                case Window.Inventory:
                {
                    int partyMemberIndex = (int)currentWindow.WindowParameters[0];
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, true);
                    break;
                }
                case Window.Stats:
                {
                    int partyMemberIndex = (int)currentWindow.WindowParameters[0];
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, false);
                    break;
                }
                case Window.Chest:
                {
                    var chestEvent = (ChestEvent)currentWindow.WindowParameters[0];
                    currentWindow = DefaultWindow;
                    ShowChest(chestEvent);
                    break;
                }
                case Window.Merchant:
                {
                    // TODO
                    break;
                }
                case Window.Riddlemouth:
                {
                    var riddlemouthEvent = (RiddlemouthEvent)currentWindow.WindowParameters[0];
                    var solvedEvent = currentWindow.WindowParameters[1] as Action;
                    currentWindow = DefaultWindow;
                    ShowRiddlemouth(Map, riddlemouthEvent, solvedEvent, false);
                    break;
                }
                case Window.Conversation:
                {
                    var conversationPartner = currentWindow.WindowParameters[0] as IConversationPartner;
                    var conversationEvent = currentWindow.WindowParameters[1] as Event;
                    currentWindow = DefaultWindow;
                    ShowConversation(conversationPartner, conversationEvent);
                    break;
                }
                case Window.Battle:
                {
                    var nextEvent = (Event)currentWindow.WindowParameters[0];
                    ShowBattleWindow(nextEvent);
                    break;
                }
                case Window.BattleLoot:
                {
                    // TODO
                    break;
                }
                default:
                    break;
            }
        }
    }
}
