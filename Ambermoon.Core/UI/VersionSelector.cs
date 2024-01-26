/*
 * VersionSelector.cs - Version selector window
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Globalization;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Color = Ambermoon.Render.Color;
using System.Linq;

namespace Ambermoon.UI
{
    public class VersionSelector
    {
        const int FlagWidth = 16;
        const int FlagHeight = 16;
        const TextColor NormalTooltipColor = TextColor.White;
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly ITextureAtlas flagsTextureAtlas;
        readonly IConfiguration configuration;
        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly Cursor cursor = null;
        readonly IRenderText headerRenderText = null;
        readonly IRenderText[] versionTexts = new IRenderText[5];
        readonly IRenderText[] versionTextHighlightShadows = new IRenderText[5];
        readonly IColoredRect[] versionHighlights = new IColoredRect[5];
        readonly Rect gameDataVersionTooltipArea = null;
        IText gameDataVersionTooltipText = null;
        readonly IColoredRect selectedVersionMarker = null;
        readonly Button changeSaveOptionButton = null;
        readonly IRenderText saveOptionText = null;
        readonly Tooltip saveOptionTooltip = new Tooltip();
        readonly IRenderText tooltipText = null;
        readonly Dictionary<Button, IColoredRect[]> buttonBackgrounds
            = new Dictionary<Button, IColoredRect[]>();
        readonly List<List<GameVersion>> mergedGameVersions = new List<List<GameVersion>>();
        readonly List<GameLanguage> selectedVersionLanguages = new List<GameLanguage>();
        IColoredRect[] flagSunkenBox = null;
        IColoredRect tooltipBorder = null;
        IColoredRect tooltipBackground = null;
        List<ILayerSprite> languageChangeButtons = new List<ILayerSprite>();
        IText currentSaveTooltipText = null;
        readonly Button okButton = null;
        readonly List<Rect> versionAreas = new List<Rect>(5);
        int selectedSaveOption = 0;
        int selectedVersion = 0;
        readonly int versionCount;
        int SelectedVersion
        {
            get => selectedVersion;
            set
            {
                if (selectedVersion != value)
                {
                    selectedVersion = value;

                    bool externalVersion = IsSelectedVersionFromExternalData();
                    ShowSaveOptionButton(externalVersion);
                    saveOptionText.Visible = externalVersion;
                    selectedVersionMarker.X = versionAreas[value].X;
                    selectedVersionMarker.Y = versionAreas[value].Y;
                }
            }
        }
        uint ticks = 0;

        public event Action<int, IGameData, bool> Closed;

        public VersionSelector(string ambermoonNetVersion, IRenderView renderView, TextureAtlasManager textureAtlasManager,
            List<GameVersion> gameVersions, Cursor cursor, int selectedVersion, SaveOption saveOption, IConfiguration configuration)
        {
            this.renderView = renderView;
            this.configuration = configuration;
            textureAtlas = textureAtlasManager.GetOrCreate(Layer.UI);
            flagsTextureAtlas = textureAtlasManager.GetOrCreate(Layer.Misc);
            var fontTextureAtlas = textureAtlasManager.GetOrCreate(Layer.Text);
            var spriteFactory = renderView.SpriteFactory;
            var layer = renderView.GetLayer(Layer.UI);
            this.cursor = cursor;

            #region Window
            var windowSize = new Size(16, 8);
            var windowArea = new Rect
            (
                (Global.VirtualScreenWidth - windowSize.Width * 16) / 2,
                (Global.VirtualScreenHeight - windowSize.Height * 16) / 2 - 8,
                windowSize.Width * 16,
                windowSize.Height * 16
            );
            void AddBorder(PopupFrame frame, int column, int row)
            {
                var sprite = spriteFactory.Create(16, 16, true) as ILayerSprite;
                sprite.Layer = layer;
                sprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetPopupFrameGraphicIndex(frame));
                sprite.PaletteIndex = 0;
                sprite.X = windowArea.X + column * 16;
                sprite.Y = windowArea.Y + row * 16;
                sprite.Visible = true;
                borders.Add(sprite);
            }
            // 4 corners
            AddBorder(PopupFrame.FrameUpperLeft, 0, 0);
            AddBorder(PopupFrame.FrameUpperRight, windowSize.Width - 1, 0);
            AddBorder(PopupFrame.FrameLowerLeft, 0, windowSize.Height - 1);
            AddBorder(PopupFrame.FrameLowerRight, windowSize.Width - 1, windowSize.Height - 1);
            // top and bottom border
            for (int i = 0; i < windowSize.Width - 2; ++i)
            {
                AddBorder(PopupFrame.FrameTop, i + 1, 0);
                AddBorder(PopupFrame.FrameBottom, i + 1, windowSize.Height - 1);
            }
            // left and right border
            for (int i = 0; i < windowSize.Height - 2; ++i)
            {
                AddBorder(PopupFrame.FrameLeft, 0, i + 1);
                AddBorder(PopupFrame.FrameRight, windowSize.Width - 1, i + 1);
            }
            FillArea(new Rect(windowArea.X + 16, windowArea.Y + 16,
                windowSize.Width * 16 - 32, windowSize.Height * 16 - 32), GetPaletteColor(28), 0);
            #endregion

            #region Version list
            var versionListSize = new Size(14 * 16, 52);
            var versionListArea = new Rect
            (
                windowArea.Left + 16,
                windowArea.Top + 16 + 14,
                versionListSize.Width,
                versionListSize.Height
            );

            int width = ambermoonNetVersion.Length * Global.GlyphWidth;
            int x = (Global.VirtualScreenWidth - width) / 2;
            AddText(new Position(x, Global.VirtualScreenHeight - 10),
                ambermoonNetVersion, TextColor.DarkerGray);
            var headerPosition = new Position(versionListArea.X, versionListArea.Y - 12);
            var headerText = GetHeaderText();
            headerRenderText = AddText(headerPosition, headerText, TextColor.BrightGray);
            gameDataVersionTooltipArea = new Rect(new Position(headerPosition.X + (headerText.Length - 3) * Global.GlyphWidth, headerPosition.Y),
                new Size(3 * Global.GlyphWidth, Global.GlyphLineHeight - 1));
            gameDataVersionTooltipText = renderView.TextProcessor.CreateText(GetVersionInfoTooltip());
            gameDataVersionTooltipText = renderView.TextProcessor.WrapText(gameDataVersionTooltipText,
                new Rect(0, 0, 300, 200), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            AddSunkenBox(versionListArea.CreateModified(-1, -1, 2, 2));
            int versionToSelect = 0;
            for (int i = 0; i < gameVersions.Count; ++i)
            {
                var gameVersion = gameVersions[i];

                if (gameVersion.MergeWithPrevious && mergedGameVersions.Count != 0)
                {
                    mergedGameVersions[^1].Add(gameVersion);
                }
                else
                {
                    int index = mergedGameVersions.Count;
                    string text = BuildVersionEntryText(gameVersion);
                    var versionArea = new Rect(versionListArea.X, versionListArea.Y + index * 10, versionListArea.Width, 10);
                    var markerArea = versionArea.CreateModified(0, 0, 0, -1);
                    var highlight = versionHighlights[index] = FillArea(markerArea, Color.White, 6);
                    highlight.Visible = false;
                    versionTexts[index] = AddText(new Position(versionArea.X + 1, versionArea.Y + 2), text, TextColor.White, true, 14);
                    versionTextHighlightShadows[index] = AddText(new Position(versionArea.X + 2, versionArea.Y + 3), text, TextColor.LightGray, false, 10);
                    versionTextHighlightShadows[index].Visible = false;
                    if (SelectedVersion == index)
                        selectedVersionMarker = FillArea(markerArea, Color.Green, 2);
                    versionAreas.Add(versionArea);

                    mergedGameVersions.Add(new List<GameVersion>() { gameVersion });
                }

                if (selectedVersion == i)
                    versionToSelect = mergedGameVersions.Count - 1;
            }
            versionCount = mergedGameVersions.Count;
            foreach (var mergedGameVersion in mergedGameVersions)
            {
                if (mergedGameVersion.Any(v => v.Language == configuration.Language))
                    selectedVersionLanguages.Add(configuration.Language);
                else if (configuration.Language != GameLanguage.English && mergedGameVersion.Any(v => v.Language == GameLanguage.English))
                    selectedVersionLanguages.Add(GameLanguage.English);
                else
                    selectedVersionLanguages.Add(mergedGameVersion.First().Language);
            }
            #endregion

            #region Savegame option and OK button
            selectedSaveOption = (int)saveOption % 2;
            changeSaveOptionButton = CreateButton(new Position(versionListArea.X, versionListArea.Bottom + 3), textureAtlasManager);
            changeSaveOptionButton.ButtonType = Data.Enumerations.ButtonType.MoveRight;
            ShowSaveOptionButton(false);
            changeSaveOptionButton.LeftClickAction = () => ToggleSaveOption();
            var saveOptionPosition = new Position(versionListArea.X + 34, versionListArea.Bottom + 4);
            string savegameOptionText = GetSavegameOptionText(selectedSaveOption);
            saveOptionText = AddText(saveOptionPosition, savegameOptionText, TextColor.BrightGray);
            saveOptionText.Visible = false;
            okButton = CreateButton(new Position(versionListArea.Right - 32, versionListArea.Bottom + 3), textureAtlasManager);
            okButton.ButtonType = Data.Enumerations.ButtonType.Ok;
            okButton.Visible = true;
            okButton.LeftClickAction = () =>
            {
                int totalSelectedIndex = 0;

                for (int i = 0; i < this.selectedVersion; ++i)
                {
                    totalSelectedIndex += mergedGameVersions[i].Count;
                }

                totalSelectedIndex += mergedGameVersions[this.selectedVersion].FindIndex(v => v.Language == selectedVersionLanguages[this.selectedVersion]);

                Closed?.Invoke(totalSelectedIndex,
                    mergedGameVersions[this.selectedVersion].First(v => v.Language == selectedVersionLanguages[this.selectedVersion]).DataProvider?.Invoke(),
                    IsSelectedVersionFromExternalData() && selectedSaveOption == 1);
            };
            saveOptionTooltip.Area = Global.GetTextRect(renderView, new Rect(saveOptionPosition, new Size(savegameOptionText.Length * Global.GlyphWidth, Global.GlyphLineHeight)));
            UpdateSaveOptionTooltip();
            #endregion

            #region Language change button
            int languageCount = EnumHelper.GetValues<GameLanguage>().Length;
            var languageButtonArea = new Rect(versionListArea.Center.X - languageCount * (FlagWidth + 4) / 2, okButton.Area.Top + 12, FlagWidth, FlagHeight);
            var renderLayer = renderView.GetLayer(Layer.Misc);
            int textureFactor = (int)renderLayer.TextureFactor;
            for (int i = 0; i < languageCount; ++i)
            {
                var language = (GameLanguage)i;
                if (configuration.Language == language)
                    flagSunkenBox = AddSunkenBox(languageButtonArea.CreateModified(-2, -2, 4, 4), 1, 28);
                var languageChangeButton = renderView.SpriteFactory.Create(FlagWidth, FlagHeight, true, 3) as ILayerSprite;
                languageChangeButton.Layer = renderLayer;
                languageChangeButton.TextureAtlasOffset = GetFlagImageOffset(language, textureFactor);
                languageChangeButton.X = languageButtonArea.X;
                languageChangeButton.Y = languageButtonArea.Y;
                languageChangeButton.PaletteIndex = (byte)(renderView.GraphicProvider.FirstFantasyIntroPaletteIndex + 2);
                languageChangeButton.Visible = true;
                languageChangeButtons.Add(languageChangeButton);
                languageButtonArea.Position.X += FlagWidth + 4;
            }
            #endregion

            tooltipText = AddText(new Position(), "", TextColor.White, true, 250);
            tooltipText.Visible = false;

            SelectedVersion = versionToSelect;
            UpdateLanguageDependentValues();
        }

        bool IsSelectedVersionFromExternalData() => mergedGameVersions[selectedVersion].First().ExternalData;

        string BuildVersionEntryText(GameVersion gameVersion)
        {
            string languageText = GetLanguageText(gameVersion.Language);
            return $"{gameVersion.Info[..Math.Min(20, gameVersion.Info.Length)],-20} {gameVersion.Version,-4} {languageText[..Math.Min(11, languageText.Length)]}";
        }

        void UpdateVersionText(int index, GameLanguage language)
        {
            var gameVersions = mergedGameVersions[index];
            var gameVersion = gameVersions.FirstOrDefault(v => v.Language == language) ?? gameVersions.FirstOrDefault(v => v.Language == GameLanguage.English) ?? gameVersions.First();

            selectedVersionLanguages[index] = gameVersion.Language;

            versionTexts[index].Text = versionTextHighlightShadows[index].Text = renderView.TextProcessor.CreateText(BuildVersionEntryText(gameVersion));
        }

        void UpdateVersionTexts(GameLanguage language)
        {
            for (int i = 0; i < mergedGameVersions.Count; ++i)
            {
                UpdateVersionText(i, language);
            }
        }

        void UpdateFlags()
        {
            int languageCount = EnumHelper.GetValues<GameLanguage>().Length;
            int x = languageChangeButtons[0].X;
            int sunkenBoxDist = flagSunkenBox[0].X - x;
            int textureFactor = (int)renderView.GetLayer(Layer.Misc).TextureFactor;

            for (int i = 0; i < languageCount; ++i)
            {
                var language = (GameLanguage)i;

                if (configuration.Language == language)
                {
                    for (int b = 0; b < 5; ++b)
                    {
                        flagSunkenBox[b].X -= sunkenBoxDist;
                        flagSunkenBox[b].X += i * (FlagWidth + 4) - 2;
                    }
                }

                languageChangeButtons[i].TextureAtlasOffset = GetFlagImageOffset(language, textureFactor);
            }
        }

        Position GetFlagImageOffset(GameLanguage language, int textureFactor) => flagsTextureAtlas.GetOffset(1) + new Position((int)language * FlagWidth * textureFactor, 0); // TODO: maybe later the image has multiple rows

        string GetLanguageText(GameLanguage gameLanguage)
        {
            return configuration.Language switch
            {
                GameLanguage.German => gameLanguage switch
                {
                    GameLanguage.German => "Deutsch",
                    GameLanguage.French => "Französisch",
                    GameLanguage.Polish => "Polnisch",
                    _ => "Englisch"
                },
                GameLanguage.French => gameLanguage switch
                {
                    GameLanguage.German => "Allemand",
                    GameLanguage.French => "Français",
                    GameLanguage.Polish => "Polonais",
                    _ => "Anglais"
                },
                GameLanguage.Polish => gameLanguage switch
                {
                    GameLanguage.German => "Niemiecki",
                    GameLanguage.French => "Francuski",
                    GameLanguage.Polish => "Polski",
                    _ => "Angielski"
                },
                _ => gameLanguage switch
                {
                    GameLanguage.German => "German",
                    GameLanguage.French => "French",
                    GameLanguage.Polish => "Polish",
                    _ => "English"
                }
            };
        }

        string GetHeaderText()
        {
            return configuration.Language switch
            {
                GameLanguage.German => "Wähle eine Spieldaten-Version:     (?)",
                GameLanguage.French => "Choisir une version de données:    (?)",
                GameLanguage.Polish => "Wybierz wersję gry:                (?)",
                _ =>                   "Select a game data version:        (?)"
            };
        }

        string GetVersionInfoTooltip()
        {
            return configuration.Language switch
            {
                GameLanguage.German => "Die Spieldaten-Version bezieht sich auf die Amiga-Basisdaten. Diese Versionierung ist unabhängig von der Ambermoon.net Version.",
                GameLanguage.French => "La version des données concerne les données de base de l'Amiga. Cette version est indépendante de la version d'Ambermoon.net.",
                GameLanguage.Polish => "Wersja danych gry odnosi się do danych bazowych Amigi. Ta wersja jest niezależna od wersji Ambermoon.net.",
                _ =>                   "The game data version relates to the Amiga base data. This version is independent of the Ambermoon.net version."
            };
        }

        string GetSavegameOptionText(int option)
        {
            return option == 0
                ? configuration.Language switch
                {
                    GameLanguage.German => "Speichere beim Programm",
                    GameLanguage.French => "Sauvegarder au programme",
                    GameLanguage.Polish => "Zapis gry w ścieżce progr.",
                    _ =>                   "Save games in program path"
                }
                : configuration.Language switch
                {
                    GameLanguage.German => "Speichere bei den Daten",
                    GameLanguage.French => "Sauvegarder aux données",
                    GameLanguage.Polish => "Zapis gry w ścieżce danych",
                    _ =>                   "Save games in data path"
                };
        }

        string GetSavegameOptionTooltip(int option)
        {
            return option == 0
                ? configuration.Language switch
                {
                    GameLanguage.German =>
                        "Spielstände werden neben der Ambermoon.net.exe im Unterorder 'Saves' gespeichert.",
                    GameLanguage.French =>
                        "Les sauvegardes sont stockées à côté d'Ambermoon.net.exe dans le sous-dossier 'Saves'.",
                    GameLanguage.Polish =>
                        "Zapisane gry są przechowywane obok pliku Ambermoon.net.exe w podfolderze 'Saves'.",
                    _ =>
                        "Savegames are stored next to the Ambermoon.net.exe inside the sub-folder 'Saves'."
                }
                : configuration.Language switch
                {
                    GameLanguage.German =>
                        "Spielstände werden im Pfad der Originaldaten gespeichert und überschreiben die Originalspielstände!",
                    GameLanguage.French =>
                        "Les sauvegardes sont stockées dans le chemin de données d'origine et peuvent écraser les sauvegardes d'origine!",
                    GameLanguage.Polish =>
                        "Zapisane gry są przechowywane w oryginalnej ścieżce danych i mogą nadpisywać oryginalne zapisy!",
                    _ =>
                        "Savegames are stored in the original data path and may overwrite original savegames!"
                };
        }

        void ShowSaveOptionButton(bool show)
        {
            changeSaveOptionButton.Visible = show;
            foreach (var background in buttonBackgrounds[changeSaveOptionButton])
                background.Visible = show;
        }

        Button CreateButton(Position position, TextureAtlasManager textureAtlasManager)
        {
            var button = new Button(renderView, position, textureAtlasManager);
            button.Disabled = false;
            button.DisplayLayer = 8;
            AddSunkenBox(new Rect(position.X - 1, position.Y - 1, Button.Width + 2, Button.Height + 2), 2, 0,
                button);
            return button;
        }

        void UpdateHeaderText()
        {
            var headerText = GetHeaderText();
            headerRenderText.Text = renderView.TextProcessor.CreateText(headerText);
            gameDataVersionTooltipText = renderView.TextProcessor.CreateText(GetVersionInfoTooltip());
            gameDataVersionTooltipText = renderView.TextProcessor.WrapText(gameDataVersionTooltipText,
                new Rect(0, 0, 300, 200), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        void UpdateSaveOptionTexts()
        {
            string optionText = GetSavegameOptionText(selectedSaveOption);
            saveOptionText.Text = renderView.TextProcessor.CreateText(optionText);
            saveOptionTooltip.Area.Size.Width = optionText.Length * Global.GlyphWidth;
            UpdateSaveOptionTooltip();
        }

        void ToggleSaveOption()
        {
            selectedSaveOption = 1 - selectedSaveOption;
            UpdateSaveOptionTexts();
        }

        void UpdateSaveOptionTooltip()
        {
            saveOptionTooltip.Text = GetSavegameOptionTooltip(selectedSaveOption);
            saveOptionTooltip.TextColor = selectedSaveOption == 0 ? NormalTooltipColor : TextColor.LightRed;
            currentSaveTooltipText = renderView.TextProcessor.CreateText(saveOptionTooltip.Text);
            currentSaveTooltipText = renderView.TextProcessor.WrapText(currentSaveTooltipText,
                new Rect(0, 0, 200, 200), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        Color GetPaletteColor(byte colorIndex)
        {
            var paletteData = renderView.GraphicProvider.Palettes[renderView.GraphicProvider.PrimaryUIPaletteIndex].Data;
            return new Color
            (
                paletteData[colorIndex * 4 + 0],
                paletteData[colorIndex * 4 + 1],
                paletteData[colorIndex * 4 + 2],
                paletteData[colorIndex * 4 + 3]
            );
        }

        IColoredRect[] AddSunkenBox(Rect area, byte displayLayer = 1, byte fillColorIndex = 27,
            Button associatedButton = null)
        {
            var darkBorderColor = GetPaletteColor(26);
            var brightBorderColor = GetPaletteColor(31);
            var fillColor = GetPaletteColor(fillColorIndex);

            // upper dark border
            var upperArea = FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer);
            // left dark border
            var leftArea = FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer);
            // fill
            var fillArea = FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer);
            // right bright border
            var rightArea = FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer);
            // lower bright border
            var lowerArea = FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer);

            if (associatedButton != null)
            {
                return buttonBackgrounds[associatedButton] = new IColoredRect[]
                {
                    upperArea, leftArea, fillArea, rightArea, lowerArea
                };
            }
            else
            {
                return new IColoredRect[]
                {
                    upperArea, leftArea, fillArea, rightArea, lowerArea
                };
            }
        }

        IColoredRect FillArea(Rect area, Color color, byte displayLayer = 1)
        {
            var filledArea = renderView.ColoredRectFactory.Create(area.Width, area.Height, color, displayLayer);
            filledArea.Layer = renderView.GetLayer(Layer.UI);
            filledArea.X = area.Left;
            filledArea.Y = area.Top;
            filledArea.Visible = true;
            return filledArea;
        }

        IRenderText AddText(Position position, string text, TextColor textColor, bool shadow = true,
            byte displayLayer = 1, char? fallbackChar = null)
        {
            position = Global.GetTextRect(renderView, new Rect(position, new Size(Global.GlyphWidth, Global.GlyphLineHeight))).Position;
            var renderText = renderView.RenderTextFactory.Create(
                (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
                renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text, fallbackChar), textColor, shadow);
            renderText.DisplayLayer = displayLayer;
            renderText.X = position.X;
            renderText.Y = position.Y;
            renderText.PaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);
            renderText.Visible = true;
            return renderText;
        }

        public void Update(double deltaTime)
        {
            ticks = Game.UpdateTicks(ticks, deltaTime);
            okButton.Update(ticks);
            changeSaveOptionButton.Update(ticks);
        }

        public void Render()
        {
            renderView.Render(null);
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            switch (key)
            {
                case Key.Up:
                    SelectedVersion = (SelectedVersion - 1 + versionCount) % versionCount;
                    break;
                case Key.Down:
                    SelectedVersion = (SelectedVersion + 1) % versionCount;
                    break;
                case Key.PageUp:
                case Key.Home:
                    SelectedVersion = 0;
                    break;
                case Key.PageDown:
                case Key.End:
                    SelectedVersion = versionCount - 1;
                    break;
                case Key.Return:
                case Key.Space:
                {
                    var action = okButton.LeftClickAction;
                    okButton.LeftClickAction = () =>
                    {
                        okButton.Release(true);
                        okButton.Disabled = true;                        
                        action?.Invoke();
                    };
                    okButton.ContinuousActionDelayInTicks = 3 * Game.TicksPerSecond / 2;
                    okButton.Pressed = true;
                    break;
                }
            }

            if (!IsSelectedVersionFromExternalData())
                HideTooltip();
        }

        public void OnKeyUp(Key key, KeyModifiers modifiers)
        {

        }

        public void OnKeyChar(char keyChar)
        {

        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);
                okButton.LeftMouseUp(position, 0u);
                changeSaveOptionButton.LeftMouseUp(position, 0u);
            }
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);

                for (int i = 0; i < versionAreas.Count; ++i)
                {
                    if (versionAreas[i].Contains(position))
                    {
                        SelectedVersion = i;
                        return;
                    }
                }

                for (int i = 0; i < languageChangeButtons.Count; ++i)
                {
                    var button = languageChangeButtons[i];

                    if (position.X >= button.X && position.X < button.X + button.Width &&
                        position.Y >= button.Y && position.Y < button.Y + button.Height)
                    {
                        if ((int)configuration.Language != i)
                        {
                            configuration.Language = (GameLanguage)i;
                            UpdateLanguageDependentValues();
                        }
                    }
                }

                okButton.LeftMouseDown(position, 0u);
                changeSaveOptionButton.LeftMouseDown(position, 0u);
            }
        }

        void UpdateLanguageDependentValues()
        {
            UpdateHeaderText();
            UpdateFlags();
            UpdateSaveOptionTexts();
            UpdateVersionTexts(configuration.Language);
        }

        void HighlightVersion(int index)
        {
            for (int i = 0; i < versionHighlights.Length; ++i)
            {
                if (versionHighlights[i] == null)
                    continue;

                bool active = i == index;
                versionHighlights[i].Visible = active;
                versionTexts[i].TextColor = active ? TextColor.Black : TextColor.White;
                versionTexts[i].Shadow = !active;
                versionTextHighlightShadows[i].Visible = active;
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            cursor.UpdatePosition(position, null);

            position = renderView.ScreenToGame(position);

            for (int i = 0; i < versionAreas.Count; ++i)
            {
                if (versionAreas[i].Contains(position))
                {
                    HighlightVersion(i);
                    return;
                }
            }

            HighlightVersion(-1);

            void ShowTooltip(IText text, TextColor textColor, bool up)
            {
                tooltipText.Text = text;
                tooltipText.TextColor = textColor;

                int textWidth = text.MaxLineSize * Global.GlyphWidth;
                int x = Util.Limit(0, position.X - textWidth / 2, Global.VirtualScreenWidth - textWidth - 3);
                int textHeight = text.LineCount * Global.GlyphLineHeight;
                int y = up ? position.Y - textHeight - 8 : position.Y + 16;

                var backgroundColor = textColor == NormalTooltipColor ? GetPaletteColor((byte)TextColor.Green) : GetPaletteColor((byte)TextColor.Pink);

                tooltipText.Place(Global.GetTextRect(renderView, new Rect(x, y, textWidth, textHeight)), TextAlign.Center);
                tooltipText.Visible = true;
                tooltipBorder?.Delete();
                tooltipBackground?.Delete();
                tooltipBorder = FillArea(new Rect(x - 2, y - 3, textWidth + 4, textHeight + 4), GetPaletteColor(29), 248);
                tooltipBackground = FillArea(new Rect(x - 1, y - 2, textWidth + 2, textHeight + 2), backgroundColor, 249);
            }

            if (IsSelectedVersionFromExternalData() && currentSaveTooltipText != null && saveOptionTooltip.Area.Contains(position))
            {
                ShowTooltip(currentSaveTooltipText, saveOptionTooltip.TextColor, false);
            }
            else if (gameDataVersionTooltipArea.Contains(position))
            {
                ShowTooltip(gameDataVersionTooltipText, NormalTooltipColor, true);
            }
            else
            {
                HideTooltip();
            }
        }

        void HideTooltip()
        {
            tooltipText.Visible = false;
            tooltipBorder?.Delete();
            tooltipBackground?.Delete();
        }

        public void OnMouseWheel(int _, int yScroll, Position mousePosition)
        {
            if (yScroll != 0)
            {
                if (yScroll > 0) // up
                    SelectedVersion = (SelectedVersion - 1 + versionCount) % versionCount;
                else
                    SelectedVersion = (SelectedVersion + 1) % versionCount;

                if (!IsSelectedVersionFromExternalData())
                    HideTooltip();
            }
        }
    }
}
