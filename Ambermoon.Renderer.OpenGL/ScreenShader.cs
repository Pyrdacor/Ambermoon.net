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
            $"     5,  5, -3,",
            $"     5,  0, -3,",
            $"    -3, -3, -3",
            $");",
            $"const mat3 m10 = mat3",
            $"(",
            $"     5,  5,  5,",
            $"    -3,  0, -3,",
            $"    -3, -3, -3",
            $");",
            $"const mat3 m20 = mat3",
            $"(",
            $"    -3,  5,  5,",
            $"    -3,  0,  5,",
            $"    -3, -3, -3",
            $");",
            $"const mat3 m01 = mat3",
            $"(",
            $"     5, -3, -3,",
            $"     5,  0, -3,",
            $"     5, -3, -3",
            $");",
            $"const mat3 m21 = mat3",
            $"(",
            $"    -3, -3,  5,",
            $"    -3,  0,  5,",
            $"    -3, -3,  5",
            $");",
            $"const mat3 m02 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"     5,  0, -3,",
            $"     5,  5, -3",
            $");",
            $"const mat3 m12 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"    -3,  0, -3,",
            $"     5,  5,  5",
            $");",
            $"const mat3 m22 = mat3",
            $"(",
            $"    -3, -3, -3,",
            $"    -3,  0,  5,",
            $"    -3,  5,  5",
            $");",
            $"",
            $"float getGrayValue(mat3 colors, mat3 factors)",
            $"{{",
            $"    mat3 r = transpose(factors) * colors;",
            $"    return r[0][0] + r[1][0] + r[2][0] + r[0][1] + r[1][1] + r[2][1] + r[0][2] + r[1][2] + r[2][2];",
            $"}}",
            $"",
            $"float gray(vec4 color)",
            $"{{",
            $"    return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;",
            $"}}",
            $"",
            $"float value(vec4 color)",
            $"{{",
            $"    return max(color.r, max(color.g, color.b));",
            $"}}",
            $"",
            $"vec4 getColor(vec2 coord)",
            $"{{",
            $"    return texture({DefaultSamplerName}, coord);",
            $"}}",
            $"",
            $"void main()",
            $"{{",
            $"    if ({DefaultModeName} < 0.5f)",
            $"    {{",
            $"        {DefaultFragmentOutColorName} = getColor(varTexCoord);",
            $"    }}",
            $"    else",
            $"    {{",
            $"        vec2 pixelSize = vec2(1.0f / 320.0f, 1.0f / 200.0f);",
            $"        ",
            $"        vec4 color00 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y-pixelSize.y));",
            $"        vec4 color10 = getColor(vec2(varTexCoord.x, varTexCoord.y-pixelSize.y));",
            $"        vec4 color20 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y-pixelSize.y));",
            $"        vec4 color01 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y));",
            $"        vec4 color11 = getColor(varTexCoord);",
            $"        vec4 color21 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y));",
            $"        vec4 color02 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y+pixelSize.y));",
            $"        vec4 color12 = getColor(vec2(varTexCoord.x, varTexCoord.y+pixelSize.y));",
            $"        vec4 color22 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y+pixelSize.y));",
            $"        ",
            $"        mat3 colors = mat3(gray(color00), gray(color10), gray(color20),",
            $"                           gray(color01), gray(color11), gray(color21),",
            $"                           gray(color02), gray(color12), gray(color22));",
            $"        ",
            $"        float a = color11.a;",
            $"        vec2 pixel = varTexCoord * {DefaultResolutionName};",
            $"        vec2 offset = fract(varTexCoord * vec2(320.0f, 200.0f));",
            $"        ",
            $"        float smoothFactor = 0.65f;",
            $"        float leftFactor = smoothFactor * (1.0f - offset.x);",
            $"        float rightFactor = smoothFactor * offset.x;",
            $"        float topFactor = smoothFactor * (1.0f - offset.y);",
            $"        float bottomFactor = smoothFactor * offset.y;",
            $"        float edge = 1.0f / 9.0f;",
            $"        ",
            $"        float f00 = getGrayValue(colors, m00);",
            $"        float f10 = getGrayValue(colors, m10);",
            $"        float f20 = getGrayValue(colors, m20);",
            $"        float f01 = getGrayValue(colors, m01);",
            $"        float f21 = getGrayValue(colors, m21);",
            $"        float f02 = getGrayValue(colors, m02);",
            $"        float f12 = getGrayValue(colors, m12);",
            $"        float f22 = getGrayValue(colors, m22);",
            $"        float v = value(color11);",
            $"        float g00 = 1.0f - abs(v - value(color00));",
            $"        float g10 = 1.0f - abs(v - value(color10));",
            $"        float g20 = 1.0f - abs(v - value(color20));",
            $"        float g01 = 1.0f - abs(v - value(color01));",
            $"        float g21 = 1.0f - abs(v - value(color21));",
            $"        float g02 = 1.0f - abs(v - value(color02));",
            $"        float g12 = 1.0f - abs(v - value(color12));",
            $"        float g22 = 1.0f - abs(v - value(color22));",
            $"        color00 = mix(color11, color00, (1.0f - edge * max(f00, max(f10, f01))) * g00 * 0.5f * (leftFactor + topFactor));",
            $"        color10 = mix(color11, color10, (1.0f - edge * max(f00, max(f10, f20))) * g10 * topFactor);",
            $"        color20 = mix(color11, color20, (1.0f - edge * max(f10, max(f20, f21))) * g20 * 0.5f * (rightFactor + topFactor));",
            $"        color01 = mix(color11, color01, (1.0f - edge * max(f00, max(f01, f02))) * g01 * leftFactor);",
            $"        color21 = mix(color11, color21, (1.0f - edge * max(f20, max(f21, f22))) * g21 * rightFactor);",
            $"        color02 = mix(color11, color02, (1.0f - edge * max(f01, max(f02, f12))) * g02 * 0.5f * (leftFactor + bottomFactor));",
            $"        color12 = mix(color11, color12, (1.0f - edge * max(f02, max(f12, f22))) * g12 * bottomFactor);",
            $"        color22 = mix(color11, color22, (1.0f - edge * max(f21, max(f12, f22))) * g22 * 0.5f * (rightFactor + bottomFactor));",
            $"        ",
            $"        color11 = (color00 + color10 + color20 + color01 + color21 + color02 + color12 + color22) / 8.0f;",
            $"        color11 += vec4(0.005f, 0.005f, 0.005f, 0.0f);",
            $"        ",
            $"        float twoDim = a;",
            $"        if ({DefaultModeName} > 1.5f && {DefaultModeName} < 3.5f && int(mod(round(pixel.y + 0.49f), 2)) == 1)",
            $"        {{",
            $"            const vec3 add = gray(color11) < 0.025f ? vec3(0.0f) : vec3(-0.035f);",
            $"            if (twoDim < 0.5f) a = 0.125f;",
            $"            else color11.rgb += add;",
            $"        }}",
            $"        else if ({DefaultModeName} > 2.5f && {DefaultModeName} < 3.5f && int(mod(round(pixel.x + 0.49f), 2)) == 1)",
            $"        {{",
            $"            const vec3 add = gray(color11) < 0.025f ? vec3(0.0f) : vec3(-0.035f);",
            $"            if (twoDim < 0.5f) a = 0.125f;",
            $"            else color11.rgb += add;",
            $"        }}",
            $"        else if ({DefaultModeName} > 2.5f && {DefaultModeName} < 3.5f)",
            $"        {{",
            $"            if (twoDim > 0.5f)",
            $"                color11.rgb +=  gray(color11) < 0.025f ? vec3(0.0f) : vec3(0.075f);",
            $"        }}",
            $"        else if ({DefaultModeName} > 1.5f && {DefaultModeName} < 2.5f)",
            $"        {{",
            $"            if (twoDim > 0.5f)",
            $"                color11.rgb +=  gray(color11) < 0.025f ? vec3(0.0f) : vec3(0.035f);",
            $"        }}",
            $"        if ({DefaultModeName} > 3.5f && {DefaultModeName} < 4.5f)",
            $"        {{",
            $"            const float b = 1.0f;",
            $"            const float d = 0.85f;",
            $"            const vec3 add = gray(color11) < 0.1f ? vec3(0.0f) : vec3(0.05f);",
            $"            float m = int(mod(round(pixel.y + 0.49f), 5));",
            $"            if (m < 0.5f)",
            $"            {{",
            $"                color11.rgb *= vec3(b, d, d);",
            $"                color11.rgb += add;",
            $"                if (twoDim < 0.5f) {{ a = 0.15f; }}",
            $"                else {{ color11.a *= 0.875f; }}",
            $"            }}",
            $"            else if (m < 1.5f)",
            $"            {{",
            $"                color11.rgb *= vec3(d, b, d);",
            $"                color11.rgb += add;",
            $"                if (twoDim < 0.5f) {{ a = 0.15f; }}",
            $"                else {{ color11.a *= 0.875f; }}",
            $"            }}",
            $"            else if (m < 2.5f)",
            $"            {{",
            $"                color11.rgb *= vec3(d, d, b);",
            $"                color11.rgb += add;",
            $"                if (twoDim < 0.5f) {{ a = 0.15f; }}",
            $"                else {{ color11.a *= 0.875f; }}",
            $"            }}",
            $"            else if (m < 3.5f)",
            $"            {{",
            $"                if (twoDim < 0.5f) {{ a = 0.15f; }}",
            $"                else {{ color11.a *= 0.875f; color11.rgb *= vec3(0.75f, 0.75f, 0.75f); }}",
            $"            }}",
            $"        }}",
            $"        ",
            $"        // Preserve original alpha",
            $"        {DefaultFragmentOutColorName} = vec4(color11.rgb, a);",
            $"    }}",
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
            $"    vec2 pos = vec2(float({DefaultPositionName}.x), float({DefaultPositionName}.y));",
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

        protected ScreenShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
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
