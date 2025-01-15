using System;
using System.IO;
using System.Net;

namespace ConsoleApplication
{
    public static class VariablesDownloader
    {
        public static void DownloadVariables()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {

                if (!Directory.Exists("./files"))
                {
                    Directory.CreateDirectory("./files");
                }

                Console.WriteLine("Saving External Variables...");

                string externalvarsurl = config["AppSettings:externalvarsurl"];
                if (string.IsNullOrEmpty(externalvarsurl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: External Variables URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                if (!Uri.TryCreate(externalvarsurl, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid External Variables URL.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                int retryCount = 3;
                while (retryCount > 0)
                {
                    try
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            webClient.Headers.Add($"User-Agent: {CommonConfig.UserAgent}");
                            webClient.DownloadFile(externalvarsurl, "./files/external_variables.txt");
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("External Variables Saved");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return;
                    }
                    catch (WebException ex)
                    {
                        retryCount--;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Error downloading external variables: {ex.Message}. Retries left: {retryCount}");
                        Console.ForegroundColor = ConsoleColor.Gray;

                        if (retryCount == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to download external variables after 3 attempts.");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading external variables: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}