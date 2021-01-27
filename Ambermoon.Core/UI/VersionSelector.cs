using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    public class VersionSelector
    {
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;
        readonly ITextureAtlas fontTextureAtlas;

        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly IColoredRect backgroundFill = null;
        readonly Cursor cursor = null;
        readonly IRenderText[] versionTexts = new IRenderText[3];
        readonly IColoredRect[] versionHighlights = new IColoredRect[3];
        readonly IColoredRect selectedVersionMarker = null;
        readonly Button changeSaveOptionButton = null;
        readonly IRenderText saveOptionText = null;
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

                    changeSaveOptionButton.Visible = externalVersion;
                    saveOptionText.Visible = externalVersion;
                    selectedVersionMarker.X = versionAreas[value].X;
                    selectedVersionMarker.Y = versionAreas[value].Y;
                }
            }
        }

        public event Action<IGameData, bool> Closed;

        public VersionSelector(IRenderView renderView, TextureAtlasManager textureAtlasManager,
            List<GameVersion> gameVersions, Cursor cursor)
        {
            this.renderView = renderView;
            textureAtlas = textureAtlasManager.GetOrCreate(Layer.UI);
            fontTextureAtlas = textureAtlasManager.GetOrCreate(Layer.Text);
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
            backgroundFill = FillArea(new Rect(windowArea.X + 16, windowArea.Y + 16,
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
            AddText(new Position(versionListArea.X, versionListArea.Y - 12), "Select a game data version:", TextColor.Gray);
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
                "Save games in program path",
                "Save games in data path"
            };
            changeSaveOptionButton = CreateButton(new Position(versionListArea.X, versionListArea.Bottom + 3), textureAtlasManager);
            changeSaveOptionButton.ButtonType = Data.Enumerations.ButtonType.MoveRight;
            changeSaveOptionButton.Visible = false;
            changeSaveOptionButton.Action = () => ToggleSaveOption(savegameOptions);
            saveOptionText = AddText(new Position(versionListArea.X + 34, versionListArea.Bottom + 9), savegameOptions[0], TextColor.Gray);
            saveOptionText.Visible = false;
            okButton = CreateButton(new Position(versionListArea.Right - 32, versionListArea.Bottom + 3), textureAtlasManager);
            okButton.ButtonType = Data.Enumerations.ButtonType.Ok;
            okButton.Visible = true;
            okButton.Action = () => Closed?.Invoke(gameVersions[selectedVersion].DataProvider?.Invoke(), selectedVersion == 2 && selectedSaveOption == 1);
            #endregion
        }

        Button CreateButton(Position position, TextureAtlasManager textureAtlasManager)
        {
            var button = new Button(renderView, position, textureAtlasManager);
            button.Disabled = false;
            button.DisplayLayer = 1;
            return button;
        }

        void ToggleSaveOption(string[] savegameOptions)
        {
            selectedSaveOption = 1 - selectedSaveOption;
            saveOptionText.Text = renderView.TextProcessor.CreateText(savegameOptions[selectedSaveOption]);
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

        void AddSunkenBox(Rect area, byte displayLayer = 1)
        {
            var darkBorderColor = GetPaletteColor(26);
            var brightBorderColor = GetPaletteColor(31);
            var fillColor = GetPaletteColor(27);

            // upper dark border
            FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer);
            // left dark border
            FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer);
            // fill
            FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer);
            // right bright border
            FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer);
            // lower bright border
            FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer);
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
            renderText.Visible = true;
            return renderText;
        }

        public void Update(double deltaTime)
        {
            okButton.Update(0u);
            changeSaveOptionButton.Update(0u);
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
            }
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
            cursor.UpdatePosition(position);

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
        }

        public void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
        {
            if (yScroll != 0)
            {
                if (yScroll > 0) // up
                    SelectedVersion = (SelectedVersion - 1 + versionCount) % versionCount;
                else
                    SelectedVersion = (SelectedVersion + 1) % versionCount;
            }
        }
    }
}
