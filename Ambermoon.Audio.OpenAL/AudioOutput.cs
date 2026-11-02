using Ambermoon.Data.Audio;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Soft;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ambermoon.Audio.OpenAL;

public unsafe class AudioOutput : IAudioOutput, IDisposable
{
    const int ALC_FREQUENCY = 0x1007;
    const int ALC_CONNECTED = 0x313;
    const int ALC_DEFAULT_ALL_DEVICES_SPECIFIER = 0x1012;
    const int ALC_ALL_DEVICES_SPECIFIER = 0x1013;
    static readonly TimeSpan DeviceCheckInterval = TimeSpan.FromSeconds(1);
    readonly AL al = null;
    readonly ALContext alContext = null;
    Device* device = null;
    Context* context = null;
    ReopenDevices reopenDevices = null;
    uint source = 0;
    bool disposed = false;
    float volume = 1.0f;
    bool enabled = true;
    volatile bool streaming = false;
    readonly Dictionary<IAudioStream, AudioBuffers> audioBuffers = [];
    AudioBuffers currentBuffer = null;
    volatile CancellationTokenSource cancellationTokenSource;
    readonly object deviceLock = new();
    readonly CancellationTokenSource deviceMonitorCancellation = new();
    readonly Thread deviceMonitorThread = null;
    // what should be played as soon as a device is available
    (IAudioStream Stream, int Channels, int SampleRate, bool Sample8Bit)? pendingStream = null;
    bool playRequested = false;

    public AudioOutput()
    {
        try
        {
            al = AL.GetApi(true);
            alContext = ALContext.GetApi(true);
        }
        catch
        {
            try
            {
                al = AL.GetApi(false);
                alContext = ALContext.GetApi(false);
            }
            catch
            {
                Available = false;
                return;
            }
        }

        if (TryOpenDevice())
        {
            int deviceFrequency = 0;
            alContext.GetContextProperty(device, (GetContextInteger)ALC_FREQUENCY, 1, &deviceFrequency);

            // this is not changed later as already created songs rely on it
            if (deviceFrequency > 0)
                SampleRate = deviceFrequency;
        }

        deviceMonitorThread = new Thread(MonitorDevice) { IsBackground = true };
        deviceMonitorThread.Start();
    }

    bool TryOpenDevice()
    {
        var newDevice = alContext.OpenDevice("");

        if (newDevice == null)
            return false;

        // the context is null if the device can't be initialized (e.g. if the system rejects it)
        var newContext = alContext.CreateContext(newDevice, null);

        if (newContext != null)
            alContext.MakeContextCurrent(newContext);

        if (newContext == null || al.GetError() != AudioError.NoError)
        {
            if (newContext != null)
                alContext.DestroyContext(newContext);
            alContext.CloseDevice(newDevice);
            return false;
        }

        lock (deviceLock)
        {
            device = newDevice;
            context = newContext;
            alContext.TryGetExtension(device, out reopenDevices);
            source = al.GenSource();
            al.SetSourceProperty(source, SourceBoolean.Looping, false);
            al.SetSourceProperty(source, SourceFloat.Gain, volume);
            Available = true;

            if (pendingStream is var (stream, channels, sampleRate, sample8Bit))
            {
                StreamData(stream, channels, sampleRate, sample8Bit);

                if (playRequested)
                    Start();
            }

            playRequested = false;
            pendingStream = null;
        }

        return true;
    }

    bool IsDeviceConnected()
    {
        int connected = 1;
        alContext.GetContextProperty(device, (GetContextInteger)ALC_CONNECTED, 1, &connected);
        return connected != 0;
    }

