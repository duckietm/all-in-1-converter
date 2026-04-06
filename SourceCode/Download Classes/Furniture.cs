using System.Xml.Linq;


namespace ConsoleApplication
{
    internal static class FurnitureDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task DownloadFurnitureAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:furnidataXML"];
            string furnitureUrl = config["AppSettings:furnitureurl"];

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            Directory.CreateDirectory("./Habbo_Default/hof_furni");
            Directory.CreateDirectory("./Habbo_Default/hof_furni/icons");
            Directory.CreateDirectory("./temp");

            string furnidataXmlPath = "./temp/furnidata.xml";

            try
            {
                Console.WriteLine("Downloading furnidata...");
                await DownloadFileAsync(furnidataUrl, furnidataXmlPath, "furnidata.xml");
                Console.WriteLine("Furnidata downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading furnidata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            int downloadedCount = 0;
            int iconDownloadCount = 0;

            try
            {
                XDocument doc = XDocument.Load(furnidataXmlPath);
                var root = doc.Element("furnidata");
                if (root == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid furnidata XML format.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                var furniEntries = new List<(string classname, int revision)>();

                var roomItems = root.Element("roomitemtypes");
                if (roomItems != null)
                {
                    foreach (var item in roomItems.Elements("furnitype"))
                    {
                        string classname = (string)item.Attribute("classname") ?? "";
                        int revision = (int?)item.Element("revision") ?? 0;
                        if (!string.IsNullOrEmpty(classname))
                            furniEntries.Add((classname, revision));
                    }
                }

                var wallItems = root.Element("wallitemtypes");
                if (wallItems != null)
                {
                    foreach (var item in wallItems.Elements("furnitype"))
                    {
                        string classname = (string)item.Attribute("classname") ?? "";
                        int revision = (int?)item.Element("revision") ?? 0;
                        if (!string.IsNullOrEmpty(classname))
                            furniEntries.Add((classname, revision));
                    }
                }

                Console.WriteLine($"Found {furniEntries.Count} furniture entries.");

                foreach (var (classname, revision) in furniEntries)
                {
                    string furnitureName = classname.Split('*')[0];
                    string variant = classname.Contains('*') ? classname.Split('*')[1] : "";
                    string iconName = string.IsNullOrEmpty(variant) ? furnitureName : $"{furnitureName}_{variant}";

                    string swfFilePath = $"./Habbo_Default/hof_furni/{furnitureName}.swf";
                    string iconFilePath = $"./Habbo_Default/hof_furni/icons/{iconName}_icon.png";

                    if (!File.Exists(swfFilePath))
                    {
                        string swfUrl = $"{furnitureUrl}/{revision}/{furnitureName}.swf";

                        if (await FileExistsOnServerAsync(swfUrl))
                        {
                            try
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Downloading: {furnitureName}.swf");
                                await DownloadFileAsync(swfUrl, swfFilePath, $"{furnitureName}.swf");
                                downloadedCount++;
                            }
                            catch (HttpRequestException ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error downloading {furnitureName}.swf: {ex.Message}");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                    }

                    if (!File.Exists(iconFilePath))
                    {
                        string iconUrl = $"{furnitureUrl}/{revision}/{iconName}_icon.png";

                        if (await FileExistsOnServerAsync(iconUrl))
                        {
                            try
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Downloading: {iconName}_icon.png");
                                await DownloadFileAsync(iconUrl, iconFilePath, $"{iconName}_icon.png");
                                iconDownloadCount++;
                            }
                            catch (HttpRequestException ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error downloading {iconName}_icon.png: {ex.Message}");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Downloading furniture completed!");
                Console.WriteLine($"Downloaded {downloadedCount} new .swf files and {iconDownloadCount} new icons.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                foreach (string file in Directory.GetFiles("./temp"))
                {
                    File.Delete(file);
                }
                Directory.Delete("./temp");
            }
        }

        private static async Task<bool> FileExistsOnServerAsync(string url)
        {
            try
            {
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task DownloadFileAsync(string url, string filePath, string fileName)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error downloading {fileName}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
        }
    }
}
