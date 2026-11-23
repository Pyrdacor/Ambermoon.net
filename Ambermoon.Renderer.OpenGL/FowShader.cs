/*
 * FOWShader.cs - Shader for fog of war in 2D
 *
 * Copyright (C) 2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

internal class FowShader : ColorShader
{
    internal static readonly string DefaultCenterName = "center";
    internal static readonly string DefaultRadiusName = "radius";
    internal static readonly string DefaultRealisticEnabledName = "rlEnabled";
    internal static readonly string DefaultRealisticTintName = "rlTint";
    internal static readonly string DefaultRealisticLightPosName = "rlLightPos";
    internal static readonly string DefaultRealisticLightColorName = "rlLightColor";
    internal static readonly string DefaultRealisticLightCountName = "rlLightCount";
    internal const int MaxRealisticLights = 24;

    protected static string[] FowFragmentShader(State state) =>
    [
        GetFragmentShaderHeader(state),
        $"in vec2 currentPos;",
        $"flat in vec2 fowCenter;",
        $"flat in float fowRadius;",
        $"uniform float {DefaultRealisticEnabledName};",
        $"uniform vec4 {DefaultRealisticTintName};", // rgb: color of the darkness, a: darkness
        $"uniform vec4 {DefaultRealisticLightPosName}[{MaxRealisticLights}];", // xy: position, z: strength, w: radius
        $"uniform vec4 {DefaultRealisticLightColorName}[{MaxRealisticLights}];", // rgb: glow color
        $"uniform int {DefaultRealisticLightCountName};",
        $"",
        $"void main()",
        $"{{",
        $"    if ({DefaultRealisticEnabledName} > 0.5f)",
        $"    {{",
        $"        float lit = 0.0f;",
        $"        vec3 glow = vec3(0.0f);",
        $"        for (int i = 0; i < {MaxRealisticLights}; ++i)",
        $"        {{",
        $"            if (i >= {DefaultRealisticLightCountName})",
        $"                break;",
        $"            vec4 light = {DefaultRealisticLightPosName}[i];",
        $"            float a = clamp(1.0f - length(currentPos - light.xy) / max(light.w, 1.0f), 0.0f, 1.0f);",
        $"            a = a * a * (3.0f - 2.0f * a);",
        $"            lit += a * light.z;",
        $"            glow += {DefaultRealisticLightColorName}[i].rgb * a;",
        $"        }}",
        $"        float darkness = {DefaultRealisticTintName}.a * (1.0f - min(lit, 1.0f));",
        $"        // the glow is only visible in dark environments",
        $"        float g = clamp(max(glow.r, max(glow.g, glow.b)), 0.0f, 0.25f) * smoothstep(0.0f, 0.6f, {DefaultRealisticTintName}.a);",
        $"        vec3 glowColor = g > 0.0f ? min(glow / max(g, 0.0001f), vec3(1.0f)) : vec3(0.0f);",
        $"        float alpha = 1.0f - (1.0f - darkness) * (1.0f - g);",
        $"        vec3 color = alpha > 0.0f ? ({DefaultRealisticTintName}.rgb * darkness * (1.0f - g) + glowColor * g) / alpha : vec3(0.0f);",
        $"        {DefaultFragmentOutColorName} = vec4(color, alpha);",
        $"    }}",
        $"    else",
        $"    {{",
        $"        vec2 dist = abs(currentPos - fowCenter);",
        $"        float d = sqrt(dist.x * dist.x + dist.y * dist.y);",
        $"        float alpha = d < fowRadius ? 0.0f : 1.0f;",
        $"        {DefaultFragmentOutColorName} = vec4(0, 0, 0, alpha);",
        $"    }}",
        $"}}"
    ];

    protected static string[] FowVertexShader(State state) =>
    [
        GetVertexShaderHeader(state),
        $"in vec2 {DefaultPositionName};",
        $"in uint {DefaultLayerName};",
        $"in vec2 {DefaultCenterName};",
        $"in uint {DefaultRadiusName};",
        $"uniform float {DefaultZName};",
        $"uniform mat4 {DefaultProjectionMatrixName};",
        $"uniform mat4 {DefaultModelViewMatrixName};",
        $"out vec2 currentPos;",
        $"flat out vec2 fowCenter;",
        $"flat out float fowRadius;",
        $"",
        $"void main()",
        $"{{",
        $"    vec2 pos = vec2({DefaultPositionName}.x, {DefaultPositionName}.y);",
        $"    fowCenter = vec2({DefaultCenterName}.x, {DefaultCenterName}.y);",
        $"    fowRadius = float({DefaultRadiusName});",
        $"    currentPos = pos;",
        $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos + vec2(0.49f, 0.49f), 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
        $"}}"
    ];

    FowShader(State state)
        : this(state, FowFragmentShader(state), FowVertexShader(state))
    {

    }

    protected FowShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        : base(state, fragmentShaderLines, vertexShaderLines)
    {

    }

    public void DisableRealisticLighting()
    {
        shaderProgram.SetInput(DefaultRealisticEnabledName, 0.0f);
    }

    public void SetRealisticLighting(float tintR, float tintG, float tintB, float darkness,
        float[] lightPositions, float[] lightColors, int lightCount)
    {
        shaderProgram.SetInput(DefaultRealisticEnabledName, 1.0f);
        shaderProgram.SetInputVector4(DefaultRealisticTintName, tintR, tintG, tintB, darkness);
        shaderProgram.SetInput(DefaultRealisticLightCountName, lightCount);

        if (lightCount > 0)
        {
            shaderProgram.SetInputVector4Array(DefaultRealisticLightPosName, lightPositions, lightCount);
            shaderProgram.SetInputVector4Array(DefaultRealisticLightColorName, lightColors, lightCount);
        }
    }

    public new static FowShader Create(State state) => new FowShader(state);
}
