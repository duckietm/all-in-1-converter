using System.Text.RegularExpressions;


namespace ConsoleApplication
{
    internal static class FurnitureDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task DownloadFurnitureAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:furnidataTXT"];
            string furnitureUrl = config["AppSettings:furnitureurl"];

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            Directory.CreateDirectory("./Habbo_Default/hof_furni");
            Directory.CreateDirectory("./Habbo_Default/icons");
            Directory.CreateDirectory("./temp");

            string furnidataTxtPath = "./temp/furnidata.txt";

            try
            {
                Console.WriteLine("Downloading furnidata...");
                await DownloadFileAsync(furnidataUrl, furnidataTxtPath, "furnidata.txt");
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

            try
            {
                string furnidataContent = File.ReadAllText(furnidataTxtPath);
                string pattern = @"\[""(.*?)"",""(.*?)"",""(.*?)"",""(.*?)""\]";
                MatchCollection matches = Regex.Matches(furnidataContent, pattern);

                Console.WriteLine($"Found {matches.Count} furniture entries.");

                foreach (Match match in matches)
                {
                    if (match.Groups.Count < 5)
                        continue;

                    string furnitureNameWithMetadata = match.Groups[3].Value.Replace("\"", "");
                    string furnitureDirWithMetadata = match.Groups[4].Value.Replace("\"", "");

                    string furnitureName = furnitureNameWithMetadata.Split('*')[0];
                    string variant = furnitureNameWithMetadata.Contains('*') ? furnitureNameWithMetadata.Split('*')[1] : "";

                    string iconName = string.IsNullOrEmpty(variant) ? furnitureName : $"{furnitureName}_{variant}";
                    string furnitureDir = furnitureDirWithMetadata.Split(',')[0];

                    string swfFilePath = $"./Habbo_Default/hof_furni/{furnitureName}.swf";
                    string iconFilePath = $"./Habbo_Default/hof_furni/icons/{iconName}_icon.png";

                    if (!File.Exists(swfFilePath))
                    {
                        string swfUrl = $"{furnitureUrl}/{furnitureDir}/{furnitureName}.swf";

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
                        string iconUrl = $"{furnitureUrl}/{furnitureDir}/{iconName}_icon.png";

                        if (await FileExistsOnServerAsync(iconUrl))
                        {
                            try
                            {
                                await DownloadFileAsync(iconUrl, iconFilePath, $"{iconName}_icon.png");
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
                Console.WriteLine($"Downloaded {downloadedCount} new furniture items.");
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