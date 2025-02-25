using Habbo_Downloader.Tools;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Habbo_Downloader.SWF_Effects_Compiler.Mapper.Assets;
using Habbo_Downloader.SWF_Effects_Compiler.Spritesheet;
using Habbo_Downloader.SWF_Effects_Compiler.Mapper.Animation;
using System.Collections.Concurrent;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Effects_To_Nitro
    {
        private static string ImportDirectory = @"SWFCompiler\import\effects";
        private const string OutputDirectory = @"SWFCompiler\effects";

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

                Console.WriteLine($"✅ Found {swfFiles.Length} SWF files.");

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

            if (File.Exists(nitroFilePath))
                return false; // Skip already converted files

            string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
            Directory.CreateDirectory(fileOutputDirectory);

            string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

            Console.WriteLine($"🔍 Start Decompiling effects SWF: {fileName}...");
            await FfdecExtractorEffects.ExtractSWFAsync(swfFile, binaryOutputPath);

            if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Extraction failed for {fileName}. No binaryData folder found.");
                Console.ResetColor();
                return false;
            }

            string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
            var imageSources = DebugXmlParser.ParseDebugXml(csvPath);
            var EffectsMapping = EffectXMLParser.GetEffectsImageMapping(csvPath);

            if (EffectsMapping.Count == 0)
            {
                Console.WriteLine("⚠️ No valid effects image mappings found. Skipping sprite sheet generation.");
            }

            // Now GetAssetDataAsync returns an AssetData instance rather than a tuple.
            var assetDataResult = await GetAssetDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory);
            // Access assets and library name via properties
            var assetsData = assetDataResult.Assets;
            var libraryName = assetDataResult.LibraryName; // use if needed later

            var animationDataResult = await EffectAnimationMapper.ParseAnimationFileAsync(Path.Combine(binaryOutputPath, "binaryData"));

            string imagesDirectory = Path.Combine(binaryOutputPath, "images");
            string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");
            await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, EffectsMapping);
            var images = LoadImages(imagesDirectory);

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
                    if (spriteSheetPath == null || spriteSheetData == null)
                    {
                        Console.WriteLine($"⚠️ No images found to generate spritesheet for {fileName}. Spritesheet will be omitted.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error generating spritesheet for {fileName}: {ex.Message}. Spritesheet will be omitted.");
                }
            }

            try
            {
                var jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");

                // If assetsData is empty, set it to null.
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

                // Optionally delete the output directory after bundling.
                DeleteDirectory(fileOutputDirectory);

                return true;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating Nitro file for {fileName}: {ex.Message}");
                return false;
            }
        }


        private static Dictionary<string, Bitmap> LoadImages(string imagesDirectory)
        {
            var images = new Dictionary<string, Bitmap>();
            foreach (var imageFile in Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly))
            {
                string imageName = Path.GetFileNameWithoutExtension(imageFile);
                if (imageName.StartsWith("sh_") || imageName.Contains("_32_"))
                    continue;

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
