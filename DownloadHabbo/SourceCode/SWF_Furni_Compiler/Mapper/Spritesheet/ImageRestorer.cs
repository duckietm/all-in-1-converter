namespace Habbo_Downloader.Tools
{
    public static class ImageRestorer
    {
        public static async Task RestoreImagesFromTmpAsync(string tmpDirectory, string imagesDirectory, string imageMappingCsvPath)
        {
            if (!Directory.Exists(tmpDirectory))
            {
                Console.WriteLine($"❌ TMP directory not found: {tmpDirectory}");
                return;
            }

            if (!File.Exists(imageMappingCsvPath))
            {
                Console.WriteLine($"❌ image_mapping.csv not found: {imageMappingCsvPath}");
                return;
            }

            // Read the image mapping CSV and create a dictionary
            var imageMappings = new Dictionary<string, string>();
            using (var reader = new StreamReader(imageMappingCsvPath))
            {
                // Skip the header line
                await reader.ReadLineAsync();

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        string id = parts[0].Trim();        // ID from CSV
                        string correctName = parts[1].Trim(); // Correct image name
                        imageMappings[id] = correctName;
                    }
                }
            }

            // Ensure the target images directory exists
            Directory.CreateDirectory(imagesDirectory);

            // Get all PNG files in the TMP directory
            var tmpFiles = Directory.GetFiles(tmpDirectory, "*.png", SearchOption.AllDirectories);

            foreach (var tmpFile in tmpFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(tmpFile);
                string extension = Path.GetExtension(tmpFile);

                // Extract the ID from the file name (everything before the first '_')
                string id = fileName.Split('_')[0];

                if (!imageMappings.TryGetValue(id, out string correctName))
                {
                    continue;
                }

                // Construct the new filename with the correct name
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
        }
    }
}
