using System;
using System.Collections.Generic;
using Ambermoon.Data;

namespace Ambermoon;

partial class GameCore
{
    string? lastSpokenTooltip = null;
    // text panels like gold and food of chests, merchants and the inventory
    readonly Dictionary<CharacterInfo, (Rect Area, string Text)> textPanelSpeech = [];
    uint? lastAnnouncedMapIndex = null;

    /// <summary>
    /// If set, the texts of popups, conversations, events, messages, tooltips and map names are spoken.
    /// </summary>
    public IScreenReader? ScreenReader
    {
        get => ScreenReaderOutput.ScreenReader;
        set => ScreenReaderOutput.ScreenReader = value;
    }

    /// <summary>
    /// Speaks a text of the game data (with tags like ~INK 30~ or ~LEAD~).
    /// </summary>
    internal void Speak(string? text, bool interrupt = true) => ScreenReaderOutput.Speak(text, interrupt, nameProvider);

    /// <summary>
    /// Speaks a tooltip when it changes. Null means that no tooltip is shown anymore.
    /// </summary>
    internal void SpeakTooltip(string? text)
    {
        if (text == lastSpokenTooltip)
            return;

        lastSpokenTooltip = text;

        if (text != null)
            Speak(text);
    }

    /// <summary>
    /// Stops the output, e.g. when a popup or window with text is closed before the text was read completely.
    /// </summary>
    internal void StopSpeaking()
    {
        lastSpokenTooltip = null;
        ScreenReaderOutput.Silence();
    }

    public void RepeatLastSpokenText() => ScreenReaderOutput.RepeatLastText();

    /// <summary>
    /// Returns the text of the text panel (e.g. "GOLD^37") at the given position or null.
    /// </summary>
    internal string? GetTextPanelSpeech(Position position)
    {
        foreach (var (area, text) in textPanelSpeech.Values)
        {
            if (area.Contains(position))
                return text;
        }

        return null;
    }

    void SetTextPanelText(CharacterInfo characterInfo, string text)
    {
        characterInfoTexts[characterInfo].SetText(renderView.TextProcessor.CreateText(text));

        if (textPanelSpeech.TryGetValue(characterInfo, out var entry))
            textPanelSpeech[characterInfo] = (entry.Area, text);
    }

    void AnnounceMapName(Map map, string mapName)
    {
        // The map name is updated quite often, e.g. after closing a window.
        if (lastAnnouncedMapIndex == map.Index)
            return;

        lastAnnouncedMapIndex = map.Index;
        Speak(mapName, false);
        // don't cut off the map name by the text event which is often triggered on map entry
        ScreenReaderOutput.ProtectFromInterruption(TimeSpan.FromSeconds(2));
    }
}
