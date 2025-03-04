﻿using System.Text.Json;
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
            string swfFileName,
            string swfOutputDirectory)
        {
            try
            {
                if (imageSources == null) imageSources = new Dictionary<string, string>();

                if (!File.Exists(assetsFilePath) || !File.Exists(manifestFilePath))
                {
                    Console.WriteLine("❌ Error: Assets or manifest file not found.");
                    return new Dictionary<string, Asset>();
                }

                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);

                var assets = MapAssetsXML(XElement.Parse(assetsContent), XElement.Parse(manifestContent), imageSources, debugXmlPath);
                await BuildMappingsInMemoryAsync(assets, debugXmlPath, imageSources);

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
                Console.WriteLine($"❌ Error parsing assets or manifest file: {ex.Message}");
                return new Dictionary<string, Asset>();
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
                var assetMappingLines = new List<string>();
                var imageMapping = new Dictionary<string, string>();
                var sourceMap = new Dictionary<string, string>();
                var idTracker = new HashSet<string>();

                foreach (var kvp in tagMappings)
                {
                    string tagId = kvp.Key;
                    foreach (var tagName in kvp.Value)
                    {
                        string cleanedName = RemoveSwfPrefix(tagName, tagMappings.ContainsKey("0") ? tagMappings["0"].FirstOrDefault() ?? "" : "");
                        if (Regex.IsMatch(cleanedName, "_32_|visualization|logic|index|assets|manifest$")) continue;

                        if (!idTracker.Contains(tagId))
                        {
                            idTracker.Add(tagId);
                            sourceMap[tagId] = cleanedName;
                        }
                        assetMappingLines.Add($"{tagId},{cleanedName}");
                    }
                }
                await UpdateAssetsWithSourceFromCsvLinesAsync(assets, assetMappingLines.Skip(1), imageSources, sourceMap);
                LatestImageMapping = sourceMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error building in-memory mappings: {ex.Message}");
            }
        }

        private static async Task UpdateAssetsWithSourceFromCsvLinesAsync(
            Dictionary<string, Asset> assets,
            IEnumerable<string> csvLines,
            Dictionary<string, string> imageSources,
            Dictionary<string, string> sourceMap)
        {
            foreach (var line in csvLines)
            {
                var parts = line.Split(',');
                if (parts.Length != 2) continue;
                string id = parts[0];
                string name = parts[1].ToLowerInvariant();

                if (!assets.ContainsKey(name)) continue;

                if (sourceMap.TryGetValue(id, out string source))
                {
                    assets[name].Source = assets.ContainsKey(source) ? source : imageSources.GetValueOrDefault(name, source);
                }
            }
            await Task.CompletedTask;
        }

        private static Dictionary<string, Asset> MapAssetsXML(
            XElement root,
            XElement manifestRoot,
            Dictionary<string, string> imageSources,
            string debugXmlPath)
        {
            var output = new Dictionary<string, Asset>();
            var manifestAssetNames = manifestRoot.Descendants("asset")
                .Where(a => a.Attribute("mimeType")?.Value == "image/png")
                .Select(a => a.Attribute("name")?.Value?.ToLowerInvariant() ?? "")
                .Where(name => !name.Contains("_32_"))
                .ToHashSet();

            foreach (var assetElement in root.Elements("asset"))
            {
                string assetName = assetElement.Attribute("name")?.Value?.ToLowerInvariant() ?? "";
                if (assetName.Contains("_32_")) continue;

                if (!manifestAssetNames.Contains(assetName) && assetElement.Attribute("source") == null)
                    continue;

                output[assetName] = new Asset
                {
                    Source = assetElement.Attribute("source")?.Value?.ToLowerInvariant(),
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1",
                    usesPalette = assetElement.Attribute("usesPalette")?.Value == "1"
                };
            }

            var debugMapping = DebugXmlParser.ParseDebugXml(debugXmlPath)
                .ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value.ToLowerInvariant());

            foreach (var kv in debugMapping)
            {
                if (output.ContainsKey(kv.Key) && string.IsNullOrEmpty(output[kv.Key].Source))
                {
                    output[kv.Key].Source = kv.Value;
                }
            }

            return output;
        }

        public static string RemoveSwfPrefix(string name, string swfPrefix)
        {
            return string.IsNullOrEmpty(name) || string.IsNullOrEmpty(swfPrefix)
                ? name
                : Regex.Replace(name, "^" + Regex.Escape(swfPrefix) + "_", "", RegexOptions.IgnoreCase);
        }

        private static string ResolveSource(Dictionary<string, Asset> assets, string source)
        {
            var visited = new HashSet<string>();
            while (!string.IsNullOrEmpty(source) &&
                   assets.TryGetValue(source, out var referencedAsset) &&
                   !string.IsNullOrEmpty(referencedAsset.Source))
            {
                string next = referencedAsset.Source;
                if (!visited.Add(source))
                {
                    return source;
                }
                if (visited.Contains(next))
                {
                    return source;
                }
                source = next;
            }
            return source;
        }

        public class Asset
        {
            [JsonPropertyName("source")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string? Source { get; set; }
            [JsonPropertyName("x")]
            public int X { get; set; }
            [JsonPropertyName("y")]
            public int Y { get; set; }
            [JsonPropertyName("flipH")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool FlipH { get; set; }
            [JsonPropertyName("flipV")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool FlipV { get; set; }
            [JsonPropertyName("usesPalette")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public bool usesPalette { get; set; }
        }
    }
}
