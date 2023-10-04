/*
 * CharacterCreator.cs - Character creator window
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

using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI
{
    public class CharacterCreator
    {
        readonly IRenderView renderView;
        readonly ITextureAtlas textureAtlas;

        readonly List<ILayerSprite> borders = new List<ILayerSprite>();
        readonly IColoredRect backgroundFill = null;
        readonly IRenderText header = null;
        readonly Button leftButton = null;
        readonly Button rightButton = null;
        readonly Button maleButton = null;
        readonly Button femaleButton = null;
        readonly Button okButton = null;
        readonly ILayerSprite portraitBackground = null;
        readonly ILayerSprite portrait = null;
        readonly TextInput nameInput = null;
        readonly List<IColoredRect> portraitBorders = new List<IColoredRect>(4);
        readonly List<IColoredRect> sunkenBoxParts = new List<IColoredRect>(30);
        IColoredRect fadeArea;
        const int FadeTime = 250;
        DateTime? fadeInStartTime = null;
        DateTime? fadeOutStartTime = null;
        bool fadeIn = true;
        bool fadeOut = false;
        Action afterFadeOutAction;
        bool isFemale = false;
        int portraitIndex = MalePortraitIndices[0];

        static readonly int[] MalePortraitIndices = new int[]
        {
            2, 25, 7, 23,
            // New in remake
            3, 16, 9, 17, 21
        };

        static readonly int[] FemalePortraitIndices = new int[]
        {
            31, 38, 44, 51,
            // New in remake
            39, 40, 41, 47, 52
        };

        public CharacterCreator(IRenderView renderView, Game game, Action<string, bool, int> selectHandler)
        {
            this.renderView = renderView;
            textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.UI);
            var fontTextureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.Text);
            var spriteFactory = renderView.SpriteFactory;
            var layer = renderView.GetLayer(Layer.UI);

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
                windowSize.Width * 16 - 32, windowSize.Height * 16 - 32), game.GetUIColor(28), 0);
            #endregion

            #region Buttons
            var offset = windowArea.Position;
            maleButton = CreateButton(game, offset + new Position(16, 26));
            maleButton.ButtonType = ButtonType.Male;
            maleButton.Visible = true;
            maleButton.LeftClickAction = () => ChangeMale(false);
            femaleButton = CreateButton(game, offset + new Position(16, 45));
            femaleButton.ButtonType = ButtonType.Female;
            femaleButton.Visible = true;
            femaleButton.LeftClickAction = () => ChangeMale(true);
            leftButton = CreateButton(game, offset + new Position(64, 35));
            leftButton.ButtonType = ButtonType.MoveLeft;
            leftButton.Visible = true;
            leftButton.LeftClickAction = () => SwapPortrait(-1);
            rightButton = CreateButton(game, offset + new Position(160, 35));
            rightButton.ButtonType = ButtonType.MoveRight;
            rightButton.Visible = true;
            rightButton.LeftClickAction = () => SwapPortrait(1);
            okButton = CreateButton(game, new Position(windowArea.Right - 16 - 32, windowArea.Bottom - 16 - 17));
            okButton.ButtonType = ButtonType.Ok;
            okButton.Visible = true;
            okButton.LeftClickAction = () =>
            {
                nameInput.Submit();
                afterFadeOutAction = () => selectHandler?.Invoke(nameInput.Text.ToUpper(), isFemale, portraitIndex);
                DestroyAndFadeOut();
            };
            #endregion

            portraitBackground = spriteFactory.Create(32, 34, true, 1) as ILayerSprite;
            portraitBackground.Layer = layer;
            portraitBackground.X = offset.X + 112;
            portraitBackground.Y = offset.Y + 32;
            portraitBackground.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.UICustomGraphicOffset + (uint)UICustomGraphic.PortraitBackground);
            portraitBackground.PaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex + 3 - 1);
            portraitBackground.Visible = true;

            portrait = spriteFactory.Create(32, 34, true, 2) as ILayerSprite;
            portrait.Layer = layer;
            portrait.X = portraitBackground.X;
            portrait.Y = portraitBackground.Y;
            portrait.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.PortraitOffset + (uint)portraitIndex - 1);
            portrait.PaletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);
            portrait.Visible = true;

            // draw border around portrait
            var area = new Rect(portraitBackground.X - 1, portraitBackground.Y - 1, 34, 36);
            // TODO: use named palette colors
            var darkBorderColor = game.GetUIColor(26);
            var brightBorderColor = game.GetUIColor(31);
            // upper dark border
            portraitBorders.Add(FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, 1));
            // left dark border
            portraitBorders.Add(FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, 1));
            // right bright border
            portraitBorders.Add(FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, 1));
            // lower bright border
            portraitBorders.Add(FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, 1));

            const int inputWidth = 16 * Global.GlyphWidth - 2;
            nameInput = new TextInput(null, renderView, new Position(windowArea.Center.X - inputWidth / 2, offset.Y + 32 + 40),
                15, 2, TextInput.ClickAction.FocusOrSubmit, TextInput.ClickAction.Abort, TextAlign.Left);
            nameInput.AllowEmpty = true;
            nameInput.AutoSubmit = true;
            nameInput.SetText("Thalion");
            nameInput.InputChanged += text => { okButton.Disabled = string.IsNullOrWhiteSpace(text); };
            AddSunkenBox(game, new Rect(windowArea.Center.X - inputWidth / 2 - 2, offset.Y + 32 + 38, inputWidth + 6, Global.GlyphLineHeight + 3));

            string headerText = game.DataNameProvider.ChooseCharacter.Trim();
            int textWidth = headerText.Length * Global.GlyphWidth;
            int textOffset = (windowArea.Width - textWidth) / 2;
            header = AddText(offset + new Position(textOffset, 16), headerText, TextColor.BrightGray);

            fadeArea = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Render.Color.Black, 255);
            fadeArea.Layer = renderView.GetLayer(Layer.Effects);
            fadeArea.X = 0;
            fadeArea.Y = 0;
            fadeArea.Visible = true;
            fadeInStartTime = DateTime.Now;
        }

        void DestroyAndFadeOut()
        {
            fadeOutStartTime = DateTime.Now;
            fadeOut = true;
            fadeArea.Color = Render.Color.Transparent;
            fadeArea.Visible = true;
        }

        void Cleanup()
        {
            borders.ForEach(b => b?.Delete());
            backgroundFill?.Delete();
            header?.Delete();
            leftButton?.Destroy();
            rightButton?.Destroy();
            maleButton?.Destroy();
            femaleButton?.Destroy();
            okButton?.Destroy();
            portraitBackground?.Delete();
            portrait?.Delete();
            portraitBorders.ForEach(b => b?.Delete());
            nameInput?.Destroy();
            sunkenBoxParts.ForEach(b => b?.Delete());
            fadeArea.Delete();
            fadeArea = null;
        }

        void ChangeMale(bool female)
        {
            isFemale = female;
            portraitIndex = isFemale ? FemalePortraitIndices[0] : MalePortraitIndices[0];
            UpdatePortrait();
        }

        void SwapPortrait(int offset)
        {
            var portraits = isFemale ? FemalePortraitIndices : MalePortraitIndices;
            int listIndex = portraits.ToList().IndexOf(portraitIndex);

            if (listIndex == -1)
                portraitIndex = portraits[0];
            else
                portraitIndex = portraits[(listIndex + offset + portraits.Length) % portraits.Length];

            UpdatePortrait();
        }

        void UpdatePortrait()
        {
            portrait.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.PortraitOffset + (uint)portraitIndex - 1);
        }

        Button CreateButton(Game game, Position position)
        {
            AddSunkenBox(game, new Rect(position.X - 1, position.Y - 1, Button.Width + 2, Button.Height + 2), 2, 0);
            var button = new Button(renderView, position);
            button.Disabled = false;
            button.DisplayLayer = 1;
            return button;
        }

        void AddSunkenBox(Game game, Rect area, byte displayLayer = 1, byte fillColorIndex = 27)
        {
            var darkBorderColor = game.GetUIColor(26);
            var brightBorderColor = game.GetUIColor(31);
            var fillColor = game.GetUIColor(fillColorIndex);

            // upper dark border
            sunkenBoxParts.Add(FillArea(new Rect(area.X, area.Y, area.Width - 1, 1), darkBorderColor, displayLayer));
            // left dark border
            sunkenBoxParts.Add(FillArea(new Rect(area.X, area.Y + 1, 1, area.Height - 2), darkBorderColor, displayLayer));
            // fill
            sunkenBoxParts.Add(FillArea(new Rect(area.X + 1, area.Y + 1, area.Width - 2, area.Height - 2), fillColor, displayLayer));
            // right bright border
            sunkenBoxParts.Add(FillArea(new Rect(area.Right - 1, area.Y + 1, 1, area.Height - 2), brightBorderColor, displayLayer));
            // lower bright border
            sunkenBoxParts.Add(FillArea(new Rect(area.X + 1, area.Bottom - 1, area.Width - 1, 1), brightBorderColor, displayLayer));
        }


        IColoredRect FillArea(Rect area, Render.Color color, byte displayLayer = 1)
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
            var renderText = renderView.RenderTextFactory.Create(
                (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
                renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.CreateText(text, fallbackChar), textColor, shadow);
            renderText.DisplayLayer = displayLayer;
            renderText.X = position.X;
            renderText.Y = position.Y + Global.GlyphLineHeight - renderView.FontProvider.GetFont().GlyphHeight;
            renderText.Visible = true;
            return renderText;
        }

        public void Update(double deltaTime)
        {
            if (fadeIn)
            {
                var blackness = 1.0f - (float)(DateTime.Now - fadeInStartTime.Value).TotalMilliseconds / FadeTime;

                if (blackness <= 0.0f)
                {
                    fadeArea.Visible = false;
                    fadeIn = false;
                }
                else
                    fadeArea.Color = new Render.Color(0, 0, 0, Util.Round(blackness * 255));
            }
            else if (fadeOut)
            {
                if (fadeArea != null)
                {
                    var blackness = (float)(DateTime.Now - fadeOutStartTime.Value).TotalMilliseconds / FadeTime;

                    if (blackness >= 1.0f)
                    {
                        afterFadeOutAction?.Invoke();
                        Cleanup();
                    }
                    else
                        fadeArea.Color = new Render.Color(0, 0, 0, Util.Round(blackness * 255));
                }
            }
            else
            {
                maleButton.Update(0u);
                femaleButton.Update(0u);
                leftButton.Update(0u);
                rightButton.Update(0u);
                okButton.Update(0u);
                nameInput.Update();
            }
        }

        public void OnKeyDown(Key key, KeyModifiers modifiers)
        {
            if (fadeIn || fadeOut)
                return;

            if (TextInput.FocusedInput == nameInput)
            {
                nameInput.KeyDown(key);
                return;
            }

            switch (key)
            {
                case Key.PageUp:
                case Key.Up:
                case Key.Left:
                    SwapPortrait(-1);
                    break;
                case Key.PageDown:
                case Key.Down:
                case Key.Right:
                    SwapPortrait(1);
                    break;
                case Key.Return:
                    if (!okButton.Disabled)
                        okButton.Press(0);
                    break;
            }
        }

        public void OnKeyChar(char keyChar)
        {
            if (fadeIn || fadeOut)
                return;

            if (TextInput.FocusedInput == nameInput)
            {
                nameInput.KeyChar(keyChar);
                return;
            }
        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            if (fadeIn || fadeOut)
                return;

            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);
                maleButton.LeftMouseUp(position, 0u);
                femaleButton.LeftMouseUp(position, 0u);
                leftButton.LeftMouseUp(position, 0u);
                rightButton.LeftMouseUp(position, 0u);
                okButton.LeftMouseUp(position, 0u);
            }
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (fadeIn || fadeOut)
                return;

            position = renderView.ScreenToGame(position);

            if (nameInput.MouseDown(position, buttons))
                return;

            if (buttons == MouseButtons.Left)
            {
                maleButton.LeftMouseDown(position, 0u);
                femaleButton.LeftMouseDown(position, 0u);
                leftButton.LeftMouseDown(position, 0u);
                rightButton.LeftMouseDown(position, 0u);
                okButton.LeftMouseDown(position, 0u);
            }
        }

        public void OnMouseWheel(int xScroll, int yScroll, Position mousePosition)
        {
            if (fadeIn || fadeOut)
                return;

            if (yScroll != 0)
            {
                if (yScroll > 0) // up
                    SwapPortrait(-1);
                else
                    SwapPortrait(1);
            }
            else if(xScroll != 0)
            {
                if (xScroll > 0) // left
                    SwapPortrait(-1);
                else
                    SwapPortrait(1);
            }
        }
    }
}
