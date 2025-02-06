/*
 * TextureShader.cs - Shader for textured objects
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

namespace Ambermoon.Renderer.OpenGL;

internal class TextureShader : ColorShader
{
    internal static readonly string DefaultUsePaletteName = "usePalette";
    internal static readonly string DefaultTexCoordName = "texCoord";
    internal static readonly string DefaultSamplerName = "sampler";
    internal static readonly string DefaultAtlasSizeName = "atlasSize";
    internal static readonly string DefaultPaletteName = "palette";
    internal static readonly string DefaultPaletteIndexName = "paletteIndex";
    internal static readonly string DefaultColorKeyName = "colorKeyIndex";
    internal static readonly string DefaultMaskColorIndexName = "maskColorIndex";
    internal static readonly string DefaultPaletteCountName = "palCount";

    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    protected static string[] TextureFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform float {DefaultUsePaletteName};",
        $"uniform float {DefaultPaletteCountName};",
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform sampler2D {DefaultPaletteName};",
        $"uniform float {DefaultColorKeyName};",
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
        $"            pixelColor = textureLod({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
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

    protected static string[] TextureVertexShader(State state) =>
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
        $"flat out float maskColIndex;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    vec2 pos = vec2({DefaultPositionName}.x + 0.49f, {DefaultPositionName}.y + 0.49f);",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    maskColIndex = float({DefaultMaskColorIndexName});",
        $"    float z = 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f;",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, z, 1.0f);",
        $"}}"
    ];

    TextureShader(State state)
        : this(state, TextureFragmentShader(state), TextureVertexShader(state))
    {

    }

    protected TextureShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        : base(state, fragmentShaderLines, vertexShaderLines)
    {

    }

    public void UsePalette(bool use)
    {
        shaderProgram.SetInput(DefaultUsePaletteName, use ? 1.0f : 0.0f);
    }

    public void SetSampler(int textureUnit = 0)
    {
        shaderProgram.SetInput(DefaultSamplerName, textureUnit);
    }

    public void SetPalette(int textureUnit = 1)
    {
        shaderProgram.SetInput(DefaultPaletteName, textureUnit);
    }

    public void SetAtlasSize(uint width, uint height)
    {
        shaderProgram.SetInputVector2(DefaultAtlasSizeName, width, height);
    }

    public void SetColorKey(byte colorIndex)
    {
        if (colorIndex > 31)
            throw new AmbermoonException(ExceptionScope.Render, "Color index must be in the range 0 to 31.");

        shaderProgram.SetInput(DefaultColorKeyName, (float)colorIndex);
    }

    public void SetPaletteCount(int count)
    {
        shaderProgram.SetInput(DefaultPaletteCountName, (float)count);
    }

    public new static TextureShader Create(State state) => new TextureShader(state);
}
