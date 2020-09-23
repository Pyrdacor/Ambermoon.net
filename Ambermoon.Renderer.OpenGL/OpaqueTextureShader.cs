/*
 * OpaqueTextureShader.cs - Shader for opaque textured objects
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
    internal class OpaqueTextureShader : TextureShader
    {
        static string[] OpaqueTextureFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"",
            $"void main()",
            $"{{",
            $"    float colorIndex = texture({DefaultSamplerName}, varTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / 52.0f));",
            $"    ",
            $"    {DefaultFragmentOutColorName} = pixelColor;",
            $"}}"
        };

        OpaqueTextureShader(State state)
            : base(state, DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName,
                  DefaultTexCoordName, DefaultSamplerName, DefaultAtlasSizeName, DefaultLayerName, DefaultPaletteName,
                  DefaultPaletteIndexName, DefaultColorKeyName, OpaqueTextureFragmentShader(state), TextureVertexShader(state))
        {

        }

        public new static OpaqueTextureShader Create(State state) => new OpaqueTextureShader(state);
    }
}
