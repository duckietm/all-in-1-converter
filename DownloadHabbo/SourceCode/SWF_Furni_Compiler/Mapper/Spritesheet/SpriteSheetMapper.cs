using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Habbo_Downloader.SWFCompiler.Mapper.Spritesheets
{
    public static class SpriteSheetMapper
    {
        public class SpriteSheetData
        {
            [JsonPropertyName("frames")]
            public Dictionary<string, FrameData> Frames { get; set; } = new();

            [JsonPropertyName("meta")]
            public MetaData Meta { get; set; }
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

        public class PivotData
        {
            [JsonPropertyName("x")]
            public float X { get; set; } = 0.5f; // Default pivot (center)

            [JsonPropertyName("y")]
            public float Y { get; set; } = 0.5f; // Default pivot (center)
        }

        public class RectDataConverter : JsonConverter<RectData>
        {
            public override RectData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, RectData value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                // Always write x and y, even if they are 0
                writer.WriteNumber("x", value.X);
                writer.WriteNumber("y", value.Y);

                // Conditionally write width and height based on DefaultIgnoreCondition
                if (value.Width != 0 || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingDefault)
                {
                    writer.WriteNumber("w", value.Width);
                }

                if (value.Height != 0 || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingDefault)
                {
                    writer.WriteNumber("h", value.Height);
                }

                writer.WriteEndObject();
            }
        }

        private static string RemoveNumericPrefix(string name)
        {
            return Regex.Replace(name, @"^\d+_", "");
        }

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(
            Dictionary<string, Bitmap> images,
            string outputDirectory,
            string name,
            int numRows = 10,
            int maxWidth = 10240,
            int maxHeight = 7000)
        {
            if (images == null || images.Count == 0) return (null, null);

            int imagesPerRow = (int)Math.Ceiling((double)images.Count / numRows);

            int maxRowWidth = 0;
            int maxRowHeight = 0;

            var imageGroups = images.Values
                .Select((image, index) => new { Image = image, Index = index })
                .GroupBy(x => x.Index / imagesPerRow)
                .ToList();

            foreach (var group in imageGroups)
            {
                int rowWidth = group.Sum(image => image.Image.Width);
                int rowHeight = group.Max(image => image.Image.Height);

                if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
                if (rowHeight > maxRowHeight) maxRowHeight = rowHeight;
            }

            int totalWidth = maxRowWidth;
            int totalHeight = maxRowHeight * numRows;

            if (totalWidth > maxWidth || totalHeight > maxHeight)
            {
                throw new InvalidOperationException(
                    $"Sprite sheet dimensions ({totalWidth}x{totalHeight}) exceed the maximum allowed dimensions ({maxWidth}x{maxHeight}). " +
                    "Reduce the number of images or adjust the maximum dimensions."
                );
            }

            // Create the sprite sheet
            var spriteSheet = new Bitmap(totalWidth, totalHeight);
            using var graphics = Graphics.FromImage(spriteSheet);
            graphics.Clear(Color.Transparent);

            var spriteSheetData = new SpriteSheetData
            {
                Meta = new MetaData
                {
                    Image = $"{name}.png",
                    Size = new SizeData { Width = totalWidth, Height = totalHeight }
                }
            };

            int currentY = 0;
            int imageIndex = 0;

            foreach (var group in imageGroups)
            {
                int currentX = 0;
                int rowHeight = 0;

                foreach (var imageItem in group)
                {
                    var image = imageItem.Image;
                    var key = images.Keys.ElementAt(imageIndex);

                    var cleanedKey = RemoveNumericPrefix(key);

                    graphics.DrawImage(image, new Point(currentX, currentY));

                    var frameData = new FrameData
                    {
                        Frame = new RectData
                        {
                            X = currentX,
                            Y = currentY,
                            Width = image.Width,
                            Height = image.Height
                        },
                        Rotated = false,
                        Trimmed = false,
                        SpriteSourceSize = new RectData
                        {
                            X = 0,
                            Y = 0,
                            Width = image.Width,
                            Height = image.Height
                        },
                        SourceSize = new SizeData
                        {
                            Width = image.Width,
                            Height = image.Height
                        },
                        Pivot = new PivotData
                        {
                            X = 0.5f,
                            Y = 0.5f
                        }
                    };

                    spriteSheetData.Frames[cleanedKey] = frameData;

                    currentX += image.Width;
                    rowHeight = Math.Max(rowHeight, image.Height);
                    imageIndex++;
                }
                currentY += rowHeight;
            }

            string imagePath = Path.Combine(outputDirectory, $"{name}.png");
            spriteSheet.Save(imagePath, ImageFormat.Png);

            return (imagePath, spriteSheetData);
        }
    }
}