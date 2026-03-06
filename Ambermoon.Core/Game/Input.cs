using System;
using Ambermoon.Data;
using Ambermoon.Render;
using Ambermoon.UI;

namespace Ambermoon;

partial class GameCore
{
    public enum MobileIconAction
    {
        Eye,
        Hand,
        Mouth,
        Transport,
        Map,
        SpellBook,
        Camp,
        Wait,
        BattlePositions,
        Options,
    }

    internal enum MobileAction
    {
        None,
        Hand,
        Eye,
        Mouth,
        Interact
    }

    const int Mobile3DThreshold = 32;

    public delegate void DrawTouchFingerHandler(int x, int y, bool longPress, Rect clipArea, bool behindPopup);

    private protected readonly DrawTouchFingerHandler? drawTouchFingerRequest;
    private protected readonly Action<bool>? showMobileTouchPadHandler;
    readonly Action<bool, string> keyboardRequest;
    bool allInputWasDisabled = false;
    bool allInputDisabled = false;
    bool inputEnable = true;
    bool clickMoveActive = false;
    bool trappedAfterClickMoveActivation = false;
    Rect? trapMouseArea = null;
    Rect? trapMouseGameArea = null;
    Rect? preFullscreenChangeTrapMouseArea = null;
    Position? preFullscreenMousePosition = null;
    bool mouseTrappingActive = false;
    Position lastMousePosition = new();
    FloatPosition mobileAutomapScroll = new();
    Position lastMobileAutomapFingerPosition = new();
    readonly Position trappedMousePositionOffset = new();
    MobileAction currentMobileAction = MobileAction.None;
    readonly ILayerSprite mobileActionIndicator;
    bool fingerDown = false;
    bool targetMode2DActive = false;
    bool disableUntrapping = false;

    public event Action<bool, Position>? MouseTrappedChanged;
    public event Action<Position>? MousePositionChanged;

