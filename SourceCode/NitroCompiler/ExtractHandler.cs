public static class ExtractHandler
{
    public static string[] GetFiles(string folder)
    {
        string fullPath = Path.Combine("NitroCompiler", "extract", folder);

        if (!Directory.Exists(fullPath))
        {
            Console.WriteLine($"Directory not found: {fullPath}");
            return Array.Empty<string>();
        }
        return Directory.GetFiles(fullPath, "*.nitro");
    }

    public static byte[] ReadFile(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }
}