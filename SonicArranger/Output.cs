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
        }

        struct Track
        {
            public TrackData[] Data;
        }

        /// <summary>
        /// Converts a 0-based note id to a frequency.
        /// 
        /// Not used anymore but I leave it here.
        /// </summary>
        public static double MidiNoteToFreq(int id)
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
            int quarterNoteDuration = 60000 / 125;
            int patternEntryDenominator = 96 / stbl.SongSpeed;
            int patternEntryFactor = patternEntryDenominator / 4;
            int patternEntryDuration = quarterNoteDuration / patternEntryFactor;
            double bytesPerSecond = 1000.0 / patternEntryDuration;
            double songLength = size / bytesPerSecond;
            var instruments = new List<SampleInstrument>();
            for (int i = 0; i < inst.Length; i++)
            {
                var instr = inst[i];
                int len = instr.Length * 2;
                int repeat = instr.Repeat * 2;
                instruments.Add(new SampleInstrument
                {
                    Data = instr.SynthMode == 0
                        ? sonicArrangerFile.Samples[instr.SampleWaveNo].Data.Take(len).ToArray()
                        : sonicArrangerFile.Waves[instr.SampleWaveNo].Data.Take(len).ToArray(),
                    Volume = (double)instr.Volume / 0x40,
                    RepeatOffset = repeat == 0 ? -1 : len - repeat,
                    Adsr = LoadAdsr(sonicArrangerFile, instr),
                    Amf = LoadAmf(sonicArrangerFile, instr),
                    Synthetic = instr.SynthMode != 0
                });
            }
            var tracks = new Track[4];
            for (int i = 0; i < 4; ++i)
                tracks[i].Data = new TrackData[size];
            for (int i = stbl.StartPos; i < stbl.StopPos; i++)
            {
                for (int track = 0; track < 4; track++)
                {
                    var voice = ovtb[i * 4 + track];

                    for (int k = 0; k < stbl.PatternLength; k++)
                    {
                        var note = notb[voice.NoteAddress + k];

                        if (k == 0 && note.Command == 15) // set tempo
                        {
                            // TODO
                            //bpm = stbl.BPMFromSpeed(note.CommandInfo);
                            Console.WriteLine($"Set song speed to {note.CommandInfo}");
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
                                    Instrument = instr
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

            double GetWaveAmplitude(byte[] waveData, double timeInSecond, int repeatOffset, int delayInBytes, double scale)
            {
                const double baseLength = 128.0;
                int lowerIndex = (int)Math.Floor(timeInSecond * baseLength);

                if (lowerIndex >= waveData.Length)
                {
                    if (repeatOffset < 0 || repeatOffset >= waveData.Length)
                        return 0.0;

                    lowerIndex -= waveData.Length;
                    int repeatLength = waveData.Length - repeatOffset;
                    lowerIndex = repeatOffset + lowerIndex % repeatLength;
                }

                if (lowerIndex < delayInBytes)
                    return 0.0;

                int lowerValue = unchecked((sbyte)waveData[lowerIndex]);
                int upperValue;

                if (lowerIndex + 1 >= waveData.Length)
                {
                    if (repeatOffset < 0 || repeatOffset >= waveData.Length)
                        upperValue = 0;
                    else
                    {
                        int upperIndex = lowerIndex + 1 - waveData.Length;
                        int repeatLength = waveData.Length - repeatOffset;
                        upperIndex = repeatOffset + upperIndex % repeatLength;
                        upperValue = unchecked((sbyte)waveData[upperIndex]);
                    }
                }
                else
                {
                    upperValue = unchecked((sbyte)waveData[lowerIndex + 1]);
                }

                double d = timeInSecond * baseLength - lowerIndex;

                return scale * (lowerValue + d * (upperValue - lowerValue));
            }

            double GetInstrumentAmplitude(int index, double sampleTime, double factor)
            {
                var instrument = instruments[index];
                double adsrFactor = 1.0;
                
                // 130.81 Hz (C3)
                if (instrument.Adsr != null)
                {
                    var adsr = instrument.Adsr.Value;
                    adsrFactor = GetWaveAmplitude(adsr.Data, sampleTime % 1.0, adsr.RepeatOffset, adsr.Delay, 1.0 / 64.0);
                }

                // TODO: AMF?

                double soundData;

                if (instrument.Synthetic) // Synth wave
                {
                    soundData = GetWaveAmplitude(instrument.Data, sampleTime % 1.0, instrument.RepeatOffset, 0, 1.0 / 128.0);
                }
                else // Sampled
                {
                    int sampleIndex = (int)Math.Round(sampleTime * SonicArrangerFile.SampleRate * factor);
                    if (sampleIndex >= instrument.Data.Length)
                    {
                        if (instrument.RepeatOffset < 0 || instrument.RepeatOffset >= instrument.Data.Length) // no repeat
                            return 0.0;

                        int repeatLength = instrument.Data.Length - instrument.RepeatOffset;
                        sampleIndex = instrument.RepeatOffset + (sampleIndex - instrument.Data.Length) % repeatLength;
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

            for (int i = 0; i < data.Length; ++i)
            {
                double amplitude = 0.0;
                int trackDataIndex = (int)Math.Floor(bytesPerSecond * time);

                for (int t = 0; t < 4; ++t)
                {
                    var trackData = tracks[t].Data[trackDataIndex];
                    var lastData = lastTrackData[t];
                    bool fadeOut = false;

                    if (lastData != null && trackData == null)
                    {
                        trackData = lastData;
                        fadeOut = true;
                    }
                    else if (lastData == null || trackData == null ||
                        trackData.Instrument != lastData.Instrument// ||
                        //Math.Abs(trackData.Frequency - lastData.Frequency) > 0.00001)
                        /*trackData.NoteId != lastData.NoteId*/)
                    {
                        sampleTimes[t] = 0.0;
                    }

                    double value = trackData == null ? 0.0 :
                        instruments[trackData.Instrument].Volume * GetInstrumentAmplitude(trackData.Instrument, sampleTimes[t],
                        SonicArrangerFile.GetNoteFrequencyFactor(trackData.NoteId));

                    amplitude += value;
                    sampleTimes[t] += tickTime;

                    if (fadeOut && Math.Abs(value) < 0.0001)
                        lastTrackData[t] = null;
                    else
                        lastTrackData[t] = trackData;
                }

                data[i] = (byte)unchecked((sbyte)Math.Round(127.0 * (Math.Max(-1.0, Math.Min(amplitude, 1.0)))));

                time += tickTime;
            }

            return data;
        }
    }
}
