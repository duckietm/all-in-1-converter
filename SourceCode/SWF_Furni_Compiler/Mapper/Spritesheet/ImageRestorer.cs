namespace Habbo_Downloader.Tools
{
    public static class ImageRestorer
    {
        // Restores images from the TMP directory into the images directory using the provided in the memory image mapping.
        public static async Task RestoreImagesFromTmpAsync(string tmpDirectory, string imagesDirectory, Dictionary<string, string> imageMapping)
        {
            if (!Directory.Exists(tmpDirectory))
            {
                Console.WriteLine($"❌ TMP directory not found: {tmpDirectory}");
                return;
            }

            if (imageMapping == null || imageMapping.Count == 0)
            {
                Console.WriteLine("❌ In-memory image mapping is empty.");
                return;
            }

            // Ensure the target images directory exists.
            Directory.CreateDirectory(imagesDirectory);

            // Get all PNG files in the TMP directory.
            var tmpFiles = Directory.GetFiles(tmpDirectory, "*.png", SearchOption.AllDirectories);

            foreach (var tmpFile in tmpFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(tmpFile);
                string extension = Path.GetExtension(tmpFile);

                // Extract the ID from the file name (everything before the first '_').
                string id = fileName.Split('_')[0];

                if (!imageMapping.TryGetValue(id, out string correctName))
                {
                    continue;
                }

                // Construct the new filename with the correct name.
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
