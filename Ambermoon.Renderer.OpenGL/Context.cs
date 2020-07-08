/*
 * Context.cs - Render context which is capable of rotating the whole screen
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
        State State { get; }

        public Context(State state, int width, int height)
        {
            State = state;

            // We need at least OpenGL 3.1 for instancing and shaders
            if (State.OpenGLVersionMajor < 3 || (State.OpenGLVersionMajor == 3 && State.OpenGLVersionMinor < 1))
                throw new Exception($"OpenGL version 3.1 is required for rendering. Your version is {State.OpenGLVersionMajor}.{State.OpenGLVersionMinor}.");

            State.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            State.Gl.Enable(EnableCap.DepthTest);
            State.Gl.DepthFunc(DepthFunction.Lequal);

            State.Gl.Enable(EnableCap.Blend);
            State.Gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.FuncAdd);
            State.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);

            Resize(width, height);
        }

        public void Resize(int width, int height)
        {
            State.ProjectionMatrix2D = Matrix4.CreateOrtho2D(0, width, 0, height, 0, 1);
            // TODO: 9.0f cause 10 is distance per tile and -1 is enough as there can not be any surface in last row
            // TODO: 500.0f cause max map height should be 50 (50 * 10 = 500)
            State.ProjectionMatrix3D = Matrix4.CreatePerspective(60.0f, (float)Global.MapViewWidth / Global.MapViewHeight, 9.0f, 500.0f);

            State.ClearMatrices();
            State.PushModelViewMatrix(Matrix4.Identity);
            State.PushProjectionMatrix(State.ProjectionMatrix2D);

            this.width = width;
            this.height = height;

            SetRotation(rotation, true);
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
                        Matrix4.CreateRotationMatrix(rotationDegree) *
                        Matrix4.CreateScalingMatrix(factor, 1.0f / factor) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
                else // 180°
                {
                    modelViewMatrix =
                        Matrix4.CreateTranslationMatrix(x, y) *
                        Matrix4.CreateRotationMatrix(rotationDegree) *
                        Matrix4.CreateTranslationMatrix(-x, -y);
                }
            }

            State.PushModelViewMatrix(modelViewMatrix);
        }
    }
}
