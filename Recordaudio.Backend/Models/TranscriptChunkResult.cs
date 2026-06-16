namespace Recordaudio.Backend.Models;

public class TranscriptChunkResult
{
    public string FileName { get; set; } = "";
    public bool Success { get; set; }
    public string? TranscriptText { get; set; }
    public string? ErrorMessage { get; set; }
}