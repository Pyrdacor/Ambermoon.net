/*
 * Tutorial.cs - Game introduction sequence
 *
 * Copyright (C) 2021-2025  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Immutable;
using System.Linq;
using Ambermoon.Render;
using Ambermoon.UI;
using static Ambermoon.Game;

namespace Ambermoon
{
    internal class Tutorial
    {
        const int MobileButtonAreaX = 202;
        const int MobileButtonAreaY = 37 + 92;
        const int MobileButtonAreaWidth = 108;
        const int MobileButtonAreaHeight = 71;
        const double MobileButtonAreaFactorX = MobileButtonAreaWidth / 1526.0;
        const double MobileButtonAreaFactorY = MobileButtonAreaHeight / 994.0;
        readonly int mobileButtonAreaIconWidth = Util.Round(MobileButtonAreaFactorX * 270);
        readonly int mobileButtonAreaIconHeight = Util.Round(MobileButtonAreaFactorY * 276);
        readonly int mobileButtonAreaArrowIconX = MobileButtonAreaX + Util.Round(MobileButtonAreaFactorX * 1116.0);
        readonly int mobileButtonAreaArrowIconY = MobileButtonAreaY + Util.Round(MobileButtonAreaFactorY * 528.0);
        readonly int mobileButtonAreaEyeIconX = MobileButtonAreaX + Util.Round(MobileButtonAreaFactorX * 136.0);
        readonly int mobileButtonAreaEyeIconY = MobileButtonAreaY + Util.Round(MobileButtonAreaFactorY * 186.0);
        Rect MobileButtonAreaArrowIconArea => new(mobileButtonAreaArrowIconX, mobileButtonAreaArrowIconY, mobileButtonAreaIconWidth, mobileButtonAreaIconHeight);
        Rect MobileButtonAreaEyeIconArea => new(mobileButtonAreaEyeIconX, mobileButtonAreaEyeIconY, mobileButtonAreaIconWidth, mobileButtonAreaIconHeight);

        static readonly ImmutableDictionary<GameLanguage, string> introductionTooltips = new Dictionary<GameLanguage, string>
		{
			{ GameLanguage.German, "Tutorial" },
			{ GameLanguage.English, "Tutorial" },
			{ GameLanguage.French, "Tutoriel" },
			{ GameLanguage.Polish, "Tutoriál" },
			{ GameLanguage.Czech, "Poradnik" }
		}.ToImmutableDictionary();
		static readonly ImmutableDictionary<GameLanguage, string> introduction = new Dictionary<GameLanguage, string>
		{
			{ GameLanguage.German, "Hi ~SELF~ und willkommen zum Ambermoon Remake.^^Möchtest du eine kleine Einführung?" },
			{ GameLanguage.English, "Hi ~SELF~ and welcome to the Ambermoon Remake.^^Do you need a little introduction?" },
			{ GameLanguage.French, "Bonjour ~SELF~ et bienvenue sur Ambermoon Remake.^^Avez-vous besoin d'une petite introduction ?" },
			{ GameLanguage.Polish, "Cześć ~SELF~, witaj w Ambermoon Remake.^^Potrzebujesz małego wprowadzenia?" },
			{ GameLanguage.Czech, "Ahoj ~SELF~ a vítej v remaku původního Ambermoon.^^Potřebuješ hru trochu představit?" }
		}.ToImmutableDictionary();
		static readonly ImmutableDictionary<GameLanguage, string[]> tips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Tip 1
                "Die Schaltflächen am unteren rechten Bildschirmrand enthalten sehr viele Funktionen " +
                "des Spiels. Wenn du dich auf dem Hauptbildschirm befindest, kannst du eine zweite " +
                "Belegung der Schaltflächen nutzen indem du auf den Bereich rechtsklickst oder die " +
                "Enter-Taste benutzt.",
                // Tip 2
                "Du kannst auch das NumPad auf der Tastatur nutzen um die Schaltflächen auszulösen." +
                "Die Anordnung der Tasten entspricht den Schaltflächen im Spiel. Mit der Taste 7 auf " +
                "dem NumPad würde so die Schaltfläche links oben (das Auge) ausgelöst.",
                // Tip 3
                "Im oberen Bereich siehst du die Spielerportraits. Du kannst die Portraits anklicken um " +
                "den aktiven Spieler auszuwählen. Per Rechtsklick gelangst du ins Inventar. " +
                "Die Tasten 1-6 selektieren ebenfalls den Charakter, und F1-F6 öffnen das jeweilige Inventar.",
                // Tip 4
                "Du kannst dich mit der Maus, den Tasten W, A, S, D oder auch den Pfeiltasten auf der Map "+
                "bewegen. In 2D kannst du per Rechtsklick auf die Map den Cursor umschalten und aus ihm " +
                "einen Aktionscursor machen, mit dem du Dinge untersuchen oder berühren oder aber mit " +
                "NPCs sprechen kannst.",
                // End
                "Ich bin nun still und wünsche dir viel Spaß beim Spielen von Ambermoon!"
            } },
            { GameLanguage.English, new string[]
            {
                // Tip 1
                "The buttons in the lower right area of the screen provide many useful functions of " +
                "the game. If you are on the main screen you can toggle the buttons by pressing the " +
                "right mouse button while hovering the area or hitting the Return key. It will unlock " +
                "additional functions.",
                // Tip 2
                "You can also use the NumPad on your keyboard to control those buttons. The layout " +
                "is exactly as the in-game buttons. So hitting the key 7 will be equivalent to pressing " +
                "the upper left button (the eye).",
                // Tip 3
                "In the upper area you see the character portraits. You can click on them to select the " +
                "active player or right click them to open the inventories. The keyboard keys 1-6 will select " +
                "a player as well and keys F1-F6 will open the inventories.",
                // Tip 4
                "You can move on maps by using the mouse, keys W, A, S, D or the cursor keys. In 2D you can " +
                "right click on the map to change the cursor into an action cursor to interact with objects " +
                "or characters like NPCs.",
                // End
                "Now I'm quiet. Have fun playing Ambermoon!"
            } },
            { GameLanguage.French, new string[]
            {
                // Tip 1
                "Les boutons situés dans la partie inférieure droite de l'écran permettent d'accéder à " +
                "de nombreuses fonctions utiles du jeu. Si vous êtes sur l'écran principal, vous pouvez " +
                "faire basculer les boutons en appuyant sur le bouton droit de la souris tout en survolant " +
                "la zone ou en appuyant sur la touche Retour. Cela révèle des fonctions supplémentaires.",
                // Tip 2
                "Vous pouvez également utiliser le pavé numérique de votre clavier pour contrôler ces boutons." +
                "La disposition est exactement la même que celle des boutons du jeu. Ainsi, appuyer sur la " +
                "touche 7 équivaudra à appuyer sur le bouton supérieur gauche (l'œil).",
                // Tip 3
                "Dans la partie supérieure, vous voyez les portraits des personnages. Vous pouvez cliquer sur " +
                "eux pour sélectionner le joueur actif ou faire un clic droit pour ouvrir les inventaires." +
                "Les touches 1 à 6 du clavier permettent de sélectionner un joueur et les touches F1 à F6 " +
                "ouvrent les inventaires.",
                // Tip 4
                "Vous pouvez vous déplacer sur les cartes à l'aide de la souris, des touches W, A, S, D ou " +
                "des touches du curseur. En 2D, vous pouvez faire un clic droit sur la carte pour transformer " +
                "le curseur en curseur d'action afin d'interagir avec des objets ou des personnages comme les PNJ.",
                // End
                "Maintenant, je suis silencieux. Amusez-vous bien avec Ambermoon !"
            } },
            { GameLanguage.Polish, new string[]
            {
                // Tip 1
                "Przyciski w prawym dolnym rogu ekranu zapewniają wiele przydatnych funkcji " +
                "w grze. Na głównym ekranie można je przełączać , naciskając prawy " +
                "przycisk myszki nad ich obszarem lub wciskając klawisz Return. To pokaże " +
                "dodatkowe funkcje.",
                // Tip 2
                "Do obsługi tych przycisków możesz też użyć klawiatury numerycznej. Układ " +
                "jest dokładnie taki sam jak przycisków w grze. Tak więc klawisz 7 odpowiada " +
                "górnemu lewemu przyciskowi (oko).",
                // Tip 3
                "W górnej części znajdują się portrety postaci. Kliknięcie w jeden z nich wybiera " +
                "aktywną postać a prawy klik otwiera ekwipunek. Z klawiatury, klawisze 1-6 wybierają " +
                "aktywną postać a F1-F6 otworzą ekwipunek.",
                // Tip 4
                "Po mapie możesz się poruszać używając myszy, klawiszy W, A, S, D lub strzałek." +
                "W widoku 2D prawy klik na mapie zmienia kursor w ikonę interakcji z obiektami " +
                "lub postaciami takimi jak NPC.",
                // End
                "Teraz zamilknę. baw się dobrze grając w Ambermoon!"
            } },
            { GameLanguage.Czech, new string[]
            {
				// Tip 1
	            "Ikony v pravé dolní části obrazovky poskytují mnoho užitečných funkcí ve hře. " +
	            "Pokud jsi na hlavní obrazovce, můžeš přepínat zobrazení ikon stiskem " +
	            "pravého tlačítka myši po najetí na oblast, nebo stiskem klávesy Enter. Tím se odemknou " +
	            "další funkce.",
				// Tip 2
	            "Ikony lze ovládat také pomocí numerické klávesnice. Rozložení " +
	            "je přesně takové, jaké jsou plochy ikon ve hře. Takže stisknutí klávesy 7 bude ekvivalent stisknutí " +
	            "levého horního tlačítka (oka).",
				// Tip 3
	            "V horní části se zobrazují portréty postav. Kliknutím na ně, můžeš vybrat " +
                "aktivního hrdinu a pomocí pravého tlačítka otevřeš inventář. Klávesy 1-6 vyberou " +
				"postavu a klávesy F1-F6 otevírají inventář.",
				// Tip 4
	            "Na mapě se můžeš pohybovat pomocí myši, kláves W, A, S, D nebo kurzorových kláves. Ve 2D můžeš " +
	            "kliknutím pravým tlačítkem myši na mapě, změnit kurzor na akční kurzor pro interakci s objekty, " +
	            "nebo postavami, jako jsou NPC.",
				// End
	            "Teď už budu zticha. Bav se při hraní Ambermoonu!"
			} }
		}.ToImmutableDictionary();
		static readonly ImmutableDictionary<GameLanguage, string[]> mobileTips = new Dictionary<GameLanguage, string[]>
		{
			{ GameLanguage.German, new string[]
			{
                // Tip 1
                "Die Schaltflächen am unteren rechten Bildschirmrand enthalten sehr viele Funktionen " +
				"des Spiels. Wenn du dich auf dem Hauptbildschirm befindest, kannst du weitere " +
				"Schaltflächen einblenden, indem du die Schaltfläche in der unteren rechten Ecke " +
				"antippst. Es gibt 3 Schaltflächen-Seiten, die du so durchschalten kannst.",
                // Tip 2
                "Mit dem Steuerkreuz kannst du dich bewegen. Ein kurzes Antippen der Pfeile bewegt " +
                "den Charakter einen Schritt in die gewünschte Richtung. Wenn du den Finger auf der " +
                "Mitte des Steuerkreuzes gedrückt hälst und ihn dann bewegst, kannst du kontinuierlich " +
                "in jede Richtung laufen.",
                // Tip 3
                "Wenn du eine Aktion wie das Auge wählst, siehst du über deinem Charakter " +
				"ein Symbol. Wenn du dann auf der Karte ein Objekt antippst, wird der aktive " +
				"Charakter mit diesem Objekt interagieren. Diese Aktionen haben aber eine " +
                "begrenzte Reichweite!",
                // Tip 4
                "Du kannst auf der Karte deinen Finger gedrückt halten. Dies ermöglicht eine direkte " +
                "Interaktion mit Objekten. Auch das hat natürlich eine begrenzte Reichweite, also solltest " +
                "du nah am Zielobjekt stehen. In 3D-Bereichen ist dies ebenfalls mit Objekten vor dir möglich.",
                // Tip 5
                "Im oberen Bereich siehst du die Spielerportraits. Du kannst die Portraits antippen um " +
				"den aktiven Spieler auszuwählen. Wenn du den Finger gedrückt hälst gelangst du ins Inventar.",
                // Tip 6
                "Wenn du diese Einführung nochmals sehen möchtest, kannst du ein neues Spiel starten und " +
                "dort den Schalter für das Tutorial aktivieren.",
                // End
                "Ich bin nun still und wünsche dir viel Spaß beim Spielen von Ambermoon!"
			} },
			{ GameLanguage.English, new string[]
			{
                // Tip 1
                "The buttons at the bottom right of the screen contain many of the game's functions. " +
                "When you are on the main screen, you can show additional buttons by tapping the button in " +
                "the lower right corner. There are 3 button pages that you can cycle through.",
                // Tip 2
                "You can move using the D-pad. A short tap on an arrow moves the character one step in the " +
                "chosen direction. If you hold your finger on the center of the D-pad and then move it, you " +
                "can walk continuously in any direction.",
                // Tip 3
                "When you choose an action such as the eye, you will see an icon above your character. When " +
                "you then tap an object on the map, the active character will interact with it. However, " +
                "these actions have a limited range!",
                // Tip 4
                "You can keep your finger pressed on the map. This enables direct interaction with objects. " +
                "This also has a limited range, so you should stand close to the target object. In 3D areas, " +
                "this is also possible with objects in front of you.",
                // Tip 5
                "At the top you can see the player portraits. You can tap a portrait to select the active " +
                "player. If you hold your finger on a portrait, you will enter the inventory.",
                // Tip 6
                "If you want to see this introduction again, you can start a new game and enable the " +
                "tutorial switch there.",
                // End
                "Now I'm quiet. Have fun playing Ambermoon!"
			} },
			{ GameLanguage.French, new string[]
			{
                // Tip 1
                "Les boutons en bas à droite de l'écran regroupent de nombreuses fonctions du jeu. " +
                "Depuis l'écran principal, tu peux afficher d'autres boutons en appuyant sur celui " +
                "situé dans le coin inférieur droit. Il existe 3 pages de boutons que tu peux faire défiler.",
                // Tip 2
                "Tu peux te déplacer avec la croix directionnelle. Un court appui sur une flèche déplace " +
                "le personnage d'un pas dans la direction choisie. Si tu maintiens ton doigt au centre de " +
                "la croix puis que tu le bouges, tu peux marcher continuellement dans n'importe quelle direction.",
				// Tip 3
				"Lorsque tu choisis une action comme l'œil, une icône apparaît au-dessus de ton personnage. " +
                "En touchant ensuite un objet sur la carte, ton personnage actif interagira avec lui. Toutefois, " +
                "ces actions ont une portée limitée !",
				// Tip 4
				"Tu peux garder ton doigt appuyé sur la carte. Cela permet d'interagir directement avec les objets. " +
                "Là aussi, la portée est limitée, donc tu dois être suffisamment proche de l'objet visé. Dans les " +
                "zones 3D, cela fonctionne également avec les objets devant toi.",
				// Tip 5
				"En haut, tu vois les portraits des personnages. Tu peux toucher un portrait pour sélectionner le " +
                "personnage actif. Si tu maintiens ton doigt dessus, tu accèdes à l'inventaire.",
                // Tip 6
                "Si tu veux revoir cette introduction, tu peux démarrer une nouvelle partie et activer l'option du tutoriel.",
                // End
                "Maintenant, je suis silencieux. Amusez-vous bien avec Ambermoon !"
			} },
			{ GameLanguage.Polish, new string[]
			{
                // Tip 1
			    "Przyciski w prawym dolnym rogu ekranu zawierają wiele funkcji gry. Na ekranie głównym możesz wyświetlić " +
                "dodatkowe przyciski, dotykając przycisku w prawym dolnym rogu. Są 3 strony przycisków, między którymi " +
                "możesz przełączać.",
                // Tip 2
				"Możesz poruszać się za pomocą krzyżaka. Krótkie stuknięcie strzałki przesuwa postać o jeden krok w " +
                "wybranym kierunku. Jeśli przytrzymasz palec na środku krzyżaka i poruszysz nim, możesz chodzić " +
                "nieprzerwanie w dowolnym kierunku.",
                // Tip 3
                "Gdy wybierzesz akcję, np. oko, nad twoją postacią pojawi się ikona. Jeśli następnie stukniesz obiekt " +
                "na mapie, aktywna postać wejdzie z nim w interakcję. Te akcje mają jednak ograniczony zasięg!",
                // Tip 4
                "Możesz przytrzymać palec na mapie. Umożliwia to bezpośrednią interakcję z obiektami. To również ma " +
                "ograniczony zasięg, więc powinieneś stać blisko celu. W obszarach 3D jest to również możliwe z " +
                "obiektami znajdującymi się przed tobą.",
                // Tip 5
                "Na górze widać portrety graczy. Możesz stuknąć portret, aby wybrać aktywnego gracza. Jeśli " +
                "przytrzymasz palec na portrecie, wejdziesz do ekwipunku.",
                // Tip 6
                "Jeśli chcesz ponownie obejrzeć to wprowadzenie, możesz rozpocząć nową grę i tam włączyć samouczek.",
                // End
                "Teraz zamilknę. baw się dobrze grając w Ambermoon!"
			} },
			{ GameLanguage.Czech, new string[]
            {
                // Tip 1
				"Tlačítka v pravém dolním rohu obrazovky obsahují mnoho funkcí hry. Na hlavní obrazovce můžeš zobrazit " +
                "další tlačítka klepnutím na tlačítko v pravém dolním rohu. Jsou zde 3 stránky tlačítek, mezi kterými " +
                "můžeš přepínat.",
                // Tip 2
                "Můžeš se pohybovat pomocí směrového kříže. Krátké klepnutí na šipku posune postavu o jeden krok " +
                "požadovaným směrem. Pokud podržíš prst uprostřed kříže a pohneš jím, můžeš nepřetržitě chodit " +
                "jakýmkoli směrem.",
                // Tip 3
                "Když vybereš akci, například oko, objeví se nad tvojí postavou symbol. Když pak klepneš na objekt " +
                "na mapě, aktivní postava s ním bude interagovat. Tyto akce však mají omezený dosah!",
                // Tip 4
                "Můžeš držet prst na mapě. To umožňuje přímou interakci s objekty. I zde je dosah omezený, takže " +
                "bys měl stát blízko cílového objektu. V 3D oblastech to funguje také s objekty před tebou.",
                // Tip 5
                "Nahoře vidíš portréty hráčů. Klepnutím na portrét vybereš aktivního hráče. Pokud na portrétu " +
                "podržíš prst, otevře se inventář.",
                // Tip 6
                "Pokud chceš toto úvodní vysvětlení vidět znovu, můžeš spustit novou hru a tam zapnout přepínač tutoriálu.",
                // End
	            "Teď už budu zticha. Bav se při hraní Ambermoonu!"
			} },
		}.ToImmutableDictionary();

		readonly Game game;
        readonly IColoredRect[] markers = new IColoredRect[4];
        readonly string[] texts;
        readonly string introductionText;
        readonly DrawTouchFingerHandler drawTouchFingerRequest;

		public Tutorial(Game game, DrawTouchFingerHandler drawTouchFingerRequest)
        {
            this.game = game;
            this.drawTouchFingerRequest = drawTouchFingerRequest;
			var textSource = game.Configuration.IsMobile ? mobileTips : tips;
			texts = textSource.TryGetValue(game.GameLanguage, out var languageTexts)
                ? languageTexts : textSource[GameLanguage.English];
			introductionText = introduction.TryGetValue(game.GameLanguage, out var text)
                ? text : introduction[GameLanguage.English];
		}

		string GetText(int index) => index == 0 ? introductionText : texts[index - 1];

		internal static string GetIntroductionTooltip(GameLanguage language) =>
            introductionTooltips.TryGetValue(language, out var tooltip) ? tooltip : introductionTooltips[GameLanguage.English];

		public void Run(IGameRenderView renderView)
        {
            game.StartSequence();
            game.ShowDecisionPopup(GetText(0), response =>
            {
                if (response == Data.PopupTextEvent.Response.Yes)
                {
                    ShowTips(renderView);
                }
                else
                {
                    game.EndSequence();
				}
            }, 4, 0, TextAlign.Center, false);
        }

        void ShowMarker(IGameRenderView renderView, Rect area)
        {
            var red = new Color(255, 0, 0);
            markers[0] = renderView.ColoredRectFactory.Create(area.Width, 1, red, 15);
            markers[1] = renderView.ColoredRectFactory.Create(1, area.Height - 2, red, 15);
            markers[2] = renderView.ColoredRectFactory.Create(1, area.Height - 2, red, 15);
            markers[3] = renderView.ColoredRectFactory.Create(area.Width, 1, red, 15);

            markers[0].X = area.Left;
            markers[0].Y = area.Top;
            markers[1].X = area.Left;
            markers[1].Y = area.Top + 1;
            markers[2].X = area.Right - 1;
            markers[2].Y = area.Top + 1;
            markers[3].X = area.Left;
            markers[3].Y = area.Bottom - 1;

            var layer = renderView.GetLayer(Layer.UI);

            foreach (var marker in markers)
            {
                marker.Layer = layer;
                marker.Visible = true;
            }
        }

        void HideMarker()
        {
            foreach (var marker in markers)
                marker?.Delete();
        }

        void DrawTouchFinger(int x, int y, bool longPress, Rect clipArea = null, bool behindPopup = false)
        {
            drawTouchFingerRequest?.Invoke(x, y, longPress, clipArea, behindPopup);
		}

        void HideTouchFinger()
        {
			drawTouchFingerRequest?.Invoke(-1, -1, false, null, false);
		}

        void ToggleButtons()
        {
            game.InputEnable = true;
            game.ToggleButtonGridPage();
            game.InputEnable = false;
        }

        void ShowTips(IGameRenderView renderView)
        {
            if (game.Configuration.IsMobile)
                ShowTipChain(renderView, ShowTip1, ShowTip2, ShowTip3, ShowTip4, ShowTip5, ShowTip6, ShowTutorialEnd);
            else
				ShowTipChain(renderView, ShowTip1, ShowTip2, ShowTip3, ShowTip4, ShowTutorialEnd);
		}

        static void ShowTipChain(IGameRenderView renderView, params Action<IGameRenderView, Action>[] tips)
        {
            ShowTipChain(renderView, tips as IEnumerable<Action<IGameRenderView, Action>>);
        }

        static void ShowTipChain(IGameRenderView renderView, IEnumerable<Action<IGameRenderView, Action>> tips)
        {
            var count = tips.Count();

            if (count == 1)
                tips.First()?.Invoke(renderView, null);
            else
                tips.First()?.Invoke(renderView, () => ShowTipChain(renderView, tips.Skip(1)));
        }

        void ShowMessagePopup(int textId, Action closeAction = null, int yOffset = 0)
        {
            game.ShowMessagePopup(GetText(textId), closeAction, TextAlign.Center, 0, new(0, yOffset));
        }

        void ShowTip1(IGameRenderView renderView, Action next)
        {
            int yOffset = 0;

            if (game.Configuration.IsMobile)
            {
                ShowMarker(renderView, new Rect(MobileButtonAreaX - 1, MobileButtonAreaY - 1,
                    MobileButtonAreaWidth, MobileButtonAreaHeight));

                var arrowIconArea = MobileButtonAreaArrowIconArea;
                DrawTouchFinger(arrowIconArea.Center.X + 4, arrowIconArea.Center.Y + 14, false);

                game.HideMobileTouchpadDisableOverlay = true;
                yOffset = -20;
            }
            else
            {
                ShowMarker(renderView, new Rect(Global.ButtonGridX - 1, Global.ButtonGridY - 1,
                    3 * Button.Width + 2, 3 * Button.Height + 2));
            }

            ShowMessagePopup(1, next, yOffset);
        }

        void ShowTip2(IGameRenderView renderView, Action next)
        {
            int yOffset = 0;

            if (game.Configuration.IsMobile)
            {
                HideTouchFinger();
                HideMarker();
                ShowMarker(renderView, new Rect(MobileButtonAreaX - 1, MobileButtonAreaY - 1,
                    MobileButtonAreaWidth, MobileButtonAreaHeight));
                DrawTouchFinger(MobileButtonAreaX + MobileButtonAreaWidth / 2 + 4, MobileButtonAreaY + MobileButtonAreaHeight / 2 + 4, true);

                yOffset = -20;
            }
            else
            {
                ToggleButtons();
            }
                
            ShowMessagePopup(2, next, yOffset);
        }

        void ShowTip3(IGameRenderView renderView, Action next)
        {
            int yOffset = 0;

            HideMarker();

            if (game.Configuration.IsMobile)
            {
                HideTouchFinger();

                var eyeIconArea = MobileButtonAreaEyeIconArea;
                ShowMarker(renderView, new Rect(eyeIconArea.X - 3, eyeIconArea.Y - 3,
                    mobileButtonAreaIconWidth + 6, mobileButtonAreaIconHeight + 6));
                DrawTouchFinger(eyeIconArea.Center.X + 2, eyeIconArea.Center.Y + 16, false);

                yOffset = -20;
            }
            else
            {
                ToggleButtons();
                ShowMarker(renderView, Global.PartyMemberPortraitArea);
            }

            ShowMessagePopup(3, next, yOffset);
        }

        void ShowTip4(IGameRenderView renderView, Action next)
        {
			HideMarker();

            if (game.Configuration.IsMobile)
            {
                DrawTouchFinger(Map2DViewArea.Right - 72, Map2DViewArea.Bottom - 50, false);
                ShowMarker(renderView, new(Map2DViewArea.X + 16 - 2, Map2DViewArea.Y + 32 - 2, 20, 20));
                game.SetClickHandler(next);
                game.InputEnable = false;
                game.CurrentMobileAction = MobileAction.Eye;
            }
            else
            {
                ShowMarker(renderView, Map2DViewArea);
                game.ShowMessagePopup(GetText(4), next);
            }
        }

		void ShowTip5(IGameRenderView renderView, Action next)
        {
            // Mobile only
            game.CurrentMobileAction = MobileAction.None;
            game.InputEnable = true;
            DrawTouchFinger(Map2DViewArea.Center.X, Map2DViewArea.Bottom - 36, true);
            HideMarker();
			ShowMessagePopup(4, () =>
            {
                HideTouchFinger();
                game.ExecuteNextUpdateCycle(next);
            }, -20);
		}

        void ShowTip6(IGameRenderView renderView, Action next)
        {
            // Mobile only            
            DrawTouchFinger(Global.PartyMemberPortraitArea.X + 34, Global.PartyMemberPortraitArea.Y + 32, false, new(0, 0, Global.VirtualScreenHeight, 64), true);
            ShowMarker(renderView, Global.PartyMemberPortraitArea);

            ShowMessagePopup(5, () =>
            {
                game.ExecuteNextUpdateCycle(() =>
                {
                    HideTouchFinger();
                    HideMarker();

                    game.OpenPartyMember(0, true, () =>
                    {
                        game.InputEnable = false;
                        game.SetClickHandler(() =>
                        {
                            game.InputEnable = true;
                            game.CloseWindow(() =>
                            {
                                ShowMessagePopup(6, next);
                            });                            
                        });
                    });
                });
            });
        }

		void ShowTutorialEnd(IGameRenderView renderView, Action next)
        {
            HideMarker();
            game.ShowMessagePopup(GetText(texts.Length), () =>
            {
                game.HideMobileTouchpadDisableOverlay = false;
                next?.Invoke();
            });
        }
    }
}
