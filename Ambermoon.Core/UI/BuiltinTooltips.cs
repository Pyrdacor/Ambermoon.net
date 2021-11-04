/*
 * BuiltinTooltips.cs - Custom tooltip texts
 *
 * Copyright (C) 2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using System.Collections.Generic;

namespace Ambermoon.UI
{
    internal static class BuiltinTooltips
    {
        public enum SecondaryStat
        {
            Age,
            LP,
            SP,
            SLP,
            TP,
            Gold,
            Food,
            Damage,
            Defense,
            EPPre50,
            EP50,
            LevelWithAPRIncrease,
            LevelWithoutAPRIncrease
        }

        public static string GetSecondaryStatTooltip(GameLanguage gameLanguage, SecondaryStat secondaryStat, PartyMember partyMember)
        {
            var formatString = SecondaryStatTooltips[gameLanguage][(int)secondaryStat];

            string CreateAPRLevelString()
            {
                string aprLevels = "";

                for (int i = partyMember.AttacksPerRoundIncreaseLevels; i <= 50; i += partyMember.AttacksPerRoundIncreaseLevels)
                {
                    aprLevels += $"{i} ";
                }

                return aprLevels.TrimEnd(' ');
            }

            switch (secondaryStat)
            {
                case SecondaryStat.Age:
                    return string.Format(formatString, partyMember.Attributes[Attribute.Age].MaxValue);
                case SecondaryStat.EPPre50:
                    return string.Format(formatString, partyMember.GetNextLevelExperiencePoints());
                case SecondaryStat.LevelWithAPRIncrease:
                    return string.Format(formatString, partyMember.HitPointsPerLevel, partyMember.SpellPointsPerLevel,
                        partyMember.SpellLearningPointsPerLevel, partyMember.TrainingPointsPerLevel,
                        partyMember.AttacksPerRound, CreateAPRLevelString());
                case SecondaryStat.LevelWithoutAPRIncrease:
                    return string.Format(formatString, partyMember.HitPointsPerLevel, partyMember.SpellPointsPerLevel,
                        partyMember.SpellLearningPointsPerLevel, partyMember.TrainingPointsPerLevel,
                        partyMember.AttacksPerRound);
                default:
                    return formatString;
            }
        }

        public static string GetAttributeTooltip(GameLanguage gameLanguage, Attribute attribute, PartyMember partyMember)
        {
            var formatString = AttributeTooltips[gameLanguage][(int)attribute];
            var attributeValue = partyMember.Attributes[attribute].TotalCurrentValue;

            switch (attribute)
            {
                case Attribute.Strength:
                    return string.Format(formatString, attributeValue, attributeValue / 25);
                case Attribute.Intelligence:
                    return string.Format(formatString, attributeValue / 25);
                case Attribute.Dexterity:
                    return string.Format(formatString, attributeValue, (attributeValue + partyMember.Attributes[Attribute.Luck].TotalCurrentValue) * 100 / 150);
                case Attribute.Speed:
                    return string.Format(formatString, 1 + attributeValue / 80);
                case Attribute.Stamina:
                    return string.Format(formatString, attributeValue / 25);
                case Attribute.Charisma:
                    return string.Format(formatString, attributeValue / 10);
                case Attribute.Luck:
                    return string.Format(formatString, attributeValue, (attributeValue + partyMember.Attributes[Attribute.Dexterity].TotalCurrentValue) * 100 / 150);
                case Attribute.AntiMagic:
                    return string.Format(formatString, attributeValue);
                default:
                    throw new AmbermoonException(ExceptionScope.Application, "Unsupported attribute tooltip");
            }
        }

        public static string GetAbilityTooltip(GameLanguage gameLanguage, Ability ability, PartyMember partyMember)
        {
            var formatString = AbilityTooltips[gameLanguage][(int)ability];
            var abilityValue = partyMember.Abilities[ability].TotalCurrentValue;

            switch (ability)
            {
                case Ability.Swim:
                    return string.Format(formatString, abilityValue / 2);
                case Ability.Searching:
                    return formatString;
                default:
                    return string.Format(formatString, abilityValue);
            }
        }

        public static string GetAilmentTooltip(GameLanguage gameLanguage, Ailment ailment, PartyMember partyMember) => ailment == Ailment.Aging
            ? string.Format(AilmentTooltips[gameLanguage][ailment], partyMember.Attributes[Attribute.Age].MaxValue) : AilmentTooltips[gameLanguage][ailment];

        static readonly Dictionary<GameLanguage, string[]> AttributeTooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Strength
                "Stärke^^Erhöht das Maximalgewicht um 1kg pro Punkt.^Außerdem wird pro 25 Punkte der Schaden um 1 erhöht.^^Aktuell +{0}kg und +{1} Schaden",
                // Intelligence
                "Intelligenz^^Fügt pro 25 Punkte zusätzliche 1 SP und^SLP beim Level-Aufstieg hinzu.^^Aktuell +{0} SP und SLP",
                // Dexterity
                "Geschicklichkeit^^Chance in Prozent, Schlösser-Fallen^nicht auszulösen.^^Erhöht zusammen mit Glück die^Chance Kämpfe zu vermeiden.^^Aktuell {0}% Fallen-Vermeidung^        {1}% Kampf-Vermeidung",
                // Speed
                "Schnelligkeit^^Höhere Schnelligkeit ermöglicht es im^Kampf früher an der Reihe zu sein.^^Für alle 80 Punkte kannst du dich^ein weiteres Feld pro Runde bewegen.^^Aktuell kannst du dich {0} Feld(er) bewegen.",
                // Stamina
                "Konstitution^^Erhöht die Abwehr um 1 pro 25 Punkte.^^Aktuell +{0} Abwehr",
                // Charisma
                "Karisma^^Erhöht den Verkaufspreis um 1% alle 10 Punkte.^^Aktuell +{0}% Verkaufspreis",
                // Luck
                "Glück^^Chance in Prozent, die Effekte einer^bereits ausgelösten Falle zu verhindern.^^Erhöht zusammen mit Geschicklichkeit^die Chance Kämpfe zu vermeiden.^^Aktuell {0}% Falleneffekt-Vermeidung^        {1}% Kampf-Vermeidung",
                // Anti-Magic
                "Anti-Magie^^Chance in Prozent, einen gegnerischen^Zauber abzuwehren.^^Aktuell {0}% Abwehrchance"
            } },
            { GameLanguage.English, new string[]
            {
                // Strength
                "Strength^^Increases the max weight by 1kg per point.^Also increases damage by 1 every 25 points.^^Currently +{0}kg and +{1} damage",
                // Intelligence
                "Intelligence^^Adds 1 additional SP and SLP on^level up for every 25 points.^^Currently +{0} SP and SLP",
                // Dexterity
                "Dexterity^^Chance in percent to not trigger^a trap when messing with locks.^Adds, together with Luck,^to the chance of avoiding fights.^^Currently {0}% trap avoid chance^          {1}% fight avoid chance",
                // Speed
                "Speed^^Higher speed values let you act earlier in battle.^Every 80 points you can move 1 additional field^per round.^^Currently you can move {0} field(s).",
                // Stamina
                "Stamina^^Increases defense by 1 every 25 points.^^Currently +{0} defense",
                // Charisma
                "Charisma^^Increases the sell price by 1%^every full 10 points.^^Currently +{0}% sell price",
                // Luck
                "Luck^^Chance in percent to avoid the effect^of an already triggered trap.^Adds, together with Dexterity,^to the chance of avoiding fights.^^Currently {0}% trap effect avoid chance^          {1}% fight avoid chance",
                // Anti-Magic
                "Anti-Magic^^Chance in percent to block enemy spells.^^Currently {0}% spell block chance"
            } }
        };

        static readonly Dictionary<GameLanguage, string[]> AbilityTooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Attack
                "Attacke^^Chance in Prozent, den Gegner zu treffen.^^Aktuell {0}% Trefferchance",
                // Parry
                "Parade^^Chance in Prozent, einen Angriff abzuwehren.^Benötigt die Aktion 'Verteidigen' im Kampf.^^Aktuell {0}% Abwehrchance",
                // Swim
                "Schwimmen^^Schadensreduktion beim Schwimmen.^^Aktuell {0}% Schadensreduktion",
                // Crit
                "Kritischer Treffer^^Chance in Prozent, einen Gegner^mit einem Schlag zu töten.^Funktioniert nicht gegen Bosse.^^Aktuell {0}% Chance",
                // Find traps
                "Fallen Finden^^Chance in Prozent, eine Schloss-Falle zu finden.^^Aktuell {0}% Chance",
                // Disarm traps
                "Fallen Entschärfen^^Chance in Prozent, eine gefundene^Schloss-Falle zu entschärfen.^^Aktuell {0}% Chance",
                // Lockpick
                "Schlösser Öffnen^^Chance in Prozent, ein Schloss^ohne Dietrich zu knacken.^Funktioniert nicht bei Schlössern,^die einen Schlüssel benötigen.^^Aktuell {0}% Chance",
                // Search
                "Suchen^^Chance geheime Schätze zu entdecken.^Höhere Werte ermöglichen es^bestimmte Truhen zu finden.",
                // Read magic
                "Spruchrollen Lesen^^Chance in Prozent, eine Spruchrolle^erfolgreich zu lesen.^Ansonsten wird diese zerstört.^^Aktuell {0}% Chance",
                // Use magic
                "Magie Benutzen^^Chance in Prozent, einen Zauber^erfolgreich zu wirken.^Manche Zauber haben negative Effekte^wenn sie fehlschlagen.^^Aktuell {0}% Chance"
            } },
            { GameLanguage.English, new string[]
            {
                // Attack
                "Attack^^Chance to hit an enemy.^^Currently {0}% hit chance",
                // Parry
                "Parry^^Chance to block an enemy attack.^The battle action 'Parry' is necessary for this.^^Currently {0}% block chance",
                // Swim
                "Swim^^Damage reduction while swimming.^^Currently {0}% damage reduction",
                // Crit
                "Critical Hit^^Chance to kill an opponent with a single strike.^Does not work against bosses.^^Current chance: {0}%",
                // Find traps
                "Find Traps^^Chance to find a lock trap.^^Current chance: {0}%",
                // Disarm traps
                "Disarm Traps^^Chance to disarm a found lock trap.^^Current chance: {0}%",
                // Lockpick
                "Lockpicking^^Chance to pick a lock without a lockpick.^Does not work for doors which require a key.^^Current chance: {0}%",
                // Search
                "Searching^^Chance to find secret treasures.^Higher values allow you to find specific chests.",
                // Read magic
                "Read Magic^^Chance to learn a spell from a scroll.^Otherwise the scroll is destroyed.^^Current chance: {0}%",
                // Use magic
                "Use Magic^^Chance to cast a spell successfully.^Some spells have negative effects if the cast fails.^^Current chance: {0}%",
            } }
        };

        static readonly Dictionary<GameLanguage, Dictionary<Ailment, string>> AilmentTooltips = new Dictionary<GameLanguage, Dictionary<Ailment, string>>
        {
            { GameLanguage.German, new Dictionary<Ailment, string>
            {
                // Irritated
                { Ailment.Irritated, "Der Charakter kann keine Zauber wirken.^^Hält nur für die Dauer des Kampfes." },
                // Crazy
                { Ailment.Crazy, "Der Charakter führt zufällige Aktionen im Kampf aus.^Sein Inventar ist nicht einsehbar." },
                // Sleep
                { Ailment.Sleep, "Der Charakter kann keine Kampfaktion ausführen.^Erleidet er Schaden, endet der Status.^^Hält nur für die Dauer des Kampfes." },
                // Panic
                { Ailment.Panic, "Der Charakter versucht zu fliehen.^Keine Kampfaktion möglich.^Inventar nicht einsehbar.^^Hält nur für die Dauer des Kampfes." },
                // Blind
                { Ailment.Blind, "Der Charakter kann nicht sehen.^Lichtradius auf 2D-Karten nicht vorhanden.^Völlige Dunkelheit auf 3D-Karten." },
                // Drugged
                { Ailment.Drugged, "Der Charakter steht unter Drogeneinfluss.^Steuerung erschwert und visuelle Effekte." },
                // Exhausted
                { Ailment.Exhausted, "Alle Attribute temporär halbiert.^Kann durch Schlafen beseitigt werden." },
                // Unused
                { Ailment.Unused, "" },
                // Lamed
                { Ailment.Lamed, "Keine Bewegung und kein Angriff möglich." },
                // Poisoned
                { Ailment.Poisoned, "Der Charakter erleidet Schaden pro^Kampfrunde und sonst jede Stunde." },
                // Petrified
                { Ailment.Petrified, "Das Inventar ist nicht verfügbar.^Der Charakter kann keine Aktion^ausführen und altert nicht." },
                // Diseased
                { Ailment.Diseased, "Der Charakter verliert jeden Tag^dauerhaft einen Punkt eines^zufälligen Attributs." },
                // Aging
                { Ailment.Aging, "Der Charakter altert pro Tag um ein Jahr.^Je nach Rasse stirbt er ab^einem bestimmten Alter.^^Maximalalter: {0}" },
                // DeadCorpse
                { Ailment.DeadCorpse, "Der Charakter nimmt nicht an Kämpfen teil.^Er kann nicht kommunizieren." },
                // DeadAshes
                { Ailment.DeadAshes, "Der Charakter nimmt nicht an Kämpfen teil.^Er kann nicht kommunizieren.^Er muss zunächst in Fleisch verwandelt^werden, um ihn wiederzubeleben." },
                // DeadDust
                { Ailment.DeadDust, "Der Charakter nimmt nicht an Kämpfen teil.^Er kann nicht kommunizieren.^Er muss zunächst in Asche und danach^in Fleisch verwandelt werden,^um ihn wiederzubeleben." }
            } },
            { GameLanguage.English, new Dictionary<Ailment, string>
            {
                // Irritated
                { Ailment.Irritated,"The character can not cast spells.^^Only active during battle." },
                // Crazy
                { Ailment.Crazy, "The character performs random actions in battle.^His inventory is not accessible." },
                // Sleep
                { Ailment.Sleep, "The character can not perform battle actions.^Any damage will cancel the status.^^Only active during battle." },
                // Panic
                { Ailment.Panic, "The character tries to flee.^No battle action possible.^Inventory not accessible.^^Only active during battle." },
                // Blind
                { Ailment.Blind, "The character can not see.^Light radius on 2D maps is disabled.^Complete darkness on 3D maps." },
                // Drugged
                { Ailment.Drugged, "The charater is under the influence of drugs.^Complicated control and visual effects.." },
                // Exhausted
                { Ailment.Exhausted, "All attributes halved temporarly.^Can be removed by sleeping." },
                // Unused
                { Ailment.Unused, "" },
                // Lamed
                { Ailment.Lamed, "No movement or attack is possible." },
                // Poisoned
                { Ailment.Poisoned, "The character receives damage^every battle round or hour." },
                // Petrified
                { Ailment.Petrified, "The inventory is not accessible.^The character can not take any^action and does not age." },
                // Diseased
                { Ailment.Diseased, "The character loses a point of^a random attribute every day." },
                // Aging
                { Ailment.Aging, "The character ages every day.^Dependent on his race he will^eventually die at a specific age.^^Max age: {0}" },
                // DeadCorpse
                { Ailment.DeadCorpse, "The character does not participate in battles.^He can not communicate." },
                // DeadAshes
                { Ailment.DeadAshes, "The character does not participate in battles.^He can not communicate.^His ashes must be converted to^flesh first to resurrect him." },
                // DeadDust
                { Ailment.DeadDust, "The character does not participate in battles.^He can not communicate.^His dust must be converted to ashes^and then to flesh to resurrect him." }
            } }
        };

        static readonly Dictionary<GameLanguage, string[]> SecondaryStatTooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Age
                "Alter des Charakters^^Er stirbt wenn das maximale^Alter von {0} erreicht ist.",
                // LP
                "Lebenspunkte^^Wenn sie auf 0 sinken,^stirbt der Charakter.",
                // SP
                "Spruchpunkte^^Werden zum Wirken^von Zaubern benötigt.",
                // SLP
                "Spruchlernpunkte^^Werden zum Lernen^von Zaubern benötigt.",
                // TP
                "Trainingspunkte^^Werden zum Steigern von^Fähigkeiten bei Trainern benötigt.",
                // Gold
                "Gold^^Die Währung in Ambermoon.^Wird für den Kauf von Waren benötigt.",
                // Food
                "Rationen^^Bei jeder Rast (außer in Gasthäusern),^wird 1 Ration pro Charakter^verbraucht um LP und SP aufzufüllen.",
                // Damage
                "Schaden^^Grundwert für den Schaden im Kampf.^^Wird durch die Ausrüstung und Stärke bestimmt.",
                // Defense
                "Abwehr^^Grundwert für die Schadensreduzierung^von physischen Angriffen.^^Wird durch Ausrüstung und Konstitution bestimmt.",
                // EPPre50
                "Erfahrungspunkte^^Werden für den Levelaufstieg benötigt.^^Nächster Level bei {0} EP.",
                // EP50
                "Erfahrungspunkte^^Werden für den Levelaufstieg benötigt.^^Maximallevel bereits erreicht.",
                // LevelWithAPRIncrease
                "Charakterlevel^^Jeder Levelaufstieg verbessert die^Werte des Charakters um:^^ {0} LP, {1} SP, {2} SLP und {3} TP^^Attacken pro Runde erhöhen sich^bei bestimmten Leveln:^^ {5}^^Attacken pro Runde sind {4}",
                // LevelWithoutAPRIncrease
                "Charakterlevel^^Jeder Levelaufstieg verbessert die^Werte des Charakters um:^^ {0} LP, {1} SP, {2} SLP und {3} TP^^Attacken pro Runde sind {4}"
            } },
            { GameLanguage.English, new string[]
            {
                // Age
                "Age of the character^^He dies at the max age of {0}.",
                // LP
                "Life Points^^When they reach 0^the character dies.",
                // SP
                "Spell Points^^Are used to cast spells.",
                // SLP
                "Spell Learning Points^^Are used to learn spells.",
                // TP
                "Training Points^^Are used to increase^skills at trainers.",
                // Gold
                "Gold^^Currency of Ambermoon.^Is used to buy goods.",
                // Food
                "Rations^^For every rest (besides sleeping at inns),^1 ration is consumed per character to^refill LP and SP.",
                // Damage
                "Damage^^Base value for damage in battles.^^Is composed of equipment and strength.",
                // Defense
                "Defense^^Base value for physical damage^reduction in battles.^^Is composed of equipment and stamina.",
                // EPPre50
                "Experience Points^^Are needed to gain levels.^^Next level at {0} EP.",
                // EP50
                "Experience Points^^Are needed to gain levels.^^Max level already reached.",
                // LevelWithAPRIncrease
                "Character Level^^Each level-up increases the^character's values by:^^ {0} LP, {1} SP, {2} SLP and {3} TP^^Attacks per round increase^at specific levels:^^ {5}^^Attacks per round are {4}",
                // LevelWithoutAPRIncrease
                "Character Level^^Each level-up increases the^character's values by:^^ {0} LP, {1} SP, {2} SLP and {3} TP^^Attacks per round are {4}"
            } }
        };
    }
}
