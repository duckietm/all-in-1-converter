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
            Console.WriteLine("Starting Nitro Furniture Compilation...");

            string compileFolder = Path.Combine("Compiler", "compile", "furni");
            string outputFolder = Path.Combine("Compiler", "compiled", "furni");

            if (!Directory.Exists(compileFolder))
            {
                Console.WriteLine($"Compile folder not found: {compileFolder}");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Console.WriteLine($"Creating output directory: {outputFolder}");
                Directory.CreateDirectory(outputFolder);
            }

            string[] furnitureItems = Directory.GetDirectories(compileFolder);

            if (furnitureItems.Length == 0)
            {
                Console.WriteLine("No furniture items found to compile.");
                return;
            }

            Console.WriteLine($"Compiling {furnitureItems.Length} furniture items...");

            foreach (var itemFolder in furnitureItems)
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

                    Console.WriteLine($"Compiled: {Path.GetFileName(itemFolder)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error compiling {Path.GetFileName(itemFolder)}: {ex.Message}");
                }
            }

            Console.WriteLine("Nitro Furniture Compilation completed.");
        }
    }
}