using System.Collections.Concurrent;

namespace ConsoleApplication
{
    public static class Badges
    {
        private const string BadgesDirectory = "./Habbo_Default/badges";
        private const string TempDirectory = "./temp";
        private const int MaxParallelDownloads = 32;
        private static HttpClient httpClient;

        static Badges()
        {
            httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 100 });
            httpClient.EnsureUserAgent();
        }

        public static async Task DownloadBadgesAsync()
        {
            EnsureDirectoriesExist();

            int initialBadgeCount = Directory.GetFiles(BadgesDirectory, "*.*", SearchOption.AllDirectories).Length;

            string[] domains = { "com", "fr", "fi", "es", "nl", "de", "it", "com.tr", "com.br" };

            // The hotels share most badges: collect the names once, then download each badge once.
            var badgeNames = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
            await Task.WhenAll(domains.Select(async domain =>
            {
                Console.WriteLine($"Start initializing badges from .{domain.ToUpper()}");
                foreach (string name in await ReadBadgeNamesAsync(domain)) badgeNames.TryAdd(name, 0);
                Console.WriteLine($"Finished reading badges from .{domain.ToUpper()}");
            }));

            Console.WriteLine($"{badgeNames.Count:N0} different badges in {domains.Length} hotels; downloading the missing ones...");
            await Parallel.ForEachAsync(badgeNames.Keys, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelDownloads },
                async (badgeName, _) => await DownloadBadgeAsync(badgeName));

            int finalBadgeCount = Directory.GetFiles(BadgesDirectory, "*.*", SearchOption.AllDirectories).Length;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nDownloading done! We downloaded {finalBadgeCount - initialBadgeCount} badges!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(BadgesDirectory))
            {
                Directory.CreateDirectory(BadgesDirectory);
            }
            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
        }

        /// <summary>The badge names in one hotel's external_flash_texts (empty when it cannot be downloaded).</summary>
        private static async Task<List<string>> ReadBadgeNamesAsync(string domain)
        {
            string externalFlashTextsUrl = $"https://www.habbo.{domain}/gamedata/external_flash_texts/1";
            string externalFlashTextsFilePath = Path.Combine(TempDirectory, $"external_flash_texts_{domain}.txt");
            var names = new List<string>();

            try
            {
                using (var response = await httpClient.GetAsync(externalFlashTextsUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(externalFlashTextsFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to download external flash texts for {domain}. Status code: {response.StatusCode}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return names;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"HTTP request failed for {domain}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return names;
            }

            names.AddRange(ParseBadgeNames(File.ReadLines(externalFlashTextsFilePath)));
            return names;
        }

        /// <summary>The badge codes of "badge_name_CODE=..." lines; _HHCA / _HHUK variants are skipped unless fb_ or al_.</summary>
        internal static IEnumerable<string> ParseBadgeNames(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (!line.StartsWith("badge_name_")) continue;
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length < 2) continue;

                string badgeName = parts[0].Replace("badge_name_", "");
                if (badgeName.StartsWith("fb_") || badgeName.StartsWith("al_")
                    || (!badgeName.Contains("_HHCA") && !badgeName.Contains("_HHUK")))
                {
                    yield return badgeName;
                }
            }
        }

        private static async Task DownloadBadgeAsync(string badgeName)
        {
            string badgeUrl = $"http://images-eussl.habbo.com/c_images/album1584/{badgeName}.gif";
            string badgeFilePath = Path.Combine(BadgesDirectory, $"{badgeName}.gif");
            if (File.Exists(badgeFilePath)) return;

            try
            {
                using var response = await httpClient.GetAsync(badgeUrl);
                if (!response.IsSuccessStatusCode) return;

                // Written next to it first, so a failed download never leaves half a .gif.
                string partPath = badgeFilePath + ".part";
                await using (var fileStream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                File.Move(partPath, badgeFilePath, overwrite: true);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Downloading badge: {badgeName}.gif");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"HTTP request failed for badge {badgeName}.gif: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
