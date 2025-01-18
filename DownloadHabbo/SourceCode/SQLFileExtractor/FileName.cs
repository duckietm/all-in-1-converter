using System.Text.Json;

namespace ConsoleApplication
{
    public static class NitroFileExtractor
    {
        public static async Task ExtractNitroFile(string nitroFilePath, string outputDir)
        {
            if (!File.Exists(nitroFilePath))
            {
                throw new FileNotFoundException($"Nitro file not found: {nitroFilePath}");
            }

            byte[] data = await File.ReadAllBytesAsync(nitroFilePath);
            var bundle = new NitroBundle(data);

            Directory.CreateDirectory(outputDir);

            string name = Path.GetFileNameWithoutExtension(nitroFilePath);
            string jsonPath = Path.Combine(outputDir, $"{name}.json");
            string texturePath = Path.Combine(outputDir, $"{name}.png");

            if (bundle.JsonFile != null)
            {
                await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(bundle.JsonFile, new JsonSerializerOptions { WriteIndented = true }));
            }

            if (!string.IsNullOrEmpty(bundle.BaseTexture))
            {
                byte[] textureData = Convert.FromBase64String(bundle.BaseTexture);
                await File.WriteAllBytesAsync(texturePath, textureData);
            }
        }
    }
}
