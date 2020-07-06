/*
 * TextureAtlas.cs - Texture atlas creating and handling
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

using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Renderer
{
    internal class TextureAtlas : ITextureAtlas
    {
        readonly Dictionary<uint, Position> textureOffsets = new Dictionary<uint, Position>();

        public Render.Texture Texture
        {
            get;
        }

        internal TextureAtlas(Texture texture, Dictionary<uint, Position> textureOffsets)
        {
            Texture = texture;
            this.textureOffsets = textureOffsets;
        }

        public Position GetOffset(uint index)
        {
            return new Position(textureOffsets[index]);
        }
    }

    internal class TextureAtlasBuilder : ITextureAtlasBuilder
    {
        readonly Dictionary<uint, Graphic> textures = new Dictionary<uint, Graphic>();
        readonly State state = null;

        // key = max height of category
        class TextureCategorySorter : IComparer<KeyValuePair<uint, List<uint>>>
        {
            public int Compare(KeyValuePair<uint, List<uint>> x, KeyValuePair<uint, List<uint>> y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }

        public TextureAtlasBuilder(State state)
        {
            this.state = state;
        }

        public void AddTexture(uint index, Graphic texture)
        {
            textures.Add(index, texture);
        }

        public ITextureAtlas CreateUnpacked(uint maxWidth, uint bytesPerPixel)
        {
            uint width = 0u;
            uint height = 0u;
            uint xOffset = 0u;
            uint yOffset = 0u;
            Dictionary<uint, Position> textureOffsets = new Dictionary<uint, Position>();

            foreach (var textureEntry in textures)
            {
                var textureIndex = textureEntry.Key;
                var texture = textureEntry.Value;                

                if (xOffset + texture.Width <= maxWidth)
                {
                    if (yOffset + texture.Height > height)
                        height = yOffset + (uint)texture.Height;

                    textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

                    xOffset += (uint)texture.Width;

                    if (xOffset > width)
                        width = xOffset;
                }
                else
                {
                    xOffset = 0;
                    yOffset = height;

                    height = yOffset + (uint)texture.Height;

                    textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

                    xOffset += (uint)texture.Width;

                    if (xOffset > width)
                        width = xOffset;
                }
            }

            // create texture
            var atlasTexture = new MutableTexture(state, (int)width, (int)height, bytesPerPixel);

            foreach (var offset in textureOffsets)
            {
                var subTexture = textures[offset.Key];

                atlasTexture.AddSubTexture(offset.Value, subTexture.Data, subTexture.Width, subTexture.Height);
            }

            atlasTexture.Finish(0);

            return new TextureAtlas(atlasTexture, textureOffsets);
        }

        // Note: It is not the best texture packing algorithm but it will do its job
        public ITextureAtlas Create(uint bytesPerPixel)
        {
            // sort textures by similar heights (16-pixel bands)
            // heights of items are < key * 16
            // value = list of texture indices
            Dictionary<uint, List<uint>> textureCategories = new Dictionary<uint, List<uint>>();
            Dictionary<uint, uint> textureCategoryMinValues = new Dictionary<uint, uint>();
            Dictionary<uint, uint> textureCategoryMaxValues = new Dictionary<uint, uint>();
            Dictionary<uint, uint> textureCategoryTotalWidth = new Dictionary<uint, uint>();

            foreach (var texture in textures)
            {
                uint category = (uint)texture.Value.Height / 16u;

                if (!textureCategories.ContainsKey(category))
                {
                    textureCategories.Add(category, new List<uint>());
                    textureCategoryMinValues.Add(category, (uint)texture.Value.Height);
                    textureCategoryMaxValues.Add(category, (uint)texture.Value.Height);
                    textureCategoryTotalWidth.Add(category, (uint)texture.Value.Width);
                }
                else
                {
                    if (texture.Value.Height < textureCategoryMinValues[category])
                        textureCategoryMinValues[category] = (uint)texture.Value.Height;
                    if (texture.Value.Height > textureCategoryMaxValues[category])
                        textureCategoryMaxValues[category] = (uint)texture.Value.Height;
                    textureCategoryTotalWidth[category] += (uint)texture.Value.Width;
                }

                textureCategories[category].Add(texture.Key);
            }

            var filteredTextureCategories = new List<KeyValuePair<uint, List<uint>>>();

            foreach (var category in textureCategories)
            {
                if (textureCategories[category.Key].Count == 0)
                    continue; // was merged with lower category

                // merge categories with minimal differences
                if (textureCategoryMinValues[category.Key] >= category.Key * 16 + 8 &&
                    textureCategories.ContainsKey(category.Key + 1) &&
                    textureCategoryMaxValues[category.Key + 1] <= (category.Key + 1) * 16 + 8)
                {
                    textureCategories[category.Key].AddRange(textureCategories[category.Key + 1]);
                    textureCategoryMaxValues[category.Key] = Math.Max(textureCategoryMaxValues[category.Key], textureCategoryMaxValues[category.Key + 1]);
                    textureCategories[category.Key + 1].Clear();
                }

                filteredTextureCategories.Add(new KeyValuePair<uint, List<uint>>(textureCategoryMaxValues[category.Key], textureCategories[category.Key]));
            }

            filteredTextureCategories.Sort(new TextureCategorySorter());

            // now we have a sorted category list with all texture indices

            uint maxWidth = Math.Max(512u, textureCategoryMaxValues.Max(m => m.Value));
            uint width = 0u;
            uint height = 0u;
            uint xOffset = 0u;
            uint yOffset = 0u;
            Dictionary<uint, Position> textureOffsets = new Dictionary<uint, Position>();

            // create texture offsets
            foreach (var category in filteredTextureCategories)
            {
                foreach (var textureIndex in category.Value)
                {
                    var texture = textures[textureIndex];

                    if (xOffset + texture.Width <= maxWidth)
                    {
                        if (yOffset + texture.Height > height)
                            height = yOffset + (uint)texture.Height;

                        textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

                        xOffset += (uint)texture.Width;

                        if (xOffset > width)
                            width = xOffset;
                    }
                    else
                    {
                        xOffset = 0;
                        yOffset = height;

                        height = yOffset + (uint)texture.Height;

                        textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

                        xOffset += (uint)texture.Width;

                        if (xOffset > width)
                            width = xOffset;
                    }
                }

                if (xOffset > maxWidth - 320) // we do not expect textures with a width greater than 320
                {
                    xOffset = 0;
                    yOffset = height;
                }
            }

            // create texture
            var atlasTexture = new MutableTexture(state, (int)width, (int)height, bytesPerPixel);

            foreach (var offset in textureOffsets)
            {
                var subTexture = textures[offset.Key];

                atlasTexture.AddSubTexture(offset.Value, subTexture.Data, subTexture.Width, subTexture.Height);
            }

            atlasTexture.Finish(0);

            return new TextureAtlas(atlasTexture, textureOffsets);
        }
    }

    public class TextureAtlasBuilderFactory : ITextureAtlasBuilderFactory
    {
        readonly State state = null;

        public TextureAtlasBuilderFactory(State state)
        {
            this.state = state;
        }

        public ITextureAtlasBuilder Create()
        {
            return new TextureAtlasBuilder(state);
        }
    }
}
