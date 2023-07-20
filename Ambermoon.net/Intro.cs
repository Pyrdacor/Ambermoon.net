using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;

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
            public abstract void Update(long ticks, int frameCounter, int meteorSparkFrameCounter);
            public abstract void Destroy();

            public static IntroAction CreateAction(IntroActionType actionType, IRenderView renderView, long startTicks, Func<int, int, int> rng)
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
            private readonly long startTicks;
            private readonly Func<int, int, int> rng;

            public IntroActionStarfield(IRenderView renderView, long startTicks, Func<int, int, int> rng)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                this.rng = rng;
            }

            private void SpawnStar()
            {
                stars.Enqueue(renderView.ColoredRectFactory.Create(1, 1, Color.White, 10));
                starTicks.Enqueue(rng(10, 20));
            }

            public override void Update(long ticks, int frameCounter, int meteorSparkFrameCounter)
            {
                // TODO
            }

            public override void Destroy()
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
            private readonly IAnimatedLayerSprite[] meteorSparks = new IAnimatedLayerSprite[2];
            private readonly long startTicks;
            // TODO: somewhere the zoom is also set to 14000
            private int currentZoom = 0; // 7000, start value
            private int zoomWaitCounter = -1;
            private const int MaxZoom = 22248;
            private const int MeteorEndZoom = 18868;
            private const int MeteorSparkAppearZoom = 21000;
            private const int MeteorObjectIndex = 3;
            private const int SunObjectIndex = 4;
            private long lastTicks = 0;
            private int zoomTransitionInfoIndex = 0;

            public IntroActionZoomingObjects(IRenderView renderView, long startTicks, Func<int, int, int> _)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                lastTicks = startTicks;
                var layer = renderView.GetLayer(Layer.IntroGraphics);
                var textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);
                int textureAtlasWidth = textureAtlas.Texture.Width;

                // Note: The objects are ordered in render order so the
                // last one is drawn over the others. Thus we use the
                // index for the display layer and multiply it by 50.
                for (int i = 0; i < 5; i++)
                {
                    var graphicIndex = i == SunObjectIndex ? IntroGraphic.SunAnimation : IntroGraphic.Lyramion + i;
                    var info = ZoomInfos[i];
                    objects[i] = i == SunObjectIndex
                        ? renderView.SpriteFactory.CreateAnimated(info.ImageWidth, info.ImageHeight, textureAtlasWidth, 12, true, (byte)(i * 50)) as IAnimatedLayerSprite
                        : renderView.SpriteFactory.Create(info.ImageWidth, info.ImageHeight, true, (byte)(i * 50)) as ILayerSprite;
                    objects[i].TextureSize = new Size(info.ImageWidth, info.ImageHeight);
                    objects[i].Layer = layer;
                    objects[i].ClipArea = new Rect(0, 0, 320, 200);
                    objects[i].TextureAtlasOffset = textureAtlas.GetOffset((uint)graphicIndex);
                    objects[i].PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + IntroData.GraphicPalettes[graphicIndex] - 1);
                    objects[i].Visible = false;
                }

                for (int i = 0; i < 2; i++)
                {
                    meteorSparks[i] = renderView.SpriteFactory.CreateAnimated(64, 47, textureAtlasWidth, 15, true, (byte)(i * 25)) as IAnimatedLayerSprite;
                    meteorSparks[i].Layer = layer;
                    meteorSparks[i].TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.MeteorSparks);
                    meteorSparks[i].BaseFrame = (uint)i * 15;
                    meteorSparks[i].PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + IntroData.GraphicPalettes[IntroGraphic.MeteorSparks] - 1);
                    meteorSparks[i].Visible = false;
                    meteorSparks[i].X = 192 - i * 144;
                    meteorSparks[i].Y = 153;
                }
            }

            public override void Update(long ticks, int frameCounter, int meteorSparkFrameCounter)
            {
                // Update the sun frame
                (objects[SunObjectIndex] as IAnimatedLayerSprite).CurrentFrame = (uint)frameCounter / 4;

                if (meteorSparkFrameCounter == -1)
                {
                    if (currentZoom >= MeteorSparkAppearZoom)
                        meteorSparkFrameCounter = 0;
                }

                for (int i = 0; i < 2; i++)
                {
                    if (meteorSparkFrameCounter != -1)
                        meteorSparks[i].CurrentFrame = (uint)meteorSparkFrameCounter / 4;
                    
                    meteorSparks[i].Visible = meteorSparkFrameCounter != -1;
                }

                ProcessTicks(ticks - lastTicks);
                lastTicks = ticks;
            }

            public override void Destroy()
            {
                foreach (var obj in objects)
                    obj?.Delete();
            }

            private void ProcessTicks(long ticks)
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
                                currentZoom = Math.Min(currentZoom + zoomTransition.Increase, MaxZoom);
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
                        if (n == MeteorObjectIndex && currentZoom >= MeteorEndZoom)
                            continue;

                        var obj = objects[n];
                        var info = ZoomInfos[n];
                        int distance = info.InitialDistance - currentZoom;

                        if (distance >= short.MinValue)
                        {
                            distance += 256;

                            if (distance > 0 && distance <= short.MaxValue)
                            {
                                int offsetX = info.EndOffsetX * 256;
                                int offsetY = info.EndOffsetY * 256;
                                offsetX /= distance;
                                offsetY /= distance;
                                offsetX += 160;
                                offsetY = 100 - offsetY;

                                int width = info.ZoomToWidth * 256;
                                int height = info.ZoomToHeight * 256;
                                width /= distance;
                                height /= distance;

                                offsetX -= width / 2;
                                offsetY -= height / 2;

                                obj.Resize(width, height);
                                obj.X = offsetX;
                                obj.Y = offsetY;
                                obj.Visible = width >= 1.0f && height >= 1.0f;                                
                            }
                        }
                        else
                        {
                            obj.Visible = false;
                        }
                    }
                }
            }
        }

        readonly Action finishAction;
        readonly IIntroData introData;
        readonly Font introFont;
        readonly Font introFontLarge;
        readonly IRenderView renderView;
        readonly IRenderLayer renderLayer;
        long ticks = 0;
        readonly List<Text> texts = new();
        readonly List<IntroAction> actions = new();
        readonly IColoredRect fadeArea;
        Action fadeMidAction = null;
        long fadeStartTicks = 0;
        const long HalfFadeDurationInTicks = 3 * Game.TicksPerSecond / 4;
        const double TicksPerSecond = 60.0; // or test with 50 if not ok
        int animationFrameCounter = 0;
        int meteorSparkFrameCounter = -1;

        public Intro(IRenderView renderView, IIntroData introData, Font introFont, Font introFontLarge, Action finishAction)
        {
            this.finishAction = finishAction;
            this.introData = introData;
            this.introFont = introFont;
            this.introFontLarge = introFontLarge;
            this.renderView = renderView;
            renderLayer = renderView.GetLayer(Layer.IntroGraphics);

            fadeArea = renderView.ColoredRectFactory.Create(Global.VirtualScreenWidth, Global.VirtualScreenHeight, Color.Black, 255);
            fadeArea.Layer = renderView.GetLayer(Layer.Effects);
            fadeArea.X = 0;
            fadeArea.Y = 0;
            fadeArea.Visible = false;

            // TODO
            actions.Add(IntroAction.CreateAction(IntroActionType.SpawnZoomObjects, renderView, 0, (min, max) => 0));
        }

        public void Update(double deltaTime)
        {
            long oldTicks = ticks;
            ticks += (long)Math.Round(TicksPerSecond * deltaTime);

            animationFrameCounter = (int)((animationFrameCounter + (ticks - oldTicks)) % 48);
            if (meteorSparkFrameCounter != -1)
            {
                if (++meteorSparkFrameCounter == 60)
                    meteorSparkFrameCounter = 28;
            }

            if (fadeArea.Visible || fadeMidAction != null)
            {
                long fadeDuration = ticks - fadeStartTicks;

                if (fadeDuration >= HalfFadeDurationInTicks * 2)
                {
                    fadeMidAction = null;
                    fadeArea.Visible = false;
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
            }

            foreach (var action in actions)
            {
                action.Update(ticks, animationFrameCounter, meteorSparkFrameCounter);
            }
        }

        public void Click()
        {
            End();
        }

        void End()
        {
            Destroy();
            finishAction?.Invoke();
        }

        void Fade(Action midAction)
        {
            fadeStartTicks = ticks;
            fadeMidAction = midAction;
        }

        void PrintText(int x, int y, string text, bool large)
        {
            Text textEntry;

            if (large)
            {
                textEntry = introFontLarge.CreateText(renderView, Layer.OutroText,
                    new Rect(x, y, Global.VirtualScreenWidth - x, 22), text, 10, TextAlign.Left, 208);
            }
            else
            {
                textEntry = introFont.CreateText(renderView, Layer.OutroText,
                    new Rect(x, y, Global.VirtualScreenWidth - x, 11), text, 10, TextAlign.Left, 208);
            }

            textEntry.Visible = true;

            texts.Add(textEntry);
        }

        public void Destroy()
        {
            actions.ForEach(action => action.Destroy());
            actions.Clear();
            texts.ForEach(text => text.Destroy());
            texts.Clear();
            fadeArea.Delete();
        }
    }
}
