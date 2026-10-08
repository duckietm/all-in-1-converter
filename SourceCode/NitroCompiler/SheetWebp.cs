using Habbo_Downloader.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// PNG sheet -> lossless WebP. The WebP is decoded again and must match every pixel; when it does not,
/// or the sheet is too large for the WebP encoder, the PNG is kept.
/// </summary>
public static class SheetWebp
{
    public static bool TryConvert(byte[] png, string name, out byte[] webp, out string result)
    {
        webp = png;
        using var image = Image.Load<Rgba32>(png);

        if (ConverterSettings.ResolveSheetExtension(image.Width, image.Height, name) != ".webp")
        {
            result = "kept PNG (too large for lossless WebP)";
            return false;
        }

        byte[] encoded;
        using (var output = new MemoryStream())
        {
            image.Save(output, ConverterSettings.OptimalWebpEncoder);
            encoded = output.ToArray();
        }

        using (var check = Image.Load<Rgba32>(encoded))
        {
            if (!SamePixels(image, check))
            {
                result = "kept PNG (WebP would change pixels)";
                return false;
            }
        }

        webp = encoded;
        result = $"PNG {png.Length:N0} -> WebP {encoded.Length:N0} bytes";
        return true;
    }

    /// <summary>A bundle entry with its PNG sheet as WebP (renamed to .webp); other entries are returned as they are.</summary>
    public static KeyValuePair<string, byte[]> ToWebpEntry(KeyValuePair<string, byte[]> file, string name)
    {
        if (!file.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return file;

        return TryConvert(file.Value, name, out byte[] webp, out _)
            ? new KeyValuePair<string, byte[]>(Path.ChangeExtension(file.Key, ".webp"), webp)
            : file;
    }

    private static bool SamePixels(Image<Rgba32> expected, Image<Rgba32> actual)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height) return false;

        var a = new Rgba32[expected.Width * expected.Height];
        var b = new Rgba32[a.Length];
        expected.CopyPixelDataTo(a);
        actual.CopyPixelDataTo(b);
        return a.AsSpan().SequenceEqual(b);
    }
}

/// <summary>
/// Swaps the PNG sheet of a Habbo .hab for a lossless WebP, like the sheets our own converter writes.
/// The json is kept byte for byte; when the WebP would not be identical the .hab is kept as it was.
/// </summary>
public static class HabSheetConverter
{
    public static byte[] ToWebp(byte[] hab, out string result)
    {
        var (name, files) = HabBundle.Read(hab);

        int[] pngs = files.Select((file, index) => (file, index))
            .Where(item => item.file.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.index)
            .ToArray();

        if (pngs.Length != 1 || !files.Any(file => file.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            result = "kept as is (not one json + one png sheet)";
            return hab;
        }

        var (pngName, png) = files[pngs[0]];
        if (!SheetWebp.TryConvert(png, name, out byte[] webp, out result)) return hab;

        files[pngs[0]] = new KeyValuePair<string, byte[]>(Path.ChangeExtension(pngName, ".webp"), webp);
        return HabBundle.Write(name, files);
    }
}
