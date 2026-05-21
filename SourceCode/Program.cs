using System.Threading.Tasks;

namespace ConsoleApplication
{
    internal class Program
    {
        private static Task<int> Main(string[] args)
            => Habbo_Downloader.App.App.RunAsync(args);
    }

    public static class UserAgentClass
    {
        public static string UserAgent { get; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
    }
}
