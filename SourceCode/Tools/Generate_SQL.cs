using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public static class SQLGenerator
    {
        private static Dictionary<string, FileSettings> processedFileSettings = new Dictionary<string, FileSettings>(StringComparer.OrdinalIgnoreCase);

        public static void GenerateSQL()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Generate");
            string furnidataDir = Path.Combine(baseDir, "Furnidata");
            string furnitureDir = Path.Combine(baseDir, "Furniture");
            string outputDir = Path.Combine(baseDir, "Output_SQL");
            string ffdecPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools\\ffdec\\ffdec.jar");

            Directory.CreateDirectory(outputDir);

            if (CheckForDuplicateFiles(furnitureDir))
            {
                return;
            }

            Console.Write("Enter the starting ID for items_base and catalog_items: ");
            int startId = int.Parse(Console.ReadLine());

            Console.Write("Enter the Catalog_Page ID for catalog_items: ");
            int pageId = int.Parse(Console.ReadLine());

            string furnidataPath = Path.Combine(furnidataDir, "FurnitureData.json");
            if (!File.Exists(furnidataPath))
            {
                Console.WriteLine("⚠️ FurnitureData.json file is missing.");
                return;
            }

            JObject furnidata = JObject.Parse(File.ReadAllText(furnidataPath));

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
            var furnitureFiles = Directory.GetFiles(furnitureDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nitro", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in furnitureFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file); // e.g. "table_norja_med"
                if (file.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Processing SWF file: {fileName}");
                    string extractedDir = Path.Combine(furnitureDir, $"{fileName}_extracted");
                    ExtractSWF(file, extractedDir, ffdecPath);
                    ProcessExtractedSWF(extractedDir, furnidata, fileName, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId);
                    Console.WriteLine($"✅ Completed processing SWF file: {fileName}");
                    if (Directory.Exists(extractedDir))
                    {
                        Directory.Delete(extractedDir, true);
                    }
                }
                else if (file.EndsWith(".nitro", StringComparison.OrdinalIgnoreCase))
                {
                    ProcessNitroFile(file, furnidata, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId);
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
({id}, {spriteId}, '{variantClassname}', '{variantClassname}', 1, 1, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', 'default', {settings.InteractionModesCount}, '0', '0', '0', 0, 0, '0');");

                        catalogItemsSQL.Add($@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`) VALUES
({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{variantClassname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
                    }
                    else if (settings.FileType.Equals("Nitro", StringComparison.OrdinalIgnoreCase))
                    {
                        itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES 
({id}, {spriteId}, '{variantClassname}', '{variantClassname}', {settings.Width.ToString(CultureInfo.InvariantCulture)}, {settings.Length.ToString(CultureInfo.InvariantCulture)}, {settings.Height.ToString(CultureInfo.InvariantCulture)}, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', 'default', {settings.InteractionModesCount}, '0', '0', '0', 0, 0, '0');");

                        catalogItemsSQL.Add($@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`) VALUES 
({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{variantClassname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
                    }
                }
            }

            List<string> combinedSQL = new List<string>
            {
                "-- items_base inserts"
            };
            combinedSQL.AddRange(itemsBaseSQL);
            combinedSQL.Add("-- catalog_items inserts");
            combinedSQL.AddRange(catalogItemsSQL);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string outputPath = Path.Combine(outputDir, $"items_{timestamp}.sql");
            File.WriteAllLines(outputPath, combinedSQL);

            Console.WriteLine($"📦 SQL file generated successfully:\n {outputPath}");
        }

        private static bool CheckForDuplicateFiles(string furnitureDir)
        {
            var allFiles = Directory.GetFiles(furnitureDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nitro", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".swf", StringComparison.OrdinalIgnoreCase))
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

        private static void ExtractSWF(string swfFilePath, string outputDir, string ffdecPath)
        {
            if (!File.Exists(ffdecPath))
            {
                throw new FileNotFoundException($"❌ FFDec tool not found at: {ffdecPath}");
            }
            Directory.CreateDirectory(outputDir);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{ffdecPath}\" -export binaryData \"{outputDir}\" \"{swfFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
        }

        private static void ProcessExtractedSWF(string extractedDir, JObject furnidata, string fileName, HashSet<string> roomItems, HashSet<string> wallItems, List<string> itemsBaseSQL, List<string> catalogItemsSQL, ref int startId, int pageId)
        {
            var logicFiles = Directory.GetFiles(extractedDir, "*_logic.bin", SearchOption.AllDirectories);
            foreach (var logicFile in logicFiles)
            {
                try
                {
                    var typeAttribute = Path.GetFileNameWithoutExtension(logicFile).Split('_')[1];
                    var visualizationFilePath = Directory.GetFiles(extractedDir, "*_visualization.bin", SearchOption.TopDirectoryOnly)
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

                    var itemData = furnidata["roomitemtypes"]["furnitype"]
                        .Concat(furnidata["wallitemtypes"]["furnitype"])
                        .FirstOrDefault(item => item["classname"]?.ToString() == fileName);
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

                    itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES 
({id}, {spriteId}, '{classname}', '{classname}', {width.ToString(CultureInfo.InvariantCulture)}, {length.ToString(CultureInfo.InvariantCulture)}, {stackHeight.ToString(CultureInfo.InvariantCulture)}, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', 'default', {interactionModesCount}, '0', '0', '0', 0, 0, '0');");

                    catalogItemsSQL.Add($@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`) VALUES 
({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");

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

        private static void ProcessNitroFile(string nitroFilePath, JObject furnidata, HashSet<string> roomItems, HashSet<string> wallItems, List<string> itemsBaseSQL, List<string> catalogItemsSQL, ref int startId, int pageId)
        {
            string fileName = Path.GetFileNameWithoutExtension(nitroFilePath);
            try
            {
                var itemData = furnidata["roomitemtypes"]["furnitype"]
                    .Concat(furnidata["wallitemtypes"]["furnitype"])
                    .FirstOrDefault(item => item["classname"]?.ToString() == fileName);
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

                itemsBaseSQL.Add($@"INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`) VALUES 
({id}, {spriteId}, '{classname}', '{classname}', {width.ToString(CultureInfo.InvariantCulture)}, {length.ToString(CultureInfo.InvariantCulture)}, {height.ToString(CultureInfo.InvariantCulture)}, '{(isStackable ? "1" : "0")}', '{(canSitOn ? "1" : "0")}', '{(canLayOn ? "1" : "0")}', '{(canStandOn ? "1" : "0")}', '1', '1', '0', '1', '1', '{type}', 'default', {interactionModesCount}, '0', '0', '0', 0, 0, '0');");

                catalogItemsSQL.Add($@"INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`) VALUES 
({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");

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