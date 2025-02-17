using SkiaSharp;
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
        /// <summary>
        /// If disableCleanKey is false, duplicate prefixes are removed.
        /// If true, the asset key is left intact.
        /// </summary>
        public static string CleanAssetName(string name, bool disableCleanKey = false)
        {
            string lowerName = name.ToLowerInvariant();
            if (disableCleanKey)
            {
                return lowerName;
            }
            return Regex.Replace(lowerName, @"^([^_]+)_\1_", "$1_");
        }

        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(Dictionary<string, SKBitmap>
            images,
            string outputDirectory,
            string name,
            Dictionary<string, string> canonicalMapping,
            bool disableCleanKey = false,
            int numRows = 10,
            int maxWidth = 7500,
            int maxHeight = 12500)
        {
            if (images == null || images.Count == 0)
            {
                Console.WriteLine("⚠️ No images provided to generate sprite sheet.");
                return (null, null);
            }

            int imagesCount = images.Count;
            int imagesPerRow = (int)Math.Ceiling((double)imagesCount / numRows);

            int maxRowWidth = 0;
            int maxRowHeight = 0;

            var imageGroups = images.Values
                .Select((img, index) => new { Image = img, Index = index })
                .GroupBy(x => x.Index / imagesPerRow)
                .ToList();

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
                throw new InvalidOperationException($"Sprite sheet dimensions ({totalWidth}x{totalHeight}) exceed maximum allowed.");
            }

            using var spriteSheet = new SKBitmap(totalWidth, totalHeight);
            using var canvas = new SKCanvas(spriteSheet);
            canvas.Clear(SKColors.Transparent);

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
                    string shortKey = CleanAssetName(key, disableCleanKey: false);
                    string finalKey = canonicalMapping.ContainsKey(shortKey) ? canonicalMapping[shortKey] : CleanAssetName(key, disableCleanKey);

                    canvas.DrawBitmap(image, new SKPoint(currentX, currentY));

                    var frameData = new FrameData
                    {
                        Frame = new RectData { X = currentX, Y = currentY, Width = image.Width, Height = image.Height },
                        Rotated = false,
                        Trimmed = false,
                        SpriteSourceSize = new RectData { X = 0, Y = 0, Width = image.Width, Height = image.Height },
                        SourceSize = new SizeData { Width = image.Width, Height = image.Height },
                        Pivot = new PivotData { X = 0.5f, Y = 0.5f }
                    };

                    spriteSheetData.Frames[finalKey] = frameData;

                    currentX += image.Width;
                    rowHeight = Math.Max(rowHeight, image.Height);
                    imageIndex++;
                }
                currentY += rowHeight;
            }

            string imagePath = Path.Combine(outputDirectory, $"{name}.png");
            using var imageFileStream = File.OpenWrite(imagePath);
            spriteSheet.Encode(imageFileStream, SKEncodedImageFormat.Png, 100);

            return (imagePath, spriteSheetData);
        }
    }
}