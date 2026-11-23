/*
 * LightSources.cs - Light sources for the realistic lighting
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

using System.Collections.Generic;

namespace Ambermoon.Render;

/// <summary>
/// A light source for the realistic lighting.
/// </summary>
/// <param name="R">Red component of the light color (0 to 1).</param>
/// <param name="G">Green component of the light color (0 to 1).</param>
/// <param name="B">Blue component of the light color (0 to 1).</param>
/// <param name="Intensity">Brightness of the light (1 is a normal torch).</param>
/// <param name="Radius">Range of the light in tiles (map blocks).</param>
/// <param name="Flicker">Amount of flickering (0 = steady, 1 = like a torch).</param>
/// <param name="U">Horizontal position of the light inside the wall graphic or overlay (0 = left, 1 = right).</param>
/// <param name="V">Vertical position of the light inside the wall graphic or overlay (0 = top, 1 = bottom).</param>
public readonly record struct LightSourceInfo(float R, float G, float B, float Intensity, float Radius, float Flicker = 1.0f,
    float U = 0.5f, float V = 0.5f)
{
    public LightSourceInfo WithIntensity(float intensity) => this with { Intensity = intensity };
    public LightSourceInfo WithRadius(float radius) => this with { Radius = radius };
    public LightSourceInfo At(float u, float v) => this with { U = u, V = v };
}

/// <summary>
/// List of all graphics which emit light when the realistic lighting is active.
///
/// The indices are the ones from the original game data (they were identified
/// by looking at the graphics). Add new entries here to make more things glow.
/// </summary>
public static class LightSources
{
    // Some presets
    public static readonly LightSourceInfo Torch = new(1.0f, 0.66f, 0.34f, 1.2f, 3.5f, 1.0f);
    public static readonly LightSourceInfo Fire = new(1.0f, 0.60f, 0.28f, 1.4f, 4.0f, 1.0f);
    public static readonly LightSourceInfo Campfire = new(1.0f, 0.58f, 0.26f, 1.3f, 4.0f, 1.0f);
    public static readonly LightSourceInfo Fireplace = new(1.0f, 0.60f, 0.30f, 1.3f, 4.0f, 0.9f);
    public static readonly LightSourceInfo Embers = new(1.0f, 0.45f, 0.18f, 1.0f, 2.5f, 0.6f);
    public static readonly LightSourceInfo Candle = new(1.0f, 0.80f, 0.55f, 0.9f, 2.5f, 0.7f);
    public static readonly LightSourceInfo Lantern = new(1.0f, 0.82f, 0.55f, 1.0f, 3.0f, 0.35f);
    public static readonly LightSourceInfo Lava = new(1.0f, 0.40f, 0.14f, 0.9f, 2.5f, 0.25f);
    public static readonly LightSourceInfo MagicOrange = new(1.0f, 0.68f, 0.40f, 0.9f, 2.5f, 0.15f);
    public static readonly LightSourceInfo Lightning = new(0.85f, 0.80f, 1.0f, 1.1f, 3.0f, 1.0f);
    // Faint shimmer of glowing plants
    public static readonly LightSourceInfo GlowPlantOrange = new(1.0f, 0.65f, 0.35f, 0.35f, 1.6f, 0.15f);
    public static readonly LightSourceInfo GlowPlantWhite = new(0.80f, 0.90f, 1.0f, 0.35f, 1.6f, 0.15f);
    public static readonly LightSourceInfo GlowPlantGreen = new(0.55f, 1.0f, 0.55f, 0.3f, 1.8f, 0.15f);

    /// <summary>
    /// 3D object textures (key is the object texture index, see <see cref="Data.Labdata.ObjectInfo.TextureIndex"/>).
    /// </summary>
    public static readonly Dictionary<uint, LightSourceInfo> Object3DTextures = new()
    {
        { 16, Fire }, // big animated flame
        { 61, Embers.WithIntensity(0.8f) }, // cauldron above fire
        { 89, Lava }, // lava pools
        { 90, Lava },
        { 91, Lava.WithRadius(2.0f) },
        { 121, Embers }, // brazier with glowing coals
        { 141, Campfire }, // campfire with stones
        { 142, Candle }, // candle on a candlestick
        { 144, Fire }, // big animated flame
        { 149, Candle }, // small candle
        { 151, Candle.WithIntensity(0.8f) }, // small flames on the floor
        // Glowing plants (mostly on the forest moon)
        { 186, GlowPlantOrange.WithRadius(1.2f) }, // plant with orange bulbs
        { 187, GlowPlantOrange.WithRadius(1.2f) }, // orange flower
        { 204, GlowPlantOrange }, // flowers with orange heads
        { 205, GlowPlantWhite }, // flowers with white heads
        { 232, GlowPlantOrange.WithRadius(1.2f) }, // bush with orange berries
        { 233, GlowPlantOrange }, // big bush with orange berries
        { 235, GlowPlantOrange }, // plant with orange blossoms
        { 237, GlowPlantOrange.WithIntensity(0.3f) }, // big orange blossom
        { 242, GlowPlantOrange.WithIntensity(0.25f).WithRadius(1.0f) }, // small orange flower
        { 244, GlowPlantOrange.WithIntensity(0.25f).WithRadius(1.0f) }, // small orange flower
        { 248, GlowPlantOrange.WithIntensity(0.25f).WithRadius(1.0f) }, // orange mushroom
        { 257, GlowPlantGreen }, // glowing swamp plant
        { 258, GlowPlantGreen }, // bubbling green plant
        { 202, MagicOrange.WithIntensity(0.9f) with { Flicker = 0.8f } }, // fire orb
        { 203, Fire }, // flame column
        { 262, MagicOrange }, // glowing flask
        { 276, Fire.WithRadius(3.0f) }, // fire pit in a stone ring
        { 314, MagicOrange.WithIntensity(0.5f) }, // glowing tube
        { 356, Lightning }, // lightning
        { 358, MagicOrange.WithIntensity(0.5f) }, // machines with glowing tubes
        { 359, MagicOrange.WithIntensity(0.5f) },
        { 360, MagicOrange.WithIntensity(0.4f) },
        { 361, MagicOrange }, // orb with a glowing core
        { 364, MagicOrange.WithIntensity(0.5f) with { Flicker = 0.5f } }, // pulsing lights
        { 365, MagicOrange.WithIntensity(0.5f) with { Flicker = 0.5f } },
    };

    /// <summary>
    /// 3D wall textures (key is the wall texture index, see <see cref="Data.Labdata.WallData.TextureIndex"/>).
    /// </summary>
    public static readonly Dictionary<uint, LightSourceInfo> Wall3DTextures = new()
    {
    };

    /// <summary>
    /// 3D wall overlay textures (key is the overlay texture index, see <see cref="Data.Labdata.OverlayData.TextureIndex"/>).
    /// The light is placed in front of the wall at the position given by U and V inside the overlay.
    /// </summary>
    public static readonly Dictionary<uint, LightSourceInfo> Wall3DOverlayTextures = new()
    {
        { 15, Torch.At(0.5f, 0.25f) }, // pillar with a fire bowl at the top (left)
        { 16, Torch.At(0.5f, 0.25f) }, // pillar with a fire bowl at the top (right)
        { 56, Lantern.WithIntensity(0.8f) }, // small lamp next to a door (left)
        { 57, Lantern.WithIntensity(0.8f) }, // small lamp next to a door (right)
    };

    /// <summary>
    /// 2D tile graphics (key is the tileset index and the graphic index, see <see cref="Data.Tileset.Tile.GraphicIndex"/>).
    /// </summary>
    public static readonly Dictionary<(uint Tileset, uint GraphicIndex), LightSourceInfo> Tileset2DGraphics = CreateTileset2DGraphics();

    static Dictionary<(uint, uint), LightSourceInfo> CreateTileset2DGraphics()
    {
        var lights = new Dictionary<(uint, uint), LightSourceInfo>();

        void Add(uint tileset, LightSourceInfo info, params uint[] graphicIndices)
        {
            foreach (var graphicIndex in graphicIndices)
                lights[(tileset, graphicIndex)] = info;
        }

        // Tileset 1
        Add(1, Lava, 291, 297, 303, 309); // lava
        Add(1, Campfire, 1136); // campfire
        // Tileset 4
        Add(4, Torch, 322); // torch
        Add(4, Fireplace, 477); // fireplace
        Add(4, Embers, 586, 589); // cauldron above fire, brazier
        Add(4, Torch, 648, 660); // walls with torches
        Add(4, Fireplace.WithIntensity(0.6f), 640, 652); // walls with fire at the bottom
        // Tileset 5
        Add(5, Fireplace, 279); // fireplace
        Add(5, Fireplace.WithIntensity(0.7f), 283); // side of the fireplace
        Add(5, Torch, 287); // fire bowl
        Add(5, Embers, 455, 458); // cauldron above fire, brazier
        // Tileset 6
        Add(6, Campfire, 212); // campfire
        Add(6, Embers, 216, 361, 364); // braziers and cauldron
        // Tileset 7
        for (uint graphicIndex = 70; graphicIndex <= 105; graphicIndex += 5)
            Add(7, Lava, graphicIndex); // lava
        // Tileset 8
        Add(8, Fire, 210, 214, 218, 222, 268); // walls of fire, fire bowl
        Add(8, Fire, 332, 336, 340, 344, 348, 352); // burning logs, fire bowls
        Add(8, Fire.WithIntensity(0.8f), 406, 410, 414, 418, 422, 426); // walls with fire

        return lights;
    }

    public static bool TryGetObject3DLight(uint textureIndex, out LightSourceInfo info) => Object3DTextures.TryGetValue(textureIndex, out info);
    public static bool TryGetWall3DLight(uint textureIndex, out LightSourceInfo info) => Wall3DTextures.TryGetValue(textureIndex, out info);
    public static bool TryGetWallOverlay3DLight(uint textureIndex, out LightSourceInfo info) => Wall3DOverlayTextures.TryGetValue(textureIndex, out info);
    public static bool TryGetTile2DLight(uint tilesetIndex, uint graphicIndex, out LightSourceInfo info) => Tileset2DGraphics.TryGetValue((tilesetIndex, graphicIndex), out info);
}
