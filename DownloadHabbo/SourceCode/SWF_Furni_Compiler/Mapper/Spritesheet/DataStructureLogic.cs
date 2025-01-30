using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Habbo_Downloader.SWFCompiler.Mapper
{
    public class SwfTag
    {
        public string[] Names { get; set; }
        public int[] Tags { get; set; }
    }

    public class ImageTag
    {
        public int CharacterId { get; set; }
        public string ClassName { get; set; }
        public byte[] ImgData { get; set; }
    }

    public class ImageBundle
    {
        public List<(string ClassName, byte[] ImgData)> Images { get; set; } = new();

        public void AddImage(string className, byte[] imgData)
        {
            Images.Add((className, imgData));
        }
    }

    public class SpriteSheetData
    {
        [JsonPropertyName("frames")]
        public Dictionary<string, FrameData> Frames { get; set; } = new();

        [JsonPropertyName("meta")]
        public MetaData Meta { get; set; }
    }

    public class FrameData
    {
        [JsonPropertyName("frame")]
        public RectData Frame { get; set; }

        [JsonPropertyName("rotated")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool Rotated { get; set; } = false;

        [JsonPropertyName("trimmed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool Trimmed { get; set; } = false;

        [JsonPropertyName("spriteSourceSize")]
        public RectData SpriteSourceSize { get; set; }

        [JsonPropertyName("sourceSize")]
        public SizeData SourceSize { get; set; }

        [JsonPropertyName("pivot")]
        public PivotData Pivot { get; set; }
    }

    public class RectData
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("w")]
        public int Width { get; set; }

        [JsonPropertyName("h")]
        public int Height { get; set; }
    }
    public class SizeData
    {
        [JsonPropertyName("w")]
        public int Width { get; set; }

        [JsonPropertyName("h")]
        public int Height { get; set; }
    }

    public class PivotData
    {
        [JsonPropertyName("x")]
        public float X { get; set; } = 0.5f; // Default pivot (center)

        [JsonPropertyName("y")]
        public float Y { get; set; } = 0.5f; // Default pivot (center)
    }

    public class MetaData
    {
        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; } = "RGBA8888"; // Default format

        [JsonPropertyName("size")]
        public SizeData Size { get; set; }

        [JsonPropertyName("scale")]
        public float Scale { get; set; } = 1.0f; // Default scale
    }

    public class HabboAssetSWF
    {
        public List<SwfTag> SymbolTags()
        {
            return new List<SwfTag>();
        }

        public List<ImageTag> ImageTags()
        {
            return new List<ImageTag>();
        }

        public string GetDocumentClass()
        {
            return "DocumentClass";
        }
    }

    public static class SpriteSheetGenerator
    {
        private static readonly Dictionary<string, string> ImageSources = new();

        public static async Task<SpriteSheetData> GenerateSpriteSheet(HabboAssetSWF habboAssetSWF, bool convertCase = false)
        {
            var tagList = habboAssetSWF.SymbolTags();
            var names = new List<string>();
            var tags = new List<int>();

            string documentClass = habboAssetSWF.GetDocumentClass();

            if (convertCase)
            {
                documentClass = ConvertToSnakeCase(documentClass);
            }

            foreach (var tag in tagList)
            {
                names.AddRange(tag.Names);
                tags.AddRange(tag.Tags);
            }

            var imageBundle = new ImageBundle();
            var imageTags = habboAssetSWF.ImageTags();

            foreach (var imageTag in imageTags)
            {
                if (tags.Contains(imageTag.CharacterId))
                {
                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (tags[i] != imageTag.CharacterId) continue;

                        if (names[i] == imageTag.ClassName) continue;

                        if (imageTag.ClassName.StartsWith("sh_")) continue;

                        if (imageTag.ClassName.Contains("_32_")) continue;

                        string key = names[i].Substring(documentClass.Length + 1);
                        string value = imageTag.ClassName.Substring(documentClass.Length + 1);
                        ImageSources[key] = value;
                    }
                }

                if (imageTag.ClassName.StartsWith("sh_")) continue;

                if (imageTag.ClassName.Contains("_32_")) continue;

                string className = imageTag.ClassName;

                if (convertCase)
                {
                    className = ConvertToSnakeCase(className).Substring(1);
                }

                imageBundle.AddImage(className, imageTag.ImgData);
            }

            if (imageBundle.Images.Count == 0)
            {
                return null;
            }

            return await PackImages(documentClass, imageBundle, convertCase);
        }

        private static string ConvertToSnakeCase(string input)
        {
            return Regex.Replace(input, "(?:^|\\.?)([A-Z])", match => "_" + match.Groups[1].Value.ToLower()).TrimStart('_');
        }

        private static async Task<SpriteSheetData> PackImages(string documentClass, ImageBundle imageBundle, bool convertCase)
        {
            return new SpriteSheetData
            {
                Frames = new Dictionary<string, FrameData>(),
                Meta = new MetaData
                {
                    Image = $"{documentClass}.png",
                    Size = new SizeData { Width = 1024, Height = 1024 }
                }
            };
        }
    }
}