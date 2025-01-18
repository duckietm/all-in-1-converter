using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

public class NitroBundler
{
    private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

    // Add a file to the bundle
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
        Console.WriteLine($"Added file: {name}, Size: {data.Length} bytes");
    }

    // Generate the .nitro file buffer asynchronously
    public async Task<byte[]> ToBufferAsync()
    {
        using var memoryStream = new MemoryStream();
        using var binaryWriter = new BinaryWriter(memoryStream);

        // Write the file count
        binaryWriter.Write(ToBigEndian((short)_files.Count));
        Console.WriteLine($"Writing total file count: {_files.Count}");

        foreach (var file in _files)
        {
            string fileName = file.Key;
            byte[] fileData = file.Value;

            // Write the file name length and file name
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            binaryWriter.Write(ToBigEndian((short)fileNameBytes.Length));
            binaryWriter.Write(fileNameBytes);
            Console.WriteLine($"Writing file name: {fileName}, Length: {fileNameBytes.Length}");

            // Compress the file data
            byte[] compressed = Compress(fileData);
            Console.WriteLine($"Original Size: {fileData.Length}, Compressed Size: {compressed.Length}");

            // Write the compressed file length and data
            binaryWriter.Write(ToBigEndian(compressed.Length));
            binaryWriter.Write(compressed);
        }

        return memoryStream.ToArray();
    }

    // Compress the file data using SharpZipLib (ZLIB Compression)
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



    // Convert short to big-endian
    private static byte[] ToBigEndian(short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    // Convert int to big-endian
    private static byte[] ToBigEndian(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }
}
