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
}
