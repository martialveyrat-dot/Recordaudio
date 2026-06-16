namespace Recordaudio.Models
{
    public sealed class TranscriptionResult
    {
        public bool Success { get; set; }
        public string TranscriptText { get; set; } = "";
        public string TranscriptFilePath { get; set; } = "";
        public string SummaryFilePath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}