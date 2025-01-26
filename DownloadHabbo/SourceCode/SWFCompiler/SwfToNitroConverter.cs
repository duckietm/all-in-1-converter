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
    public static class SwfToNitroConverter
    {
        private const string ImportDirectory = @"SWFCompiler\import";
        private const string OutputDirectory = @"SWFCompiler\output";

        public static async Task ConvertSwfFilesAsync()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);

                string[] swfFiles = Directory.GetFiles(ImportDirectory, "*.swf", SearchOption.TopDirectoryOnly);

                if (swfFiles.Length == 0)
                {
                    Console.WriteLine("No SWF files found in the import directory.");
                    return;
                }

                Console.WriteLine($"Found {swfFiles.Length} SWF files. Starting conversion...");

                foreach (string swfFile in swfFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(swfFile);
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
                        Console.WriteLine($"Using assets file: {assetsFilePath}");
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

                    var imageFiles = Directory.GetFiles(imageOutputPath, "*.png", SearchOption.TopDirectoryOnly);
                    var images = new Dictionary<string, Bitmap>();
                    foreach (var imageFile in imageFiles)
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imageFile);
                        images[imageName] = new Bitmap(imageFile);
                    }

                    var (spriteSheetPath, spriteSheetData) = SpriteSheetMapper.GenerateSpriteSheet(images, fileOutputDirectory, fileName);

                    if (spriteSheetPath == null || spriteSheetData == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"No images found to generate spritesheet for {fileName}. Skipping spritesheet generation.");
                        Console.ResetColor();
                    }

                    var combinedJson = new
                    {
                        name = indexData.Name,
                        logicType = indexData.LogicType,
                        visualizationType = indexData.VisualizationType,
                        assets = assetData,
                        logic = logicData,
                        visualizations = visualizations,
                        spriteSheet = spriteSheetData
                    };

                    // Generate {name}.json
                    string jsonOutputPath = Path.Combine(fileOutputDirectory, fileName + ".json");

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                    };

                    options.Converters.Add(new AssetConverter());

                    string jsonContent = JsonSerializer.Serialize(combinedJson, options);
                    await File.WriteAllTextAsync(jsonOutputPath, jsonContent);

                    Console.WriteLine($"Generated {fileName}.json -> {jsonOutputPath}");
                }

                Console.WriteLine("All SWF files have been converted successfully.");
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
    }
}
