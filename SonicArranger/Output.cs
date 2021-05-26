﻿using System;
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
            /// -128 to +127
            /// </summary>
            public int FineTune;
            public Instrument.Effect EffectNumber;
            public short Effect1;
            public short Effect2;
            public short Effect3;
            public short EffectDelay;

            public class InstrumentState
            {
                public byte[] Data;
                public int EffectDelay;
            }

            // State data
            public InstrumentState State;
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
        /// Creates 8-bit PCM data from a loaded SA file with the default
        /// sample rate of 22168.
        /// 
        /// Note: Even though the data type is unsigned, the data itself
        /// really is signed data (-128 to +127). But most software uses
        /// unsigned bytes as input so this should be ok.
        /// </summary>
        public static byte[] Create(SonicArrangerFile sonicArrangerFile, out uint sampleRate)
        {
            sampleRate = SonicArrangerFile.SampleRate;

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
                int fineTuning = instr.FineTuning;
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
                    FineTune = fineTuning < 128 ? fineTuning : fineTuning - 256,
                    EffectNumber = instr.EffectNumber,
                    Effect1 = instr.Effect1,
                    Effect2 = instr.Effect2,
                    Effect3 = instr.Effect3,
                    EffectDelay = instr.EffectDelay,
                    State = new SampleInstrument.InstrumentState()
                };
                // Initialize state
                instrument.State.Data = new byte[instrument.Data.Length];
                instrument.State.EffectDelay = instrument.EffectDelay;
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

            double tickTime = 1.0 / sampleRate;

            // TODO: WIP
            double GetWaveAmplitude(byte[] waveData, double sampleTime, double frequency,
                int repeatOffset, int delayInBytes, double scale, Func<byte, double> valueTransform, SampleInstrument? instrument)
            {
                int totalSize = delayInBytes + waveData.Length;
                double samplesPerSecond = frequency * totalSize; // e.g. 220Hz (220 periods per sec) and 32 bytes per period = 7040 bytes per sec
                double sampleIndex = sampleTime * samplesPerSecond; // sampleTime is in seconds

                if (sampleIndex < delayInBytes)
                    return 0.0;

                sampleIndex -= delayInBytes;
                int startIndex = 0;

                if (sampleIndex >= waveData.Length)
                {
                    if (repeatOffset >= 0 && repeatOffset < waveData.Length)
                    {
                        startIndex = repeatOffset;
                        int repeatLength = waveData.Length - repeatOffset;
                        sampleIndex = repeatOffset + (sampleIndex - waveData.Length) % repeatLength;
                    }
                    else
                    {
                        sampleIndex %= waveData.Length;
                    }
                }

                int leftIndex = (int)sampleIndex;
                int rightIndex = leftIndex == waveData.Length - 1 ? startIndex : leftIndex + 1;
                double gamma = sampleIndex - leftIndex;

                double TransformValue(byte b) => valueTransform?.Invoke(b) ?? b;

                // Interpolation
                double leftValue = TransformValue(waveData[leftIndex]);
                double rightValue = TransformValue(waveData[rightIndex]);
                double value;

                if (instrument != null)
                {
                    if (--instrument.Value.State.EffectDelay == 0)
                    {
                        instrument.Value.State.EffectDelay = instrument.Value.EffectDelay;
                        var effect = instrument.Value.EffectNumber;
                        var param1 = instrument.Value.Effect1;
                        var param2 = instrument.Value.Effect2;
                        var param3 = instrument.Value.Effect3;
                        var delay = instrument.Value.EffectDelay - 1;

                        switch (effect)
                        {
                            case Instrument.Effect.WaveNegator:
                            {
                                // TODO: Test this!
                                var startPnt = param2;
                                var stopPnt = param3;
                                if (leftIndex >= startPnt && leftIndex <= stopPnt)
                                    leftValue = -leftValue;
                                if (rightIndex >= startPnt && rightIndex <= stopPnt)
                                    rightValue = -rightValue;
                                value = leftValue + gamma * (rightValue - leftValue);
                                break;
                            }
                            case Instrument.Effect.LowPassFilter1:
                            {
                                // TODO: WIP
                                // TODO: Test this!
                                // TODO: effect and normal delay?
                                var deltaVal = param1;
                                var startPnt = param2;
                                var stopPnt = Math.Min(param3, waveData.Length - 1);
                                if (startPnt >= 0 && startPnt < waveData.Length - 1 && stopPnt > startPnt)
                                {
                                    for (int i = startPnt; i <= stopPnt && i < waveData.Length; ++i)
                                    {
                                        var next = i == stopPnt ? waveData[startPnt] : waveData[i + 1];
                                        var diff = Math.Abs(waveData[i] - next);

                                        if (deltaVal < diff)
                                        {
                                            if (next >= waveData[i])
                                                waveData[i] += 2;
                                            else
                                                waveData[i] -= 2;
                                        }
                                    }
                                }
                                value = leftValue + gamma * (rightValue - leftValue);
                                break;
                            }
                            // TODO: other effects
                            case Instrument.Effect.NoEffect:
                            default:
                                value = leftValue + gamma * (rightValue - leftValue);
                                break;
                        }
                    }
                    else
                    {
                        value = leftValue + gamma * (rightValue - leftValue);
                    }
                }
                else
                {
                    value = leftValue + gamma * (rightValue - leftValue);
                }

                //if (instrument == null)
                //    System.IO.File.AppendAllText(@"D:\Programmierung\C#\Projects\Ambermoon\ambermoon.net\FileSpecs\Extract\decoded\Music.amb\test.csv", $"{scale * value:0.00}\r\n");

                return scale * value;
            }

            double GetInstrumentAmplitude(int instrumentIndex, double sampleTime, int noteId, double patternDuration,
                out bool finished)
            {
                var instrument = instruments[instrumentIndex];
                finished = false;

                if (instrument.Synthetic) // Synth wave
                {
                    if (sampleTime >= patternDuration)
                    {
                        finished = true;
                        return 0.0;
                    }

                    var frequency = NoteToFreq(noteId);
                    var output = GetWaveAmplitude(instrument.State.Data, sampleTime, frequency, -1, 0, 1.0 / 128.0,
                        b => unchecked((sbyte)b), instrument);

                    if (instrument.Adsr != null)
                    {
                        var adsr = instrument.Adsr.Value;
                        // The ADSR should run once per pattern (duration) so the frequency is
                        // 1.0 / patternDuration.
                        double adsrFrequency = 2.048;// 1.0 / patternDuration;
                        output *= GetWaveAmplitude(adsr.Data, sampleTime, adsrFrequency,
                            adsr.RepeatOffset, adsr.Delay - 1, 1.0 / (instrument.Volume * 64.0), null, null);
                    }

                    // TODO: AMF?

                    return output;
                }
                else // Sampled
                {
                    double noteFactor = SonicArrangerFile.GetNoteFrequencyFactor(noteId, instrument.FineTune);

                    if (sampleTime >= patternDuration)
                    {
                        double noteTime = instrument.State.Data.Length / (SonicArrangerFile.SampleRate * noteFactor);
                        if (sampleTime >= noteTime)
                        {
                            finished = true;
                            return 0.0;
                        }
                    }

                    int sampleIndex = (int)Math.Round(sampleTime * SonicArrangerFile.SampleRate * noteFactor);
                    if (sampleIndex >= instrument.State.Data.Length)
                    {
                        if (instrument.RepeatOffset < 0 || instrument.RepeatOffset == instrument.State.Data.Length) // no repeat
                            return 0.0;

                        int repeatLength = instrument.State.Data.Length - instrument.RepeatOffset;
                        sampleIndex = instrument.RepeatOffset + (sampleIndex - instrument.RepeatOffset) % repeatLength;
                    }
                    return unchecked((sbyte)instrument.State.Data[sampleIndex]) / 128.0;
                }
            }

            byte[] data = new byte[(int)Math.Ceiling(songLength * sampleRate)];
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
                        (trackData.NoteId != lastData.NoteId && !instruments[trackData.Instrument].Synthetic))
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
                        if (sampleTimes[t] == 0.0)
                            Array.Copy(instrument.Data, instrument.State.Data, instrument.State.Data.Length);
                        value = instrument.Volume * trackData.Volume * GetInstrumentAmplitude(trackData.Instrument, sampleTimes[t],
                            trackData.NoteId, GetPatternEntryDuration(trackSpeed[t]) / 1000.0, out bool finished);

                        if (fadeOut && finished)
                            trackData = null;
                    }

                    amplitude += value;
                    sampleTimes[t] += tickTime;
                    lastTrackData[t] = trackData;
                }

                data[i] = unchecked((byte)Math.Round(127.0 * (Math.Max(-1.0, Math.Min(amplitude, 1.0)))));

                time += tickTime;
            }

            return data;
        }
    }
}