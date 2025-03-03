using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class AssetsMapper
    {
        // Holds the latest image mapping dictionary (ID → original tag name) built in memory.
        public static Dictionary<string, string> LatestImageMapping { get; private set; } = new Dictionary<string, string>();

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
                    Console.WriteLine($"❌ Error Assets file not found: {assetsFilePath}");
                    return new Dictionary<string, Asset>();
                }

                if (!File.Exists(manifestFilePath))
                {
                    Console.WriteLine($"❌ Error Manifest file not found: {manifestFilePath}");
                    return new Dictionary<string, Asset>();
                }

                // Read and parse XML files
                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                XElement root = XElement.Parse(assetsContent);

                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                var assets = MapAssetsXML(root, manifestRoot, imageSources, debugXmlPath);

                // Build the mappings in memory (both asset mapping for updating assets and image mapping for image renaming).
                await BuildMappingsInMemoryAsync(assets, debugXmlPath);

                return assets;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error parsing assets or manifest file: {ex.Message}");
                return new Dictionary<string, Asset>();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static async Task BuildMappingsInMemoryAsync(
            Dictionary<string, Asset> assets,
            string debugXmlPath)
        {
            try
            {
                // Get tag mappings from the debug XML.
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);

                // In-memory list for asset mapping lines (used to update assets).
                var assetMappingLines = new List<string>();
                assetMappingLines.Add("ID,Name"); // header (not used in processing, just for consistency)

                // In-memory dictionary for image mapping (ID → original tag name).
                var imageMapping = new Dictionary<string, string>();

                // Extract the SWF prefix from tag id "0"
                string swfPrefix = tagMappings.TryGetValue("0", out var swfNames) ? swfNames.FirstOrDefault() : "";
                if (string.IsNullOrEmpty(swfPrefix))
                {
                    Console.WriteLine("⚠️ Warning: Unable to determine SWF file prefix (ID 0).");
                    swfPrefix = "";
                }

                var idTracker = new HashSet<string>(); // to avoid duplicate image mappings

                // Loop through the tag mappings (skip ID "0")
                foreach (var kvp in tagMappings)
                {
                    string tagId = kvp.Key;
                    List<string> originalTagNames = kvp.Value;

                    if (tagId == "0") continue;

                    foreach (var originalTagName in originalTagNames)
                    {
                        // Remove the SWF prefix from the tag name for asset mapping.
                        string cleanedName = RemoveSwfPrefix(originalTagName, swfPrefix);

                        // Skip names with undesired parts.
                        if (cleanedName.Contains("_32_"))
                            continue;
                        if (cleanedName.EndsWith("visualization", StringComparison.OrdinalIgnoreCase) ||
                            cleanedName.EndsWith("logic", StringComparison.OrdinalIgnoreCase) ||
                            cleanedName.EndsWith("index", StringComparison.OrdinalIgnoreCase) ||
                            cleanedName.EndsWith("assets", StringComparison.OrdinalIgnoreCase) ||
                            cleanedName.EndsWith("manifest", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Build the asset mapping (in memory).
                        assetMappingLines.Add($"{tagId},{cleanedName}");

                        // Build the image mapping using the original tag name.
                        if (!idTracker.Contains(tagId))
                        {
                            idTracker.Add(tagId);
                            imageMapping[tagId] = originalTagName;
                        }
                    }
                }

                // Update the assets using the in-memory asset mapping lines.
                await UpdateAssetsWithSourceFromCsvLinesAsync(assets, assetMappingLines.Skip(1));

                // Store the in-memory image mapping.
                LatestImageMapping = imageMapping;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error building in-memory mappings: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static Task UpdateAssetsWithSourceFromCsvLinesAsync(
            Dictionary<string, Asset> assets, IEnumerable<string> csvLines)
        {
            var sourceMap = new Dictionary<string, string>();

            foreach (var line in csvLines)
            {
                var parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string id = parts[0];
                    string name = parts[1].ToLowerInvariant();

                    if (sourceMap.ContainsKey(id))
                    {
                        // When a duplicate ID is encountered, update the asset's Source property.
                        string source = sourceMap[id];
                        if (assets.ContainsKey(name))
                        {
                            assets[name].Source = source;
                        }
                    }
                    else
                    {
                        // Store the name for a future asset with the same ID.
                        sourceMap[id] = name;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix))
                return name;

            // Remove the SWF prefix (SWF name + underscore) dynamically.
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

            // Remove .ToLowerInvariant() here to keep the original case.
            var manifestAssetNames = manifestRoot.Descendants("asset")
                .Where(asset => asset.Attribute("mimeType")?.Value == "image/png")
                .Select(asset => asset.Attribute("name")?.Value ?? "")
                .Where(name => !name.Contains("_32_"))
                .ToHashSet();

            foreach (var assetElement in root.Elements("asset"))
            {
                // Remove .ToLowerInvariant() so the original name is kept.
                string assetName = assetElement.Attribute("name")?.Value ?? "";

                if (assetName.Contains("_32_"))
                    continue;

                // Include the asset if it is in the manifest OR it has a source attribute.
                if (!manifestAssetNames.Contains(assetName) && assetElement.Attribute("source") == null)
                    continue;

                var asset = new Asset
                {
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1",
                    // Keep the source as-is instead of converting it.
                    Source = assetElement.Attribute("source")?.Value
                };

                output[assetName] = asset;
            }

            // Now apply the debug.xml mappings for assets that don't already have a source
            var debugMapping = DebugXmlParser.ParseDebugXml(debugXmlPath);
            // Remove conversion here if you want to maintain original capitalization
            var cleanedDebugMapping = debugMapping.ToDictionary(
                kv => kv.Key, // removed .ToLowerInvariant()
                kv => kv.Value  // removed .ToLowerInvariant()
            );

            foreach (var kv in cleanedDebugMapping)
            {
                if (output.ContainsKey(kv.Key) && string.IsNullOrEmpty(output[kv.Key].Source))
                {
                    output[kv.Key].Source = kv.Value;
                }
            }

            return output;
        }

        public static string RemoveFirstPrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Remove the first prefix (the text up to and including the first underscore).
            string pattern = @"^[^_]+_";
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
