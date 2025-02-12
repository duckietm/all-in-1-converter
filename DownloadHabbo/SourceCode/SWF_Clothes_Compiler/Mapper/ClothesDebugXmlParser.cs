using System.Xml.Linq;

public static class ClothesDebugXmlParser
{
    public static Dictionary<string, string> GetClothesImageMapping(string debugXmlPath)
    {
        var mapping = new Dictionary<string, string>();
        if (!File.Exists(debugXmlPath))
        {
            Console.WriteLine($"❌ Debug.xml not found at {debugXmlPath}");
            return mapping;
        }

        XElement debugXml = XElement.Load(debugXmlPath);

        // Find the first item with type "SymbolClassTag"
        var symbolItem = debugXml.Descendants("item")
            .FirstOrDefault(x => x.Attribute("type")?.Value == "SymbolClassTag");
        if (symbolItem == null)
        {
            Console.WriteLine("❌ SymbolClassTag item not found in Debug.xml.");
            return mapping;
        }

        // Get the tags and names lists.
        var tags = symbolItem.Element("tags")?.Elements("item").Select(x => x.Value.Trim()).ToList();
        var names = symbolItem.Element("names")?.Elements("item").Select(x => x.Value.Trim()).ToList();

        if (tags == null || names == null || tags.Count != names.Count)
        {
            Console.WriteLine("❌ Error: Tags and Names count mismatch in Debug.xml.");
            return mapping;
        }

        // Build mapping: for each pair, if tag is not "0" and name doesn't contain "manifest",
        // and if the tag is not already mapped (i.e. only use first occurrence), add it.
        for (int i = 0; i < tags.Count; i++)
        {
            string tag = tags[i];
            string name = names[i];
            if (tag == "0" || name.ToLowerInvariant().Contains("manifest"))
                continue;

            if (!mapping.ContainsKey(tag))
            {
                mapping[tag] = name;
            }
        }
        return mapping;
    }
}
