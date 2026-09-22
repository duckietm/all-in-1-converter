using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using Habbo_Downloader.App.Workspaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_clothes_To_Nitro
    {
        private static string ImportDirectory;
        private static string OutputDirectory => AssetWorkspaceRuntime.Router.AssetDirectory(
            WorkspaceAssetKind.Clothing,
            Path.Combine("SWFCompiler", "clothes"));

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported clothes? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                ImportDirectory = string.IsNullOrEmpty(input) || input == "H"
                    ? Path.Combine("Habbo_Default", "clothes")
                    : input == "I" ? Path.Combine("SWFCompiler", "import", "clothes") : Path.Combine("Habbo_Default", "hof_furni");

                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("ℹ️ No SWF files found in the import (SWFCompiler\\import\\clothes) directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} clothing SWF files to convert.");

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

                    if (string.Equals(Path.GetFileName(swfFile), "hh_human_fx.swf", StringComparison.OrdinalIgnoreCase))
                    {
                        Interlocked.Increment(ref skippedCount);
                    }
                    else if (File.Exists(targetNitroPath))
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
                    processTitle: $"SWF Clothes -> Nitro ({formatLabel})",
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
            // Skip the effect file.
            if (string.Equals(Path.GetFileName(swfFile), "hh_human_fx.swf", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

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
                await FfdecExtractorClothes.ExtractSWFAsync(swfFile, binaryOutputPath);

                if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
                {
                    return false;
                }

                // Use CSV instead of debug.xml:
                string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
                var imageSources = DebugXmlParser.ParseDebugXml(csvPath);

                // For clothes, obtain the clothes mapping from the CSV.
                var clothesMapping = ClothesDebugXmlParser.GetClothesImageMapping(csvPath);
                if (clothesMapping.Count == 0)
                {
                    return false;
                }

                // Process asset data.
                var assetDataResult = await GetAssetDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory);

                // Image Processing.
                string imagesDirectory = Path.Combine(binaryOutputPath, "images");
                string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

                await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, clothesMapping);

                images = LoadImages(imagesDirectory);
                if (images.Count == 0)
                {
                    return false;
                }

                var (spriteSheetPath, spriteSheetData) = SpritesheetClothesMapper.GenerateSpriteSheet(
                    images, fileOutputDirectory, fileName, maxWidth: 10240, maxHeight: 7000
                );

                if (spriteSheetPath == null || spriteSheetData == null)
                {
                    return false;
                }

                var jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");
                var jsonContent = JsonSerializer.Serialize(new
                {
                    assets = assetDataResult.Assets,
                    name = assetDataResult.LibraryName,
                    spritesheet = spriteSheetData
                }, new JsonSerializerOptions
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
                // Keep the small-scale (`sh_`) sprites: the client renders avatars
                // at scale `sh` when the room geometry is zoomed out (scale 32). If
                // these are stripped here the zoomed-out avatar has no textures and
                // is invisible.
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

        private static async Task<(string LibraryName, Dictionary<string, ClothesAssetsMapper.Asset> Assets)> GetAssetDataAsync(
            string binaryOutputPath, Dictionary<string, string> imageSources, string csvPath, string fileOutputDirectory)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            var manifestFiles = Directory.GetFiles(binaryDataPath, "*_manifest.*", SearchOption.TopDirectoryOnly);

            if (manifestFiles.Length == 0)
            {
                Console.WriteLine($"❌ Manifest file not found in {binaryDataPath}");
                return ("", new Dictionary<string, ClothesAssetsMapper.Asset>());
            }

            return await ClothesAssetsMapper.ParseAssetsFileAsync(null, imageSources, manifestFiles[0], csvPath, fileOutputDirectory);
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
