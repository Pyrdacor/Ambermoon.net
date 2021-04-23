/*
 * IRenderView.cs - Render view interface
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
using System;

namespace Ambermoon.Render
{
    public interface IRenderView : IRenderLayerFactory
    {
        void Render(FloatPosition viewportOffset);
        void AddLayer(IRenderLayer layer);
        IRenderLayer GetLayer(Layer layer);
        void Resize(int width, int height);
        void Close();

        Position GameToScreen(Position position);
        Position ViewToScreen(Position position);
        Size ViewToScreen(Size size);
        Rect GameToScreen(Rect rect);
        Rect ViewToScreen(Rect rect);
        Position ScreenToGame(Position position);
        Position ScreenToView(Position position);
        Size ScreenToView(Size size);
        Rect ScreenToView(Rect rect);

        Rect WindowArea { get; }
        Size MaxScreenSize { get; }
        bool Fullscreen { get; set; }

        ISpriteFactory SpriteFactory { get; }
        IColoredRectFactory ColoredRectFactory { get; }
        ISurface3DFactory Surface3DFactory { get; }
        IRenderTextFactory RenderTextFactory { get; }
        IFowFactory FowFactory { get; }
        ITextProcessor TextProcessor { get; }
        ICamera3D Camera3D { get; }
        Action<float> AspectProcessor { get; }
        void SetLight(float light);

        IGameData GameData { get; }
        IGraphicProvider GraphicProvider { get; }
    }
}
