namespace Ambermoon.Data.Audio
{
    public interface IAudioOutput
    {
        public void Start();
        public void Stop();
        public void StreamData(byte[] data, double timeToKeep);
        public void Clear();
        public bool Available { get; }
        public bool Enabled { get; set; }
        public bool Streaming { get; }
    }
}
