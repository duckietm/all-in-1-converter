using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Habbo_Downloader.SWFCompiler.Mapper.Assests.AssetsMapper;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class AssetsMapper
    {
        public static async Task<Dictionary<string, Asset>> ParseAssetsFileAsync(
            string assetsFilePath,
            Dictionary<string, string> imageSources,
            string manifestFilePath,
            string debugXmlPath,
            string swfOutputDirectory)
        {
            try
            {
                if (imageSources == null)
                {
                    Console.WriteLine("⚠️ WARNING: imageSources is null. Initializing an empty dictionary.");
                    imageSources = new Dictionary<string, string>();
                }

                if (!File.Exists(assetsFilePath))
                {
                    Console.WriteLine($"Error: Assets file not found: {assetsFilePath}");
                    return new Dictionary<string, Asset>();
                }

                if (!File.Exists(manifestFilePath))
                {
                    Console.WriteLine($"Error: Manifest file not found: {manifestFilePath}");
                    return new Dictionary<string, Asset>();
                }

                // Read and parse XML
                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                XElement root = XElement.Parse(assetsContent);

                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                var assets = MapAssetsXML(root, manifestRoot, imageSources, debugXmlPath);

                // Generate CSVs
                await WriteAssetAndImageMappingsAsync(assets, debugXmlPath, swfOutputDirectory);

                return assets;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error parsing assets or manifest file: {ex.Message}");
                return new Dictionary<string, Asset>();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static async Task WriteAssetAndImageMappingsAsync(
    Dictionary<string, Asset> assets,
    string debugXmlPath,
    string outputDirectory)
        {
            try
            {
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);
                Console.WriteLine("\n🔍 DEBUG: Extracted Symbol Class Tags Mapping:");
                foreach (var kvp in tagMappings)
                {
                    Console.WriteLine($"Tag ID: {kvp.Key} -> Asset Names: {string.Join(",", kvp.Value)}");
                }

                string assetMappingPath = Path.Combine(outputDirectory, "asset_mapping.csv");
                string imageMappingPath = Path.Combine(outputDirectory, "image_mapping.csv");

                using (StreamWriter assetWriter = new StreamWriter(assetMappingPath, false))
                using (StreamWriter imageWriter = new StreamWriter(imageMappingPath, false))
                {
                    await assetWriter.WriteLineAsync("ID,Name");
                    await imageWriter.WriteLineAsync("ID,ImageFile");

                    // ✅ Extract SWF file name from ID 0
                    string swfPrefix = tagMappings.TryGetValue("0", out var swfNames) ? swfNames.FirstOrDefault() : null;

                    if (string.IsNullOrEmpty(swfPrefix))
                    {
                        Console.WriteLine("⚠️ Warning: Unable to determine SWF file prefix (ID 0).");
                        swfPrefix = "";  // Fallback to empty if missing
                    }
                    else
                    {
                        Console.WriteLine($"✅ Identified SWF Prefix: {swfPrefix}");
                    }

                    var idTracker = new HashSet<string>(); // Track IDs to identify duplicates

                    foreach (var kvp in tagMappings)
                    {
                        string tagId = kvp.Key.ToString(); // Treat ID as a string
                        List<string> originalTagNames = kvp.Value;

                        if (tagId == "0") continue;

                        foreach (var originalTagName in originalTagNames)
                        {
                            string cleanedName = RemoveSwfPrefix(originalTagName, swfPrefix);

                            if (cleanedName.Contains("_32_"))
                            {
                                Console.WriteLine($"⚠️ Skipping: {cleanedName} (contains '_32_')");
                                continue;
                            }

                            // ✅ Skip `_visualization`, `_logic`, `_index`, `_manifest`
                            if (cleanedName.EndsWith("visualization") ||
                                cleanedName.EndsWith("logic") ||
                                cleanedName.EndsWith("index") ||
                                cleanedName.EndsWith("assets") ||
                                cleanedName.EndsWith("manifest"))
                            {
                                continue;
                            }

                            Console.WriteLine($"✅ Writing: ID: {tagId}, Name: {cleanedName}");
                            await assetWriter.WriteLineAsync($"{tagId},{cleanedName}");

                            // 🔹 Write to image CSV (only write the first occurrence of each ID)
                            if (!idTracker.Contains(tagId))
                            {
                                idTracker.Add(tagId); // Mark this ID as processed
                                await imageWriter.WriteLineAsync($"{tagId},{cleanedName}");
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ asset_mapping.csv and image_mapping.csv successfully written to {outputDirectory}");

                // Read the CSV file and update the assets dictionary
                await UpdateAssetsWithSourceFromCsvAsync(assets, assetMappingPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error writing CSVs: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task UpdateAssetsWithSourceFromCsvAsync(Dictionary<string, Asset> assets, string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"Error: CSV file not found: {csvFilePath}");
                return;
            }

            var sourceMap = new Dictionary<string, string>();

            using (var reader = new StreamReader(csvFilePath))
            {
                // Skip the header line
                await reader.ReadLineAsync();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        string id = parts[0];
                        string name = parts[1].ToLowerInvariant(); // Normalize the name

                        if (sourceMap.ContainsKey(id))
                        {
                            // If the ID already exists, it means we have a source-object pair
                            string source = sourceMap[id];
                            if (assets.ContainsKey(name))
                            {
                                assets[name].Source = source;
                            }
                        }
                        else
                        {
                            // Store the name as a potential source for the next entry with the same ID
                            sourceMap[id] = name;
                        }
                    }
                }
            }

            Console.WriteLine("✅ Successfully updated assets with source information from CSV.");
        }

        /// <summary>
        /// Removes the SWF file name prefix dynamically (ID 0 value + `_`).
        /// </summary>
        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix)) return name;

            // Dynamically remove the SWF name followed by `_`
            string pattern = $"^{Regex.Escape(swfPrefix)}_";
            return Regex.Replace(name, pattern, "", RegexOptions.IgnoreCase);
        }



        private static Dictionary<string, Asset> MapAssetsXML(
    XElement root,
    XElement manifestRoot,
    Dictionary<string, string> imageSources,
    string debugXmlPath)
        {
            if (root == null || manifestRoot == null)
                return new Dictionary<string, Asset>();

            var output = new Dictionary<string, Asset>();

            var manifestAssets = manifestRoot.Descendants("asset")
                .Where(asset => asset.Attribute("mimeType")?.Value == "image/png")
                .Select(asset => (asset.Attribute("name")?.Value ?? "").ToLowerInvariant()) // Keep the full name
                .Where(name => !name.Contains("_32_")) // Exclude _32_ assets
                .ToList();

            var debugMapping = DebugXmlParser.ParseDebugXml(debugXmlPath);
            var cleanedDebugMapping = debugMapping.ToDictionary(
                kv => kv.Key.ToLowerInvariant(), // Keep the full name
                kv => kv.Value.ToLowerInvariant()
            );

            foreach (var assetKey in manifestAssets)
            {
                var assetElement = root.Elements("asset")
                    .FirstOrDefault(a => (a.Attribute("name")?.Value ?? "").ToLowerInvariant() == assetKey);

                if (assetElement == null)
                    continue;

                var asset = new Asset
                {
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1"
                };

                output[assetKey] = asset; // Use the full name as the key
            }

            foreach (var kv in cleanedDebugMapping)
            {
                if (output.ContainsKey(kv.Key))
                {
                    output[kv.Key].Source = kv.Value;
                }
            }

            return output;
        }

        public static string RemoveFirstPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Detect first prefix dynamically (first word before `_`)
            string pattern = @"^[^_]+_";

            // ✅ Replace only the first occurrence of the detected prefix
            return Regex.Replace(name, pattern, "", RegexOptions.None);
        }

        public class Asset
        {
            [JsonPropertyOrder(0)]
            [JsonPropertyName("source")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Source { get; set; }

            [JsonPropertyOrder(1)]
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyOrder(2)]
            [JsonPropertyName("y")]
            public int Y { get; set; }

            [JsonPropertyOrder(3)]
            [JsonPropertyName("flipH")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool FlipH { get; set; }

            [JsonPropertyOrder(4)]
            [JsonPropertyName("flipV")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool FlipV { get; set; }
        }
    }
}
