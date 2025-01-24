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

            if (imageSources != null)
            {
                if (asset.Source != null && imageSources.ContainsKey(asset.Source))
                {
                    asset.Source = imageSources[asset.Source];
                }

                if (imageSources.ContainsKey(name))
                {
                    asset.Source = imageSources[name];
                }
            }

            output[lowercaseName] = asset;
        }

        public class Asset
        {
            [JsonPropertyName("x")]
            public int X { get; set; }

            [JsonPropertyName("y")]
            public int Y { get; set; }

            [JsonPropertyName("source")]
            public string? Source { get; set; }

            [JsonIgnore]
            public bool FlipH { get; set; }

            [JsonIgnore]
            public bool FlipV { get; set; }
        }

        public static string SerializeToJson(Dictionary<string, Asset> assets)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(assets, options);
        }
    }
}