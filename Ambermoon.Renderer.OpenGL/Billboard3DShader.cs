/*
 * Billboard3DShader.cs - Shader for textured 3D billboards
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
    internal class Billboard3DShader : Texture3DShader
    {
        internal static readonly string DefaultBillboardCenterName = "center";
        internal static readonly string DefaultBillboardOrientationName = "orientation";
        internal static readonly string DefaultExtrudeName = "extrude";

        static string[] Billboard3DFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultLightName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in vec2 textureEndCoord;",
            $"flat in vec2 textureSize;",
            $"flat in float depth;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 realTexCoord = varTexCoord;",
            $"    if (realTexCoord.x >= textureEndCoord.x)",
            $"        realTexCoord.x -= int((textureSize.x + realTexCoord.x - textureEndCoord.x) / textureSize.x) * textureSize.x;",
            $"    if (realTexCoord.y >= textureEndCoord.y)",
            $"        realTexCoord.y -= int((textureSize.y + realTexCoord.y - textureEndCoord.y) / textureSize.y) * textureSize.y;",
            $"    float colorIndex = texture({DefaultSamplerName}, realTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / 52.0f));",
            $"    ",
            $"    if (colorIndex < 0.5f || pixelColor.a < 0.5f)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = vec4({DefaultLightName} * pixelColor.rg, min(1.0f, 0.25f + {DefaultLightName}) * pixelColor.b, pixelColor.a);",
            $"     gl_FragDepth = 0.5 * depth + 0.5;",
            $"}}"
        };
        // Note: gl_FragDepth = 0.5 * depth + 0.5 is basically (far-near)/2 * depth + (far+near)/2 with far = 1.0 and near = 0.0 (gl_DepthRange uses 0.0 to 1.0).
        // If the depth range is changed, this formula has to be adjusted accordingly!

        static string[] Billboard3DVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in vec3 {DefaultPositionName};",
            $"in vec3 {DefaultBillboardCenterName};",
            $"in uint {DefaultBillboardOrientationName};",
            $"in ivec2 {DefaultTexCoordName};",
            $"in ivec2 {DefaultTexEndCoordName};",
            $"in ivec2 {DefaultTexSizeName};",
            $"in uint {DefaultPaletteIndexName};",
            $"in float {DefaultExtrudeName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"flat out float palIndex;",
            $"flat out vec2 textureEndCoord;",
            $"flat out vec2 textureSize;",
            $"flat out float depth;",
            $"",
            $"void main()",
            $"{{",
            $"    vec3 offset = ({DefaultPositionName} - {DefaultBillboardCenterName});",
            $"    vec4 localPos = {DefaultModelViewMatrixName} * vec4({DefaultBillboardCenterName}, 1);",
            $"    if ({DefaultBillboardOrientationName} == 1u) // floor",
            $"        localPos += vec4(offset.x, 0, offset.z, 0);",
            $"    else // normal",
            $"        localPos += vec4(offset.xy, {DefaultExtrudeName}, 0);",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    textureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
            $"    textureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
            $"    gl_Position = {DefaultProjectionMatrixName} * localPos;",
            $"    if ({DefaultBillboardOrientationName} == 1u) // floor",
            $"    {{",
            $"        localPos.z += {DefaultExtrudeName};",
            $"        vec4 adjustedFloorPos = {DefaultProjectionMatrixName} * localPos;",
            $"        depth = adjustedFloorPos.z / adjustedFloorPos.w;",
            $"    }}",
            $"    else // normal",
            $"    {{",
            $"        depth = gl_Position.z / gl_Position.w;",
            $"    }}",
            $"}}"
        };

        Billboard3DShader(State state)
            : this(state, Billboard3DFragmentShader(state), Billboard3DVertexShader(state))
        {

        }

        protected Billboard3DShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, fragmentShaderLines, vertexShaderLines)
        {

        }

        public new static Billboard3DShader Create(State state) => new Billboard3DShader(state);
    }
}
