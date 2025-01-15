using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class FurnidataDownloader
    {
        public static async Task DownloadFurnidata()
        {
            // Load configuration from config.ini
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            // Get URLs from config
            string furnidataTXT = config["AppSettings:furnidataTXT"];
            string furnidataXML = config["AppSettings:furnidataXML"];

            // Ensure the files directory exists
            if (!Directory.Exists("./files"))
            {
                Directory.CreateDirectory("./files");
            }

            Console.WriteLine("Saving furnidata...");

            using (HttpClient httpClient = new HttpClient())
            {
                // Add User-Agent header to mimic a browser request
                httpClient.DefaultRequestHeaders.Add("User-Agent", CommonConfig.UserAgent);

                try
                {
                    // Debug: Display the full URL for furnidata.txt
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Downloading furnidata.txt from: {furnidataTXT}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    // Download furnidata.txt
                    byte[] txtData = await httpClient.GetByteArrayAsync(furnidataTXT);
                    File.WriteAllBytes("./files/furnidata.txt", txtData);

                    // Debug: Display the full URL for furnidata_xml.xml
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Downloading furnidata_xml.xml from: {furnidataXML}");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    // Download furnidata_xml.xml
                    byte[] xmlData = await httpClient.GetByteArrayAsync(furnidataXML);
                    File.WriteAllBytes("./files/furnidata_xml.xml", xmlData);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Furnidata Saved");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                catch (HttpRequestException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error downloading furnidata: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unexpected error downloading furnidata: " + ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }
    }
}