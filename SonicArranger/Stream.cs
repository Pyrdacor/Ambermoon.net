using System;

namespace SonicArranger
{
    public class Stream
    {
        readonly SonicArrangerFile sonicArrangerFile;
        readonly Song song;
        readonly uint sampleRate;
        readonly bool stereo;
        readonly byte[] buffer;
        int bufferSampleIndex = 0;
        readonly PaulaState paulaState = new PaulaState();
        readonly TrackState[] tracks = new TrackState[PaulaState.NumTracks];
        double playTime = 0.0; // in seconds
        double nextInterruptTime = 0.0;
        readonly double interruptDelay = 0.020; // 20 ms by default
        double nextNoteTime = 0.0;
        double noteDuration = 0.120;
        int songSpeed = 6;
        int patternIndex = 0;
        int noteIndex = 0;
        long? endOfStreamIndex = null;
        long processedAmount = 0;
        public int LoopCounter { get; private set; } = 0;
        readonly bool allowLowPassFilter = true;
        readonly bool pal = true;
        readonly object readMutex = new object();
        readonly object copyMutex = new object();
        bool initialized = false;

        public bool EndOfStream => endOfStreamIndex == processedAmount;

        /// <summary>
        /// Create a readable sound data stream from a sonic arranger song.
        /// </summary>
        /// <param name="sonicArrangerFile">Loaded sonic arranger file</param>
        /// <param name="song">Song index (0-based)</param>
        /// <param name="sampleRate">Output sample rate in Hz</param>
        /// <param name="stereo">If active the LRRL channel pattern is used</param>
        /// <param name="allowAmigaLowPassFilter">Allows the Amiga hardware LPF emulation</param>
        /// <param name="pal">If active the PAL frequency is used, otherwise the NTSC frequency is used.</param>
        public Stream(SonicArrangerFile sonicArrangerFile, int song, uint sampleRate, bool stereo,
            bool allowAmigaLowPassFilter = true, bool pal = true)
        {
            if (sonicArrangerFile == null)
                throw new ArgumentNullException(nameof(sonicArrangerFile));

            if (song < 0 || song >= sonicArrangerFile.Songs.Length)
                throw new ArgumentOutOfRangeException(nameof(song));

            if (sampleRate < 2000 || sampleRate > 200000)
                throw new NotSupportedException("Only sample rates in the range from 2kHz to 200kHz are supported.");

            this.sonicArrangerFile = sonicArrangerFile;
            this.sampleRate = sampleRate;
            this.stereo = stereo;
            this.song = sonicArrangerFile.Songs[song];
            this.pal = pal;
            allowLowPassFilter = allowAmigaLowPassFilter;

            if (this.song.NBIrqps < 1 || this.song.NBIrqps > 200)
                throw new NotSupportedException("Number of interrupts must be in the range 1 to 200.");

            // We store 2 seconds of data
            buffer = new byte[2 * sampleRate * (stereo ? 2 : 1)];

            interruptDelay = 1.0 / this.song.NBIrqps;

            for (int i = 0; i < PaulaState.NumTracks; ++i)
                tracks[i] = new TrackState(i, paulaState, sonicArrangerFile);

            Reset();
        }

        /// <summary>
        /// Resets the stream. Reading will start from the beginning afterwards.
        /// </summary>
        public void Reset()
        {
            if (initialized)
                return;

            paulaState.Reset(allowLowPassFilter, pal);
            playTime = 0.0;
            nextInterruptTime = 0.0;
            nextNoteTime = 0.0;
            noteDuration = song.GetNoteDuration(song.SongSpeed);
            songSpeed = song.SongSpeed;
            patternIndex = song.StartPos;
            noteIndex = 0;
            endOfStreamIndex = null;
            processedAmount = 0;
            LoopCounter = 0;

            // Load initial data
            Load(0, (int)sampleRate * 2, false);

            initialized = true;
        }

        /// <summary>
        /// Reads the next n milliseconds of sound data.
        /// 
        /// Note: If loop is set to false the returned data might have a lower size
        /// when reaching the end or even can be empty when already at the end.
        /// 
        /// In case the end is reached without the loop option it is not possible
        /// to read further even if loop is then set to true.
        /// 
        /// Best use the same loop option for the whole stream reading.
        /// </summary>
        /// <param name="milliSeconds">Time in milliseconds to read.</param>
        /// <param name="loop">If set the track is looped when reaching the end.</param>
        /// <returns></returns>
        public byte[] Read(int milliSeconds, bool loop)
        {
            lock (readMutex)
            {
                initialized = false;

                if (endOfStreamIndex == processedAmount)
                    throw new System.IO.EndOfStreamException("End of stream reached.");

                if (milliSeconds < 0)
                    throw new ArgumentOutOfRangeException(nameof(milliSeconds));

                if (milliSeconds > 1000)
                    throw new NotSupportedException("Only 1 second of data can be read at once.");

                if (milliSeconds == 0)
                    return new byte[0];

                int numSamples = ((int)sampleRate * milliSeconds + 999) / 1000;
                int sizePerSample = stereo ? 2 : 1;
                int bufferIndex = bufferSampleIndex * sizePerSample;
                int size = numSamples * sizePerSample;
                bool endOfStream = false;
                if (endOfStreamIndex != null && size > endOfStreamIndex.Value - processedAmount)
                {
                    size = (int)(endOfStreamIndex.Value - processedAmount);
                    numSamples = size / sizePerSample;
                    endOfStream = true;
                }
                byte[] data = new byte[size];
                Buffer.BlockCopy(buffer, bufferIndex, data, 0, data.Length);
                bufferSampleIndex += numSamples;
                processedAmount += data.Length;

                if (!endOfStream && bufferSampleIndex > sampleRate)
                {
                    // When we have read more than 1 second of data we will
                    // load more data to the end of the buffer.                
                    bufferIndex += data.Length;
                    int loadedSize = buffer.Length - bufferIndex;
                    if (loadedSize != 0)
                        Buffer.BlockCopy(buffer, bufferIndex, buffer, 0, loadedSize);
                    if (endOfStreamIndex != null)
                    {
                        int remainingSize = (int)(endOfStreamIndex.Value - processedAmount) - loadedSize;
                        Load(loadedSize, Math.Min(remainingSize, (buffer.Length - loadedSize) / sizePerSample), loop);
                    }
                    else
                    {
                        Load(loadedSize, (buffer.Length - loadedSize) / sizePerSample, loop);
                    }
                    bufferSampleIndex = 0;
                }

                return data;
            }
        }

