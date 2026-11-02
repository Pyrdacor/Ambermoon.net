/*
 * FullscreenImage.cs - Non-palette image which is displayed instead of the game
 *
 * Copyright (C) 2020-2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using System;

namespace Ambermoon.Renderer.OpenGL;

internal sealed class FullscreenImage : IDisposable
{
    sealed class ImageTexture : Texture
    {
        public unsafe ImageTexture(State state, int width, int height, byte[] rgbaData)
            : base(state, width, height)
        {
            Bind();

            // the image is scaled freely so it needs smooth filtering in contrast to the game graphics
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            state.Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            fixed (byte* ptr = rgbaData)
            {
                state.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, ptr);
            }

            state.Gl.GenerateMipmap(GLEnum.Texture2D);
        }
    }

    const string PositionName = "position";
    const string AreaName = "area";
    const string SamplerName = "sampler";

    readonly State state;
    readonly ImageTexture texture;
    readonly ShaderProgram shaderProgram;
    readonly VertexArrayObject vertexArrayObject;
    readonly FloatPositionBuffer positionBuffer;
    readonly IndexBuffer indexBuffer;
    float areaX = 0.0f;
    float areaY = 0.0f;
    float areaWidth = 1.0f;
    float areaHeight = 1.0f;
    bool disposed = false;

    static string Version(State state)
    {
#if GLES
        return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor:00} es\n\n";
#else
        return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n\n";
#endif
    }

    static string FragmentShader(State state) => Version(state) + string.Join("\n",
    [
        "#ifdef GL_ES",
        " precision highp float;",
        " precision highp int;",
        "#endif",
        "",
        "out vec4 outColor;",
        $"uniform sampler2D {SamplerName};",
        "in vec2 varTexCoord;",
        "",
        "void main()",
        "{",
        $"    outColor = texture({SamplerName}, varTexCoord);",
        "}"
    ]);

    static string VertexShader(State state) => Version(state) + string.Join("\n",
    [
        $"in vec2 {PositionName};",
        $"uniform vec4 {AreaName};", // x, y, width, height relative to the window with the origin at the top-left
        "out vec2 varTexCoord;",
        "",
        "void main()",
        "{",
        $"    vec2 pos = {AreaName}.xy + {PositionName} * {AreaName}.zw;",
        $"    varTexCoord = {PositionName};",
        "    gl_Position = vec4(pos.x * 2.0f - 1.0f, 1.0f - pos.y * 2.0f, 0.0f, 1.0f);",
        "}"
    ]);

    public FullscreenImage(State state, int width, int height, byte[] rgbaData)
    {
        this.state = state;
        texture = new ImageTexture(state, width, height, rgbaData);
        var fragmentShader = new Shader(state, Shader.Type.Fragment, FragmentShader(state));
        var vertexShader = new Shader(state, Shader.Type.Vertex, VertexShader(state));
        shaderProgram = new ShaderProgram(state, fragmentShader, vertexShader);
        vertexArrayObject = new VertexArrayObject(state, shaderProgram);
        positionBuffer = new FloatPositionBuffer(state, true);
        positionBuffer.Add(0, 0, 0);
        positionBuffer.Add(1, 0, 1);
        positionBuffer.Add(1, 1, 2);
        positionBuffer.Add(0, 1, 3);
        vertexArrayObject.AddBuffer(PositionName, positionBuffer);
        indexBuffer = new IndexBuffer(state);
        indexBuffer.InsertQuad(0);
        vertexArrayObject.AddBuffer("index", indexBuffer);
    }

    public void SetArea(float x, float y, float width, float height)
    {
        areaX = x;
        areaY = y;
        areaWidth = width;
        areaHeight = height;
    }

    public void Render()
    {
        if (disposed)
            return;

        state.Gl.Disable(EnableCap.Blend);
        state.Gl.Disable(EnableCap.DepthTest);

        shaderProgram.Use();
        shaderProgram.SetInputVector4(AreaName, areaX, areaY, areaWidth, areaHeight);
        shaderProgram.SetInput(SamplerName, 0);
        state.Gl.ActiveTexture(GLEnum.Texture0);
        texture.Bind();
        vertexArrayObject.Bind();

        unsafe
        {
            vertexArrayObject.Lock();

            try
            {
                state.Gl.DrawElements(GLEnum.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
                vertexArrayObject.Unbind();
            }
            finally
            {
                vertexArrayObject.Unlock();
            }
        }

        state.Gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            texture.Unbind();
            texture.Dispose();
            vertexArrayObject.Dispose();
            positionBuffer.Dispose();
            indexBuffer.Dispose();
            shaderProgram.Dispose();

            disposed = true;
        }
    }
}
