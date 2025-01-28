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
    int maxRowWidth = 1024,
    int maxSheetHeight = 7000)
        {
            if (images == null || images.Count == 0) return (null, null);

            var spriteSheetData = new SpriteSheetData
            {
                Meta = new MetaData
                {
                    Image = $"{name}.png",
                    Size = new SizeData { Width = 0, Height = 0 } // Will be updated later
                }
            };

            int currentY = 0;
            int totalWidth = 0;
            int totalHeight = 0;

            var imageList = images.ToList();
            var spriteSheet = new Bitmap(maxRowWidth, maxSheetHeight);
            using var graphics = Graphics.FromImage(spriteSheet);
            graphics.Clear(Color.Transparent);

            for (int i = 0; i < imageList.Count;)
            {
                int currentX = 0;
                int rowHeight = 0;

                // Check if the current image is wider than 800 pixels
                if (imageList[i].Value.Width > 800)
                {
                    // Place the image on its own row
                    var image = imageList[i].Value;
                    var key = imageList[i].Key;

                    // Ensure the image fits within the maximum row width
                    if (image.Width > maxRowWidth)
                    {
                        throw new InvalidOperationException(
                            $"Image '{key}' width ({image.Width}) exceeds the maximum row width ({maxRowWidth})."
                        );
                    }

                    graphics.DrawImage(image, new Point(currentX, currentY));

                    var frameData = CreateFrameData(currentX, currentY, image);
                    spriteSheetData.Frames[RemoveNumericPrefix(key)] = frameData;

                    rowHeight = image.Height;
                    currentY += rowHeight;
                    totalWidth = Math.Max(totalWidth, image.Width);
                    totalHeight += rowHeight;

                    i++; // Move to the next image
                }
                else
                {
                    // Place smaller images on the same row until the row width exceeds maxRowWidth
                    while (i < imageList.Count && currentX + imageList[i].Value.Width <= maxRowWidth)
                    {
                        var image = imageList[i].Value;
                        var key = imageList[i].Key;

                        graphics.DrawImage(image, new Point(currentX, currentY));

                        var frameData = CreateFrameData(currentX, currentY, image);
                        spriteSheetData.Frames[RemoveNumericPrefix(key)] = frameData;

                        currentX += image.Width;
                        rowHeight = Math.Max(rowHeight, image.Height);
                        i++; // Move to the next image
                    }

                    currentY += rowHeight;
                    totalWidth = Math.Max(totalWidth, currentX);
                    totalHeight += rowHeight;
                }

                // Check if the total height exceeds the maximum allowed height
                if (totalHeight > maxSheetHeight)
                {
                    throw new InvalidOperationException(
                        $"Sprite sheet height ({totalHeight}) exceeds the maximum allowed height ({maxSheetHeight}). " +
                        "Reduce the number of images or adjust the maximum dimensions."
                    );
                }
            }

            // Update the sprite sheet dimensions
            spriteSheetData.Meta.Size = new SizeData { Width = totalWidth, Height = totalHeight };

            // Crop the sprite sheet to the actual dimensions
            var croppedSpriteSheet = new Bitmap(totalWidth, totalHeight);
            using (var croppedGraphics = Graphics.FromImage(croppedSpriteSheet))
            {
                croppedGraphics.DrawImage(spriteSheet, new Rectangle(0, 0, totalWidth, totalHeight));
            }

            string imagePath = Path.Combine(outputDirectory, $"{name}.png");
            croppedSpriteSheet.Save(imagePath, ImageFormat.Png);

            return (imagePath, spriteSheetData);
        }

        private static FrameData CreateFrameData(int x, int y, Bitmap image)
        {
            return new FrameData
            {
                Frame = new RectData
                {
                    X = x,
                    Y = y,
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
        }
    }
}