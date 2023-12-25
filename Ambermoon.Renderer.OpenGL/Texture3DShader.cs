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

using Ambermoon.Data;

namespace Ambermoon.Renderer
{
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

        // The palette has a size of 32xNumPalettes pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        static string[] Texture3DFragmentShader(State state) => new string[]
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
            $"uniform vec4 {DefaultFogColorName} = vec4(0);",
            $"in vec2 varTexCoord;",
            $"in float distance;",
            $"flat in float palIndex;",
            $"flat in vec2 textureEndCoord;",
            $"flat in vec2 textureSize;",
            $"flat in float alphaEnabled;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 realTexCoord = varTexCoord;",
            $"    if (realTexCoord.x >= textureEndCoord.x)",
            $"        realTexCoord.x -= floor((textureSize.x + realTexCoord.x - textureEndCoord.x) / textureSize.x) * textureSize.x;",
            $"    if (realTexCoord.y >= textureEndCoord.y)",
            $"        realTexCoord.y -= floor((textureSize.y + realTexCoord.y - textureEndCoord.y) / textureSize.y) * textureSize.y;",
            $"    float colorIndex = texture({DefaultSamplerName}, realTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = {DefaultUseColorReplaceName} > 0.5f && colorIndex < 15.5f ? {DefaultColorReplaceName}[int(colorIndex + 0.5f)]",
            $"        : texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {DefaultPaletteCountName}));",
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
            $"        {DefaultFragmentOutColorName} = vec4(max(vec3(0), pixelColor.rgb + vec3({DefaultLightName}) - vec3(1)), pixelColor.a);",
            $"    ",
            $"    if ({DefaultFogColorName}.a > 0.001f)",
            $"    {{",
            $"        float fogFactor = {DefaultFogColorName}.a * min({DefaultSkyColorIndexName} < 31.5f ? 0.75f : 1.0f, distance * ({DefaultSkyColorIndexName} < 31.5f ? 0.5f : 1.0f) / {Global.DistancePerBlock * 8.0f});",
            $"        {DefaultFragmentOutColorName} = {DefaultFragmentOutColorName} * (1.0f - fogFactor) + fogFactor * {DefaultFogColorName};",
            $"    }}",
            $"}}"
        };

        static string[] Texture3DVertexShader(State state) => new string[]
        {
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
            $"out float distance;",
            $"flat out float palIndex;",
            $"flat out vec2 textureEndCoord;",
            $"flat out vec2 textureSize;",
            $"flat out float alphaEnabled;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / float({DefaultAtlasSizeName}.x), 1.0f / float({DefaultAtlasSizeName}.y));",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    textureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
            $"    textureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
            $"    alphaEnabled = float({DefaultAlphaName});",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4({DefaultPositionName}, 1.0f);",
            $"    distance = gl_Position.z;",
            $"}}"
        };

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

        public void SetFogColor(Render.Color fogColor)
        {
            shaderProgram.SetInputVector4(DefaultFogColorName, fogColor.R / 255.0f,
                fogColor.G / 255.0f, fogColor.B / 255.0f, fogColor.A / 255.0f);
        }

        public new static Texture3DShader Create(State state) => new Texture3DShader(state);
    }
}
