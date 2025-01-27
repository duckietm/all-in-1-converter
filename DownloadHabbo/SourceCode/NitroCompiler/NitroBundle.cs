using System.Text;
using System.Text.Json;
using System.IO.Compression;

public class NitroBundle
{
    private static readonly Encoding TextDecoder = Encoding.UTF8;

    private object _jsonFile;
    private string _baseTexture;

    public NitroBundle(byte[] arrayBuffer)
    {
        Parse(arrayBuffer);
    }

    private static string ArrayBufferToBase64(byte[] buffer)
    {
        return Convert.ToBase64String(buffer);
    }

    public void Parse(byte[] arrayBuffer)
    {
        var binaryReader = new BinaryReader(arrayBuffer);

        int fileCount = binaryReader.ReadShort();
        Console.WriteLine($"Total files to extract: {fileCount}");

        while (fileCount > 0)
        {
            int fileNameLength = binaryReader.ReadShort();
            string fileName = Encoding.UTF8.GetString(binaryReader.ReadBytes(fileNameLength));

            int fileLength = binaryReader.ReadInt();
            byte[] buffer = binaryReader.ReadBytes(fileLength);

            if (fileLength > 0)
            {
                try
                {
                    byte[] decompressed = Inflate(buffer);

                    if (fileName.EndsWith(".json"))
                    {
                        _jsonFile = JsonSerializer.Deserialize<object>(Encoding.UTF8.GetString(decompressed));
                    }
                    else
                    {
                        _baseTexture = ArrayBufferToBase64(decompressed);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process file {fileName}: {ex.Message}");
                }
            }

            fileCount--;
        }
    }

    private static byte[] Inflate(byte[] data)
    {
        try
        {
            using var inputStream = new MemoryStream(data);
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(outputStream);
            }
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Decompression failed: {ex.Message}");
        }
    }

    public object JsonFile => _jsonFile;
    public string BaseTexture => _baseTexture;
}
