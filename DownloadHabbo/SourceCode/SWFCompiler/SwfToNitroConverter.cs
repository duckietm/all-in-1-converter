using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Index;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using Habbo_Downloader.Tools;

using System.Text.Json;
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
                    Console.WriteLine($"Using index file: {indexFilePath}");

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
                    AssetsMapper.AssetData assetData = null;

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
                        Console.WriteLine($"Using logic file: {logicFilePath}");

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

                    // Combine data into JSON
                    var combinedJson = new
                    {
                        name = indexData.Name, // Use lowercase property name
                        logicType = indexData.LogicType, // Use lowercase property name
                        visualizationType = indexData.VisualizationType, // Use lowercase property name
                        assets = assetData != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(AssetsMapper.SerializeToJson(assetData)) : null,
                        logic = logicData // Include logic data if available
                    };

                    // Generate {name}.json
                    string jsonOutputPath = Path.Combine(fileOutputDirectory, fileName + ".json");
                    string jsonContent = System.Text.Json.JsonSerializer.Serialize(combinedJson, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
                    });
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