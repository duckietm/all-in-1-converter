using Avalonia.Media;

namespace Habbo_Downloader.App.Gui
{
    public enum GuiThemeKind
    {
        Mainframe, // IBM 3270 / CICS look: cyan header, green-on-black body, red BACK
        Matrix     // Pure phosphor green-on-black, no cyan, no red - minimal Matrix vibe
    }

    /// <summary>
    /// Resolves brushes / fonts for the two GUI themes the operator can pick:
    /// Mainframe (IBM 3270 / CICS) or Matrix (phosphor green only).
    /// </summary>
    public sealed class GuiTheme
    {
        public IBrush Background    { get; private init; } = Brushes.Black;
        public IBrush Foreground    { get; private init; } = Brushes.LightGreen;
        public IBrush HeaderBg      { get; private init; } = Brushes.DarkCyan;
        public IBrush HeaderFg      { get; private init; } = Brushes.Black;
        public IBrush FrameBorder   { get; private init; } = Brushes.LimeGreen;
        public IBrush ButtonBg      { get; private init; } = Brushes.Black;
        public IBrush ButtonFg      { get; private init; } = Brushes.LimeGreen;
        public IBrush ButtonHover   { get; private init; } = Brushes.Black;
        public IBrush ButtonHoverFg { get; private init; } = Brushes.Yellow;
        public IBrush ExitBg        { get; private init; } = Brushes.Black;
        public IBrush ExitFg        { get; private init; } = Brushes.Red;
        public IBrush FooterBg      { get; private init; } = Brushes.DarkCyan;
        public IBrush FooterFg      { get; private init; } = Brushes.Black;
        public IBrush InputBg       { get; private init; } = Brushes.Black;
        public IBrush InputFg       { get; private init; } = Brushes.LimeGreen;
        public IBrush InputCaret    { get; private init; } = Brushes.LimeGreen;
        public string FontFamily    { get; private init; } = "Cascadia Mono,Consolas,Courier New,monospace";
        public string Kind          { get; private init; } = "MAINFRAME";

        public static GuiTheme Mainframe() => new()
        {
            Background    = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            Foreground    = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)), // bright phosphor green
            HeaderBg      = new SolidColorBrush(Color.FromRgb(0x00, 0xCE, 0xD1)), // cyan turquoise
            HeaderFg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            FrameBorder   = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
            ButtonBg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            ButtonFg      = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
            ButtonHover   = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
            ButtonHoverFg = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            ExitBg        = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            ExitFg        = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)),
            FooterBg      = new SolidColorBrush(Color.FromRgb(0x00, 0xCE, 0xD1)),
            FooterFg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            InputBg       = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            InputFg       = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
            InputCaret    = new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)),
            FontFamily    = "Cascadia Mono,Consolas,Courier New,monospace",
            Kind          = "MAINFRAME"
        };

        public static GuiTheme Matrix() => new()
        {
            Background    = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            Foreground    = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)), // iconic Matrix green
            HeaderBg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            HeaderFg      = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)),
            FrameBorder   = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x27)), // dimmer green border
            ButtonBg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            ButtonFg      = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)),
            ButtonHover   = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0x1A)),
            ButtonHoverFg = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)),
            ExitBg        = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            ExitFg        = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x27)), // dimmer for back
            FooterBg      = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            FooterFg      = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x27)),
            InputBg       = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            InputFg       = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)),
            InputCaret    = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41)),
            FontFamily    = "Cascadia Mono,Consolas,Courier New,monospace",
            Kind          = "MATRIX"
        };
    }
}
