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
    public sealed class BackendChunkTranscriptionService
    {
        private readonly string backendUrl;

        public BackendChunkTranscriptionService()
        {
            backendUrl = ResolveBackendUrl();
        }

        public async Task<TranscriptionResult> TranscribeChunksViaBackendAsync(
            RecordingItem item,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (item == null)
                    return Fail("Aucun enregistrement fourni.");

                if (string.IsNullOrWhiteSpace(item.ChunkManifestPath) || !File.Exists(item.ChunkManifestPath))
                    return Fail("Manifest de chunks introuvable. Prépare les chunks d'abord.");

                var manifest = LoadManifest(item.ChunkManifestPath);
                if (manifest == null || manifest.Chunks.Count == 0)
                    return Fail("Aucun chunk trouvé dans le manifest.");

                string transcriptFolder = Path.Combine(item.ChunkFolderPath, "transcripts");
                Directory.CreateDirectory(transcriptFolder);

                var chunkBlockPaths = new List<string>();
                var failedChunks = new List<string>();

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };

                foreach (var chunk in manifest.Chunks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string chunkBlockPath = Path.Combine(transcriptFolder, $"chunk_{chunk.Index:00}.transcript.txt");
                    string chunkErrorPath = Path.Combine(transcriptFolder, $"chunk_{chunk.Index:00}.error.txt");

                    try
                    {
                        string moiText = "";
                        string clientText = "";

                        if (!string.IsNullOrWhiteSpace(chunk.MicFilePath) && File.Exists(chunk.MicFilePath))
                        {
                            moiText = await TranscribeSingleSourceAsync(httpClient, chunk.MicFilePath, cancellationToken);
                        }

                        if (!string.IsNullOrWhiteSpace(chunk.SystemFilePath) && File.Exists(chunk.SystemFilePath))
                        {
                            clientText = await TranscribeSingleSourceAsync(httpClient, chunk.SystemFilePath, cancellationToken);
                        }

                        string blockText = BuildChunkSpeakerBlock(chunk, moiText, clientText);

                        File.WriteAllText(chunkBlockPath, blockText);
                        chunkBlockPaths.Add(chunkBlockPath);

                        if (File.Exists(chunkErrorPath))
                        {
                            File.Delete(chunkErrorPath);
                        }

                        AppLogger.Log($"Chunk source-aware transcrit : {chunk.Index:00}");
                    }
                    catch (Exception ex)
                    {
                        string msg = $"Chunk {chunk.Index:00} en erreur : {ex.Message}";
                        File.WriteAllText(chunkErrorPath, msg);
                        failedChunks.Add(msg);
                        AppLogger.Log(msg);
                    }
                }

                if (chunkBlockPaths.Count == 0)
                    return Fail("Aucun chunk n'a pu être transcrit via le backend.");

                string globalTranscriptPath = Path.Combine(
                    item.ChunkFolderPath,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.transcript.txt");

                string finalTranscript = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    chunkBlockPaths.ConvertAll(File.ReadAllText));

                if (failedChunks.Count > 0)
                {
                    finalTranscript += Environment.NewLine + Environment.NewLine +
                                       "=== CHUNKS EN ÉCHEC ===" + Environment.NewLine +
                                       string.Join(Environment.NewLine, failedChunks);
                }

                File.WriteAllText(globalTranscriptPath, finalTranscript);

                return new TranscriptionResult
                {
                    Success = true,
                    TranscriptText = finalTranscript,
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
                AppLogger.Log($"Erreur TranscribeChunksViaBackendAsync : {ex}");
                return Fail(ex.Message);
            }
        }

        private async Task<string> TranscribeSingleSourceAsync(
            HttpClient httpClient,
            string chunkPath,
            CancellationToken cancellationToken)
        {
            string url = $"{backendUrl.TrimEnd('/')}/api/transcribe";

            await using var fileStream = File.OpenRead(chunkPath);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

            using var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", Path.GetFileName(chunkPath));

            using var response = await httpClient.PostAsync(url, form, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var backendResponse = JsonSerializer.Deserialize<BackendTranscriptionResponse>(responseBody, options);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    backendResponse?.Error ??
                    backendResponse?.ErrorMessage ??
                    $"Erreur backend {(int)response.StatusCode} : {responseBody}");
            }

            if (backendResponse == null || !backendResponse.Success || string.IsNullOrWhiteSpace(backendResponse.Transcript))
            {
                throw new InvalidOperationException("Réponse backend de transcription invalide.");
            }

            return backendResponse.Transcript.Trim();
        }

        private string BuildChunkSpeakerBlock(AudioChunkItem chunk, string moiText, string clientText)
        {
            var lines = new List<string>
            {
                $"=== CHUNK {chunk.Index:00} | {chunk.StartSeconds:0.0}s -> {chunk.EndSeconds:0.0}s ==="
            };

            if (!string.IsNullOrWhiteSpace(moiText))
            {
                lines.Add($"[MOI] {moiText}");
            }

            if (!string.IsNullOrWhiteSpace(clientText))
            {
                lines.Add($"[CLIENT] {clientText}");
            }

            if (string.IsNullOrWhiteSpace(moiText) && string.IsNullOrWhiteSpace(clientText))
            {
                lines.Add("[AUCUN CONTENU TRANSCRIT]");
            }

            return string.Join(Environment.NewLine, lines);
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

        private string ResolveBackendUrl()
        {
            string? envUrl = Environment.GetEnvironmentVariable("RECORDAUDIO_BACKEND_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                return envUrl.Trim();
            }

            try
            {
                AppPaths.EnsureDirectories();

                string configPath = Path.Combine(AppPaths.ConfigDirectory, "backend_url.txt");
                if (File.Exists(configPath))
                {
                    string fileUrl = File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrWhiteSpace(fileUrl))
                    {
                        return fileUrl;
                    }
                }
            }
            catch
            {
            }

            return "http://localhost:5178";
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