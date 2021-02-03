using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

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

        readonly IRenderView renderView;
        readonly Cursor cursor = null;
        ILayerSprite background;
        IColoredRect fadeArea;
        List<KeyValuePair<Rect, IntroText>> mainMenuTexts = new List<KeyValuePair<Rect, IntroText>>(4);
        int hoveredTextIndex = -1;
        DateTime? hoverStartTime = null;
        const int HoverColorTime = 125;
        const int FadeOutTime = 1000;
        DateTime? fadeOutStartTime = null;
        static readonly byte[] hoveredColorIndices = new byte[]
        {
            2, // White
            5, // Yellow
            6, // Orange
            4, // Red
            11, // Dark red
            4, // Red
            6, // Orange
            5 // Yellow
        };
        public event Action<CloseAction> Closed;

        public MainMenu(IRenderView renderView, Cursor cursor, IReadOnlyDictionary<IntroGraphic, byte> paletteIndices,
            IntroFont introFont, string[] texts, bool canContinue)
        {
            this.renderView = renderView;
            this.cursor = cursor;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);

            background = renderView.SpriteFactory.Create(320, 256, true) as ILayerSprite;
            background.Layer = renderView.GetLayer(Layer.IntroGraphics);
            background.PaletteIndex = (byte)(52 + paletteIndices[IntroGraphic.MainMenuBackground]);
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.MainMenuBackground);
            background.X = 0;
            background.Y = 0;
            background.Visible = true;

            // For now we use a font where each glyph has a height of 28. But the base glyph is inside a
            // 16 pixel height area in the y-center (from y=6 to y=22). So basically these 16 pixels are
            // the height we use for calculations.
            int y = 56;
            for (int i = 0; i < 4; ++i)
            {
                int textWidth = introFont.MeasureTextWidth(texts[i]);
                int offsetX = (Global.VirtualScreenWidth - textWidth) / 2;
                var area = new Rect(0, y - 6, Global.VirtualScreenWidth, 24);
                var clickArea = new Rect(offsetX, y, textWidth, 16);
                var mainMenuText = introFont.CreateText(renderView, area, texts[i], 1);
                mainMenuText.Visible = i != 0 || canContinue;
                mainMenuTexts.Add(KeyValuePair.Create(clickArea, mainMenuText));
                y += 16 + 8;
            }

            fadeArea = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Color.Transparent, 255);
            fadeArea.Layer = renderView.GetLayer(Layer.Effects);
            fadeArea.X = 0;
            fadeArea.Y = 0;
            fadeArea.Visible = false;

            cursor.Type = Data.CursorType.Sword;
        }

        public void Destroy()
        {
            background?.Delete();
            background = null;
            mainMenuTexts?.ForEach(t => t.Value?.Destroy());
            mainMenuTexts = null;
            fadeArea?.Delete();
            fadeArea = null;
        }

        public void FadeOutAndDestroy()
        {
            fadeArea.Visible = true;
            fadeOutStartTime = DateTime.Now;
        }

        public void Render()
        {
            renderView.Render(null);
        }

        public void Update(double deltaTime)
        {
            if (fadeOutStartTime != null)
            {
                if (fadeArea != null)
                {
                    var blackness = (float)(DateTime.Now - fadeOutStartTime.Value).TotalMilliseconds / FadeOutTime;

                    if (blackness >= 1.0f)
                        Destroy();
                    else
                        fadeArea.Color = new Color(0, 0, 0, Util.Round(blackness * 255));
                }
            }
            else
            {
                for (int i = 0; i < mainMenuTexts.Count; ++i)
                {
                    if (i == hoveredTextIndex)
                    {
                        int duration = (int)(DateTime.Now - hoverStartTime.Value).TotalMilliseconds / HoverColorTime;
                        byte colorIndex = hoveredColorIndices[duration % hoveredColorIndices.Length];
                        mainMenuTexts[i].Value.ColorIndex = colorIndex;
                    }
                    else
                    {
                        mainMenuTexts[i].Value.ColorIndex = 2; // White
                    }
                }
            }
        }

        public void OnMouseUp(Position position, MouseButtons buttons)
        {

        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (fadeOutStartTime != null)
                return;

            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);

                for (int i = 0; i < mainMenuTexts.Count; ++i)
                {
                    if (mainMenuTexts[i].Key.Contains(position))
                    {
                        Closed?.Invoke((CloseAction)i);
                    }
                }
            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            cursor.UpdatePosition(position);

            if (fadeOutStartTime != null)
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
