using System.Xml;

namespace ConsoleApplication
{
    internal static class ClothesDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task DownloadClothesAsync()
        {
            httpClient.DefaultRequestHeaders.Add("user-agent", CommonConfig.UserAgent);

            try
            {
                string externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/1";
                string source = await httpClient.GetStringAsync(externalVariablesUrl);

                string release = null;
                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (line.Contains("flash.client.url="))
                    {
                        release = line.Substring(0, line.Length - 1).Split('/')[4];
                        Console.WriteLine("We are going to download from release: " + release);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(release))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Could not determine the release version.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                string currentDirectory = Environment.CurrentDirectory;
                string gordonDirectory = "https://images.habbo.com/gordon/";
                string downloadDirectory = Path.Combine(currentDirectory, "clothes");

                string figuremapUrl = $"{gordonDirectory}{release}/figuremap.xml";
                string figuredataUrl = "http://habbo.com/gamedata/figuredata/1";

                Directory.CreateDirectory(downloadDirectory);

                await DownloadFileAsync(figuredataUrl, Path.Combine(downloadDirectory, "figuredata.xml"), "figuredata.xml");

                await DownloadFileAsync(figuremapUrl, Path.Combine(downloadDirectory, "figuremap.xml"), "figuremap.xml");

                string figuremapFile = Path.Combine(downloadDirectory, "figuremap.xml");
                if (!File.Exists(figuremapFile))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: figuremap.xml does not exist.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                int downloadCount = 0;
                using (XmlReader reader = XmlReader.Create(figuremapFile))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement() && reader.Name == "lib")
                        {
                            string id = reader["id"];
                            if (!string.IsNullOrEmpty(id))
                            {
                                string swfUrl = $"{gordonDirectory}{release}/{id}.swf";
                                string swfFilePath = Path.Combine(downloadDirectory, $"{id}.swf");

                                if (!File.Exists(swfFilePath))
                                {
                                    bool success = await DownloadFileAsync(swfUrl, swfFilePath, id);
                                    if (success)
                                    {
                                        downloadCount++;
                                    }
                                }
                            }
                        }
                    }
                }

                if (downloadCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Downloaded {downloadCount} new clothes!");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("You have the latest clothes!");
                }
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine();
                Console.WriteLine("All has been done!");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task<bool> DownloadFileAsync(string url, string filePath, string fileName)
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
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error downloading {fileName}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
        }
    }
}