    // there is no notification about lost or new devices so we have to poll
    void MonitorDevice()
    {
        string triedDefaultDevice = null;

        while (!deviceMonitorCancellation.Token.WaitHandle.WaitOne(DeviceCheckInterval))
        {
            if (!Available)
            {
                TryOpenDevice();
                continue;
            }

            // OpenAL does not follow a changed default device on its own and it
            // only updates the default device if the device list is queried
            alContext.GetContextProperty(null, (GetContextString)ALC_ALL_DEVICES_SPECIFIER);
            string defaultDevice = alContext.GetContextProperty(null, (GetContextString)ALC_DEFAULT_ALL_DEVICES_SPECIFIER);
            string currentDevice = alContext.GetContextProperty(device, (GetContextString)ALC_ALL_DEVICES_SPECIFIER);

            if (defaultDevice == currentDevice)
                triedDefaultDevice = null;

            bool defaultChanged = !string.IsNullOrEmpty(defaultDevice) && defaultDevice != currentDevice && defaultDevice != triedDefaultDevice;

            if (!IsDeviceConnected() || defaultChanged)
            {
                // only try a default device once so that we don't reopen all the time if it can't be used
                if (defaultChanged)
                    triedDefaultDevice = defaultDevice;

                reopenDevices?.ReopenDevice(device, null, null); // keeps context, source and buffers
            }
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public int SampleRate { get; } = 44100; // songs are also created without an available audio device

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Available { get; private set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Enabled
    {
        get => enabled;
        set
        {
            lock (deviceLock)
            {
                if (enabled == value)
                    return;

                if (!value)
                    playRequested = false;

                if (!value && Available && Streaming)
                {
                    Stop();
                    Reset();
                }

                enabled = value;
            }
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Streaming => streaming;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public float Volume
    {
        get => volume;
        set
        {
            value = Math.Max(0.0f, Math.Min(value, 1.0f));

            lock (deviceLock)
            {
                if (Util.FloatEqual(volume, value))
                    return;

                volume = value;

                if (Available)
                    al.SetSourceProperty(source, SourceFloat.Gain, volume);
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        deviceMonitorCancellation.Cancel();
        deviceMonitorThread?.Join();
        deviceMonitorCancellation.Dispose();

        if (Streaming)
            Stop();

        var cts = Interlocked.Exchange(ref cancellationTokenSource, null);
        cts?.Dispose();

        if (Available)
        {
            al.DeleteSource(source);
            foreach (var audioBuffer in audioBuffers)
                audioBuffer.Value?.Dispose();
            alContext.DestroyContext(context);
            alContext.CloseDevice(device);
        }

        al?.Dispose();
        alContext?.Dispose();

        streaming = false;
        Enabled = false;
        Available = false;
        disposed = true;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Start()
    {
        lock (deviceLock)
        {
            if (!Enabled)
                return;

            if (!Available)
            {
                playRequested = pendingStream != null;
                return;
            }

            if (Streaming)
                return;

            if (source == 0)
                throw new NotSupportedException("Start was called without a valid source.");

            if (currentBuffer == null)
                return;

            streaming = true;

            var cts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref cancellationTokenSource, cts);

            old?.Dispose();
            currentBuffer?.Play(cts.Token);
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Stop()
    {
        lock (deviceLock)
        {
            playRequested = false;

            if (!Available || !Enabled)
                return;

            if (!Streaming)
                return;

            if (source == 0)
                throw new NotSupportedException("Stop was called without a valid source.");

            streaming = false;
            var cts = Interlocked.Exchange(ref cancellationTokenSource, null);
            cts?.Cancel();

            if (currentBuffer != null)
                currentBuffer.Stop();
            else
                al.SourceStop(source);

            cts?.Dispose();
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, bool sample8Bit = true)
    {
        lock (deviceLock)
        {
            if (!Available)
            {
                pendingStream = (audioStream, channels, sampleRate, sample8Bit);
                return;
            }

            if (!audioBuffers.TryGetValue(audioStream, out var buffer))
                currentBuffer = audioBuffers[audioStream] = new AudioBuffers(al, source, channels, sampleRate, sample8Bit, audioStream, IsDeviceConnected);
            else
                currentBuffer = buffer;
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Reset()
    {
        lock (deviceLock)
        {
            if (!Available)
                return;

            al.SetSourceProperty(source, SourceInteger.Buffer, 0u);
        }
    }
}