        /// <summary>
        /// Writes the stream data to the given output stream.
        /// </summary>
        /// <param name="stream">Output stream to write to.</param>
        /// <param name="maxLoops">Max number of loops (0 means no looping). Is limited to 100 for safety reasons.</param>
        /// <param name="reset">If set the stream is reset to the beginning first.</param>
        public void WriteTo(System.IO.Stream stream, int maxLoops = 0, bool reset = true)
        {
            if (maxLoops > 100)
                throw new ArgumentOutOfRangeException("Max loop count is limited to 100 to avoid unintended harddrive floating.");

            lock (copyMutex)
            {
                if (reset)
                    Reset();

                while (!EndOfStream)
                {
                    var data = Read(1000, maxLoops > LoopCounter);
                    stream.Write(data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Provides the stream data as a byte array.
        /// </summary>
        /// <param name="maxLoops">Max number of loops (0 means no looping). Is limited to 100 for safety reasons.</param>
        /// <param name="reset">If set the stream is reset to the beginning first.</param>
        /// <returns></returns>
        public byte[] ToArray(int maxLoops = 0, bool reset = true)
        {
            var memoryStream = new System.IO.MemoryStream();
            WriteTo(memoryStream, maxLoops, reset);
            return memoryStream.ToArray();
        }

        void Load(int bufferIndex, int numSamples, bool loop)
        {
            double tick = 1.0 / sampleRate;
            double deltaTime = (double)numSamples / sampleRate - 0.1 * tick; // - 0.1 tick avoids rounding errors in loop condition

            for (double d = 0.0; d < deltaTime; d += tick)
            {
                if (endOfStreamIndex != null && endOfStreamIndex == processedAmount + bufferIndex)
                    return;

                if (nextNoteTime <= playTime)
                {
                    ProcessNotes();                    
                }

                for (int i = 0; i < PaulaState.NumTracks; ++i)
                {
                    paulaState.UpdateCurrentSample(i, playTime);
                }

                if (nextInterruptTime <= playTime)
                {
                    for (int i = 0; i < PaulaState.NumTracks; ++i)
                        tracks[i].Tick();

                    nextInterruptTime += interruptDelay;
                }

                if (stereo)
                {
                    var left = paulaState.ProcessLeftOutput(playTime) * 128.0;
                    var right = paulaState.ProcessRightOutput(playTime) * 128.0;
                    buffer[bufferIndex++] = unchecked((byte)(sbyte)Math.Max(-128, Math.Min(127, Math.Round(left))));
                    buffer[bufferIndex++] = unchecked((byte)(sbyte)Math.Max(-128, Math.Min(127, Math.Round(right))));
                }
                else
                {
                    var data = paulaState.Process(playTime) * 128.0;
                    buffer[bufferIndex++] = unchecked((byte)(sbyte)Math.Max(-128, Math.Min(127, Math.Round(data))));
                }

                playTime += tick;
            }

            void ProcessNotes()
            {
                int? noteChangeIndex = null;
                int? patternChangeIndex = null;

                for (int i = 0; i < PaulaState.NumTracks; ++i)
                {
                    var voice = sonicArrangerFile.Voices[patternIndex * 4 + i];
                    var note = sonicArrangerFile.Notes[voice.NoteAddress + noteIndex];
                    int speed = songSpeed;
                    tracks[i].Play(note, voice.NoteTranspose, voice.SoundTranspose, playTime);
                    tracks[i].ProcessNoteCommand(note.Command, note.CommandInfo, ref speed,
                        patternIndex, out var trackNoteChangeIndex, out var trackPatternChangeIndex);

                    if (trackNoteChangeIndex != null)
                        noteChangeIndex = trackNoteChangeIndex;
                    if (trackPatternChangeIndex != null)
                        patternChangeIndex = trackPatternChangeIndex;

                    if (speed != songSpeed)
                    {
                        songSpeed = speed;
                        noteDuration = song.GetNoteDuration(speed);
                    }
                }

                if (noteChangeIndex != null)
                {
                    noteIndex = noteChangeIndex.Value;

                    if (patternChangeIndex != null)
                        patternIndex = patternChangeIndex.Value;
                }
                else
                {
                    ++noteIndex;
                }
                if (noteIndex >= song.PatternLength)
                {
                    noteIndex = 0;

                    if (patternChangeIndex != null)
                        patternIndex = patternChangeIndex.Value;
                    else
                        ++patternIndex;
                }
                if (patternIndex > song.StopPos)
                {
                    if (loop)
                    {
                        ++LoopCounter;
                        patternIndex = Math.Min(song.RepeatPos, song.StopPos);
                    }
                    else
                    {
                        // one full note till the end which lasts for noteDuration
                        int remainingSamples = (int)(noteDuration * sampleRate);
                        endOfStreamIndex = processedAmount + bufferIndex + remainingSamples * (stereo ? 2 : 1) - 1;
                    }
                }

                nextNoteTime += noteDuration;
            }
        }
    }
}
