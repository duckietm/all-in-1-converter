using System.Xml.Linq;

public static class DebugXmlParser
{
    public static Dictionary<string, string> ParseDebugXml(string debugXmlPath)
    {
        var assetMappings = new Dictionary<string, string>();

        if (!File.Exists(debugXmlPath))
        {
            Console.WriteLine($"❌ Error: Debug.xml file not found: {debugXmlPath}");
            return assetMappings;
        }

        try
        {
            XDocument doc = XDocument.Load(debugXmlPath);
            var symbolClassTag = doc.Descendants("item")
                                    .FirstOrDefault(item => item.Attribute("type")?.Value == "SymbolClassTag");

            if (symbolClassTag == null)
            {
                Console.WriteLine("❌ Error: SymbolClassTag not found in Debug.xml.");
                return assetMappings;
            }

            var tags = symbolClassTag.Element("tags")?.Elements("item").Select(e => e.Value).ToList();
            var names = symbolClassTag.Element("names")?.Elements("item").Select(e => e.Value).ToList();

            if (tags == null || names == null || tags.Count != names.Count)
            {
                Console.WriteLine("❌ Error: Tags and names count mismatch in Debug.xml.");
                return assetMappings;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                string name = names[i];

                if (!assetMappings.ContainsKey(tag))
                {
                    assetMappings[tag] = name;
                }
            }

            Console.WriteLine("✅ Successfully parsed Debug.xml for asset mappings.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing Debug.xml: {ex.Message}");
        }

        return assetMappings;
    }
}