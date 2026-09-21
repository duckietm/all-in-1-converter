using System;
using System.IO;

namespace Habbo_Downloader.Tools
{
    /// <summary>
    /// Global converter settings (e.g., spritesheet output format: WebP vs PNG).
    /// WebP Lossless preserves 100% alpha transparency and pixel fidelity while reducing file size by 25-40%.
    /// </summary>
    public static class ConverterSettings
    {
        private static string? _cachedFormat;

        public static string SpritesheetFormat
        {
            get
            {
                if (_cachedFormat != null) return _cachedFormat;

                try
                {
                    string configPath = Path.Combine(Environment.CurrentDirectory, "config.ini");
                    if (File.Exists(configPath))
                    {
                        foreach (var line in File.ReadAllLines(configPath))
                        {
                            var trimmed = line.Trim();
                            if (trimmed.StartsWith("spritesheet_format", StringComparison.OrdinalIgnoreCase))
                            {
                                var parts = trimmed.Split('=', 2);
                                if (parts.Length == 2)
                                {
                                    var val = parts[1].Trim().ToLowerInvariant();
                                    if (val == "png")
                                    {
                                        _cachedFormat = "png";
                                        return _cachedFormat;
                                    }
                                    if (val == "webp")
                                    {
                                        _cachedFormat = "webp";
                                        return _cachedFormat;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to default
                }

                _cachedFormat = "webp"; // Default to webp for lighter assets and faster loading
                return _cachedFormat;
            }
            set => _cachedFormat = value;
        }

        public static bool UseWebp => string.Equals(SpritesheetFormat, "webp", StringComparison.OrdinalIgnoreCase);

        public static string ImageExtension => UseWebp ? ".webp" : ".png";

        /// <summary>
        /// Produces the highest compression efficiency (Level 6) with 100% bit-perfect Lossless quality
        /// and preserved alpha transparency.
        /// </summary>
        public static SixLabors.ImageSharp.Formats.Webp.WebpEncoder OptimalWebpEncoder => new()
        {
            FileFormat = SixLabors.ImageSharp.Formats.Webp.WebpFileFormatType.Lossless,
            Quality = 100,
            Method = SixLabors.ImageSharp.Formats.Webp.WebpEncodingMethod.Level6,
            TransparentColorMode = SixLabors.ImageSharp.Formats.Webp.WebpTransparentColorMode.Preserve
        };
    }
}
