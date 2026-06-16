using System.Collections.Generic;

namespace Recordaudio.Models
{
    public sealed class ChunkPreparationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string ChunkFolderPath { get; set; } = "";
        public string ChunkManifestPath { get; set; } = "";
        public List<AudioChunkItem> Chunks { get; set; } = new();
        public string PreviewText { get; set; } = "";
    }
}