namespace Recordaudio.Models
{
    public sealed class RecordingSessionOptions
    {
        public string ClientName { get; set; } = "client_inconnu";
        public bool IncludeMic { get; set; }
        public int? MicrophoneDeviceNumber { get; set; }
    }
}