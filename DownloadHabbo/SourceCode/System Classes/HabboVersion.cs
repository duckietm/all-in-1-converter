using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    internal static class VersionChecker
    {
        internal static async Task CheckVersionAsync()
        {
            string release_ver;
            HttpClient httpClient_version_ver = new HttpClient();

            // Use the shared UserAgent from CommonConfig
            httpClient_version_ver.DefaultRequestHeaders.Add("User-Agent", CommonConfig.UserAgent);

            try
            {
                HttpResponseMessage res = await httpClient_version_ver.GetAsync("https://www.habbo.com/gamedata/external_variables/1");
                string source = await res.Content.ReadAsStringAsync();

                foreach (string Line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (!Line.Contains("flash.client.url="))
                    {
                        continue;
                    }
                    release_ver = Line.Substring(0, Line.Length - 1).Split('/')[4];
                    Console.WriteLine("Current habbo release: " + release_ver);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}