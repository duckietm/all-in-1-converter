using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

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

            Directory.CreateDirectory(outputDir);

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

            List<string> itemsBaseSQL = new List<string>();
            List<string> catalogItemsSQL = new List<string>();

            var roomItems = furnidata["roomitemtypes"]["furnitype"];
            foreach (var item in roomItems)
            {
                int id = startId++;
                string classname = item["classname"].ToString();
                int width = item["xdim"].ToObject<int>();
                int length = item["ydim"].ToObject<int>();
                int offerId = item["offerid"].ToObject<int>();

                itemsBaseSQL.Add($@"
INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`)
VALUES ({id}, {id}, '{classname}', '{classname}', {width}, {length}, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', 's', 'default', 1, '0', '0', '0', 0, 0, '0');");

                catalogItemsSQL.Add($@"
INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`)
VALUES ({id}, '{id}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
            }

            var wallItems = furnidata["wallitemtypes"]["furnitype"];
            foreach (var item in wallItems)
            {
                int id = startId++;
                string classname = item["classname"].ToString();
                int offerId = item["offerid"].ToObject<int>();

                itemsBaseSQL.Add($@"
INSERT INTO `items_base` (`id`, `sprite_id`, `item_name`, `public_name`, `width`, `length`, `stack_height`, `allow_stack`, `allow_sit`, `allow_lay`, `allow_walk`, `allow_gift`, `allow_trade`, `allow_recycle`, `allow_marketplace_sell`, `allow_inventory_stack`, `type`, `interaction_type`, `interaction_modes_count`, `vending_ids`, `multiheight`, `customparams`, `effect_id_male`, `effect_id_female`, `clothing_on_walk`)
VALUES ({id}, {id}, '{classname}', '{classname}', 1, 1, 0.00, '0', '0', '0', '0', '1', '1', '0', '1', '1', 'i', 'default', 1, '0', '0', '0', 0, 0, '0');");

                catalogItemsSQL.Add($@"
INSERT INTO `catalog_items` (`id`, `item_ids`, `page_id`, `offer_id`, `song_id`, `order_number`, `catalog_name`, `cost_credits`, `cost_points`, `points_type`, `amount`, `limited_sells`, `limited_stack`, `extradata`, `have_offer`, `club_only`)
VALUES ({id}, '{id}', {pageId}, {offerId}, 0, 99, '{classname}', 5, 0, 0, 1, 0, 0, '', '1', '0');");
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
    }
}