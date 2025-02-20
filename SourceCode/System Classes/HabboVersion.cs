namespace ConsoleApplication
{
    internal static class VersionChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();

        internal static async Task CheckVersionAsync()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

            try
            {
                string externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/1";
                string source = await httpClient.GetStringAsync(externalVariablesUrl);

                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (line.Contains("flash.client.url="))
                    {
                        string releaseVersion = line.Substring(0, line.Length - 1).Split('/')[4];
                        Console.WriteLine("Current Habbo release: " + releaseVersion);
                        break;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error fetching Habbo release version: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unexpected error: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}