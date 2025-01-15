namespace ConsoleApplication
{
    public static class VariablesDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadVariablesAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                Directory.CreateDirectory("./files");

                Console.WriteLine("Saving External Variables...");

                string externalvarsurl = config["AppSettings:externalvarsurl"];
                if (string.IsNullOrEmpty(externalvarsurl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: External Variables URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                if (!Uri.TryCreate(externalvarsurl, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid External Variables URL.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CommonConfig.UserAgent);

                int retryCount = 3;
                while (retryCount > 0)
                {
                    try
                    {
                        await DownloadFileAsync(externalvarsurl, "./files/external_variables.txt", "external_variables.txt");

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("External Variables Saved");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        retryCount--;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Error downloading external variables: {ex.Message}. Retries left: {retryCount}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        if (retryCount == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to download external variables after 3 attempts.");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading external variables: " + ex.Message);
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