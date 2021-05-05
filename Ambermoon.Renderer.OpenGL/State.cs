/*
 * State.cs - OpenGL state
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

using Ambermoon.Renderer.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ambermoon.Renderer
{
    public class State : IEquatable<State>
    {
        public readonly int OpenGLVersionMajor = 0;
        public readonly int OpenGLVersionMinor = 0;
        public readonly int GLSLVersionMajor = 0;
        public readonly int GLSLVersionMinor = 0;
        public readonly GL Gl = null;
        readonly string contextIdentifier;

        public State(IContextProvider contextProvider)
        {
            contextIdentifier = contextProvider.Identifier;

            Gl = GL.GetApi(contextProvider);

            var openGLVersion = Gl.GetStringS(StringName.Version).TrimStart();

            Regex versionRegex = new Regex(@"([0-9]+)\.([0-9]+)", RegexOptions.Compiled);

            var match = versionRegex.Match(openGLVersion);

            if (!match.Success || match.Index != 0 || match.Groups.Count < 3)
            {
                throw new Exception("OpenGL is not supported or the version could not be determined.");
            }

            OpenGLVersionMajor = int.Parse(match.Groups[1].Value);
            OpenGLVersionMinor = int.Parse(match.Groups[2].Value);

            if (OpenGLVersionMajor >= 2) // glsl is supported since OpenGL 2.0
            {
                var glslVersion = Gl.GetStringS(StringName.ShadingLanguageVersion);

                match = versionRegex.Match(glslVersion);

                if (match.Success && match.Index == 0 && match.Groups.Count >= 3)
                {
                    GLSLVersionMajor = int.Parse(match.Groups[1].Value);
                    GLSLVersionMinor = int.Parse(match.Groups[2].Value);
                }
            }
        }

        readonly Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        readonly Stack<Matrix4> modelViewMatrixStack = new Stack<Matrix4>();
        public Matrix4 ProjectionMatrix2D { get; set; } = Matrix4.Identity;
        public Matrix4 ProjectionMatrix3D { get; set; } = Matrix4.Identity;

        public void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(matrix);
        }

        public void PushModelViewMatrix(Matrix4 matrix)
        {
            modelViewMatrixStack.Push(matrix);
        }

        public Matrix4 PopProjectionMatrix()
        {
            return projectionMatrixStack.Pop();
        }

        public Matrix4 PopModelViewMatrix()
        {
            return modelViewMatrixStack.Pop();
        }

        public void RestoreProjectionMatrix(Matrix4 matrix)
        {
            if (projectionMatrixStack.Contains(matrix))
            {
                while (CurrentProjectionMatrix != matrix)
                    projectionMatrixStack.Pop();
            }
            else
                PushProjectionMatrix(matrix);
        }

        public void RestoreModelViewMatrix(Matrix4 matrix)
        {
            if (modelViewMatrixStack.Contains(matrix))
            {
                while (CurrentModelViewMatrix != matrix)
                    modelViewMatrixStack.Pop();
            }
            else
                PushModelViewMatrix(matrix);
        }

        public void ClearMatrices()
        {
            projectionMatrixStack.Clear();
            modelViewMatrixStack.Clear();
        }

        public bool Equals(State other)
        {
            if (other == null)
                return false;

            return contextIdentifier == other.contextIdentifier &&
                OpenGLVersionMajor == other.OpenGLVersionMajor &&
                OpenGLVersionMinor == other.OpenGLVersionMinor &&
                GLSLVersionMajor == other.GLSLVersionMajor &&
                GLSLVersionMinor == other.GLSLVersionMinor;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is State state))
                return false;

            return Equals(state);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(contextIdentifier,
                OpenGLVersionMajor, OpenGLVersionMinor,
                GLSLVersionMajor, GLSLVersionMinor);
        }

        public Matrix4 CurrentProjectionMatrix => (projectionMatrixStack.Count == 0) ? null : projectionMatrixStack.Peek();
        public Matrix4 CurrentModelViewMatrix => (modelViewMatrixStack.Count == 0) ? null : modelViewMatrixStack.Peek();
    }
}
