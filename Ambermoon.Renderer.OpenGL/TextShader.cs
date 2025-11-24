/*
 * TextShader.cs - Shader for text rendering
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

internal class TextShader : TextureShader
{
    internal static readonly string DefaultTextColorIndexName = "textColorIndex";

    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    static string[] TextFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform float {DefaultUsePaletteName};",
        $"uniform float {DefaultPaletteCountName};",
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform sampler2D {DefaultPaletteName};",
        $"in vec2 varTexCoord;",
        $"flat in float palIndex;",
        $"flat in float textColIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    if ({DefaultUsePaletteName} > 0.5f)",
        $"    {{",
        $"        float alpha = textureLod({DefaultSamplerName}, varTexCoord, 0.0f).r * 255.0f;",
        $"        if (alpha < 0.5f)",
        $"            discard;",
        $"        else",
        $"            {DefaultFragmentOutColorName} = textureLod({DefaultPaletteName}, vec2((textColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"    }}",
        $"    else",
        $"    {{",
        $"        vec4 pixelColor = textureLod({DefaultSamplerName}, varTexCoord, 0.0f);",
        $"        if (pixelColor.a < 0.5f)",
        $"            discard;",
        $"        else",
        $"        {{",
        $"            vec4 textColor = textureLod({DefaultPaletteName}, vec2((textColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"            {DefaultFragmentOutColorName} = pixelColor * vec4(textColor.rgb, 1.0f);",
        $"        }}",
        $"    }}",
        $"}}"
    ];

    static string[] TextVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"in ivec2 {DefaultTexCoordName};",
        $"in uint {DefaultLayerName};",
        $"in uint {DefaultPaletteIndexName};",
        $"in uint {DefaultTextColorIndexName};",
        $"uniform uvec2 {DefaultAtlasSizeName};",
        $"uniform float {DefaultZName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 varTexCoord;",
        $"flat out float palIndex;",
        $"flat out float textColIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    vec2 pos = vec2({DefaultPositionName}.x + 0.49f, {DefaultPositionName}.y + 0.49f);",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    textColIndex = float({DefaultTextColorIndexName});",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
        $"}}"
    ];

    TextShader(State state)
        : this(state, TextFragmentShader(state), TextVertexShader(state))
    {

    }

    protected TextShader(State state, string[] vertexShaderLines)
        : base(state, TextFragmentShader(state), vertexShaderLines)
    {

    }

    protected TextShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        : base(state, fragmentShaderLines, vertexShaderLines)
    {

    }

    public new static TextShader Create(State state) => new(state);
}
