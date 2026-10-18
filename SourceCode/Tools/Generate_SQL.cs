using Habbo_Downloader.IO;
using Habbo_Downloader.Tools;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    /// <summary>Which catalog the generated furni go into; items_base is the same for both.</summary>
    public enum FurnitureSqlTarget
    {
        /// <summary>Generate/default_furniture -> catalog_items (page_id from catalog_pages).</summary>
        Standard,

        /// <summary>Generate/bc_furniture -> catalog_items_bc (page_id from catalog_pages_bc).</summary>
        BuildersClub
    }

    public static class SQLGenerator
    {
        private static readonly Dictionary<string, FileSettings> processedFileSettings = new Dictionary<string, FileSettings>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, JToken> furniByClassname = new Dictionary<string, JToken>(StringComparer.Ordinal);
        private static JObject? indexedFurnidata;

        /// <summary>The folder inside Generate/ whose furni a target turns into SQL.</summary>
        public static string FurnitureFolder(FurnitureSqlTarget target) =>
            target == FurnitureSqlTarget.BuildersClub ? "bc_furniture" : "default_furniture";

        /// <summary>The folder Standaard furniture used before it was renamed to default_furniture.</summary>
        public const string LegacyFurnitureFolder = "Furniture";

        public static void GenerateSQL() => GenerateSQL(FurnitureSqlTarget.Standard);

        public static void GenerateSQL(FurnitureSqlTarget target)
        {
            // Per run: settings of an earlier run must not add variants again, and the furnidata is released after.
            ResetRunState();
            try
            {
                GenerateSqlRun(target);
            }
            finally
            {
                ResetRunState();
            }
        }

        private static void ResetRunState()
        {
            processedFileSettings.Clear();
            furniByClassname.Clear();
            indexedFurnidata = null;
        }

        /// <summary>The furnidata entry of a classname (the first one, as before), from an index built once per run.</summary>
        private static JToken? FindFurni(JObject furnidata, string classname)
        {
            if (!ReferenceEquals(indexedFurnidata, furnidata))
            {
                furniByClassname.Clear();
                foreach (JToken item in furnidata["roomitemtypes"]["furnitype"].Concat(furnidata["wallitemtypes"]["furnitype"]))
                {
                    string? name = item["classname"]?.ToString();
                    if (name != null) furniByClassname.TryAdd(name, item);
                }
                indexedFurnidata = furnidata;
            }
            return furniByClassname.GetValueOrDefault(classname);
        }

        private static void GenerateSqlRun(FurnitureSqlTarget target)
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Generate");
            string furnidataDir = Path.Combine(baseDir, "Furnidata");
            MoveLegacyFurnitureFolder(baseDir);
            string furnitureDir = Path.Combine(baseDir, FurnitureFolder(target));
            string outputDir = Path.Combine(baseDir, "Output_SQL");
            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(furnitureDir);
            string catalogTable = CatalogItemsTable(target);

            if (CheckForDuplicateFiles(furnitureDir))
            {
                return;
            }

            Console.WriteLine(target == FurnitureSqlTarget.BuildersClub
                ? $"Build Club furniture: Generate/{FurnitureFolder(target)}/ -> items_base + catalog_items_bc"
                : $"Standaard furniture: Generate/{FurnitureFolder(target)}/ -> items_base + catalog_items");
            Console.Write($"Enter the starting ID for items_base and {catalogTable}: ");
            int startId = int.Parse(Console.ReadLine());
            Console.Write(target == FurnitureSqlTarget.BuildersClub
                ? "Enter the Catalog_Page ID (catalog_pages_bc) for catalog_items_bc: "
                : "Enter the Catalog_Page ID (catalog_pages) for catalog_items: ");
            int pageId = int.Parse(Console.ReadLine());

            JObject furnidata;
            try
            {
                furnidata = FurnidataIO.LoadAsync(Path.Combine(furnidataDir, FurnidataIO.FlatFileName)).GetAwaiter().GetResult();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("FurnitureData.json is missing in Generate/Furnidata/.");
                return;
            }

            // Build hash sets for room and wall items.
            var roomItems = furnidata["roomitemtypes"]["furnitype"]
                .Select(item => item["classname"].ToString())
                .ToHashSet();
            var wallItems = furnidata["wallitemtypes"]["furnitype"]
                .Select(item => item["classname"].ToString())
                .ToHashSet();

            List<string> itemsBaseSQL = new List<string>();
            List<string> catalogItemsSQL = new List<string>();

            // Process physical files.
            var furnitureFiles = FurnitureFiles(furnitureDir);

            foreach (var file in furnitureFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file); // e.g. "table_norja_med"
                if (file.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Processing SWF file: {fileName}");
                    string extractedDir = Path.Combine(furnitureDir, $"{fileName}_extracted");
                    ExtractSWF(file, extractedDir);
                    ProcessExtractedSWF(extractedDir, furnidata, fileName, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId, target);
                    Console.WriteLine($"✅ Completed processing SWF file: {fileName}");
                    if (Directory.Exists(extractedDir))
                    {
                        Directory.Delete(extractedDir, true);
                    }
                }
                else if (file.EndsWith(".nitro", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".hab", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessNitroFile(file, furnidata, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId, target);
                    Console.WriteLine($"✅ Nitro file: {fileName} is done!");
                }
            }

            var variantItems = furnidata["roomitemtypes"]["furnitype"]
                .Concat(furnidata["wallitemtypes"]["furnitype"])
                .Where(item =>
                {
                    var cn = item["classname"]?.ToString();
                    return !string.IsNullOrWhiteSpace(cn) && cn.Contains("*");
                });

            foreach (var variant in variantItems)
            {
                string variantClassname = variant["classname"].ToString();
                string baseName = variantClassname.Split('*')[0];
                if (processedFileSettings.ContainsKey(baseName))
                {
                    FileSettings settings = processedFileSettings[baseName];
                    string type = roomItems.Contains(baseName) ? "s" : wallItems.Contains(baseName) ? "i" : "unknown";
                    if (type == "unknown")
                    {
                        Console.WriteLine($"Skipping unknown type for variant: {variantClassname}");
                        continue;
                    }
                    int spriteId = variant["id"]?.ToObject<int>() ?? startId++;
                    int offerId = variant["offerid"]?.ToObject<int>() ?? -1;
                    int id = startId++;
                    if (settings.FileType.Equals("SWF", StringComparison.OrdinalIgnoreCase))
                    {
                        itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES
({id}, {spriteId}, '{variantClassname}', '{variantClassname}', 1, 1, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', '{InteractionTypeFor(variantClassname)}', {settings.InteractionModesCount}, '0', '0', '0', 0, 0, '0');");
                        catalogItemsSQL.Add(CatalogItemInsert(target, id, pageId, offerId, variantClassname));
                    }
                    else if (settings.FileType.Equals("Nitro", StringComparison.OrdinalIgnoreCase))
                    {
                        // Wall items ('i') don't use tile dimensions; the objectData x/y/z are
                        // client rendering geometry, so store neutral server defaults.
                        string widthVal = type == "i" ? "1" : settings.Width.ToString(CultureInfo.InvariantCulture);
                        string lengthVal = type == "i" ? "1" : settings.Length.ToString(CultureInfo.InvariantCulture);
                        string stackHeightVal = type == "i" ? "0.00" : settings.Height.ToString(CultureInfo.InvariantCulture);
                        itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES
({id}, {spriteId}, '{variantClassname}', '{variantClassname}', {widthVal}, {lengthVal}, {stackHeightVal}, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', '{InteractionTypeFor(variantClassname)}', {settings.InteractionModesCount}, '0', '0', '0', 0, 0, '0');");
                        catalogItemsSQL.Add(CatalogItemInsert(target, id, pageId, offerId, variantClassname));
                    }
                }
            }

            List<string> combinedSQL = new List<string>
            {
                "-- items_base inserts"
            };
            combinedSQL.AddRange(itemsBaseSQL);
            combinedSQL.Add($"-- {catalogTable} inserts");
            combinedSQL.AddRange(catalogItemsSQL);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string prefix = target == FurnitureSqlTarget.BuildersClub ? "bc_items" : "items";
            string outputPath = Path.Combine(outputDir, $"{prefix}_{timestamp}.sql");
            File.WriteAllLines(outputPath, combinedSQL);
            Console.WriteLine($"📦 SQL file generated successfully:\n {outputPath}");
        }

        public static string CatalogItemsTable(FurnitureSqlTarget target) =>
            target == FurnitureSqlTarget.BuildersClub ? "catalog_items_bc" : "catalog_items";

        /// <summary>
        /// The catalog row of one furni. catalog_items_bc has no price or offer columns, so a Builders
        /// Club row keeps only the ones it has, with the same values.
        /// </summary>
        // Habbo's recolourable Builders Club furni (*_recolorable) keep their own colour on the server.
        public static string InteractionTypeFor(string classname)
        {
            return classname != null && classname.EndsWith("_recolorable", StringComparison.OrdinalIgnoreCase) ? "recolorable" : "default";
        }

        public static string CatalogItemInsert(FurnitureSqlTarget target, int id, int pageId, int offerId, string classname)
        {
            if (target == FurnitureSqlTarget.BuildersClub)
            {
                return $@"INSERT INTO `catalog_items_bc` (`id`, `item_ids`, `page_id`, `catalog_name`, `order_number`, `extradata`) VALUES
({id}, '{id}', {pageId}, '{classname}', 99, '');";
            }

            return $@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`) VALUES
({id}, '{id}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');";
        }

        /// <summary>Generate/Furniture became Generate/default_furniture; an existing folder is moved once.</summary>
        public static void MoveLegacyFurnitureFolder(string generateDir)
        {
            string legacy = Path.Combine(generateDir, LegacyFurnitureFolder);
            string current = Path.Combine(generateDir, FurnitureFolder(FurnitureSqlTarget.Standard));

            if (!Directory.Exists(legacy)) return;

            // An empty default_furniture (made at start-up) does not stop the old folder moving in.
            if (Directory.Exists(current) && !Directory.EnumerateFileSystemEntries(current).Any())
            {
                Directory.Delete(current);
            }

            if (!Directory.Exists(current))
            {
                Directory.Move(legacy, current);
                Console.WriteLine($"📁 Generate/{LegacyFurnitureFolder}/ is now Generate/{FurnitureFolder(FurnitureSqlTarget.Standard)}/.");
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(legacy).Any()) Directory.Delete(legacy);
            else Console.WriteLine($"⚠️ Generate/{LegacyFurnitureFolder}/ still has files; move them to Generate/{FurnitureFolder(FurnitureSqlTarget.Standard)}/ (Standaard) or Generate/{FurnitureFolder(FurnitureSqlTarget.BuildersClub)}/ (Build Club).");
        }

        /// <summary>
        /// One file per furni and folder: .swf before .nitro before .hab, so a .hab saved next to its .swf
        /// (Habbo download) or its .nitro is not counted twice.
        /// </summary>
        private static List<string> FurnitureFiles(string furnitureDir) =>
            Directory.GetFiles(furnitureDir, "*.*", SearchOption.AllDirectories)
                .Where(f => FurnitureFileRank(f) >= 0)
                .GroupBy(f => Path.Combine(Path.GetDirectoryName(f) ?? "", Path.GetFileNameWithoutExtension(f)), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(FurnitureFileRank).First())
                .ToList();

        private static int FurnitureFileRank(string file) => Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".swf" => 0,
            ".nitro" => 1,
            ".hab" => 2,
            _ => -1
        };

        private static bool CheckForDuplicateFiles(string furnitureDir)
        {
            var allFiles = FurnitureFiles(furnitureDir)
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .GroupBy(name => name)
                .Where(group => group.Count() > 1)
                .ToList();

            if (allFiles.Any())
            {
                Console.WriteLine("❌ Error: Duplicate filenames detected:");
                foreach (var duplicate in allFiles)
                {
                    Console.WriteLine($" - {duplicate.Key}");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Extracts a SWF's binaryData with the same native parser the .nitro
        /// converters use, so generating SQL needs no JVM and no FFDEC install.
        /// FFDEC stays as the automatic fallback for a SWF the native parser
        /// cannot read, launched through <see cref="FfdecInvocation"/> so it
        /// finds the shipped ffdec-cli (exe on Windows, jar elsewhere) rather
        /// than the ffdec.jar this method used to hardcode - that file is not
        /// part of the bundled tools, so the old path always threw.
        /// </summary>
        private static void ExtractSWF(string swfFilePath, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            NativeSwfExtractor
                    .ExtractWithFallbackAsync(
                            swfFilePath,
                            outputDir,
                            dir => RunFfdecBinaryDataExportAsync(swfFilePath, dir),
                            includeImages: false)
                    .GetAwaiter()
                    .GetResult();
        }

        private static async Task RunFfdecBinaryDataExportAsync(string swfFilePath, string outputDir)
        {
            using var process = new Process
            {
                StartInfo = FfdecInvocation.BuildStartInfo(
                        $"-onerror ignore -export binaryData \"{outputDir}\" \"{swfFilePath}\"")
            };

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                        $"⚠️ Could not start FFDEC for {Path.GetFileName(swfFilePath)} ({ex.Message}); "
                        + "this furni is skipped.");
                return;
            }

            _ = Task.Run(async () => await process.StandardOutput.ReadToEndAsync());
            _ = Task.Run(async () => await process.StandardError.ReadToEndAsync());

            if (!await FfdecInvocation.WaitForExitAsync(process, 60000))
            {
                process.Kill(true);
            }
        }

        private static void ProcessExtractedSWF(string extractedDir, JObject furnidata, string fileName, HashSet<string> roomItems, HashSet<string> wallItems, List<string> itemsBaseSQL, List<string> catalogItemsSQL, ref int startId, int pageId, FurnitureSqlTarget target)
        {
            var logicFiles = Directory.GetFiles(extractedDir, "*_logic.bin", SearchOption.AllDirectories);
            foreach (var logicFile in logicFiles)
            {
                try
                {
                    var typeAttribute = Path.GetFileNameWithoutExtension(logicFile).Split('_')[1];
                    // Both exporters write into {extractedDir}/binaryData, so this has to
                    // recurse exactly like the *_logic.bin lookup above.
                    var visualizationFilePath = Directory.GetFiles(extractedDir, "*_visualization.bin", SearchOption.AllDirectories)
                        .FirstOrDefault(file => file.Contains(typeAttribute));

                    if (visualizationFilePath == null)
                    {
                        continue;
                    }

                    int interactionModesCount = CalculateInteractionModesCount(visualizationFilePath);
                    string xmlContent = File.ReadAllText(logicFile);
                    var xmlDoc = XDocument.Parse(xmlContent);
                    var dimensionsElement = xmlDoc.Descendants("dimensions").FirstOrDefault();

                    // Set default values.
                    double width = 1, length = 1, stackHeight = 0.00;
                    if (dimensionsElement != null)
                    {
                        if (dimensionsElement.Attribute("x") != null)
                            width = double.Parse(dimensionsElement.Attribute("x").Value, CultureInfo.InvariantCulture);
                        if (dimensionsElement.Attribute("y") != null)
                            length = double.Parse(dimensionsElement.Attribute("y").Value, CultureInfo.InvariantCulture);
                        if (dimensionsElement.Attribute("z") != null)
                            stackHeight = double.Parse(dimensionsElement.Attribute("z").Value, CultureInfo.InvariantCulture);
                    }

                    var itemData = FindFurni(furnidata, fileName);

                    if (itemData == null)
                    {
                        Console.WriteLine($"❌ {fileName} was not found in the FurnitureData");
                        continue;
                    }

                    string classname = itemData["classname"]?.ToString();
                    string type = roomItems.Contains(fileName) ? "s" : wallItems.Contains(fileName) ? "i" : "unknown";
                    if (type == "unknown")
                    {
                        Console.WriteLine($"⚠️ Skipping unknown type for SWF file: {fileName}");
                        continue;
                    }

                    int spriteId = itemData["id"]?.ToObject<int>() ?? startId++;
                    int offerId = itemData["offerid"]?.ToObject<int>() ?? -1;
                    int id = startId++;

                    // Wall items ('i') don't use tile dimensions; the objectData x/y/z are
                    // client rendering geometry, so store neutral server defaults.
                    if (type == "i") { width = 1; length = 1; stackHeight = 0.00; }

                    itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES
({id}, {spriteId}, '{classname}', '{classname}', {width.ToString(CultureInfo.InvariantCulture)}, {length.ToString(CultureInfo.InvariantCulture)}, {stackHeight.ToString(CultureInfo.InvariantCulture)}, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', '{InteractionTypeFor(classname)}', {interactionModesCount}, '0', '0', '0', 0, 0, '0');");
                    catalogItemsSQL.Add(CatalogItemInsert(target, id, pageId, offerId, classname));

                    if (!processedFileSettings.ContainsKey(fileName))
                    {
                        processedFileSettings[fileName] = new FileSettings
                        {
                            InteractionModesCount = interactionModesCount,
                            Width = width,
                            Length = length,
                            Height = stackHeight,
                            FileType = "SWF"
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error processing SWF logic file {logicFile}: {ex.Message}");
                }
            }
        }

        private static void ProcessNitroFile(string nitroFilePath, JObject furnidata, HashSet<string> roomItems, HashSet<string> wallItems, List<string> itemsBaseSQL, List<string> catalogItemsSQL, ref int startId, int pageId, FurnitureSqlTarget target)
        {
            string fileName = Path.GetFileNameWithoutExtension(nitroFilePath);
            try
            {
                var itemData = FindFurni(furnidata, fileName);

                if (itemData == null)
                {
                    Console.WriteLine($"❌ {fileName} was not found in the FurnitureData");
                    return;
                }

                string classname = itemData["classname"]?.ToString();
                string type = roomItems.Contains(fileName) ? "s" : wallItems.Contains(fileName) ? "i" : "unknown";
                if (type == "unknown")
                {
                    Console.WriteLine($"Skipping unknown type for Nitro file: {fileName}");
                    return;
                }

                int spriteId = itemData["id"]?.ToObject<int>() ?? startId++;
                int offerId = itemData["offerid"]?.ToObject<int>() ?? -1;
                int id = startId++;

                string tempDir = Path.Combine(Path.GetTempPath(), "NitroExtraction", fileName);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                NitroFileExtractor.ExtractNitroFile(nitroFilePath, tempDir).GetAwaiter().GetResult();
                string nitroJsonPath = Path.Combine(tempDir, $"{fileName}.json");
                int interactionModesCount = CalculateInteractionModesCountFromNitroJson(nitroJsonPath);
                (double width, double length, double height) = GetDimensionsFromNitroJson(nitroJsonPath);

                // Read interaction properties from itemData
                bool canSitOn = itemData["cansiton"]?.ToObject<bool>() ?? false;
                bool canLayOn = itemData["canlayon"]?.ToObject<bool>() ?? false;
                bool canStandOn = itemData["canstandon"]?.ToObject<bool>() ?? false;

                // Determine if item is stackable: height != 1.0 and not sittable or layable
                bool isStackable = height != 1.0 && !canSitOn && !canLayOn;

                // Wall items ('i') don't use tile dimensions; the objectData x/y/z are
                // client rendering geometry, so store neutral server defaults.
                if (type == "i") { width = 1; length = 1; height = 0.00; }

                itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES
({id}, {spriteId}, '{classname}', '{classname}', {width.ToString(CultureInfo.InvariantCulture)}, {length.ToString(CultureInfo.InvariantCulture)}, {height.ToString(CultureInfo.InvariantCulture)}, '{(isStackable ? "1" : "0")}', '{(canSitOn ? "1" : "0")}', '{(canLayOn ? "1" : "0")}', '{(canStandOn ? "1" : "0")}', '1', '1', '0', '1', '1', '{type}', '{InteractionTypeFor(classname)}', {interactionModesCount}, '0', '0', '0', 0, 0, '0');");
                catalogItemsSQL.Add(CatalogItemInsert(target, id, pageId, offerId, classname));

                if (!processedFileSettings.ContainsKey(fileName))
                {
                    processedFileSettings[fileName] = new FileSettings
                    {
                        InteractionModesCount = interactionModesCount,
                        Width = width,
                        Length = length,
                        Height = height,
                        FileType = "Nitro"
                    };
                }

                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Nitro file {fileName}: {ex.Message}");
            }
        }

        private static int CalculateInteractionModesCount(string visualizationFilePath)
        {
            try
            {
                if (!File.Exists(visualizationFilePath))
                {
                    return 1;
                }

                string xmlContent = File.ReadAllText(visualizationFilePath);
                var xmlDoc = XDocument.Parse(xmlContent);
                var animationIds = xmlDoc.Descendants("animation")
                    .Select(a => int.TryParse(a.Attribute("id")?.Value, out int id) ? id : -1)
                    .Where(id => id >= 0 && id < 50)
                    .ToList();

                if (!animationIds.Any())
                {
                    return 1;
                }

                if (animationIds.Contains(0))
                {
                    int maxAnimationId = animationIds.Max();
                    return maxAnimationId + 1;
                }
                else
                {
                    return animationIds.Count;
                }
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private static int CalculateInteractionModesCountFromNitroJson(string nitroJsonPath)
        {
            try
            {
                if (!File.Exists(nitroJsonPath))
                {
                    return 1;
                }

                string jsonContent = File.ReadAllText(nitroJsonPath);
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    if (!doc.RootElement.TryGetProperty("visualizations", out JsonElement visualizations))
                    {
                        return 1;
                    }

                    foreach (var viz in visualizations.EnumerateArray())
                    {
                        if (viz.TryGetProperty("animations", out JsonElement animationsElement))
                        {
                            List<int> validAnimationIds = new List<int>();
                            foreach (var prop in animationsElement.EnumerateObject())
                            {
                                if (int.TryParse(prop.Name, out int animId) && animId >= 0 && animId < 50)
                                {
                                    validAnimationIds.Add(animId);
                                }
                            }
                            if (validAnimationIds.Count > 0)
                            {
                                if (validAnimationIds.Contains(0))
                                {
                                    int maxId = validAnimationIds.Max();
                                    return maxId + 1;
                                }
                                else
                                {
                                    return validAnimationIds.Count;
                                }
                            }
                        }
                    }
                }
                return 1;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private static (double width, double length, double height) GetDimensionsFromNitroJson(string nitroJsonPath)
        {
            try
            {
                if (!File.Exists(nitroJsonPath))
                {
                    return (1, 1, 0.00);
                }

                string jsonContent = File.ReadAllText(nitroJsonPath);
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    if (doc.RootElement.TryGetProperty("logic", out JsonElement logicElement) &&
                        logicElement.TryGetProperty("model", out JsonElement modelElement) &&
                        modelElement.TryGetProperty("dimensions", out JsonElement dimensionsElement))
                    {
                        double width = 1, length = 1, height = 0.00;
                        if (dimensionsElement.TryGetProperty("x", out JsonElement xElem))
                        {
                            width = xElem.GetDouble();
                        }
                        if (dimensionsElement.TryGetProperty("y", out JsonElement yElem))
                        {
                            length = yElem.GetDouble();
                        }
                        if (dimensionsElement.TryGetProperty("z", out JsonElement zElem))
                        {
                            height = zElem.GetDouble();
                        }
                        return (width, length, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error reading dimensions from Nitro JSON: " + ex.Message);
            }
            return (1, 1, 0.00);
        }

        private class FileSettings
        {
            public int InteractionModesCount { get; set; }
            public double Width { get; set; } = 1;
            public double Length { get; set; } = 1;
            public double Height { get; set; } = 0.00;
            public string FileType { get; set; }
        }
    }
}
