using System.Text.Json.Serialization;

namespace Habbo_Downloader.SWFCompiler.Mapper.Logic
{
    public class AssetLogicData
    {
        [JsonPropertyName("model")]
        public AssetLogicModel Model { get; set; }

        [JsonPropertyName("maskType")]
        public string MaskType { get; set; }

        [JsonPropertyName("credits")]
        public string Credits { get; set; }

        [JsonPropertyName("soundSample")]
        public SoundSample SoundSample { get; set; }

        [JsonPropertyName("action")]
        public ActionData Action { get; set; }

        [JsonPropertyName("planetSystems")]
        public List<AssetLogicPlanetSystem> PlanetSystems { get; set; }

        [JsonPropertyName("particleSystems")]
        public List<ParticleSystem> ParticleSystems { get; set; }

        [JsonPropertyName("customVars")]
        public CustomVars CustomVars { get; set; }
    }

    public class AssetLogicModel
    {
        [JsonPropertyName("dimensions")]
        public AssetDimension Dimensions { get; set; }

        [JsonPropertyName("directions")]
        public List<int> Directions { get; set; }
    }

    public class AssetDimension
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float? Z { get; set; }

        [JsonPropertyName("centerZ")]
        public float? CenterZ { get; set; }
    }

    public class SoundSample
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("noPitch")]
        public bool NoPitch { get; set; }
    }

    public class ActionData
    {
        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("startState")]
        public int? StartState { get; set; }
    }

    public class AssetLogicPlanetSystem
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parent")]
        public string Parent { get; set; }

        [JsonPropertyName("radius")]
        public float? Radius { get; set; }

        [JsonPropertyName("arcSpeed")]
        public float? ArcSpeed { get; set; }

        [JsonPropertyName("arcOffset")]
        public float? ArcOffset { get; set; }

        [JsonPropertyName("blend")]
        public int? Blend { get; set; }

        [JsonPropertyName("height")]
        public float? Height { get; set; }
    }

    public class ParticleSystem
    {
        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("canvasId")]
        public int? CanvasId { get; set; }

        [JsonPropertyName("offsetY")]
        public float? OffsetY { get; set; }

        [JsonPropertyName("blend")]
        public float? Blend { get; set; }

        [JsonPropertyName("bgColor")]
        public string BgColor { get; set; }

        [JsonPropertyName("emitters")]
        public List<ParticleSystemEmitter> Emitters { get; set; }
    }

    public class ParticleSystemEmitter
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("spriteId")]
        public int? SpriteId { get; set; }

        [JsonPropertyName("maxNumParticles")]
        public int? MaxNumParticles { get; set; }

        [JsonPropertyName("particlesPerFrame")]
        public int? ParticlesPerFrame { get; set; }

        [JsonPropertyName("burstPulse")]
        public int? BurstPulse { get; set; }

        [JsonPropertyName("fuseTime")]
        public int? FuseTime { get; set; }

        [JsonPropertyName("simulation")]
        public ParticleSystemSimulation Simulation { get; set; }

        [JsonPropertyName("particles")]
        public List<ParticleSystemParticle> Particles { get; set; }
    }

    public class ParticleSystemSimulation
    {
        [JsonPropertyName("force")]
        public float? Force { get; set; }

        [JsonPropertyName("direction")]
        public float? Direction { get; set; }

        [JsonPropertyName("gravity")]
        public float? Gravity { get; set; }

        [JsonPropertyName("airFriction")]
        public float? AirFriction { get; set; }

        [JsonPropertyName("shape")]
        public string Shape { get; set; }

        [JsonPropertyName("energy")]
        public float? Energy { get; set; }
    }

    public class ParticleSystemParticle
    {
        [JsonPropertyName("isEmitter")]
        public bool? IsEmitter { get; set; }

        [JsonPropertyName("lifeTime")]
        public int? LifeTime { get; set; }

        [JsonPropertyName("fade")]
        public bool? Fade { get; set; }

        [JsonPropertyName("frames")]
        public List<string> Frames { get; set; }
    }

    public class CustomVars
    {
        [JsonPropertyName("variables")]
        public List<string> Variables { get; set; }
    }
}
