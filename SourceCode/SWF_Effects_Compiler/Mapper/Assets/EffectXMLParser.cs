using System.Xml.Linq;

public static class EffectXMLParser
{
    public static Dictionary<string, string> GetEffectsImageMapping(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ Debug file not found: {filePath}");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSymbolsCsv(filePath);
        }
        else
        {
            return ParseXml(filePath);
        }
    }

    private static Dictionary<string, string> ParseSymbolsCsv(string csvPath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var lines = File.ReadAllLines(csvPath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToList();

            var entries = new List<(string Tag, string Name)>();
            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;
                string tag = parts[0].Trim();
                string name = parts[1].Trim();
                entries.Add((tag, name));
            }

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
                else if (names.Count == 1)
                {
                    string onlyName = names[0].ToLowerInvariant();
                    mapping[onlyName] = onlyName;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing CSV: {ex.Message}");
        }
        return mapping;
    }


    private static Dictionary<string, string> ParseXml(string xmlPath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            XDocument doc = XDocument.Load(xmlPath);
            var symbolClassTag = doc.Descendants("item")
                                    .FirstOrDefault(item => item.Attribute("type")?.Value == "SymbolClassTag");

            if (symbolClassTag == null)
            {
                Console.WriteLine("❌ Error SymbolClassTag not found in XML.");
                return mapping;
            }

            var tagItems = symbolClassTag.Element("tags")?.Elements("item").Select(e => e.Value).ToList();
            var nameItems = symbolClassTag.Element("names")?.Elements("item").Select(e => e.Value).ToList();

            if (tagItems == null || nameItems == null || tagItems.Count != nameItems.Count)
            {
                Console.WriteLine("❌ Error: Tags and names count mismatch in XML.");
                return mapping;
            }

            var temp = new Dictionary<string, List<string>>();
            for (int i = 0; i < tagItems.Count; i++)
            {
                string tag = tagItems[i];
                string name = nameItems[i];
                if (!temp.ContainsKey(tag))
                    temp[tag] = new List<string>();
                temp[tag].Add(name);
            }

            foreach (var kv in temp)
            {
                var names = kv.Value;
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
            Console.WriteLine($"❌ Error parsing XML: {ex.Message}");
        }
        return mapping;
    }
}
