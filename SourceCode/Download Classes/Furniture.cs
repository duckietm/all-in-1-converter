using System.Xml.Linq;
using Habbo_Downloader.Tools;
using static ConsoleApplication.HabboAssetDownloader;


namespace ConsoleApplication
{
    /// <summary>Downloads Habbo's furniture into Habbo_Default/hof_furni/{swf,hab,icons}.</summary>
    internal static class FurnitureDownloader
    {
        internal static async Task DownloadFurnitureAsync()
        {
            var config = IniFileParser.Parse("config.ini");

            string furnidataUrl = config["AppSettings:furnidataXML"];
            string furnitureUrl = config["AppSettings:furnitureurl"].TrimEnd('/');

            Console.WriteLine($"Furniture download format: {ConverterSettings.DownloadFormat} (config.ini download_format)");

            Directory.CreateDirectory(HofFurniPaths.Icons);
            PrepareFolder(HabboAssetFolder.Furniture);

            string furnidataXml;
            try
            {
                Console.WriteLine("Downloading furnidata...");
                furnidataXml = await Http.GetStringAsync(furnidataUrl);
                Console.WriteLine("Furnidata downloaded successfully.");
            }
            catch (Exception ex)
            {
                Log(ConsoleColor.Red, "Error downloading furnidata: " + ex.Message);
                return;
            }

            XElement? root;
            try
            {
                root = XDocument.Parse(furnidataXml).Element("furnidata");
            }
            catch (Exception ex)
            {
                Log(ConsoleColor.Red, "Error: Invalid furnidata XML format. " + ex.Message);
                return;
            }

            if (root == null)
            {
                Log(ConsoleColor.Red, "Error: Invalid furnidata XML format.");
                return;
            }

            var entries = new List<(string classname, int revision)>();
            foreach (var section in new[] { "roomitemtypes", "wallitemtypes" })
            {
                foreach (var item in root.Element(section)?.Elements("furnitype") ?? Enumerable.Empty<XElement>())
                {
                    string classname = (string?)item.Attribute("classname") ?? "";
                    int revision = (int?)item.Element("revision") ?? 0;
                    if (!string.IsNullOrEmpty(classname)) entries.Add((classname, revision));
                }
            }

            Console.WriteLine($"Found {entries.Count} furniture entries.");

            // One library per furni (colour variants share it), one icon per variant.
            var libraries = entries
                .GroupBy(entry => entry.classname.Split('*')[0], StringComparer.OrdinalIgnoreCase)
                .Select(group => (name: group.Key, baseUrl: $"{furnitureUrl}/{group.Max(entry => entry.revision)}/{group.Key}"));

            Counts counts = await DownloadLibrariesAsync(HabboAssetFolder.Furniture, libraries);

            int iconCount = 0;
            await Parallel.ForEachAsync(entries, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelDownloads }, async (entry, _) =>
            {
                string furnitureName = entry.classname.Split('*')[0];
                string variant = entry.classname.Contains('*') ? entry.classname.Split('*')[1] : "";
                string iconName = string.IsNullOrEmpty(variant) ? furnitureName : $"{furnitureName}_{variant}";
                string iconPath = Path.Combine(HofFurniPaths.Icons, $"{iconName}_icon.png");

                if (File.Exists(iconPath)) return;

                byte[]? icon = await TryGetAsync($"{furnitureUrl}/{entry.revision}/{iconName}_icon.png");
                if (icon != null && IsPng(icon))
                {
                    await SaveAsync(iconPath, icon);
                    Interlocked.Increment(ref iconCount);
                    Log(ConsoleColor.Green, $"Downloaded: {iconName}_icon.png");
                }
            });

            Log(ConsoleColor.Green, "Downloading furniture completed!");
            Log(ConsoleColor.Green, Summary($"furniture files and {iconCount} new icons", counts));
        }
    }
}
