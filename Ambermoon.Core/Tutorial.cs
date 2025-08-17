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

using Ambermoon.Render;
using Ambermoon.UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Ambermoon.Game;

namespace Ambermoon
{
    internal class Tutorial
    {
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
				"des Spiels. Wenn du dich auf dem Hauptbildschirm befindest, kannst du eine zweite " +
				"Belegung der Schaltflächen nutzen indem du deinen Finger länger auf der mittleren " +
				"Schaltfläche gedrückt hälst.",
                // Tip 2
                "Wenn du eine Aktion wie das Auge wählst, siehst du über deinem Charakter " +
				"ein Symbol. Wenn du dann auf der Karte ein Objekt antipst " +
				"wird der Spieler mit diesem Objekt interagieren. Diese Aktionen haben aber " +
                "eine begrenzte Reichweite!",
                // Tip 3
                "Im oberen Bereich siehst du die Spielerportraits. Du kannst die Portraits antippen um " +
				"den aktiven Spieler auszuwählen. Wenn du den Finger gedrückt hälst gelangst du ins Inventar.",
                // Tip 4
                "Du kannst auf der Karte deinen Finger gedrückt halten. Dies ermöglicht eine direkte " +
				"Interaktion mit Objekten. Außerdem kannst du so auch laufen, wenn du dann den Finger bewegst. " +
				"Und zum Schluss gibt es noch die Bewegungstasten.",
                // Tip 5
                "Wenn du diese kurz antippst, bewegst du dich ein kleines Stück. Wenn du deinen Finger " +
				"gedrückt hälst, läufst du kontinuierlich. Du kannst dabei auch die Richtung ändern, " +
                "indem du deinen Finger auf die anderen Tasten bewegst.",
                // Tip 6
                "Wenn du diese Einführung nochmals sehen möchtest, kannst du ein neues Spiel starten und " +
                "den Schalter für das Tutorial aktivieren.",
                // End
                "Ich bin nun still und wünsche dir viel Spaß beim Spielen von Ambermoon!"
			} },
			{ GameLanguage.English, new string[]
			{
                // Tip 1
                "The buttons in the lower right area of the screen provide many useful functions of " +
				"the game. If you are on the main screen you can toggle the buttons by holding your " +
				"finger pressed on the center button. It will unlock additional functions.",
                // Tip 2
                "If you choose actions like the eye, you will see some symbol over the character's " +
				"head. When you tap some object on the map, the player tries to interact with it. But " +
				"those actions have a limited range!",
                // Tip 3
                "In the upper area you see the character portraits. You can tap on them to select the " +
				"active player or long press them to open the inventories.",
                // Tip 4
                "You can long press on the map. This allows direct interaction with objects. You can also " +
				"walk this way if you move your finger afterwards. And then we also have the movement buttons.",
                // Tip 5
                "If you briefly tap a button, you will move a small step but if you long press a button " +
                "you will move continously. You can also change direction by moving your finger to the " +
				"other buttons while you hold your finger down.",
                // Tip 6
                "If you want to see this introduction again, just start a new game and activate the option " +
                "for the tutorial there.",
                // End
                "Now I'm quiet. Have fun playing Ambermoon!"
			} },
			{ GameLanguage.French, new string[]
			{
                // Tip 1
                "Les boutons dans la zone en bas à droite de l'écran fournissent plusieurs fonctions utiles " +
				"du jeu. Si vous êtes sur l'écran principal vous pouvez changer les boutons en maintenant " +
				"votre doigt pressé sur le bouton central. Ca débloquera les fonctions additionnelles.",
                // Tip 2
                "Si vous choisissez des actions comme l'oeil, vous verrez des symbols sur la tête du personnage. " +
                "Lorsque vous cliquez sur certains objets sur la carte, le joueur essaie d'intéragir avec. " +
				"Mais ces actions ont une portée limitée!",
				// Tip 3
				"Dans la zone haute vous voyez les portraits des personnages. Vous pouvez cliquer dessus pour " +
                "sélectionner le joueur actif ou avec une pression longue pour ouvrir les inventaires.",
				// Tip 4
				"Vous pouvez faire une pression longue sur la carte. Ceci permet une intéraction directe avec " +
                "les objets. Vous pouvez aussi marcher de cette façon si vous déplacez votre doigt après coup. " +
				"Et nous avons également les boutons de mouvement.",
				// Tip 5
				"Si vous appuyez brièvement sur un bouton, vous vous déplacerez d'un petit pas, mais si vous appuyez " +
                "longuement sur un bouton, vous vous déplacerez continuellement. Vous pouvez également changer de " +
                "direction en déplaçant votre doigt vers les autres boutons tout en maintenant votre doigt appuyé.",
                // Tip 6
                "Si vous souhaitez revoir cette introduction, démarrez une nouvelle partie et activez l'option " +
                "du tutoriel à cet endroit.",
                // End
                "Maintenant, je suis silencieux. Amusez-vous bien avec Ambermoon !"
			} },
			{ GameLanguage.Polish, new string[]
			{
                // Tip 1
			    "Przyciski w prawym dolnym rogu ekranu zapewniają wiele przydatnych funkcji gry. Jeśli jesteś " +
				"na ekranie głównym, możesz przełączać przyciski, przytrzymując palec na środkowym przycisku. "+
                "Umożliwia to dostęp do dodatkowych funkcji.",
                // Tip 2
				"Jeśli wybierzesz akcję taką jak oko, zobaczysz symbol nad głową postaci. Po dotknięciu " +
                "jakiegoś obiektu na mapie, gracz próbuje wejść z nim w interakcję. Te działania mają jednak " +
                "ograniczony zasięg!",
                // Tip 3
                "W górnym obszarze znajdują się portrety postaci. Możesz je dotknąć, aby wybrać aktywnego " +
                "gracza lub przytrzymać dłużej, aby otworzyć ekwipunek.",
                // Tip 4
                "Dłuższy nacisk na obszarze mapy pozwala na bezpośrednią interakcję z obiektami. Możesz także " +
				"chodzić w ten sposób, jeśli w trakcie przesuniesz palec. Mamy też przyciski ruchu.",
                // Tip 5
                "Krótkie naciśnięcie przycisku spowoduje niewielki ruch, natomiast długie naciśnięcie przycisku " +
                "spowoduje ciągły ruch. Możesz także zmienić kierunek, przesuwając palec do innych przycisków, " +
                "przytrzymując go.",
                // Tip 6
                "Jeśli chcesz ponownie zobaczyć to wprowadzenie, po prostu rozpocznij nową grę i aktywuj opcję " +
                "samouczka.",
                // End
                "Teraz zamilknę. baw się dobrze grając w Ambermoon!"
			} },
			{ GameLanguage.Czech, new string[]
            {
                // Tip 1
				"Tlačítka v pravém dolním rohu obrazovky poskytují mnoho užitečných funkcí hry. " + 
                "Pokud jste na hlavní obrazovce, můžete tato tlačítka přepínat podržením " +
				"prstu na prostředním tlačítku. Tím odemknete další funkce.",
                // Tip 2
                "Pokud zvolíte akce jako oko, zobrazí se nad postavou symbol hlavy. " +
                "Když klepnete na nějaký objekt na mapě, hráč se s ním pokusí interagovat. " +
                "Ovšem tyto akce mají omezený dosah!",
                // Tip 3
                "V horní části vidíte portréty postav. Můžete na ně klepnout a vybrat " +
                "aktivního hráče, nebo jen dlouze podržet pro otevření inventáře.",
                // Tip 4
                "Na mapě můžete podržet prst. To umožňuje přímou interakci s objekty. Je možné " +
				"tímto způsobem i chodit, pokud poté pohnete prstem. A pak tu máme také pohybová tlačítka.",
                // Tip 5
                "Pokud krátce klepnete na tlačítko, posunete se o malý krok, ale pokud tlačítko stisknete " +
                "dlouze, budete se pohybovat nepřetržitě. Směr můžete měnit také tak, že při podržení prstu " +
                "přejdete na jiná tlačítka.",
                // Tip 6
                "Pokud chcete tento úvod vidět znovu, spusťte novou hru a aktivujte v ní možnost výukového " +
                "programu.",
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

        void ShowTip1(IGameRenderView renderView, Action next)
        {
            if (game.Configuration.IsMobile)
            {
                ShowMarker(renderView, new Rect(Global.ButtonGridX + Button.Width - 1, Global.ButtonGridY + Button.Height - 1,
                    Button.Width + 2, Button.Height + 2));
                DrawTouchFinger(Global.ButtonGridX + Button.Width, Global.ButtonGridY + Button.Height * 2, true);
            }
            else
            {
                ShowMarker(renderView, new Rect(Global.ButtonGridX - 1, Global.ButtonGridY - 1,
                    3 * Button.Width + 2, 3 * Button.Height + 2));
            }
			game.ShowMessagePopup(GetText(1), next);
        }

        void ShowTip2(IGameRenderView renderView, Action next)
        {
            ToggleButtons();
            if (game.Configuration.IsMobile)
            {
                HideTouchFinger();
                HideMarker();
				ShowMarker(renderView, new Rect(Global.ButtonGridX - 1, Global.ButtonGridY - 1,
				    Button.Width + 2, Button.Height + 2));
			}
			game.ShowMessagePopup(GetText(2), next);
        }

        void ShowTip3(IGameRenderView renderView, Action next)
        {
			HideMarker();

            if (game.Configuration.IsMobile)
            {
                game.CurrentMobileAction = MobileAction.Eye;
                DrawTouchFinger(Map2DViewArea.Right - 72, Map2DViewArea.Bottom - 50, false);
				ShowMarker(renderView, new(Map2DViewArea.X + 16 - 2, Map2DViewArea.Y + 32 - 2, 20, 20));
                game.SetClickHandler(next);
			}
            else
            {
                ToggleButtons();
                ShowMarker(renderView, Global.PartyMemberPortraitArea);
                game.ShowMessagePopup(GetText(3), next);
            }
        }

        void ShowTip4(IGameRenderView renderView, Action next)
        {
			HideMarker();

			if (game.Configuration.IsMobile)
            {
				game.CurrentMobileAction = MobileAction.None;
				DrawTouchFinger(Global.PartyMemberPortraitArea.X + 34, Global.PartyMemberPortraitArea.Y + 32, false, new(0, 0, Global.VirtualScreenHeight, 64), true);
				ToggleButtons();
				ShowMarker(renderView, Global.PartyMemberPortraitArea);
				game.ShowMessagePopup(GetText(3), next);
			}
            else
            {
                ShowMarker(renderView, Map2DViewArea);
                game.ShowMessagePopup(GetText(4), next);
            }
        }

		void ShowTip5(IGameRenderView renderView, Action next)
        {
			DrawTouchFinger(Map2DViewArea.Center.X, Map2DViewArea.Bottom - 36, true);
            HideMarker();
			game.ShowMessagePopup(GetText(4), () =>
            {
                HideTouchFinger();
                game.ExecuteNextUpdateCycle(next);
            });
		}

		void ShowTip6(IGameRenderView renderView, Action next)
		{
            ShowMarker(renderView, Global.ButtonGridArea.CreateModified(-1, -1, 2, 2));
			var center = Global.ButtonGridArea.Center;
			DrawTouchFinger(center.X, center.Y - Button.Height + 8, true);
            uint? buttonDelay = null;
            game.ShowMessagePopup(GetText(5), () =>
            {
                game.ExecuteNextUpdateCycle(() =>
                {
                    ReleaseButton(1, buttonDelay);
                    buttonDelay = PressButton(5);
                    HideTouchFinger();
                    DrawTouchFinger(center.X + Button.Width + 8, center.Y + 4, true);
                    game.SetClickHandler(() =>
                    {
                        ReleaseButton(5, buttonDelay);
                        HideTouchFinger();
                        HideMarker();
                        game.ShowMessagePopup(GetText(6), next);
                    });
                });
            });
            buttonDelay = PressButton(1); // Do this after ShowMessagePopup as it would release the buttons again
		}

        uint? PressButton(int index)
        {
            var button = game.Layout.GetButton(index);
            var oldContinuousActionDelayInTicks = button.ContinuousActionDelayInTicks;
            button.ContinuousActionDelayInTicks = null;
			button.Pressed = true;
            return oldContinuousActionDelayInTicks;
		}

		void ReleaseButton(int index, uint? continuousActionDelayInTicks)
		{
			var button = game.Layout.GetButton(index);
			button.ContinuousActionDelayInTicks = continuousActionDelayInTicks;
			button.Release(true);
		}

		void ShowTutorialEnd(IGameRenderView renderView, Action next)
        {
 			HideMarker();
            game.ShowMessagePopup(GetText(texts.Length), next);
        }
    }
}
