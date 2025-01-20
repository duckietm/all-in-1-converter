namespace ConsoleApplication
{
    public static class IconDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadIcons()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            Console.WriteLine("Catalogue Icons Download Started");

            Directory.CreateDirectory("./icons");

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            string catalogIconUrl = config["AppSettings:catalogiconurl"];
            int iconNumber = 1;
            int consecutiveErrors = 0;

            while (consecutiveErrors < 20)
            {
                string filePath = $"./icons/icon_{iconNumber}.png";

                if (!File.Exists(filePath))
                {
                    try
                    {
                        string url = $"{catalogIconUrl}{iconNumber}.png";
                        byte[] iconData = await httpClient.GetByteArrayAsync(url);

                        await File.WriteAllBytesAsync(filePath, iconData);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloaded Icon {iconNumber}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        consecutiveErrors = 0;
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Icon {iconNumber} is not found on the server.");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        consecutiveErrors++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error downloading icon {iconNumber}: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        consecutiveErrors++;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"Icon {iconNumber} already exists!");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    consecutiveErrors = 0;
                }

                iconNumber++;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished downloading icons!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}