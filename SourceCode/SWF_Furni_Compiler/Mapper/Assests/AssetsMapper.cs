using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class AssetsMapper
    {
        public static Dictionary<string, string> LatestImageMapping { get; private set; } = new Dictionary<string, string>();

        private static string ForceCFUpper(string name)
        {
            return Regex.Replace(name, @"(?<=^|_)(cf_)", "CF_", RegexOptions.IgnoreCase);
        }

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

                await BuildMappingsInMemoryAsync(assets, debugXmlPath);

                foreach (var asset in assets.Values)
                {
                    if (!string.IsNullOrEmpty(asset.Source))
                    {
                        asset.Source = ResolveSource(assets, asset.Source);
                    }
                }

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
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);

                var assetMappingLines = new List<string>();
                assetMappingLines.Add("ID,Name");

                var imageMapping = new Dictionary<string, string>();

                string swfPrefix = tagMappings.TryGetValue("0", out var swfNames) ? swfNames.FirstOrDefault() : "";
                if (string.IsNullOrEmpty(swfPrefix))
                {
                    Console.WriteLine("⚠️ Warning: Unable to determine SWF file prefix (ID 0).");
                    swfPrefix = "";
                }

                var idTracker = new HashSet<string>();

                foreach (var kvp in tagMappings)
                {
                    string tagId = kvp.Key;
                    List<string> originalTagNames = kvp.Value;

                    if (tagId == "0") continue;

                    foreach (var originalTagName in originalTagNames)
                    {
                        string cleanedName = RemoveSwfPrefix(originalTagName, swfPrefix);
                        cleanedName = ForceCFUpper(cleanedName);

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

                        assetMappingLines.Add($"{tagId},{cleanedName}");

                        if (!idTracker.Contains(tagId))
                        {
                            idTracker.Add(tagId);
                            imageMapping[tagId] = ForceCFUpper(originalTagName);
                        }
                    }
                }

                await UpdateAssetsWithSourceFromCsvLinesAsync(assets, assetMappingLines.Skip(1));

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
                    string name = ForceCFUpper(parts[1]);
                    if (sourceMap.ContainsKey(id))
                    {
                        string source = sourceMap[id];
                        if (assets.ContainsKey(name))
                        {
                            assets[name].Source = source;
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

        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix))
                return name;

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
                .Select(asset => ForceCFUpper(asset.Attribute("name")?.Value ?? ""))
                .Where(name => !name.Contains("_32_"))
                .ToHashSet();

            foreach (var assetElement in root.Elements("asset"))
            {
                string assetName = ForceCFUpper(assetElement.Attribute("name")?.Value ?? "");

                if (assetName.Contains("_32_"))
                    continue;

                if (!manifestAssetNames.Contains(assetName) && assetElement.Attribute("source") == null)
                    continue;

                var asset = new Asset
                {
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1",
                    Source = assetElement.Attribute("source") != null
                                ? ForceCFUpper(assetElement.Attribute("source").Value)
                                : null
                };

                output[assetName] = asset;
            }

            var debugMapping = DebugXmlParser.ParseDebugXml(debugXmlPath);
            var cleanedDebugMapping = debugMapping.ToDictionary(
                kv => ForceCFUpper(kv.Key),
                kv => ForceCFUpper(kv.Value)
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

            string pattern = @"^[^_]+_";
            return Regex.Replace(name, pattern, "", RegexOptions.None);
        }

        private static string ResolveSource(Dictionary<string, Asset> assets, string source)
        {
            var visited = new HashSet<string>();
            while (!string.IsNullOrEmpty(source) &&
                   assets.TryGetValue(source, out var referencedAsset) &&
                   !string.IsNullOrEmpty(referencedAsset.Source))
            {
                if (!visited.Add(source))
                    break;
                source = referencedAsset.Source;
            }
            return source;
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
