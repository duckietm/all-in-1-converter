using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class AssetsMapper
    {
        public static async Task<Dictionary<string, string>> LoadImageSourcesFromCSV(string csvFilePath)
        {
            var imageSources = new Dictionary<string, string>();

            try
            {
                if (!File.Exists(csvFilePath))
                {
                    Console.WriteLine($"❌ Error: CSV file not found: {csvFilePath}");
                    return imageSources; // Return empty dictionary if missing
                }

                string[] lines = await File.ReadAllLinesAsync(csvFilePath);

                Console.WriteLine("\n🔍 DEBUG: Parsing Symbols CSV for Image Sources...");

                foreach (var line in lines)
                {
                    var parts = line.Split(';');
                    if (parts.Length < 2) continue;

                    string id = parts[0].Trim();
                    string name = parts[1].Trim();

                    if (!imageSources.ContainsKey(name))
                    {
                        imageSources[name] = id;
                        Console.WriteLine($"✅ Mapped: ID {id} -> {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading CSV: {ex.Message}");
            }

            return imageSources;
        }

        public static async Task<Dictionary<string, Asset>> ParseAssetsFileAsync(
    string assetsFilePath, Dictionary<string, string> imageSources, string manifestFilePath)
        {
            try
            {
                if (imageSources == null)
                {
                    Console.WriteLine("⚠️ WARNING: imageSources is null. Initializing an empty dictionary.");
                    imageSources = new Dictionary<string, string>(); // Prevent null reference
                }

                if (!File.Exists(assetsFilePath))
                {
                    Console.WriteLine($"❌ Error: Assets file not found: {assetsFilePath}");
                    return new Dictionary<string, Asset>(); // Return an empty dictionary to avoid crashing
                }

                if (!File.Exists(manifestFilePath))
                {
                    Console.WriteLine($"❌ Error: Manifest file not found: {manifestFilePath}");
                    return new Dictionary<string, Asset>(); // Return empty if manifest is missing
                }

                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                XElement root = XElement.Parse(assetsContent);

                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestRoot = XElement.Parse(manifestContent);

                // Debugging: Print imageSources to verify it's correctly populated
                Console.WriteLine("\n🔍 DEBUG: Image Sources Mapping:");
                foreach (var kvp in imageSources)
                {
                    Console.WriteLine($"ID: {kvp.Key} -> Asset Name: {kvp.Value}");
                }

                return MapAssetsXML(root, manifestRoot, imageSources);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error parsing assets or manifest file: {ex.Message}");
                return new Dictionary<string, Asset>(); // Prevent crashing
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static Dictionary<string, Asset> MapAssetsXML(XElement root, XElement manifestRoot, Dictionary<string, string> imageSources)
        {
            if (root == null || manifestRoot == null) return null;

            var output = new Dictionary<string, Asset>();
            var manifestSourceMap = new Dictionary<string, string>(); // Stores correct source mappings

            Console.WriteLine("\nProcessing Assets from XML using Manifest Order...\n");

            // ✅ Extract asset order & source relationships from the manifest
            var manifestAssets = manifestRoot.Descendants("asset")
                .Where(asset => asset.Attribute("mimeType")?.Value == "image/png")
                .Select(asset => asset.Attribute("name")?.Value.ToLowerInvariant())
                .ToList();

            string lastMainAsset = null;  // Keeps track of last "Main" asset for source assignment

            // 🔹 First pass: Store all assets & identify source relationships
            foreach (var assetElement in manifestRoot.Descendants("asset"))
            {
                var assetName = assetElement.Attribute("name")?.Value.ToLowerInvariant();
                if (assetName == null) continue;

                // **🔹 Extract source relationship**
                var nextSibling = assetElement.NextNode?.ToString().Trim();
                if (!string.IsNullOrEmpty(nextSibling))
                {
                    if (nextSibling.Contains(" - Main"))
                    {
                        lastMainAsset = assetName;
                        Console.WriteLine($"{assetName} is a Main asset.");
                    }
                    else if (nextSibling.Contains(" - source") && lastMainAsset != null)
                    {
                        manifestSourceMap[assetName] = lastMainAsset;
                        Console.WriteLine($"{assetName} should use {lastMainAsset} as its source.");
                    }
                }
            }

            // 🔹 Second pass: Assign each asset, linking sources
            foreach (var assetName in manifestAssets)
            {
                var assetElement = root.Elements("asset")
                    .FirstOrDefault(a => a.Attribute("name")?.Value.ToLowerInvariant() == assetName);

                if (assetElement == null) continue;

                var asset = new Asset
                {
                    X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                    Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                    FlipH = assetElement.Attribute("flipH")?.Value == "1",
                    FlipV = assetElement.Attribute("flipV")?.Value == "1"
                };

                // ✅ Assign source if found in the manifest mapping
                if (manifestSourceMap.TryGetValue(assetName, out string sourceName) && output.ContainsKey(sourceName))
                {
                    asset.Source = sourceName;
                    Console.WriteLine($"{assetName} correctly assigned Source: {sourceName}");
                }

                output[assetName] = asset;
            }

            Console.WriteLine("\nAsset Dictionary Generated with Correct Sources:\n");
            foreach (var kvp in output)
            {
                string sourceText = kvp.Value.Source != null ? $"Source: {kvp.Value.Source}" : "No Source";
                Console.WriteLine($"  - {kvp.Key}: X={kvp.Value.X}, Y={kvp.Value.Y}, {sourceText}");
            }

            return output;
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

        public static string SerializeToJson(Dictionary<string, Asset> assets)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            options.Converters.Add(new AssetConverter());

            return JsonSerializer.Serialize(assets, options);
        }
    }

    public class AssetConverter : JsonConverter<AssetsMapper.Asset>
    {
        public override AssetsMapper.Asset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, AssetsMapper.Asset value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.Source != null)
            {
                writer.WritePropertyName("source");
                writer.WriteStringValue(value.Source);
            }

            writer.WritePropertyName("x");
            writer.WriteNumberValue(value.X);

            writer.WritePropertyName("y");
            writer.WriteNumberValue(value.Y);

            if (value.FlipH)
            {
                writer.WritePropertyName("flipH");
                writer.WriteBooleanValue(value.FlipH);
            }

            if (value.FlipV)
            {
                writer.WritePropertyName("flipV");
                writer.WriteBooleanValue(value.FlipV);
            }

            writer.WriteEndObject();
        }
    }
}
