using System;

namespace Recordaudio.Models
{
    public sealed class RecordingItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ClientName { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        // Dossier visible pour l'utilisateur
        public string VisibleFolderPath { get; set; } = "";

        // Dossier technique caché (_work)
        public string WorkFolderPath { get; set; } = "";

        // Audio final visible
        public string AudioFilePath { get; set; } = "";

        // Pistes de travail séparées
        public string SystemAudioFilePath { get; set; } = "";
        public string MicAudioFilePath { get; set; } = "";

        public double DurationSeconds { get; set; }
        public RecordingStatus Status { get; set; } = RecordingStatus.Recorded;

        // Transcript brut
        public string TranscriptText { get; set; } = "";
        public string TranscriptFilePath { get; set; } = "";

        // Transcript anonymisé
        public string AnonymizedTranscriptText { get; set; } = "";
        public string AnonymizedTranscriptFilePath { get; set; } = "";

        // Analyse texte
        public string SummaryText { get; set; } = "";
        public string SummaryFilePath { get; set; } = "";

        // Analyse structurée / checklist
        public string AnalysisJsonFilePath { get; set; } = "";
        public string ChecklistFilePath { get; set; } = "";

        // Document final visible pour l'utilisateur
        public string FinalDocumentPath { get; set; } = "";

        // Compatibilité avec les morceaux de code plus anciens
        public string DocxFilePath
        {
            get => FinalDocumentPath;
            set => FinalDocumentPath = value ?? "";
        }

        public string LastError { get; set; } = "";

        // Chunking / transcription
        public string ChunkFolderPath { get; set; } = "";
        public string ChunkManifestPath { get; set; } = "";
        public int ChunkCount { get; set; }

        public override string ToString()
        {
            return $"{CreatedAt:yyyy-MM-dd HH:mm} | {ClientName} | {Status}";
        }
    }
}