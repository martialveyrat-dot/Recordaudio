namespace Recordaudio.Backend.Models;

public sealed class CmdAnalysisResult
{
    public string CallType { get; set; } = "";
    public string KeySubject { get; set; } = "";
    public string ExecutiveSummary { get; set; } = "";

    public List<string> ContextFacts { get; set; } = new();
    public List<string> CurrentSituation { get; set; } = new();
    public List<string> ExplicitNeeds { get; set; } = new();
    public List<string> ImplicitNeeds { get; set; } = new();

    public List<string> ClientExplicitExpectations { get; set; } = new();
    public List<string> ClientObjections { get; set; } = new();
    public List<string> InterestSignals { get; set; } = new();
    public List<string> DisengagementSignals { get; set; } = new();

    public List<string> Pains { get; set; } = new();
    public List<string> Impacts { get; set; } = new();

    public string UrgencyLevel { get; set; } = "";
    public string MaturityLevel { get; set; } = "";
    public string ComplexityLevel { get; set; } = "";
    public string FitLevel { get; set; } = "";

    public List<string> Opportunities { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public List<string> MissingInformation { get; set; } = new();

    public string RecommendedNextStep { get; set; } = "";
    public string MeetingObjective { get; set; } = "";

    public string AgilityNote { get; set; } = "";
    public string FollowupEmailDraft { get; set; } = "";

    public List<string> DiscoveryChecklist { get; set; } = new();
}