using System;

namespace Habbo_Downloader.App
{
    public enum RunMode { Cli, Tui, Gui, Quit }

    public sealed class Args
    {
        public RunMode Mode { get; set; } = RunMode.Tui;
        public bool ModeExplicitlySet { get; set; } // true when the user passed --cli/--tui/--gui
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public string? Command { get; set; } // optional: directly invoke a top-level menu (cli mode only)

        public static Args Parse(string[] argv)
        {
            var a = new Args();
            for (int i = 0; i < argv.Length; i++)
            {
                switch (argv[i].ToLowerInvariant())
                {
                    case "--gui":  a.Mode = RunMode.Gui; a.ModeExplicitlySet = true; break;
                    case "--tui":  a.Mode = RunMode.Tui; a.ModeExplicitlySet = true; break;
                    case "--cli":  a.Mode = RunMode.Cli; a.ModeExplicitlySet = true; break;
                    case "--help":
                    case "-h":     a.ShowHelp = true; break;
                    case "--version":
                    case "-v":     a.ShowVersion = true; break;
                    case "--command":
                    case "-c":
                        if (i + 1 < argv.Length) a.Command = argv[++i];
                        break;
                    default:
                        if (argv[i].StartsWith("--command=", StringComparison.OrdinalIgnoreCase))
                            a.Command = argv[i].Substring("--command=".Length);
                        else
                            Console.Error.WriteLine($"Unknown flag: {argv[i]}");
                        break;
                }
            }
            return a;
        }

        public static string HelpText =>
            "Habbo Downloader (All-in-1) - usage:\n" +
            "  habbo-downloader [--tui|--cli|--gui] [--command <name>]\n" +
            "\n" +
            "Modes:\n" +
            "  --tui   Mouse-driven mainframe TUI (DEFAULT; Windows + Linux)\n" +
            "  --cli   Plain console menu (legacy; for scripts / non-tty)\n" +
            "  --gui   Desktop window (Windows + Linux, Avalonia)\n" +
            "\n" +
            "Options:\n" +
            "  --command <name>   In CLI mode, jump straight to a top-level menu:\n" +
            "                     habbo | nitro | tools | database\n" +
            "  --version          Print fetched Habbo client version and exit\n" +
            "  --help             Show this help and exit\n";
    }
}
