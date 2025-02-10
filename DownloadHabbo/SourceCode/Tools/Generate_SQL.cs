using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Xml.Linq;

namespace ConsoleApplication
{
    public static class SQLGenerator
    {
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
                Console.WriteLine("FurnitureData.json file is missing.");
                return;
            }

            JObject furnidata = JObject.Parse(File.ReadAllText(furnidataPath));

            var roomItems = furnidata["roomitemtypes"]["furnitype"]
                .Select(item => item["classname"].ToString())
                .ToHashSet();

            var wallItems = furnidata["wallitemtypes"]["furnitype"]
                .Select(item => item["classname"].ToString())
                .ToHashSet();

            List<string> itemsBaseSQL = new List<string>();
            List<string> catalogItemsSQL = new List<string>();

            var furnitureFiles = Directory.GetFiles(furnitureDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nitro") || f.EndsWith(".swf"))
                .ToList();

            foreach (var file in furnitureFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (file.EndsWith(".swf"))
                {
                    Console.WriteLine($"Processing SWF file: {fileName}");
                    string extractedDir = Path.Combine(furnitureDir, $"{fileName}_extracted");
                    ExtractSWF(file, extractedDir, ffdecPath);
                    ProcessExtractedSWF(extractedDir, furnidata, fileName, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId);
                    Console.WriteLine($"Completed processing SWF file: {fileName}");

                    if (Directory.Exists(extractedDir))
                    {
                        Directory.Delete(extractedDir, true);
                    }
                }
                else if (file.EndsWith(".nitro"))
                {
                    Console.WriteLine($"Processing Nitro file: {fileName}");
                    ProcessNitroFile(fileName, furnidata, roomItems, wallItems, itemsBaseSQL, catalogItemsSQL, ref startId, pageId);
                    Console.WriteLine($"Completed processing Nitro file: {fileName}");
                }
            }

            List<string> combinedSQL = new List<string>();
            combinedSQL.Add("-- items_base inserts");
            combinedSQL.AddRange(itemsBaseSQL);
            combinedSQL.Add("-- catalog_items inserts");
            combinedSQL.AddRange(catalogItemsSQL);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string outputPath = Path.Combine(outputDir, $"items_{timestamp}.sql");
            File.WriteAllLines(outputPath, combinedSQL);

            Console.WriteLine($"SQL file generated successfully: {outputPath}");
        }

        private static bool CheckForDuplicateFiles(string furnitureDir)
        {
            var allFiles = Directory.GetFiles(furnitureDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nitro") || f.EndsWith(".swf"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .GroupBy(name => name)
                .Where(group => group.Count() > 1)
                .ToList();

            if (allFiles.Any())
            {
                Console.WriteLine("Error: Duplicate filenames detected:");
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
                throw new FileNotFoundException($"FFDec tool not found at: {ffdecPath}");
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

        private static void ProcessExtractedSWF(
            string extractedDir,
            JObject furnidata,
            string fileName,
            HashSet<string> roomItems,
            HashSet<string> wallItems,
            List<string> itemsBaseSQL,
            List<string> catalogItemsSQL,
            ref int startId,
            int pageId)
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

                    var itemData = furnidata["roomitemtypes"]["furnitype"]
                                   .Concat(furnidata["wallitemtypes"]["furnitype"])
                                   .FirstOrDefault(item => item["classname"]?.ToString() == fileName);

                    if (itemData == null)
                    {
                        Console.WriteLine($"Item data not found for: {fileName}");
                        continue;
                    }

                    string classname = itemData["classname"]?.ToString();
                    string type = roomItems.Contains(fileName) ? "s" : wallItems.Contains(fileName) ? "i" : "unknown";

                    if (type == "unknown")
                    {
                        Console.WriteLine($"Skipping unknown type for SWF file: {fileName}");
                        continue;
                    }

                    int spriteId = itemData["id"]?.ToObject<int>() ?? startId++;
                    int offerId = itemData["offerid"]?.ToObject<int>() ?? -1; // Default to -1
                    int id = startId++;

                    itemsBaseSQL.Add($@"
INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`)
VALUES ({id}, {spriteId}, '{classname}', '{classname}', 1, 1, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', 'default', {interactionModesCount}, '0', '0', '0', 0, 0, '0');");

                    catalogItemsSQL.Add($@"
INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`)
VALUES ({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing SWF logic file {logicFile}: {ex.Message}");
                }
            }
        }

        private static void ProcessNitroFile(
            string fileName,
            JObject furnidata,
            HashSet<string> roomItems,
            HashSet<string> wallItems,
            List<string> itemsBaseSQL,
            List<string> catalogItemsSQL,
            ref int startId,
            int pageId)
        {
            try
            {
                var itemData = furnidata["roomitemtypes"]["furnitype"]
                               .Concat(furnidata["wallitemtypes"]["furnitype"])
                               .FirstOrDefault(item => item["classname"]?.ToString() == fileName);

                if (itemData == null)
                {
                    Console.WriteLine($"Item data not found for: {fileName}");
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
                int offerId = itemData["offerid"]?.ToObject<int>() ?? -1; // Default to -1
                int id = startId++;

                itemsBaseSQL.Add($@"
INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`)
VALUES ({id}, {spriteId}, '{classname}', '{classname}', 1, 1, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', '{type}', 'default', 1, '0', '0', '0', 0, 0, '0');");

                catalogItemsSQL.Add($@"
INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`)
VALUES ({id}, '{spriteId}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
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
                    .Select(a => int.TryParse(a.Attribute("id")?.Value, out var id) ? id : -1)
                    .Where(id => id >= 0)
                    .ToList();

                int maxAnimationId = animationIds.Any() ? animationIds.Max() : -1;
                int interactionModesCount = maxAnimationId + 1;

                return interactionModesCount <= 0 ? 1 : interactionModesCount;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }
}
