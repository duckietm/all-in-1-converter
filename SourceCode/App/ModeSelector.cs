using System;
using System.Text;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Interactive welcome screen shown when the user launches the binary without
    /// an explicit --cli/--tui/--gui flag. Lets them pick the UI mode and explains
    /// what each one is good for.
    /// </summary>
    internal static class ModeSelector
    {
        public static RunMode Prompt(RunMode defaultMode)
        {
            Console.ResetColor();
            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;

            int w = SafeWidth();

            // Cyan banner
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(" ALL-IN-1 CONVERTER  --  HABBO ASSET WORKSTATION ".PadRight(w));
            Console.WriteLine(new string('=', w));
            Console.ResetColor();

            // Green body with the three options
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("  Pick how you want to drive the tool today:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    [1] TUI  - Mainframe terminal UI with MOUSE support  (recommended)");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("              Click buttons, navigate sub-menus, watch output");
            Console.WriteLine("              render live in a 3270-styled green window.");
            Console.WriteLine();
            Console.WriteLine("    [2] CLI  - Classic console menu (keyboard only)");
            Console.WriteLine("              Same mainframe look, but you type a number and press");
            Console.WriteLine("              Enter. Useful when piped, over SSH without mouse,");
            Console.WriteLine("              or in restricted terminals.");
            Console.WriteLine();
            Console.WriteLine("    [Q] QUIT");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Tip: TUI is the most polished experience. {Default(defaultMode)} is the default.");
            Console.WriteLine("  Note: GUI mode is for double-click launch only; from a terminal stick");
            Console.WriteLine("        with TUI or CLI. You can switch to GUI later via the menu's");
            Console.WriteLine("        SWITCH UI MODE entry.");
            Console.WriteLine();

            // Cyan prompt footer
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine(" Type 1, 2 or Q and press ENTER (empty = default) ".PadRight(w));
            Console.ResetColor();

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("CHOICE:> ");
            string raw = Console.ReadLine()?.Trim().ToLowerInvariant() ?? string.Empty;
            Console.ResetColor();

            return raw switch
            {
                "1" or "tui"  => RunMode.Tui,
                "2" or "cli"  => RunMode.Cli,
                "q" or "quit" or "exit" => RunMode.Quit,
                ""            => defaultMode == RunMode.Gui ? RunMode.Tui : defaultMode,
                _             => defaultMode == RunMode.Gui ? RunMode.Tui : defaultMode
            };
        }

        private static int SafeWidth()
        {
            try { return Math.Max(60, Math.Min(120, Console.WindowWidth)); }
            catch { return 100; }
        }

        private static string Default(RunMode m) => m switch
        {
            RunMode.Cli => "CLI",
            RunMode.Gui => "GUI",
            _           => "TUI"
        };
    }
}
