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
    public class PaulaState
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

        public delegate void TrackFinishedHandler(int trackIndex, double currentPlayTime);
        public event TrackFinishedHandler TrackFinished;
        public readonly TrackState[] Tracks = new TrackState[NumTracks];
        readonly CurrentTrackState[] currentTrackStates = new CurrentTrackState[NumTracks];
        const double palClockFrequency = 7093789.2;
        const double ntscClockFrequency = 7159090.5;
        double clockFrequency = palClockFrequency;


        public PaulaState()
        {
            for (int i = 0; i < NumTracks; ++i)
            {
                Tracks[i] = new TrackState();
                currentTrackStates[i] = new CurrentTrackState();
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
        }

        public double ProcessTrack(int trackIndex, double currentPlaybackTime)
        {
            if (trackIndex < 0 || trackIndex > NumTracks)
                throw new IndexOutOfRangeException("Invalid track index.");

            var trackState = currentTrackStates[trackIndex];

            if (trackState.Data == null || trackState.StartPlayTime > currentPlaybackTime)
                return 0.0;

            var period = Tracks[trackIndex].Period;

            if (period < 0.01)
                return 0.0;            

            double samplesPerSecond = clockFrequency / (2.0 * period);
            double trackTime = currentPlaybackTime - trackState.StartPlayTime;
            double index = samplesPerSecond * trackTime;

            var data = trackState.Data;
            int leftIndex = (int)index;

            if (leftIndex >= data.Length)
            {
                TrackFinished?.Invoke(trackIndex, currentPlaybackTime);

                if (trackState.Data == null)
                    return 0.0;

                index -= data.Length;
                data = trackState.Data;                
                leftIndex = 0;
                trackState.StartPlayTime = currentPlaybackTime;
            }

            int rightIndex = leftIndex == data.Length - 1 ? 0 : leftIndex + 1;
            double gamma = index - leftIndex;
            double leftValue = unchecked((sbyte)data[leftIndex]) / 128.0;
            double rightValue = unchecked((sbyte)data[rightIndex]) / 128.0;

            return Tracks[trackIndex].Volume * (leftValue + gamma * (rightValue - leftValue)) / 64.0;
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
