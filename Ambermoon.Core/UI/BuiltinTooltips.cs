using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.UI
{
    internal static class BuiltinTooltips
    {
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

        static readonly Dictionary<GameLanguage, string[]> AttributeTooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Strength
                "Erhöht das Maximalgewicht um 1kg pro Punkt.^Außerdem wird pro 25 Punkte der Schaden um 1 erhöht.^^Aktuell +{0}kg und +{1} Schaden",
                // Intelligence
                "Fügt pro 25 Punkte zusätzliche 1 SP und^SLP beim Level-Aufstieg hinzu.^^Aktuell +{0} SP und SLP",
                // Dexterity
                "Chance in Prozent, Schlösser-Fallen^nicht auszulösen.^^Erhöht zusammen mit Glück die^Chance Kämpfe zu vermeiden.^^Aktuell {0}% Fallen-Vermeidung^        {1}% Kampf-Vermeidung",
                // Speed
                "Höhere Schnelligkeit ermöglicht es im^Kampf früher an der Reihe zu sein.^^Für alle 80 Punkte kannst du dich^ein weiteres Feld pro Runde bewegen.^^Aktuell kannst du dich {0} Feld(er) bewegen.",
                // Stamina
                "Erhöht die Abwehr um 1 pro 25 Punkte.^^Aktuell +{0} Abwehr",
                // Charisma
                "Erhöht den Verkaufspreis um 1% alle 10 Punkte.^^Aktuell +{0}% Verkaufspreis",
                // Luck
                "Chance in Prozent, die Effekte einer^bereits ausgelösten Falle zu verhindern.^^Erhöht zusammen mit Geschicklichkeit^die Chance Kämpfe zu vermeiden.^^Aktuell {0}% Falleneffekt-Vermeidung^        {1}% Kampf-Vermeidung",
                // Anti-Magic
                "Chance in Prozent, einen gegnerischen^Zauber abzuwehren.^^Aktuell {0}% Abwehrchance"
            } },
            { GameLanguage.English, new string[]
            {
                // Strength
                "Increases the max weight by 1kg per point.^Also increases damage by 1 every 25 points.^^Currently +{0}kg and +{1} damage",
                // Intelligence
                "Adds 1 additional SP and SLP on level up for every 25 points.^^Currently +{0} SP and SLP",
                // Dexterity
                "Chance in percent to not trigger a trap when messing with locks.^Adds, together with Luck, to the chance of avoiding fights.^^Currently {0}% trap avoid chance, {1}% fight avoid chance",
                // Speed
                "Higher speed values let you act earlier in battle.^Every 80 points you can move 1 additional field per round.^^Currently you can move {0} field(s).",
                // Stamina
                "Increases defense by 1 every 25 points.^^Currently +{0} defense",
                // Charisma
                "Increases the sell price by 1% every full 10 points.^^Currently +{0}% sell price",
                // Luck
                "Chance in percent to avoid the effect of an already triggered trap.^Adds, together with Dexterity, to the chance of avoiding fights.^^Currently {0}% trap effect avoid chance, {1}% fight avoid chance",
                // Anti-Magic
                "Chance in percent to block enemy spells.^^Currently {0}% spell block chance"
            } }
        };

        static readonly Dictionary<GameLanguage, string[]> AbilityTooltips = new Dictionary<GameLanguage, string[]>
        {
            { GameLanguage.German, new string[]
            {
                // Attack
                "Chance in Prozent, den Gegner zu treffen.^^Aktuell {0}% Trefferchance",
                // Parry
                "Chance in Prozent, einen Angriff abzuwehren.^Benötigt die Aktion 'Verteidigen' im Kampf.^^Aktuell {0}% Abwehrchance",
                // Swim
                "Schadensreduktion beim Schwimmen.^^Aktuell {0}% Schadensreduktion",
                // Crit
                "Chance in Prozent, einen Gegner mit einem Schlag zu töten.^Funktioniert nicht gegen Bosse.^^Aktuell {0}% Chance",
                // Find traps
                "Chance in Prozent, eine Schloss-Falle zu finden.^^Aktuell {0}% Chance",
                // Disarm traps
                "Chance in Prozent, eine gefundene Schloss-Falle zu entschärfen.^^Aktuell {0}% Chance",
                // Lockpick
                "Chance in Prozent, ein Schloss ohne Dietrich zu knacken.^Funktioniert nicht bei Schlössern, die einen Schlüssel benötigen.^^Aktuell {0}% Chance",
                // Search
                "Chance geheime Schätze zu entdecken.^Höhere Werte ermöglichen es bestimmte Truhen zu finden.",
                // Read magic
                "Chance in Prozent, eine Spruchrolle erfolgreich zu lesen.^Ansonsten wird diese zerstört.^^Aktuell {0}% Chance",
                // Use magic
                "Chance in Prozent, einen Zauber erfolgreich zu wirken.^Manche Zauber haben negative Effekte, wenn sie fehlschlagen.^^Aktuell {0}% Chance"
            } },
            { GameLanguage.English, new string[]
            {
                // Attack
                "Chance to hit an enemy.^^Currently {0}% hit chance",
                // Parry
                "Chance to block an enemy attack.^The battle action 'Parry' is necessary for this.^^Currently {0}% block chance",
                // Swim
                "Damage reduction while swimming.^^Currently {0}% damage reduction",
                // Crit
                "Chance to kill an opponent with a single strike.^Does not work against bosses.^^Current chance: {0}%",
                // Find traps
                "Chance to find a lock trap.^^Current chance: {0}%",
                // Disarm traps
                "Chance to disarm a found lock trap.^^Current chance: {0}%",
                // Lockpick
                "Chance to pick a lock without a lockpick.^Does not work for doors which require a key.^^Current chance: {0}%",
                // Search
                "Chance to find secret treasures.^Higher values allow you to find specific chests.",
                // Read magic
                "Chance to learn a spell from a scroll.^Otherwise the scroll is destroyed.^^Current chance: {0}%",
                // Use magic
                "Chance to cast a spell successfully.^Some spells have negative effects if the cast fails.^^Current chance: {0}%",
            } }
        };
    }
}
