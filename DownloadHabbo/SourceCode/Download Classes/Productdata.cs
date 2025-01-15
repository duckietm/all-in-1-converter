using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace ConsoleApplication
{
    public static class ProductDataDownloader
    {
        public static void DownloadProductData()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                if (!Directory.Exists("./files"))
                {
                    Directory.CreateDirectory("./files");
                }

                Console.WriteLine("Saving productdata...");
                string productdataurl = config["AppSettings:productdataurl"];
                if (string.IsNullOrEmpty(productdataurl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Productdata URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add($"User-Agent: {CommonConfig.UserAgent}");
                    webClient.DownloadFile(productdataurl, "./files/productdata.txt");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Productdata Saved");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading productdata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}