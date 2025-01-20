namespace ConsoleApplication
{
    public static class ReceptionDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadReceptionImages()
        {
            Console.WriteLine("Starting Reception Download...");

            EnsureDirectoryExists("./temp");
            EnsureDirectoryExists("./reception");
            EnsureDirectoryExists("./reception/catalogue");
            EnsureDirectoryExists("./reception/web_promo_small");

            Console.WriteLine("This downloads not all images. Only the ones that are defined in the external_variables.");
            Console.WriteLine("Run this once in a while to collect all images!");
            Console.WriteLine("Catalogue Teasers used on the reception are stored in /catalogue/");
            Console.WriteLine("web_promo_small images used on the reception are stored in /reception/web_promo_small");
            Console.WriteLine();

            string externalVariablesPath = "./temp/external_variables.txt";
            Console.WriteLine("Downloading external variables");

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            var externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/";
            var externalVariablesContent = await httpClient.GetStringAsync(externalVariablesUrl);
            await File.WriteAllTextAsync(externalVariablesPath, externalVariablesContent);

            Console.WriteLine("Let's start downloading!");
            int downloadCount = 0;

            using (StreamReader streamReader = new StreamReader(externalVariablesPath))
            {
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    if (line.Contains("reception/"))
                    {
                        downloadCount = await ProcessImageLineAsync(line, "reception/", "./reception", "receptionurl", downloadCount);
                    }
                    if (line.Contains("catalogue/"))
                    {
                        downloadCount = await ProcessImageLineAsync(line, "catalogue/", "./reception/catalogue", "catalogurl", downloadCount);
                    }
                    if (line.Contains("web_promo_small/"))
                    {
                        downloadCount = await ProcessImageLineAsync(line, "web_promo_small/", "./reception/web_promo_small", "promosmallurl", downloadCount);
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Finished downloading {downloadCount} images");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (Directory.Exists("./temp"))
            {
                foreach (string file in Directory.GetFiles("./temp"))
                {
                    File.Delete(file);
                }
                Directory.Delete("./temp");
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static async Task<int> ProcessImageLineAsync(string line, string splitString, string saveDirectory, string configKey, int downloadCount)
        {
            string[] parts = line.Split(new string[] { splitString }, StringSplitOptions.None);
            if (parts.Length < 2) return downloadCount;

            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                string[] fileParts = parts[1].Split(new string[] { ",", ";" }, StringSplitOptions.None);
                string fileName = fileParts[0].Trim();

                if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Skipping invalid file: {fileName}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return downloadCount;
                }

                string baseUrl = config[$"AppSettings:{configKey}"];
                string fullUrl = $"{baseUrl}/{fileName}";

                if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Skipping invalid URL: {fullUrl}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return downloadCount;
                }

                string filePath = Path.Combine(saveDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    int retryCount = 3;
                    while (retryCount > 0)
                    {
                        try
                        {
                            var imageBytes = await httpClient.GetByteArrayAsync(fullUrl);
                            await File.WriteAllBytesAsync(filePath, imageBytes);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Downloading {fileName}");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            downloadCount++;
                            break;
                        }
                        catch (HttpRequestException ex)
                        {
                            retryCount--;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error downloading {fileName}: {ex.Message}. Retries left: {retryCount}");
                            Console.ForegroundColor = ConsoleColor.Gray;

                            if (retryCount == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Failed to download {fileName} after 3 attempts.");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"{fileName} already exists!");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error processing line: {parts[1]}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            return downloadCount;
        }
    }
}