using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NAudio.Wave;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class AudioChunkingService
    {
        private readonly RecordingWorkspaceService workspaceService = new();

        private const double TargetChunkSeconds = 12 * 60;
        private const double MaxChunkSeconds = 15 * 60;
        private const double MinChunkSeconds = 8 * 60;
        private const double OverlapSeconds = 1.0;
        private const double SilenceThresholdDb = -35.0;
        private const double MinSilenceSeconds = 0.8;

        public async Task<ChunkPreparationResult> PrepareChunksAsync(RecordingItem item)
        {
            try
            {
                if (item == null)
                    return Fail("Aucun enregistrement fourni.");

                if (string.IsNullOrWhiteSpace(item.AudioFilePath) || !File.Exists(item.AudioFilePath))
                    return Fail("Fichier audio final introuvable.");

                RecordingWorkspace workspace = !string.IsNullOrWhiteSpace(item.WorkFolderPath)
                    ? new RecordingWorkspace
                    {
                        VisibleRootFolder = item.VisibleFolderPath,
                        WorkFolder = item.WorkFolderPath,
                        AudioFolder = Path.Combine(item.WorkFolderPath, "audio"),
                        ChunksRootFolder = Path.Combine(item.WorkFolderPath, "chunks"),
                        AnalysisFolder = Path.Combine(item.WorkFolderPath, "analysis")
                    }
                    : workspaceService.InferFromAudioPath(item.AudioFilePath);

                workspaceService.EnsureHidden(workspace.WorkFolder);
                workspaceService.EnsureHidden(workspace.ChunksRootFolder);

                string systemSource = !string.IsNullOrWhiteSpace(item.SystemAudioFilePath)
                    ? item.SystemAudioFilePath
                    : RecordingService.InferSystemAudioPath(item.AudioFilePath);

                string micSource = !string.IsNullOrWhiteSpace(item.MicAudioFilePath)
                    ? item.MicAudioFilePath
                    : RecordingService.InferMicAudioPath(item.AudioFilePath);

                if (!File.Exists(systemSource))
                    return Fail($"Piste système introuvable : {systemSource}");

                if (!File.Exists(micSource))
                    return Fail($"Piste micro introuvable : {micSource}");

                string ffmpegPath = ResolveFfmpegPath();
                if (!File.Exists(ffmpegPath))
                    return Fail(GetFfmpegSetupInstructions());

                Directory.CreateDirectory(workspace.ChunksRootFolder);

                string chunkFolder = Path.Combine(
                    workspace.ChunksRootFolder,
                    Path.GetFileNameWithoutExtension(item.AudioFilePath) + "_chunks");

                Directory.CreateDirectory(chunkFolder);
                workspaceService.EnsureHidden(chunkFolder);

                double totalDuration = GetAudioDuration(item.AudioFilePath);
                if (totalDuration <= 0)
                    return Fail("Impossible de lire la durée du fichier audio.");

                AppLogger.Log($"Chunking source-aware démarré pour {item.AudioFilePath} (durée={totalDuration:0.0}s)");

                var silenceMarkers = await DetectSilencesAsync(ffmpegPath, item.AudioFilePath);
                var segments = BuildSegments(totalDuration, silenceMarkers);

                var chunks = new List<AudioChunkItem>();

                for (int i = 0; i < segments.Count; i++)
                {
                    var seg = segments[i];

                    string systemChunkPath = Path.Combine(chunkFolder, $"chunk_{i + 1:00}_system.wav");
                    string micChunkPath = Path.Combine(chunkFolder, $"chunk_{i + 1:00}_mic.wav");

                    await ExtractChunkAsync(ffmpegPath, systemSource, systemChunkPath, seg.Start, seg.End - seg.Start);
                    await ExtractChunkAsync(ffmpegPath, micSource, micChunkPath, seg.Start, seg.End - seg.Start);

                    long systemSize = new FileInfo(systemChunkPath).Length;
                    long micSize = new FileInfo(micChunkPath).Length;

                    chunks.Add(new AudioChunkItem
                    {
                        Index = i + 1,
                        StartSeconds = seg.Start,
                        EndSeconds = seg.End,
                        SystemFilePath = systemChunkPath,
                        MicFilePath = micChunkPath,
                        FilePath = systemChunkPath,
                        EstimatedSystemSizeBytes = systemSize,
                        EstimatedMicSizeBytes = micSize
                    });
                }

                string manifestPath = Path.Combine(chunkFolder, "chunks_manifest.json");
                string previewText = BuildPreviewText(item, chunks, totalDuration);

                var manifest = new
                {
                    recordingId = item.Id,
                    clientName = item.ClientName,
                    sourceAudio = item.AudioFilePath,
                    systemAudio = systemSource,
                    micAudio = micSource,
                    totalDurationSeconds = totalDuration,
                    createdAt = DateTime.Now,
                    overlapSeconds = OverlapSeconds,
                    chunks
                };

                File.WriteAllText(
                    manifestPath,
                    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

                File.WriteAllText(Path.Combine(chunkFolder, "chunks_preview.txt"), previewText);

                AppLogger.Log($"Chunking source-aware terminé. {chunks.Count} chunk(s) généré(s).");

                return new ChunkPreparationResult
                {
                    Success = true,
                    ChunkFolderPath = chunkFolder,
                    ChunkManifestPath = manifestPath,
                    Chunks = chunks,
                    PreviewText = previewText
                };
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur chunking : {ex}");
                return Fail(ex.Message);
            }
        }

        private ChunkPreparationResult Fail(string message)
        {
            return new ChunkPreparationResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        private string GetFfmpegSetupInstructions()
        {
            return
$@"FFmpeg introuvable.

Place ffmpeg.exe ici :
{AppPaths.FfmpegExePath}

Structure attendue :
[app]\tools\ffmpeg\ffmpeg.exe";
        }

        private string ResolveFfmpegPath()
        {
            return AppPaths.FfmpegExePath;
        }

        private double GetAudioDuration(string audioPath)
        {
            try
            {
                using var reader = new AudioFileReader(audioPath);
                return reader.TotalTime.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<List<double>> DetectSilencesAsync(string ffmpegPath, string inputFilePath)
        {
            var silenceEnds = new List<double>();

            string args =
                $"-hide_banner -i \"{inputFilePath}\" " +
                $"-af silencedetect=noise={SilenceThresholdDb.ToString(CultureInfo.InvariantCulture)}dB:d={MinSilenceSeconds.ToString(CultureInfo.InvariantCulture)} " +
                "-f null NUL";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            var stderr = new StringBuilder();

            process.Start();

            string err = await process.StandardError.ReadToEndAsync();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            stderr.AppendLine(err);
            stderr.AppendLine(output);

            string full = stderr.ToString();

            var regex = new Regex(@"silence_end:\s*(?<end>[0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);

            foreach (Match match in regex.Matches(full))
            {
                if (double.TryParse(match.Groups["end"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double end))
                {
                    silenceEnds.Add(end);
                }
            }

            AppLogger.Log($"Silencedetect terminé. {silenceEnds.Count} fin(s) de silence trouvée(s).");
            return silenceEnds;
        }

        private List<(double Start, double End)> BuildSegments(double totalDuration, List<double> silenceEnds)
        {
            var segments = new List<(double Start, double End)>();

            double currentStart = 0;

            while (currentStart < totalDuration)
            {
                double preferredTarget = currentStart + TargetChunkSeconds;
                double hardMax = Math.Min(currentStart + MaxChunkSeconds, totalDuration);

                double minAcceptable = currentStart + MinChunkSeconds;
                double chosenEnd = hardMax;

                double bestSilence = -1;

                foreach (double silenceEnd in silenceEnds)
                {
                    if (silenceEnd >= minAcceptable && silenceEnd <= hardMax)
                    {
                        if (silenceEnd <= preferredTarget)
                        {
                            bestSilence = silenceEnd;
                        }
                        else if (bestSilence < 0)
                        {
                            bestSilence = silenceEnd;
                            break;
                        }
                    }
                }

                if (bestSilence > 0)
                {
                    chosenEnd = bestSilence;
                }

                if (chosenEnd <= currentStart)
                {
                    chosenEnd = Math.Min(currentStart + MaxChunkSeconds, totalDuration);
                }

                segments.Add((currentStart, chosenEnd));

                if (chosenEnd >= totalDuration)
                {
                    break;
                }

                currentStart = Math.Max(0, chosenEnd - OverlapSeconds);
            }

            return segments;
        }

        private async Task ExtractChunkAsync(string ffmpegPath, string inputFilePath, string outputFilePath, double startSeconds, double durationSeconds)
        {
            string args =
                $"-y -hide_banner -i \"{inputFilePath}\" " +
                $"-ss {startSeconds.ToString(CultureInfo.InvariantCulture)} " +
                $"-t {durationSeconds.ToString(CultureInfo.InvariantCulture)} " +
                "-ac 1 -ar 16000 -c:a pcm_s16le " +
                $"\"{outputFilePath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            string err = await process.StandardError.ReadToEndAsync();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(outputFilePath))
            {
                throw new InvalidOperationException($"FFmpeg extraction échouée : {err} {output}");
            }
        }

        private string BuildPreviewText(RecordingItem item, List<AudioChunkItem> chunks, double totalDuration)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== PRÉPARATION CHUNKS ===");
            sb.AppendLine();
            sb.AppendLine($"Client : {item.ClientName}");
            sb.AppendLine($"Date : {item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Durée totale : {totalDuration:0.0} secondes");
            sb.AppendLine($"Nombre de chunks : {chunks.Count}");
            sb.AppendLine();
            sb.AppendLine("Étape suivante : transcription séparée des pistes micro et système.");

            return sb.ToString();
        }
    }
}