using System.Text.Json;

namespace ConsoleApplication
{
    public static class VariablesDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadVariablesAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                string externalvarsurl = config["AppSettings:externalvarsurl"];
                if (string.IsNullOrEmpty(externalvarsurl))
                {
                    WriteColoredMessage("Error: External Variables URL is not configured.", ConsoleColor.Red);
                    return;
                }

                if (!Uri.TryCreate(externalvarsurl, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    WriteColoredMessage("Error: Invalid External Variables URL.", ConsoleColor.Red);
                    return;
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

                for (int retryCount = 3; retryCount > 0; retryCount--)
                {
                    try
                    {
                        string txtPath = Path.Combine("./Habbo_Default/files/txt", "external_variables.txt");
                        await DownloadFileAsync(externalvarsurl, txtPath, "external_variables.txt");
                        WriteColoredMessage("External Variables Saved", ConsoleColor.Green);

                        await ConvertVariablesToJsonAsync(txtPath);
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        WriteColoredMessage($"Error downloading external variables: {ex.Message}. Retries left: {retryCount - 1}", ConsoleColor.Yellow);

                        if (retryCount == 1)
                        {
                            WriteColoredMessage("Failed to download external variables after 3 attempts.", ConsoleColor.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteColoredMessage("Error downloading external variables: " + ex.Message, ConsoleColor.Red);
            }
        }

        private static async Task ConvertVariablesToJsonAsync(string txtPath)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(txtPath);
                var variables = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    variables[key] = value;
                }

                string jsonPath = Path.Combine(Path.GetDirectoryName(txtPath)!, "external_variables.json");
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(variables, jsonOptions);
                await File.WriteAllTextAsync(jsonPath, jsonContent);

                WriteColoredMessage("External Variables JSON Saved", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteColoredMessage($"Error converting variables to JSON: {ex.Message}", ConsoleColor.Red);
            }
        }

        private static async Task DownloadFileAsync(string url, string filePath, string fileName)
        {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            WriteColoredMessage($"Downloaded: {fileName}", ConsoleColor.Green);
        }

        private static void WriteColoredMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}