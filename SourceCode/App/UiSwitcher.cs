using System;
using System.Threading.Tasks;
using Habbo_Downloader.App.Menus;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Builds the "Switch UI mode" MenuItem injected into every runner's main menu.
    /// Pressing it sets MenuHost.RequestSwitch(...) and returns; the runner exits its
    /// top-level menu and App.RunAsync restarts with the new mode.
    ///
    /// In a CLI/TUI session the switch options are TUI, CLI and GUI. In GUI mode the
    /// same set is offered (operator can drop back to a terminal-style UI if they want).
    /// </summary>
    public static class UiSwitcher
    {
        public static MenuItem ForCurrentMode()
        {
            var current = MenuHost.Mode;
            return new MenuItem(
                "s",
                $"Switch UI mode (current: {current.ToString().ToUpperInvariant()})",
                () => PromptAndRequestSwitchAsync(current),
                IsSubMenu: false,
                HowToUse:
                    "Closes the current UI and reopens the workstation in the mode you pick.\n" +
                    "Available targets: TUI (mouse-driven mainframe terminal),\n" +
                    "                  CLI (classic console, keyboard only),\n" +
                    "                  GUI (Avalonia desktop window, Mainframe or Matrix theme).\n" +
                    "Useful for instance when running over SSH (-> TUI) or for screenshots (-> GUI).");
        }

        private static Task PromptAndRequestSwitchAsync(RunMode current)
        {
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("                            SWITCH UI MODE");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine($"  Current mode: {current}");
            Console.WriteLine();
            Console.WriteLine("    [1] TUI  - Mouse-driven mainframe terminal UI (Terminal.Gui)");
            Console.WriteLine("    [2] CLI  - Classic console menu, keyboard only");
            Console.WriteLine("    [3] GUI  - Desktop window (Avalonia, Mainframe or Matrix theme)");
            Console.WriteLine("    [x] CANCEL - stay in the current UI");
            Console.WriteLine();
            Console.Write("Choice: ");
            var raw = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();

            RunMode? target = raw switch
            {
                "1" or "tui" => RunMode.Tui,
                "2" or "cli" => RunMode.Cli,
                "3" or "gui" => RunMode.Gui,
                _            => null
            };

            if (target == null || target == current)
            {
                Console.WriteLine("Cancelled - staying in the current UI.");
                return Task.CompletedTask;
            }

            MenuHost.RequestSwitch(target.Value);
            Console.WriteLine($"Switching to {target} ... the window/terminal will refresh.");
            return Task.CompletedTask;
        }
    }
}
