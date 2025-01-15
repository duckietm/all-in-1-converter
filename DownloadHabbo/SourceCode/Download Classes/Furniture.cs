using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ConsoleApplication
{
    internal static class FurnitureDownloader
    {
        internal static void DownloadFurniture()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:furnidataTXT"];
            string furnitureUrl = config["AppSettings:furnitureurl"];
            Console.WriteLine("Furniture Download Started");

            if (!Directory.Exists("./hof_furni"))
                Directory.CreateDirectory("./hof_furni");
            if (!Directory.Exists("./hof_furni/icons"))
                Directory.CreateDirectory("./hof_furni/icons");
            if (!Directory.Exists("./temp"))
                Directory.CreateDirectory("./temp");

            string furnidataTxtPath = "./temp/furnidata.txt";
            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                Console.WriteLine("Downloading furnidata...");
                webClient.DownloadFile(furnidataUrl, furnidataTxtPath);
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

                    string swfFilePath = $"./hof_furni/{furnitureName}.swf";
                    string iconFilePath = $"./hof_furni/icons/{iconName}_icon.png";

                    if (!File.Exists(swfFilePath))
                    {
                        string swfUrl = $"{furnitureUrl}/{furnitureDir}/{furnitureName}.swf";

                        if (!FileExistsOnServer(swfUrl))
                        {
                            continue;
                        }

                        try
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Downloading: {furnitureName}.swf");
                            webClient.DownloadFile(swfUrl, swfFilePath);
                            downloadedCount++;
                        }
                        catch (WebException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error downloading {furnitureName}.swf: {ex.Message}");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            continue;
                        }
                    }

                    if (File.Exists(iconFilePath))
                    {
                        continue;
                    }

                    string iconUrl = $"{furnitureUrl}/{furnitureDir}/{iconName}_icon.png";

                    if (!FileExistsOnServer(iconUrl))
                    {
                        continue;
                    }

                    try
                    {
                        Console.WriteLine($"Downloading: {iconName}_icon.png");
                        webClient.DownloadFile(iconUrl, iconFilePath);
                    }
                    catch (WebException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error downloading {iconName}_icon.png: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
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

        private static bool FileExistsOnServer(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "HEAD";
                using (WebResponse response = request.GetResponse())
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}