using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// Console rendering of the menu, styled to mirror the mainframe TUI:
    /// cyan header bar, green double-line ASCII frame with the menu items on a
    /// black background, red [BACK] / [EXIT] button, cyan footer help line.
    /// No mouse, only keyboard - the operator types the item key (e.g. "1") or
    /// "back" / "exit".
    /// </summary>
    internal static class ConsoleMenuPresenter
    {
        public static async Task ShowAsync(string title, MenuItem[] items, bool isTopLevel = false)
        {
            var byKey = items.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);
            var exitWord = isTopLevel ? "exit" : "back";

            while (true)
            {
                Render(title, items, isTopLevel);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("COMMAND:> ");
                Console.OutputEncoding = Encoding.UTF8;

                var input = (Console.ReadLine() ?? string.Empty).Trim();
                Console.ResetColor();

                if (string.Equals(input, exitWord, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(isTopLevel ? "Exiting..." : "Returning to the previous menu...");
                    return;
                }

                if (!byKey.TryGetValue(input, out var picked))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown command: {input}");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    continue;
                }

                try
                {
                    Console.ResetColor();
                    Console.Clear();
                    PrintActionHeader(title, picked.Label);
                    await picked.Action();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error executing command: {ex.Message}");
                }
                finally
                {
                    Console.ResetColor();
                    Console.WriteLine();
                    if (!MenuHost.SwitchRequested)
                    {
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }

                // The "Switch UI mode" action sets MenuHost.SwitchRequested. Bail out
                // of every nested menu so App.RunAsync can restart the new runner.
                if (MenuHost.SwitchRequested) return;
            }
        }

        private static int FrameWidth()
        {
            int w;
            try { w = Console.WindowWidth; }
            catch { w = 100; }
            if (w < 60) w = 60;
            if (w > 120) w = 120;
            return w;
        }

        private static void Render(string title, MenuItem[] items, bool isTopLevel)
        {
            Console.ResetColor();
            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;

            int w = FrameWidth();

            // Cyan header bar
            string headerLeft = "ALL-IN-1 CONVERTER";
            string headerRight = $"DATE: {DateTime.UtcNow:yyyy-MM-dd}";
            string headerCenter = $"*** {title.ToUpperInvariant()} ***";
            int totalContent = headerLeft.Length + headerRight.Length + headerCenter.Length;
            int slack = Math.Max(2, w - totalContent - 2);
            int padLeft = slack / 2;
            int padRight = slack - padLeft;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($" {headerLeft}{new string(' ', padLeft)}{headerCenter}{new string(' ', padRight)}{headerRight} ".PadRight(w));
            Console.WriteLine(new string('=', w));
            Console.ResetColor();

            // Green double-line frame on black background
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("╔" + new string('═', w - 2) + "╗");
            string frameTitle = " " + title.ToUpperInvariant() + " ";
            int titleSpace = w - 2 - frameTitle.Length;
            int titleLeft = titleSpace / 2;
            int titleRight = titleSpace - titleLeft;
            Console.WriteLine("║" + new string(' ', titleLeft) + frameTitle + new string(' ', titleRight) + "║");
            Console.WriteLine("╠" + new string('═', w - 2) + "╣");
            Console.WriteLine("║" + new string(' ', w - 2) + "║");

            foreach (var item in items)
            {
                if (string.Equals(item.Key, "back", StringComparison.OrdinalIgnoreCase)) continue;
                var inner = $"  [{item.Key}] {item.Label.ToUpperInvariant()}";
                if (inner.Length > w - 2) inner = inner.Substring(0, w - 5) + "...";
                Console.WriteLine("║" + inner.PadRight(w - 2) + "║");
            }

            Console.WriteLine("║" + new string(' ', w - 2) + "║");

            // Red exit/back button
            Console.ForegroundColor = ConsoleColor.Red;
            var exitLine = isTopLevel
                ? "  [EXIT] QUIT APPLICATION"
                : "  [BACK] RETURN TO PREVIOUS MENU";
            Console.WriteLine("║" + exitLine.PadRight(w - 2) + "║");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("╚" + new string('═', w - 2) + "╝");

            // Cyan footer help line
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            var help = isTopLevel
                ? " Type a number to select, or \"exit\" to quit "
                : " Type a number to select, or \"back\" to return ";
            Console.WriteLine(help.PadRight(w));
            Console.ResetColor();
        }

        private static void PrintActionHeader(string menuTitle, string actionLabel)
        {
            int w = FrameWidth();
            string headerLeft = "ALL-IN-1 CONVERTER";
            string headerCenter = $"{menuTitle.ToUpperInvariant()} -> {actionLabel.ToUpperInvariant()}";
            string headerRight = $"DATE: {DateTime.UtcNow:yyyy-MM-dd}";
            int slack = Math.Max(2, w - headerLeft.Length - headerCenter.Length - headerRight.Length - 2);
            int padLeft = slack / 2;
            int padRight = slack - padLeft;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($" {headerLeft}{new string(' ', padLeft)}{headerCenter}{new string(' ', padRight)}{headerRight} ".PadRight(w));
            Console.WriteLine(new string('=', w));
            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
        }
    }
}
