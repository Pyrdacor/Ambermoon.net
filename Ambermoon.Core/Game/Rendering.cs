using System;
using System.Collections.Generic;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using Ambermoon.UI;
using static Ambermoon.UI.BuiltinTooltips;
using Color = Ambermoon.Render.Color;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon;

partial class GameCore
{
    internal const int FadeTime = 1000;
    internal const ushort MaxBaseLine = 0x4000;
    // Note: This is half the max base line which is used for the player in complete
    // darkness. Big gaps are needed as the z buffer precision is lower with higher distance.
    const ushort FowBaseLine = 0x2000;

    private protected readonly IGameRenderView renderView;

    public bool Fading { get; private set; } = false;


    #region Misc rendering

    static readonly float[] ShakeOffsetFactors = new float[]
    {
        -0.5f, 0.0f, 1.0f, 0.5f, -1.0f, 0.0f, 0.5f
    };

    bool blinkingHighlight = false;
    readonly TimedGameEvent ouchEvent = new();
    readonly TimedGameEvent hurtPlayerEvent = new();
    readonly ILayerSprite ouchSprite;
    readonly ILayerSprite[] hurtPlayerSprites = new ILayerSprite[MaxPartyMembers]; // splash
    readonly IRenderText[] hurtPlayerDamageTexts = new IRenderText[MaxPartyMembers];
    IColoredRect? drugOverlay = null;
    uint lastDrugColorChangeTicks = 0;
    uint lastDrugMouseMoveTicks = 0;
    public Action? DrugTicked;
    ILayerSprite? mobileClickIndicator = null;

    /// <summary>
    /// This is used for screen shaking.
    /// Position is in percentage of the resolution.
    /// </summary>
    public FloatPosition? ViewportOffset { get; private set; } = null;

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

    internal void ShowDamageSplash(PartyMember partyMember, Func<PartyMember, uint> damageProvider, Action finished)
    {
        int slot = SlotFromPartyMember(partyMember)!.Value;
        layout.SetCharacter(slot, partyMember);
        ShowPlayerDamage(slot, damageProvider?.Invoke(partyMember) ?? 0);
        finished?.Invoke();
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
            var playerArea = player2D!.DisplayArea;
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

    #endregion


    #region UI

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

    bool weightDisplayBlinking = false;
    FilledArea? mapViewRightFillArea = null;
    Rect mapViewArea = Map2DViewArea;
    FilledArea? buttonGridBackground;
    readonly Dictionary<CharacterInfo, UIText> characterInfoTexts = [];
    readonly Dictionary<CharacterInfo, Panel> characterInfoPanels = [];
    readonly Dictionary<SecondaryStat, Tooltip?> characterInfoStatTooltips = [];

    public void UpdateCharacterBars()
    {
        if (!Ingame || layout == null || CurrentSavegame == null)
            return;

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            layout.FillCharacterBars(i, GetPartyMember(i));
        }
    }

    public void UpdateCharacterStatus(PartyMember partyMember)
    {
        if (!Ingame || layout == null || CurrentSavegame == null)
            return;

        layout.UpdateCharacterStatus(partyMember);
    }

    public void UpdateCharacters(Action finishAction, IEnumerable<PartyMember>? partyMembers = null)
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

    Tooltip? ShowSecondaryStatTooltip(Rect area, SecondaryStat secondaryStat, Character character)
    {
        if (character is PartyMember partyMember && CoreConfiguration.ShowPlayerStatsTooltips)
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

        if (tooltip != null && character is PartyMember partyMember && CoreConfiguration.ShowPlayerStatsTooltips)
            tooltip.Text = GetSecondaryStatTooltip(Features, GameLanguage, secondaryStat, partyMember);
    }

    void PlayHealAnimation(PartyMember partyMember, Action? finishAction = null)
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

    #endregion


    #region Text

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

    #endregion


    #region Map

    internal static readonly Rect Map2DViewArea = new(Global.Map2DViewX, Global.Map2DViewY,
        Global.Map2DViewWidth, Global.Map2DViewHeight);
    internal static readonly Rect Map3DViewArea = new(Global.Map3DViewX, Global.Map3DViewY,
        Global.Map3DViewWidth, Global.Map3DViewHeight);

