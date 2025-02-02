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
        // Dynamic cleaning function using a regex.
        // It removes a duplicated token at the beginning.
        // For example, "pura_mdl1_pura_mdl1_64_d_0_0" becomes "pura_mdl1_64_d_0_0".
        public static string CleanAssetName(string name)
        {
            return Regex.Replace(name, @"^([^_]+)_\1_", "$1_");
        }

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(
            Dictionary<string, Bitmap> images,
            string outputDirectory,
            string name,
            int numRows = 10,
            int maxWidth = 10240,
            int maxHeight = 7000)
        {
            if (images == null || images.Count == 0)
                return (null, null);

            // Calculate how many images go per row.
            int imagesCount = images.Count;
            int imagesPerRow = (int)Math.Ceiling((double)imagesCount / numRows);

            int maxRowWidth = 0;
            int maxRowHeight = 0;

            // Group images into rows.
            var imageGroups = images.Values
                .Select((img, index) => new { Image = img, Index = index })
                .GroupBy(x => x.Index / imagesPerRow)
                .ToList();

            // Calculate the maximum row width and height.
            foreach (var group in imageGroups)
            {
                int rowWidth = group.Sum(x => x.Image.Width);
                int rowHeight = group.Max(x => x.Image.Height);
                maxRowWidth = Math.Max(maxRowWidth, rowWidth);
                maxRowHeight = Math.Max(maxRowHeight, rowHeight);
            }

            int totalWidth = maxRowWidth;
            int totalHeight = imageGroups.Count * maxRowHeight;

            if (totalWidth > maxWidth || totalHeight > maxHeight)
            {
                throw new InvalidOperationException(
                    $"Sprite sheet dimensions ({totalWidth}x{totalHeight}) exceed maximum allowed ({maxWidth}x{maxHeight}). " +
                    "Reduce the number of images or adjust the maximum dimensions.");
            }

            // Create the sprite sheet.
            var spriteSheet = new Bitmap(totalWidth, totalHeight);
            using (var graphics = Graphics.FromImage(spriteSheet))
            {
                graphics.Clear(Color.Transparent);

                var spriteSheetData = new SpriteSheetData
                {
                    Meta = new MetaData
                    {
                        Image = $"{name}.png",
                        Size = new SizeData { Width = totalWidth, Height = totalHeight },
                        Scale = 1.0f,
                        Format = "RGBA8888"
                    },
                    Frames = new Dictionary<string, FrameData>()
                };

                int currentY = 0;
                int imageIndex = 0;
                // For each row (group) of images.
                foreach (var group in imageGroups)
                {
                    int currentX = 0;
                    int rowHeight = 0;

                    foreach (var imageItem in group)
                    {
                        var image = imageItem.Image;
                        // Get the original key from the dictionary and clean it.
                        var key = images.Keys.ElementAt(imageIndex);
                        var cleanedKey = CleanAssetName(key.ToLowerInvariant());

                        // Draw the image.
                        graphics.DrawImage(image, new Point(currentX, currentY));

                        // Create frame data.
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
}
