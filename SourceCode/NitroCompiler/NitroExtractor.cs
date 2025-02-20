using System.Text.Json;

namespace ConsoleApplication
{
    public static class NitroExtractor
    {
        public static async Task Extract()
        {
            Console.WriteLine("Starting Nitro Furniture Extraction...");

            await ExtractFiles("furni");
            await ExtractFiles("clothing");
            await ExtractFiles("effects");
            await ExtractFiles("pets");

            Console.WriteLine("Nitro Furniture Extraction completed.");
        }

        private static async Task ExtractFiles(string folder)
        {
            string[] files = ExtractHandler.GetFiles(folder);
            if (files.Length == 0) return;

            Console.WriteLine($"Extracting {files.Length} {folder} files...");

            foreach (var file in files)
            {
                byte[] data = ExtractHandler.ReadFile(file);
                var bundle = new NitroBundle(data);

                string name = Path.GetFileNameWithoutExtension(file);
                await ExtractedHandler.SaveExtractedFiles(folder, name, JsonSerializer.Serialize(bundle.JsonFile, new JsonSerializerOptions { WriteIndented = true }), bundle.BaseTexture);
            }
        }
    }
}