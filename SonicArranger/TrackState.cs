using System;
using System.Linq;

namespace SonicArranger
{
    internal class TrackState
    {
        class PlayState
        {
            public Instrument? Instrument { get; set; }
            public int EffectDelayCounter { get; set; }
            public int AmfDelayCounter { get; set; }
            public int AdsrDelayCounter { get; set; }
            public int SustainCounter { get; set; }
            public int AdsrIndex { get; set; }
            public int AmfIndex { get; set; }
            public int NotePeriod { get; set; }
            /// <summary>
            /// This is used if the ADSR repeat portion is
            /// 0 and the full ADSR wave was processed.
            /// </summary>
            public bool AdsrFinished { get; set; }
            /// <summary>
            /// This is used for effects that can theoretically be
            /// applied repeated but has a repeat portion of 0.
            /// </summary>
            public bool EffectFinished { get; set; }
            public bool NoteOff { get; set; }
            public int NoteVolume { get; set; }
            public int FadeOutVolume { get; set; }
            public int VibratoDelayCounter { get; set; }
            public int VibratoIndex { get; set; }
            // A4+0x84
            public int Finetuning { get; set; }
            // A4+0x86
            public int PitchReductionPerTick { get; set; }
            public int CurrentEffectRuns { get; set; }
            public int CurrentEffectIndex { get; set; }
            public int LastNotePeriod { get; set; }
            public int LastNoteIndex { get; set; }
            public int CurrentNoteIndex { get; set; }
            public bool FirstNoteTick { get; set; }
        }

        readonly PaulaState paulaState;
        readonly PaulaState.TrackState state;
        readonly int trackIndex;
        readonly SonicArrangerFile sonicArrangerFile;
        readonly PlayState playState = new PlayState();

        public TrackState(int index, PaulaState paulaState, SonicArrangerFile sonicArrangerFile)
        {
            if (index < 0 || index > 3)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (paulaState == null)
                throw new ArgumentNullException(nameof(paulaState));

            this.paulaState = paulaState;
            trackIndex = index;
            state = paulaState.Tracks[index];
            this.sonicArrangerFile = sonicArrangerFile;
            paulaState.TrackFinished += TrackFinished;
        }

        public void ProcessNoteCommand(byte command, byte param, ref int songSpeed)
        {
            switch (command)
            {
                case 0x0: // No command
                    break;
                case 0x1: // Pitch fade?
                    // TODO
                    break;
                case 0x2: // Set ADSR data index?
                    playState.AdsrIndex = param; // TODO: out of range checks, etc
                    break;
                case 0x3:
                    // TODO
                    break;
                case 0x4: // Set vibrato
                    // TODO: vibrato
                    // Speed = 2 * (param >> 4)
                    // Level = 160 - (param & 0xf) * 16
                    break;
                case 0x5:
                    // TODO
                    break;
                case 0x6: // Set instrument volume
                    // TODO
                    break;
                case 0x7: // Set note period value
                    // TODO
                    break;
                case 0x8: // Set note period value to 0 (mute note)
                    // TODO
                    break;
                case 0x9:
                    // TODO
                    break;
                case 0xA:
                    // TODO
                    break;
                case 0xB:
                    // TODO
                    break;
                case 0xC: // Set volume
                    playState.NoteVolume = Math.Max(0, Math.Min(64, (int)param));
                    playState.FadeOutVolume = ((paulaState.MasterVolume * playState.NoteVolume) >> 6) * 4;
                    break;
                case 0xD:
                    // TODO
                    break;
                case 0xE: // Enable LED (and therefore the LPF)
                    // TODO
                    break;
                case 0xF: // Set speed
                    songSpeed = Math.Max(0, Math.Min((int)param, 16));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid note command.");
            }
        }

