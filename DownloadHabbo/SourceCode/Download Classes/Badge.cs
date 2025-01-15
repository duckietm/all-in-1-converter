using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApplication
{
    public static class Badges
    {
        private const string BadgesDirectory = "./badges";
        private const string TempDirectory = "./temp";
        private const int BufferSize = 1500;
        private static SemaphoreSlim semaphore = new SemaphoreSlim(100);

        public static void DownloadBadges()
        {
            EnsureDirectoriesExist();

            int initialBadgeCount = Directory.GetFiles(BadgesDirectory, "*.*", SearchOption.AllDirectories).Length;

            string[] domains = { "com", "fr", "fi", "es", "nl", "de", "it", "com.tr", "com.br" };

            Parallel.ForEach(domains, domain =>
            {
                Console.WriteLine($"Start initializing badges from .{domain.ToUpper()}");
                DownloadBadgesForDomain(domain);
                Console.WriteLine($"Finished downloading badges from .{domain.ToUpper()}");
            });

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

        private static void DownloadBadgesForDomain(string domain)
        {
            string externalFlashTextsUrl = $"https://www.habbo.{domain}/gamedata/external_flash_texts/1";
            string externalFlashTextsFilePath = Path.Combine(TempDirectory, $"external_flash_texts_{domain}.txt");

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("user-agent", CommonConfig.UserAgent);
                webClient.DownloadFile(externalFlashTextsUrl, externalFlashTextsFilePath);
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
                DownloadBadgesInBuffer(badgeBuffer, tasks);
            }

            Task.WaitAll(tasks.ToArray());
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
                DownloadBadgesInBuffer(badgeBuffer, tasks);
                while (!badgeBuffer.IsEmpty)
                {
                    badgeBuffer.TryTake(out _);
                }
            }
        }

        private static void DownloadBadgesInBuffer(ConcurrentBag<string> badgeBuffer, ConcurrentBag<Task> tasks)
        {
            var badgesToDownload = badgeBuffer.ToArray();

            foreach (var badgeName in badgesToDownload)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        DownloadBadge(badgeName);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        }

        private static void DownloadBadge(string badgeName)
        {
            string badgeUrl = $"http://images-eussl.habbo.com/c_images/album1584/{badgeName}.gif";
            string badgeFilePath = Path.Combine(BadgesDirectory, $"{badgeName}.gif");

            if (File.Exists(badgeFilePath))
            {
                return;
            }

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("user-agent", CommonConfig.UserAgent);
                try
                {
                    webClient.DownloadFile(badgeUrl, badgeFilePath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Downloading badge: {badgeName}.gif");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                catch (Exception ex) { }
            }
        }
    }
}