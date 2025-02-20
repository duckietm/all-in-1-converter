using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

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
                    Console.WriteLine("❌ Error: Productdata URL is not configured.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);

                string textFilePath = "./Habbo_Default/files/txt/productdata.txt";
                await DownloadFileAsync(productdataurl, textFilePath, "productdata.txt");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Productdata Saved");
                Console.ForegroundColor = ConsoleColor.Gray;

                // Pass the file path correctly
                await ConvertProductDataToJsonAsync(textFilePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error downloading productdata: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        private static async Task DownloadFileAsync(string url, string filePath, string fileName)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Downloaded: {fileName}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error downloading {fileName}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                throw;
            }
        }

        private static async Task ConvertProductDataToJsonAsync(string textFilePath)
        {
            string jsonFilePath = "./Habbo_Default/files/json/ProductData.json";

            if (!File.Exists(textFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error: productdata.txt not found.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            try
            {
                string text = await File.ReadAllTextAsync(textFilePath);

                var matches = Regex.Matches(text, @"\[\s*""(.*?)""\s*,\s*""(.*?)""\s*,\s*""(.*?)""\s*\]");
                var products = new List<object>();

                foreach (Match match in matches)
                {
                    string code = match.Groups[1].Value.Trim(); // 🔥 Ensure NO leading spaces
                    string name = DecodeUnicode(match.Groups[2].Value.Trim()); // ✅ Fully decode Unicode
                    string description = DecodeUnicode(match.Groups[3].Value.Trim()); // ✅ Fully decode Unicode

                    products.Add(new { code, name, description });
                }

                var productDataWrapper = new { productdata = new { product = products } };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(productDataWrapper, options);

                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                await File.WriteAllTextAsync(jsonFilePath, json);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"🔄 Converted: {jsonFilePath}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error converting productdata to JSON: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
        private static string DecodeUnicode(string input)
        {
            return Regex.Unescape(HttpUtility.HtmlDecode(input));
        }
    }
}
