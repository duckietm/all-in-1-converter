using System;
using System.Threading.Tasks;

namespace Habbo_Downloader.App.Runners
{
    /// <summary>
    /// Desktop window runner (Avalonia 11, COBOL/IBM mainframe theme).
    /// Placeholder — implemented in Phase 5.
    /// </summary>
    public sealed class GuiRunner : IAppRunner
    {
        public Task RunAsync(Args args)
        {
            Console.Error.WriteLine("[--gui] Avalonia mode not yet wired (Phase 5). Falling back to --cli for now.");
            return new CliRunner().RunAsync(new Args { Mode = RunMode.Cli, Command = args.Command });
        }
    }
}
