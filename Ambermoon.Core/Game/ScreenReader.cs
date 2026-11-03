using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ambermoon.Data;

namespace Ambermoon;

partial class GameCore
{
    static readonly Regex ScreenReaderTag = new(@"~[A-Z0-9 ]+~", RegexOptions.Compiled);
    static readonly Regex ScreenReaderWhitespace = new(@"\s+", RegexOptions.Compiled);
    string? lastSpokenText = null;
    uint? lastAnnouncedMapIndex = null;
    DateTime lastMapNameAnnouncement = DateTime.MinValue;

    /// <summary>
    /// If set, the texts of popups, conversations, events, messages and map names are spoken.
    /// </summary>
    public IScreenReader? ScreenReader { get; set; }

    /// <summary>
    /// Speaks a text of the game data (with tags like ~INK 30~ or ~LEAD~).
    /// </summary>
    internal void Speak(string? text, bool interrupt = true)
    {
        if (ScreenReader == null || string.IsNullOrWhiteSpace(text))
            return;

        text = ToSpeechText(text, nameProvider);

        if (text.Length == 0)
            return;

        // don't cut off the map name by the text event which is often triggered on map entry
        if (interrupt && DateTime.Now - lastMapNameAnnouncement < TimeSpan.FromSeconds(2))
            interrupt = false;

        lastSpokenText = text;
        ScreenReader.Speak(text, interrupt);
    }

    public void RepeatLastSpokenText()
    {
        if (lastSpokenText != null)
            ScreenReader?.Speak(lastSpokenText, true);
    }

    void AnnounceMapName(Map map, string mapName)
    {
        // The map name is updated quite often, e.g. after closing a window.
        if (lastAnnouncedMapIndex == map.Index)
            return;

        lastAnnouncedMapIndex = map.Index;
        Speak(mapName, false);
        lastMapNameAnnouncement = DateTime.Now;
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

        // ~INK 30~, ~RUN1~ etc., the markers of dictionary words (>word<) and the special spaces
        text = ScreenReaderTag.Replace(text, " ").Replace("~", "").Replace(">", "").Replace("<", "")
            .Replace('$', ' ').Replace('^', '\n');
        text = ScreenReaderWhitespace.Replace(text, " ").Trim();

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