        void InitState(int noteId, int instrumentIndex, double currentPlayTime)
        {
            if (instrumentIndex <= 0)
                throw new ArgumentOutOfRangeException("Expected instrument index to be > 0.");

            var instrument = sonicArrangerFile.Instruments[instrumentIndex - 1];

            ResetEffectState();

            playState.Instrument = instrument;
            playState.NoteOff = false;
            playState.NotePeriod = Tables.NotePeriodTable[noteId];
            playState.NoteVolume = instrument.Volume;
            playState.VibratoDelayCounter = instrument.VibDelay;
            playState.VibratoIndex = 0;
            playState.LastNotePeriod = 0;
            playState.PitchReductionPerTick = 0;

            state.Data = instrument.SynthMode
                ? sonicArrangerFile.Waves[instrument.SampleWaveNo].Data
                : sonicArrangerFile.Samples[instrument.SampleWaveNo].Data;
            int length = instrument.Length * 2;
            if (!instrument.SynthMode && instrument.Repeat > 1)
                length += instrument.Repeat * 2;
            if (length > state.Data.Length)
                throw new ArgumentOutOfRangeException("Length + repeat is greater than the sample data size.");
            else if (length < state.Data.Length)
                state.Data = state.Data.Take(length).ToArray();
            state.DataIndex = 0;
            state.Period = playState.NotePeriod;
            state.Volume = (playState.NoteVolume * paulaState.MasterVolume) >> 6;
            paulaState.StartTrackData(trackIndex, currentPlayTime);
        }

        void ResetEffectState()
        {
            var instrument = playState.Instrument;
            playState.Finetuning = instrument?.FineTuning ?? 0;
            playState.EffectDelayCounter = instrument?.EffectDelay ?? 1;
            playState.AmfDelayCounter = instrument?.AmfDelay ?? 1;
            playState.AdsrDelayCounter = instrument?.AdsrDelay ?? 1;
            playState.SustainCounter = instrument?.SustainVal ?? 1;
            playState.CurrentEffectIndex = instrument?.Effect2 ?? 0;
            playState.CurrentEffectRuns = 0;
            playState.AdsrIndex = 0;
            playState.AmfIndex = 0;
            playState.AdsrFinished = false;
            playState.EffectFinished = false;
            playState.FadeOutVolume = ((playState.NoteVolume * paulaState.MasterVolume) >> 6) * 4;
        }

        void Mute()
        {
            playState.NoteOff = true;
            playState.AdsrFinished = true;
            playState.EffectFinished = true;
            playState.NoteVolume = 0;
            playState.FadeOutVolume = 0;
            paulaState.StopTrack(trackIndex);
        }

        /// <summary>
        /// This should be called whenever a new note or instrument
        /// is played.
        /// </summary>
        public void Play(int noteId, int noteInstrument, int noteTranspose, int soundTranspose,
            Note.Flags noteFlags, double currentPlayTime)
        {
            playState.FirstNoteTick = true;

            if (noteId == 0)
            {
                if (noteInstrument != 0)
                    InitState(0, noteInstrument, currentPlayTime);
            }
            else
            {
                if (noteId != 0x80)
                {
                    if (noteId == 0x7f)
                    {
                        Mute();
                    }
                    else
                    {
                        if (!noteFlags.HasFlag(Note.Flags.DisableNoteTranspose))
                            noteId += noteTranspose;
                        if (noteId > 9 * 12)
                            throw new ArgumentOutOfRangeException(nameof(noteId));
                        if (noteInstrument != 0 && !noteFlags.HasFlag(Note.Flags.DisableSoundTranspose))
                            noteInstrument += soundTranspose;
                        playState.LastNoteIndex = playState.CurrentNoteIndex;
                        playState.CurrentNoteIndex = noteId;
                        if (playState.LastNoteIndex == 0)
                            playState.LastNoteIndex = noteId;
                        if (noteInstrument <= 0)
                        {
                            if (playState.Instrument == null)
                            {
                                Mute();                                
                                return;
                            }
                            ResetEffectState();
                        }
                        else
                        {
                            InitState(noteId, noteInstrument, currentPlayTime);
                        }                        
                    }
                }
            }
        }

