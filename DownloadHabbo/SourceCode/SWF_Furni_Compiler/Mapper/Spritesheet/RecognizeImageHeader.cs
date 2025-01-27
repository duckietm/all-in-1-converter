using System;
using System.IO;
using System.Linq;

public static class ImageHeaderRecognizer
{
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] GifMagic = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8 };
    public static string RecognizeImageHeader(string filePath)
    {
        try
        {
            // Read the first 8 bytes of the file
            byte[] header = new byte[8];
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Read(header, 0, header.Length);
            }

            // Check for PNG
            if (header.Take(PngMagic.Length).SequenceEqual(PngMagic))
            {
                return "png";
            }

            // Check for GIF
            if (header.Take(GifMagic.Length).SequenceEqual(GifMagic))
            {
                return "gif";
            }

            // Check for JPEG
            if (header.Take(JpegMagic.Length).SequenceEqual(JpegMagic))
            {
                return "jpeg";
            }

            // Unknown format
            Console.Error.WriteLine("Unknown format: " + BitConverter.ToString(header.Take(8).ToArray()));
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading file {filePath}: {ex.Message}");
            return null;
        }
    }
}