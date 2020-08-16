/*
 * TextShader.cs - Shader for text rendering
 *
 * Copyright (C) 2020  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
    internal class TextShader : TextureShader
    {
        internal static readonly string DefaultTextColorIndexName = "textColorIndex";

        readonly string textColorIndexName;

        // The palette has a size of 32x51 pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        static string[] TextFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in float textColIndex;",
            $"",
            $"void main()",
            $"{{",
            $"    float colorIndex = texture({DefaultSamplerName}, varTexCoord).r * 255.0f * textColIndex;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / 51.0f));",
            $"    ",
            $"    if (colorIndex < 0.5f || pixelColor.a < 0.5f)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = pixelColor;",
            $"}}"
        };

        static string[] TextVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
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
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    textColIndex = float({DefaultTextColorIndexName});",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        TextShader(State state)
            : this(state, DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultAtlasSizeName, DefaultLayerName, DefaultPaletteName,
                  DefaultPaletteIndexName, DefaultTextColorIndexName, TextFragmentShader(state), TextVertexShader(state))
        {

        }

        protected TextShader(State state, string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string atlasSizeName, string layerName,
            string paletteName, string paletteIndexName, string textColorIndexName, string[] fragmentShaderLines,
            string[] vertexShaderLines)
            : base(state, modelViewMatrixName, projectionMatrixName, zName, positionName, texCoordName,
                  samplerName, atlasSizeName, layerName, paletteName, paletteIndexName, fragmentShaderLines, vertexShaderLines)
        {
            this.textColorIndexName = textColorIndexName;
        }

        public new static TextShader Create(State state) => new TextShader(state);
    }
}
