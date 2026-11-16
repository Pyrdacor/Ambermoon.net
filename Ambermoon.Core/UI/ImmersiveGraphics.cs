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
