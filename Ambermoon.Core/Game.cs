using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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
            public string LeadName => game.CurrentSavegame.PartyMembers.First(p => p.Alive).Name;
            /// <inheritdoc />
            public string SelfName => game.CurrentPartyMember.Name;
            /// <inheritdoc />
            public string CastName => game.CurrentCaster?.Name;
            /// <inheritdoc />
            public string InvnName => game.CurrentInventory?.Name;
            /// <inheritdoc />
            public string SubjName => game.CurrentPartyMember?.Name; // TODO
            /// <inheritdoc />
            public string Sex1Name => game.CurrentPartyMember.Gender == Gender.Male ? "he" : "she"; // TODO
            /// <inheritdoc />
            public string Sex2Name => game.CurrentPartyMember.Gender == Gender.Male ? "his" : "her"; // TODO
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
            Weight
        }

        // TODO: cleanup members
        readonly Random random = new Random();
        internal Time GameTime { get; private set; } = null;
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
        uint lastMapTicksReset = 0;
        uint lastMoveTicksReset = 0;
        readonly TimedGameEvent ouchEvent = new TimedGameEvent();
        TravelType travelType = TravelType.Walk;
        readonly NameProvider nameProvider;
        readonly TextDictionary textDictionary;
        internal IDataNameProvider DataNameProvider { get; }
        readonly Layout layout;
        readonly Dictionary<CharacterInfo, UIText> characterInfoTexts = new Dictionary<CharacterInfo, UIText>();
        readonly IMapManager mapManager;
        readonly IItemManager itemManager;
        internal ICharacterManager CharacterManager { get; }
        readonly IRenderView renderView;
        internal ISavegameManager SavegameManager { get; }
        readonly ISavegameSerializer savegameSerializer;
        Player player;
        PartyMember CurrentPartyMember { get; set; } = null;
        internal PartyMember CurrentInventory => CurrentInventoryIndex == null ? null : GetPartyMember(CurrentInventoryIndex.Value);
        internal int? CurrentInventoryIndex { get; private set; } = null;
        PartyMember CurrentCaster { get; set; } = null;
        public Map Map => !ingame ? null : is3D ? renderMap3D?.Map : renderMap2D?.Map;
        public Position PartyPosition => !ingame || Map == null || player == null ? new Position() : Map.MapOffset + player.Position;
        readonly ILayerSprite ouchSprite;
        readonly bool[] keys = new bool[Enum.GetValues<Key>().Length];
        bool allInputDisabled = false;
        bool inputEnable = true;
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
        /// <summary>
        /// All words you have heard about in conversations.
        /// </summary>
        readonly List<string> dictionary = new List<string>(); // TODO: read from savegame?
        public GameVariablePool GlobalVariables { get; } = new GameVariablePool(); // TODO: read from savegame?
        readonly Dictionary<uint, GameVariablePool> mapVariables = new Dictionary<uint, GameVariablePool>(); // TODO: read from savegame?
        public GameVariablePool GetMapVariables(Map map)
        {
            if (!mapVariables.ContainsKey(map.Index))
                return mapVariables[map.Index] = new GameVariablePool();

            return mapVariables[map.Index];
        }
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
            IDataNameProvider dataNameProvider, TextDictionary textDictionary, Cursor cursor, bool legacyMode)
        {
            this.cursor = cursor;
            this.legacyMode = legacyMode;
            movement = new Movement(legacyMode);
            nameProvider = new NameProvider(this);
            this.renderView = renderView;
            this.mapManager = mapManager;
            this.itemManager = itemManager;
            CharacterManager = characterManager;
            SavegameManager = savegameManager;
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
            ouchSprite = renderView.SpriteFactory.Create(32, 23, false, true) as ILayerSprite;
            ouchSprite.Layer = renderView.GetLayer(Layer.UI);
            ouchSprite.PaletteIndex = 0;
            ouchSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI).GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Ouch));
            ouchSprite.Visible = false;
            ouchEvent.Action = () => ouchSprite.Visible = false;
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

        public void Update(double deltaTime)
        {
            GameTime?.Update();

            for (int i = timedEvents.Count - 1; i >= 0; --i)
            {
                if (DateTime.Now >= timedEvents[i].ExecutionTime)
                {
                    timedEvents[i].Action?.Invoke();
                    timedEvents.RemoveAt(i);
                }
            }

            uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

            if (CurrentTicks <= uint.MaxValue - add)
                CurrentTicks += add;
            else
                CurrentTicks = (uint)(((long)CurrentTicks + add) % uint.MaxValue);

            if (ingame)
            {
                var animationTicks = CurrentTicks >= lastMapTicksReset ? CurrentTicks - lastMapTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMapTicksReset);

                if (is3D)
                {
                    renderMap3D.Update(animationTicks, GameTime);
                }
                else // 2D
                {
                    renderMap2D.Update(animationTicks, GameTime);
                }
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

        internal void TrapMouse(Rect area)
        {
            trapMouseArea = renderView.GameToScreen(area);
            trappedMousePositionOffset.X = trapMouseArea.X - lastMousePosition.X;
            trappedMousePositionOffset.Y = trapMouseArea.Y - lastMousePosition.Y;
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

            ResetMoveKeys();
            layout.SetLayout(LayoutType.Map2D,  movement.MovementTicks(false, Map?.IsWorldMap == true, TravelType.Walk));
            is3D = false;

            if (renderMap2D.Map != map)
            {
                renderMap2D.SetMap
                (
                    map,
                    (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                    (uint)Util.Limit(0, (int)playerY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y)
                );
            }
            else
            {
                renderMap2D.ScrollTo
                (
                    (uint)Util.Limit(0, (int)playerX - RenderMap2D.NUM_VISIBLE_TILES_X / 2, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X),
                    (uint)Util.Limit(0, (int)playerY - RenderMap2D.NUM_VISIBLE_TILES_Y / 2, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y),
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

            ResetMoveKeys();
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
            dictionary.Clear();
            GlobalVariables.Clear();
            mapVariables.Clear();

            for (int i = 0; i < keys.Length; ++i)
                keys[i] = false;
            leftMouseDown = false;
            clickMoveActive = false;
            UntrapMouse();
            InputEnable = false;
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

        void RemovePartyMember(int slot)
        {
            var partyMember = GetPartyMember(slot);

            if (partyMember != null)
                partyMember.Died -= PartyMemberDied;

            layout.SetCharacter(slot, null);
        }

        int? SlotFromPartyMember(PartyMember partyMember)
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
            GameTime = new Time(savegame);

            for (int i = 0; i < MaxPartyMembers; ++i)
            {
                if (savegame.CurrentPartyMemberIndices[i] != 0)
                {
                    var partyMember = savegame.GetPartyMember(i);
                    AddPartyMember(i, partyMember);
                }
                else
                {
                    RemovePartyMember(i);
                }
            }
            CurrentPartyMember = GetPartyMember(CurrentSavegame.ActivePartyMemberSlot);
            SetActivePartyMember(CurrentSavegame.ActivePartyMemberSlot);

            player = new Player();
            var map = mapManager.GetMap(savegame.CurrentMapIndex);
            bool is3D = map.Type == MapType.Map3D;
            renderMap2D = new RenderMap2D(this, null, mapManager, renderView);
            renderMap3D = new RenderMap3D(null, mapManager, renderView, 0, 0, CharacterDirection.Up);
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

            // This is the word "Hello" which is already present on game start.
            dictionary.Add(textDictionary.Entries[0]);

            InputEnable = true;

            // Trigger events after game load
            TriggerMapEvents(MapEventTrigger.Move, (uint)player.Position.X,
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
            RunSavegameTileChangeEvents(map.Index);
        }

        void RenderMap2D_MapChanged(Map[] maps)
        {
            foreach (var map in maps)
                RunSavegameTileChangeEvents(map.Index);
        }

        public void LoadGame(int slot)
        {
            var savegame = SavegameManager.Load(renderView.GameData, savegameSerializer, slot);
            Start(savegame);
        }

        public void ContinueGame()
        {
            LoadGame(0);
        }

        public IText ProcessText(string text)
        {
            return renderView.TextProcessor.ProcessText(text, nameProvider, dictionary);
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

        void HandleClickMovement()
        {
            if (WindowActive || !InputEnable || !clickMoveActive || allInputDisabled)
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
                        player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
                        break;
                    case CursorType.ArrowBackward:
                        player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
                        break;
                    case CursorType.ArrowStrafeLeft:
                        player3D.MoveLeft(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
                        break;
                    case CursorType.ArrowStrafeRight:
                        player3D.MoveRight(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
                        break;
                    case CursorType.ArrowTurnLeft:
                        player3D.TurnLeft(movement.TurnSpeed3D * 0.7f);
                        if (!fromNumpadButton)
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerTile * 0.75f, CurrentTicks);
                        break;
                    case CursorType.ArrowTurnRight:
                        player3D.TurnRight(movement.TurnSpeed3D * 0.7f);
                        if (!fromNumpadButton)
                            player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerTile * 0.75f, CurrentTicks);
                        break;
                    case CursorType.ArrowRotateLeft:
                        if (fromNumpadButton)
                        {
                            PlayTimedSequence(12, () => player3D.TurnLeft(15.0f), 75);
                        }
                        else
                        {
                            player3D.TurnLeft(movement.TurnSpeed3D * 0.7f);
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerTile * 0.75f, CurrentTicks);
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
                            player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerTile * 0.75f, CurrentTicks);
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
            if (WindowActive || !InputEnable || allInputDisabled)
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
                    player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
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
                    player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerTile, CurrentTicks);
            }
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            if (allInputDisabled)
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
                            CloseWindow();
                    }

                    break;
                }
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                    if (!layout.PopupActive)
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
                    if (layout.PopupDisableButtons)
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

            if (!InputEnable)
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

            if (layout.KeyChar(keyChar))
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

            var position = GetMousePosition(cursorPosition);

            if (buttons.HasFlag(MouseButtons.Right))
            {
                layout.RightMouseUp(renderView.ScreenToGame(position), out CursorType? cursorType, CurrentTicks);

                if (cursorType != null)
                    CursorType = cursorType.Value;
            }

            if (buttons.HasFlag(MouseButtons.Left))
            {
                leftMouseDown = false;
                clickMoveActive = false;

                layout.LeftMouseUp(renderView.ScreenToGame(position), out CursorType? cursorType, CurrentTicks);

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

            position = GetMousePosition(position);

            if (buttons.HasFlag(MouseButtons.Left))
                leftMouseDown = true;

            if (ingame)
            {
                var relativePosition = renderView.ScreenToGame(position);

                if (!WindowActive && InputEnable && mapViewArea.Contains(relativePosition))
                {
                    // click into the map area
                    if (buttons == MouseButtons.Right)
                        CursorType = CursorType.Sword;
                    if (!buttons.HasFlag(MouseButtons.Left))
                        return;

                    relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);

                    if (cursor.Type == CursorType.Eye)
                        TriggerMapEvents(MapEventTrigger.Eye, relativePosition);
                    else if (cursor.Type == CursorType.Hand)
                        TriggerMapEvents(MapEventTrigger.Hand, relativePosition);
                    else if (cursor.Type == CursorType.Mouth)
                        TriggerMapEvents(MapEventTrigger.Mouth, relativePosition);
                    else if (cursor.Type > CursorType.Sword && cursor.Type < CursorType.Wait)
                    {
                        clickMoveActive = true;
                        HandleClickMovement();
                    }

                    if (cursor.Type > CursorType.Wait)
                        CursorType = CursorType.Sword;
                    return;
                }
                else
                {
                    var cursorType = CursorType.Sword;
                    layout.Click(relativePosition, buttons, ref cursorType, CurrentTicks);
                    CursorType = cursorType;

                    if (InputEnable)
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
                    else if (layout.Type == LayoutType.Event)
                        CursorType = CursorType.Click;
                    else
                        CursorType = CursorType.Sword;

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
            if (!InputEnable)
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

        internal PartyMember GetPartyMember(int slot) => CurrentSavegame.GetPartyMember(slot);
        internal Chest GetChest(uint index) => CurrentSavegame.Chests[(int)index];
        internal Merchant GetMerchant(uint index) => CurrentSavegame.Merchants[(int)index];

        /// <summary>
        /// Triggers map events with the given trigger and position.
        /// </summary>
        /// <param name="trigger">Trigger</param>
        /// <param name="position">Position inside the map view</param>
        bool TriggerMapEvents(MapEventTrigger trigger, Position position)
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

        bool TriggerMapEvents(MapEventTrigger trigger, uint x, uint y)
        {
            if (is3D)
            {
                return renderMap3D.Map.TriggerEvents(this, player3D, trigger, x, y, mapManager,
                    CurrentTicks, CurrentSavegame);
            }
            else // 2D
            {
                return renderMap2D.TriggerEvents(player2D, trigger, x, y, mapManager,
                    CurrentTicks, CurrentSavegame);
            }
        }

        internal void TriggerMapEvents(MapEventTrigger trigger)
        {
            bool consumed = TriggerMapEvents(trigger, (uint)player.Position.X, (uint)player.Position.Y);

            if (is3D)
            {
                if (consumed)
                    return;

                // In 3D we might trigger adjacent tile events.
                if (trigger != MapEventTrigger.Move)
                {
                    camera3D.GetForwardPosition(Global.DistancePerTile, out float x, out float z, false, false);
                    var position = Geometry.Geometry.CameraToBlockPosition(Map, x, z);

                    if (position != player.Position &&
                        position.X >= 0 && position.X < Map.Width &&
                        position.Y >= 0 && position.Y < Map.Height)
                    {
                        TriggerMapEvents(trigger, (uint)position.X, (uint)position.Y);
                    }
                }
            }
        }

        void ShowMap(bool show)
        {
            if (show)
            {
                layout.CancelDrag();
                ResetCursor();
                OpenStorage = null;
                string mapName = Map.IsWorldMap
                    ? DataNameProvider.GetWorldName(Map.World)
                    : Map.Name;
                windowTitle.Text = renderView.TextProcessor.CreateText(mapName);
                windowTitle.TextColor = TextColor.Gray;
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
            }
        }

        internal void OpenPartyMember(int slot, bool inventory)
        {
            if (CurrentSavegame.CurrentPartyMemberIndices[slot] == 0)
                return;

            void OpenInventory()
            {
                CurrentInventoryIndex = slot;
                var partyMember = GetPartyMember(slot);

                layout.Reset();
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
                var inventoryGrid = ItemGrid.CreateInventory(layout, slot, renderView, itemManager,
                    inventorySlotPositions, partyMember.Inventory.Slots.ToList());
                layout.AddItemGrid(inventoryGrid);
                for (int i = 0; i < partyMember.Inventory.Slots.Length; ++i)
                {
                    if (!partyMember.Inventory.Slots[i].Empty)
                        inventoryGrid.SetItem(i, partyMember.Inventory.Slots[i]);
                }
                var equipmentGrid = ItemGrid.CreateEquipment(layout, slot, renderView, itemManager,
                    equipmentSlotPositions, partyMember.Equipment.Slots.Values.ToList());
                layout.AddItemGrid(equipmentGrid);
                foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>().Skip(1))
                {
                    if (!partyMember.Equipment.Slots[equipmentSlot].Empty)
                        equipmentGrid.SetItem((int)equipmentSlot - 1, partyMember.Equipment.Slots[equipmentSlot]);
                }
                equipmentGrid.Dropping += (int slotIndex, Item item) =>
                {
                    var equipmentSlot = (EquipmentSlot)(slotIndex + 1);

                    if (item.Type == ItemType.Ring)
                    {
                        if (equipmentSlot == EquipmentSlot.RightFinger ||
                            equipmentSlot == EquipmentSlot.LeftFinger)
                        {
                            // place on first free finger starting at right one
                            int rightFingerSlot = (int)(EquipmentSlot.RightFinger - 1);

                            if (equipmentGrid.GetItem(rightFingerSlot) == null)
                                return rightFingerSlot;
                            else
                                return rightFingerSlot + 2; // left finger
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if (item.Type == ItemType.Amulet ||
                        item.Type == ItemType.Brooch)
                    {
                        if (equipmentSlot == EquipmentSlot.Neck ||
                            equipmentSlot == EquipmentSlot.Chest)
                        {
                            return (int)item.Type.ToEquipmentSlot() - 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if (item.Type == ItemType.CloseRangeWeapon ||
                        item.Type == ItemType.LongRangeWeapon)
                    {
                        if (equipmentSlot == EquipmentSlot.RightHand ||
                            equipmentSlot == EquipmentSlot.LeftHand)
                        {
                            return (int)item.Type.ToEquipmentSlot() - 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }

                    var itemEquipmentSlot = item.Type.ToEquipmentSlot();

                    if (equipmentSlot == itemEquipmentSlot)
                        return slotIndex;

                    return -1;
                };
                void RemoveEquipment(int slotIndex, ItemSlot itemSlot, int amount)
                {
                    var item = itemManager.GetItem(itemSlot.ItemIndex);
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
                    var item = itemManager.GetItem(itemSlot.ItemIndex);
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
                    InventoryItemRemoved(itemManager.GetItem(itemSlot.ItemIndex), amount);
                    UpdateCharacterInfo();
                }
                void AddInventoryItem(int slotIndex, ItemSlot itemSlot)
                {
                    InventoryItemAdded(itemManager.GetItem(itemSlot.ItemIndex), itemSlot.Amount);
                    UpdateCharacterInfo();
                }
                equipmentGrid.ItemExchanged += (int slotIndex, ItemSlot draggedItem, int draggedAmount, ItemSlot droppedItem) =>
                {
                    RemoveEquipment(slotIndex, draggedItem, draggedAmount);
                    AddEquipment(slotIndex, droppedItem);
                };
                equipmentGrid.ItemDragged += (int slotIndex, ItemSlot itemSlot, int amount) =>
                {
                    RemoveEquipment(slotIndex, itemSlot, amount);
                    partyMember.Equipment.Slots[(EquipmentSlot)(slotIndex + 1)].Remove(amount);
                };
                equipmentGrid.ItemDropped += (int slotIndex, ItemSlot itemSlot) =>
                {
                    AddEquipment(slotIndex, itemSlot);
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
                DisplayCharacterInfo(partyMember);
                // Weight display
                var weightArea = new Rect(27, 152, 68, 15);
                layout.AddPanel(weightArea, 2);
                layout.AddText(weightArea.CreateModified(0, 1, 0, 0), DataNameProvider.CharacterInfoWeightHeaderString,
                    TextColor.White, TextAlign.Center, 5);
                characterInfoTexts.Add(CharacterInfo.Weight, layout.AddText(weightArea.CreateModified(0, 8, 0, 0),
                    string.Format(DataNameProvider.CharacterInfoWeightString, Util.Round(partyMember.TotalWeight / 1000.0f),
                    partyMember.Attributes[Data.Attribute.Strength].TotalCurrentValue), TextColor.White, TextAlign.Center, 5));
                #endregion
            }

            void OpenCharacterStats()
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Stats, slot);
                layout.SetLayout(LayoutType.Stats);
                layout.FillArea(new Rect(16, 49, 176, 145), Color.LightGray, false);

                windowTitle.Visible = false;

                CurrentInventoryIndex = slot;
                var partyMember = GetPartyMember(slot);
                int index;

                #region Character info
                DisplayCharacterInfo(partyMember);
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
                if (!partyMember.Alive)
                {
                    // When dead, only show the dead condition.
                    layout.AddSprite(new Rect(96, 124, 16, 16), Graphics.GetAilmentGraphicIndex(Ailment.DeadCorpse), 49,
                        2, DataNameProvider.GetAilmentName(Ailment.DeadCorpse), TextColor.Yellow);
                }
                else
                {
                    foreach (var ailment in Enum.GetValues<Ailment>().Skip(1)) // skip Ailment.None
                    {
                        if (!partyMember.Ailments.HasFlag(ailment))
                            continue;

                        int column = index % ailmentsPerRow;
                        int row = index / ailmentsPerRow;
                        ++index;

                        int x = 96 + column * 16;
                        int y = 124 + row * 17;
                        layout.AddSprite(new Rect(x, y, 16, 16), Graphics.GetAilmentGraphicIndex(ailment), 49,
                            2, DataNameProvider.GetAilmentName(ailment), TextColor.Yellow);
                    }
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

        void DisplayCharacterInfo(PartyMember partyMember)
        {
            characterInfoTexts.Clear();
            layout.FillArea(new Rect(208, 49, 96, 80), Color.LightGray, false);
            layout.AddSprite(new Rect(208, 49, 32, 34), Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground, 51, 1);
            layout.AddSprite(new Rect(208, 49, 32, 34), Graphics.PortraitOffset + partyMember.PortraitIndex - 1, 49, 2);
            layout.AddText(new Rect(242, 49, 62, 7), DataNameProvider.GetRaceName(partyMember.Race));
            layout.AddText(new Rect(242, 56, 62, 7), DataNameProvider.GetGenderName(partyMember.Gender));
            characterInfoTexts.Add(CharacterInfo.Age, layout.AddText(new Rect(242, 63, 62, 7),
                string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                partyMember.Attributes[Data.Attribute.Age].CurrentValue)));
            characterInfoTexts.Add(CharacterInfo.Level, layout.AddText(new Rect(242, 70, 62, 7),
                $"{DataNameProvider.GetClassName(partyMember.Class)} {partyMember.Level}"));
            characterInfoTexts.Add(CharacterInfo.EP, layout.AddText(new Rect(242, 77, 62, 7),
                string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                partyMember.ExperiencePoints)));
            layout.AddText(new Rect(208, 84, 96, 7), partyMember.Name, TextColor.Yellow, TextAlign.Center);
            characterInfoTexts.Add(CharacterInfo.LP, layout.AddText(new Rect(208, 92, 96, 7),
                string.Format(DataNameProvider.CharacterInfoHitPointsString,
                partyMember.HitPoints.CurrentValue, partyMember.HitPoints.MaxValue),
                TextColor.White, TextAlign.Center));
            characterInfoTexts.Add(CharacterInfo.SP, layout.AddText(new Rect(208, 99, 96, 7),
                string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                partyMember.SpellPoints.CurrentValue, partyMember.SpellPoints.MaxValue),
                TextColor.White, TextAlign.Center));
            characterInfoTexts.Add(CharacterInfo.SLPAndTP, layout.AddText(new Rect(208, 106, 96, 7),
                string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, partyMember.SpellLearningPoints) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, partyMember.TrainingPoints), TextColor.White, TextAlign.Center));
            characterInfoTexts.Add(CharacterInfo.GoldAndFood, layout.AddText(new Rect(208, 113, 96, 7),
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, partyMember.Gold, partyMember.Food),
                TextColor.White, TextAlign.Center));
            layout.AddSprite(new Rect(214, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Attack), 0);
            characterInfoTexts.Add(CharacterInfo.Attack, layout.AddText(new Rect(220, 122, 30, 7),
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', partyMember.Attack < 0 ? '-' : '+'), Math.Abs(partyMember.Attack)),
                TextColor.White, TextAlign.Left));
            layout.AddSprite(new Rect(261, 120, 16, 9), Graphics.GetUIGraphicIndex(UIGraphic.Defense), 0);
            characterInfoTexts.Add(CharacterInfo.Defense, layout.AddText(new Rect(268, 122, 30, 7),
                string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', partyMember.Defense < 0 ? '-' : '+'), Math.Abs(partyMember.Defense)),
                TextColor.White, TextAlign.Left));
        }

        internal void UpdateCharacterInfo()
        {
            if ((currentWindow.Window != Window.Inventory &&
                currentWindow.Window != Window.Stats) ||
                CurrentInventoryIndex == null)
                return;

            void UpdateText(CharacterInfo characterInfo, string text)
            {
                characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text));
            }

            var partyMember = CurrentInventory;

            UpdateText(CharacterInfo.Age, string.Format(DataNameProvider.CharacterInfoAgeString.Replace("000", "0"),
                partyMember.Attributes[Data.Attribute.Age].CurrentValue));
            UpdateText(CharacterInfo.Level, $"{DataNameProvider.GetClassName(partyMember.Class)} {partyMember.Level}");
            UpdateText(CharacterInfo.EP, string.Format(DataNameProvider.CharacterInfoExperiencePointsString.Replace("0000000000", "0"),
                partyMember.ExperiencePoints));
            UpdateText(CharacterInfo.LP, string.Format(DataNameProvider.CharacterInfoHitPointsString,
                partyMember.HitPoints.CurrentValue, partyMember.HitPoints.MaxValue));
            UpdateText(CharacterInfo.SP, string.Format(DataNameProvider.CharacterInfoSpellPointsString,
                partyMember.SpellPoints.CurrentValue, partyMember.SpellPoints.MaxValue));
            UpdateText(CharacterInfo.SLPAndTP,
                string.Format(DataNameProvider.CharacterInfoSpellLearningPointsString, partyMember.SpellLearningPoints) + " " +
                string.Format(DataNameProvider.CharacterInfoTrainingPointsString, partyMember.TrainingPoints));
            UpdateText(CharacterInfo.GoldAndFood,
                string.Format(DataNameProvider.CharacterInfoGoldAndFoodString, partyMember.Gold, partyMember.Food));
            UpdateText(CharacterInfo.Attack,
                string.Format(DataNameProvider.CharacterInfoDamageString.Replace(' ', partyMember.Attack < 0 ? '-' : '+'), Math.Abs(partyMember.Attack)));
            UpdateText(CharacterInfo.Defense,
                string.Format(DataNameProvider.CharacterInfoDefenseString.Replace(' ', partyMember.Defense < 0 ? '-' : '+'), Math.Abs(partyMember.Defense)));
            if (characterInfoTexts.ContainsKey(CharacterInfo.Weight))
            {
                UpdateText(CharacterInfo.Weight, string.Format(DataNameProvider.CharacterInfoWeightString,
                    Util.Round(partyMember.TotalWeight / 1000.0f), partyMember.Attributes[Data.Attribute.Strength].TotalCurrentValue));
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
            InventoryItemAdded(itemManager.GetItem(itemIndex), amount);
        }

        void InventoryItemRemoved(Item item, int amount)
        {
            var partyMember = CurrentInventory;

            partyMember.TotalWeight -= (uint)amount * item.Weight;
            // TODO ...
        }

        internal void InventoryItemRemoved(uint itemIndex, int amount)
        {
            InventoryItemRemoved(itemManager.GetItem(itemIndex), amount);
        }

        void EquipmentAdded(Item item, int amount, PartyMember partyMember = null)
        {
            partyMember ??= CurrentInventory;

            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            partyMember.Attack = (short)(partyMember.Attack + item.Damage);
            partyMember.Defense = (short)(partyMember.Defense + item.Defense);
            partyMember.TotalWeight += (uint)amount * item.Weight;
            // TODO ...
        }

        internal void EquipmentAdded(uint itemIndex, int amount, PartyMember partyMember)
        {
            EquipmentAdded(itemManager.GetItem(itemIndex), amount, partyMember);
        }

        void EquipmentRemoved(Item item, int amount)
        {
            var partyMember = CurrentInventory;

            // Note: amount is only used for ammunition. The weight is
            // influenced by the amount but not the damage/defense etc.
            partyMember.Attack = (short)(partyMember.Attack - item.Damage);
            partyMember.Defense = (short)(partyMember.Defense - item.Defense);
            partyMember.TotalWeight -= (uint)amount * item.Weight;
            // TODO ...
        }

        internal void EquipmentRemoved(uint itemIndex, int amount)
        {
            EquipmentRemoved(itemManager.GetItem(itemIndex), amount);
        }

        void RenewTimedEvent(TimedGameEvent timedGameEvent, TimeSpan delay)
        {
            timedGameEvent.ExecutionTime = DateTime.Now + delay;

            if (!timedEvents.Contains(timedGameEvent))
                timedEvents.Add(timedGameEvent);
        }

        void AddTimedEvent(TimeSpan delay, Action action)
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

                ShowMap(true);

                // The position (x, y) is 1-based in the data so we subtract 1.
                // Moreover the players position is 1 tile below its drawing position
                // in non-world 2D so subtract another 1 from y.
                player.MoveTo(newMap, mapChangeEvent.X - 1,
                    mapChangeEvent.Y - (newMap.Type == MapType.Map2D && !newMap.IsWorldMap ? 2u : 1u),
                    CurrentTicks, true, mapChangeEvent.Direction);
                this.player.Position.X = player.Position.X;
                this.player.Position.Y = player.Position.Y;

                if (!mapTypeChanged)
                {
                    // Trigger events after map transition
                    TriggerMapEvents(MapEventTrigger.Move, (uint)player.Position.X,
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

                Map.TriggerEvents(this, player2D, MapEventTrigger.Move, x, y, mapManager,
                    CurrentTicks, CurrentSavegame);
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

        internal void ResetStorageItem(int slotIndex, ItemSlot item)
        {
            if (OpenStorage == null)
                throw new AmbermoonException(ExceptionScope.Application, "Reset storage item while no storage is open.");

            OpenStorage.ResetItem(slotIndex, item);
        }

        internal void ShowChest(ChestMapEvent chestMapEvent)
        {
            Fade(() =>
            {
                layout.Reset();
                ShowMap(false);
                SetWindow(Window.Chest, chestMapEvent);
                layout.SetLayout(LayoutType.Items);
                var chest = GetChest(chestMapEvent.ChestIndex);
                var itemSlotPositions = Enumerable.Range(1, 6).Select(index => new Position(index * 22, 139)).ToList();
                itemSlotPositions.AddRange(Enumerable.Range(1, 6).Select(index => new Position(index * 22, 168)));
                var itemGrid = ItemGrid.Create(layout, renderView, itemManager, itemSlotPositions, chest.Slots.ToList(),
                    !chestMapEvent.RemoveWhenEmpty, 12, 6, 24, new Rect(7 * 22, 139, 6, 53), new Size(6, 27), ScrollbarType.SmallVertical);
                layout.AddItemGrid(itemGrid);

                if (!chestMapEvent.RemoveWhenEmpty)
                    OpenStorage = chest;

                if (CurrentSavegame.IsChestLocked(chestMapEvent.ChestIndex))
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

                    // TODO: gold and food
                }
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

        void OpenDictionary(Action<string> choiceHandler)
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
            popup.AddDictionaryListBox(dictionary.Select(entry => new KeyValuePair<string, Action<int, string>>
            (
                entry, (int _, string text) =>
                {
                    layout.ClosePopup(false);
                    choiceHandler?.Invoke(text);
                }
            )).ToList());
            popup.Closed += UntrapMouse;
        }

        internal void ShowTextPopup(IText text, Action<PopupTextEvent.Response> responseHandler)
        {
            InputEnable = false;
            // Simple text popup
            layout.OpenTextPopup(text, () =>
            {
                InputEnable = true;
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

        internal void ShowDecisionPopup(Map map, DecisionEvent decisionEvent, Action<PopupTextEvent.Response> responseHandler)
        {
            var text = ProcessText(map.Texts[(int)decisionEvent.TextIndex]);
            layout.OpenYesNoPopup
            (
                text,
                () =>
                {
                    layout.ClosePopup(false);
                    InputEnable = true;
                    responseHandler?.Invoke(PopupTextEvent.Response.Yes);
                },
                () =>
                {
                    layout.ClosePopup(false);
                    InputEnable = true;
                    responseHandler?.Invoke(PopupTextEvent.Response.No);
                },
                () =>
                {
                    InputEnable = true;
                    responseHandler?.Invoke(PopupTextEvent.Response.Close);
                }
            );
            InputEnable = false;
            CursorType = CursorType.Sword;
        }

        internal void SetActivePartyMember(int index)
        {
            var partyMember = GetPartyMember(index);

            if (partyMember != null)
            {
                CurrentSavegame.ActivePartyMemberSlot = index;
                CurrentPartyMember = partyMember;
                layout.SetActiveCharacter(index, CurrentSavegame.PartyMembers);
            }
        }

        internal void DropGold(uint amount)
        {
            CurrentInventory.Gold = (ushort)Math.Max(0, CurrentInventory.Gold - (int)amount);
            UpdateCharacterInfo();
        }

        internal void DropFood(uint amount)
        {
            CurrentInventory.Food = (ushort)Math.Max(0, CurrentInventory.Food - (int)amount);
            UpdateCharacterInfo();
        }

        internal void StoreGold(uint amount)
        {
            const uint MaxGoldPerChest = 50000; // TODO
            amount = Math.Min(amount, MaxGoldPerChest - OpenStorage.Gold);
            CurrentInventory.Gold = (ushort)Math.Max(0, CurrentInventory.Gold - (int)amount);
            OpenStorage.Gold += amount;
            UpdateCharacterInfo();
        }

        internal void StoreFood(uint amount)
        {
            const uint MaxFoodPerChest = 5000; // TODO
            amount = Math.Min(amount, MaxFoodPerChest - OpenStorage.Food);
            CurrentInventory.Food = (ushort)Math.Max(0, CurrentInventory.Food - (int)amount);
            OpenStorage.Food += amount;
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

            if (itemManager.GetItem(itemSlot.ItemIndex).Flags.HasFlag(ItemFlags.Stackable))
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

            if (partyMember == null)
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

                InventoryItemAdded(itemManager.GetItem(emptySlot.ItemIndex), added, partyMember);

                return remaining;
            }

            var itemToAdd = itemManager.GetItem(item.ItemIndex);

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

        void SetWindow(Window window, object param = null, Action ev = null)
        {
            if ((window != Window.Inventory && window != Window.Stats) ||
                (currentWindow.Window != Window.Inventory && currentWindow.Window != Window.Stats))
                lastWindow = currentWindow;
            currentWindow = new WindowInfo { Window = window, WindowParameter = param, WindowEvent = ev };
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
                    int partyMemberIndex = (int)currentWindow.WindowParameter;
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, true);
                    break;
                }
                case Window.Stats:
                {
                    int partyMemberIndex = (int)currentWindow.WindowParameter;
                    currentWindow = DefaultWindow;
                    OpenPartyMember(partyMemberIndex, false);
                    break;
                }
                case Window.Chest:
                {
                    var chestEvent = (ChestMapEvent)currentWindow.WindowParameter;
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
                    var riddlemouthEvent = (RiddlemouthEvent)currentWindow.WindowParameter;
                    var solvedEvent = currentWindow.WindowEvent;
                    currentWindow = DefaultWindow;
                    ShowRiddlemouth(Map, riddlemouthEvent, solvedEvent, false);
                    break;
                }
                default:
                    break;
            }
        }
    }
}
