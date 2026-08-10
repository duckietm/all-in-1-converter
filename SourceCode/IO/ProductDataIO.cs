using Newtonsoft.Json.Linq;

namespace Habbo_Downloader.IO;

/// <summary>Reads and writes a single strict ProductData JSON file.</summary>
public static class ProductDataIO
{
    public const string FlatFileName = "ProductData.json";

    public static Task<JObject> LoadAsync(string path) => JsonReadHelper.LoadJObjectAsync(path);

    public static Task SaveAsync(JObject data, string path) => JsonReadHelper.SaveJObjectAsync(data, path);
}
