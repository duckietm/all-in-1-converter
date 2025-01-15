using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace ConsoleApplication
{
    public static class CustomFurniDownloader
    {
        public static void DownloadCustomFurni()
        {
            if (!Directory.Exists("./hof_furni_custom"))
            {
                Directory.CreateDirectory("./hof_furni_custom");
            }
            if (!Directory.Exists("./hof_furni_custom/config"))
            {
                Directory.CreateDirectory("./hof_furni_custom/config");
            }
            if (!Directory.Exists("./hof_furni_custom/icons"))
            {
                Directory.CreateDirectory("./hof_furni_custom/icons");
            }
            if (!Directory.Exists("./temp"))
            {
                Directory.CreateDirectory("./temp");
            }

            string customfurnidata = ConfigurationManager.AppSettings["customfurnidata"];
            string customproductdataurl = ConfigurationManager.AppSettings["customproductdataurl"];
            string customfurnidataxml = ConfigurationManager.AppSettings["customfurnidataxml"];
            string tempFilePath = "./temp/furnidata.txt";

            WebClient webClient = new WebClient();
            webClient.Headers.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                // Download and save furnidata.txt
                webClient.DownloadFile(customfurnidata, tempFilePath);
                Console.WriteLine("Custom Furnidata Downloaded...");

                // Process the downloaded file
                File.WriteAllLines(tempFilePath, File.ReadAllLines(tempFilePath)
                    .Select(line => string.Join("\n", line.Split(new[] { "]," }, StringSplitOptions.RemoveEmptyEntries)))
                    .Select(line => string.Join("\n", line.Split(new[] { "[" }, StringSplitOptions.RemoveEmptyEntries)))
                    .Select(line => string.Join("", line.Split(new[] { "\"" }, StringSplitOptions.RemoveEmptyEntries)))
                    .ToArray());

                // Download productdata.txt, furnidata.txt, and furnidata_xml.xml
                Console.WriteLine("Saving productdata...");
                webClient.DownloadFile(customproductdataurl, "./hof_furni_custom/config/productdata.txt");

                Console.WriteLine("Saving Furnidata...");
                webClient.DownloadFile(customfurnidata, "./hof_furni_custom/config/furnidata.txt");
                webClient.DownloadFile(customfurnidataxml, "./hof_furni_custom/config/furnidata_xml.xml");

                Console.WriteLine("Custom Furnidata Downloaded...");

                // Download custom furniture and icons
                int downloadedCount = 0;
                using (StreamReader reader = new StreamReader(tempFilePath))
                {
                    Console.WriteLine("Begin downloading Custom Furniture...");
                    Thread.Sleep(3000);

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] directoryPath = line.Split(new[] { "," }, StringSplitOptions.None);
                        string[] furnitureName = directoryPath[2].Split(new[] { "*" }, StringSplitOptions.None);

                        string customfileextension = ConfigurationManager.AppSettings["customfileextension"];
                        string customfurnitureurl = ConfigurationManager.AppSettings["customfurnitureurl"];
                        string customiconurl = ConfigurationManager.AppSettings["customiconureurl"];

                        if (!File.Exists($"./hof_furni_custom/{furnitureName[0]}{customfileextension}"))
                        {
                            try
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Furniture Downloading: {furnitureName[0]}{customfileextension}");
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.WriteLine($"Icon Downloading: {furnitureName[0]}.png");
                                Console.ForegroundColor = ConsoleColor.Gray;

                                webClient.DownloadFile($"{customfurnitureurl}/{furnitureName[0]}.swf", $"./hof_furni_custom/{furnitureName[0]}{customfileextension}");
                                webClient.DownloadFile($"{customiconurl}/{furnitureName[0]}_icon.png", $"./hof_furni_custom/icons/{furnitureName[0]}.png");

                                downloadedCount++;
                            }
                            catch
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error while downloading: {furnitureName[0]}");
                                Console.ForegroundColor = ConsoleColor.Gray;
                            }
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Downloading Custom Furniture Done!");
                    Console.WriteLine($"We've downloaded {downloadedCount} new furniture!");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading custom furniture: " + ex.Message);
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
        }
    }
}