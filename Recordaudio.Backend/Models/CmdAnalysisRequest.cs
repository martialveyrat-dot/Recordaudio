namespace Recordaudio.Backend.Models;

public sealed class CmdAnalysisRequest
{
    public string ClientAlias { get; set; } = "CLIENT";
    public string Transcript { get; set; } = "";
}