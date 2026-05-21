using Terminal.Gui;

namespace Habbo_Downloader.App.Runners
{
    /// <summary>
    /// IBM 3270 / COBOL mainframe color scheme. Phosphor-green text on black,
    /// red highlights for protected/error fields, cyan for status/labels.
    /// </summary>
    internal static class TuiTheme
    {
        public static ColorScheme Base() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black),
            Focus      = new Terminal.Gui.Attribute(Color.Black, Color.BrightGreen),
            HotNormal  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            HotFocus   = new Terminal.Gui.Attribute(Color.BrightYellow, Color.BrightGreen),
            Disabled   = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
        };

        public static ColorScheme Header() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
            Focus      = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
            HotNormal  = new Terminal.Gui.Attribute(Color.Red, Color.BrightCyan),
            HotFocus   = new Terminal.Gui.Attribute(Color.Red, Color.BrightCyan),
            Disabled   = new Terminal.Gui.Attribute(Color.DarkGray, Color.BrightCyan)
        };

        public static ColorScheme StatusBar() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
            Focus      = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
            HotNormal  = new Terminal.Gui.Attribute(Color.Red, Color.BrightCyan),
            HotFocus   = new Terminal.Gui.Attribute(Color.Red, Color.BrightCyan),
            Disabled   = new Terminal.Gui.Attribute(Color.DarkGray, Color.BrightCyan)
        };

        public static ColorScheme Button() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black),
            Focus      = new Terminal.Gui.Attribute(Color.Black, Color.BrightGreen),
            HotNormal  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            HotFocus   = new Terminal.Gui.Attribute(Color.Black, Color.BrightYellow),
            Disabled   = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
        };

        public static ColorScheme ErrorBox() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
            Focus      = new Terminal.Gui.Attribute(Color.Black, Color.BrightRed),
            HotNormal  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            HotFocus   = new Terminal.Gui.Attribute(Color.BrightYellow, Color.BrightRed),
            Disabled   = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
        };

        // Color scheme for the output log pane: green phosphor text on black,
        // never inverted even when the view is focused (otherwise the whole
        // pane would flood with the Focus background colour).
        public static ColorScheme LogPane() => new ColorScheme
        {
            Normal     = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black),
            Focus      = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black),
            HotNormal  = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            HotFocus   = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
            Disabled   = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
        };
    }
}