        void TrackFinished(int trackIndex, double currentPlayTime)
        {
            if (this.trackIndex != trackIndex)
                return;

            if (playState.Instrument == null || playState.Instrument.Value.Repeat == 1)
            {
                // Repeat=1 is a "no loop" marker.
                paulaState.StopTrack(trackIndex);
            }
            else if (playState.Instrument.Value.Repeat > 0)
            {
                state.DataIndex = playState.Instrument.Value.Length * 2;
                paulaState.StartTrackData(trackIndex, currentPlayTime);
            }

            // Note: instrument.Template.Repeat == 0 does nothing so the data is just looped.
        }

        /// <summary>
        /// In original this is an interrupt called
        /// every 20ms by default. Use <see cref="Song.NBIrqps"/>
        /// to get a value how often this is called per second.
        /// </summary>
        public void Tick()
        {
            if (playState == null)
                return;

            bool firstTick = playState.FirstNoteTick;
            playState.FirstNoteTick = false;

            // Note fade out
            if (playState.NoteOff || playState.AdsrFinished || playState.Instrument == null)
            {
                if (playState.FadeOutVolume > 0)
                {
                    playState.FadeOutVolume -= 4;
                }
                return;
            }

            var instrument = playState.Instrument.Value;
            var period = playState.NotePeriod;

            // Vibrato effect
            if (playState.VibratoDelayCounter != -1)
            {
                if (--playState.VibratoDelayCounter == 0)
                {
                    playState.VibratoDelayCounter = instrument.VibDelay;

                    if (instrument.VibLevel != 0)
                    {
                        period += unchecked((sbyte)Tables.VibratoTable[playState.VibratoIndex]) * 4 / instrument.VibLevel;
                    }

                    playState.VibratoIndex = (playState.VibratoIndex + instrument.VibSpeed) & 0xff;
                }
            }

            // AMF (pitch amplifier)
            int amfTotalLength = Math.Min(128, instrument.AmfLength + instrument.AmfRepeat);
            if (amfTotalLength != 0 && instrument.AmfWave < sonicArrangerFile.AmfWaves.Length)
            {
                byte amfData = sonicArrangerFile.AmfWaves[instrument.AmfWave].Data[playState.AmfIndex];
                period -= unchecked((sbyte)amfData);

                if (--playState.AmfDelayCounter == 0)
                {
                    playState.AmfDelayCounter = instrument.AmfDelay;
                    ++playState.AmfIndex;

                    if (playState.AmfIndex >= amfTotalLength)
                    {
                        if (instrument.AmfRepeat == 0)
                            playState.AmfIndex = instrument.AmfLength - 1;
                        else
                            playState.AmfIndex = instrument.AmfLength;
                    }
                }
            }

            period -= playState.Finetuning;
            if (!firstTick)
                playState.Finetuning -= playState.PitchReductionPerTick;

            // Instrument effects
            if (instrument.SynthMode && !playState.EffectFinished)
            {
                // Note: Changes to pitch/period through effects is only
                // applied in next tick so only playState.Finetuning will
                // be changed and used above in the next tick.
                ApplyInstrumentEffects();
            }

            int volume = playState.NoteVolume;

            // ADSR envelop
            int adsrTotalLength = Math.Min(128, instrument.AdsrLength + instrument.AdsrRepeat);
            if (adsrTotalLength != 0 && instrument.AdsrWave < sonicArrangerFile.AdsrWaves.Length)
            {
                byte adsrData = sonicArrangerFile.AdsrWaves[instrument.AdsrWave].Data[playState.AdsrIndex];
                int adsrVolume = (adsrData * instrument.Volume) >> 6;
                volume = Math.Max(0, Math.Min(64, (volume * adsrVolume) >> 6));
                playState.FadeOutVolume = volume * 4;

                if (playState.AdsrIndex >= instrument.SustainPt)
                {
                    // Sustain mode
                    if (instrument.SustainVal != 0) // If 0, keep adsr index forever
                    {
                        if (--playState.SustainCounter == 0)
                        {
                            playState.SustainCounter = instrument.SustainVal;
                            ProcessAdsrTick();
                        }
                    }
                }
                else
                {
                    ProcessAdsrTick();
                }

                void ProcessAdsrTick()
                {
                    if (--playState.AdsrDelayCounter == 0)
                    {
                        playState.AdsrDelayCounter = instrument.AdsrDelay;
                        ++playState.AdsrIndex;

                        if (playState.AdsrIndex >= adsrTotalLength)
                        {
                            if (instrument.AdsrRepeat == 0)
                                playState.AdsrIndex = instrument.AdsrLength - 1;
                            else
                                playState.AdsrIndex = instrument.AdsrLength;

                            if (instrument.AdsrRepeat == 0 && adsrData == 0)
                                playState.AdsrFinished = true;
                        }
                    }
                }
            }
            else
            {
                // Normal volume without envelop
                volume = Math.Max(0, Math.Min(64, (volume * playState.Instrument.Value.Volume) >> 6));

                if (playState.FadeOutVolume > 0)
                {
                    playState.FadeOutVolume -= 4;
                }
            }

            // Safety checks
            if (period < 124)
                period = 124;

            // Update Paula track state
            state.Period = period;
            state.Volume = volume;
        }