    RenderMap2D? renderMap2D = null;    
    RenderMap3D? renderMap3D = null;
    readonly IFow? fow2D = null;
    uint lightIntensity = 0;
    readonly ILightEffectProvider lightEffectProvider;

    public Rect CurrentMapViewArea => new(mapViewArea);

    #endregion


    #region Player

    Player? player;
    Player2D? player2D = null;
    Player3D? player3D = null;
    readonly ICamera3D camera3D = null;
    Position? lastPlayerPosition = null;

    internal IRenderPlayer RenderPlayer => Is3D ? player3D! : player2D!;
    internal int PlayerAngle => Is3D ? Util.Round(player3D!.Angle) : (int)player2D!.Direction.ToAngle();
    internal CharacterDirection PlayerDirection => Is3D ? player3D!.Direction : player2D!.Direction;

    #endregion


    #region Palettes & Colors

    byte currentUIPaletteIndex = 0;

    internal byte PrimaryUIPaletteIndex { get; }
    internal byte SecondaryUIPaletteIndex { get; }
    internal byte AutomapPaletteIndex { get; }
    internal byte CustomGraphicPaletteIndex => (byte)(PrimaryUIPaletteIndex + 3);
    internal byte UIPaletteIndex => currentUIPaletteIndex;
    internal byte TextPaletteIndex => WindowActive ? UIPaletteIndex : (byte)((Map?.PaletteIndex ?? 1) - 1);

    internal void SetCurrentUIPaletteIndex(byte paletteIndex) => currentUIPaletteIndex = paletteIndex;

    internal uint GetPlayerPaletteIndex() => Math.Max(1, Map?.PaletteIndex ?? 1) - 1;

    public Color GetTextColor(TextColor textColor) => GetUIColor((int)textColor);

    public Color GetNamedPaletteColor(NamedPaletteColors namedPaletteColor) => GetUIColor((int)namedPaletteColor);

    internal Color GetPaletteColor(int paletteIndex, int colorIndex)
    {
        var paletteData = renderView.GraphicInfoProvider.Palettes[paletteIndex].Data;
        return new Color
        (
            paletteData[colorIndex * 4 + 0],
            paletteData[colorIndex * 4 + 1],
            paletteData[colorIndex * 4 + 2],
            paletteData[colorIndex * 4 + 3]
        );
    }

    public Color GetPrimaryUIColor(int colorIndex) => GetPaletteColor(renderView.GraphicInfoProvider.PrimaryUIPaletteIndex, colorIndex);

    public Color GetUIColor(int colorIndex) => GetPaletteColor(1 + UIPaletteIndex, colorIndex);

    void UpdateUIPalette(bool map)
    {
        if (map)
        {
            // TODO: MapFlags.SecondaryUI2D / MapFlags.SecondaryUI3D?
            currentUIPaletteIndex = (byte)((Map?.PaletteIndex ?? 1) - 1);
        }
        else
        {
            currentUIPaletteIndex = PrimaryUIPaletteIndex;
        }

        ouchSprite.PaletteIndex = currentUIPaletteIndex;
        layout.UpdateUIPalette(currentUIPaletteIndex);
        cursor.UpdatePalette(this);
    }

    #endregion


    #region Cursor

    readonly Cursor cursor = null;

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

            if (CoreConfiguration.IsMobile)
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
                    int yOffset = Map?.UseTravelTypes == true ? 12 : 0;
                    TrapMouse(new Rect(player2D!.DisplayArea.X - 9, player2D.DisplayArea.Y - 9 - yOffset, 33, 49));
                }
                else if (!is3D && !WindowActive && !layout.PopupActive &&
                    (cursor.Type == CursorType.Mouth ||
                    cursor.Type == CursorType.Target))
                {
                    int yOffset = Map?.UseTravelTypes == true ? 12 : 0;
                    TrapMouse(new Rect(player2D!.DisplayArea.X - 25, player2D.DisplayArea.Y - 25 - yOffset, 65, 65));
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
                if (Ingame && cursor.Type >= CursorType.Sword && cursor.Type <= CursorType.Wait)
                {
                    if (Map!.Type == MapType.Map2D)
                    {
                        var playerArea = player2D!.DisplayArea;
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

    // Alternates through eye, mouth and hand cursor when the
    // mouse wheel is used on the 2D map screen.
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

    #endregion
}