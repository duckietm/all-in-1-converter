﻿namespace ConsoleApplication
{
    public static class FurnidataDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadFurnidata()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string furnidataTXT = config["AppSettings:furnidataTXT"];
            string furnidataXML = config["AppSettings:furnidataXML"];

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            Console.WriteLine("🚀 Saving Starting the furnidata download");

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"📡 Downloading furnidata.txt from: {furnidataTXT}");
                Console.ForegroundColor = ConsoleColor.Gray;

                await DownloadFileAsync(furnidataTXT, "./Habbo_Default/files/txt/furnidata.txt", "furnidata.txt");
                await DownloadFileAsync(furnidataXML, "./Habbo_Default/files/txt/furnidata_xml.xml", "furnidata_xml.xml");
                FurnidataConverter.ConvertXmlToJson("./Habbo_Default/files/txt/furnidata_xml.xml", "./Habbo_Default/files/json/FurnitureData.json");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"📦 Converted furnidata_xml to FurnitureData.json");
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine("🎉 Furnidata completed successfully!");
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error downloading furnidata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Unexpected error downloading furnidata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
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
                Console.WriteLine($"✅ Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error downloading {fileName}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
        }
    }
}