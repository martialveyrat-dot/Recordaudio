using System.Collections.Generic;

namespace Recordaudio.Models
{
    public sealed class BackendTranscriptionResponse
    {
        public bool Success { get; set; }
        public string Transcript { get; set; } = "";
        public string Error { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string WarningMessage { get; set; } = "";
        public List<BackendTranscriptChunkResult> Chunks { get; set; } = new();
    }

    public sealed class BackendTranscriptChunkResult
    {
        public string FileName { get; set; } = "";
        public bool Success { get; set; }
        public string? TranscriptText { get; set; }
        public string? ErrorMessage { get; set; }
    }
}