using Habbo_Downloader.Tools;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Habbo_Downloader.Compiler
{
    public static class SwfToNitroConverter
    {
        private const string ImportDirectory = @"SWFCompiler\import";
        private const string OutputDirectory = @"SWFCompiler\output";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                // Ensure the output directory exists
                Directory.CreateDirectory(OutputDirectory);

                // Get all SWF files in the import directory
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"Found {swfFiles.Length} SWF files. Starting conversion...");

                foreach (string swfFile in swfFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(swfFile);
                    string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);

                    Directory.CreateDirectory(fileOutputDirectory);

                    string binaryOutputPath = Path.Combine(fileOutputDirectory, fileName + "_binaryData");
                    string imageOutputPath = Path.Combine(fileOutputDirectory, fileName + "_images");

                    Console.WriteLine($"Decompiling SWF: {fileName}...");
                    await FfdecExtractor.ExtractBinaryDataAsync(swfFile, binaryOutputPath);
                    await FfdecExtractor.ExtractImageAsync(swfFile, imageOutputPath);

                    string[] indexFiles = Directory.GetFiles(binaryOutputPath, "*_index.bin", SearchOption.TopDirectoryOnly);

                    if (indexFiles.Length == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"No *_index.bin file found in {binaryOutputPath}. Skipping this SWF file.");
                        Console.ResetColor();
                        continue;
                    }

                    string indexFilePath = indexFiles[0];
                    Console.WriteLine($"Using index file: {indexFilePath}");

                    var indexData = await IndexMapper.ParseIndexFileAsync(indexFilePath);
                    if (indexData == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to parse {indexFilePath}. Skipping...");
                        Console.ResetColor();
                        continue;
                    }

                    // Generate {name}.json
                    string jsonOutputPath = Path.Combine(fileOutputDirectory, fileName + ".json");
                    string jsonContent = IndexMapper.GenerateJson(indexData);
                    await File.WriteAllTextAsync(jsonOutputPath, jsonContent);

                    Console.WriteLine($"Generated {fileName}.json -> {jsonOutputPath}");
                }

                Console.WriteLine("All SWF files have been converted successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during SWF conversion: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
