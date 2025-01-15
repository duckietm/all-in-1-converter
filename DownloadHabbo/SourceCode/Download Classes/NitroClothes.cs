using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    internal static class NitroClothesDownloader
    {
        // Constants for config keys
        private const string NitroClothesDirKey = "AppSettings:nitro_clothes_dir";
        private const string NitroFigureDataKey = "AppSettings:nitro_figuredata";
        private const string NitroFigureMapKey = "AppSettings:nitro_figuremap";

        internal static async Task DownloadCustomClothesAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            // Check if required keys exist in the config
            if (!config.TryGetValue(NitroClothesDirKey, out string nitro_clothes_dir) ||
                !config.TryGetValue(NitroFigureDataKey, out string nitro_figuredata) ||
                !config.TryGetValue(NitroFigureMapKey, out string nitro_figuremap))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error! Missing required keys in config.ini.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            using (HttpClient httpClient_CustomClothes = new HttpClient())
            {
                httpClient_CustomClothes.DefaultRequestHeaders.Add("User-Agent", CommonConfig.UserAgent);

                try
                {
                    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    string figuremapUrlWithTimestamp = $"{nitro_figuremap}?timestamp={timestamp}";

                    HttpResponseMessage res = await httpClient_CustomClothes.GetAsync(figuremapUrlWithTimestamp);
                    string source = await res.Content.ReadAsStringAsync();

                    string CurrentDirectory = Environment.CurrentDirectory;
                    string DownloadDirectory = Path.Combine(CurrentDirectory, "Nitro_clothes");

                    if (!Directory.Exists(DownloadDirectory))
                    {
                        Directory.CreateDirectory(DownloadDirectory);
                    }

                    using (WebClient customclothes = new WebClient())
                    {
                        customclothes.Headers.Add("User-Agent", CommonConfig.UserAgent);

                        try
                        {
                            string figuredataUrlWithTimestamp = $"{nitro_figuredata}?timestamp={timestamp}";
                            Console.ForegroundColor = ConsoleColor.Green;
                            customclothes.DownloadFile(figuredataUrlWithTimestamp, Path.Combine(DownloadDirectory, "FigureData.json"));
                            Console.WriteLine("Downloaded FigureData.json\n");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error! Can't download FigureData.json: " + ex.Message);
                            Console.WriteLine();
                        }

                        try
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            customclothes.DownloadFile(figuremapUrlWithTimestamp, Path.Combine(DownloadDirectory, "FigureMap.json"));
                            Console.WriteLine("Downloaded FigureMap.json\n");
                         }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error! Can't download FigureMap.json: " + ex.Message);
                            Console.WriteLine();
                        }

                        string figuremapfile = Path.Combine(DownloadDirectory, "FigureMap.json");

                        try
                        {
                            string jsonContent = File.ReadAllText(figuremapfile);
                            JObject figuremap = JObject.Parse(jsonContent);

                            int TotalFilesProcessed = 0;
                            int TotalFilesDownloaded = 0;
                            int TotalFilesSkipped = 0;

                            foreach (var library in figuremap["libraries"])
                            {
                                string id = library["id"].ToString();
                                TotalFilesProcessed++;

                                if (id == "hh_human_fx" || id == "hh_pets")
                                {
                                    continue;
                                }

                                try
                                {
                                    string filePath = Path.Combine(DownloadDirectory, id + ".nitro");
                                    if (!File.Exists(filePath))
                                    {
                                        string downloadUrl = $"{nitro_clothes_dir}/{id}.nitro";
                                        customclothes.DownloadFile(downloadUrl, filePath);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Downloaded: " + id + ".nitro");
                                        Console.ForegroundColor = ConsoleColor.Gray;
                                        TotalFilesDownloaded++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error when downloading file: " + id + ".nitro");
                                    Console.WriteLine("Exception: " + ex.Message);
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }
                            }

                            // Display summary
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("\n--- Summary ---");
                            Console.WriteLine($"Total files processed: {TotalFilesProcessed}");
                            Console.WriteLine($"Total files downloaded: {TotalFilesDownloaded}");
                            Console.WriteLine($"Total files skipped: {TotalFilesSkipped}");
                            Console.WriteLine("---");
                            Console.ForegroundColor = ConsoleColor.Gray;

                            if (TotalFilesDownloaded > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Downloaded " + TotalFilesDownloaded + " new clothes!");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("You have the latest clothes!");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error! Can't read or parse file: " + figuremapfile);
                            Console.WriteLine("Exception: " + ex.Message);
                            Console.WriteLine();
                        }

                        Console.WriteLine();
                        Console.WriteLine("All has been done!");
                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}