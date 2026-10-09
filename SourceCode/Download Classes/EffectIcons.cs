using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using static ConsoleApplication.HabboAssetDownloader;

namespace ConsoleApplication
{
    /// <summary>
    /// The avatar effect icons (fx_icon_&lt;type&gt;) for the client's avatareditor.effects.icon.url.
    /// Habbo does not put them on its CDN: they ship inside the Habbo Classic app, at
    /// HabboClassicWin.zip/resources/app.asar/client/generated/habbo-inventory-com.hab (the app only
    /// loads that local file). The source can be the zip, the app.asar, the .hab, or a folder with
    /// any of them or with already extracted icons. Output: Habbo_Default/fx_icons/fx_icon_&lt;type&gt;.png.
    /// </summary>
    internal static class EffectIconsExtractor
    {
        private const string BundleName = "habbo-inventory-com.hab";
        private const string ZipAsarPath = "resources/app.asar";
        private const string DefaultSource = "./Habbo_Default/import/effect_icons";
        private const string OutputFolder = "./Habbo_Default/fx_icons";

        // fx_icon_12_png inside the bundle; fx_icon_12_png.png or fx_icon_12.png once extracted.
        private static readonly Regex IconName = new(@"^fx_icon_(\d+)(?:_png)?(?:\.png)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static async Task ExtractAsync()
        {
            var config = IniFileParser.Parse("config.ini");
            string source = config.TryGetValue("AppSettings:effect_icons_source", out string? configured) && !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim().Trim('"')
                : DefaultSource;

            // A download link (for example a mirror of the Habbo Classic app) is fetched first;
            // a link put in effect_icons_source counts as well.
            string? url = config.TryGetValue("AppSettings:effect_icons_url", out string? configuredUrl) && !string.IsNullOrWhiteSpace(configuredUrl)
                ? configuredUrl.Trim().Trim('"')
                : null;

            if (IsLink(source))
            {
                url ??= source;
                source = DefaultSource;
            }

            if (url != null)
            {
                string? downloaded = await DownloadSourceAsync(url);
                if (downloaded != null) source = downloaded;
            }

            var icons = new SortedDictionary<int, byte[]>();

            if (File.Exists(source)) ReadFile(source, icons);
            else if (Directory.Exists(source)) ReadFolder(source, icons);
            else
            {
                Directory.CreateDirectory(DefaultSource);
                Log(ConsoleColor.Red, $"Effect icons source not found: {source}");
            }

            if (icons.Count == 0)
            {
                Log(ConsoleColor.Yellow,
                    $"No effect icons found. Put HabboClassicWin.zip, its resources/app.asar, {BundleName} or a folder of\n" +
                    $"fx_icon_<type> PNG files in {DefaultSource}, or point config.ini effect_icons_source at it.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            int written = 0, unchanged = 0;
            foreach (var (type, png) in icons)
            {
                string path = Path.Combine(OutputFolder, $"fx_icon_{type}.png");

                if (File.Exists(path) && (await File.ReadAllBytesAsync(path)).AsSpan().SequenceEqual(png))
                {
                    unchanged++;
                    continue;
                }

                await SaveAsync(path, png);
                written++;
            }

            Log(ConsoleColor.Green, $"Effect icons: {written} written, {unchanged} already up to date, in {OutputFolder}.");
            Log(ConsoleColor.Gray, "Copy the fx_icons folder into the client's image library (avatareditor.effects.icon.url).");
        }

        private static bool IsLink(string value) =>
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        private static readonly HttpClient DownloadClient = CreateDownloadClient();

        private static HttpClient CreateDownloadClient()
        {
            // The whole app zip is large: allow more time than the asset downloads get.
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent);
            return client;
        }

        /// <summary>
        /// Downloads the effect_icons_url file (zip, app.asar, .hab or one PNG) into the import folder,
        /// once. Anything that is not one of those (an error page) is thrown away.
        /// </summary>
        private static async Task<string?> DownloadSourceAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                Log(ConsoleColor.Red, $"effect_icons_url is not an http(s) link: {url}");
                return null;
            }

            Directory.CreateDirectory(DefaultSource);

            // Every release's link can end in "app.asar": the link's hash keeps each download apart,
            // so a newer link is fetched instead of reusing the older file.
            string name = Path.GetFileName(uri.AbsolutePath);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) name = "effect_icons_download";

            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(uri.AbsoluteUri)))[..10].ToLowerInvariant();
            string stem = $"{Path.GetFileNameWithoutExtension(name)}-{hash}";
            string path = Path.Combine(DefaultSource, stem + Path.GetExtension(name));

            string? earlier = Directory.EnumerateFiles(DefaultSource, stem + "*").FirstOrDefault(file => !file.EndsWith(".part", StringComparison.OrdinalIgnoreCase));
            if (earlier != null)
            {
                Log(ConsoleColor.DarkCyan, $"Using the earlier download {earlier} (delete it to download again).");
                return earlier;
            }

            string temp = path + ".part";
            try
            {
                Log(ConsoleColor.Blue, $"Downloading {uri}...");

                using (HttpResponseMessage response = await DownloadClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    await using Stream body = await response.Content.ReadAsStreamAsync();
                    await using FileStream file = File.Create(temp);
                    await body.CopyToAsync(file);
                }

                string? kind = DetectKind(temp);
                if (kind == null)
                {
                    File.Delete(temp);
                    Log(ConsoleColor.Red, "The download is not a zip, app.asar, .hab or PNG file.");
                    return null;
                }

                // Give the file the extension its content has, so it is read the right way.
                if (!path.EndsWith(kind, StringComparison.OrdinalIgnoreCase)) path += kind;

                File.Move(temp, path, true);
                Log(ConsoleColor.Green, $"Downloaded {path}.");
                return path;
            }
            catch (Exception ex)
            {
                if (File.Exists(temp)) File.Delete(temp);
                Log(ConsoleColor.Red, $"Could not download {uri}: {ex.Message}");
                return null;
            }
        }

        private static string? DetectKind(string path)
        {
            byte[] head = new byte[8];
            using (FileStream file = File.OpenRead(path))
            {
                if (file.Read(head, 0, head.Length) < head.Length) return null;
            }

            if (head[0] == (byte)'P' && head[1] == (byte)'K' && head[2] == 3 && head[3] == 4) return ".zip";
            if (head[0] == (byte)'H' && head[1] == (byte)'A' && head[2] == (byte)'B' && head[3] == 0) return ".hab";
            if (IsPng(head.Concat(new byte[1]).ToArray())) return ".png";
            if (BitConverter.ToUInt32(head, 0) == 4) return ".asar";
            return null;
        }

        private static void ReadFile(string path, SortedDictionary<int, byte[]> icons)
        {
            try
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                byte[]? bundle = extension switch
                {
                    ".zip" => FromZip(path),
                    ".asar" => FromAsar(File.ReadAllBytes(path), path + ".unpacked"),
                    _ => File.ReadAllBytes(path)
                };

                if (bundle == null)
                {
                    Log(ConsoleColor.Red, $"{BundleName} not found in {path}.");
                    return;
                }

                var (_, files) = HabBundle.Read(bundle);

                foreach (var (name, data) in files) Add(name, data, icons);
            }
            catch (Exception ex)
            {
                Log(ConsoleColor.Red, $"Could not read {path}: {ex.Message}");
            }
        }

        private static byte[]? FromZip(string path)
        {
            using ZipArchive zip = ZipFile.OpenRead(path);

            // An entry Electron keeps outside the archive sits in app.asar.unpacked.
            ZipArchiveEntry? unpacked = zip.Entries.FirstOrDefault(entry =>
                entry.FullName.StartsWith($"{ZipAsarPath}.unpacked/", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Name, BundleName, StringComparison.OrdinalIgnoreCase));
            if (unpacked != null) return ReadEntry(unpacked);

            ZipArchiveEntry? asar = zip.GetEntry(ZipAsarPath);
            return asar == null ? null : FromAsar(ReadEntry(asar), null);
        }

        private static byte[] ReadEntry(ZipArchiveEntry entry)
        {
            using Stream stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        /// <summary>
        /// The inventory bundle from an Electron .asar: u32 4, u32 header size, then the header pickle
        /// (u32 payload size, u32 json length, json) and the file data after the header. The app's own
        /// asar keeps it in client/generated/, a mirror of the client folder in generated/, so it is
        /// looked up by name.
        /// </summary>
        internal static byte[]? FromAsar(byte[] asar, string? unpackedFolder)
        {
            if (asar.Length < 16) throw new InvalidDataException("Not an .asar archive.");

            long headerSize = BitConverter.ToUInt32(asar, 4);
            int jsonLength = (int)BitConverter.ToUInt32(asar, 12);
            if (16 + jsonLength > asar.Length || 8 + headerSize > asar.Length) throw new InvalidDataException(".asar header is damaged.");

            using JsonDocument header = JsonDocument.Parse(asar.AsMemory(16, jsonLength));

            if (!FindAsarEntry(header.RootElement, "", out JsonElement node, out string entryPath)) return null;

            if (node.TryGetProperty("unpacked", out JsonElement isUnpacked) && isUnpacked.ValueKind == JsonValueKind.True)
            {
                string? unpackedPath = unpackedFolder == null ? null : Path.Combine(unpackedFolder, entryPath);
                return unpackedPath != null && File.Exists(unpackedPath) ? File.ReadAllBytes(unpackedPath) : null;
            }

            long offset = long.Parse(node.GetProperty("offset").GetString() ?? "0");
            long size = node.GetProperty("size").GetInt64();
            long start = 8 + headerSize + offset;

            if (offset < 0 || size < 0 || start + size > asar.Length) throw new InvalidDataException(".asar entry lies outside the archive.");

            return asar.AsSpan((int)start, (int)size).ToArray();
        }

        private static bool FindAsarEntry(JsonElement directory, string prefix, out JsonElement entry, out string entryPath)
        {
            if (directory.TryGetProperty("files", out JsonElement children) && children.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty child in children.EnumerateObject())
                {
                    string childPath = prefix.Length == 0 ? child.Name : $"{prefix}/{child.Name}";

                    if (child.Value.TryGetProperty("files", out _))
                    {
                        if (FindAsarEntry(child.Value, childPath, out entry, out entryPath)) return true;
                    }
                    else if (string.Equals(child.Name, BundleName, StringComparison.OrdinalIgnoreCase))
                    {
                        entry = child.Value;
                        entryPath = childPath;
                        return true;
                    }
                }
            }

            entry = default;
            entryPath = "";
            return false;
        }

        private static void ReadFolder(string folder, SortedDictionary<int, byte[]> icons)
        {
            foreach (string bundle in Directory.EnumerateFiles(folder, BundleName, SearchOption.AllDirectories)) ReadFile(bundle, icons);
            foreach (string asar in Directory.EnumerateFiles(folder, "*.asar", SearchOption.AllDirectories)) ReadFile(asar, icons);
            foreach (string zip in Directory.EnumerateFiles(folder, "HabboClassic*.zip", SearchOption.AllDirectories)) ReadFile(zip, icons);

            foreach (string file in Directory.EnumerateFiles(folder, "fx_icon_*", SearchOption.AllDirectories))
            {
                Add(Path.GetFileName(file), File.ReadAllBytes(file), icons);
            }
        }

        private static void Add(string name, byte[] data, SortedDictionary<int, byte[]> icons)
        {
            Match match = IconName.Match(name);

            // Only real PNGs: a renamed or broken file must not end up as an icon.
            if (!match.Success || !IsPng(data) || !int.TryParse(match.Groups[1].Value, out int type) || type <= 0) return;

            icons.TryAdd(type, data);
        }
    }
}
