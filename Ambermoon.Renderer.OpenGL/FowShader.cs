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

namespace Ambermoon.Renderer
{
    internal class FowShader : ColorShader
    {
        internal static readonly string DefaultCenterName = "center";
        internal static readonly string DefaultRadiusName = "radius";

        protected static string[] FowFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"in float alpha;",
            $"",
            $"void main()",
            $"{{",
            $"    {DefaultFragmentOutColorName} = vec4(0, 0, 0, alpha);",
            $"}}"
        };

        protected static string[] FowVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in ivec2 {DefaultPositionName};",
            $"in ivec2 {DefaultCenterName};",
            $"in uint {DefaultRadiusName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out float alpha;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x), float({DefaultPositionName}.y));",
            $"    vec2 dist = abs(pos - vec2(float({DefaultCenterName}.x), float({DefaultCenterName}.y)));",
            $"    float d = sqrt(dist.x * dist.x + dist.y * dist.y);",
            $"    alpha = d < radius ? 0 : 1;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos + vec2(0.49f, 0.49f), 1.0f - {DefaultZName}, 1.0f);",
            $"}}"
        };

        FowShader(State state)
            : this(state, FowFragmentShader(state), FowVertexShader(state))
        {

        }

        protected FowShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, fragmentShaderLines, vertexShaderLines)
        {

        }

        public new static FowShader Create(State state) => new FowShader(state);
    }
}
