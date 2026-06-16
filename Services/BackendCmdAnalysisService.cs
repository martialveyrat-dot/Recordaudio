using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class BackendCmdAnalysisService
    {
        private readonly string backendUrl;

        public BackendCmdAnalysisService()
        {
            backendUrl = ResolveBackendUrl();
        }

        public async Task<CmdAnalysisResult> AnalyzeTranscriptAsync(
            string clientAlias,
            string anonymizedTranscript,
            CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            string url = $"{backendUrl.TrimEnd('/')}/api/analyze-transcript";

            var payload = new
            {
                ClientAlias = string.IsNullOrWhiteSpace(clientAlias) ? "CLIENT" : clientAlias,
                Transcript = anonymizedTranscript
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.PostAsync(url, content, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            BackendCmdAnalysisResponse? result =
                JsonSerializer.Deserialize<BackendCmdAnalysisResponse>(body, options);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    result?.ErrorMessage ?? $"Erreur backend analyse {(int)response.StatusCode} : {body}");
            }

            if (result == null || !result.Success || result.Analysis == null)
            {
                throw new InvalidOperationException(
                    result?.ErrorMessage ?? "Le backend n'a pas renvoyé d'analyse CMD.");
            }

            return result.Analysis;
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
    }
}