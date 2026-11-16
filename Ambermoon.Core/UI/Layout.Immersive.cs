/*
 * Layout.Immersive.cs - Immersive mode of the map view
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI
{
    // In the immersive mode the map covers the whole screen. Only the names of the
    // party members stay visible at the top (the portraits move up). Below them a small
    // bar shows the map name, the compass, the clock and the other special items.
    // The active spells are shown at the lower left. The action buttons can be shown
    // without the UI frame by a small arrow at the right.
    // The idea of the bar was taken from the Immersive Edition of A5Projects
    // (https://github.com/A5Projects/Ambermoon.net).
    internal partial class Layout
    {
        // The portraits move up until they are out of the screen. The names stay at the top.
        const int ImmersivePortraitOffset = -36;
        const int ImmersiveNameOffset = -30;
        const int ImmersiveAnimationDuration = 250; // ms
        // the arrow is right next to the button grid
        public static readonly Rect ImmersiveButtonsArrowArea = new(Global.VirtualScreenWidth - ImmersiveGraphics.IconSize,
            Global.ButtonGridY + (3 * Button.Height - ImmersiveGraphics.IconSize) / 2, ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize);
        // A small arrow at the lower right switches the page of the button grid (in the immersive
        // mode only if the buttons are shown). The clickable area has a border of 1 pixel around it.
        public static readonly Rect ButtonPageArrowArea = new(310, 188, 8, 6);
        static readonly Rect ButtonPageArrowClickArea = ButtonPageArrowArea.CreateModified(-1, -1, 2, 2);
        // The bar below the names (between the quest log icon and the immersive icon).
        static readonly Rect ImmersiveHudArea = new(17, 8, Global.VirtualScreenWidth - 2 * 17, 16);
        const int ImmersiveHudTextY = 11;
        const int ImmersiveHudIconSize = 16; // special item graphics are shown at half size
        // The compass shows the directions on a horizontal strip which scrolls under a fixed pointer.
        static readonly Rect ImmersiveCompassArea = new(Global.VirtualScreenWidth / 2 - 32, ImmersiveHudTextY - 2, 64, 10);
        const int ImmersiveCompassBearingSpacing = 24; // pixels per 45 degrees
        // the coordinates are shown centered at the bottom (left of the button grid)
        const int ImmersiveCoordinatesBackgroundY = Global.VirtualScreenHeight - 10;
        const int ImmersiveCoordinatesTextY = ImmersiveCoordinatesBackgroundY + 2;
        // the active spells are shown at the lower left
        static readonly Position ImmersiveActiveSpellsPosition = new(2, Global.VirtualScreenHeight - 22);
        // the clickable area of the names at the top
        static readonly Rect ImmersiveNamesArea = new(Global.PartyMemberPortraitAreas[0].Left, 0,
            Global.PartyMemberPortraitAreas[^1].Right - Global.PartyMemberPortraitAreas[0].Left, 8);
        // The icon to toggle the immersive mode is the horizontal mirror of the quest log icon (0, 8).
        // In the immersive mode both icons move up to the names.
        public const int IconY = 8;
        public const int ImmersiveIconY = 0;
        public Rect ImmersiveIconArea => new(Global.VirtualScreenWidth - ImmersiveGraphics.IconSize, ImmersiveActive ? ImmersiveIconY : IconY,
            ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize + 2);
        // the background of the names and the bar
        static readonly Rect ImmersiveTopBackgroundArea = new(0, 0, Global.VirtualScreenWidth, 24);
        static readonly Render.Color ImmersiveBackgroundColor = new(16, 15, 12, 95);
        ILayerSprite? immersiveIcon = null;

        /// <summary>
        /// The icon is only available on maps.
        /// </summary>
        public bool ImmersiveIconVisible => immersiveIcon?.Visible == true;

        bool immersive = false;
        bool immersiveButtonsExpanded = false;
        int portraitOffsetY = 0;
        int portraitNameOffsetY = 0;
        float immersiveAnimationProgress = 0.0f; // 0 = normal, 1 = immersive
        bool immersiveAnimating = false;
        DateTime lastImmersiveAnimationTime = DateTime.MinValue;
        ILayerSprite? immersiveButtonsArrow = null;
        ILayerSprite? buttonPageArrow = null;
        IColoredRect? buttonPageArrowBackground = null;
        Tooltip? buttonPageArrowTooltip = null;
        // the buttons have transparent parts, so they get a black background
        IColoredRect? immersiveButtonsBackground = null;
        // the bar below the names
        IColoredRect? immersiveHudBackground = null;
        IColoredRect? immersiveCoordinatesBackground = null;
        readonly System.Collections.Generic.List<IColoredRect> immersiveCompassFrame = [];
        readonly System.Collections.Generic.List<UIText> immersiveCompassLabels = [];
        readonly System.Collections.Generic.List<IColoredRect> immersiveCompassTicks = [];
        int? shownImmersiveCompassAngle = null;
        UIText? immersiveMapNameText = null;
        UIText? immersiveClockText = null;
        UIText? immersiveCoordinatesText = null;
        Tooltip? immersiveMapNameTooltip = null;
        string immersiveMapName = "";
        string? shownImmersiveMapName = null;
        string? shownImmersiveClock = null;
        string? shownImmersiveCoordinates = null;
        // The special item sprites and the active spells are moved in the immersive mode.
        // The positions of the normal mode are kept here.
        readonly System.Collections.Generic.Dictionary<object, Position> immersiveDefaultPositions = [];

        /// <summary>
        /// True if the immersive mode is active and the map is shown.
        /// </summary>
        public bool ImmersiveActive => immersive && (Type == LayoutType.Map2D || Type == LayoutType.Map3D);

        public bool ImmersiveButtonsExpanded => immersiveButtonsExpanded;

        Rect GetPortraitClipArea(int slot) => Global.PartyMemberPortraitAreas[slot].CreateModified(1, 1 + portraitOffsetY, -2, -2);

        Rect GetPortraitNameArea(int slot) => GetTextRect(Global.PartyMemberPortraitAreas[slot].Left + 2,
            Global.PartyMemberPortraitAreas[slot].Top + 31 + portraitNameOffsetY, 30, 6);

        /// <summary>
        /// Activates or deactivates the immersive mode. The portraits move up or down.
        /// </summary>
        public void SetImmersive(bool active, bool animate = true)
        {
            if (immersive == active)
            {
                UpdateImmersiveUI();
                return;
            }

            immersive = active;
            lastImmersiveAnimationTime = DateTime.Now;
            immersiveAnimating = animate && (Type == LayoutType.Map2D || Type == LayoutType.Map3D);

            UpdateImmersiveUI();
        }

        public void ToggleImmersiveButtons()
        {
            immersiveButtonsExpanded = !immersiveButtonsExpanded;
            UpdateImmersiveUI();
        }

        /// <summary>
        /// The map name is shown in the bar of the immersive mode (the window title is hidden).
        /// </summary>
        internal void SetImmersiveMapName(string mapName)
        {
            immersiveMapName = mapName;
            shownImmersiveMapName = null; // forces an update
            UpdateImmersiveHud();
        }

        bool ButtonPageArrowVisible => (Type == LayoutType.Map2D || Type == LayoutType.Map3D) && !OptionMenuOpen &&
            (!ImmersiveActive || (buttonGrid.Visible && !buttonGrid.Concealed));

        /// <summary>
        /// The page can only be switched by the arrow if it could be switched
        /// by a right click or the return key as well.
        /// </summary>
        bool CanSwitchButtonGridPage => game.InputEnable && !game.ScriptAllInputDisabled && !game.WindowActive && !PopupActive &&
            !OptionMenuOpen && !game.Fading && game.CursorType == CursorType.Sword;

        /// <summary>
        /// The option menu overlay covers everything but the button grid. The rows 142 overlap
        /// so that the dither pattern stays aligned (odd y offsets would shift it).
        /// </summary>
        static Rect[] GetImmersiveOptionMenuOverlayAreas() =>
        [
            new(0, 0, Global.VirtualScreenWidth, Global.ButtonGridY),
            new(0, 142, Global.ButtonGridX, Global.VirtualScreenHeight - 142),
            new(Global.ButtonGridArea.Right, 142, Global.VirtualScreenWidth - Global.ButtonGridArea.Right, Global.VirtualScreenHeight - 142),
            new(Global.ButtonGridX, Global.ButtonGridArea.Bottom, Global.ButtonGridArea.Width, Global.VirtualScreenHeight - Global.ButtonGridArea.Bottom)
        ];

        /// <summary>
        /// Returns true if the given position is on a visible UI element of the immersive
        /// mode (and not on the map). Those positions are handled by the layout.
        /// </summary>
        public bool ImmersiveUIContains(Position position)
        {
            if (ImmersiveIconVisible && ImmersiveIconArea.Contains(position))
                return true;

            if (ButtonPageArrowVisible && ButtonPageArrowClickArea.Contains(position))
                return true;

            if (!ImmersiveActive)
                return false;

            if (!OptionMenuOpen && ImmersiveButtonsArrowArea.Contains(position))
                return true;

            if ((immersiveButtonsExpanded || OptionMenuOpen) && Global.ButtonGridArea.Contains(position))
                return true;

            if (ImmersiveHudArea.Contains(position))
                return true;

            if (immersiveCoordinatesBackground?.Visible == true &&
                new Rect(immersiveCoordinatesBackground.X, immersiveCoordinatesBackground.Y, immersiveCoordinatesBackground.Width, immersiveCoordinatesBackground.Height).Contains(position))
                return true;

            return ImmersiveNamesArea.Contains(position);
        }

        /// <summary>
        /// Handles clicks on the arrows of the immersive mode.
        /// </summary>
        bool ImmersiveClick(Position position, MouseButtons buttons)
        {
            if (ImmersiveIconVisible && ImmersiveIconArea.Contains(position))
            {
                if (buttons == MouseButtons.Left)
                    game.ToggleImmersiveMode();

                return true;
            }

            if (ButtonPageArrowVisible && ButtonPageArrowClickArea.Contains(position))
            {
                if (buttons == MouseButtons.Left && CanSwitchButtonGridPage)
                    ToggleButtonGridPage();

                return true;
            }

            // nothing can be collapsed while the option menu is open
            if (!ImmersiveActive || OptionMenuOpen || buttons != MouseButtons.Left)
                return false;

            if (ImmersiveButtonsArrowArea.Contains(position))
            {
                ToggleImmersiveButtons();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the visibility of all UI elements dependent on the immersive mode.
        /// This is also called after the map layout was recreated (e.g. after closing a window).
        /// </summary>
        public void UpdateImmersiveUI()
        {
            bool active = ImmersiveActive;

            if (Type != LayoutType.None)
                sprite.Visible = !active;

            // the buttons of the option menu are always shown
            buttonGrid.Concealed = active && !immersiveButtonsExpanded && !OptionMenuOpen;

            if (immersiveButtonsBackground == null)
            {
                var area = Global.ButtonGridArea;
                immersiveButtonsBackground = RenderView.ColoredRectFactory.Create(area.Width, area.Height, Render.Color.Black, 1);
                immersiveButtonsBackground.Layer = renderLayer;
                immersiveButtonsBackground.X = area.X;
                immersiveButtonsBackground.Y = area.Y;
            }

            immersiveButtonsBackground.Visible = active && buttonGrid.Visible && !buttonGrid.Concealed;

            UpdateImmersiveItems();

            immersiveButtonsArrow ??= CreateImmersiveIcon(ImmersiveButtonsArrowArea);
            UpdateImmersiveArrow(immersiveButtonsArrow, active && !OptionMenuOpen, immersiveButtonsExpanded);

            UpdateButtonGridPageIndicator();

            if (immersiveIcon == null)
            {
                immersiveIcon = CreateImmersiveIcon(ImmersiveIconArea);
                immersiveIcon.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Misc)!.GetOffset(ImmersiveGraphics.ImmersiveIconGraphicIndex);
            }

            // only on maps (also in the normal mode to activate the immersive mode)
            immersiveIcon.Visible = Type == LayoutType.Map2D || Type == LayoutType.Map3D;
            immersiveIcon.Y = ImmersiveIconArea.Y;
            game.SetQuestLogIconY(active ? ImmersiveIconY : IconY);
        }

        /// <summary>
        /// The small arrow at the lower right switches the page of the button grid (movement or actions)
        /// when it is clicked. Many players don't know that the page can be switched by a right click
        /// or the return key otherwise.
        /// </summary>
        void UpdateButtonGridPageIndicator()
        {
            if (buttonPageArrow == null)
            {
                buttonPageArrow = (RenderView.SpriteFactory.Create(ButtonPageArrowArea.Width, ButtonPageArrowArea.Height, true, 50) as ILayerSprite)!;
                buttonPageArrow.Layer = RenderView.GetLayer(Layer.Misc);
                buttonPageArrow.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Misc)!.GetOffset(ImmersiveGraphics.ButtonPageArrowGraphicIndex);
                buttonPageArrow.X = ButtonPageArrowArea.X;
                buttonPageArrow.Y = ButtonPageArrowArea.Y;
            }

            if (buttonPageArrowBackground == null)
            {
                // in the immersive mode the arrow gets a black background with a border of 1 pixel
                var area = ButtonPageArrowClickArea;
                buttonPageArrowBackground = RenderView.ColoredRectFactory.Create(area.Width, area.Height, Render.Color.Black, 49);
                buttonPageArrowBackground.Layer = RenderView.GetLayer(Layer.Misc);
                buttonPageArrowBackground.X = area.X;
                buttonPageArrowBackground.Y = area.Y;
            }

            bool arrowVisible = ButtonPageArrowVisible;
            // same palette as the buttons (on maps the palette of the map)
            buttonPageArrow.PaletteIndex = game.UIPaletteIndex;
            buttonPageArrow.Visible = arrowVisible;
            buttonPageArrowBackground.Visible = arrowVisible && ImmersiveActive;

            if (buttonPageArrowTooltip == null || !tooltips.Contains(buttonPageArrowTooltip))
            {
                buttonPageArrowTooltip = AddTooltip(ButtonPageArrowClickArea,
                    ImmersiveGraphics.GetButtonPageTooltip(game.GameLanguage), TextColor.White); // like the button tooltips
            }

            buttonPageArrowTooltip.Enabled = arrowVisible;

            if (!arrowVisible && activeTooltip == buttonPageArrowTooltip)
                HideTooltip();
        }

        void UpdateImmersiveItems()
        {
            bool active = ImmersiveActive;

            ArrangeImmersiveItems(active);

            // In the immersive mode the compass ball, the coordinates and the clock are replaced
            // by own elements in the bar. The other special items are shown in the bar at half size.
            foreach (var specialItemSprite in specialItemSprites)
                specialItemSprite.Value.Visible = !active || specialItemSprite.Key != SpecialItemPurpose.Compass;
            foreach (var specialItemText in specialItemTexts.Values)
                specialItemText.Visible = !active;
            foreach (var activeSpellSprite in activeSpellSprites.Values)
                activeSpellSprite.Visible = true;
            foreach (var activeSpellDurationBackground in activeSpellDurationBackgrounds.Values)
                activeSpellDurationBackground.Visible = true;
            foreach (var activeSpellDurationBar in activeSpellDurationBars.Values)
                activeSpellDurationBar.Visible = true; // empty bars are out of the screen

            UpdateImmersiveHud();
        }

        UIText CreateImmersiveHudText(Rect area, TextAlign textAlign = TextAlign.Left)
        {
            var text = new UIText(RenderView, game.UIPaletteIndex, RenderView.TextProcessor.CreateText(""),
                GetTextRect(area), 3, TextColor.BrightGray, true, textAlign, false);
            text.Visible = false;
            return text;
        }

        IColoredRect CreateImmersiveHudArea(Rect area, Render.Color color, byte displayLayer)
        {
            var coloredRect = RenderView.ColoredRectFactory.Create(area.Width, area.Height, color, displayLayer);
            coloredRect.Layer = renderLayer;
            coloredRect.X = area.X;
            coloredRect.Y = area.Y;
            coloredRect.Visible = false;
            return coloredRect;
        }

        static void SetImmersiveHudText(UIText text, ref string? shownText, string newText, IGameRenderView renderView)
        {
            if (shownText == newText)
                return;

            shownText = newText;
            text.SetText(renderView.TextProcessor.CreateText(newText));
        }

        /// <summary>
        /// Updates the bar below the names: map name at the left, compass in the center,
        /// coordinates, special item icons and clock at the right.
        /// </summary>
        void UpdateImmersiveHud()
        {
            bool active = ImmersiveActive;

            if (!active && immersiveHudBackground == null)
                return;

            var translucent = ImmersiveBackgroundColor;
            immersiveHudBackground ??= CreateImmersiveHudArea(ImmersiveTopBackgroundArea, translucent, 1);
            immersiveMapNameText ??= CreateImmersiveHudText(new Rect(ImmersiveHudArea.X + 3, ImmersiveHudTextY, ImmersiveHudArea.Width - 6, 7));
            immersiveClockText ??= CreateImmersiveHudText(new Rect(ImmersiveHudArea.Right - 33, ImmersiveHudTextY, 30, 7));
            immersiveCoordinatesText ??= CreateImmersiveHudText(new Rect(0, ImmersiveCoordinatesTextY, 13 * Global.GlyphWidth, 7));

            bool hasCompass = specialItemSprites.ContainsKey(SpecialItemPurpose.Compass);
            bool hasClock = specialItemTexts.ContainsKey(SpecialItemPurpose.Clock);
            bool hasCoordinates = specialItemTexts.ContainsKey(SpecialItemPurpose.MapLocation);

            immersiveHudBackground.Visible = active;
            if (hasCompass)
                EnsureImmersiveCompass();
            SetImmersiveCompassVisible(active && hasCompass);
            immersiveClockText.Visible = active && hasClock;
            immersiveCoordinatesText.Visible = active && hasCoordinates;
            immersiveMapNameText.Visible = active;

            foreach (var text in new[] { immersiveMapNameText, immersiveClockText, immersiveCoordinatesText })
                text.PaletteIndex = game.UIPaletteIndex;
            foreach (var label in immersiveCompassLabels)
                label.PaletteIndex = game.UIPaletteIndex;

            if (!active)
            {
                if (immersiveCoordinatesBackground != null)
                    immersiveCoordinatesBackground.Visible = false;

                if (immersiveMapNameTooltip != null)
                    immersiveMapNameTooltip.Enabled = false;

                return;
            }

            if (hasCompass && game.PlayerAvailable)
                UpdateImmersiveCompass();

            // Right side from right to left: clock, day time, monster eye, windchain
            int x = ImmersiveHudArea.Right - 3;

            if (hasClock)
            {
                SetImmersiveHudText(immersiveClockText, ref shownImmersiveClock, $"{game.GameTime!.Hour,2}:{game.GameTime.Minute:00}", RenderView);
                x -= 5 * Global.GlyphWidth + 2;
            }

            foreach (var purpose in new[] { SpecialItemPurpose.DayTime, SpecialItemPurpose.MonsterEye, SpecialItemPurpose.WindChain })
            {
                if (specialItemSprites.TryGetValue(purpose, out var itemSprite))
                {
                    x -= itemSprite.Width + 2;
                    itemSprite.X = x;
                    itemSprite.Y = ImmersiveHudArea.Y + (ImmersiveHudArea.Height - itemSprite.Height) / 2;
                }
            }

            if (hasCoordinates)
            {
                // centered at the bottom (the last glyph has 1 pixel spacing at the right)
                string coordinates = $"X:{game.PartyPosition.X + 1} Y:{game.PartyPosition.Y + 1}";
                int width = coordinates.Length * Global.GlyphWidth - 1;
                int textX = (Global.VirtualScreenWidth - width) / 2;
                SetImmersiveHudText(immersiveCoordinatesText, ref shownImmersiveCoordinates, coordinates, RenderView);
                immersiveCoordinatesText.SetPosition(new Position(textX, ImmersiveCoordinatesTextY));
                var backgroundArea = new Rect(textX - 3, ImmersiveCoordinatesBackgroundY, width + 6, Global.VirtualScreenHeight - ImmersiveCoordinatesBackgroundY);
                immersiveCoordinatesBackground ??= CreateImmersiveHudArea(backgroundArea, translucent, 1);
                immersiveCoordinatesBackground.Resize(backgroundArea.Width, backgroundArea.Height);
                immersiveCoordinatesBackground.X = backgroundArea.X;
                immersiveCoordinatesBackground.Y = backgroundArea.Y;
                immersiveCoordinatesBackground.Visible = true;
            }
            else if (immersiveCoordinatesBackground != null)
            {
                immersiveCoordinatesBackground.Visible = false;
            }

            // Map name at the left. It is shortened if there is not enough space.
            int nameRight = (hasCompass ? ImmersiveCompassArea.X : x) - 4;
            int nameX = ImmersiveHudArea.X + 3;
            int maxCharacters = Math.Max(4, (nameRight - nameX) / Global.GlyphWidth);
            string mapName = immersiveMapName;
            bool truncated = mapName.Length > maxCharacters;

            if (truncated)
                mapName = mapName[..(maxCharacters - 3)].TrimEnd() + "...";

            SetImmersiveHudText(immersiveMapNameText, ref shownImmersiveMapName, mapName, RenderView);

            // tooltips are removed when the layout is reset
            if (immersiveMapNameTooltip == null || !tooltips.Contains(immersiveMapNameTooltip))
            {
                immersiveMapNameTooltip = AddTooltip(new Rect(0, 0, 0, 0), immersiveMapName, TextColor.BrightGray);
                immersiveMapNameTooltip.ShowBelow = true;
            }

            immersiveMapNameTooltip.Area = new Rect(nameX, ImmersiveHudTextY - 1, mapName.Length * Global.GlyphWidth, Global.GlyphLineHeight + 1);
            immersiveMapNameTooltip.Text = immersiveMapName;
            immersiveMapNameTooltip.Enabled = truncated && !OptionMenuOpen;
        }

        /// <summary>
        /// Creates the compass of the bar: a black face with a metal rim, a small pointer at the top
        /// and the directions with small ticks between them. The idea is taken from the Immersive Edition
        /// of A5Projects.
        /// </summary>
        void EnsureImmersiveCompass()
        {
            if (immersiveCompassFrame.Count != 0)
                return;

            var area = ImmersiveCompassArea;
            var interior = area.CreateModified(1, 1, -2, -2);
            int center = area.X + area.Width / 2;

            void AddFrame(Rect bounds, Render.Color color, byte displayLayer = 3) =>
                immersiveCompassFrame.Add(CreateImmersiveHudArea(bounds, color, displayLayer));

            AddFrame(area, new Render.Color(5, 8, 9), 2);
            AddFrame(new Rect(area.X, area.Y, area.Width, 1), new Render.Color(69, 80, 82));
            AddFrame(new Rect(area.X, area.Bottom - 1, area.Width, 1), new Render.Color(35, 43, 46));
            AddFrame(new Rect(area.X, area.Y + 1, 1, area.Height - 2), new Render.Color(46, 56, 60));
            AddFrame(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), new Render.Color(27, 34, 37));
            // the pointer (a small arrow pointing down)
            var pointerColor = new Render.Color(191, 207, 203);
            AddFrame(new Rect(center - 1, area.Y, 3, 1), pointerColor, 4);
            AddFrame(new Rect(center, area.Y + 1, 1, 1), pointerColor, 4);

            // The directions start with west and continue clockwise ("W N-W N ..."). Only the first 8 are needed.
            var directions = game.DataNameProvider.CompassDirections.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < 8; ++i)
            {
                string direction = directions[i].Replace("-", "");
                var label = CreateImmersiveHudText(new Rect(center, ImmersiveHudTextY, direction.Length * Global.GlyphWidth, 7));
                label.SetText(RenderView.TextProcessor.CreateText(direction));
                label.Clip(interior);
                immersiveCompassLabels.Add(label);

                var tick = CreateImmersiveHudArea(new Rect(center, area.Y + 4, 1, 3), new Render.Color(140, 162, 161), 3);
                tick.ClipArea = interior;
                immersiveCompassTicks.Add(tick);
            }

            shownImmersiveCompassAngle = null;
        }

        void SetImmersiveCompassVisible(bool visible)
        {
            foreach (var rect in immersiveCompassFrame)
                rect.Visible = visible;
            foreach (var label in immersiveCompassLabels)
                label.Visible = visible;
            foreach (var tick in immersiveCompassTicks)
                tick.Visible = visible;
        }

        /// <summary>
        /// Scrolls the directions of the compass pixel by pixel according to the player angle.
        /// </summary>
        void UpdateImmersiveCompass()
        {
            int angle = (game.PlayerAngle % 360 + 360) % 360;

            if (shownImmersiveCompassAngle == angle)
                return;

            shownImmersiveCompassAngle = angle;
            var area = ImmersiveCompassArea;
            int center = area.X + area.Width / 2;

            // the difference is wrapped so the strip never jumps while turning through north
            int Offset(float bearing) => Util.Round(((bearing - angle + 540.0f) % 360.0f - 180.0f) * ImmersiveCompassBearingSpacing / 45.0f);

            for (int i = 0; i < 8; ++i)
            {
                int bearing = (i * 45 + 270) % 360; // the first direction is west
                var label = immersiveCompassLabels[i];
                int width = label.Area.Width;
                label.SetPosition(new Position(center + Offset(bearing) - width / 2, ImmersiveHudTextY));
                immersiveCompassTicks[i].X = center + Offset(bearing + 22.5f);
            }
        }

        UIGraphic? GetSpecialItemGraphic(SpecialItemPurpose specialItem) => specialItem switch
        {
            SpecialItemPurpose.MonsterEye => game.MonsterSeesPlayer ? UIGraphic.MonsterEyeActive : UIGraphic.MonsterEyeInactive,
            SpecialItemPurpose.DayTime => UIGraphic.Night + (int)game.GameTime!.GetDayTime(),
            SpecialItemPurpose.WindChain => UIGraphic.Windchain,
            _ => null
        };

        /// <summary>
        /// Sets the graphic of a special item sprite. In the immersive mode the graphic is shown
        /// at half size and without its frame (if the frameless graphic is available).
        /// </summary>
        void ApplySpecialItemGraphic(SpecialItemPurpose specialItem, ILayerSprite sprite)
        {
            var graphic = GetSpecialItemGraphic(specialItem);

            if (graphic == null)
                return;

            bool immersive = ImmersiveActive;
            uint? framelessIndex = immersive ? ImmersiveGraphics.GetFramelessGraphicIndex(graphic.Value) : null;
            uint textureIndex;
            Size textureSize;

            if (framelessIndex != null)
            {
                textureIndex = framelessIndex.Value;
                textureSize = ImmersiveGraphics.GetFramelessGraphicSize(graphic.Value)!;
            }
            else
            {
                textureIndex = Graphics.GetUIGraphicIndex(graphic.Value);
                textureSize = new Size(32, specialItem == SpecialItemPurpose.WindChain ? 15 : 32);
            }

            var size = immersive ? new Size((textureSize.Width + 1) / 2, (textureSize.Height + 1) / 2) : textureSize;

            sprite.TextureAtlasOffset = textureAtlas.GetOffset(textureIndex);

            if (sprite.TextureSize != textureSize)
                sprite.TextureSize = textureSize;

            if (sprite.Width != size.Width || sprite.Height != size.Height)
                sprite.Resize(size.Width, size.Height);
        }

        Position GetImmersiveDefaultPosition(object element, Position currentPosition)
        {
            if (!immersiveDefaultPositions.TryGetValue(element, out var position))
            {
                position = new Position(currentPosition);
                immersiveDefaultPositions.Add(element, position);
            }

            return position;
        }

        /// <summary>
        /// Shrinks the special item sprites to half size for the bar of the immersive mode
        /// (the bar positions them) and moves the active spells to the lower left.
        /// In the normal mode everything is moved back to the normal positions and sizes.
        /// </summary>
        void ArrangeImmersiveItems(bool immersive)
        {
            foreach (var specialItemSprite in specialItemSprites)
            {
                if (specialItemSprite.Key == SpecialItemPurpose.Compass)
                    continue; // the compass ball is hidden in the immersive mode

                var itemSprite = specialItemSprite.Value;
                var defaultPosition = GetImmersiveDefaultPosition(itemSprite, new Position(itemSprite.X, itemSprite.Y));
                ApplySpecialItemGraphic(specialItemSprite.Key, itemSprite);

                if (!immersive)
                {
                    itemSprite.X = defaultPosition.X;
                    itemSprite.Y = defaultPosition.Y;
                }
            }

            int index = 0;

            foreach (var activeSpell in EnumHelper.GetValues<ActiveSpellType>())
            {
                if (!activeSpellSprites.TryGetValue(activeSpell, out var spellSprite))
                    continue;

                var defaultPosition = GetImmersiveDefaultPosition(spellSprite, new Position(spellSprite.X, spellSprite.Y));
                var position = immersive
                    ? new Position(ImmersiveActiveSpellsPosition.X + index * 16, ImmersiveActiveSpellsPosition.Y)
                    : defaultPosition;
                int offsetX = position.X - defaultPosition.X;
                int offsetY = position.Y - defaultPosition.Y;
                spellSprite.X = position.X;
                spellSprite.Y = position.Y;

                if (activeSpellDurationBackgrounds.TryGetValue(activeSpell, out var durationBackground))
                {
                    var defaultBackgroundPosition = GetImmersiveDefaultPosition(durationBackground, new Position(durationBackground.X, durationBackground.Y));
                    durationBackground.X = defaultBackgroundPosition.X + offsetX;
                    durationBackground.Y = defaultBackgroundPosition.Y + offsetY;
                }

                if (activeSpellDurationBars.TryGetValue(activeSpell, out var durationBar))
                {
                    durationBar.OffsetX = offsetX;
                    durationBar.OffsetY = offsetY;
                }

                ++index;
            }
        }

        ILayerSprite CreateImmersiveIcon(Rect area)
        {
            var arrow = (RenderView.SpriteFactory.Create(ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize, true, 50) as ILayerSprite)!;
            arrow.Layer = RenderView.GetLayer(Layer.Misc);
            arrow.PaletteIndex = game.PrimaryUIPaletteIndex;
            arrow.X = area.X;
            arrow.Y = area.Y;
            arrow.Visible = false;
            return arrow;
        }

        static void UpdateImmersiveArrow(ILayerSprite arrow, bool active, bool expanded)
        {
            // A left arrow pulls the panel in from the right, a right arrow pushes it away.
            arrow.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Misc)!.GetOffset(expanded
                ? ImmersiveGraphics.ArrowRightGraphicIndex : ImmersiveGraphics.ArrowLeftGraphicIndex);
            arrow.Visible = active;
        }

        /// <summary>
        /// Moves the portraits up or down (called every frame).
        /// </summary>
        void UpdateImmersiveAnimation()
        {
            // Outside of the map (e.g. in windows or battles) the portraits are always shown.
            float target = ImmersiveActive ? 1.0f : 0.0f;

            if (immersiveAnimationProgress == target)
            {
                immersiveAnimating = false;
                return;
            }

            if (!immersiveAnimating)
            {
                // Only toggling the mode is animated. Opening or closing windows is not.
                immersiveAnimationProgress = target;
                ApplyPortraitOffset();
                return;
            }

            var now = DateTime.Now;
            float step = (float)(now - lastImmersiveAnimationTime).TotalMilliseconds / ImmersiveAnimationDuration;
            lastImmersiveAnimationTime = now;

            if (target > immersiveAnimationProgress)
                immersiveAnimationProgress = Math.Min(target, immersiveAnimationProgress + step);
            else
                immersiveAnimationProgress = Math.Max(target, immersiveAnimationProgress - step);

            ApplyPortraitOffset();
        }

        void ApplyPortraitOffset()
        {
            int newOffset = Util.Round(ImmersivePortraitOffset * immersiveAnimationProgress);
            int newNameOffset = Util.Round(ImmersiveNameOffset * immersiveAnimationProgress);
            int delta = newOffset - portraitOffsetY;

            portraitOffsetY = newOffset;
            portraitNameOffsetY = newNameOffset;

            if (delta != 0)
            {
                foreach (var border in portraitBorders)
                    border.Y += delta;

                foreach (var barBackground in portraitBarBackgrounds)
                {
                    if (barBackground != null)
                        barBackground.Y += delta;
                }

                for (int i = 0; i < GameCore.MaxPartyMembers; ++i)
                {
                    if (portraits[i] != null)
                    {
                        portraits[i]!.Y += delta;
                        portraits[i]!.ClipArea = GetPortraitClipArea(i);
                    }

                    if (portraitBackgrounds[i] != null)
                        portraitBackgrounds[i]!.Y += delta;

                    if (characterStatusIcons[i] != null)
                        characterStatusIcons[i]!.Y += delta;
                }

                if (portraitAnimation?.SecondarySprite != null)
                {
                    portraitAnimation.SecondarySprite.Y += delta;
                    portraitAnimation.SecondarySprite.ClipArea = portraitAnimation.PrimarySprite?.ClipArea ?? portraitAnimation.SecondarySprite.ClipArea;
                }

                foreach (var bar in characterBars)
                {
                    if (bar != null)
                        bar.OffsetY = portraitOffsetY;
                }
            }

            for (int i = 0; i < GameCore.MaxPartyMembers; ++i)
                portraitNames[i]?.Place(GetPortraitNameArea(i), TextAlign.Center);
        }
    }
}
