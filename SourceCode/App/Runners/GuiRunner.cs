using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ConsoleApplication;
using Habbo_Downloader.App.Gui;
using Habbo_Downloader.App.Menus;
using MenuItem = Habbo_Downloader.App.Menus.MenuItem;

namespace Habbo_Downloader.App.Runners
{
    /// <summary>
    /// Desktop window runner (Avalonia 11). Must execute on the main STA thread,
    /// so this runner blocks the calling thread while Avalonia's main loop is up.
    /// The menu loop is scheduled on the UI dispatcher right after framework init;
    /// when it completes (user exited or switched mode) we shut down the lifetime
    /// and return control to App.RunAsync.
    /// </summary>
    public sealed class GuiRunner : IAppRunner
    {
        public Task RunAsync(Args args)
        {
            var theme = PromptTheme();
            GuiMenuPresenter.ActiveTheme = theme;

            BuildAvaloniaApp()
                .AfterSetup(_ =>
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            await MenuHost.ShowAsync("Main Menu: Select a Topic", new MenuItem[]
                            {
                                new("1",       "Habbo Original Downloads",            HabboOriginalMenu.DisplayMenu, IsSubMenu: true),
                                new("2",       "Nitro Custom Downloads",              NitroCustomMenu.DisplayMenu,   IsSubMenu: true),
                                new("3",       "Hotel Tools",                         HotelToolsMenu.DisplayMenu,    IsSubMenu: true),
                                new("4",       "Database Tools",                      DatabaseMenu.DisplayMenu,      IsSubMenu: true),
                                new("version", "Fetch current Habbo client version",  CliRunner.DisplayVersionAsync),
                                new("credits", "Credits & contributors",               Habbo_Downloader.App.Credits.ShowAsync),
                                Habbo_Downloader.App.UiSwitcher.ForCurrentMode(),
                            }, isTopLevel: true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[gui] menu loop crashed: {ex.Message}");
                        }
                        finally
                        {
                            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime life)
                                life.Shutdown();
                        }
                    });
                })
                .StartWithClassicDesktopLifetime(Array.Empty<string>(), Avalonia.Controls.ShutdownMode.OnExplicitShutdown);

            return Task.CompletedTask;
        }

        private static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<AvaloniaApp>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        public static GuiTheme PromptThemeFromConsole() => PromptTheme();

        private static GuiTheme PromptTheme()
        {
            Console.ResetColor();
            try { Console.Clear(); } catch { /* not a tty */ }
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            int w = 80;
            try { w = Math.Max(60, Math.Min(120, Console.WindowWidth)); } catch { }
            Console.WriteLine(" ALL-IN-1 CONVERTER  --  GUI THEME SELECTION ".PadRight(w));
            Console.WriteLine(new string('=', w));
            Console.ResetColor();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("  Pick a visual theme for the desktop window:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    [1] MAINFRAME  - IBM 3270 / CICS look:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("                    cyan header bar, phosphor green body,");
            Console.WriteLine("                    red BACK / EXIT buttons. Classic bank-terminal vibe.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    [2] MATRIX     - Pure phosphor green on black:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("                    no cyan, no red, minimal chrome.");
            Console.WriteLine("                    Just one shade of green falling down a black void.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Choice [1=MAINFRAME, 2=MATRIX, default=1]: ");
            Console.ForegroundColor = ConsoleColor.Green;
            var raw = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
            Console.ResetColor();
            return raw switch
            {
                "2" or "matrix"    => GuiTheme.Matrix(),
                _                  => GuiTheme.Mainframe()
            };
        }
    }
}
