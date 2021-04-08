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

namespace Ambermoon.Renderer
{
    internal class TextureShader : ColorShader
    {
        internal static readonly string DefaultTexCoordName = "texCoord";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultAtlasSizeName = "atlasSize";
        internal static readonly string DefaultPaletteName = "palette";
        internal static readonly string DefaultPaletteIndexName = "paletteIndex";
        internal static readonly string DefaultColorKeyName = "colorKeyIndex";
        internal static readonly string DefaultMaskColorIndexName = "maskColorIndex";

        // The palette has a size of 32xNumPalettes pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        protected static string[] TextureFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultColorKeyName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in float maskColIndex;",
            $"",
            $"void main()",
            $"{{",
            $"    float colorIndex = texture({DefaultSamplerName}, varTexCoord).r * 255.0f;",
            $"    ",
            $"    if (colorIndex < 0.5f)",
            $"        discard;",
            $"    else",
            $"    {{",
            $"        if (colorIndex >= 31.5f)",
            $"            colorIndex = 0.0f;",
            $"        vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"        if (maskColIndex < 0.5f)",
            $"            {DefaultFragmentOutColorName} = pixelColor;",
            $"        else",
            $"            {DefaultFragmentOutColorName} = texture({DefaultPaletteName}, vec2((maskColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"    }}",
            $"}}"
        };

        protected static string[] TextureVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
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
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    maskColIndex = float({DefaultMaskColorIndexName});",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        TextureShader(State state)
            : this(state, TextureFragmentShader(state), TextureVertexShader(state))
        {

        }

        protected TextureShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, fragmentShaderLines, vertexShaderLines)
        {

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

        public new static TextureShader Create(State state) => new TextureShader(state);
    }
}
