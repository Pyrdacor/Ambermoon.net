using Ambermoon.Render;
using System;
using System.Collections.Generic;

namespace Ambermoon
{
    internal class Intro : Video
    {
        public Intro(IRenderView renderView)
            : base(renderView, Layer.Intro)
        {

        }
    }
}
