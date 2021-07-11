using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ambermoon.Renderer.OpenGL
{
    public class FrameBuffer// : IDisposable
    {
        uint index;
        uint depthBuffer;
        uint renderTexture;
        readonly State state;

        public FrameBuffer(State state)
        {
            this.state = state;
            index = state.Gl.GenFramebuffer();
            depthBuffer = state.Gl.GenRenderbuffer();
            renderTexture = state.Gl.GenTexture();

            var gl = state.Gl;

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, index);

            gl.BindTexture(GLEnum.Texture2D, renderTexture);
            gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba8, Global.VirtualScreenWidth,
                Global.VirtualScreenHeight, 0, GLEnum.Rgba, GLEnum.UnsignedByte, IntPtr.Zero);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            gl.BindTexture(GLEnum.Texture2D, 0);

            gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, renderTexture, 0);

            gl.BindRenderbuffer(GLEnum.Renderbuffer, depthBuffer);
            gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.Depth24Stencil8, Global.VirtualScreenWidth,
                Global.VirtualScreenHeight);
            gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

            gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, depthBuffer);

            if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
                throw new AmbermoonException(ExceptionScope.Render, "Unable to setup framebuffer");

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind()
        {
            state.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, index);
        }
    }
}
