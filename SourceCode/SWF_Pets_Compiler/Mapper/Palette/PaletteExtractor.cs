using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.palette
{
    public static class PaletteExtractor
    {
        public static Dictionary<int, PaletteData> ExtractPalettes(string binaryOutputPath)
        {
            var assetsFile = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_assets.bin", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (assetsFile == null)
            {
                Console.WriteLine("❌ No _assets.bin file found.");
                return new Dictionary<int, PaletteData>();
            }

            XElement assetsRoot = XElement.Load(assetsFile);
            var allBinFiles = Directory.GetFiles(Path.Combine(binaryOutputPath, "binaryData"), "*_*.bin", SearchOption.TopDirectoryOnly);

            var palettes = new Dictionary<int, PaletteData>();

            foreach (var palette in assetsRoot.Elements("palette"))
            {
                int id = int.Parse(palette.Attribute("id")?.Value ?? "-1");
                if (id == -1) continue;

                string source = palette.Attribute("source")?.Value ?? "unknown";
                bool master = bool.TryParse(palette.Attribute("master")?.Value, out bool isMaster) && isMaster;
                string color1 = palette.Attribute("color1")?.Value;
                string color2 = palette.Attribute("color2")?.Value;
                string tags = palette.Attribute("tags")?.Value ?? "";
                int? breed = palette.Attribute("breed") != null ? int.Parse(palette.Attribute("breed").Value) : (int?)null;
                int? colorTag = palette.Attribute("colortag") != null ? int.Parse(palette.Attribute("colortag").Value) : (int?)null;

                string paletteFilePath = allBinFiles.FirstOrDefault(f => f.EndsWith($"_{source}.bin"));

                List<List<int>> colors;
                if (paletteFilePath == null)
                {
                    Console.WriteLine($"⚠️ Palette {id} ({source}): colour data '_{source}.bin' not found — using a default one-colour palette so the pet renders instead of turning black.");
                    colors = CreateDefaultRamp();
                    if (string.IsNullOrEmpty(color1)) color1 = DEFAULT_COLOR_HEX;
                }
                else
                {
                    colors = ReadPaletteFile(paletteFilePath);
                }

                palettes[id] = new PaletteData
                {
                    Id = id,
                    Source = source,
                    Master = master,
                    Tags = tags.Split(',').Where(t => !string.IsNullOrEmpty(t)).ToList(),
                    Breed = breed,
                    ColorTag = colorTag,
                    Color1 = color1,
                    Color2 = color2,
                    RGB = colors
                };

                Console.WriteLine($"✅ Loaded palette {id}: {source} | Color1: {color1}, Color2: {color2}, RGB count: {colors.Count}");
            }

            if (palettes.Count == 0)
            {
                Console.WriteLine("⚠️ No palettes found in SWF — injecting a default one-colour palette (id 0) so the pet renders instead of turning black.");
                palettes[0] = new PaletteData
                {
                    Id = 0,
                    Source = "default",
                    Master = true,
                    Tags = new List<string>(),
                    Color1 = DEFAULT_COLOR_HEX,
                    RGB = CreateDefaultRamp()
                };
            }

            return palettes;
        }

        private const string DEFAULT_COLOR_HEX = "999999";

        private static List<List<int>> CreateDefaultRamp()
        {
            var ramp = new List<List<int>>(256);
            for (int i = 0; i < 256; i++) ramp.Add(new List<int> { 0x99, 0x99, 0x99 });
            return ramp;
        }

        private static List<List<int>> ReadPaletteFile(string filePath)
        {
            var paletteColors = new List<List<int>>();
            byte[] binaryData = File.ReadAllBytes(filePath);

            for (int i = 0; i + 2 < binaryData.Length; i += 3)
            {
                int r = binaryData[i];
                int g = binaryData[i + 1];
                int b = binaryData[i + 2];

                paletteColors.Add(new List<int> { r, g, b });
            }

            return paletteColors;
        }
    }

    public class PaletteData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("master")]
        public bool Master { get; set; } = false;

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonPropertyName("breed")]
        public int? Breed { get; set; } = null;

        [JsonPropertyName("colorTag")]
        public int? ColorTag { get; set; } = null;

        [JsonPropertyName("color1")]
        public string Color1 { get; set; }

        [JsonPropertyName("color2")]
        public string Color2 { get; set; }

        [JsonPropertyName("rgb")]
        [JsonConverter(typeof(CompactRgbConverter))]
        public List<List<int>> RGB { get; set; }
    }
}
