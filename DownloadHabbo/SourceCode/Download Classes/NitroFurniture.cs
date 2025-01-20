using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleApplication
{
    internal static class NitroFurnitureDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task DownloadFurnitureAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataUrl = config["AppSettings:nitro_furnidataJSON"];
            string furnitureUrl = config["AppSettings:nitro_furnitureurl"];
            string furnitureIconUrl = config["AppSettings:nitro_furniture_icon_url"];

            Console.WriteLine("Furniture Download Started");

            Directory.CreateDirectory("./Nitro_hof_furni");
            Directory.CreateDirectory("./Nitro_hof_furni/icons");
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
                        await DownloadFileAsync(nitroUrl, nitroFilePath, $"{baseClassname}.nitro");
                        nitroDownloadCount++;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Downloading {baseClassname}.nitro => Failed: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Downloading {baseClassname}.nitro => Failed: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }

                if (!File.Exists(iconFilePath))
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloading: {classname.Replace('*', '_')}_icon.png");
                        await DownloadFileAsync(iconUrl, iconFilePath, $"{classname.Replace('*', '_')}_icon.png");
                        iconDownloadCount++;
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Downloading {classname.Replace('*', '_')}_icon.png => Failed: {ex.Message}");
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