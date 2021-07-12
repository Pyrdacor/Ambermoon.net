using Silk.NET.OpenGL;
using System;

namespace Ambermoon.Renderer.OpenGL
{
    public class FrameBuffer : IDisposable
    {
        uint index;
        uint depthBuffer;
        uint renderTexture;
        bool disposed = false;
        readonly State state;
        Size size = new Size(0, 0);

        public FrameBuffer(State state)
        {
            this.state = state;
            index = state.Gl.GenFramebuffer();
            depthBuffer = state.Gl.GenRenderbuffer();
            renderTexture = state.Gl.GenTexture();
        }

        void EnsureSize(int width, int height)
        {
            if (size.Width != width || size.Height != height)
            {
                size.Width = width;
                size.Height = height;
                var gl = state.Gl;

                gl.BindFramebuffer(FramebufferTarget.Framebuffer, index);

                gl.BindTexture(GLEnum.Texture2D, renderTexture);
                unsafe
                {
                    gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
                }
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                gl.BindTexture(GLEnum.Texture2D, 0);

                gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, renderTexture, 0);

                gl.BindRenderbuffer(GLEnum.Renderbuffer, depthBuffer);
                gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.Depth24Stencil8, (uint)width, (uint)height);
                gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

                gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, depthBuffer);

                if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
                    throw new AmbermoonException(ExceptionScope.Render, "Unable to setup framebuffer");

                gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        public void Bind(int width, int height)
        {
            if (disposed)
                return;

            EnsureSize(width, height);

            state.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, index);
        }

        public void BindAsTexture()
        {
            if (!disposed)
                state.Gl.BindTexture(GLEnum.Texture2D, renderTexture);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                state.Gl.DeleteTexture(renderTexture);
                state.Gl.DeleteRenderbuffer(depthBuffer);
                state.Gl.DeleteFramebuffer(index);

                renderTexture = 0;
                depthBuffer = 0;
                index = 0;

                disposed = true;
            }
        }
    }
}
