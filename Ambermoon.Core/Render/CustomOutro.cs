using Ambermoon.Data;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using TextColor = Ambermoon.Data.Enumerations.Color;

namespace Ambermoon.Render
{
    internal class CustomOutro
    {
        readonly Game game;
        readonly Layout layout;
        readonly IRenderView renderView;
        readonly Savegame savegame;
        readonly List<Popup> popups = new List<Popup>();
        readonly List<ILayerSprite> images = new List<ILayerSprite>();
        readonly List<UIText> texts = new List<UIText>();

        public CustomOutro(Game game, Layout layout, Savegame savegame)
        {
            this.game = game;
            this.layout = layout;
            renderView = layout.RenderView;
            this.savegame = savegame;
        }

        static readonly List<string> InvariantStrings = new List<string>
        {
        };

        static readonly Dictionary<GameLanguage, List<string>> LanguageDependentStrings = new Dictionary<GameLanguage, List<string>>
        {
        };

        public void Start()
        {
            // Load initial save but use current game hero portrait and name.
            var hero = savegame.PartyMembers[1];
            game.LoadInitial(hero.Name, hero.Gender == Gender.Female, hero.PortraitIndex, newSavegame =>
            {
                // Add Egil to second slot
                newSavegame.CurrentPartyMemberIndices[1] = 8;
                // Place ship at the docks
                newSavegame.TransportLocations[0].MapIndex = 156;
                newSavegame.TransportLocations[0].Position = new Position(12, 33);
                newSavegame.TransportLocations[0].TravelType = Data.Enumerations.TravelType.Ship;
                // Place horse on the docks
                newSavegame.TransportLocations[1].MapIndex = 156;
                newSavegame.TransportLocations[1].Position = new Position(16, 33);
                newSavegame.TransportLocations[1].TravelType = Data.Enumerations.TravelType.Horse;
                // Place the player on the docks
                newSavegame.CurrentMapIndex = 156;
                newSavegame.CurrentMapX = 13;
                newSavegame.CurrentMapY = 33;
                newSavegame.CharacterDirection = CharacterDirection.Left;
                // Set to day
                newSavegame.Hour = 13;
                newSavegame.Minute = 37;
                newSavegame.HoursWithoutSleep = 0;
                // Activate all special items
                newSavegame.SpecialItemsActive = 0xffff;
            });
            void StopGame()
            {
                game.Pause();
                game.StartSequence();
            }
            StopGame();
            game.AddTimedEvent(TimeSpan.FromMilliseconds(Game.FadeTime / 2), StopGame);
        }

        void Clear()
        {
            foreach (var text in texts)
                text.Destroy();
            foreach (var image in images)
                image.Delete();
            foreach (var popup in popups)
                popup.Destroy();

            texts.Clear();
            images.Clear();
            popups.Clear();
        }

        void AddImage(Layer layer, uint index, Rect rect, bool withFrameBorder, byte displayLayer = 0,
            byte? paletteIndex = null)
        {
            int columns = (rect.Width + 15) / 16;
            int rows = (rect.Height + 15) / 16;

            if (withFrameBorder)
            {
                columns += 2;
                rows += 2;
            }

            var popup = new Popup(game, renderView, rect.Position - new Position(16, 16), columns, rows,
                !withFrameBorder, (byte)Math.Max(0, displayLayer - Popup.BaseDisplayLayer));
            popup.AddImage(rect, index, layer, 0, paletteIndex ?? game.PrimaryUIPaletteIndex);

            popups.Add(popup);
        }

        void AddText(string text, Rect rect, TextColor color, TextAlign textAlign = TextAlign.Left, byte displayLayer = 0)
        {
            texts.Add(layout.AddText(rect, game.ProcessText(text, rect), color, textAlign, displayLayer));
        }

        interface IAction
        {
            void Run(CustomOutro outro);
        }

        class ShowConversationPortraitAction : IAction
        {
            readonly uint portraitIndex;
            readonly Rect rect;
            readonly string name;

            public ShowConversationPortraitAction(uint portraitIndex, Position position, string name)
            {
                this.portraitIndex = portraitIndex;
                rect = new Rect(position, new Size(32, 32));
                this.name = name;
            }

            public void Run(CustomOutro outro)
            {
                outro.AddImage(Layer.UI, Graphics.PortraitOffset + portraitIndex - 1, rect, true);
                outro.AddText(name, new Rect(rect.X, rect.Y + 64, Global.VirtualScreenWidth, Global.GlyphLineHeight), TextColor.Yellow);
            }
        }
    }
}
