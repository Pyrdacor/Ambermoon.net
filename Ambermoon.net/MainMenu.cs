using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;

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
        readonly IntroText continueText;
        readonly IntroText newGameText;
        readonly IntroText introText;
        readonly IntroText exitText;
        public event Action<CloseAction> Closed;

        public MainMenu(IRenderView renderView, Cursor cursor, IntroFont introFont, bool canContinue)
        {
            this.renderView = renderView;
            this.cursor = cursor;
            var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);

            background = renderView.SpriteFactory.Create(320, 256, true) as ILayerSprite;
            background.Layer = renderView.GetLayer(Layer.IntroGraphics);
            background.PaletteIndex = 59; // intro palette 6
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.MainMenuBackground);
            background.X = 0;
            background.Y = 0;
            background.Visible = true;

            // For now we use a font where each glyph has a height of 28. But the base glyph is inside a
            // 16 pixel height area in the y-center (from y=6 to y=22). So basically these 16 pixels are
            // the height we use for calculations.
            int y = 56;
            continueText = introFont.CreateText(renderView, new Rect(0, y - 6, 320, 24), "Weiterspielen", 1);
            y += 16 + 8;
            newGameText = introFont.CreateText(renderView, new Rect(0, y - 6, 320, 24), "Neue Quest starten", 1);
            y += 16 + 8;
            introText = introFont.CreateText(renderView, new Rect(0, y - 6, 320, 24), "Intro", 1);
            y += 16 + 8;
            exitText = introFont.CreateText(renderView, new Rect(0, y - 6, 320, 24), "Quit", 1);

            continueText.Visible = canContinue;
            newGameText.Visible = true;
            introText.Visible = true;
            exitText.Visible = true;

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

            
        }
    }
}
