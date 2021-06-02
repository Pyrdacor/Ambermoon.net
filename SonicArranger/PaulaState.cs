using System;

namespace SonicArranger
{
    /// <summary>
    /// This mimicks the audio channel data of the Amiga
    /// which can be found at 0xdff0a0, 0xdff0b0, 0xdff0c0
    /// and 0xdff0d0 for the 4 audio channels.
    /// 
    /// It is documented at http://amiga-dev.wikidot.com/information:hardware.
    /// </summary>
    internal class PaulaState
    {
        public const int NumTracks = 4;

        public class TrackState
        {
            byte[] data = null;
            int dataIndex = 0;

            public bool DataChanged { get; private set; } = false;

            /// <summary>
            /// Source data for playback.
            /// 
            /// This mimicks the audio channel location bits at
            /// AUDxLCH and AUDxLCL.
            /// </summary>
            public byte[] Data
            {
                get => data;
                set
                {
                    if (data != value)
                    {
                        data = value;
                        DataChanged = true;
                    }
                }
            }
            /// <summary>
            /// Length of the data to use.
            /// 
            /// This mimicks the audio channel length at AUDxLEN.
            /// </summary>
            public int Length { get; set; }
            /// <summary>
            /// Current period value (note frequency).
            /// 
            /// This mimicks the audio channel period at AUDxPER
            /// </summary>
            public int Period { get; set; }
            /// <summary>
            /// Current output volume.
            /// 
            /// This mimicks the audio channel volume at AUDxVOL
            /// </summary>
            public int Volume { get; set; }
            /// <summary>
            /// The current data index into the source data.
            /// 
            /// This together with <see cref="Data"/> is used
            /// for playback by replacing the DMA audio controller
            /// which fills AUDxDAT automatically.
            /// </summary>
            public int DataIndex
            {
                get => dataIndex;
                set
                {
                    if (dataIndex != value)
                    {
                        dataIndex = value;
                        DataChanged = true;
                    }
                }
            }
        }

        class CurrentTrackState
        {
            public byte[] Data { get; set; }
            public double StartPlayTime { get; set; }
        }

        public interface ICurrentSample
        {
            sbyte Sample { get; set; }
            int Index { get; }
            int Length { get; }
            sbyte this[int index] { get; set; }
            byte[] CopyTarget { get; }
        }

        class CurrentSample : ICurrentSample
        {
            readonly CurrentTrackState currentTrackState;

            public CurrentSample(CurrentTrackState currentTrackState)
            {
                this.currentTrackState = currentTrackState;
            }

            public int Index { get; set; } = 0;
            public int NextIndex { get; set; } = 1;
            public double Gamma { get; set; } = 0.0;
            public int Length => currentTrackState.Data?.Length ?? 0;
            public byte[] CopyTarget => currentTrackState.Data;

            public sbyte Sample
            {
                get => this[Index];
                set => this[Index] = value;
            }

            public sbyte this[int index]
            {
                get => currentTrackState.Data == null ? (sbyte)0 : unchecked((sbyte)currentTrackState.Data[index]);
                set
                {
                    if (currentTrackState.Data != null)
                        currentTrackState.Data[index] = unchecked((byte)value);
                }
            }
        }

        public delegate void TrackFinishedHandler(int trackIndex, double currentPlayTime);
        public event TrackFinishedHandler TrackFinished;
        public readonly TrackState[] Tracks = new TrackState[NumTracks];
        readonly CurrentTrackState[] currentTrackStates = new CurrentTrackState[NumTracks];
        readonly CurrentSample[] currentSamples = new CurrentSample[4];
        public ICurrentSample[] CurrentSamples => currentSamples;
        const double palClockFrequency = 7093789.2;
        const double ntscClockFrequency = 7159090.5;
        double clockFrequency = palClockFrequency;
        int masterVolume = 64;
        public int MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Math.Max(0, Math.Min(value, 64));
        }

