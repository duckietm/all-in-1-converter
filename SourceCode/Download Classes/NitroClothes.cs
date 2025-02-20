using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    internal static class NitroClothesDownloader
    {
        private const string NitroClothesDirKey = "AppSettings:nitro_clothes_dir";
        private const string NitroFigureDataKey = "AppSettings:nitro_figuredata";
        private const string NitroFigureMapKey = "AppSettings:nitro_figuremap";

        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task DownloadCustomClothesAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            if (!config.TryGetValue(NitroClothesDirKey, out string nitroClothesDir) ||
                !config.TryGetValue(NitroFigureDataKey, out string nitroFigureData) ||
                !config.TryGetValue(NitroFigureMapKey, out string nitroFigureMap))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error! Missing required keys in config.ini.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            try
            {
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string figureMapUrlWithTimestamp = $"{nitroFigureMap}?timestamp={timestamp}";

                string figureMapContent = await httpClient.GetStringAsync(figureMapUrlWithTimestamp);

                string currentDirectory = Environment.CurrentDirectory;
                string downloadDirectory = Path.Combine(currentDirectory, "./custom_downloads/clothes");

                Directory.CreateDirectory(downloadDirectory);

                string figureDataUrlWithTimestamp = $"{nitroFigureData}?timestamp={timestamp}";
                await DownloadFileAsync(figureDataUrlWithTimestamp, Path.Combine(downloadDirectory, "FigureData.json"), "FigureData.json");

                await DownloadFileAsync(figureMapUrlWithTimestamp, Path.Combine(downloadDirectory, "FigureMap.json"), "FigureMap.json");

                string figureMapFilePath = Path.Combine(downloadDirectory, "FigureMap.json");

                try
                {
                    string jsonContent = File.ReadAllText(figureMapFilePath);
                    JObject figureMap = JObject.Parse(jsonContent);

                    int totalFilesProcessed = 0;
                    int totalFilesDownloaded = 0;
                    int totalFilesSkipped = 0;

                    foreach (var library in figureMap["libraries"])
                    {
                        string id = library["id"].ToString();
                        totalFilesProcessed++;

                        if (id == "hh_human_fx" || id == "hh_pets")
                        {
                            continue;
                        }

                        try
                        {
                            string filePath = Path.Combine(downloadDirectory, $"{id}.nitro");
                            if (!File.Exists(filePath))
                            {
                                string downloadUrl = $"{nitroClothesDir}/{id}.nitro";
                                await DownloadFileAsync(downloadUrl, filePath, $"{id}.nitro");

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Downloaded: {id}.nitro");
                                Console.ForegroundColor = ConsoleColor.Gray;

                                totalFilesDownloaded++;
                            }
                            else
                            {
                                totalFilesSkipped++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error downloading {id}.nitro: {ex.Message}");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n--- Summary ---");
                    Console.WriteLine($"Total files processed: {totalFilesProcessed}");
                    Console.WriteLine($"Total files downloaded: {totalFilesDownloaded}");
                    Console.WriteLine($"Total files skipped: {totalFilesSkipped}");
                    Console.WriteLine("---");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    if (totalFilesDownloaded > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloaded {totalFilesDownloaded} new clothes!");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("You have the latest clothes!");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error reading or parsing {figureMapFilePath}: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine();
                Console.WriteLine("All has been done!");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error downloading custom clothes: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
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