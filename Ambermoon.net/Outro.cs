using Ambermoon.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon
{
    internal class Outro : IOutro
    {
        static ITextureAtlas textureAtlas = null;
        readonly Action finishAction;
        readonly OutroData outroData;
        readonly Font outroFont;
        readonly IRenderView renderView;
        readonly IRenderTextFactory renderTextFactory;
        readonly ITextProcessor textProcessor;
        readonly IRenderLayer renderLayer;
        long ticks = 0;
        const double PixelScrollPerSecond = 10.0;
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

        static void EnsureTextures(IRenderView renderView, OutroData outroData, Font outroFont)
        {
            if (textureAtlas == null)
            {
                TextureAtlasManager.Instance.AddFromGraphics(Layer.OutroGraphics,
                    outroData.Graphics.Select((g, i) => new { Graphic = g, Index = i }).ToDictionary(g => (uint)g.Index, g => g.Graphic));
                textureAtlas = TextureAtlasManager.Instance.GetOrCreate(Layer.OutroGraphics);
                renderView.GetLayer(Layer.OutroGraphics).Texture = textureAtlas.Texture;
                TextureAtlasManager.Instance.AddFromGraphics(Layer.OutroText, outroFont.GlyphGraphics);
            }
        }

        public Outro(IRenderView renderView, OutroData outroData, Font outroFont, Action finishAction)
        {
            this.finishAction = finishAction;
            this.outroData = outroData;
            this.outroFont = outroFont;
            this.renderView = renderView;
            renderLayer = renderView.GetLayer(Layer.OutroGraphics);
            picture = renderView.SpriteFactory.Create(160, 128, true, 1) as ILayerSprite;
            picture.Layer = renderLayer;
            picture.PaletteIndex = paletteOffset = renderView.GraphicProvider.FirstOutroPaletteIndex;
            picture.Visible = false;
            renderTextFactory = renderView.RenderTextFactory;
            textProcessor = renderView.TextProcessor;
            
            graphicInfos = outroData.GraphicInfos;

            EnsureTextures(renderView, outroData, outroFont);            
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

            var option = OutroOption.ValdynNotInParty;

            if (savegame.CurrentPartyMemberIndices.Contains(12u)) // Valdyn in party
            {
                if (false) // TODO
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
            Process();
        }

        public void Click()
        {
            waitForClick = false;
            nextActionTicks = 0; // this ensures immediate processing of next action
        }

        void Scroll()
        {
            long scrollTicks = ticks - scrollStartTicks;
            double pixelsPerTick = PixelScrollPerSecond / Game.TicksPerSecond;
            int scrollAmount = (int)Math.Round(scrollTicks * pixelsPerTick);
            int delta = scrollAmount - scrolledAmount;

            if (delta == 0)
                return;

            scrolledAmount = scrollAmount;
            
            foreach (var text in texts.ToList())
            {
                text.MoveY(-delta);

                if (!text.OnScreen) // not on screen anymore
                {
                    text.Destroy();
                    texts.Remove(text);
                }
            }
        }

        void Process()
        {
            if (waitForClick)
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
                    var graphicInfo = graphicInfos[action.ImageOffset.Value];
                    picture.PaletteIndex = (byte)(paletteOffset + graphicInfo.PaletteIndex);
                    picture.TextureAtlasOffset = textureAtlas.GetOffset(graphicInfo.GraphicIndex);
                    picture.Resize(graphicInfo.Width, graphicInfo.Height);
                    picture.X = (Global.VirtualScreenWidth - graphicInfo.Width) / 2;
                    picture.Y = (Global.VirtualScreenHeight - graphicInfo.Height) / 2;
                    picture.Visible = true;
                    ++actionIndex;
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
                    double pixelsPerTick = PixelScrollPerSecond / Game.TicksPerSecond;
                    long scrollTicks = (long)Math.Round(action.ScrollAmount / pixelsPerTick);
                    ++actionIndex;
                    scrolledAmount = 0;
                    scrollStartTicks = ticks;
                    nextActionTicks = ticks + scrollTicks;
                    break;
                }
            }
        }

        void PrintText(int x, string text, bool large)
        {
            Text textEntry;

            if (large)
            {
                // TODO
                textEntry = outroFont.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight, Global.VirtualScreenWidth, 16), text, 10, 10, '!', false);
            }
            else
            {
                textEntry = outroFont.CreateText(renderView, Layer.OutroText,
                    new Rect(x, Global.VirtualScreenHeight, Global.VirtualScreenWidth, 16), text, 10, 10, '!', false);
            }

            textEntry.Visible = true;

            texts.Add(textEntry);
        }

        public void Destroy()
        {
            Active = false;
            picture.Visible = false;
        }
    }

    internal class OutroFactory : IOutroFactory
    {
        readonly IRenderView renderView;
        readonly OutroData outroData;
        readonly Font outroFont;

        public OutroFactory(IRenderView renderView, OutroData outroData, Font outroFont)
        {
            this.renderView = renderView;
            this.outroData = outroData;
            this.outroFont = outroFont;
        }

        public IOutro Create(Action finishAction) => new Outro(renderView, outroData, outroFont, finishAction);
    }
}
