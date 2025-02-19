using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.Assests
{
    public static class AssetsPetsMapper
    {
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

                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                XElement root = XElement.Parse(assetsContent);

                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                var assets = MapAssetsXML(root, manifestRoot, imageSources, debugXmlPath);
                await BuildMappingsInMemoryAsync(assets, debugXmlPath, imageSources);
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
            string debugXmlPath,
            Dictionary<string, string> imageSources)
        {
            try
            {
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);
                var assetMappingLines = new List<string> { "ID,Name" };
                var imageMapping = new Dictionary<string, string>();
                string swfPrefix = tagMappings.TryGetValue("0", out var swfNames) ? swfNames.FirstOrDefault() : "";
                if (string.IsNullOrEmpty(swfPrefix)) Console.WriteLine("⚠️ Warning: Unable to determine SWF file prefix (ID 0).");
                swfPrefix = swfPrefix ?? "";
                var idTracker = new HashSet<string>();

                foreach (var kvp in tagMappings)
                {
                    string tagId = kvp.Key;
                    List<string> originalTagNames = kvp.Value;
                    if (tagId == "0") continue;

                    foreach (var originalTagName in originalTagNames)
                    {
                        string cleanedName = RemoveSwfPrefix(originalTagName, swfPrefix);
                        if (cleanedName.Contains("_32_") ||
                            cleanedName.EndsWith("visualization") ||
                            cleanedName.EndsWith("logic") ||
                            cleanedName.EndsWith("index") ||
                            cleanedName.EndsWith("assets") ||
                            cleanedName.EndsWith("manifest")) continue;

                        assetMappingLines.Add($"{tagId},{cleanedName}");
                        if (!idTracker.Contains(tagId))
                        {
                            idTracker.Add(tagId);
                            imageMapping[tagId] = originalTagName;
                        }
                    }
                }
                await UpdateAssetsWithSourceFromCsvLinesAsync(assets, assetMappingLines.Skip(1), imageSources);
                LatestImageMapping = imageMapping;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error building in-memory mappings: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task UpdateAssetsWithSourceFromCsvLinesAsync(
            Dictionary<string, Asset> assets,
            IEnumerable<string> csvLines,
            Dictionary<string, string> imageSources)
        {
            var sourceMap = new Dictionary<string, string>();

            foreach (var line in csvLines)
            {
                var parts = line.Split(',');
                if (parts.Length != 2) continue;

                string id = parts[0];
                string name = parts[1].ToLowerInvariant();

                if (!assets.ContainsKey(name)) continue;

                if (sourceMap.ContainsKey(id))
                {
                    string source = sourceMap[id];

                    if (imageSources.ContainsKey(name))
                    {
                        assets[name].Source = imageSources[name];
                    }
                    else if (imageSources.ContainsKey(source))
                    {
                        assets[name].Source = imageSources[source];
                    }
                    else if (assets.ContainsKey(source) && assets[source].Source != null)
                    {
                        assets[name].Source = assets[source].Source;
                    }
                    else if (!source.Equals(name))
                    {
                        assets[name].Source = source;
                    }
                }
                else
                {
                    sourceMap[id] = name;
                }
            }
            // Fix incorrect `_0` mappings by finding correct priorities
            foreach (var asset in assets)
            {
                if (asset.Value.Source == asset.Key) // Prevent self-referencing
                {
                    var possibleSources = assets.Keys.Where(k => k.StartsWith(asset.Key.Substring(0, asset.Key.LastIndexOf('_')))).ToList();
                    if (possibleSources.Any())
                    {
                        asset.Value.Source = possibleSources.OrderBy(k => k).FirstOrDefault(); // Pick the best match
                    }
                }
            }

            await Task.CompletedTask;
        }
        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix)) return name;
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

            var manifestAssetNames = manifestRoot.Descendants("asset")
                .Where(asset => asset.Attribute("mimeType")?.Value == "image/png")
                .Select(asset => (asset.Attribute("name")?.Value ?? "").ToLowerInvariant())
                .Where(name => !name.Contains("_32_"))
                .ToHashSet();

            foreach (var assetElement in root.Elements("asset"))
            {
                string assetName = (assetElement.Attribute("name")?.Value ?? "").ToLowerInvariant();

                if (assetName.Contains("_32_"))
                    continue;

                // Include the asset if it is in the manifest OR it has a source attribute.
                if (!manifestAssetNames.Contains(assetName) && assetElement.Attribute("source") == null)
                    continue;

                var asset = new Asset
                {
                    Source = assetElement.Attribute("source")?.Value?.ToLowerInvariant(),
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1",
                    usesPalette = assetElement.Attribute("usesPalette")?.Value == "1"                    
                };

                output[assetName] = asset;
            }


            // Now apply the debug.xml mappings for assets that don't already have a source
            var debugMapping = DebugXmlParser.ParseDebugXml(debugXmlPath);
            var cleanedDebugMapping = debugMapping.ToDictionary(
                kv => kv.Key.ToLowerInvariant(),
                kv => kv.Value.ToLowerInvariant()
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

            [JsonPropertyOrder(5)]
            [JsonPropertyName("usesPalette")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool usesPalette { get; set; }
        }
    }
}