        void ApplyInstrumentEffects()
        {
            var instr = playState.Instrument.Value;
            var currentSample = paulaState.CurrentSamples[trackIndex];

            if (--playState.EffectDelayCounter == 0)
            {
                playState.EffectDelayCounter = Math.Max(1, (int)instr.EffectDelay);

                switch (instr.EffectNumber)
                {
                    case Instrument.Effect.NoEffect:
                        return;
                    case Instrument.Effect.WaveNegator:
                    {
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        if (currentSample.Index >= startPos && currentSample.Index <= stopPos)
                        {
                            if (currentSample.Sample == -128)
                                currentSample.Sample = 127;
                            else
                                currentSample.Sample = (sbyte)-currentSample.Sample;
                        }
                        break;
                    }
                    case Instrument.Effect.FreeNegator:
                    {
                        if (this.playState.AdsrFinished || this.playState.EffectFinished)
                            return;
                        int effectWave = instr.Effect1;
                        int waveLen = instr.Effect2;
                        int waveRep = instr.Effect3;
                        int offset = sonicArrangerFile.Waves[effectWave].Data[this.playState.CurrentEffectRuns] & 0x7f;
                        int length = Math.Min(currentSample.Length, instr.Length * 2);
                        Array.Copy(sonicArrangerFile.Waves[instr.SampleWaveNo].Data, offset, currentSample.CopyTarget, offset, length);
                        for (int i = 0; i < offset; ++i)
                        {
                            sbyte input = unchecked((sbyte)sonicArrangerFile.Waves[instr.SampleWaveNo].Data[i]);
                            if (input == -128)
                                currentSample.Sample = 127;
                            else
                                currentSample.Sample = (sbyte)-input;
                        }
                        if (++playState.CurrentEffectRuns < waveLen + waveRep)
                            return;
                        playState.CurrentEffectRuns = waveLen;
                        if (waveRep != 0)
                            return;
                        if (offset != 0)
                        {
                            --playState.CurrentEffectRuns;
                            return;
                        }
                        this.playState.EffectFinished = true;
                        break;
                    }
                    case Instrument.Effect.RotateVertical:
                    {
                        sbyte deltaVal = (sbyte)instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            currentSample[i] = unchecked((sbyte)(currentSample[i] + deltaVal));
                        }
                        break;
                    }
                    case Instrument.Effect.RotateHorizontal:
                    {
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        sbyte first = currentSample[startPos];
                        for (int i = startPos; i < stopPos; ++i)
                        {
                            currentSample[i] = currentSample[i + 1];
                        }
                        currentSample[stopPos] = first;
                        break;
                    }
                    case Instrument.Effect.AlienVoice:
                    {
                        // This just adds two waves together
                        int effectWave = instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        byte[] data = effectWave >= sonicArrangerFile.Waves.Length ? null : sonicArrangerFile.Waves[effectWave].Data;
                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            currentSample[i] = unchecked((sbyte)(currentSample[i] + (data == null ? 0 : data[i])));
                        }
                        break;
                    }
                    case Instrument.Effect.PolyNegator:
                    {
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        int index = this.playState.CurrentEffectIndex;
                        currentSample.Sample = unchecked((sbyte)sonicArrangerFile.Waves[instr.SampleWaveNo].Data[index]);
                        if (stopPos <= index)
                            index = startPos - 1;
                        ++index;
                        currentSample[index] = unchecked((sbyte)-currentSample[index]);
                        break;
                    }
                    case Instrument.Effect.ShackWave1:
                        ProcessShackWave();
                        break;
                    case Instrument.Effect.ShackWave2:
                    {
                        ProcessShackWave();
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        int index = startPos + playState.CurrentEffectRuns;
                        if (currentSample[index] == -128)
                            currentSample[index] = 127;
                        else
                            currentSample[index] = (sbyte)-currentSample[index];
                        if (++playState.CurrentEffectRuns == stopPos - startPos)
                            playState.CurrentEffectRuns = 0;
                        break;
                    }
                    case Instrument.Effect.Metawdrpk:
                        // TODO
                        break;
                    case Instrument.Effect.LaserAwf:
                        // TODO
                        break;
                    case Instrument.Effect.WaveAlias:
                    {
                        int deltaVal = instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;

                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            var next = i == stopPos ? currentSample[startPos] : currentSample[i + 1];

                            if (currentSample[i] <= next)
                                currentSample[i] = unchecked((sbyte)(currentSample[i] + deltaVal));
                            else
                                currentSample[i] = unchecked((sbyte)(currentSample[i] - deltaVal));
                        }
                        break;
                    }
                    case Instrument.Effect.NoiseGenerator:
                    {
                        // Note: Original uses the lower byte of VHPOSR
                        // which is the horizontal screen position of the beam
                        // and then uses: currentSample = hBeamPos ^ currentSample
                        var random = new Random(DateTime.Now.Millisecond);
                        currentSample[this.playState.CurrentEffectIndex] = (sbyte)random.Next(-128, 128);
                        break;
                    }
                    case Instrument.Effect.LowPassFilter1:
                    {
                        int deltaVal = instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            var next = i == stopPos ? currentSample[startPos] : currentSample[i + 1];
                            var diff = Math.Abs(currentSample[i] - next);

                            if (deltaVal < diff)
                            {
                                if (next >= currentSample[i])
                                    currentSample[i] = (sbyte)Math.Min(127, currentSample[i] + 2);
                                else
                                    currentSample[i] = (sbyte)Math.Max(-128, currentSample[i] - 2);
                            }
                        }
                        break;
                    }
                    case Instrument.Effect.LowPassFilter2:
                        // TODO
                        break;
                    case Instrument.Effect.Oscillator1:
                        // TODO
                        break;
                    case Instrument.Effect.NoiseGenerator2:
                        // TODO
                        break;
                    case Instrument.Effect.FMDrum:
                    {
                        int level = instr.Effect1;
                        int factor = instr.Effect2;
                        int repeats = instr.Effect3;
                        if (playState.CurrentEffectRuns > repeats)
                        {
                            playState.Finetuning = instr.FineTuning;
                            playState.CurrentEffectRuns = 0;
                        }
                        playState.Finetuning -= level * factor;
                        ++playState.CurrentEffectRuns;
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Unknown instrument effect: 0x{(int)instr.EffectNumber:x2}.");
                }

                // Note: Those effects which use the index have StartPos and StopPos
                // in Effect2 and Effect3. Other effects are not care anyways.
                if (++this.playState.CurrentEffectIndex > instr.Effect3)
                    this.playState.CurrentEffectIndex = instr.Effect2;

                void ProcessShackWave()
                {
                    int effectWave = instr.Effect1;
                    int startPos = instr.Effect2;
                    int stopPos = instr.Effect3;
                    var waveData = sonicArrangerFile.Waves[effectWave].Data;
                    int offset = this.playState.CurrentEffectIndex;

                    for (int i = startPos; i <= stopPos; ++i)
                    {
                        int waveIndex = offset + i;
                        var wave = waveIndex >= currentSample.Length || waveIndex >= waveData.Length ? 0 : waveData[waveIndex];
                        currentSample[i] = unchecked((sbyte)(currentSample[i] + wave));
                    }
                }
            }
        }
    }
}
