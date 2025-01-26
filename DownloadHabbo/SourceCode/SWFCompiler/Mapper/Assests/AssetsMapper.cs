using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Assests
{
    public static class AssetsMapper
    {
        public static async Task<Dictionary<string, Asset>> ParseAssetsFileAsync(string assetsFilePath, Dictionary<string, string> imageSources = null)
        {
            try
            {
                string assetsContent = await File.ReadAllTextAsync(assetsFilePath);
                XElement root = XElement.Parse(assetsContent);
                return MapAssetsXML(root, imageSources);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error parsing *_assets.bin: {ex.Message}");
                return null;
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static Dictionary<string, Asset> MapAssetsXML(XElement root, Dictionary<string, string> imageSources = null)
        {
            if (root == null) return null;

            var output = new Dictionary<string, Asset>();

            var assetElements = root.Elements("asset");
            foreach (var assetElement in assetElements)
            {
                MapAssetXML(assetElement, output, imageSources);
            }

            return output;
        }

        private static void MapAssetXML(XElement assetElement, Dictionary<string, Asset> output, Dictionary<string, string> imageSources = null)
        {
            if (assetElement == null || output == null) return;

            var name = assetElement.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) return;

            if (name.StartsWith("sh_") || name.Contains("_32_")) return;

            var lowercaseName = name.ToLowerInvariant();

            var asset = new Asset
            {
                X = int.TryParse(assetElement.Attribute("x")?.Value, out int x) ? x : 0,
                Y = int.TryParse(assetElement.Attribute("y")?.Value, out int y) ? y : 0,
                Source = assetElement.Attribute("source")?.Value,
                FlipH = assetElement.Attribute("flipH")?.Value == "1",
                FlipV = assetElement.Attribute("flipV")?.Value == "1"
            };

            if (asset.Source != null)
            {
                var sourceKey = asset.Source.ToLowerInvariant();
                if (output.ContainsKey(sourceKey))
                {
                    var sourceAsset = output[sourceKey];
                    asset.X = asset.X == 0 ? sourceAsset.X : asset.X;
                    asset.Y = asset.Y == 0 ? sourceAsset.Y : asset.Y;
                }
            }
            output[lowercaseName] = asset;
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
