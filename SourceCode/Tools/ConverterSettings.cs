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

        /// <summary>Forgets the cached values, so an edited config.ini is read again.</summary>
        public static void Reload()
        {
            _cachedFormat = null;
            _cachedDownloadFormat = null;
        }

        public static bool UseWebp => string.Equals(SpritesheetFormat, "webp", StringComparison.OrdinalIgnoreCase);

        private static string? _cachedDownloadFormat;

        /// <summary>
        /// config.ini download_format: swf, hab or both, for furniture, clothes, effects and pets.
        /// Older configs: furniture_download_format, else .swf plus the .hab when download_hab is on.
        /// </summary>
        public static string DownloadFormat
        {
            get
            {
                if (_cachedDownloadFormat != null) return _cachedDownloadFormat;

                string? value = (ReadSetting("download_format") ?? ReadSetting("furniture_download_format"))?.ToLowerInvariant();
                if (value is not ("swf" or "hab" or "both"))
                {
                    string? legacy = ReadSetting("download_hab")?.ToLowerInvariant();
                    value = legacy is "false" or "0" ? "swf" : "both";
                }

                _cachedDownloadFormat = value;
                return value;
            }
            set => _cachedDownloadFormat = value;
        }

        public static bool DownloadSwf => DownloadFormat is "swf" or "both";

        public static bool DownloadHab => DownloadFormat is "hab" or "both";

        /// <summary>The value of a config.ini line "key=value", or null.</summary>
        private static string? ReadSetting(string key)
        {
            try
            {
                string configPath = Path.Combine(Environment.CurrentDirectory, "config.ini");
                if (!File.Exists(configPath)) return null;

                foreach (var line in File.ReadAllLines(configPath))
                {
                    var parts = line.Trim().Split('=', 2);
                    if (parts.Length == 2 && parts[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        return parts[1].Trim();
                }
            }
            catch
            {
                // Fallback to default
            }
            return null;
        }

        public static string ImageExtension => UseWebp ? ".webp" : ".png";

        /// <summary>
        /// The largest sheet the lossless WebP encoder can take. It builds its backward
        /// reference table as one contiguous PixOrCopy[] - 8 bytes per pixel - and ImageSharp
        /// refuses any single buffer over 1 GiB. That cap (MemoryAllocator's internal
        /// SingleBufferAllocationLimitBytes) cannot be raised: MemoryAllocator.Create only ever
        /// Math.Min's it downwards, so a bigger allocator does not help.
        /// </summary>
        public const long WebpLosslessPixelLimit = (1L << 30) / 8; // 134,217,728 pixels

        public static bool FitsWebpLossless(int width, int height) =>
                (long)width * height <= WebpLosslessPixelLimit;

        /// <summary>
        /// The extension a sheet of this size can actually be written with: the configured
        /// format, except that a WebP sheet too large for the lossless encoder falls back to
        /// PNG instead of failing the whole conversion with an allocation error.
        /// </summary>
        public static string ResolveSheetExtension(int width, int height, string sheetName)
        {
            if (!UseWebp)
            {
                return ".png";
            }

            if (FitsWebpLossless(width, height))
            {
                return ".webp";
            }

            Console.WriteLine(
                    $"\u2139\uFE0F {sheetName}: {width}x{height} is {(long)width * height:N0} pixels, past the "
                    + $"{WebpLosslessPixelLimit:N0} the lossless WebP encoder can hold in one buffer "
                    + "- writing this sheet as PNG.");
            return ".png";
        }

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
