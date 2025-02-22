using System.Xml.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleApplication
{
    // This class represents the final JSON structure.
    public class FurnitureDataJson
    {
        [JsonPropertyName("roomitemtypes")]
        public FurnitureItemTypes RoomItemTypes { get; set; }
        [JsonPropertyName("wallitemtypes")]
        public FurnitureItemTypes WallItemTypes { get; set; }
    }

    // This is just a wrapper for the list of items.
    public class FurnitureItemTypes
    {
        [JsonPropertyName("furnitype")]
        public List<FurnitureType> Furnitype { get; set; } = new List<FurnitureType>();
    }

    // This class represents each furniture item (the properties mirror your IFurnitureType interface).
    public class FurnitureType
    {
        [JsonIgnore]
        public string FurniType { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("classname")]
        public string Classname { get; set; }
        [JsonPropertyName("revision")]
        public int Revision { get; set; }
        [JsonPropertyName("category")]
        public string Category { get; set; }
        [JsonPropertyName("defaultdir")]
        public int Defaultdir { get; set; }
        [JsonPropertyName("xdim")]
        public int Xdim { get; set; }
        [JsonPropertyName("ydim")]
        public int Ydim { get; set; }
        [JsonPropertyName("partcolors")]
        public Partcolors Partcolors { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("adurl")]
        public string Adurl { get; set; }
        [JsonPropertyName("offerid")]
        public int Offerid { get; set; }
        [JsonPropertyName("buyout")]
        public bool Buyout { get; set; }
        [JsonPropertyName("rentofferid")]
        public int Rentofferid { get; set; }
        [JsonPropertyName("rentbuyout")]
        public bool Rentbuyout { get; set; }
        [JsonPropertyName("bc")]
        public bool Bc { get; set; }
        [JsonPropertyName("excludeddynamic")]
        public bool Excludeddynamic { get; set; }
        [JsonPropertyName("customparams")]
        public string Customparams { get; set; }
        [JsonPropertyName("specialtype")]
        public int Specialtype { get; set; }
        [JsonPropertyName("canstandon")]
        public bool Canstandon { get; set; }
        [JsonPropertyName("cansiton")]
        public bool Cansiton { get; set; }
        [JsonPropertyName("canlayon")]
        public bool Canlayon { get; set; }
        [JsonPropertyName("furniline")]
        public string Furniline { get; set; }
        [JsonPropertyName("environment")]
        public string Environment { get; set; }
        [JsonPropertyName("rare")]
        public bool Rare { get; set; }

        // Creates an instance from an XElement and the type ("floor" or "wall")
        public static FurnitureType FromXElement(XElement element, string type)
        {
            var ft = new FurnitureType();
            ft.FurniType = type;
            ft.Id = (int?)element.Attribute("id") ?? 0;
            ft.Classname = (string)element.Attribute("classname") ?? "";
            ft.Revision = (int?)element.Element("revision") ?? 0;
            ft.Category = (string)element.Element("category") ?? "unknown";

            if (type == "floor")
            {
                ft.Defaultdir = (int?)element.Element("defaultdir") ?? 0;
                ft.Xdim = (int?)element.Element("xdim") ?? 0;
                ft.Ydim = (int?)element.Element("ydim") ?? 0;
                // Read partcolors if available
                var partcolorsElement = element.Element("partcolors");
                if (partcolorsElement != null)
                {
                    var colors = partcolorsElement.Elements("color")
                        .Select(c =>
                        {
                            var code = (string)c;
                            if (!string.IsNullOrEmpty(code) && !code.StartsWith("#"))
                                code = "#" + code;
                            return code;
                        })
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                    ft.Partcolors = new Partcolors { Color = colors };
                }
            }

            ft.Name = (string)element.Element("name") ?? "";
            ft.Description = (string)element.Element("description") ?? "";
            ft.Adurl = (string)element.Element("adurl") ?? "";
            ft.Offerid = (int?)element.Element("offerid") ?? 0;
            ft.Buyout = ((int?)element.Element("buyout") ?? 0) == 1;
            ft.Rentofferid = (int?)element.Element("rentofferid") ?? 0;
            ft.Rentbuyout = ((int?)element.Element("rentbuyout") ?? 0) == 1;
            ft.Bc = ((int?)element.Element("bc") ?? 0) == 1;
            ft.Excludeddynamic = ((int?)element.Element("excludeddynamic") ?? 0) == 1;
            ft.Customparams = (string)element.Element("customparams") ?? "";
            ft.Specialtype = (int?)element.Element("specialtype") ?? 0;

            if (type == "floor")
            {
                ft.Canstandon = ((int?)element.Element("canstandon") ?? 0) == 1;
                ft.Cansiton = ((int?)element.Element("cansiton") ?? 0) == 1;
                ft.Canlayon = ((int?)element.Element("canlayon") ?? 0) == 1;
            }

            ft.Furniline = (string)element.Element("furniline") ?? "";
            ft.Environment = (string)element.Element("environment") ?? "";
            ft.Rare = ((int?)element.Element("rare") ?? 0) == 1;

            return ft;
        }
    }

    public class Partcolors
    {
        [JsonPropertyName("color")]
        public List<string> Color { get; set; }
    }

    // This class handles converting the downloaded XML into JSON.
    public static class FurnidataConverter
    {
        public static void ConvertXmlToJson(string xmlFilePath, string jsonFilePath)
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            var root = doc.Element("furnidata");
            if (root == null)
            {
                throw new Exception("Invalid XML format: missing 'furnidata' root element.");
            }

            var output = new FurnitureDataJson();

            // Process floor items from <roomitemtypes>
            var roomItems = root.Element("roomitemtypes");
            if (roomItems != null)
            {
                var floorItems = roomItems.Elements("furnitype")
                    .Select(x => FurnitureType.FromXElement(x, "floor"))
                    .ToList();
                if (floorItems.Any())
                {
                    output.RoomItemTypes = new FurnitureItemTypes { Furnitype = floorItems };
                }
            }

            // Process wall items from <wallitemtypes>
            var wallItems = root.Element("wallitemtypes");
            if (wallItems != null)
            {
                var wallItemList = wallItems.Elements("furnitype")
                    .Select(x => FurnitureType.FromXElement(x, "wall"))
                    .ToList();
                if (wallItemList.Any())
                {
                    output.WallItemTypes = new FurnitureItemTypes { Furnitype = wallItemList };
                }
            }

            // Serialize to JSON (using System.Text.Json)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(output, options);
            File.WriteAllText(jsonFilePath, json);
        }
    }
}
