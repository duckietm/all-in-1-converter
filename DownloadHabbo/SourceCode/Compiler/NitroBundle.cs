using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip.Compression;

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
            Console.WriteLine($"File Name Length: {fileNameLength}");

            if (fileNameLength < 0 || fileNameLength > binaryReader.Remaining())
            {
                throw new InvalidDataException("Invalid file name length.");
            }

            byte[] fileNameBytes = binaryReader.ReadBytes(fileNameLength);
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            Console.WriteLine($"File Name: {fileName}");

            int fileLength = binaryReader.ReadInt();
            Console.WriteLine($"File Length: {fileLength}");

            if (fileLength < 0 || fileLength > binaryReader.Remaining())
            {
                throw new InvalidDataException("Invalid file length.");
            }

            byte[] buffer = binaryReader.ReadBytes(fileLength);
            Console.WriteLine($"Read {buffer.Length} bytes for file: {fileName}");

            if (fileLength > 0) // Skip if file is empty
            {
                try
                {
                    byte[] decompressed = Inflate(buffer); // Use custom Inflate method

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
        var inflater = new Inflater();
        inflater.SetInput(data);

        using var outputStream = new MemoryStream();
        var buffer = new byte[4096];
        while (!inflater.IsFinished)
        {
            int bytesRead = inflater.Inflate(buffer);
            outputStream.Write(buffer, 0, bytesRead);
        }

        return outputStream.ToArray();
    }

    public object JsonFile => _jsonFile;

    public string BaseTexture => _baseTexture;
}