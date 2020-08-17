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
    // TODO: some billboards appear much too narrow (seems dependent on overall coordinate)
    internal class Billboard3DShader : Texture3DShader
    {
        internal static readonly string DefaultTexCoordName = Texture3DShader.DefaultTexCoordName;
        internal static readonly string DefaultSamplerName = Texture3DShader.DefaultSamplerName;
        internal static readonly string DefaultAtlasSizeName = Texture3DShader.DefaultAtlasSizeName;
        internal static readonly string DefaultTexEndCoordName = Texture3DShader.DefaultTexEndCoordName;
        internal static readonly string DefaultTexSizeName = Texture3DShader.DefaultTexSizeName;
        internal static readonly string DefaultPaletteName = Texture3DShader.DefaultPaletteName;
        internal static readonly string DefaultPaletteIndexName = Texture3DShader.DefaultPaletteIndexName;
        internal static readonly string DefaultCameraPositionName = "camPos";
        internal static readonly string DefaultCameraDirectionName = "camDir"; // TODO: not used anymore. delete later.
        internal static readonly string DefaultBillboardCenterName = "center";
        internal static readonly string DefaultScaleName = "scale";

        readonly string texCoordName;
        readonly string texEndCoordName;
        readonly string texSizeName;
        readonly string samplerName;
        readonly string atlasSizeName;
        readonly string paletteName;
        readonly string paletteIndexName;
        readonly string cameraPositionName;
        readonly string cameraDirectionName;
        readonly string billboardCenterName;
        readonly string scaleName;

        static string[] Billboard3DFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in vec2 textureEndCoord;",
            $"flat in vec2 textureSize;",
            $"flat in vec2 oneTexturePixel;",
            $"flat in float depth;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 realTexCoord = varTexCoord;",
            $"    if (realTexCoord.x > textureEndCoord.x)",
            $"        realTexCoord.x -= int((textureSize.x - oneTexturePixel.x + realTexCoord.x - textureEndCoord.x) / textureSize.x) * textureSize.x;",
            $"    if (realTexCoord.y > textureEndCoord.y)",
            $"        realTexCoord.y -= int((textureSize.y - oneTexturePixel.y + realTexCoord.y - textureEndCoord.y) / textureSize.y) * textureSize.y;",
            $"    float colorIndex = texture({DefaultSamplerName}, realTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / 51.0f));",
            $"    ",
            $"    if (colorIndex < 0.5f || pixelColor.a < 0.5f)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = pixelColor;",
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
            $"in ivec2 {DefaultTexCoordName};",
            $"in ivec2 {DefaultTexEndCoordName};",
            $"in ivec2 {DefaultTexSizeName};",
            $"in uint {DefaultPaletteIndexName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"uniform vec3 {DefaultCameraPositionName};",
            $"uniform vec3 {DefaultCameraDirectionName};",
            $"uniform float {DefaultScaleName};",
            $"out vec2 varTexCoord;",
            $"flat out float palIndex;",
            $"flat out vec2 textureEndCoord;",
            $"flat out vec2 textureSize;",
            $"flat out vec2 oneTexturePixel;",
            $"flat out float depth;",
            $"",
            $"void main()",
            $"{{",
            $"    vec3 localUpVector = vec3(0, 1, 0);",
            $"    vec3 camLook = {DefaultCameraPositionName} - {DefaultBillboardCenterName};",
            $"    camLook.y = 0.0;",
            $"    vec3 zaxis = normalize(camLook);",
            $"    vec3 xaxis = normalize(cross(localUpVector, zaxis));",
            $"    vec3 yaxis = cross(zaxis, xaxis);",
            $"    mat3 lookAtMatrix = mat3(xaxis, yaxis, zaxis);",
            $"    vec3 offset = lookAtMatrix * ({DefaultPositionName} - {DefaultBillboardCenterName});",
            $"    vec4 localPos = {DefaultModelViewMatrixName} * vec4({DefaultBillboardCenterName}, 1) + scale * vec4(offset.xy, 0, 0);",
            $"    ",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    textureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
            $"    textureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
            $"    oneTexturePixel = atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * localPos;",
            $"    vec4 realPos = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4({DefaultBillboardCenterName}, 1);",
            $"    depth = realPos.z / realPos.w;",
            $"}}"
        };

        Billboard3DShader(State state)
            : this(state, DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultPositionName,
                  DefaultTexCoordName, DefaultTexEndCoordName, DefaultTexSizeName, DefaultSamplerName,
                  DefaultAtlasSizeName, DefaultPaletteName, DefaultPaletteIndexName, DefaultCameraPositionName,
                  DefaultCameraDirectionName, DefaultBillboardCenterName, DefaultScaleName,
                  Billboard3DFragmentShader(state), Billboard3DVertexShader(state))
        {

        }

        protected Billboard3DShader(State state, string modelViewMatrixName, string projectionMatrixName,
            string positionName, string texCoordName, string texEndCoordName, string texSizeName,
            string samplerName, string atlasSizeName, string paletteName, string paletteIndexName,
            string cameraPositionName, string cameraDirectionName, string billboardCenterName,
            string scaleName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, modelViewMatrixName, projectionMatrixName, positionName, texCoordName, texEndCoordName,
                  texSizeName, samplerName, atlasSizeName, paletteName, paletteIndexName, null, fragmentShaderLines,
                  vertexShaderLines)
        {
            this.cameraPositionName = cameraPositionName;
            this.cameraDirectionName = cameraDirectionName;
            this.billboardCenterName = billboardCenterName;
            this.scaleName = scaleName;
        }

        public void SetCameraPosition(float x, float y, float z)
        {
            shaderProgram.SetInputVector3(cameraPositionName, x, y, z);
        }

        public void SetCameraDirection(float x, float y, float z)
        {
            shaderProgram.SetInputVector3(cameraDirectionName, x, y, z);
        }

        public void SetScale(float scale)
        {
            shaderProgram.SetInput(scaleName, scale);
        }

        public new static Billboard3DShader Create(State state) => new Billboard3DShader(state);
    }
}
