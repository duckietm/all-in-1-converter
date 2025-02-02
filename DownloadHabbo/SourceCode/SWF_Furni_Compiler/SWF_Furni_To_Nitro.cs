using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWFCompiler.Mapper.Visualizations;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using static Habbo_Downloader.Compiler.SWF_Furni_To_Nitro;
using Habbo_Downloader.SWFCompiler.Mapper;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Furni_To_Nitro
    {
        private static string ImportDirectory;
        private const string OutputDirectory = @"SWFCompiler\nitro";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported furniture? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                ImportDirectory = string.IsNullOrEmpty(input) || input == "H"
                    ? @"hof_furni"
                    : input == "I" ? @"SWFCompiler\import\furniture" : @"hof_furni";

                Console.WriteLine($"DEBUG: Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"We have found {swfFiles.Length} SWF files.");
                int nitroFilesGenerated = 0;

                foreach (string swfFile in swfFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(swfFile);
                    string nitroFilePath = Path.Combine(OutputDirectory, $"{fileName}.nitro");

                    if (File.Exists(nitroFilePath)) continue;

                    string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
                    Directory.CreateDirectory(fileOutputDirectory);

                    string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

                    Console.WriteLine($"Decompiling SWF: {fileName}...");
                    await FfdecExtractor.ExtractSWFAsync(swfFile, binaryOutputPath);

                    if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: Extraction failed for {fileName}. No binaryData folder found.");
                        Console.ResetColor();
                        continue;
                    }

                    Console.WriteLine("✅ Extraction completed successfully.");

                    string debugXmlPath = Path.Combine(binaryOutputPath, "debug.xml");
                    var imageSources = DebugXmlParser.ParseDebugXml(debugXmlPath);

                    IndexData indexData = null;
                    AssetLogicData logicData = null;
                    List<Visualization> visualizations = null;
                    SpriteBundle spriteBundle = null;
                    Dictionary<string, AssetsMapper.Asset> assetData = null;

                    string[] indexFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_index.bin", SearchOption.TopDirectoryOnly);
                    if (indexFiles.Length > 0)
                    {
                        string indexFilePath = indexFiles[0];
                        indexData = await IndexMapper.ParseIndexFileAsync(indexFilePath);
                    }

                    if (indexData == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to parse index file for {fileName}. Skipping...");
                        Console.ResetColor();
                        continue;
                    }

                    string[] assetsFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_assets.bin", SearchOption.TopDirectoryOnly);
                    string[] manifestFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_manifest.bin", SearchOption.TopDirectoryOnly);

                    if (assetsFiles.Length > 0 && manifestFiles.Length > 0)
                    {
                        string assetsFilePath = assetsFiles[0];
                        string manifestFilePath = manifestFiles[0];

                        assetData = await AssetsMapper.ParseAssetsFileAsync(
                            assetsFilePath,
                            imageSources,
                            manifestFilePath,
                            debugXmlPath,
                            fileOutputDirectory
                        );
                    }

                    // Generate the two CSV files inside the SWF extraction directory
                    await AssetsMapper.WriteAssetAndImageMappingsAsync(assetData, debugXmlPath, fileOutputDirectory);

                    string jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");
                    string jsonContent = JsonSerializer.Serialize(new
                    {
                        name = indexData?.Name ?? "unknown",
                        logicType = indexData?.LogicType ?? "default",
                        visualizationType = indexData?.VisualizationType ?? "default",
                        assets = assetData ?? new Dictionary<string, AssetsMapper.Asset>(),
                        logic = logicData ?? new AssetLogicData(),
                        visualizations = visualizations ?? new List<Visualization>(),
                        spritesheet = spriteBundle?.Spritesheet ?? new SpriteSheetData()
                    }, new JsonSerializerOptions { WriteIndented = true });

                    await File.WriteAllTextAsync(jsonOutputPath, jsonContent);
                    await BundleNitroFileAsync(fileOutputDirectory, fileName, OutputDirectory);

                    nitroFilesGenerated++;
                }

                Console.WriteLine($"All SWF files have been converted. {nitroFilesGenerated} nitro files were generated.");
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

        private static async Task BundleNitroFileAsync(string outputDirectory, string fileName, string nitroOutputDirectory)
        {
            var nitroBundler = new NitroBundler();

            string jsonFilePath = Path.Combine(outputDirectory, $"{fileName}.json");
            byte[] jsonData = await File.ReadAllBytesAsync(jsonFilePath);
            nitroBundler.AddFile($"{fileName}.json", jsonData);

            string imageFilePath = Path.Combine(outputDirectory, $"{fileName}.png");
            if (File.Exists(imageFilePath))
            {
                byte[] imageData = await File.ReadAllBytesAsync(imageFilePath);
                nitroBundler.AddFile($"{fileName}.png", imageData);
            }
            else
            {
                Console.WriteLine("⚠️ Warning: No image file found to include in nitro file.");
            }

            byte[] nitroData = await nitroBundler.ToBufferAsync();
            string nitroFilePath = Path.Combine(nitroOutputDirectory, $"{fileName}.nitro");

            await File.WriteAllBytesAsync(nitroFilePath, nitroData);
            Console.WriteLine($"Generated {fileName}.nitro -> {nitroFilePath}");
        }

        public class SpriteBundle
        {
            public SpriteSheetData Spritesheet { get; set; }
            public ImageData ImageData { get; set; }
        }

        public class ImageData
        {
            public string Name { get; set; }
            public byte[] Buffer { get; set; }
        }
    }
}
