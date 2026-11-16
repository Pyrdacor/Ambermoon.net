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

using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;

namespace Ambermoon.UI
{
    // In the immersive mode the map covers the whole screen. Only the names of the
    // party members stay visible at the top (the portraits move up). The action buttons
    // and the special items (compass, clock, etc) can be shown without the UI frame
    // by small arrows at the right.
    internal partial class Layout
    {
        // The portraits move up until they are out of the screen. The names stay at the top.
        const int ImmersivePortraitOffset = -36;
        const int ImmersiveNameOffset = -30;
        const int ImmersiveAnimationDuration = 250; // ms
        // the arrows are right next to the button grid and the special items
        public static readonly Rect ImmersiveButtonsArrowArea = new(Global.VirtualScreenWidth - ImmersiveGraphics.IconSize,
            Global.ButtonGridY + (3 * Button.Height - ImmersiveGraphics.IconSize) / 2, ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize);
        public static readonly Rect ImmersiveItemsArrowArea = new(Global.VirtualScreenWidth - ImmersiveGraphics.IconSize,
            Global.UpperRightArea.Y + (Global.UpperRightArea.Height - ImmersiveGraphics.IconSize) / 2, ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize);
        // the clickable area of the names at the top
        static readonly Rect ImmersiveNamesArea = new(Global.PartyMemberPortraitAreas[0].Left, 0,
            Global.PartyMemberPortraitAreas[^1].Right - Global.PartyMemberPortraitAreas[0].Left, 8);
        // the icon to toggle the immersive mode is the horizontal mirror of the quest log icon (0, 8)
        public static readonly Rect ImmersiveIconArea = new(Global.VirtualScreenWidth - ImmersiveGraphics.IconSize, 8,
            ImmersiveGraphics.IconSize, ImmersiveGraphics.IconSize + 2);
        ILayerSprite? immersiveIcon = null;

        /// <summary>
        /// The icon is only available on maps.
        /// </summary>
        public bool ImmersiveIconVisible => immersiveIcon?.Visible == true;

        bool immersive = false;
        bool immersiveButtonsExpanded = false;
        bool immersiveItemsExpanded = false;
        int portraitOffsetY = 0;
        int portraitNameOffsetY = 0;
        float immersiveAnimationProgress = 0.0f; // 0 = normal, 1 = immersive
        bool immersiveAnimating = false;
        DateTime lastImmersiveAnimationTime = DateTime.MinValue;
        ILayerSprite? immersiveButtonsArrow = null;
        ILayerSprite? immersiveItemsArrow = null;

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

        public void ToggleImmersiveItems()
        {
            immersiveItemsExpanded = !immersiveItemsExpanded;
            UpdateImmersiveUI();
        }

        /// <summary>
        /// Returns true if the given position is on a visible UI element of the immersive
        /// mode (and not on the map). Those positions are handled by the layout.
        /// </summary>
        public bool ImmersiveUIContains(Position position)
        {
            if (ImmersiveIconVisible && ImmersiveIconArea.Contains(position))
                return true;

            if (!ImmersiveActive)
                return false;

            if (ImmersiveButtonsArrowArea.Contains(position) || ImmersiveItemsArrowArea.Contains(position))
                return true;

            if ((immersiveButtonsExpanded || OptionMenuOpen) && Global.ButtonGridArea.Contains(position))
                return true;

            if (immersiveItemsExpanded && Global.UpperRightArea.Contains(position))
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

            if (!ImmersiveActive || buttons != MouseButtons.Left)
                return false;

            if (ImmersiveButtonsArrowArea.Contains(position))
            {
                ToggleImmersiveButtons();
                return true;
            }

            if (ImmersiveItemsArrowArea.Contains(position))
            {
                ToggleImmersiveItems();
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
            UpdateImmersiveItems();

            immersiveButtonsArrow ??= CreateImmersiveIcon(ImmersiveButtonsArrowArea);
            immersiveItemsArrow ??= CreateImmersiveIcon(ImmersiveItemsArrowArea);
            UpdateImmersiveArrow(immersiveButtonsArrow, active, immersiveButtonsExpanded);
            UpdateImmersiveArrow(immersiveItemsArrow, active, immersiveItemsExpanded);

            if (immersiveIcon == null)
            {
                immersiveIcon = CreateImmersiveIcon(ImmersiveIconArea);
                immersiveIcon.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(Layer.Misc)!.GetOffset(ImmersiveGraphics.ImmersiveIconGraphicIndex);
            }

            // only on maps (also in the normal mode to activate the immersive mode)
            immersiveIcon.Visible = Type == LayoutType.Map2D || Type == LayoutType.Map3D;
        }

        void UpdateImmersiveItems()
        {
            bool visible = !ImmersiveActive || immersiveItemsExpanded;

            foreach (var specialItemSprite in specialItemSprites.Values)
                specialItemSprite.Visible = visible;
            foreach (var specialItemText in specialItemTexts.Values)
                specialItemText.Visible = visible;
            foreach (var activeSpellSprite in activeSpellSprites.Values)
                activeSpellSprite.Visible = visible;
            foreach (var activeSpellDurationBackground in activeSpellDurationBackgrounds.Values)
                activeSpellDurationBackground.Visible = visible;
            foreach (var activeSpellDurationBar in activeSpellDurationBars.Values)
                activeSpellDurationBar.Visible = visible; // empty bars are out of the screen
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
