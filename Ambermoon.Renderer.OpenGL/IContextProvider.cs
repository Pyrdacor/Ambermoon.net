using Silk.NET.Core.Contexts;

namespace Ambermoon.Renderer.OpenGL;

public interface IContextProvider : IGLContextSource
{
    public string Identifier { get; }
}
