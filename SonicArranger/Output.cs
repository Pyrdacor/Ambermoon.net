using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicArranger
{
    public static class Output
    {
        struct Adsr
        {
            /// <summary>
            /// Volume amplitudes (0x00 to 0x40)
            /// </summary>
            public byte[] Data;
            /// <summary>
            /// -1 means no repeat
            /// </summary>
            public int RepeatOffset;
            /// <summary>
            /// In bytes
            /// </summary>
            public int Delay;
        }

        struct Amf
        {
            /// <summary>
            /// Unknown yet
            /// </summary>
            public byte[] Data;
            /// <summary>
            /// -1 means no repeat
            /// </summary>
            public int RepeatOffset;
            /// <summary>
            /// In bytes
            /// </summary>
            public int Delay;
        }

        struct SampleInstrument
        {
            public byte[] Data;
            public double Volume;
            /// <summary>
            /// -1 means no repeat
            /// </summary>
            public int RepeatOffset;
            public Adsr? Adsr;
            public Amf? Amf;
            public bool Synthetic;
            /// <summary>
            /// -8 to +7
            /// </summary>
            public int FineTuneFactor;
        }

        class TrackData
        {
            /// <summary>
            /// 0-based
            /// </summary>
            public int NoteId;
            /// <summary>
            /// 0-based index
            /// </summary>
            public int Instrument;
            public double Volume;
            public int Speed;
        }

        struct Track
        {
            public TrackData[] Data;
        }

        // TODO: WIP
        class Vibrato
        {
            readonly int delay;
            readonly double frequency;
            readonly double depth;

            // TODO: calculate depth from Instrument.VibLevel
            // TODO: calculate frequency from Instrument.VibSpeed
            public Vibrato(int delay, double frequency, double depth)
            {
                this.delay = delay;
                this.frequency = frequency;
                this.depth = depth;
            }

            public void ProcessSamples(byte[] input, double sampleRate)
            {
                if (sampleRate <= 0.0)
                    throw new ArgumentOutOfRangeException("Sample rate was out of range.");

                double halfDelayTime = 0.5 * delay / sampleRate;
                double phaseIncrement = 2.0 * Math.PI * frequency / sampleRate;
                double phase = 0.0;
                int writeIndex = 0;
                var buffer = new double[delay];

                for (int i = 0; i < input.Length; ++i)
                {
                    double offset = halfDelayTime * (1.0 + Math.Sin(phase) * depth) * sampleRate;

                    if (offset > delay)
                        offset = delay;

                    double readOffset = writeIndex - offset;
                    readOffset = readOffset >= 0.0
                        ? (readOffset < delay ? readOffset : readOffset - delay)
                        : readOffset + delay;

                    // Interpolate
                    int readPosition = (int)readOffset;
                    double frac = readOffset - readPosition;
                    double nextValue = readPosition == buffer.Length - 1 ? buffer[0] : buffer[readPosition + 1];
                    double output = buffer[readPosition] + frac * (nextValue - buffer[readPosition]);

                    buffer[writeIndex] = input[i];
                    input[i] = (byte)Math.Round(output * 127.0 + 128.0);

                    writeIndex = (writeIndex + 1) % buffer.Length;
                    phase += phaseIncrement;
                }
            }
        }

        /// <summary>
        /// Converts a 0-based note id to a frequency.
        /// 
        /// Not used anymore but I leave it here.
        /// </summary>
        public static double NoteToFreq(int id)
        {
            return 440.0 * Math.Pow(2, (id - 69) / 12);
        }

        static Adsr? LoadAdsr(SonicArrangerFile sonicArrangerFile, Instrument instrument)
        {
            if (instrument.AdsrLength == 0)
                return null;

            return new Adsr
            {
                Data = sonicArrangerFile.AdsrWaves[instrument.AdsrWave].Data.Take(instrument.AdsrLength).ToArray(),
                RepeatOffset = instrument.AdsrRepeat == 0 ? -1 : instrument.AdsrRepeat - instrument.AdsrLength,
                Delay = instrument.AdsrDelay
            };
        }

        static Amf? LoadAmf(SonicArrangerFile sonicArrangerFile, Instrument instrument)
        {
            if (instrument.AmfLength == 0)
                return null;

            return new Amf
            {
                Data = sonicArrangerFile.AmfWaves[instrument.AmfWave].Data.Take(instrument.AmfLength).ToArray(),
                RepeatOffset = instrument.AmfRepeat == 0 ? -1 : instrument.AmfRepeat - instrument.AmfLength,
                Delay = instrument.AmfDelay
            };
        }

        static double GetBytesPerSecond(int speed)
        {            
            return 1000.0 / GetPatternEntryDuration(speed);
        }

        static double GetPatternEntryDuration(int speed)
        {
            const int quarterNoteDuration = 60000 / 125;
            int patternEntryDenominator = 96 / speed;
            int patternEntryFactor = patternEntryDenominator / 4;
            return quarterNoteDuration / patternEntryFactor;
        }

        /// <summary>
        /// Creates 8-bit PCM data from a loaded SA file with the given
        /// sample rate. Note that the sample rate is not used for
        /// loading or processing the SA data but only for the
        /// output resolution.
        /// 
        /// Note: Even though the data type is unsigned, the data itself
        /// really is signed data (-128 to +127). But most software uses
        /// unsigned bytes as input so this should be ok.
        /// </summary>
        public static byte[] Create(SonicArrangerFile sonicArrangerFile, uint sampleRate = 44100)
        {
            // 4 tracks with up to 16 channels
            var inst = sonicArrangerFile.Instruments;
            var ovtb = sonicArrangerFile.Voices;
            var stbl = sonicArrangerFile.Songs[0];
            var notb = sonicArrangerFile.Notes;
            int size = (stbl.StopPos - stbl.StartPos) * stbl.PatternLength;
            double bytesPerSecond = GetBytesPerSecond(stbl.SongSpeed);
            double songLength = size / bytesPerSecond;
            var instruments = new List<SampleInstrument>();
            for (int i = 0; i < inst.Length; i++)
            {
                var instr = inst[i];
                int len = instr.Length * 2;
                int repeat = instr.Repeat * 2;
                int fineTuning = instr.FineTuning & 0xf; // TODO: SA seems to support up to 0xff
                var instrument = new SampleInstrument
                {
                    Data = instr.SynthMode
                        ? sonicArrangerFile.Waves[instr.SampleWaveNo].Data.Take(len).ToArray()
                        : sonicArrangerFile.Samples[instr.SampleWaveNo].Data.Take(len + repeat).ToArray(),
                    Volume = instr.Volume / 64.0,
                    RepeatOffset = repeat == 0 ? -1 : len,
                    Adsr = LoadAdsr(sonicArrangerFile, instr),
                    Amf = LoadAmf(sonicArrangerFile, instr),
                    Synthetic = instr.SynthMode,
                    FineTuneFactor = fineTuning < 8 ? fineTuning : fineTuning - 16
                };
                instruments.Add(instrument);
            }
            var tracks = new Track[4];
            for (int i = 0; i < 4; ++i)
                tracks[i].Data = new TrackData[size];
            var trackVolume = new double[4] { 1.0, 1.0, 1.0, 1.0 };
            var trackSpeed = new int[4] { stbl.SongSpeed, stbl.SongSpeed, stbl.SongSpeed, stbl.SongSpeed };
            for (int i = stbl.StartPos; i < stbl.StopPos; i++)
            {
                for (int track = 0; track < 4; track++)
                {
                    var voice = ovtb[i * 4 + track];

                    for (int k = 0; k < stbl.PatternLength; k++)
                    {
                        var note = notb[voice.NoteAddress + k];

                        if (note.Command == 12) // Set volume
                            trackVolume[track] = Math.Min(64, (int)note.CommandInfo) / 64.0;
                        else if (note.Command == 15) // Set speed / tempo
                            trackSpeed[track] = note.CommandInfo;

                        if (note.Command != 0 && note.Command != 12 && note.Command != 15)
                        {
                            Console.WriteLine($"Command {note.Command} with param {note.CommandInfo}");
                        }

                        int index = i * stbl.PatternLength + k;
                        if (note.Value != 0)
                        {
                            int instr = note.Instrument - 1;

                            if (instr >= 0)
                            {
                                if (instr == 9 || instr >= 15)
                                {
                                    // TODO
                                    Console.WriteLine("Instrument 9 or 15");
                                }

                                tracks[track].Data[index] = new TrackData
                                {
                                    NoteId = note.Value - 1,
                                    Instrument = instr,
                                    Volume = trackVolume[track],
                                    Speed = trackSpeed[track]
                                };
                                continue;
                            }
                            else
                            {
                                // TODO
                                Console.WriteLine("No instrument");
                            }
                        }

                        tracks[track].Data[index] = null;
                    }
                }
            }

            // TODO: WIP
            double GetWaveAmplitude(byte[] waveData, double sampleTime, double frequency,
                int repeatOffset, int delayInBytes, double scale, double valueAdd)
            {
                double samplesPerSecond = frequency * waveData.Length;
                double sampleIndex = sampleTime * samplesPerSecond;

                if (sampleIndex < delayInBytes)
                    return 0.0;

                if (sampleIndex >= waveData.Length)
                {
                    if (repeatOffset >= 0 && repeatOffset < waveData.Length)
                    {
                        int repeatLength = waveData.Length - repeatOffset;
                        sampleIndex = repeatOffset + (sampleIndex - waveData.Length) % repeatLength;
                    }
                    else
                    {
                        sampleIndex %= waveData.Length;
                    }
                }

                int byteIndex = (int)sampleIndex;
                double gamma = sampleIndex - byteIndex;

                // Interpolation
                double leftValue = waveData[byteIndex] + valueAdd;
                double rightValue = (byteIndex == waveData.Length - 1 ? waveData[0] : waveData[byteIndex + 1]) + valueAdd;
                double value = leftValue + gamma * (rightValue - leftValue);

                return scale * value;
            }

            double GetInstrumentAmplitude(int instrumentIndex, double sampleTime, int noteId)
            {
                var instrument = instruments[instrumentIndex];
                double adsrFactor = 1.0;
                
                if (instrument.Adsr != null)
                {
                    var adsr = instrument.Adsr.Value;
                    adsrFactor = GetWaveAmplitude(adsr.Data, sampleTime, /*tempo * adsr.Data.Length / 1000.0*/NoteToFreq(noteId), adsr.RepeatOffset, adsr.Delay - 1, 1.0 / 64.0, 0.0);
                }

                // TODO: AMF?

                double soundData;

                if (instrument.Synthetic) // Synth wave
                {
                    soundData = GetWaveAmplitude(instrument.Data, sampleTime, NoteToFreq(noteId), instrument.RepeatOffset, 0, 1.0 / 128.0, -128.0);
                }
                else // Sampled
                {
                    double noteFactor = SonicArrangerFile.GetNoteFrequencyFactor(noteId, instrument.FineTuneFactor);
                    int sampleIndex = (int)Math.Round(sampleTime * SonicArrangerFile.SampleRate * noteFactor);
                    if (sampleIndex >= instrument.Data.Length)
                    {
                        if (instrument.RepeatOffset < 0 || instrument.RepeatOffset == instrument.Data.Length) // no repeat
                            return 0.0;

                        int repeatLength = instrument.Data.Length - instrument.RepeatOffset;
                        sampleIndex = instrument.RepeatOffset + (sampleIndex - instrument.RepeatOffset) % repeatLength;
                    }
                    soundData = unchecked((sbyte)instrument.Data[sampleIndex]) / 128.0;
                }

                return adsrFactor * soundData;
            }

            byte[] data = new byte[(int)Math.Ceiling(songLength * sampleRate)];
            double tickTime = 1.0 / sampleRate;
            double time = 0.0;
            TrackData[] lastTrackData = new TrackData[4];
            double[] sampleTimes = new double[4];
            for (int i = 0; i < 4; ++i)
                trackSpeed[i] = stbl.SongSpeed;

            for (int i = 0; i < data.Length; ++i)
            {
                double amplitude = 0.0;

                for (int t = 0; t < 4; ++t)
                {
                    int trackDataIndex = (int)Math.Floor(GetBytesPerSecond(trackSpeed[t]) * time);
                    var trackData = tracks[t].Data[trackDataIndex];
                    var lastData = lastTrackData[t];
                    bool fadeOut = false;

                    if (lastData != null && trackData == null)
                    {
                        trackData = lastData;
                        fadeOut = true;
                    }
                    else if (lastData == null || trackData == null ||
                        trackData.Instrument != lastData.Instrument ||
                        trackData.NoteId != lastData.NoteId)
                    {
                        sampleTimes[t] = 0.0;
                    }

                    double value;

                    if (trackData == null)
                        value = 0.0;
                    else
                    {
                        trackSpeed[t] = trackData.Speed;
                        var instrument = instruments[trackData.Instrument];
                        value = instrument.Volume * trackData.Volume * GetInstrumentAmplitude(trackData.Instrument, sampleTimes[t],
                            trackData.NoteId);

                        if (fadeOut)
                        {
                            double noteFactor = SonicArrangerFile.GetNoteFrequencyFactor(trackData.NoteId, instrument.FineTuneFactor);
                            double noteTime = instrument.Data.Length / (SonicArrangerFile.SampleRate * noteFactor);
                            var patternEntryDuration = GetPatternEntryDuration(trackSpeed[t]) / 1000.0; // in seconds
                            if (sampleTimes[t] >= patternEntryDuration &&
                                sampleTimes[t] >= noteTime)
                            {
                                trackData = null;
                            }
                        }
                    }

                    amplitude += value;
                    sampleTimes[t] += tickTime;
                    lastTrackData[t] = trackData;
                }

                data[i] = (byte)unchecked((sbyte)Math.Round(127.0 * (Math.Max(-1.0, Math.Min(amplitude, 1.0)))));

                time += tickTime;
            }

            return data;
        }
    }
}
