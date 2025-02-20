using ConsoleApplication;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Habbo_Downloader.Compiler
{
    public static class NitroFurniCompile
    {
        public static async Task Compile()
        {
            Console.WriteLine("Starting Nitro Assets Compilation...");

            // Define compile and output folders for different asset types
            string baseCompilePath = Path.Combine("NitroCompiler", "compile");
            string baseOutputPath = Path.Combine("NitroCompiler", "compiled");

            // Asset categories to handle
            string[] assetTypes = { "furni", "clothing", "effects", "pets" };

            foreach (string assetType in assetTypes)
            {
                string compileFolder = Path.Combine(baseCompilePath, assetType);
                string outputFolder = Path.Combine(baseOutputPath, assetType);

                Console.WriteLine($"Processing {assetType} assets...");

                if (!Directory.Exists(compileFolder))
                {
                    Console.WriteLine($"Compile folder not found: {compileFolder}");
                    continue;
                }

                if (!Directory.Exists(outputFolder))
                {
                    Console.WriteLine($"Creating output directory: {outputFolder}");
                    Directory.CreateDirectory(outputFolder);
                }

                string[] assetItems = Directory.GetDirectories(compileFolder);

                if (assetItems.Length == 0)
                {
                    Console.WriteLine($"No {assetType} items found to compile.");
                    continue;
                }

                Console.WriteLine($"Compiling {assetItems.Length} {assetType} items...");

                foreach (var itemFolder in assetItems)
                {
                    try
                    {
                        var nitroBundler = new NitroBundler();
                        string[] assets = Directory.GetFiles(itemFolder);

                        foreach (var asset in assets)
                        {
                            byte[] data = await File.ReadAllBytesAsync(asset);
                            nitroBundler.AddFile(Path.GetFileName(asset), data);
                        }

                        byte[] compiledData = await nitroBundler.ToBufferAsync();

                        string outputPath = Path.Combine(outputFolder, $"{Path.GetFileName(itemFolder)}.nitro");
                        await File.WriteAllBytesAsync(outputPath, compiledData);

                        Console.WriteLine($"Compiled: {Path.GetFileName(itemFolder)} ({assetType})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error compiling {Path.GetFileName(itemFolder)} ({assetType}): {ex.Message}");
                    }
                }
            }

            Console.WriteLine("Nitro Assets Compilation completed.");
        }
    }
}
