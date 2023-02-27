/*
 * AlphaTextureShader.cs - Shader for textured objects with alpha channel
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

namespace Ambermoon.Renderer
{
    internal class AlphaTextureShader : TextureShader
    {
        internal static readonly string DefaultAlphaName = "alpha";

        protected static string[] AlphaTextureFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform float {DefaultUsePaletteName};",
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultColorKeyName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in float maskColIndex;",
            $"flat in float a;",
            $"",
            $"    vec4 pixelColor;",
            $"    if ({DefaultUsePaletteName} > 0.5f)",
            $"    {{",
            $"        float colorIndex = texture({DefaultSamplerName}, varTexCoord).r * 255.0f;",
            $"        ",
            $"        if (colorIndex < 0.5f)",
            $"            discard;",
            $"        else",
            $"        {{",
            $"            if (colorIndex >= 31.5f)",
            $"                colorIndex = 0.0f;",
            $"            pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"        }}",
            $"    }}",
            $"    else",
            $"    {{",
            $"        pixelColor = texture({DefaultSamplerName}, varTexCoord);",
            $"        if (pixelColor.a < 0.5f)",
            $"            discard;",
            $"    }}",
            $"    ",
            $"    if (maskColIndex > 0.5f)",
            $"        pixelColor = texture({DefaultPaletteName}, vec2((maskColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"    {DefaultFragmentOutColorName} = vec4(pixelColor.rgb, pixelColor.a * a);",
        };

        protected static string[] AlphaTextureVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"in ivec2 {DefaultTexCoordName};",
            $"in uint {DefaultLayerName};",
            $"in uint {DefaultPaletteIndexName};",
            $"in uint {DefaultMaskColorIndexName};",
            $"in uint {DefaultAlphaName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"flat out float palIndex;",
            $"flat out float maskColIndex;",
            $"flat out float a;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    a = float({DefaultAlphaName}) / 255.0f;",
            $"    maskColIndex = float({DefaultMaskColorIndexName});",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        AlphaTextureShader(State state)
            : base(state, AlphaTextureFragmentShader(state), AlphaTextureVertexShader(state))
        {

        }

        public new static AlphaTextureShader Create(State state) => new AlphaTextureShader(state);
    }
}
