/*
 * Credits.cs - Remake credits
 *
 * Copyright (C) 2021-2022  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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

using System;
using System.Collections.Generic;
using Ambermoon.UI;

namespace Ambermoon.Render
{
    internal class Credits
    {
        struct CreditsText
        {
            public uint EmptyLines;
            public string Text;
        }

        readonly IRenderView renderView;
        readonly Layout layout;
        readonly Action<Action> finishAction;
        readonly List<IRenderText> texts = new List<IRenderText>();
        readonly Queue<CreditsText> creditsTexts = new Queue<CreditsText>();
        double ticks = 0;
        double nextTextTicks = 0;
        double lastScrollTicks = 0;
        double lineScrollTicks = 0;
        const long TicksPerLine = 6 * Global.GlyphLineHeight;
        CreditsText lastText;

        public Credits(IRenderView renderView, Layout layout, Action<Action> finishAction)
        {
            this.renderView = renderView;
            this.layout = layout;
            this.finishAction = finishAction;

            AddHeader("Ambermoon");
            AddText("rewritten by Pyrdacor");

            AddText("With this project I fulfilled a dream of mine.", 6);
            AddText("I loved Ambermoon from the start and making it");
            AddText("available to more people makes me very happy.");

            AddText("I am very grateful to Karsten Köper and the whole", 2);
            AddText("team of Thalion Software for creating this game.");
            AddText("It made my childhood an adventure too. Thank you!");

            AddHeader("Special Thanks", 14);

            AddText("First of all I want to thank kermitfrog for his", 1);
            AddText("awesome m68k skills and introducing me to Ghidra.");
            AddText("Without him much of this wouldn't have been possible.");

            AddText("And of course I want to thank Alex Holland!", 3);
            AddText("Not only managed he to save an english version");
            AddText("of Ambermoon but also he knows so much about");
            AddText("Ambermoon, Amberstar and Thalion. He also preserved");
            AddText("so much knowledge and resources over the years that");
            AddText("Ambermoon would not be possible without Alex I guess.");
            AddText("Thank you so much for all you have done!");

            AddText("I also want to thank Nico Bendlin and Jurie Horneman.", 3);
            AddText("Even though they don't have much time, they support");
            AddText("where they can. Nico decoded the fantasy intro");
            AddText("and Jurie finally found and released the original");
            AddText("Ambermoon source code and docs in May 2023.");

            AddText("And of course I want to thank my wife.", 3);
            AddText("She was very patient and supportive with me.");

            AddHeader("My supporters", 16);
            AddText("Every nerd also needs something to eat. So I am very", 1);
            AddText("thankful for all the support I get. Many people");
            AddText("donated or even became a patron of mine.");
            AddText("Thanks to you all! Especially to my top patrons:");

            AddText("Philip Breitsprecher", 1);
            AddText("Mike Valtix");
            AddText("Sebberick");
            AddText("Thomas Ritschel");
            AddText("Tschorle");
            AddText("Daniel Egger");
            AddText("Kaspar");
            AddText("NeXuS-Arts");
            AddText("timbo t");
            AddText("Other Retro Matt");
            AddText("Anton Huber");
            AddText("Lars");
            AddText("Robin Mattheussen");
            AddText("giom");
            AddText("Levidega");
            AddText("Lorenz P.");
            AddText("LoneRaider");
            AddText("MD");
            AddText("Unreality");
            AddText("Milan");
            AddText("Peter Holtgrewe");
            AddText("frostworx");
            AddText("meok meok");
            AddText("Martin Tramm");
            AddText("Stay Forever");
            AddText("Alexander Holland");
            AddText("Stephan Mankie");
            AddText("André Wösten");
            AddText("Benno");
            AddText("orgi");
            AddText("JR_Riketz");
            AddText("Wolfgang");
            AddText("Sprudel");
            AddText("NLS");
            AddText("Benjamin Ziebert");
            AddText("David Geiger");
            AddText("skobry");
            AddText("crediar");
            AddText("AMike");
            AddText("soulsuckingjerk");
            AddText("Mahen");            
            AddText("Teladi");

            AddHeader("Contributors", 12);
            AddText("Over the years many people contributed to Ambermoon.", 1);
            AddText("In honor of their efforts I list some of them here:");

            AddText("meynaf", 1);
            AddText("st-h");
            AddText("dlfrSilver (Dennis Lechevalier)");
            AddText("MetalliC (Vitaly Grebennik)");
            AddText("Hexaae (Luca Longone)");
            AddText("Oliver Gantert (amberworlds project)");
            AddText("Daniel Schulz (slothsoft.net)");
            AddText("Nico Bendlin (Ambermoon gitlab)");
            AddText("Metibor");
            AddText("prophesore");
            AddText("Michael Böhnisch");
            AddText("Simone Bevilacqua");
            AddText("Karol Kliestenec");
            AddText("Georg Fuchs");
            AddText("Gerald Müller-Bruhnke");

            AddText("Thank you guys! You're awesome!", 1);

            AddText("Also thanks to all the testers of Ambermoon.net!", 3);
            AddText("Especially to Thallyrion, Uukrull, Nephilim, crediar");
            AddText("and skdubg who also helped fixing translation bugs.");

            AddText("Thanks to Czudak who created the app icon, convinced", 1);
            AddText("me to create a patreon page and wrote about my project.");

            AddText("Matthias Steinwachs (the guy who made the incredible", 6);
            AddText("music for Ambermoon) started creating remixes of");
            AddText("all the beautiful tracks. Check his work out at:");
            AddText("https://soundcloud.com/audiotexturat/sets");

            AddHeader("Projects to come", 16);
            AddText("The next project will be ~INK17~Ambermoon Advanced~INK31~.", 1);
            AddText("It will balance the game, add new quests, places,");
            AddText("monsters, NPCs, items and much more.");
            AddText("You can play it on the Amiga or with Ambermoon.net.");

            AddText("After this I will start creating the ~INK17~third part~INK31~.", 3);
            AddText("~INK17~of the Amber trilogy~INK31~. This will be a huge project.");

            AddText("To stay informed visit me on github, follow me on", 3);
            AddText("twitter or just stay in touch.");

            AddHeader("The real end", 9);

            AddText("Pyrdacor - trobt(at)web.de", 2);
            AddText("github.com/Pyrdacor");
            AddText("www.patreon.com/Pyrdacor");
            AddText("twitter.com/Pyrdacor");
            AddText("www.pyrdacor.net");

            AddText("December 2022", 2);

            lastText = creditsTexts.Peek();
            SetupNextText(lastText.EmptyLines);
        }

        void SetupNextText(uint emptyLines)
        {
            nextTextTicks = ticks + emptyLines * TicksPerLine;
        }

        void AddHeader(string text, uint emptyLines = 0)
        {
            AddText(text, emptyLines);
            AddText(new string('-', text.Length));
        }

        void AddText(string text, uint emptyLines = 0)
        {
            creditsTexts.Enqueue(new CreditsText { EmptyLines = emptyLines, Text = text });
        }

        void CreateText(string text)
        {
            var bounds = layout.GetTextRect(0, Global.VirtualScreenHeight, Global.VirtualScreenWidth, Global.GlyphLineHeight);
            var renderText = renderView.RenderTextFactory.Create(
                (byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
                renderView.GetLayer(Layer.Text),
                renderView.TextProcessor.ProcessText(text, null, null),
                Data.Enumerations.Color.Bright, false, bounds, TextAlign.Center);
            texts.Add(renderText);
            renderText.Visible = true;
        }

        void Scroll()
        {
            double tickDiff = ticks - lastScrollTicks;
            lastScrollTicks = ticks;
            lineScrollTicks += tickDiff;
            int scrollAmount = Util.Round(lineScrollTicks / 6.0);

            if (scrollAmount != 0)
            {
                for (int i = texts.Count - 1; i >= 0; --i)
                {
                    texts[i].Place(new Rect(0, texts[i].Y - scrollAmount, Global.VirtualScreenWidth, texts[i].Height), TextAlign.Center);

                    if (texts[i].Y <= -Global.GlyphLineHeight)
                    {
                        texts[i].Delete();
                        texts.RemoveAt(i);
                    }
                }
            }

            lastScrollTicks -= lineScrollTicks - scrollAmount * 6;
            lineScrollTicks = 0;
        }

        public void Update(double deltaTime)
        {
            ticks += Game.TicksPerSecond * deltaTime;

            Scroll();

            if (ticks >= nextTextTicks)
            {
                if (creditsTexts.Count == 0)
                {
                    finishAction?.Invoke(() =>
                    {
                        texts.ForEach(text => text?.Delete());
                        texts.Clear();
                    });
                    return;
                }

                var text = creditsTexts.Dequeue();

                if (creditsTexts.Count == 0)
                    nextTextTicks = ticks + 9.25 * Game.TicksPerSecond;
                else
                    SetupNextText(1 + creditsTexts.Peek().EmptyLines);

                CreateText(text.Text);                
            }
        }
    }
}
