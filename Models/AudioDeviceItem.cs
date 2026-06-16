namespace Recordaudio.Models
{
    public sealed class AudioDeviceItem
    {
        public int DeviceNumber { get; set; }
        public string DisplayName { get; set; } = "";

        public override string ToString()
        {
            return DisplayName;
        }
    }
}