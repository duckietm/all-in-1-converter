using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public static class DebugXmlParser
{
    /// <summary>
    /// Parses the Debug.xml file and extracts object-to-source mappings.
    /// </summary>
    public static Dictionary<string, string> ParseDebugXml(string debugXmlPath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(debugXmlPath))
        {
            Console.WriteLine($"Error: Debug.xml file not found: {debugXmlPath}");
            return mapping;
        }

        try
        {
            XDocument doc = XDocument.Load(debugXmlPath);
            var symbolClassTag = doc.Descendants("item")
                                    .FirstOrDefault(item => item.Attribute("type")?.Value == "SymbolClassTag");

            if (symbolClassTag == null)
            {
                Console.WriteLine("Error: SymbolClassTag not found in Debug.xml.");
                return mapping;
            }

            var tagItems = symbolClassTag.Element("tags")?.Elements("item").Select(e => e.Value).ToList();
            var nameItems = symbolClassTag.Element("names")?.Elements("item").Select(e => e.Value).ToList();

            if (tagItems == null || nameItems == null || tagItems.Count != nameItems.Count)
            {
                Console.WriteLine("Error: Tags and names count mismatch in Debug.xml.");
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
            Console.WriteLine("Successfully mapped Debug.xml sources.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Debug.xml: {ex.Message}");
        }

        return mapping;
    }
    /// <summary>
    /// Extracts symbol class tags from Debug.xml and returns a dictionary of tag ID (string) to a list of asset names.
    /// </summary>
    public static Dictionary<string, List<string>> ExtractSymbolClassTags(string debugXmlPath)
    {
        var tagMapping = new Dictionary<string, List<string>>();

        if (!File.Exists(debugXmlPath))
        {
            Console.WriteLine($"Error: Debug.xml file not found: {debugXmlPath}");
            return tagMapping;
        }

        try
        {
            XDocument doc = XDocument.Load(debugXmlPath);
            var symbolClassTag = doc.Descendants("item")
                                    .FirstOrDefault(item => item.Attribute("type")?.Value == "SymbolClassTag");

            if (symbolClassTag == null)
            {
                Console.WriteLine("Error: SymbolClassTag not found in Debug.xml.");
                return tagMapping;
            }

            var tagItems = symbolClassTag.Element("tags")?.Elements("item").Select(e => e.Value).ToList();
            var nameItems = symbolClassTag.Element("names")?.Elements("item").Select(e => e.Value).ToList();

            if (tagItems == null || nameItems == null || tagItems.Count != nameItems.Count)
            {
                Console.WriteLine("Error: Tags and names count mismatch in Debug.xml.");
                return tagMapping;
            }

            for (int i = 0; i < tagItems.Count; i++)
            {
                string tagId = tagItems[i]; // Treat ID as string
                string assetName = nameItems[i];

                if (!tagMapping.ContainsKey(tagId))
                {
                    tagMapping[tagId] = new List<string>(); // Initialize list for the ID
                }

                // ✅ Append asset name to the same ID
                tagMapping[tagId].Add(assetName);
            }

            Console.WriteLine("✅ Successfully extracted symbol class tags.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting symbol class tags: {ex.Message}");
        }

        return tagMapping;
    }

}
