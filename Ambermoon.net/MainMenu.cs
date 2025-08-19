﻿using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon
{
    class MainMenu
    {
        public enum CloseAction
        {
            Continue,
            NewGame,
            Intro,
            Exit
        }

        readonly IGameRenderView renderView;
        readonly Cursor cursor = null;
        readonly string continueLoadingText;
        readonly string newLoadingText;
        ILayerSprite background;
        Fader mainMenuFader;
        UI.UIText loadingText;
        List<KeyValuePair<Rect, Text>> mainMenuTexts = new(4);
        int hoveredTextIndex = -1;
        DateTime? hoverStartTime = null;
        const int HoverColorTime = 125;
        const int FadeInTime = 1000;
        const int FadeOutTime = 1000;
        readonly Action<Song> playMusicAction;
        internal bool GameDataLoaded { get; set; } = false;
        static readonly byte[] hoveredColorIndices = new byte[]
        {
            (byte)TextColor.White,
            (byte)TextColor.LightYellow,
            (byte)TextColor.LightRed,
            (byte)TextColor.Red,
            (byte)TextColor.Pink,
            (byte)TextColor.Red,
            (byte)TextColor.LightRed,
            (byte)TextColor.LightYellow
        };
        bool closed = false;
        public event Action<CloseAction> Closed;
        bool started = false;

        public MainMenu(IGameRenderView renderView, Cursor cursor, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            Font introFont, string[] texts, bool canContinue, string continueLoadingText, string newLoadingText,
            Action<Song> playMusicAction, bool fromIntro)
        {
            this.renderView = renderView;
            this.cursor = cursor;
            this.continueLoadingText = continueLoadingText;
            this.newLoadingText = newLoadingText;
            this.playMusicAction = playMusicAction;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.MainMenuGraphics);

            mainMenuFader = new Fader(renderView, 0xff, 0x00, 50, false, true);

            background = renderView.SpriteFactory.CreateLayered(320, 200);
            background.Layer = renderView.GetLayer(Layer.MainMenuGraphics);
            background.PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + paletteIndices[IntroGraphic.MainMenuBackground] - 1);
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.MainMenuBackground);
            background.TextureSize = new Size(320, 256);
            background.X = 0;
            background.Y = 0;
            background.Visible = true;

            int y = 48;
            for (int i = 0; i < 4; ++i)
            {
                var area = new Rect(0, y, Global.VirtualScreenWidth, 22);
                var clickArea = new Rect(0, y - 5, Global.VirtualScreenWidth, 22 + 8);
                var mainMenuText = introFont.CreateText(renderView, Layer.MainMenuText, area, texts[i], 1);
                mainMenuText.Visible = i != 0 || canContinue;
                mainMenuTexts.Add(KeyValuePair.Create(clickArea, mainMenuText));
                y += 22 + 8;
            }

            var text = renderView.TextProcessor.CreateText("");
            loadingText = new UI.UIText(renderView, (byte)(renderView.GraphicProvider.SecondaryUIPaletteIndex - 1), text,
                Global.GetTextRect(renderView, new Rect(0, Global.VirtualScreenHeight / 2 - 3, Global.VirtualScreenWidth, 6)),
                254, TextColor.White, false, TextAlign.Center);
            loadingText.Visible = false;
            
            cursor.Type = CursorType.Sword;

            if (fromIntro)
            {
                ShowMainMenu();
                mainMenuFader?.SetColor(Ambermoon.Render.Color.Transparent);
            }
            else
                FadeInMainMenu();
        }

        void ShowMainMenu()
        {
            if (background != null)
                background.Visible = true;
        }

        void FadeInMainMenu()
        {
            playMusicAction?.Invoke(Song.Menu);
            ShowMainMenu();
            mainMenuFader.Start(FadeInTime);
        }

        public void Destroy()
        {
            closed = true;
            mainMenuFader?.Destroy();
            mainMenuFader = null;
            background?.Delete();
            background = null;
            mainMenuTexts?.ForEach(t => t.Value?.Destroy());
            mainMenuTexts = null;
            loadingText?.Destroy();
            loadingText = null;
        }

        public void FadeOutAndDestroy(bool continued, Action finished)
        {
            mainMenuFader.AttachFinishEvent(() =>
            {
                loadingText.SetText(renderView.TextProcessor.CreateText(continued ? continueLoadingText : newLoadingText));
                loadingText.Visible = true;
                finished?.Invoke();
            });
            mainMenuFader.Start(FadeOutTime, true);            
        }

        public void Render()
        {
            if (!closed)
                renderView.Render(null);
        }

        public void Update()
        {
            if (closed)
                return;

            mainMenuFader?.Update();

            for (int i = 0; i < mainMenuTexts.Count; ++i)
            {
                if (i == hoveredTextIndex)
                {
                    int duration = (int)(DateTime.Now - hoverStartTime.Value).TotalMilliseconds / HoverColorTime;
                    byte colorIndex = hoveredColorIndices[duration % hoveredColorIndices.Length];
                    mainMenuTexts[i].Value.TextColor = (TextColor)colorIndex;
                }
                else
                {
                    mainMenuTexts[i].Value.TextColor = TextColor.White;
                }
            }

            if (background != null)
            {
                if (GameDataLoaded)
                    Destroy();
            }
        }

        public void OnKeyDown(Key key)
        {
            if (closed || loadingText.Visible || started)
                return;

            if (key == Key.Space || key == Key.Return)
            {
                started = true;

                if (mainMenuTexts[0].Value.Visible)
                    Closed?.Invoke(CloseAction.Continue);
                else
                    Closed?.Invoke(CloseAction.NewGame);
            }
        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            // not used
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (closed || loadingText.Visible || started)
                return;

            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToLayer(position, Layer.MainMenuText);

                for (int i = 0; i < mainMenuTexts.Count; ++i)
                {
                    if (mainMenuTexts[i].Value.Visible && mainMenuTexts[i].Key.Contains(position))
                    {
                        started = true;
                        Closed?.Invoke((CloseAction)i);
                        break;
                    }
                }
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            if (closed || started)
                return;

            cursor.UpdatePosition(position, null);

            position = renderView.ScreenToLayer(position, Layer.MainMenuText);

            for (int i = 0; i < mainMenuTexts.Count; ++i)
            {
                if (mainMenuTexts[i].Key.Contains(position))
                {
                    if (hoveredTextIndex != i)
                    {
                        hoveredTextIndex = i;
                        hoverStartTime = DateTime.Now;
                    }
                    return;
                }
            }

            hoveredTextIndex = -1;
            hoverStartTime = null;
        }
    }
}
