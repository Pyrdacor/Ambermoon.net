using Ambermoon.Data;
using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

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
            TextCommands,
            SpawnZoomObjects,
            TwinlakeAnimation,
            ColorEffect
        }

        private abstract class IntroAction
        {
            protected IntroAction(IntroActionType type)
            {
                Type = type;
            }

            public IntroActionType Type { get; }
            public abstract void Update(long ticks, int frameCounter);
            public abstract void Destroy();

            public static IntroAction CreateAction(IntroActionType actionType, IRenderView renderView, long startTicks, IIntroData introData, Func<int, int, int> rng, Font introFont, Font introFontLarge)
            {
                return actionType switch
                {
                    IntroActionType.Starfield => new IntroActionStarfield(renderView, startTicks, rng),
                    IntroActionType.SpawnZoomObjects => new IntroActionZoomingObjects(renderView, startTicks),
                    IntroActionType.TwinlakeAnimation => new IntroActionTwinlake(renderView, startTicks, introData),
                    IntroActionType.TextCommands => new IntroActionTextCommands(renderView, startTicks, introData, introFont),
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

            public override void Update(long ticks, int frameCounter)
            {
                // TODO
            }

            public override void Destroy()
            {
                // TODO
            }
        }

        private class IntroActionTextCommands : IntroAction
        {
            private readonly IRenderView renderView;
            private readonly IIntroData introData;
            private readonly List<Text> texts = new();
            private readonly Queue<IIntroTextCommand> commands = new();
            private readonly long startTicks;
            private readonly Font introFont;
            private readonly int lineHeight;
            private Data.Enumerations.Color currentTextColor = Data.Enumerations.Color.Black;
            private long waitEndTicks = -1;

            // To avoid the need for additional palettes just to mimic the dynamic
            // text coloring of the Amiga version, we just map the colors to an
            // appropriate index in the primary UI palette.
            // We know which colors are possible anyway.
            private readonly Dictionary<int, Data.Enumerations.Color> colorMapping = new()
            {
                { 0x0000, Data.Enumerations.Color.Black },
                { 0x0ccc, Data.Enumerations.Color.BrightGray }, // it almost fits with ccb instead of ccc
                { 0x0e92, Data.Enumerations.Color.LightOrange } // it almost fits with f90 instead of e92
            };

            public IntroActionTextCommands(IRenderView renderView, long startTicks, IIntroData introData, Font introFont)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                this.introFont = introFont;
                this.introData = introData;
                lineHeight = introFont.GlyphGraphics.Values.Select(g => g.Height).Max();

                foreach (var command in introData.TextCommands)
                    commands.Enqueue(command);
            }

            public override void Update(long ticks, int frameCounter)
            {
                if (waitEndTicks > ticks)
                    return;

                if (commands.Count != 0)
                {
                    var nextCommand = commands.Dequeue();

                    switch (nextCommand.Type)
                    {
                        case IntroTextCommandType.Clear:
                            texts.ForEach(text => text?.Destroy());
                            texts.Clear();
                            break;
                        case IntroTextCommandType.Add:
                            // Texts are displayed in the lower area starting at Y=200
                            AddText(nextCommand.Args[0], 200 + nextCommand.Args[1], nextCommand.Args[2]);
                            break;
                        case IntroTextCommandType.Render:
                            texts.ForEach(text => text.Visible = true);
                            break;
                        case IntroTextCommandType.Wait:
                            waitEndTicks = ticks + nextCommand.Args[0];
                            break;
                        case IntroTextCommandType.SetTextColor:
                            if (!colorMapping.TryGetValue(nextCommand.Args[0], out currentTextColor))
                                throw new AmbermoonException(ExceptionScope.Data, "Unsupported intro text color.");
                            texts.ForEach(text => text.TextColor = currentTextColor);
                            break;
                        case IntroTextCommandType.Unknown:
                            // TODO
                            break;
                        default:
                            throw new AmbermoonException(ExceptionScope.Data, "Unsupported intro text command.");
                    }
                }
                
            }

            private void AddText(int x, int y, int index)
            {
                // The clip area is important. Otherwise the virtual screen is used which is only 320x200 and the text only starts at Y = 200.
                var text = introFont.CreateText(renderView, Layer.IntroText, new Rect(), introData.TextCommandTexts[index], 200, TextAlign.Left, 255, new Rect(0, 200, 320, 256));
                text.Visible = false;
                text.Place(new Rect(x, y, 320 - x, lineHeight), TextAlign.Left);
                text.TextColor = currentTextColor;
                texts.Add(text);
            }

            public override void Destroy()
            {
                texts.ForEach(text => text?.Destroy());
                texts.Clear();
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

            private readonly struct TownShowInfo
            {
                public int ZoomLevel { get; init; }
                public int Duration { get; init; }
            }

            // This is stored between code in the ambermoon_intro.
            // If the current zoom is above the given zoom, the town
            // image and text is shown for the given duration.
            private static readonly TownShowInfo[] TownShowInfos = new TownShowInfo[3]
            {
                new TownShowInfo { ZoomLevel= 0x52f0, Duration = 200 }, // Gemstone
                new TownShowInfo { ZoomLevel= 0x54c4, Duration = 150 }, // Illien
                new TownShowInfo { ZoomLevel= 0x5654, Duration = 100 }, // Snakesign
            };

            // Sun is using animationFrameCounter / 4 to get the frame index

            private readonly IRenderView renderView;
            private readonly ITextureAtlas textureAtlas;
            private readonly ILayerSprite[] objects = new ILayerSprite[5];
            private readonly IAnimatedLayerSprite[] meteorSparks = new IAnimatedLayerSprite[2];
            private readonly ILayerSprite town;
            private readonly IColoredRect black;
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
            private int meteorSparkFrameCounter = -1;
            private int currentTownIndex = -1;
            private long currentTownStartTicks = -1;

            public IntroActionZoomingObjects(IRenderView renderView, long startTicks)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                lastTicks = startTicks;
                var layer = renderView.GetLayer(Layer.IntroGraphics);
                textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);
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
                    meteorSparks[i].X = 48 + i * 144;
                    meteorSparks[i].Y = 153;
                }

                town = renderView.SpriteFactory.Create(160, 128, true, 150) as ILayerSprite;
                town.Layer = layer;
                town.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.Gemstone);
                town.PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + IntroData.GraphicPalettes[IntroGraphic.Gemstone] - 1);
                town.Visible = false;

                black = renderView.ColoredRectFactory.Create(320, 256, Color.Black, 120);
                black.Layer = layer;
                black.X = 0;
                black.Y = 0;
                black.Visible = false;

                // TODO: text
            }

            public override void Update(long ticks, int frameCounter)
            {
                CheckTownDisplay(ticks);

                if (!town.Visible)
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
                }
                else
                {
                    foreach (var obj in objects)
                    {
                        obj.Visible = false;
                    }

                    meteorSparks[0].Visible = false;
                    meteorSparks[1].Visible = false;
                }

                lastTicks = ticks;
            }

            public override void Destroy()
            {
                foreach (var obj in objects)
                    obj?.Delete();

                foreach (var spark in meteorSparks)
                    spark?.Delete();

                town?.Delete();
            }

            private void CheckTownDisplay(long ticks)
            {
                if (currentTownIndex == 3)
                    return;

                int nextTownIndex = currentTownIndex + 1;

                if (nextTownIndex < 3 && currentZoom >= TownShowInfos[nextTownIndex].ZoomLevel)
                {
                    currentTownIndex = nextTownIndex;
                    currentTownStartTicks = ticks;
                    town.X = 80;
                    town.Y = 10; // TODO
                    town.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.Gemstone + (uint)currentTownIndex);
                    town.Visible = true;
                    black.Visible = true;
                }
                else if (currentTownIndex >= 0)
                {
                    town.Visible = TownShowInfos[currentTownIndex].Duration > (ticks - currentTownStartTicks);
                    black.Visible = town.Visible;

                    if (nextTownIndex == 3 && !town.Visible)
                        currentTownIndex = 3;

                    if (!town.Visible)
                    {
                        for (int i = 0; i < objects.Length; i++)
                        {
                            if (i != SunObjectIndex)
                                objects[i].Visible = true;
                        }
                    }
                }
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

                    if (meteorSparkFrameCounter != -1)
                    {
                        if (++meteorSparkFrameCounter == 60)
                            meteorSparkFrameCounter = 28;
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

        private class IntroActionTwinlake : IntroAction
        {
            private readonly IRenderView renderView;
            private readonly ITextureAtlas textureAtlas;
            private readonly long startTicks;
            private readonly ILayerSprite frame;
            private readonly ILayerSprite[] images = new ILayerSprite[95];
            private int activeFrame = -1;

            public IntroActionTwinlake(IRenderView renderView, long startTicks, IIntroData introData)
                : base(IntroActionType.Starfield)
            {
                this.renderView = renderView;
                this.startTicks = startTicks;
                var layer = renderView.GetLayer(Layer.IntroGraphics);
                textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.IntroGraphics);
                byte paletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + IntroData.GraphicPalettes[IntroGraphic.Twinlake] - 1);
                uint partAtlasOffset = (uint)introData.Graphics.Keys.Max();

                frame = renderView.SpriteFactory.Create(288, 200, true, 0) as ILayerSprite;
                frame.Layer = layer;
                frame.TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.Frame);
                frame.PaletteIndex = (byte)(renderView.GraphicProvider.FirstIntroPaletteIndex + IntroData.GraphicPalettes[IntroGraphic.Frame] - 1);
                frame.X = 16;
                frame.Y = 0;
                frame.Visible = true;

                images[0] = renderView.SpriteFactory.Create(256, 177, true, 20) as ILayerSprite;
                images[0].Layer = layer;
                images[0].TextureAtlasOffset = textureAtlas.GetOffset((uint)IntroGraphic.Twinlake);
                images[0].PaletteIndex = paletteIndex;
                images[0].X = 32;
                images[0].Y = 7;
                images[0].Visible = true;

                for (int i = 1; i < 95; i++)
                {
                    var twinlakePart = introData.TwinlakeImageParts[i - 1];
                    var graphic = twinlakePart.Graphic;
                    images[i] = renderView.SpriteFactory.Create(graphic.Width, graphic.Height, true, (byte)(50 + i * 2)) as ILayerSprite;
                    images[i].Layer = layer;
                    images[i].TextureAtlasOffset = textureAtlas.GetOffset(++partAtlasOffset);
                    images[i].PaletteIndex = paletteIndex;
                    images[i].X = 32 + twinlakePart.Position.X;
                    images[i].Y = 7 + twinlakePart.Position.Y;
                    images[i].Visible = false;
                }
            }

            public override void Destroy()
            {
                frame.Delete();

                foreach (var image in images)
                    image?.Delete();
            }

            public override void Update(long ticks, int frameCounter)
            {
                long elapsed = ticks - startTicks;

                if (elapsed >= 250)
                {
                    elapsed -= 250;
                    int frame = (int)(elapsed / 4);

                    if (frame < 94)
                    {
                        if (frame != activeFrame)
                        {
                            /*if (activeFrame != -1)
                                images[activeFrame + 1].Visible = false;*/

                            images[frame + 1].Visible = true;
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

            void AddAction(IntroActionType actionType)
            {
                actions.Add(IntroAction.CreateAction(actionType, renderView, 0, introData, (min, max) => 0, introFont, introFontLarge));
            }

            // TODO
            AddAction(IntroActionType.SpawnZoomObjects);
            AddAction(IntroActionType.TextCommands);
            //AddAction(IntroActionType.TwinlakeAnimation);
        }

        public void Update(double deltaTime)
        {
            long oldTicks = ticks;
            ticks += (long)Math.Round(TicksPerSecond * deltaTime);

            animationFrameCounter = (int)((animationFrameCounter + (ticks - oldTicks)) % 48);

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
                action.Update(ticks, animationFrameCounter);
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
