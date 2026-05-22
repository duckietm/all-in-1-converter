using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Shared bootstrap concerns: dependency checks (Java), runtime directory layout.
    /// Independent of UI mode.
    /// </summary>
    public static class Bootstrap
    {
        public static bool IsJavaAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void CreateDirectories()
        {
            var structure = new Dictionary<string, string[]>
            {
                { Path.Combine("NitroCompiler", "compile"),   new[] { "furni", "clothing", "effects", "pets" } },
                { Path.Combine("NitroCompiler", "compiled"),  new[] { "furni", "clothing", "effects", "pets" } },
                { Path.Combine("NitroCompiler", "extract"),   new[] { "furni", "clothing", "effects", "pets" } },
                { Path.Combine("NitroCompiler", "extracted"), new[] { "furni", "clothing", "effects", "pets" } },
                { "Habbo_Default", new[]
                {
                    "badges", "clothes",
                    Path.Combine("files", "txt"),
                    Path.Combine("files", "xml"),
                    Path.Combine("files", "json"),
                    "hof_furni",
                    Path.Combine("hof_furni", "icons"),
                    "icons", "mp3", "quests",
                    "reception",
                    Path.Combine("reception", "web_promo_small")
                } },
                { "Merge", new[]
                {
                    "Original_Furnidata", "Import_Furnidata", "Merged_Furnidata",
                    "Original_ClothesData", "Import_ClothesData", "Merged_ClothesData",
                    "Original_Productdata", "Import_Productdata", "Merged_Productdata"
                } },
                { "Generate", new[] { "Furnidata", "Furniture", "Output_SQL" } },
                { "SWFCompiler", new[]
                {
                    "clothes", "furniture", "import",
                    Path.Combine("import", "clothes"),
                    Path.Combine("import", "furniture"),
                    Path.Combine("import", "pets"),
                    Path.Combine("import", "effects"),
                    Path.Combine("import", "effects", "CustomXML")
                } },
                { "Database", new[] { "Variables" } },
                { "custom_downloads", new[] { "clothes", "nitro_furniture", Path.Combine("nitro_furniture", "icons") } }
            };

            foreach (var (baseDir, subDirs) in structure)
            {
                Directory.CreateDirectory(baseDir);
                foreach (var sub in subDirs)
                    Directory.CreateDirectory(Path.Combine(baseDir, sub));
            }
        }
    }
}
