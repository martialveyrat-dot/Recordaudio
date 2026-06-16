namespace Recordaudio.Models
{
    public sealed class RecordingWorkspace
    {
        public string VisibleRootFolder { get; set; } = "";
        public string WorkFolder { get; set; } = "";
        public string AudioFolder { get; set; } = "";
        public string ChunksRootFolder { get; set; } = "";
        public string AnalysisFolder { get; set; } = "";
    }
}