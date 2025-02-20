namespace Habbo_Downloader.Tools
{
    public static class ImageRestorerClothes
    {
        public static async Task RestoreImagesFromTmpAsync(string tmpDirectory, string imagesDirectory, Dictionary<string, string> imageMapping)
        {
            if (!Directory.Exists(tmpDirectory))
            {
                Console.WriteLine($"❌ TMP directory not found: {tmpDirectory}");
                return;
            }

            if (imageMapping == null || imageMapping.Count == 0)
            {
                Console.WriteLine($"tmpDirectory : {tmpDirectory}");
                Console.WriteLine($"imagesDirectory: {imagesDirectory}");
                Console.WriteLine("❌ In-memory image mapping is empty.");
                return;
            }

            Directory.CreateDirectory(imagesDirectory);

            var tmpFiles = Directory.GetFiles(tmpDirectory, "*.png", SearchOption.AllDirectories);

            foreach (var tmpFile in tmpFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(tmpFile);
                string extension = Path.GetExtension(tmpFile);

                string id = fileName.Split('_')[0];

                if (!imageMapping.TryGetValue(id, out string correctName))
                {
                    continue;
                }

                string newFileName = $"{correctName}{extension}";
                string newFilePath = Path.Combine(imagesDirectory, newFileName);

                try
                {
                    File.Move(tmpFile, newFilePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error moving {fileName} to {newFilePath}: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }
    }
}