using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Effects_Compiler.Mapper.Assets
{
    public static class EffectAssetsMapper
    {
        public static Dictionary<string, string> LatestImageMapping { get; private set; } = new Dictionary<string, string>();

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

                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                var assets = MapAssetsFromManifest(manifestRoot);

                string libraryName = manifestRoot.Element("library")?.Attribute("name")?.Value ?? "";

                await BuildMappingsInMemoryAsync(assets, debugXmlPath);

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

                output[assetName] = new Asset { X = x, Y = y };
            }

            return output;
        }

        public static async Task BuildMappingsInMemoryAsync(
            Dictionary<string, Asset> assets,
            string debugXmlPath)
        {
            try
            {
                var tagMappings = DebugXmlParser.ExtractSymbolClassTags(debugXmlPath);

                var assetMappingLines = new List<string>();
                assetMappingLines.Add("ID,Name"); // header

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
                            imageMapping[tagId] = originalTagName;
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

        private static Task UpdateAssetsWithSourceFromCsvLinesAsync(Dictionary<string, Asset> assets, IEnumerable<string> csvLines)
        {
            var sourceMap = new Dictionary<string, string>();

            foreach (var line in csvLines)
            {
                var parts = line.Split(',');
                if (parts.Length == 2)
                {
                    string id = parts[0];
                    string name = parts[1];

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
    }
}
