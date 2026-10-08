using Habbo_Downloader.Tools;

namespace ConsoleApplication
{
    /// <summary>
    /// Shared download of Habbo's asset libraries (furniture, clothes, effects, pets) into a
    /// Habbo_Default/&lt;kind&gt;/{swf,hab} folder. config.ini download_format picks .swf, .hab or both;
    /// a .hab gets a lossless WebP sheet (spritesheet_format=webp) with its json left untouched.
    /// </summary>
    internal static class HabboAssetDownloader
    {
        internal const int MaxParallelDownloads = 8;

        internal static readonly HttpClient Http = CreateClient();
        private static readonly object ConsoleLock = new();

        internal sealed class Counts
        {
            public int Swf;
            public int Hab;
            public int Failed;
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);
            return client;
        }

        /// <summary>Creates the folders and moves a download from before the swf/ and hab/ folders into them.</summary>
        internal static void PrepareFolder(HabboAssetFolder folder)
        {
            if (ConverterSettings.DownloadSwf) Directory.CreateDirectory(folder.Swf);
            if (ConverterSettings.DownloadHab) Directory.CreateDirectory(folder.Hab);
            MoveOldLayout(folder);
        }

        /// <summary>Downloads every library ("name", "url without extension") in the configured formats.</summary>
        internal static async Task<Counts> DownloadLibrariesAsync(HabboAssetFolder folder, IEnumerable<(string name, string baseUrl)> libraries)
        {
            var counts = new Counts();
            var options = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelDownloads };

            await Parallel.ForEachAsync(libraries, options, async (library, _) =>
                await DownloadLibraryAsync(folder, library.name, library.baseUrl, counts));

            return counts;
        }

        internal static async Task DownloadLibraryAsync(HabboAssetFolder folder, string name, string baseUrl, Counts counts)
        {
            if (ConverterSettings.DownloadSwf)
            {
                string swfPath = Path.Combine(folder.Swf, $"{name}.swf");
                if (!File.Exists(swfPath))
                {
                    byte[]? swf = await TryGetAsync(baseUrl + ".swf");
                    if (swf != null && IsSwf(swf))
                    {
                        await SaveAsync(swfPath, swf);
                        Interlocked.Increment(ref counts.Swf);
                        Log(ConsoleColor.Green, $"Downloaded: {name}.swf");
                    }
                }
            }

            if (ConverterSettings.DownloadHab)
            {
                string habPath = Path.Combine(folder.Hab, $"{name}.hab");
                if (!File.Exists(habPath))
                {
                    byte[]? hab = await TryGetAsync(baseUrl + ".hab");
                    if (hab != null && HabBundle.IsHab(hab))
                    {
                        if (await SaveHabAsync(habPath, name, hab)) Interlocked.Increment(ref counts.Hab);
                        else Interlocked.Increment(ref counts.Failed);
                    }
                }
            }
        }

        internal static string Summary(string what, Counts counts) =>
            $"Downloaded {counts.Swf} new .swf and {counts.Hab} new .hab {what}"
            + (counts.Failed > 0 ? $"; {counts.Failed} .hab files could not be saved." : ".");

        /// <summary>The Habbo release folder ("flash-assets-PRODUCTION-...") from external_variables.</summary>
        internal static async Task<string?> GetReleaseAsync(string externalVariablesUrl)
        {
            string source = await Http.GetStringAsync(externalVariablesUrl);
            foreach (string line in source.Split('\n', '\r'))
            {
                if (!line.Contains("flash.client.url=")) continue;

                string[] parts = line.TrimEnd('/').Split('/');
                if (parts.Length > 4) return parts[4];
            }
            return null;
        }

        /// <summary>Saves a .hab with a WebP sheet (or as downloaded for spritesheet_format=png).</summary>
        internal static async Task<bool> SaveHabAsync(string habPath, string name, byte[] hab)
        {
            try
            {
                string result = "as downloaded (spritesheet_format=png)";
                byte[] output = ConverterSettings.UseWebp ? HabSheetConverter.ToWebp(hab, out result) : hab;

                await SaveAsync(habPath, output);
                Log(ConsoleColor.Green, $"Downloaded: {name}.hab ({result})");
                return true;
            }
            catch (Exception ex)
            {
                Log(ConsoleColor.Red, $"Error saving {name}.hab: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// A download from before the swf/ and hab/ folders: .swf files move into swf/, .hab files are
        /// converted into hab/ like a new download, so nothing has to be downloaded again.
        /// </summary>
        private static void MoveOldLayout(HabboAssetFolder folder)
        {
            if (!Directory.Exists(folder.Root)) return;

            foreach (string swf in Directory.GetFiles(folder.Root, "*.swf"))
            {
                Directory.CreateDirectory(folder.Swf);
                string target = Path.Combine(folder.Swf, Path.GetFileName(swf));
                if (File.Exists(target)) File.Delete(swf);
                else File.Move(swf, target);
            }

            string[] habs = Directory.GetFiles(folder.Root, "*.hab");
            if (habs.Length == 0 || !ConverterSettings.DownloadHab) return;

            Console.WriteLine($"Moving {habs.Length} .hab files into {folder.Hab}...");
            Directory.CreateDirectory(folder.Hab);

            Parallel.ForEach(habs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, habFile =>
            {
                string name = Path.GetFileNameWithoutExtension(habFile);
                string target = Path.Combine(folder.Hab, name + ".hab");
                if (!File.Exists(target))
                {
                    byte[] hab = File.ReadAllBytes(habFile);
                    if (!HabBundle.IsHab(hab) || !SaveHabAsync(target, name, hab).GetAwaiter().GetResult()) return;
                }
                File.Delete(habFile);
            });
        }

        /// <summary>The body of a successful response, or null (missing asset, network error).</summary>
        internal static async Task<byte[]?> TryGetAsync(string url)
        {
            try
            {
                using var response = await Http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Writes next to the target first, so a stopped run never leaves half a file that counts as downloaded.</summary>
        internal static async Task SaveAsync(string path, byte[] data)
        {
            string temp = path + ".part";
            await File.WriteAllBytesAsync(temp, data);
            File.Move(temp, path, true);
        }

        // An error page from the CDN must never be saved as an asset.
        internal static bool IsSwf(byte[] data) =>
            data.Length > 3 && (data[0] == (byte)'F' || data[0] == (byte)'C' || data[0] == (byte)'Z') && data[1] == (byte)'W' && data[2] == (byte)'S';

        internal static bool IsPng(byte[] data) =>
            data.Length > 8 && data[0] == 0x89 && data[1] == (byte)'P' && data[2] == (byte)'N' && data[3] == (byte)'G';

        internal static void Log(ConsoleColor color, string message)
        {
            lock (ConsoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
