/*
 * SubPixelTextShader.cs - Shader for sub-pixel positioned text rendering
 *
 * Copyright (C) 2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

internal class SubPixelTextShader : TextShader
{
    static string[] TextVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
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
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    textColIndex = float({DefaultTextColorIndexName});",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
        $"}}"
    ];

    SubPixelTextShader(State state)
        : base(state, TextVertexShader(state))
    {

    }

    public new static SubPixelTextShader Create(State state) => new(state);
}
