/*
 * TextureShader.cs - Shader for textured objects
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

namespace Ambermoon.Renderer.OpenGL;

using Data;

// NOTE: This won't support non-palette graphics as it uses the color indices
// for color replacements.
internal class SkyShader : TextureShader
{
    internal static readonly string DefaultLightName = "light";
    internal static readonly string DefaultColorReplaceName = "palReplace";
    internal static readonly string DefaultUseColorReplaceName = "useReplace";

    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    protected static string[] SkyFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform sampler2D {DefaultPaletteName};",
        $"uniform float {DefaultColorKeyName};",
        $"uniform float {DefaultLightName};",
        $"uniform float {DefaultPaletteCountName};",
        $"uniform vec4 {DefaultColorReplaceName}[16];",
        $"uniform float {DefaultUseColorReplaceName};",
        $"in vec2 varTexCoord;",
        $"flat in float palIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    float colorIndex = textureLod({DefaultSamplerName}, varTexCoord, 0.0f).r * 255.0f;",
        $"    ",
        $"    if (colorIndex < 0.5f || {DefaultLightName} < 0.01f)",
        $"        discard;",
        $"    else",
        $"    {{",
        $"        vec4 pixelColor = {DefaultUseColorReplaceName} > 0.5f && colorIndex < 15.5f ? {DefaultColorReplaceName}[int(colorIndex + 0.5f)]",
        $"            : textureLod({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"        {DefaultFragmentOutColorName} = vec4(max(vec3(0), pixelColor.rgb + vec3({DefaultLightName}) - 1.0f), pixelColor.a);",
        $"    }}",
        $"}}"
    ];

    protected static string[] SkyVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"in ivec2 {DefaultTexCoordName};",
        $"in uint {DefaultLayerName};",
        $"in uint {DefaultPaletteIndexName};",
        $"in uint {DefaultMaskColorIndexName};",
        $"uniform uvec2 {DefaultAtlasSizeName};",
        $"uniform float {DefaultZName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 varTexCoord;",
        $"flat out float palIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    int index = int(mod(float(gl_VertexID), 4.0f));",
        $"    float dx = index == 0 || index == 3 ? -0.49f : 0.49f;",
        $"    float dy = index < 2 ? -0.49f : 0.49f;",
        $"    vec2 pos = vec2({DefaultPositionName}.x + dx, {DefaultPositionName}.y + dy);",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
        $"}}"
    ];

    SkyShader(State state)
        : base(state, SkyFragmentShader(state), SkyVertexShader(state))
    {

    }

    public void SetLight(float light)
    {
        shaderProgram.SetInput(DefaultLightName, light);
    }

    public void SetPaletteReplacement(PaletteReplacement paletteReplacement)
    {
        if (paletteReplacement == null)
        {
            shaderProgram.SetInput(DefaultUseColorReplaceName, 0.0f);
        }
        else
        {
            shaderProgram.SetInputColorArray(DefaultColorReplaceName, paletteReplacement.ColorData);
            shaderProgram.SetInput(DefaultUseColorReplaceName, 1.0f);
        }
    }

    public new static SkyShader Create(State state) => new SkyShader(state);
}
