using System.Text.Json.Serialization;
using System.Text.Json;
using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations
{
    public class Animation
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyName("transitionTo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? TransitionTo { get; set; }

        [JsonPropertyName("transitionFrom")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? TransitionFrom { get; set; }

        [JsonPropertyName("immediateChangeFrom")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ImmediateChangeFrom { get; set; }

        [JsonPropertyName("randomStart")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool? RandomStart { get; set; }

        [JsonPropertyName("layers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, AnimationLayer> Layers { get; set; } = new();

        public Animation(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            TransitionTo = int.TryParse(xml.Attribute("transitionTo")?.Value, out int transitionTo) ? transitionTo : (int?)null;
            TransitionFrom = int.TryParse(xml.Attribute("transitionFrom")?.Value, out int transitionFrom) ? transitionFrom : (int?)null;
            ImmediateChangeFrom = xml.Attribute("immediateChangeFrom")?.Value;
            RandomStart = xml.Attribute("randomStart")?.Value == "1" ? true : (bool?)null;
            Layers = ParseAnimationLayers(xml);
        }

        private Dictionary<int, AnimationLayer> ParseAnimationLayers(XElement xml)
        {
            var animationLayers = new Dictionary<int, AnimationLayer>();
            foreach (var animationLayerElement in xml.Elements("animationLayer"))
            {
                if (int.TryParse(animationLayerElement.Attribute("id")?.Value, out int id))
                {
                    animationLayers[id] = new AnimationLayer(animationLayerElement);
                }
            }
            return animationLayers;
        }
    }

    public class AnimationLayer
    {
        [JsonPropertyName("frameRepeat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? FrameRepeat { get; set; }

        [JsonPropertyName("loopCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? LoopCount { get; set; }

        [JsonPropertyName("random")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? Random { get; set; }

        [JsonPropertyName("frameSequences")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, FrameSequence> FrameSequences { get; set; } = new();

        public AnimationLayer(XElement xml)
        {
            FrameRepeat = int.TryParse(xml.Attribute("frameRepeat")?.Value, out int frameRepeat) ? frameRepeat : (int?)null;
            LoopCount = int.TryParse(xml.Attribute("loopCount")?.Value, out int loopCount) ? loopCount : (int?)null;
            Random = int.TryParse(xml.Attribute("random")?.Value, out int random) ? random : (int?)null;
            FrameSequences = ParseFrameSequences(xml);

            if (FrameSequences.Count == 0)
            {
                FrameSequences = null;
            }
        }

        private Dictionary<int, FrameSequence> ParseFrameSequences(XElement xml)
        {
            var frameSequences = new Dictionary<int, FrameSequence>();
            int index = 0;

            foreach (var frameSequenceElement in xml.Elements("frameSequence"))
            {
                frameSequences[index++] = new FrameSequence(frameSequenceElement);
            }

            return frameSequences;
        }
    }

    public class FrameSequence
    {
        [JsonPropertyName("frames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Frame> Frames { get; set; } = new();

        public FrameSequence(XElement xml)
        {
            Frames = ParseFrames(xml);
        }

        private Dictionary<int, Frame> ParseFrames(XElement xml)
        {
            var frames = new Dictionary<int, Frame>();
            int index = 0;

            foreach (var frameElement in xml.Elements("frame"))
            {
                var frame = new Frame(frameElement);
                frames[index++] = frame;
            }

            return frames;
        }
    }

    [JsonConverter(typeof(FrameConverter))]
    public class Frame
    {
        public int Id { get; set; }
        public int? RandomX { get; set; }
        public int? RandomY { get; set; }
        public int? Y { get; set; }

        [JsonPropertyName("offsets")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<int, Offset> Offsets { get; set; } = new();

        public Frame(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            RandomX = int.TryParse(xml.Attribute("randomX")?.Value, out int randomX) ? randomX : (int?)null;
            RandomY = int.TryParse(xml.Attribute("randomY")?.Value, out int randomY) ? randomY : (int?)null;
            Y = int.TryParse(xml.Attribute("y")?.Value, out int y) ? y : (int?)null;

            Offsets = ParseOffsets(xml);
        }

        private Dictionary<int, Offset> ParseOffsets(XElement xml)
        {
            var offsets = new Dictionary<int, Offset>();

            var offsetsElement = xml.Element("offsets");
            if (offsetsElement != null)
            {
                foreach (var offsetElement in offsetsElement.Elements("offset"))
                {
                    if (int.TryParse(offsetElement.Attribute("direction")?.Value, out int direction))
                    {
                        offsets[direction] = new Offset(offsetElement);
                    }
                }
            }

            return offsets.Count > 0 ? offsets : null;
        }
    }

    public class Offset
    {
        [JsonPropertyName("direction")]
        public int Direction { get; set; }

        [JsonPropertyName("x")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? X { get; set; }

        [JsonPropertyName("y")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? Y { get; set; }

        public Offset(XElement xml)
        {
            Direction = int.TryParse(xml.Attribute("direction")?.Value, out int dir) ? dir : 0;
            X = int.TryParse(xml.Attribute("x")?.Value, out int x) ? x : (int?)null;
            Y = int.TryParse(xml.Attribute("y")?.Value, out int y) ? y : (int?)null;
        }
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


    public class FrameConverter : JsonConverter<Frame>
    {
        public override Frame Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported for Frame objects.");
        }

        public override void Write(Utf8JsonWriter writer, Frame value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteNumberValue(value.Id);

            if (value.RandomX.HasValue)
            {
                writer.WritePropertyName("randomX");
                writer.WriteNumberValue(value.RandomX.Value);
            }

            if (value.RandomY.HasValue)
            {
                writer.WritePropertyName("randomY");
                writer.WriteNumberValue(value.RandomY.Value);
            }

            if (value.Y.HasValue)
            {
                writer.WritePropertyName("y");
                writer.WriteNumberValue(value.Y.Value);
            }

            // Serialize Offsets
            if (value.Offsets != null && value.Offsets.Count > 0)
            {
                writer.WritePropertyName("offsets");
                writer.WriteStartObject();
                foreach (var offset in value.Offsets)
                {
                    writer.WritePropertyName(offset.Key.ToString());
                    writer.WriteStartObject();

                    writer.WritePropertyName("direction");
                    writer.WriteNumberValue(offset.Value.Direction);

                    if (offset.Value.X.HasValue)
                    {
                        writer.WritePropertyName("x");
                        writer.WriteNumberValue(offset.Value.X.Value);
                    }

                    if (offset.Value.Y.HasValue)
                    {
                        writer.WritePropertyName("y");
                        writer.WriteNumberValue(offset.Value.Y.Value);
                    }

                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
