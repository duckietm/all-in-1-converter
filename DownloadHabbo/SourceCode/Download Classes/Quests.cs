using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;

namespace ConsoleApplication
{
    public static class QuestsDownloader
    {
        public static void DownloadQuests()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                if (!Directory.Exists("./temp"))
                {
                    Directory.CreateDirectory("./temp");
                }

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

                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add($"User-Agent: {CommonConfig.UserAgent}");
                    webClient.DownloadFile(externaltexturl, tempFilePath);

                    Console.WriteLine("External Flash Texts Downloaded...");
                    Console.WriteLine("Begin parsing...");

                    int downloadCount = 0;
                    using (StreamReader streamReader = new StreamReader(tempFilePath))
                    {
                        Thread.Sleep(1000);
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            if (line.StartsWith("quests."))
                            {
                                string[] parts = line.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 3)
                                {
                                    string questName = parts[1].ToLower();
                                    string questImage = parts[2].ToLower();

                                    if (!File.Exists($"quests/{questName}_{questImage}.png") && !questImage.Contains("="))
                                    {
                                        DownloadQuestImage(webClient, questsurl, questName, questImage, ref downloadCount);
                                    }
                                    else if (questImage.Contains("="))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"{questName}_{questImage}.png is not valid!");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                                        Console.WriteLine($"{questName}_{questImage}.png already exists!");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                    }

                                    if (!File.Exists($"quests/{questName}.png"))
                                    {
                                        DownloadQuestImage(webClient, questsurl, questName, "", ref downloadCount);
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                                        Console.WriteLine($"{questName}.png already exists!");
                                        Console.ForegroundColor = ConsoleColor.Gray;
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
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading quests: " + ex.Message);
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

        private static void DownloadQuestImage(WebClient webClient, string baseUrl, string questName, string questImage, ref int downloadCount)
        {
            try
            {
                string fileName = string.IsNullOrEmpty(questImage) ? $"{questName}.png" : $"{questName}_{questImage}.png";
                string filePath = $"quests/{fileName}";

                webClient.Headers.Add("user-agent", "Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/70.0.3538.102+Safari/537.36+Edge/18.18362;)");
                webClient.DownloadFile($"{baseUrl}/{fileName}", filePath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;

                downloadCount++;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error downloading: {questName}_{questImage}.png");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}