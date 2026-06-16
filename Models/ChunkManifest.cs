using System;
using System.Collections.Generic;

namespace Recordaudio.Models
{
    public sealed class ChunkManifest
    {
        public string RecordingId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string SourceAudio { get; set; } = "";
        public double TotalDurationSeconds { get; set; }
        public DateTime CreatedAt { get; set; }
        public double OverlapSeconds { get; set; }
        public List<AudioChunkItem> Chunks { get; set; } = new();
    }
}