using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Habbo_Downloader.SWFCompiler.Mapper.Spritesheets
{
    public static class SpriteSheetMapper
    {
        // If disableCleanKey is false, duplicate prefixes are removed.
        // If true, the asset key is left intact.

        public static string CleanAssetName(string name, bool disableCleanKey = false)
        {
            string result = name;
            if (!disableCleanKey)
            {
                result = Regex.Replace(name, @"^([^_]+)_\1_", "$1_");
            }
            return result;
        }

        // Forces any occurrence of "cf_" or "cfc_" (at the start or following an underscore) to be uppercase.

        public static string ForceCFUpper(string name)
        {
            return Regex.Replace(name, @"(?<=^|_)(cfc_|cf_)", m => m.Value.ToUpperInvariant(), RegexOptions.IgnoreCase);
        }

        // Generates a sprite sheet from the provided images.
        // The canonicalMapping maps from short names (cleaned asset names) to full asset names (from CSV).

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(
            Dictionary<string, Bitmap> images,
            string outputDirectory,
            string name,
            Dictionary<string, string> canonicalMapping, // new parameter
            bool disableCleanKey = false, // control cleaning
            int numRows = 10,
            int maxWidth = 7500,
            int maxHeight = 12500)
        {
            if (images == null || images.Count == 0)
            {
                Console.WriteLine("⚠️ No images provided to generate sprite sheet.");
                return (null, null);
            }

            // Calculate how many images per row.
            int imagesCount = images.Count;
            int imagesPerRow = (int)Math.Ceiling((double)imagesCount / numRows);

            int maxRowWidth = 0;
            int maxRowHeight = 0;

            // Group images into rows.
            var imageGroups = images.Values
                .Select((img, index) => new { Image = img, Index = index })
                .GroupBy(x => x.Index / imagesPerRow)
                .ToList();

            // Determine maximum row dimensions.
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

            // Create the sprite sheet bitmap.
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
                        Format = "RGBA8888",
                        Converter = "All-in-1-download-tool"
                    },
                    Frames = new Dictionary<string, FrameData>()
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
                        // Clean the key as usual.
                        string shortKey = CleanAssetName(key, disableCleanKey: false);
                        // Look up the canonical mapping; if not found, use the cleaned key.
                        string finalKey = canonicalMapping.ContainsKey(shortKey)
                            ? canonicalMapping[shortKey]
                            : shortKey;
                         finalKey = ForceCFUpper(finalKey);

                        // Draw the image onto the sprite sheet.
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

                        spriteSheetData.Frames[finalKey] = frameData;

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
