using System.Collections.Concurrent;
using System.Diagnostics;

namespace ConsoleApplication
{
    public static class Badges
    {
        private const string BadgesDirectory = "./Habbo_Default/badges";
        private const string TempDirectory = "./temp";
        private const int BufferSize = 5000;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(100);
        private static HttpClient httpClient;
        private static readonly object fileLock = new object();

        static Badges()
        {
            httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 100 });
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent.Replace("User-Agent: ", ""));
        }

        public static async Task DownloadBadgesAsync()
        {
            EnsureDirectoriesExist();

            int initialBadgeCount = Directory.GetFiles(BadgesDirectory, "*.*", SearchOption.AllDirectories).Length;

            string[] domains = { "com", "fr", "fi", "es", "nl", "de", "it", "com.tr", "com.br" };

            var tasks = new Task[domains.Length];
            for (int i = 0; i < domains.Length; i++)
            {
                string domain = domains[i];
                tasks[i] = Task.Run(async () =>
                {
                    Console.WriteLine($"Start initializing badges from .{domain.ToUpper()}");
                    await DownloadBadgesForDomainAsync(domain);
                    Console.WriteLine($"Finished downloading badges from .{domain.ToUpper()}");
                });
            }

            await Task.WhenAll(tasks);

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

        private static async Task DownloadBadgesForDomainAsync(string domain)
        {
            string externalFlashTextsUrl = $"https://www.habbo.{domain}/gamedata/external_flash_texts/1";
            string externalFlashTextsFilePath = Path.Combine(TempDirectory, $"external_flash_texts_{domain}.txt");

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
                        return;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"HTTP request failed for {domain}: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            var badgeNames = new ConcurrentBag<string>();

            using (StreamReader streamReader = new StreamReader(externalFlashTextsFilePath))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line.StartsWith("badge_name_"))
                    {
                        badgeNames.Add(line);
                    }
                }
            }

            var badgeBuffer = new ConcurrentBag<string>();
            var tasks = new ConcurrentBag<Task>();

            Parallel.ForEach(badgeNames, line =>
            {
                ProcessBadgeLine(line, badgeBuffer, tasks);
            });

            if (badgeBuffer.Count > 0)
            {
                await DownloadBadgesInBufferAsync(badgeBuffer, tasks);
            }

            await Task.WhenAll(tasks.ToArray());
        }

        private static void ProcessBadgeLine(string line, ConcurrentBag<string> badgeBuffer, ConcurrentBag<Task> tasks)
        {
            string[] parts = line.Split(new[] { '=' }, 2);
            if (parts.Length < 2) return;

            string badgeName = parts[0].Replace("badge_name_", "");
            string badgeValue = parts[1];

            if (badgeName.StartsWith("fb_") || badgeName.StartsWith("al_"))
            {
                badgeBuffer.Add(badgeName);
            }
            else if (!badgeName.Contains("_HHCA") && !badgeName.Contains("_HHUK"))
            {
                badgeBuffer.Add(badgeName);
            }

            if (badgeBuffer.Count >= BufferSize)
            {
                DownloadBadgesInBufferAsync(badgeBuffer, tasks).Wait();
                while (!badgeBuffer.IsEmpty)
                {
                    badgeBuffer.TryTake(out _);
                }
            }
        }

        private static async Task DownloadBadgesInBufferAsync(ConcurrentBag<string> badgeBuffer, ConcurrentBag<Task> tasks)
        {
            var badgesToDownload = badgeBuffer.ToArray();

            foreach (var badgeName in badgesToDownload)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await DownloadBadgeAsync(badgeName);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        private static async Task DownloadBadgeAsync(string badgeName)
        {
            string badgeUrl = $"http://images-eussl.habbo.com/c_images/album1584/{badgeName}.gif";
            string badgeFilePath = Path.Combine(BadgesDirectory, $"{badgeName}.gif");

            lock (fileLock)
            {
                if (File.Exists(badgeFilePath))
                {
                    return;
                }
            }

            try
            {
                using (var response = await httpClient.GetAsync(badgeUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            lock (fileLock)
                            {
                                using (var fileStream = new FileStream(badgeFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                                {
                                    contentStream.CopyTo(fileStream);
                                }
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Downloading badge: {badgeName}.gif");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    else {}
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"HTTP request failed for badge {badgeName}.gif: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}