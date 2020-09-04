/*
 * TextureShader.cs - Shader for textured objects
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
    internal class TextureShader : ColorShader
    {
        internal static readonly string DefaultTexCoordName = "texCoord";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultAtlasSizeName = "atlasSize";
        internal static readonly string DefaultPaletteName = "palette";
        internal static readonly string DefaultPaletteIndexName = "paletteIndex";
        internal static readonly string DefaultColorKeyName = "colorKeyIndex";

        readonly string texCoordName;
        readonly string samplerName;
        readonly string atlasSizeName;
        readonly string paletteName;
        readonly string paletteIndexName;
        readonly string colorKeyName;

        // The palette has a size of 32x51 pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        static string[] TextureFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultColorKeyName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"",
            $"void main()",
            $"{{",
            $"    float colorIndex = texture({DefaultSamplerName}, varTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / 51.0f));",
            $"    ",
            $"    // Color key is only available for item/portrait palette 49.",
            $"    float colorKey = abs(palIndex - 49.0f) < 0.5f ? {DefaultColorKeyName} : 0.0f;",
            $"    ",
            $"    if (abs(colorIndex - colorKey) < 0.5f || pixelColor.a < 0.5f)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = pixelColor;",
            $"}}"
        };

        protected static string[] TextureVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"in ivec2 {DefaultTexCoordName};",
            $"in uint {DefaultLayerName};",
            $"in uint {DefaultPaletteIndexName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"flat out float palIndex;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        TextureShader(State state)
            : this(state, DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultAtlasSizeName, DefaultLayerName, DefaultPaletteName,
                  DefaultPaletteIndexName, DefaultColorKeyName, TextureFragmentShader(state), TextureVertexShader(state))
        {

        }

        protected TextureShader(State state, string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string atlasSizeName, string layerName,
            string paletteName, string paletteIndexName, string colorKeyName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, modelViewMatrixName, projectionMatrixName, DefaultColorName, zName, positionName, layerName,
                  fragmentShaderLines, vertexShaderLines)
        {
            this.texCoordName = texCoordName;
            this.samplerName = samplerName;
            this.atlasSizeName = atlasSizeName;
            this.paletteName = paletteName;
            this.paletteIndexName = paletteIndexName;
            this.colorKeyName = colorKeyName;
        }

        public void SetSampler(int textureUnit = 0)
        {
            shaderProgram.SetInput(samplerName, textureUnit);
        }

        public void SetPalette(int textureUnit = 1)
        {
            shaderProgram.SetInput(paletteName, textureUnit);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            shaderProgram.SetInputVector2(atlasSizeName, width, height);
        }

        public void SetColorKey(byte colorIndex)
        {
            if (colorIndex > 31)
                throw new AmbermoonException(ExceptionScope.Render, "Color index must be in the range 0 to 31.");

            shaderProgram.SetInput(colorKeyName, (float)colorIndex);
        }

        public new static TextureShader Create(State state) => new TextureShader(state);
    }
}
