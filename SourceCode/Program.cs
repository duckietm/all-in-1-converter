using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Habbo_Downloader.App;
using Habbo_Downloader.App.Gui;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Runners;
using HabboMenuItem = Habbo_Downloader.App.Menus.MenuItem;

namespace ConsoleApplication
{
    internal static class Program
    {
        /// <summary>
        /// STA + synchronous entry-point. Avalonia (used in GUI mode) refuses to
        /// initialise its dispatcher on a thread that has already pumped through
        /// .GetAwaiter().GetResult() of an async path, so we keep the main thread
        /// virgin here and only branch into the async runner for CLI/TUI modes.
        /// </summary>
        [STAThread]
        private static int Main(string[] argv)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            // Unpack FFDec etc. from the embedded zip on first launch.
            // No-op when the files are already on disk.
            EmbeddedToolsExtractor.EnsureExtracted();

            var args = Args.Parse(argv);

            if (args.ShowHelp)    { Console.WriteLine(Habbo_Downloader.App.Args.HelpText); return 0; }
            if (args.ShowVersion) { CliRunner.DisplayVersionAsync().GetAwaiter().GetResult(); return 0; }

            if (!Bootstrap.IsJavaAvailable())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Java is not installed or not accessible from the command line.");
                Console.WriteLine("Java is required for the SQL Generator to function properly.");
                Console.WriteLine("Please download and install the latest version of Java from:");
                Console.WriteLine("https://www.java.com/en/download/");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return 1;
            }

            try { Bootstrap.CreateDirectories(); }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating directories: {ex.Message}");
                Console.ResetColor();
            }

            // Decide the initial UI mode: Explorer launch -> GUI direct,
            // terminal -> ModeSelector with TUI/CLI only.
            if (!args.ModeExplicitlySet && string.IsNullOrEmpty(args.Command))
            {
                if (!LaunchContext.IsFromTerminal)
                {
                    args.Mode = RunMode.Gui;
                }
                else
                {
                    args.Mode = ModeSelector.Prompt(args.Mode == RunMode.Gui ? RunMode.Tui : args.Mode);
                    if (args.Mode == RunMode.Quit)
                    {
                        Console.WriteLine("Bye.");
                        return 0;
                    }
                }
            }

            // Outer loop. GUI mode runs on the virgin main STA thread; CLI / TUI
            // go through the async runner path.
            while (true)
            {
                MenuHost.SwitchRequested = false;
                MenuHost.Mode = args.Mode;

                if (args.Mode == RunMode.Gui)
                {
                    RunGui();
                }
                else
                {
                    Habbo_Downloader.App.App.RunSelectedRunnerAsync(args).GetAwaiter().GetResult();
                }

                if (!MenuHost.SwitchRequested) break;
                args.Mode = MenuHost.NextMode;
            }
            return Environment.ExitCode;
        }

        private static void RunGui()
        {
            GuiMenuPresenter.ActiveTheme = GuiRunner.PromptThemeFromConsole();

            AppBuilder.Configure<AvaloniaApp>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .AfterSetup(_ =>
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            await MenuHost.ShowAsync("Main Menu: Select a Topic", MainMenuFactory.Build(), isTopLevel: true);
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
        }
    }

    public static class UserAgentClass
    {
        public static string UserAgent { get; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
    }
}
