using System;
using System.IO;

namespace Recordaudio.Services
{
    public static class AppLogger
    {
        private static readonly object Sync = new();

        public static void Log(string message)
        {
            try
            {
                AppPaths.EnsureDirectories();

                string filePath = Path.Combine(
                    AppPaths.LogsDirectory,
                    $"app_{DateTime.Now:yyyyMMdd}.log"
                );

                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

                lock (Sync)
                {
                    File.AppendAllText(filePath, line);
                }
            }
            catch
            {
                // silence volontaire
            }
        }
    }
}