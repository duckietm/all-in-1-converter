using System.Threading.Tasks;

namespace Habbo_Downloader.App.Runners
{
    public interface IAppRunner
    {
        Task RunAsync(Args args);
    }
}
