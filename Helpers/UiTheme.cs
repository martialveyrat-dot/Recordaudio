using System.Drawing;

namespace Recordaudio.Helpers
{
    public static class UiTheme
    {
        public static readonly Color AppBackground = Color.FromArgb(243, 246, 250);
        public static readonly Color CardBackground = Color.White;
        public static readonly Color Border = Color.FromArgb(218, 223, 230);

        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color SuccessDark = Color.FromArgb(21, 128, 61);
        public static readonly Color Warning = Color.FromArgb(217, 119, 6);
        public static readonly Color Error = Color.FromArgb(220, 38, 38);

        public static readonly Color MutedText = Color.FromArgb(100, 116, 139);
        public static readonly Color StrongText = Color.FromArgb(15, 23, 42);
        public static readonly Color SoftText = Color.FromArgb(51, 65, 85);

        public static readonly Font DefaultFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        public static readonly Font HeaderFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font PreviewFont = new Font("Segoe UI", 10.2F, FontStyle.Regular);
    }
}