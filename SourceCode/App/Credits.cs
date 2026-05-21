using System;
using System.Threading.Tasks;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Credits screen. Reused by every runner (CLI / TUI / GUI) via Console.WriteLine
    /// so it is captured into the output windows of the TUI/GUI presenters and shows
    /// in the green theme of the rest of the workstation.
    /// </summary>
    public static class Credits
    {
        public static Task ShowAsync()
        {
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("                            ALL-IN-1 CONVERTER  -  CREDITS");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("  AUTHORS");
            Console.WriteLine("  -------");
            Console.WriteLine("    medievalshell  -  .NET 10 modernization, cross-platform refactor,");
            Console.WriteLine("                      ImageSharp migration, dual TUI/CLI/GUI mainframe");
            Console.WriteLine("                      shells, JSON + JSON5 split-mode IO layer,");
            Console.WriteLine("                      Avalonia desktop GUI (Mainframe + Matrix themes).");
            Console.WriteLine();
            Console.WriteLine("    duckietm       -  Original all-in-1 downloader / SWF -> Nitro / SQL");
            Console.WriteLine("                      generator / database tools. Upstream maintainer.");
            Console.WriteLine();
            Console.WriteLine("  CONTRIBUTORS");
            Console.WriteLine("  ------------");
            Console.WriteLine("    Nitro Team Discord  -  Pet converter base   (discord.gg/yCXcMqrT)");
            Console.WriteLine("    AtlasOmega          -  Among Us effects (Enable 880-903)");
            Console.WriteLine("    Leet                -  Enables 500-688");
            Console.WriteLine();
            Console.WriteLine("  STACK");
            Console.WriteLine("  -----");
            Console.WriteLine("    .NET 10 LTS     |  Newtonsoft.Json 13.0.4  |  MySql.Data 9.7");
            Console.WriteLine("    SixLabors.ImageSharp 3.1.12  (cross-platform sprite sheet generation)");
            Console.WriteLine("    Terminal.Gui 1.19            (mouse-driven TUI with 3270 theme)");
            Console.WriteLine("    Avalonia 11.3                (desktop GUI; Mainframe + Matrix themes)");
            Console.WriteLine("    SharpZipLib 1.4.2            (nitro bundle compression)");
            Console.WriteLine("    JPEXS Free Flash Decompiler  (Tools/ffdec - SWF extraction)");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
