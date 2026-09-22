using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Habbo_Downloader.App;
using Habbo_Downloader.App.Gui;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Professional.Views;
using Habbo_Downloader.App.Runners;
using Habbo_Downloader.App.Workspaces;
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
            AssetWorkspaceRuntime.Load(Path.Combine(Environment.CurrentDirectory, "config.ini"));

            var args = Args.Parse(argv);

            if (args.ShowHelp)    { Console.WriteLine(Habbo_Downloader.App.Args.HelpText); return 0; }
            if (args.ShowVersion) { CliRunner.DisplayVersionAsync().GetAwaiter().GetResult(); return 0; }

            // Java is no longer required to run: the native SWF parser handles both
            // the .nitro converters and the SQL generator. It is only needed for the
            // FFDEC fallback on a SWF the native parser cannot read, so a missing
            // Java is a warning, not a reason to refuse to start.
            if (!Bootstrap.IsJavaAvailable())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Java was not found on the command line.");
                Console.WriteLine("Converting and generating SQL still work - they use the built-in SWF reader.");
                Console.WriteLine("Only the FFDEC fallback for unusual SWF files is unavailable; install Java");
                Console.WriteLine("from https://www.java.com/en/download/ if a file ever needs it.");
                Console.ResetColor();
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
            bool showDesktopSelector = false;
            if (!args.ModeExplicitlySet && string.IsNullOrEmpty(args.Command))
            {
                if (!LaunchContext.IsFromTerminal)
                {
                    args.Mode = RunMode.Professional;
                    showDesktopSelector = true;
                }
                else
                {
                    args.Mode = ModeSelector.Prompt(RunMode.Professional);
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

                if (args.Mode is RunMode.Gui or RunMode.Professional)
                {
                    RunDesktop(args.Mode, showDesktopSelector);
                }
                else
                {
                    Habbo_Downloader.App.App.RunSelectedRunnerAsync(args).GetAwaiter().GetResult();
                }

                if (!MenuHost.SwitchRequested) break;
                args.Mode = MenuHost.NextMode;
                showDesktopSelector = false;
            }
            return Environment.ExitCode;
        }

        private static void RunDesktop(RunMode initialMode, bool showSelector)
        {
            GuiTheme guiTheme = GuiMenuPresenter.ConsumeQueuedTheme()
                ?? (initialMode == RunMode.Gui && !showSelector
                    ? GuiRunner.PromptThemeFromConsole()
                    : GuiTheme.Mainframe());
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
                            RunMode selectedMode = initialMode;
                            if (showSelector)
                            {
                                var selector = new InterfaceSelectorWindow();
                                selector.Show();
                                selectedMode = await selector.ResultTask;
                            }

                            MenuHost.Mode = selectedMode;
                            if (selectedMode == RunMode.Professional)
                            {
                                var window = new ProfessionalWindow();
                                window.Show();
                                await window.ClosedTask;
                            }
                            else if (selectedMode == RunMode.Gui)
                            {
                                GuiMenuPresenter.ActiveTheme = GuiMenuPresenter.ConsumeQueuedTheme() ?? guiTheme;
                                await MenuHost.ShowAsync("Main Menu: Select a Topic", MainMenuFactory.Build(), isTopLevel: true);
                            }
                            else if (selectedMode is RunMode.Cli or RunMode.Tui)
                            {
                                MenuHost.RequestSwitch(selectedMode);
                            }
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
