using Habbo_Downloader.SWFCompiler.Mapper.Logic;
using System.Globalization;
using System.Xml.Linq;

public static class MapParticleSystem
{
    public static List<ParticleSystem> MapParticleSystems(IEnumerable<XElement> particleElements)
    {
        return particleElements
            .Where(p => !(int.TryParse(p.Attribute("size")?.Value, out var size) && size == 32)) // Exclude systems with Size = 32
            .Select(p => new ParticleSystem
            {
                Size = int.TryParse(p.Attribute("size")?.Value, out var size) ? size : (int?)null,
                CanvasId = int.TryParse(p.Attribute("canvas_id")?.Value, out var canvasId) ? canvasId : (int?)null,
                OffsetY = float.TryParse(p.Attribute("offset_y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var offsetY) ? offsetY : (float?)null,
                Blend = float.TryParse(p.Attribute("blend")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var blend) ? blend : (float?)null,
                BgColor = p.Attribute("bg_color")?.Value,
                Emitters = MapParticleSystemEmitters(p.Elements("emitter")).Any() ? MapParticleSystemEmitters(p.Elements("emitter")) : null
            })
            .Where(ps => ps.Emitters != null || ps.Size != null)
            .ToList();
    }

    private static List<ParticleSystemEmitter> MapParticleSystemEmitters(IEnumerable<XElement> emitterElements)
    {
        return emitterElements.Select(e => new ParticleSystemEmitter
        {
            Id = int.TryParse(e.Attribute("id")?.Value, out var id) ? id : (int?)null,
            Name = e.Attribute("name")?.Value,
            SpriteId = int.TryParse(e.Attribute("sprite_id")?.Value, out var spriteId) ? spriteId : (int?)null,
            MaxNumParticles = int.TryParse(e.Attribute("max_num_particles")?.Value, out var maxNumParticles) ? maxNumParticles : (int?)null,
            ParticlesPerFrame = int.TryParse(e.Attribute("particles_per_frame")?.Value, out var particlesPerFrame) ? particlesPerFrame : (int?)null,
            BurstPulse = int.TryParse(e.Attribute("burst_pulse")?.Value, out var burstPulse) ? burstPulse : 1, // Default to 1 if not present
            FuseTime = int.TryParse(e.Attribute("fuse_time")?.Value, out var fuseTime) ? fuseTime : (int?)null,
            Simulation = MapParticleSystemSimulation(e.Element("simulation")),
            Particles = MapParticleSystemParticles(e.Element("particles"))
        }).ToList();
    }

    private static ParticleSystemSimulation MapParticleSystemSimulation(XElement simulationElement)
    {
        if (simulationElement == null) return null;

        return new ParticleSystemSimulation
        {
            Force = float.TryParse(simulationElement.Attribute("force")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var force) ? force : (float?)null,
            Direction = float.TryParse(simulationElement.Attribute("direction")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var direction) ? direction : (float?)null,
            Gravity = float.TryParse(simulationElement.Attribute("gravity")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gravity) ? gravity : (float?)null,
            AirFriction = float.TryParse(simulationElement.Attribute("airfriction")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var airFriction) ? airFriction : (float?)null,
            Shape = simulationElement.Attribute("shape")?.Value,
            Energy = float.TryParse(simulationElement.Attribute("energy")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var energy) ? energy : (float?)null
        };
    }

    private static List<ParticleSystemParticle> MapParticleSystemParticles(XElement particlesElement)
    {
        if (particlesElement == null) return null;

        return particlesElement.Elements("particle").Select(p => new ParticleSystemParticle
        {
            IsEmitter = p.Attribute("is_emitter")?.Value == "true",
            LifeTime = int.TryParse(p.Attribute("lifetime")?.Value, out var lifeTime) ? lifeTime : (int?)null,
            Fade = p.Attribute("fade")?.Value == "true",
            Frames = p.Elements("frame").Select(f => f.Attribute("name")?.Value).Where(name => !string.IsNullOrEmpty(name)).ToList()
        }).ToList();
    }
}
