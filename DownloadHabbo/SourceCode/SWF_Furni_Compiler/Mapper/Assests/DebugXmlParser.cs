using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DebugXmlParser
{
    /// <summary>
    /// Parses the CSV file (in place of Debug.xml) and creates a mapping.
    /// For each tag group (each unique first column), if there are at least two names,
    /// it maps the second name (object) to the first name (source) in lowercase.
    /// </summary>
    public static Dictionary<string, string> ParseDebugXml(string csvPath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"❌ Error CSV file not found: {csvPath}");
            return mapping;
        }

        try
        {
            // Read all non-empty lines from the CSV file.
            var lines = File.ReadAllLines(csvPath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            // Parse each line into a tuple (tag, name)
            var entries = new List<(string Tag, string Name)>();
            foreach (var line in lines)
            {
                // Expecting format: tag;name
                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;

                string tag = parts[0].Trim();
                string name = parts[1].Trim();
                entries.Add((tag, name));
            }

            // Group by tag.
            var groups = entries.GroupBy(e => e.Tag);
            foreach (var group in groups)
            {
                var names = group.Select(e => e.Name).ToList();
                if (names.Count >= 2)
                {
                    string source = names[0].ToLowerInvariant();
                    string obj = names[1].ToLowerInvariant();
                    mapping[obj] = source;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing CSV: {ex.Message}");
        }

        return mapping;
    }

    /// <summary>
    /// Extracts symbol class tags from the CSV file (instead of Debug.xml).
    /// Returns a dictionary mapping each tag (first column) to a list of asset names (second column).
    /// </summary>
    public static Dictionary<string, List<string>> ExtractSymbolClassTags(string csvPath)
    {
        var tagMapping = new Dictionary<string, List<string>>();

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"❌ Error CSV file not found: {csvPath}");
            return tagMapping;
        }

        try
        {
            var lines = File.ReadAllLines(csvPath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            foreach (var line in lines)
            {
                // Expecting format: tag;name
                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;

                string tag = parts[0].Trim();
                string assetName = parts[1].Trim();

                if (!tagMapping.ContainsKey(tag))
                {
                    tagMapping[tag] = new List<string>();
                }
                tagMapping[tag].Add(assetName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error extracting symbol class tags: {ex.Message}");
        }

        return tagMapping;
    }
}
