using System;
using System.Threading.Tasks;
using Habbo_Downloader.App.Runners;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Helper exposed for the CLI / TUI branch of Program.Main. The outer loop
    /// and bootstrap (Java check, mkdirs, mode decision) live in Program.cs because
    /// Avalonia needs a virgin main STA thread for GUI mode and cannot follow
    /// .GetAwaiter().GetResult() on async paths.
    /// </summary>
    public static class App
    {
        public static Task RunSelectedRunnerAsync(Args args)
        {
            IAppRunner runner = args.Mode switch
            {
                RunMode.Tui => new TuiRunner(),
                _           => new CliRunner()
            };
            return runner.RunAsync(args);
        }
    }
}
