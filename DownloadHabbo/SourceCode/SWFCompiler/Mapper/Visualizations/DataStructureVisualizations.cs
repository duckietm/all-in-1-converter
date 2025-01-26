using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public class Visualization
    {
        [JsonPropertyOrder(0)]
        [JsonPropertyName("angle")]
        public int Angle { get; set; }

        [JsonPropertyOrder(1)]
        [JsonPropertyName("layerCount")]
        public int LayerCount { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("layers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Layer> Layers { get; set; } = new();

        [JsonPropertyOrder(4)]
        [JsonPropertyName("directions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, object> Directions { get; set; } = new();

        [JsonPropertyOrder(5)]
        [JsonPropertyName("animations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Animation> Animations { get; set; } = new();

        [JsonPropertyOrder(6)]
        [JsonPropertyName("colors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Color> Colors { get; set; } = new();

        public Visualization(XElement xml)
        {
            Size = int.TryParse(xml.Attribute("size")?.Value, out int size) ? size : 0;
            LayerCount = int.TryParse(xml.Attribute("layerCount")?.Value, out int layerCount) ? layerCount : 0;
            Angle = int.TryParse(xml.Attribute("angle")?.Value, out int angle) ? angle : 0;

            Layers = ParseLayers(xml.Element("layers"));
            Directions = ParseDirections(xml.Element("directions"));
            Animations = ParseAnimations(xml.Element("animations"));
            Colors = ParseColors(xml.Element("colors"));

            if (Animations != null && Animations.Count == 0)
            {
                Animations = null;
            }
        }

        private Dictionary<int, Layer> ParseLayers(XElement layersElement)
        {
            var layers = new Dictionary<int, Layer>();
            if (layersElement == null) return layers;

            foreach (var layerElement in layersElement.Elements("layer"))
            {
                if (int.TryParse(layerElement.Attribute("id")?.Value, out int id))
                {
                    layers[id] = new Layer(layerElement);
                }
            }
            return layers;
        }

        private Dictionary<int, object> ParseDirections(XElement directionsElement)
        {
            var directions = new Dictionary<int, object>();
            if (directionsElement == null) return directions;

            foreach (var directionElement in directionsElement.Elements("direction"))
            {
                if (int.TryParse(directionElement.Attribute("id")?.Value, out int id))
                {
                    directions[id] = new object();
                }
            }
            return directions;
        }

        private Dictionary<int, Animation> ParseAnimations(XElement animationsElement)
        {
            var animations = new Dictionary<int, Animation>();
            if (animationsElement == null) return animations;

            foreach (var animationElement in animationsElement.Elements("animation"))
            {
                if (int.TryParse(animationElement.Attribute("id")?.Value, out int id))
                {
                    animations[id] = new Animation(animationElement);
                }
            }
            return animations;
        }

        private Dictionary<int, Color> ParseColors(XElement colorsElement)
        {
            var colors = new Dictionary<int, Color>();
            if (colorsElement == null) return colors;

            foreach (var colorElement in colorsElement.Elements("color"))
            {
                if (int.TryParse(colorElement.Attribute("id")?.Value, out int id))
                {
                    colors[id] = new Color(colorElement);
                }
            }
            return colors;
        }
    }

    public class Layer
    {
        [JsonPropertyName("ink")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Ink { get; set; }

        [JsonPropertyName("z")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Z { get; set; }

        [JsonPropertyName("ignoreMouse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IgnoreMouse { get; set; }

        public Layer(XElement xml)
        {
            Ink = xml.Attribute("ink")?.Value;
            Z = int.TryParse(xml.Attribute("z")?.Value, out int z) ? z : (int?)null;
            IgnoreMouse = xml.Attribute("ignoreMouse")?.Value == "1" ? true : (bool?)null;
        }
    }
}
