using System;
using System.IO;
using System.Net;

namespace ConsoleApplication
{
    public static class ReceptionDownloader
    {
        public static void DownloadReceptionImages()
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

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add($"User-Agent: {CommonConfig.UserAgent}");
                webClient.DownloadFile("https://www.habbo.com/gamedata/external_variables/", externalVariablesPath);

                Console.WriteLine("Let's start downloading!");
                int downloadCount = 0;

                using (StreamReader streamReader = new StreamReader(externalVariablesPath))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.Contains("reception/"))
                        {
                            ProcessImageLine(line, "reception/", "./reception", "receptionurl", webClient, ref downloadCount);
                        }
                        if (line.Contains("catalogue/"))
                        {
                            ProcessImageLine(line, "catalogue/", "./reception/catalogue", "catalogurl", webClient, ref downloadCount);
                        }
                        if (line.Contains("web_promo_small/"))
                        {
                            ProcessImageLine(line, "web_promo_small/", "./reception/web_promo_small", "promosmallurl", webClient, ref downloadCount);
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Finished downloading {downloadCount} images");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

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

        private static void ProcessImageLine(string line, string splitString, string saveDirectory, string configKey, WebClient webClient, ref int downloadCount)
        {
            string[] parts = line.Split(new string[] { splitString }, StringSplitOptions.None);
            if (parts.Length < 2) return;

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
                    return;
                }

                string baseUrl = config[$"AppSettings:{configKey}"];
                string fullUrl = $"{baseUrl}/{fileName}";

                if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Skipping invalid URL: {fullUrl}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                string filePath = Path.Combine(saveDirectory, fileName);
                if (!File.Exists(filePath))
                {
                     int retryCount = 3;
                    while (retryCount > 0)
                    {
                        try
                        {
                            webClient.DownloadFile(fullUrl, filePath);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Downloading {fileName}");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            downloadCount++;
                            break;
                        }
                        catch (WebException ex)
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
        }
    }
}