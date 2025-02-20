using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
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

        public Frame(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            RandomX = int.TryParse(xml.Attribute("randomX")?.Value, out int randomX) ? randomX : (int?)null;
            RandomY = int.TryParse(xml.Attribute("randomY")?.Value, out int randomY) ? randomY : (int?)null;
        }
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

            writer.WriteEndObject();
        }
    }
}
