using System.Text.Json.Serialization;
using System.Xml.Linq;
using static Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations.Layer;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations
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

        private Dictionary<int, Layer> _layers = new();
        [JsonPropertyOrder(3)]
        [JsonPropertyName("layers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, Layer>? Layers
        {
            get => _layers.Count > 0 ? _layers : null;
            set => _layers = value ?? new();
        }

        private SortedDictionary<int, object> _directions = new();
        [JsonPropertyOrder(4)]
        [JsonPropertyName("directions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SortedDictionary<int, object>? Directions
        {
            get => _directions.Count > 0 ? _directions : null;
            set => _directions = value ?? new();
        }

        [JsonPropertyOrder(5)]
        [JsonPropertyName("animations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Animation> Animations { get; set; } = new();

        private Dictionary<int, Color> _colors = new();
        [JsonPropertyOrder(6)]
        [JsonPropertyName("colors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, Color>? Colors
        {
            get => _colors.Count > 0 ? _colors : null;
            set => _colors = value ?? new();
        }

        private PostureCollection? _postures;
        [JsonPropertyOrder(7)]
        [JsonPropertyName("postures")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PostureCollection? Postures
        {
            get => _postures != null && _postures.Postures.Count > 0 ? _postures : null;
            set => _postures = value;
        }

        private List<Gesture> _gestures = new();
        [JsonPropertyOrder(8)]
        [JsonPropertyName("gestures")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Gesture>? Gestures
        {
            get => _gestures.Count > 0 ? _gestures : null;
            set => _gestures = value ?? new();
        }

        public Visualization(XElement xml)
        {
            Size = int.TryParse(xml.Attribute("size")?.Value, out int size) ? size : 0;
            LayerCount = int.TryParse(xml.Attribute("layerCount")?.Value, out int layerCount) ? layerCount : 0;
            Angle = int.TryParse(xml.Attribute("angle")?.Value, out int angle) ? angle : 0;

            Layers = ParseLayers(xml.Element("layers"));
            Directions = ParseDirections(xml.Element("directions"));
            Animations = ParseAnimations(xml.Element("animations"));
            Colors = ParseColors(xml.Element("colors"));
            Postures = ParsePostures(xml.Element("postures"));
            Gestures = ParseGestures(xml.Element("gestures"));

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

        private SortedDictionary<int, object> ParseDirections(XElement directionsElement)
        {
            var directions = new SortedDictionary<int, object>();
            if (directionsElement == null) return directions;

            foreach (var directionElement in directionsElement.Elements("direction"))
            {
                if (int.TryParse(directionElement.Attribute("id")?.Value, out int id))
                {
                    var layers = new SortedDictionary<int, Layer>();
                    foreach (var layerElement in directionElement.Elements("layer"))
                    {
                        if (int.TryParse(layerElement.Attribute("id")?.Value, out int layerId))
                        {
                            layers[layerId] = new Layer(layerElement);
                        }
                    }

                    directions[id] = new { layers = layers };
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

        private PostureCollection ParsePostures(XElement posturesElement)
        {
            if (posturesElement == null) return null;

            var postures = new PostureCollection();

            // Check for default posture
            if (posturesElement.Attribute("defaultPosture") != null)
            {
                postures.DefaultPosture = posturesElement.Attribute("defaultPosture")?.Value;
            }

            // Extract posture list
            foreach (var posture in posturesElement.Elements("posture"))
            {
                postures.Postures.Add(new Posture(posture));
            }

            return postures;
        }

        private List<Gesture> ParseGestures(XElement gesturesElement)
        {
            if (gesturesElement == null) return null;

            var gestures = new List<Gesture>();

            foreach (var gesture in gesturesElement.Elements("gesture"))
            {
                gestures.Add(new Gesture(gesture));
            }

            return gestures;
        }
    }

    public class Layer
    {
        [JsonPropertyName("ink")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Ink { get; set; }

        [JsonPropertyName("alpha")]
        public int? Alpha { get; set; } // Always included, even if 0

        [JsonPropertyName("z")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Z { get; set; }

        [JsonPropertyName("x")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? X { get; set; }

        [JsonPropertyName("y")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? Y { get; set; }

        [JsonPropertyName("tag")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Tag { get; set; }

        [JsonPropertyName("ignoreMouse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IgnoreMouse { get; set; }

        public Layer(XElement xml)
        {
            Ink = xml.Attribute("ink")?.Value;
            Alpha = int.TryParse(xml.Attribute("alpha")?.Value, out int alpha) ? alpha : null;
            Z = int.TryParse(xml.Attribute("z")?.Value, out int z) ? z : (int?)null;
            X = int.TryParse(xml.Attribute("x")?.Value, out int x) ? x : (int?)null;
            Y = int.TryParse(xml.Attribute("y")?.Value, out int y) ? y : (int?)null;
            Tag = xml.Attribute("tag")?.Value;
            IgnoreMouse = xml.Attribute("ignoreMouse")?.Value == "1" ? true : (bool?)null;
        }

        public class Posture
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("animationId")]
            public int AnimationId { get; set; }

            public Posture(XElement xml)
            {
                Id = xml.Attribute("id")?.Value;
                AnimationId = int.TryParse(xml.Attribute("animationId")?.Value, out int animId) ? animId : 0;
            }
        }

        public class PostureCollection
        {
            [JsonPropertyName("defaultPosture")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string DefaultPosture { get; set; }

            [JsonPropertyName("postures")]
            public List<Posture> Postures { get; set; } = new();
        }

        public class Gesture
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("animationId")]
            public int AnimationId { get; set; }

            public Gesture(XElement xml)
            {
                Id = xml.Attribute("id")?.Value;
                AnimationId = int.TryParse(xml.Attribute("animationId")?.Value, out int animId) ? animId : 0;
            }
        }
    }
}