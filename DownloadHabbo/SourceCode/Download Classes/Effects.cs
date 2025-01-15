using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class EffectsDownloader
    {
        public static async Task DownloadEffectsAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string externalVarsUrl = config["AppSettings:externalvarsurl"];
            string effectUrl = config["AppSettings:effecturl"];

            string release_effect;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                HttpResponseMessage res = await httpClient.GetAsync(externalVarsUrl);
                string source = await res.Content.ReadAsStringAsync();

                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!line.Contains("flash.client.url="))
                    {
                        continue;
                    }

                    release_effect = line.Substring(0, line.Length - 1).Split('/')[4];

                    if (!Directory.Exists("./effect"))
                    {
                        Directory.CreateDirectory("./effect");
                    }

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Downloading Effects version: " + release_effect);

                    WebClient webClient = new WebClient();
                    webClient.Headers.Add("user-agent", CommonConfig.UserAgent);

                    webClient.DownloadFile($"{effectUrl}/{release_effect}/effectmap.xml", "./effect/effectmap.xml");

                    webClient.DownloadFile($"{effectUrl}/{release_effect}/HabboAvatarActions.xml", "./effect/HabboAvatarActions.xml");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Effects Downloaded and Saved");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading effects: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}