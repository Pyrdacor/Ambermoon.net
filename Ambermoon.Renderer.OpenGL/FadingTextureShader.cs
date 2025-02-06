/*
 * FadingTextureShader.cs - Shader for textured objects with palette fade support
 *
 * Copyright (C) 2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

internal class FadingTextureShader : TextureShader
{
    internal static readonly string DefaultPaletteFadingSourceName = "paletteFadingSrc";
    internal static readonly string DefaultPaletteFadingDestinationName = "paletteFadingDst";
    internal static readonly string DefaultPaletteFadingSourceFactorName = "paletteFadingSrcFactor";

    protected static string[] FadingTextureFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform float {DefaultUsePaletteName};",
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform sampler2D {DefaultPaletteName};",
        $"uniform float {DefaultColorKeyName};",
        $"uniform float {DefaultPaletteCountName};",
        $"uniform float {DefaultPaletteFadingSourceName};",
        $"uniform float {DefaultPaletteFadingDestinationName};",
        $"uniform float {DefaultPaletteFadingSourceFactorName};",
        $"in vec2 varTexCoord;",
        $"flat in float palIndex;",
        $"flat in float maskColIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    vec4 pixelColor = vec4(0);",
        $"    if ({DefaultUsePaletteName} > 0.5f)",
        $"    {{",
        $"        float colorIndex = textureLod({DefaultSamplerName}, varTexCoord, 0.0f).r * 255.0f;",
        $"        ",
        $"        if (colorIndex < 0.5f)",
        $"            discard;",
        $"        else",
        $"        {{",
        $"            if (colorIndex >= 31.5f)",
        $"                colorIndex = 0.0f;",
        $"            if (colorIndex < 15.5f && {DefaultPaletteFadingSourceName} > 0.5f)",
        $"            {{",
        $"                vec4 srcPixelColor = textureLod({DefaultPaletteName}, vec2((colorIndex + 16.5f) / 32.0f, ({DefaultPaletteFadingSourceName} - 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"                vec4 dstPixelColor = textureLod({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, ({DefaultPaletteFadingDestinationName} - 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"                pixelColor = mix(dstPixelColor, srcPixelColor, {DefaultPaletteFadingSourceFactorName});",
        $"            }}",
        $"            else",
        $"            {{",
        $"                pixelColor = textureLod({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"            }}",
        $"        }}",
        $"    }}",
        $"    else",
        $"    {{",
        $"        pixelColor = textureLod({DefaultSamplerName}, varTexCoord, 0.0f);",
        $"        if (pixelColor.a < 0.5f)",
        $"            discard;",
        $"    }}",
        $"    ",
        $"    if (maskColIndex < 0.5f)",
        $"        {DefaultFragmentOutColorName} = pixelColor;",
        $"    else",
        $"        {DefaultFragmentOutColorName} = textureLod({DefaultPaletteName}, vec2((maskColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"}}"
    ];

    public void SetPaletteFading(PaletteFading paletteFading)
    {
        if (paletteFading == null)
        {
            shaderProgram.SetInput(DefaultPaletteFadingSourceName, 0.0f);
            shaderProgram.SetInput(DefaultPaletteFadingDestinationName, 0.0f);
            shaderProgram.SetInput(DefaultPaletteFadingSourceFactorName, 0.0f);
        }
        else
        {
            shaderProgram.SetInput(DefaultPaletteFadingSourceName, paletteFading.SourcePalette + 1.0f);
            shaderProgram.SetInput(DefaultPaletteFadingDestinationName, paletteFading.DestinationPalette + 1.0f);
            shaderProgram.SetInput(DefaultPaletteFadingSourceFactorName, paletteFading.SourceFactor);
        }
    }

    FadingTextureShader(State state)
        : base(state, FadingTextureFragmentShader(state), TextureVertexShader(state))
    {

    }

    public new static FadingTextureShader Create(State state) => new FadingTextureShader(state);
}
