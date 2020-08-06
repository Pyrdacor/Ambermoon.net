/*
 * TextureAtlasManager.cs - Manages texture atlases
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
using System;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    public class TextureAtlasManager
    {
        static TextureAtlasManager instance = null;
        static ITextureAtlasBuilderFactory factory = null;
        readonly Dictionary<Layer, ITextureAtlasBuilder> atlasBuilders = new Dictionary<Layer, ITextureAtlasBuilder>();
        readonly Dictionary<Layer, ITextureAtlas> atlas = new Dictionary<Layer, ITextureAtlas>();

        public static TextureAtlasManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new TextureAtlasManager();

                return instance;
            }
        }

        TextureAtlasManager()
        {

        }

        public static void RegisterFactory(ITextureAtlasBuilderFactory factory)
        {
            TextureAtlasManager.factory = factory;
        }

        // Note: Animation frames must be added as one large compound graphic.
        void AddTexture(Layer layer, uint index, Graphic texture)
        {
            if (factory == null)
                throw new AmbermoonException(ExceptionScope.Application, "No TextureAtlasBuilderFactory was registered.");

            if (atlas.ContainsKey(layer))
                throw new AmbermoonException(ExceptionScope.Application, $"Texture atlas already created for layer {layer}.");

            if (!atlasBuilders.ContainsKey(layer))
                atlasBuilders.Add(layer, factory.Create());

            atlasBuilders[layer].AddTexture(index, texture);
        }

        public ITextureAtlas GetOrCreate(Layer layer)
        {
            if (!atlas.ContainsKey(layer))
            {
                if (!atlasBuilders.ContainsKey(layer))
                    return null; // no texture for this layer

                atlas.Add(layer, atlasBuilders[layer].CreateUnpacked(320, 1));
            }

            return atlas[layer];
        }

        public ITextureAtlas CreateFromGraphics(Dictionary<uint, Graphic> graphics, uint bytesPerPixel)
        {
            var builder = factory.Create();

            foreach (var graphic in graphics)
                builder.AddTexture(graphic.Key, graphic.Value);

            return builder.Create(bytesPerPixel);
        }

        public Texture CreatePalette(IGraphicProvider graphicProvider)
        {
            var paletteBuilder = factory.Create();
            uint index = 0;

            foreach (var palette in graphicProvider.Palettes)
                paletteBuilder.AddTexture(index++, palette.Value);

            return paletteBuilder.CreateUnpacked(32, 4).Texture;
        }

        public void AddAll(IGameData gameData, IGraphicProvider graphicProvider, IFontProvider fontProvider)
        {
            if (gameData == null)
                throw new ArgumentNullException(nameof(gameData));

            if (graphicProvider == null)
                throw new ArgumentNullException(nameof(graphicProvider));

            #region Map 2D

            for (int i = (int)GraphicType.Tileset1; i <= (int)GraphicType.Tileset8; ++i)
            {
                var tilesetGraphics = graphicProvider.GetGraphics((GraphicType)i);

                for (uint graphicIndex = 0; graphicIndex < tilesetGraphics.Count; ++graphicIndex)
                {
                    AddTexture(Layer.MapBackground1 + i, graphicIndex, tilesetGraphics[(int)graphicIndex]);
                    AddTexture(Layer.MapForeground1 + i, graphicIndex, tilesetGraphics[(int)graphicIndex]);
                }
            }

            #endregion

            #region Player 2D

            var playerGraphics = graphicProvider.GetGraphics(GraphicType.Player);

            if (playerGraphics.Count != 3 * 17)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong number of player graphics.");

            // There are 3 player characters (one for each world, first lyramion, second forest moon, third morag).
            // Each has 17 frames: 3 back, 3 right, 3 front, 3 left, 1 sit back, 1 sit right, 1 sit front, 1 sit left, 1 bed/sleep.
            // All have a dimension of 16x32 pixels.
            for (int i = 0; i < playerGraphics.Count; ++i)
                AddTexture(Layer.Characters, (uint)i, playerGraphics[i]);

            #endregion

            #region UI Layout

            var layoutGraphics = graphicProvider.GetGraphics(GraphicType.Layout);

            for (int i = 0; i < layoutGraphics.Count; ++i)
                AddTexture(Layer.UIForeground, (uint)i, layoutGraphics[i]);

            #endregion

            #region Text

            var font = fontProvider.GetFont();

            for (uint i = 0; i < 94; ++i) // TODO: no space glyphs etc
                AddTexture(Layer.Text, i, font.GetGlyphGraphic(i));

            #endregion

            #region Items

            var itemGraphics = graphicProvider.GetGraphics(GraphicType.Item);

            for (int i = 0; i < itemGraphics.Count; ++i)
                AddTexture(Layer.Items, (uint)i, itemGraphics[i]);

            #endregion
        }
    }
}
