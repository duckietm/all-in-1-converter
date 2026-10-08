namespace Habbo_Downloader.Tools;

public sealed record HabboHotel(string Domain, string Name, string Region, string Language)
{
    public override string ToString() => $"{Name} - {Region} ({Language})";
}

/// <summary>The official Habbo hotels and the config.ini gamedata URLs that point at one of them.</summary>
public static class HabboHotels
{
    public static IReadOnlyList<HabboHotel> All { get; } =
    [
        new("habbo.com", "Habbo.com", "International", "English"),
        new("habbo.com.br", "Habbo.com.br", "Brazil", "Portuguese"),
        new("habbo.com.tr", "Habbo.com.tr", "Turkey", "Turkish"),
        new("habbo.de", "Habbo.de", "Germany", "German"),
        new("habbo.es", "Habbo.es", "Spain and Spanish-speaking regions", "Spanish"),
        new("habbo.fi", "Habbo.fi", "Finland", "Finnish"),
        new("habbo.fr", "Habbo.fr", "France", "French"),
        new("habbo.it", "Habbo.it", "Italy", "Italian"),
        new("habbo.nl", "Habbo.nl", "Netherlands and Belgium", "Dutch")
    ];

    /// <summary>The hotel-specific config.ini keys and their URL for this hotel.</summary>
    public static IReadOnlyList<(string Key, string Url)> Urls(HabboHotel hotel) =>
    [
        ("externalvarsurl", $"https://www.{hotel.Domain}/gamedata/external_variables/1"),
        ("externaltexturl", $"https://{hotel.Domain}/gamedata/external_flash_texts/1"),
        ("productdataurl", $"https://{hotel.Domain}/gamedata/productdata/1"),
        ("furnidataTXT", $"https://{hotel.Domain}/gamedata/furnidata/1"),
        ("furnidataXML", $"https://www.{hotel.Domain}/gamedata/furnidata_xml/1")
    ];

    /// <summary>The hotel all five URLs point at (with or without www.), or null for custom URLs.</summary>
    public static HabboHotel? Detect(Func<string, string?> setting) =>
        All.FirstOrDefault(hotel => Urls(hotel).All(entry =>
            string.Equals(Normalize(setting(entry.Key)), Normalize(entry.Url), StringComparison.OrdinalIgnoreCase)));

    private static string Normalize(string? url) =>
        (url ?? string.Empty).Trim().TrimEnd('/').Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
            .Replace("://www.", "://", StringComparison.OrdinalIgnoreCase);
}
