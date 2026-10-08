using System;
using System.Text;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Interactive welcome screen shown when the user launches the binary without
    /// an explicit --professional/--cli flag. Lets them pick the UI mode and explains
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

            // Green body with the interface options
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("  Pick how you want to drive the tool today:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    [1] PROFESSIONAL - Native MVVM dashboard  (recommended)");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("              Modern cards, native operation pages, live logs and system theme.");
            Console.WriteLine();
            Console.WriteLine("    [2] CLI  - Classic keyboard-only console menu");
            Console.WriteLine();
            Console.WriteLine("    [Q] QUIT");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Tip: Professional is the recommended experience. {Default(defaultMode)} is the default.");
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
                "1" or "professional" or "pro" => RunMode.Professional,
                "2" or "cli" => RunMode.Cli,
                "q" or "quit" or "exit" => RunMode.Quit,
                ""            => defaultMode,
                _             => defaultMode
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
            _           => "PROFESSIONAL"
        };
    }
}
