/*
 * ColorShader.cs - Basic color shader for colored shapes
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
    internal class ColorShader
    {
        internal static readonly string DefaultFragmentOutColorName = "outColor";
        internal static readonly string DefaultPositionName = "position";
        internal static readonly string DefaultModelViewMatrixName = "mvMat";
        internal static readonly string DefaultProjectionMatrixName = "projMat";
        internal static readonly string DefaultColorName = "color";
        internal static readonly string DefaultZName = "z";
        internal static readonly string DefaultLayerName = "layer";

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

        static string[] ColorFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"flat in vec4 pixelColor;",
            $"",
            $"void main()",
            $"{{",
            $"    {DefaultFragmentOutColorName} = pixelColor;",
            $"}}"
        };

        static string[] ColorVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"in uint {DefaultLayerName};",
            $"in uvec4 {DefaultColorName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"flat out vec4 pixelColor;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    pixelColor = vec4({DefaultColorName}.r / 255.0f, {DefaultColorName}.g / 255.0f, {DefaultColorName}.b / 255.0f, {DefaultColorName}.a / 255.0f);",
            $"    ",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        public void UpdateMatrices(State state)
        {
            shaderProgram.SetInputMatrix(DefaultModelViewMatrixName, state.CurrentModelViewMatrix.ToArray(), true);
            shaderProgram.SetInputMatrix(DefaultProjectionMatrixName, state.CurrentProjectionMatrix.ToArray(), true);
        }

        public void Use()
        {
            if (shaderProgram != ShaderProgram.ActiveProgram)
                shaderProgram.Use();
        }

        ColorShader(State state)
            : this(state, ColorFragmentShader(state), ColorVertexShader(state))
        {

        }

        protected ColorShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
        {
            var fragmentShader = new Shader(state, Shader.Type.Fragment, string.Join("\n", fragmentShaderLines));
            var vertexShader = new Shader(state, Shader.Type.Vertex, string.Join("\n", vertexShaderLines));

            shaderProgram = new ShaderProgram(state, fragmentShader, vertexShader);
        }

        public ShaderProgram ShaderProgram => shaderProgram;

        public void SetZ(float z)
        {
            shaderProgram.SetInput(DefaultZName, z);
        }

        public static ColorShader Create(State state) => new ColorShader(state);
    }
}
