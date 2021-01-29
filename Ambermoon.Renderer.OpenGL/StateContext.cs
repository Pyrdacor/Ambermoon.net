/*
 * StateContext.cs - OpenGL state switch helper
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

using System;

namespace Ambermoon.Renderer
{
    // TODO: this is not used. remove?
    internal class StateContext : IDisposable
    {
        readonly State state;
        bool disposed = false;
        ShaderProgram preProgram;
        VertexArrayObject preVAO;
        readonly Matrix4 preProjectionMatrix;
        readonly Matrix4 preModelViewMatrix;

        public StateContext(State state)
        {
            this.state = state;
            preProgram = ShaderProgram.ActiveProgram;
            preVAO = VertexArrayObject.ActiveVAO;
            preProjectionMatrix = state.CurrentProjectionMatrix;
            preModelViewMatrix = state.CurrentModelViewMatrix;
        }

        void Release()
        {
            if (preProgram != ShaderProgram.ActiveProgram && preProgram != null)
                preProgram.Use();
            if (preVAO != VertexArrayObject.ActiveVAO && preVAO != null)
                preVAO.Bind();
            if (preProjectionMatrix != state.CurrentProjectionMatrix && preProjectionMatrix != null)
                state.RestoreProjectionMatrix(preProjectionMatrix);
            if (preModelViewMatrix != state.CurrentModelViewMatrix && preModelViewMatrix != null)
                state.RestoreModelViewMatrix(preModelViewMatrix);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Release();

                    disposed = true;
                }
            }
        }
    }
}