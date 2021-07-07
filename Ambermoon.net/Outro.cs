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
        readonly Action finishAction;
        readonly OutroData outroData;
        readonly ISpriteFactory spriteFactory;
        readonly IRenderTextFactory renderTextFactory;
        readonly ITextProcessor textProcessor;
        long ticks = 0;
        const double PixelScrollPerSecond = 100.0;
        IReadOnlyList<OutroAction> actions = null;
        int actionIndex = 0;
        int scrolledAmount = 0;
        long scrollStartTicks = 0;
        long nextActionTicks = 0;

        public Outro(IRenderView renderView, Action finishAction)
        {
            this.finishAction = finishAction;
            outroData = new OutroData(renderView.GameData);
            spriteFactory = renderView.SpriteFactory;
            renderTextFactory = renderView.RenderTextFactory;
            textProcessor = renderView.TextProcessor;
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

        void Scroll()
        {
            long scrollTicks = ticks - scrollStartTicks;
            double pixelsPerTick = PixelScrollPerSecond / Game.TicksPerSecond;
            int scrollAmount = scrollTicks * pixelsPerTick;
        }

        void Process()
        {
            if (nextActionTicks > ticks)
            {
                Scroll();
                return;
            }

            var action = actions[actionIndex];
        }

        public void Destroy()
        {
            Active = false;
        }
    }

    internal class OutroFactory : IOutroFactory
    {
        readonly IRenderView renderView;

        public OutroFactory(IRenderView renderView)
        {
            this.renderView = renderView;
        }

        public IOutro Create(Action finishAction) => new Outro(renderView, finishAction);
    }
}
