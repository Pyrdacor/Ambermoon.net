/*
 * EffectShader.cs - Adds effects like grayscale or sepia
 *
 * Copyright (C) 2022  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

// Note: The texture has the same size as the whole screen so
// the position is also the texture coordinate!
internal class EffectShader : ScreenShader
{
    static string[] EffectFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform vec2 {DefaultResolutionName};",
        $"uniform float {DefaultPrimaryModeName};",
        $"in vec2 varTexCoord;",
        $"",
        $"float gray(vec4 color)",
        $"{{",
        $"    return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;",
        $"}}",
        $"",
        $"vec4 getColor(vec2 coord)",
        $"{{",
        $"    return textureLod({DefaultSamplerName}, coord, 0.0f);",
        $"}}",
        $"",
        $"void main()",
        $"{{",
        $"    vec4 color = getColor(varTexCoord);",
        $"    if ({DefaultPrimaryModeName} > 0.5f && {DefaultPrimaryModeName} < 1.5f) // grayscale mode",
        $"    {{",
        $"        float g = gray(color);",
        $"        color = vec4(g, g, g, color.a);",
        $"    }}",
        $"    else if ({DefaultPrimaryModeName} > 1.5f && {DefaultPrimaryModeName} < 2.5f) // sepia mode",
        $"    {{",
        $"        float r = (color.r * 0.393f) + (color.g * 0.769f) + (color.b * 0.189f);",
        $"        float g = (color.r * 0.349f) + (color.g * 0.686f) + (color.b * 0.168f);",
        $"        float b = (color.r * 0.272f) + (color.g * 0.534f) + (color.b * 0.131f);",
        $"        color = vec4(r, g, b, color.a);",
        $"    }}",
        $"    {DefaultFragmentOutColorName} = color;",
        $"}}"
    ];

    static string[] EffectVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"uniform vec2 {DefaultResolutionName};",
        $"out vec2 varTexCoord;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    varTexCoord = vec2(pos.x / {DefaultResolutionName}.x, ({DefaultResolutionName}.y - pos.y) / {DefaultResolutionName}.y);",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 0.001f, 1.0f);",
        $"}}"
    ];

    EffectShader(State state)
        : base(state, EffectFragmentShader(state), EffectVertexShader(state))
    {

    }

    public void SetMode(int mode)
    {
        shaderProgram.SetInput(DefaultPrimaryModeName, (float)mode);
    }

    public static new EffectShader Create(State state) => new EffectShader(state);
}
