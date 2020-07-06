/*
 * ISprite.cs - Sprite interface
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

namespace Ambermoon.Render
{
    public interface ISprite : IRenderNode
    {
        Position TextureAtlasOffset
        {
            get;
            set;
        }

        int BaseLineOffset
        {
            get;
            set;
        }

        byte PaletteIndex
        {
            get;
            set;
        }
    }

    public interface IMaskedSprite : ISprite
    {
        Position MaskTextureAtlasOffset
        {
            get;
            set;
        }
    }

    public interface ILayerSprite : ISprite
    {
        byte DisplayLayer
        {
            get;
            set;
        }
    }

    public interface IMaskedLayerSprite : IMaskedSprite, ILayerSprite
    {

    }

    public interface IAnimatedSprite : ISprite
    {
        uint NumFrames
        {
            get;
            set;
        }

        uint CurrentFrame
        {
            get;
            set;
        }

        int TextureAtlasWidth
        {
            get;
            set;
        }
    }

    public interface ISpriteFactory
    {
        ISprite Create(int width, int height, int textureAtlasX, int textureAtlasY, bool masked, bool layered, byte displayLayer = 0);
        IAnimatedSprite CreateAnimated(int width, int height, int textureAtlasX, int textureAtlasY, int textureAtlasWidth, uint numFrames = 1);
    }
}
