using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ConsoleApplication
{
    public class ExactXmlNamingPolicy : JsonNamingPolicy
    {
        private readonly Dictionary<string, string> _mapping = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"Palettes", "palettes"},
            {"setTypes", "setTypes"},
            {"paletteId", "paletteId"},
            {"Colors", "colors"},
            {"Id", "id"},
            {"Index", "index"},
            {"Club", "club"},
            {"Selectable", "selectable"},
            {"HexCode", "hexCode"},
            {"SetTypes", "setTypes"},
            {"Type", "type"},
            {"PaletteId", "paletteId"},
            {"MandatoryF0", "mandatory_f_0"},
            {"MandatoryF1", "mandatory_f_1"},
            {"MandatoryM0", "mandatory_m_0"},
            {"MandatoryM1", "mandatory_m_1"},
            {"Sets", "sets"},
            {"Gender", "gender"},
            {"Colorable", "colorable"},
            {"Preselectable", "preselectable"},
            {"Sellable", "sellable"},
            {"Parts", "parts"},
            {"ColorIndex", "colorindex"},
            {"PartType", "partType"},
            {"HiddenLayers", "hiddenLayers"},
            // For figuremap
            {"Libraries", "libraries"},
            {"Revision", "revision"}
        };

        public override string ConvertName(string name)
        {
            if (_mapping.TryGetValue(name, out var mapped))
            {
                return mapped;
            }
            return name.ToLower();
        }
    }

    internal static class ClothesDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly object consoleLock = new object();

        internal static async Task DownloadClothesAsync()
        {
            httpClient.DefaultRequestHeaders.Add("user-agent", UserAgentClass.UserAgent);

            try
            {
                string externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/1";
                string source = await httpClient.GetStringAsync(externalVariablesUrl);

                string release = null;
                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (line.Contains("flash.client.url="))
                    {
                        release = line.Substring(0, line.Length - 1).Split('/')[4];
                        Console.WriteLine("We are going to download from release: " + release);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(release))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not determine the release version.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                Console.WriteLine("👉 We can disable all the sellable setting in the JSON,");
                Console.WriteLine("if this is disabled all clothes will be visible if not then only clothes that are not sellable 👈");
                Console.WriteLine("❓ Do you want to disable all sellable? (Y/N): ");
                string answer = Console.ReadLine();
                bool disableSellable = answer.Trim().ToUpper() == "Y";

                string currentDirectory = Environment.CurrentDirectory;
                string gordonDirectory = "https://images.habbo.com/gordon/";

                string xmlDirectory = Path.Combine(currentDirectory, "Habbo_Default", "files", "xml");
                string jsonDirectory = Path.Combine(currentDirectory, "Habbo_Default", "files", "json");
                string swfDirectory = Path.Combine(currentDirectory, "Habbo_Default", "clothes");

                string figuremapUrl = $"{gordonDirectory}{release}/figuremap.xml";
                string figuredataUrl = "http://habbo.com/gamedata/figuredata/1";

                await DownloadFileAsync(figuredataUrl, Path.Combine(xmlDirectory, "figuredata.xml"), "figuredata.xml");
                await DownloadFileAsync(figuremapUrl, Path.Combine(xmlDirectory, "figuremap.xml"), "figuremap.xml");

                // Convert XML to JSON.
                string figuredataFile = Path.Combine(xmlDirectory, "figuredata.xml");
                string figuredataJsonOutput = Path.Combine(jsonDirectory, "figuredata.json");
                await ConvertFigureDataToJson(figuredataFile, figuredataJsonOutput, disableSellable);

                string figuremapFile = Path.Combine(xmlDirectory, "figuremap.xml");
                string figuremapJsonOutput = Path.Combine(jsonDirectory, "figuremap.json");
                await ConvertFigureMapToJson(figuremapFile, figuremapJsonOutput);

                List<string> libIds = new List<string>();
                if (File.Exists(figuremapFile))
                {
                    using (XmlReader reader = XmlReader.Create(figuremapFile))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement() && reader.Name == "lib")
                            {
                                string id = reader["id"];
                                if (!string.IsNullOrEmpty(id))
                                {
                                    libIds.Add(id);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: figuremap.xml does not exist.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                int totalLibs = libIds.Count;
                int processedLibs = 0;
                int downloadCount = 0;

                foreach (string id in libIds)
                {
                    string swfUrl = $"{gordonDirectory}{release}/{id}.swf";
                    string swfFilePath = Path.Combine(swfDirectory, $"{id}.swf");

                    if (!File.Exists(swfFilePath))
                    {
                        bool success = await DownloadFileAsync(swfUrl, swfFilePath, id, silent: true);
                        if (success)
                        {
                            downloadCount++;
                        }
                    }

                    processedLibs++;
                    lock (consoleLock)
                    {
                        string progressBar = BuildProgressBar(processedLibs, totalLibs, 50);
                        double percent = ((double)processedLibs / totalLibs) * 100;
                        Console.Write($"\r{progressBar} {percent:F2}% ({processedLibs}/{totalLibs} SWF files processed)");
                    }
                }

                Console.WriteLine();

                if (downloadCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Downloaded {downloadCount} new clothes!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You have the latest clothes!");
                }
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine();
                Console.WriteLine("All has been done!");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task<bool> DownloadFileAsync(string url, string filePath, string fileName, bool silent = false)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                if (!silent)
                {
                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"📁 Downloaded: {fileName}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
                return true;
            }
            catch (HttpRequestException ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error downloading {fileName}: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                return false;
            }
        }

        private static async Task ConvertFigureDataToJson(string xmlFilePath, string jsonOutputPath, bool disableSellable)
        {
            if (!File.Exists(xmlFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: figuredata.xml does not exist.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(xmlFilePath);
                var root = doc.Element("figuredata");

                var palettes = root.Element("colors")?.Elements("palette").Select(p => new FigureDataPalette
                {
                    Id = int.Parse(p.Attribute("id")?.Value ?? "0"),
                    Colors = p.Elements("color").Select(c => new FigureDataColor
                    {
                        Id = int.Parse(c.Attribute("id")?.Value ?? "0"),
                        Index = int.Parse(c.Attribute("index")?.Value ?? "0"),
                        Club = int.Parse(c.Attribute("club")?.Value ?? "0"),
                        Selectable = c.Attribute("selectable")?.Value == "1",
                        HexCode = c.Value.Trim()
                    }).ToList()
                }).ToList();

                var setTypes = root.Element("sets")?.Elements("settype").Select(st => new FigureDataSetType
                {
                    Type = st.Attribute("type")?.Value ?? "",
                    PaletteId = int.Parse(st.Attribute("paletteid")?.Value ?? "0"),
                    MandatoryF0 = st.Attribute("mand_f_0")?.Value == "1",
                    MandatoryF1 = st.Attribute("mand_f_1")?.Value == "1",
                    MandatoryM0 = st.Attribute("mand_m_0")?.Value == "1",
                    MandatoryM1 = st.Attribute("mand_m_1")?.Value == "1",
                    Sets = st.Elements("set").Select(s =>
                    {
                        var hiddenLayers = s.Elements("hiddenlayers").Elements("layer")
                            .Select(hl => new FigureDataHiddenLayer
                            {
                                PartType = hl.Attribute("parttype")?.Value ?? ""
                            }).ToList();
                        if (hiddenLayers.Count == 0)
                        {
                            hiddenLayers = null;
                        }

                        return new FigureDataSet
                        {
                            Id = int.Parse(s.Attribute("id")?.Value ?? "0"),
                            Gender = s.Attribute("gender")?.Value ?? "",
                            Club = int.Parse(s.Attribute("club")?.Value ?? "0"),
                            Colorable = s.Attribute("colorable")?.Value == "1",
                            Selectable = s.Attribute("selectable")?.Value == "1",
                            Preselectable = s.Attribute("preselectable")?.Value == "1",
                            Sellable = disableSellable ? false : s.Attribute("sellable")?.Value == "1",
                            Parts = s.Elements("part").Select(p => new FigureDataPart
                            {
                                Id = int.Parse(p.Attribute("id")?.Value ?? "0"),
                                Type = p.Attribute("type")?.Value ?? "",
                                Colorable = p.Attribute("colorable")?.Value == "1",
                                Index = int.Parse(p.Attribute("index")?.Value ?? "0"),
                                ColorIndex = int.Parse(p.Attribute("colorindex")?.Value ?? "0")
                            }).ToList(),
                            HiddenLayers = hiddenLayers
                        };
                    }).ToList()
                }).ToList();

                var figureData = new FigureData
                {
                    Palettes = palettes,
                    SetTypes = setTypes
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    PropertyNamingPolicy = new ExactXmlNamingPolicy()
                };

                string json = JsonSerializer.Serialize(figureData, options);
                await File.WriteAllTextAsync(jsonOutputPath, json);

                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"📁 Downloaded: {jsonOutputPath}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error during conversion: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        private static async Task ConvertFigureMapToJson(string xmlFilePath, string jsonOutputPath)
        {
            if (!File.Exists(xmlFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: figuremap.xml does not exist.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(xmlFilePath);
                var root = doc.Root;
                var libraries = root?.Elements("lib").Select(lib =>
                {
                    int revision = 0;
                    string revValue = lib.Attribute("revision")?.Value;
                    if (!int.TryParse(revValue, out revision))
                    {
                        revision = 0;
                    }

                    var parts = lib.Elements("part").Select(part =>
                    {
                        int? partId = null;
                        string partIdValue = part.Attribute("id")?.Value;
                        if (int.TryParse(partIdValue, out int parsed))
                        {
                            partId = parsed;
                        }
                        return new FigureMapLibraryPart
                        {
                            Id = partId,
                            Type = part.Attribute("type")?.Value
                        };
                    }).ToList();

                    return new FigureMapLibrary
                    {
                        Id = lib.Attribute("id")?.Value,
                        Revision = revision,
                        Parts = parts
                    };
                }).ToList();

                var figureMap = new FigureMap
                {
                    Libraries = libraries
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    PropertyNamingPolicy = new ExactXmlNamingPolicy()
                };

                string json = JsonSerializer.Serialize(figureMap, options);
                await File.WriteAllTextAsync(jsonOutputPath, json);

                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"📁 Downloaded: {jsonOutputPath}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error during figuremap conversion: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        private static string BuildProgressBar(int processed, int total, int barWidth)
        {
            double fraction = (double)processed / total;
            int filledBars = (int)(fraction * barWidth);
            int emptyBars = barWidth - filledBars;
            return "[" + new string('▓', filledBars) + new string('-', emptyBars) + "]";
        }
    }
}
