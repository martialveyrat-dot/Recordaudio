namespace Recordaudio.Backend.Models;

public sealed class CmdAnalysisResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public CmdAnalysisResult? Analysis { get; set; }
}