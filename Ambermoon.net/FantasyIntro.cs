using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon
{
    internal class FantasyIntro
    {
        static ITextureAtlas textureAtlas = null;
        readonly Action finishAction;
        readonly IRenderView renderView;
        readonly IRenderLayer renderLayer;
        readonly IRenderLayer colorLayer;
        readonly Queue<FantasyIntroAction> actions = null;
        readonly ILayerSprite backgroundLeftBorder;
        readonly ILayerSprite backgroundRightBorder;
        readonly ILayerSprite background;
        readonly ILayerSprite fairy;
        readonly ILayerSprite writing;
        readonly Dictionary<int, ILayerSprite> sparks = new(512);
        readonly Dictionary<int, IColoredRect> sparkLines = new(96);
        readonly Dictionary<int, IColoredRect> sparkDots = new(512);
        readonly Color[] colors = new Color[32];
        readonly byte paletteOffset;
        readonly IColoredRect fadeArea;
        double time = 0;
        long fadeStartFrames = -1;
        bool fadeOut = false;
        long lastFrame = -1;
        bool playFairyAnimation = false;
        uint fairyAnimationIndex = 0;
        static uint borderTextureIndexOffset = 0;

        static void EnsureTextures(IRenderView renderView, IFantasyIntroData fantasyIntroData)
        {
            if (textureAtlas == null)
            {
                TextureAtlasManager.Instance.AddFromGraphics(Layer.FantasyIntroGraphics,
                    fantasyIntroData.Graphics.ToDictionary(g => (uint)g.Key, g => g.Value));
                var borders256 = new DataReader(Resources.Borders256);
                borderTextureIndexOffset = (uint)fantasyIntroData.Graphics.Keys.Max() + 1;
                var borderGraphicInfo = new GraphicInfo
                {
                    Width = 45,
                    Height = 256,
                    GraphicFormat = GraphicFormat.Palette5Bit,
                    Alpha = false
                };
                var graphicReader = new GraphicReader();
                Graphic LoadBorder()
                {
                    var border = new Graphic();
                    graphicReader.ReadGraphic(border, borders256, borderGraphicInfo);
                    return border;
                }
                TextureAtlasManager.Instance.AddFromGraphics(Layer.FantasyIntroGraphics,
                    Enumerable.Range(0, 2).ToDictionary(i => (uint)(borderTextureIndexOffset + i), _ => LoadBorder()));
                textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.FantasyIntroGraphics);
                renderView.GetLayer(Layer.FantasyIntroGraphics).Texture = textureAtlas.Texture;
            }
        }

        public FantasyIntro(IRenderView renderView, IFantasyIntroData fantasyIntroData, Action finishAction)
        {
            this.finishAction = finishAction;
            this.renderView = renderView;
            renderLayer = renderView.GetLayer(Layer.FantasyIntroGraphics);
            colorLayer = renderView.GetLayer(Layer.Effects);
            paletteOffset = renderView.GraphicProvider.FirstFantasyIntroPaletteIndex;

            EnsureTextures(renderView, fantasyIntroData);

            backgroundLeftBorder = renderView.SpriteFactory.Create(45, 256, true, 1) as ILayerSprite;
            backgroundLeftBorder.Layer = renderLayer;
            backgroundLeftBorder.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            backgroundLeftBorder.TextureAtlasOffset = textureAtlas.GetOffset(borderTextureIndexOffset);
            backgroundLeftBorder.X = -45;
            backgroundLeftBorder.Y = 0;
            backgroundLeftBorder.ClipArea = new Rect(-45, 0, 366, 256);
            backgroundLeftBorder.Visible = false;

            backgroundRightBorder = renderView.SpriteFactory.Create(45, 256, true, 1) as ILayerSprite;
            backgroundRightBorder.Layer = renderLayer;
            backgroundRightBorder.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            backgroundRightBorder.TextureAtlasOffset = textureAtlas.GetOffset(borderTextureIndexOffset + 1);
            backgroundRightBorder.X = 320;
            backgroundRightBorder.Y = 0;
            backgroundRightBorder.ClipArea = new Rect(-45, 0, 366, 256);
            backgroundRightBorder.Visible = false;

            background = renderView.SpriteFactory.Create(320, 256, true, 1) as ILayerSprite;
            background.Layer = renderLayer;
            background.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Background);
            background.X = 0;
            background.Y = 0;
            background.Visible = false;

            fairy = renderView.SpriteFactory.Create(64, 71, true, 7) as ILayerSprite;
            fairy.Layer = renderLayer;
            fairy.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Fairy);
            fairy.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Fairy);
            fairy.ClipArea = new Rect(0, 0, 320, 256);
            fairy.Visible = false;

            writing = renderView.SpriteFactory.Create(0, 83, true, 4) as ILayerSprite; // width will be increased later up to 208
            writing.Layer = renderLayer;
            writing.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Writing);
            writing.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Writing);
            writing.X = 64;
            writing.Y = 146;
            writing.Visible = false;

            fadeArea = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Color.Black, 255);
            fadeArea.Layer = colorLayer;
            fadeArea.X = 0;
            fadeArea.Y = 0;
            fadeArea.Visible = true; // start with a black screen

            // Note: all colors beside the background graphic use the
            // first palette so we just use paletteOffset here.
            var colorPaletteData = renderView.GraphicProvider.Palettes[paletteOffset].Data;
            for (int i = 0; i < 32; ++i)
            {
                // Index 0 is transparent and 16 too as Amiga sprites can only use the upper 16 colors.
                if (i % 16 == 0)
                    colors[i] = Color.Transparent;
                else
                    colors[i] = new Color(colorPaletteData[i * 4 + 0], colorPaletteData[i * 4 + 1], colorPaletteData[i * 4 + 2]);
            }

            actions = fantasyIntroData.Actions;
        }

        public bool Active { get; private set; }

        private byte GetPaletteIndex(FantasyIntroGraphic fantasyIntroGraphic)
        {
            return (byte)(paletteOffset + FantasyIntroData.GraphicPalettes[fantasyIntroGraphic] - 1);
        }

        private Position GetFairyFrameTextureOffset()
        {
            var position = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Fairy);
            position.X += (int)(fairyAnimationIndex % 6) * 64;
            position.Y += (int)(fairyAnimationIndex / 6) * 71;
            return position;
        }

        private IColoredRect EnsureSparkLine(int index)
        {
            if (!sparkLines.TryGetValue(index, out var sparkLine))
            {
                sparkLine = renderView.ColoredRectFactory.Create(2, 1, Color.Transparent, 10);
                sparkLine.Layer = colorLayer;
                sparkLines.Add(index, sparkLine);
            }

            return sparkLine;
        }

        private IColoredRect EnsureSparkDot(int index)
        {
            if (!sparkDots.TryGetValue(index, out var sparkDot))
            {
                sparkDot = renderView.ColoredRectFactory.Create(1, 1, Color.Transparent, 10);
                sparkDot.Layer = colorLayer;
                sparkDots.Add(index, sparkDot);
            }

            // Dots and sparks share an index
            if (sparks.TryGetValue(index, out var spark))
            {
                spark.Delete();
                sparks.Remove(index);
            }

            return sparkDot;
        }

        private ILayerSprite EnsureSpark(int index)
        {
            if (!sparks.TryGetValue(index, out var spark))
            {
                spark = renderView.SpriteFactory.Create(16, 5, true, 10) as ILayerSprite;
                spark.Layer = renderLayer;
                spark.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.FairySparks);
                spark.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.FairySparks);
                spark.Visible = spark.X >= 0;
                sparks.Add(index, spark);
            }

            // Dots and sparks share an index
            if (sparkDots.TryGetValue(index, out var sparkDot))
            {
                sparkDot.Delete();
                sparkDots.Remove(index);
            }

            return spark;
        }

        private void UpdateSparkLine(int index, int x, int y)
        {
            EnsureSparkLine(index);
            var sparkLine = sparkLines[index];
            sparkLine.X = x;
            sparkLine.Y = y;
            sparkLine.Visible = x >= 0;
        }

        private void UpdateSparkOrDot(int index, int x, int y)
        {
            // We always enable both, sparks and dots
            EnsureSpark(index);
            EnsureSparkDot(index);

            var spark = sparks[index];
            var sparkDot = sparkDots[index];

            spark.X = sparkDot.X = x;
            spark.Y = sparkDot.Y = y;

            if (x < 0)
            {
                spark.Visible = false;
                sparkDot.Visible = false;
            }
        }

        private void UpdateSparkLineColor(int index, int colorIndex)
        {
            EnsureSparkLine(index);
            sparkLines[index].Color = colors[colorIndex & 0x1f];
        }

        private void UpdateSparkOrDotColor(int index, int colorIndex)
        {
            // We always enable both, sparks and dots
            EnsureSpark(index);
            EnsureSparkDot(index);

            sparkDots[index].Color = colors[colorIndex & 0x1f];
            // TODO: also change image color?
        }

        public void Update(double deltaTime)
        {
            if (time == 0)
            {
                Active = true;
                backgroundLeftBorder.Visible = true;
                backgroundRightBorder.Visible = true;
                background.Visible = true;
            }

            time += deltaTime;
            long frame = Util.Floor(time / 0.1); // 10 ms per frame (= 100 fps, 50 interleaved screen renders)

            lock (actions)
            {
                if (Active && actions.Count == 0)
                    Abort();
                else if (frame == lastFrame)
                    return;

                lastFrame = frame;

                if (Active)
                {
                    while (actions.Count != 0 && actions.Peek().Frames <= frame)
                    {
                        ProcessAction(frame, actions.Dequeue());
                    }
                }
            }

            if (fairy != null)
            {
                if (playFairyAnimation)
                {
                    ++fairyAnimationIndex;

                    if (fairyAnimationIndex == 3) // skip frame 3 at transition
                        fairyAnimationIndex = 4;
                    else if (fairyAnimationIndex == 23)
                        fairyAnimationIndex = 0;
                }
                else
                {
                    // While moving / idle just loop the first 4 frames.
                    fairyAnimationIndex = (fairyAnimationIndex + 1) & 0x3;
                }

                fairy.TextureAtlasOffset = GetFairyFrameTextureOffset();
            }

            if (fadeStartFrames >= 0 && frame - fadeStartFrames < 32)
            {
                byte alpha = (byte)Util.Limit(0, fadeOut ? frame * 8 : 255 - frame * 8, 255);
                fadeArea.Color = new Color(fadeArea.Color, alpha);
                fadeArea.Visible = true;
            }
            else
            {
                fadeArea.Visible = false;

                if (!Active)
                    Destroy();
            }
        }

        void ProcessAction(long frame, FantasyIntroAction action)
        {
            switch (action.Command)
            {
                case FantasyIntroCommand.FadeIn:
                {
                    Fade(frame, false);
                    break;
                }
                case FantasyIntroCommand.FadeOut:
                {
                    Fade(frame, true);
                    break;
                }
                case FantasyIntroCommand.MoveFairy:
                {
                    fairy.X = action.Parameters[0];
                    fairy.Y = action.Parameters[1];
                    fairy.Visible = true;
                    playFairyAnimation = false;
                    break;
                }
                case FantasyIntroCommand.PlayFairyAnimation:
                {
                    playFairyAnimation = true;
                    break;
                }
                case FantasyIntroCommand.AddWritingPart:
                {
                    // Each time by 4 pixels
                    writing.Resize(writing.Width + 4, writing.Height);
                    writing.Visible = true;
                    break;
                }
                case FantasyIntroCommand.DrawSparkDot:
                {
                    break;
                }
            }
        }

        void Fade(long frame, bool fadeOut)
        {
            fadeStartFrames = frame;
            this.fadeOut = fadeOut;
        }

        public void Abort()
        {
            Active = false;
        }

        private void Destroy()
        {
            Active = false;
            backgroundLeftBorder?.Delete();
            backgroundRightBorder?.Delete();
            background?.Delete();
            fairy?.Delete();
            writing?.Delete();
            fadeArea?.Delete();
            foreach (var spark in sparks)
                spark.Value?.Delete();
            foreach (var sparkLine in sparkLines)
                sparkLine.Value?.Delete();
            foreach (var sparkDot in sparkDots)
                sparkDot.Value?.Delete();
            finishAction?.Invoke();
        }

        ushort randomState = 17;

        uint Random()
        {
            uint next = randomState * 53527u;
            randomState = (ushort)(next & 0xFFFF);
            return (next >> 8) & 0x7FFFu;
        }
    }
}
