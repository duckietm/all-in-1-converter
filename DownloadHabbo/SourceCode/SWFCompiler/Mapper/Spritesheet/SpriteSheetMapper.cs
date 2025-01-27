using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        // Custom JSON converter for RectData
        public class RectDataConverter : JsonConverter<RectData>
        {
            public override RectData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Implement deserialization logic if needed
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

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(
            Dictionary<string, Bitmap> images,
            string outputDirectory,
            string name,
            int numRows = 10,
            int maxWidth = 10240,
            int maxHeight = 7000)
        {
            if (images == null || images.Count == 0) return (null, null);

            // Calculate the number of images per row
            int imagesPerRow = (int)Math.Ceiling((double)images.Count / numRows);

            // Calculate the maximum width and height for each row
            int maxRowWidth = 0;
            int maxRowHeight = 0;

            // Group images into rows
            var imageGroups = images.Values
                .Select((image, index) => new { Image = image, Index = index })
                .GroupBy(x => x.Index / imagesPerRow)
                .ToList();

            // Calculate the total width and height of the sprite sheet
            foreach (var group in imageGroups)
            {
                int rowWidth = group.Sum(image => image.Image.Width);
                int rowHeight = group.Max(image => image.Image.Height);

                if (rowWidth > maxRowWidth) maxRowWidth = rowWidth;
                if (rowHeight > maxRowHeight) maxRowHeight = rowHeight;
            }

            int totalWidth = maxRowWidth;
            int totalHeight = maxRowHeight * numRows;

            // Check if the sprite sheet exceeds the maximum dimensions
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

                    // Draw the image on the sprite sheet
                    graphics.DrawImage(image, new Point(currentX, currentY));

                    // Update the frame data for the sprite sheet
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

                    spriteSheetData.Frames[key] = frameData;

                    // Update the current position
                    currentX += image.Width;
                    rowHeight = Math.Max(rowHeight, image.Height);
                    imageIndex++;
                }

                // Move to the next row
                currentY += rowHeight;
            }

            // Save the sprite sheet
            string imagePath = Path.Combine(outputDirectory, $"{name}.png");
            spriteSheet.Save(imagePath, ImageFormat.Png);

            return (imagePath, spriteSheetData);
        }
    }
}