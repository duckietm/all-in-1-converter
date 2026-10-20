using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Habbo_Downloader.Tools;

namespace ConsoleApplication.FixSettings
{
    /// <summary>
    /// The shared part of the Database fixes that compare items_base with Habbo_Default/hof_furni/hab: which rows
    /// they check, reading each furni's .hab json once, and the list of .hab files that are missing or unreadable.
    /// </summary>
    internal static class HabFurniScan
    {
        /// <summary>
        /// Floor (s) and wall (i) furni without multiheight; pets ("a0 pet0" ...) have no furni bundle. Fixes add
        /// their own conditions after it.
        /// </summary>
        public const string BaseFilter =
            "(multiheight IS NULL OR multiheight IN ('', '0')) AND type IN ('s', 'i') AND item_name NOT LIKE 'a0%'";

        public static string HabDir => HofFurniPaths.Hab;

        /// <summary>False (with a message) when the hab folder is not there.</summary>
        public static bool CheckHabFolder()
        {
            if (Directory.Exists(HabDir)) return true;

            Console.WriteLine($"⚠️ {HabDir} not found. Download the furniture as .hab first (Habbo Original -> Download furniture).");
            return false;
        }

        /// <summary>The bundle a classname draws from: the part before '*' (colour variants).</summary>
        public static string BundleName(string itemName) => itemName.Split('*')[0].Trim();

        /// <summary>
        /// Reads every bundle the item names need, once each (colour variants share their base furni's bundle), and
        /// keeps what <paramref name="read"/> takes from its json; null for a bundle that is missing or unreadable.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, T?>> ReadBundlesAsync<T>(IEnumerable<string> itemNames, Func<JsonElement, T?> read)
            where T : struct
        {
            var results = new ConcurrentDictionary<string, T?>(StringComparer.OrdinalIgnoreCase);
            var bundleNames = itemNames.Select(BundleName).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            int done = 0;

            await Parallel.ForEachAsync(bundleNames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (name, token) =>
            {
                results[name] = await ReadBundleAsync(Path.Combine(HabDir, name + ".hab"), read, token);
                int finished = Interlocked.Increment(ref done);
                if (finished % 500 == 0) Console.WriteLine($"   {finished}/{bundleNames.Count} bundles read...");
            });

            return results;
        }

        /// <summary>What a bundle gave for this item name, or null when it has no readable bundle.</summary>
        public static T? ResultFor<T>(IReadOnlyDictionary<string, T?> results, string itemName) where T : struct
        {
            string name = BundleName(itemName);
            return name.Length > 0 && results.TryGetValue(name, out T? value) ? value : null;
        }

        private static async Task<T?> ReadBundleAsync<T>(string habPath, Func<JsonElement, T?> read, CancellationToken token)
            where T : struct
        {
            if (!File.Exists(habPath)) return null;

            try
            {
                // Only the json is needed: read the bundle in memory, without unpacking it to disk.
                byte[] data = await File.ReadAllBytesAsync(habPath, token);
                if (!HabBundle.IsHab(data)) return null;

                foreach (var (fileName, content) in HabBundle.Read(data).Files)
                {
                    if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                    using JsonDocument doc = JsonDocument.Parse(content);
                    return read(doc.RootElement);
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Generate/&lt;prefix&gt;_&lt;date&gt;.sql and Generate/Missing_Hab_files_&lt;date&gt;.txt, with the same date.</summary>
        public static (string SqlPath, string MissingPath) OutputPaths(string sqlPrefix)
        {
            string generateDir = Path.Combine(Directory.GetCurrentDirectory(), "Generate");
            Directory.CreateDirectory(generateDir);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            return (Path.Combine(generateDir, $"{sqlPrefix}_{stamp}.sql"), Path.Combine(generateDir, $"Missing_Hab_files_{stamp}.txt"));
        }

        public static string Now => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        /// <summary>
        /// One line per .hab file that is needed, sorted: first the files that are not in the hab folder, then the
        /// ones that are there but could not be read. Each line names the items_base rows that use the file.
        /// </summary>
        public static async Task WriteMissingListAsync(string path, string checkedRows, List<string> itemNames)
        {
            var byFile = itemNames
                .GroupBy(itemName => BundleName(itemName) + ".hab", StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var notFound = byFile.Where(group => !File.Exists(Path.Combine(HabDir, group.Key))).ToList();
            var unreadable = byFile.Where(group => File.Exists(Path.Combine(HabDir, group.Key))).ToList();

            var text = new StringBuilder();
            text.AppendLine($"# .hab files needed by items_base ({checkedRows}), {Now}");
            text.AppendLine($"# Folder: {Path.GetFullPath(HabDir)}");
            text.AppendLine($"# {notFound.Count} missing, {unreadable.Count} unreadable; {itemNames.Count} items_base rows in total.");
            text.AppendLine();

            text.AppendLine($"## Missing ({notFound.Count})");
            foreach (var group in notFound) text.AppendLine(FileLine(group));

            if (unreadable.Count > 0)
            {
                text.AppendLine();
                text.AppendLine($"## In the folder but unreadable, or without the data this fix needs ({unreadable.Count})");
                foreach (var group in unreadable) text.AppendLine(FileLine(group));
            }

            await File.WriteAllTextAsync(path, text.ToString(), new UTF8Encoding(false));
        }

        /// <summary>"name.hab" alone, or with the classnames when colour variants use it ("name.hab  (name*1, name*2)").</summary>
        private static string FileLine(IGrouping<string, string> group)
        {
            var names = group.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(VariantNumber)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            bool onlyItself = names.Count == 1 && string.Equals(names[0] + ".hab", group.Key, StringComparison.OrdinalIgnoreCase);
            return onlyItself ? group.Key : $"{group.Key}  ({string.Join(", ", names)})";
        }

        /// <summary>The colour number of "name*3" (3), -1 for the plain name, so variants list in number order.</summary>
        private static int VariantNumber(string itemName)
        {
            int star = itemName.IndexOf('*');
            return star >= 0 && int.TryParse(itemName.AsSpan(star + 1), out int number) ? number : -1;
        }
    }
}
