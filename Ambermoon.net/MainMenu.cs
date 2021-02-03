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
            NewGame,
            Continue,
            Intro,
            Exit
        }

        readonly IRenderView renderView;
        readonly Cursor cursor = null;
        readonly ILayerSprite background;
        readonly List<KeyValuePair<Rect, IntroText>> mainMenuTexts = new List<KeyValuePair<Rect, IntroText>>(4);
        int hoveredTextIndex = -1;
        DateTime? hoverStartTime = null;
        const int HoverColorTime = 125;
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

            cursor.Type = Data.CursorType.Sword;
        }

        public void Destroy()
        {
            background?.Delete();
        }

        public void Render()
        {
            renderView.Render(null);
        }

        public void Update(double deltaTime)
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

        public void OnMouseUp(Position position, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);

            }
        }

        public void OnMouseDown(Position position, MouseButtons buttons)
        {
            if (buttons == MouseButtons.Left)
            {
                position = renderView.ScreenToGame(position);

            }
        }

        public void OnMouseMove(Position position, MouseButtons buttons)
        {
            cursor.UpdatePosition(position);

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
