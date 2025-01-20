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
                                Console.WriteLine("Downloading Effects version: " + releaseEffect);
                                break;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Error: Insufficient parts in URL to determine release effect.");
                                return; // Exit gracefully
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error processing line: {ex.Message}");
                        return; // Exit gracefully
                    }
                }

                if (string.IsNullOrEmpty(releaseEffect))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not determine the release version.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                if (!Directory.Exists("./effect"))
                {
                    Directory.CreateDirectory("./effect");
                }

                string effectMapUrl = $"{effectUrl}/{releaseEffect}/effectmap.xml";
                await DownloadFileAsync(effectMapUrl, "./effect/effectmap.xml", "effectmap.xml");

                string habboAvatarActionsUrl = $"{effectUrl}/{releaseEffect}/HabboAvatarActions.xml";
                await DownloadFileAsync(habboAvatarActionsUrl, "./effect/HabboAvatarActions.xml", "HabboAvatarActions.xml");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Effects Downloaded and Saved");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading effects: " + ex.Message);
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
            }
        }
    }
}