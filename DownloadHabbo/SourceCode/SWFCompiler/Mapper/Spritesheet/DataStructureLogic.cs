using System;
using System.Collections.Generic;
using System.Linq;
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
        public Dictionary<string, FrameData> Frames { get; set; } = new();
        public MetaData Meta { get; set; }
    }

    public class FrameData
    {
        public RectData Frame { get; set; }
        public bool Rotated { get; set; }
        public bool Trimmed { get; set; }
        public RectData SpriteSourceSize { get; set; }
        public SizeData SourceSize { get; set; }
        public PivotData Pivot { get; set; }
    }

    public class RectData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }

    public class SizeData
    {
        public int W { get; set; }
        public int H { get; set; }
    }

    public class PivotData
    {
        public float X { get; set; } = 0.5f;
        public float Y { get; set; } = 0.5f;
    }

    public class MetaData
    {
        public string Image { get; set; }
        public string Format { get; set; } = "RGBA8888";
        public SizeData Size { get; set; }
        public float Scale { get; set; } = 1.0f;
    }

    public class HabboAssetSWF
    {
        public List<SwfTag> SymbolTags()
        {
            // Implement logic to extract symbol tags from the SWF file
            // For now, return a placeholder list
            return new List<SwfTag>();
        }

        public List<ImageTag> ImageTags()
        {
            // Implement logic to extract image tags from the SWF file
            // For now, return a placeholder list
            return new List<ImageTag>();
        }

        public string GetDocumentClass()
        {
            // Implement logic to get the document class from the SWF file
            // For now, return a placeholder string
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

            // Collect names and tags from symbol tags
            foreach (var tag in tagList)
            {
                names.AddRange(tag.Names);
                tags.AddRange(tag.Tags);
            }

            var imageBundle = new ImageBundle();
            var imageTags = habboAssetSWF.ImageTags();

            // Process image tags
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
            // Implement the logic to pack images into a sprite sheet
            // This will depend on your specific requirements and libraries
            // For now, return a placeholder SpriteSheetData object
            return new SpriteSheetData
            {
                Frames = new Dictionary<string, FrameData>(),
                Meta = new MetaData
                {
                    Image = $"{documentClass}.png",
                    Size = new SizeData { W = 1024, H = 1024 } // Placeholder size
                }
            };
        }
    }
}