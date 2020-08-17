/*
 * GameView.cs - Implementation of a game render view
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Ambermoon.Renderer.OpenGL
{
    public delegate bool FullscreenRequestHandler(bool fullscreen);

    public class RenderView : RenderLayerFactory, IRenderView, IDisposable
    {
        bool disposed = false;
        readonly Context context;
        Rect virtualScreenDisplay;
        readonly SizingPolicy sizingPolicy;
        readonly OrientationPolicy orientationPolicy;
        readonly DeviceType deviceType;
        readonly bool isLandscapeRatio = true;
        Rotation rotation = Rotation.None;
        readonly SortedDictionary<Layer, RenderLayer> layers = new SortedDictionary<Layer, RenderLayer>();
        readonly SpriteFactory spriteFactory = null;
        readonly ColoredRectFactory coloredRectFactory = null;
        readonly Surface3DFactory surface3DFactory = null;
        readonly RenderTextFactory renderTextFactory = null;
        readonly Camera3D camera3D = null;
        bool fullscreen = false;

        float sizeFactorX = 1.0f;
        float sizeFactorY = 1.0f;
        Position cursorPosition = new Position();
        Position lastCursorPosition = new Position();

        public event EventHandler Closed;
        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler Drag;
        public event EventHandler KeyPress;
        public event EventHandler SystemKeyPress;
        public event EventHandler StopDrag;
        public FullscreenRequestHandler FullscreenRequestHandler { get; set; }

        public Rect VirtualScreen { get; }

        public ISpriteFactory SpriteFactory => spriteFactory;

        public IColoredRectFactory ColoredRectFactory => coloredRectFactory;

        public ISurface3DFactory Surface3DFactory => surface3DFactory;
        public IRenderTextFactory RenderTextFactory => renderTextFactory;

        public ICamera3D Camera3D => camera3D;

        public IGameData GameData { get; }
        public IGraphicProvider GraphicProvider { get; }
        public ITextProcessor TextProcessor { get; }

        #region Coordinate transformations

        PositionTransformation PositionTransformation => (Position position) =>
        {
            float factorX = (float)virtualScreenDisplay.Size.Width / Global.VirtualScreenWidth;
            float factorY = (float)virtualScreenDisplay.Size.Height / Global.VirtualScreenHeight;

            return new Position(Misc.Round(position.X * factorX), Misc.Round(position.Y * factorY));
        };

        SizeTransformation SizeTransformation => (Size size) =>
        {
            float factorX = (float)virtualScreenDisplay.Size.Width / Global.VirtualScreenWidth;
            float factorY = (float)virtualScreenDisplay.Size.Height / Global.VirtualScreenHeight;

            // don't scale a dimension of 0
            int width = (size.Width == 0) ? 0 : Misc.Ceiling(size.Width * factorX);
            int height = (size.Height == 0) ? 0 : Misc.Ceiling(size.Height * factorY);

            return new Size(width, height);
        };

        #endregion


        public RenderView(IContextProvider contextProvider, IGameData gameData, IGraphicProvider graphicProvider,
            IFontProvider fontProvider, ITextProcessor textProcessor, int screenWidth, int screenHeight,
            DeviceType deviceType = DeviceType.Desktop, SizingPolicy sizingPolicy = SizingPolicy.FitRatio,
            OrientationPolicy orientationPolicy = OrientationPolicy.Support180DegreeRotation)
            : base(new State(contextProvider))
        {
            GameData = gameData;
            GraphicProvider = graphicProvider;
            TextProcessor = textProcessor;
            VirtualScreen = new Rect(0, 0, screenWidth, screenHeight);
            virtualScreenDisplay = new Rect(VirtualScreen);
            this.sizingPolicy = sizingPolicy;
            this.orientationPolicy = orientationPolicy;
            this.deviceType = deviceType;
            isLandscapeRatio = VirtualScreen.Size.Width > VirtualScreen.Size.Height;

            Resize(screenWidth, screenHeight);

            context = new Context(State, virtualScreenDisplay.Size.Width, virtualScreenDisplay.Size.Height);

            // factories
            var visibleArea = new Rect(0, 0, Global.VirtualScreenWidth, Global.VirtualScreenHeight);
            spriteFactory = new SpriteFactory(visibleArea);
            coloredRectFactory = new ColoredRectFactory(visibleArea);
            surface3DFactory = new Surface3DFactory(visibleArea);
            renderTextFactory = new RenderTextFactory(visibleArea);

            camera3D = new Camera3D(State);

            TextureAtlasManager.RegisterFactory(new TextureAtlasBuilderFactory(State));

            var textureAtlasManager = TextureAtlasManager.Instance;
            textureAtlasManager.AddAll(gameData, graphicProvider, fontProvider);
            var palette = textureAtlasManager.CreatePalette(graphicProvider);

            foreach (Layer layer in Enum.GetValues(typeof(Layer)))
            {
                if (layer == Layer.None)
                    continue;

                try
                {
                    var texture = textureAtlasManager.GetOrCreate(layer)?.Texture;
                    var renderLayer = Create(layer, texture, palette);

                    if (layer != Layer.Map3D && layer != Layer.Billboards3D)
                    {
                        renderLayer.PositionTransformation = PositionTransformation;
                        renderLayer.SizeTransformation = SizeTransformation;

                        renderLayer.Visible = true;
                    }

                    AddLayer(renderLayer);
                }
                catch (Exception ex)
                {
                    throw new AmbermoonException(ExceptionScope.Render, $"Unable to create layer '{layer}': {ex.Message}");
                }
            }
        }

        public void Close()
        {
            //GameManager.Instance.GetCurrentGame()?.Close();

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

        public void Resize(int width, int height)
        {
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
            SetRotation(orientation);

            if (sizingPolicy == SizingPolicy.FitWindow ||
                sizingPolicy == SizingPolicy.FitWindowKeepOrientation ||
                sizingPolicy == SizingPolicy.FitWindowForcePortrait ||
                sizingPolicy == SizingPolicy.FitWindowForceLandscape)
            {
                virtualScreenDisplay = new Rect(0, 0, width, height);

                sizeFactorX = 1.0f;
                sizeFactorY = 1.0f;
            }
            else
            {
                float ratio = (float)width / (float)height;
                float virtualRatio = Global.VirtualAspectRatio;

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                    virtualRatio = 1.0f / virtualRatio;

                if (Misc.FloatEqual(ratio, virtualRatio))
                {
                    virtualScreenDisplay = new Rect(0, 0, width, height);
                }
                else if (ratio < virtualRatio)
                {
                    int newHeight = Misc.Round(width / virtualRatio);
                    virtualScreenDisplay = new Rect(0, (height - newHeight) / 2, width, newHeight);
                }
                else // ratio > virtualRatio
                {
                    int newWidth = Misc.Round(height * virtualRatio);
                    virtualScreenDisplay = new Rect((width - newWidth) / 2, 0, newWidth, height);
                }

                if (rotation == Rotation.Deg90 || rotation == Rotation.Deg270)
                {
                    sizeFactorX = (float)VirtualScreen.Size.Height / (float)virtualScreenDisplay.Size.Width;
                    sizeFactorY = (float)VirtualScreen.Size.Width / (float)virtualScreenDisplay.Size.Height;
                }
                else
                {
                    sizeFactorX = (float)VirtualScreen.Size.Width / (float)virtualScreenDisplay.Size.Width;
                    sizeFactorY = (float)VirtualScreen.Size.Height / (float)virtualScreenDisplay.Size.Height;
                }
            }

            State.Gl.Viewport(virtualScreenDisplay.Position.X, virtualScreenDisplay.Position.Y,
                (uint)virtualScreenDisplay.Size.Width, (uint)virtualScreenDisplay.Size.Height);
        }

        public void AddLayer(IRenderLayer layer)
        {
            if (!(layer is RenderLayer))
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

        public void Render()
        {
            if (disposed)
                return;

            context.SetRotation(rotation);

            State.Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

            bool render3DMap = layers[Layer.Map3D].Visible;

            foreach (var layer in layers)
            {
                if (render3DMap)
                {
                    if (layer.Key == Layer.Map3D)
                    {
                        // Setup 3D stuff
                        camera3D.Activate();
                        State.RestoreProjectionMatrix(State.ProjectionMatrix3D);
                        var mapViewArea = new Rect(Global.Map3DViewX, Global.Map3DViewY, Global.Map3DViewWidth, Global.Map3DViewHeight);
                        mapViewArea.Position = PositionTransformation(mapViewArea.Position);
                        mapViewArea.Size = SizeTransformation(mapViewArea.Size);
                        State.Gl.Viewport
                        (
                            virtualScreenDisplay.Position.X + mapViewArea.Position.X,
                            VirtualScreen.Size.Height - (virtualScreenDisplay.Position.Y + mapViewArea.Position.Y + mapViewArea.Size.Height),
                            (uint)mapViewArea.Size.Width, (uint)mapViewArea.Size.Height
                        );
                        State.Gl.Enable(EnableCap.CullFace);
                    }
                    else if (layer.Key == Layer.Billboards3D)
                    {
                        camera3D.ActivateBillboards(layer.Value.RenderBuffer.Billboard3DShader);
                        layer.Value.RenderBuffer.Billboard3DShader.SetScale(1.0f);
                        State.Gl.Disable(EnableCap.CullFace);
                    }
                    else if (layer.Key == Global.First2DLayer)
                    {
                        // Reset to 2D stuff
                        State.RestoreModelViewMatrix(Matrix4.Identity);
                        State.RestoreProjectionMatrix(State.ProjectionMatrix2D);
                        State.Gl.Viewport(virtualScreenDisplay.Position.X, virtualScreenDisplay.Position.Y,
                            (uint)virtualScreenDisplay.Size.Width, (uint)virtualScreenDisplay.Size.Height);
                    }
                }

                layer.Value.Render();
            }
        }

        public void SetCursorPosition(int x, int y)
        {
            cursorPosition.X = x;
            cursorPosition.Y = y;

            cursorPosition = ScreenToView(cursorPosition);

            if (cursorPosition == null)
                cursorPosition = lastCursorPosition;
            else
                lastCursorPosition = cursorPosition;
        }

        public Position ScreenToView(Position position)
        {
            if (!virtualScreenDisplay.Contains(position))
                return null;

            int relX = position.X - virtualScreenDisplay.Left;
            int relY = position.Y - virtualScreenDisplay.Top;
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
                    rotatedY = virtualScreenDisplay.Size.Width - relX;
                    break;
                case Rotation.Deg180:
                    rotatedX = virtualScreenDisplay.Size.Width - relX;
                    rotatedY = virtualScreenDisplay.Size.Height - relY;
                    break;
                case Rotation.Deg270:
                    rotatedX = virtualScreenDisplay.Size.Height - relY;
                    rotatedY = relX;
                    break;
            }

            int x = Misc.Round(sizeFactorX * rotatedX);
            int y = Misc.Round(sizeFactorY * rotatedY);

            return new Position(x, y);
        }

        public Size ScreenToView(Size size)
        {
            bool swapDimensions = rotation == Rotation.Deg90 || rotation == Rotation.Deg270;

            int width = (swapDimensions) ? size.Height : size.Width;
            int height = (swapDimensions) ? size.Width : size.Height;

            return new Size(Misc.Round(sizeFactorX * width), Misc.Round(sizeFactorY * height));
        }

        public Rect ScreenToView(Rect rect)
        {
            var clippedRect = new Rect(rect);

            clippedRect.Clip(virtualScreenDisplay);

            if (clippedRect.Empty)
                return null;

            var position = ScreenToView(clippedRect.Position);
            var size = ScreenToView(clippedRect.Size);

            return new Rect(position, size);
        }

        bool RunHandler(EventHandler handler, EventArgs args)
        {
            /*bool? handlerResult = handler?.Invoke(this, args);

            if (handlerResult.HasValue)
                args.Done = handlerResult.Value;

            return args.Done;*/
            return false; //TODO
        }

        /*public bool NotifyClick(int x, int y, Button button, bool delayed)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(Click, new EventArgs(delayed ? EventType.DelayedClick : EventType.Click, position.X, position.Y, 0, 0, button));
        }

        public bool NotifyDoubleClick(int x, int y, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            return RunHandler(DoubleClick, new EventArgs(EventType.DoubleClick, position.X, position.Y, 0, 0, button));
        }

        public bool NotifySpecialClick(int x, int y)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));

            if (position == null)
                return false;

            // The special click is mapped to a double click with left mouse button
            return RunHandler(SpecialClick, new EventArgs(EventType.SpecialClick, position.X, position.Y, 0, 0, Button.Left));
        }

        public bool NotifyDrag(int x, int y, int dx, int dy, Button button)
        {
            // transform from screen to view
            var position = ScreenToView(new Position(x, y));
            var delta = ScreenToView(new Size(dx, dy));

            if (position == null)
                position = new Position();

            return RunHandler(Drag, new EventArgs(EventType.Drag, position.X, position.Y, delta.Width, delta.Height, button));
        }

        public bool NotifyStopDrag()
        {
            return RunHandler(StopDrag, new EventArgs(EventType.StopDrag, 0, 0, 0, 0));
        }

        public bool NotifyKeyPressed(char key, byte modifier)
        {
            return RunHandler(KeyPress, new EventArgs(EventType.KeyPressed, 0, 0, (byte)key, modifier));
        }

        public bool NotifySystemKeyPressed(SystemKey key, byte modifier)
        {
            return RunHandler(SystemKeyPress, new EventArgs(EventType.SystemKeyPressed, 0, 0, (int)key, modifier));
        }*/

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var layer in layers.Values)
                        layer?.Dispose();

                    layers.Clear();

                    disposed = true;
                }
            }
        }
    }
}
