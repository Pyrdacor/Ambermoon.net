/*
 * ImageShader.cs - Used for displaying non-palette images
 *
 * Copyright (C) 2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

internal class ImageShader : TextureShader
{
    internal static readonly string DefaultAlphaName = AlphaTextureShader.DefaultAlphaName;

    static string[] ImageFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform sampler2D {DefaultSamplerName};",
        $"in vec2 varTexCoord;",
        $"flat in float a;",
        $"",
        $"void main()",
        $"{{",
        $"    vec4 color = textureLod({DefaultSamplerName}, varTexCoord, 0.0f);",
        $"    {DefaultFragmentOutColorName} = vec4(color.rgb, color.a * a);",
        $"}}"
    ];

    static string[] ImageVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"in ivec2 {DefaultTexCoordName};",
        $"in uint {DefaultLayerName};",
        $"in uint {DefaultAlphaName};",
        $"uniform float {DefaultZName};",
        $"uniform uvec2 {DefaultAtlasSizeName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 varTexCoord;",
        $"flat out float a;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    float z = 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f;",
        $"    a = float({DefaultAlphaName}) / 255.0f;",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, z, 1.0f);",
        $"}}"
    ];

    ImageShader(State state)
        : base(state, ImageFragmentShader(state), ImageVertexShader(state))
    {

    }

    public static new ImageShader Create(State state) => new ImageShader(state);
}
