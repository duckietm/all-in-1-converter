using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleApplication
{
    internal static class NitroFurnitureDownloader
    {
        internal static void DownloadFurniture()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:nitro_furnidataJSON"];
            string furnitureUrl = config["AppSettings:nitro_furnitureurl"];
            string furnitureIconUrl = config["AppSettings:nitro_furniture_icon_url"];

            Console.WriteLine("Furniture Download Started");

            // Create necessary directories
            Directory.CreateDirectory("./Nitro_hof_furni");
            Directory.CreateDirectory("./Nitro_hof_furni/icons");
            Directory.CreateDirectory("./temp");

            string furnidataJsonPath = "./temp/furnidata.json";
            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                Console.WriteLine("Downloading furnidata...");
                webClient.DownloadFile(furnidataUrl, furnidataJsonPath);
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
                    var result = ProcessFurniType(furnidata.RoomItemTypes.FurniType, furnitureUrl, furnitureIconUrl, webClient);
                    nitroDownloadCount += result.NitroCount;
                    iconDownloadCount += result.IconCount;
                }

                if (furnidata?.WallItemTypes?.FurniType != null)
                {
                    var result = ProcessFurniType(furnidata.WallItemTypes.FurniType, furnitureUrl, furnitureIconUrl, webClient);
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
                // Clean up temporary files
                if (Directory.Exists("./temp"))
                {
                    foreach (string file in Directory.GetFiles("./temp"))
                    {
                        File.Delete(file);
                    }
                    Directory.Delete("./temp");
                }
            }

            // Prompt user to press Enter to continue
            Console.WriteLine("Press Enter to exit...");
            while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
        }

        private static (int NitroCount, int IconCount) ProcessFurniType(FurniType[] furniTypes, string furnitureUrl, string furnitureIconUrl, WebClient webClient)
        {
            int nitroDownloadCount = 0;
            int iconDownloadCount = 0;

            foreach (var furni in furniTypes)
            {
                string classname = furni.Classname;
                string baseClassname = classname.Split('*')[0];
                string nitroFilePath = $"./Nitro_hof_furni/{baseClassname}.nitro";
                string iconFilePath = $"./Nitro_hof_furni/icons/{classname.Replace('*', '_')}_icon.png";

                string nitroUrl = $"{furnitureUrl}/{baseClassname}.nitro";
                string iconUrl = $"{furnitureIconUrl}/{classname.Replace('*', '_')}_icon.png";

                if (!File.Exists(nitroFilePath))
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloading: {baseClassname}.nitro");
                        webClient.DownloadFile(nitroUrl, nitroFilePath);
                        Console.WriteLine($"Successfully downloaded: {baseClassname}.nitro");
                        nitroDownloadCount++;
                    }
                    catch (WebException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (ex.Response is HttpWebResponse response)
                        {
                            Console.WriteLine($"Downloading {classname}.nitro => Failed: {response.StatusDescription}");
                        }
                        else
                        {
                            Console.WriteLine($"Downloading {classname}.nitro => Failed: {ex.Message}");
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Downloading {classname}.nitro => Failed: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }

                if (!File.Exists(iconFilePath))
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloading: {classname.Replace('*', '_')}_icon.png");
                        webClient.DownloadFile(iconUrl, iconFilePath);
                        Console.WriteLine($"Successfully downloaded: {classname.Replace('*', '_')}_icon.png");
                        iconDownloadCount++;
                    }
                    catch (WebException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (ex.Response is HttpWebResponse response)
                        {
                            Console.WriteLine($"Downloading {classname.Replace('*', '_')}_icon.png => Failed: {response.StatusDescription}");
                        }
                        else
                        {
                            Console.WriteLine($"Downloading {classname.Replace('*', '_')}_icon.png => Failed: {ex.Message}");
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Downloading {classname.Replace('*', '_')}_icon.png => Failed: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }

            return (nitroDownloadCount, iconDownloadCount);
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