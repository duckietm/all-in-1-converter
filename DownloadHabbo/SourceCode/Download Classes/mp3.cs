namespace ConsoleApplication
{
    public static class Mp3Downloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadMp3sAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            string soundMachineUrl = config["AppSettings:soundmachineurl"];
            int mp3Number = 1;
            int errorCount = 0;

            while (errorCount < 20)
            {
                string fileName = $"./Habbo_Default/mp3/sound_machine_sample_{mp3Number}.mp3";

                if (File.Exists(fileName))
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"MP3 {mp3Number} already exists!");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    mp3Number++;
                    errorCount = 0;
                    continue;
                }

                try
                {
                    string downloadUrl = $"{soundMachineUrl}{mp3Number}.mp3";
                    await DownloadFileAsync(downloadUrl, fileName, $"MP3 {mp3Number}");

                    mp3Number++;
                    errorCount = 0;
                }
                catch (HttpRequestException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error downloading MP3 {mp3Number}: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    errorCount++;
                    mp3Number++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unexpected error downloading MP3 {mp3Number}: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    errorCount++;
                    mp3Number++;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished downloading MP3s!");
            Console.ForegroundColor = ConsoleColor.Gray;
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