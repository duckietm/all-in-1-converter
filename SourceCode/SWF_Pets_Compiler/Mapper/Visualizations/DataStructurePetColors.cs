using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations
{
    public class Color
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyName("layers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, ColorLayer> Layers { get; set; } = new();

        public Color(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            Layers = ParseColorLayers(xml);
        }

        private Dictionary<int, ColorLayer> ParseColorLayers(XElement xml)
        {
            var colorLayers = new Dictionary<int, ColorLayer>();
            foreach (var colorLayerElement in xml.Elements("colorLayer"))
            {
                if (int.TryParse(colorLayerElement.Attribute("id")?.Value, out int id))
                {
                    var colorLayer = new ColorLayer(colorLayerElement);
                    colorLayers[id] = colorLayer;
                }
            }
            return colorLayers;
        }
    }

    public class ColorLayer
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }

        public ColorLayer(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;

            string hexColor = xml.Attribute("color")?.Value;
            if (!string.IsNullOrEmpty(hexColor))
            {
                Color = Convert.ToInt32(hexColor, 16);
            }
        }
    }
}
