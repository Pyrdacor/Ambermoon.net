/*
 * IRenderView.cs - Render view interface
 *
 * Copyright (C) 2020-2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon.Render;

public enum WindowMode
{
    Normal,
    Fullscreen,
    FullsizedWindow,
}

public interface IRenderView : IRenderLayerFactory
{
    void Render(FloatPosition viewportOffset);
    void AddLayer(IRenderLayer layer);
    IRenderLayer GetLayer(Layer layer);
    void Resize(int width, int height, int? windowWidth = null, int? windowHeight = null);
    void Close();
    void UsePalette(Layer layer, bool use);
    void SetTextureFactor(Layer layer, uint factor);
    Position GameToScreen(Position position);
    Position ViewToScreen(Position position);
    Size ViewToScreen(Size size);
    Rect GameToScreen(Rect rect);
    Rect ViewToScreen(Rect rect);
    Position ScreenToLayer(Position position, Layer layer);
    Position ScreenToGame(Position position);
    Position ScreenToView(Position position);
    Size ScreenToView(Size size);
    Rect ScreenToView(Rect rect);
    void TakeScreenshot(Action<int, int, byte[]> dataHandler);
    Rect RenderScreenArea { get; }
    WindowMode WindowMode { get; set; }
    bool AllowFramebuffer { get; }
    bool AllowEffects { get; }
    bool ShowImageLayerOnly { get; set; }
    ISpriteFactory SpriteFactory { get; }
    IColoredRectFactory ColoredRectFactory { get; }
}

public interface IGameRenderView : IRenderView
{
    IGameData GameData { get; }
    IGraphicProvider GraphicProvider { get; }
    IFontProvider FontProvider { get; }       
    ISurface3DFactory Surface3DFactory { get; }
    IRenderTextFactory RenderTextFactory { get; }
    IFowFactory FowFactory { get; }
    ITextProcessor TextProcessor { get; }
    ICamera3D Camera3D { get; }
    Action<float> AspectProcessor { get; }
    void SetLight(float light);
    void Set3DFade(float fade);
    void SetSkyColorReplacement(uint? skyColor, Color replaceColor);
    PaletteReplacement PaletteReplacement { get; set; }
    PaletteReplacement HorizonPaletteReplacement { get; set; }
    int? DrugColorComponent { get; set; }
    PaletteFading PaletteFading { get; set; }
    void SetFog(Color fogColor, float distance);
}
