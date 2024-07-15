/*
 * GameView.cs - Implementation of a game render view
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Data;
using Ambermoon.Render;
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using System;
using System.Collections.Generic;

namespace Ambermoon.Renderer.OpenGL
{
    public delegate bool FullscreenRequestHandler(bool fullscreen);

    public class RenderView : RenderLayerFactory, IRenderView, IDisposable
    {
        Action<byte[]> screenshotDataHandler = null;
        bool disposed = false;
        readonly Context context;
        bool useFrameBuffer = false;
        readonly FrameBuffer frameBuffer;
        readonly ScreenShader screenShader;
        readonly ScreenRenderBuffer screenBuffer;
        bool useEffectFrameBuffer = false;
        readonly FrameBuffer effectFrameBuffer;
        readonly EffectShader effectShader;
        readonly ScreenRenderBuffer effectBuffer;
        // Area inside the window where the rendering happens.
        // Note that this area is in screen coordinates and not
        // necessarily in pixels!
        Rect renderDisplayArea;
        // The content size of the window in screen coordinates (not pixels!)
        Size windowSize;
        // The size of the framebuffer in pixels
        Size frameBufferSize;
        // The rendering area in pixels
        Rect frameBufferWindowArea => new Rect
        (
            renderDisplayArea.Position, frameBufferSize
        );
        readonly SizingPolicy sizingPolicy;
        readonly OrientationPolicy orientationPolicy;
        readonly DeviceType deviceType;
        Rotation rotation = Rotation.None;
        readonly SortedDictionary<Layer, RenderLayer> layers = new SortedDictionary<Layer, RenderLayer>();
        readonly SpriteFactory spriteFactory = null;
        readonly ColoredRectFactory coloredRectFactory = null;
        readonly Surface3DFactory surface3DFactory = null;
        readonly RenderTextFactory renderTextFactory = null;
        readonly FowFactory fowFactory = null;
        readonly Camera3D camera3D = null;
        PaletteReplacement paletteReplacement = null;
        PaletteReplacement horizonPaletteReplacement = null;
        PaletteFading paletteFading = null;
        bool fullscreen = false;
        const float VirtualAspectRatio = Global.VirtualAspectRatio;
        float sizeFactorX = 1.0f;
        float sizeFactorY = 1.0f;
        readonly Func<KeyValuePair<int, int>> screenBufferModeProvider = null;
        readonly Func<int> effectProvider = null;

        float RenderFactorX => (float)frameBufferSize.Width / Global.VirtualScreenWidth;
        float RenderFactorY => (float)frameBufferSize.Height / Global.VirtualScreenHeight;
        float WindowFactorX => (float)renderDisplayArea.Width / Global.VirtualScreenWidth;
        float WindowFactorY => (float)renderDisplayArea.Height / Global.VirtualScreenHeight;

#pragma warning disable 0067
        public event EventHandler Closed;
        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler Drag;
        public event EventHandler KeyPress;
        public event EventHandler SystemKeyPress;
        public event EventHandler StopDrag;
#pragma warning restore 0067
        public FullscreenRequestHandler FullscreenRequestHandler { get; set; }

        public Size FramebufferSize => new Size(frameBufferSize);
        public Size MaxScreenSize { get; set; }
        public List<Size> AvailableFullscreenModes { get; set; }
        public bool IsLandscapeRatio { get; } = true;

        public ISpriteFactory SpriteFactory => spriteFactory;
        public IColoredRectFactory ColoredRectFactory => coloredRectFactory;
        public ISurface3DFactory Surface3DFactory => surface3DFactory;
        public IRenderTextFactory RenderTextFactory => renderTextFactory;
        public IFowFactory FowFactory => fowFactory;
        public ICamera3D Camera3D => camera3D;
        public IGameData GameData { get; }
        public IGraphicProvider GraphicProvider { get; }
        public IFontProvider FontProvider { get; }
        public ITextProcessor TextProcessor { get; }
        public Action<float> AspectProcessor { get; }

        #region Coordinate transformations

        PositionTransformation PositionTransformation => (FloatPosition position) =>
            new FloatPosition(position.X * RenderFactorX, position.Y * RenderFactorY);

        SizeTransformation SizeTransformation => (FloatSize size) =>
            new FloatSize(size.Width * RenderFactorX, size.Height * RenderFactorY);

        #endregion


        public RenderView(IContextProvider contextProvider, IGameData gameData, IGraphicProvider graphicProvider,
            IFontProvider fontProvider, ITextProcessor textProcessor, Func<TextureAtlasManager> textureAtlasManagerProvider,
            int framebufferWidth, int framebufferHeight, Size windowSize, ref bool useFrameBuffer, ref bool useEffectFrameBuffer,
            Func<KeyValuePair<int, int>> screenBufferModeProvider, Func<int> effectProvider, Graphic[] additionalPalettes,
            DeviceType deviceType = DeviceType.Desktop, SizingPolicy sizingPolicy = SizingPolicy.FitRatio,
            OrientationPolicy orientationPolicy = OrientationPolicy.Support180DegreeRotation)
            : base(new State(contextProvider))
        {
            AspectProcessor = UpdateAspect;
            GameData = gameData;
            GraphicProvider = graphicProvider;
            FontProvider = fontProvider;
            TextProcessor = textProcessor;
            frameBufferSize = new Size(framebufferWidth, framebufferHeight);
            renderDisplayArea = new Rect(new Position(0, 0), windowSize);
            this.windowSize = new Size(windowSize);
            this.sizingPolicy = sizingPolicy;
            this.orientationPolicy = orientationPolicy;
            this.deviceType = deviceType;
            IsLandscapeRatio = framebufferWidth > framebufferHeight;

            Resize(framebufferWidth, framebufferHeight);

            context = new Context(State, framebufferWidth, framebufferHeight, 1.0f);

            // factories
            var visibleArea = new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight);
            spriteFactory = new SpriteFactory(visibleArea);
            coloredRectFactory = new ColoredRectFactory(visibleArea);
            surface3DFactory = new Surface3DFactory(visibleArea);
            renderTextFactory = new RenderTextFactory(visibleArea);
            fowFactory = new FowFactory(visibleArea);

            this.screenBufferModeProvider = screenBufferModeProvider;
            this.effectProvider = effectProvider;

            camera3D = new Camera3D(State);

            TextureAtlasManager.RegisterFactory(new TextureAtlasBuilderFactory(State));

            var textureAtlasManager = textureAtlasManagerProvider();
            var palette = textureAtlasManager.CreatePalette(graphicProvider, additionalPalettes);

            static void Set320x256View(IRenderLayer renderLayer)
            {
                // To keep the aspect ration of 16:10 we use a virtual screen of 409,6 x 256.
                // All positions are in this screen even though position values will only be
                // in the range 320 x 256. There will be a black border left and right.
                // The factor 200/256 is used to transform all positions. All X coordinates
                // are increased by (409,6 - 320) / 2 (44.8) to center the display. But this
                // values has to be factored by 200/256 as well and will become exactly 35.
                const float factorY = 200.0f / 256.0f;
                const float factorX = factorY;// factorY * (1.0f + 0.4f / 410.0f);

                renderLayer.PositionTransformation = (FloatPosition position) =>
                    new FloatPosition(35.0f + position.X * factorX, position.Y * factorY);
                renderLayer.SizeTransformation = (FloatSize size) =>
                    new FloatSize(size.Width * factorX, size.Height * factorY);
            }

            foreach (var layer in EnumHelper.GetValues<Layer>())
            {
                if (layer == Layer.None)
                    continue;

                try
                {
                    var texture = textureAtlasManager.GetOrCreate(layer)?.Texture;
                    var renderLayer = Create(layer, texture, palette);

                    if (layer != Layer.Map3DBackground && layer != Layer.Map3DBackgroundFog && layer != Layer.Map3DCeiling && layer != Layer.Map3D && layer != Layer.Billboards3D)
                        renderLayer.Visible = true;

                    if (RenderLayer.DefaultLayerConfigs[layer].Use320x256)
                        Set320x256View(renderLayer);

                    AddLayer(renderLayer);
                }
                catch (Exception ex)
                {
                    throw new AmbermoonException(ExceptionScope.Render, $"Unable to create layer '{layer}': {ex.Message}");
                }
            }

            try
            {
                frameBuffer = new FrameBuffer(State);
                screenShader = ScreenShader.Create(State);
                screenBuffer = new ScreenRenderBuffer(State, screenShader);
                this.useFrameBuffer = useFrameBuffer;
            }
            catch
            {
                frameBuffer?.Dispose();
                frameBuffer = null;
                screenShader = null;
                screenBuffer?.Dispose();
                screenBuffer = null;
                useFrameBuffer = false;
            }

            try
            {
                effectFrameBuffer = new FrameBuffer(State);
                effectShader = EffectShader.Create(State);
                effectBuffer = new ScreenRenderBuffer(State, effectShader);
                this.useEffectFrameBuffer = useEffectFrameBuffer;
            }
            catch
            {
                effectFrameBuffer?.Dispose();
                effectFrameBuffer = null;
                effectShader = null;
                effectBuffer?.Dispose();
                effectBuffer = null;
                useEffectFrameBuffer = false;
            }
        }

        public bool AllowFramebuffer => frameBuffer != null;
        public bool AllowEffects => effectFrameBuffer != null;

        public bool TryUseFrameBuffer()
        {
            if (AllowFramebuffer)
            {
                useFrameBuffer = true;
                return true;
            }

            useFrameBuffer = false;
            return false;
        }

        public bool TryUseEffects()
        {
            if (AllowEffects)
            {
                useEffectFrameBuffer = true;
                return true;
            }

            useEffectFrameBuffer = false;
            return false;
        }

        void UpdateAspect(float aspect)
        {
            context?.UpdateAspect(aspect);
        }

        public void UsePalette(Layer layer, bool use)
        {
            layers[layer]?.UsePalette(use);
        }

        public void SetTextureFactor(Layer layer, uint factor)
        {
            layers[layer]?.SetTextureFactor(factor);
        }

        public void Close()
        {
            Dispose();

            Closed?.Invoke(this, EventArgs.Empty);
        }

        public bool Fullscreen
        {
            get => fullscreen;
            set
            {
                if (fullscreen == value || FullscreenRequestHandler == null)
                    return;

                if (FullscreenRequestHandler(value))
                    fullscreen = value;
            }
        }

        void SetRotation(Orientation orientation)
        {
            if (deviceType == DeviceType.Desktop ||
                sizingPolicy == SizingPolicy.FitRatioKeepOrientation ||
                sizingPolicy == SizingPolicy.FitWindowKeepOrientation)
            {
                rotation = Rotation.None;
                return;
            }

            if (orientation == Orientation.Default)
                orientation = (deviceType == DeviceType.MobilePortrait) ? Orientation.PortraitTopDown : Orientation.LandscapeLeftRight;

            if (sizingPolicy == SizingPolicy.FitRatioForcePortrait ||
                sizingPolicy == SizingPolicy.FitWindowForcePortrait)
            {
                if (orientation == Orientation.LandscapeLeftRight)
                    orientation = Orientation.PortraitTopDown;
                else if (orientation == Orientation.LandscapeRightLeft)
                    orientation = Orientation.PortraitBottomUp;
            }
            else if (sizingPolicy == SizingPolicy.FitRatioForceLandscape ||
                     sizingPolicy == SizingPolicy.FitWindowForceLandscape)
            {
                if (orientation == Orientation.PortraitTopDown)
                    orientation = Orientation.LandscapeLeftRight;
                else if (orientation == Orientation.PortraitBottomUp)
                    orientation = Orientation.LandscapeRightLeft;
            }

            switch (orientation)
            {
                case Orientation.PortraitTopDown:
                    if (deviceType == DeviceType.MobilePortrait)
                        rotation = Rotation.None;
                    else
                        rotation = Rotation.Deg90;
                    break;
                case Orientation.LandscapeLeftRight:
                    if (deviceType == DeviceType.MobilePortrait)
                        rotation = Rotation.Deg270;
                    else
                        rotation = Rotation.None;
                    break;
                case Orientation.PortraitBottomUp:
                    if (deviceType == DeviceType.MobilePortrait)
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg180;
                        else
                            rotation = Rotation.None;
                    }
                    else
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg270;
                        else
                            rotation = Rotation.Deg90;
                    }
                    break;
                case Orientation.LandscapeRightLeft:
                    if (deviceType == DeviceType.MobilePortrait)
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg270;
                        else
                            rotation = Rotation.Deg90;
                    }
                    else
                    {
                        if (orientationPolicy == OrientationPolicy.Support180DegreeRotation)
                            rotation = Rotation.Deg180;
                        else
                            rotation = Rotation.None;
                    }
                    break;
            }
        }

        public void Resize(int width, int height, int? windowWidth = null, int? windowHeight = null)
        {
            if (windowWidth != null)
                windowSize.Width = windowWidth.Value;
            if (windowHeight != null)
                windowSize.Height = windowHeight.Value;

            switch (deviceType)
            {
                default:
                case DeviceType.Desktop:
                case DeviceType.MobileLandscape:
                    Resize(width, height, Orientation.LandscapeLeftRight);
                    break;
                case DeviceType.MobilePortrait:
                    Resize(width, height, Orientation.PortraitTopDown);
                    break;
            }
        }

        public void Resize(int width, int height, Orientation orientation)
        {
            frameBufferSize.Width = width;
            frameBufferSize.Height = height;

            SetRotation(orientation);

            if (sizingPolicy == SizingPolicy.FitWindow ||
                sizingPolicy == SizingPolicy.FitWindowKeepOrientation ||
                sizingPolicy == SizingPolicy.FitWindowForcePortrait ||
                sizingPolicy == SizingPolicy.FitWindowForceLandscape)
            {
                renderDisplayArea = new Rect(0, 0, width, height);

                sizeFactorX = 1.0f;
                sizeFactorY = 1.0f;
            }
            else
            {
                float windowRatio = (float)width / height;
                float virtualRatio = VirtualAspectRatio;

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                    virtualRatio = 1.0f / virtualRatio;

                if (Misc.FloatEqual(windowRatio, virtualRatio))
                {
                    renderDisplayArea = new Rect(0, 0, windowSize.Width, windowSize.Height);
                }
                else if (windowRatio < virtualRatio)
                {
                    int newHeight = Misc.Round(windowSize.Width / virtualRatio);
                    renderDisplayArea = new Rect(0, (windowSize.Height - newHeight) / 2, windowSize.Width, newHeight);
                    int newFrameBufferHeight = Misc.Round(frameBufferSize.Width / virtualRatio);
                    frameBufferSize.Height = newFrameBufferHeight;
                }
                else // windowRatio > virtualRatio
                {
                    int newWidth = Misc.Round(windowSize.Height * virtualRatio);
                    renderDisplayArea = new Rect((windowSize.Width - newWidth) / 2, 0, newWidth, windowSize.Height);
                    int newFrameBufferWidth = Misc.Round(frameBufferSize.Height * virtualRatio);
                    frameBufferSize.Width = newFrameBufferWidth;
                }

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                {
                    sizeFactorX = (float)frameBufferSize.Height / renderDisplayArea.Width;
                    sizeFactorY = (float)frameBufferSize.Width / renderDisplayArea.Height;
                }
                else
                {
                    sizeFactorX = (float)frameBufferSize.Width / renderDisplayArea.Width;
                    sizeFactorY = (float)frameBufferSize.Height / renderDisplayArea.Height;
                }
            }

            var viewport = frameBufferWindowArea;
            State.Gl.Viewport(viewport.X, viewport.Y, (uint)viewport.Width, (uint)viewport.Height);
        }

        public void TakeScreenshot(Action<byte[]> dataHandler)
        {
            if (screenshotDataHandler == null)
                screenshotDataHandler = dataHandler;
        }

        public void AddLayer(IRenderLayer layer)
        {
            if (layer is not RenderLayer)
                throw new InvalidCastException("The given layer is not valid for this renderer.");

            layers.Add(layer.Layer, layer as RenderLayer);
        }

        public IRenderLayer GetLayer(Layer layer)
        {
            return layers[layer];
        }

        public void ShowLayer(Layer layer, bool show)
        {
            layers[layer].Visible = show;
        }

        bool accessViolationDetected = false;

        void BindEffectBuffer(Position viewOffset)
        {
            effectFrameBuffer.Bind(frameBufferSize.Width, frameBufferSize.Height);
            var viewport = frameBufferWindowArea;
            State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y - viewOffset.Y,
                (uint)viewport.Width, (uint)viewport.Height);
        }

        public void Render(FloatPosition viewportOffset)
        {
            if (disposed)
                return;
            
            if (screenshotDataHandler != null)
            {
                try
                {
                    var area = frameBufferWindowArea;
                    byte[] buffer = new byte[area.Width * area.Height * 3];
                    State.Gl.ReadBuffer(GLEnum.Back);
                    State.Gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
                    State.Gl.ReadPixels<byte>(area.X, area.Y, (uint)area.Width, (uint)area.Height, GLEnum.Rgb, GLEnum.UnsignedByte, buffer);
                    screenshotDataHandler(buffer);
                    screenshotDataHandler = null;
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                context.SetRotation(rotation);

                bool render3DMap = layers[Layer.Map3D].Visible;
                var viewOffset = new Position
                (
                    Util.Round((viewportOffset?.X ?? 0.0f) * renderDisplayArea.Width),
                    Util.Round((viewportOffset?.Y ?? 0.0f) * renderDisplayArea.Height)
                );

                if (useEffectFrameBuffer)
                    BindEffectBuffer(viewOffset);
                else
                {
                    State.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0u);
                    State.Gl.Viewport(0, 0, (uint)frameBufferSize.Width, (uint)frameBufferSize.Height);
                }

                State.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

                bool set2DViewport = false;
                viewOffset.X -= Util.Floor(0.49f * frameBufferSize.Width / Global.VirtualScreenWidth);
                viewOffset.Y -= Util.Floor(0.49f * frameBufferSize.Height / Global.VirtualScreenHeight);

                foreach (var layer in layers)
                {
                    if (render3DMap)
                    {
                        if (layer.Key == Layer.Map3DBackground)
                        {
                            var viewport = frameBufferWindowArea;

                            if (useEffectFrameBuffer)
                                State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
                            else
                            {
                                State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y + viewOffset.Y,
                                    (uint)viewport.Width, (uint)viewport.Height);
                            }
                        }
                        else if (layer.Key == Layer.Map3DCeiling)
                        {
                            // Setup 3D stuff
                            camera3D.Activate();
                            State.RestoreProjectionMatrix(State.ProjectionMatrix3D);
                            var mapViewArea = new Rect(Global.Map3DViewX, Global.Map3DViewY, Global.Map3DViewWidth + 1, Global.Map3DViewHeight + 1);
                            mapViewArea.Position = PositionTransformation(mapViewArea.Position).Round();
                            mapViewArea.Size = SizeTransformation(mapViewArea.Size).ToSize();
                            var viewport = frameBufferWindowArea;
                            if (useEffectFrameBuffer)
                            {
                                State.Gl.Viewport(mapViewArea.X, viewport.Height - (mapViewArea.Y + mapViewArea.Height),
                                    (uint)mapViewArea.Width, (uint)mapViewArea.Height);
                            }
                            else
                            {
                                State.Gl.Viewport
                                (
                                    viewport.X + mapViewArea.X,
                                    viewport.Height - (viewport.Y + mapViewArea.Y + mapViewArea.Height),
                                    (uint)mapViewArea.Width, (uint)mapViewArea.Height
                                );
                            }
                            State.Gl.Enable(EnableCap.CullFace);
                            State.Gl.Disable(EnableCap.DepthTest);
                        }
                        else if (layer.Key == Layer.Map3D)
                        {
                            State.Gl.Enable(EnableCap.DepthTest);
                        }
                        else if (layer.Key == Layer.Billboards3D)
                        {
                            State.Gl.Disable(EnableCap.CullFace);
                        }
                        else if (layer.Key == Global.First2DLayer)
                        {
                            // Reset to 2D stuff
                            State.Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
                            State.RestoreModelViewMatrix(Matrix4.Identity);
                            State.RestoreProjectionMatrix(State.ProjectionMatrix2D);

                            var viewport = frameBufferWindowArea;

                            if (!useFrameBuffer)
                            {
                                if (useEffectFrameBuffer)
                                    State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
                                else
                                    State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y + viewOffset.Y,
                                        (uint)viewport.Width, (uint)viewport.Height);
                            }
                            else
                            {
                                frameBuffer.Bind(viewport.Width, viewport.Height);
                                State.Gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                                State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
                                State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
                                State.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
                            }
                            set2DViewport = true;
                        }
                    }
                    else if (!set2DViewport)
                    {
                        var viewport = frameBufferWindowArea;

                        if (!useFrameBuffer)
                        {
                            if (useEffectFrameBuffer)
                                State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
                            else
                                State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y + viewOffset.Y,
                                    (uint)viewport.Width, (uint)viewport.Height);
                        }
                        else
                        {
                            frameBuffer.Bind(viewport.Width, viewport.Height);
                            State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
                            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
                        }
  
                        set2DViewport = true;
                    }

                    if (useEffectFrameBuffer && layer.Key == Global.LastLayer)
                    {
                        State.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0u);
                        var viewport = frameBufferWindowArea;
                        State.Gl.Viewport(viewport.X, viewport.Y, (uint)viewport.Width, (uint)viewport.Height);
                        State.Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
                    }

                    if (layer.Key == Layer.DrugEffect)
                    {
                        if (useFrameBuffer)
                            RenderToScreen(viewOffset, useEffectFrameBuffer);
                        if (useEffectFrameBuffer)
                            RenderEffects(viewOffset);
                        if (DrugColorComponent != null)
                            State.Gl.BlendColor(System.Drawing.Color.FromArgb(255, System.Drawing.Color.FromArgb(0x202020 |
                                (0x800000 >> (8 * (DrugColorComponent.Value % 3))))));
                        State.Gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.OneMinusConstantColor);
                    }
                    else if (layer.Key == Layer.MobileOverlays)
                    {
						State.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
					}
                    else if (layer.Value.Config.EnableBlending)
                    {
                        State.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    }

                    if (layer.Key == Layer.Effects)
                    {
						State.Gl.Enable(EnableCap.DepthTest);
					}

                    if (layer.Value.Config.EnableBlending)
                        State.Gl.Enable(EnableCap.Blend);
                    else
                        State.Gl.Disable(EnableCap.Blend);

                    if (!layer.Value.Config.RenderToVirtualScreen)
                    {
                        State.PushProjectionMatrix(State.FullScreenProjectionMatrix2D);
                        try
                        {
                            layer.Value.Render();
                        }
                        finally
                        {
                            State.PopProjectionMatrix();
                        }
                    }
                    else
                    {
                        layer.Value.Render();
                    }
                }

                accessViolationDetected = false;
            }
            catch (AccessViolationException)
            {
                if (accessViolationDetected)
                    throw;

                accessViolationDetected = true;
            }
        }

        void RenderToScreen(Position viewOffset, bool useEffects)
        {
            if (useEffects)
                BindEffectBuffer(Position.Zero);
            else
                State.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            screenBuffer.SetSize(frameBufferSize);
            screenShader.Use(screenBuffer.ProjectionMatrix);
            screenShader.SetResolution(frameBufferSize);
            screenShader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
            var filterModes = screenBufferModeProvider?.Invoke() ?? KeyValuePair.Create(0, 0);
            screenShader.SetMode(filterModes.Key, filterModes.Value);
            State.Gl.ActiveTexture(GLEnum.Texture0);
            frameBuffer.BindAsTexture();
            State.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            State.Gl.Enable(EnableCap.Blend);
            State.Gl.Disable(EnableCap.DepthTest);
            var viewport = frameBufferWindowArea;
            if (useEffects)
            {
                State.Gl.Viewport(0, 0, (uint)viewport.Width, (uint)viewport.Height);
            }
            else
            {
                State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y - viewOffset.Y,
                    (uint)viewport.Width, (uint)viewport.Height);
            }
            screenBuffer.Render();
            State.Gl.BindTexture(GLEnum.Texture2D, 0);
            State.Gl.Enable(EnableCap.DepthTest);
        }

        void RenderEffects(Position viewOffset)
        {
            State.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            State.Gl.Viewport(0, 0, (uint)frameBufferSize.Width, (uint)frameBufferSize.Height);
            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit);
            effectBuffer.SetSize(frameBufferSize);
            effectShader.Use(effectBuffer.ProjectionMatrix);
            effectShader.SetResolution(frameBufferSize);
            effectShader.SetSampler(0); // we use texture unit 0 -> see Gl.ActiveTexture below
            effectShader.SetMode(effectProvider?.Invoke() ?? 0);
            State.Gl.ActiveTexture(GLEnum.Texture0);
            effectFrameBuffer.BindAsTexture();
            State.Gl.Disable(EnableCap.Blend);
            State.Gl.Disable(EnableCap.DepthTest);
            var viewport = frameBufferWindowArea;
            State.Gl.Viewport(viewport.X + viewOffset.X, viewport.Y - viewOffset.Y,
                (uint)viewport.Width, (uint)viewport.Height);
            effectBuffer.Render();
            State.Gl.BindTexture(GLEnum.Texture2D, 0);
            State.Gl.Enable(EnableCap.DepthTest);
        }

        public Position GameToScreen(Position position) =>
            ViewToScreen(new Position(Misc.Round(position.X * WindowFactorX), Misc.Round(position.Y * WindowFactorY)));

        public Position ViewToScreen(Position position)
        {
            int rotatedX = position.X;
            int rotatedY = position.Y;
            int relX;
            int relY;

            switch (rotation)
            {
                case Rotation.None:
                default:
                    relX = rotatedX;
                    relY = rotatedY;
                    break;
                case Rotation.Deg90:
                    relX = renderDisplayArea.Width - rotatedY;
                    relY = rotatedX;
                     break;
                case Rotation.Deg180:
                    relX = renderDisplayArea.Width - rotatedX;
                    relY = renderDisplayArea.Height - rotatedY;
                    break;
                case Rotation.Deg270:
                    relX = rotatedY;
                    relY = renderDisplayArea.Height - rotatedX;
                    break;
            }

            return new Position(renderDisplayArea.X + relX, renderDisplayArea.Y + relY);
        }

        public Size ViewToScreen(Size size)
        {
            bool swapDimensions = rotation == Rotation.Deg90 || rotation == Rotation.Deg270;

            int width = swapDimensions ? size.Height : size.Width;
            int height = swapDimensions ? size.Width : size.Height;

            return new Size(width, height);
        }

        public Size GameToScreen(Size size)
        {
            return ViewToScreen(new Size(Misc.Round(size.Width * WindowFactorX), Misc.Round(size.Height * WindowFactorY)));
        }

        public Rect GameToScreen(Rect rect)
        {
            var position = GameToScreen(rect.Position);
            var size = GameToScreen(rect.Size);
            rect = new Rect(position, size);

            rect.Clip(renderDisplayArea);

            if (rect.Empty)
                return null;

            return rect;
        }

        public Rect ViewToScreen(Rect rect)
        {
            var position = ViewToScreen(rect.Position);
            var size = ViewToScreen(rect.Size);
            rect = new Rect(position, size);

            rect.Clip(renderDisplayArea);

            if (rect.Empty)
                return null;

            return rect;
        }

        public Position ScreenToLayer(Position position, Layer layer)
        {
            if (RenderLayer.DefaultLayerConfigs[layer].Use320x256)
            {
                position = ScreenToView(position);

                float WindowFactorX = renderDisplayArea.Width / 409.6f;
                float WindowFactorY = renderDisplayArea.Height / 256.0f;

                return new Position(Misc.Round(position.X / WindowFactorX - 44.8f), Misc.Round(position.Y / WindowFactorY));
            }
            else
            {
                return ScreenToGame(position);
            }
        }

        // This is used to convert mouse coordinates to game coordinates
        public Position ScreenToGame(Position position)
        {
            position = ScreenToView(position);

            return new Position(Misc.Round(position.X / WindowFactorX), Misc.Round(position.Y / WindowFactorY));
        }

        public Position ScreenToView(Position position)
        {
            int relX = position.X - renderDisplayArea.X;
            int relY = position.Y - renderDisplayArea.Y;
            int rotatedX;
            int rotatedY;

            switch (rotation)
            {
                case Rotation.None:
                default:
                    rotatedX = relX;
                    rotatedY = relY;
                    break;
                case Rotation.Deg90:
                    rotatedX = relY;
                    rotatedY = renderDisplayArea.Width - relX;
                    break;
                case Rotation.Deg180:
                    rotatedX = renderDisplayArea.Width - relX;
                    rotatedY = renderDisplayArea.Height - relY;
                    break;
                case Rotation.Deg270:
                    rotatedX = renderDisplayArea.Height - relY;
                    rotatedY = relX;
                    break;
            }

            return new Position(rotatedX, rotatedY);
        }

        public Size ScreenToView(Size size)
        {
            bool swapDimensions = rotation == Rotation.Deg90 || rotation == Rotation.Deg270;

            int width = swapDimensions ? size.Height : size.Width;
            int height = swapDimensions ? size.Width : size.Height;

            return new Size(width, height);
        }

        public Rect ScreenToView(Rect rect)
        {
            var clippedRect = new Rect(rect);

            clippedRect.Clip(renderDisplayArea);

            if (clippedRect.Empty)
                return null;

            var position = ScreenToView(clippedRect.Position);
            var size = ScreenToView(clippedRect.Size);

            return new Rect(position, size);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (var layer in layers.Values)
                    layer?.Dispose();

                layers.Clear();

                disposed = true;
            }
        }

        public PaletteReplacement PaletteReplacement
        {
            get => paletteReplacement;
            set
            {
                if (paletteReplacement != value)
                {
                    paletteReplacement = value;

                    (GetLayer(Layer.Billboards3D) as RenderLayer).RenderBuffer.Billboard3DShader.SetPaletteReplacement(paletteReplacement);
                    (GetLayer(Layer.Map3D) as RenderLayer).RenderBuffer.Texture3DShader.SetPaletteReplacement(paletteReplacement);
                }
            }
        }

        public PaletteReplacement HorizonPaletteReplacement
        {
            get => horizonPaletteReplacement;
            set
            {
                if (horizonPaletteReplacement != value)
                {
                    horizonPaletteReplacement = value;

                    (GetLayer(Layer.Map3DBackground) as RenderLayer).RenderBuffer.SkyShader.SetPaletteReplacement(horizonPaletteReplacement);
                }
}
        }

        public PaletteFading PaletteFading
        {
            get => paletteFading;
            set
            {
                if (paletteFading != value)
                {
                    paletteFading = value;

                    (GetLayer(Layer.MainMenuGraphics) as RenderLayer).RenderBuffer.FadingTextureShader.SetPaletteFading(paletteFading);
                }
            }
        }

        public int? DrugColorComponent { get; set; } = null;

        public void SetLight(float light)
        {
            (GetLayer(Layer.Billboards3D) as RenderLayer).RenderBuffer.Billboard3DShader.SetLight(light);
            (GetLayer(Layer.Map3D) as RenderLayer).RenderBuffer.Texture3DShader.SetLight(light);
            (GetLayer(Layer.Map3DBackground) as RenderLayer).RenderBuffer.SkyShader.SetLight(light);
        }

        public void SetSkyColorReplacement(uint? skyColor, Color replaceColor)
        {
            (GetLayer(Layer.Billboards3D) as RenderLayer).RenderBuffer.Billboard3DShader.SetSkyColorReplacement(skyColor, replaceColor);
            (GetLayer(Layer.Map3D) as RenderLayer).RenderBuffer.Texture3DShader.SetSkyColorReplacement(skyColor, replaceColor);
        }

        public void SetFog(Color fogColor, float distance)
        {
            (GetLayer(Layer.Billboards3D) as RenderLayer).RenderBuffer.Billboard3DShader.SetFog(fogColor, distance);
            (GetLayer(Layer.Map3D) as RenderLayer).RenderBuffer.Texture3DShader.SetFog(fogColor, distance);
        }
    }
}
