using System;
using System.IO;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class RecordingWorkspaceService
    {
        public RecordingWorkspace CreateForNewRecording(string baseName)
        {
            AppPaths.EnsureDirectories();

            string visibleRoot = Path.Combine(AppPaths.RecordingsDirectory, baseName);
            string workFolder = Path.Combine(visibleRoot, "_work");
            string audioFolder = Path.Combine(workFolder, "audio");
            string chunksRoot = Path.Combine(workFolder, "chunks");
            string analysisFolder = Path.Combine(workFolder, "analysis");

            Directory.CreateDirectory(visibleRoot);
            Directory.CreateDirectory(workFolder);
            Directory.CreateDirectory(audioFolder);
            Directory.CreateDirectory(chunksRoot);
            Directory.CreateDirectory(analysisFolder);

            EnsureHidden(workFolder);
            EnsureHidden(audioFolder);
            EnsureHidden(chunksRoot);
            EnsureHidden(analysisFolder);

            return new RecordingWorkspace
            {
                VisibleRootFolder = visibleRoot,
                WorkFolder = workFolder,
                AudioFolder = audioFolder,
                ChunksRootFolder = chunksRoot,
                AnalysisFolder = analysisFolder
            };
        }

        public RecordingWorkspace InferFromAudioPath(string audioFilePath)
        {
            if (string.IsNullOrWhiteSpace(audioFilePath))
                throw new InvalidOperationException("audioFilePath vide.");

            string audioFolder = Path.GetDirectoryName(audioFilePath) ?? "";
            string workFolder = Directory.GetParent(audioFolder)?.FullName ?? "";
            string visibleRoot = Directory.GetParent(workFolder)?.FullName ?? "";

            string chunksRoot = Path.Combine(workFolder, "chunks");
            string analysisFolder = Path.Combine(workFolder, "analysis");

            return new RecordingWorkspace
            {
                VisibleRootFolder = visibleRoot,
                WorkFolder = workFolder,
                AudioFolder = audioFolder,
                ChunksRootFolder = chunksRoot,
                AnalysisFolder = analysisFolder
            };
        }

        public void EnsureHidden(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var attributes = File.GetAttributes(folderPath);
            if ((attributes & FileAttributes.Hidden) == 0)
            {
                File.SetAttributes(folderPath, attributes | FileAttributes.Hidden);
            }
        }
    }
}