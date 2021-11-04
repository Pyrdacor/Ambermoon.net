/*
 * VersionSelector.cs - Version selector window
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon.UI
{
    public class VersionSelector
    {
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;

        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly Cursor cursor = null;
        readonly IRenderText[] versionTexts = new IRenderText[3];
        readonly IColoredRect[] versionHighlights = new IColoredRect[3];
        readonly Rect gameDataVersionTooltipArea = null;
        readonly IText gameDataVersionTooltipText = null;
        readonly IColoredRect selectedVersionMarker = null;
        readonly Button changeSaveOptionButton = null;
        readonly IRenderText saveOptionText = null;
        readonly Tooltip saveOptionTooltip = new Tooltip();
        readonly IRenderText tooltipText = null;
        readonly Dictionary<Button, IColoredRect[]> buttonBackgrounds
            = new Dictionary<Button, IColoredRect[]>();
        IColoredRect tooltipBorder = null;
        IColoredRect tooltipBackground = null;
        IText currentSaveTooltipText = null;
        readonly Button okButton = null;
        readonly List<Rect> versionAreas = new List<Rect>(3);
        int selectedSaveOption = 0;
        int selectedVersion = 0;
        readonly int versionCount = 2;
        int SelectedVersion
        {
            get => selectedVersion;
            set
            {
                if (selectedVersion != value)
                {
                    selectedVersion = value;
                    bool externalVersion = selectedVersion == 2;

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
            List<GameVersion> gameVersions, Cursor cursor, int selectedVersion, SaveOption saveOption)
        {
            var culture = CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
            var cultureName = culture?.Name ?? "";
            var language = cultureName == "de" || cultureName.StartsWith("de-") ? GameLanguage.German : GameLanguage.English;
            this.renderView = renderView;
            textureAtlas = textureAtlasManager.GetOrCreate(Layer.UI);
            var fontTextureAtlas = textureAtlasManager.GetOrCreate(Layer.Text);
            var spriteFactory = renderView.SpriteFactory;
            var layer = renderView.GetLayer(Layer.UI);
            this.cursor = cursor;
            versionCount = gameVersions.Count;

            if (versionCount < 1 || versionCount > 3)
                throw new AmbermoonException(ExceptionScope.Application, $"Invalid game version count: {versionCount}");

            #region Window
            var windowSize = new Size(16, 6);
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
            var versionListSize = new Size(14 * 16, 2 * 16 - 2);
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
            var headerText = language == GameLanguage.German
                ? "Wähle eine Spieldaten-Version:     (?)"
                : "Select a game data version:        (?)";
            AddText(headerPosition, headerText, TextColor.BrightGray);
            gameDataVersionTooltipArea = new Rect(new Position(headerPosition.X + (headerText.Length - 3) * Global.GlyphWidth, headerPosition.Y),
                new Size(3 * Global.GlyphWidth, Global.GlyphLineHeight - 1));
            gameDataVersionTooltipText = renderView.TextProcessor.CreateText(language == GameLanguage.German
                ? "Die Spieldaten-Version bezieht sich auf die Amiga-Basisdaten. Diese Versionierung ist unabhängig von der Ambermoon.net Version."
                : "The game data version relates to the Amiga base data. This version is independent of the Ambermoon.net version.");
            gameDataVersionTooltipText = renderView.TextProcessor.WrapText(gameDataVersionTooltipText,
                new Rect(0, 0, 300, 200), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
            AddSunkenBox(versionListArea.CreateModified(-1, -1, 2, 2));
            for (int i = 0; i < gameVersions.Count; ++i)
            {
                var gameVersion = gameVersions[i];
                string text = $"{gameVersion.Version} {gameVersion.Language.PadRight(9)} {gameVersion.Info.Substring(0, Math.Min(22, gameVersion.Info.Length))}";
                var versionArea = new Rect(versionListArea.X, versionListArea.Y + i * 10, versionListArea.Width, 10);
                var markerArea = versionArea.CreateModified(0, 0, 0, -1);
                var highlight = versionHighlights[i] = FillArea(markerArea, Color.White, 2);
                highlight.Visible = false;
                versionTexts[i] = AddText(new Position(versionArea.X + 1, versionArea.Y + 2), text, TextColor.White, true, 3);
                if (SelectedVersion == i)
                    selectedVersionMarker = FillArea(markerArea, Color.Green, 1);
                versionAreas.Add(versionArea);
            }
            #endregion

            #region Savegame option and OK button
            var savegameOptions = new string[2]
            {
                language == GameLanguage.German
                    ? "Speichere beim Programm"
                    : "Save games in program path",
                language == GameLanguage.German
                    ? "Speichere bei den Daten"
                    : "Save games in data path"
            };
            var savegameOptionTooltips = new string[2]
            {
                language == GameLanguage.German
                    ? "Spielstände werden neben der Ambermoon.net.exe im Unterorder 'Saves' gespeichert."
                    : "Savegames are stored next to the Ambermoon.net.exe inside the sub-folder 'Saves'.",
                language == GameLanguage.German
                    ? "Spielstände werden im Pfad der Originaldaten gespeichert und überschreiben die Originalspielstände!"
                    : "Savegames are stored in the original data path and may overwrite original savegames!"
            };
            selectedSaveOption = (int)saveOption % 2;
            changeSaveOptionButton = CreateButton(new Position(versionListArea.X, versionListArea.Bottom + 3), textureAtlasManager);
            changeSaveOptionButton.ButtonType = Data.Enumerations.ButtonType.MoveRight;
            ShowSaveOptionButton(false);
            changeSaveOptionButton.LeftClickAction = () => ToggleSaveOption(savegameOptions, savegameOptionTooltips);
            var saveOptionPosition = new Position(versionListArea.X + 34, versionListArea.Bottom + 9);
            saveOptionText = AddText(saveOptionPosition, savegameOptions[selectedSaveOption], TextColor.BrightGray);
            saveOptionText.Visible = false;
            okButton = CreateButton(new Position(versionListArea.Right - 32, versionListArea.Bottom + 3), textureAtlasManager);
            okButton.ButtonType = Data.Enumerations.ButtonType.Ok;
            okButton.Visible = true;
            okButton.LeftClickAction = () =>
            {
                Closed?.Invoke(this.selectedVersion, gameVersions[this.selectedVersion].DataProvider?.Invoke(), this.selectedVersion == 2 && selectedSaveOption == 1);
            };
            saveOptionTooltip.Area = new Rect(saveOptionPosition, new Size(savegameOptions[selectedSaveOption].Length * Global.GlyphWidth, Global.GlyphLineHeight));
            UpdateSaveOptionTooltip(savegameOptionTooltips);
            #endregion

            tooltipText = AddText(new Position(), "", TextColor.White, true, 250);
            tooltipText.Visible = false;

            SelectedVersion = selectedVersion;
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
            button.DisplayLayer = 3;
            AddSunkenBox(new Rect(position.X - 1, position.Y - 1, Button.Width + 2, Button.Height + 2), 2, 0,
                button);
            return button;
        }

        void ToggleSaveOption(string[] savegameOptions, string[] savegameOptionTooltips)
        {
            selectedSaveOption = 1 - selectedSaveOption;
            saveOptionText.Text = renderView.TextProcessor.CreateText(savegameOptions[selectedSaveOption]);
            saveOptionTooltip.Area.Size.Width = savegameOptions[selectedSaveOption].Length * Global.GlyphWidth;
            UpdateSaveOptionTooltip(savegameOptionTooltips);
        }

        void UpdateSaveOptionTooltip(string[] savegameOptionTooltips)
        {
            saveOptionTooltip.Text = savegameOptionTooltips[selectedSaveOption];
            saveOptionTooltip.TextColor = selectedSaveOption == 0 ? TextColor.White : TextColor.LightRed;
            currentSaveTooltipText = renderView.TextProcessor.CreateText(saveOptionTooltip.Text);
            currentSaveTooltipText = renderView.TextProcessor.WrapText(currentSaveTooltipText,
                new Rect(0, 0, 200, 200), new Size(Global.GlyphWidth, Global.GlyphLineHeight));
        }

        Color GetPaletteColor(byte colorIndex)
        {
            var paletteData = renderView.GraphicProvider.Palettes[50].Data;
            return new Color
            (
                paletteData[colorIndex * 4 + 0],
                paletteData[colorIndex * 4 + 1],
                paletteData[colorIndex * 4 + 2],
                paletteData[colorIndex * 4 + 3]
            );
        }

        void AddSunkenBox(Rect area, byte displayLayer = 1, byte fillColorIndex = 27,
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
                buttonBackgrounds[associatedButton] = new IColoredRect[]
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
            var renderText = renderView.RenderTextFactory.Create(renderView.GetLayer(Layer.Text),
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

            if (SelectedVersion != 2)
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

                okButton.LeftMouseDown(position, 0u);
                changeSaveOptionButton.LeftMouseDown(position, 0u);
            }
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

                var backgroundColor = textColor == TextColor.White ? GetPaletteColor(15) : GetPaletteColor(19);

                tooltipText.Place(new Rect(x, y, textWidth, textHeight), TextAlign.Center);
                tooltipText.Visible = true;
                tooltipBorder?.Delete();
                tooltipBackground?.Delete();
                tooltipBorder = FillArea(new Rect(x - 2, y - 3, textWidth + 4, textHeight + 4), GetPaletteColor(29), 248);
                tooltipBackground = FillArea(new Rect(x - 1, y - 2, textWidth + 2, textHeight + 2), backgroundColor, 249);
            }

            if (selectedVersion == 2 && currentSaveTooltipText != null && saveOptionTooltip.Area.Contains(position))
            {
                ShowTooltip(currentSaveTooltipText, saveOptionTooltip.TextColor, false);
            }
            else if (gameDataVersionTooltipArea.Contains(position))
            {
                ShowTooltip(gameDataVersionTooltipText, TextColor.White, true);
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

                if (SelectedVersion != 2)
                    HideTooltip();
            }
        }
    }
}
