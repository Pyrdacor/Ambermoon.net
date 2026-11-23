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
using System.Linq;

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

    static string DescribeLight(LightSourceInfo? info)
    {
        if (info is not { } l)
            return "no light";

        var text = FormattableString.Invariant($"light: color {l.R:0.00}/{l.G:0.00}/{l.B:0.00}, intensity {l.Intensity:0.00}, radius {l.Radius:0.0}, flicker {l.Flicker:0.00}");

        if (l.Pulse > 0.0f)
            text += FormattableString.Invariant($", pulse {l.Pulse:0.0}/s");

        return text;
    }

    /// <summary>
    /// Describes the graphics on the player's position and up to 3 tiles in front of the player
    /// with their indices and light sources. Used by the cheat "lightinfo" to find graphics
    /// which should emit light (see <see cref="LightSources"/>).
    /// </summary>
    public List<string> GetLightSourceReport()
    {
        var lines = new List<string>();
        var map = Map;

        if (!Ingame || map == null || (Is3D ? renderMap3D == null || player3D == null : renderMap2D == null || player2D == null))
        {
            lines.Add("Only available on a map.");
            return lines;
        }

        string On(bool active) => active ? "on" : "off";
        lines.Add($"Realistic lighting is {On(RealisticLightingEnabled)}.");

        if (Is3D)
        {
            var labdata = MapManager.GetLabdataForMap(map);
            var camera = player3D!.Camera;
            camera.GetForwardPosition(1.0f, out float forwardX, out float forwardZ, false, false);
            forwardX = camera.X - forwardX;
            forwardZ = camera.Z - forwardZ;
            float worldX = -camera.X;
            float worldZ = -camera.Z;
            var blocks = new List<(int X, int Y)>();

            // Walk along the view direction in small steps and collect the blocks.
            for (float distance = 0.0f; distance <= 3.6f * Global.DistancePerBlock; distance += 0.1f * Global.DistancePerBlock)
            {
                int x = (int)Math.Floor((worldX + forwardX * distance) / Global.DistancePerBlock);
                int y = (int)Math.Floor((worldZ + forwardZ * distance) / Global.DistancePerBlock + map.Height);

                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                    break;

                if (!blocks.Contains((x, y)))
                    blocks.Add((x, y));

                var block = map.Blocks[x, y];

                // Stop behind the first wall which blocks the view
                if (block.WallIndex != 0 && blocks.Count > 1)
                {
                    var wall = labdata.Walls[((int)block.WallIndex - 1) % labdata.Walls.Count];

                    if (!wall.Flags.HasFlag(Tileset.TileFlags.Transparency))
                        break;
                }
            }

            lines.Add($"Map {map.Index} (3D, labdata {map.TilesetOrLabdataIndex}), position {blocks[0].X + 1},{blocks[0].Y + 1}, {blocks.Count - 1} block(s) in front");

            foreach (var (x, y) in blocks)
            {
                var block = map.Blocks[x, y];
                string where = x == blocks[0].X && y == blocks[0].Y ? "here" : "ahead";

                if (block.WallIndex != 0)
                {
                    var wall = labdata.Walls[((int)block.WallIndex - 1) % labdata.Walls.Count];
                    lines.Add($"  Block {x + 1},{y + 1} ({where}): wall {block.WallIndex}, wall texture {wall.TextureIndex} ({DescribeLight(LightSources.TryGetWall3DLight(wall.TextureIndex, out var wallLight) ? wallLight : null)})");

                    foreach (var overlay in wall.Overlays ?? [])
                        lines.Add($"    overlay {overlay.TextureIndex} ({DescribeLight(LightSources.TryGetWallOverlay3DLight(overlay.TextureIndex, out var overlayLight) ? overlayLight : null)})");
                }
                else if (block.ObjectIndex != 0)
                {
                    var obj = labdata.Objects[((int)block.ObjectIndex - 1) % labdata.Objects.Count];
                    lines.Add($"  Block {x + 1},{y + 1} ({where}): object {block.ObjectIndex}");

                    foreach (var subObject in obj.SubObjects)
                    {
                        uint texture = subObject.Object.TextureIndex;
                        lines.Add($"    object texture {texture} ({DescribeLight(LightSources.TryGetObject3DLight(texture, out var objectLight) ? objectLight : null)})");
                    }
                }
                else
                {
                    lines.Add($"  Block {x + 1},{y + 1} ({where}): empty");
                }

                foreach (var character in renderMap3D!.CharacterTextureInfos)
                {
                    if (character.Position.X != x || character.Position.Y != y)
                        continue;

                    lines.Add($"    map character {character.Index + 1}{(character.Visible ? "" : " (hidden)")}");

                    foreach (var texture in character.Textures.Distinct())
                        lines.Add($"      object texture {texture} ({DescribeLight(LightSources.TryGetObject3DLight(texture, out var characterLight) ? characterLight : null)})");
                }
            }
        }
        else
        {
            var tileset = renderMap2D!.Tileset;
            var position = player2D!.Position;
            var direction = player2D.Direction;
            int dx = direction == CharacterDirection.Left ? -1 : direction == CharacterDirection.Right ? 1 : 0;
            int dy = direction == CharacterDirection.Up ? -1 : direction == CharacterDirection.Down ? 1 : 0;
            int maxX = map.IsWorldMap ? map.Width * 2 : map.Width;
            int maxY = map.IsWorldMap ? map.Height * 2 : map.Height;

            lines.Add($"Map {map.Index} (2D, tileset {tileset?.Index}), position {position.X + 1},{position.Y + 1}, looking {direction}");

            if (tileset == null)
                return lines;

            string Tile(uint tileIndex)
            {
                if (tileIndex == 0 || tileIndex > tileset.Tiles.Length)
                    return "none";

                uint graphicIndex = tileset.Tiles[tileIndex - 1].GraphicIndex;
                return $"tile {tileIndex}, graphic {graphicIndex} ({DescribeLight(LightSources.TryGetTile2DLight(tileset.Index, graphicIndex, out var tileLight) ? tileLight : null)})";
            }

            for (int i = 0; i <= 3; ++i)
            {
                int x = position.X + dx * i;
                int y = position.Y + dy * i;

                if (x < 0 || y < 0 || x >= maxX || y >= maxY)
                    break;

                var tile = renderMap2D[(uint)x, (uint)y];
                lines.Add($"  Tile {x + 1},{y + 1} ({(i == 0 ? "here" : i + " ahead")}):");
                lines.Add($"    background {Tile(tile.BackTileIndex)}");
                lines.Add($"    foreground {Tile(tile.FrontTileIndex)}");

                foreach (var character in renderMap2D.MapCharacters)
                {
                    if (character.Position.X != x || character.Position.Y != y || character.TileGraphicIndex is not uint graphicIndex)
                        continue;

                    lines.Add($"    map character with graphic {graphicIndex}{(character.Active && character.Visible ? "" : " (hidden)")} ({DescribeLight(LightSources.TryGetTile2DLight(tileset.Index, graphicIndex, out var characterLight) ? characterLight : null)})");
                }
            }
        }

        return lines;
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

    /// <summary>
    /// Brightness factor of a light source: flickering and an optional regular pulsing.
    /// </summary>
    static float LightFactor(double time, int seed, LightSourceInfo info)
    {
        float factor = Flicker(time, seed, info.Flicker);

        if (info.Pulse > 0.0f)
            factor *= 0.7f + 0.3f * (float)Math.Sin(2.0 * Math.PI * info.Pulse * time + (seed % 628) * 0.01);

        return factor;
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
            // The camera stores the negated world position, so the direction is negated as well.
            camera.GetForwardPosition(1.0f, out float forwardX, out float forwardZ, false, false);
            forwardX = camera.X - forwardX;
            forwardZ = camera.Z - forwardZ;
            const float maxDistance = 12.0f * Global.DistancePerBlock;
            // Lights near the selection limit fade out over this score range.
            const float limitFadeRange = 2.0f * Global.DistancePerBlock;
            var candidates = realisticLightSources3D;
            candidates.Clear();

            foreach (var light in renderMap3D!.WorldLightSources)
            {
                float dx = light.X - cameraX;
                float dz = light.Z - cameraZ;
                float radius = light.Info.Radius * Global.DistancePerBlock;

                // Everything visible is in front of the camera. A light which is further behind
                // the camera than its radius can't light anything visible.
                if (dx * forwardX + dz * forwardZ < -radius)
                    continue;

                float distance = (float)Math.Sqrt(dx * dx + dz * dz);

                if (distance - radius > maxDistance)
                    continue;

                candidates.Add((distance - radius, light));
            }

            int count = Math.Min(RealisticLighting3D.MaxLights, candidates.Count);
            float limitScore = float.MaxValue;

            if (candidates.Count > RealisticLighting3D.MaxLights)
            {
                candidates.Sort((a, b) => a.Score.CompareTo(b.Score));
                // Lights near the limit fade out smoothly. Otherwise lights would suddenly
                // switch on when they become one of the nearest lights.
                limitScore = candidates[RealisticLighting3D.MaxLights].Score;
            }

            for (int i = 0; i < count; ++i)
            {
                var light = candidates[i].Light;
                var info = light.Info;
                float flicker = LightFactor(time, light.Seed, info);
                float intensity = info.Intensity * flicker;

                // Lights which are far away or near the light limit fade out so that switching lights is not visible.
                float fade = Math.Clamp(1.0f - (candidates[i].Score - (maxDistance - 3.0f)) / 3.0f, 0.0f, 1.0f);
                fade = Math.Min(fade, Math.Clamp((limitScore - candidates[i].Score) / limitFadeRange, 0.0f, 1.0f));
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
                float flicker = LightFactor(time, seed, info);
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
