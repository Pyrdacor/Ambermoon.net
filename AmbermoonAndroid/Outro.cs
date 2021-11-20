using Ambermoon;
using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AmbermoonAndroid
{
    internal class Outro : IOutro
    {
        static ITextureAtlas textureAtlas = null;
        readonly Action finishAction;
        readonly OutroData outroData;
        readonly Font outroFont;
        readonly Font outroFontLarge;
        readonly IRenderView renderView;
        readonly IRenderLayer renderLayer;
        long ticks = 0;
        static readonly double[] PixelScrollPerSecond = new double[5] { 10.0, 20.0, 60.0, 100.0, 200.0 };
        int speedIndex = 1;
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

        static void EnsureTextures(IRenderView renderView, OutroData outroData, Font outroFont, Font outroFontLarge)
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

        public Outro(IRenderView renderView, OutroData outroData, Font outroFont, Font outroFontLarge, Action finishAction)
        {
            this.finishAction = finishAction;
            this.outroData = outroData;
            this.outroFont = outroFont;
            this.outroFontLarge = outroFontLarge;
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
            speedIndex = 1;

            var option = OutroOption.ValdynNotInParty;

            if (savegame.CurrentPartyMemberIndices.Contains(12u)) // Valdyn in party
            {
                if (savegame.IsGameOptionActive(Ambermoon.Data.Enumerations.Option.FoundYellowSphere))
                    option = OutroOption.ValdynInPartyWithYellowSphere;
                else
                    option = OutroOption.ValdynInPartyNoYellowSphere;
            }

            actions = outroData.OutroActions[option];

            Process();
        }

        public void Update(double deltaTime)
        {
            ticks += (long)Math.Round(Game.TicksPerSecond * deltaTime);

            if (fadeArea.Visible || fadeMidAction != null)
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
            }

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
            if (waitForClick || fadeMidAction != null)
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
                        PrintText(action.TextDisplayX, outroData.Texts[action.TextIndex.Value], action.LargeText);
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
                textEntry = outroFontLarge.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight - 1, Global.VirtualScreenWidth, 22), text, 10);
            }
            else
            {
                textEntry = outroFont.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight - 1, Global.VirtualScreenWidth, 11), text, 10);
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
        readonly OutroData outroData;
        readonly Font outroFont;
        readonly Font outroFontLarge;

        public OutroFactory(IRenderView renderView, OutroData outroData, Font outroFont, Font outroFontLarge)
        {
            this.renderView = renderView;
            this.outroData = outroData;
            this.outroFont = outroFont;
            this.outroFontLarge = outroFontLarge;
        }

        public IOutro Create(Action finishAction) => new Outro(renderView, outroData, outroFont, outroFontLarge, finishAction);
    }
}
