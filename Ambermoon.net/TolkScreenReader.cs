using System;
using System.Runtime.InteropServices;

namespace Ambermoon;

/// <summary>
/// Screen reader output with the Tolk library (https://github.com/dkager/tolk) on Windows.
/// It supports NVDA, JAWS and others and falls back to the Windows speech synthesizer (SAPI).
/// Tolk.dll (and the client libraries of the screen readers, e.g. nvdaControllerClient64.dll)
/// must be next to the executable.
/// </summary>
internal sealed class TolkScreenReader : IScreenReader, IDisposable
{
    const string Library = "Tolk.dll";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_Load();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_Unload();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    static extern void Tolk_TrySAPI([MarshalAs(UnmanagedType.I1)] bool trySAPI);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I1)]
    static extern bool Tolk_Output(string text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

    bool disposed = false;

    TolkScreenReader()
    {
    }

    /// <summary>
    /// Returns null if this is not Windows or Tolk is not available.
    /// </summary>
    public static TolkScreenReader? TryCreate()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            Tolk_TrySAPI(true);
            Tolk_Load();
            return new TolkScreenReader();
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is BadImageFormatException)
        {
            Console.WriteLine($"Screen reader output is not available: {ex.Message}");
            return null;
        }
    }

    public void Speak(string text, bool interrupt)
    {
        if (!disposed)
            Tolk_Output(text, interrupt);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Tolk_Unload();
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
}
