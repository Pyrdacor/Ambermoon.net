using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal class FantasyIntro
    {
        static ITextureAtlas textureAtlas = null;
        readonly Action finishAction;
        readonly IGameRenderView renderView;
        readonly IRenderLayer renderLayer;
        readonly IRenderLayer colorLayer;
        readonly Queue<FantasyIntroAction> actions = null;
        readonly ILayerSprite backgroundLeftBorder;
        readonly ILayerSprite backgroundRightBorder;
        readonly ILayerSprite background;
        readonly ILayerSprite fairy;
        readonly ILayerSprite writing;
        readonly Dictionary<int, ILayerSprite> writingSparks = new(400);
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

        static void EnsureTextures(IGameRenderView renderView, IFantasyIntroData fantasyIntroData)
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

        public FantasyIntro(IGameRenderView renderView, IFantasyIntroData fantasyIntroData, Action finishAction)
        {
            this.finishAction = finishAction;
            this.renderView = renderView;
            renderLayer = renderView.GetLayer(Layer.FantasyIntroGraphics);
            colorLayer = renderView.GetLayer(Layer.FantasyIntroEffects);
            paletteOffset = renderView.GraphicProvider.FirstFantasyIntroPaletteIndex;

            EnsureTextures(renderView, fantasyIntroData);

            var extendedScreenArea = new Rect(-45, 0, 410, 256);

            backgroundLeftBorder = renderView.SpriteFactory.Create(45, 256, true, 1) as ILayerSprite;
            backgroundLeftBorder.Layer = renderLayer;
            backgroundLeftBorder.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            backgroundLeftBorder.TextureAtlasOffset = textureAtlas.GetOffset(borderTextureIndexOffset);
            backgroundLeftBorder.X = -45;
            backgroundLeftBorder.Y = 0;
            backgroundLeftBorder.ClipArea = extendedScreenArea;
            backgroundLeftBorder.Visible = false;

            backgroundRightBorder = renderView.SpriteFactory.Create(45, 256, true, 1) as ILayerSprite;
            backgroundRightBorder.Layer = renderLayer;
            backgroundRightBorder.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            backgroundRightBorder.TextureAtlasOffset = textureAtlas.GetOffset(borderTextureIndexOffset + 1);
            backgroundRightBorder.X = 320;
            backgroundRightBorder.Y = 0;
            backgroundRightBorder.ClipArea = extendedScreenArea;
            backgroundRightBorder.Visible = false;

            background = renderView.SpriteFactory.Create(320, 256, true, 1) as ILayerSprite;
            background.Layer = renderLayer;
            background.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Background);
            background.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Background);
            background.X = 0;
            background.Y = 0;
            background.Visible = false;

            fairy = renderView.SpriteFactory.Create(64, 71, true, 50) as ILayerSprite;
            fairy.Layer = renderLayer;
            fairy.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Fairy);
            fairy.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Fairy);
            fairy.ClipArea = extendedScreenArea;
            fairy.Visible = false;

            writing = renderView.SpriteFactory.Create(0, 83, true, 10) as ILayerSprite; // width will be increased later up to 208
            writing.Layer = renderLayer;
            writing.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.Writing);
            writing.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Writing);
            writing.X = 64;
            writing.Y = 146;
            writing.Visible = false;

            fadeArea = renderView.ColoredRectFactory.Create(extendedScreenArea.Width, extendedScreenArea.Height, Color.Black, 255);
            fadeArea.Layer = colorLayer;
            fadeArea.X = -45;
            fadeArea.Y = -0;
            fadeArea.ClipArea = extendedScreenArea.CreateModified(-1, -1, 2, 2);
            fadeArea.Visible = false; // start with a black screen

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

            // As we have a larger viewport we let the fairy fly a bit further in the end.
            var fantasyIntroActions = new List<FantasyIntroAction>(fantasyIntroData.Actions);
            var lastFairyMovements = fantasyIntroActions.Where(a => a.Command == FantasyIntroCommand.MoveFairy).Reverse().Take(5).ToList();
            uint lastFrames = lastFairyMovements[4].Frames;
            int lastX = lastFairyMovements[4].Parameters[0];
            int lastY = lastFairyMovements[4].Parameters[1];
            int additionalFrameCount = 365 - lastX;
            var actionList = new List<FantasyIntroAction>(fantasyIntroData.Actions.Count + additionalFrameCount);

            for (int i = 0; i < 4; ++i)
                fantasyIntroActions.Remove(lastFairyMovements[i]);

            foreach (var action in fantasyIntroActions)
                actionList.Add(action);

            for (int i = 0; i < additionalFrameCount; ++i)
            {
                lastFrames += (uint)(1 + i % 2);
                ++lastX;
                if (i % 2 == 1)
                    --lastY;
                actionList.Add(new FantasyIntroAction(lastFrames, FantasyIntroCommand.MoveFairy, lastX, lastY));
            }

            actionList.Sort((a, b) => a.Frames.CompareTo(b.Frames));

            actions = new Queue<FantasyIntroAction>(actionList);
        }

        public bool Active { get; private set; }

        private byte GetPaletteIndex(FantasyIntroGraphic fantasyIntroGraphic)
        {
            return (byte)(paletteOffset + FantasyIntroData.GraphicPalettes[fantasyIntroGraphic] - 1);
        }

        private Position GetFairyFrameTextureOffset()
        {
            int textureFactor = (int)renderLayer.TextureFactor;
            var position = textureAtlas.GetOffset((uint)FantasyIntroGraphic.Fairy);
            position.X += (int)(fairyAnimationIndex % 6) * 64 * textureFactor;
            position.Y += (int)(fairyAnimationIndex / 6) * 71 * textureFactor;
            return position;
        }

        private IColoredRect EnsureSparkLine(int index)
        {
            if (!sparkLines.TryGetValue(index, out var sparkLine))
            {
                sparkLine = renderView.ColoredRectFactory.Create(2, 1, Color.Transparent, 25);
                sparkLine.Layer = colorLayer;
                sparkLine.ClipArea = new Rect(0, 0, 320, 256);
                sparkLines.Add(index, sparkLine);
            }

            return sparkLine;
        }

        private IColoredRect EnsureSparkDot(int index)
        {
            if (!sparkDots.TryGetValue(index, out var sparkDot))
            {
                sparkDot = renderView.ColoredRectFactory.Create(1, 1, Color.Transparent, 20);
                sparkDot.Layer = colorLayer;
                sparkDot.ClipArea = new Rect(0, 0, 320, 256);
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

        private ILayerSprite EnsureWritingSpark(int index)
        {
            if (!writingSparks.TryGetValue(index, out var spark))
            {
                spark = renderView.SpriteFactory.Create(16, 9, true, 30) as ILayerSprite;
                spark.Layer = renderLayer;
                spark.ClipArea = new Rect(0, 0, 320, 256);
                spark.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.WritingSparks);
                spark.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.WritingSparks);
                spark.Visible = false;
                writingSparks.Add(index, spark);
            }

            return spark;
        }

        private ILayerSprite EnsureSpark(int index)
        {
            if (!sparks.TryGetValue(index, out var spark))
            {
                spark = renderView.SpriteFactory.Create(16, 5, true, 30) as ILayerSprite;
                spark.Layer = renderLayer;
                spark.ClipArea = new Rect(0, 0, 320, 256);
                spark.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.FairySparks);
                spark.PaletteIndex = GetPaletteIndex(FantasyIntroGraphic.FairySparks);
                spark.Visible = false;
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

        private void UpdateWritingSpark(int index, int x, int y, int frameIndex)
        {
            var spark = EnsureWritingSpark(index);
            spark.X = x;
            spark.Y = y;
            int textureFactor = (int)spark.Layer.TextureFactor;
            spark.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.WritingSparks) + new Position(0, frameIndex * 9 * textureFactor);
            spark.Visible = true;
        }

        private void UpdateSparkLine(int index, int x, int y)
        {
            var sparkLine = EnsureSparkLine(index);
            sparkLine.X = x;
            sparkLine.Y = y;
            sparkLine.Visible = x >= 0;
        }

        private void UpdateSparkOrDot(int index, int x, int y)
        {
            if (sparks.TryGetValue(index, out var spark))
            {
                spark.X = x;
                spark.Y = y;
                spark.Visible = x >= 0;
            }
            else
            {
                var sparkDot = EnsureSparkDot(index);
                sparkDot.X = x;
                sparkDot.Y = y;
                sparkDot.Visible = x >= 0;
            }
        }

        private void UpdateSparkLineColor(int index, int colorIndex)
        {
            EnsureSparkLine(index).Color = colors[colorIndex & 0x1f];
        }

        private void UpdateSparkDotColor(int index, int colorIndex)
        {
            EnsureSparkDot(index).Color = colors[colorIndex & 0x1f];
        }

        public void Update(double deltaTime)
        {
            if (time == 0)
            {
                Active = true;
                backgroundLeftBorder.Visible = true;
                backgroundRightBorder.Visible = true;
                background.Visible = true;
                fadeArea.Visible = true;
            }

            time += deltaTime;
            long frame = Util.Floor(time / 0.02); // 20 ms per frame (= 50 interleaved screen renders)

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

            if (fairy != null && frame % 2 == 0)
            {
                if (playFairyAnimation)
                {
                    ++fairyAnimationIndex;

                    if (fairyAnimationIndex == 3) // skip frame 3 at transition
                        fairyAnimationIndex = 4;
                    else if (fairyAnimationIndex == 23)
                    {
                        fairyAnimationIndex = 0;
                        playFairyAnimation = false;
                    }
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
                long factor = frame - fadeStartFrames;
                factor = (factor * factor) / 4;
                byte alpha = (byte)Util.Limit(0, fadeOut ? factor : 255 - factor, 255);
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
                case FantasyIntroCommand.DrawSparkLine:
                {
                    int index = action.Parameters[0];
                    var sparkLine = EnsureSparkLine(index);
                    sparkLine.Visible = true;
                    break;
                }
                case FantasyIntroCommand.DrawSparkStar:
                {
                    int index = action.Parameters[0];
                    var spark = EnsureSpark(index);
                    spark.Visible = true;
                    break;
                }
                case FantasyIntroCommand.DrawSparkDot:
                {
                    int index = action.Parameters[0];
                    var sparkDot = EnsureSparkDot(index);
                    sparkDot.Visible = true;
                    break;
                }
                case FantasyIntroCommand.UpdateSparkLine:
                {
                    UpdateSparkLine(action.Parameters[0], action.Parameters[1], action.Parameters[2]);
                    break;
                }
                case FantasyIntroCommand.UpdateSparkStar:
                {
                    UpdateSparkOrDot(action.Parameters[0], action.Parameters[1], action.Parameters[2]);
                    break;
                }
                case FantasyIntroCommand.SetSparkLineColor:
                {
                    UpdateSparkLineColor(action.Parameters[0], action.Parameters[1]);
                    break;
                }
                case FantasyIntroCommand.SetSparkDotColor:
                {
                    UpdateSparkDotColor(action.Parameters[0], action.Parameters[1]);
                    break;
                }
                case FantasyIntroCommand.SetSparkStarFrame:
                {
                    int index = action.Parameters[0];
                    int frameIndex = action.Parameters[1];
                    var spark = EnsureSpark(index);
                    int textureFactor = (int)spark.Layer.TextureFactor;
                    spark.TextureAtlasOffset = textureAtlas.GetOffset((uint)FantasyIntroGraphic.FairySparks) + new Position(0, frameIndex * 5 * textureFactor);
                    break;
                }
                case FantasyIntroCommand.UpdateWritingSpark:
                {
                    int index = action.Parameters[0];
                    int x = action.Parameters[1];
                    int y = action.Parameters[2];
                    int frameIndex = action.Parameters[3];
                    if (x < 0 && writingSparks.TryGetValue(index, out var spark))
                    {
                        spark.Visible = false;
                    }
                    else if (x >= 0)
                    {
                        UpdateWritingSpark(index, x, y, frameIndex);
                    }
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
            Destroy();
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
            foreach (var writingSpark in writingSparks)
                writingSpark.Value?.Delete();
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
