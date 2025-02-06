/*
 * Billboard3DShader.cs - Shader for textured 3D billboards
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

namespace Ambermoon.Renderer.OpenGL;

// NOTE: This won't support non-palette graphics as it uses the color indices
// for color replacements.
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
        $"uniform float {DefaultPaletteCountName};",
        $"uniform vec4 {DefaultColorReplaceName}[16];",
        $"uniform float {DefaultUseColorReplaceName};",
        $"uniform float {DefaultSkyColorIndexName};",
        $"uniform vec4 {DefaultSkyReplaceColorName};",
        $"uniform vec4 {DefaultFogColorName};",
        $"uniform float {DefaultFogDistanceName};",
        $"uniform float {DefaultFadeName};",
        $"in vec2 varTexCoord;",
        $"in float dist;",
        $"in float drawY;",
        $"flat in float palIndex;",
        $"flat in vec2 varTextureEndCoord;",
        $"flat in vec2 varTextureSize;",
        $"",
        $"void main()",
        $"{{",
        $"    {DefaultFragmentOutColorName} = vec4(0);",
        $"    vec2 realTexCoord = varTexCoord;",
        $"    if (realTexCoord.x >= varTextureEndCoord.x)",
        $"        realTexCoord.x -= floor((varTextureSize.x + realTexCoord.x - varTextureEndCoord.x) / varTextureSize.x) * varTextureSize.x;",
        $"    if (realTexCoord.y >= varTextureEndCoord.y)",
        $"        realTexCoord.y -= floor((varTextureSize.y + realTexCoord.y - varTextureEndCoord.y) / varTextureSize.y) * varTextureSize.y;",
        $"    float colorIndex = textureLod({DefaultSamplerName}, realTexCoord, 0.0f).r * 255.0f;",
        $"    vec4 pixelColor = {DefaultUseColorReplaceName} > 0.5f && colorIndex < 15.5f ? {DefaultColorReplaceName}[int(colorIndex + 0.5f)]",
        $"        : textureLod({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}), 0.0f);",
        $"    ",
        $"    ",
        $"    if (colorIndex < 0.5f || pixelColor.a < 0.5f || {DefaultLightName} < 0.01f)",
        $"        discard;",
        $"    else if (abs(colorIndex - {DefaultSkyColorIndexName}) < 0.001f)",
        $"        {DefaultFragmentOutColorName} = {DefaultSkyReplaceColorName};",
        $"    else",
        $"        {DefaultFragmentOutColorName} = vec4(pixelColor.rgb + vec3({DefaultLightName}) - vec3(1), pixelColor.a);",
        $"    ",
        $"    if ({DefaultFogColorName}.a > 0.001f)",
        $"    {{",
        $"        float fogFactor = {DefaultFogColorName}.a * ({DefaultSkyColorIndexName} < 31.5f && drawY > 0.0f ? min({DefaultSkyColorIndexName} < 31.5f ? 0.8f : 1.0f, dist / ({DefaultFogDistanceName} * (1.0f + 2.5f * drawY))) : min({DefaultSkyColorIndexName} < 31.5f ? 0.8f : 1.0f, dist / {DefaultFogDistanceName}));",
        $"        {DefaultFragmentOutColorName} = {DefaultFragmentOutColorName} * (1.0f - fogFactor) + fogFactor * {DefaultFogColorName};",
        $"    }}",
        $"    if ({DefaultFadeName} < 0.9999f)",
        $"    {{",
        $"        {DefaultFragmentOutColorName} = {DefaultFragmentOutColorName} * {DefaultFadeName};",
        $"    }}",
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
        $"out float dist;",
        $"out float drawY;",
        $"flat out float palIndex;",
        $"flat out vec2 varTextureEndCoord;",
        $"flat out vec2 varTextureSize;",
        $"",
        $"void main()",
        $"{{",
        $"    vec3 offset = ({DefaultPositionName} - {DefaultBillboardCenterName});",
        $"    vec4 localPos = {DefaultModelViewMatrixName} * vec4({DefaultBillboardCenterName}, 1);",
        $"    if ({DefaultBillboardOrientationName} == 0u) // normal billboard",
        $"    {{",
        $"        localPos += vec4(offset.xy, {DefaultExtrudeName}, 0);",
        $"    }}",
        $"    else // floor or ceiling billboard",
        $"    {{",
        $"        vec4 rotatedOffset = -1.0f * {DefaultModelViewMatrixName} * vec4(offset.x, 0, offset.y, 0);",
        $"        localPos += vec4(rotatedOffset.x, 0, rotatedOffset.z, 0);",
        $"    }}",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    varTextureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
        $"    varTextureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
        $"    gl_Position = {DefaultProjectionMatrixName} * localPos;",
        $"    dist = gl_Position.z;",
        $"    drawY = localPos.y;",
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
