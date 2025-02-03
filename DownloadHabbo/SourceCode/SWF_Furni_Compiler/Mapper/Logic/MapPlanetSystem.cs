using System.Globalization;
using System.Xml.Linq;
using Habbo_Downloader.SWFCompiler.Mapper.Logic;

public static class MapPlanetSystem
{
    public static List<AssetLogicPlanetSystem> MapPlanetSystems(IEnumerable<XElement> planetElements)
    {
        return planetElements.Select(p => new AssetLogicPlanetSystem
        {
            Id = int.TryParse(p.Attribute("id")?.Value, out var id) ? id : (int?)null,
            Name = p.Attribute("name")?.Value,
            Parent = p.Attribute("parent")?.Value,
            Radius = float.TryParse(p.Attribute("radius")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var radius) ? radius : (float?)null,
            ArcSpeed = float.TryParse(p.Attribute("arcspeed")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var arcSpeed) ? arcSpeed : (float?)null,
            ArcOffset = float.TryParse(p.Attribute("arcoffset")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var arcOffset) ? arcOffset : (float?)null,
            Blend = int.TryParse(p.Attribute("blend")?.Value, out var blend) ? blend : (int?)null,
            Height = float.TryParse(p.Attribute("height")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ? height : (float?)null
        }).ToList();
    }
}