using System;
using System.IO;
using System.Net;

namespace ConsoleApplication
{
    public static class TextsDownloader
    {
        public static void DownloadTexts()
        {

            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string externalTextUrl = config["AppSettings:externaltexturl"];

            if (!Directory.Exists("./files"))
            {
                Directory.CreateDirectory("./files");
            }

            Console.WriteLine("Saving external flash texts...");

            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                webClient.DownloadFile(externalTextUrl, "./files/external_flash_texts.txt");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("External Flash Texts Saved");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading external flash texts: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}