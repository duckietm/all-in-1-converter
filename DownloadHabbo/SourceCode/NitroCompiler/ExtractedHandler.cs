public static class ExtractedHandler
{
    public static Task SaveExtractedFiles(string folder, string name, string jsonContent, string base64Image)
    {
        if (string.IsNullOrEmpty(base64Image))
        {
            Console.WriteLine($"Base64 image data is null or empty for file: {name}");
            return Task.CompletedTask;
        }

        try
        {
            string outputFolder = Path.Combine("NitroCompiler", "extracted", folder, name);

            if (!Directory.Exists(outputFolder))
            {
                Console.WriteLine($"Creating output directory: {outputFolder}");
                Directory.CreateDirectory(outputFolder);
            }

            string jsonFilePath = Path.Combine(outputFolder, $"{name}.json");
            File.WriteAllText(jsonFilePath, jsonContent);
            Console.WriteLine($"Saved JSON file: {jsonFilePath}");

            string textureFilePath = Path.Combine(outputFolder, $"{name}.png");
            File.WriteAllBytes(textureFilePath, Convert.FromBase64String(base64Image));
            Console.WriteLine($"Saved texture file: {textureFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save extracted files for {name}: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}