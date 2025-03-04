using System.Xml.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Habbo_DownloaderSWF_Pets_Compiler.Mapper.Index
{
    public static class IndexPetsMapper
    {
        public static async Task<IndexPetsData> ParsePetsIndexFileAsync(string indexFilePath)
        {
            try
            {
                string indexContent = await File.ReadAllTextAsync(indexFilePath);

                XElement root = XElement.Parse(indexContent);

                return MapPetsIndexXML(root);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error parsing _index.bin for pets: {ex.Message}");
                return null;
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static IndexPetsData MapPetsIndexXML(XElement root)
        {
            if (root == null) return null;

            return new IndexPetsData
            {
                Type = root.Attribute("logic")?.Value,
                Name = root.Attribute("type")?.Value,
                VisualizationType = root.Attribute("visualization")?.Value,
                LogicType = root.Attribute("logic")?.Value
            };
        }

        public static string GeneratePetsJson(IndexPetsData indexData)
        {
            var json = new
            {
                type = indexData.Type,
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

    public class IndexPetsData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("visualizationType")]
        public string VisualizationType { get; set; }

        [JsonPropertyName("logicType")]
        public string LogicType { get; set; }
    }
}
