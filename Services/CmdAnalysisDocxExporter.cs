using System;
using System.Collections.Generic;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class CmdAnalysisDocxExporter
    {
        private readonly RecordingWorkspaceService workspaceService = new();

        public string Export(CmdAnalysisResult analysis, RecordingItem item)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            string visibleRoot = !string.IsNullOrWhiteSpace(item.VisibleFolderPath)
                ? item.VisibleFolderPath
                : workspaceService.InferFromAudioPath(item.AudioFilePath).VisibleRootFolder;

            Directory.CreateDirectory(visibleRoot);

            string outputPath = Path.Combine(visibleRoot, "Synthese_CMD.docx");

            using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var body = new Body();

            AddDocumentTitle(body, "Analyse commerciale CMD");
            AddMetaInfo(body, item, analysis);

            AddSectionTitle(body, "1. Synthèse exécutive");
            AddHighlightParagraph(body, analysis.ExecutiveSummary);

            AddSectionTitle(body, "2. Recommandation immédiate");
            AddKeyValueParagraph(body, "Prochaine étape recommandée", analysis.RecommendedNextStep);
            AddKeyValueParagraph(body, "Objectif du prochain rendez-vous", analysis.MeetingObjective);

            AddSectionTitle(body, "3. Lecture commerciale CMD");
            AddScoreLine(body, analysis);

            AddSectionTitle(body, "4. Opportunités");
            AddBulletList(body, analysis.Opportunities);

            AddSectionTitle(body, "5. Risques / freins");
            AddBulletList(body, analysis.Risks);

            AddSectionTitle(body, "6. Informations manquantes");
            AddBulletList(body, analysis.MissingInformation);

            AddSectionTitle(body, "7. Attentes explicites du client");
            AddBulletList(body, analysis.ClientExplicitExpectations);

            AddSectionTitle(body, "8. Objections du client");
            AddBulletList(body, analysis.ClientObjections);

            AddSectionTitle(body, "9. Signaux d'intérêt");
            AddBulletList(body, analysis.InterestSignals);

            AddSectionTitle(body, "10. Signaux de désengagement");
            AddBulletList(body, analysis.DisengagementSignals);

            AddSectionTitle(body, "11. Bloc Agility");
            AddMultilineParagraph(body, analysis.AgilityNote);

            AddSectionTitle(body, "12. Contexte client");
            AddBulletList(body, analysis.ContextFacts);

            AddSectionTitle(body, "13. Situation actuelle");
            AddBulletList(body, analysis.CurrentSituation);

            AddSectionTitle(body, "14. Besoins explicites");
            AddBulletList(body, analysis.ExplicitNeeds);

            AddSectionTitle(body, "15. Besoins implicites");
            AddBulletList(body, analysis.ImplicitNeeds);

            AddSectionTitle(body, "16. Douleurs");
            AddBulletList(body, analysis.Pains);

            AddSectionTitle(body, "17. Impacts");
            AddBulletList(body, analysis.Impacts);

            AddSectionTitle(body, "18. Checklist de requalification");
            AddBulletList(body, analysis.DiscoveryChecklist);

            AddSectionTitle(body, "19. Mail de suivi");
            AddMultilineParagraph(body, analysis.FollowupEmailDraft);

            mainPart.Document.Append(body);
            mainPart.Document.Save();

            return outputPath;
        }

        private void AddDocumentTitle(Body body, string text)
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Center },
                    new SpacingBetweenLines() { Before = "100", After = "220" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "34" }),
                    new Text(text)));

            body.Append(paragraph);
        }

        private void AddMetaInfo(Body body, RecordingItem item, CmdAnalysisResult analysis)
        {
            AddMetaParagraph(body, $"Client : {item.ClientName}");
            AddMetaParagraph(body, $"Date : {item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            AddMetaParagraph(body, $"Type d'appel : {analysis.CallType}");
            AddMetaParagraph(body, $"Sujet principal : {analysis.KeySubject}");
            AddBlankLine(body);
        }

        private void AddMetaParagraph(Body body, string text)
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "60" }),
                new Run(
                    new RunProperties(new FontSize() { Val = "21" }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

            body.Append(paragraph);
        }

        private void AddSectionTitle(Body body, string text)
        {
            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { Before = "260", After = "120" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "28" }),
                    new Text(text)));

            body.Append(paragraph);
        }

        private void AddHighlightParagraph(Body body, string text)
        {
            text = string.IsNullOrWhiteSpace(text) ? "Non renseigné" : text;

            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "160" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

            body.Append(paragraph);
        }

        private void AddKeyValueParagraph(Body body, string label, string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? "Non renseigné" : value;

            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "100" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }),
                    new Text(label + " : ")),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }),
                    new Text(value) { Space = SpaceProcessingModeValues.Preserve }));

            body.Append(paragraph);
        }

        private void AddScoreLine(Body body, CmdAnalysisResult analysis)
        {
            string line =
                $"Urgence : {Safe(analysis.UrgencyLevel)}   |   " +
                $"Maturité : {Safe(analysis.MaturityLevel)}   |   " +
                $"Complexité : {Safe(analysis.ComplexityLevel)}   |   " +
                $"Fit : {Safe(analysis.FitLevel)}";

            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "140" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "23" }),
                    new Text(line) { Space = SpaceProcessingModeValues.Preserve }));

            body.Append(paragraph);
        }

        private void AddBulletList(Body body, List<string> items)
        {
            if (items == null || items.Count == 0)
            {
                AddNormalParagraph(body, "- Non mentionné");
                return;
            }

            foreach (var item in items)
            {
                AddNormalParagraph(body, $"• {item}");
            }
        }

        private void AddNormalParagraph(Body body, string text)
        {
            text = string.IsNullOrWhiteSpace(text) ? "Non renseigné" : text;

            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "90" }),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

            body.Append(paragraph);
        }

        private void AddMultilineParagraph(Body body, string text)
        {
            text = string.IsNullOrWhiteSpace(text) ? "Non renseigné" : text;

            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            var paragraph = new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines() { After = "120" }));

            bool first = true;

            foreach (string line in lines)
            {
                if (!first)
                {
                    paragraph.Append(new Break());
                }

                paragraph.Append(
                    new Run(
                        new RunProperties(new FontSize() { Val = "22" }),
                        new Text(line) { Space = SpaceProcessingModeValues.Preserve }));

                first = false;
            }

            body.Append(paragraph);
        }

        private void AddBlankLine(Body body)
        {
            body.Append(new Paragraph(new Run(new Text(" "))));
        }

        private string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "non renseigné" : value;
        }
    }
}