using System.Text;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class CmdAnalysisFormatter
    {
        public string BuildAnalysisReport(CmdAnalysisResult analysis)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== ANALYSE COMMERCIALE CMD ===");
            sb.AppendLine();
            sb.AppendLine($"Nature de l'appel : {analysis.CallType}");
            sb.AppendLine($"Sujet principal : {analysis.KeySubject}");
            sb.AppendLine();

            sb.AppendLine("=== SYNTHÈSE EXÉCUTIVE ===");
            sb.AppendLine(analysis.ExecutiveSummary);
            sb.AppendLine();

            AppendListSection(sb, "CONTEXTE CLIENT", analysis.ContextFacts);
            AppendListSection(sb, "SITUATION ACTUELLE", analysis.CurrentSituation);
            AppendListSection(sb, "BESOINS EXPLICITES", analysis.ExplicitNeeds);
            AppendListSection(sb, "BESOINS IMPLICITES", analysis.ImplicitNeeds);

            AppendListSection(sb, "ATTENTES EXPLICITES DU CLIENT", analysis.ClientExplicitExpectations);
            AppendListSection(sb, "OBJECTIONS DU CLIENT", analysis.ClientObjections);
            AppendListSection(sb, "SIGNAUX D'INTÉRÊT", analysis.InterestSignals);
            AppendListSection(sb, "SIGNAUX DE DÉSENGAGEMENT", analysis.DisengagementSignals);

            AppendListSection(sb, "DOULEURS", analysis.Pains);
            AppendListSection(sb, "IMPACTS", analysis.Impacts);

            sb.AppendLine("=== LECTURE COMMERCIALE CMD ===");
            sb.AppendLine($"Urgence : {analysis.UrgencyLevel}");
            sb.AppendLine($"Maturité : {analysis.MaturityLevel}");
            sb.AppendLine($"Complexité : {analysis.ComplexityLevel}");
            sb.AppendLine($"Fit : {analysis.FitLevel}");
            sb.AppendLine();

            AppendListSection(sb, "OPPORTUNITÉS", analysis.Opportunities);
            AppendListSection(sb, "RISQUES / FREINS", analysis.Risks);
            AppendListSection(sb, "INFORMATIONS MANQUANTES", analysis.MissingInformation);

            sb.AppendLine("=== RECOMMANDATION ===");
            sb.AppendLine($"Prochaine étape recommandée : {analysis.RecommendedNextStep}");
            sb.AppendLine($"Objectif du prochain rendez-vous : {analysis.MeetingObjective}");
            sb.AppendLine();

            sb.AppendLine("=== BLOC AGILITY ===");
            sb.AppendLine(analysis.AgilityNote);
            sb.AppendLine();

            sb.AppendLine("=== MAIL DE SUIVI ===");
            sb.AppendLine(analysis.FollowupEmailDraft);
            sb.AppendLine();

            AppendListSection(sb, "CHECKLIST DE REQUALIFICATION", analysis.DiscoveryChecklist);

            return sb.ToString().Trim();
        }

        public string BuildChecklist(CmdAnalysisResult analysis)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== CHECKLIST DE POINTS À CREUSER ===");
            sb.AppendLine();

            foreach (var item in analysis.DiscoveryChecklist)
            {
                sb.AppendLine($"- {item}");
            }

            if (analysis.MissingInformation.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== INFORMATIONS MANQUANTES DÉTECTÉES ===");

                foreach (var item in analysis.MissingInformation)
                {
                    sb.AppendLine($"- {item}");
                }
            }

            return sb.ToString().Trim();
        }

        private void AppendListSection(StringBuilder sb, string title, List<string> items)
        {
            sb.AppendLine($"=== {title} ===");

            if (items == null || items.Count == 0)
            {
                sb.AppendLine("- Non mentionné");
            }
            else
            {
                foreach (var item in items)
                {
                    sb.AppendLine($"- {item}");
                }
            }

            sb.AppendLine();
        }
    }
}