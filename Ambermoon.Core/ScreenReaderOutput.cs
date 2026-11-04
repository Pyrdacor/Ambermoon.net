using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ambermoon.Data;
using Ambermoon.UI;

namespace Ambermoon;

/// <summary>
/// Central access to the screen reader so that every UI element can speak,
/// also outside of the game like in the character creator.
/// </summary>
public static class ScreenReaderOutput
{
    static readonly Regex Tag = new(@"~[A-Z0-9 ]+~", RegexOptions.Compiled);
    static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    static string? lastSpokenText = null;
    static DateTime noInterruptUntil = DateTime.MinValue;

    public static IScreenReader? ScreenReader { get; set; }

    public static bool Active => ScreenReader != null;

    /// <summary>
    /// Speaks a text which may contain the control codes of the game texts (like ~INK 30~ or ^).
    /// </summary>
    public static void Speak(string? text, bool interrupt = true, ITextNameProvider? nameProvider = null)
    {
        if (ScreenReader == null || string.IsNullOrWhiteSpace(text))
            return;

        text = ToSpeechText(text, nameProvider);

        if (text.Length == 0)
            return;

        if (interrupt && DateTime.Now < noInterruptUntil)
            interrupt = false;

        lastSpokenText = text;
        ScreenReader.Speak(text, interrupt);
    }

    /// <summary>
    /// The next texts won't interrupt the current output for the given time.
    /// Used for the map name which is often followed by a text event.
    /// </summary>
    public static void ProtectFromInterruption(TimeSpan duration) => noInterruptUntil = DateTime.Now + duration;

    public static void Silence() => ScreenReader?.Silence();

    public static void SetLanguage(GameLanguage language) => ScreenReader?.SetLanguage(language);

    /// <summary>
    /// Repeats the last text. While typing into a text input its current content is spoken.
    /// </summary>
    public static void RepeatLastText()
    {
        if (ScreenReader == null)
            return;

        if (TextInput.FocusedInput is TextInput input)
            ScreenReader.Speak(input.CurrentInput, true);
        else if (lastSpokenText != null)
            ScreenReader.Speak(lastSpokenText, true);
    }

    internal static string ToSpeechText(string text, ITextNameProvider? nameProvider)
    {
        if (nameProvider != null)
        {
            text = text.Replace("~LEAD~", nameProvider.LeadName)
                .Replace("~SELF~", nameProvider.SelfName)
                .Replace("~CAST~", nameProvider.CastName)
                .Replace("~INVN~", nameProvider.InvnName)
                .Replace("~SUBJ~", nameProvider.SubjName)
                .Replace("~SEX1~", nameProvider.Sex1Name)
                .Replace("~SEX2~", nameProvider.Sex2Name);
        }

        // single characters (e.g. typed into a text input) are kept as they are
        if (text.Length == 1)
            return text;

        // ~INK 30~, ~RUN1~ etc., the markers of dictionary words (>word<) and the special spaces
        text = Tag.Replace(text, " ").Replace("~", "").Replace(">", "").Replace("<", "")
            .Replace('$', ' ').Replace('^', '\n');
        text = Whitespace.Replace(text, " ").Trim();

        return ToSentenceCase(text);
    }

    // Some speech synthesizers spell out upper case words, so texts without
    // any lower case letter are converted. Mixed case texts are kept as they are.
    static string ToSentenceCase(string text)
    {
        // ß is a lower case letter but has no upper case variant in the game font
        if (text.Any(ch => char.IsLower(ch) && ch != 'ß'))
            return text;

        var builder = new StringBuilder(text.Length);
        bool sentenceStart = true;

        foreach (char ch in text)
        {
            if (char.IsLetter(ch))
            {
                builder.Append(sentenceStart ? char.ToUpper(ch, CultureInfo.InvariantCulture) : char.ToLower(ch, CultureInfo.InvariantCulture));
                sentenceStart = false;
            }
            else
            {
                builder.Append(ch);

                if (ch == '.' || ch == '!' || ch == '?' || ch == ':')
                    sentenceStart = true;
            }
        }

        return builder.ToString();
    }
}
