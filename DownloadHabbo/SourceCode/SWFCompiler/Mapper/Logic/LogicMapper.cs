using System.Globalization;
using System.Xml.Linq;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;

public static class LogicMapper
{
    public static AssetLogicData MapLogicXml(XElement logicElement)
    {
        if (logicElement == null) return null;

        var output = new AssetLogicData();

        // Map model
        var modelElement = logicElement.Element("model");
        if (modelElement != null)
        {
            output.Model = new AssetLogicModel
            {
                Dimensions = MapDimensions(modelElement.Element("dimensions")),
                Directions = MapDirections(modelElement.Element("directions"))
            };
        }

        // Map action
        var actionElement = logicElement.Element("action");
        if (actionElement != null)
        {
            output.Action = new ActionData
            {
                Link = actionElement.Element("link")?.Value,
                StartState = int.TryParse(actionElement.Element("startState")?.Value, out var startState) ? startState : (int?)null
            };
        }

        // Map maskType, credits, and soundSample
        output.MaskType = logicElement.Element("mask")?.Attribute("type")?.Value;
        output.Credits = logicElement.Element("credits")?.Attribute("value")?.Value;

        var soundSampleElement = logicElement.Element("soundSample");
        if (soundSampleElement != null)
        {
            output.SoundSample = new SoundSample
            {
                Id = int.TryParse(soundSampleElement.Attribute("id")?.Value, out var id) ? id : 0,
                NoPitch = soundSampleElement.Attribute("noPitch")?.Value == "true"
            };
        }

        // Map planet systems
        var planetSystemElement = logicElement.Element("planetsystem"); // Corrected to lowercase "planetsystem"
        if (planetSystemElement != null)
        {
            output.PlanetSystems = MapPlanetSystem.MapPlanetSystems(planetSystemElement.Elements("object"));
        }

        // Map particle systems
        var particleSystemsElement = logicElement.Element("particlesystems");
        if (particleSystemsElement != null)
        {
            output.ParticleSystems = MapParticleSystem.MapParticleSystems(particleSystemsElement.Elements("particlesystem"));
        }

        // Map custom variables
        var customVarsElement = logicElement.Element("customVars");
        if (customVarsElement != null)
        {
            output.CustomVars = new CustomVars
            {
                Variables = customVarsElement.Elements("variable").Select(v => v.Value).ToList()
            };
        }

        return output;
    }

    private static AssetDimension MapDimensions(XElement dimensionsElement)
    {
        if (dimensionsElement == null) return null;

        return new AssetDimension
        {
            X = float.TryParse(dimensionsElement.Attribute("x")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0f,
            Y = float.TryParse(dimensionsElement.Attribute("y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? y : 0f,
            Z = float.TryParse(dimensionsElement.Attribute("z")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z : (float?)null,
            CenterZ = float.TryParse(dimensionsElement.Attribute("centerZ")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var centerZ) ? centerZ : (float?)null
        };
    }

    private static List<int> MapDirections(XElement directionsElement)
    {
        if (directionsElement == null) return new List<int>();

        var directions = directionsElement.Elements("direction")
            .Select(d => int.TryParse(d.Attribute("id")?.Value, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToList();

        if (directions.Count == 0) return new List<int>();

        return directions;
    }
}