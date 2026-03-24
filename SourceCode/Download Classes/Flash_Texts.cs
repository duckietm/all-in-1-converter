using System.Text.Encodings.Web;
using System.Text.Json;

namespace ConsoleApplication
{
    public static class TextsDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadTextsAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            string externalTextUrl = config["AppSettings:externaltexturl"];

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            try
            {
                string txtPath = "./Habbo_Default/files/txt/ExternalTexts.txt";
                await DownloadFileAsync(externalTextUrl, txtPath, "ExternalTexts.txt");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("External Flash Texts Saved");
                Console.ForegroundColor = ConsoleColor.Gray;

                await ConvertTextsToJsonAsync(txtPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error downloading external flash texts: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task ConvertTextsToJsonAsync(string txtPath)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(txtPath);
                var texts = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    texts[key] = value;
                }

                string jsonPath = "./Habbo_Default/files/json/ExternalTexts.json";
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                string jsonContent = JsonSerializer.Serialize(texts, jsonOptions);
                await File.WriteAllTextAsync(jsonPath, jsonContent);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("External Flash Texts JSON Saved");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error converting texts to JSON: {ex.Message}");
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
    }
}