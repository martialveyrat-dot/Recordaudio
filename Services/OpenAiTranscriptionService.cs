using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class OpenAiTranscriptionService
    {
        private const string TranscriptionEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        private const string ModelName = "gpt-4o-transcribe";

        public string GetApiKeySetupInstructions()
        {
            return
$@"Clé API OpenAI introuvable.

Méthode recommandée :
1. Crée une variable d'environnement Windows nommée OPENAI_API_KEY
2. Mets ta clé API dedans
3. Redémarre l'application

Méthode alternative :
1. Crée le fichier :
{AppPaths.OpenAiApiKeyFilePath}
2. Colle uniquement la clé API dedans";
        }

        public async Task<TranscriptionResult> TranscribeAsync(RecordingItem item, CancellationToken cancellationToken = default)
        {
            try
            {
                if (item == null)
                {
                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "Aucun enregistrement fourni."
                    };
                }

                if (string.IsNullOrWhiteSpace(item.AudioFilePath) || !File.Exists(item.AudioFilePath))
                {
                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "Fichier audio introuvable."
                    };
                }

                string apiKey = GetApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = GetApiKeySetupInstructions()
                    };
                }

                string transcriptFilePath = Path.ChangeExtension(item.AudioFilePath, ".transcript.txt");
                string summaryFilePath = Path.ChangeExtension(item.AudioFilePath, ".summary.txt");

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                await using var fileStream = File.OpenRead(item.AudioFilePath);
                using var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

                using var form = new MultipartFormDataContent();

                form.Add(fileContent, "file", Path.GetFileName(item.AudioFilePath));
                form.Add(new StringContent(ModelName), "model");
                form.Add(new StringContent("text"), "response_format");

                string prompt =
                    "Transcris fidèlement cet appel de découverte commercial en français. " +
                    "Conserve les noms propres, acronymes, logiciels, montants et dates. " +
                    "Termes fréquents possibles : Baker Tilly, Agility, pré-compta, facturation électronique, CRM, Teams, intégration digitale.";

                form.Add(new StringContent(prompt), "prompt");

                AppLogger.Log($"Transcription OpenAI démarrée pour : {item.AudioFilePath}");

                using var response = await httpClient.PostAsync(TranscriptionEndpoint, form, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    AppLogger.Log($"Erreur API OpenAI transcription : {(int)response.StatusCode} - {responseBody}");

                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = $"Erreur OpenAI {(int)response.StatusCode} : {responseBody}"
                    };
                }

                string transcriptText = responseBody.Trim();

                if (string.IsNullOrWhiteSpace(transcriptText))
                {
                    return new TranscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "La transcription retournée est vide."
                    };
                }

                File.WriteAllText(transcriptFilePath, transcriptText);

                AppLogger.Log($"Transcription OpenAI terminée. Fichier texte : {transcriptFilePath}");

                return new TranscriptionResult
                {
                    Success = true,
                    TranscriptText = transcriptText,
                    TranscriptFilePath = transcriptFilePath,
                    SummaryFilePath = summaryFilePath
                };
            }
            catch (TaskCanceledException)
            {
                return new TranscriptionResult
                {
                    Success = false,
                    ErrorMessage = "La transcription a expiré ou a été annulée."
                };
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur TranscribeAsync : {ex}");
                return new TranscriptionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string GetApiKey()
        {
            string? envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return envKey.Trim();
            }

            try
            {
                AppPaths.EnsureDirectories();

                if (File.Exists(AppPaths.OpenAiApiKeyFilePath))
                {
                    return File.ReadAllText(AppPaths.OpenAiApiKeyFilePath).Trim();
                }
            }
            catch
            {
            }

            return "";
        }
    }
}