using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;

public static class AssetNameMapper
{
    public static Dictionary<string, string> BuildCanonicalMapping(string csvPath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"❌ CSV file not found: {csvPath}");
            return mapping;
        }

        var lines = File.ReadAllLines(csvPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        foreach (var line in lines)
        {
            var parts = line.Split(';');
            if (parts.Length < 2)
                continue;

            if (!int.TryParse(parts[0].Trim(), out int id) || id == 0)
                continue;

            string namePart = parts[1].Trim();
            if (namePart.Contains("_32_") ||
                namePart.ToLower().Contains("manifest") ||
                namePart.ToLower().Contains("assets") ||
                namePart.ToLower().Contains("logic") ||
                namePart.ToLower().Contains("visualization") ||
                namePart.ToLower().Contains("index"))
            {
                continue;
            }

            string fullName = namePart.ToLowerInvariant();

            string shortName = SpriteSheetMapper.CleanAssetName(fullName, disableCleanKey: false);
            if (!mapping.ContainsKey(shortName))
            {
                mapping[shortName] = fullName;
            }
        }

        return mapping;
    }
}
