using System;
using System.Text;
using System.Threading.Tasks;
using Habbo_Downloader.App.Runners;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Entry-point orchestrator: parse args, run shared bootstrap, dispatch to the runner
    /// that matches the chosen mode.
    /// </summary>
    public static class App
    {
        public static async Task<int> RunAsync(string[] argv)
        {
            // Set UTF-8 on stdout once, before any output, so the unicode glyphs
            // (✅ 🔍 📦 ⚠️ ❌) used across the tool render correctly in CMD/PowerShell.
            try { Console.OutputEncoding = Encoding.UTF8; } catch { /* not a tty */ }

            var args = Args.Parse(argv);

            if (args.ShowHelp)
            {
                Console.WriteLine(Habbo_Downloader.App.Args.HelpText);
                return 0;
            }
            if (args.ShowVersion)
            {
                await CliRunner.DisplayVersionAsync();
                return 0;
            }

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

            try
            {
                Bootstrap.CreateDirectories();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating directories: {ex.Message}");
                Console.ResetColor();
            }

            Menus.MenuHost.Mode = args.Mode;

            IAppRunner runner = args.Mode switch
            {
                RunMode.Gui => new GuiRunner(),
                RunMode.Tui => new TuiRunner(),
                _           => new CliRunner()
            };

            await runner.RunAsync(args);
            return Environment.ExitCode;
        }
    }
}
