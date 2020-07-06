/*
 * MaskedTextureShader.cs - Shader for masked textured objects
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
    internal sealed class MaskedTextureShader : TextureShader
    {
        internal static readonly string DefaultMaskTexCoordName = "maskTexCoord";

        readonly string maskTexCoordName;

        static string[] MaskedTextureFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"in vec2 varTexCoord;",
            $"in vec2 varMaskTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec4 pixelColor = texture({DefaultSamplerName}, varTexCoord);",
            $"    vec4 maskColor = texture({DefaultSamplerName}, varMaskTexCoord);",
            $"    ",
            $"    pixelColor.r *= maskColor.r;",
            $"    pixelColor.g *= maskColor.g;",
            $"    pixelColor.b *= maskColor.b;",
            $"    pixelColor.a *= maskColor.a;",
            $"    ",
            $"    if (pixelColor.a < 0.5)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = pixelColor;",
            $"}}"
        };

        static string[] MaskedTextureVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"in ivec2 {DefaultTexCoordName};",
            $"in ivec2 {DefaultMaskTexCoordName};",
            $"in uint {DefaultLayerName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"out vec2 varMaskTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    varMaskTexCoord = vec2({DefaultMaskTexCoordName}.x, {DefaultMaskTexCoordName}.y);",
            $"    ",
            $"    varTexCoord *= atlasFactor;",
            $"    varMaskTexCoord *= atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        MaskedTextureShader(State state)
            : this(state, DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultAtlasSizeName, DefaultMaskTexCoordName, DefaultLayerName,
                  DefaultPaletteName, MaskedTextureFragmentShader(state), MaskedTextureVertexShader(state))
        {

        }

        MaskedTextureShader(State state, string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string atlasSizeName, string maskTexCoordName,
            string layerName, string paletteName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, modelViewMatrixName, projectionMatrixName, zName, positionName, texCoordName, samplerName,
                  atlasSizeName, layerName, paletteName, fragmentShaderLines, vertexShaderLines)
        {
            this.maskTexCoordName = maskTexCoordName;
        }

        public new static MaskedTextureShader Create(State state) => new MaskedTextureShader(state);
    }
}
