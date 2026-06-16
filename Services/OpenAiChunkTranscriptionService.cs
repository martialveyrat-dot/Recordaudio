using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class OpenAiChunkTranscriptionService
    {
        private const string Endpoint = "https://api.openai.com/v1/audio/transcriptions";
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

        public async Task<TranscriptionResult> TranscribeChunksAsync(
            RecordingItem item,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (item == null)
                {
                    return Fail("Aucun enregistrement fourni.");
                }

                if (string.IsNullOrWhiteSpace(item.ChunkManifestPath) || !File.Exists(item.ChunkManifestPath))
                {
                    return Fail("Manifest de chunks introuvable. Prépare les chunks d'abord.");
                }

                string apiKey = GetApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Fail(GetApiKeySetupInstructions());
                }

                var manifest = LoadManifest(item.ChunkManifestPath);
                if (manifest == null || manifest.Chunks.Count == 0)
                {
                    return Fail("Aucun chunk trouvé dans le manifest.");
                }

                string chunkTranscriptFolder = Path.Combine(
                    item.ChunkFolderPath,
                    "transcripts"
                );

                Directory.CreateDirectory(chunkTranscriptFolder);

                var chunkTranscriptPaths = new List<string>();
                var failedChunks = new List<string>();

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                for (int i = 0; i < manifest.Chunks.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var chunk = manifest.Chunks[i];

                    if (string.IsNullOrWhiteSpace(chunk.FilePath) || !File.Exists(chunk.FilePath))
                    {
                        string msg = $"Chunk manquant : {chunk.FilePath}";
                        AppLogger.Log(msg);
                        failedChunks.Add(msg);
                        continue;
                    }

                    string chunkTranscriptPath = Path.Combine(
                        chunkTranscriptFolder,
                        $"chunk_{chunk.Index:00}.transcript.txt"
                    );

                    string chunkErrorPath = Path.Combine(
                        chunkTranscriptFolder,
                        $"chunk_{chunk.Index:00}.error.txt"
                    );

                    try
                    {
                        string transcriptText = await TranscribeSingleChunkAsync(
                            httpClient,
                            chunk.FilePath,
                            cancellationToken);

                        File.WriteAllText(chunkTranscriptPath, transcriptText);
                        chunkTranscriptPaths.Add(chunkTranscriptPath);

                        if (File.Exists(chunkErrorPath))
                        {
                            File.Delete(chunkErrorPath);
                        }

                        AppLogger.Log($"Chunk transcrit OK : {chunk.FilePath}");
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Chunk {chunk.Index:00} en erreur : {ex.Message}";
                        AppLogger.Log(msg);
                        File.WriteAllText(chunkErrorPath, msg);
                        failedChunks.Add(msg);
                    }
                }

                if (chunkTranscriptPaths.Count == 0)
                {
                    return Fail("Aucun chunk n'a pu être transcrit.");
                }

                string globalTranscriptPath = Path.Combine(
                    item.ChunkFolderPath,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.transcript.txt"
                );

                var recomposer = new TranscriptRecompositionService();
                string recomposedText = recomposer.RecomposeFromChunkTexts(
                    chunkTranscriptPaths,
                    globalTranscriptPath);

                if (failedChunks.Count > 0)
                {
                    recomposedText += Environment.NewLine + Environment.NewLine +
                                      "=== CHUNKS EN ÉCHEC ===" + Environment.NewLine +
                                      string.Join(Environment.NewLine, failedChunks);
                    File.WriteAllText(globalTranscriptPath, recomposedText);
                }

                return new TranscriptionResult
                {
                    Success = true,
                    TranscriptText = recomposedText,
                    TranscriptFilePath = globalTranscriptPath,
                    SummaryFilePath = Path.ChangeExtension(globalTranscriptPath, ".summary.txt"),
                    ErrorMessage = failedChunks.Count > 0
                        ? $"Transcription partielle : {failedChunks.Count} chunk(s) en échec."
                        : ""
                };
            }
            catch (TaskCanceledException)
            {
                return Fail("La transcription a été annulée.");
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur TranscribeChunksAsync : {ex}");
                return Fail(ex.Message);
            }
        }

        private async Task<string> TranscribeSingleChunkAsync(
            HttpClient httpClient,
            string chunkPath,
            CancellationToken cancellationToken)
        {
            await using var fileStream = File.OpenRead(chunkPath);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            using var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", Path.GetFileName(chunkPath));
            form.Add(new StringContent(ModelName), "model");
            form.Add(new StringContent("json"), "response_format");

            string prompt =
                "Transcris fidèlement cet échange commercial en français. " +
                "Conserve les acronymes, outils et termes métier si présents. " +
                "N'invente rien.";

            form.Add(new StringContent(prompt), "prompt");

            using var response = await httpClient.PostAsync(Endpoint, form, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Erreur OpenAI {(int)response.StatusCode} : {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("text", out JsonElement textElement))
            {
                throw new InvalidOperationException("Réponse OpenAI sans champ 'text'.");
            }

            string transcriptText = textElement.GetString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(transcriptText))
            {
                throw new InvalidOperationException("Transcription vide pour ce chunk.");
            }

            return transcriptText;
        }

        private ChunkManifest? LoadManifest(string manifestPath)
        {
            string json = File.ReadAllText(manifestPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<ChunkManifest>(json, options);
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

        private TranscriptionResult Fail(string message)
        {
            return new TranscriptionResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}