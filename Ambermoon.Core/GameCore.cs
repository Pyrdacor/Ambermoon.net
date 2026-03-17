/*
 * GameCore.cs - Game core of Ambermoon
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;
using Ambermoon.UI;
using TextColor = Ambermoon.Data.Enumerations.Color;

[assembly: InternalsVisibleTo("Ambermoon.Game")]

namespace Ambermoon;

public abstract partial class GameCore
{
    public Features Features { get; }

    // NOTE: The content of the game core is spread across multiple source files in the "Game" folder.
    public GameCore(ICoreConfiguration configuration, GameLanguage gameLanguage, IGameRenderView renderView, IGraphicInfoProvider graphicInfoProvider,
        ISavegameManager savegameManager, ISavegameSerializer savegameSerializer, TextDictionary textDictionary,
        Cursor cursor, IAudioOutput audioOutput, ISongManager songManager, FullscreenChangeHandler fullscreenChangeHandler,
        ResolutionChangeHandler resolutionChangeHandler, Func<List<Key>> pressedKeyProvider,
        Features features, string gameVersionName, string version, Action<bool, string> keyboardRequest,
        DrawTouchFingerHandler? drawTouchFingerRequest = null, Action<bool>? showMobileTouchPadHandler = null)
    {
        Features = features;

        Character.FoodWeight = Features.HasFlag(Features.ReducedFoodWeight) ? 25u : 250u;

        spellInfos = new(Data.SpellInfos.Entries);

        this.drawTouchFingerRequest = drawTouchFingerRequest;
        this.showMobileTouchPadHandler = showMobileTouchPadHandler;
        this.keyboardRequest = keyboardRequest;

        this.gameVersionName = gameVersionName;

        currentUIPaletteIndex = PrimaryUIPaletteIndex = (byte)(renderView.GraphicInfoProvider.PrimaryUIPaletteIndex - 1);
        SecondaryUIPaletteIndex = (byte)(renderView.GraphicInfoProvider.SecondaryUIPaletteIndex - 1);
        AutomapPaletteIndex = (byte)(renderView.GraphicInfoProvider.AutomapPaletteIndex - 1);

        this.fullscreenChangeHandler = fullscreenChangeHandler;
        this.resolutionChangeHandler = resolutionChangeHandler;
        CoreConfiguration = configuration;
        GameLanguage = gameLanguage;

        SavegameManager = savegameManager;
        this.savegameSerializer = savegameSerializer;

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
        layout = new Layout(this, renderView, ItemManager);
        layout.BattleFieldSlotClicked += BattleFieldSlotClicked;
        places = renderView.GameData.Places;
        DataNameProvider = renderView.GameData.DataNameProvider;
        fullVersion = version + $"^{GameVersion.RemakeReleaseDate}^^{DataNameProvider.DataVersionString}^{DataNameProvider.DataInfoString}";
        this.textDictionary = textDictionary;
        this.lightEffectProvider = renderView.GameData.LightEffectProvider;
        camera3D = renderView.Camera3D;
        windowTitle = renderView.RenderTextFactory.Create(
            (byte)(renderView.GraphicInfoProvider.DefaultTextPaletteIndex - 1),
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
        ouchSprite = (renderView.SpriteFactory.Create(32, 23, true) as ILayerSprite)!;
        ouchSprite.ClipArea = Map2DViewArea;
        ouchSprite.Layer = renderView.GetLayer(Layer.UI);
        ouchSprite.PaletteIndex = currentUIPaletteIndex;
        ouchSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)!.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.Ouch));
        ouchSprite.Visible = false;
        ouchEvent.Action = () => ouchSprite.Visible = false;

        if (CoreConfiguration.IsMobile)
        {
            mobileClickIndicator = (renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite)!;
            mobileClickIndicator.Layer = renderView.GetLayer(Layer.Cursor);
            mobileClickIndicator.Visible = false;
            mobileClickIndicator.PaletteIndex = PrimaryUIPaletteIndex;
            mobileClickIndicator.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Cursor)!.GetOffset((uint)CursorType.Click);
        }

        for (int i = 0; i < MaxPartyMembers; ++i)
        {
            hurtPlayerSprites[i] = (renderView.SpriteFactory.Create(32, 26, true, 200) as ILayerSprite)!;
            hurtPlayerSprites[i].Layer = renderView.GetLayer(Layer.UI);
            hurtPlayerSprites[i].PaletteIndex = PrimaryUIPaletteIndex;
            hurtPlayerSprites[i].TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)!.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.DamageSplash));
            hurtPlayerSprites[i].Visible = false;
            hurtPlayerDamageTexts[i] = renderView.RenderTextFactory.Create((byte)(renderView.GraphicInfoProvider.DefaultTextPaletteIndex - 1));
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
        battleRoundActiveSprite = (renderView.SpriteFactory.Create(32, 36, true) as ILayerSprite)!;
        battleRoundActiveSprite.Layer = renderView.GetLayer(Layer.UI);
        battleRoundActiveSprite.PaletteIndex = PrimaryUIPaletteIndex;
        battleRoundActiveSprite.DisplayLayer = 2;
        battleRoundActiveSprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.UI)!
            .GetOffset(Graphics.CombatGraphicOffset + (uint)CombatGraphicIndex.UISwordAndMace);
        battleRoundActiveSprite.X = 240;
        battleRoundActiveSprite.Y = 150;
        battleRoundActiveSprite.Visible = false;

        // Create texture atlas for monsters in battle
        var textureAtlasManager = TextureAtlasManager.Instance;

        if (CharacterManager.MonsterGraphicAtlas != null)
        {
            textureAtlasManager.AddAtlas(Layer.BattleMonsterRow, CharacterManager.MonsterGraphicAtlas);
        }
        else
        {
            var monsterGraphicDictionary = CharacterManager.Monsters.ToDictionary(m => m.Index, m => m.CombatGraphic);
            textureAtlasManager.AddFromGraphics(Layer.BattleMonsterRow, monsterGraphicDictionary);
        }

        var monsterGraphicAtlas = textureAtlasManager.GetOrCreate(Layer.BattleMonsterRow);
        renderView.GetLayer(Layer.BattleMonsterRow).Texture = monsterGraphicAtlas!.Texture;

        layout.ShowPortraitArea(false);

        // Mobile action indicator
        mobileActionIndicator = (renderView.SpriteFactory.Create(16, 16, true) as ILayerSprite)!;
        mobileActionIndicator.Layer = renderView.GetLayer(Layer.UI);
        mobileActionIndicator.PaletteIndex = PrimaryUIPaletteIndex;
        mobileActionIndicator.DisplayLayer = 0;
        mobileActionIndicator.Visible = false;

        TextInput.FocusChanged += InputFocusChanged;
    }

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

    public void Schnism()
    {
        ShowMessagePopup(DataNameProvider.TurnOnTuneInAndDropOut, () =>
            DamageAllPartyMembers(p => 0u, p => p.Alive, null, null, Condition.Drugged));
    }
}
