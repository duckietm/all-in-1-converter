using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_clothes_To_Nitro
    {
        private static string ImportDirectory;
        private const string OutputDirectory = @"SWFCompiler\clothes";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported clothes? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                ImportDirectory = string.IsNullOrEmpty(input) || input == "H"
                    ? @"Habbo_Default\clothes"
                    : input == "I" ? @"SWFCompiler\import\clothes" : @"Habbo_Default\hof_furni";

                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("ℹ️ No SWF files found in the import (SWFCompiler\\import\\clothes) directory.");
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
            if (string.Equals(Path.GetFileName(swfFile), "hh_human_fx.swf", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("ℹ️ Skipping file: hh_human_fx.swf This is an effect file.");
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(swfFile);
            string nitroFilePath = Path.Combine(OutputDirectory, $"{fileName}.nitro");

            if (File.Exists(nitroFilePath)) return false; // Skip already converted files

            string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);
            Directory.CreateDirectory(fileOutputDirectory);

            string binaryOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}_binaryData");

            Console.WriteLine($"🔍 Start Decompiling Clothes SWF: {fileName}...");
            await FfdecExtractorClothes.ExtractSWFAsync(swfFile, binaryOutputPath);

            if (!Directory.Exists(Path.Combine(binaryOutputPath, "binaryData")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Extraction failed for {fileName}. No binaryData folder found.");
                Console.ResetColor();
                return false;
            }

            string debugXmlPath = Path.Combine(binaryOutputPath, "debug.xml");
            var imageSources = DebugXmlParser.ParseDebugXml(debugXmlPath);

            // For clothes, get the mapping using the new helper:
            var clothesMapping = ClothesDebugXmlParser.GetClothesImageMapping(debugXmlPath);
            if (clothesMapping.Count == 0)
            {
                Console.WriteLine("❌ No valid clothes image mappings found. Skipping sprite sheet generation.");
                return false;
            }

            // ✅ Process asset data
            var assetDataResult = await GetAssetDataAsync(binaryOutputPath, imageSources, debugXmlPath, fileOutputDirectory);

            // ✅ Image Processing
            string imagesDirectory = Path.Combine(binaryOutputPath, "images");
            string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

            await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, clothesMapping);


            var images = LoadImages(imagesDirectory);
            if (images.Count == 0)
            {
                Console.WriteLine($"⚠️ No valid images found for {fileName}. Skipping sprite sheet generation.");
                return false;
            }

            try
            {
                var (spriteSheetPath, spriteSheetData) = SpritesheetClothesMapper.GenerateSpriteSheet(
                    images, fileOutputDirectory, fileName, maxWidth: 10240, maxHeight: 7000
                );

                if (spriteSheetPath == null || spriteSheetData == null)
                {
                    Console.WriteLine($"⚠️ No images found to generate spritesheet for {fileName}. Skipping...");
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

                // ✅ After .nitro is created, delete the directory (commented for debugging)
                DeleteDirectory(fileOutputDirectory);

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

        private static async Task<(string LibraryName, Dictionary<string, ClothesAssetsMapper.Asset> Assets)> GetAssetDataAsync(
            string binaryOutputPath, Dictionary<string, string> imageSources, string debugXmlPath, string fileOutputDirectory)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            var manifestFiles = Directory.GetFiles(binaryDataPath, "*_manifest.*", SearchOption.TopDirectoryOnly);

            if (manifestFiles.Length == 0)
            {
                Console.WriteLine($"❌ Manifest file not found in {binaryDataPath}");
                return ("", new Dictionary<string, ClothesAssetsMapper.Asset>());
            }

            return await ClothesAssetsMapper.ParseAssetsFileAsync(null, imageSources, manifestFiles[0], debugXmlPath, fileOutputDirectory);
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
