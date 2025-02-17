using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Habbo_Downloader.SWFCompiler.Mapper.Spritesheets
{
    public static class SpritesheetClothesMapper
    {
        public static (string ImagePath, SpriteSheetData SpriteData) GenerateSpriteSheet(
            Dictionary<string, SKBitmap> images,
            string outputDirectory,
            string name,
            int numRows = 10,
            int maxWidth = 10240,
            int maxHeight = 7000)
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
                throw new InvalidOperationException(
                    $"Sprite sheet dimensions ({totalWidth}x{totalHeight}) exceed maximum allowed ({maxWidth}x{maxHeight}). " +
                    "Reduce the number of images or adjust the maximum dimensions.");
            }

            // Create the sprite sheet using SkiaSharp
            using var spriteSheet = new SKBitmap(totalWidth, totalHeight);
            using var canvas = new SKCanvas(spriteSheet);
            canvas.Clear(new SKColor(0, 0, 0, 0)); // Transparent background

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

            foreach (var group in imageGroups)
            {
                int currentX = 0;
                int rowHeight = 0;

                foreach (var imageItem in group)
                {
                    var image = imageItem.Image;

                    var key = images.Keys.ElementAt(imageIndex);

                    // 🔥 **Fix: Get the correct name from Debug.xml mapping**
                    var originalName = ClothesAssetsMapper.LatestImageMapping.ContainsKey(key)
                        ? ClothesAssetsMapper.LatestImageMapping[key]
                        : key;

                    // Draw the image on the canvas
                    canvas.DrawBitmap(image, new SKPoint(currentX, currentY));

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

                    // ✅ **Store with correct naming from Debug.xml**
                    spriteSheetData.Frames[originalName] = frameData;

                    currentX += image.Width;
                    rowHeight = Math.Max(rowHeight, image.Height);
                    imageIndex++;
                }
                currentY += rowHeight;
            }

            string imagePath = Path.Combine(outputDirectory, $"{name}.png");

            // Save the image using SkiaSharp
            using (var image = SKImage.FromBitmap(spriteSheet))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(imagePath))
            {
                data.SaveTo(stream);
            }

            return (imagePath, spriteSheetData);
        }
    }
}
