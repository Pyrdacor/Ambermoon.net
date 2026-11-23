/*
 * RealisticLighting.cs - Parameters for the realistic lighting
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

namespace Ambermoon.Render;

/// <summary>
/// A point light which is passed to the renderer.
/// </summary>
public struct RealisticPointLight
{
    public float X;
    public float Y;
    public float Z;
    public float Radius;
    /// <summary>
    /// Color multiplied with the intensity (including flickering).
    /// </summary>
    public float R;
    public float G;
    public float B;
}

/// <summary>
/// Lighting of the 3D view. All colors are multiplied with the texture colors.
/// </summary>
public class RealisticLighting3D
{
    public const int MaxLights = 32;

    /// <summary>
    /// Ambient light (daylight, darkness of dungeons, etc).
    /// </summary>
    public float AmbientR = 1.0f;
    public float AmbientG = 1.0f;
    public float AmbientB = 1.0f;
    /// <summary>
    /// Light of the carried torch (light spell) which lights a cone in front of the party.
    /// The color is multiplied with the intensity (including flickering).
    /// </summary>
    public float TorchR = 0.0f;
    public float TorchG = 0.0f;
    public float TorchB = 0.0f;
    /// <summary>
    /// Range of the torch in world units (0 means no torch).
    /// </summary>
    public float TorchRange = 0.0f;
    /// <summary>
    /// Position of the torch relative to the eye (view space, -Z is forward).
    /// </summary>
    public float TorchX = 0.0f;
    public float TorchY = 0.0f;
    public float TorchZ = 0.0f;
    /// <summary>
    /// Light sources in world space.
    /// </summary>
    public readonly RealisticPointLight[] Lights = new RealisticPointLight[MaxLights];
    public int LightCount = 0;
}

/// <summary>
/// Lighting of the 2D view. It is drawn as an overlay above the map.
/// </summary>
public class RealisticLighting2D
{
    public const int MaxLights = 24;

    /// <summary>
    /// Color of the darkness (e.g. dark blue at night).
    /// </summary>
    public float TintR = 0.0f;
    public float TintG = 0.0f;
    public float TintB = 0.0f;
    /// <summary>
    /// Darkness where no light is (0 = no darkness, 1 = black).
    /// </summary>
    public float Darkness = 0.0f;
    /// <summary>
    /// Light sources in screen coordinates (X, Y and Radius in virtual screen pixels).
    /// The color is the glow color (multiplied with the glow strength), Z is the
    /// light strength which removes darkness (0 to 1).
    /// </summary>
    public readonly RealisticPointLight[] Lights = new RealisticPointLight[MaxLights];
    public int LightCount = 0;
}
