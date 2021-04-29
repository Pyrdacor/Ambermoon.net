/*
 * Context.cs - Render context which is capable of rotating the whole screen
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

using Silk.NET.OpenGL;
using System;

namespace Ambermoon.Renderer
{
    public class Context
    {
        int width = -1;
        int height = -1;
        Rotation rotation = Rotation.None;
        Matrix4 modelViewMatrix = Matrix4.Identity;
        static readonly float FovY3D = (float)Math.PI * 0.26f;
        State State { get; }

        public Context(State state, int width, int height, float aspect)
        {
            State = state;

            // We need at least OpenGL 3.1 for instancing and shaders
            if (State.OpenGLVersionMajor < 3 || (State.OpenGLVersionMajor == 3 && State.OpenGLVersionMinor < 1))
                throw new Exception($"OpenGL version 3.1 is required for rendering. Your version is {State.OpenGLVersionMajor}.{State.OpenGLVersionMinor}.");

            State.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            State.Gl.Enable(EnableCap.DepthTest);
            State.Gl.DepthRange(0.0f, 1.0f);
            State.Gl.DepthFunc(DepthFunction.Lequal);
            State.Gl.Disable(EnableCap.CullFace);

            State.Gl.Disable(EnableCap.Blend);
            State.Gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.FuncAdd);
            State.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);

            Resize(width, height, aspect);
        }

        public void Resize(int width, int height, float aspect)
        {
            State.ProjectionMatrix2D = Matrix4.CreateOrtho2D(0, Global.VirtualScreenWidth, 0, Global.VirtualScreenHeight, 0, 1);
            State.ProjectionMatrix3D = Matrix4.CreatePerspective(FovY3D, aspect, 0.1f, 40.0f * Global.DistancePerBlock); // Max 3D map dimension is 41

            State.ClearMatrices();
            State.PushModelViewMatrix(Matrix4.Identity);
            State.PushProjectionMatrix(State.ProjectionMatrix2D);

            this.width = width;
            this.height = height;

            SetRotation(rotation, true);
        }

        public void UpdateAspect(float aspect)
        {
            Resize(width, height, aspect);
        }

        public void SetRotation(Rotation rotation, bool forceUpdate = false)
        {
            if (forceUpdate || rotation != this.rotation)
            {
                this.rotation = rotation;

                ApplyMatrix();
            }
        }

        void ApplyMatrix()
        {
            State.RestoreModelViewMatrix(modelViewMatrix);
            State.PopModelViewMatrix();

            if (rotation == Rotation.None)
            {
                modelViewMatrix = Matrix4.Identity;
            }
            else
            {
                var rotationDegree = 0.0f;

                switch (rotation)
                {
                    case Rotation.Deg90:
                        rotationDegree = 90.0f;
                        break;
                    case Rotation.Deg180:
                        rotationDegree = 180.0f;
                        break;
                    case Rotation.Deg270:
                        rotationDegree = 270.0f;
                        break;
                    default:
                        break;
                }

                var x = 0.5f * width;
                var y = 0.5f * height;

                if (rotation != Rotation.Deg180) // 90° or 270°
                {
                    float factor = (float)height / (float)width;

                    modelViewMatrix =
                        Matrix4.CreateTranslationMatrix(x, y) *
                        Matrix4.CreateYRotationMatrix(rotationDegree) *
                        Matrix4.CreateScalingMatrix(factor, 1.0f / factor) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
                else // 180°
                {
                    modelViewMatrix =
                        Matrix4.CreateTranslationMatrix(x, y) *
                        Matrix4.CreateYRotationMatrix(rotationDegree) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
            }

            State.PushModelViewMatrix(modelViewMatrix);
        }
    }
}
