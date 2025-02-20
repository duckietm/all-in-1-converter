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
                Link = actionElement.Attribute("link")?.Value,
                StartState = int.TryParse(actionElement.Attribute("startState")?.Value, out var startState) ? startState : (int?)null
            };
        }

        output.MaskType = logicElement.Element("mask")?.Attribute("type")?.Value;
        output.Credits = logicElement.Element("credits")?.Attribute("value")?.Value;

        var soundElement = logicElement.Element("sound");
        if (soundElement != null)
        {
            var sampleElement = soundElement.Element("sample");
            if (sampleElement != null)
            {
                output.SoundSample = new SoundSample
                {
                    Id = int.TryParse(sampleElement.Attribute("id")?.Value, out var id) ? id : 0,
                    NoPitch = sampleElement.Attribute("nopitch")?.Value == "true"
                };
            }
        }

        // Map planet systems
        var planetSystemElement = logicElement.Element("planetsystem");
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
        var customVarsElement = logicElement.Element("customvars");
        if (customVarsElement != null)
        {
            output.CustomVars = MapCustomVars(customVarsElement);
        }

        return output;
    }

    private static AssetDimension MapDimensions(XElement dimensionsElement)
    {
        if (dimensionsElement == null) return null;

        float? ParseFloatOrNull(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return float.IsNaN(result) ? (float?)null : result;
            }
            return null;
        }

        return new AssetDimension
        {
            X = ParseFloatOrNull(dimensionsElement.Attribute("x")?.Value) ?? 0f,
            Y = ParseFloatOrNull(dimensionsElement.Attribute("y")?.Value) ?? 0f,
            Z = ParseFloatOrNull(dimensionsElement.Attribute("z")?.Value) ?? 0.000001f,
            CenterZ = ParseFloatOrNull(dimensionsElement.Attribute("centerZ")?.Value)
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

        if (directions.Count == 0) directions.Add(0);

        return directions;
    }

    private static CustomVars MapCustomVars(XElement customVarsElement)
    {
        if (customVarsElement == null) return null;

        var customVars = new CustomVars();

        var variableElements = customVarsElement.Elements("variable");
        foreach (var variableElement in variableElements)
        {
            var name = variableElement.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                customVars.Variables.Add(name);
            }
        }

        return customVars;
    }
}