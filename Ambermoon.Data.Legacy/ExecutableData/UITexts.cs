using System.Collections.Generic;

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
        Abilities,
        Attributes,
        Languages,
        Ailments,
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
        /// Use for things like 50%/80% (e.g. ability values).
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
        Ability,
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
    /// After the <see cref="AilmentNames"/> there are the
    /// UI texts.
    /// </summary>
    public class UITexts
    {
        readonly Dictionary<UITextIndex, string> entries = new Dictionary<UITextIndex, string>();
        public IReadOnlyDictionary<UITextIndex, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the UI texts just behind the
        /// ailment names.
        /// 
        /// It will be behind the UI texts after this.
        /// </summary>
        internal UITexts(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<UITextIndex>())
            {
                // TODO: This needs improvement!
                var text = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                text = text.Replace("0123456789", "{1:1111111111}");
                text = text.Replace("012345678", "{1:111111111}");
                text = text.Replace("01234567", "{1:11111111}");
                text = text.Replace("0123456", "{1:1111111}");
                text = text.Replace("012345", "{1:111111}");
                text = text.Replace("01234", "{1:11111}");
                text = text.Replace("0123", "{1:1111}");
                text = text.Replace("012", "{1:111}");
                text = text.Replace("01", "{1:11}");
                text = text.Replace("0", "{1:1}");
                text = text.Replace('1', '0');
                int offset = 0;
                int n = 0;
                while (offset < text.Length)
                {
                    int index = text.IndexOf("0:", offset);

                    if (index == -1)
                        break;

                    if (n == 0)
                        ++n;
                    else
                    {
                        text = text.Remove(index, 1);
                        text = text.Insert(index, (n++).ToString());
                    }
                    offset = index + 3;
                }
                entries.Add(type, text);
            }

            dataReader.AlignToWord();
        }
    }
}
