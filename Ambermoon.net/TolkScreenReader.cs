using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Threading;
using Ambermoon.Data;

namespace Ambermoon;

/// <summary>
/// Screen reader output with the Tolk library (https://github.com/dkager/tolk) on Windows.
/// It supports NVDA, JAWS and others. If no screen reader is running, the Windows speech
/// synthesizer is used directly with a voice of the game language (if installed).
/// Tolk.dll (and the client libraries of the screen readers, e.g. nvdaControllerClient64.dll)
/// must be next to the executable.
/// All calls run on an own thread so that the game never waits for the speech output.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class TolkScreenReader : IScreenReader, IDisposable
{
    const string Library = "Tolk.dll";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_Load();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_Unload();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.I1)] bool trySAPI);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr Tolk_DetectScreenReader();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I1)]
    static extern bool Tolk_Output(string text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    static extern bool Tolk_Silence();

    // a null text means silence
    readonly Queue<(string? Text, bool Interrupt)> queue = new();
    readonly AutoResetEvent signal = new(false);
    readonly Thread thread;
    GameLanguage language = GameLanguage.English;
    bool languageChanged = true;
    bool disposed = false;
    bool stopping = false;
    // only used by the speech thread
    SpeechSynthesizer? synthesizer = null;
    string? defaultVoice = null;

    TolkScreenReader()
    {
        thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "Screen reader"
        };
        thread.SetApartmentState(ApartmentState.STA); // Tolk uses COM
        thread.Start();
    }

    /// <summary>
    /// Returns null if this is not Windows or Tolk is not available.
    /// </summary>
    public static TolkScreenReader? TryCreate()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        if (!NativeLibrary.TryLoad(Library, typeof(TolkScreenReader).Assembly, null, out _))
        {
            Console.WriteLine("Screen reader output is not available: Tolk.dll was not found.");
            return null;
        }

        return new TolkScreenReader();
    }

    static bool ScreenReaderRunning => Tolk_DetectScreenReader() != IntPtr.Zero;

    void Run()
    {
        // SAPI is used directly to be able to choose the voice
        Tolk_TrySAPI(false);
        Tolk_Load();

        while (true)
        {
            signal.WaitOne();

            while (true)
            {
                (string? Text, bool Interrupt) entry;
                bool updateVoice;
                bool empty;

                lock (queue)
                {
                    if (stopping)
                    {
                        synthesizer?.Dispose();
                        Tolk_Unload();
                        return;
                    }

                    updateVoice = languageChanged;
                    languageChanged = false;
                    empty = queue.Count == 0;
                    entry = empty ? (null, false) : queue.Dequeue();
                }

                if (updateVoice && synthesizer != null)
                    SelectVoice();

                if (empty)
                    break;

                if (entry.Text == null)
                {
                    Tolk_Silence();
                    synthesizer?.SpeakAsyncCancelAll();
                }
                else if (ScreenReaderRunning)
                {
                    synthesizer?.SpeakAsyncCancelAll();
                    Tolk_Output(entry.Text, entry.Interrupt);
                }
                else
                {
                    if (synthesizer == null)
                    {
                        synthesizer = new SpeechSynthesizer();
                        synthesizer.SetOutputToDefaultAudioDevice();
                        defaultVoice = synthesizer.Voice.Name;
                        SelectVoice();
                    }

                    if (entry.Interrupt)
                        synthesizer.SpeakAsyncCancelAll();

                    synthesizer.SpeakAsync(entry.Text);
                }
            }
        }
    }

    void SelectVoice()
    {
        GameLanguage currentLanguage;

        lock (queue)
            currentLanguage = language;

        string culture = currentLanguage switch
        {
            GameLanguage.German => "de",
            GameLanguage.French => "fr",
            GameLanguage.Polish => "pl",
            GameLanguage.Czech => "cs",
            _ => "en"
        };

        try
        {
            var voice = synthesizer!.GetInstalledVoices().FirstOrDefault(v => v.Enabled && v.VoiceInfo.Culture.TwoLetterISOLanguageName == culture);
            string? name = voice?.VoiceInfo.Name ?? defaultVoice;

            if (name != null && synthesizer.Voice.Name != name)
                synthesizer.SelectVoice(name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to select a voice: {ex.Message}");
        }
    }

    void Enqueue(string? text, bool interrupt)
    {
        lock (queue)
        {
            // texts which were not passed to the output yet are obsolete
            if (interrupt)
                queue.Clear();

            queue.Enqueue((text, interrupt));
        }

        signal.Set();
    }

    public void Speak(string text, bool interrupt)
    {
        if (!disposed)
            Enqueue(text, interrupt);
    }

    public void Silence()
    {
        if (!disposed)
            Enqueue(null, true);
    }

    public void SetLanguage(GameLanguage language)
    {
        if (disposed)
            return;

        lock (queue)
        {
            if (this.language == language)
                return;

            this.language = language;
            languageChanged = true;
        }

        signal.Set();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            lock (queue)
                stopping = true;

            signal.Set();
            thread.Join(1000);
            disposed = true;
        }
    }
}

/// <summary>
/// Writes the spoken texts to the console. Used by scenario scripts.
/// </summary>
internal sealed class ConsoleScreenReader : IScreenReader
{
    public void Speak(string text, bool interrupt) => Console.WriteLine($"SPEAK{(interrupt ? "!" : "")}: {text}");

    public void Silence() => Console.WriteLine("SILENCE");

    public void SetLanguage(GameLanguage language) => Console.WriteLine($"LANGUAGE: {language}");
}
