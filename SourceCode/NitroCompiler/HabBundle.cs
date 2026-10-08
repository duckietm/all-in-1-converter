using System.IO.Compression;
using System.Text.Json;

/// <summary>
/// Habbo's .hab bundle, the HTML5 client's replacement for .swf (and ours for .nitro):
/// "HAB\0", u16 version, u16 flags, u32 index length, u32 raw index length, u32 data length (all little-endian),
/// then a zlib JSON index { format: "hab", version, name, entries: [...] } and the data section.
/// Entry offsets count from the start of the data section.
/// </summary>
public static class HabBundle
{
    public const int HeaderLength = 20;
    private const ushort FormatVersion = 1;
    /// <summary>Habbo's own furni bundles carry flags 1.</summary>
    private const ushort Flags = 1;
    private static readonly byte[] Magic = { (byte)'H', (byte)'A', (byte)'B', 0 };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record HabIndex(string Format, int Version, string Name, List<HabIndexEntry> Entries);

    private sealed record HabIndexEntry(string Name, string? MimeType, long Offset, long StoredLength, long? OriginalLength, string? Compression);

    public static bool IsHab(byte[] data) =>
        data != null && data.Length >= HeaderLength && data.AsSpan(0, Magic.Length).SequenceEqual(Magic);

    /// <summary>
    /// Packs the files (name, bytes) into one .hab. Like Habbo's own bundles, an entry is zlib-compressed
    /// only when that makes it smaller; an already compressed image is stored as is ("none").
    /// </summary>
    public static byte[] Write(string name, IEnumerable<KeyValuePair<string, byte[]>> files)
    {
        using var data = new MemoryStream();
        var entries = new List<HabIndexEntry>();

        foreach (var (fileName, bytes) in files)
        {
            byte[] deflated = Deflate(bytes);
            bool compress = deflated.Length < bytes.Length;
            byte[] stored = compress ? deflated : bytes;
            entries.Add(new HabIndexEntry(fileName, MimeTypeOf(fileName), data.Length, stored.Length, bytes.Length, compress ? "deflate" : "none"));
            data.Write(stored);
        }

        byte[] rawIndex = JsonSerializer.SerializeToUtf8Bytes(new HabIndex("hab", FormatVersion, name, entries), JsonOptions);
        byte[] index = Deflate(rawIndex);

        using var output = new MemoryStream(HeaderLength + index.Length + (int)data.Length);
        using (var writer = new BinaryWriter(output, System.Text.Encoding.UTF8, true))
        {
            // BinaryWriter is always little-endian.
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(Flags);
            writer.Write((uint)index.Length);
            writer.Write((uint)rawIndex.Length);
            writer.Write((uint)data.Length);
            writer.Write(index);
            writer.Write(data.ToArray());
        }

        return output.ToArray();
    }

    /// <summary>Reads every entry; all lengths are checked, so a damaged file throws instead of giving half a bundle.</summary>
    public static (string Name, List<KeyValuePair<string, byte[]>> Files) Read(byte[] buffer)
    {
        if (!IsHab(buffer)) throw new InvalidDataException("Not a .hab bundle (missing HAB header).");

        long indexLength = BitConverter.ToUInt32(buffer, 8);
        long rawIndexLength = BitConverter.ToUInt32(buffer, 12);
        long dataLength = BitConverter.ToUInt32(buffer, 16);
        long dataStart = HeaderLength + indexLength;

        if (dataStart + dataLength > buffer.Length) throw new InvalidDataException(".hab bundle is shorter than its header says.");

        byte[] rawIndex = Inflate(buffer, HeaderLength, (int)indexLength);
        if (rawIndexLength != 0 && rawIndex.Length != rawIndexLength) throw new InvalidDataException(".hab index has the wrong length.");

        HabIndex? index = JsonSerializer.Deserialize<HabIndex>(rawIndex, JsonOptions);
        if (index == null || index.Format != "hab" || index.Entries == null) throw new InvalidDataException(".hab index is not a \"hab\" index.");

        var files = new List<KeyValuePair<string, byte[]>>();
        foreach (var entry in index.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) || entry.Offset < 0 || entry.StoredLength < 0 || entry.Offset + entry.StoredLength > dataLength)
                throw new InvalidDataException($".hab entry \"{entry.Name}\" lies outside the data section.");

            int start = (int)(dataStart + entry.Offset);
            byte[] bytes = entry.Compression == "deflate"
                ? Inflate(buffer, start, (int)entry.StoredLength)
                : buffer.AsSpan(start, (int)entry.StoredLength).ToArray();

            if (entry.OriginalLength.HasValue && bytes.Length != entry.OriginalLength.Value)
                throw new InvalidDataException($".hab entry \"{entry.Name}\" has the wrong length.");

            files.Add(new KeyValuePair<string, byte[]>(entry.Name, bytes));
        }

        return (index.Name ?? "", files);
    }

    public static string MimeTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".json" => "application/json",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".xml" => "application/xml",
        _ => "text/plain"
    };

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, true))
        {
            zlib.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    /// <summary>Habbo stores zlib streams; a raw deflate stream is accepted too.</summary>
    private static byte[] Inflate(byte[] buffer, int offset, int length)
    {
        try
        {
            return Inflate(new ZLibStream(new MemoryStream(buffer, offset, length), CompressionMode.Decompress));
        }
        catch (InvalidDataException)
        {
            return Inflate(new DeflateStream(new MemoryStream(buffer, offset, length), CompressionMode.Decompress));
        }
    }

    private static byte[] Inflate(Stream stream)
    {
        using (stream)
        using (var output = new MemoryStream())
        {
            stream.CopyTo(output);
            return output.ToArray();
        }
    }
}
