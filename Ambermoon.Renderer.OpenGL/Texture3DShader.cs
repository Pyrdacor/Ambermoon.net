/*
 * Texture3DShader.cs - Shader for textured 3D objects
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using Data;

// NOTE: This won't support non-palette graphics as it uses the color indices
// for color replacements.
internal class Texture3DShader : ColorShader
{
    internal static readonly string DefaultTexCoordName = TextureShader.DefaultTexCoordName;
    internal static readonly string DefaultSamplerName = TextureShader.DefaultSamplerName;
    internal static readonly string DefaultAtlasSizeName = TextureShader.DefaultAtlasSizeName;
    internal static readonly string DefaultTexEndCoordName = "texEndCoord";
    internal static readonly string DefaultTexSizeName = "texSize";
    internal static readonly string DefaultPaletteName = TextureShader.DefaultPaletteName;
    internal static readonly string DefaultPaletteIndexName = TextureShader.DefaultPaletteIndexName;
    internal static readonly string DefaultAlphaName = "alpha";
    internal static readonly string DefaultLightName = "light";
    internal static readonly string DefaultColorReplaceName = "palReplace";
    internal static readonly string DefaultUseColorReplaceName = "useReplace";
    internal static readonly string DefaultSkyColorIndexName = "skyColorIndex";
    internal static readonly string DefaultSkyReplaceColorName = "skyColorReplace";
    internal static readonly string DefaultPaletteCountName = TextureShader.DefaultPaletteCountName;
    internal static readonly string DefaultFogColorName = "fogColor";
    internal static readonly string DefaultFogDistanceName = "fogDist";
    internal static readonly string DefaultFadeName = "fade";
    internal static readonly string DefaultRealisticEnabledName = "rlEnabled";
    internal static readonly string DefaultRealisticAmbientName = "rlAmbient";
    internal static readonly string DefaultRealisticTorchName = "rlTorch";
    internal static readonly string DefaultRealisticTorchPosName = "rlTorchPos";
    internal static readonly string DefaultRealisticLightPosName = "rlLightPos";
    internal static readonly string DefaultRealisticLightColorName = "rlLightColor";
    internal static readonly string DefaultRealisticLightCountName = "rlLightCount";
    internal const int MaxRealisticLights = 16;

    /// <summary>
    /// Uniforms and the light function of the realistic lighting.
    /// The fragment shader needs the input "viewPos" (position in view space).
    /// RealisticLight returns the light color which is multiplied with the pixel color.
    /// </summary>
    protected static string[] RealisticLightingFragmentCode =>
    [
        $"uniform float {DefaultRealisticEnabledName};",
        $"uniform vec3 {DefaultRealisticAmbientName};",
        $"uniform vec4 {DefaultRealisticTorchName};",
        $"uniform vec3 {DefaultRealisticTorchPosName};",
        $"uniform vec4 {DefaultRealisticLightPosName}[{MaxRealisticLights}];",
        $"uniform vec4 {DefaultRealisticLightColorName}[{MaxRealisticLights}];",
        $"uniform int {DefaultRealisticLightCountName};",
        $"in vec3 viewPos;",
        $"",
        $"vec3 RealisticLight(vec3 normal)",
        $"{{",
        $"    vec3 lightColor = {DefaultRealisticAmbientName};",
        $"    if ({DefaultRealisticTorchName}.a > 0.0f)",
        $"    {{",
        $"        vec3 toFrag = viewPos - {DefaultRealisticTorchPosName};",
        $"        float d = length(toFrag);",
        $"        vec3 dir = toFrag / max(d, 0.0001f);",
        $"        float att = clamp(1.0f - d * d / ({DefaultRealisticTorchName}.a * {DefaultRealisticTorchName}.a), 0.0f, 1.0f);",
        $"        att *= att;",
        $"        float cone = mix(0.5f, 1.0f, smoothstep(0.3f, 0.85f, -dir.z));",
        $"        float lambert = mix(0.6f, 1.0f, abs(dot(normal, dir)));",
        $"        lightColor += {DefaultRealisticTorchName}.rgb * att * cone * lambert;",
        $"    }}",
        $"    for (int i = 0; i < {MaxRealisticLights}; ++i)",
        $"    {{",
        $"        if (i >= {DefaultRealisticLightCountName})",
        $"            break;",
        $"        vec3 toFrag = viewPos - {DefaultRealisticLightPosName}[i].xyz;",
        $"        float d = length(toFrag);",
        $"        float r = {DefaultRealisticLightPosName}[i].w;",
        $"        float att = clamp(1.0f - d * d / (r * r), 0.0f, 1.0f);",
        $"        att *= att;",
        $"        float lambert = mix(0.6f, 1.0f, abs(dot(normal, toFrag / max(d, 0.0001f))));",
        $"        lightColor += {DefaultRealisticLightColorName}[i].rgb * att * lambert;",
        $"    }}",
        $"    return min(lightColor, vec3(1.5f));",
        $"}}",
        $"",
        $"vec3 RealisticNormal()",
        $"{{",
        $"    vec3 n = cross(dFdx(viewPos), dFdy(viewPos));",
        $"    float l = length(n);",
        $"    return l > 0.0000001f ? n / l : vec3(0.0f, 0.0f, 1.0f);",
        $"}}",
    ];

    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    static string[] Texture3DFragmentShader(State state) =>
    [
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
        $"flat in float palIndex;",
        $"flat in vec2 varTextureEndCoord;",
        $"flat in vec2 varTextureSize;",
        $"flat in float alphaEnabled;",
        .. RealisticLightingFragmentCode,
        $"",
        $"void main()",
        $"{{",
        $"    // derivatives must be calculated before any branching",
        $"    vec3 normal = RealisticNormal();",
        $"    vec3 rl = vec3(1.0f);",
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
        $"    if (alphaEnabled > 0.5f && alphaEnabled < 1.5f && (colorIndex < 0.5f || pixelColor.a < 0.5f) || {DefaultLightName} < 0.01f)",
        $"        discard;",
        $"    else if (abs(colorIndex - {DefaultSkyColorIndexName}) < 0.5f)",
        $"    {{",
        $"        if (alphaEnabled > 1.5f)",
        $"            discard;",
        $"        else",
        $"            {DefaultFragmentOutColorName} = {DefaultSkyReplaceColorName};",
        $"    }}",
        $"    else",
        $"    {{",
        $"        {DefaultFragmentOutColorName} = vec4(max(vec3(0), pixelColor.rgb + vec3({DefaultLightName}) - vec3(1)), pixelColor.a);",
        $"        if ({DefaultRealisticEnabledName} > 0.5f)",
        $"        {{",
        $"            rl = RealisticLight(normal);",
        $"            {DefaultFragmentOutColorName}.rgb = min(vec3(1.0f), {DefaultFragmentOutColorName}.rgb * rl);",
        $"        }}",
        $"    }}",
        $"    ",
        $"    if ({DefaultFogColorName}.a > 0.001f)",
        $"    {{",
        $"        float fogFactor = {DefaultFogColorName}.a * min({DefaultSkyColorIndexName} < 31.5f ? 0.8f : 1.0f, dist / {DefaultFogDistanceName});",
        $"        // with realistic lighting the fog is only visible where light is",
        $"        vec4 fogColor = vec4({DefaultFogColorName}.rgb * min(rl, vec3(1.0f)), {DefaultFogColorName}.a);",
        $"        {DefaultFragmentOutColorName} = {DefaultFragmentOutColorName} * (1.0f - fogFactor) + fogFactor * fogColor;",
        $"    }}",
        $"    if ({DefaultFadeName} < 0.9999f)",
        $"    {{",
        $"        {DefaultFragmentOutColorName} = {DefaultFragmentOutColorName} * {DefaultFadeName};",
        $"    }}",
        $"}}"
    ];

    static string[] Texture3DVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec3 {DefaultPositionName};",
        $"in ivec2 {DefaultTexCoordName};",
        $"in ivec2 {DefaultTexEndCoordName};",
        $"in ivec2 {DefaultTexSizeName};",
        $"in uint {DefaultPaletteIndexName};",
        $"in uint {DefaultAlphaName};",
        $"uniform uvec2 {DefaultAtlasSizeName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 varTexCoord;",
        $"out float dist;",
        $"flat out float palIndex;",
        $"flat out vec2 varTextureEndCoord;",
        $"flat out vec2 varTextureSize;",
        $"flat out float alphaEnabled;",
        $"out vec3 viewPos;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
        $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
        $"    palIndex = float({DefaultPaletteIndexName});",
        $"    varTextureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
        $"    varTextureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
        $"    alphaEnabled = float({DefaultAlphaName});",
        $"    vec4 localPos = {DefaultModelViewMatrixName} * vec4({DefaultPositionName}, 1.0f);",
        $"    viewPos = localPos.xyz;",
        $"    gl_Position = {DefaultProjectionMatrixName} * localPos;",
        $"    dist = gl_Position.z;",
        $"}}"
    ];

    Texture3DShader(State state)
        : this(state, Texture3DFragmentShader(state), Texture3DVertexShader(state))
    {

    }

    protected Texture3DShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        : base(state, fragmentShaderLines, vertexShaderLines)
    {

    }

    public void UsePalette(bool use)
    {
        shaderProgram.SetInput(DefaultPaletteName, use ? 1.0f : 0.0f);
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

    public void SetLight(float light)
    {
        shaderProgram.SetInput(DefaultLightName, light);
    }

    public void SetPaletteReplacement(PaletteReplacement paletteReplacement)
    {
        if (paletteReplacement == null)
        {
            shaderProgram.SetInput(DefaultUseColorReplaceName, 0.0f);
        }
        else
        {
            shaderProgram.SetInputColorArray(DefaultColorReplaceName, paletteReplacement.ColorData);
            shaderProgram.SetInput(DefaultUseColorReplaceName, 1.0f);
        }
    }

    public void SetSkyColorReplacement(uint? skyColor, Render.Color replaceColor)
    {
        shaderProgram.SetInput(DefaultSkyColorIndexName, skyColor == null ? 32.0f : skyColor.Value);
        if (replaceColor != null)
        {
            shaderProgram.SetInputVector4(DefaultSkyReplaceColorName, replaceColor.R / 255.0f,
                replaceColor.G / 255.0f, replaceColor.B / 255.0f, replaceColor.A / 255.0f);
        }
    }

    public void SetPaletteCount(int count)
    {
        shaderProgram.SetInput(DefaultPaletteCountName, (float)count);
    }

    public void SetFog(Render.Color fogColor, float distance)
    {
        shaderProgram.SetInputVector4(DefaultFogColorName, fogColor.R / 255.0f,
            fogColor.G / 255.0f, fogColor.B / 255.0f, fogColor.A / 255.0f);
        shaderProgram.SetInput(DefaultFogDistanceName, distance);
    }

    public void SetFade(float fade)
    {
        shaderProgram.SetInput(DefaultFadeName, fade);
    }

    public void DisableRealisticLighting()
    {
        shaderProgram.SetInput(DefaultRealisticEnabledName, 0.0f);
    }

    /// <summary>
    /// Light positions are in view space (xyz) with the radius in w.
    /// </summary>
    public void SetRealisticLighting(float ambientR, float ambientG, float ambientB,
        float torchR, float torchG, float torchB, float torchRange, float torchX, float torchY, float torchZ,
        float[] lightPositions, float[] lightColors, int lightCount)
    {
        shaderProgram.SetInput(DefaultRealisticEnabledName, 1.0f);
        shaderProgram.SetInputVector3(DefaultRealisticAmbientName, ambientR, ambientG, ambientB);
        shaderProgram.SetInputVector4(DefaultRealisticTorchName, torchR, torchG, torchB, torchRange);
        shaderProgram.SetInputVector3(DefaultRealisticTorchPosName, torchX, torchY, torchZ);
        shaderProgram.SetInput(DefaultRealisticLightCountName, lightCount);

        if (lightCount > 0)
        {
            shaderProgram.SetInputVector4Array(DefaultRealisticLightPosName, lightPositions, lightCount);
            shaderProgram.SetInputVector4Array(DefaultRealisticLightColorName, lightColors, lightCount);
        }
    }

    public new static Texture3DShader Create(State state) => new Texture3DShader(state);
}
