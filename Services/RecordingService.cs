using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class RecordingService
    {
        private readonly RecordingWorkspaceService workspaceService = new();

        private WasapiLoopbackCapture? loopbackCapture;
        private WaveInEvent? micCapture;

        private WaveFileWriter? finalWriter;
        private WaveFileWriter? systemRawWriter;
        private WaveFileWriter? micRawWriter;

        private string? finalFilePath;
        private string? systemAudioFilePath;
        private string? micAudioFilePath;

        private string currentVisibleRootFolder = "";
        private string currentWorkFolder = "";
        private string currentAudioFolder = "";
        private string currentRecordingBaseName = "";

        private CancellationTokenSource? mixCts;
        private Task? mixTask;

        private readonly object loopbackLock = new();
        private readonly object micLock = new();

        private readonly List<TimestampedAudioChunk> loopbackChunks = new();
        private readonly List<TimestampedAudioChunk> micChunks = new();

        private Stopwatch? timelineStopwatch;

        private bool isRecording = false;
        private bool includeMic = false;
        private bool stopRequested = false;
        private bool loopbackStopped = false;
        private bool micStopped = false;

        private WaveFormat? captureFormat;
        private WaveFormat? outputFormat;

        private long outputFrameCursor = 0;
        private long highestReceivedEndFrame = 0;
        private long? finalizationTargetFrame = null;

        private const int ChunkMilliseconds = 20;
        private const int SafetyBufferMilliseconds = 250;
        private static readonly TimeSpan MaxRecordingDuration = TimeSpan.FromHours(2);

        private bool loopbackAnchorInitialized = false;
        private bool micAnchorInitialized = false;

        private long loopbackNextFrameIndex = 0;
        private long micNextFrameIndex = 0;

        private const float MicGain = 0.9f;

        private const int OutputSampleRate = 16000;
        private const int OutputBitsPerSample = 16;
        private const int OutputChannels = 1;

        private const long MaxOutputFileBytes = 500L * 1024L * 1024L;
        private long maxWritableInputFrames = 0;

        private string currentClientName = "";

        public bool IsRecording => isRecording;

        public event Action<string>? StatusChanged;
        public event Action<string>? RecordingCompleted;
        public event Action<string, string>? RecordingCompletedDetailed;
        public event Action<string>? ErrorOccurred;

        public List<AudioDeviceItem> GetAvailableMicrophones()
        {
            var devices = new List<AudioDeviceItem>();

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var caps = WaveInEvent.GetCapabilities(i);
                devices.Add(new AudioDeviceItem
                {
                    DeviceNumber = i,
                    DisplayName = caps.ProductName
                });
            }

            return devices;
        }

        public async Task StartAsync(RecordingSessionOptions options)
        {
            if (isRecording)
                return;

            try
            {
                ResetInternalState();
                AppPaths.EnsureDirectories();

                string clientName = SanitizeFileName(options.ClientName);
                if (string.IsNullOrWhiteSpace(clientName))
                {
                    clientName = "client_inconnu";
                }

                currentClientName = clientName;

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseName = $"{timestamp}_{clientName}";
                currentRecordingBaseName = baseName;

                RecordingWorkspace workspace = workspaceService.CreateForNewRecording(baseName);

                currentVisibleRootFolder = workspace.VisibleRootFolder;
                currentWorkFolder = workspace.WorkFolder;
                currentAudioFolder = workspace.AudioFolder;

                finalFilePath = Path.Combine(workspace.VisibleRootFolder, "Enregistrement_audio.wav");
                systemAudioFilePath = Path.Combine(workspace.AudioFolder, $"{baseName}.system.raw.wav");
                micAudioFilePath = Path.Combine(workspace.AudioFolder, $"{baseName}.mic.raw.wav");

                includeMic = options.IncludeMic;
                stopRequested = false;
                loopbackStopped = false;
                micStopped = !includeMic;

                timelineStopwatch = Stopwatch.StartNew();

                StartLoopbackCapture();

                if (captureFormat == null)
                {
                    throw new InvalidOperationException("Impossible d'initialiser le format audio système.");
                }

                outputFormat = new WaveFormat(OutputSampleRate, OutputBitsPerSample, OutputChannels);
                finalWriter = new WaveFileWriter(finalFilePath, outputFormat);
                systemRawWriter = new WaveFileWriter(systemAudioFilePath, captureFormat);

                maxWritableInputFrames = (long)(captureFormat.SampleRate * MaxRecordingDuration.TotalSeconds);

                if (includeMic)
                {
                    if (!options.MicrophoneDeviceNumber.HasValue)
                    {
                        throw new InvalidOperationException("Aucun micro sélectionné.");
                    }

                    StartMicCapture(options.MicrophoneDeviceNumber.Value);
                }

                mixCts = new CancellationTokenSource();
                mixTask = RunTimelineMixerAsync(mixCts.Token);

                isRecording = true;

                AppLogger.Log($"Service démarré. Fichier final visible : {finalFilePath}");
                AppLogger.Log($"Fichier source système : {systemAudioFilePath}");
                AppLogger.Log($"Fichier source micro : {micAudioFilePath}");

                RaiseStatus(includeMic
                    ? "Enregistrement en cours... (audio interne + micro)"
                    : "Enregistrement en cours... (audio interne seul)");
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur StartAsync : {ex}");
                await SafeStopAndCleanupAsync();
                RaiseError($"Erreur au démarrage : {ex.Message}");
                RaiseStatus("Erreur au démarrage");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!isRecording)
                return;

            try
            {
                stopRequested = true;
                AppLogger.Log("StopAsync demandé.");
                RaiseStatus("Finalisation en cours...");

                try { loopbackCapture?.StopRecording(); } catch (Exception ex) { AppLogger.Log($"Stop loopback: {ex.Message}"); }
                try { micCapture?.StopRecording(); } catch (Exception ex) { AppLogger.Log($"Stop mic: {ex.Message}"); }

                if (mixTask != null)
                {
                    await mixTask;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur StopAsync : {ex}");
                await SafeStopAndCleanupAsync();
                RaiseError($"Erreur lors de l'arrêt : {ex.Message}");
                RaiseStatus("Erreur à l'arrêt");
                throw;
            }
        }

        public async Task CleanupOnCloseAsync()
        {
            if (isRecording)
            {
                AppLogger.Log("CleanupOnCloseAsync déclenché.");
                await SafeStopAndCleanupAsync();
            }
        }

        public string GetRecordingFolder()
        {
            AppPaths.EnsureDirectories();
            return AppPaths.RecordingsDirectory;
        }

        public static string InferSystemAudioPath(string finalAudioPath)
        {
            string visibleRoot = Path.GetDirectoryName(finalAudioPath) ?? "";
            string folderName = new DirectoryInfo(visibleRoot).Name;
            return Path.Combine(visibleRoot, "_work", "audio", $"{folderName}.system.raw.wav");
        }

        public static string InferMicAudioPath(string finalAudioPath)
        {
            string visibleRoot = Path.GetDirectoryName(finalAudioPath) ?? "";
            string folderName = new DirectoryInfo(visibleRoot).Name;
            return Path.Combine(visibleRoot, "_work", "audio", $"{folderName}.mic.raw.wav");
        }

        private void StartLoopbackCapture()
        {
            loopbackCapture = new WasapiLoopbackCapture();

            if (loopbackCapture.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat ||
                loopbackCapture.WaveFormat.BitsPerSample != 32 ||
                loopbackCapture.WaveFormat.Channels != 2)
            {
                throw new NotSupportedException(
                    $"Format loopback non supporté : {loopbackCapture.WaveFormat}. Cette version attend du float stéréo 32 bits.");
            }

            captureFormat = loopbackCapture.WaveFormat;

            loopbackCapture.DataAvailable += LoopbackCapture_DataAvailable;
            loopbackCapture.RecordingStopped += LoopbackCapture_RecordingStopped;

            loopbackCapture.StartRecording();
        }

        private void StartMicCapture(int deviceNumber)
        {
            if (captureFormat == null)
            {
                throw new InvalidOperationException("Le format système n'est pas initialisé.");
            }

            micCapture = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(captureFormat.SampleRate, 16, 1),
                BufferMilliseconds = ChunkMilliseconds
            };

            micRawWriter = new WaveFileWriter(micAudioFilePath!, micCapture.WaveFormat);

            micCapture.DataAvailable += MicCapture_DataAvailable;
            micCapture.RecordingStopped += MicCapture_RecordingStopped;

            micCapture.StartRecording();
        }

        private void LoopbackCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (timelineStopwatch == null || captureFormat == null || e.BytesRecorded <= 0)
                    return;

                systemRawWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                systemRawWriter?.Flush();

                int frames = e.BytesRecorded / (sizeof(float) * 2);
                if (frames <= 0)
                    return;

                float[] samples = new float[frames * 2];
                Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

                long startFrame;

                lock (loopbackLock)
                {
                    if (!loopbackAnchorInitialized)
                    {
                        long currentFrame = GetCurrentFrameFromClock();
                        startFrame = Math.Max(0, currentFrame - frames);
                        loopbackNextFrameIndex = startFrame + frames;
                        loopbackAnchorInitialized = true;
                    }
                    else
                    {
                        startFrame = loopbackNextFrameIndex;
                        loopbackNextFrameIndex += frames;
                    }

                    loopbackChunks.Add(new TimestampedAudioChunk
                    {
                        StartFrameIndex = startFrame,
                        Samples = samples
                    });
                }

                UpdateHighestReceivedEndFrame(startFrame + frames);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur buffer loopback : {ex}");
                RaiseError($"Erreur buffer audio interne : {ex.Message}");
            }
        }

        private void MicCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (timelineStopwatch == null || captureFormat == null || e.BytesRecorded <= 0)
                    return;

                micRawWriter?.Write(e.Buffer, 0, e.BytesRecorded);
                micRawWriter?.Flush();

                int monoFrames = e.BytesRecorded / sizeof(short);
                if (monoFrames <= 0)
                    return;

                float[] stereoSamples = new float[monoFrames * 2];

                for (int i = 0; i < monoFrames; i++)
                {
                    short pcm = BitConverter.ToInt16(e.Buffer, i * 2);
                    float sample = (pcm / 32768f) * MicGain;

                    int stereoIndex = i * 2;
                    stereoSamples[stereoIndex] = sample;
                    stereoSamples[stereoIndex + 1] = sample;
                }

                long startFrame;

                lock (micLock)
                {
                    if (!micAnchorInitialized)
                    {
                        long currentFrame = GetCurrentFrameFromClock();
                        startFrame = Math.Max(0, currentFrame - monoFrames);
                        micNextFrameIndex = startFrame + monoFrames;
                        micAnchorInitialized = true;
                    }
                    else
                    {
                        startFrame = micNextFrameIndex;
                        micNextFrameIndex += monoFrames;
                    }

                    micChunks.Add(new TimestampedAudioChunk
                    {
                        StartFrameIndex = startFrame,
                        Samples = stereoSamples
                    });
                }

                UpdateHighestReceivedEndFrame(startFrame + monoFrames);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur buffer micro : {ex}");
                RaiseError($"Erreur buffer micro : {ex.Message}");
            }
        }

        private void LoopbackCapture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                systemRawWriter?.Dispose();
                systemRawWriter = null;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur dispose systemRawWriter : {ex}");
            }

            try
            {
                if (loopbackCapture != null)
                {
                    loopbackCapture.DataAvailable -= LoopbackCapture_DataAvailable;
                    loopbackCapture.RecordingStopped -= LoopbackCapture_RecordingStopped;
                    loopbackCapture.Dispose();
                    loopbackCapture = null;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur dispose loopback : {ex}");
            }

            loopbackStopped = true;

            if (e.Exception != null)
            {
                AppLogger.Log($"Erreur stop loopback : {e.Exception}");
                RaiseError($"Erreur arrêt audio interne : {e.Exception.Message}");
            }
        }

        private void MicCapture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                micRawWriter?.Dispose();
                micRawWriter = null;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur dispose micRawWriter : {ex}");
            }

            try
            {
                if (micCapture != null)
                {
                    micCapture.DataAvailable -= MicCapture_DataAvailable;
                    micCapture.RecordingStopped -= MicCapture_RecordingStopped;
                    micCapture.Dispose();
                    micCapture = null;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur dispose mic : {ex}");
            }

            micStopped = true;

            if (e.Exception != null)
            {
                AppLogger.Log($"Erreur stop mic : {e.Exception}");
                RaiseError($"Erreur arrêt micro : {e.Exception.Message}");
            }
        }

        private async Task RunTimelineMixerAsync(CancellationToken token)
        {
            if (captureFormat == null || outputFormat == null || finalWriter == null)
                return;

            int inputSampleRate = captureFormat.SampleRate;
            int inputFramesPerChunk = inputSampleRate * ChunkMilliseconds / 1000;
            if (inputFramesPerChunk <= 0) inputFramesPerChunk = 1;

            int inputStereoSamples = inputFramesPerChunk * 2;
            float[] mixedSamples = new float[inputStereoSamples];

            int outputFramesPerChunk = outputFormat.SampleRate * ChunkMilliseconds / 1000;
            if (outputFramesPerChunk <= 0) outputFramesPerChunk = 1;

            byte[] outputBytes = new byte[outputFramesPerChunk * sizeof(short)];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (timelineStopwatch == null)
                        break;

                    if (!stopRequested && timelineStopwatch.Elapsed > MaxRecordingDuration)
                    {
                        stopRequested = true;
                        AppLogger.Log("Durée max atteinte, arrêt automatique.");
                        RaiseError("Durée maximale atteinte. Arrêt automatique de l'enregistrement.");

                        try { loopbackCapture?.StopRecording(); } catch { }
                        try { micCapture?.StopRecording(); } catch { }
                    }

                    long currentRealTimeFrame = GetCurrentFrameFromClock();
                    long safetyFrames = (long)(inputSampleRate * (SafetyBufferMilliseconds / 1000.0));

                    long safeWritableFrame;
                    if (stopRequested)
                    {
                        if (!finalizationTargetFrame.HasValue)
                        {
                            long snapshot = Interlocked.Read(ref highestReceivedEndFrame);
                            long maxReasonable = currentRealTimeFrame + inputSampleRate;
                            finalizationTargetFrame = Math.Min(snapshot, maxReasonable);
                            AppLogger.Log($"Finalization target frame figé à {finalizationTargetFrame.Value}");
                        }

                        safeWritableFrame = finalizationTargetFrame.Value;
                    }
                    else
                    {
                        safeWritableFrame = Math.Max(0, currentRealTimeFrame - safetyFrames);
                    }

                    if (outputFrameCursor + inputFramesPerChunk > maxWritableInputFrames)
                    {
                        AppLogger.Log("Cap durée écrite atteint.");
                        RaiseError("Cap de sécurité atteint sur la durée écrite. Arrêt du mixage.");
                        break;
                    }

                    if (!stopRequested && outputFrameCursor + inputFramesPerChunk > safeWritableFrame)
                    {
                        await Task.Delay(5, token);
                        continue;
                    }

                    if (stopRequested && outputFrameCursor >= safeWritableFrame)
                    {
                        bool capturesFullyStopped = loopbackStopped && (!includeMic || micStopped);
                        if (capturesFullyStopped)
                        {
                            break;
                        }
                    }

                    Array.Clear(mixedSamples, 0, mixedSamples.Length);

                    MixChunksIntoBuffer(loopbackChunks, loopbackLock, outputFrameCursor, inputFramesPerChunk, mixedSamples);

                    if (includeMic)
                    {
                        MixChunksIntoBuffer(micChunks, micLock, outputFrameCursor, inputFramesPerChunk, mixedSamples);
                    }

                    ConvertStereoFloatToMonoPcm16Resampled(
                        mixedSamples,
                        inputFramesPerChunk,
                        inputSampleRate,
                        outputBytes,
                        outputFramesPerChunk,
                        outputFormat.SampleRate);

                    int safeCount = Math.Min(outputBytes.Length, outputFramesPerChunk * sizeof(short));

                    if (safeCount <= 0)
                    {
                        await Task.Delay(5, token);
                        continue;
                    }

                    if (finalWriter.Position + safeCount > MaxOutputFileBytes)
                    {
                        AppLogger.Log("Cap taille fichier atteint.");
                        RaiseError("Taille maximale de fichier atteinte. Arrêt sécurité.");
                        break;
                    }

                    finalWriter.Write(outputBytes, 0, safeCount);

                    outputFrameCursor += inputFramesPerChunk;

                    bool noPendingChunks = AreAllChunkListsEmpty();
                    bool capturesStopped = loopbackStopped && (!includeMic || micStopped);

                    if (stopRequested && capturesStopped && outputFrameCursor >= safeWritableFrame && noPendingChunks)
                    {
                        break;
                    }

                    await Task.Delay(stopRequested ? 5 : ChunkMilliseconds, token);
                }
            }
            catch (TaskCanceledException)
            {
                AppLogger.Log("Mix task annulée.");
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur boucle mixage : {ex}");
                RaiseError($"Erreur dans la boucle de mixage : {ex.Message}");
            }
            finally
            {
                try { finalWriter?.Dispose(); finalWriter = null; } catch (Exception ex) { AppLogger.Log($"Erreur dispose writer : {ex}"); }
                try { systemRawWriter?.Dispose(); systemRawWriter = null; } catch { }
                try { micRawWriter?.Dispose(); micRawWriter = null; } catch { }

                isRecording = false;

                if (!string.IsNullOrWhiteSpace(finalFilePath) && File.Exists(finalFilePath))
                {
                    AppLogger.Log($"Fichier final prêt : {finalFilePath}");
                    RaiseStatus("Fichier enregistré.");
                    RaiseCompleted(finalFilePath);
                    RaiseCompletedDetailed(finalFilePath, currentClientName);
                }
                else
                {
                    RaiseStatus("Erreur pendant la finalisation");
                }
            }
        }

        private void MixChunksIntoBuffer(List<TimestampedAudioChunk> chunks, object syncLock, long outputStartFrame, int framesPerChunk, float[] mixedSamples)
        {
            long outputEndFrame = outputStartFrame + framesPerChunk;

            lock (syncLock)
            {
                for (int i = chunks.Count - 1; i >= 0; i--)
                {
                    var chunk = chunks[i];
                    long chunkFrames = chunk.Samples.Length / 2;
                    long chunkStart = chunk.StartFrameIndex;
                    long chunkEnd = chunkStart + chunkFrames;

                    if (chunkEnd <= outputStartFrame)
                    {
                        chunks.RemoveAt(i);
                        continue;
                    }

                    if (chunkStart >= outputEndFrame)
                    {
                        continue;
                    }

                    long overlapStart = Math.Max(chunkStart, outputStartFrame);
                    long overlapEnd = Math.Min(chunkEnd, outputEndFrame);

                    if (overlapEnd <= overlapStart)
                    {
                        continue;
                    }

                    int chunkOffsetFrames = (int)(overlapStart - chunkStart);
                    int outputOffsetFrames = (int)(overlapStart - outputStartFrame);
                    int overlapFrames = (int)(overlapEnd - overlapStart);

                    for (int frame = 0; frame < overlapFrames; frame++)
                    {
                        int chunkSampleIndex = (chunkOffsetFrames + frame) * 2;
                        int outputSampleIndex = (outputOffsetFrames + frame) * 2;

                        mixedSamples[outputSampleIndex] += chunk.Samples[chunkSampleIndex];
                        mixedSamples[outputSampleIndex + 1] += chunk.Samples[chunkSampleIndex + 1];
                    }
                }
            }
        }

        private void ConvertStereoFloatToMonoPcm16Resampled(
            float[] inputStereo,
            int inputFrameCount,
            int inputSampleRate,
            byte[] outputBytes,
            int outputFrameCount,
            int outputSampleRate)
        {
            for (int outIndex = 0; outIndex < outputFrameCount; outIndex++)
            {
                double srcPosition = outIndex * (double)inputSampleRate / outputSampleRate;
                int srcIndex0 = (int)Math.Floor(srcPosition);
                int srcIndex1 = Math.Min(srcIndex0 + 1, inputFrameCount - 1);
                double frac = srcPosition - srcIndex0;

                float mono0 = 0.5f * (inputStereo[srcIndex0 * 2] + inputStereo[srcIndex0 * 2 + 1]);
                float mono1 = 0.5f * (inputStereo[srcIndex1 * 2] + inputStereo[srcIndex1 * 2 + 1]);

                float sample = (float)(mono0 + (mono1 - mono0) * frac);

                if (sample > 1f) sample = 1f;
                if (sample < -1f) sample = -1f;

                short pcm = (short)(sample * short.MaxValue);

                outputBytes[outIndex * 2] = (byte)(pcm & 0xFF);
                outputBytes[outIndex * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }
        }

        private bool AreAllChunkListsEmpty()
        {
            lock (loopbackLock)
            {
                if (loopbackChunks.Count > 0)
                    return false;
            }

            if (includeMic)
            {
                lock (micLock)
                {
                    if (micChunks.Count > 0)
                        return false;
                }
            }

            return true;
        }

        private long GetCurrentFrameFromClock()
        {
            if (timelineStopwatch == null || captureFormat == null)
                return 0;

            double elapsedSeconds = timelineStopwatch.Elapsed.TotalSeconds;
            return (long)Math.Round(elapsedSeconds * captureFormat.SampleRate);
        }

        private void UpdateHighestReceivedEndFrame(long endFrame)
        {
            long current;
            do
            {
                current = highestReceivedEndFrame;
                if (endFrame <= current)
                    return;
            }
            while (Interlocked.CompareExchange(ref highestReceivedEndFrame, endFrame, current) != current);
        }

        private async Task SafeStopAndCleanupAsync()
        {
            try { stopRequested = true; } catch { }
            try { loopbackCapture?.StopRecording(); } catch { }
            try { micCapture?.StopRecording(); } catch { }
            try { mixCts?.Cancel(); } catch { }

            try
            {
                if (mixTask != null)
                {
                    await mixTask;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur await mixTask cleanup : {ex}");
            }

            try { finalWriter?.Dispose(); finalWriter = null; } catch { }
            try { systemRawWriter?.Dispose(); systemRawWriter = null; } catch { }
            try { micRawWriter?.Dispose(); micRawWriter = null; } catch { }

            try { loopbackCapture?.Dispose(); loopbackCapture = null; } catch { }
            try { micCapture?.Dispose(); micCapture = null; } catch { }

            isRecording = false;
            ResetInternalState();
        }

        private void ResetInternalState()
        {
            lock (loopbackLock) { loopbackChunks.Clear(); }
            lock (micLock) { micChunks.Clear(); }

            outputFrameCursor = 0;
            highestReceivedEndFrame = 0;
            finalizationTargetFrame = null;

            loopbackStopped = false;
            micStopped = false;
            stopRequested = false;

            captureFormat = null;
            outputFormat = null;
            timelineStopwatch = null;

            loopbackAnchorInitialized = false;
            micAnchorInitialized = false;
            loopbackNextFrameIndex = 0;
            micNextFrameIndex = 0;
            maxWritableInputFrames = 0;
            currentClientName = "";

            finalFilePath = null;
            systemAudioFilePath = null;
            micAudioFilePath = null;

            currentVisibleRootFolder = "";
            currentWorkFolder = "";
            currentAudioFolder = "";
            currentRecordingBaseName = "";
        }

        private string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c.ToString(), "");
            }

            input = input.Trim();
            input = input.Replace(" ", "_");

            return input;
        }

        private void RaiseStatus(string message) => StatusChanged?.Invoke(message);
        private void RaiseCompleted(string filePath) => RecordingCompleted?.Invoke(filePath);
        private void RaiseCompletedDetailed(string filePath, string clientName) => RecordingCompletedDetailed?.Invoke(filePath, clientName);
        private void RaiseError(string message) => ErrorOccurred?.Invoke(message);
    }
}