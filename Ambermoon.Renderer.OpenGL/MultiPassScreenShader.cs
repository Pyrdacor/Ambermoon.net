/*
 * MultiPassScreenShader.cs - Adds effects to the whole rendered screen in multiple passes
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
 *//*

using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;
using Silk.NET.OpenGLES;

namespace Ambermoon.Renderer.OpenGL;

// Note: The texture has the same size as the whole screen so
// the position is also the texture coordinate!
internal class MultiPassScreenShader : ScreenShader
{
    private static readonly string DefaultPrevPassSamplerName = "prevPassSampler";

    private readonly List<ShaderProgram> additionalPassesShaderPrograms = [];
    private readonly List<FrameBuffer> additionalFrameBuffers = [];

    static string[] ScreenFragmentShaders(State state) =>
    [
        GetFragmentShaderHeader(state) + $@"
        uniform sampler2D {DefaultSamplerName};
        uniform vec2 {DefaultResolutionName};
        uniform float {DefaultPrimaryModeName};
        uniform float {DefaultSecondaryModeName};
        in vec2 varTexCoord;
        const vec2 sourceSize = vec2(320.0f, 200.0f);
        const vec2 pixelSize = vec2(1.0f / 320.0f, 1.0f / 200.0f);
        
        float gray(vec4 color)
        {{
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }}
        
        vec4 getColor(vec2 coord)
        {{
            return textureLod({DefaultSamplerName}, clamp(coord, vec2(0.0f), vec2(1.0f)), 0.0f);
        }}
        const mat3 yuvMatrix = mat3( 0.299f,    0.587f,    0.114f,
                                    -0.14713f, -0.28886f,  0.436f,
                                     0.615f,   -0.51499f, -0.10001f);
        
        float colorDistFactor(vec4 a, vec4 b)
        {{
            vec3 yuvA = yuvMatrix * a.rgb;
            vec3 yuvB = yuvMatrix * b.rgb;
            vec3 yuvDist = (yuvB - yuvA) * vec3(1.25f, 1.0f, 1.0f); // weight luminance more than chroma
            const float normFactor = 0.2807f; // around 1 / (1.25^2 + 1^2 + 1^2)
            return 1.0f - min(1.0f, 5.5f * normFactor * sqrt(yuvDist.r * yuvDist.r + yuvDist.g * yuvDist.g + yuvDist.b * yuvDist.b));
        }}
        
        vec4 processColor(vec4 color)
        {{
            if (color.a < 0.9999f)
                return vec4(color.rgb, sqrt(color.a));
            else
                return color;
        }}
        
        int sameColor(vec4 a, vec4 b)
        {{
            vec4 diff = abs(a - b);
            if (diff.r < 0.00001f && diff.g < 0.00001f && diff.b < 0.00001f && diff.a < 0.00001f)
                return 1;
            return 0;
        }}
        
        vec3 rgb2yuv(vec3 c)
        {{
            // BT.601-ish; keep it cheap and stable
            float Y = 0.299*c.r + 0.587*c.g + 0.114*c.b;
            float U = -0.14713*c.r - 0.28886*c.g + 0.436*c.b;
            float V = 0.615*c.r - 0.51499*c.g - 0.10001*c.b;
            return vec3(Y, U, V);
        }}
        
        float colDist(vec3 a, vec3 b)
        {{
            // Weighted distance in YUV, favors luma
            vec3 da = rgb2yuv(a) - rgb2yuv(b);
            return abs(da.x)*1.0 + abs(da.y)*0.5 + abs(da.z)*0.5;
        }}
        
        vec4 texel(vec2 p)
        {{
            // p in SOURCE pixel space (centered at .5)
            return texture({DefaultSamplerName}, p * pixelSize);
        }}
        
        vec4 samplePx(vec2 base, ivec2 o)
        {{
            // base: center pixel coordinate in source pixel space (center at .5)
            return texel(base + vec2(o));
        }}

        vec3 rgb2xyz(vec3 c)
        {{
            // Gamma correction (sRGB -> linear RGB)
            vec3 cLin = mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(0.04045, c));

            // Convert to XYZ (D65)
            const mat3 M = mat3(
                0.4124564, 0.3575761, 0.1804375,
                0.2126729, 0.7151522, 0.0721750,
                0.0193339, 0.1191920, 0.9503041
            );

            return M * cLin;
        }}

        vec3 xyz2lab(vec3 xyz)
        {{
            // D65 reference white
            const vec3 refWhite = vec3(0.95047, 1.0, 1.08883);

            vec3 v = xyz / refWhite;

            // f(t) function
            vec3 f = mix(
                (7.787 * v) + (16.0/116.0),
                pow(v, vec3(1.0/3.0)),
                step(0.008856, v)
            );

            float L = (116.0 * f.y) - 16.0;
            float a = 500.0 * (f.x - f.y);
            float b = 200.0 * (f.y - f.z);

            return vec3(L, a, b);
        }}

        vec3 rgb2lab(vec3 c)
        {{
            return xyz2lab(rgb2xyz(c));
        }}

        float deltaE(vec3 rgb1, vec3 rgb2)
        {{
            vec3 lab1 = rgb2lab(rgb1);
            vec3 lab2 = rgb2lab(rgb2);
            return length(lab1 - lab2); // Euclidean distance
        }}

        //float colorDiff(vec3 rgb1, vec3 rgb2)
        float colorDiff(vec3 A, vec3 B)
        {{
            //return clamp(deltaE(rgb1, rgb2) / 40.0, 0.0, 1.0);
            //return log(max(1.0, 0.33 * deltaE(rgb1, rgb2))) / log(10.0);
            //return log(max(1.0, 2.0 * deltaE(rgb1, rgb2))) / log(10.0);
            //float normDist = deltaE(rgb1, rgb2);
            //return clamp(deltaE(rgb1, rgb2) / 28.0, 0.0, 1.0);

            float r = 0.5 * (A.r + B.r);
            vec3 d = A - B;
            vec3 c = vec3(2. + r, 4., 3. - r);

            return sqrt(dot(c*d, d)) / 3.;
        }}

        #define diff colorDiff
        
        void main()
        {{
            {DefaultFragmentOutColorName} = getColor(varTexCoord);
            return;
            vec4 color;
            float a;
            
            vec2 sub = fract(varTexCoord * sourceSize);
            vec2 outPixelSize = 1.0f / {DefaultResolutionName};
            vec4 UL = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y-pixelSize.y));
            vec4 UP = getColor(vec2(varTexCoord.x, varTexCoord.y-pixelSize.y));
            vec4 UR = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y-pixelSize.y));
            vec4 LE = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y));
            vec4 CE = getColor(varTexCoord);
            vec4 RI = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y));
            vec4 LL = getColor(vec2(varTexCoord.x-pixelSize.x, varTexCoord.y+pixelSize.y));
            vec4 DO = getColor(vec2(varTexCoord.x, varTexCoord.y+pixelSize.y));
            vec4 LR = getColor(vec2(varTexCoord.x+pixelSize.x, varTexCoord.y+pixelSize.y));

            float f = 1.0;
            float dUL = diff(CE.rgb, UL.rgb) * f;
            float dUP = diff(CE.rgb, UP.rgb) * f;
            float dUR = diff(CE.rgb, UR.rgb) * f;
            float dLE = diff(CE.rgb, LE.rgb) * f;
            float dRI = diff(CE.rgb, RI.rgb) * f;
            float dLL = diff(CE.rgb, LL.rgb) * f;
            float dDO = diff(CE.rgb, DO.rgb) * f;
            float dLR = diff(CE.rgb, LR.rgb) * f;

            const float cornerF = 1.15;//4.75;
            const float sideF = 1.0;//4.5;
            float minBlend = 0.5;
            vec3 result = CE.rgb;

            float subDist = sqrt(sub.x * sub.x + sub.y * sub.y) / sqrt(2.0);
            //minBlend += clamp(0.5 - 0.5 * subDist, 0.0, 0.5);
            minBlend = clamp(1.0 - subDist, 0.0, 1.0);

            // Mix edge neighbors
            result = mix(UP.rgb, result, clamp(dUP * sideF, minBlend, 1.0));
            result = mix(LE.rgb, result, clamp(dLE * sideF, minBlend, 1.0));
            result = mix(RI.rgb, result, clamp(dRI * sideF, minBlend, 1.0));
            result = mix(DO.rgb, result, clamp(dDO * sideF, minBlend, 1.0));
                
            // Mix corner neighbors
            result = mix(UL.rgb, result, clamp(dUL * cornerF, minBlend, 1.0));
            result = mix(UR.rgb, result, clamp(dUR * cornerF, minBlend, 1.0));
            result = mix(LL.rgb, result, clamp(dLL * cornerF, minBlend, 1.0));
            result = mix(LR.rgb, result, clamp(dLR * cornerF, minBlend, 1.0));

            //result = clamp(result * 31.0f / 32.0f, vec3(0.0), vec3(1.0));
                
            color = vec4(result, 1.0);
                
            // TODO
            color = getColor(varTexCoord);
            
            float twoDim = a;
            if ({DefaultSecondaryModeName} > 0.5f && {DefaultSecondaryModeName} < 2.5f && mod(round(gl_FragCoord.y - 0.5f), 2.0f) > 0.5f)
            {{
                vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(-0.035f);
                if (twoDim < 0.5f) color.a = 0.125f;
                else color.rgb += add;
            }}
            else if ({DefaultSecondaryModeName} > 1.5f && {DefaultSecondaryModeName} < 2.5f && mod(round(gl_FragCoord.x - 0.5f), 2.0f) > 0.5f)
            {{
                vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(-0.035f);
                if (twoDim < 0.5f) color.a = 0.125f;
                else color.rgb += add;
            }}
            else if ({DefaultSecondaryModeName} > 1.5f && {DefaultSecondaryModeName} < 2.5f)
            {{
                if (twoDim > 0.5f)
                    color.rgb += gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);
            }}
            else if ({DefaultSecondaryModeName} > 0.5f && {DefaultSecondaryModeName} < 1.5f)
            {{
                if (twoDim > 0.5f)
                    color.rgb += gray(color) < 0.025f ? vec3(0.0f) : vec3(0.035f);
            }}
            if ({DefaultSecondaryModeName} > 2.5f && {DefaultSecondaryModeName} < 3.5f)
            {{
                const float b = 1.0f;
                const float d = 0.9f;
                vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);
                float m = mod(round(gl_FragCoord.y - 0.5f), 5.0f);
                if (m < 0.5f)
                {{
                    color.rgb *= vec3(b, d, d);
                    color.rgb += add;
                    if (twoDim < 0.5f) {{ color.a = 0.15f; }}
                    else {{ color.a *= 0.875f; }}
                }}
                else if (m < 1.5f)
                {{
                    color.rgb *= vec3(d, b, d);
                    color.rgb += add;
                    if (twoDim < 0.5f) {{ color.a = 0.15f; }}
                    else {{ color.a *= 0.875f; }}
                }}
                else if (m < 2.5f)
                {{
                    color.rgb *= vec3(d, d, b);
                    color.rgb += add;
                    if (twoDim < 0.5f) {{ color.a = 0.15f; }}
                    else {{ color.a *= 0.875f; }}
                }}
                else if (m < 3.5f)
                {{
                    if (twoDim < 0.5f) {{ color.a = 0.15f; }}
                    else {{ color.a *= 0.875f; color.rgb *= vec3(0.75f, 0.75f, 0.75f); }}
                }}
            }}
            if ({DefaultSecondaryModeName} > 3.5f && {DefaultSecondaryModeName} < 4.5f)
            {{
                float col = mod(round(gl_FragCoord.x - 0.5f), 6.0f);
                float row = mod(round(gl_FragCoord.y - 0.5f), 4.0f);
                float light = 0.1f + mod(round(gl_FragCoord.x - 0.5f), 12.0f) / 96.0f;
                if ((col < 0.5f && row < 2.5f) || (col > 2.5f && col < 3.5f && !(row > 0.5f && row < 1.5f)))
                {{
                    // magenta
                    if (twoDim < 0.5f) {{ color = vec4(0.8f, 0.0f, 0.8f, 0.15f); }}
                    else color.rgb = mix(color.rgb, vec3(1.0f, 0.0f, 1.0f), light);
                }}
                else if ((col > 0.5f && col < 1.5f && row < 2.5f) || (col > 3.5f && col < 4.5f && !(row > 0.5f && row < 1.5f)))
                {{
                    // lime
                    if (twoDim < 0.5f) {{ color = vec4(0.0f, 0.9f, 0.0f, 0.15f); }}
                    else color.rgb = mix(color.rgb, vec3(0.0f, 1.0f, 0.0f), light);
                }}
                else
                {{
                    if (twoDim < 0.5f) {{ color.a = 0.15f; }}
                    else color.rgb = mix(color.rgb, vec3(0.0f, 0.0f, 0.0f), light);
                }}
                vec3 add = gray(color) < 0.025f ? vec3(0.0f) : vec3(0.075f);
                color.rgb += light * add;
            }}
            
            {DefaultFragmentOutColorName} = color;
        }}
        ",
        GetFragmentShaderHeader(state) + $@"
        uniform sampler2D {DefaultSamplerName};
        uniform vec2 {DefaultResolutionName};
        uniform float {DefaultPrimaryModeName};
        uniform float {DefaultSecondaryModeName};
        in vec2 varTexCoord;
        const vec2 sourceSize = vec2(320.0f, 200.0f);
        const vec2 pixelSize = vec2(1.0f / 320.0f, 1.0f / 200.0f);

        vec4 getColor(vec2 coord)
        {{
            return textureLod({DefaultSamplerName}, clamp(coord, vec2(0.0f), vec2(1.0f)), 0.0f);
        }}

        void main()
        {{
            vec2 pixelSize = 1.0f / {DefaultResolutionName};
            vec2 texCoord = varTexCoord;// - 0.5f * pixelSize;
            vec4 color = getColor(texCoord);
            {DefaultFragmentOutColorName} = vec4(1.0, 0, 0, 1.0);//color * vec4(1.5, 1.0, 1.0, 1.0);
        }}
        "
    ];

    static string[] ScreenVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 varTexCoord;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    float u = pos.x > 0.5 ? 1.0 : 0.0;",
        $"    float v = pos.y > 0.5 ? 1.0 : 0.0;",
        $"    varTexCoord = vec2(u, 1.0 - v);",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 0.001, 1.0);",
        $"}}"
    ];

    public void Use(Matrix4 projectionMatrix, Matrix4 lastProjectionMatrix)
    {
        for (int i = 0; i < additionalPassesShaderPrograms.Count; i++)
        {
            var projMatrix = i == additionalPassesShaderPrograms.Count - 1 ? lastProjectionMatrix : projectionMatrix;
            var shaderProgram = additionalPassesShaderPrograms[i];

            shaderProgram.Use();
            shaderProgram.SetInputMatrix(DefaultModelViewMatrixName, Matrix4.Identity.ToArray(), true);
            shaderProgram.SetInputMatrix(DefaultProjectionMatrixName, projMatrix.ToArray(), true);
        }

        base.Use(projectionMatrix);
    }

    public void BindFirstFrameBuffer(int width, int height)
    {
        additionalFrameBuffers[0].Bind(width, height);
    }

    private void ProcessPass(State state, int pass, Action render, Action targetBufferSetup)
    {
        targetBufferSetup();

        var frameBuffer = additionalFrameBuffers[pass];
        var shaderProgram = additionalPassesShaderPrograms[pass];

        shaderProgram.Use();

        state.Gl.ActiveTexture(GLEnum.Texture1);
        frameBuffer.BindAsTexture();

        shaderProgram.SetInput(DefaultPrevPassSamplerName, 1);

        render();
    }

    public void ProcessPasses(State state, int width, int height, Action render, Action lastTargetBufferSetup)
    {
        for (int i = 0; i < additionalPassesShaderPrograms.Count; i++)
        {
            ProcessPass(state, i, render, i == additionalPassesShaderPrograms.Count - 1
                ? lastTargetBufferSetup
                : () => additionalFrameBuffers[i + 1].Bind(width, height));
        }
    }

    MultiPassScreenShader(State state)
        : this(state, ScreenFragmentShaders(state), ScreenVertexShader(state))
    {

    }

    private MultiPassScreenShader(State state, string[] fragmentShaderCodes, string[] vertexShaderLines)
    {
        var fragmentShaders = fragmentShaderCodes.Select(code => new Shader(state, Shader.Type.Fragment, code)).ToArray();
        var vertexShader = new Shader(state, Shader.Type.Vertex, string.Join("\n", vertexShaderLines));

        shaderProgram = new ShaderProgram(state, fragmentShaders[0], vertexShader);

        for (int i = 1; i < fragmentShaderCodes.Length; i++)
        {
            additionalPassesShaderPrograms.Add(new ShaderProgram(state, fragmentShaders[i], vertexShader));
            additionalFrameBuffers.Add(new FrameBuffer(state));
        }
    }

    public override void SetSampler(int textureUnit = 0)
    {
        base.SetSampler(0);

        foreach (var shaderProgram in additionalPassesShaderPrograms)
        {
            shaderProgram.Use();
            shaderProgram.SetInput(DefaultSamplerName, textureUnit);
        }

        shaderProgram.Use();
    }

    public override void SetResolution(Size resolution)
    {
        var virtualScreenSize = new Size(320, 200);

        base.SetResolution(virtualScreenSize);

        for (int i = 0; i < additionalPassesShaderPrograms.Count; i++)
        {
            var size = i == additionalPassesShaderPrograms.Count - 1 ? resolution : virtualScreenSize;
            var shaderProgram = additionalPassesShaderPrograms[i];

            shaderProgram.Use();
            shaderProgram.SetInputVector2(DefaultResolutionName, (float)size.Width, (float)size.Height);
        }

        shaderProgram.Use();
    }

    /// <summary>
    /// Changes the filter modes.
    /// </summary>
    /// <param name="primary">0: No filter, 1: Blurry filter (old), 2: New filter</param>
    /// <param name="secondary">0: No addition, 1: Vertical lines, 2: Grid, 3: Scan lines</param>
    public override void SetMode(int primary, int secondary)
    {
        base.SetMode(primary, secondary);

        foreach (var shaderProgram in additionalPassesShaderPrograms)
        {
            shaderProgram.Use();
            shaderProgram.SetInput(DefaultPrimaryModeName, (float)primary);
            shaderProgram.SetInput(DefaultSecondaryModeName, (float)secondary);
        }

        shaderProgram.Use();
    }

    public static new MultiPassScreenShader Create(State state) => new(state);
}
*/