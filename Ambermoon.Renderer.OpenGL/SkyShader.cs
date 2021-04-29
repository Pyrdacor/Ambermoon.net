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
    internal class SkyShader : TextureShader
    {
        internal static readonly string DefaultLightName = "light";

        // The palette has a size of 32xNumPalettes pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        protected static string[] SkyFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultColorKeyName};",
            $"uniform float {DefaultLightName};",
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
            $"        if (maskColIndex >= 0.5f)",
            $"            pixelColor = texture({DefaultPaletteName}, vec2((maskColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"        if ({DefaultLightName} < 0.01f)",
            $"            discard;",
            $"        else",
            $"            {DefaultFragmentOutColorName} = vec4(max(vec3(0), pixelColor.rgb + vec3({DefaultLightName}) - 1), pixelColor.a);",
            $"    }}",
            $"}}"
        };

        SkyShader(State state)
            : base(state, SkyFragmentShader(state), TextureVertexShader(state))
        {

        }

        public void SetLight(float light)
        {
            shaderProgram.SetInput(DefaultLightName, light);
        }

        public new static SkyShader Create(State state) => new SkyShader(state);
    }
}
