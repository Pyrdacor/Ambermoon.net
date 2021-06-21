using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.Render
{
    internal class Video
    {
        readonly IRenderView renderView;
        readonly Layer layer;
        readonly IRenderLayer renderLayer;
        readonly List<Text> texts = new List<Text>();
        readonly List<Image> images = new List<Image>();
        readonly List<FadeEffect> fadeEffects = new List<FadeEffect>();

        protected abstract class TimedEffect
        {
            public enum ExecutionType
            {
                Once,
                Looped,
                Oscillated // loop back and forth
            }

            public DateTime ExecutionTime { get; set; }
            DateTime startTime = DateTime.Now;
            public bool Started { get; private set; } = false;
            bool reversed = false;
            readonly ExecutionType executionType;
            readonly bool destroyOnFinish;
            bool destroyed = false;
            public bool Processed { get; private set; } = false;
            public event Action Finished;
            public event Action Destroyed;
            public abstract bool Visible { get; set; }

            protected TimedEffect(ExecutionType executionType = ExecutionType.Once,
                bool destroyOnFinish = true)
            {
                this.executionType = executionType;
                this.destroyOnFinish = destroyOnFinish;
            }

            public void Start()
            {
                if (destroyed)
                    throw new AmbermoonException(ExceptionScope.Application, "Destroyed timed effect can't be restarted.");

                startTime = DateTime.Now;
                Restart();
                Started = true;
                Visible = true;
            }

            public void Update()
            {
                if (Started)
                {
                    Update(DateTime.Now - startTime, reversed, out bool finished);

                    if (finished)
                    {
                        switch (executionType)
                        {
                            case ExecutionType.Once:
                                Started = false;
                                reversed = false;
                                Finished?.Invoke();
                                if (destroyOnFinish)
                                {
                                    destroyed = true;
                                    Destroy();
                                }
                                Processed = true;
                                break;
                            case ExecutionType.Looped:
                                Start();
                                break;
                            case ExecutionType.Oscillated:
                                reversed = !reversed;
                                Start();
                                break;
                        }
                    }
                }
            }

            protected virtual void Restart()
            {
                // default empy
            }

            protected abstract void Update(TimeSpan elapsed, bool reversed, out bool finished);

            protected virtual void Destroy()
            {
                Destroyed?.Invoke();
            }
        }

        protected class FadeEffect : TimedEffect
        {
            readonly byte startAlpha;
            readonly byte endAlpha;
            readonly TimeSpan duration;
            public byte Alpha { get; private set; }
            public event Action<byte> AlphaUpdated;

            public override bool Visible
            {
                get;
                set;
            }

            public FadeEffect(byte startAlpha, byte endAlpha, TimeSpan duration,
                ExecutionType executionType = ExecutionType.Once, bool destroyAfterFade = true)
                : base(executionType, destroyAfterFade)
            {
                this.startAlpha = startAlpha;
                this.endAlpha = endAlpha;
                this.duration = duration;
                Alpha = startAlpha;
            }

            void SetAlpha(byte alpha)
            {
                Alpha = alpha;
                AlphaUpdated?.Invoke(alpha);
            }

            protected override void Restart()
            {
                SetAlpha(startAlpha);
            }

            protected override void Update(TimeSpan elapsed, bool reversed, out bool finished)
            {
                if (elapsed >= duration)
                {
                    SetAlpha(reversed ? startAlpha : endAlpha);
                    finished = true;
                }
                else
                {
                    var remaining = (float)((duration - elapsed).TotalSeconds / duration.TotalSeconds);

                    if (reversed)
                        remaining = 1.0f - remaining;

                    SetAlpha((byte)Util.Round(startAlpha + remaining * (endAlpha - startAlpha)));
                    finished = false;
                }
            }
        }

        protected class Text : TimedEffect
        {
            readonly Position startPosition;
            Position position;
            readonly IRenderText renderText;
            static readonly Size glyphSize = new Size(Global.GlyphWidth, Global.GlyphLineHeight);
            Position targetPosition;
            TimeSpan duration;

            public int Width => renderText.Width;
            public int Height => renderText.Height;
            public override bool Visible
            {
                get => renderText.Visible;
                set => renderText.Visible = value;
            }

            public Text(IRenderView renderView, IRenderLayer renderLayer, Rect bounds, string text,
                TextColor color, TextAlign textAlign, bool shadow = true, bool removeWhenScrolledOffScreen = true)
                : base(ExecutionType.Once, removeWhenScrolledOffScreen)
            {
                var wrappedText = renderView.TextProcessor.CreateText(text);
                wrappedText = renderView.TextProcessor.WrapText(wrappedText, bounds, glyphSize);
                renderText = renderView.RenderTextFactory.Create(renderLayer, wrappedText, color, shadow, bounds, textAlign);
                renderText.Visible = false;
                startPosition = new Position(bounds.Position);
                position = new Position(startPosition);
            }

            public void StartScrolling(Position targetPosition, double pixelsPerSecond)
            {
                if (pixelsPerSecond > 0)
                {
                    int dist = Math.Max(Math.Abs(targetPosition.X - position.X), Math.Abs(targetPosition.Y - position.Y));

                    if (dist != 0)
                    {
                        this.targetPosition = new Position(targetPosition);
                        duration = TimeSpan.FromSeconds(dist / pixelsPerSecond);
                        Start();
                    }
                }
            }

            void SetPosition(Position position)
            {
                this.position = position;
                renderText.Place(new Rect(position, new Size(renderText.Width, renderText.Height)));
            }

            protected override void Update(TimeSpan elapsed, bool reversed, out bool finished)
            {
                if (elapsed >= duration)
                {
                    SetPosition(new Position(reversed ? startPosition : targetPosition));
                    finished = true;
                }
                else
                {
                    var remaining = (float)((duration - elapsed).TotalSeconds / duration.TotalSeconds);

                    if (reversed)
                        remaining = 1.0f - remaining;

                    SetPosition((startPosition + remaining * (targetPosition - startPosition)).Round());
                    finished = false;
                }
            }

            protected override void Destroy()
            {
                renderText?.Delete();
                base.Destroy();
            }
        }

        protected class Image : TimedEffect
        {
            readonly Position position;
            readonly ILayerSprite sprite;
            readonly ImageTransformations imageTransformations;
            readonly Size frameSize;

            public int Width => sprite.Width;
            public int Height => sprite.Height;
            public override bool Visible
            {
                get => sprite.Visible;
                set => sprite.Visible = value;
            }

            public interface IImageTransformation
            {
                TimeSpan Duration { get; }
                void Init(Image image);
                void Perform(Image image, float progress);
                event Action Finished;
            }

            public class ImageTransformations
            {
                public class ImageTransformationBuilder
                {
                    readonly ImageTransformations imageTransformations = new ImageTransformations();
                    readonly List<IImageTransformation> transformations = new List<IImageTransformation>();

                    public ImageTransformationBuilder AddMovement(Position targetPosition, TimeSpan duration)
                    {
                        transformations.Add(new MoveTransformation(targetPosition, duration));
                        return this;
                    }

                    public ImageTransformationBuilder AddScaling(float targetScale, TimeSpan duration)
                    {
                        transformations.Add(new ScaleTransformation(targetScale, duration));
                        return this;
                    }

                    public ImageTransformationBuilder AddAnimation(int numFrames, float framesPerSecond,
                        int numCycles = 1, bool alternate = false)
                    {
                        transformations.Add(new AnimateTransformation(numFrames, framesPerSecond, numCycles, alternate));
                        return this;
                    }

                    public ImageTransformationBuilder AddZMovement(byte targetDisplayLayer, TimeSpan duration)
                    {
                        transformations.Add(new MoveZTransformation(targetDisplayLayer, duration));
                        return this;
                    }

                    void With(Func<IImageTransformation> creator)
                    {
                        if (transformations.Count == 0)
                            throw new InvalidOperationException("'With' calls must be used after at least one 'Add' call.");

                        var last = transformations[transformations.Count - 1];

                        if (last is CompoundTransformation compoundTransformation)
                            compoundTransformation.Add(creator());
                        else
                            transformations[transformations.Count - 1] = new CompoundTransformation(last, creator());
                    }

                    public ImageTransformationBuilder WithMovement(Position targetPosition, TimeSpan duration)
                    {
                        With(() => new MoveTransformation(targetPosition, duration));
                        return this;
                    }

                    public ImageTransformationBuilder WithScaling(float targetScale, TimeSpan duration)
                    {
                        With(() => new ScaleTransformation(targetScale, duration));
                        return this;
                    }

                    public ImageTransformationBuilder WithAnimation(int numFrames, float framesPerSecond,
                        int numCycles = 1, bool alternate = false)
                    {
                        With(() => new AnimateTransformation(numFrames, framesPerSecond, numCycles, alternate));
                        return this;
                    }

                    public ImageTransformationBuilder WithZMovement(byte targetDisplayLayer, TimeSpan duration)
                    {
                        With(() => new MoveZTransformation(targetDisplayLayer, duration));
                        return this;
                    }

                    public ImageTransformations Build()
                    {
                        imageTransformations.imageTransformations.Add(new CompoundTransformation(transformations));
                        return imageTransformations;
                    }
                }

                readonly List<IImageTransformation> imageTransformations = new List<IImageTransformation>();

                private ImageTransformations()
                {
                    // only accessable by ImageTransformationBuilder
                }

                public int Count => imageTransformations.Count;
                public IImageTransformation this[int index] => imageTransformations[index];

                public void Clear() => imageTransformations.Clear();

                public static ImageTransformationBuilder AddMovement(Position targetPosition, TimeSpan duration)
                {
                    var builder = new ImageTransformationBuilder();
                    builder.AddMovement(targetPosition, duration);
                    return builder;
                }

                public static ImageTransformationBuilder AddScaling(float targetScale, TimeSpan duration)
                {
                    var builder = new ImageTransformationBuilder();
                    builder.AddScaling(targetScale, duration);
                    return builder;
                }

                public static ImageTransformationBuilder AddAnimation(int numFrames, float framesPerSecond,
                    int numCycles = 1, bool alternate = false)
                {
                    var builder = new ImageTransformationBuilder();
                    builder.AddAnimation(numFrames, framesPerSecond, numCycles, alternate);
                    return builder;
                }

                public static ImageTransformationBuilder AddZMovement(byte targetDisplayLayer, TimeSpan duration)
                {
                    var builder = new ImageTransformationBuilder();
                    builder.AddZMovement(targetDisplayLayer, duration);
                    return builder;
                }
            }

            class CompoundTransformation : IImageTransformation
            {
                readonly List<IImageTransformation> transformations;
                readonly List<float> transformationEndTimes = new List<float>();

                public CompoundTransformation(List<IImageTransformation> transformations)
                {
                    this.transformations = transformations;
                }

                public CompoundTransformation(params IImageTransformation[] transformations)
                {
                    this.transformations = new List<IImageTransformation>(transformations);
                }

                public TimeSpan Duration { get; private set; }
                public event Action Finished;

                public void Add(IImageTransformation imageTransformation) => transformations.Add(imageTransformation);

                public void Init(Image image)
                {
                    Duration = new TimeSpan();

                    foreach (var transformation in transformations)
                    {
                        transformation.Init(image);

                        if (transformation.Duration > Duration)
                            Duration = transformation.Duration;
                    }

                    transformationEndTimes.Clear();

                    if (Duration.TotalSeconds < 0.0001f)
                        return;

                    foreach (var transformation in transformations)
                    {
                        transformationEndTimes.Add((float)(transformation.Duration / Duration));
                    }
                }

                public void Perform(Image image, float progress)
                {
                    progress = Math.Min(progress, 1.0f);

                    int index = 0;

                    foreach (var transformation in transformations)
                    {
                        if (transformationEndTimes[index] > 0.0001f && transformationEndTimes[index] >= progress)
                        {
                            transformation.Perform(image, progress / transformationEndTimes[index]);
                        }

                        ++index;
                    }

                    if (progress >= 1.0f)
                        Finished?.Invoke();
                }
            }

            class MoveTransformation : IImageTransformation
            {
                readonly Position targetPosition;
                Position startPosition;

                public MoveTransformation(Position targetPosition, TimeSpan duration)
                {
                    this.targetPosition = new Position(targetPosition);
                    Duration = duration;
                }

                public TimeSpan Duration { get; }
                public event Action Finished;

                public void Init(Image image)
                {
                    startPosition = new Position(image.position);
                }

                public void Perform(Image image, float progress)
                {
                    progress = Math.Min(progress, 1.0f);

                    int dx = targetPosition.X - startPosition.X;
                    int dy = targetPosition.Y - startPosition.Y;
                    image.sprite.X = image.position.X = startPosition.X + Util.Round(dx * progress);
                    image.sprite.Y = image.position.Y = startPosition.Y + Util.Round(dy * progress);                    

                    if (progress >= 1.0f)
                        Finished?.Invoke();
                }
            }

            class ScaleTransformation : IImageTransformation
            {
                readonly float targetScale;
                Size startSize;

                public ScaleTransformation(float targetScale, TimeSpan duration)
                {
                    this.targetScale = targetScale;
                    Duration = duration;
                }

                public TimeSpan Duration { get; }
                public event Action Finished;

                public void Init(Image image)
                {
                    startSize = new Size(image.Width, image.Height);
                }

                public void Perform(Image image, float progress)
                {
                    progress = Math.Min(progress, 1.0f);

                    float sizeFactor = 1.0f + (targetScale - 1.0f) * progress;
                    int width = Util.Round(startSize.Width * sizeFactor);
                    int height = Util.Round(startSize.Height * sizeFactor);
                    image.sprite.Resize(width, height);

                    if (progress >= 1.0f)
                        Finished?.Invoke();
                }
            }

            class AnimateTransformation : IImageTransformation
            {
                readonly int numFrames;
                readonly float framesPerSecond;
                readonly int numCycles;
                readonly bool alternate;
                Position startTextureAtlasOffset;

                public AnimateTransformation(int numFrames, float framesPerSecond,
                    int numCycles = 1, bool alternate = false)
                {
                    if (numFrames < 0)
                        throw new ArgumentOutOfRangeException(nameof(numFrames));

                    if (framesPerSecond < 0.0001f)
                        throw new ArgumentOutOfRangeException(nameof(framesPerSecond));

                    if (numCycles < 1)
                        throw new ArgumentOutOfRangeException(nameof(numCycles));

                    this.numFrames = numFrames;
                    this.framesPerSecond = framesPerSecond;
                    this.numCycles = numCycles;
                    this.alternate = alternate;
                    if (numFrames == 0)
                        Duration = new TimeSpan();
                    else
                        Duration = numCycles * TimeSpan.FromSeconds(numFrames / framesPerSecond);
                }

                public TimeSpan Duration { get; }
                public event Action Finished;

                public void Init(Image image)
                {
                    startTextureAtlasOffset = new Position(image.sprite.TextureAtlasOffset);
                }

                public void Perform(Image image, float progress)
                {
                    progress = Math.Min(progress, 1.0f);

                    float progressPerCycle = 1.0f / numCycles;
                    int cycle = Util.Floor(progress / progressPerCycle);
                    progress %= progressPerCycle;
                    progress /= progressPerCycle;

                    if (alternate && cycle % 2 == 1)
                        progress = 1.0f - progress;

                    int frameIndex = Util.Floor(progress / numFrames);

                    image.sprite.TextureAtlasOffset = new Position(startTextureAtlasOffset.X +
                        frameIndex * image.frameSize.Width, startTextureAtlasOffset.Y);

                    if (progress >= 1.0f)
                        Finished?.Invoke();
                }
            }

            class MoveZTransformation : IImageTransformation
            {
                readonly byte targetDisplayLayer;
                byte startDisplayLayer;

                public MoveZTransformation(byte targetDisplayLayer, TimeSpan duration)
                {
                    this.targetDisplayLayer = targetDisplayLayer;
                    Duration = duration;
                }

                public TimeSpan Duration { get; }
                public event Action Finished;

                public void Init(Image image)
                {
                    startDisplayLayer = image.sprite.DisplayLayer;
                }

                public void Perform(Image image, float progress)
                {
                    progress = Math.Min(progress, 1.0f);

                    byte displayLayer = (byte)(startDisplayLayer + Util.Round((targetDisplayLayer - startDisplayLayer) * progress));
                    image.sprite.DisplayLayer = displayLayer;

                    if (progress >= 1.0f)
                        Finished?.Invoke();
                }
            }

            public Image(IRenderView renderView, Layer layer, Position position, uint graphicIndex,
                Size size, ImageTransformations imageTransformations, bool deleteAfterTransformations,
                byte displayLayer, byte paletteIndex)
                : this(renderView, layer, position, graphicIndex, size, displayLayer, paletteIndex)
            {
                if (imageTransformations == null)
                    throw new ArgumentNullException(nameof(imageTransformations));

                if (deleteAfterTransformations)
                    imageTransformations[imageTransformations.Count - 1].Finished += Destroy;

                this.imageTransformations = imageTransformations;
            }

            public Image(IRenderView renderView, Layer layer, Position position, uint graphicIndex,
                Size size, byte displayLayer, byte paletteIndex)
                : base(ExecutionType.Once, false)
            {
                this.position = new Position(position);
                frameSize = new Size(size);
                sprite = renderView.SpriteFactory.Create(size.Width, size.Height, true, displayLayer) as ILayerSprite;
                sprite.Layer = renderView.GetLayer(layer);
                sprite.PaletteIndex = paletteIndex;
                sprite.TextureAtlasOffset = TextureAtlasManager.Instance.GetOrCreate(layer).GetOffset(graphicIndex);
                sprite.X = position.X;
                sprite.Y = position.Y;
                sprite.Visible = false;
            }

            protected override void Destroy()
            {
                imageTransformations?.Clear();
                sprite?.Delete();
                base.Destroy();
            }

            protected override void Update(TimeSpan elapsed, bool reversed, out bool finished)
            {
                finished = false;

                if (imageTransformations != null && imageTransformations.Count != 0)
                {
                    var transformationElapsed = elapsed;

                    for (int i = 0; i < imageTransformations.Count; ++i)
                    {
                        var transformation = imageTransformations[i];

                        if (transformationElapsed < transformation.Duration)
                        {
                            transformation.Perform(this, (float)(transformationElapsed / transformation.Duration));
                            return;
                        }

                        transformationElapsed -= transformation.Duration;
                    }

                    finished = true;
                }
            }
        }

        public Video(IRenderView renderView, Layer layer)
        {
            this.renderView = renderView;
            this.layer = layer;
            renderLayer = renderView.GetLayer(layer);
        }

        protected Text AddText(DateTime showTime, Rect bounds, string text, TextColor color, TextAlign textAlign, bool shadow = true)
        {
            var newText = new Text(renderView, renderLayer, bounds, text, color, textAlign, shadow);
            texts.Add(newText);
            newText.Destroyed += () => texts.Remove(newText);
            newText.ExecutionTime = showTime;
            return newText;
        }

        protected Text AddScrollingText(DateTime showTime, Rect startBounds, string text, TextColor color, TextAlign textAlign,
            bool up, double pixelsPerSecond, bool shadow = true, Action finishAction = null)
        {
            var newText = AddText(showTime, startBounds, text, color, textAlign, shadow);
            var targetPosition = new Position(startBounds.Position);
            if (up)
                targetPosition.Y = -newText.Height;
            else
                targetPosition.Y = Global.VirtualScreenHeight;
            newText.StartScrolling(targetPosition, pixelsPerSecond);
            if (finishAction != null)
                newText.Finished += finishAction;
            return newText;
        }

        protected Image AddStaticImage(DateTime showTime, Position position, uint graphicIndex, Size size,
            byte displayLayer, byte paletteIndex)
        {
            var image = new Image(renderView, layer, position, graphicIndex, size, displayLayer, paletteIndex);
            images.Add(image);
            image.Destroyed += () => images.Remove(image);
            image.ExecutionTime = showTime;
            return image;
        }

        protected Image AddDynamicImage(DateTime showTime, Position position, uint graphicIndex, Size size,
            byte displayLayer, byte paletteIndex, Image.ImageTransformations imageTransformations,
            bool deleteAfterTransformations = true, Action finishAction = null)
        {
            var image = new Image(renderView, layer, position, graphicIndex, size, imageTransformations,
                deleteAfterTransformations, displayLayer, paletteIndex);
            images.Add(image);
            image.Destroyed += () => images.Remove(image);
            if (finishAction != null)
                image.Finished += finishAction;
            image.ExecutionTime = showTime;
            return image;
        }

        protected FadeEffect AddFadeEffect(DateTime showTime, byte startAlpha, byte endAlpha, TimeSpan duration,
            TimedEffect.ExecutionType executionType = TimedEffect.ExecutionType.Once, bool destroyAfterFade = true,
            Action finishAction = null)
        {
            var fadeEffect = new FadeEffect(startAlpha, endAlpha, duration, executionType, destroyAfterFade);
            fadeEffects.Add(fadeEffect);
            fadeEffect.Destroyed += () => fadeEffects.Remove(fadeEffect);
            if (finishAction != null)
                fadeEffect.Finished += finishAction;
            fadeEffect.ExecutionTime = showTime;
            return fadeEffect;
        }

        public virtual void Update()
        {
            foreach (var fadeEffect in fadeEffects)
            {
                if (!fadeEffect.Processed)
                {
                    if (!fadeEffect.Started && fadeEffect.ExecutionTime <= DateTime.Now)
                        fadeEffect.Start();

                    fadeEffect.Start();
                }
            }

            foreach (var text in texts)
            {
                if (!text.Processed)
                {
                    if (!text.Started && text.ExecutionTime <= DateTime.Now)
                        text.Start();

                    text.Update();
                }
            }

            foreach (var image in images)
            {
                if (!image.Processed)
                {
                    if (!image.Started && image.ExecutionTime <= DateTime.Now)
                        image.Start();

                    image.Update();
                }
            }
        }
    }
}
