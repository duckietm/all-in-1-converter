using System.Xml.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Habbo_Downloader.SWFCompiler.Mapper.Index
{
    public static class IndexMapper
    {
        public static async Task<IndexData> ParseIndexFileAsync(string indexFilePath)
        {
            try
            {
                string indexContent = await File.ReadAllTextAsync(indexFilePath);

                XElement root = XElement.Parse(indexContent);

                return MapIndexXML(root);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error parsing _index.bin: {ex.Message}");
                return null;
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static IndexData MapIndexXML(XElement root)
        {
            if (root == null) return null;

            return new IndexData
            {
                Name = root.Attribute("type")?.Value,
                VisualizationType = root.Attribute("visualization")?.Value,
                LogicType = root.Attribute("logic")?.Value
            };
        }

        public static string GenerateJson(IndexData indexData)
        {
            var json = new
            {
                name = indexData.Name,
                logicType = indexData.LogicType,
                visualizationType = indexData.VisualizationType
            };

            return JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    public class IndexData
    {
        [JsonPropertyName("name")] // Specify lowercase JSON property name
        public string Name { get; set; }

        [JsonPropertyName("visualizationType")] // Specify lowercase JSON property name
        public string VisualizationType { get; set; }

        [JsonPropertyName("logicType")] // Specify lowercase JSON property name
        public string LogicType { get; set; }
    }
}
