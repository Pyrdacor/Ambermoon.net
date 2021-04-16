/*
 * TextureAtlasManager.cs - Manages texture atlases
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

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
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

                if (layer == Layer.BattleMonsterRow)
                    atlas.Add(layer, atlasBuilders[layer].Create(1));
                else
                    atlas.Add(layer, atlasBuilders[layer].CreateUnpacked(320, 1));
            }

            return atlas[layer];
        }

        public void AddFromGraphics(Layer layer, Dictionary<uint, Graphic> graphics)
        {
            foreach (var graphic in graphics)
                AddTexture(layer, graphic.Key, graphic.Value);
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

        public static KeyValuePair<TextureAtlasManager, Action> CreateUIOnly(IGraphicProvider graphicProvider, IFontProvider fontProvider)
        {
            var textureAtlasManager = new TextureAtlasManager();
            return KeyValuePair.Create<TextureAtlasManager, Action>(textureAtlasManager, () =>
            {
                textureAtlasManager.AddUI(graphicProvider, false);
                textureAtlasManager.AddCursors(graphicProvider);
                textureAtlasManager.AddFont(fontProvider);
            });
        }

        public static TextureAtlasManager CreateEmpty()
        {
            return new TextureAtlasManager();
        }

        public void AddUIOnly(IGraphicProvider graphicProvider, IFontProvider fontProvider)
        {
            AddUI(graphicProvider, false);
            AddCursors(graphicProvider);
            AddFont(fontProvider);
        }

        void AddUI(IGraphicProvider graphicProvider, bool withLayout = true)
        {
            if (withLayout)
            {
                var layoutGraphics = graphicProvider.GetGraphics(GraphicType.Layout);

                for (int i = 0; i < layoutGraphics.Count; ++i)
                    AddTexture(Layer.UI, Graphics.LayoutOffset + (uint)i, layoutGraphics[i]);
            }

            var uiElementGraphics = graphicProvider.GetGraphics(GraphicType.UIElements);

            for (int i = 0; i < uiElementGraphics.Count; ++i)
                AddTexture(Layer.UI, Graphics.UICustomGraphicOffset + (uint)i, uiElementGraphics[i]);
        }

        void AddCursors(IGraphicProvider graphicProvider)
        {
            var cursorGraphics = graphicProvider.GetGraphics(GraphicType.Cursor);

            for (int i = 0; i < cursorGraphics.Count; ++i)
                AddTexture(Layer.Cursor, (uint)i, cursorGraphics[i]);
        }

        void AddFont(IFontProvider fontProvider)
        {
            var font = fontProvider.GetFont();

            for (uint i = 0; i < 94; ++i)
                AddTexture(Layer.Text, i, font.GetGlyphGraphic(i));

            // Add simple digits for damage display
            for (uint i = 0; i < 10; ++i)
                AddTexture(Layer.Text, 100 + i, font.GetDigitGlyphGraphic(i));
        }

        public void AddAll(IGameData gameData, IGraphicProvider graphicProvider, IFontProvider fontProvider,
            Dictionary<uint, Graphic> introTextGlyphs, Dictionary<uint, Graphic> introGraphics)
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

            // On world maps the travel graphics are used.
            // Only 4 sprites are used (one for each direction).
            var travelGraphics = graphicProvider.GetGraphics(GraphicType.TravelGfx);

            if (travelGraphics.Count != 11 * 4)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong number of travel graphics.");

            for (int i = 0; i < travelGraphics.Count; ++i)
                AddTexture(Layer.Characters, Graphics.TravelGraphicOffset + (uint)i, travelGraphics[i]);

            var transportGraphics = graphicProvider.GetGraphics(GraphicType.Transports);

            if (transportGraphics.Count != 5)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong number of transport graphics.");

            for (int i = 0; i < transportGraphics.Count; ++i)
                AddTexture(Layer.Characters, Graphics.TransportGraphicOffset + (uint)i, transportGraphics[i]);

            var npcGraphics = graphicProvider.GetGraphics(GraphicType.NPC);

            if (npcGraphics.Count != 34)
                throw new AmbermoonException(ExceptionScope.Data, "Wrong number of NPC graphics.");

            for (int i = 0; i < npcGraphics.Count; ++i)
                AddTexture(Layer.Characters, Graphics.NPCGraphicOffset + (uint)i, npcGraphics[i]);

            #endregion

            #region UI Layout

            AddUI(graphicProvider);

            #endregion

            #region Portraits

            var portraits = graphicProvider.GetGraphics(GraphicType.Portrait);

            for (int i = 0; i < portraits.Count; ++i)
                AddTexture(Layer.UI, Graphics.PortraitOffset + (uint)i, portraits[i]);

            #endregion

            #region Pics 80x80

            var pics80x80Graphics = graphicProvider.GetGraphics(GraphicType.Pics80x80);

            for (int i = 0; i < pics80x80Graphics.Count; ++i)
                AddTexture(Layer.UI, Graphics.Pics80x80Offset + (uint)i, pics80x80Graphics[i]);

            #endregion

            #region Event pix

            var eventGraphics = graphicProvider.GetGraphics(GraphicType.EventPictures);

            for (int i = 0; i < eventGraphics.Count; ++i)
                AddTexture(Layer.UI, Graphics.EventPictureOffset + (uint)i, eventGraphics[i]);

            #endregion

            #region Text

            AddFont(fontProvider);

            #endregion

            #region Items

            var itemGraphics = graphicProvider.GetGraphics(GraphicType.Item);

            for (int i = 0; i < itemGraphics.Count; ++i)
                AddTexture(Layer.Items, (uint)i, itemGraphics[i]);

            #endregion

            #region Cursors

            AddCursors(graphicProvider);

            #endregion

            #region Combat backgrounds

            var combatBackgrounds = graphicProvider.GetGraphics(GraphicType.CombatBackground);

            for (int i = 0; i < combatBackgrounds.Count; ++i)
                AddTexture(Layer.CombatBackground, Graphics.CombatBackgroundOffset + (uint)i, combatBackgrounds[i]);

            #endregion

            #region Combat graphics (without battle field icons)

            var combatGraphics = graphicProvider.GetGraphics(GraphicType.CombatGraphics);

            for (int i = 0; i < combatGraphics.Count; ++i)
            {
                // Note: One graphic is an UI element so we put it into the UI layer. The rest goes into the BattleEffects layer.
                AddTexture(i == (int)CombatGraphicIndex.UISwordAndMace ? Layer.UI : Layer.BattleEffects, Graphics.CombatGraphicOffset + (uint)i, combatGraphics[i]);
            }

            #endregion

            #region Battle field icons

            var battleFieldIcons = graphicProvider.GetGraphics(GraphicType.BattleFieldIcons);

            for (int i = 0; i < battleFieldIcons.Count; ++i)
            {
                AddTexture(Layer.UI, Graphics.BattleFieldIconOffset + (uint)i, battleFieldIcons[i]);
            }

            #endregion

            #region Automap graphics

            var automapGraphics = graphicProvider.GetGraphics(GraphicType.AutomapGraphics);

            for (int i = 0; i < automapGraphics.Count; ++i)
                AddTexture(Layer.UI, Graphics.AutomapOffset + (uint)i, automapGraphics[i]);

            #endregion

            #region Riddlemouth graphics

            var riddlemouthGraphics = graphicProvider.GetGraphics(GraphicType.RiddlemouthGraphics);

            for (int i = 0; i < riddlemouthGraphics.Count; ++i)
                AddTexture(Layer.UI, Graphics.RiddlemouthOffset + (uint)i, riddlemouthGraphics[i]);

            #endregion

            #region Intro Text

            foreach (var introTextGlyph in introTextGlyphs)
                AddTexture(Layer.IntroText, introTextGlyph.Key, introTextGlyph.Value);

            #endregion

            #region Intro Graphics

            foreach (var introGraphic in introGraphics)
                AddTexture(Layer.IntroGraphics, introGraphic.Key, introGraphic.Value);

            #endregion
        }
    }
}
