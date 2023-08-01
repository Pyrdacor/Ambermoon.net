namespace Ambermoon.Renderer.OpenGL
{
    public struct LayerConfig
    {
        public LayerConfig()
        {
            Layered = true;
            Opaque = false;
            EnableBlending = false;
            UsePalette = true;
            SupportTextures = true;
            SupportColoredRects = false;
            SupportAnimations = true;
            RenderToVirtualScreen = true;
            TextureFactor = 1;
            BaseZ = 0.0f;
            Use320x256 = false;
            SupportPaletteFading = false;
        }

        /// <summary>
        /// If true there is a DisplayLayer which can be used to control
        /// the render order. If false the Z coordinate of the vertex
        /// or the y-coordinate of the drawn tile controls the render order.
        /// </summary>
        public bool Layered { get; set; }
        /// <summary>
        /// If true, the shader won't support any transparency. Each pixel
        /// will have a fully opaque color. Otherwise pixels may be discarded
        /// so the background shines through. Opaque layers are in general
        /// much faster.
        /// </summary>
        public bool Opaque { get; set; }
        /// <summary>
        /// This flag controls if the layer supports blending. In general
        /// it uses simple alpha blending: src * srcA + dst * (1 - srcA).
        /// </summary>
        public bool EnableBlending { get; set; }
        /// <summary>
        /// If true the graphics are expected to be indexed and contain
        /// indices into a 32 color palette. The palette index of the
        /// render node then specifies which palette to use.
        /// If false the graphics are expected to contain rgba pixel
        /// data and will be rendered directly while ignoring the palette
        /// index.
        /// </summary>
        public bool UsePalette { get; set; }
        /// <summary>
        /// If true, the layer supports textures, otherwise not.
        /// </summary>
        public bool SupportTextures { get; set; }
        /// <summary>
        /// If true, the layer supports colored rectangles,
        /// otherwise not.
        /// </summary>
        public bool SupportColoredRects { get; set; }
        /// <summary>
        /// If true, the layer supports animations. This has not much
        /// effect but will create the texture coord buffer as static
        /// as it is expected that those coords don't change much.
        /// </summary>
        public bool SupportAnimations { get; set; }
        /// <summary>
        /// If true, all render node positions and sizes are treated
        /// as relative to a virtual screen of 320x200. This eases
        /// positioning of objects in relation to the original game.
        /// If false, the layer will use a projection matrix which
        /// directly renders to the full framebuffer dimensions instead.
        /// </summary>
        public bool RenderToVirtualScreen { get; set; }
        /// <summary>
        /// Texture atlas positions and sizes are multiplied by this
        /// value before they are passed as texture coordinates to the
        /// layer's shader. This allows to use game size related
        /// dimensions but still use higher resolution textures.
        /// </summary>
        public uint TextureFactor { get; set; }
        /// <summary>
        /// Basic Z coordinate for all vertices on the layer. It is
        /// used to group render objects dependent on the usage.
        /// For example all UI elements have a higher base Z value
        /// so that they will always be drawn on top of map tiles etc.
        /// 3D layers should use a value of 0.0f here.
        /// </summary>
        public float BaseZ { get; set; }
        /// <summary>
        /// Uses a slightly bigger resolution of 320x256 instead
        /// of 320x200. This will be mapped to 409,6 x 256 which
        /// has the same ratio as 320x200. Positions are then
        /// in the range 320x256 and a black border is shown at
        /// left and right. Used for the intros mainly.
        /// </summary>
        public bool Use320x256 { get; set;  }
        /// <summary>
        /// Used by the main menu layer (actually the intro end
        /// screen) to fade the background image from yellowish
        /// to normal.
        /// </summary>
        public bool SupportPaletteFading { get; set; }
    }
}
