using Ambermoon.Data;
using Ambermoon.Render;
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
            public string LeadName => game.party.First(p => p.Alive).Name;
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

            public uint TickDivider(bool is3D) => tickDivider[is3D ? 1 : 0];
            public float MoveSpeed3D { get; }
            public float TurnSpeed3D { get; }

            public Movement(bool legacyMode)
            {
                tickDivider = new uint[] { 8u, GetTickDivider3D(legacyMode) };
                MoveSpeed3D = GetMoveSpeed3D(legacyMode);
                TurnSpeed3D = GetTurnSpeed3D(legacyMode);
            }

            static uint GetTickDivider3D(bool legacyMode) => legacyMode ? 8u : 60u;
            static float GetMoveSpeed3D(bool legacyMode) => legacyMode ? 0.25f : 0.0625f;
            static float GetTurnSpeed3D(bool legacyMode) => legacyMode ? 15.0f : 3.00f;
        }

        const uint TicksPerSecond = 60;
        readonly bool legacyMode = false;
        bool ingame = false;
        bool is3D = false;
        bool windowActive = false;
        readonly Movement movement;
        uint currentTicks = 0;
        uint lastMapTicksReset = 0;
        uint lastKeyTicksReset = 0;        
        readonly NameProvider nameProvider;
        readonly UI.Layout layout;
        readonly IMapManager mapManager;
        readonly IItemManager itemManager;
        readonly IRenderView renderView;
        readonly ISavegameManager savegameManager;
        readonly ISavegameSerializer savegameSerializer;
        Player player;
        readonly PartyMember[] party = new PartyMember[6];
        PartyMember CurrentPartyMember { get; } = null;
        PartyMember CurrentInventory { get; } = null;
        PartyMember CurrentCaster { get; } = null;
        public Map Map => !ingame ? null : is3D ? renderMap3D?.Map : renderMap2D?.Map;
        readonly bool[] keys = new bool[Enum.GetValues(typeof(Key)).Length];
        /// <summary>
        /// All words you have heard about in conversations.
        /// </summary>
        readonly List<string> dictionary = new List<string>();
        public GameVariablePool GlobalVariables { get; } = new GameVariablePool();
        readonly Dictionary<uint, GameVariablePool> mapVariables = new Dictionary<uint, GameVariablePool>();
        public GameVariablePool GetMapVariables(Map map)
        {
            if (!mapVariables.ContainsKey(map.Index))
                return mapVariables[map.Index] = new GameVariablePool();

            return mapVariables[map.Index];
        }
        Savegame currentSavegame;

        // Rendering
        readonly Cursor cursor = null;
        RenderMap2D renderMap2D = null;
        Player2D player2D = null;
        RenderMap3D renderMap3D = null;
        Player3D player3D = null;
        readonly ICamera3D camera3D = null;
        readonly IRenderText messageText = null;
        Rect mapViewArea = map2DViewArea;
        static readonly Rect map2DViewArea = new Rect(Global.Map2DViewX, Global.Map2DViewY,
            Global.Map2DViewWidth, Global.Map2DViewHeight);
        static readonly Rect map3DViewArea = new Rect(Global.Map3DViewX, Global.Map3DViewY,
            Global.Map3DViewWidth, Global.Map3DViewHeight);

        public Game(IRenderView renderView, IMapManager mapManager, IItemManager itemManager,
            ISavegameManager savegameManager, ISavegameSerializer savegameSerializer,
            Cursor cursor, bool legacyMode)
        {
            this.cursor = cursor;
            this.legacyMode = legacyMode;
            movement = new Movement(legacyMode);
            nameProvider = new NameProvider(this);
            this.renderView = renderView;
            this.mapManager = mapManager;
            this.itemManager = itemManager;
            this.savegameManager = savegameManager;
            this.savegameSerializer = savegameSerializer;
            camera3D = renderView.Camera3D;
            messageText = renderView.RenderTextFactory.Create();
            messageText.Layer = renderView.GetLayer(Layer.Text);
            layout = new UI.Layout(renderView);

            // TODO: values should come from the character select menu
            party[0] = PartyMember.Create("Thalion", 2, Gender.Male);
            CurrentPartyMember = party[0];
        }

        /// <summary>
        /// This is called when the game starts.
        /// This includes intro, main menu, etc.
        /// </summary>
        public void Run()
        {
            // TODO: For now we just start a new game.
            var initialSavegame = savegameManager.LoadInitial(renderView.GameData, savegameSerializer);
            Start(initialSavegame);
        }

        public void Update(double deltaTime)
        {
            uint add = (uint)Util.Round(TicksPerSecond * (float)deltaTime);

            if (currentTicks <= uint.MaxValue - add)
                currentTicks += add;
            else
                currentTicks = (uint)(((long)currentTicks + add) % uint.MaxValue);

            if (ingame)
            {
                if (is3D)
                {
                    // TODO
                }
                else // 2D
                {
                    var animationTicks = currentTicks >= lastMapTicksReset ? currentTicks - lastMapTicksReset : (uint)((long)currentTicks + uint.MaxValue - lastMapTicksReset);
                    renderMap2D.UpdateAnimations(animationTicks);
                }
            }

            var keyTicks = currentTicks >= lastKeyTicksReset ? currentTicks - lastKeyTicksReset : (uint)((long)currentTicks + uint.MaxValue - lastKeyTicksReset);

            if (keyTicks >= TicksPerSecond / movement.TickDivider(is3D))
            {
                Move();

                lastKeyTicksReset = currentTicks;
            }
        }

        void ResetMoveKeys()
        {
            keys[(int)Key.Up] = false;
            keys[(int)Key.Down] = false;
            keys[(int)Key.Left] = false;
            keys[(int)Key.Right] = false;
            lastKeyTicksReset = currentTicks;
        }

        // TODO: When changing map, the screen should shortly black out (fading transition)

        internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction)
        {
            if (map.Type != MapType.Map2D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 2D.");

            ResetMoveKeys();
            layout.SetLayout(UI.LayoutType.Map2D);

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

            player2D.Visible = true;
            player2D.MoveTo(map, playerX, playerY, currentTicks, true, direction);

            var mapOffset = map.MapOffset;
            player.Position.X = mapOffset.X + (int)playerX - (int)renderMap2D.ScrollX;
            player.Position.Y = mapOffset.Y + (int)playerY - (int)renderMap2D.ScrollY;
            player.Direction = direction;

            is3D = false;
            renderView.GetLayer(Layer.Map3D).Visible = false;
            renderView.GetLayer(Layer.Billboards3D).Visible = false;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = true;

            mapViewArea = map2DViewArea;
        }

        internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction)
        {
            if (map.Type != MapType.Map3D)
                throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

            ResetMoveKeys();
            layout.SetLayout(UI.LayoutType.Map3D);

            renderMap2D.Destroy();
            renderMap3D.SetMap(map, playerX, playerY, direction);
            player3D = new Player3D(this, mapManager, camera3D, renderMap3D, 0, 0);
            player3D.SetPosition((int)playerX, (int)playerY, currentTicks);
            player2D.Visible = false;
            player.Position.X = (int)playerX;
            player.Position.Y = (int)playerY;
            player.Direction = direction;

            is3D = true;
            renderView.GetLayer(Layer.Map3D).Visible = true;
            renderView.GetLayer(Layer.Billboards3D).Visible = true;
            for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                renderView.GetLayer((Layer)i).Visible = false;

            mapViewArea = map3DViewArea;
        }

        public void Start(Savegame savegame)
        {
            ingame = true;
            currentSavegame = savegame;
            player = new Player();
            var map = mapManager.GetMap(savegame.CurrentMapIndex);
            bool is3D = map.Type == MapType.Map3D;
            renderMap2D = new RenderMap2D(this, !is3D ? map : null, mapManager, renderView);
            renderMap3D = new RenderMap3D(is3D ? map : null, mapManager, renderView, 0, 0, CharacterDirection.Up);
            player2D = new Player2D(this, renderView.GetLayer(Layer.Characters), player, renderMap2D,
                renderView.SpriteFactory, renderView.GameData, new Position(0, 0), mapManager);
            player2D.Visible = !is3D;
            player3D = new Player3D(this, mapManager, camera3D, renderMap3D, 0, 0);
            player.MovementAbility = PlayerMovementAbility.Walking;
            if (is3D)
                Start3D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1, savegame.CharacterDirection);
            else
                Start2D(map, savegame.CurrentMapX - 1, savegame.CurrentMapY - 1 - (map.IsWorldMap ? 0u : 1u), savegame.CharacterDirection);

            for (int i = 0; i < 6; ++i)
            {
                if (savegame.CurrentPartyMemberIndices[i] != null)
                    layout.SetPortrait(i, savegame.GetPartyMember(i).PortraitIndex);
            }
        }

        public void LoadGame()
        {
            // TODO
        }

        public void ContinueGame()
        {
            // TODO: load latest game
        }

        public void ShowMessage(Rect bounds, string text, TextColor color, bool shadow, TextAlign textAlign = TextAlign.Left)
        {
            messageText.Text = renderView.TextProcessor.ProcessText(text, nameProvider, dictionary);
            messageText.TextColor = color;
            messageText.Shadow = shadow;
            messageText.Place(bounds, textAlign);
            messageText.Visible = true;
        }

        public void HideMessage()
        {
            messageText.Visible = false;
        }

        void Move()
        {
            if (windowActive)
                return;

            if (keys[(int)Key.Left] && !keys[(int)Key.Right])
            {
                if (!is3D)
                {
                    // diagonal movement is handled in up/down
                    if (!keys[(int)Key.Up] && !keys[(int)Key.Down])
                        player2D.Move(-1, 0, currentTicks);
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
                        player2D.Move(1, 0, currentTicks);
                }
                else
                    player3D.TurnRight(movement.TurnSpeed3D);
            }
            if (keys[(int)Key.Up] && !keys[(int)Key.Down])
            {
                if (!is3D)
                {
                    var prevDirection = player2D.Direction;
                    int x = keys[(int)Key.Left] && !keys[(int)Key.Right] ? -1 :
                        keys[(int)Key.Right] && !keys[(int)Key.Left] ? 1 : 0;

                    if (!player2D.Move(x, -1, currentTicks) && x != 0)
                    {
                        if (!player2D.Move(0, -1, currentTicks, prevDirection))
                            player2D.Move(x, 0, currentTicks, prevDirection);
                    }
                }
                else
                    player3D.MoveForward(movement.MoveSpeed3D * Global.DistancePerTile, currentTicks);
            }
            if (keys[(int)Key.Down] && !keys[(int)Key.Up])
            {
                if (!is3D)
                {
                    var prevDirection = player2D.Direction;
                    int x = keys[(int)Key.Left] && !keys[(int)Key.Right] ? -1 :
                        keys[(int)Key.Right] && !keys[(int)Key.Left] ? 1 : 0;

                    if (!player2D.Move(x, 1, currentTicks, null, false) && x != 0)
                    {
                        if (!player2D.Move(0, 1, currentTicks, prevDirection, false))
                            player2D.Move(x, 0, currentTicks, prevDirection);
                    }
                }
                else
                    player3D.MoveBackward(movement.MoveSpeed3D * Global.DistancePerTile, currentTicks);
            }
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            keys[(int)key] = true;

            Move();

            switch (key)
            {
                case Key.Escape:
                {
                    if (ingame)
                    {
                        if (is3D)
                            layout.SetLayout(UI.LayoutType.Map3D);
                        else
                            layout.SetLayout(UI.LayoutType.Map2D);
                        layout.Reset();
                        ShowMap(true);
                    }

                    break;
                }
                case Key.Num1:
                    break;
                case Key.Num2:
                    break;
                case Key.Num3:
                    break;
                case Key.Num4:
                    break;
                case Key.Num5:
                    break;
                case Key.Num6:
                    break;
                case Key.Num7:
                    // TODO
                    cursor.Type = CursorType.Eye;
                    break;
                case Key.Num8:
                    // TODO
                    cursor.Type = CursorType.Hand;
                    break;
                case Key.Num9:
                    // TODO
                    cursor.Type = CursorType.Mouth;
                    break;
            }

            lastKeyTicksReset = currentTicks;
        }

        public void OnKeyUp(Key key, KeyModifiers modifiers)
        {
            keys[(int)key] = false;
        }

        public void OnKeyChar(char keyChar)
        {

        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (ingame)
            {
                var relativePosition = renderView.ScreenToGame(position);

                if (mapViewArea.Contains(relativePosition))
                {
                    // click into the map area

                    relativePosition.Offset(-mapViewArea.Left, -mapViewArea.Top);

                    if (cursor.Type == CursorType.Eye)
                        TriggerMapEvents(MapEventTrigger.Eye, relativePosition);
                    else if (cursor.Type == CursorType.Hand)
                        TriggerMapEvents(MapEventTrigger.Hand, relativePosition);
                    else if (cursor.Type == CursorType.Mouth)
                        TriggerMapEvents(MapEventTrigger.Mouth, relativePosition);
                }
                else
                {
                    // TODO: check for other clicks
                }
            }

            cursor.Type = CursorType.Sword;
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            cursor.UpdatePosition(position);
        }

        internal PartyMember GetPartyMember(int slot) => currentSavegame.GetPartyMember(slot);
        internal Chest GetChest(uint index) => currentSavegame.Chests[(int)index];
        internal Merchant GetMerchant(uint index) => currentSavegame.Merchants[(int)index];

        /// <summary>
        /// Triggers map events with the given trigger and position.
        /// </summary>
        /// <param name="trigger">Trigger</param>
        /// <param name="position">Position inside the map view</param>
        void TriggerMapEvents(MapEventTrigger trigger, Position position)
        {
            if (is3D)
            {
                // TODO
                // renderMap3D.Map.TriggerEvents(this, player3D, trigger, x, y, mapManager, currentTicks);
            }
            else // 2D
            {
                var tilePosition = renderMap2D.PositionToTile(position);
                renderMap2D.TriggerEvents(player2D, trigger, (uint)tilePosition.X, (uint)tilePosition.Y, mapManager, currentTicks);
            }
        }

        void ShowMap(bool show)
        {
            if (is3D)
            {
                renderView.GetLayer(Layer.Map3D).Visible = show;
                renderView.GetLayer(Layer.Billboards3D).Visible = show;
            }
            else
            {
                for (int i = (int)Global.First2DLayer; i <= (int)Global.Last2DLayer; ++i)
                    renderView.GetLayer((Layer)i).Visible = show;
            }

            if (show)
                windowActive = false;
        }

        internal void ShowChest(ChestMapEvent chestMapEvent)
        {
            windowActive = true;
            ShowMap(false);
            layout.SetLayout(UI.LayoutType.Items);
            var chest = GetChest(chestMapEvent.ChestIndex);

            if (chestMapEvent.Lock != ChestMapEvent.LockFlags.Open)
            {
                layout.Set80x80Picture(Data.Enumerations.Picture80x80.ChestClosed);
            }
            else
            {
                if (chest.Empty)
                {
                    layout.Set80x80Picture(Data.Enumerations.Picture80x80.ChestOpenEmpty);
                }
                else
                {
                    layout.Set80x80Picture(Data.Enumerations.Picture80x80.ChestOpenFull);
                }
            }

            // TODO ...
        }
    }
}
