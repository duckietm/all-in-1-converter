using System.IO.Compression;
using System.Text;

public class NitroBundler
{
    private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

    public void AddFile(string name, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(name));
        }

        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("File data cannot be null or empty.", nameof(data));
        }

        _files[name] = data;
    }
    public async Task<byte[]> ToBufferAsync()
    {
        using var memoryStream = new MemoryStream();
        using var binaryWriter = new BinaryWriter(memoryStream);

        binaryWriter.Write(ToBigEndian((short)_files.Count));

        foreach (var file in _files)
        {
            string fileName = file.Key;
            byte[] fileData = file.Value;
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            binaryWriter.Write(ToBigEndian((short)fileNameBytes.Length));
            binaryWriter.Write(fileNameBytes);
            byte[] compressed = Compress(fileData);

            binaryWriter.Write(ToBigEndian(compressed.Length));
            binaryWriter.Write(compressed);
        }

        return memoryStream.ToArray();
    }
    private static byte[] Compress(byte[] data)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Compression failed: {ex.Message}");
            throw;
        }
    }
    private static byte[] ToBigEndian(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }
    private static byte[] ToBigEndian(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }
}
