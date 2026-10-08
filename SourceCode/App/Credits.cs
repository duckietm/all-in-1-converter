using System;
using System.Threading.Tasks;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Credits screen, written with Console.WriteLine so the CLI and the
    /// Professional log both show it.
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
            Console.WriteLine("    Life           -  .NET 11 upgrade, strict JSON-only migration,");
            Console.WriteLine("                      JSON5 split-layout removal and Professional");
            Console.WriteLine("                      Avalonia MVVM dashboard.");
            Console.WriteLine();
            Console.WriteLine("    medievalshell  -  .NET 10 modernization, cross-platform refactor,");
            Console.WriteLine("                      ImageSharp migration, original TUI/CLI/GUI shells,");
            Console.WriteLine("                      JSON5 split-mode layer and Avalonia desktop GUI");
            Console.WriteLine("                      (Mainframe + Matrix themes).");
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
            Console.WriteLine("    .NET 11 Preview |  Newtonsoft.Json 13.0.4  |  MySql.Data 9.7");
            Console.WriteLine("    SixLabors.ImageSharp 3.1.12  (cross-platform sprite sheet generation)");
            Console.WriteLine("    Avalonia 12                  (Professional desktop window)");
            Console.WriteLine("    SharpZipLib 1.4.2            (nitro bundle compression)");
            Console.WriteLine("    JPEXS Free Flash Decompiler  (Tools/ffdec - SWF extraction)");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
