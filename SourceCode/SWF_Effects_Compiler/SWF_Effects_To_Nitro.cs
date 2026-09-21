using Habbo_Downloader.Tools;
using System.Text.Json;
using System.Text.Json.Serialization;
using Habbo_Downloader.SWF_Effects_Compiler.Mapper.Assets;
using Habbo_Downloader.SWF_Effects_Compiler.Spritesheet;
using Habbo_Downloader.SWF_Effects_Compiler.Mapper.Animation;
using Habbo_Downloader.SWFCompiler.Mapper;
using Habbo_Downloader.App.Workspaces;
using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Effects_To_Nitro
    {
        private static readonly string ImportDirectory = Path.Combine("SWFCompiler", "import", "effects");
        private static string OutputDirectory => AssetWorkspaceRuntime.Router.AssetDirectory(
            WorkspaceAssetKind.Effects,
            Path.Combine("SWFCompiler", "effects"));

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("ℹ️ No SWF files found in the import (SWFCompiler\\import\\effects) directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} effect SWF files to convert.");

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
                    if (current % 25 == 0 || current == totalFiles || current <= 10)
                    {
                        double pct = (double)current / totalFiles * 100.0;
                        double itemsPerSec = current / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                        Console.WriteLine($"⚡ [{current,4}/{totalFiles}] ({pct,5:F1}%) | {itemsPerSec,5:F1} items/sec | Converted: {convertedCount} | Skipped: {skippedCount}");
                    }
                });

                stopwatch.Stop();

                string formatLabel = ConverterSettings.UseWebp ? "WebP Lossless" : "Standard PNG";
                ConversionSummaryPrinter.PrintSummary(
                    processTitle: $"SWF Effects -> Nitro ({formatLabel})",
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

            if (File.Exists(nitroFilePath))
                return false; // Skip already converted files

            string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
            Directory.CreateDirectory(fileOutputDirectory);

            string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

            Dictionary<string, Image<Rgba32>>? images = null;

            try
            {
                await FfdecExtractorEffects.ExtractSWFAsync(swfFile, binaryOutputPath);

                if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
                {
                    return false;
                }

                string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
                var imageSources = DebugXmlParser.ParseDebugXml(csvPath);
                var EffectsMapping = EffectXMLParser.GetEffectsImageMapping(csvPath);

                // Now GetAssetDataAsync returns an AssetData instance rather than a tuple.
                var assetDataResult = await GetAssetDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory);
                var assetsData = assetDataResult.Assets;

                var animationDataResult = await EffectAnimationMapper.ParseAnimationFileAsync(Path.Combine(binaryOutputPath, "binaryData"));

                string imagesDirectory = Path.Combine(binaryOutputPath, "images");
                string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");
                await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, EffectsMapping);
                images = LoadImages(imagesDirectory);

                string? spriteSheetPath = null;
                object? spriteSheetData = null;
                if (images.Count > 0)
                {
                    try
                    {
                        var result = EffectsSpritesheetMapper.GenerateSpriteSheet(
                            images, fileOutputDirectory, fileName, maxWidth: 10240, maxHeight: 7000
                        );
                        spriteSheetPath = result.ImagePath;
                        spriteSheetData = result.SpriteData;
                    }
                    catch { }
                }

                var jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");

                if ((assetsData == null || assetsData.Count == 0)
                    && spriteSheetData is SpriteSheetData sheetForAssets
                    && sheetForAssets.Frames != null
                    && sheetForAssets.Frames.Count > 0)
                {
                    var derivedAssets = new Dictionary<string, EffectAssetsMapper.Asset>();
                    string libPrefix = fileName + "_";

                    foreach (var frameName in sheetForAssets.Frames.Keys)
                    {
                        string lookupName = frameName.StartsWith(libPrefix)
                            ? frameName.Substring(libPrefix.Length)
                            : frameName;

                        if (!derivedAssets.ContainsKey(lookupName))
                        {
                            derivedAssets[lookupName] = new EffectAssetsMapper.Asset { X = 0, Y = 0 };
                        }
                    }

                    if (derivedAssets.Count > 0)
                    {
                        assetsData = derivedAssets;
                    }
                }

                if (assetsData == null || assetsData.Count == 0)
                {
                    assetsData = null;
                }

                var jsonContent = JsonSerializer.Serialize(new
                {
                    assets = assetsData,
                    aliases = assetDataResult.Aliases,
                    animations = animationDataResult,
                    name = fileName,
                    spritesheet = spriteSheetData
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                await File.WriteAllTextAsync(jsonOutputPath, jsonContent);
                await BundleNitroFileAsync(fileOutputDirectory, fileName, OutputDirectory, spriteSheetPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating Nitro file for {fileName}: {ex.Message}");
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
                // Keep the small-scale (`sh_`) effect sprites: the client renders
                // avatars (and their effects) at scale `sh` when the room geometry
                // is zoomed out (scale 32). Dropping them left zoomed-out effects
                // (dances, held items, etc.) invisible.
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

        private static async Task<EffectAssetsMapper.AssetData> GetAssetDataAsync(
    string binaryOutputPath, Dictionary<string, string> imageSources, string csvPath, string fileOutputDirectory)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            var manifestFiles = Directory.GetFiles(binaryDataPath, "*_manifest.*", SearchOption.TopDirectoryOnly);

            if (manifestFiles.Length == 0)
            {
                Console.WriteLine($"❌ Manifest file not found in {binaryDataPath}");
                return new EffectAssetsMapper.AssetData();
            }

            return await EffectAssetsMapper.ParseAssetsFileAsync(null, imageSources, manifestFiles[0], csvPath, fileOutputDirectory);
        }


        private static async Task BundleNitroFileAsync(string outputDirectory, string fileName, string nitroOutputDirectory, string spriteSheetPath)
        {
            var nitroBundler = new NitroBundler();
            string jsonFilePath = Path.Combine(outputDirectory, $"{fileName}.json");

            if (File.Exists(jsonFilePath))
                nitroBundler.AddFile($"{fileName}.json", await File.ReadAllBytesAsync(jsonFilePath));

            if (!string.IsNullOrEmpty(spriteSheetPath) && File.Exists(spriteSheetPath))
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
