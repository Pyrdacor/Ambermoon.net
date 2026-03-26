/*
 * TextureAtlasManager.cs - Manages texture atlases
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

using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Render
{
    public class TextureAtlasManager
    {
        static TextureAtlasManager? instance = null;
        static ITextureAtlasBuilderFactory? factory = null;
        static ITextureAtlasConverter? converter = null;
        readonly Dictionary<Layer, ITextureAtlasBuilder> atlasBuilders = [];
        readonly Dictionary<Layer, ITextureAtlas> atlas = [];

        public static TextureAtlasManager Instance => instance ??= new TextureAtlasManager();

        TextureAtlasManager()
        {

        }

        public static void RegisterFactory(ITextureAtlasBuilderFactory factory)
        {
            TextureAtlasManager.factory = factory;
        }

        public static void RegisterConverter(ITextureAtlasConverter converter)
        {
            TextureAtlasManager.converter = converter;
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

        public bool HasLayer(Layer layer) => atlas.ContainsKey(layer);

        public ITextureAtlas? GetOrCreate(Layer layer)
        {
            if (!atlas.ContainsKey(layer))
            {
                if (!atlasBuilders.TryGetValue(layer, out ITextureAtlasBuilder? value))
                    return null; // no texture for this layer

                if (layer == Layer.BattleMonsterRow)
                    atlas.Add(layer, value.Create(1));
                else if (layer == Layer.Images || layer == Layer.MobileOverlays)
                    atlas.Add(layer, atlasBuilders[layer].Create(4));
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

        public void AddAtlas(Layer layer, IGraphicAtlas atlas)
        {
            if (this.atlas.ContainsKey(layer))
                throw new AmbermoonException(ExceptionScope.Application, $"Texture atlas already created for layer {layer}.");
            
            this.atlas.Add(layer, converter!.Convert(atlas));
        }

        public void AddAtlases(Layer layer, params (uint Offset, IGraphicAtlas Atlas)[] atlases)
        {
            AddAtlases(layer, atlases.ToDictionary(a => a.Offset, a => a.Atlas));
        }

        public void AddAtlases(Layer layer, Dictionary<uint, IGraphicAtlas> atlases)
        {
            if (this.atlas.ContainsKey(layer))
                throw new AmbermoonException(ExceptionScope.Application, $"Texture atlas already created for layer {layer}.");

            this.atlas.Add(layer, converter!.Convert(atlases));
        }

        public static ITextureAtlas CreateFromGraphics(Dictionary<uint, Graphic> graphics, uint bytesPerPixel)
        {
            var builder = factory!.Create();

            foreach (var graphic in graphics)
                builder.AddTexture(graphic.Key, graphic.Value);

            return builder.Create(bytesPerPixel);
        }

        public static Texture CreatePalette(IPaletteProvider paletteProvider, params Graphic[] additionalPalettes)
        {
            var paletteBuilder = factory!.Create();
            uint index = 0;

            foreach (var palette in paletteProvider.Palettes)
                paletteBuilder.AddTexture(index++, palette.Value);

            if (additionalPalettes != null && additionalPalettes.Length != 0 && additionalPalettes[0] != null)
            {
                foreach (var palette in additionalPalettes)
                    paletteBuilder.AddTexture(index++, palette);
            }

            return paletteBuilder.CreateUnpacked(32, 4).Texture;
        }

        public static KeyValuePair<TextureAtlasManager, Action> CreateUIOnly(IGraphicProvider graphicProvider, IFontProvider fontProvider)
        {
            var textureAtlasManager = new TextureAtlasManager();

            return KeyValuePair.Create(textureAtlasManager, () =>
            {
                textureAtlasManager.AddUI(graphicProvider, [], false);
                textureAtlasManager.AddCursors(graphicProvider);
                textureAtlasManager.AddFont(fontProvider);
            });
        }

        public static TextureAtlasManager CreateEmpty()
        {
            return new TextureAtlasManager();
        }

        public void AddUIOnly(IGraphicInfoProvider graphicInfoProvider, IFontProvider fontProvider)
        {
            var uiGraphicAtlases = new Dictionary<uint, IGraphicAtlas>();

            AddUI(graphicInfoProvider, uiGraphicAtlases, false);
            AddCursors(graphicInfoProvider);
            AddFont(fontProvider);

            if (graphicInfoProvider is IGraphicAtlasProvider graphicAtlasProvider)
                AddAtlases(Layer.UI, uiGraphicAtlases);
        }

        void AddUI(IGraphicInfoProvider graphicInfoProvider, Dictionary<uint, IGraphicAtlas> uiGraphicAtlases, bool withLayout = true)
        {
            if (withLayout)
            {
                if (graphicInfoProvider is IGraphicProvider graphicProvider1)
                {
                    var layoutGraphics = graphicProvider1.GetGraphics(GraphicType.Layout);

                    for (int i = 0; i < layoutGraphics.Count; ++i)
                        AddTexture(Layer.UI, Graphics.LayoutOffset + (uint)i, layoutGraphics[i]);
                }
                else if (graphicInfoProvider is IGraphicAtlasProvider graphicAtlasProvider)
                {
                    uiGraphicAtlases.Add(Graphics.LayoutOffset, graphicAtlasProvider.GetGraphicAtlas(GraphicType.Layout));
                }
            }

            if (graphicInfoProvider is IGraphicProvider graphicProvider)
            {
                var uiElementGraphics = graphicProvider.GetGraphics(GraphicType.UIElements);

                for (int i = 0; i < uiElementGraphics.Count; ++i)
                    AddTexture(Layer.UI, Graphics.UICustomGraphicOffset + (uint)i, uiElementGraphics[i]);
            }
            else if (graphicInfoProvider is IGraphicAtlasProvider graphicAtlasProvider)
            {
                var uiElementGraphicAtlas = graphicAtlasProvider!.GetGraphicAtlas(GraphicType.UIElements);

                // The last entry is the sword and mace image for the battle window.
                var oldSwordAndMaceKey = uiElementGraphicAtlas.Offsets.Keys.Max();
                uint newSwordAndMaceKey = Graphics.CombatGraphicOffset + (uint)CombatGraphicIndex.UISwordAndMace - Graphics.UICustomGraphicOffset;

                if (oldSwordAndMaceKey != newSwordAndMaceKey)
                {
                    uiElementGraphicAtlas.Offsets[newSwordAndMaceKey] = uiElementGraphicAtlas.Offsets[oldSwordAndMaceKey];
                    uiElementGraphicAtlas.Offsets.Remove(oldSwordAndMaceKey);
                }

                uiGraphicAtlases.Add(Graphics.UICustomGraphicOffset, uiElementGraphicAtlas);
            }
        }

        void AddCursors(IGraphicInfoProvider graphicInfoProvider)
        {
            if (graphicInfoProvider is IGraphicProvider graphicProvider)
            {
                var cursorGraphics = graphicProvider.GetGraphics(GraphicType.Cursor);

                for (int i = 0; i < cursorGraphics.Count; ++i)
                    AddTexture(Layer.Cursor, (uint)i, cursorGraphics[i]);
            }
            else if (graphicInfoProvider is IGraphicAtlasProvider graphicAtlasProvider)
            {
                AddAtlas(Layer.Cursor, graphicAtlasProvider.GetGraphicAtlas(GraphicType.Cursor));
            }
        }

        void AddFont(IFontProvider fontProvider)
        {
            var font = fontProvider.GetFont();

            for (uint i = 0; i < font.GlyphCount; ++i)
            {
                var glyphGraphic = font.GetGlyphGraphic(i);

                AddTexture(Layer.Text, i, glyphGraphic);
                AddTexture(Layer.SubPixelText, i, glyphGraphic);
            }        

            // Add simple digits for damage display
            for (uint i = 0; i < 10; ++i)
                AddTexture(Layer.SmallDigits, i, font.GetDigitGlyphGraphic(i));
        }

        public void ReplaceGraphic(Layer layer, uint index, Graphic graphic)
        {
            if (atlas.ContainsKey(layer))
                throw new AmbermoonException(ExceptionScope.Application, $"Texture atlas already created for layer {layer}.");

            if (!atlasBuilders.TryGetValue(layer, out var builder))
                throw new AmbermoonException(ExceptionScope.Application, $"No texture atlas builder for layer {layer}.");

            builder.ReplaceTexture(index, graphic);
        }

        public void AddAll(IGameData gameData, IGraphicInfoProvider graphicInfoProvider, IFontProvider fontProvider,
            Dictionary<uint, Graphic> introTextGlyphs, Dictionary<uint, Graphic> introLargeTextGlyphs,
            Dictionary<uint, Graphic> introGraphics, Features features, int numTilesets)
        {
            ArgumentNullException.ThrowIfNull(gameData);
            ArgumentNullException.ThrowIfNull(graphicInfoProvider);

            var graphicProvider = graphicInfoProvider as IGraphicProvider;
            var graphicAtlasProvider = graphicInfoProvider as IGraphicAtlasProvider;

            if (graphicProvider == null && graphicAtlasProvider == null)
                throw new AmbermoonException(ExceptionScope.Application, "GraphicInfoProvider must implement IGraphicProvider or IGraphicAtlasProvider.");

            #region Map 2D

            if (graphicProvider is not null)
            {
                for (int i = 0; i < numTilesets; ++i)
                {
                    var tilesetGraphics = graphicProvider.GetGraphics(GraphicType.Tileset1 + i);

                    for (uint graphicIndex = 0; graphicIndex < tilesetGraphics.Count; ++graphicIndex)
                    {
                        AddTexture(Layer.MapBackground1 + i, graphicIndex, tilesetGraphics[(int)graphicIndex]);
                        AddTexture(Layer.MapForeground1 + i, graphicIndex, tilesetGraphics[(int)graphicIndex]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < numTilesets; ++i)
                {
                    var atlas = graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Tileset1 + i);

                    AddAtlas(Layer.MapBackground1 + i, atlas);
                    AddAtlas(Layer.MapForeground1 + i, atlas);
                }
            }

            #endregion

            #region Player 2D

            if (graphicProvider is not null)
            {
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
                int count = features.HasFlag(Features.WaspTransport) ? 12 : 11;

                if (travelGraphics.Count != count * 4)
                    throw new AmbermoonException(ExceptionScope.Data, "Wrong number of travel graphics.");

                for (int i = 0; i < travelGraphics.Count; ++i)
                    AddTexture(Layer.Characters, Graphics.TravelGraphicOffset + (uint)i, travelGraphics[i]);

                var transportGraphics = graphicProvider.GetGraphics(GraphicType.Transports);

                if (transportGraphics.Count != 5)
                    throw new AmbermoonException(ExceptionScope.Data, "Wrong number of transport graphics.");

                for (int i = 0; i < transportGraphics.Count; ++i)
                    AddTexture(Layer.Characters, Graphics.TransportGraphicOffset + (uint)i, transportGraphics[i]);

                var npcGraphics = graphicProvider.GetGraphics(GraphicType.NPC);

                if (npcGraphics.Count < 34)
                    throw new AmbermoonException(ExceptionScope.Data, "Wrong number of NPC graphics.");

                for (int i = 0; i < npcGraphics.Count; ++i)
                    AddTexture(Layer.Characters, Graphics.NPCGraphicOffset + (uint)i, npcGraphics[i]);
            }
            else
            {
                AddAtlases(Layer.Characters,
                    (0u, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Player)),
                    (Graphics.TravelGraphicOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.TravelGfx)),
                    (Graphics.TransportGraphicOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Transports)),
                    (Graphics.NPCGraphicOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.NPC)));
            }

            #endregion

            #region UI Layout

            Dictionary<uint, IGraphicAtlas> uiGraphicAtlases = [];

            AddUI(graphicInfoProvider, uiGraphicAtlases);

            #endregion

            #region Portraits

            if (graphicProvider is not null)
            {
                var portraits = graphicProvider.GetGraphics(GraphicType.Portrait);

                for (int i = 0; i < portraits.Count; ++i)
                    AddTexture(Layer.UI, Graphics.PortraitOffset + (uint)i, portraits[i]);
            }
            else
            {
                uiGraphicAtlases.Add(Graphics.PortraitOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Portrait));
            }

            #endregion

            #region Pics 80x80

            if (graphicProvider is not null)
            {
                var pics80x80Graphics = graphicProvider.GetGraphics(GraphicType.Pics80x80);

                for (int i = 0; i < pics80x80Graphics.Count; ++i)
                    AddTexture(Layer.UI, Graphics.Pics80x80Offset + (uint)i, pics80x80Graphics[i]);
            }
            else
            {
                uiGraphicAtlases.Add(Graphics.Pics80x80Offset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Pics80x80));
            }

            #endregion

            #region Event pix

            if (graphicProvider is not null)
            {
                var eventGraphics = graphicProvider.GetGraphics(GraphicType.EventPictures);

                for (int i = 0; i < eventGraphics.Count; ++i)
                {
                    eventGraphics[i].ReplaceColor(0, 32); // Black instead of transparent
                    AddTexture(Layer.UI, Graphics.EventPictureOffset + (uint)i, eventGraphics[i]);
                }
            }
            else
            {
                // Note: We assume, that the new graphic atlas format will
                // take care of the color replacement already!
                uiGraphicAtlases.Add(Graphics.EventPictureOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.EventPictures));
            }

            #endregion

            #region Text

            AddFont(fontProvider);

            #endregion

            #region Items

            if (graphicProvider is not null)
            {
                var itemGraphics = graphicProvider.GetGraphics(GraphicType.Item);

                for (int i = 0; i < itemGraphics.Count; ++i)
                    AddTexture(Layer.Items, (uint)i, itemGraphics[i]);
            }
            else
            {
                AddAtlas(Layer.Items, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.Item));
            }

            #endregion

            #region Cursors

            AddCursors(graphicInfoProvider);

            #endregion

            #region Combat backgrounds

            if (graphicProvider is not null)
            {
                var combatBackgrounds = graphicProvider.GetGraphics(GraphicType.CombatBackground);

                for (int i = 0; i < combatBackgrounds.Count; ++i)
                    AddTexture(Layer.CombatBackground, (uint)i, combatBackgrounds[i]);
            }
            else
            {
                AddAtlas(Layer.CombatBackground, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.CombatBackground));
            }

            #endregion

            #region Combat graphics (without battle field icons)

            if (graphicProvider is not null)
            {
                var combatGraphics = graphicProvider.GetGraphics(GraphicType.CombatGraphics);

                for (int i = 0; i < combatGraphics.Count; ++i)
                {
                    // Note: One graphic is an UI element so we put it into the UI layer. The rest goes into the BattleEffects layer.
                    AddTexture(i == (int)CombatGraphicIndex.UISwordAndMace ? Layer.UI : Layer.BattleEffects, Graphics.CombatGraphicOffset + (uint)i, combatGraphics[i]);
                }
            }
            else
            {
                // NOTE: The new graphic atlas format will include the Sword and Mace image in the custom UI elements.
                // So here we only handle the battle effects graphics.
                AddAtlas(Layer.BattleEffects, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.CombatGraphics));
            }

            #endregion

            #region Battle field icons

            if (graphicProvider is not null)
            {
                var battleFieldIcons = graphicProvider.GetGraphics(GraphicType.BattleFieldIcons);

                for (int i = 0; i < battleFieldIcons.Count; ++i)
                {
                    AddTexture(Layer.UI, Graphics.BattleFieldIconOffset + (uint)i, battleFieldIcons[i]);
                }
            }
            else
            {
                uiGraphicAtlases.Add(Graphics.BattleFieldIconOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.BattleFieldIcons));
            }

            #endregion

            #region Automap graphics

            if (graphicProvider is not null)
            {
                var automapGraphics = graphicProvider.GetGraphics(GraphicType.AutomapGraphics);

                for (int i = 0; i < automapGraphics.Count; ++i)
                    AddTexture(Layer.UI, Graphics.AutomapOffset + (uint)i, automapGraphics[i]);
            }
            else
            {
                uiGraphicAtlases.Add(Graphics.AutomapOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.AutomapGraphics));
            }

            #endregion

            #region Riddlemouth graphics

            if (graphicProvider is not null)
            {
                var riddlemouthGraphics = graphicProvider.GetGraphics(GraphicType.RiddlemouthGraphics);

                for (int i = 0; i < riddlemouthGraphics.Count; ++i)
                    AddTexture(Layer.UI, Graphics.RiddlemouthOffset + (uint)i, riddlemouthGraphics[i]);
            }
            else
            {
                uiGraphicAtlases.Add(Graphics.RiddlemouthOffset, graphicAtlasProvider!.GetGraphicAtlas(GraphicType.RiddlemouthGraphics));
            }

            #endregion

            #region Intro Text

            foreach (var introTextGlyph in introTextGlyphs)
                AddTexture(Layer.IntroText, introTextGlyph.Key, introTextGlyph.Value);
            foreach (var introTextGlyph in introLargeTextGlyphs)
            {
                AddTexture(Layer.IntroText, introTextGlyph.Key, introTextGlyph.Value);
                AddTexture(Layer.MainMenuText, introTextGlyph.Key, introTextGlyph.Value);
            }

            #endregion

            #region Intro Graphics

            foreach (var introGraphic in introGraphics)
            {
                switch (introGraphic.Key)
                {
                    case (uint)IntroGraphic.MainMenuBackground:
                    case (uint)IntroGraphic.CloudsLeft:
                    case (uint)IntroGraphic.CloudsRight:
                        // We only need the background for the main menu and the clouds from the intro.
                        // The intro does not need them as they only use the main menu layers.
                        AddTexture(Layer.MainMenuGraphics, introGraphic.Key, introGraphic.Value);
                        break;
                    default:
                        AddTexture(Layer.IntroGraphics, introGraphic.Key, introGraphic.Value);
                        break;
                }
            }

            #endregion

            if (graphicAtlasProvider is not null)
                AddAtlases(Layer.UI, uiGraphicAtlases);
        }
    }
}
