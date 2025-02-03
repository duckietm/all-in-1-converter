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

        private List<AssetLogicPlanetSystem> _planetSystems = new List<AssetLogicPlanetSystem>();

        [JsonIgnore]
        public List<AssetLogicPlanetSystem> PlanetSystems
        {
            get => _planetSystems;
            set => _planetSystems = value ?? new List<AssetLogicPlanetSystem>();
        }

        [JsonPropertyName("planetSystems")]
        public List<AssetLogicPlanetSystem> PlanetSystemsSerializable
        {
            get => _planetSystems.Count > 0 ? _planetSystems : null;
            set => _planetSystems = value ?? new List<AssetLogicPlanetSystem>();
        }

        private List<ParticleSystem> _particleSystems = new List<ParticleSystem>();

        [JsonIgnore]
        public List<ParticleSystem> ParticleSystems
        {
            get => _particleSystems;
            set => _particleSystems = value ?? new List<ParticleSystem>();
        }

        [JsonPropertyName("particleSystems")]
        public List<ParticleSystem> ParticleSystemsSerializable
        {
            get => _particleSystems.Count > 0 ? _particleSystems : null;
            set => _particleSystems = value ?? new List<ParticleSystem>();
        }

        private CustomVars _customVars = new CustomVars();

        [JsonIgnore]
        public CustomVars CustomVars
        {
            get => _customVars;
            set => _customVars = value ?? new CustomVars();
        }

        [JsonPropertyName("customVars")]
        public CustomVars CustomVarsSerializable
        {
            get => _customVars.Variables.Count > 0 ? _customVars : null;
            set => _customVars = value ?? new CustomVars();
        }
    }

    public class AssetLogicModel
    {
        [JsonPropertyName("dimensions")]
        public AssetDimension Dimensions { get; set; }

        private List<int> _directions = new List<int>();

        [JsonIgnore]
        public List<int> Directions
        {
            get => _directions;
            set => _directions = value ?? new List<int>();
        }

        [JsonPropertyName("directions")]
        public List<int> DirectionsSerializable
        {
            get => _directions.Count > 0 ? _directions : null;
            set => _directions = value ?? new List<int>();
        }
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
        private string _link;
        private int? _startState;

        [JsonIgnore]
        public string Link
        {
            get => _link;
            set => _link = value;
        }

        [JsonIgnore]
        public int? StartState
        {
            get => _startState;
            set => _startState = value;
        }

        [JsonPropertyName("link")]
        public string LinkSerializable
        {
            get => !string.IsNullOrEmpty(_link) ? _link : null;
            set => _link = value;
        }

        [JsonPropertyName("startState")]
        public int? StartStateSerializable
        {
            get => _startState.HasValue ? _startState : null;
            set => _startState = value;
        }
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
        public List<ParticleSystemEmitter> Emitters { get; set; } = new List<ParticleSystemEmitter>();
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
        public int BurstPulse { get; set; } = 1;

        [JsonPropertyName("fuseTime")]
        public int? FuseTime { get; set; }

        [JsonPropertyName("simulation")]
        public ParticleSystemSimulation Simulation { get; set; }

        [JsonPropertyName("particles")]
        public List<ParticleSystemParticle> Particles { get; set; } = new List<ParticleSystemParticle>();
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
        public bool IsEmitter { get; set; }

        [JsonPropertyName("lifeTime")]
        public int? LifeTime { get; set; }

        [JsonPropertyName("fade")]
        public bool Fade { get; set; }

        [JsonPropertyName("frames")]
        public List<string> Frames { get; set; } = new List<string>();
    }

    public class CustomVars
    {
        private List<string> _variables = new List<string>();

        [JsonIgnore]
        public List<string> Variables
        {
            get => _variables;
            set => _variables = value ?? new List<string>();
        }

        [JsonPropertyName("variables")]
        public List<string> VariablesSerializable
        {
            get => _variables.Count > 0 ? _variables : null;
            set => _variables = value ?? new List<string>();
        }
    }
}