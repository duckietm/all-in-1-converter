using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWFCompiler.Mapper.Visualizations;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using Habbo_Downloader.App.Workspaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Furni_To_Nitro
    {
        private static string ImportDirectory;
        private static string OutputDirectory => AssetWorkspaceRuntime.Router.AssetDirectory(
            WorkspaceAssetKind.Furniture,
            Path.Combine("SWFCompiler", "furniture"));

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported furniture? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                ImportDirectory = string.IsNullOrEmpty(input) || input == "H"
                    ? Path.Combine("Habbo_Default", "hof_furni")
                    : input == "I" ? Path.Combine("SWFCompiler", "import", "furniture") : Path.Combine("Habbo_Default", "hof_furni");

                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} SWF files to convert.");

                // Process multiple SWFs in parallel.
                int totalFiles = swfFiles.Length;
                int processedCount = 0;
                int convertedCount = 0;
                int skippedCount = 0;
                int failedCount = 0;
                long totalOriginalBytes = 0;
                long totalOutputBytes = 0;
                int maxParallelism = Math.Max(2, (int)(Environment.ProcessorCount * 0.9));

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                await Parallel.ForEachAsync(swfFiles, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, async (swfFile, _) =>
                {
                    string fName = Path.GetFileNameWithoutExtension(swfFile);
                    string targetNitroPath = Path.Combine(OutputDirectory, $"{fName}.nitro");
                    long swfLength = 0;
                    try { swfLength = new FileInfo(swfFile).Length; } catch { }
                    Interlocked.Add(ref totalOriginalBytes, swfLength);

                    if (File.Exists(targetNitroPath))
                    {
                        Interlocked.Increment(ref skippedCount);
                        try { Interlocked.Add(ref totalOutputBytes, new FileInfo(targetNitroPath).Length); } catch { }
                    }
                    else
                    {
                        bool converted = await ProcessSwfFileAsync(swfFile);
                        if (converted && File.Exists(targetNitroPath))
                        {
                            Interlocked.Increment(ref convertedCount);
                            try { Interlocked.Add(ref totalOutputBytes, new FileInfo(targetNitroPath).Length); } catch { }
                        }
                        else
                        {
                            Interlocked.Increment(ref failedCount);
                        }
                    }

                    int current = Interlocked.Increment(ref processedCount);
                    if (current % 50 == 0 || current == totalFiles || current <= 10)
                    {
                        double pct = (double)current / totalFiles * 100.0;
                        double itemsPerSec = current / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                        Console.WriteLine($"⚡ [{current,5}/{totalFiles}] ({pct,5:F1}%) | {itemsPerSec,5:F1} items/sec | Converted: {convertedCount} | Skipped: {skippedCount}");
                    }
                });

                stopwatch.Stop();

                string formatLabel = ConverterSettings.UseWebp ? "WebP Lossless" : "Standard PNG";
                ConversionSummaryPrinter.PrintSummary(
                    processTitle: $"SWF Furniture -> Nitro ({formatLabel})",
                    totalFiles: totalFiles,
                    convertedFiles: convertedCount,
                    skippedFiles: skippedCount,
                    failedFiles: failedCount,
                    totalOriginalBytes: totalOriginalBytes,
                    totalOutputBytes: totalOutputBytes,
                    elapsed: stopwatch.Elapsed,
                    outputDirectory: OutputDirectory,
                    formatName: formatLabel
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during SWF conversion: {ex.Message}");
            }
        }

        public static async Task<bool> ProcessSwfFileAsync(string swfFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(swfFile);
            string nitroFilePath = Path.Combine(OutputDirectory, $"{fileName}.nitro");

            if (File.Exists(nitroFilePath)) return false; // Skip already converted files

            string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
            Directory.CreateDirectory(fileOutputDirectory);

            string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

            Dictionary<string, Image<Rgba32>>? images = null;

            try
            {
                await FfdecExtractor.ExtractSWFAsync(swfFile, binaryOutputPath);

                if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
                {
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
                    return false;
                }

                // Image Processing
                string imagesDirectory = Path.Combine(binaryOutputPath, "images");
                string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

                await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, AssetsMapper.LatestImageMapping);

                images = LoadImages(imagesDirectory);
                if (images.Count == 0)
                {
                    return false;
                }

                // Pass the canonicalMapping to GenerateSpriteSheet.
                var (spriteSheetPath, spriteSheetData) = SpriteSheetMapper.GenerateSpriteSheet(
                    images,
                    fileOutputDirectory,
                    fileName,
                    canonicalMapping,
                    disableCleanKey: false,
                    numRows: 10,
                    maxWidth: 11266,
                    maxHeight: 12800
                );

                if (spriteSheetPath == null || spriteSheetData == null)
                {
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
                        dimensions = logicData?.Model?.Dimensions,
                        directions = logicData?.Model?.Directions
                    },
                    action = logicData?.Action,
                    maskType = logicData?.MaskType,
                    credits = logicData?.Credits,
                    soundSample = logicData?.SoundSample,
                    planetSystems = logicData?.PlanetSystems?.Any() == true ? logicData.PlanetSystems : null,
                    particleSystems = logicData?.ParticleSystems?.Any() == true ? logicData.ParticleSystems : null,
                    customVars = logicData?.CustomVars?.Variables.Any() == true ? logicData.CustomVars : null
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
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating sprite sheet for {fileName}: {ex.Message}");
                return false;
            }
            finally
            {
                if (images != null)
                {
                    foreach (var img in images.Values)
                    {
                        try { img.Dispose(); } catch { }
                    }
                }
                DeleteDirectory(fileOutputDirectory);
            }
        }

        private static Dictionary<string, Image<Rgba32>> LoadImages(string imagesDirectory)
        {
            var images = new Dictionary<string, Image<Rgba32>>();
            foreach (var imageFile in Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                string imageName = Path.GetFileNameWithoutExtension(imageFile);
                if (imageName.StartsWith("sh_")) continue;

                try
                {
                    if (!images.ContainsKey(imageName))
                    {
                        images[imageName] = Image.Load<Rgba32>(imageFile);
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
