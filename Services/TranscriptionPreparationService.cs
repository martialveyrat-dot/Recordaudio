using System;
using System.IO;
using NAudio.Wave;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class TranscriptionPreparationService
    {
        public TranscriptionResult PrepareForTranscription(RecordingItem item)
        {
            try
            {
                if (!File.Exists(item.AudioFilePath))
                {
                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "Fichier audio introuvable."
                    };
                }

                string transcriptFilePath = Path.ChangeExtension(item.AudioFilePath, ".transcript.txt");
                string summaryFilePath = Path.ChangeExtension(item.AudioFilePath, ".summary.txt");

                string preparationText =
$@"=== PRÉPARATION TRANSCRIPTION ===

Client : {item.ClientName}
Date : {item.CreatedAt:yyyy-MM-dd HH:mm:ss}
Fichier audio : {item.AudioFilePath}
Durée estimée : {item.DurationSeconds:0.0} secondes

STATUT :
Ce fichier est prêt à être envoyé dans un moteur de transcription.

ÉTAPES FUTURES :
1. Envoyer l'audio à un moteur de speech-to-text
2. Récupérer le verbatim
3. Sauvegarder le verbatim dans :
{transcriptFilePath}
4. Générer une synthèse commerciale dans :
{summaryFilePath}
";

                File.WriteAllText(transcriptFilePath, preparationText);

                return new TranscriptionResult
                {
                    Success = true,
                    TranscriptText = preparationText,
                    TranscriptFilePath = transcriptFilePath,
                    SummaryFilePath = summaryFilePath
                };
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur préparation transcription : {ex.Message}");

                return new TranscriptionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public double TryGetDurationSeconds(string audioFilePath)
        {
            try
            {
                using var reader = new AudioFileReader(audioFilePath);
                return reader.TotalTime.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}