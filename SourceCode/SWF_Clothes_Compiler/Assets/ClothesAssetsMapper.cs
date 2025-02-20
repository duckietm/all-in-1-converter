using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class ClothesAssetsMapper
    {
        // In-memory image mapping (ID → original tag name)
        public static Dictionary<string, string> LatestImageMapping { get; private set; } = new Dictionary<string, string>();

        // Updated Asset class now includes a Source property.
        public class Asset
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }

            [JsonPropertyName("source")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Source { get; set; }
        }

        // This method returns a tuple containing the library name and the asset dictionary.
        public static async Task<(string LibraryName, Dictionary<string, Asset> Assets)> ParseAssetsFileAsync(
            string assetsFilePath,
            Dictionary<string, string> imageSources,
            string manifestFilePath,
            string debugXmlPath,
            string swfOutputDirectory)
        {
            try
            {
                if (!File.Exists(manifestFilePath))
                {
                    Console.WriteLine($"❌ Error: Manifest file not found: {manifestFilePath}");
                    return ("", new Dictionary<string, Asset>());
                }

                // Read and parse the manifest file.
                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                // Build the asset dictionary from the manifest.
                var assets = MapAssetsFromManifest(manifestRoot);

                // Extract the library name.
                string libraryName = manifestRoot.Element("library")?.Attribute("name")?.Value ?? "";

                // Update the assets with their source information using debug XML.
                await BuildMappingsInMemoryAsync(assets, debugXmlPath);

                // Update the in-memory image mapping.
                LatestImageMapping = assets.ToDictionary(kvp => kvp.Key, kvp => kvp.Key);

                return (libraryName, assets);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error parsing manifest file: {ex.Message}");
                return ("", new Dictionary<string, Asset>());
            }
            finally
            {
                Console.ResetColor();
            }
        }

        // This method reads the manifest XML and extracts asset offset data.
        private static Dictionary<string, Asset> MapAssetsFromManifest(XElement manifestRoot)
        {
            var output = new Dictionary<string, Asset>();

            var libraryElement = manifestRoot.Element("library");
            if (libraryElement == null)
            {
                Console.WriteLine("❌ No <library> element found in manifest.");
                return output;
            }

            var assetsElement = libraryElement.Element("assets");
            if (assetsElement == null)
            {
                Console.WriteLine("❌ No <assets> element found in manifest.");
                return output;
            }

            foreach (var assetElement in assetsElement.Elements("asset"))
            {
                if (assetElement.Attribute("mimeType")?.Value != "image/png")
                    continue;

                string assetName = assetElement.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(assetName))
                    continue;

                // ✅ Skip assets that should be ignored
                if (assetName.StartsWith("sh_") || assetName.Contains("_32_"))
                    continue;

                var paramElement = assetElement.Elements("param")
                                               .FirstOrDefault(p => p.Attribute("key")?.Value == "offset");
                if (paramElement == null)
                    continue;

                string offsetValue = paramElement.Attribute("value")?.Value;
                if (string.IsNullOrEmpty(offsetValue))
                    continue;

                var parts = offsetValue.Split(',');
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0].Trim(), out int x) ||
                    !int.TryParse(parts[1].Trim(), out int y))
                {
                    continue;
                }

                // ✅ Asset name is kept **exactly** as in the manifest (no lowercasing)
                output[assetName] = new Asset { X = x, Y = y };
            }

            return output;
        }


        // Builds the in-memory mappings so that each asset gets updated with its source.
        public static async Task BuildMappingsInMemoryAsync(
            Dictionary<string, Asset> assets,
            string debugXmlPath)
        {
            try
            {
                // Extract tag mappings from the debug XML.
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);

                // Build a list of mapping lines in the format "ID,Name".
                var assetMappingLines = new List<string>();
                assetMappingLines.Add("ID,Name"); // header

                // Dictionary for image mapping (ID → original tag name).
                var imageMapping = new Dictionary<string, string>();

                // Get SWF prefix from tag "0".
                string swfPrefix = tagMappings.TryGetValue("0", out var swfNames) ? swfNames.FirstOrDefault() : "";
                if (string.IsNullOrEmpty(swfPrefix))
                {
                    Console.WriteLine("⚠️ Warning: Unable to determine SWF file prefix (ID 0).");
                    swfPrefix = "";
                }

                var idTracker = new HashSet<string>();

                // Loop through tag mappings (skip ID "0").
                foreach (var kvp in tagMappings)
                {
                    string tagId = kvp.Key;
                    List<string> originalTagNames = kvp.Value;

                    if (tagId == "0") continue;

                    foreach (var originalTagName in originalTagNames)
                    {
                        // Remove the SWF prefix from the tag name.
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

                        // Add mapping line "tagId,cleanedName".
                        assetMappingLines.Add($"{tagId},{cleanedName}");

                        // Build the image mapping using the original tag name.
                        if (!idTracker.Contains(tagId))
                        {
                            idTracker.Add(tagId);
                            imageMapping[tagId] = originalTagName;
                        }
                    }
                }

                // Update the assets with source values from the mapping lines.
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

        // This method updates each asset's Source property based on CSV lines.
        private static Task UpdateAssetsWithSourceFromCsvLinesAsync(Dictionary<string, Asset> assets, IEnumerable<string> csvLines)
        {
            var sourceMap = new Dictionary<string, string>();

            foreach (var line in csvLines)
            {
                var parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string id = parts[0];
                    string name = parts[1]; // ✅ Keep original casing

                    if (sourceMap.ContainsKey(id))
                    {
                        string source = sourceMap[id];
                        if (assets.ContainsKey(name))
                        {
                            assets[name].Source = source; // ✅ Correct source mapping
                        }
                    }
                    else
                    {
                        sourceMap[id] = name;
                    }
                }
            }

            return Task.CompletedTask;
        }

        // Removes the SWF prefix (the SWF name followed by an underscore) from the asset name.
        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix))
                return name;

            string pattern = $"^{Regex.Escape(swfPrefix)}_";
            return Regex.Replace(name, pattern, "", RegexOptions.IgnoreCase);
        }
    }
}
