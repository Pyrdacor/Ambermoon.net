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
using System.Linq;
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
    // small arrows (8x6) to expand or collapse the portraits in the immersive mode
    public const uint PortraitsArrowUpGraphicIndex = 1007u;
    public const uint PortraitsArrowDownGraphicIndex = 1008u;
    public const int IconSize = 16;

    // Same style as the button page arrow (UI palette colors 29 and 31).
    // . = transparent, M = medium, B = bright
    static readonly string[] PortraitsArrowUp =
    [
        "...B....",
        "..BMB...",
        ".BMMMB..",
        "BMMMMMB.",
        "..MBM...",
        "..MBM...",
    ];

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

    static readonly Dictionary<GameLanguage, string> portraitsTooltips = new()
    {
        { GameLanguage.English, "Toggle portraits (P)" },
        { GameLanguage.German, "Portraits umschalten (P)" },
        { GameLanguage.French, "Afficher/masquer les portraits (P)" },
        { GameLanguage.Polish, "Przełącz portrety (P)" },
        { GameLanguage.Czech, "Přepnout portréty (P)" },
    };

    public static string GetPortraitsTooltip(GameLanguage gameLanguage) =>
        portraitsTooltips.TryGetValue(gameLanguage, out var tooltip) ? tooltip : portraitsTooltips[GameLanguage.English];

    static Graphic CreateSmallArrow(string[] rows, bool flipVertically)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        var graphic = new Graphic(width, height, 0);

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                graphic.Data[x + y * width] = rows[flipVertically ? height - 1 - y : y][x] switch
                {
                    'M' => 29,
                    'B' => 31,
                    _ => 0 // transparent
                };
            }
        }

        return graphic;
    }

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

    /// <summary>
    /// Size of the arrows which show or hide the action buttons.
    /// </summary>
    public static Size ButtonsArrowSize { get; private set; } = new(IconSize, IconSize);
    /// <summary>
    /// Size of the arrows which expand or collapse the portraits.
    /// </summary>
    public static Size PortraitsArrowSize { get; private set; } = new(8, 6);

    // The tips of the 2D movement cursors (up, down, left, right) are used as arrows.
    // The area of each tip inside the 16x16 cursor graphic.
    static readonly Rect CursorTipUpArea = new(2, 2, 11, 7);
    static readonly Rect CursorTipDownArea = new(3, 7, 11, 7);
    static readonly Rect CursorTipLeftArea = new(2, 3, 7, 11);
    static readonly Rect CursorTipRightArea = new(7, 2, 7, 11);
    const byte CursorOutlineColorIndex = 26;

    /// <summary>
    /// Cuts the tip of a movement cursor. The side where the shaft starts is closed with the outline color.
    /// </summary>
    static Graphic CutCursorTip(Graphic cursor, Rect area, CursorType direction)
    {
        var tip = new Graphic(area.Width, area.Height, 0);

        for (int y = 0; y < area.Height; ++y)
        {
            for (int x = 0; x < area.Width; ++x)
            {
                byte color = cursor.Data[area.X + x + (area.Y + y) * cursor.Width];
                bool shaftSide = direction switch
                {
                    CursorType.ArrowUp => y == area.Height - 1,
                    CursorType.ArrowDown => y == 0,
                    CursorType.ArrowLeft => x == area.Width - 1,
                    _ => x == 0
                };
                tip.Data[x + y * area.Width] = color != 0 && shaftSide ? CursorOutlineColorIndex : color;
            }
        }

        return tip;
    }

    /// <summary>
    /// Creates the graphics of the immersive mode. If the cursor graphics are given (not available
    /// if the game data only provides a graphic atlas), the arrows are the tips of the movement cursors.
    /// </summary>
    public static Dictionary<uint, Graphic> GetGraphics(IReadOnlyList<Graphic>? cursorGraphics)
    {
        var graphics = new Dictionary<uint, Graphic>
        {
            { ImmersiveIconGraphicIndex, CreateGraphic(ImmersiveIcon, false) }
        };

        if (cursorGraphics != null && cursorGraphics.Count > (int)CursorType.ArrowRight &&
            cursorGraphics.Take((int)CursorType.ArrowRight + 1).Skip(1).All(cursor => cursor.Width == 16 && cursor.Height == 16))
        {
            graphics.Add(ArrowLeftGraphicIndex, CutCursorTip(cursorGraphics[(int)CursorType.ArrowLeft], CursorTipLeftArea, CursorType.ArrowLeft));
            graphics.Add(ArrowRightGraphicIndex, CutCursorTip(cursorGraphics[(int)CursorType.ArrowRight], CursorTipRightArea, CursorType.ArrowRight));
            graphics.Add(PortraitsArrowUpGraphicIndex, CutCursorTip(cursorGraphics[(int)CursorType.ArrowUp], CursorTipUpArea, CursorType.ArrowUp));
            graphics.Add(PortraitsArrowDownGraphicIndex, CutCursorTip(cursorGraphics[(int)CursorType.ArrowDown], CursorTipDownArea, CursorType.ArrowDown));
            ButtonsArrowSize = new Size(CursorTipLeftArea.Width, CursorTipLeftArea.Height);
            PortraitsArrowSize = new Size(CursorTipUpArea.Width, CursorTipUpArea.Height);
        }
        else
        {
            graphics.Add(ArrowLeftGraphicIndex, CreateGraphic(ArrowLeft, false));
            graphics.Add(ArrowRightGraphicIndex, CreateGraphic(ArrowLeft, true));
            graphics.Add(PortraitsArrowUpGraphicIndex, CreateSmallArrow(PortraitsArrowUp, false));
            graphics.Add(PortraitsArrowDownGraphicIndex, CreateSmallArrow(PortraitsArrowUp, true));
            ButtonsArrowSize = new Size(IconSize, IconSize);
            PortraitsArrowSize = new Size(8, 6);
        }

        return graphics;
    }
}
