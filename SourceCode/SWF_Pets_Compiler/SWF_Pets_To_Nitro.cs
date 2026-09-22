using Habbo_Downloader.SWF_Pets_Compiler.Mapper.Assests;
using Habbo_DownloaderSWF_Pets_Compiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using Habbo_Downloader.App.Workspaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Habbo_Downloader.SWF_Pets_Compiler.Mapper.palette;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Pets_To_Nitro
    {
        private static string ImportDirectory;
        private static string OutputDirectory => AssetWorkspaceRuntime.Router.AssetDirectory(
            WorkspaceAssetKind.Pets,
            Path.Combine("SWFCompiler", "pets"));

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                ImportDirectory = Path.Combine("SWFCompiler", "import", "pets");
                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("ℹ️ No SWF files found in the import (SWFCompiler\\import\\pets) directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} pet SWF files to convert.");

                int totalFiles = swfFiles.Length;
                int processedCount = 0;
                int convertedCount = 0;
                int skippedCount = 0;
                int failedCount = 0;
                long totalOriginalBytes = 0;
                long totalOutputBytes = 0;
                int maxParallelism = Math.Max(2, (int)(Environment.ProcessorCount * 0.8));

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
                    if (current % 10 == 0 || current == totalFiles || current <= 5)
                    {
                        double pct = (double)current / totalFiles * 100.0;
                        double itemsPerSec = current / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                        Console.WriteLine($"⚡ [{current,4}/{totalFiles}] ({pct,5:F1}%) | {itemsPerSec,5:F1} items/sec | Converted: {convertedCount} | Skipped: {skippedCount}");
                    }
                });

                stopwatch.Stop();

                string formatLabel = ConverterSettings.UseWebp ? "WebP Lossless" : "Standard PNG";
                ConversionSummaryPrinter.PrintSummary(
                    processTitle: $"SWF Pets -> Nitro ({formatLabel})",
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

            if (File.Exists(nitroFilePath)) return false;

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

                string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
                var canonicalMapping = AssetNameMapper.BuildCanonicalMapping(csvPath);
                var imageSources = DebugXmlParser.ParseDebugXml(csvPath);

                var indexTask = GetIndexPetsDataAsync(binaryOutputPath);
                var assetsTask = GetAssetPetsDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory, Path.GetFileName(swfFile));
                var logicTask = GetLogicDataAsync(binaryOutputPath);
                var visualizationTask = GetVisualizationsDataAsync(binaryOutputPath);

                await Task.WhenAll(indexTask, assetsTask, logicTask, visualizationTask);
                var indexData = indexTask.Result;
                var assetData = assetsTask.Result;
                var logicData = logicTask.Result;
                var visualizations = visualizationTask.Result;
                var palettes = PaletteExtractor.ExtractPalettes(binaryOutputPath);

                if (indexData == null)
                {
                    return false;
                }

                string imagesDirectory = Path.Combine(binaryOutputPath, "images");
                string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

                await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, AssetsPetsMapper.LatestImageMapping);

                // Some pets ship an INCOMPLETE size-32 variant (fewer animations, or
                // most of the size-32 frames missing) that cannot render and breaks
                // catalog loading (e.g. monkey, turtle). Keep size-32 only when it is
                // as complete as size-64; otherwise drop the size-32 visualization,
                // its assets and its images so the pet renders at size-64 like before.
                bool keepSize32 = ShouldKeepSize32(visualizations, assetData);
                if (!keepSize32)
                {
                    if (visualizations != null) visualizations = visualizations.Where(v => v.Size != 32).ToList();

                    foreach (var key in assetData.Keys.Where(k => k.Contains("_32_")).ToList())
                    {
                        assetData.Remove(key);
                    }
                }

                images = LoadImages(imagesDirectory, keepSize32);
                if (images.Count == 0)
                {
                    return false;
                }

                var (spriteSheetPath, spriteSheetData) = SpriteSheetMapper.GenerateSpriteSheet(
                    images,
                    fileOutputDirectory,
                    fileName,
                    canonicalMapping,
                    disableCleanKey: false,
                    numRows: 10,
                    maxWidth: 7500,
                    maxHeight: 12500
                );

                if (spriteSheetPath == null || spriteSheetData == null)
                {
                    return false;
                }

                var jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");

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
                    type = indexData.Type,
                    name = indexData.Name,
                    logicType = indexData.LogicType,
                    visualizationType = indexData.VisualizationType,
                    assets = assetData,
                    palettes = palettes,
                    logic = logicObject,
                    visualizations = visualizations?.Any(v => !IsVisualizationEmpty(v)) == true ? visualizations.Where(v => !IsVisualizationEmpty(v)).ToList() : null,
                    spritesheet = spriteSheetData
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                };

                string jsonContent = JsonSerializer.Serialize(fullObject, jsonOptions)
                    .Replace("[\n  ", "[")
                    .Replace("\n  ]", "]")
                    .Replace("\n    ", " ");

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

        // Pets ship an INCOMPLETE size-32 variant for some animals; ShouldKeepSize32
        // decides whether this pet's size-32 art is usable. Only include the _32_
        // frames when it is (sh_ never applies to pets).
        private static bool ShouldKeepSize32(
            List<Visualization> visualizations,
            Dictionary<string, AssetsPetsMapper.Asset> assets)
        {
            if (visualizations == null || assets == null) return false;

            var size32 = visualizations.FirstOrDefault(v => v.Size == 32);
            var size64 = visualizations.FirstOrDefault(v => v.Size == 64);

            // No size-32 to keep, or no size-64 baseline to compare against.
            if (size32 == null || size64 == null) return false;

            // Animation completeness: size-32 must declare at least as many
            // animations as size-64 (monkey has 32 vs 33 -> dropped).
            int anims32 = size32.Animations?.Count ?? 0;
            int anims64 = size64.Animations?.Count ?? 0;
            if (anims32 < anims64) return false;

            // Frame completeness: size-32 must ship a comparable number of assets
            // to size-64. Some pets declare the animations but ship almost no
            // size-32 frames (turtle: 198 vs 697 -> dropped).
            int assets32 = assets.Keys.Count(k => k.Contains("_32_"));
            int assets64 = assets.Keys.Count(k => k.Contains("_64_"));
            if (assets64 > 0 && assets32 < (int)(assets64 * 0.9)) return false;

            return true;
        }

        private static Dictionary<string, Image<Rgba32>> LoadImages(string imagesDirectory, bool includeSize32)
        {
            var images = new Dictionary<string, Image<Rgba32>>();
            foreach (var imageFile in Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                string imageName = Path.GetFileNameWithoutExtension(imageFile);
                // Drop size-32 frames unless this pet's size-32 art passed the
                // completeness check (sh_ never applies to pets).
                if (imageName.StartsWith("sh_") || (!includeSize32 && imageName.Contains("_32_"))) continue;

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

        private static async Task<IndexPetsData> GetIndexPetsDataAsync(string binaryOutputPath)
        {
            var indexFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_index.bin", SearchOption.TopDirectoryOnly);
            return indexFiles.Length > 0 ? await IndexPetsMapper.ParsePetsIndexFileAsync(indexFiles[0]) : null;
        }

        private static async Task<Dictionary<string, AssetsPetsMapper.Asset>> GetAssetPetsDataAsync(
            string binaryOutputPath, Dictionary<string, string> imageSources, string csvPath, string fileOutputDirectory, string swfFileName)
        {
            var assetsFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_assets.bin", SearchOption.TopDirectoryOnly);
            var manifestFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_manifest.bin", SearchOption.TopDirectoryOnly);
            return (assetsFiles.Length > 0 && manifestFiles.Length > 0)
                ? await AssetsPetsMapper.ParseAssetsFileAsync(assetsFiles[0], imageSources, manifestFiles[0], csvPath, swfFileName, fileOutputDirectory)
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

        private static bool IsVisualizationEmpty(Visualization v)
        {
            return v == null ||
                   (v.Layers == null || v.Layers.Count == 0) &&
                   (v.Directions == null || v.Directions.Count == 0) &&
                   (v.Animations == null || v.Animations.Count == 0) &&
                   (v.Colors == null || v.Colors.Count == 0) &&
                   (v.Postures?.Postures == null || v.Postures.Postures.Count == 0) &&
                   (v.Gestures == null || v.Gestures.Count == 0);
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
