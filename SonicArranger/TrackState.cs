using System;
using System.Linq;

namespace SonicArranger
{
    internal class TrackState
    {
        class Instrument
        {
            public SonicArranger.Instrument Template { get; set; }
            public int EffectDelayCounter { get; set; }
            public int AmfDelayCounter { get; set; }
            public int AdsrDelayCounter { get; set; }
            public int SustainCounter { get; set; }
            public int AdsrIndex { get; set; }
            public int AmfIndex { get; set; }
            public int NotePeriod { get; set; }
            public bool AdsrFinished { get; set; }
            public bool NoteOff { get; set; }
            public int NoteVolume { get; set; }
            public int FadeOutVolume { get; set; }
            public int VibratoDelayCounter { get; set; }
            public int VibratoIndex { get; set; }
        }

        readonly PaulaState paulaState;
        readonly PaulaState.TrackState state;
        readonly int trackIndex;
        readonly SonicArrangerFile sonicArrangerFile;
        Instrument instrument = null;

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
                case 0x1:
                    // TODO
                    break;
                case 0x2: // Set ADSR data index?
                    instrument.AdsrIndex = param; // TODO: out of range checks, etc
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
                    instrument.NoteVolume = Math.Max(0, Math.Min(64, (int)param));
                    instrument.FadeOutVolume = ((instrument.Template.Volume * instrument.NoteVolume) >> 6) * 4;
                    break;
                case 0xD:
                    // TODO
                    break;
                case 0xE: // Enable LED (and therefore the LPF)
                    // TODO
                    break;
                case 0xF: // Set speed
                    songSpeed = param;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Invalid note command.");
            }
        }

        /// <summary>
        /// This should be called whenever a new note or instrument
        /// is played.
        /// </summary>
        public void Play(int noteId, SonicArranger.Instrument? instrument,
            double currentPlayTime)
        {
            if (noteId == 0 || instrument == null) // No note
            {
                if (this.instrument != null)
                    this.instrument.NoteOff = true;
                else
                {
                    // No note played yet
                    this.instrument = new Instrument
                    {
                        NoteOff = true,
                        NoteVolume = 64,
                        FadeOutVolume = 256
                    };
                }

                return;
            }

            if (noteId > 9 * 12)
                throw new ArgumentOutOfRangeException(nameof(noteId));

            var instr = instrument.Value;

            this.instrument = new Instrument
            {
                Template = instr,
                EffectDelayCounter = instr.EffectDelay,
                AmfDelayCounter = instr.AmfDelay,
                AdsrDelayCounter = instr.AdsrDelay,
                SustainCounter = instr.SustainVal,
                AdsrIndex = 0,
                AmfIndex = 0,
                NotePeriod = Tables.NotePeriodTable[noteId],
                AdsrFinished = false,
                NoteOff = false,
                NoteVolume = this.instrument == null ? 64 : this.instrument.NoteVolume,
                FadeOutVolume = 256,
                VibratoDelayCounter = instr.VibDelay,
                VibratoIndex = 0
            };

            state.Data = instr.SynthMode
                ? sonicArrangerFile.Waves[instr.SampleWaveNo].Data
                : sonicArrangerFile.Samples[instr.SampleWaveNo].Data;
            int length = instr.Length * 2;
            if (!instr.SynthMode && instr.Repeat > 1)
                length += instr.Repeat * 2;
            if (length > state.Data.Length)
                throw new ArgumentOutOfRangeException("Length + repeat is greater than the sample data size.");
            else if (length < state.Data.Length)
                state.Data = state.Data.Take(length).ToArray();
            state.DataIndex = 0;
            state.Period = this.instrument.NotePeriod;
            state.Volume = (this.instrument.NoteVolume * instrument.Value.Volume) >> 6;
            paulaState.StartTrackData(trackIndex, currentPlayTime);
        }

        void TrackFinished(int trackIndex, double currentPlayTime)
        {
            if (this.trackIndex != trackIndex)
                return;

            if (instrument.Template.Repeat == 1)
            {
                // This is a "no loop" marker.
                paulaState.StopTrack(trackIndex);
            }
            else if (instrument.Template.Repeat > 0)
            {
                state.DataIndex = instrument.Template.Length * 2;
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
            if (instrument == null)
                return;

            // Note fade out
            if (instrument.NoteOff || instrument.AdsrFinished)
            {
                if (instrument.FadeOutVolume > 0)
                {
                    instrument.FadeOutVolume -= 4;
                }
                else // TODO: Is this right?
                    paulaState.StopTrack(trackIndex);
                return;
            }

            var period = instrument.NotePeriod;

            // Vibrato effect
            if (instrument.VibratoDelayCounter != -1)
            {
                if (--instrument.VibratoDelayCounter == 0)
                {
                    instrument.VibratoDelayCounter = instrument.Template.VibDelay;

                    if (instrument.Template.VibLevel != 0)
                    {
                        period += unchecked((sbyte)Tables.VibratoTable[instrument.VibratoIndex]) * 4 / instrument.Template.VibLevel;
                    }

                    instrument.VibratoIndex = (instrument.VibratoIndex + instrument.Template.VibSpeed) & 0xff;
                }
            }

            // AMF (pitch amplifier)
            int amfTotalLength = Math.Min(128, instrument.Template.AmfLength + instrument.Template.AmfRepeat);
            if (amfTotalLength != 0 && instrument.Template.AmfWave < sonicArrangerFile.AmfWaves.Length)
            {
                byte amfData = sonicArrangerFile.AmfWaves[instrument.Template.AmfWave].Data[instrument.AmfIndex];
                period -= unchecked((sbyte)amfData);

                if (--instrument.AmfDelayCounter == 0)
                {
                    instrument.AmfDelayCounter = instrument.Template.AmfDelay;
                    ++instrument.AmfIndex;

                    if (instrument.AmfIndex >= amfTotalLength)
                    {
                        if (instrument.Template.AmfRepeat == 0)
                            instrument.AmfIndex = instrument.Template.AmfLength - 1;
                        else
                            instrument.AmfIndex = instrument.Template.AmfLength;
                    }
                }
            }

            // TODO: A4+0x84 decreases period
            // TODO: if DAT_002658f4 != 0, A4+0x84 is increased by A4+0x86
            // I guess it is some pitch up effect/slider

            // Instrument effects
            if (instrument.Template.SynthMode)
            {
                // TODO: apply effects (only for synthethic instruments)
                ApplyInstrumentEffects(instrument, ref period);
            }

            int volume = instrument.NoteVolume;

            // ADSR envelop
            int adsrTotalLength = Math.Min(128, instrument.Template.AdsrLength + instrument.Template.AdsrRepeat);
            if (adsrTotalLength != 0 && instrument.Template.AdsrWave < sonicArrangerFile.AdsrWaves.Length)
            {
                byte adsrData = sonicArrangerFile.AdsrWaves[instrument.Template.AdsrWave].Data[instrument.AdsrIndex];
                int adsrVolume = (adsrData * instrument.Template.Volume) >> 6;
                volume = Math.Max(0, Math.Min(64, (volume * adsrVolume) >> 6));
                instrument.FadeOutVolume = volume * 4;

                if (instrument.AdsrIndex >= instrument.Template.SustainPt)
                {
                    // Sustain mode
                    if (instrument.Template.SustainVal != 0) // If 0, keep adsr index forever
                    {
                        if (--instrument.SustainCounter == 0)
                        {
                            instrument.SustainCounter = instrument.Template.SustainVal;
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
                    if (--instrument.AdsrDelayCounter == 0)
                    {
                        instrument.AdsrDelayCounter = instrument.Template.AdsrDelay;
                        ++instrument.AdsrIndex;

                        if (instrument.AdsrIndex >= adsrTotalLength)
                        {
                            if (instrument.Template.AdsrRepeat == 0)
                                instrument.AdsrIndex = instrument.Template.AdsrLength - 1;
                            else
                                instrument.AdsrIndex = instrument.Template.AdsrLength;

                            if (instrument.Template.AdsrRepeat == 0 && adsrData == 0)
                                instrument.AdsrFinished = true;
                        }
                    }
                }
            }
            else
            {
                // Normal volume without envelop
                volume = Math.Max(0, Math.Min(64, (volume * instrument.Template.Volume) >> 6));

                if (instrument.FadeOutVolume > 0)
                {
                    instrument.FadeOutVolume -= 4;
                }
                else // TODO: Is this right?
                    paulaState.StopTrack(trackIndex);
            }

            // Safety checks
            if (period < 124)
                period = 124;

            // Update Paula track state
            state.Period = period;
            state.Volume = volume;
        }

        void ApplyInstrumentEffects(Instrument instrument, ref int period)
        {
            var instr = instrument.Template;
            var currentSample = paulaState.CurrentSamples[trackIndex];

            if (--instrument.EffectDelayCounter == 0)
            {
                instrument.EffectDelayCounter = Math.Max(1, (int)instr.EffectDelay);

                switch (instr.EffectNumber)
                {
                    case SonicArranger.Instrument.Effect.NoEffect:
                        break;
                    case SonicArranger.Instrument.Effect.WaveNegator:
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
                    case SonicArranger.Instrument.Effect.FreeNegator:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.RotateVertical:
                    {
                        sbyte deltaVal = (sbyte)instr.Effect1;
                        int startPos = instr.Effect2;
                        int stopPos = instr.Effect3;
                        for (int i = startPos; i <= stopPos; ++i)
                        {
                            currentSample.Sample = unchecked((sbyte)(currentSample.Sample + deltaVal));
                        }
                        break;
                    }
                    case SonicArranger.Instrument.Effect.RotateHorizontal:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.AlienVoice:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.PolyNegator:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.ShackWave1:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.ShackWave2:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.Metawdrpk:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.LaserAwf:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.WaveAlias:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.NoiseGenerator:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.LowPassFilter1:
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
                                    currentSample[i] += 2;
                                else
                                    currentSample[i] -= 2;
                            }
                        }
                        break;
                    }
                    case SonicArranger.Instrument.Effect.LowPassFilter2:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.Oscillator1:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.NoiseGenerator2:
                        // TODO
                        break;
                    case SonicArranger.Instrument.Effect.FMDrum:
                        // TODO
                        break;
                    default:
                        throw new NotSupportedException($"Unknown instrument effect: 0x{(int)instr.EffectNumber:x2}.");
                }
            }
        }
    }
}
