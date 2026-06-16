using System;
using System.IO;

namespace Recordaudio.Services
{
    public static class AppPaths
    {
        public static string AppBaseDirectory =>
            AppContext.BaseDirectory;

        public static string ToolsDirectory =>
            Path.Combine(AppBaseDirectory, "tools");

        public static string FfmpegDirectory =>
            Path.Combine(ToolsDirectory, "ffmpeg");

        public static string FfmpegExePath =>
            Path.Combine(FfmpegDirectory, "ffmpeg.exe");

        public static string UserDataRoot =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Recordaudio"
            );

        public static string RecordingsDirectory =>
            Path.Combine(UserDataRoot, "recordings");

        public static string LogsDirectory =>
            Path.Combine(UserDataRoot, "logs");

        public static string ChunksDirectory =>
            Path.Combine(UserDataRoot, "chunks");

        public static string CatalogDirectory =>
            Path.Combine(UserDataRoot, "catalog");

        public static string ConfigDirectory =>
            Path.Combine(UserDataRoot, "config");

        public static string CatalogFilePath =>
            Path.Combine(CatalogDirectory, "recordings_catalog.json");

        public static string OpenAiApiKeyFilePath =>
            Path.Combine(ConfigDirectory, "openai_api_key.txt");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(UserDataRoot);
            Directory.CreateDirectory(RecordingsDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(ChunksDirectory);
            Directory.CreateDirectory(CatalogDirectory);
            Directory.CreateDirectory(ConfigDirectory);
        }
    }
}