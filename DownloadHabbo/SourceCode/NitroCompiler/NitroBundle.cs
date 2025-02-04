using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

public class NitroBundle
{
    private static readonly Encoding TextDecoder = Encoding.UTF8;
    private readonly string _extractedBasePath = Path.Combine(Directory.GetCurrentDirectory(), "NitroCompiler", "extracted", "furni");

    private object? _jsonFile;
    private string? _baseTexture;

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
        using MemoryStream memoryStream = new MemoryStream(arrayBuffer);
        using System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(memoryStream);

        // Read file count as big-endian
        short fileCount = BitConverter.ToInt16(binaryReader.ReadBytes(2).Reverse().ToArray(), 0);

        while (fileCount > 0)
        {
            // Read filename length as big-endian
            short fileNameLength = BitConverter.ToInt16(binaryReader.ReadBytes(2).Reverse().ToArray(), 0);
            if (fileNameLength <= 0 || fileNameLength > memoryStream.Length - memoryStream.Position)
            {
                Console.WriteLine("❌ Error: Corrupted fileNameLength detected!");
                return;
            }

            // Read filename
            string fileName = Encoding.UTF8.GetString(binaryReader.ReadBytes(fileNameLength));

            // Read file length as big-endian
            int fileLength = BitConverter.ToInt32(binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
            if (fileLength < 0 || fileLength > memoryStream.Length - memoryStream.Position)
            {
                Console.WriteLine($"❌ Error: Invalid file length ({fileLength}) for {fileName}");
                return;
            }

            // Read file data
            byte[] buffer = binaryReader.ReadBytes(fileLength);

            if (fileLength > 0)
            {
                try
                {
                    byte[] decompressed = DetectAndDecompress(buffer);

                    if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        _jsonFile = JsonSerializer.Deserialize<object>(Encoding.UTF8.GetString(decompressed));
                    }
                    else if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        _baseTexture = ArrayBufferToBase64(decompressed); //Convert PNG to Base64
                    }

                    // Save the extracted file in the correct directory
                    SaveExtractedFile(fileName, decompressed);

                    Console.WriteLine($"✅ Extracted: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to process file {fileName}: {ex.Message}");
                }
            }

            fileCount--;
        }

        // Debug: Check if Base64 image is correctly stored
        if (string.IsNullOrEmpty(_baseTexture))
        {
            Console.WriteLine("⚠️ Warning: Base64 image data is null or empty.");
        }
    }

    private void SaveExtractedFile(string fileName, byte[] data)
    {
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string fileExtension = Path.GetExtension(fileName);

        string saveDirectory = Path.Combine(_extractedBasePath, baseName);
        Directory.CreateDirectory(saveDirectory);

        string savePath = Path.Combine(saveDirectory, fileName);

        File.WriteAllBytes(savePath, data);
    }

    private static byte[] DetectAndDecompress(byte[] data)
    {
        // Check if file is using the "extra header" Zlib format
        if (data.Length > 2 && data[0] == 0x78 && data[1] == 0x9C)
        {
            return InflateCustomZlib(data); // Remove first 2 bytes and decompress
        }
        else
        {
            return TryDecompress(data); // Use normal Zlib or GZip decompression
        }
    }

    private static byte[] TryDecompress(byte[] data)
    {
        try
        {
            return InflateZlib(data);  // Try standard Zlib first
        }
        catch
        {
            try
            {
                return InflateGZip(data);  // If Zlib fails, try GZip
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"❌ Decompression failed using both Zlib and GZip: {ex.Message}");
            }
        }
    }

    private static byte[] InflateCustomZlib(byte[] data)
    {
        try
        {
            // Remove first 2 bytes (78 9C) which are Zlib headers
            byte[] rawDeflateData = data.Skip(2).ToArray();

            using MemoryStream inputStream = new MemoryStream(rawDeflateData);
            using MemoryStream outputStream = new MemoryStream();
            using (DeflateStream deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(outputStream);
            }

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"❌ Custom Zlib decompression failed: {ex.Message}");
        }
    }

    private static byte[] InflateGZip(byte[] data)
    {
        using MemoryStream inputStream = new MemoryStream(data);
        using MemoryStream outputStream = new MemoryStream();
        using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            gzipStream.CopyTo(outputStream);
        }
        return outputStream.ToArray();
    }

    private static byte[] InflateZlib(byte[] data)
    {
        using MemoryStream inputStream = new MemoryStream(data);
        using MemoryStream outputStream = new MemoryStream();
        using (DeflateStream zlibStream = new DeflateStream(inputStream, CompressionMode.Decompress))
        {
            zlibStream.CopyTo(outputStream);
        }
        return outputStream.ToArray();
    }

    public object? JsonFile => _jsonFile;
    public string? BaseTexture => _baseTexture;
}
