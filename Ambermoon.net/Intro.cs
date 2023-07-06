using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal class Intro
    {
        private enum IntroActionType
        {
            Starfield,
            LogoFlyIn,
            LogoFadeOut,
            AmbermoonFlyIn,
            AmbermoonFadeOut,
            ShowText,
            TextFadeOut,
            SpawnZoomObjects,
            ShowImage,
            HideImage,
            ColorEffect
        }

        private abstract class IntroAction
        {
            protected IntroAction(IntroActionType type)
            {
                Type = type;
            }

            public IntroActionType Type { get; }
            public abstract void Update(uint ticks, int frameCounter);

            public IntroAction CreateAction(IntroActionType actionType, IRenderView renderView, uint startTicks, Func<int, int, int> rng)
            {
                return actionType switch
                {
                    IntroActionType.Starfield => new IntroActionStarfield(renderView, startTicks, rng),
                    IntroActionType.SpawnZoomObjects => new IntroActionZoomingObjects(renderView, startTicks, rng),
                    _ => throw new NotImplementedException()
                };
            }
        }

        private class IntroActionStarfield : IntroAction
        {
            private readonly IRenderView renderView;
            private readonly Queue<IColoredRect> stars = new();
            private readonly Queue<int> starTicks = new();
            private readonly uint startTicks;
            private readonly Func<int, int, int> rng;

            public IntroActionStarfield(IRenderView renderView, uint startTicks, Func<int, int, int> rng)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                this.duration = duration;
                this.rng = rng;
            }

            private void SpawnStar()
            {
                stars.Enqueue(renderView.ColoredRectFactory.Create(1, 1, Color.White, 10));
                starTicks.Enqueue(rng(10, 20));
            }

            public override void Update(uint ticks, int frameCounter)
            {
                // TODO
            }
        }

        private class IntroActionZoomingObjects : IntroAction
        {
            private readonly struct ZoomInfo
            {
                public int EndOffsetX { get; init; }
                public int EndOffsetY { get; init; }
                public int InitialDistance { get; init; }
                public int ImageWidth { get; init; }
                public int ImageHeight { get; init; }
                public int ZoomToWidth { get; init; }
                public int ZoomToHeight { get; init; }
                public IntroGraphic IntroGraphic { get; init; }
            }

            // This is located at the end of the second last data hunk in the ambermoon_intro.
            // There might be a zero word after it. The section starts with the word 00 05
            // which gives the amount of zoom infos.
            // Lyramion, Morag, Forest Moon, Meteor and Sun
            private static readonly ZoomInfo[] ZoomInfos = new ZoomInfo[5]
            {
                new ZoomInfo
                {
                    EndOffsetX = -136,
                    EndOffsetY = -24,
                    InitialDistance = 22000,
                    ImageWidth = 128,
                    ImageHeight = 128,
                    ZoomToWidth = 512,
                    ZoomToHeight = 512,
                    IntroGraphic = IntroGraphic.Lyramion,
                },
                new ZoomInfo
                {
                    EndOffsetX = -256,
                    EndOffsetY = 70,
                    InitialDistance = 21700,
                    ImageWidth = 64,
                    ImageHeight = 64,
                    ZoomToWidth = 256,
                    ZoomToHeight = 256,
                    IntroGraphic = IntroGraphic.Morag,
                },
                new ZoomInfo
                {
                    EndOffsetX = 200,
                    EndOffsetY = -150,
                    InitialDistance = 21500,
                    ImageWidth = 64,
                    ImageHeight = 64,
                    ZoomToWidth = 100,
                    ZoomToHeight = 100,
                    IntroGraphic = IntroGraphic.ForestMoon,
                },
                new ZoomInfo
                {
                    EndOffsetX = -18,
                    EndOffsetY = -210,
                    InitialDistance = 19000,
                    ImageWidth = 96,
                    ImageHeight = 88,
                    ZoomToWidth = 288,
                    ZoomToHeight = 264,
                    IntroGraphic = IntroGraphic.Meteor,
                },
                new ZoomInfo
                {
                    EndOffsetX = -300,
                    EndOffsetY = -32,
                    InitialDistance = 8000,
                    ImageWidth = 64,
                    ImageHeight = 64,
                    ZoomToWidth = 512,
                    ZoomToHeight = 512,
                    IntroGraphic = IntroGraphic.SunAnimation,
                }
            };

            // This is stored between code in the ambermoon_intro.
            // If the current zoom is below MaxZoom, the zoom is
            // increased by Increase each tick. If MaxZoom is exceeded,
            // the next ZoomTransitionInfo is used.
            private record ZoomTransitionInfo(int Increase, int MaxZoom);

            private static readonly ZoomTransitionInfo[] ZoomTransitionInfos = new ZoomTransitionInfo[16]
            {
                new ZoomTransitionInfo(0x10, 0x4268),
                new ZoomTransitionInfo(0x0c, 0x42cc),
                new ZoomTransitionInfo(0x0a, 0x4330),
                new ZoomTransitionInfo(0x08, 0x4394),
                new ZoomTransitionInfo(0x07, 0x43f8),
                new ZoomTransitionInfo(0x06, 0x445c),
                new ZoomTransitionInfo(0x05, 0x44c0),
                new ZoomTransitionInfo(0x04, 0x4524),
                new ZoomTransitionInfo(0x03, 0x4588),
                new ZoomTransitionInfo(0x02, 0x45ec),
                new ZoomTransitionInfo(0x01, 0x4650),
                new ZoomTransitionInfo(0x00, 0x4742), // In this case there is special handling
                new ZoomTransitionInfo(0x01, 0x477c),
                new ZoomTransitionInfo(0x02, 0x47e0),
                new ZoomTransitionInfo(0x03, 0x4a38),
                new ZoomTransitionInfo(0x04, 0x4a9c),
            };

            // Sun is using animationFrameCounter / 4 to get the frame index

            private readonly IRenderView renderView;
            private readonly ILayerSprite[] objects = new ILayerSprite[5];
            private readonly uint startTicks;
            // TODO: somewhere the zoom is also set to 14000
            private int currentZoom = 7000; // start value
            private int zoomWaitCounter = -1;
            private const int MaxZoom = 22248;
            private uint lastTicks = 0;
            private int zoomTransitionInfoIndex = 0;

            public IntroActionZoomingObjects(IRenderView renderView, uint startTicks, Func<int, int, int> _)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                lastTicks = startTicks;
                var layer = renderView.GetLayer(Layer.IntroGraphics);
                int textureAtlasWidth = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics).Texture.Width;

                // Note: The objects are ordered in render order so the
                // last one is drawn over the others. Thus we use the
                // index for the display layer and multiply it by 50.
                for (int i = 0; i < 5; i++)
                {
                    var info = ZoomInfos[i];
                    objects[i] = i == 4
                        ? renderView.SpriteFactory.CreateAnimated(0, 0, textureAtlasWidth, 12, true, (byte)(i * 50)) as IAnimatedLayerSprite
                        : renderView.SpriteFactory.Create(0, 0, true, (byte)(i * 50)) as ILayerSprite;
                    objects[i].TextureSize = new Size(info.ImageWidth, info.ImageHeight);
                    objects[i].Layer = layer;
                    objects[i].ClipArea = new Rect(0, 0, 320, 200);
                    objects[i].Visible = false;
                }
            }

            public override void Update(uint ticks, int frameCounter)
            {
                // Update the sun frame
                (objects[4] as IAnimatedLayerSprite).CurrentFrame = (uint)frameCounter / 4;

                ProcessTicks(ticks - lastTicks);
                lastTicks = ticks;
            }

            private void ProcessTicks(uint ticks)
            {
                for (int i = 0; i < ticks; i++)
                {
                    if (zoomWaitCounter != 0)
                    {
                        if (zoomWaitCounter < 0)
                        {
                            var zoomTransition = ZoomTransitionInfos[zoomTransitionInfoIndex];

                            while (currentZoom >= zoomTransition.MaxZoom && zoomTransitionInfoIndex < 15)
                            {
                                zoomTransition = ZoomTransitionInfos[++zoomTransitionInfoIndex];
                            }

                            if (zoomTransition.Increase == 0)
                            {
                                zoomWaitCounter = 150;
                                ++zoomTransitionInfoIndex;
                            }
                            else
                            {
                                currentZoom += zoomTransition.Increase;
                            }
                        }
                        else
                        {
                            if (--zoomWaitCounter == 0)
                                zoomWaitCounter = -1;
                        }
                    }

                    for (int n = 0; n < 5; n++)
                    {
                        var info = ZoomInfos[n];
                        int distance = info.InitialDistance - currentZoom;
                        // TODO
                    }
                }
            }
        }

        static ITextureAtlas textureAtlas = null;
        readonly Action finishAction;
        readonly IIntroData introData;
        readonly Font introFont;
        readonly Font introFontLarge;
        readonly IRenderView renderView;
        readonly IRenderLayer renderLayer;
        long ticks = 0;
        IReadOnlyList<OutroAction> actions = null;
        int actionIndex = 0;
        int scrolledAmount = 0;
        long scrollStartTicks = 0;
        long nextActionTicks = 0;
        readonly ILayerSprite picture;
        readonly IReadOnlyDictionary<uint, OutroGraphicInfo> graphicInfos;
        readonly byte paletteOffset;
        bool waitForClick = false;
        readonly List<Text> texts = new List<Text>();
        readonly IColoredRect fadeArea;
        Action fadeMidAction = null;
        long fadeStartTicks = 0;
        const long HalfFadeDurationInTicks = 3 * Game.TicksPerSecond / 4;
        const double TicksPerSecond = 60.0; // or test with 50 if not ok
        int animationFrameCounter = 0;

        static void EnsureTextures(IRenderView renderView, IOutroData outroData, Font outroFont, Font outroFontLarge)
        {
            if (textureAtlas == null)
            {
                TextureAtlasManager.Instance.AddFromGraphics(Layer.OutroGraphics,
                    outroData.Graphics.Select((g, i) => new { Graphic = g, Index = i }).ToDictionary(g => (uint)g.Index, g => g.Graphic));
                textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.OutroGraphics);
                renderView.GetLayer(Layer.OutroGraphics).Texture = textureAtlas.Texture;
                TextureAtlasManager.Instance.AddFromGraphics(Layer.OutroText, outroFont.GlyphGraphics);
                TextureAtlasManager.Instance.AddFromGraphics(Layer.OutroText, outroFontLarge.GlyphGraphics);
                renderView.GetLayer(Layer.OutroText).Texture = TextureAtlasManager.Instance.GetOrCreate(Layer.OutroText).Texture;
            }
        }

        public Intro(IRenderView renderView, IOutroData outroData, Font outroFont, Font outroFontLarge, Action finishAction)
        {
            this.finishAction = finishAction;
            this.introData = outroData;
            this.introFont = outroFont;
            this.introFontLarge = outroFontLarge;
            this.renderView = renderView;
            renderLayer = renderView.GetLayer(Layer.OutroGraphics);
            picture = renderView.SpriteFactory.Create(160, 128, true, 1) as ILayerSprite;
            picture.Layer = renderLayer;
            picture.PaletteIndex = paletteOffset = renderView.GraphicProvider.FirstOutroPaletteIndex;
            picture.Visible = false;

            fadeArea = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Color.Black, 255);
            fadeArea.Layer = renderView.GetLayer(Layer.Effects);
            fadeArea.X = 0;
            fadeArea.Y = 0;
            fadeArea.Visible = false;

            graphicInfos = outroData.GraphicInfos;

            EnsureTextures(renderView, outroData, outroFont, outroFontLarge);
        }

        public bool Active { get; private set; }

        public void Start(Savegame savegame)
        {
            ticks = 0;
            Active = true;
            actionIndex = 0;
            scrolledAmount = 0;
            scrollStartTicks = 0;
            nextActionTicks = 0;
            waitForClick = false;
            speedIndex = 2;

            var option = OutroOption.ValdynNotInParty;

            if (savegame.CurrentPartyMemberIndices.Contains(12u)) // Valdyn in party
            {
                if (savegame.IsGameOptionActive(Data.Enumerations.Option.FoundYellowSphere))
                    option = OutroOption.ValdynInPartyWithYellowSphere;
                else
                    option = OutroOption.ValdynInPartyNoYellowSphere;
            }

            actions = introData.OutroActions[option];

            Process();
        }

        public void Update(double deltaTime)
        {
            long oldTicks = ticks;
            ticks += (long)Math.Round(TicksPerSecond * deltaTime);

            animationFrameCounter = (int)((animationFrameCounter + (ticks - oldTicks)) % 48);

            /*if (fadeArea.Visible || fadeMidAction != null)
            {
                long fadeDuration = ticks - fadeStartTicks;

                if (fadeDuration >= HalfFadeDurationInTicks * 2)
                {
                    fadeMidAction = null;
                    fadeArea.Visible = false;
                    nextActionTicks = ticks;
                }
                else
                {
                    byte alpha = (byte)(255 - Math.Abs(HalfFadeDurationInTicks - fadeDuration) * 255 / HalfFadeDurationInTicks);
                    fadeArea.Color = new Color(fadeArea.Color, alpha);
                    fadeArea.Visible = true;

                    if (fadeMidAction != null && fadeDuration >= HalfFadeDurationInTicks)
                    {
                        fadeMidAction?.Invoke();
                        fadeMidAction = null;
                    }

                    return;
                }
            }*/


            Process();
        }

        public void Click(bool right)
        {
            if (!waitForClick)
            {
                ToggleSpeed(!right);
                return;
            }

            waitForClick = false;
            nextActionTicks = 0; // this ensures immediate processing of next action
        }

        void ToggleSpeed(bool up)
        {
            if (up)
            {
                if (speedIndex == PixelScrollPerSecond.Length - 1)
                    return;

                ++speedIndex;
            }
            else // down
            {
                if (speedIndex == 0)
                    return;

                --speedIndex;
            }

            if (speedIndex == 0)
                return; // paused

            double pixelsPerTick = PixelScrollPerSecond[speedIndex] / Game.TicksPerSecond;
            long scrollTicks = (long)Math.Round((actions[actionIndex - 1].ScrollAmount - scrolledAmount) / pixelsPerTick);
            scrolledAmount = 0;
            scrollStartTicks = ticks;
            nextActionTicks = ticks + scrollTicks;
        }

        void Scroll()
        {
            long scrollTicks = ticks - scrollStartTicks;
            double pixelsPerTick = PixelScrollPerSecond[speedIndex] / Game.TicksPerSecond;
            int scrollAmount = (int)Math.Round(scrollTicks * pixelsPerTick);
            int delta = scrollAmount - scrolledAmount;

            if (delta == 0)
                return;

            scrolledAmount = scrollAmount;
            
            foreach (var text in texts.ToList())
            {
                text.Move(0, -delta);

                if (!text.OnScreen) // not on screen anymore
                {
                    text.Destroy();
                    texts.Remove(text);
                }
            }
        }

        void Process()
        {
            if (waitForClick || fadeMidAction != null || speedIndex == 0)
                return;

            if (nextActionTicks > ticks)
            {
                Scroll();
                return;
            }

            if (actionIndex == actions.Count)
            {
                Active = false;
                finishAction?.Invoke();
                return;
            }

            var action = actions[actionIndex];

            switch (action.Command)
            {
                case OutroCommand.ChangePicture:
                {
                    Fade(() =>
                    {
                        texts.ForEach(text => text.Destroy());
                        texts.Clear();
                        var graphicInfo = graphicInfos[action.ImageOffset.Value];
                        picture.PaletteIndex = (byte)(paletteOffset + graphicInfo.PaletteIndex - 1);
                        picture.TextureAtlasOffset = textureAtlas.GetOffset(graphicInfo.GraphicIndex);
                        picture.Resize(graphicInfo.Width, graphicInfo.Height);
                        picture.X = (Global.VirtualScreenWidth - graphicInfo.Width) / 2;
                        picture.Y = (Global.VirtualScreenHeight - graphicInfo.Height) / 2;
                        picture.Visible = true;
                        ++actionIndex;
                    });
                    break;
                }
                case OutroCommand.WaitForClick:
                {
                    ++actionIndex;
                    waitForClick = true;
                    break;
                }
                case OutroCommand.PrintTextAndScroll:
                {
                    if (action.TextIndex != null)
                        PrintText(action.TextDisplayX, introData.Texts[action.TextIndex.Value], action.LargeText);
                    double pixelsPerTick = PixelScrollPerSecond[speedIndex] / Game.TicksPerSecond;
                    long scrollTicks = (long)Math.Round(action.ScrollAmount / pixelsPerTick);
                    ++actionIndex;
                    scrolledAmount = 0;
                    scrollStartTicks = ticks;
                    nextActionTicks = ticks + scrollTicks;
                    break;
                }
            }
        }

        void Fade(Action midAction)
        {
            fadeStartTicks = ticks;
            fadeMidAction = midAction;
        }

        void PrintText(int x, string text, bool large)
        {
            Text textEntry;

            if (large)
            {
                textEntry = introFontLarge.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight - 1, Global.VirtualScreenWidth, 22), text, 10, TextAlign.Left, 208);
            }
            else
            {
                textEntry = introFont.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight - 1, Global.VirtualScreenWidth, 11), text, 10, TextAlign.Left, 208);
            }

            textEntry.Visible = true;

            texts.Add(textEntry);
        }

        public void Abort()
        {
            if (Active)
            {
                Active = false;
                finishAction?.Invoke();
            }
        }

        public void Destroy()
        {
            Active = false;
            picture.Visible = false;
            texts.ForEach(text => text.Destroy());
            texts.Clear();
            fadeArea.Visible = false;
        }
    }

    internal class OutroFactory : IOutroFactory
    {
        readonly IRenderView renderView;
        readonly IOutroData outroData;
        readonly Font outroFont;
        readonly Font outroFontLarge;

        public OutroFactory(IRenderView renderView, IOutroData outroData, Font outroFont, Font outroFontLarge)
        {
            this.renderView = renderView;
            this.outroData = outroData;
            this.outroFont = outroFont;
            this.outroFontLarge = outroFontLarge;
        }

        public IOutro Create(Action finishAction) => new Outro(renderView, outroData, outroFont, outroFontLarge, finishAction);
    }
}
