using Ambermoon.Data;

namespace Ambermoon;

/// <summary>
/// Outputs texts to a screen reader or speech synthesizer so blind players can play.
/// </summary>
public interface IScreenReader
{
    /// <summary>
    /// Speaks the given text. With interrupt the current output is stopped first.
    /// </summary>
    void Speak(string text, bool interrupt);

    /// <summary>
    /// Stops the current output.
    /// </summary>
    void Silence();

    /// <summary>
    /// Selects a voice of the given language if possible.
    /// </summary>
    void SetLanguage(GameLanguage language);
}
