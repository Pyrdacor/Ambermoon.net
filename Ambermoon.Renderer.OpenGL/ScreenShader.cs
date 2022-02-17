/*
 * ScreenShader.cs - Adds effects to the whole rendered screen
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
    // Note: The texture has the same size as the whole screen so
    // the position is also the texture coordinate!
    internal class ScreenShader
    {
        internal static readonly string DefaultFragmentOutColorName = "outColor";
        internal static readonly string DefaultPositionName = "position";
        internal static readonly string DefaultModelViewMatrixName = "mvMat";
        internal static readonly string DefaultProjectionMatrixName = "projMat";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultResolutionName = "resolution";
        internal static readonly string DefaultModeName = "mode";

        internal ShaderProgram shaderProgram;

        protected static string GetFragmentShaderHeader(State state)
        {
#if GLES
            string header = $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor:00} es\n";
#else
            string header = $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n";
#endif

            header += "\n";
            header += "#ifdef GL_ES\n";
            header += " precision highp float;\n";
            header += " precision highp int;\n";
            header += "#endif\n";
            header += "\n";
            header += $"out vec4 {DefaultFragmentOutColorName};\n";

            return header;
        }

        protected static string GetVertexShaderHeader(State state)
        {
#if GLES
            return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor:00} es\n\n";
#else
            return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n\n";
#endif
        }

        static readonly string PixelWidth = (1.0f / 320.0f).ToString(System.Globalization.CultureInfo.InvariantCulture);
        static readonly string PixelHeight = (1.0f / 200.0f).ToString(System.Globalization.CultureInfo.InvariantCulture);

        static string[] ScreenFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform vec2 {DefaultResolutionName};",
            $"uniform float {DefaultModeName};",
            $"in vec2 varTexCoord;",
            $"",
            $"const mat3 m00 = mat3",
            $"(",
            $"     5,  5,  5,",
            $"    -3,  0, -3,",
            $"    -3, -3, -3",
            $");",
            $"const mat3 m01 = mat3",
            $"(",
            $"     5,  5, -3,",
            $"     5,  0, -3,",
            $"    -3, -3, -3",
            $");",
            $"const mat3 m02 = mat3",
            $"(",
            $"     5, -3, -3,",
            $"     5,  0, -3,",
            $"     5, -3, -3",
            $");",
            $"const mat3 m03 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"     5,  0, -3,",
            $"     5,  5, -3",
            $");",
            $"const mat3 m04 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"    -3,  0, -3,",
            $"     5,  5,  5",
            $");",
            $"const mat3 m05 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"    -3,  0,  5,",
            $"    -3,  5,  5",
            $");",
            $"const mat3 m06 = mat3",
            $"(",
            $"    -3, -3,  5,",
            $"    -3,  0,  5,",
            $"    -3, -3,  5",
            $");",
            $"const mat3 m07 = mat3",
            $"(",
            $"    -3,  5,  5,",
            $"    -3,  0,  5,",
            $"    -3, -3, -3",
            $");",
            $"",
            $"float getGrayValue(mat3 colors, mat3 factors)",
            $"{{",
            $"    mat3 r = factors * colors;",
            $"    return r[0][0] + r[1][0] + r[2][0] + r[0][1] + r[1][1] + r[2][1] + r[0][2] + r[1][2] + r[2][2];",
            $"}}",
            $"",
            $"// Sobel edge detection",
            $"float getEdge(float x00, float x10, float x20,",
            $"              float x01, float x11, float x21,",
            $"              float x02, float x12, float x22)",
            $"{{",
            $"    mat3 colors = mat3(x00, x10, x20, x01, x11, x21, x02, x12, x22);",
            $"    return max(max(max(max(max(max(max(",
            $"      getGrayValue(colors, m00),",
            $"      getGrayValue(colors, m01)),",
            $"      getGrayValue(colors, m02)),",
            $"      getGrayValue(colors, m03)),",
            $"      getGrayValue(colors, m04)),",
            $"      getGrayValue(colors, m05)),",
            $"      getGrayValue(colors, m06)),",
            $"      getGrayValue(colors, m07));",
            $"}}",
            $"",
            $"float gray(vec4 color)",
            $"{{",
            $"    return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;",
            $"}}",
            $"",
            $"vec4 getVEdge(vec4 x00, vec4 x10, vec4 x20,",
            $"              vec4 x01, vec4 x11, vec4 x21,",
            $"              vec4 x02, vec4 x12, vec4 x22)",
            $"{{",
            $"    float y = getEdge(gray(x00), gray(x10), gray(x20),",
            $"                      gray(x01), gray(x11), gray(x21),",
            $"                      gray(x02), gray(x12), gray(x22));",
            $"    return vec4(y, y, y, 0.0f);",
            $"}}",
            $"",
            $"vec4 getColor(vec2 coord)",
            $"{{",
            $"    if (coord.x < 0.0f || coord.y < 0.0f || coord.x > 1.0f || coord.y > 1.0f)",
            $"        return vec4(0.0f, 0.0f, 0.0f, 1.0f);",
            $"    else",
            $"        return texture({DefaultSamplerName}, coord);",
            $"}}",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pixelSize = vec2(1.0f / {DefaultResolutionName}.x, 1.0f / {DefaultResolutionName}.y);",
            $"    ",
            $"    vec4 color00 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y-pixelSize.y));",
            $"    vec4 color10 = getColor(vec2(varTexCoord.x, varTexCoord.y-pixelSize.y));",
            $"    vec4 color20 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y-pixelSize.y));",
            $"    vec4 color01 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y));",
            $"    vec4 color11 = getColor(varTexCoord);",
            $"    vec4 color21 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y));",
            $"    vec4 color02 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y+pixelSize.y));",
            $"    vec4 color12 = getColor(vec2(varTexCoord.x, varTexCoord.y+pixelSize.y));",
            $"    vec4 color22 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y+pixelSize.y));",
            $"    ",
            $"    float a = color00.a;",
            $"    vec2 pixel = varTexCoord * {DefaultResolutionName};",
            $"    vec2 offset = fract(pixel);",
            $"    ",
            $"    // Edges will ignore surrounding pixels more",
            $"    float edgeInfluence = {DefaultModeName} < 0.5f ? 0.075f : 0.065f;",
            $"    vec4 edgeFactor = vec4(1) - edgeInfluence * getVEdge(color00, color10, color20, color01, color11, color21, color02, color12, color22);",
            $"    ",
            $"    float leftFactor = min(1.0f, 1.5f - offset.x);",
            $"    float rightFactor = min(1.0f, 0.5f + offset.x);",
            $"    float topFactor = min(1.0f, 1.5f - offset.y);",
            $"    float bottomFactor = min(1.0f, 0.5f + offset.y);",
            $"    ",
            $"    color00 = mix(color11, color00, edgeFactor * leftFactor * topFactor);",
            $"    color10 = mix(color11, color10, edgeFactor * topFactor);",
            $"    color20 = mix(color11, color20, edgeFactor * rightFactor * topFactor);",
            $"    color01 = mix(color11, color01, edgeFactor * leftFactor);",
            $"    color21 = mix(color11, color21, edgeFactor * rightFactor);",
            $"    color02 = mix(color11, color02, edgeFactor * leftFactor * bottomFactor);",
            $"    color12 = mix(color11, color12, edgeFactor * bottomFactor);",
            $"    color22 = mix(color11, color22, edgeFactor * rightFactor * bottomFactor);",
            $"    ",
            $"    color11 = (color00 + color10 + color20 + color01 + color21 + color02 + color12 + color22) / 8.0f;",
            $"    float twoDim = a;",
            $"    if ({DefaultModeName} > 0.5f && {DefaultModeName} < 2.5f && mod(int(round(pixel.y)), 2) == 1)",
            $"    {{",
            $"        const vec3 add = vec3(0.05f);",
            $"        if (twoDim < 0.5f) a = 0.2f;",
            $"        else color11 *= 0.8f;",
            $"        color11.rgb += add;",
            $"    }}",
            $"    else if ({DefaultModeName} > 1.5f && {DefaultModeName} < 2.5f && mod(int(round(pixel.x)), 2) == 1)",
            $"    {{",
            $"        const vec3 add = vec3(0.05f);",
            $"        if (twoDim < 0.5f) a = 0.2f;",
            $"        else color11 *= 0.8f;",
            $"        color11.rgb += add;",
            $"    }}",
            $"    if ({DefaultModeName} > 2.5f && {DefaultModeName} < 3.5f)",
            $"    {{",
            $"        const float b = 1.0f;",
            $"        const float d = 0.8f;",
            $"        const vec3 add = vec3(0.05f);",
            $"        float m = mod(int(round(pixel.y)), 5);",
            $"        if (m < 0.5f)",
            $"        {{",
            $"            color11.rgb *= vec3(b, d, d);",
            $"            color11.rgb += add;",
            $"            if (twoDim < 0.5f) {{ a = 0.1f; }}",
            $"            else {{ color11.a *= 0.9f; }}",
            $"        }}",
            $"        else if (m < 1.5f)",
            $"        {{",
            $"            color11.rgb *= vec3(d, b, d);",
            $"            color11.rgb += add;",
            $"            if (twoDim < 0.5f) {{ a = 0.1f; }}",
            $"            else {{ color11.a *= 0.9f; }}",
            $"        }}",
            $"        else if (m < 2.5f)",
            $"        {{",
            $"            color11.rgb *= vec3(d, d, b);",
            $"            color11.rgb += add;",
            $"            if (twoDim < 0.5f) {{ a = 0.1f; }}",
            $"            else {{ color11.a *= 0.9f; }}",
            $"        }}",
            $"        else if (m < 3.5f)",
            $"        {{",
            $"            if (twoDim < 0.5f) {{ a = 0.2f; }}",
            $"            else {{ color11.a *= 0.9f; color11.rgb *= vec3(0.75f, 0.75f, 0.75f); }}",
            $"        }}",
            $"    }}",
            $"    ",
            $"    // Preserve original alpha",
            $"    {DefaultFragmentOutColorName} = vec4(color11.rgb, a);",
            $"}}"
        };

        static string[] ScreenVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"uniform vec2 {DefaultResolutionName};",
            $"out vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = vec2(pos.x / {DefaultResolutionName}.x, ({DefaultResolutionName}.y - pos.y) / {DefaultResolutionName}.y);",
            $"    ",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 0.001f, 1.0f);",
            $"}}"
        };

        public void Use(Matrix4 projectionMatrix)
        {
            if (shaderProgram != ShaderProgram.ActiveProgram)
                shaderProgram.Use();

            shaderProgram.SetInputMatrix(DefaultModelViewMatrixName, Matrix4.Identity.ToArray(), true);
            shaderProgram.SetInputMatrix(DefaultProjectionMatrixName, projectionMatrix.ToArray(), true);
        }

        ScreenShader(State state)
            : this(state, ScreenFragmentShader(state), ScreenVertexShader(state))
        {

        }

        ScreenShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        {
            var fragmentShader = new Shader(state, Shader.Type.Fragment, string.Join("\n", fragmentShaderLines));
            var vertexShader = new Shader(state, Shader.Type.Vertex, string.Join("\n", vertexShaderLines));

            shaderProgram = new ShaderProgram(state, fragmentShader, vertexShader);
        }

        public ShaderProgram ShaderProgram => shaderProgram;

        public void SetSampler(int textureUnit = 0)
        {
            shaderProgram.SetInput(DefaultSamplerName, textureUnit);
        }

        public void SetResolution(Size resolution)
        {
            shaderProgram.SetInputVector2(DefaultResolutionName, (float)resolution.Width, (float)resolution.Height);
        }

        public void SetMode(int mode)
        {
            shaderProgram.SetInput(DefaultModeName, (float)mode);
        }

        public static ScreenShader Create(State state) => new ScreenShader(state);
    }
}
