namespace Recordaudio.Models
{
    public sealed class TimestampedAudioChunk
    {
        public long StartFrameIndex { get; set; }
        public float[] Samples { get; set; } = System.Array.Empty<float>();
    }
}