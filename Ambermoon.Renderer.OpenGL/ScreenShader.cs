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

        internal ShaderProgram shaderProgram;

        protected static string GetFragmentShaderHeader(State state)
        {
            string header = $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n";

            header += "\n";
            header += "#ifdef GL_ES\n";
            header += " precision mediump float;\n";
            header += " precision highp int;\n";
            header += "#endif\n";
            header += "\n";
            header += $"out vec4 {DefaultFragmentOutColorName};\n";

            return header;
        }

        protected static string GetVertexShaderHeader(State state)
        {
            return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n\n";
        }

        static string[] ScreenFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"in vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    {DefaultFragmentOutColorName} = texture({DefaultSamplerName}, varTexCoord);",
            $"}}"
        };

        static string[] ScreenVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = vec2(pos.x / 320.0f, (200.0f - pos.y) / 200.0f);",
            $"    ",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f, 1.0f);",
            $"}}"
        };

        public void Use(State state)
        {
            if (shaderProgram != ShaderProgram.ActiveProgram)
                shaderProgram.Use();

            shaderProgram.SetInputMatrix(DefaultModelViewMatrixName, Matrix4.Identity.ToArray(), true);
            shaderProgram.SetInputMatrix(DefaultProjectionMatrixName, state.ProjectionMatrix2D.ToArray(), true);
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

        public static ScreenShader Create(State state) => new ScreenShader(state);
    }
}
