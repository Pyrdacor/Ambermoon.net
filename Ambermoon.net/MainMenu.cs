using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
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
            //Intro,
            Exit
        }

        readonly IRenderView renderView;
        readonly Cursor cursor = null;
        readonly string continueLoadingText;
        readonly string newLoadingText;
        ILayerSprite background;
        ILayerSprite thalionLogo;
        Fader fader;
        Fader mainMenuFader;
        UI.UIText loadingText;
        List<KeyValuePair<Rect, Text>> mainMenuTexts = new List<KeyValuePair<Rect, Text>>(4);
        int hoveredTextIndex = -1;
        DateTime? hoverStartTime = null;
        const int HoverColorTime = 125;
        const int FadeInTime = 1000;
        const int FadeOutTime = 1000;
        const int LogoFadeInTime = 1000;
        const int LogoDisplayDuration = 5000;
        const int LogoFadeOutTime = 1000;
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

        public MainMenu(IRenderView renderView, Cursor cursor, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            Font introFont, string[] texts, bool canContinue, string continueLoadingText, string newLoadingText,
            Action<Song> playMusicAction)
        {
            this.renderView = renderView;
            this.cursor = cursor;
            this.continueLoadingText = continueLoadingText;
            this.newLoadingText = newLoadingText;
            this.playMusicAction = playMusicAction;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);

            fader = new Fader(renderView, 0xff, 0x00, 255, false, true);
            mainMenuFader = new Fader(renderView, 0xff, 0x00, 50, false, true);

            thalionLogo = renderView.SpriteFactory.Create(128, 82, true, 252) as ILayerSprite;
            thalionLogo.Layer = renderView.GetLayer(Layer.IntroGraphics);
            thalionLogo.PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + paletteIndices[IntroGraphic.ThalionLogo] - 1);
            thalionLogo.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.ThalionLogo);
            thalionLogo.X = (Global.VirtualScreenWidth - 128) / 2;
            thalionLogo.Y = (Global.VirtualScreenHeight - 82) / 2;
            thalionLogo.Visible = true;

            background = renderView.SpriteFactory.Create(320, 256, true) as ILayerSprite;
            background.Layer = renderView.GetLayer(Layer.IntroGraphics);
            background.PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + paletteIndices[IntroGraphic.MainMenuBackground] - 1);
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.MainMenuBackground);
            background.X = 0;
            background.Y = 0;
            background.Visible = false;

            // For now we use a font where each glyph has a height of 28. But the base glyph is inside a
            // 16 pixel height area in the y-center (from y=6 to y=22). So basically these 16 pixels are
            // the height we use for calculations.
            int y = 56 + 12;
            for (int i = 0; i < 4; ++i)
            {
                if (i == 2) // TODO: for now we don't show the intro option as it is not implemented
                    continue;

                int textWidth = introFont.MeasureTextWidth(texts[i]);
                int offsetX = (Global.VirtualScreenWidth - textWidth) / 2;
                var area = new Rect(0, y - 6, Global.VirtualScreenWidth, 24);
                var clickArea = new Rect(offsetX, y, textWidth, 16);
                var mainMenuText = introFont.CreateText(renderView, Layer.IntroText, area, texts[i], 1);
                mainMenuText.Visible = i != 0 || canContinue;
                mainMenuTexts.Add(KeyValuePair.Create(clickArea, mainMenuText));
                y += 16 + 8;
            }

            var text = renderView.TextProcessor.CreateText("");
            loadingText = new UI.UIText(renderView, 51, text,
                new Rect(0, Global.VirtualScreenHeight / 2 - 3, Global.VirtualScreenWidth, 6), 254, TextColor.White, false, TextAlign.Center);
            loadingText.Visible = false;

            cursor.Type = Data.CursorType.Sword;
            fader.AttachFinishEvent(() =>
            {
                fader.AttachFinishEvent(SkipThalionLogo);
                fader.StartAt(DateTime.Now + TimeSpan.FromMilliseconds(LogoDisplayDuration), LogoFadeOutTime, true);
            });
            fader.Start(LogoFadeInTime);

            ShowMainMenu(false);
            playMusicAction?.Invoke(Song.Intro);
        }

        void ShowMainMenu(bool show)
        {
            if (background != null)
                background.Visible = show;
            renderView.GetLayer(Layer.IntroText).Visible = show;
        }

        void FadeInMainMenu()
        {
            ShowMainMenu(true);
            mainMenuFader.Start(FadeInTime);
        }

        void SkipThalionLogo()
        {
            fader?.DetachFinishEvent();
            fader?.Destroy();
            fader = null;
            playMusicAction?.Invoke(Song.Menu);
            thalionLogo?.Delete();
            thalionLogo = null;
            FadeInMainMenu();
        }

        public void Destroy()
        {
            closed = true;
            thalionLogo?.Delete();
            thalionLogo = null;
            fader?.Destroy();
            fader = null;
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

            fader?.Update();
            mainMenuFader?.Update();

            if (thalionLogo == null)
            {
                if (mainMenuFader?.HasFinished == true)
                {
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
                }
                else if (background != null)
                {
                    if (GameDataLoaded)
                        Destroy();
                }
            }
        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            // not used
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (closed || loadingText.Visible)
                return;

            if (thalionLogo == null && mainMenuFader?.HasFinished != true)
                return;

            if (buttons == MouseButtons.Left)
            {
                if (thalionLogo != null)
                {
                    SkipThalionLogo();
                    return;
                }

                position = renderView.ScreenToGame(position);

                for (int i = 0; i < mainMenuTexts.Count; ++i)
                {
                    if (mainMenuTexts[i].Key.Contains(position))
                    {
                        Closed?.Invoke((CloseAction)i);
                        break;
                    }
                }
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            if (closed)
                return;

            cursor.UpdatePosition(position, null);

            if (thalionLogo != null || mainMenuFader?.HasFinished != true)
                return;

            position = renderView.ScreenToGame(position);

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
