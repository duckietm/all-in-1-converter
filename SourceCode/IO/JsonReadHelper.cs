using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Habbo_Downloader.IO
{
    public static class JsonReadHelper
    {
        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace
        };

        public static async Task<JObject> LoadJObjectAsync(string path)
        {
            var raw = await File.ReadAllTextAsync(path);
            using var sr = new StringReader(raw);
            using var jr = new JsonTextReader(sr);
            return JObject.Load(jr, LoadSettings);
        }

        public static JObject LoadJObject(string path)
        {
            var raw = File.ReadAllText(path);
            using var sr = new StringReader(raw);
            using var jr = new JsonTextReader(sr);
            return JObject.Load(jr, LoadSettings);
        }
    }
}
