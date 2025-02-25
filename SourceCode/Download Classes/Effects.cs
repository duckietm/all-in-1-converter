using System.Xml.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleApplication
{
    public static class EffectsDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadEffectsAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string externalVarsUrl = config["AppSettings:externalvarsurl"];
            string effectUrl = config["AppSettings:effecturl"];

            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgentClass.UserAgent);

            try
            {
                HttpResponseMessage res = await httpClient.GetAsync(externalVarsUrl);
                string source = await res.Content.ReadAsStringAsync();

                string releaseEffect = null;
                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    try
                    {
                        if (line.Contains("flash.client.url="))
                        {
                            string[] parts = line.Substring(0, line.Length - 1).Split('/');
                            if (parts.Length > 4)
                            {
                                releaseEffect = parts[4];
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.WriteLine("📃 Downloading Effects from habbo version: " + releaseEffect);
                                break;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Error: Insufficient parts in URL to determine release effect.");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error processing line: {ex.Message}");
                        return;
                    }
                }

                if (string.IsNullOrEmpty(releaseEffect))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not determine the release version.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                // Download XML files
                string effectMapUrl = $"{effectUrl}/{releaseEffect}/effectmap.xml";
                string effectMapFilePath = "./Habbo_Default/files/xml/effectmap.xml";
                await DownloadFileAsync(effectMapUrl, effectMapFilePath, "effectmap.xml");

                string habboAvatarActionsUrl = $"{effectUrl}/{releaseEffect}/HabboAvatarActions.xml";
                string avatarActionsFilePath = "./Habbo_Default/files/xml/HabboAvatarActions.xml";
                await DownloadFileAsync(habboAvatarActionsUrl, avatarActionsFilePath, "HabboAvatarActions.xml");

                // Download SWF files
                await DownloadSwfFilesAsync(effectUrl, releaseEffect, effectMapFilePath);

                // Generate JSON files from the XML
                await GenerateJsonFromXmlAsync(effectMapFilePath);
                await GenerateAvatarActionsJsonFromXmlAsync(avatarActionsFilePath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("🎉 Effects Downloaded, JSON generated, and Saved");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading effects: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        private static async Task DownloadSwfFilesAsync(string effectUrl, string releaseEffect, string effectMapFilePath)
        {
            try
            {
                if (!File.Exists(effectMapFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: effectmap.xml file not found at " + effectMapFilePath);
                    return;
                }

                var doc = XDocument.Load(effectMapFilePath);

                var swfItems = doc.Descendants("effect")
                                  .Select(x => (string)x.Attribute("lib"))
                                  .Where(x => !string.IsNullOrEmpty(x))
                                  .Distinct();

                int count = swfItems.Count();
                Console.WriteLine($"Found {count} SWF items in effectmap.xml.");

                string destinationDirectory = "./SWFCompiler/import/effects";
                Directory.CreateDirectory(destinationDirectory);

                foreach (var item in swfItems)
                {
                    string swfUrl = $"{effectUrl}/{releaseEffect}/{item}.swf";
                    string destinationPath = Path.Combine(destinationDirectory, $"{item}.swf");
                    await DownloadFileAsync(swfUrl, destinationPath, $"{item}.swf");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ downloading SWF files: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        private static async Task GenerateJsonFromXmlAsync(string effectMapFilePath)
        {
            try
            {
                if (!File.Exists(effectMapFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ effectmap.xml file not found at " + effectMapFilePath);
                    return;
                }

                var doc = XDocument.Load(effectMapFilePath);

                var effects = doc.Descendants("effect")
                                 .Select(x => new Effect
                                 {
                                     id = (string)x.Attribute("id"),
                                     lib = (string)x.Attribute("lib"),
                                     type = (string)x.Attribute("type"),
                                     revision = int.Parse((string)x.Attribute("revision"))
                                 })
                                 .ToList();

                var effectMap = new { effects = effects };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(effectMap, options);

                string jsonDirectory = "./Habbo_Default/files/json";
                Directory.CreateDirectory(jsonDirectory);
                string jsonFilePath = Path.Combine(jsonDirectory, "EffectMap.json");

                await File.WriteAllTextAsync(jsonFilePath, json);

                Console.WriteLine($"📦 Generated JSON : {jsonFilePath}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ generating JSON: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task GenerateAvatarActionsJsonFromXmlAsync(string avatarActionsFilePath)
        {
            try
            {
                if (!File.Exists(avatarActionsFilePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ HabboAvatarActions.xml file not found at " + avatarActionsFilePath);
                    return;
                }

                var doc = XDocument.Load(avatarActionsFilePath);

                var actions = doc.Descendants("action")
                                 .Select(x =>
                                 {
                                     IEnumerable<XElement> paramElements = x.Element("params")?.Elements("param");
                                     if (paramElements == null || !paramElements.Any())
                                         paramElements = x.Elements("param");

                                     var action = new AvatarAction
                                     {
                                         id = (string)x.Attribute("id"),
                                         state = (string)x.Attribute("state"),
                                         precedence = int.TryParse((string)x.Attribute("precedence"), out int p) ? p : (int?)null,
                                         animation = GetNullableBoolFromAttribute(x, "animation"),
                                         main = GetNullableBoolFromAttribute(x, "main"),
                                         geometryType = (string)x.Attribute("geometrytype"),
                                         activePartSet = (string)x.Attribute("activepartset"),
                                         assetPartDefinition = (string)x.Attribute("assetpartdefinition"),
                                         startFromFrameZero = GetNullableBoolFromAttribute(x, "startfromframezero"),
                                         preventHeadTurn = GetNullableBoolFromAttribute(x, "preventheadturn"),
                                         prevents = GetListFromAttribute(x, "prevents"),
                                         parameters = paramElements.Any() ?
                                                      paramElements.Select(y => new AvatarActionParam
                                                      {
                                                          id = (string)y.Attribute("id"),
                                                          value = (string)y.Attribute("value")
                                                      }).ToList() : null,
                                         types = x.Elements("type")
                                                  .Select(y =>
                                                  {
                                                      var typeObj = new AvatarActionType
                                                      {
                                                          id = (string)y.Attribute("id"),
                                                          // Always output "animated" (default to false if missing)
                                                          animated = ((string)y.Attribute("animated") == "1" || ((string)y.Attribute("animated"))?.ToLower() == "true"),
                                                          preventHeadTurn = GetNullableBoolFromAttribute(y, "preventheadturn"),
                                                          prevents = GetListFromAttribute(y, "prevents")
                                                      };
                                                      if (typeObj.prevents != null && typeObj.prevents.Count == 0)
                                                          typeObj.prevents = null;
                                                      return typeObj;
                                                  }).ToList()
                                     };

                                     if (action.prevents != null && action.prevents.Count == 0)
                                         action.prevents = null;
                                     if (action.types != null && action.types.Count == 0)
                                         action.types = null;

                                     return action;
                                 })
                                 .ToList();

                var actionsMap = new { actions = actions };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(actionsMap, options);

                string jsonDirectory = "./Habbo_Default/files/json";
                Directory.CreateDirectory(jsonDirectory);
                string jsonFilePath = Path.Combine(jsonDirectory, "HabboAvatarActions.json");

                await File.WriteAllTextAsync(jsonFilePath, json);

                Console.WriteLine($"📦 Generated JSON : {jsonFilePath}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ generating AvatarActions JSON: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static bool? GetNullableBoolFromAttribute(XElement element, string attributeName)
        {
            string attrVal = (string)element.Attribute(attributeName);
            if (!string.IsNullOrEmpty(attrVal) && (attrVal == "1" || attrVal.ToLower() == "true"))
                return true;
            return null;
        }

        private static List<string> GetListFromAttribute(XElement element, string attributeName)
        {
            var attr = element.Attribute(attributeName);
            if (attr == null) return new List<string>();
            return attr.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .ToList();
        }

        private static async Task DownloadFileAsync(string url, string filePath, string fileName)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"📥 Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ downloading {fileName}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private class Effect
        {
            public string id { get; set; }
            public string lib { get; set; }
            public string type { get; set; }
            public int revision { get; set; }
        }

        public class AvatarAction
        {
            [JsonPropertyOrder(1)]
            public string id { get; set; }

            [JsonPropertyOrder(2)]
            public string state { get; set; }

            [JsonPropertyOrder(3)]
            public int? precedence { get; set; }

            [JsonPropertyOrder(4)]
            public bool? animation { get; set; }

            [JsonPropertyOrder(5)]
            public bool? main { get; set; }

            [JsonPropertyOrder(6)]
            public string geometryType { get; set; }

            [JsonPropertyOrder(7)]
            public string activePartSet { get; set; }

            [JsonPropertyOrder(8)]
            public string assetPartDefinition { get; set; }

            [JsonPropertyOrder(9)]
            public bool? startFromFrameZero { get; set; }

            [JsonPropertyOrder(10)]
            public bool? preventHeadTurn { get; set; }

            [JsonPropertyOrder(11)]
            public List<string> prevents { get; set; }

            [JsonPropertyName("params")]
            [JsonPropertyOrder(12)]
            public List<AvatarActionParam> parameters { get; set; }

            [JsonPropertyOrder(13)]
            public List<AvatarActionType> types { get; set; }
        }

        public class AvatarActionParam
        {
            public string id { get; set; }
            public string value { get; set; }
        }

        public class AvatarActionType
        {
            public string id { get; set; }
            public bool animated { get; set; }
            public bool? preventHeadTurn { get; set; }
            public List<string> prevents { get; set; }
        }

        public class ActionOffset
        {
            public string action { get; set; }
            public List<Offset> offsets { get; set; }
        }

        public class Offset
        {
            public string size { get; set; }
            public int? direction { get; set; }
            public double? x { get; set; }
            public double? y { get; set; }
            public double? z { get; set; }
        }
    }
}