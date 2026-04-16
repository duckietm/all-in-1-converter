using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations;
using Habbo_DownloaderSWF_Pets_Compiler.Mapper.Index;
using Habbo_Downloader.Tools;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Xml.Linq;

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
            // Skip the effect file.
            if (string.Equals(Path.GetFileName(swfFile), "hh_human_fx.swf", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("ℹ️ Skipping file: hh_human_fx.swf This is an effect file.");
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(swfFile);
            string nitroFilePath = Path.Combine(OutputDirectory, $"{fileName}.nitro");

            if (File.Exists(nitroFilePath))
                return false; // Skip already converted files

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

            // Use CSV instead of debug.xml:
            string csvPath = Path.Combine(binaryOutputPath, "symbolClass", "symbols.csv");
            var imageSources = DebugXmlParser.ParseDebugXml(csvPath);

            // For clothes, obtain the clothes mapping from the CSV.
            var clothesMapping = ClothesDebugXmlParser.GetClothesImageMapping(csvPath);
            if (clothesMapping.Count == 0)
            {
                Console.WriteLine("❌ No valid clothes image mappings found. Skipping sprite sheet generation.");
                return false;
            }

            // Process asset data.
            var assetDataResult = await GetAssetDataAsync(binaryOutputPath, imageSources, csvPath, fileOutputDirectory);

            // Optional per-SWF data (needed for Buddy / pet_* clothes that have their own
            // postures / animations — regular human figure parts simply don't ship these
            // bins, so these helpers just return null and the fields are omitted).
            var indexTask = GetIndexDataAsync(binaryOutputPath);
            var logicTask = GetLogicDataAsync(binaryOutputPath);
            var visualizationTask = GetVisualizationsDataAsync(binaryOutputPath);
            await Task.WhenAll(indexTask, logicTask, visualizationTask);
            var indexData = indexTask.Result;
            var logicData = logicTask.Result;
            var visualizations = visualizationTask.Result;

            // Image Processing.
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

                object? logicObject = null;
                if (logicData != null)
                {
                    logicObject = new
                    {
                        model = logicData.Model == null ? null : new
                        {
                            dimensions = logicData.Model.Dimensions,
                            directions = logicData.Model.Directions
                        },
                        action = logicData.Action,
                        maskType = logicData.MaskType,
                        credits = logicData.Credits,
                        soundSample = logicData.SoundSample,
                        planetSystems = logicData.PlanetSystems?.Any() == true ? logicData.PlanetSystems : null,
                        particleSystems = logicData.ParticleSystems?.Any() == true ? logicData.ParticleSystems : null,
                        customVars = logicData.CustomVars?.Variables.Any() == true ? logicData.CustomVars : null
                    };
                }

                var visualizationsList = visualizations?.Where(v => !IsVisualizationEmpty(v)).ToList();
                if (visualizationsList != null && visualizationsList.Count == 0)
                    visualizationsList = null;

                string? indexName = indexData?.Name;
                string resolvedName = !string.IsNullOrEmpty(indexName) ? indexName! : assetDataResult.LibraryName;

                var jsonContent = JsonSerializer.Serialize(new
                {
                    type = indexData?.Type,
                    name = resolvedName,
                    logicType = indexData?.LogicType,
                    visualizationType = indexData?.VisualizationType,
                    assets = assetDataResult.Assets,
                    logic = logicObject,
                    visualizations = visualizationsList,
                    spritesheet = spriteSheetData
                }, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                });

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

        private static async Task<IndexPetsData?> GetIndexDataAsync(string binaryOutputPath)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            if (!Directory.Exists(binaryDataPath)) return null;

            var indexFiles = Directory.GetFiles(binaryDataPath, "*_index.*", SearchOption.TopDirectoryOnly);
            return indexFiles.Length > 0 ? await IndexPetsMapper.ParsePetsIndexFileAsync(indexFiles[0]) : null;
        }

        private static async Task<AssetLogicData?> GetLogicDataAsync(string binaryOutputPath)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            if (!Directory.Exists(binaryDataPath)) return null;

            var logicFiles = Directory.GetFiles(binaryDataPath, "*_logic.*", SearchOption.TopDirectoryOnly);
            if (logicFiles.Length == 0) return null;

            try
            {
                string logicContent = await File.ReadAllTextAsync(logicFiles[0]);
                XElement logicElement = XElement.Parse(logicContent);
                return LogicMapper.MapLogicXml(logicElement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error parsing logic for clothes SWF: {ex.Message}");
                return null;
            }
        }

        private static async Task<List<Visualization>?> GetVisualizationsDataAsync(string binaryOutputPath)
        {
            var binaryDataPath = Path.Combine(binaryOutputPath, "binaryData");
            if (!Directory.Exists(binaryDataPath)) return null;

            var visualizationFiles = Directory.GetFiles(binaryDataPath, "*_visualization.*", SearchOption.TopDirectoryOnly);
            if (visualizationFiles.Length == 0) return null;

            try
            {
                string visualizationContent = await File.ReadAllTextAsync(visualizationFiles[0]);
                XElement visualizationElement = XElement.Parse(visualizationContent);
                return VisualizationsMapper.MapVisualizationsXml(visualizationElement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error parsing visualization for clothes SWF: {ex.Message}");
                return null;
            }
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