    bool Trapped => trapMouseArea != null;
    protected bool AllInputDisabled => allInputDisabled;
    internal MobileAction CurrentMobileAction
    {
        get => currentMobileAction;
        set
        {
            if (!CoreConfiguration.IsMobile || currentMobileAction == value)
                return;

            currentMobileAction = value;
            var layer = Layer.Cursor;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(layer);
            mobileActionIndicator.Visible = false;
            try
            {
                mobileActionIndicator.TextureAtlasOffset = currentMobileAction switch
                {
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
    private protected Position LastMousePosition => new(lastMousePosition);

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

    public bool HideMobileTouchpadDisableOverlay
    {
        get;
        set;
    } = false;

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

    public void OnMouseMove(Position position, MouseButtons buttons)
    {
        if (outro?.Active != true && !InputEnable && !layout.PopupActive)
            UntrapMouse();

        if (Trapped)
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

    public virtual void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
    {
        if (allInputDisabled)
            return;

        if (CoreConfiguration.IsMobile && currentWindow.Window == Window.Automap)
        {
            lastMobileAutomapFingerPosition = renderView.ScreenToGame(mousePosition);
            mobileAutomapScroll.X += xScroll * 4;
            mobileAutomapScroll.Y += yScroll * 4;
            return;
        }

        bool scrolled = false;

        if (xScroll != 0)
            scrolled = layout.ScrollX(xScroll);
        if (yScroll != 0 && layout.ScrollY(yScroll))
            scrolled = true;

        if (scrolled)
        {
            mousePosition = GetMousePosition(mousePosition);
            UpdateCursor(mousePosition, MouseButtons.None);
        }
        else if (yScroll != 0 && !WindowActive && !layout.PopupActive && !Is3D && !CoreConfiguration.IsMobile)
        {
            ScrollCursor(mousePosition, yScroll < 0);
        }
    }

    private void InputFocusChanged()
    {
        UpdateCursor();
        keyboardRequest?.Invoke(TextInput.FocusedInput != null, TextInput.FocusedInput?.Text ?? "");
    }

    void Determine2DTargetMode(Position cursorPosition)
    {
        var gamePosition = renderView.ScreenToGame(GetMousePosition(cursorPosition));
        var playerArea = player2D!.DisplayArea;

        int xDiff = gamePosition.X < playerArea.Left ? playerArea.Left - gamePosition.X : gamePosition.X - playerArea.Right;

        bool yTargetRange = gamePosition.Y < playerArea.Top
            ? playerArea.Top - gamePosition.Y <= 3 * RenderMap2D.TILE_HEIGHT / 2
            : gamePosition.Y - playerArea.Bottom <= RenderMap2D.TILE_HEIGHT;
        //int yDiff = gamePosition.Y < playerArea.Top ? playerArea.Top - gamePosition.Y : gamePosition.Y - playerArea.Bottom;

        if (xDiff <= RenderMap2D.TILE_WIDTH && yTargetRange)
            CursorType = CursorType.Target;
        else
            CursorType = CursorType.Mouth;
    }

    public void TriggerMobileIconAction(MobileIconAction mobileIconAction)
    {
        if (!CoreConfiguration.IsMobile || !InputEnable || allInputDisabled || PopupActive || WindowActive || currentWindow.Window != Window.MapView)
            return;

        switch (mobileIconAction)
        {
            case MobileIconAction.Eye:
                if (Is3D)
                    TriggerMapEvents(EventTrigger.Eye);
                else
                    CursorType = CursorType.Eye;
                break;
            case MobileIconAction.Hand:
                if (Is3D)
                    TriggerMapEvents(EventTrigger.Hand);
                else
                    CursorType = CursorType.Hand;
                break;
            case MobileIconAction.Mouth:
                if (Is3D)
                {
                    if (!TriggerMapEvents(EventTrigger.Mouth))
                        SpeakToParty();
                }
                else
                    CursorType = CursorType.Mouth;
                break;
            case MobileIconAction.Transport:
                if (!Is3D && layout.TransportEnabled)
                    ToggleTransport();
                break;
            case MobileIconAction.Map:
                if (Is3D)
                    ShowAutomap();
                break;
            case MobileIconAction.SpellBook:
                if (CanUseSpells())
                    CastSpell(false);
                break;
            case MobileIconAction.Camp:
                if (Map?.CanCamp == true)
                    OpenCamp(false);
                break;
            case MobileIconAction.Wait:
                layout.OpenWaitPopup();
                break;
            case MobileIconAction.BattlePositions:
                ShowBattlePositionWindow();
                break;
            case MobileIconAction.Options:
                showMobileTouchPadHandler?.Invoke(false);
                layout.OpenOptionMenu();
                break;
        }
    }

    internal void ShowMobileTouchPadHandler()
    {
        if (CoreConfiguration.IsMobile)
            showMobileTouchPadHandler?.Invoke(true);
    }

    private void UpdateMobileActionIndicatorPosition()
    {
        if (!CoreConfiguration.IsMobile)
            return;

        if (Is3D)
        {
            var mapViewCenter = mapViewArea.Center;
            mobileActionIndicator.X = mapViewCenter.X - mobileActionIndicator.Width / 2;
            mobileActionIndicator.Y = mapViewCenter.Y - mobileActionIndicator.Height / 2;
        }
        else
        {
            mobileActionIndicator.X = player2D!.DisplayArea.X;
            mobileActionIndicator.Y = player2D.DisplayArea.Y - mobileActionIndicator.Height;
        }
    }

    void ShowMobileClickIndicator(int x, int y)
    {
        if (CoreConfiguration.IsMobile && mobileClickIndicator != null)
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

            if (Is3D)
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

    internal void SetClickHandler(Action action)
    {
        nextClickHandler = _ => { action?.Invoke(); return true; };
    }

    public virtual void OnFingerDown(Position position)
    {
        fingerDown = true;
        lastMobileAutomapFingerPosition = renderView.ScreenToGame(position);
    }

    public virtual void OnFingerUp(Position position)
    {
        fingerDown = false;
        lastMobileAutomapFingerPosition = renderView.ScreenToGame(position);

        if (!CoreConfiguration.IsMobile)
            return;

        keys[(int)Key.W] = false;
        keys[(int)Key.A] = false;
        keys[(int)Key.S] = false;
        keys[(int)Key.D] = false;

        CurrentMobileAction = MobileAction.None;
    }

    public virtual void OnFingerMoveTo(Position position)
    {
        fingerDown = true;

        if (!CoreConfiguration.IsMobile)
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
    }

    public void OnKeyDown(Key key, KeyModifiers modifiers, bool tapped = false)
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
            Move(tapped);
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
                    if (CoreConfiguration.ShowSaveLoadMessage)
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
                if (CoreConfiguration.ShowSaveLoadMessage)
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

    public virtual void OnKeyChar(char keyChar)
    {
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
            if (char.ToLower(keyChar) == 'm' && ingame && is3D)
                ShowAutomap();
        }
    }

    public void OnLongPress(Position position)
    {
        if (!CoreConfiguration.IsMobile)
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
                TriggerMapEvents(null);
            }
            else
            {
                var tilePosition = renderMap2D!.PositionToTile(relativePosition);

                if (tilePosition != null)
                {
                    uint tileX = (uint)tilePosition.X;
                    uint tileY = (uint)tilePosition.Y;

                    var character = renderMap2D.GetCharacterFromTile(tileX, tileY);

                    if (character?.IsConversationPartner == true)
                    {
                        int xDist = Math.Abs(player2D!.Position.X - tilePosition.X);
                        int yDist = Math.Abs(player2D.Position.Y - tilePosition.Y);

                        if (xDist > 3 || yDist > 3)
                        {
                            ShowMessagePopup(GetCustomText(CustomTexts.Index.MobileTargetOutOfReach));
                            return;
                        }

                        if (character.Interact(EventTrigger.Mouth, renderMap2D[tileX, tileY].Type == Map.TileType.Bed))
                            return;
                    }

                    var @event = renderMap2D.GetEvent(tileX, tileY, CurrentSavegame!);

                    void TriggerEvent(EventTrigger trigger)
                    {
                        int range = trigger == EventTrigger.Mouth ? 3 : 2;

                        int xDist = Math.Abs(player2D!.Position.X - tilePosition.X);
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
                        if (CurrentSavegame!.IsDoorLocked(doorEvent.Index))
                        {
                            TriggerEvent(EventTrigger.Eye);
                            return;
                        }
                    }
                }
            }
        }
    }

    public virtual void OnMouseUp(Position cursorPosition, MouseButtons buttons)
    {
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

    public virtual void OnMouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers = KeyModifiers.None)
    {
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
                                CurrentSavegame!.CharacterDirection = player!.Direction = player3D!.Direction;
                            }, 65);
                        }

                        switch (CursorType)
                        {
                            case CursorType.ArrowTurnLeft:
                                PlayTurnSequence(6, () => player3D!.TurnLeft(15.0f));
                                return;
                            case CursorType.ArrowTurnRight:
                                PlayTurnSequence(6, () => player3D!.TurnRight(15.0f));
                                return;
                            case CursorType.ArrowRotateLeft:
                                PlayTurnSequence(12, () => player3D!.TurnLeft(15.0f));
                                return;
                            case CursorType.ArrowRotateRight:
                                PlayTurnSequence(12, () => player3D!.TurnRight(15.0f));
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
                    GameTime!.Tick();
                }
                else if (cursor.Type > CursorType.Sword && cursor.Type < CursorType.Wait)
                {
                    if (is3D)
                        TrapMouse(Map3DViewArea);
                    clickMoveActive = true;
                    lastMoveTicksReset = CurrentTicks;
                    HandleClickMovement();
                }
                else if (CoreConfiguration.IsMobile)
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

    public void OnMobileMove(Direction? direction)
    {
        if (!CoreConfiguration.IsMobile)
            return;

        if (CurrentWindow.Window != Window.MapView)
            return;

        keys[(int)Key.W] = false;
        keys[(int)Key.A] = false;
        keys[(int)Key.S] = false;
        keys[(int)Key.D] = false;

        switch (direction)
        {
            case Direction.Up:
                keys[(int)Key.W] = true;
                break;
            case Direction.UpLeft:
                keys[(int)Key.W] = true;
                keys[(int)Key.A] = true;
                break;
            case Direction.UpRight:
                keys[(int)Key.W] = true;
                keys[(int)Key.D] = true;
                break;
            case Direction.Down:
                keys[(int)Key.S] = true;
                break;
            case Direction.DownLeft:
                keys[(int)Key.S] = true;
                keys[(int)Key.A] = true;
                break;
            case Direction.DownRight:
                keys[(int)Key.S] = true;
                keys[(int)Key.D] = true;
                break;
            case Direction.Left:
                keys[(int)Key.A] = true;
                break;
            case Direction.Right:
                keys[(int)Key.D] = true;
                break;
            default:
                break;
        }
    }
}
