using Habbo_Downloader.SWF_Pets_Compiler.Mapper.Assests;
using Habbo_DownloaderSWF_Pets_Compiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.Tools;
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
using Habbo_Downloader.SWF_Pets_Compiler.Mapper.palette;

namespace Habbo_Downloader.Compiler
{
    public static class SWF_Pets_To_Nitro
    {
        private static string ImportDirectory;
        private const string OutputDirectory = @"SWFCompiler\pets";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                ImportDirectory = @"SWFCompiler\import\pets";
                Console.WriteLine($"✅ Converting SWF to Nitro from source {ImportDirectory}");

                Directory.CreateDirectory(OutputDirectory);
                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"✅ Found {swfFiles.Length} SWF files.");

                int convertedCount = 0;

                foreach (var swfFile in swfFiles)
                {
                    Console.WriteLine($"\n🎬 Processing: {Path.GetFileName(swfFile)}");

                    bool success = await ProcessSwfFileAsync(swfFile);

                    if (success)
                    {
                        convertedCount++;
                    }

                    Console.WriteLine($"✅ Completed: {Path.GetFileName(swfFile)}");
                }

                Console.WriteLine($"✅ All SWF files have been processed. {convertedCount} nitro files were generated.");
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

            if (File.Exists(nitroFilePath)) return false;

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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Failed to parse index file for {fileName}. Skipping...");
                Console.ResetColor();
                return false;
            }

            string imagesDirectory = Path.Combine(binaryOutputPath, "images");
            string tmpDirectory = Path.Combine(binaryOutputPath, "tmp");

            await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, AssetsPetsMapper.LatestImageMapping);

            var images = LoadImages(imagesDirectory);
            if (images.Count == 0)
            {
                Console.WriteLine($"⚠️ No valid images found for {fileName}. Skipping sprite sheet generation.");
                return false;
            }

            try
            {
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
                    Console.WriteLine($"⚠️ No images found to generate spritesheet for {fileName}. Skipping...");
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

                // Optionally delete the output directory after bundling.
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
                        // Create a copy of the bitmap to avoid disposing issues.
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
