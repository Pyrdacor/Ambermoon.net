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
            /// <summary>
            /// A4+0xa4
            /// </summary>
            public int AmfIndex { get; set; }
            /// <summary>
            /// This is used if the ADSR repeat portion is
            /// 0 and the full ADSR wave was processed.
            /// 
            /// It is also used for sampled instruments
            /// if the specified sample is not present.
            /// 
            /// A4+0xb4 bit 0
            /// </summary>
            public bool InstrumentFinished { get; set; }
            /// <summary>
            /// This is used for effects that can theoretically be
            /// applied repeated but has a repeat portion of 0.
            /// 
            /// A4+0xb4 bit 2
            /// </summary>
            public bool EffectFinished { get; set; }
            public bool NoteOff { get; set; }
            public int NoteVolume { get; set; }
            /// <summary>
            /// A4+0xb2
            /// </summary>
            public int FadeOutVolume { get; set; }
            public int VibratoDelayCounter { get; set; }
            public int VibratoIndex { get; set; }
            public int VibratoSpeed { get; set; }
            public int VibratoLevel { get; set; }
            /// <summary>
            /// A4+0x84
            /// </summary>
            public int Finetuning { get; set; }
            /// <summary>
            /// A4+0x86
            /// </summary>
            public int PeriodReductionPerTick { get; set; }
            /// <summary>
            /// A4+0x90
            /// </summary>
            public int VolumeChangePerTick { get; set; }
            public int CurrentEffectRuns { get; set; }
            public int CurrentEffectIndex { get; set; }
            public int LastNoteIndex { get; set; }
            public int CurrentNoteIndex { get; set; }
            public bool FirstNoteTick { get; set; }
            public Note? CurrentNote { get; set; }
            /// <summary>
            /// A4+0xae
            /// </summary>
            public int CurrentArpeggioIndex { get; set; }
            /// <summary>
            /// A4+0xb0
            /// </summary>
            public int CurrentArpeggioCommandIteration { get; set; }
            /// <summary>
            /// A4+0x9a
            /// </summary>
            public int CurrentNotePortamentoPeriod { get; set; }
            /// <summary>
            /// A4+0x9c
            /// </summary>
            public int LastNotePortamentoPeriod { get; set; }
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

            paulaState.AttachTrackFinishHandler(index, TrackFinished);
        }

        public void ProcessNoteCommand(Note.NoteCommand command, byte param, ref int songSpeed,
            int currentPatternIndex, out int? noteChangeIndex, out int? patternChangeIndex)
        {
            noteChangeIndex = null;
            patternChangeIndex = null;

            switch (command)
            {
                case Note.NoteCommand.None:
                case Note.NoteCommand.Unused:
                    break;
                case Note.NoteCommand.SlideUp: // smoothly reduce note period, increases pitch
                    playState.PeriodReductionPerTick = param;
                    break;
                case Note.NoteCommand.SetADSRIndex:
                {
                    int maxIndex = 0;
                    if (playState.Instrument != null)
                    {
                        var instr = playState.Instrument.Value;
                        maxIndex = Math.Max(0, instr.AdsrLength + instr.AdsrRepeat - 1);
                    }
                    playState.AdsrIndex = Math.Min(param, maxIndex);
                    break;
                }
                case Note.NoteCommand.ResetVibrato:
                    playState.VibratoDelayCounter = 0;
                    break;
                case Note.NoteCommand.SetVibrato:
                    playState.VibratoSpeed = 2 * (param >> 4);
                    playState.VibratoLevel = unchecked((sbyte)(160 - (param & 0xf) * 16));
                    break;
                case Note.NoteCommand.SetMasterVolume: // in contrast to SetVolume this will affect all channels
                    paulaState.MasterVolume = Math.Min(64, (int)param);
                    break;
                case Note.NoteCommand.SetPortamento:
                    playState.CurrentNotePortamentoPeriod = param;
                    break;
                case Note.NoteCommand.ClearPortamento:
                    playState.CurrentNotePortamentoPeriod = 0;
                    break;
                case Note.NoteCommand.Unknown9:
                    // TODO
                    break;
                case Note.NoteCommand.VolumeSlide:
                    if ((param & 0xf0) != 0)
                        playState.VolumeChangePerTick = param >> 4;
                    else
                        playState.VolumeChangePerTick = -(param & 0xf);
                    break;
                case Note.NoteCommand.PositionJump: // stop after the current note and then continue with given pattern
                    // Note: In contrast to ProTracker the division is reset to 0.
                    noteChangeIndex = 0;
                    patternChangeIndex = Math.Max(0, (param - 1) & 0x7f);
                    break;
                case Note.NoteCommand.SetVolume:
                    playState.NoteVolume = Math.Min(64, (int)param);
                    playState.FadeOutVolume = ((paulaState.MasterVolume * playState.NoteVolume) >> 6) * 4;
                    break;
                case Note.NoteCommand.PatternBreak: // continue with next pattern after current note
                    noteChangeIndex = 0;
                    patternChangeIndex = currentPatternIndex + 1;
                    break;
                case Note.NoteCommand.DisableHardwareLPF: // Disable LED (and therefore the LPF)
                    paulaState.UseLowPassFilter = param == 0;
                    break;
                case Note.NoteCommand.SetSpeed:
                    songSpeed = Math.Min((int)param, 16);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid note command.");
            }
        }

        void InitState(int instrumentIndex)
        {
            if (instrumentIndex <= 0)
                throw new ArgumentOutOfRangeException("Expected instrument index to be > 0.");

            var instrument = sonicArrangerFile.Instruments[instrumentIndex - 1];

            ResetEffectState();

            playState.Instrument = instrument;
            playState.NoteOff = false;
            playState.NoteVolume = instrument.Volume;
            playState.VibratoDelayCounter = instrument.VibDelay;
            playState.VibratoIndex = 0;
            playState.VibratoLevel = instrument.VibLevel;
            playState.VibratoSpeed = instrument.VibSpeed;
            playState.PeriodReductionPerTick = 0;
            playState.VolumeChangePerTick = 0;
            playState.CurrentArpeggioIndex = 0;
            playState.CurrentNotePortamentoPeriod = instrument.Portamento;
            playState.LastNotePortamentoPeriod = 0;
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
            playState.InstrumentFinished = false;
            playState.EffectFinished = false;
            playState.FadeOutVolume = ((playState.NoteVolume * paulaState.MasterVolume) >> 6) * 4;
        }

        void Mute()
        {
            playState.NoteOff = true;
            playState.InstrumentFinished = true;
            playState.EffectFinished = true;
            playState.NoteVolume = 0;
            playState.FadeOutVolume = 0;
            paulaState.StopTrack(trackIndex);
        }

        /// <summary>
        /// This should be called whenever a new note or instrument is played.
        /// </summary>
        public void Play(Note note, int noteTranspose, int soundTranspose, double currentPlayTime)
        {
            int noteId = note.Value;
            int noteInstrument = note.Instrument;
            var noteFlags = note.Flags;
            playState.CurrentNote = note;
            playState.FirstNoteTick = true;

            if (noteId == 0)
            {
                if (noteInstrument != 0)
                {
                    InitState(noteInstrument);
                }
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
                        if (!noteFlags.HasFlag(Note.NoteFlags.DisableNoteTranspose))
                            noteId += noteTranspose;
                        if (noteId > 9 * 12)
                            throw new ArgumentOutOfRangeException(nameof(noteId));
                        if (noteInstrument != 0 && !noteFlags.HasFlag(Note.NoteFlags.DisableSoundTranspose))
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
                            InitState(noteInstrument);
                        }
                        var instrument = playState.Instrument.Value;
                        if (instrument.SynthMode)
                        {
                            if (instrument.SampleWaveNo >= sonicArrangerFile.Waves.Length ||
                                sonicArrangerFile.Waves[instrument.SampleWaveNo].Data == null ||
                                sonicArrangerFile.Waves[instrument.SampleWaveNo].Data.Length == 0)
                            {
                                playState.InstrumentFinished = true;
                                Mute();
                                return;
                            }
                            state.Data = sonicArrangerFile.Waves[instrument.SampleWaveNo].Data;
                        }
                        else
                        {
                            if (instrument.SampleWaveNo >= sonicArrangerFile.Samples.Length ||
                                sonicArrangerFile.Samples[instrument.SampleWaveNo].Data == null ||
                                sonicArrangerFile.Samples[instrument.SampleWaveNo].Data.Length == 0)
                            {
                                playState.InstrumentFinished = true;
                                Mute();
                                return;
                            }
                            state.Data = sonicArrangerFile.Samples[instrument.SampleWaveNo].Data;
                        }
                        int length = instrument.Length * 2;
                        if (!instrument.SynthMode && instrument.Repeat > 1)
                            length += instrument.Repeat * 2;
                        if (length > state.Data.Length)
                            throw new ArgumentOutOfRangeException("Length + repeat is greater than the sample/wave data size.");
                        else if (length < state.Data.Length)
                            state.Data = state.Data.Take(length).ToArray();
                        state.DataIndex = 0;
                        state.Period = Tables.NotePeriodTable[playState.CurrentNoteIndex];
                        state.Volume = (playState.NoteVolume * paulaState.MasterVolume) >> 6;
                        paulaState.StartTrackData(trackIndex, currentPlayTime);
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
            if (playState.CurrentNote == null || playState.NoteOff || playState.InstrumentFinished || playState.Instrument == null)
            {
                if (playState.FadeOutVolume > 0)
                {
                    playState.FadeOutVolume -= 4;
                }
                return;
            }

            var note = playState.CurrentNote.Value;
            var instrument = playState.Instrument.Value;
            int noteId = playState.CurrentNoteIndex;
            int lastNoteId = playState.LastNoteIndex;

            if (note.ArpeggioIndex == 0)
            {
                if (note.Command == 0 && // This is also used to influence arpeggio play if the param is != 0
                    note.CommandInfo != 0)
                {
                    // In this case a sequence of "normal note", "note + x semitones", "note + y semitones"
                    // is played. This is also documented for ProTracker.
                    if (playState.CurrentArpeggioCommandIteration == 0)
                    {
                        // Normal note. Just increase the iteration.
                        ++playState.CurrentArpeggioCommandIteration;
                    }
                    else
                    {
                        // Here the notes are adjusted.
                        if (playState.CurrentArpeggioCommandIteration == 1)
                        {
                            noteId += (note.CommandInfo >> 4);
                            ++playState.CurrentArpeggioCommandIteration;
                        }
                        else // 2
                        {
                            noteId += note.CommandInfo & 0xf;
                            playState.CurrentArpeggioCommandIteration = 0;
                        }
                    }
                }
            }
            else
            {
                // Arpeggio
                var arpeggio = instrument.ArpegData[note.ArpeggioIndex];
                int arpeggioTotalLength = Math.Min(14, arpeggio.Length + arpeggio.Repeat);
                int index = playState.CurrentArpeggioIndex;
                int noteOffset = index >= arpeggio.Data.Length ? 0 : arpeggio.Data[index];
                noteId += noteOffset;
                lastNoteId += noteOffset;
                if (++index >= arpeggioTotalLength)
                    index = arpeggio.Length;
                playState.CurrentArpeggioIndex = index;
            }

            int period = Tables.NotePeriodTable[noteId];

            // Portamento
            if (playState.CurrentNotePortamentoPeriod != 0)
            {
                if (playState.LastNotePortamentoPeriod == 0)
                    playState.LastNotePortamentoPeriod = Tables.NotePeriodTable[lastNoteId];
                int diff = Math.Abs(period - playState.LastNotePortamentoPeriod);
                if (playState.CurrentNotePortamentoPeriod > diff)
                    playState.CurrentNotePortamentoPeriod = 0;
                else
                {
                    int add = playState.LastNotePortamentoPeriod < period
                        ? playState.CurrentNotePortamentoPeriod
                        : -playState.CurrentNotePortamentoPeriod;
                    playState.LastNotePortamentoPeriod += add;
                    period = playState.LastNotePortamentoPeriod;
                }
            }

            // Vibrato effect
            if (playState.VibratoDelayCounter != -1)
            {
                if (--playState.VibratoDelayCounter == 0)
                {
                    playState.VibratoDelayCounter = instrument.VibDelay;

                    if (playState.VibratoLevel != 0)
                    {
                        period += unchecked((sbyte)Tables.VibratoTable[playState.VibratoIndex]) * 4 / playState.VibratoLevel;
                    }

                    playState.VibratoIndex = (playState.VibratoIndex + playState.VibratoSpeed) & 0xff;
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
                playState.Finetuning -= playState.PeriodReductionPerTick;

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
                                playState.InstrumentFinished = true;
                        }
                    }
                }
            }
            else
            {
                // Normal volume without envelop
                volume = Math.Max(0, Math.Min(64, (volume * paulaState.MasterVolume) >> 6));

                if (playState.FadeOutVolume > 0)
                {
                    playState.FadeOutVolume -= 4;
                }
            }

            playState.NoteVolume -= playState.VolumeChangePerTick;

            // Safety checks
            if (period < 124)
                period = 124;
            if (playState.NoteVolume < 0)
                playState.NoteVolume = 0;
            if (playState.NoteVolume > 64)
                playState.NoteVolume = 64;

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
                        if (this.playState.InstrumentFinished || this.playState.EffectFinished)
                            return;
                        int effectWave = instr.Effect1;
                        int waveLen = instr.Effect2;
                        int waveRep = instr.Effect3;
                        int offset = sonicArrangerFile.Waves[effectWave].Data[this.playState.CurrentEffectRuns] & 0x7f;
                        int length = Math.Min(currentSample.Length, instr.Length * 2);
                        Array.Copy(sonicArrangerFile.Waves[instr.SampleWaveNo].Data, offset, currentSample.CopyTarget, offset, length - offset);
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
                    case Instrument.Effect.LaserAmf:
                    {
                        int detune = instr.Effect2;
                        int repeats = instr.Effect3;
                        if (playState.CurrentEffectRuns < repeats)
                        {
                            playState.Finetuning += detune;
                            ++playState.CurrentEffectRuns;
                        }
                        break;
                    }
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
                    {
                        int effectWave = instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        byte[] data = effectWave >= sonicArrangerFile.Waves.Length ? null : sonicArrangerFile.Waves[effectWave].Data;
                        if (data != null)
                        {
                            for (int i = startPos; i <= stopPos; ++i)
                            {
                                var next = i == stopPos ? currentSample[startPos] : currentSample[i + 1];
                                var diff = Math.Abs(currentSample[i] - next);

                                if (data[i] < diff)
                                {
                                    if (next >= currentSample[i])
                                        currentSample[i] = (sbyte)Math.Min(127, currentSample[i] + 2);
                                    else
                                        currentSample[i] = (sbyte)Math.Max(-128, currentSample[i] - 2);
                                }
                            }
                        }
                        break;
                    }
                    case Instrument.Effect.Oscillator1:
                        // TODO: this is quiet a monster to implement :D
                        break;
                    case Instrument.Effect.NoiseGenerator2:
                    {
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        var random = new Random(DateTime.Now.Millisecond);
                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            var value = currentSample[i];
                            currentSample[i] = unchecked((sbyte)((value ^ 5) << 2 | (value >> 6) + random.Next(-128, 128)));
                        }
                        break;
                    }
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
