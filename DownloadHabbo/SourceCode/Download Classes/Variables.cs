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
                        await DownloadFileAsync(externalvarsurl, Path.Combine("./Habbo_Default/files", "external_variables.txt"), "external_variables.txt");
                        WriteColoredMessage("External Variables Saved", ConsoleColor.Green);
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