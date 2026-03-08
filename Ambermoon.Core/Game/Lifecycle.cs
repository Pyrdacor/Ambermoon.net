using System;
using System.Collections.Generic;
using System.Linq;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;

namespace Ambermoon;

class TimedGameEvent
{
    public DateTime ExecutionTime;
    public Action? Action;
}

partial class GameCore
{
    public const uint TicksPerSecond = 60;

    bool paused = false;
    bool disableTimeEvents = false;
    // Technical game pause settings
    bool audioWasEnabled = false;
    bool musicWasPlaying = false;
    bool gameWasPaused = false;
    bool gamePaused = false;
    uint lastMapTicksReset = 0;
    // Note: These are not meant for ingame stuff but for fade effects etc that use real time.
    readonly List<TimedGameEvent> timedEvents = [];
    Func<MouseButtons, bool>? nextClickHandler = null;

    public event Action? QuitRequested;

    internal bool Ingame { get; private set; } = false;
    internal uint CurrentTicks { get; private set; } = 0;
    internal uint CurrentMapTicks { get; private set; } = 0;
    internal uint CurrentBattleTicks { get; private set; } = 0;
    internal uint CurrentNormalizedBattleTicks { get; private set; } = 0;
    internal uint CurrentPopupTicks { get; private set; } = 0;
    internal uint CurrentAnimationTicks { get; private set; } = 0;


    #region Hooks

    protected virtual void Hook_NewGameCleanup() { }
    protected abstract void Hook_NewGame();
    protected virtual Savegame Hook_GameLoaded(Savegame savegame, int slot, bool updateSlot) { return savegame; }
    protected virtual void Hook_GameSaved(int slot, string name) { }
    protected virtual void Hook_Paused() { }
    protected virtual void Hook_Resumed() { }
    protected virtual void Hook_PreUpdate(double deltaTime, out bool proceedWithUpdate) { proceedWithUpdate = true; }
    protected virtual void Hook_AfterTimedEventUpdate(double deltaTime, out bool proceedWithUpdate) { proceedWithUpdate = true; }
    protected virtual void Hook_PostUpdate(double deltaTime) { }
    protected virtual void Hook_DestroyCleanup() { }
    protected virtual void Hook_Outro() { }
    protected abstract void Hook_GameOver();

    #endregion


    #region Providers

    protected abstract int Provider_ContinueSavegameSlot();
    protected abstract int Provider_NumSavegameSlots();
    protected abstract bool Provider_HasSavegames();
    protected abstract IEnumerable<string> Provider_AdditionalSavegameNames();
    protected abstract Action<int>? Provider_ContinueGameSlotUpdater();

    #endregion


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
        Hook_PreUpdate(deltaTime, out bool proceed);

        if (!proceed)
            return;

        for (int i = timedEvents.Count - 1; i >= 0; --i)
        {
            if (DateTime.Now >= timedEvents[i].ExecutionTime)
            {
                var timedEvent = timedEvents[i];
                timedEvents.RemoveAt(i);
                timedEvent.Action?.Invoke();
            }
        }

        Hook_AfterTimedEventUpdate(deltaTime, out proceed);

        if (!proceed)
            return;

