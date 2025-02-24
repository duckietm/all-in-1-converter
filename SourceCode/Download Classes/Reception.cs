using System.Text.RegularExpressions;

namespace ConsoleApplication
{
    public static class ReceptionDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadReceptionImages()
        {
            Console.WriteLine("Starting Reception Download...");

            EnsureDirectoryExists("./temp");

            string externalVariablesPath = "./temp/external_variables.txt";
            Console.WriteLine("Downloading external variables");

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            var externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/";
            var externalVariablesContent = await httpClient.GetStringAsync(externalVariablesUrl);
            await File.WriteAllTextAsync(externalVariablesPath, externalVariablesContent);

            Console.WriteLine("🚀 Let's start downloading some images!");
            int downloadCount = 0;

            using (StreamReader streamReader = new StreamReader(externalVariablesPath))
            {
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    if (line.Contains("${image.library.url}"))
                    {
                        downloadCount = await ProcessImageLineAsync(line, "${image.library.url}", "./Habbo_Default/reception", downloadCount);
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🎉 Finished downloading {downloadCount} images");
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

        private static async Task<int> ProcessImageLineAsync(string line, string splitString, string saveDirectory, int downloadCount)
        {
            string[] parts = line.Split(new string[] { splitString }, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                Console.WriteLine("❌ Skipping line (not split correctly)");
                return downloadCount;
            }

            string[] fileParts = parts[1].Split(new string[] { ",", ";" }, StringSplitOptions.None);
            string filePath = fileParts[0].Trim().TrimEnd('}', '"');

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("❌ Skipping line (empty filename)");
                return downloadCount;
            }

            // Ignore non-image files
            if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                !filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                !filePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Skipping non-image file: {filePath}");
                return downloadCount;
            }

            string subFolder = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(subFolder))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"❌ No subfolder found for: {filePath}. Saving in 'other/'");
                Console.ForegroundColor = ConsoleColor.Gray;
                subFolder = "other";
            }

            string baseUrl = "https://images.habbo.com/c_images/";
            string fullUrl = $"{baseUrl}{filePath}";
            string finalSaveDirectory = Path.Combine(saveDirectory, subFolder);
            EnsureDirectoryExists(finalSaveDirectory);
            string fullFilePath = Path.Combine(finalSaveDirectory, Path.GetFileName(filePath));

            if (File.Exists(fullFilePath))
            {
                return downloadCount;
            }

            int retryCount = 3;
            while (retryCount > 0)
            {
                try
                {
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/png"));
                    httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");

                    var imageBytes = await httpClient.GetByteArrayAsync(fullUrl);
                    await File.WriteAllBytesAsync(fullFilePath, imageBytes);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"📁 Downloaded: {filePath}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    downloadCount++;
                    break;
                }
                catch (HttpRequestException ex)
                {
                    retryCount--;
                }
            }
            return downloadCount;
        }
    }
}
