using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Habbo_Downloader.IO
{
    public static class JsonReadHelper
    {
        public static async Task<JObject> LoadJObjectAsync(string path)
        {
            ValidateJsonFilePath(path);
            var raw = await File.ReadAllTextAsync(path);
            return ParseStrict(raw, path);
        }

        public static JObject LoadJObject(string path)
        {
            ValidateJsonFilePath(path);
            var raw = File.ReadAllText(path);
            return ParseStrict(raw, path);
        }

        public static async Task SaveJObjectAsync(JObject data, string path)
        {
            ArgumentNullException.ThrowIfNull(data);
            ValidateJsonDestination(path);
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(path, data.ToString(Formatting.None));
        }

        private static JObject ParseStrict(string raw, string path)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(raw, new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow
                });
                return JObject.Parse(document.RootElement.GetRawText());
            }
            catch (System.Text.Json.JsonException exception)
            {
                throw new InvalidDataException($"Invalid strict JSON file: {path}", exception);
            }
        }

        private static void ValidateJsonFilePath(string path)
        {
            ValidateJsonDestination(path);
            if (Directory.Exists(path))
                throw new NotSupportedException("Directory and split-layout inputs are not supported. Select one JSON file.");
            if (!File.Exists(path)) throw new FileNotFoundException($"JSON file not found: {path}", path);
        }

        private static void ValidateJsonDestination(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A JSON file path is required.", nameof(path));
            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Only single .json files are supported.");
        }
    }
}
