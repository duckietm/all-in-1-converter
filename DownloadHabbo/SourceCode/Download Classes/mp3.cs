using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace ConsoleApplication
{
    public static class Mp3Downloader
    {
        public static void DownloadMp3s()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            Console.WriteLine("MP3 Sounds Download Started");

            if (!Directory.Exists("./mp3"))
            {
                Directory.CreateDirectory("./mp3");
            }

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add($"User-Agent: {CommonConfig.UserAgent}");

                int mp3Number = 1;
                int errorCount = 0;

                while (errorCount < 20)
                {
                    try
                    {
                        string fileName = $"./mp3/sound_machine_sample_{mp3Number}.mp3";

                        if (File.Exists(fileName))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"MP3 {mp3Number} already exists!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            mp3Number++;
                            errorCount = 0;
                            continue;
                        }

                        string soundMachineUrl = config["AppSettings:soundmachineurl"];
                        string downloadUrl = $"{soundMachineUrl}{mp3Number}.mp3";

                        webClient.DownloadFile(downloadUrl, fileName);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloaded MP3 {mp3Number}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        mp3Number++;
                        errorCount = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error downloading MP3 {mp3Number}: {ex.Message}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        errorCount++;
                        mp3Number++;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished downloading MP3s!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}