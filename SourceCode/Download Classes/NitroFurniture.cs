using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    internal static class NitroFurnitureDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // Dictionary to hold semaphores for each file path.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        internal static async Task DownloadFurnitureAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:nitro_furnidataJSON"];
            string furnitureUrl = config["AppSettings:nitro_furnitureurl"];
            string furnitureIconUrl = config["AppSettings:nitro_furniture_icon_url"];

            Directory.CreateDirectory("./temp");

            string furnidataJsonPath = "./temp/furnidata.json";

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            try
            {
                Console.WriteLine("Downloading furnidata...");
                await DownloadFileAsync(furnidataUrl, furnidataJsonPath, "furnidata.json");
                Console.WriteLine("Furnidata downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading furnidata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            int nitroDownloadCount = 0;
            int iconDownloadCount = 0;

            try
            {
                string furnidataContent = File.ReadAllText(furnidataJsonPath);
                var furnidata = JsonSerializer.Deserialize<Furnidata>(furnidataContent);

                if (furnidata?.RoomItemTypes?.FurniType != null)
                {
                    var result = await ProcessFurniTypeAsync(furnidata.RoomItemTypes.FurniType, furnitureUrl, furnitureIconUrl);
                    nitroDownloadCount += result.NitroCount;
                    iconDownloadCount += result.IconCount;
                }

                if (furnidata?.WallItemTypes?.FurniType != null)
                {
                    var result = await ProcessFurniTypeAsync(furnidata.WallItemTypes.FurniType, furnitureUrl, furnitureIconUrl);
                    nitroDownloadCount += result.NitroCount;
                    iconDownloadCount += result.IconCount;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Downloading furniture completed!");
                Console.WriteLine($"Downloaded {nitroDownloadCount} .nitro files and {iconDownloadCount} .png icons.");
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

            Console.WriteLine("Press Enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }

        private static async Task<(int NitroCount, int IconCount)> ProcessFurniTypeAsync(FurniType[] furniTypes, string furnitureUrl, string furnitureIconUrl)
        {
            int nitroDownloadCount = 0;
            int iconDownloadCount = 0;
            int maxConcurrency = 100;

            using SemaphoreSlim globalSemaphore = new SemaphoreSlim(maxConcurrency);

            List<Task> tasks = new List<Task>();

            foreach (var furni in furniTypes)
            {
                tasks.Add(Task.Run(async () =>
                {
                    string classname = furni.Classname;
                    string baseClassname = classname.Split('*')[0];
                    string nitroFilePath = $"./custom_downloads/nitro_furniture/{baseClassname}.nitro";
                    string iconFilePath = $"./custom_downloads/nitro_furniture/icons/{classname.Replace('*', '_')}_icon.png";

                    string nitroUrl = $"{furnitureUrl}/{baseClassname}.nitro";
                    string iconUrl = $"{furnitureIconUrl}/{classname.Replace('*', '_')}_icon.png";

                    if (!File.Exists(nitroFilePath))
                    {
                        SemaphoreSlim fileLock = fileLocks.GetOrAdd(nitroFilePath, _ => new SemaphoreSlim(1, 1));
                        await fileLock.WaitAsync();
                        try
                        {
                            if (!File.Exists(nitroFilePath))
                            {
                                await globalSemaphore.WaitAsync();
                                try
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"Downloading: {baseClassname}.nitro");
                                    await DownloadFileAsync(nitroUrl, nitroFilePath, $"{baseClassname}.nitro");
                                    Interlocked.Increment(ref nitroDownloadCount);
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Downloading {baseClassname}.nitro => Failed: {ex.Message}");
                                }
                                finally
                                {
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    globalSemaphore.Release();
                                }
                            }
                        }
                        finally
                        {
                            fileLock.Release();
                        }
                    }

                    if (!File.Exists(iconFilePath))
                    {
                        SemaphoreSlim fileLock = fileLocks.GetOrAdd(iconFilePath, _ => new SemaphoreSlim(1, 1));
                        await fileLock.WaitAsync();
                        try
                        {
                            if (!File.Exists(iconFilePath))
                            {
                                await globalSemaphore.WaitAsync();
                                try
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"Downloading: {classname.Replace('*', '_')}_icon.png");
                                    await DownloadFileAsync(iconUrl, iconFilePath, $"{classname.Replace('*', '_')}_icon.png");
                                    Interlocked.Increment(ref iconDownloadCount);
                                }
                                catch (Exception ex)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"Downloading {classname.Replace('*', '_')}_icon.png => Failed: {ex.Message}");
                                }
                                finally
                                {
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    globalSemaphore.Release();
                                }
                            }
                        }
                        finally
                        {
                            fileLock.Release();
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return (nitroDownloadCount, iconDownloadCount);
        }

        private static async Task DownloadFileAsync(string url, string filePath, string fileName)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

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

        private class Furnidata
        {
            [JsonPropertyName("roomitemtypes")]
            public ItemTypes RoomItemTypes { get; set; }

            [JsonPropertyName("wallitemtypes")]
            public ItemTypes WallItemTypes { get; set; }
        }

        private class ItemTypes
        {
            [JsonPropertyName("furnitype")]
            public FurniType[] FurniType { get; set; }
        }

        private class FurniType
        {
            [JsonPropertyName("classname")]
            public string Classname { get; set; }
        }
    }
}
