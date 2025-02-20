namespace ConsoleApplication
{
    public static class QuestsDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadQuestsAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                Directory.CreateDirectory("./temp");

                string externaltexturl = config["AppSettings:externaltexturl"];
                if (string.IsNullOrEmpty(externaltexturl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: External Text URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                string questsurl = config["AppSettings:questsurl"];
                if (string.IsNullOrEmpty(questsurl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Quests URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                string tempFilePath = "./temp/external_texts.txt";

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

                await DownloadFileAsync(externaltexturl, tempFilePath, "external_texts.txt");

                Console.WriteLine("External Flash Texts Downloaded...");
                Console.WriteLine("Begin parsing...");

                int downloadCount = 0;
                using (StreamReader streamReader = new StreamReader(tempFilePath))
                {
                    string line;
                    while ((line = await streamReader.ReadLineAsync()) != null)
                    {
                        if (line.StartsWith("quests."))
                        {
                            string[] parts = line.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                string questName = parts[1].ToLower();
                                string questImage = parts[2].ToLower();

                                if (!File.Exists($"Habbo_Default/quests/{questName}_{questImage}.png") && !questImage.Contains("="))
                                {
                                    downloadCount += await DownloadQuestImageAsync(questsurl, questName, questImage);
                                }
                                else if (questImage.Contains("="))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"{questName}_{questImage}.png is not valid!");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }

                                if (!File.Exists($"Habbo_Default/quests/{questName}.png"))
                                {
                                    downloadCount += await DownloadQuestImageAsync(questsurl, questName, "");
                                }
                            }
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Finished Downloading Quest images!");
                Console.WriteLine($"{downloadCount} images have been downloaded!");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                if (Directory.Exists("./temp"))
                {
                    foreach (string file in Directory.GetFiles("./temp"))
                    {
                        File.Delete(file);
                    }
                    Directory.Delete("./temp");
                }
            }
        }

        private static async Task<int> DownloadQuestImageAsync(string baseUrl, string questName, string questImage)
        {
            try
            {
                string fileName = string.IsNullOrEmpty(questImage) ? $"{questName}.png" : $"{questName}_{questImage}.png";
                string filePath = $"Habbo_Default/quests/{fileName}";

                await DownloadFileAsync($"{baseUrl}/{fileName}", filePath, fileName);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;

                return 1;
            }
            catch (HttpRequestException ex)
            {
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
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
                throw;
            }
        }
    }
}