using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json.Serialization;

namespace Habbo_Downloader.SWFCompiler.Mapper.Spritesheets
{
    public static class SpriteSheetMapper
    {
        public class SpriteSheetData
        {
            [JsonPropertyName("meta")]
            public MetaData Meta { get; set; }

            [JsonPropertyName("frames")]
            public Dictionary<string, FrameData> Frames { get; set; } = new();
        }

        public class MetaData
        {
            [JsonPropertyName("image")]
            public string Image { get; set; }

            [JsonPropertyName("size")]
            public SizeData Size { get; set; }
        }

        public class FrameData
        {
            [JsonPropertyName("frame")]
            public RectData Frame { get; set; }
        }

        public class SizeData
        {
            [JsonPropertyName("w")]
            public int Width { get; set; }

            [JsonPropertyName("h")]
            public int Height { get; set; }
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

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(Dictionary<string, Bitmap> images, string outputDirectory, string name)
        {
            if (images == null || images.Count == 0) return (null, null);

            int totalHeight = 0;
            int maxWidth = 0;

            foreach (var image in images.Values)
            {
                totalHeight += image.Height;
                if (image.Width > maxWidth) maxWidth = image.Width;
            }

            var spriteSheet = new Bitmap(maxWidth, totalHeight);
            using var graphics = Graphics.FromImage(spriteSheet);
            graphics.Clear(Color.Transparent);

            var spriteSheetData = new SpriteSheetData
            {
                Meta = new MetaData
                {
                    Image = $"{name}.png",
                    Size = new SizeData { Width = maxWidth, Height = totalHeight }
                }
            };

            int currentY = 0;
            foreach (var kvp in images)
            {
                var key = kvp.Key;
                var image = kvp.Value;

                spriteSheetData.Frames[key] = new FrameData
                {
                    Frame = new RectData
                    {
                        X = 0,
                        Y = currentY,
                        Width = image.Width,
                        Height = image.Height
                    }
                };

                graphics.DrawImage(image, new Point(0, currentY));
                currentY += image.Height;
            }

            string imagePath = Path.Combine(outputDirectory, $"{name}.png");
            spriteSheet.Save(imagePath, ImageFormat.Png);

            return (imagePath, spriteSheetData);
        }
    }
}
