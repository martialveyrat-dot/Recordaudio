namespace Recordaudio.Models
{
    public sealed class BackendCmdAnalysisResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public CmdAnalysisResult? Analysis { get; set; }
    }
}