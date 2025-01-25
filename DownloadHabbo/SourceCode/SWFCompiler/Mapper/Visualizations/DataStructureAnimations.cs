using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public class Animation
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyName("layers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // Exclude if empty
        public Dictionary<int, AnimationLayer> Layers { get; set; } = new();

        public Animation(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
            Layers = ParseAnimationLayers(xml);
        }

        private Dictionary<int, AnimationLayer> ParseAnimationLayers(XElement xml)
        {
            var animationLayers = new Dictionary<int, AnimationLayer>();
            foreach (var animationLayerElement in xml.Elements("animationLayer"))
            {
                if (int.TryParse(animationLayerElement.Attribute("id")?.Value, out int id))
                {
                    var animationLayer = new AnimationLayer(animationLayerElement);
                    animationLayers[id] = animationLayer; // Do not filter here
                }
            }
            return animationLayers;
        }

        public bool ShouldSerialize() => Layers.Count > 0;
    }

    public class AnimationLayer
    {
        [JsonPropertyName("frameSequences")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // Exclude if empty
        public Dictionary<int, FrameSequence> FrameSequences { get; set; } = new();

        public AnimationLayer(XElement xml)
        {
            FrameSequences = ParseFrameSequences(xml);
        }

        private Dictionary<int, FrameSequence> ParseFrameSequences(XElement xml)
        {
            var frameSequences = new Dictionary<int, FrameSequence>();
            foreach (var frameSequenceElement in xml.Elements("frameSequence"))
            {
                if (int.TryParse(frameSequenceElement.Attribute("id")?.Value, out int id))
                {
                    var frameSequence = new FrameSequence(frameSequenceElement);
                    frameSequences[id] = frameSequence; // Do not filter here
                }
            }
            return frameSequences;
        }
    }

    public class FrameSequence
    {
        [JsonPropertyName("frames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // Exclude if empty
        public Dictionary<int, Frame> Frames { get; set; } = new();

        public FrameSequence(XElement xml)
        {
            Frames = ParseFrames(xml);
        }

        private Dictionary<int, Frame> ParseFrames(XElement xml)
        {
            var frames = new Dictionary<int, Frame>();
            foreach (var frameElement in xml.Elements("frame"))
            {
                if (int.TryParse(frameElement.Attribute("id")?.Value, out int id))
                {
                    frames[id] = new Frame(frameElement);
                }
            }
            return frames;
        }
    }

    public class Frame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        public Frame(XElement xml)
        {
            Id = int.TryParse(xml.Attribute("id")?.Value, out int id) ? id : 0;
        }
    }
}
