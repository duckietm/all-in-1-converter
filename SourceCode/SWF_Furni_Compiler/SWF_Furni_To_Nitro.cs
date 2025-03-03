using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWFCompiler.Mapper.Visualizations;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Furni_To_Nitro
    {
        private static string ImportDirectory;
        private const string OutputDirectory = @"SWFCompiler\furniture";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported furniture? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                ImportDirectory = string.IsNullOrEmpty(input) || input == "H"
                    ? @"Habbo_Default\hof_furni"
                    : input == "I" ? @"SWFCompiler\import\furniture" : @"Habbo_Default\hof_furni";

                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} SWF files.");

                // Process multiple SWFs in parallel.
                var nitroFilesGenerated = new ConcurrentBag<int>();
                int maxParallelism = (int)(Environment.ProcessorCount * 0.8);
                if (maxParallelism < 1) maxParallelism = 1;

                await Parallel.ForEachAsync(swfFiles, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, async (swfFile, _) =>
                {
                    if (await ProcessSwfFileAsync(swfFile))
                    {
                        nitroFilesGenerated.Add(1);
                    }
                });

                Console.WriteLine($"✅ All SWF files have been converted. {nitroFilesGenerated.Count} nitro files were generated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during SWF conversion: {ex.Message}");
            }
        }

        private static async Task<bool> ProcessSwfFileAsync(string swfFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(swfFile);
            string nitroFilePath = Path.Combine(OutputDirectory, $"{fileName}.nitro");

            if (File.Exists(nitroFilePath)) return false; // Skip already converted files

            string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
            Directory.CreateDirectory(fileOutputDirectory);

            string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

            Console.WriteLine($"🔍 Start Decompiling SWF: {fileName}...");
            await FfdecExtractor.ExtractSWFAsync(swfFile, binaryOutputPath);

            if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Extraction failed for {fileName}. No binaryData folder found.");
                Console.ResetColor();
                return false;
            }

            // Build canonical mapping from CSV (using AssetNameMapper)
            string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
            var canonicalMapping = AssetNameMapper.BuildCanonicalMapping(csvPath);

            // Parse the CSV via DebugXmlParser (which now reads CSV)
            var imageSources = DebugXmlParser.ParseDebugXml(csvPath);

            // Process Index, Assets, Logic, and Visualizations in parallel.
            var indexTask = GetIndexDataAsync(binaryOutputPath);
            var assetsTask = GetAssetDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory);
            var logicTask = GetLogicDataAsync(binaryOutputPath);
            var visualizationTask = GetVisualizationsDataAsync(binaryOutputPath);

            await Task.WhenAll(indexTask, assetsTask, logicTask, visualizationTask);
            var indexData = indexTask.Result;
            var assetData = assetsTask.Result;
            var logicData = logicTask.Result;
            var visualizations = visualizationTask.Result;

            if (indexData == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Failed to parse index file for {fileName}. Skipping...");
                Console.ResetColor();
                return false;
            }

            // Image Processing
            string imagesDirectory = Path.Combine(binaryOutputPath, "images");
            string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

            await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, AssetsMapper.LatestImageMapping);

            var images = LoadImages(imagesDirectory);
            if (images.Count == 0)
            {
                Console.WriteLine($"⚠️ No valid images found for {fileName}. Skipping sprite sheet generation.");
                return false;
            }

            try
            {
                // Pass the canonicalMapping to GenerateSpriteSheet.
                var (spriteSheetPath, spriteSheetData) = SpriteSheetMapper.GenerateSpriteSheet(
                    images,
                    fileOutputDirectory,
                    fileName,
                    canonicalMapping,        // canonical mapping argument
                    disableCleanKey: false,  // set as desired
                    numRows: 10,
                    maxWidth: 7500,
                    maxHeight: 12500
                );

                if (spriteSheetPath == null || spriteSheetData == null)
                {
                    Console.WriteLine($"⚠️ No images found to generate spritesheet for {fileName}. Skipping...");
                    return false;
                }

                var jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                    Converters = { new FloatToFixedDecimalConverter() }
                };

                var logicObject = new
                {
                    model = new
                    {
                        dimensions = logicData.Model?.Dimensions,
                        directions = logicData.Model?.Directions
                    },
                    action = logicData.Action,
                    maskType = logicData.MaskType,
                    credits = logicData.Credits,
                    soundSample = logicData.SoundSample,
                    planetSystems = logicData.PlanetSystems?.Any() == true ? logicData.PlanetSystems : null,
                    particleSystems = logicData.ParticleSystems?.Any() == true ? logicData.ParticleSystems : null,
                    customVars = logicData.CustomVars?.Variables.Any() == true ? logicData.CustomVars : null
                };

                var fullObject = new
                {
                    name = indexData.Name,
                    logicType = indexData.LogicType,
                    visualizationType = indexData.VisualizationType,
                    assets = assetData,
                    logic = logicObject,
                    visualizations = visualizations,
                    spritesheet = spriteSheetData
                };

                string jsonContent = JsonSerializer.Serialize(fullObject, jsonOptions);
                await File.WriteAllTextAsync(jsonOutputPath, jsonContent);

                await BundleNitroFileAsync(fileOutputDirectory, fileName, OutputDirectory, spriteSheetPath);

                // Optionally delete the output directory after bundling.
                // DeleteDirectory(fileOutputDirectory);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating sprite sheet for {fileName}: {ex.Message}");
                return false;
            }
        }

        private static Dictionary<string, Bitmap> LoadImages(string imagesDirectory)
        {
            var images = new Dictionary<string, Bitmap>();
            foreach (var imageFile in Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                string imageName = Path.GetFileNameWithoutExtension(imageFile);
                if (imageName.StartsWith("sh_") || imageName.Contains("_32_")) continue;

                try
                {
                    using var bitmap = new Bitmap(imageFile);
                    if (!images.ContainsKey(imageName))
                    {
                        images[imageName] = new Bitmap(bitmap);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error loading image {imageFile}: {ex.Message}");
                }
            }
            return images;
        }

        private static async Task<IndexData> GetIndexDataAsync(string binaryOutputPath)
        {
            var indexFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_index.bin", SearchOption.TopDirectoryOnly);
            return indexFiles.Length > 0 ? await IndexMapper.ParseIndexFileAsync(indexFiles[0]) : null;
        }

        private static async Task<Dictionary<string, AssetsMapper.Asset>> GetAssetDataAsync(
            string binaryOutputPath, Dictionary<string, string> imageSources, string csvPath, string fileOutputDirectory)
        {
            var assetsFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_assets.bin", SearchOption.TopDirectoryOnly);
            var manifestFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_manifest.bin", SearchOption.TopDirectoryOnly);
            return (assetsFiles.Length > 0 && manifestFiles.Length > 0)
                ? await AssetsMapper.ParseAssetsFileAsync(assetsFiles[0], imageSources, manifestFiles[0], csvPath, fileOutputDirectory)
                : null;
        }

        private static async Task<AssetLogicData> GetLogicDataAsync(string binaryOutputPath)
        {
            string[] logicFiles = Directory.GetFiles(
                Path.Combine(binaryOutputPath, "binaryData"),
                "*_logic.bin",
                SearchOption.TopDirectoryOnly
            );

            if (logicFiles.Length > 0)
            {
                string logicFilePath = logicFiles[0];
                string logicContent = await File.ReadAllTextAsync(logicFilePath);
                XElement logicElement = XElement.Parse(logicContent);
                return LogicMapper.MapLogicXml(logicElement);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"No *_logic.bin file found in {binaryOutputPath}. Continuing without logic.");
                Console.ResetColor();
                return null;
            }
        }

        private static async Task<List<Visualization>> GetVisualizationsDataAsync(string binaryOutputPath)
        {
            string[] visualizationFiles = Directory.GetFiles(
                Path.Combine(binaryOutputPath, "binaryData"),
                "*_visualization.bin",
                SearchOption.TopDirectoryOnly
            );

            if (visualizationFiles.Length > 0)
            {
                string visualizationFilePath = visualizationFiles[0];
                string visualizationContent = await File.ReadAllTextAsync(visualizationFilePath);
                XElement visualizationElement = XElement.Parse(visualizationContent);
                return VisualizationsMapper.MapVisualizationsXml(visualizationElement);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"No *_visualization.bin file found in {binaryOutputPath}. Continuing without visualization.");
                Console.ResetColor();
                return null;
            }
        }

        private static async Task BundleNitroFileAsync(string outputDirectory, string fileName, string nitroOutputDirectory, string spriteSheetPath)
        {
            var nitroBundler = new NitroBundler();
            string jsonFilePath = Path.Combine(outputDirectory, $"{fileName}.json");

            if (File.Exists(jsonFilePath))
                nitroBundler.AddFile($"{fileName}.json", await File.ReadAllBytesAsync(jsonFilePath));

            if (File.Exists(spriteSheetPath))
                nitroBundler.AddFile(Path.GetFileName(spriteSheetPath), await File.ReadAllBytesAsync(spriteSheetPath));

            await File.WriteAllBytesAsync(Path.Combine(nitroOutputDirectory, $"{fileName}.nitro"), await nitroBundler.ToBufferAsync());
            Console.WriteLine($"📦 Generated {fileName}.nitro -> {nitroOutputDirectory}");
        }

        private static void DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error deleting directory {directoryPath}: {ex.Message}");
                }
            }
        }
    }
}
