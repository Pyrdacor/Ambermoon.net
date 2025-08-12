using Ambermoon.Render;
using System;

namespace Ambermoon
{
    internal class Fader
    {
        readonly byte startAlpha;
        readonly byte endAlpha;
        readonly Color baseColor;
        IColoredRect coloredRect;
        DateTime? startTime = null;
        bool reverse = false;
        int duration = 1000;
        public bool DestroyWhenFinished { get; set; }

        public bool HasFinished { get; private set; } = false;

        event Action Finished;

        public void AttachFinishEvent(Action handler)
        {
            Finished = () =>
            {
                Finished = null;
                handler?.Invoke();
            };
        }

        public void DetachFinishEvent()
        {
            Finished = null;
        }

        public Fader(IGameRenderView renderView, byte startAlpha, byte endAlpha, byte displayLayer, bool destroyWhenFinished,
            bool initialVisible = false, Color baseColor = null)
        {
            this.startAlpha = startAlpha;
            this.endAlpha = endAlpha;
            this.baseColor = baseColor ?? Color.Black;
            coloredRect = renderView.ColoredRectFactory.Create(320, 256,
                this.baseColor, displayLayer);
            coloredRect.Layer = renderView.GetLayer(Layer.MainMenuEffects);
            coloredRect.X = 0;
            coloredRect.Y = 0;
            coloredRect.Visible = initialVisible;
            DestroyWhenFinished = destroyWhenFinished;
        }

        public void Destroy()
        {
            coloredRect?.Delete();
            coloredRect = null;
        }

        public void SetColor(Color color)
        {
            if (coloredRect != null)
                coloredRect.Color = color;
        }

        public void Start(int durationInMs, bool reverse = false)
        {
            StartAt(DateTime.Now, duration, reverse);
        }

        public void StartAt(DateTime startTime, int durationInMs, bool reverse = false)
        {
            HasFinished = false;
            this.reverse = reverse;
            this.startTime = startTime;
            duration = durationInMs;
        }

        public void Update()
        {
            if (coloredRect == null || startTime == null || startTime > DateTime.Now)
                return;

            var elapsed = (DateTime.Now - startTime.Value).TotalMilliseconds;

            if (elapsed >= duration)
            {
                if (DestroyWhenFinished)
                    Destroy();
                else
                    coloredRect.Color = new Color(baseColor, reverse ? startAlpha : endAlpha);
                startTime = null;
                HasFinished = true;
                Finished?.Invoke();
            }
            else
            {
                float factor = (float)(elapsed / duration);
                float alpha;

                if (reverse)
                {
                    float diff = factor * (startAlpha - endAlpha);
                    alpha = endAlpha + diff;
                }
                else
                {
                    float diff = factor * (endAlpha - startAlpha);
                    alpha = startAlpha + diff;
                }

                coloredRect.Color = new Color(baseColor, (byte)Util.Round(alpha));
            }
        }
    }
}
