namespace Recordaudio.Models
{
    public sealed class AudioChunkItem
    {
        public int Index { get; set; }
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }

        public string SystemFilePath { get; set; } = "";
        public string MicFilePath { get; set; } = "";

        // Compat legacy
        public string FilePath { get; set; } = "";

        public long EstimatedSystemSizeBytes { get; set; }
        public long EstimatedMicSizeBytes { get; set; }

        public override string ToString()
        {
            return $"Chunk {Index:00} | {StartSeconds:0.0}s -> {EndSeconds:0.0}s";
        }
    }
}