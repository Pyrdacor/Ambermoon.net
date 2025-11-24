/*
 * IRenderText.cs - Render text interface
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
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.Render
{
    public interface IRenderText : IRenderNode
    {
        void Place(int x, int y);
        void Place(Rect rect, TextAlign textAlign = TextAlign.Left);
        TextColor TextColor { get; set; }
        TextAlign TextAlign { get; set; }
        bool Shadow { get; set; }
        IText Text { get; set; }
        byte DisplayLayer { get; set; }
        byte PaletteIndex { get; set; }
        IReadOnlyList<TextColor> GetTextColorPerLine(IText text);
    }

    public interface IRenderTextFactory
    {
        /// <summary>
        /// Mapping from glyph code to texture index.
        /// </summary>
        Dictionary<byte, Position> GlyphTextureMapping { get; set; }
        /// <summary>
        /// Mapping from digit (0 to 9) to texture index.
        /// </summary>
        Dictionary<byte, Position> DigitGlyphTextureMapping { get; set; }
        IRenderText Create(byte defaultTextPaletteIndex);
        IRenderText Create(byte defaultTextPaletteIndex, IRenderLayer layer, IText text, TextColor textColor, bool shadow);
        IRenderText Create(byte defaultTextPaletteIndex, IRenderLayer layer, IText text, TextColor textColor, bool shadow,
            Rect bounds, TextAlign textAlign = TextAlign.Left);
        IRenderText Create(byte defaultTextPaletteIndex, IRenderLayer layer, IText text, TextColor textColor, bool shadow,
            Rect bounds, int positionFactor, int sizeFactor, TextAlign textAlign = TextAlign.Left);
        IRenderText CreateDigits(byte defaultTextPaletteIndex, IRenderLayer layer, IText digits, TextColor textColor,
            bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left);
    }
}
