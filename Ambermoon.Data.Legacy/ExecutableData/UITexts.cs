using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// Note: Placeholders can be inside the texts.
    /// They are represented by increasing numbers
    /// starting at 0.
    /// 
    /// Examples:
    /// 0 -> Replaced by a 1-digit value (e.g. "5")
    /// 01 -> Replaced by a 2-digit value (e.g. " 5" or "23")
    /// 012 -> Replaced by a 3-digit value (e.g. "  5" or " 23" or "512")
    /// And so on. It allows up to 10 digits.
    /// 
    /// If a 2-digit placeholders is replaced by a 3-digit value (e.g. 100)
    /// the display should show "**" instead. As a general rule, fill the
    /// hole placeholder length with '*' if the replacement value has more
    /// digits as the placeholder allows.
    /// 
    /// Text indices that ends with "Display" are UI labels which display
    /// some ingame value along with some text. All others are just texts.
    /// </summary>
    public enum UITextIndex
    {
        APR,
        He,
        She,
        His,
        Her,
        Skills,
        Attributes,
        Languages,
        Conditions,
        Male,
        Female,
        /// <summary>
        /// This contains all of them starting with W (West) and going
        /// clock-wise until W again and then additional N-W and N again.
        /// I guess it is used for the compass which can scroll and display
        /// 3 directions partially at once.
        /// </summary>
        CardinalDirections,
        /// <summary>
        /// Contains a 3-digit placeholder for the age.
        /// </summary>
        AgeDisplay,
        /// <summary>
        /// Use for things like 50%/80% (e.g. skill values).
        /// Contains 2 2-digit placeholders.
        /// </summary>
        PercentageValueDisplay,
        /// <summary>
        /// Use for things like 50/80 (e.g. attribute values).
        /// Contains 2 2-digit placeholders.
        /// </summary>
        ValueDisplay,
        /// <summary>
        /// Just the text "EP".
        /// </summary>
        EP,
        /// <summary>
        /// Contains a 10-digit placeholder for the EP value.
        /// </summary>
        EPDisplay,
        /// <summary>
        /// Contains 2 3-digit placeholders.
        /// </summary>
        LPDisplay,
        /// <summary>
        /// Contains 2 3-digit placeholders.
        /// </summary>
        SPDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        SLPDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        TPDisplay,
        /// <summary>
        /// Contains a 5-digit placeholder for gold and a 4-digit placeholder for food.
        /// </summary>
        GoldAndFoodDisplay,
        /// <summary>
        /// A colon followed by a 3-digit placeholder.
        /// </summary>
        LabeledValueDisplay,
        Gold,
        Food,
        ClassHeader,
        Sex,
        BothSexes,
        /// <summary>
        /// Contains a 5-digit placeholder.
        /// </summary>
        WeightGramDisplay,
        /// <summary>
        /// Number of hands.
        /// Contains a 1-digit placeholder.
        /// </summary>
        HandsDisplay,
        /// <summary>
        /// Number of fingers.
        /// Contains a 1-digit placeholder.
        /// </summary>
        FingersDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        DamageDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        DefenseDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        MaxLPDisplay,
        /// <summary>
        /// Contains a 3-digit placeholder.
        /// </summary>
        MaxSPDisplay,
        /// <summary>
        /// I assume M-B-W stands for "Magische Barriere Waffenlevel"
        /// which means "Magic barrier weapon level" in english.
        /// It's a level of a magic protection penetration.
        /// If the M-B-W value is smaller than the target's M-B-R
        /// value, no damage can be dealt.
        /// Contains a 2-digit placeholder.
        /// </summary>
        MBWDisplay,
        /// <summary>
        /// I assume M-B-R stands for "Magische Barriere Rüstungslevel"
        /// which means "Magic barrier armor level" in english.
        /// It's a level of a magic protection. If the
        /// M-B-W value is smaller than the targets M-B-R
        /// value, no damage can be dealt.
        /// Contains a 2-digit placeholder.
        /// </summary>
        MBRDisplay,
        Attribute,
        Skill,
        Placeholder2Digit,
        Placeholder2DigitInParentheses,
        Cursed,
        Weight,
        /// <summary>
        /// Contains 2 3-digit placeholders.
        /// </summary>
        WeightKilogramDisplay,
        Legend,
        Location,
        On,
        Off,
        DataHeader,
        ChooseCharacter,
        Inventory
    }

    /// <summary>
    /// After the <see cref="ConditionNames"/> there are the
    /// UI texts.
    /// </summary>
    public class UITexts
    {
        readonly Dictionary<UITextIndex, string> entries = new Dictionary<UITextIndex, string>();
        public IReadOnlyDictionary<UITextIndex, string> Entries => entries;

        internal UITexts(List<string> uiTexts)
        {
            if (uiTexts.Count != 49)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of UI texts.");

            for (int i = 0; i < uiTexts.Count; ++i)
            {
                if (i < 11)
                    entries.Add((UITextIndex)i, uiTexts[i]);
                else if (i == 11)
                    entries.Add(UITextIndex.BothSexes, uiTexts[i]);
                else if (i < 28)
                    entries.Add((UITextIndex)(i - 1), uiTexts[i]);
                else if (i < 39)
                    entries.Add((UITextIndex)i, uiTexts[i]);
                else
                    entries.Add((UITextIndex)(i + 2), uiTexts[i]);
            }

            entries.Add(UITextIndex.Placeholder2Digit, "{0:00}");
            entries.Add(UITextIndex.Placeholder2DigitInParentheses, "({0:00})");
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the UI texts just behind the
        /// condition names.
        /// 
        /// It will be behind the UI texts after this.
        /// </summary>
        internal UITexts(IDataReader dataReader)
        {
            var placeholderRegex = new Regex("[0-9]+", RegexOptions.Compiled);

            foreach (var type in EnumHelper.GetValues<UITextIndex>())
            {
                var text = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                var matches = placeholderRegex.Matches(text);

                for (int m = matches.Count - 1; m >= 0; --m)
                {
                    var match = matches[m];
                    text = text.Remove(match.Index, match.Length).Insert(match.Index, "{" + m.ToString() + ":" + new string('0', match.Length) + "}");
                }
                
                entries.Add(type, text);
            }

            dataReader.AlignToWord();
        }
    }
}
