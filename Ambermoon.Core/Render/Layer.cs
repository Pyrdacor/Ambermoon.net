/*
 * Layer.cs - Render layer enumeration
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon
{
    public enum Layer
    {
        // Note: Don't add aliases here as this is used for enumerating over all layers.
        None = -1,
        Map3DBackground, // Color floor, sky, etc
        Map3DBackgroundFog,
        Map3DCeiling,
        Map3D,
        Billboards3D,
        MapBackground1,
        MapBackground2,
        MapBackground3,
        MapBackground4,
        MapBackground5,
        MapBackground6,
        MapBackground7,
        MapBackground8,
        MapBackground9,
        MapBackground10,
        Characters,
        MapForeground1,
        MapForeground2,
        MapForeground3,
        MapForeground4,
        MapForeground5,
        MapForeground6,
        MapForeground7,
        MapForeground8,
        MapForeground9,
        MapForeground10,
        FOW,
        CombatBackground,
        BattleMonsterRow,
        BattleEffects,
        UI,
        Items,
        Text,
        SmallDigits,
        MainMenuGraphics,
        MainMenuText,
        MainMenuEffects,
        IntroGraphics,
        IntroText,
        IntroEffects,
        OutroGraphics,
        OutroText,
        FantasyIntroGraphics,
        FantasyIntroEffects,
        Misc, // general purpose layer
        Images, // non-palette high-resolution images
		MobileOverlays,
		Effects,
        Cursor,
        DrugEffect
    }

    public partial class Global
    {
        public const Layer First2DLayer = Layer.MapBackground1;
        public const Layer Last2DLayer = Layer.MapForeground10;
        public const Layer LastLayer = Layer.DrugEffect;
    }
}
