using System;
using System.Linq;
using System.Threading.Tasks;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// Static facade that routes a menu request to the presenter matching the
    /// current run mode (set by App.RunAsync). Sub-menus call this without
    /// caring whether they will be drawn in Console plain or Terminal.Gui mainframe style.
    ///
    /// Also auto-injects a "?" help item at the end of the list whenever at least
    /// one of the items carries a HowToUse description.
    /// </summary>
    public static class MenuHost
    {
        public static RunMode Mode { get; set; } = RunMode.Cli;

        /// <summary>
        /// Set by the "Switch UI mode" menu entry to ask App.RunAsync to shut down
        /// the current runner and start the one for <see cref="NextMode"/>.
        /// </summary>
        public static bool SwitchRequested { get; set; }
        public static RunMode NextMode { get; set; }

        public static void RequestSwitch(RunMode target)
        {
            NextMode = target;
            SwitchRequested = true;
        }

        public static Task ShowAsync(string title, MenuItem[] items, bool isTopLevel = false)
        {
            var enriched = WithHelp(title, items);
            return Mode switch
            {
                RunMode.Gui => Gui.GuiMenuPresenter.ShowAsync(title, enriched, isTopLevel),
                RunMode.Tui => TuiMenuPresenter.ShowAsync(title, enriched, isTopLevel),
                _           => ConsoleMenuPresenter.ShowAsync(title, enriched, isTopLevel)
            };
        }

        private static MenuItem[] WithHelp(string title, MenuItem[] items)
        {
            if (items.Length == 0 || !items.Any(i => !string.IsNullOrEmpty(i.HowToUse)))
                return items;
            if (items.Any(i => string.Equals(i.Key, "?", StringComparison.OrdinalIgnoreCase)))
                return items;

            var capturedTitle = title;
            var capturedItems = items;
            var helpItem = new MenuItem(
                "?",
                "How to use the entries of this menu",
                () => RenderHelpPage(capturedTitle, capturedItems));

            var result = new MenuItem[items.Length + 1];
            Array.Copy(items, result, items.Length);
            result[items.Length] = helpItem;
            return result;
        }

        private static Task RenderHelpPage(string title, MenuItem[] items)
        {
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine($"   HOW TO USE  -  {title.ToUpperInvariant()}");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.HowToUse)) continue;
                Console.WriteLine($"  [{item.Key}] {item.Label.ToUpperInvariant()}");
                foreach (var line in item.HowToUse.Split('\n'))
                    Console.WriteLine($"      {line.TrimEnd()}");
                Console.WriteLine();
            }
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
