namespace ConsoleApplication
{
    public static class ProductDataDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task DownloadProductDataAsync()
        {
            string configFilePath = "config.ini";
            var config = IniFileParser.Parse(configFilePath);

            try
            {
                string productdataurl = config["AppSettings:productdataurl"];
                if (string.IsNullOrEmpty(productdataurl))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Productdata URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

                await DownloadFileAsync(productdataurl, "./Habbo_Default/files/xml/productdata.txt", "productdata.txt");

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