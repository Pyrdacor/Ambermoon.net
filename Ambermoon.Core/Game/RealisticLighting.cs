/*
 * RealisticLighting.cs - Realistic lighting (torch light, light sources, day and night)
 *
 * Copyright (C) 2020-2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

namespace Ambermoon;

partial class GameCore
{
    /// <summary>
    /// Ambient light of outdoor maps over the day (hour, red, green, blue).
    /// The values are interpolated linearly.
    /// </summary>
    static readonly (float Hour, float R, float G, float B)[] DaylightCurve =
    [
        (0.0f, 0.20f, 0.24f, 0.40f),
        (4.5f, 0.20f, 0.24f, 0.40f),
        (6.0f, 0.52f, 0.44f, 0.52f), // dawn
        (7.0f, 0.95f, 0.78f, 0.62f), // sunrise
        (8.5f, 1.00f, 1.00f, 1.00f),
        (16.5f, 1.00f, 1.00f, 1.00f),
        (18.0f, 1.00f, 0.82f, 0.60f), // golden hour
        (19.0f, 0.82f, 0.54f, 0.44f), // sunset
        (20.0f, 0.38f, 0.34f, 0.50f), // twilight
        (21.0f, 0.20f, 0.24f, 0.40f),
        (24.0f, 0.20f, 0.24f, 0.40f),
    ];

    /// <summary>
    /// Torch of the light spell per spell level (1 to 3): color, intensity, range (in tiles) and flickering.
    /// </summary>
    static readonly LightSourceInfo[] SpellTorches =
    [
        new(1.0f, 0.78f, 0.52f, 1.35f, 3.5f, 1.00f), // magical torch
        new(1.0f, 0.85f, 0.62f, 1.45f, 6.0f, 0.60f), // magical lantern
        new(1.0f, 0.93f, 0.80f, 1.60f, 8.5f, 0.25f), // magical sun
    ];

    const int Realistic2DTileMargin = 4;

    readonly RealisticLighting3D realisticLighting3D = new();
    readonly RealisticLighting2D realisticLighting2D = new();
    readonly List<(int X, int Y, uint MapX, uint MapY, LightSourceInfo Info)> realisticLightSources2D = [];
    readonly List<(float Score, RenderMap3D.WorldLightSource Light)> realisticLightSources3D = [];
    bool realisticLightingActive = false;
    double realisticLightingTime = 0.0;
    float realisticAmbientR = 1.0f;
    float realisticAmbientG = 1.0f;
    float realisticAmbientB = 1.0f;
    bool realisticAmbientInitialized = false;

    bool RealisticLightingEnabled => CoreConfiguration.RealisticLighting;

    /// <summary>
    /// Called when the option was toggled.
    /// </summary>
    internal void RealisticLightingChanged()
    {
        realisticAmbientInitialized = false;

        if (!RealisticLightingEnabled)
            DisableRealisticLighting();

        if (Ingame && Map != null && CurrentPartyMember != null && (Is3D ? renderMap3D != null : renderMap2D != null))
            UpdateLight(true); // restores the fog of war, sky, palette replacements, etc
    }

    void DisableRealisticLighting()
    {
        if (!realisticLightingActive)
            return;

        realisticLightingActive = false;
        renderView.SetRealisticLighting3D(null);
        renderView.SetRealisticLighting2D(null);
    }

    /// <summary>
    /// Value noise in the range 0 to 1.
    /// </summary>
    static float Noise(double t, int seed)
    {
        static float Hash(long n)
        {
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / (float)0x7fffffff;
        }

        long i = (long)Math.Floor(t);
        float f = (float)(t - i);
        f = f * f * (3.0f - 2.0f * f);
        float a = Hash(i + seed * 157L);
        float b = Hash(i + 1 + seed * 157L);

        return a + (b - a) * f;
    }

    /// <summary>
    /// Flickering factor of a flame (around 1).
    /// </summary>
    static float Flicker(double time, int seed, float amount)
    {
        if (amount <= 0.0f)
            return 1.0f;

        double s = (seed % 1000) * 0.731;
        float waves = (float)(0.5 * Math.Sin(time * 7.3 + s) + 0.3 * Math.Sin(time * 17.9 + s * 1.7) + 0.2 * Math.Sin(time * 31.1 + s * 2.3));
        float noise = Noise(time * 11.0, seed) * 2.0f - 1.0f;
        // Sometimes the flame shrinks for a short moment
        float dip = Math.Max(0.0f, Noise(time * 2.3, seed + 31) - 0.75f) * 1.2f;

        return Math.Max(0.4f, 1.0f + amount * (0.07f * waves + 0.09f * noise - dip));
    }

    static (float R, float G, float B) GetDaylight(uint hour, uint minute)
    {
        float time = Math.Min(23.999f, hour + Math.Min(59, minute) / 60.0f);

        for (int i = 1; i < DaylightCurve.Length; ++i)
        {
            var next = DaylightCurve[i];

            if (time <= next.Hour)
            {
                var prev = DaylightCurve[i - 1];
                float f = (time - prev.Hour) / (next.Hour - prev.Hour);

                return (prev.R + (next.R - prev.R) * f, prev.G + (next.G - prev.G) * f, prev.B + (next.B - prev.B) * f);
            }
        }

        var last = DaylightCurve[^1];

        return (last.R, last.G, last.B);
    }

    static float Luminance(float r, float g, float b) => 0.3f * r + 0.59f * g + 0.11f * b;

    /// <summary>
    /// Ambient light of the current map.
    /// </summary>
    (float R, float G, float B) GetRealisticAmbientLight(Map map)
    {
        if (TravelType == TravelType.Fly)
            return (1.0f, 1.0f, 1.0f);

        if (map.Flags.HasFlag(MapFlags.Outdoor))
            return GetDaylight(GameTime!.Hour, GameTime.Minute);

        if (map.Flags.HasFlag(MapFlags.Indoor))
        {
            if (map.Flags.HasFlag(MapFlags.Sky) && map.Type == MapType.Map3D)
            {
                // Towns are always lit in the original. Now they follow the daytime
                // but are a bit brighter at night (light from the houses, street lamps).
                var daylight = GetDaylight(GameTime!.Hour, GameTime.Minute);
                const float townLight = 0.25f;

                return (daylight.R + (1.0f - daylight.R) * townLight, daylight.G + (1.0f - daylight.G) * townLight,
                    daylight.B + (1.0f - daylight.B) * townLight);
            }

            return (0.8f, 0.8f, 0.8f);
        }

        // Dungeons are dark
        return (0.0f, 0.0f, 0.0f);
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    void UpdateRealisticLighting(double deltaTime)
    {
        if (!RealisticLightingEnabled)
        {
            DisableRealisticLighting();
            return;
        }

        var map = Map;

        if (map == null || CurrentPartyMember == null || CurrentSavegame == null || GameTime == null ||
            (Is3D ? renderMap3D == null || player3D == null : renderMap2D == null || player2D == null))
            return;

        realisticLightingActive = true;
        realisticLightingTime += deltaTime;

        // The ambient light changes smoothly
        var ambient = GetRealisticAmbientLight(map);

        if (!realisticAmbientInitialized)
        {
            realisticAmbientR = ambient.R;
            realisticAmbientG = ambient.G;
            realisticAmbientB = ambient.B;
            realisticAmbientInitialized = true;
        }
        else
        {
            float f = 1.0f - (float)Math.Exp(-deltaTime * 2.0);
            realisticAmbientR += (ambient.R - realisticAmbientR) * f;
            realisticAmbientG += (ambient.G - realisticAmbientG) * f;
            realisticAmbientB += (ambient.B - realisticAmbientB) * f;
        }

        bool blind = CurrentPartyMember.Conditions.HasFlag(Condition.Blind);
        uint spellLevel = Math.Min(3, CurrentSavegame.GetActiveSpellLevel(ActiveSpellType.Light));
        var torch = spellLevel == 0 ? (LightSourceInfo?)null : SpellTorches[spellLevel - 1];
        float ambientLuminance = Luminance(realisticAmbientR, realisticAmbientG, realisticAmbientB);
        // The torch is less visible in bright environments
        float torchVisibility = map.Flags.HasFlag(MapFlags.Indoor) && !(map.Flags.HasFlag(MapFlags.Sky) && map.Type == MapType.Map3D)
            ? 0.3f + 0.8f * (1.0f - ambientLuminance)
            : 0.1f + 0.9f * (1.0f - ambientLuminance);

        if (Is3D)
            UpdateRealisticLighting3D(map, blind, torch, torchVisibility);
        else
            UpdateRealisticLighting2D(map, blind, spellLevel, torch, torchVisibility);
    }

    void UpdateRealisticLighting3D(Map map, bool blind, LightSourceInfo? torch, float torchVisibility)
    {
        var lighting = realisticLighting3D;
        double time = realisticLightingTime;

        lighting.AmbientR = realisticAmbientR;
        lighting.AmbientG = realisticAmbientG;
        lighting.AmbientB = realisticAmbientB;
        lighting.LightCount = 0;

        if (torch != null && !blind)
        {
            var info = torch.Value;
            float flicker = Flicker(time, 0, info.Flicker);
            float intensity = info.Intensity * flicker * torchVisibility;
            lighting.TorchR = info.R * intensity;
            lighting.TorchG = info.G * intensity;
            lighting.TorchB = info.B * intensity;
            // The range flickers a bit as well
            lighting.TorchRange = info.Radius * Global.DistancePerBlock * (0.92f + 0.08f * flicker);
            // The torch is carried a bit to the right and below the eyes and sways slightly.
            lighting.TorchX = 0.18f + 0.015f * (float)Math.Sin(time * 1.7);
            lighting.TorchY = -0.12f + 0.012f * (float)Math.Sin(time * 2.9 + 0.5);
            lighting.TorchZ = -0.05f;
        }
        else
        {
            lighting.TorchR = lighting.TorchG = lighting.TorchB = 0.0f;
            lighting.TorchRange = 0.0f;
        }

        if (!blind)
        {
            // Use the nearest light sources
            var camera = player3D!.Camera;
            float cameraX = -camera.X;
            float cameraZ = -camera.Z;
            const float maxDistance = 12.0f * Global.DistancePerBlock;
            var candidates = realisticLightSources3D;
            candidates.Clear();

            foreach (var light in renderMap3D!.WorldLightSources)
            {
                float dx = light.X - cameraX;
                float dz = light.Z - cameraZ;
                float distance = (float)Math.Sqrt(dx * dx + dz * dz);
                float radius = light.Info.Radius * Global.DistancePerBlock;

                if (distance - radius > maxDistance)
                    continue;

                candidates.Add((distance - radius, light));
            }

            if (candidates.Count > RealisticLighting3D.MaxLights)
                candidates.Sort((a, b) => a.Score.CompareTo(b.Score));

            int count = Math.Min(RealisticLighting3D.MaxLights, candidates.Count);

            for (int i = 0; i < count; ++i)
            {
                var light = candidates[i].Light;
                var info = light.Info;
                float flicker = Flicker(time, light.Seed, info.Flicker);
                float intensity = info.Intensity * flicker;

                // Lights which are far away fade out so that switching lights is not visible.
                float fade = Math.Clamp(1.0f - (candidates[i].Score - (maxDistance - 3.0f)) / 3.0f, 0.0f, 1.0f);
                intensity *= fade;

                lighting.Lights[i] = new RealisticPointLight
                {
                    X = light.X,
                    Y = light.Y,
                    Z = light.Z,
                    Radius = info.Radius * Global.DistancePerBlock * (0.95f + 0.05f * flicker),
                    R = info.R * intensity,
                    G = info.G * intensity,
                    B = info.B * intensity
                };
            }

            lighting.LightCount = count;
        }

        renderView.SetRealisticLighting3D(lighting);
        renderView.SetRealisticLighting2D(null);

        // The floor and ceiling colors are used when no floor or ceiling textures are shown.
        float torchAmount = lighting.TorchRange > 0.0f ? 0.25f * Luminance(lighting.TorchR, lighting.TorchG, lighting.TorchB) : 0.0f;
        renderMap3D!.SetColorLight(Math.Min(1.0f, lighting.AmbientR + torchAmount), Math.Min(1.0f, lighting.AmbientG + torchAmount * 0.8f),
            Math.Min(1.0f, lighting.AmbientB + torchAmount * 0.6f));
    }

    void UpdateRealisticLighting2D(Map map, bool blind, uint spellLevel, LightSourceInfo? torch, float torchVisibility)
    {
        var lighting = realisticLighting2D;
        double time = realisticLightingTime;
        float ambientLuminance = Luminance(realisticAmbientR, realisticAmbientG, realisticAmbientB);
        int count = 0;

        // Darkness and its color
        if (blind)
        {
            lighting.Darkness = 1.0f;
            lighting.TintR = lighting.TintG = lighting.TintB = 0.0f;
        }
        else
        {
            bool inside = map.Flags.HasFlag(MapFlags.Indoor);
            float maxDarkness = map.Flags.HasFlag(MapFlags.Dungeon) ? 1.0f : 0.88f;
            lighting.Darkness = Math.Clamp((1.0f - ambientLuminance) * (inside ? 0.7f : 1.1f), 0.0f, maxDarkness);
            // The darkness has the color of the ambient light (e.g. dark blue at night, orange at sunset)
            float maxComponent = Math.Max(0.001f, Math.Max(realisticAmbientR, Math.Max(realisticAmbientG, realisticAmbientB)));
            float tint = 0.15f * (1.0f - ambientLuminance);
            lighting.TintR = realisticAmbientR / maxComponent * tint;
            lighting.TintG = realisticAmbientG / maxComponent * tint;
            lighting.TintB = realisticAmbientB / maxComponent * tint;
        }

        void AddLight(float x, float y, float radius, float strength, float glowR, float glowG, float glowB)
        {
            if (count >= RealisticLighting2D.MaxLights)
                return;

            lighting.Lights[count++] = new RealisticPointLight
            {
                X = x,
                Y = y,
                Z = strength,
                Radius = radius,
                R = glowR,
                G = glowG,
                B = glowB
            };
        }

        if (!blind)
        {
            var center = player2D!.DisplayArea.Center;

            // The light of the party
            if (torch != null)
            {
                var info = torch.Value;
                float flicker = Flicker(time, 0, info.Flicker);
                // The original uses a hard circle with a radius of 16, 32 or 48 pixels.
                // The light has a soft edge, so it is a bit larger.
                float radius = (12.0f + spellLevel * 28.0f) * (0.94f + 0.06f * flicker);
                float glow = 0.06f * info.Intensity * flicker * torchVisibility;
                AddLight(center.X, center.Y, radius, 1.0f, info.R * glow, info.G * glow, info.B * glow);
            }
            else if (!map.Flags.HasFlag(MapFlags.Dungeon))
            {
                // Some sight at night (moon and stars)
                AddLight(center.X, center.Y, 28.0f, 0.6f, 0.0f, 0.0f, 0.0f);
            }

            // Light sources
            renderMap2D!.CollectLightSources(Realistic2DTileMargin, realisticLightSources2D);

            if (realisticLightSources2D.Count > RealisticLighting2D.MaxLights - count)
            {
                realisticLightSources2D.Sort((a, b) =>
                {
                    int da = Math.Abs(a.X - center.X) + Math.Abs(a.Y - center.Y);
                    int db = Math.Abs(b.X - center.X) + Math.Abs(b.Y - center.Y);
                    return da.CompareTo(db);
                });
            }

            foreach (var lightSource in realisticLightSources2D)
            {
                var info = lightSource.Info;
                int seed = (int)(lightSource.MapX * 7919 + lightSource.MapY * 104729);
                float flicker = Flicker(time, seed, info.Flicker);
                float radius = (8.0f + info.Radius * 16.0f) * (0.95f + 0.05f * flicker);
                float glow = 0.16f * info.Intensity * flicker;
                AddLight(lightSource.X, lightSource.Y, radius, Math.Min(1.0f, info.Intensity), info.R * glow, info.G * glow, info.B * glow);
            }
        }

        lighting.LightCount = count;

        renderView.SetRealisticLighting2D(lighting);
        renderView.SetRealisticLighting3D(null);

        // The overlay is needed for darkness or glowing lights
        fow2D!.Visible = lighting.Darkness > 0.001f || count != 0;
    }
}
