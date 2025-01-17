using System.IO;

public static class ExtractHandler
{
    public static string[] GetFiles(string folder)
    {
        // Construct the full path to the folder
        string fullPath = Path.Combine("Compiler", "extract", folder);

        // Ensure the directory exists
        if (!Directory.Exists(fullPath))
        {
            Console.WriteLine($"Directory not found: {fullPath}");
            return Array.Empty<string>();
        }

        // Get all .nitro files in the directory
        return Directory.GetFiles(fullPath, "*.nitro");
    }

    public static byte[] ReadFile(string filePath)
    {
        // Replace this with your logic to read a file into a byte array
        return File.ReadAllBytes(filePath);
    }
}