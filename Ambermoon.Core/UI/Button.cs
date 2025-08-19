﻿/*
 * Button.cs - UI button implementation
 *
 * Copyright (C) 2020-2023  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Ambermoon.UI
{
    public class Button
    {
        public enum TooltipType
        {
            Exit,
            Quit,
            Options,
            Save,
            Load,
            New,
            Eye,
            Hand,
            Mouth,
            Transport,
            Spells,
            Camp,
            Automap,
            BattlePositions,
            Wait,
            Stats,
            Inventory,
            UseItem,
            ExamineItem,
            StoreItem,
            StoreGold,
            StoreFood,
            DropItem,
            DropGold,
            DropFood,
            GiveGold,
            GiveFood,
            DistributeGold,
            DistributeFood,
            Buy,
            Sell,
            Train,
            HealPerson,
            RemoveCurse,
            HealCondition,
            RestInn,
            IdentifyEquipment,
            IdentifyInventory,
            Repair,
            Recharge,
            ReadScroll,
            Sleep,
            Lockpick,
            FindTrap,
            DisarmTrap,
            SolveRiddle,
            HearRiddle,
            Say,
            ShowItemToNPC,
            GiveItemToNPC,
            GiveGoldToNPC,
            GiveFoodToNPC,
            AskToJoin,
            AskToLeave,
            Flee,
            StartBattleRound,
            BattleMove,
            BattleAdvance,
            BattleAttack,
            BattleDefend,
            BattleCast,
            IdentifyScroll
        }

        static readonly ImmutableDictionary<GameLanguage, string[]> tooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
                {
                    "Schließen",
                    "Spiel beenden",
                    "Optionen",
                    "Speichern",
                    "Laden",
                    "Neues Spiel",
                    "Untersuchen",
                    "Berühren",
                    "Sprechen",
                    "Transport",
                    "Zaubersprüche",
                    "Lager",
                    "Karte",
                    "Kampfpositionen",
                    "Warten",
                    "Charakterinfo",
                    "Inventar",
                    "Gegenstand benutzen",
                    "Gegenstand untersuchen",
                    "Gegenstand in Truhe packen",
                    "Gold in Truhe packen",
                    "Rationen in Truhe packen",
                    "Gegenstand wegwerfen",
                    "Gold wegwerfen",
                    "Rationen wegwerfen",
                    "Gold überreichen",
                    "Rationen überreichen",
                    "Gold aufteilen",
                    "Rationen aufteilen",
                    "Kaufen",
                    "Verkaufen",
                    "Trainieren",
                    "Person heilen",
                    "Fluch entfernen",
                    "Kondition heilen",
                    "Übernachten",
                    "Ausrüstung identifizieren",
                    "Gegenstände identifizieren",
                    "Gegenstand reparieren",
                    "Gegenstand laden",
                    "Spruchrolle lesen",
                    "Schlafen",
                    "Schloss knacken",
                    "Falle finden",
                    "Falle entschärfen",
                    "Antwort eingeben",
                    "Rätsel anhören",
                    "Etwas sagen",
                    "Gegenstand zeigen",
                    "Gegenstand geben",
                    "Gold geben",
                    "Rationen geben",
                    "In Gruppe einladen",
                    "Aus Gruppe entlassen",
                    "Flüchten",
                    "Kampfrunde starten",
                    "Bewegen",
                    "Vorrücken",
                    "Angreifen",
                    "Verteidigen",
                    "Zaubern",
                    "Nötige Spruchlernpunkte ermitteln"
                }
            },
            { GameLanguage.English, new string[]
                {
                    "Close",
                    "Quit game",
                    "Options",
                    "Save",
                    "Load",
                    "New game",
                    "Examine",
                    "Touch",
                    "Speak",
                    "Transport",
                    "Spell book",
                    "Camp",
                    "Map",
                    "Battle positions",
                    "Wait",
                    "Character stats",
                    "Inventory",
                    "Use item",
                    "Examine item",
                    "Store item in chest",
                    "Store gold in chest",
                    "Store food in chest",
                    "Drop item",
                    "Drop gold",
                    "Drop food",
                    "Hand over gold",
                    "Hand over food",
                    "Distribute gold",
                    "Distribute food",
                    "Buy",
                    "Sell",
                    "Train",
                    "Heal person",
                    "Remove curse",
                    "Heal condition",
                    "Stay for the night",
                    "Identify equipment",
                    "Identify items",
                    "Repair item",
                    "Recharge item",
                    "Read spell scroll",
                    "Sleep",
                    "Lockpick",
                    "Find trap",
                    "Disarm trap",
                    "Answer",
                    "Rehear riddle",
                    "Say something",
                    "Show item",
                    "Give item",
                    "Give gold",
                    "Give food",
                    "Ask to join",
                    "Ask to leave",
                    "Flee",
                    "Start round",
                    "Move",
                    "Advance",
                    "Attack",
                    "Defend",
                    "Cast spell",
                    "Determine required spell learning points"
                }
            },
            { GameLanguage.French, new string[]
                {
                    "Fermer",
                    "Quitter le jeu",
                    "Options",
                    "Sauvegarder le jeu",
                    "Charger le jeu",
                    "Nouveau jeu",
                    "Examiner",
                    "Toucher",
                    "Parler",
                    "Transport",
                    "Livre de sorts",
                    "Camp",
                    "Carte",
                    "Positions de combat",
                    "Attendre",
                    "Statistiques des personnages",
                    "Inventaire",
                    "Utiliser un objet",
                    "Examiner un objet",
                    "Stocker un objet dans le coffre",
                    "Stocker l'or dans le coffre",
                    "Stocker les aliments dans un coffre",
                    "Jeter un objet",
                    "Jeter l'or",
                    "Jeter les aliments",
                    "Donner de l'or",
                    "Donner des aliments",
                    "Distribuer l'or",
                    "Distribuer des aliments",
                    "Acheter",
                    "Vendre",
                    "Former",
                    "Guérir la personne",
                    "Lever une malédiction",
                    "Guérir l'état de santé",
                    "Rester pour la nuit",
                    "Identifier l'équipement",
                    "Identifier les objets",
                    "Réparer un objet",
                    "Recharger un objet",
                    "Lire le parchemin des sorts",
                    "Sommeil",
                    "Crocheter une serrure",
                    "Trouver un piège",
                    "Désarmer le piège",
                    "Répondre",
                    "Réécouter l'énigme",
                    "Parler",
                    "Afficher l'objet",
                    "Donner l'objet",
                    "Donner de l'or",
                    "Donner des aliments",
                    "Demander à rejoindre le groupe",
                    "Demande de quitter le groupe",
                    "Fuir",
                    "Démarrer les actions",
                    "Déplacer",
                    "Advancer",
                    "Attaquer",
                    "Défendre",
                    "Lancer un sort",
                    "Déterminer les points d'apprentissage des sorts"
                }
            },
            { GameLanguage.Polish, new string[]
                {
                    "Zamknij",
                    "Zakończ grę",
                    "Opcje",
                    "Zapisz",
                    "Wczytaj",
                    "Nowa gra",
                    "Sprawdź",
                    "Dotknij",
                    "Rozmawiaj",
                    "Transport",
                    "Księga czarów",
                    "Obóz",
                    "Mapa",
                    "Pozycja w walce",
                    "Czekaj",
                    "Statystyki postaci",
                    "Ekwipunek",
                    "Użyj przedmiot",
                    "Sprawdź przedmiot",
                    "Schowaj przedmiot w skrzyni",
                    "Schowaj złoto w skrzyni",
                    "Schowaj żywność w skrzyni",
                    "Wyrzuć przedmiot",
                    "Wyrzuć złoto",
                    "Wyrzuć żywność",
                    "Przekaż złoto",
                    "Przekaż żywność",
                    "Rozdziel złoto",
                    "Rozdziel żywność",
                    "Kup",
                    "Sprzedaj",
                    "Trenuj",
                    "Ulecz osobę",
                    "Usuń klątwę",
                    "Ulecz stan",
                    "Zostań na noc",
                    "Zidentyfikuj sprzęt",
                    "Zidentyfikuj przedmioty",
                    "Napraw przedmiot",
                    "Naładuj przedmiot",
                    "Odczytaj zwój zaklęcia",
                    "Śpij",
                    "Otwórz zamek",
                    "Znajdź pułapkę",
                    "Rozbrój pułapkę",
                    "Odpowiedz",
                    "Powtórz zagadkę",
                    "Powiedz coś",
                    "Pokaż przedmiot",
                    "Daj przedmiot",
                    "Daj złoto",
                    "Daj żywność",
                    "Poproś o dołączenie",
                    "Poproś o odejście",
                    "Uciekaj",
                    "Rozpocznij rundę",
                    "Zmień pozycję",
                    "Naprzód",
                    "Atak",
                    "Obrona",
                    "Żuć zaklęcie",
                    "Określ wymagane punkty nauki zaklęć"
                }
            },
            { GameLanguage.Czech, new string[]
                {
                    "Zavřít",
                    "Ukončit hru",
                    "Nastavení",
                    "Uložit",
                    "Načíst",
                    "Nová hra",
                    "Zkoumat",
                    "Dotknout",
                    "Mluvit",
                    "Přepravit",
                    "Kniha kouzel",
                    "Odpočinek",
                    "Mapa",
                    "Bojové pozice",
                    "Čekat",
                    "Statistiky postavy",
                    "Inventář",
                    "Použít předmět",
                    "Zkoumat předmět",
                    "Uložit předmět do truhly",
                    "Uložit zlato do truhly",
                    "Uložit jídlo do truhly",
                    "Zahodit předmět",
                    "Zahodit zlato",
					"Vyhodit jídlo",
                    "Dát zlato",
                    "Dát jídlo",
					"Rozděl zlato",
					"Rozděl jídlo",
                    "Nákup",
                    "Prodej",
                    "Trénink",
                    "Vyléčit osobu",
                    "Odstranit kletbu",
                    "Vyléčit stav",
                    "Zůstat na noc",
                    "Identifikovat výbavu",
                    "Identifikovat předmět",
                    "Opravit předmět",
                    "Dobít předmět",
                    "Přečíst svitek kouzla",
                    "Spánek",
                    "Odemknout",
                    "Najít past",
                    "Zneškodnit past",
                    "Odpověď",
                    "Zopakovat hádanku",
                    "Řekni něco",
                    "Ukaž předmět",
					"Dát předmět",
					"Dát zlato",
					"Dát jídlo",
                    "Požádat o přijetí",
                    "Požádat o odchod",
                    "Utéct",
                    "Začít kolo",
                    "Pohyb",
                    "Postup",
                    "Útok",
                    "Parírování",
                    "Seslat kouzlo",
                    "Určit body kouzel"
                }
            }
        }.ToImmutableDictionary();

        public static string GetTooltip(GameLanguage gameLanguage, TooltipType type) => tooltips[gameLanguage][(int)type];

        public const int ButtonReleaseTime = 250;
        public const int Width = 32;
        public const int Height = 17;
        readonly IGameRenderView renderView;
        public Rect Area { get; }
        public bool ToggleButton { get; set; } = false;
        ButtonType buttonType = ButtonType.Empty;
        readonly ILayerSprite frameSprite; // 32x17
        readonly ILayerSprite disableOverlay;
        readonly ILayerSprite iconSprite; // 32x13
        readonly ITextureAtlas textureAtlas;
        readonly int tooltipYOffset = 0;
        bool pressed = false;
        bool released = true;
        bool rightMouse = false;
        bool disabled = false;
        bool visible = true;
        DateTime pressedTime = DateTime.MinValue;
        uint lastActionTimeInTicks = 0;
        uint? continuousActionDelayInTicks = null;
        uint? initialContinuousActionDelayInTicks = null;
        readonly IRenderText tooltip = null;
        string tooltipText = null;

        public Button(IGameRenderView renderView, Position position,
            TextureAtlasManager textureAtlasManager = null)
        {
            this.renderView = renderView;
            Area = new Rect(position, new Size(Width, Height));
            byte paletteIndex = (byte)(renderView.GraphicProvider.PrimaryUIPaletteIndex - 1);

            frameSprite = renderView.SpriteFactory.CreateLayered(Width, Height, 3);
            disableOverlay = renderView.SpriteFactory.CreateLayered(Width, Height - 6, 7);
            iconSprite = renderView.SpriteFactory.CreateLayered(Width, Height - 4, 5);

            var layer = renderView.GetLayer(Layer.UI);
            frameSprite.Layer = layer;
            disableOverlay.Layer = layer;
            iconSprite.Layer = layer;

            textureAtlas = (textureAtlasManager ?? TextureAtlasManager.Instance).GetOrCreate(Layer.UI);
            frameSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.ButtonFrame));
            disableOverlay.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(UIGraphic.ButtonDisabledOverlay));
            iconSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetButtonGraphicIndex(ButtonType.Empty));

            frameSprite.PaletteIndex = paletteIndex;
            disableOverlay.PaletteIndex = paletteIndex;
            iconSprite.PaletteIndex = paletteIndex;

            frameSprite.X = position.X;
            frameSprite.Y = position.Y;
            disableOverlay.X = position.X;
            disableOverlay.Y = position.Y + 3;
            iconSprite.X = position.X;
            iconSprite.Y = position.Y + 2;

            frameSprite.Visible = true;
            disableOverlay.Visible = false;
            iconSprite.Visible = true;

            tooltipYOffset = Global.GlyphLineHeight - renderView.FontProvider.GetFont().GlyphHeight;
            var text = renderView.TextProcessor.CreateText("");
            tooltip = renderView.RenderTextFactory.Create((byte)(renderView.GraphicProvider.DefaultTextPaletteIndex - 1),
                renderView.GetLayer(Layer.Text), text, Data.Enumerations.Color.White, true);
            tooltip.DisplayLayer = 254;
            tooltip.Visible = false;
        }

        public string Tooltip
        {
            get => tooltipText;
            set
            {
                tooltipText = value;

                if (string.IsNullOrWhiteSpace(tooltipText))
                    tooltip.Visible = false;
            }
        }

        public Data.Enumerations.Color TooltipColor
        {
            get;
            set;
        } = Data.Enumerations.Color.White;

        public Position TooltipOffset
        {
            get;
            set;
        } = null;

        public void SetTooltip(string text)
        {
            bool visible = !string.IsNullOrWhiteSpace(text);

            if (visible)
            {
                var offset = TooltipOffset ?? new Position(0, 0);
                tooltip.Text = renderView.TextProcessor.CreateText(text);
                tooltip.TextColor = TooltipColor;
                int width = tooltip.Text.MaxLineSize * Global.GlyphWidth;
                tooltip.X = Math.Min(Global.VirtualScreenWidth - width, offset.X + Area.Center.X - width / 2);
                tooltip.Y = offset.Y + Area.Top - tooltip.Text.LineCount * Global.GlyphLineHeight + 1 + tooltipYOffset;
            }

            tooltip.Visible = visible;
        }

        public void HideTooltip() => SetTooltip(null);

        public byte DisplayLayer
        {
            get => (byte)(frameSprite.DisplayLayer - 3);
            set
            {
                frameSprite.DisplayLayer = (byte)Math.Min(251, value + 3);
                iconSprite.DisplayLayer = (byte)(frameSprite.DisplayLayer + 2);
                disableOverlay.DisplayLayer = (byte)(frameSprite.DisplayLayer + 4);
            }
        }

        public void Destroy()
        {
            frameSprite?.Delete();
            disableOverlay?.Delete();
            iconSprite?.Delete();
            tooltip?.Delete();
        }

        public ButtonType ButtonType
        {
            get => buttonType;
            set
            {
                if (buttonType == value)
                    return;

                buttonType = value;
                Pressed = false;
                released = true;

                iconSprite.TextureAtlasOffset = textureAtlas.GetOffset(Graphics.GetButtonGraphicIndex(buttonType));
            }
        }

        public Action LeftClickAction
        {
            get;
            set;
        }

        public Action RightClickAction
        {
            get;
            set;
        }

        public Func<CursorType> CursorChangeAction
        {
            get;
            set;
        }

        /// <summary>
        /// If false the action is only triggered when the mouse
        /// button is released inside the button area after it
        /// was pressed in that area.
        /// 
        /// If true the action is immediately triggered when clicked
        /// in the button area.
        /// </summary>
        public bool InstantAction
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Delay between continuous actions while buttons stays
        /// pressed.
        /// 
        /// null means no continuation.
        /// </summary>
        public uint? ContinuousActionDelayInTicks
        {
            get => initialContinuousActionDelayInTicks;
            set
            {
                initialContinuousActionDelayInTicks = value;
                continuousActionDelayInTicks = value;
            }
        }

        /// <summary>
        /// Only used in conjunction with <see cref="ContinuousActionDelayInTicks"/>.
        /// If set to non-zero value each execution will reduce the delay by the given ticks
        /// down to 1 tick at max.
        /// </summary>
        public uint ContinuousActionDelayReductionInTicks
        {
            get;
            set;
        } = 0;

        public bool Pressed
        {
            get => pressed;
            set
            {
                if (pressed == value)
                    return;

                if (value)
                    released = false;

                pressed = value;
                frameSprite.TextureAtlasOffset =
                    textureAtlas.GetOffset(Graphics.GetUIGraphicIndex(pressed ? UIGraphic.ButtonFramePressed : UIGraphic.ButtonFrame));
                iconSprite.Y = frameSprite.Y + (pressed ? 4 : 2);

                if (!pressed)
                    continuousActionDelayInTicks = initialContinuousActionDelayInTicks;

            }
        }

        public bool Disabled
        {
            get => disabled || buttonType == ButtonType.Empty || !Visible;
            set
            {
                disabled = value;
                disableOverlay.Visible = Visible && disabled;
            }
        }

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                    return;

                visible = value;

                frameSprite.Visible = visible;
                iconSprite.Visible = visible;
                disableOverlay.Visible = visible && disabled;
            }
        }

        public byte PaletteIndex
        {
            get => frameSprite.PaletteIndex;
            set
            {
                frameSprite.PaletteIndex = value;
                disableOverlay.PaletteIndex = value;
                iconSprite.PaletteIndex = value;
            }
        }

		public void LeftMouseUp(Position position, uint currentTicks)
        {
            if (ToggleButton)
                return;

            CursorType? cursorType = null;
            LeftMouseUp(position, ref cursorType, currentTicks);
        }

        public void LeftMouseUp(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled || rightMouse || ToggleButton)
                return;

            if (Pressed && !InstantAction && Area.Contains(position))
            {
                cursorType = ExecuteActions(currentTicks, false);
            }

            released = true;
            Pressed = false;
        }

        public void RightMouseUp(Position position, uint currentTicks)
        {
            if (Disabled || !rightMouse || ToggleButton)
                return;

            if (Pressed && !InstantAction && Area.Contains(position))
            {
                ExecuteActions(currentTicks, true);
            }

            rightMouse = false;
            released = true;
            Pressed = false;
        }

        public bool LeftMouseDown(Position position, uint currentTicks)
        {
            CursorType? cursorType = null;
            return LeftMouseDown(position, ref cursorType, currentTicks);
        }

        public bool LeftMouseDown(Position position, ref CursorType? cursorType, uint currentTicks)
        {
            if (Disabled)
                return false;

            if (Area.Contains(position))
            {
                pressedTime = DateTime.Now;
                rightMouse = false;

                if (ToggleButton)
                {
                    Pressed = !Pressed;
					cursorType = ExecuteActions(currentTicks, false);
					return true;
                }
                
                Pressed = true;

				if (InstantAction)
                {
                    if (continuousActionDelayInTicks == null)
                        released = true;
                    cursorType = ExecuteActions(currentTicks, false);
                }

                return true;
            }

            return false;
        }

        public bool RightMouseDown(Position position, uint currentTicks)
        {
            if (Disabled || ToggleButton)
                return false;

            if (Area.Contains(position))
            {
                pressedTime = DateTime.Now;
                Pressed = true;
                rightMouse = true;

                if (InstantAction)
                {
                    if (continuousActionDelayInTicks == null)
                        released = true;
                    ExecuteActions(currentTicks, true);
                }

                return true;
            }

            return false;
        }

        public void Hover(Position position)
        {
            if (!string.IsNullOrEmpty(tooltipText) && Area.Contains(position))
                SetTooltip(tooltipText);
            else
                HideTooltip();
        }

        CursorType? ExecuteActions(uint currentTicks, bool rightMouse)
        {
            lastActionTimeInTicks = currentTicks;
            var cursorChangeAction = CursorChangeAction; // The action invoke might change this by swapping buttons!
            if (rightMouse)
                RightClickAction?.Invoke();
            else
                LeftClickAction?.Invoke();

            if (continuousActionDelayInTicks != null && continuousActionDelayInTicks > 1)
                continuousActionDelayInTicks = (uint)Math.Max(1, (int)continuousActionDelayInTicks.Value - (int)ContinuousActionDelayReductionInTicks);

            return cursorChangeAction?.Invoke();
        }

        internal void PressImmediately(Game game, bool rightMouse = false, bool delayedPressAnimation = false)
        {
            if (!Disabled)
            {
                void Press(Action action)
                {
                    void PressAction()
                    {
                        released = true;
                        action?.Invoke();
                    }

                    if (delayedPressAnimation)
                    {
                        Pressed = true;
                        game.AddTimedEvent(TimeSpan.FromMilliseconds(250), PressAction);
                    }
                    else
                    {
                        PressAction();
                    }
                }

                if (rightMouse)
                {
                    if (RightClickAction != null)
                        Press(RightClickAction);
                }
                else
                {
                    if (LeftClickAction != null)
                        Press(LeftClickAction);
                }
            }
        }

        internal CursorType? Press(uint currentTicks)
        {
            if (Disabled)
                return null;

            pressedTime = DateTime.Now;
            Pressed = true;
            rightMouse = false;

            if (InstantAction)
            {
                if (continuousActionDelayInTicks == null)
                    released = true;
            }
            else
                released = true;

            return ExecuteActions(currentTicks, false);
        }

        internal void Release(bool immediately = false)
        {
            if (immediately)
                Pressed = false;

            released = true;
        }

        public void Update(uint currentTicks)
        {
            if (ToggleButton)
                return;

            if (Pressed && released && (DateTime.Now - pressedTime).TotalMilliseconds >= ButtonReleaseTime)
                Pressed = false;

            if (Pressed && continuousActionDelayInTicks != null)
            {
                if (currentTicks - lastActionTimeInTicks >= continuousActionDelayInTicks.Value)
                    ExecuteActions(currentTicks, rightMouse);
            }
        }
    }
}
