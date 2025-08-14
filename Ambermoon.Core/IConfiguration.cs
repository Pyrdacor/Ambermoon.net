﻿/*
 * IConfiguration.cs - Configuration interface
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ambermoon
{
    public enum SaveOption
    {
        ProgramFolder,
        DataFolder
    }

    public static class ScreenResolutions
    {
        public static List<Size> GetPossibleResolutions(Size maxSize)
        {
            var resolutions = new List<Size>(4);

            const float defaultResolution = 16.0f / 10.0f;
            float resolution = (float)maxSize.Width / maxSize.Height;

            if (resolution <= defaultResolution + 0.0001f)
            {
                // 16:10, 4:3, etc
                // Width is the leading dimension for resolutions.

                // 4/8, 5/8, 6/8 and 7/8 of max size
                for (int i = 0; i < 4; ++i)
                {
                    int width = maxSize.Width * (4 + i) / 8;
                    resolutions.Add(new Size(width, width * 10 / 16));
                }
            }
            else
            {
                // 16:9, etc
                // Height is the leading dimension for resolutions.

                // 4/8, 5/8, 6/8 and 7/8 of max size
                for (int i = 0; i < 4; ++i)
                {
                    int height = maxSize.Height * (4 + i) / 8;
                    resolutions.Add(new Size(height * 16 / 10, height));
                }
            }

            return resolutions;
        }
    }

    public class AdditionalSavegameSlots
    {
        public string GameVersionName { get; set; }
        public string[] BaseNames { get; set; } = new string[Game.NumBaseSavegameSlots];
        public string[] Names { get; set; } = new string[Game.NumAdditionalSavegameSlots];
        public int ContinueSavegameSlot { get; set; } = 0;
        public DateTime? LastSavesSync { get; set; } = null;

        public static AdditionalSavegameSlots Load(string path)
        {
            return JsonConvert.DeserializeObject<AdditionalSavegameSlots>(File.ReadAllText(path));
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public enum GraphicFilter
    {
        None,        
        Smooth,
        Blur
    }

    public enum GraphicFilterOverlay
    {
        None,
        Lines,
        Grid,
        Scanline,
        CRT
    }

    public enum Effects
    {
        None,
        Grayscale,
        Sepia
    }

    public enum Movement3D
    {
        WASD,
        WASDQE
    }

    public interface IAdditionalSaveSlotProvider
    {
        AdditionalSavegameSlots GetOrCreateAdditionalSavegameNames(string gameVersionName);
        void RequestSave(ISavegameManager savegameManager, IGameData gameData);
    }

    public interface IConfiguration
    {
        bool FirstStart { get; set; }
        bool IsMobile { get; }
        event Action SaveRequested;

        int? Width { get; set; }
        int? Height { get; set; }
        int? FullscreenWidth { get; set; }
        int? FullscreenHeight { get; set; }
        bool Fullscreen { get; set; }
        bool UseDataPath { get; set; }
        string DataPath { get; set; }
        SaveOption SaveOption { get; set; }
        int GameVersionIndex { get; set; }
        bool LegacyMode { get; set; }
        bool Music { get; set; }
        int Volume { get; set; }
        bool ExternalMusic { get; set; }
        [Obsolete("Use BattleSpeed instead.")]
        bool? FastBattleMode { get; set; }
        int BattleSpeed { get; set; }
        [Obsolete("Music is no longer cached but streamed.")]
        bool? CacheMusic { get; set; }
        bool AutoDerune { get; set; }
        bool EnableCheats { get; set; }
        bool ShowButtonTooltips { get; set; }
        bool ShowFantasyIntro { get; set; }
        bool ShowIntro { get; set; }
        [Obsolete("Use GraphicFilter instead.")]
        bool? UseGraphicFilter { get; set; }
        GraphicFilter GraphicFilter { get; set; }
        GraphicFilterOverlay GraphicFilterOverlay { get; set; }
        Effects Effects { get; set; }
        bool ShowPlayerStatsTooltips { get; set; }
        bool ShowPyrdacorLogo { get; set; }
        bool ShowAdvancedLogo { get; set; }
        [Obsolete("Now the fantasy intro is shown instead.")]
        bool? ShowThalionLogo { get; set; }
        bool ShowFloor { get; set; }
        bool ShowCeiling { get; set; }
        bool ShowFog { get; set; }
        bool ExtendedSavegameSlots { get; set; }
        [Obsolete("Use AdditionalSavegameSlots instead.")]
        string[] AdditionalSavegameNames { get; set; }
        [Obsolete("Use AdditionalSavegameSlots instead.")]
        int? ContinueSavegameSlot { get; set; }
        AdditionalSavegameSlots[] AdditionalSavegameSlots { get; set; }
        bool ShowSaveLoadMessage { get; set; }
        Movement3D Movement3D { get; set; }
        bool TurnWithArrowKeys { get; set; }
        GameLanguage Language { get; set; }

        void RequestSave();
        AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots(string gameVersionName);

	}

    public static class ConfigurationExtensions
    {
        public static Size GetScreenResolution(this IConfiguration configuration)
        {
            int? width = configuration.Width;
            int? height = configuration.Height;

            if (width == null && height == null)
                width = 1280;

            if (width != null)
            {
                height = width * 10 / 16;
            }
            else
            {
                width = height * 16 / 10;
            }

            return new Size(width.Value, height.Value);
        }

        public static Size GetScreenSize(this IConfiguration configuration)
        {
            int? width = configuration.Width;
            int? height = configuration.Height;

            if (width != null && height != null)
                return new Size(width.Value, height.Value);

            return GetScreenResolution(configuration);
        }
    }
}
