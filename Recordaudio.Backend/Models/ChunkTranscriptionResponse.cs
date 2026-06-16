namespace Recordaudio.Backend.Models;

public class ChunkTranscriptionResponse
{
    public bool Success { get; set; }
    public string Transcript { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public string WarningMessage { get; set; } = "";
    public List<TranscriptChunkResult> Chunks { get; set; } = new();
}