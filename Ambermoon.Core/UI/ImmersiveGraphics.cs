/*
 * ImmersiveGraphics.cs - Icons of the immersive mode
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Data.Enumerations;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.UI;

/// <summary>
/// The icons of the immersive mode are created in code. They use the UI palette
/// and are stored in the texture atlas of the layer Misc (like the quest log icon).
/// </summary>
public static class ImmersiveGraphics
{
    public const uint ImmersiveIconGraphicIndex = 1001u;
    public const uint ArrowLeftGraphicIndex = 1002u;
    public const uint ArrowRightGraphicIndex = 1003u;
    // The arrow to switch the button grid page in the normal mode. It is a 8x6 3-bit
    // palette graphic (palette offset 24) which is loaded from the resources.
    public const uint ButtonPageArrowGraphicIndex = 1006u;
    public const int IconSize = 16;

    // . = transparent, D = dark outline, W = bright
    static readonly string[] ImmersiveIcon =
    [
        "DDDDDD....DDDDDD",
        "DWWWWD....DWWWWD",
        "DWWWWD....DWWWWD",
        "DWWDDD....DDDWWD",
        "DWWD........DWWD",
        "DDDD........DDDD",
        "................",
        "................",
        "................",
        "................",
        "DDDD........DDDD",
        "DWWD........DWWD",
        "DWWDDD....DDDWWD",
        "DWWWWD....DWWWWD",
        "DWWWWD....DWWWWD",
        "DDDDDD....DDDDDD",
    ];

    static readonly string[] ArrowLeft =
    [
        "................",
        "................",
        ".........DD.....",
        "........DWD.....",
        ".......DWWD.....",
        "......DWWWD.....",
        ".....DWWWWD.....",
        "....DWWWWWD.....",
        "....DWWWWWD.....",
        ".....DWWWWD.....",
        "......DWWWD.....",
        ".......DWWD.....",
        "........DWD.....",
        ".........DD.....",
        "................",
        "................",
    ];

    static readonly Dictionary<GameLanguage, string> buttonPageTooltips = new()
    {
        { GameLanguage.English, "Toggle buttons" },
        { GameLanguage.German, "Buttons umschalten" },
        { GameLanguage.French, "Changer les boutons" },
        { GameLanguage.Polish, "Przełącz przyciski" },
        { GameLanguage.Czech, "Přepnout tlačítka" },
    };

    public static string GetButtonPageTooltip(GameLanguage gameLanguage) =>
        buttonPageTooltips.TryGetValue(gameLanguage, out var tooltip) ? tooltip : buttonPageTooltips[GameLanguage.English];

    // The special item graphics (windchain, monster eye and the day time pictures) are
    // shown without their frame in the immersive mode. The frame uses this color index.
    const byte SpecialItemFrameColorIndex = 28;
    static readonly UIGraphic[] FramelessSpecialItemGraphics =
    [
        UIGraphic.Windchain, UIGraphic.MonsterEyeInactive, UIGraphic.MonsterEyeActive,
        UIGraphic.Night, UIGraphic.Dusk, UIGraphic.Day, UIGraphic.Dawn
    ];
    static readonly Dictionary<UIGraphic, Size> framelessSpecialItemSizes = [];

    /// <summary>
    /// Returns the index of the frameless version of a special item graphic in the UI layer
    /// or null if there is none (e.g. if the game data only provides a graphic atlas).
    /// </summary>
    public static uint? GetFramelessGraphicIndex(UIGraphic graphic) => framelessSpecialItemSizes.ContainsKey(graphic)
        ? Graphics.FramelessSpecialItemOffset + (uint)Array.IndexOf(FramelessSpecialItemGraphics, graphic)
        : null;

    public static Size? GetFramelessGraphicSize(UIGraphic graphic) =>
        framelessSpecialItemSizes.TryGetValue(graphic, out var size) ? size : null;

    /// <summary>
    /// Creates the frameless versions of the special item graphics. The frame is removed by
    /// a flood fill from the border (the frame color is also used inside the pictures) and
    /// the graphic is cropped to the remaining pixels.
    /// </summary>
    internal static Dictionary<uint, Graphic> CreateFramelessSpecialItemGraphics(Func<UIGraphic, Graphic> graphicProvider)
    {
        var graphics = new Dictionary<uint, Graphic>();
        framelessSpecialItemSizes.Clear();

        for (int i = 0; i < FramelessSpecialItemGraphics.Length; ++i)
        {
            var uiGraphic = FramelessSpecialItemGraphics[i];
            var frameless = RemoveFrame(graphicProvider(uiGraphic));
            graphics.Add(Graphics.FramelessSpecialItemOffset + (uint)i, frameless);
            framelessSpecialItemSizes.Add(uiGraphic, new Size(frameless.Width, frameless.Height));
        }

        return graphics;
    }

    static Graphic RemoveFrame(Graphic graphic)
    {
        int width = graphic.Width;
        int height = graphic.Height;
        var data = (byte[])graphic.Data.Clone();
        var pending = new Stack<(int X, int Y)>();

        for (int x = 0; x < width; ++x)
        {
            pending.Push((x, 0));
            pending.Push((x, height - 1));
        }

        for (int y = 0; y < height; ++y)
        {
            pending.Push((0, y));
            pending.Push((width - 1, y));
        }

        while (pending.Count != 0)
        {
            var (x, y) = pending.Pop();

            if (x < 0 || y < 0 || x >= width || y >= height || data[x + y * width] != SpecialItemFrameColorIndex)
                continue;

            data[x + y * width] = 0; // transparent

            pending.Push((x - 1, y));
            pending.Push((x + 1, y));
            pending.Push((x, y - 1));
            pending.Push((x, y + 1));
        }

        int left = width, top = height, right = -1, bottom = -1;

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (data[x + y * width] == 0)
                    continue;

                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left)
            return new Graphic(graphic);

        var result = new Graphic(right - left + 1, bottom - top + 1, 0) { IndexedGraphic = graphic.IndexedGraphic };

        for (int y = top; y <= bottom; ++y)
            Array.Copy(data, left + y * width, result.Data, (y - top) * result.Width, result.Width);

        return result;
    }

    static Graphic CreateGraphic(string[] rows, bool mirror)
    {
        var graphic = new Graphic(IconSize, IconSize, 0);

        for (int y = 0; y < IconSize; ++y)
        {
            for (int x = 0; x < IconSize; ++x)
            {
                char pixel = rows[y][mirror ? IconSize - 1 - x : x];

                graphic.Data[x + y * IconSize] = pixel switch
                {
                    'D' => (byte)TextColor.VeryDarkGray,
                    'W' => (byte)TextColor.BrightGray,
                    _ => 0 // transparent
                };
            }
        }

        return graphic;
    }

    public static Dictionary<uint, Graphic> GetGraphics() => new()
    {
        { ImmersiveIconGraphicIndex, CreateGraphic(ImmersiveIcon, false) },
        { ArrowLeftGraphicIndex, CreateGraphic(ArrowLeft, false) },
        { ArrowRightGraphicIndex, CreateGraphic(ArrowLeft, true) },
    };
}