        if (Ingame)
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
                    renderMap3D!.Update(CurrentMapTicks, GameTime!);
                }
                else // 2D
                {
                    renderMap2D!.Update(CurrentMapTicks, GameTime!, monstersCanMoveImmediately, lastPlayerPosition);
                }

                monstersCanMoveImmediately = false;

                var moveTicks = CurrentTicks >= lastMoveTicksReset ? CurrentTicks - lastMoveTicksReset : (uint)((long)CurrentTicks + uint.MaxValue - lastMoveTicksReset);

                if (moveTicks >= movement.MovementTicks(is3D, Map!.UseTravelTypes, TravelType))
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
                fow2D!.Center = player2D.DisplayArea.Center;
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
                drugOverlay!.Color = new Render.Color(r | b);
                lastDrugColorChangeTicks = CurrentAnimationTicks;
            }

            if (CurrentAnimationTicks - lastDrugMouseMoveTicks >= 4)
            {
                DrugTicked?.Invoke();
                lastDrugMouseMoveTicks = CurrentAnimationTicks;
            }

            drugOverlay!.Visible = true;
        }
        else
        {
            renderView.DrugColorComponent = null;
            drugOverlay!.Visible = false;
        }
    }

    public void Start(Savegame savegame, Action? postAction = null)
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

        Ingame = true;
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

            if (Map!.Flags.HasFlag(MapFlags.Dungeon) &&
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
                var partyMember = savegame.GetPartyMember(i)!;
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
        player3D = new Player3D(this, player, MapManager, camera3D, renderMap3D);
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
            player2D!.Visible = true;

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

    internal void NewGame(bool cleanUp = true)
    {
        if (cleanUp)
        {
            ClosePopup();
            CloseWindow();
            Cleanup();
            layout.ShowPortraitArea(false);
            layout.SetLayout(LayoutType.None);
            HideWindowTitle();
            cursor.Type = CursorType.Sword;
            UpdateCursor(lastMousePosition, MouseButtons.None);
            currentUIPaletteIndex = 0;
            battleRoundActiveSprite.Visible = false;

            Hook_NewGameCleanup();
        }

        currentSong?.Stop();
        currentSong = null;

        PlayMusic(Song.HisMastersVoice);

        showMobileTouchPadHandler?.Invoke(false);

        Hook_NewGame();
    }

    public void ContinueGame()
    {
        if (SavegameManager.HasCrashSavegame())
        {
            Ingame = true;
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
            int current = Provider_ContinueSavegameSlot();

            LoadGame(current, false, true);
        }
    }

    void Exit()
    {
        QuitRequested?.Invoke();
    }

    public void Quit() => Quit(null);

    void Quit(Action? abortAction)
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

    internal void Start2D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial, Action<Map>? mapInitAction = null)
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
                map = MapManager.GetMap(map.LeftMapIndex!.Value);
                xOffset += map.Width;
                playerX += (uint)map.Width;
            }
            if (yOffset < 0)
            {
                map = MapManager.GetMap(map.UpMapIndex!.Value);
                yOffset += map.Height;
                playerY += (uint)map.Height;
            }
        }
        else
        {
            xOffset = Util.Limit(0, xOffset, map.Width - RenderMap2D.NUM_VISIBLE_TILES_X);
            yOffset = Util.Limit(0, yOffset, map.Height - RenderMap2D.NUM_VISIBLE_TILES_Y);
        }

        if (renderMap2D!.Map != map)
        {
            renderMap2D!.SetMap(map, (uint)xOffset, (uint)yOffset);
            mapInitAction?.Invoke(map);
        }
        else
        {
            renderMap2D.ScrollTo((uint)xOffset, (uint)yOffset, true);
            mapInitAction?.Invoke(map);
            renderMap2D.AddCharacters(map);
            renderMap2D.InvokeMapChange();
        }

        player2D ??= new Player2D(this, renderView.GetLayer(Layer.Characters), player!, renderMap2D,
            renderView.SpriteFactory, new Position(0, 0), MapManager);

        player2D.Visible = true;
        player2D.RecheckTopSprite();
        player2D.MoveTo(map, playerX, playerY, CurrentTicks, true, direction);

        player!.Position.X = (int)playerX;
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

    internal void Start3D(Map map, uint playerX, uint playerY, CharacterDirection direction, bool initial, Action<Map>? mapInitAction = null)
    {
        if (map.Type != MapType.Map3D)
            throw new AmbermoonException(ExceptionScope.Application, "Given map is not 3D.");

        layout.SetLayout(LayoutType.Map3D, movement.MovementTicks(true, false, TravelType.Walk));

        is3D = true;
        TravelType = TravelType.Walk;
        renderMap2D?.Destroy();
        renderMap3D!.SetMap(map, playerX, playerY, direction, CurrentPartyMember?.Race ?? Race.Human, true);
        UpdateUIPalette(true);
        mapInitAction?.Invoke(map);
        player3D!.SetPosition((int)playerX, (int)playerY, CurrentTicks, !initial);
        player3D.TurnTowards((int)direction * 90.0f);

        if (player2D != null)
            player2D.Visible = false;

        player!.Position.X = (int)playerX;
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

    private protected void Cleanup()
    {
        highlightBattleFieldSprites.ForEach(s => s?.Delete());
        highlightBattleFieldSprites.Clear();
        currentBattle?.EndBattleCleanup();
        battleRoundActiveSprite.Visible = false;
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

        Hook_DestroyCleanup();
    }

    public void Destroy()
    {
        drugOverlay?.Delete();
        ouchSprite?.Delete();
        mobileClickIndicator?.Delete();

        Util.SafeCall(UntrapMouse);
        allInputDisabled = true;
        Ingame = false;
        Util.SafeCall(() => AudioOutput?.Stop());
        Util.SafeCall(() => AudioOutput?.Reset());
        Util.SafeCall(Cleanup);
        Util.SafeCall(() => layout.Destroy());
        Util.SafeCall(() => CursorType = CursorType.None);
        Util.SafeCall(() => windowTitle?.Delete());
        TextInput.FocusChanged -= InputFocusChanged;
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

    void RenewTimedEvent(TimedGameEvent timedGameEvent, TimeSpan delay)
    {
        timedGameEvent.ExecutionTime = DateTime.Now + delay;

        if (!timedEvents.Contains(timedGameEvent))
            timedEvents.Add(timedGameEvent);
    }

    internal void AddTimedEvent(TimeSpan delay, Action? action) => AddTimedEvent(delay, action, false);

    internal void AddTimedEvent(TimeSpan delay, Action? action, bool ignoreFastBattleMode)
    {
        if (!ignoreFastBattleMode && currentBattle != null && CoreConfiguration.BattleSpeed != 0 && currentWindow.Window == Window.Battle)
            delay = TimeSpan.FromMilliseconds(Math.Max(1.0, delay.TotalMilliseconds / BattleTimeFactor));

        timedEvents.Add(new TimedGameEvent
        {
            ExecutionTime = DateTime.Now + delay,
            Action = action
        });
    }

    internal void ExecuteNextUpdateCycle(Action? action)
    {
        AddTimedEvent(TimeSpan.FromMilliseconds(0), action);
    }
}
