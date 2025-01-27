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
                // Prompt the user for input
                Console.WriteLine("Do you want (H) Hof_Furni or (I) Imported furniture? (Default is H):");
                string input = Console.ReadLine()?.Trim().ToUpper();

                if (string.IsNullOrEmpty(input) || input == "H")
                {
                    ImportDirectory = @"hof_furni";
                    Console.WriteLine("You selected Hof_Furni (default).");
                }
                else if (input == "I")
                {
                    ImportDirectory = @"SWFCompiler\import\furniture";
                    Console.WriteLine("You selected Imported furniture.");
                }
                else
                {
                    ImportDirectory = @"hof_furni";
                    Console.WriteLine("Invalid input. Defaulting to Hof_Furni.");
                }

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

                    // Skip if .nitro file already exists
                    if (File.Exists(nitroFilePath))
                    {
                        Console.WriteLine($"Skipping {fileName}.nitro as it is already processed.");
                        continue;
                    }

                    string fileOutputDirectory = Path.Combine(OutputDirectory, fileName);

                    Directory.CreateDirectory(fileOutputDirectory);

                    string binaryOutputPath = Path.Combine(fileOutputDirectory, fileName + "_binaryData");
                    string imageOutputPath = Path.Combine(fileOutputDirectory, fileName + "_images");

                    Console.WriteLine($"Decompiling SWF: {fileName}...");
                    await FfdecExtractor.ExtractBinaryDataAsync(swfFile, binaryOutputPath);
                    await FfdecExtractor.ExtractImageAsync(swfFile, imageOutputPath);

                    // Process index file
                    string[] indexFiles = Directory.GetFiles(binaryOutputPath, "*_index.bin", SearchOption.TopDirectoryOnly);
                    if (indexFiles.Length == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"No *_index.bin file found in {binaryOutputPath}. Skipping this SWF file.");
                        Console.ResetColor();
                        continue;
                    }

                    string indexFilePath = indexFiles[0];

                    var indexData = await IndexMapper.ParseIndexFileAsync(indexFilePath);
                    if (indexData == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to parse {indexFilePath}. Skipping...");
                        Console.ResetColor();
                        continue;
                    }

                    // Process assets file
                    string[] assetsFiles = Directory.GetFiles(binaryOutputPath, "*_assets.bin", SearchOption.TopDirectoryOnly);
                    Dictionary<string, AssetsMapper.Asset> assetData = null;

                    if (assetsFiles.Length > 0)
                    {
                        string assetsFilePath = assetsFiles[0];
                        assetData = await AssetsMapper.ParseAssetsFileAsync(assetsFilePath);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"No *_assets.bin file found in {binaryOutputPath}. Continuing without assets.");
                        Console.ResetColor();
                    }

                    // Process logic file
                    string[] logicFiles = Directory.GetFiles(binaryOutputPath, "*_logic.bin", SearchOption.TopDirectoryOnly);
                    AssetLogicData logicData = null;

                    if (logicFiles.Length > 0)
                    {
                        string logicFilePath = logicFiles[0];
                        string logicContent = await File.ReadAllTextAsync(logicFilePath);
                        XElement logicElement = XElement.Parse(logicContent);
                        logicData = LogicMapper.MapLogicXml(logicElement);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"No *_logic.bin file found in {binaryOutputPath}. Continuing without logic.");
                        Console.ResetColor();
                    }

                    // Process visualizations file
                    string[] visualizationFiles = Directory.GetFiles(binaryOutputPath, "*_visualization.bin", SearchOption.TopDirectoryOnly);
                    List<Visualization> visualizations = null;

                    if (visualizationFiles.Length > 0)
                    {
                        string visualizationFilePath = visualizationFiles[0];
                        string visualizationContent = await File.ReadAllTextAsync(visualizationFilePath);
                        XElement visualizationElement = XElement.Parse(visualizationContent);
                        visualizations = VisualizationsMapper.MapVisualizationsXml(visualizationElement);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"No *_visualization.bin file found in {binaryOutputPath}. Continuing without visualization.");
                        Console.ResetColor();
                    }

                    var imageFiles = Directory.GetFiles(imageOutputPath, "*.*", SearchOption.TopDirectoryOnly);
                    var images = new Dictionary<string, Bitmap>();

                    foreach (var imageFile in imageFiles)
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imageFile);
                        string format = ImageHeaderRecognizer.RecognizeImageHeader(imageFile);
                        if (format != "png")
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Skipping unsupported image format: {imageFile}");
                            Console.ResetColor();
                            continue;
                        }

                        if (imageName.StartsWith("sh_") || imageName.Contains("_32_"))
                        {
                            continue;
                        }

                        try
                        {
                            using (var bitmap = new Bitmap(imageFile))
                            {
                                images[imageName] = new Bitmap(bitmap);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error loading image {imageFile}: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                    try
                    {
                        var (spriteSheetPath, spriteSheetData) = SpriteSheetMapper.GenerateSpriteSheet(
                            images,
                            fileOutputDirectory,
                            fileName,
                            numRows: 10,
                            maxWidth: 10240,
                            maxHeight: 7000
                        );

                        if (spriteSheetPath == null || spriteSheetData == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"No images found to generate spritesheet for {fileName}. Skipping spritesheet generation.");
                            Console.ResetColor();
                            continue;
                        }

                        var spriteBundle = new SpriteBundle
                        {
                            Spritesheet = spriteSheetData,
                            ImageData = new ImageData
                            {
                                Name = $"{fileName}.png",
                                Buffer = await File.ReadAllBytesAsync(spriteSheetPath)
                            }
                        };

                        spriteBundle.Spritesheet.Meta = new SpriteSheetMapper.MetaData
                        {
                            Image = spriteBundle.ImageData.Name,
                            Format = "RGBA8888",
                            Size = spriteSheetData.Meta.Size,
                            Scale = 1.0f
                        };

                        // Generate {name}.json
                        string jsonOutputPath = Path.Combine(fileOutputDirectory, $"{fileName}.json");

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                        };

                        options.Converters.Add(new SpriteSheetMapper.RectDataConverter());
                        options.Converters.Add(new AssetConverter());

                        var combinedJson = new
                        {
                            name = indexData.Name,
                            logicType = indexData.LogicType,
                            visualizationType = indexData.VisualizationType,
                            assets = assetData,
                            logic = logicData,
                            visualizations = visualizations,
                            spritesheet = spriteBundle.Spritesheet
                        };

                        string jsonContent = JsonSerializer.Serialize(combinedJson, options);
                        await File.WriteAllTextAsync(jsonOutputPath, jsonContent);

                        // Bundle the files into a .nitro file
                        await BundleNitroFileAsync(fileOutputDirectory, fileName, OutputDirectory);
                        nitroFilesGenerated++;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error generating sprite sheet for {fileName}: {ex.Message}");
                        Console.ResetColor();
                    }
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

            // Add JSON file
            string jsonFilePath = Path.Combine(outputDirectory, $"{fileName}.json");
            byte[] jsonData = await File.ReadAllBytesAsync(jsonFilePath);
            nitroBundler.AddFile($"{fileName}.json", jsonData);

            // Add image file
            string imageFilePath = Path.Combine(outputDirectory, $"{fileName}.png");
            byte[] imageData = await File.ReadAllBytesAsync(imageFilePath);
            nitroBundler.AddFile($"{fileName}.png", imageData);

            // Generate .nitro file
            byte[] nitroData = await nitroBundler.ToBufferAsync();
            string nitroFilePath = Path.Combine(nitroOutputDirectory, $"{fileName}.nitro");

            // Write the nitro file
            await File.WriteAllBytesAsync(nitroFilePath, nitroData);

            // Clean up the temporary directory
            Directory.Delete(outputDirectory, recursive: true);
            Console.WriteLine($"Generated {fileName}.nitro -> {nitroFilePath}");
        }

        public class SpriteBundle
        {
            public SpriteSheetMapper.SpriteSheetData Spritesheet { get; set; }
            public ImageData ImageData { get; set; }
        }

        public class ImageData
        {
            public string Name { get; set; }
            public byte[] Buffer { get; set; }
        }
    }
}
