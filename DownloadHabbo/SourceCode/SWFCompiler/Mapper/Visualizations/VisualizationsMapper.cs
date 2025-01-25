using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public static class VisualizationsMapper
    {
        private const int AllowedSize = 64; // Only include visualizations with size 64

        public static VisualizationData MapVisualizationsXml(XElement root)
        {
            if (root == null) return null;

            var type = root.Attribute("type")?.Value;
            var visualizations = new List<Visualization>();

            var graphicsElements = root.Elements("graphics");
            foreach (var graphicsElement in graphicsElements)
            {
                var visualizationElements = graphicsElement.Elements("visualization");
                foreach (var visualizationElement in visualizationElements)
                {
                    var visualization = new Visualization(visualizationElement);

                    // Include only if size is 64
                    if (visualization.Size == AllowedSize)
                    {
                        visualizations.Add(visualization);
                    }
                }
            }

            return new VisualizationData
            {
                Type = type,
                Visualizations = visualizations
            };
        }

        public class VisualizationData
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("visualizations")]
            public List<Visualization> Visualizations { get; set; }
        }

        public class Visualization
        {
            [JsonPropertyName("size")]
            public int Size { get; set; }

            [JsonPropertyName("layerCount")]
            public int LayerCount { get; set; }

            [JsonPropertyName("angle")]
            public int Angle { get; set; }

            [JsonPropertyName("layers")]
            public List<Layer> Layers { get; set; }

            [JsonPropertyName("directions")]
            public List<Direction> Directions { get; set; }

            [JsonPropertyName("colors")]
            public List<Color> Colors { get; set; }

            [JsonPropertyName("animations")]
            public List<Animation> Animations { get; set; }

            public Visualization(XElement xml)
            {
                Size = int.TryParse(xml.Attribute("size")?.Value, out int size) ? size : 0;
                LayerCount = int.TryParse(xml.Attribute("layerCount")?.Value, out int layerCount) ? layerCount : 0;
                Angle = int.TryParse(xml.Attribute("angle")?.Value, out int angle) ? angle : 0;

                Layers = ParseLayers(xml.Element("layers"));
                Directions = ParseDirections(xml.Element("directions"));
                Colors = ParseColors(xml.Element("colors"));
                Animations = ParseAnimations(xml.Element("animations"));
            }

            private List<Layer> ParseLayers(XElement layersElement)
            {
                var layers = new List<Layer>();
                if (layersElement == null) return layers;

                foreach (var layerElement in layersElement.Elements("layer"))
                {
                    layers.Add(new Layer(layerElement));
                }

                return layers;
            }

            private List<Direction> ParseDirections(XElement directionsElement)
            {
                var directions = new List<Direction>();
                if (directionsElement == null) return directions;

                foreach (var directionElement in directionsElement.Elements("direction"))
                {
                    directions.Add(new Direction(directionElement));
                }

                return directions;
            }

            private List<Color> ParseColors(XElement colorsElement)
            {
                var colors = new List<Color>();
                if (colorsElement == null) return colors;

                foreach (var colorElement in colorsElement.Elements("color"))
                {
                    colors.Add(new Color(colorElement));
                }

                return colors;
            }

            private List<Animation> ParseAnimations(XElement animationsElement)
            {
                var animations = new List<Animation>();
                if (animationsElement == null) return animations;

                foreach (var animationElement in animationsElement.Elements("animation"))
                {
                    animations.Add(new Animation(animationElement));
                }

                return animations;
            }
        }

        public class Layer
        {
            [JsonPropertyName("z")]
            public int Z { get; set; }

            public Layer(XElement xml)
            {
                Z = int.TryParse(xml.Attribute("z")?.Value, out int z) ? z : 0;
            }
        }

        public class Direction
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("layers")]
            public List<Layer> Layers { get; set; }

            public Direction(XElement xml)
            {
                Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
                Layers = new List<Layer>();

                foreach (var layerElement in xml.Elements("layer"))
                {
                    Layers.Add(new Layer(layerElement));
                }
            }
        }

        public class Color
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            public Color(XElement xml)
            {
                Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            }
        }

        public class Animation
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            public Animation(XElement xml)
            {
                Id = xml.Attribute("id")?.Value;
            }
        }
    }
}
