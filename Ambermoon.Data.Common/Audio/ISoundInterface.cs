namespace Ambermoon.Data.Audio
{
    public interface ISoundInterface
    {
        /// <summary>
        /// Output volume (0.0 to 1.0)
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Sound output enable state
        /// </summary>
        public bool Enabled { get; set; }
    }
}
