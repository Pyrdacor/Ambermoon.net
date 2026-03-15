/*
 * Game.cs - Default Ambermoon game implementation
 *
 * Copyright (C) 2020-2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using Ambermoon.UI;

namespace Ambermoon.Game;

public class Game : GameCore
{
    #region Hooks

    protected override void Hook_NewGameCleanup()
    {
        customOutro = null;
    }

    protected override void Hook_NewGame()
    {
        characterCreator = new CharacterCreator(renderView, this, (name, female, portraitIndex) =>
        {
            LoadInitialCustom(name, female, (uint)portraitIndex, FixSavegameValues);
            showMobileTouchPadHandler?.Invoke(false); // This avoids showing it briefly and then immediately enter the grandfather event window
            characterCreator = null;
        });
    }

    protected override Savegame Hook_GameLoaded(Savegame savegame, int slot, bool updateSlot)
    {
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
                savegame = RequestAdvancedSavegamePatching!((ILegacyGameData)renderView.GameData, slot, sourceEpisode, targetEpisode, savegame);
            }
        }

        return savegame;
    }

    protected override void Hook_GameSaved(int slot, string name)
    {
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

            if (slot > NumBaseSavegameSlots) // 1-based slot
                additionalSavegameSlots.Names[slot - Game.NumBaseSavegameSlots - 1] = name;
            else
                additionalSavegameSlots.BaseNames[slot - 1] = name;

            additionalSavegameSlots.ContinueSavegameSlot = slot;

            additionalSaveSlotProvider.RequestSave(SavegameManager, renderView.GameData);
        }
    }

    protected override void Hook_PreUpdate(double deltaTime, out bool proceedWithUpdate)
    {
        proceedWithUpdate = true;

        if (outro?.Active == true)
        {
            outro.Update(deltaTime);
            proceedWithUpdate = false;
            return;
        }

        if (customOutro?.CreditsActive == true)
        {
            customOutro.Update(deltaTime);
            proceedWithUpdate = false;
            return;
        }

        if (characterCreator != null)
        {
            characterCreator.Update(deltaTime);
            proceedWithUpdate = false;
            return;
        }
    }

    protected override void Hook_AfterTimedEventUpdate(double deltaTime, out bool proceedWithUpdate)
    {
        // Might be activated by a timed event and we don't want to
        // process other things in this case.
        proceedWithUpdate = !(outro?.Active == true);
    }

    protected override void Hook_DestroyCleanup()
    {
        outro?.Destroy();
    }

    #endregion


    #region Providers

    protected override int Provider_ContinueSavegameSlot()
    {
        int current = Configuration.ExtendedSavegameSlots ? GetAdditionalSavegameSlots()?.ContinueSavegameSlot ?? 0 : 0;

        if (current <= 0)
            SavegameManager.GetSavegameNames(renderView.GameData, out current, NumBaseSavegameSlots);

        return current;
    }

    protected override int Provider_NumSavegameSlots()
    {
        return Configuration.ExtendedSavegameSlots ? 30 : 10;
    }

    protected override bool Provider_HasSavegames()
    {
        bool hasSavegames = SavegameManager.GetSavegameNames(renderView.GameData, out _, Provider_NumSavegameSlots()).Any(n => !string.IsNullOrWhiteSpace(n));
        if (!hasSavegames)
            hasSavegames = Configuration.ExtendedSavegameSlots && GetAdditionalSavegameSlots()?.Names?.Any(s => !string.IsNullOrWhiteSpace(s)) == true;

        return hasSavegames;
    }

    protected override IEnumerable<string> Provider_AdditionalSavegameNames()
    {
        var additionalSavegameSlots = GetAdditionalSavegameSlots();
        int remaining = NumAdditionalSavegameSlots - Math.Min(NumAdditionalSavegameSlots, additionalSavegameSlots?.Names?.Length ?? 0);

        IEnumerable<string> additionalSavegameNames = [];

        if (additionalSavegameSlots?.Names != null)
            additionalSavegameNames = Enumerable.Concat(additionalSavegameNames, additionalSavegameSlots.Names.Take(NumAdditionalSavegameSlots).Select(n => n ?? ""));
        if (remaining != 0)
            additionalSavegameNames = Enumerable.Concat(additionalSavegameNames, Enumerable.Repeat("", remaining));

        return additionalSavegameNames;
    }

    protected override Action<int>? Provider_ContinueGameSlotUpdater()
    {
        return slot =>
        {
            var additionalSavegameSlots = GetAdditionalSavegameSlots();

            if (additionalSavegameSlots != null)
                additionalSavegameSlots.ContinueSavegameSlot = slot;
        };
    }

    #endregion


    #region Configuration

    public IConfiguration Configuration { get; private set; }

    #endregion


    #region Misc

    CharacterCreator? characterCreator = null;    
    public bool Advanced => renderView.GameData.Advanced;   

    #endregion


    #region Outros

    readonly IOutroFactory outroFactory;
    IOutro? outro = null;
    CustomOutro? customOutro = null;

    internal void PrepareOutro()
    {
        Cleanup();
        layout.ShowPortraitArea(false);
        layout.SetLayout(LayoutType.None);
        HideWindowTitle();
        TrapMouse(new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight));
        CursorType = CursorType.None;
        UpdateCursor();
        SetCurrentUIPaletteIndex(0);
        Pause();
    }

    protected override void Hook_Outro()
    {
        ShowOutro();
    }

    void ShowOutro()
    {
        showMobileTouchPadHandler?.Invoke(false);

        ClosePopup();
        CloseWindow();
        Pause();
        StartSequence();
        ExecuteNextUpdateCycle(() =>
        {
            PrepareOutro();

            PlayMusic(Song.Outro);
            outro ??= outroFactory.Create(ShowCustomOutro);
            outro.Start(CurrentSavegame!);
        });
    }

    void ShowCustomOutro()
    {
        showMobileTouchPadHandler?.Invoke(false);

        customOutro = new CustomOutro(this, layout, CurrentSavegame!);
        customOutro.Start();
    }

    #endregion


    #region Savegames

    readonly IAdditionalSaveSlotProvider additionalSaveSlotProvider;
    public event Func<ILegacyGameData, int, int, int, Savegame, Savegame>? RequestAdvancedSavegamePatching;

    public const int NumAdditionalSavegameSlots = 20;

    // Used for the custom outro
    internal void LoadInitialCustom(string name, bool female, uint portraitIndex, Action<Savegame>? setup = null,
        Action? postStartAction = null)
    {
        var initialSavegame = SavegameManager.LoadInitial(renderView.GameData, savegameSerializer);

        initialSavegame.PartyMembers[1].Name = name;
        initialSavegame.PartyMembers[1].Gender = female ? Gender.Female : Gender.Male;
        initialSavegame.PartyMembers[1].PortraitIndex = (byte)portraitIndex;

        setup?.Invoke(initialSavegame);

        Start(initialSavegame, postStartAction);
    }

    internal AdditionalSavegameSlots GetAdditionalSavegameSlots() => additionalSaveSlotProvider.GetOrCreateAdditionalSavegameNames(GameVersionName);

    #endregion


    public Game(IConfiguration configuration, GameLanguage gameLanguage, IGameRenderView renderView, IGraphicInfoProvider graphicInfoProvider,
        ISavegameManager savegameManager, ISavegameSerializer savegameSerializer, TextDictionary textDictionary,
        Cursor cursor, IAudioOutput audioOutput, ISongManager songManager, FullscreenChangeHandler fullscreenChangeHandler,
        ResolutionChangeHandler resolutionChangeHandler, Func<List<Key>> pressedKeyProvider, IOutroFactory outroFactory,
        Features features, string gameVersionName, string version, Action<bool, string> keyboardRequest,
        IAdditionalSaveSlotProvider additionalSaveSlotProvider, DrawTouchFingerHandler? drawTouchFingerRequest = null,
        Action<bool>? showMobileTouchPadHandler = null)
        : base(configuration, gameLanguage, renderView, graphicInfoProvider, savegameManager, savegameSerializer,
            textDictionary, cursor, audioOutput, songManager, fullscreenChangeHandler, resolutionChangeHandler,
            pressedKeyProvider, features, gameVersionName, version, keyboardRequest, drawTouchFingerRequest,
            showMobileTouchPadHandler)
    {
        // In Advanced limit All Healing to Camp and Battle
        if (features.HasFlag(Features.AdvancedSpells))
            spellInfos[Spell.AllHealing] = spellInfos[Spell.AllHealing] with { ApplicationArea = SpellApplicationArea.CampAndBattle };

        Configuration = configuration;

        this.additionalSaveSlotProvider = additionalSaveSlotProvider;
        this.outroFactory = outroFactory;
    }

    public override void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
    {
        if (characterCreator != null)
        {
            characterCreator.OnMouseWheel(xScroll, yScroll, mousePosition, CoreConfiguration.IsMobile);
            return;
        }

        base.OnMouseWheel(xScroll, yScroll, mousePosition);
    }


    public override void OnMouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers = KeyModifiers.None)
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

        base.OnMouseDown(position, buttons, keyModifiers);
    }

    public override void OnMouseUp(Position cursorPosition, MouseButtons buttons)
    {
        if (characterCreator != null)
        {
            characterCreator.OnMouseUp(cursorPosition, buttons);
            return;
        }

        base.OnMouseUp(cursorPosition, buttons);
    }

    public override void OnMouseMove(Position position, MouseButtons buttons)
    {
        if (outro?.Active != true && !InputEnable && !layout.PopupActive)
            UntrapMouse();

        if (outro?.Active == true)
        {
            SetLastMousePosition(new Position(position));
            CursorType = CursorType.None;
        }
        else
        {
            base.OnMouseMove(position, buttons);
        }
    }

    public override void OnKeyDown(Key key, KeyModifiers modifiers, bool tapped = false)
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

        base.OnKeyDown(key, modifiers, tapped);
    }

    public override void OnKeyUp(Key key, KeyModifiers modifiers)
    {
        if (characterCreator != null)
            return;

        base.OnKeyUp(key, modifiers);
    }

    public override void OnKeyChar(char keyChar)
    {
        if (characterCreator != null)
        {
            characterCreator.OnKeyChar(keyChar);
            return;
        }

        base.OnKeyChar(keyChar);
    }


    #region Game Over

    protected override void Hook_GameOver()
    {
        GameOver();
    }

    void GameOver()
    {
        PlayMusic(Song.GameOver);
        ShowEvent(ProcessText(DataNameProvider.GameOverMessage), 8, null, true);
    }

    #endregion

}
