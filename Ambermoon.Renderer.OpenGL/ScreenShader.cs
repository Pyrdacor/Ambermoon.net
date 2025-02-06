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

namespace Ambermoon.Renderer.OpenGL;

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
    internal static readonly string DefaultPrimaryModeName = "primMode";
    internal static readonly string DefaultSecondaryModeName = "secMode";

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

    static string[] ScreenFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"uniform sampler2D {DefaultSamplerName};",
        $"uniform vec2 {DefaultResolutionName};",
        $"uniform float {DefaultPrimaryModeName};",
        $"uniform float {DefaultSecondaryModeName};",
        $"in vec2 varTexCoord;",
        $"const vec2 pixelSize = vec2(1.0f / 320.0f, 1.0f / 200.0f);",
        $"",
        $"float gray(vec4 color)",
        $"{{",
        $"    return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;",
        $"}}",
        $"",
        $"vec4 getColor(vec2 coord)",
        $"{{",
        $"    return textureLod({DefaultSamplerName}, max(vec2(0.0f), min(vec2(1.0f), coord)), 0.0f);",
        $"}}",
        $"const mat3 yuvMatrix = mat3( 0.299f,    0.587f,    0.114f,",
        $"                            -0.14713f, -0.28886f,  0.436f,",
        $"                             0.615f,   -0.51499f, -0.10001f);",
        $"",
        $"float colorDistFactor(vec4 a, vec4 b)",
        $"{{",
        $"    vec3 yuvA = yuvMatrix * a.rgb;",
        $"    vec3 yuvB = yuvMatrix * b.rgb;",
        $"    vec3 yuvDist = (yuvB - yuvA) * vec3(1.25f, 1.0f, 1.0f); // weight luminance more than chroma",
        $"    const float normFactor = 0.2807f; // around 1 / (1.25^2 + 1^2 + 1^2)",
        $"    return 1.0f - min(1.0f, 5.5f * normFactor * sqrt(yuvDist.r * yuvDist.r + yuvDist.g * yuvDist.g + yuvDist.b * yuvDist.b));",
        $"}}",
        $"",
        $"vec4 processColor(vec4 color)",
        $"{{",
        $"    if (color.a < 0.9999f)",
        $"        return vec4(color.rgb, sqrt(color.a));",
        $"    else",
        $"        return color;",
        $"}}",
        $"",
        $"int sameColor(vec4 a, vec4 b)",
        $"{{",
        $"    vec4 diff = abs(a - b);",
        $"    if (diff.r < 0.00001f && diff.g < 0.00001f && diff.b < 0.00001f && diff.a < 0.00001f)",
        $"        return 1;",
        $"    return 0;",
        $"}}",
        $"",
        $"void main()",
        $"{{",
        $"    vec4 color;",
        $"    float a;",
        $"    if ({DefaultPrimaryModeName} < 0.5f)",
        $"    {{",
        $"        color = getColor(varTexCoord);",
        $"        a = color.a;",
        $"    }}",
        $"    else if ({DefaultPrimaryModeName} < 1.5f) // new smooth filter",
        $"    {{",
        $"        ",
        $"        vec2 outPixelSize = 1.0f / {DefaultResolutionName};",
        $"        vec4 color00 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y-pixelSize.y));",
        $"        vec4 color10 = getColor(vec2(varTexCoord.x, varTexCoord.y-pixelSize.y));",
        $"        vec4 color20 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y-pixelSize.y));",
        $"        vec4 color01 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y));",
        $"        vec4 color11 = getColor(varTexCoord);",
        $"        vec4 color21 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y));",
        $"        vec4 color02 = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y+pixelSize.y));",
        $"        vec4 color12 = getColor(vec2(varTexCoord.x, varTexCoord.y+pixelSize.y));",
        $"        vec4 color22 = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y+pixelSize.y));",
        $"        vec4 ocolor00 = getColor(vec2(varTexCoord.x-outPixelSize.x, varTexCoord.y-outPixelSize.y));",
        $"        vec4 ocolor10 = getColor(vec2(varTexCoord.x, varTexCoord.y-outPixelSize.y));",
        $"        vec4 ocolor20 = getColor(vec2(varTexCoord.x+outPixelSize.x, varTexCoord.y-outPixelSize.y));",
        $"        vec4 ocolor01 = getColor(vec2(varTexCoord.x-outPixelSize.x, varTexCoord.y));",
        $"        vec4 ocolor21 = getColor(vec2(varTexCoord.x+outPixelSize.x, varTexCoord.y));",
        $"        vec4 ocolor02 = getColor(vec2(varTexCoord.x-outPixelSize.x, varTexCoord.y+pixelSize.y));",
        $"        vec4 ocolor12 = getColor(vec2(varTexCoord.x, varTexCoord.y+outPixelSize.y));",
        $"        vec4 ocolor22 = getColor(vec2(varTexCoord.x+outPixelSize.x, varTexCoord.y+outPixelSize.y));",
        $"        ",
        $"        a = color11.a;",
        $"        vec2 offset = fract(varTexCoord * vec2(320.0f, 200.0f));",
        $"        ",
        $"        const float smoothFactor = 0.625f;",
        $"        const float mergeFactor = 1.15f;",
        $"        float leftFactor = (1.0f - offset.x);",
        $"        float rightFactor = offset.x;",
        $"        float topFactor = (1.0f - offset.y);",
        $"        float bottomFactor = offset.y;",
        $"        float upperLeftFactor = 0.5f * (leftFactor + topFactor) * (sameColor(color11, ocolor00) == 0 ? mergeFactor : smoothFactor);",
        $"        float upperRightFactor = 0.5f * (rightFactor + topFactor) * (sameColor(color11, ocolor20) == 0 ? mergeFactor : smoothFactor);",
        $"        float lowerLeftFactor = 0.5f * (leftFactor + bottomFactor) * (sameColor(color11, ocolor02) == 0 ? mergeFactor : smoothFactor);",
        $"        float lowerRightFactor = 0.5f * (rightFactor + bottomFactor) * (sameColor(color11, ocolor22) == 0 ? mergeFactor : smoothFactor);",
        $"        leftFactor *= (sameColor(color11, ocolor01) == 0 ? mergeFactor : smoothFactor);",
        $"        rightFactor *= (sameColor(color11, ocolor21) == 0 ? mergeFactor : smoothFactor);",
        $"        topFactor *= (sameColor(color11, ocolor10) == 0 ? mergeFactor : smoothFactor);",
        $"        bottomFactor *= (sameColor(color11, ocolor12) == 0 ? mergeFactor : smoothFactor);",
        $"        ",
        $"        float d00 = colorDistFactor(color11, color00);",
        $"        float d10 = colorDistFactor(color11, color10);",
        $"        float d20 = colorDistFactor(color11, color20);",
        $"        float d01 = colorDistFactor(color11, color01);",
        $"        float d21 = colorDistFactor(color11, color21);",
        $"        float d02 = colorDistFactor(color11, color02);",
        $"        float d12 = colorDistFactor(color11, color12);",
        $"        float d22 = colorDistFactor(color11, color22);",
        $"        color00 = mix(color11, color00, d00 * upperLeftFactor);",
        $"        color10 = mix(color11, color10, d10 * topFactor);",
        $"        color20 = mix(color11, color20, d20 * upperRightFactor);",
        $"        color01 = mix(color11, color01, d01 * leftFactor);",
        $"        color21 = mix(color11, color21, d21 * rightFactor);",
        $"        color02 = mix(color11, color02, d02 * lowerLeftFactor);",
        $"        color12 = mix(color11, color12, d12 * bottomFactor);",
        $"        color22 = mix(color11, color22, d22 * lowerRightFactor);",
        $"        ",
        $"        color11 = (color00 + color10 + color20 + color01 + color21 + color02 + color12 + color22) / 8.0f;",
        $"        color11 += vec4(0.005f, 0.005f, 0.005f, 0.0f);",
        $"        ",
        $"        ",
        $"        // Preserve original alpha",
        $"        color = vec4(color11.rgb, a);",
        $"    }}",
        $"    else // old blurry filter",
        $"    {{",
        $"        vec2 pixelSize = 1.0f / {DefaultResolutionName};",
        $"        vec2 texCoord = varTexCoord - 0.5f * pixelSize;",
        $"        color = getColor(texCoord);",
        $"        a = color.a;",
        $"        color = processColor(color);",
        $"        vec4 right = processColor(getColor(vec2(texCoord.x+pixelSize.x, texCoord.y)));",
        $"        vec4 down = processColor(getColor(vec2(texCoord.x, texCoord.y+pixelSize.y)));",
        $"        vec4 downRight = processColor(getColor(vec2(texCoord.x+pixelSize.x, texCoord.y+pixelSize.y)));",
        $"        vec4 upperColor = mix(color, right, 0.5f);",
        $"        vec4 lowerColor = mix(down, downRight, 0.5f);",
        $"        color = mix(upperColor, lowerColor, 0.5f);",
        $"        color.a = a;",
        $"    }}",
        $"    ",
        $"    float twoDim = a;",
        $"    if ({DefaultSecondaryModeName} > 0.5f && {DefaultSecondaryModeName} < 2.5f && mod(round(gl_FragCoord.y - 0.5f), 2.0f) > 0.5f)",
        $"    {{",
        $"        vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(-0.035f);",
        $"        if (twoDim < 0.5f) color.a = 0.125f;",
        $"        else color.rgb += add;",
        $"    }}",
        $"    else if ({DefaultSecondaryModeName} > 1.5f && {DefaultSecondaryModeName} < 2.5f && mod(round(gl_FragCoord.x - 0.5f), 2.0f) > 0.5f)",
        $"    {{",
        $"        vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(-0.035f);",
        $"        if (twoDim < 0.5f) color.a = 0.125f;",
        $"        else color.rgb += add;",
        $"    }}",
        $"    else if ({DefaultSecondaryModeName} > 1.5f && {DefaultSecondaryModeName} < 2.5f)",
        $"    {{",
        $"        if (twoDim > 0.5f)",
        $"            color.rgb += gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);",
        $"    }}",
        $"    else if ({DefaultSecondaryModeName} > 0.5f && {DefaultSecondaryModeName} < 1.5f)",
        $"    {{",
        $"        if (twoDim > 0.5f)",
        $"            color.rgb += gray(color) < 0.025f ? vec3(0.0f) : vec3(0.035f);",
        $"    }}",
        $"    if ({DefaultSecondaryModeName} > 2.5f && {DefaultSecondaryModeName} < 3.5f)",
        $"    {{",
        $"        const float b = 1.0f;",
        $"        const float d = 0.9f;",
        $"        vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);",
        $"        float m = mod(round(gl_FragCoord.y - 0.5f), 5.0f);",
        $"        if (m < 0.5f)",
        $"        {{",
        $"            color.rgb *= vec3(b, d, d);",
        $"            color.rgb += add;",
        $"            if (twoDim < 0.5f) {{ color.a = 0.15f; }}",
        $"            else {{ color.a *= 0.875f; }}",
        $"        }}",
        $"        else if (m < 1.5f)",
        $"        {{",
        $"            color.rgb *= vec3(d, b, d);",
        $"            color.rgb += add;",
        $"            if (twoDim < 0.5f) {{ color.a = 0.15f; }}",
        $"            else {{ color.a *= 0.875f; }}",
        $"        }}",
        $"        else if (m < 2.5f)",
        $"        {{",
        $"            color.rgb *= vec3(d, d, b);",
        $"            color.rgb += add;",
        $"            if (twoDim < 0.5f) {{ color.a = 0.15f; }}",
        $"            else {{ color.a *= 0.875f; }}",
        $"        }}",
        $"        else if (m < 3.5f)",
        $"        {{",
        $"            if (twoDim < 0.5f) {{ color.a = 0.15f; }}",
        $"            else {{ color.a *= 0.875f; color.rgb *= vec3(0.75f, 0.75f, 0.75f); }}",
        $"        }}",
        $"    }}",
        $"    if ({DefaultSecondaryModeName} > 3.5f && {DefaultSecondaryModeName} < 4.5f)",
        $"    {{",
        $"        float col = mod(round(gl_FragCoord.x - 0.5f), 6.0f);",
        $"        float row = mod(round(gl_FragCoord.y - 0.5f), 4.0f);",
        $"        float light = 0.1f + mod(round(gl_FragCoord.x - 0.5f), 12.0f) / 96.0f;",
        $"        if ((col < 0.5f && row < 2.5f) || (col > 2.5f && col < 3.5f && !(row > 0.5f && row < 1.5f)))",
        $"        {{",
        $"            // magenta",
        $"            if (twoDim < 0.5f) {{ color = vec4(0.8f, 0.0f, 0.8f, 0.15f); }}",
        $"            else color.rgb = mix(color.rgb, vec3(1.0f, 0.0f, 1.0f), light);",
        $"        }}",
        $"        else if ((col > 0.5f && col < 1.5f && row < 2.5f) || (col > 3.5f && col < 4.5f && !(row > 0.5f && row < 1.5f)))",
        $"        {{",
        $"            // lime",
        $"            if (twoDim < 0.5f) {{ color = vec4(0.0f, 0.9f, 0.0f, 0.15f); }}",
        $"            else color.rgb = mix(color.rgb, vec3(0.0f, 1.0f, 0.0f), light);",
        $"        }}",
        $"        else",
        $"        {{",
        $"            if (twoDim < 0.5f) {{ color.a = 0.15f; }}",
        $"            else color.rgb = mix(color.rgb, vec3(0.0f, 0.0f, 0.0f), light);",
        $"        }}",
        $"        vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);",
        $"        color.rgb += light * add;",
        $"    }}",
        $"    ",
        $"    {DefaultFragmentOutColorName} = color;",
        $"}}"
    ];

    static string[] ScreenVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"uniform float {DefaultPrimaryModeName};",
        $"in vec2 {DefaultPositionName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"uniform vec2 {DefaultResolutionName};",
        $"out vec2 varTexCoord;",
        $"const vec2 endUV = vec2(319.0f / 320.0f, 199.0f / 200.0f);",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    float u = pos.x > 0.5f ? endUV.x : 0.0f;",
        $"    float v = pos.y > 0.5f ? endUV.y : 0.0f;",
        $"    varTexCoord = vec2(u, 1.0f - v);",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 0.001f, 1.0f);",
        $"}}"
    ];

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

    /// <summary>
    /// Changes the filter modes.
    /// </summary>
    /// <param name="primary">0: No filter, 1: Blurry filter (old), 2: New filter</param>
    /// <param name="secondary">0: No addition, 1: Vertical lines, 2: Grid, 3: Scan lines</param>
    public void SetMode(int primary, int secondary)
    {
        shaderProgram.SetInput(DefaultPrimaryModeName, (float)primary);
        shaderProgram.SetInput(DefaultSecondaryModeName, (float)secondary);
    }

    public static ScreenShader Create(State state) => new ScreenShader(state);
}