        public PaulaState()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                Tracks[i] = new TrackState();
                currentTrackStates[i] = new CurrentTrackState();
                currentSamples[i] = new CurrentSample(currentTrackStates[i]);
            }
        }

        public void Reset(bool pal = true)
        {
            clockFrequency = pal ? palClockFrequency : ntscClockFrequency;

            for (int i = 0; i < NumTracks; ++i)
            {
                var track = Tracks[i];
                track.Data = null;
                track.Length = 0;
                track.Period = 0;
                track.Volume = 0;
                track.DataIndex = 0;

                var trackState = currentTrackStates[i];
                trackState.Data = null;
                trackState.StartPlayTime = 0.0;

                currentSamples[i].Index = 0;
            }
        }

        public void StopTrack(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex > NumTracks)
                throw new IndexOutOfRangeException("Invalid track index.");

            var track = Tracks[trackIndex];
            var trackState = currentTrackStates[trackIndex];

            track.Data = null;
            trackState.Data = null;
            currentSamples[trackIndex].Index = 0;
        }

        public void StartTrackData(int trackIndex, double currentPlayTime)
        {
            if (trackIndex < 0 || trackIndex > NumTracks)
                throw new IndexOutOfRangeException("Invalid track index.");

            var track = Tracks[trackIndex];

            if (track.DataChanged)
            {
                int size = track.Data.Length - track.DataIndex;

                if (size <= 0)
                    throw new ArgumentOutOfRangeException("Track data index must be less than the data size.");

                var trackState = currentTrackStates[trackIndex];
                trackState.Data = new byte[size];
                Buffer.BlockCopy(track.Data, track.DataIndex, trackState.Data, 0, size);
                trackState.StartPlayTime = currentPlayTime;
            }

            currentSamples[trackIndex].Index = 0;
        }

        public double ProcessTrack(int trackIndex, double currentPlaybackTime)
        {
            if (trackIndex < 0 || trackIndex > NumTracks)
                throw new IndexOutOfRangeException("Invalid track index.");

            var data = currentTrackStates[trackIndex].Data;

            if (data == null || Tracks[trackIndex].Period < 0.01)
                return 0.0;

            var currentSample = currentSamples[trackIndex];
            double leftValue = unchecked((sbyte)data[currentSample.Index]) / 128.0;
            double rightValue = unchecked((sbyte)data[currentSample.NextIndex]) / 128.0;

            return Tracks[trackIndex].Volume * (leftValue + currentSample.Gamma * (rightValue - leftValue)) / 64.0;
        }

        public void UpdateCurrentSample(int trackIndex, double currentPlaybackTime)
        {
            if (trackIndex < 0 || trackIndex > NumTracks)
                throw new IndexOutOfRangeException("Invalid track index.");

            var trackState = currentTrackStates[trackIndex];
            var currentSample = currentSamples[trackIndex];

            if (trackState.Data == null || trackState.StartPlayTime > currentPlaybackTime)
            {
                currentSample.Index = 0;
                currentSample.NextIndex = 1;
                currentSample.Gamma = 0.0;
                return;
            }

            var period = Tracks[trackIndex].Period;

            if (period < 0.01)
            {
                currentSample.Index = 0;
                currentSample.NextIndex = 1;
                currentSample.Gamma = 0.0;
                return;
            }

            double samplesPerSecond = clockFrequency / (2.0 * period);
            double trackTime = currentPlaybackTime - trackState.StartPlayTime;
            double index = samplesPerSecond * trackTime;

            var data = trackState.Data;
            int leftIndex = (int)index;

            if (leftIndex >= data.Length)
            {
                TrackFinished?.Invoke(trackIndex, currentPlaybackTime);

                if (trackState.Data == null)
                {
                    currentSample.Index = 0;
                    currentSample.NextIndex = 1;
                    currentSample.Gamma = 0.0;
                    return;
                }

                index -= data.Length;
                data = trackState.Data;
                leftIndex = 0;
                trackState.StartPlayTime = currentPlaybackTime;
            }

            currentSample.Index = leftIndex;
            currentSample.NextIndex = leftIndex == data.Length - 1 ? 0 : leftIndex + 1;
            currentSample.Gamma = index - leftIndex;
        }

        public double Process(double currentPlaybackTime)
        {
            double output = 0.0;

            for (int i = 0; i < NumTracks; ++i)
                output += ProcessTrack(i, currentPlaybackTime);

            return Math.Max(-1.0, Math.Min(1.0, output));
        }

        public double ProcessLeftOutput(double currentPlaybackTime)
        {
            double output = 0.0;

            // LRRL
            output += ProcessTrack(0, currentPlaybackTime);
            output += ProcessTrack(3, currentPlaybackTime);

            return Math.Max(-1.0, Math.Min(1.0, output));
        }

        public double ProcessRightOutput(double currentPlaybackTime)
        {
            double output = 0.0;

            // LRRL
            output += ProcessTrack(1, currentPlaybackTime);
            output += ProcessTrack(2, currentPlaybackTime);

            return Math.Max(-1.0, Math.Min(1.0, output));
        }
    }
}
